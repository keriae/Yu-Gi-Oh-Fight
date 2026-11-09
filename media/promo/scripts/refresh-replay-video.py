#!/usr/bin/env python3
"""Replace the two replay shots in a finished trailer while retaining its design."""
from __future__ import annotations

import argparse
import hashlib
import json
import math
import os
from pathlib import Path
import subprocess
import tempfile

FPS = 60
WIDTH, HEIGHT = 1920, 1080
PAGE_WIDTH, PAGE_HEIGHT = 1840, 924
PAGE_X, PAGE_Y = 40, 24
FADE = 0.3
DURATION = 90.0
SHOTS = (("duel", 55.9, 12.0), ("review", 79.1, 5.2))


def digest(path: Path) -> str:
    value = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            value.update(block)
    return value.hexdigest()


def binary_environment(binary: Path) -> dict[str, str]:
    environment = dict(os.environ)
    # Some FFprobe distributions keep their dylibs beside the executable.
    if any(binary.parent.glob("libav*.dylib")):
        existing = environment.get("DYLD_LIBRARY_PATH", "")
        environment["DYLD_LIBRARY_PATH"] = str(binary.parent) + (":" + existing if existing else "")
    return environment


def probe(binary: Path, path: Path) -> dict:
    result = subprocess.run(
        [str(binary), "-v", "error", "-show_streams", "-show_format", "-of", "json", str(path)],
        env=binary_environment(binary), check=True, capture_output=True, text=True,
    )
    data = json.loads(result.stdout)
    video = next(stream for stream in data["streams"] if stream["codec_type"] == "video")
    numerator, denominator = video["avg_frame_rate"].split("/")
    return {"width": video["width"], "height": video["height"],
            "fps": int(numerator) / int(denominator),
            "duration_seconds": float(data["format"]["duration"]),
            "video_duration_seconds": float(video.get("duration", data["format"]["duration"])),
            "audio": any(stream["codec_type"] == "audio" for stream in data["streams"])}


def number(value: float) -> str:
    return format(value, ".9g")


def envelope(start: float, duration: float) -> str:
    # Use one envelope for the image and sound so both transitions stay aligned.
    begin, end = number(start), number(start + duration)
    return f"if(between(t,{begin},{end}),min(min((t-{begin})/{FADE},({end}-t)/{FADE}),1),0)"


