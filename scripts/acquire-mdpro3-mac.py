#!/usr/bin/env python3
"""Acquire the published, native MyCard MDPro3 macOS distribution.

Every package is checked against the publisher's Metalink SHA-256 before
extraction. Downloads resume, and tarfile's data filter prevents extraction
outside this task's destination. This does not modify an existing app install.
"""

import concurrent.futures
import contextlib
import hashlib
import http.client
import importlib.util
import json
import os
from pathlib import Path
import subprocess
import ssl
import tarfile
import tempfile
import threading
import time
import urllib.request
import urllib.error
import urllib.parse
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
METADATA = ROOT / "vendor/MDPro3-mac-metadata"
PACKAGES = ROOT / "vendor/MDPro3-mac-packages"
DESTINATION = ROOT / "vendor/MDPro3-mac"
BASE = "https://sapi.moecube.com:444/release/update/"
ORIGIN = "https://minio.moenext.com:9000/mycard/apprelease/packages/"
SUFFIX = "mdpro3/1.4.7-2?platform=darwin&locale=zh-CN&arch=arm64"
NS = {"m": "urn:ietf:params:xml:ns:metalink"}
LOCK = threading.RLock()
NETWORK_GATE = threading.BoundedSemaphore(8)
MANIFEST = {}
MANIFEST_LAST_SAVE = 0.0
MANIFEST_CHECKPOINT_SECONDS = 0.5
RANGE_POOL = None
RANGE_SIZE = 1024 * 1024
VERBOSE = os.environ.get("MDPRO3_VERBOSE") == "1"
TLS_CONTEXT = ssl.create_default_context()
CONNECTIONS = threading.local()
SELECTION = os.environ.get("MDPRO3_SELECTION", "")
if SELECTION and (len(SELECTION) > 64 or not all(character.isascii() and (character.isalnum() or character == "-") for character in SELECTION)):
    raise RuntimeError("Selection name must contain only ASCII letters, digits and hyphens")
APP_FIRST = os.environ.get("MDPRO3_APP_FIRST") == "1" or bool(SELECTION)
PREFIX = SELECTION or "app-first"
VERIFICATION_NAME = f"{PREFIX}.verification.json" if APP_FIRST else "verification.json"
CHECKSUMS_SHA256 = "28361cbef68e3d825e2cd675421057976c61cda1870fcdffe64797d9c1782a3b"
CHECKSUMS_FILE_COUNT = 48347
ZSTD_READER = None
if os.environ.get("MDPRO3_NATIVE_ZSTD", "1") == "1":
    helper = ROOT / "tools/zstd_stream.py"
    if helper.is_file():
        try:
            helper_spec = importlib.util.spec_from_file_location("resource_zstd_stream", helper)
            ZSTD_READER = importlib.util.module_from_spec(helper_spec)
            helper_spec.loader.exec_module(ZSTD_READER)
        except (ImportError, OSError, AttributeError):
            # Keep the existing command-line path on hosts without libzstd.
            ZSTD_READER = None


def emit(message):
    with LOCK:
        print(message, flush=True)


def sha256(path):
    result = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(4 * 1024 * 1024), b""):
            result.update(chunk)
    return result.hexdigest()


