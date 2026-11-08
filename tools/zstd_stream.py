"""Optional libzstd streaming reader; no package installation or extraction.

Callers must verify their archive's publisher size and SHA before opening it.
The context drains the compressed stream on successful exit, so tar readers
that stop at tar's end marker cannot skip frame/checksum/truncation errors.
No downloader, archive filter, file SHA, or atomic extraction gate is changed.
"""

from __future__ import annotations

from contextlib import contextmanager
import ctypes
import ctypes.util
import io
import os


class ZstdError(OSError):
    """The compressed stream is invalid or incomplete."""


class _Input(ctypes.Structure):
    _fields_ = [("src", ctypes.c_void_p), ("size", ctypes.c_size_t), ("pos", ctypes.c_size_t)]


class _Output(ctypes.Structure):
    _fields_ = [("dst", ctypes.c_void_p), ("size", ctypes.c_size_t), ("pos", ctypes.c_size_t)]


LIBRARY_PATH = ctypes.util.find_library("zstd")
if not LIBRARY_PATH:
    raise ImportError("An already installed libzstd is required for this optional helper")
_lib = ctypes.CDLL(LIBRARY_PATH)
_lib.ZSTD_createDStream.argtypes = []
_lib.ZSTD_createDStream.restype = ctypes.c_void_p
_lib.ZSTD_freeDStream.argtypes = [ctypes.c_void_p]
_lib.ZSTD_freeDStream.restype = ctypes.c_size_t
_lib.ZSTD_initDStream.argtypes = [ctypes.c_void_p]
_lib.ZSTD_initDStream.restype = ctypes.c_size_t
_lib.ZSTD_decompressStream.argtypes = [ctypes.c_void_p, ctypes.POINTER(_Output), ctypes.POINTER(_Input)]
_lib.ZSTD_decompressStream.restype = ctypes.c_size_t
_lib.ZSTD_isError.argtypes = [ctypes.c_size_t]
_lib.ZSTD_isError.restype = ctypes.c_uint
_lib.ZSTD_getErrorName.argtypes = [ctypes.c_size_t]
_lib.ZSTD_getErrorName.restype = ctypes.c_char_p
_lib.ZSTD_DStreamInSize.argtypes = []
_lib.ZSTD_DStreamInSize.restype = ctypes.c_size_t


def _check(code: int) -> int:
    if _lib.ZSTD_isError(code):
        raise ZstdError(_lib.ZSTD_getErrorName(code).decode("utf-8", "replace"))
    return code


class _Reader(io.RawIOBase):
    def __init__(self, source):
        super().__init__()
        self._owns_source = isinstance(source, (str, bytes, os.PathLike))
        self._source = open(source, "rb") if self._owns_source else source
        self._context = _lib.ZSTD_createDStream()
        if not self._context:
            if self._owns_source:
                self._source.close()
            raise MemoryError("libzstd could not allocate a decompression context")
        try:
            _check(_lib.ZSTD_initDStream(self._context))
        except BaseException:
            self.close()
            raise
        self._input = _Input(None, 0, 0)
        self._storage = None
        self._input_size = _lib.ZSTD_DStreamInSize()
        self._last_hint = 1
        self._seen_input = False
        self._finished = False

    def readable(self):
        return True

    def readinto(self, buffer):
        self._checkClosed()
        if self._finished or not len(buffer):
            return 0
        view = memoryview(buffer).cast("B")
        target = (ctypes.c_ubyte * len(view)).from_buffer(view)
        output = _Output(ctypes.addressof(target), len(view), 0)
        while not output.pos:
            if self._input.pos == self._input.size:
                data = self._source.read(self._input_size)
                if not data:
                    if not self._seen_input or self._last_hint != 0:
                        raise ZstdError("Truncated or empty Zstandard stream")
                    self._finished = True
                    return 0
                self._seen_input = True
                self._storage = ctypes.create_string_buffer(data, len(data))
                self._input = _Input(ctypes.addressof(self._storage), len(data), 0)
            before = self._input.pos
            self._last_hint = _check(_lib.ZSTD_decompressStream(
                self._context, ctypes.byref(output), ctypes.byref(self._input)))
            if not output.pos and self._input.pos == before:
                raise ZstdError("Zstandard decoder made no progress")
        return output.pos

    def close(self):
        if not self.closed:
            context = getattr(self, "_context", None)
            if context:
                _lib.ZSTD_freeDStream(context)
                self._context = None
            if getattr(self, "_owns_source", False):
                self._source.close()
        super().close()


@contextmanager
def open_zstd_stream(source):
    """Yield a binary buffered reader and validate all frames on clean exit.

    Accept an archive path or a caller-owned binary stream. Invalid trailing
    bytes and truncated frames raise ZstdError. Concatenated frames work.
    """
    stream = io.BufferedReader(_Reader(source), buffer_size=128 * 1024)
    try:
        yield stream
        while stream.read(128 * 1024):
            pass
    finally:
        stream.close()