def filters(duel_start: float, review_start: float) -> str:
    stages = ["[0:v]fps=60,setpts=PTS-STARTPTS[base]", "[1:v]split=2[duel_input][review_input]"]
    for (name, start, duration), source_start in zip(SHOTS, (duel_start, review_start)):
        stages.append(
            f"[{name}_input]trim=start={number(source_start)}:duration={number(duration)},"
            "setpts=PTS-STARTPTS,fps=60,"
            f"scale={PAGE_WIDTH}:{PAGE_HEIGHT}:force_original_aspect_ratio=decrease:force_divisible_by=2:flags=lanczos,"
            f"pad={PAGE_WIDTH}:{PAGE_HEIGHT}:(ow-iw)/2:(oh-ih)/2:color=0x050e16,format=rgba,"
            f"fade=t=in:st=0:d={FADE}:alpha=1,"
            f"fade=t=out:st={number(duration-FADE)}:d={FADE}:alpha=1,"
            f"setpts=PTS+{number(start)}/TB[{name}_video]"
        )
    stages.append(
        f"[base][duel_video]overlay={PAGE_X}:{PAGE_Y}:eof_action=pass:repeatlast=0:"
        "enable='between(t,55.9,67.9)'[first]"
    )
    stages.append(
        f"[first][review_video]overlay={PAGE_X}:{PAGE_Y}:eof_action=pass:repeatlast=0:"
        "enable='between(t,79.1,84.3)',format=yuv420p[video]"
    )
    baseline_gain = "*".join(f"(1-({envelope(start,duration)}))" for _, start, duration in SHOTS)
    stages.append(f"[0:a]aresample=48000,volume='{baseline_gain}':eval=frame[original_audio]")
    stages.append("[1:a]asplit=2[duel_audio_input][review_audio_input]")
    for (name, start, duration), source_start in zip(SHOTS, (duel_start, review_start)):
        stages.append(
            f"[{name}_audio_input]atrim=start={number(source_start)}:duration={number(duration)},"
            "asetpts=PTS-STARTPTS,aresample=48000,volume=0.65,"
            f"afade=t=in:st=0:d={FADE},afade=t=out:st={number(duration-FADE)}:d={FADE},"
            f"adelay={round(start*1000)}:all=1[{name}_audio]"
        )
    stages.append("[original_audio][duel_audio][review_audio]amix=inputs=3:duration=first:normalize=0[audio]")
    return ";\n".join(stages)


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--baseline", type=Path, required=True, help="Original finished 90-second EN or ZH trailer")
    parser.add_argument("--replay", type=Path, required=True, help="Fresh full-resolution 60 FPS replay with game sound")
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--duel-start", type=float, required=True, help="Duel shot offset in the fresh replay, in seconds")
    parser.add_argument("--review-start", type=float, required=True, help="Review shot offset in the fresh replay, in seconds")
    parser.add_argument("--ffmpeg", type=Path, required=True, help="Full FFmpeg executable with scale, overlay, fade and audio filters")
    parser.add_argument("--ffprobe", type=Path, help="Defaults to ffprobe beside the FFmpeg executable")
    parser.add_argument("--report", type=Path, help="Defaults to OUTPUT.refresh.json; source paths are recorded as basenames only")
    parser.add_argument("--overwrite", action="store_true")
    parser.add_argument("--check", action="store_true", help="Validate source metadata and print the shot plan without rendering")
    args = parser.parse_args()
    baseline, replay, ffmpeg = (path.expanduser().resolve(strict=True) for path in (args.baseline, args.replay, args.ffmpeg))
    ffprobe = (args.ffprobe or ffmpeg.with_name("ffprobe")).expanduser().resolve(strict=True)
    output = args.output.expanduser().absolute()
    report = (args.report or output.with_name(output.name + ".refresh.json")).expanduser().absolute()
    if output.suffix.lower() != ".mp4" or output.is_symlink() or report.is_symlink():
        parser.error("Output must be a regular MP4 destination and report must not be a symlink")
    if output.resolve() in (baseline, replay) or report.resolve() in (baseline, replay, output.resolve()):
        parser.error("Output and report must be separate from source recordings")
    if output.exists() and not args.overwrite:
        parser.error("Output exists; pass --overwrite to replace it after successful rendering")
    for offset in (args.duel_start, args.review_start):
        if not math.isfinite(offset) or offset < 0:
            parser.error("Replay offsets must be finite and nonnegative")
    baseline_metadata, replay_metadata = probe(ffprobe, baseline), probe(ffprobe, replay)
    if (baseline_metadata["width"], baseline_metadata["height"]) != (WIDTH, HEIGHT) or abs(baseline_metadata["duration_seconds"]-DURATION) > 0.2:
        parser.error("Baseline must be the original 1920×1080, 90-second trailer")
    if replay_metadata["width"] < WIDTH or replay_metadata["height"] < HEIGHT or replay_metadata["fps"] < FPS-0.01:
        parser.error("Replay must contain full HD or higher footage rendered at 60 FPS or higher")
    if not baseline_metadata["audio"] or not replay_metadata["audio"]:
        parser.error("Both source videos must contain audio")
    for offset, (_, _, duration) in zip((args.duel_start, args.review_start), SHOTS):
        if offset + duration > replay_metadata["duration_seconds"] + 0.01:
            parser.error("A selected shot extends beyond the fresh replay")
    record = {"schema": 1, "kind": "trailer-replay-shot-refresh", "baseline": baseline.name,
              "baseline_sha256": digest(baseline), "replay": replay.name, "replay_sha256": digest(replay),
              "baseline_metadata": baseline_metadata, "replay_metadata": replay_metadata,
              "fps": FPS, "canvas_pixels": [WIDTH, HEIGHT],
              "game_viewport_pixels": [PAGE_X, PAGE_Y, PAGE_WIDTH, PAGE_HEIGHT],
              "transition_seconds": FADE, "shots": [
                  {"name": name, "timeline_start_seconds": start, "duration_seconds": duration, "replay_start_seconds": offset}
                  for (name, start, duration), offset in zip(SHOTS, (args.duel_start, args.review_start))],
              "preserved": "All other shots, original footer, text, colors, design and timeline"}
    if args.check:
        print(json.dumps(record, ensure_ascii=False))
        return
    output.parent.mkdir(parents=True, exist_ok=True)
    report.parent.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(prefix=".trailer-refresh-", dir=output.parent) as directory:
        work = Path(directory)
        graph = work / "filters.txt"
        graph.write_text(filters(args.duel_start, args.review_start), encoding="utf-8")
        staged = work / output.name
        subprocess.run([str(ffmpeg), "-hide_banner", "-y", "-i", str(baseline), "-i", str(replay),
                        "-filter_complex_script", str(graph), "-map", "[video]", "-map", "[audio]",
                        "-t", str(DURATION), "-r", str(FPS), "-c:v", "libx264", "-preset", "medium",
                        "-crf", "20", "-pix_fmt", "yuv420p", "-c:a", "aac", "-b:a", "192k", "-ar", "48000",
                        "-movflags", "+faststart", "-map_metadata", "-1", str(staged)],
                       env=binary_environment(ffmpeg), check=True)
        actual = probe(ffprobe, staged)
        if (actual["width"], actual["height"], actual["fps"]) != (WIDTH, HEIGHT, float(FPS)) or abs(actual["video_duration_seconds"]-DURATION) > 1/FPS or not actual["audio"]:
            raise ValueError("Rendered trailer failed dimensions, cadence, duration or audio verification")
        if digest(baseline) != record["baseline_sha256"] or digest(replay) != record["replay_sha256"]:
            raise ValueError("Source recording changed during rendering")
        if output.exists() and not args.overwrite:
            raise FileExistsError("Destination appeared during rendering; staged output was not published")
        record.update({"ok": True, "output": output.name, "output_sha256": digest(staged), "output_metadata": actual})
        staged_report = work / "report.json"
        staged_report.write_text(json.dumps(record, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        os.replace(staged, output)
        os.replace(staged_report, report)
    print(json.dumps(record, ensure_ascii=False))


if __name__ == "__main__":
    main()