@contextlib.contextmanager
def open_package(request, timeout=60, redirects=0):
    """Reuse one verified-TLS connection per host on each fixed worker."""
    if os.environ.get("MDPRO3_KEEPALIVE", "1") != "1":
        with urllib.request.urlopen(request, timeout=timeout) as response:
            yield response
        return
    parsed = urllib.parse.urlsplit(request.full_url)
    if parsed.scheme != "https":
        raise RuntimeError("Publisher package transport must retain HTTPS")
    if not hasattr(CONNECTIONS, "pool"):
        CONNECTIONS.pool = {}
    key = (parsed.hostname, parsed.port or 443)
    connection = CONNECTIONS.pool.get(key)
    if connection is None:
        connection = http.client.HTTPSConnection(key[0], key[1], timeout=timeout, context=TLS_CONTEXT)
        CONNECTIONS.pool[key] = connection
    connection.timeout = timeout
    if connection.sock is not None:
        connection.sock.settimeout(timeout)
    headers = dict(request.header_items())
    headers["Connection"] = "keep-alive"
    headers.setdefault("Accept-Encoding", "identity")
    path = urllib.parse.urlunsplit(("", "", parsed.path or "/", parsed.query, ""))
    try:
        connection.request(request.get_method(), path, body=request.data, headers=headers)
        response = connection.getresponse()
    except Exception:
        connection.close()
        CONNECTIONS.pool.pop(key, None)
        raise
    if response.status in (301, 302, 303, 307, 308):
        location = response.getheader("Location")
        response.close()
        connection.close()
        CONNECTIONS.pool.pop(key, None)
        if not location or redirects >= 3:
            raise RuntimeError("Publisher package redirect is missing or excessive")
        redirected = urllib.request.Request(urllib.parse.urljoin(request.full_url, location), headers=dict(request.header_items()))
        with open_package(redirected, timeout, redirects + 1) as followed:
            yield followed
        return
    if response.status >= 400:
        status, reason, response_headers = response.status, response.reason, response.headers
        response.close()
        connection.close()
        CONNECTIONS.pool.pop(key, None)
        raise urllib.error.HTTPError(request.full_url, status, reason, response_headers, None)
    completed = False
    try:
        yield response
        completed = True
    finally:
        # HTTPResponse.isclosed() is also true after an unexpected EOF. A
        # remaining Content-Length must reset the socket instead of reusing a
        # stale connection for the resumable request that follows.
        consumed = completed and (response.length == 0 if response.length is not None else response.isclosed())
        response.close()
        if not consumed or response.will_close:
            connection.close()
            CONNECTIONS.pool.pop(key, None)


def save_manifest(force=False):
    global MANIFEST_LAST_SAVE
    with LOCK:
        now = time.monotonic()
        # A phase can have 2,000 tiny archives. Rewriting its entire JSON after
        # both each download and each extraction made checkpoint I/O quadratic.
        # Package bytes and extraction markers are independently revalidated on
        # resume; the final forced checkpoint retains every completed record.
        if not force and now - MANIFEST_LAST_SAVE < MANIFEST_CHECKPOINT_SECONDS:
            return
        temporary = METADATA / (VERIFICATION_NAME + ".tmp")
        temporary.write_text(json.dumps(MANIFEST, indent=2) + "\n")
        temporary.replace(METADATA / VERIFICATION_NAME)
        MANIFEST_LAST_SAVE = time.monotonic()


def fetch_metadata(name, url):
    path = METADATA / name
    if not path.exists():
        with urllib.request.urlopen(url, timeout=45) as response:
            data = response.read(16 * 1024 * 1024 + 1)
        if len(data) > 16 * 1024 * 1024:
            raise RuntimeError("Publisher metadata exceeds expected size")
        if name == "checksums.txt":
            validate_checksum_manifest(data)
        with tempfile.NamedTemporaryFile(dir=METADATA, prefix=name + ".", suffix=".tmp", delete=False) as temporary:
            temporary_path = Path(temporary.name)
            try:
                temporary.write(data)
                temporary.flush()
                os.fsync(temporary.fileno())
            except BaseException:
                temporary_path.unlink(missing_ok=True)
                raise
        try:
            temporary_path.replace(path)
        finally:
            temporary_path.unlink(missing_ok=True)
    if name == "checksums.txt":
        validate_checksum_manifest(path.read_bytes())
    return path


def validate_checksum_manifest(data):
    """Fix the file universe to the independently acquired 1.4.7-2 release."""
    digest = hashlib.sha256(data).hexdigest()
    if digest != CHECKSUMS_SHA256:
        raise RuntimeError(f"Publisher 1.4.7-2 checksum manifest identity mismatch: {digest}")
    names = [line.split("  ", 1)[1] for line in data.decode().splitlines()
             if line and not line.startswith("  ")]
    if len(names) != CHECKSUMS_FILE_COUNT or len(set(names)) != CHECKSUMS_FILE_COUNT:
        raise RuntimeError("Publisher 1.4.7-2 checksum manifest has an unexpected file universe")
    return set(names)


