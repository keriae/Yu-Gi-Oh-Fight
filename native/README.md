# Native bridges

`managed-tool-runner.c` runs the fixed publisher Mono catalog reader without
Unity scene initialization. It is used by `scripts/dump-shipping-catalog.py`
and is not installed as a replacement game runtime.

`ygoserver-host.c` hosts the matching publisher server in a separate process
and exits when its parent disappears. Game rule verification uses the
unchanged matching MDPro3 publisher core.

Original bridge contributions © 2026 keriae. Third-party libraries retain
their original licenses; see [the project notices](../THIRD_PARTY_NOTICES.md).
