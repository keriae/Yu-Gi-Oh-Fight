# Gameplay trailers

The separate 90-second English and Simplified Chinese trailers use recordings
of the real macOS game window, with the corresponding interface language.
Captured game audio is retained, with short fades and a recorded menu theme
under paused menus and the closing card. No synthetic gameplay or stock
footage is used. Remotion and React are pinned in `package-lock.json`.

Use Node.js 24 or later, and run these commands from `media/promo`. Place the
source recordings listed in `footage.json` under `public/footage/` before
previewing or rendering:

```sh
npm ci
npm run preview
npm run render
```

The `LaunchTrailerEN` and `LaunchTrailerZH` compositions export to
`docs/media/launch-trailer-en.mp4` and `docs/media/launch-trailer-zh.mp4`.
`npm run render:en` or `npm run render:zh` renders only one language.
`npm run outputs` regenerates the separate subtitle files and records final
output hashes after both videos and `preview-en.jpg` / `preview-zh.jpg` exist.

Raw recordings, dependencies, and intermediate frames are local production
files and intentionally ignored by Git. Public output is in the repository-root
[docs/media](../../docs/media/) directory; redistribute gameplay media subject
to the project's [third-party notices](../../THIRD_PARTY_NOTICES.md).

The capture canvas is 1920×1200. Captures with bottom black padding are framed
from the top for the 1920×1080 trailer. `ScreenScene` uses the selected language
for both recording and subtitle. Each scene has one bottom row containing its
chapter label and a single-language caption, measured within the safe width.
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
`docs/media/full-duel.mp4` keeps the opening, all three turns, and victory from
26–149 seconds of `full-replay-zh.mp4`. Only bottom black padding is cropped;
the original AAC audio stream is copied. Reproduce it from this directory with
a full FFmpeg build that includes the `crop` filter:

```sh
ffmpeg -ss 26 -i public/footage/full-replay-zh.mp4 -t 123 \
  -vf "crop=1920:1092:0:0,fps=30" -c:v libx264 -preset medium -crf 20 \
  -maxrate 4800k -bufsize 9600k -pix_fmt yuv420p -c:a copy \
  -movflags +faststart -map_metadata -1 ../../docs/media/full-duel.mp4
```
