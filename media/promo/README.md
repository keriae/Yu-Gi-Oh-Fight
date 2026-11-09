# Gameplay trailers

The separate 90-second English and Simplified Chinese trailers retain their
original design, chapter timing, captions and menu recordings. Their duel and
replay-playback shots use the client's 1920×1080, 60 FPS replay export with the
corresponding interface language and game audio. The other shots retain their
original recorded audio, including the menu theme under paused menus and the
closing card. No synthetic gameplay or stock footage is used. Remotion and
React are pinned in `package-lock.json`.

Use Node.js 24 or later, and run these commands from `media/promo`. Place the
source recordings listed in `footage.json` under `public/footage/` before
previewing or reconstructing the original 30 FPS design baselines:

```sh
npm ci
npm run preview
npx remotion render src/index.ts LaunchTrailerEN public/footage/launch-trailer-en-baseline.mp4 --codec h264 --crf 24
npx remotion render src/index.ts LaunchTrailerZH public/footage/launch-trailer-zh-baseline.mp4 --codec h264 --crf 24
```

The released 60 FPS videos are made from those finished baselines with
`scripts/refresh-replay-video.py`. It replaces only 55.9–67.9 seconds and
79.1–84.3 seconds, fits the complete exported game view above the existing
footer, and applies matching 0.3-second image and sound fades. All other
frames, captions and design remain sourced from the baseline. The original
menu recordings retain their original motion cadence in the 60 FPS output.
Set `FFMPEG` and `FFPROBE` to a full FFmpeg build and its FFprobe executable.
The two selected replay offsets are recorded separately for each language in
`footage.json`:

```sh
python3 scripts/refresh-replay-video.py \
  --baseline public/footage/launch-trailer-en-baseline.mp4 \
  --replay public/footage/replay-export-en.mp4 \
  --output ../../docs/media/launch-trailer-en.mp4 \
  --duel-start 23.3 --review-start 211 \
  --ffmpeg "$FFMPEG" --ffprobe "$FFPROBE" --overwrite \
  --report ../../build/media-refresh/en-refresh.json
```

Repeat with `zh` and that language's recorded offsets. After both videos,
GIFs and `preview-en.jpg` / `preview-zh.jpg` exist, run
`FFPROBE="$FFPROBE" npm run outputs` to regenerate the unchanged subtitle files
and record final output hashes, dimensions, frame counts and audio metadata.

The repository homepage embeds separate full-length English and Chinese GIFs.
Each runs for 90 seconds at 720×405 and 10 fps, with a 256-color palette,
Bayer dithering at scale 3, rectangle difference optimization, and infinite
looping. GIFs have no audio; clicking either animation opens its original MP4
with game audio. To regenerate a GIF from this directory with a full FFmpeg
build, replace `en` with `zh` for the Chinese version:

```sh
ffmpeg -i ../../docs/media/launch-trailer-en.mp4 \
  -vf "fps=10,scale=720:-1:flags=lanczos,palettegen=stats_mode=diff" \
  -frames:v 1 /tmp/yu-gi-oh-fight-en-palette.png
ffmpeg -i ../../docs/media/launch-trailer-en.mp4 \
  -i /tmp/yu-gi-oh-fight-en-palette.png \
  -filter_complex "[0:v]fps=10,scale=720:-1:flags=lanczos[v];[v][1:v]paletteuse=dither=bayer:bayer_scale=3:diff_mode=rectangle" \
  -an -loop 0 -gifflags +transdiff ../../docs/media/launch-trailer-en.gif
ffmpeg -ss 63 -i ../../docs/media/launch-trailer-en.mp4 \
  -frames:v 1 -q:v 2 ../../docs/media/preview-en.jpg
```

Raw recordings, dependencies, and intermediate frames are local production
files and intentionally ignored by Git. Public output is in the repository-root
[docs/media](../../docs/media/) directory; redistribute gameplay media subject
to the project's [third-party notices](../../THIRD_PARTY_NOTICES.md).

The original menu capture canvas is 1920×1200; new replay exports are
1920×1080 at a fixed 60 FPS with stereo AAC at 48 kHz. The complete game page
is fitted proportionally and centered above a separate footer in the
1920×1080 trailer. Only original recorded bottom black padding identified by
`content_height_pixels` is omitted; game content is never cropped or enlarged.
The new exported game view has no crop. `ScreenScene` uses the selected language
for both recording and subtitle. Each scene has one centered bottom row
containing its chapter label and a single-language caption, measured within
the safe width. This footer sits below the game page and covers no controls.
The homepage remains visible without a central title overlay. The closing card
uses the actual project logo, the complete repository URL, and a single-language
unofficial, non-commercial notice.

The trailer uses self-hosted SIL OFL 1.1 fonts under `public/fonts/`: Cinzel at
600 for English and Noto Serif SC at 600 for Chinese. The font sources, hashes,
and Chinese subset corpus are recorded in `public/fonts/provenance.json`,
with the original licenses. Loading completes before text measurement or
rendering. All timing and fades are driven by frame numbers.

The narrative runs from deck building to academy learning, AI preparation,
brief duel action, the redesigned duel menus, and replay review. The separate
`docs/media/full-duel.mp4` keeps every one of the 14,049 rendered frames of
`replay-export-zh.mp4`, from the first replay packet through victory and the
10.5-second ending tail. It remains 1920×1080 at 60 FPS with no crop, with
stereo AAC at 48 kHz and 192 kbps. H.264 uses CRF 18 with a bitrate limit chosen
for the 234.15-second duration so the Git-hosted video stays below 100 MB.
The high-bitrate native export remains a local production source.
Reproduce it from this directory:

```sh
ffmpeg -i public/footage/replay-export-zh.mp4 -t 234.15 \
  -map 0:v:0 -map 0:a:0 -r 60 -c:v libx264 -preset medium -crf 18 \
  -maxrate 3087k -bufsize 6174k -pix_fmt yuv420p \
  -c:a aac -b:a 192k -ar 48000 -ac 2 \
  -movflags +faststart -map_metadata -1 ../../docs/media/full-duel.mp4
```
