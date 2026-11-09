# Native bridges

`managed-tool-runner.c` runs the fixed publisher Mono catalog reader without
Unity scene initialization. It is used by `scripts/dump-shipping-catalog.py`
and is not installed as a replacement game runtime.

`ygoserver-host.c` hosts the matching publisher server in a separate process
and exits when its parent disappears. Game rule verification uses the
unchanged matching MDPro3 publisher core.

`StandaloneFileBrowser/` retains the macOS selector source and MIT license
from UnityStandaloneFileBrowser. Its pinned source and local changes are
recorded in `provenance.json`. The build produces a signed universal dylib
under the app's `Contents/PlugIns/` and a relative lookup link beside the
managed assembly, so both Unity and the publisher's Mono resolve the same
library. Native sources use ARC, main-thread AppKit calls and per-request
callbacks. Managed and native queues serialize asynchronous panels so each
result reaches its original caller; overlapping synchronous calls are rejected
by the managed wrapper. Cancellation returns an empty path.

`ReplayVideo/` implements MP4 replay export through AVFoundation. The signed
universal encoder receives every rendered RGBA frame and stereo PCM samples,
then writes H.264 video and AAC audio. Frame timestamps use the selected FPS;
Unity's offline audio renderer supplies the complete game mix, including
compressed clips, music, voices, effects and timelines. Mono and multichannel
audio are downmixed to stereo; input timestamps use the game's sample rate and
the AAC output uses 48 kHz.
An AppKit progress panel stays outside the exported framebuffer and supports
cancellation. The selected destination is replaced only after encoding succeeds;
temporary files are removed and the previous display, pacing and audio settings
are restored on completion, cancellation or failure.

The pinned Unity player retains its native capture-clock bindings but strips
their managed declarations. `ReplayVideo/CaptureClock.cs` contains the complete
declaration for a small separately loaded assembly. The native builder compiles
it with the pinned .NET SDK and .NET Framework reference package and embeds it
inside the encoder; `ReplayVideoClock.cs` loads that binding explicitly.
It sets the existing capture delta time,
so animations advance by one selected frame interval even when readback or
encoding takes longer. It does not replace the publisher's `UnityEngine.Time`
type or modify publisher assemblies. Replay packet timestamps are written to
a JSON sidecar for synchronizing exported footage with an existing edit.

`scripts/native_replay_video.py` builds both architectures and verifies the
encoder's exports, signature, source hashes and managed lookup link. Full
builds, code-only updates and package verification check this binding alongside
the file selector. `scripts/verify-replay-video.py` exercises native encoding
with rendered test frames and audio.

Original bridge contributions © 2026 keriae. Third-party libraries retain
their original licenses; see [the project notices](../THIRD_PARTY_NOTICES.md).