def verify_payload(checksums):
    publisher_names = validate_checksum_manifest(checksums.read_bytes())
    selected = None
    if APP_FIRST:
        selected = set(json.loads((METADATA / f"{PREFIX}.request.json").read_text()))
        if not selected or not selected.issubset(publisher_names):
            raise RuntimeError("Requested files must be a non-empty subset of the fixed publisher manifest")
    checked = 0
    missing = []
    mismatches = []
    for line in checksums.read_text().splitlines():
        if not line or line.startswith("  "):
            continue
        expected, name = line.split("  ", 1)
        if selected is not None and name not in selected:
            continue
        checked += 1
        path = DESTINATION / name
        if not path.is_file():
            missing.append(name)
        elif sha256(path) != expected:
            mismatches.append(name)
    report = {"scope": PREFIX if APP_FIRST else "full", "checked_files": checked,
              "publisher_file_count": CHECKSUMS_FILE_COUNT,
              "checksums_sha256": sha256(checksums), "missing": missing,
              "mismatches": mismatches, "passed": not missing and not mismatches,
              "verified_utc": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime())}
    name = f"{PREFIX}.file-verification.json" if APP_FIRST else "file-verification.json"
    (METADATA / name).write_text(json.dumps(report, indent=2) + "\n")
    if missing or mismatches:
        raise RuntimeError(f"Publisher file verification failed: {len(missing)} missing, {len(mismatches)} mismatched")
    emit(f"Publisher per-file SHA-256 verified: {checked} files")
    return report


def acquire_ranges(entry, path, partial):
    """Optional bounded ranges for unstable large-object transfers."""
    folder = PACKAGES / (entry["name"] + ".ranges")
    folder.mkdir(exist_ok=True)
    count = (entry["size"] + RANGE_SIZE - 1) // RANGE_SIZE
    # Reuse the valid prefix already downloaded by the sequential transport.
    if partial.exists():
        with partial.open("rb") as previous:
            for index in range(count):
                data = previous.read(RANGE_SIZE)
                if not data:
                    break
                expected = min(RANGE_SIZE, entry["size"] - index * RANGE_SIZE)
                name = folder / f"{index:06d}.part"
                finished = name.with_suffix(".done")
                if not finished.exists() and (not name.exists() or name.stat().st_size < len(data)):
                    name.write_bytes(data)
                    if len(data) == expected:
                        name.replace(finished)

    def acquire_piece(index):
        begin = index * RANGE_SIZE
        end = min(entry["size"], begin + RANGE_SIZE) - 1
        expected = end - begin + 1
        piece = folder / f"{index:06d}.part"
        finished = piece.with_suffix(".done")
        if finished.exists() and finished.stat().st_size == expected:
            return finished, "cached-range"
        last_error = None
        for attempt in range(8):
            # Try the fast origin, then the two declared publisher mirrors.
            url = entry["urls"][attempt % len(entry["urls"])]
            current = piece.stat().st_size if piece.exists() else 0
            if current > expected:
                raise RuntimeError("A cached range exceeds its declared size")
            if current == expected:
                piece.replace(finished)
                return finished, "cached-range"
            start = begin + current
            request = urllib.request.Request(url, headers={"User-Agent": "MyCard/3.0", "Range": f"bytes={start}-{end}"})
            try:
                with NETWORK_GATE, open_package(request, timeout=25) as response:
                    if response.status != 206 or response.headers.get("Content-Range") != f"bytes {start}-{end}/{entry['size']}":
                        raise RuntimeError("Publisher did not return the requested bounded range")
                    with piece.open("ab") as output:
                        while current < expected:
                            data = response.read(min(65536, expected - current))
                            if not data:
                                break
                            current += len(data)
                            output.write(data)
                if current != expected:
                    raise RuntimeError("Incomplete bounded range")
                piece.replace(finished)
                return finished, url
            except Exception as error:
                last_error = error
                time.sleep(min(attempt + 1, 4))
        raise RuntimeError(f"Range {index} could not be acquired: {last_error}")

    futures = [RANGE_POOL.submit(acquire_piece, index) for index in range(count)]
    sources = set()
    completed = 0
    for future in concurrent.futures.as_completed(futures):
        _, source = future.result()
        completed += 1
        if source != "cached-range":
            sources.add(source)
        if completed % 8 == 0 or completed == count:
            emit(f"{entry['name'][:12]}: verified range lengths {completed}/{count}")
    assembled = path.with_name(path.name + ".assembling")
    with assembled.open("wb") as output:
        for index in range(count):
            with (folder / f"{index:06d}.done").open("rb") as piece:
                for data in iter(lambda: piece.read(65536), b""):
                    output.write(data)
    if assembled.stat().st_size != entry["size"] or sha256(assembled) != entry["sha256"]:
        invalid = folder.with_name(folder.name + f".invalid-{int(time.time())}")
        folder.rename(invalid)
        raise RuntimeError("Assembled package differs from publisher SHA-256; retained ranges for diagnosis")
    assembled.replace(path)
    with LOCK:
        entry["verified_sha256"] = entry["sha256"]
        entry["download_sources"] = sorted(sources)
        entry["verified_utc"] = time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime())
        save_manifest()
    emit(f"{entry['name'][:12]}: assembled SHA-256 verified ({entry['size'] // 1048576} MiB)")
    return entry, path


