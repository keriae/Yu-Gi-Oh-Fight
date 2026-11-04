using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEngine;
using Ionic.Zip;

namespace MDPro3.IO
{
    public class ZipManager : IDisposable
    {
        private bool _initialized;
        private volatile bool _isDisposed;
        private readonly SemaphoreSlim _initLock = new(1, 1);
        private CancellationTokenSource _disposeCts;

        private Dictionary<string, (ZipFile Zip, string RealName)> _entryMap;
        public List<ZipFile> _zipFiles;

        public async UniTask InitializeAsync(
            IEnumerable<string> zipPaths,
            IEnumerable<string> zipFolders,
            IEnumerable<string> zipExtensions,
            CancellationToken token = default)
        {
            if (_initialized) return;
            await _initLock.WaitAsync(token);
            try
            {
                if (_initialized) return;

                _disposeCts?.Cancel();
                _disposeCts?.Dispose();
                _disposeCts = new CancellationTokenSource();
                DisposeInternal();

                var entryMap = new Dictionary<string, (ZipFile, string)>(StringComparer.OrdinalIgnoreCase);
                var zipFiles = new List<ZipFile>();

#if DEVELOPMENT_BUILD || UNITY_EDITOR
                if (FileManager.DebugEnabled)
                {
                    Debug.Log($"[ZipManager] Initializing with {zipPaths.Count()} explicit zips, {zipFolders.Count()} folders, extensions: {string.Join(", ", zipExtensions)}");
                }
#endif

                await UniTask.RunOnThreadPool(() =>
                {
                    foreach (var zipPath in zipPaths)
                        AddZip(zipPath, zipFiles, entryMap);

                    foreach (var zipFolder in zipFolders)
                        foreach (var zipExpansion in zipExtensions)
                            foreach (var zip in Directory.GetFiles(zipFolder, "*" + zipExpansion))
                                AddZip(zip, zipFiles, entryMap);
                }, cancellationToken: token);

                _entryMap = entryMap;
                _zipFiles = zipFiles;
                _initialized = true;
                _isDisposed = false;

#if DEVELOPMENT_BUILD || UNITY_EDITOR
                if (FileManager.DebugEnabled)
                {
                    Debug.Log($"[ZipManager] Initialization complete. Total zip files: {zipFiles.Count}, total entries: {entryMap.Count}");
                }
#endif
            }
            finally
            {
                _initLock.Release();
            }
        }

        private void AddZip(string path, List<ZipFile> zipFiles, Dictionary<string, (ZipFile, string)> entryMap)
        {
            if (!File.Exists(path))
            {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                if (FileManager.DebugEnabled)
                    Debug.LogWarning($"[ZipManager] Zip file not found: {path}");
#endif
                return;
            }
            try
            {
                var zip = new ZipFile(path);
                zipFiles.Add(zip);
                int entryCount = 0;
                foreach (ZipEntry entry in zip.Entries)
                {
                    if (entry.IsDirectory) continue;
                    var normalized = FileManager.NormalizePath(entry.FileName);
                    if (!entryMap.ContainsKey(normalized))
                    {
                        entryMap[normalized] = (zip, entry.FileName);
                        entryCount++;
                    }
                }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                if (FileManager.DebugEnabled)
                    Debug.Log($"[ZipManager] Added zip: {path}, entries: {entryCount}");
#endif
            }
            catch (Exception e)
            {
                Debug.LogError($"ZipManager: 读取 zip 文件失败 [{path}]: {e}");
            }
        }

        public void Dispose()
        {
            _disposeCts?.Cancel();
            _isDisposed = true;
            _initialized = false;

            DisposeInternal();

            _disposeCts?.Dispose();
            _disposeCts = null;
        }

        private void DisposeInternal()
        {
            if (_zipFiles != null)
            {
                foreach (var zip in _zipFiles)
                {
                    try { zip.Dispose(); }
                    catch (Exception e) { Debug.LogWarning($"释放 ZipFile 出错: {e.Message}"); }
                }
                _zipFiles.Clear();
            }
            if (_entryMap != null)
            {
                _entryMap.Clear();
                _entryMap = null;
            }
        }

        public bool FileExists(string fileName)
        {
            if (!_initialized || _isDisposed || _entryMap == null)
            {
                return false;
            }
            return _entryMap.ContainsKey(FileManager.NormalizePath(fileName));
        }

