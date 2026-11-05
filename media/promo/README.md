# Gameplay trailer

The 90-second English/Chinese trailer is composed from recordings of the real
macOS game window. The captured game audio is retained, with short fades and a recorded menu
theme under the paused menus and end card; no synthetic gameplay
or stock footage is used. Remotion and React are pinned in `package-lock.json`.

Use Node.js 24 or later, and run these commands from `media/promo`. Place
the source recordings listed in `footage.json` under `public/footage/` before
previewing or rendering:

```sh
npm ci
npm run preview
npm run render
```

The raw recordings, runtime dependencies and intermediate frames are local
production files and intentionally ignored by Git. Public output is in the
repository-root [docs/media](../../docs/media/) directory; redistribute gameplay
media subject to the project's
[third-party notices](../../THIRD_PARTY_NOTICES.md).

The capture canvas is 1920×1200. The final home/menu/replay clips contain
bottom black padding; their effective heights are recorded in `footage.json`.
`ScreenScene` frames the recordings for the
1920×1080 trailer and adds chapter labels and bilingual captions. The editor
scene uses a closer view of the card text and loaded deck.
The released video uses macOS Avenir Next and PingFang SC. Other platforms
fall back to their system sans-serif fonts unless equivalent fonts are installed.
Each scene is separately selectable in Remotion Studio. All timing and fades
are driven by frame numbers.

The narrative runs from deck building to academy learning, AI preparation,
brief duel action, the redesigned duel menus, and replay review. A separate
complete duel recording accompanies the trailer.

The separate `docs/media/full-duel.mp4` keeps the opening, all three turns, and
victory from 26–149 seconds of `full-replay-zh.mp4`. Only the bottom black
padding is cropped; the original AAC audio stream is copied. Reproduce it
from this directory with a full FFmpeg build that includes the `crop` filter:

```sh
ffmpeg -ss 26 -i public/footage/full-replay-zh.mp4 -t 123 \
  -vf "crop=1920:1092:0:0,fps=30" -c:v libx264 -preset medium -crf 20 \
  -maxrate 4800k -bufsize 9600k -pix_fmt yuv420p -c:a copy \
  -movflags +faststart -map_metadata -1 ../../docs/media/full-duel.mp4
```
