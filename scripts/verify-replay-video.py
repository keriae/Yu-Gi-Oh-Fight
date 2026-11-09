#!/usr/bin/env python3
"""Verify native replay encoding, frame cadence and audible stereo PCM."""
from pathlib import Path
import argparse
import ctypes
import math
import hashlib
import json
import subprocess
import tempfile
import time

ROOT = Path(__file__).resolve().parents[1]

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--library', type=Path, default=ROOT / 'build/libReplayVideo.dylib')
    parser.add_argument('--ffmpeg', type=Path, required=True)
    parser.add_argument('--long', action='store_true', help='Exercise encoder backpressure with six seconds of 1080p60 video')
    parser.add_argument('--report', type=Path, help='Optional JSON verification report')
    args = parser.parse_args()
    library = args.library.expanduser().resolve(strict=True)
    native = ctypes.CDLL(str(library))
    native.ReplayVideoCreate.argtypes = [ctypes.c_char_p] + [ctypes.c_int] * 5
    native.ReplayVideoCreate.restype = ctypes.c_void_p
    native.ReplayVideoAppend.argtypes = [ctypes.c_void_p, ctypes.c_void_p, ctypes.c_int, ctypes.c_void_p, ctypes.c_int]
    native.ReplayVideoAppend.restype = ctypes.c_int
    native.ReplayVideoFinish.argtypes = [ctypes.c_void_p]
    native.ReplayVideoFinish.restype = ctypes.c_int
    native.ReplayVideoError.argtypes = [ctypes.c_void_p]
    native.ReplayVideoError.restype = ctypes.c_char_p
    width, height, frames = (1920, 1080, 360) if args.long else (320, 240, 60)
    bitrate = 29859840 if args.long else 4000000
    timings = []
    with tempfile.TemporaryDirectory(prefix='replay-video-smoke-') as directory:
        output = Path(directory) / 'native.mp4'
        writer = native.ReplayVideoCreate(str(output).encode(), width, height, 60, bitrate, 1)
        assert writer, native.ReplayVideoError(None)
        for frame in range(frames):
            # Leave enough color separation to survive normal H.264 quantization.
            rgba = bytes([(frame % 30) * 8, (frame // 30) * 20, 96, 255]) * (width * height)
            data = ctypes.create_string_buffer(rgba)
            samples = (ctypes.c_float * 1600)(*[.35 * math.sin(2 * math.pi * 440 * (frame * 800 + n // 2) / 48000) for n in range(1600)])
            before = time.monotonic()
            assert native.ReplayVideoAppend(writer, data, 1, samples, 800), native.ReplayVideoError(writer)
            timings.append(time.monotonic() - before)
        assert native.ReplayVideoFinish(writer), native.ReplayVideoError(None)
        command = [str(args.ffmpeg), '-v', 'error', '-i', str(output)]
        # Reduce each decoded frame to one pixel before reading stdout: the long
        # test must not accumulate more than two gigabytes of raw HD frames.
        video = subprocess.run(command + ['-an', '-vf', 'scale=1:1:flags=area', '-fps_mode', 'passthrough',
                                          '-pix_fmt', 'rgb24', '-f', 'rawvideo', '-'], check=True, capture_output=True).stdout
        assert len(video) == 3 * frames, len(video)
        colors = [video[n * 3:n * 3 + 3] for n in range(frames)]
        assert len(set(colors)) == frames, len(set(colors))
        audio = subprocess.run(command + ['-vn', '-ac', '2', '-ar', '48000', '-f', 'f32le', '-'], check=True, capture_output=True).stdout
        values = memoryview(audio).cast('f')
        rms = math.sqrt(sum(sample * sample for sample in values) / len(values))
        assert len(audio) >= 48000 * 2 * 4 * (frames / 60) and rms > .1, (len(audio), rms)
        result = {'ok': True, 'frames': frames, 'fps': 60, 'duration_seconds': frames / 60,
                  'resolution': str(width) + 'x' + str(height), 'distinct_frames': len(set(colors)),
                  'audio_rms': round(rms, 3), 'max_append_seconds': max(timings),
                  'native_library_sha256': hashlib.sha256(library.read_bytes()).hexdigest()}
        if args.report:
            args.report.parent.mkdir(parents=True, exist_ok=True)
            args.report.write_text(json.dumps(result, indent=2) + '\n')
        print(json.dumps(result))

if __name__ == '__main__':
    main()
