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

Original bridge contributions © 2026 keriae. Third-party libraries retain
their original licenses; see [the project notices](../THIRD_PARTY_NOTICES.md).