        public byte[] Extract(string fileName, out string zipName)
        {
            if (!TryFindEntry(fileName, out var zip, out var realEntryName))
            {
                zipName = null;
                return null;
            }

            lock (zip)
            {
                var stream = Pools.GetStream();
                try
                {
                    var entry = zip[realEntryName];
                    entry.Extract(stream);
                    zipName = zip.Name;
                    return stream.ToArray();
                }
                finally
                {
                    Pools.ReturnStream(stream);
                }
            }
        }

        public bool ExtractTo(string fileName, string saveFolder, out string zipName)
        {
            var bytes = Extract(fileName, out zipName);
            if (bytes == null) return false;
            try
            {
                if (!Directory.Exists(saveFolder))
                    Directory.CreateDirectory(saveFolder);
                var newName = Path.Combine(saveFolder, Path.GetFileName(fileName));
                File.WriteAllBytes(newName, bytes);
                return true;
            }
            catch { return false; }
        }

        public async UniTask<byte[]> ExtractAsync(string fileName, CancellationToken token = default)
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, _disposeCts?.Token ?? CancellationToken.None);
            await UniTask.WaitUntil(() => _initialized, cancellationToken: linkedCts.Token);

            if (!TryFindEntry(fileName, out var zip, out var realEntryName))
                return null;

            if (_isDisposed)
                return null;

            return await UniTask.RunOnThreadPool(() =>
            {
                linkedCts.Token.ThrowIfCancellationRequested();
                lock (zip)
                {
                    try
                    {
                        var entry = zip[realEntryName];
                        var stream = Pools.GetStream();
                        try
                        {
                            entry.Extract(stream);
                            return stream.ToArray();
                        }
                        finally
                        {
                            Pools.ReturnStream(stream);
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        return null;
                    }
                }
            }, cancellationToken: linkedCts.Token);
        }

        public async UniTask ExtractToAsync(string fileName, string destinationFolder, CancellationToken token = default)
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, _disposeCts?.Token ?? CancellationToken.None);
            await UniTask.WaitUntil(() => _initialized, cancellationToken: linkedCts.Token);

            if (!TryFindEntry(fileName, out var zip, out var realEntryName))
                return;

            if (_isDisposed) return;

            await UniTask.RunOnThreadPool(() =>
            {
                linkedCts.Token.ThrowIfCancellationRequested();
                lock (zip)
                {
                    try
                    {
                        var entry = zip[realEntryName];
                        if (!Directory.Exists(destinationFolder))
                            Directory.CreateDirectory(destinationFolder);
                        var destPath = Path.Combine(destinationFolder, Path.GetFileName(fileName));
                        using var fs = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None);
                        entry.Extract(fs);
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                }
            }, cancellationToken: linkedCts.Token);
        }

        public List<string> GetAllFilesByPrefixesAndExtensions(IEnumerable<string> prefixes, IEnumerable<string> extensions)
        {
            if (_entryMap == null || _isDisposed) return new List<string>();

            var keys = _entryMap.Keys.ToList();
            var extSet = extensions.Select(e => e.ToLowerInvariant()).ToHashSet();
            var result = new List<string>();

            foreach (var key in keys)
            {
                if (prefixes.Any(p => key.StartsWith(p, StringComparison.OrdinalIgnoreCase)) &&
                    extSet.Any(ext => key.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                    result.Add(key);
            }
            return result;
        }

        public List<string> GetAllFilesByExtensions(IEnumerable<string> extensions)
        {
            if (_entryMap == null || _isDisposed) return new List<string>();

            var keys = _entryMap.Keys.ToList();
            var extSet = extensions.Select(e => e.ToLowerInvariant()).ToHashSet();
            return keys.Where(key => extSet.Any(ext => key.EndsWith(ext, StringComparison.OrdinalIgnoreCase))).ToList();
        }

        private bool TryFindEntry(string virtualPath, out ZipFile zip, out string realEntryName)
        {
            if (_isDisposed || _entryMap == null)
            {
                zip = null;
                realEntryName = null;
                return false;
            }

            var normalized = FileManager.NormalizePath(virtualPath);
            if (_entryMap.TryGetValue(normalized, out var pair))
            {
                zip = pair.Zip;
                realEntryName = pair.RealName;
                return true;
            }

            zip = null;
            realEntryName = null;
            return false;
        }
    }
}