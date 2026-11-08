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

The downloader still verifies publisher package sizes and hashes, contained
paths, extracted file sizes and final file hashes before publishing resources.
Local comparison benchmarks and malformed-input fixtures are excluded from Git.
