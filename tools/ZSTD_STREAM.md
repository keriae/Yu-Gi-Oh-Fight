# Optional installed-libzstd reader

`zstd_stream.py` uses the installed `/opt/homebrew/lib/libzstd.dylib` through
`ctypes`. It does not install packages, spawn a decompressor, extract files,
change environment variables, or validate publisher archives on its own.

An optional downloader integration can replace **only** the decompressed
binary stream after the existing publisher archive size and SHA checks:

```python
from zstd_stream import open_zstd_stream

with open_zstd_stream(verified_archive_path) as decompressed:
    with tarfile.open(fileobj=decompressed, mode="r|") as archive:
        # Keep every existing data_filter, path containment check, member size
        # check, staged fsync/atomic rename and final publisher file SHA check.
        ...
# Record an extraction marker only after the outer context exits successfully.
```

The context drains the compressed stream after a successful consumer exit.
This validates the remaining frame/footer even when tar stops at its own end
marker. Invalid or truncated compressed input raises `ZstdError`. Decoder
contexts and files are closed on errors; caller-owned input streams stay open.
Concatenated frames and small reads are supported. A missing libzstd makes this
optional module unavailable; the existing CLI remains a suitable fallback.

Reproduce the cached-archive comparison without altering game files:

```sh
python3 tools/benchmark_zstd_stream.py --phase closeup-004 --count 200 \
  --output artifacts/zstd-stream-benchmark.json
```

The benchmark first verifies all selected cached packages against their
publisher size/SHA records. It compares full decompressed SHA and every tar
member's name, size and content SHA against the existing zstd CLI. Malformed
input, early tar termination, concatenated frames, valid empty frames, short
compressed reads and high compression ratio are checked separately.

Initial 200-package measurement: CLI 3.24 seconds, helper 0.028 seconds.
This measures decompression and in-memory tar reads only. Network requests,
atomic writes, fsync and per-file publisher SHA remain necessary and are not
included in that speed comparison. The generated JSON is temporary and is not committed.