def acquire(entry):
    path = PACKAGES / entry["name"]
    partial = path.with_suffix(path.suffix + ".part")
    label = entry["name"][:12]
    if path.exists() and path.stat().st_size == entry["size"] and sha256(path) == entry["sha256"]:
        with LOCK:
            entry["verified_sha256"] = entry["sha256"]
            entry["verified_utc"] = time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime())
            save_manifest()
        if VERBOSE:
            emit(f"{label}: verified cached package")
        return entry, path
    if RANGE_POOL is not None and entry["size"] >= 8 * 1024 * 1024 and partial.exists() and partial.stat().st_size:
        return acquire_ranges(entry, path, partial)
    last_error = None
    for attempt in range(1, 11):
        # The origin is much faster than the edge mirrors for this distribution.
        # Retry temporary origin failures before falling back to the publisher's
        # Metalink mirrors, retaining valid Range-download progress.
        if entry["urls"][0].startswith(ORIGIN) and attempt <= 4:
            url = entry["urls"][0]
        else:
            mirrors = [url for url in entry["urls"] if not url.startswith(ORIGIN)]
            url = mirrors[(attempt - 5) % len(mirrors)]
        current = partial.stat().st_size if partial.exists() else 0
        headers = {"User-Agent": "MyCard/3.0"}
        if current:
            headers["Range"] = f"bytes={current}-"
        try:
            with NETWORK_GATE, open_package(urllib.request.Request(url, headers=headers), timeout=60) as response:
                if current and response.status != 206:
                    current = 0
                if response.status == 206 and not response.headers.get("Content-Range", "").startswith(f"bytes {current}-"):
                    raise RuntimeError("Publisher returned an unexpected byte range")
                mode = "ab" if current else "wb"
                last_report = time.monotonic()
                with partial.open(mode) as output:
                    while True:
                        chunk = response.read(1024 * 1024)
                        if not chunk:
                            break
                        current += len(chunk)
                        if current > entry["size"]:
                            raise RuntimeError("Package exceeded its publisher-declared size")
                        output.write(chunk)
                        if time.monotonic() - last_report > 30:
                            emit(f"{label}: {current * 100 / entry['size']:.1f}% ({current // 1048576} MiB)")
                            last_report = time.monotonic()
            if current != entry["size"]:
                raise RuntimeError(f"Incomplete package: {current}/{entry['size']} bytes")
            actual = sha256(partial)
            if actual != entry["sha256"]:
                # Preserve unexpected bytes for diagnosis; retry a fresh package.
                invalid = partial.with_name(partial.name + f".invalid-{int(time.time())}")
                partial.rename(invalid)
                raise RuntimeError(f"Publisher SHA-256 mismatch; preserved {invalid.name}")
            partial.replace(path)
            with LOCK:
                entry["verified_sha256"] = actual
                entry["downloaded_from"] = url
                entry["verified_utc"] = time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime())
                save_manifest()
            if VERBOSE or entry["size"] >= 8 * 1024 * 1024:
                emit(f"{label}: SHA-256 verified ({current // 1048576} MiB)")
            return entry, path
        except Exception as error:
            last_error = error
            emit(f"{label}: attempt {attempt} failed: {error}")
            if RANGE_POOL is not None and entry["size"] >= 8 * 1024 * 1024:
                return acquire_ranges(entry, path, partial)
            time.sleep(min(attempt * 2, 10))
    raise RuntimeError(f"Could not acquire {label}: {last_error}")


