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
The trailer uses self-hosted SIL OFL 1.1 fonts under `public/fonts/`: Cinzel
Decorative at 700 for the engraved gold title, Cinzel at 600 for English
captions, and Noto Serif SC at 600 for Chinese. The modified font families,
source revisions, hashes and Chinese subset corpus are recorded in
`public/fonts/provenance.json`; original licenses accompany the fonts. Font
loading completes before measuring or rendering text, so preview and export
do not depend on installed system fonts.

English and Chinese captions share one baseline and remain on one line,
with a thin gold separator. Captions are measured within the 1732-pixel safe
width; chapter and feature labels sit just above that row. The closing copyright
notice is also a single bilingual line.
Each scene is separately selectable in Remotion Studio. All timing and fades
are driven by frame numbers.

This typography revision also preserves the preceding trailer's mastered AAC
stream byte for byte. Its source revision and hashes are in `footage.json`.
To reproduce that exact audio master after rendering the new pictures:

```sh
npx remotion render src/index.ts LaunchTrailer out/typography-video.mp4 --codec h264 --crf 24
git show 2d3abbb2e10315b1bc590432a05565957a631103:docs/media/launch-trailer.mp4 > out/audio-master.mp4
npx remotion ffmpeg -y -i out/typography-video.mp4 -i out/audio-master.mp4 \
  -map 0:v:0 -map 1:a:0 -c copy -movflags +faststart -map_metadata -1 \
  ../../docs/media/launch-trailer.mp4
```

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