@contextlib.contextmanager
def open_verified_zstd(path):
    """Decode an already publisher-verified archive and check every frame."""
    if ZSTD_READER is not None:
        with ZSTD_READER.open_zstd_stream(path) as stream:
            yield stream
        return
    process = subprocess.Popen(["/opt/homebrew/bin/zstd", "--decompress", "--stdout", str(path)], stdout=subprocess.PIPE)
    try:
        yield process.stdout
        process.stdout.close()
        if process.wait() != 0:
            raise RuntimeError("zstd could not decompress a verified package")
    except BaseException:
        process.stdout.close()
        if process.poll() is None:
            process.terminate()
        process.wait()
        raise


def extract(entry, path):
    marker = METADATA / (entry["name"] + ".extracted")
    if marker.exists() and os.environ.get("MDPRO3_REEXTRACT") != "1":
        if marker.read_text().strip() != entry["sha256"]:
            raise RuntimeError("Extraction marker differs from publisher SHA-256")
        with LOCK:
            entry["extracted"] = True
            save_manifest()
        return
    if VERBOSE or entry["size"] >= 8 * 1024 * 1024:
        emit(f"{entry['name'][:12]}: extracting verified archive")
    staging = DESTINATION.parent / "MDPro3-mac-extract-staging"
    staging.mkdir(exist_ok=True)
    with open_verified_zstd(path) as decompressed:
        with tarfile.open(fileobj=decompressed, mode="r|") as archive:
            for member in archive:
                safe = tarfile.data_filter(member, str(DESTINATION))
                if safe is None:
                    continue
                target = DESTINATION / safe.name
                if not target.resolve().is_relative_to(DESTINATION.resolve()):
                    raise RuntimeError("An archive member resolves outside the game directory")
                if safe.isdir():
                    target.mkdir(parents=True, exist_ok=True)
                    continue
                target.parent.mkdir(parents=True, exist_ok=True)
                temporary = None
                try:
                    if safe.isfile():
                        with tempfile.NamedTemporaryFile(dir=staging, prefix="member-", suffix=".part", delete=False) as output:
                            temporary = Path(output.name)
                            with archive.extractfile(safe) as source:
                                remaining = safe.size
                                while remaining:
                                    data = source.read(min(1024 * 1024, remaining))
                                    if not data:
                                        raise RuntimeError("A verified archive contains an incomplete member")
                                    output.write(data)
                                    remaining -= len(data)
                            output.flush()
                            os.fsync(output.fileno())
                        if safe.mode is not None:
                            temporary.chmod(safe.mode)
                        if safe.mtime is not None:
                            os.utime(temporary, (safe.mtime, safe.mtime))
                    elif safe.issym() or safe.islnk():
                        with tempfile.NamedTemporaryFile(dir=staging, prefix="link-", delete=False) as reserved:
                            temporary = Path(reserved.name)
                        temporary.unlink()
                        if safe.issym():
                            os.symlink(safe.linkname, temporary)
                        else:
                            source = DESTINATION / safe.linkname
                            if not source.resolve().is_relative_to(DESTINATION.resolve()):
                                raise RuntimeError("An archive link resolves outside the game directory")
                            os.link(source, temporary)
                    else:
                        raise RuntimeError("Unsupported resource archive member type")
                    temporary.replace(target)
                    temporary = None
                finally:
                    if temporary is not None:
                        temporary.unlink(missing_ok=True)
    marker.write_text(entry["sha256"] + "\n")
    with LOCK:
        entry["extracted"] = True
        save_manifest()
    if VERBOSE or entry["size"] >= 8 * 1024 * 1024:
        emit(f"{entry['name'][:12]}: extraction complete")


def main():
    global MANIFEST, RANGE_POOL
    for folder in (METADATA, PACKAGES, DESTINATION):
        folder.mkdir(parents=True, exist_ok=True)
    xml = (METADATA / f"{PREFIX}.metalink.xml") if APP_FIRST else fetch_metadata("metalink.xml", BASE + "metalinks/" + SUFFIX)
    checksums = fetch_metadata("checksums.txt", BASE + "checksums/" + SUFFIX)
    document = ET.fromstring(xml.read_bytes())
    entries = []
    previous = {}
    verification = METADATA / VERIFICATION_NAME
    if APP_FIRST and not verification.exists():
        verification = METADATA / "app-first.verification.json"
    if verification.exists():
        previous = {item["name"]: item for item in json.loads(verification.read_text()).get("files", [])}
    for item in document.findall("m:file", NS):
        name = item.attrib["name"]
        if Path(name).name != name:
            raise RuntimeError("Unexpected publisher package filename")
        entry = {"name": name, "size": int(item.find("m:size", NS).text),
                 "sha256": item.find("m:hash", NS).text,
                 "urls": [node.text for node in item.findall("m:url", NS)]}
        # This is the publisher's public Minio origin, also used by its official
        # asset-build recipes. Final size and Metalink SHA-256 are still required.
        if os.environ.get("MDPRO3_ORIGIN_MINIO", "1") == "1":
            entry["urls"].insert(0, ORIGIN + name)
        old = previous.get(name, {})
        if old.get("sha256") == entry["sha256"]:
            for field in ("verified_sha256", "verified_utc", "downloaded_from", "download_sources", "extracted"):
                if field in old:
                    entry[field] = old[field]
        entries.append(entry)
    MANIFEST = {"application": "MDPro3", "version": "1.4.7-2", "platform": "darwin",
                "source_recipe": "https://code.moenext.com/mycard/MDPro3/-/blob/1.4.7-2/.gitlab-ci.yml",
                "metalink_url": BASE + ("update/" if APP_FIRST else "metalinks/") + SUFFIX,
                "checksums_url": BASE + "checksums/" + SUFFIX,
                "metalink_sha256": sha256(xml), "checksums_sha256": sha256(checksums),
                "total_bytes": sum(item["size"] for item in entries), "files": entries,
                "package_transport": "thread-local-verified-TLS-keepalive" if os.environ.get("MDPRO3_KEEPALIVE", "1") == "1" else "urllib",
                "archive_decompressor": "installed-libzstd-stream" if ZSTD_READER is not None else "zstd-cli",
                "archive_decompressor_library": ZSTD_READER.LIBRARY_PATH if ZSTD_READER is not None else None,
                "destination": str(DESTINATION)}
    save_manifest()
    emit(f"Acquiring {len(entries)} publisher-verified packages ({MANIFEST['total_bytes'] / 1024 ** 3:.2f} GiB)")
    # An optional filename prefix lets an inspected app-containing package go first.
    first = os.environ.get("MDPRO3_FIRST_PACKAGE", "")
    if first:
        prefixes = first.split(",")
        entries.sort(key=lambda entry: next((index for index, prefix in enumerate(prefixes) if entry["name"].startswith(prefix)), len(prefixes)))
    workers = int(os.environ.get("MDPRO3_DOWNLOAD_WORKERS", "4" if APP_FIRST else "2"))
    if workers < 1 or workers > 8:
        raise RuntimeError("Download worker count must be between 1 and 8")
    range_workers = int(os.environ.get("MDPRO3_RANGE_WORKERS", "0"))
    if range_workers < 0 or range_workers > 16:
        raise RuntimeError("Range worker count must be between 0 and 16")
    if range_workers:
        RANGE_POOL = concurrent.futures.ThreadPoolExecutor(max_workers=range_workers)
    try:
        with concurrent.futures.ThreadPoolExecutor(max_workers=workers) as pool:
            futures = [pool.submit(acquire, entry) for entry in entries]
            completed = 0
            for future in concurrent.futures.as_completed(futures):
                entry, path = future.result()
                extract(entry, path)
                completed += 1
                if completed % 25 == 0 or completed == len(entries):
                    emit(f"Publisher packages verified and extracted: {completed}/{len(entries)}")
    finally:
        try:
            if RANGE_POOL:
                RANGE_POOL.shutdown(wait=True)
        finally:
            save_manifest(force=True)
    verify_payload(checksums)
    emit(f"Publisher selection {'full' if not APP_FIRST else PREFIX} verified: {DESTINATION}")


if __name__ == "__main__":
    main()
