using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace MDPro3.IO
{
    public static class FileManager
    {
        public static bool DebugEnabled =
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            false;
#else
            false;
#endif

        private static HashSet<string> cachedLocalFiles;
        public static ZipManager zipManager;
        private static List<string> _folders;

        public static async UniTask InitializeAsync(
            IEnumerable<string> zips,
            IEnumerable<string> zipFolders,
            IEnumerable<string> folders,
            IEnumerable<string> zipExtensions,
            CancellationToken token = default)
        {
            _folders = folders.ToList();
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (DebugEnabled)
            {
                Debug.Log($"[FileManager] InitializeAsync - folders: {string.Join(", ", folders)}");
                Debug.Log($"[FileManager] InitializeAsync - explicit zips: {string.Join(", ", zips)}");
                Debug.Log($"[FileManager] InitializeAsync - zip folders: {string.Join(", ", zipFolders)}");
            }
#endif
            await BuildLocalFileCacheAsync(folders, token);
            zipManager?.Dispose();
            zipManager = new ZipManager();
            await zipManager.InitializeAsync(zips, zipFolders, zipExtensions, token);
        }

        public static bool FileExists(string path)
        {
            if (LocalFileExists(path)) return true;
            if (ZipFileExists(path)) return true;
            return false;
        }

        public static byte[] GetFile(string path)
        {
            if (LocalFileExists(path))
            {
                try { return File.ReadAllBytes(path); }
                catch { return null; }
            }

            if (ZipFileExists(path))
                return zipManager.Extract(path, out _);

            return null;
        }

        public static async UniTask<byte[]> GetFileAsync(string path, CancellationToken token = default)
        {
            if (LocalFileExists(path))
            {
                var fullPath = Path.GetFullPath(path);
                return await UniTask.RunOnThreadPool(() => File.ReadAllBytes(fullPath), cancellationToken: token);
            }

            if (ZipFileExists(path))
            {
                return await zipManager.ExtractAsync(path, token);
            }

            return null;
        }

        public static string NormalizePath(string path)
        {
            return path.Replace('\\', '/');
        }

        public static List<string> GetFilesByPrefixesAndExtensions(IEnumerable<string> prefixes, IEnumerable<string> extensions)
        {
            var files = GetFilesInZipByPrefixesAndExtensions(prefixes, extensions);
            files.AddRange(GetFilesNotInZipByPrefixesAndExtensions(prefixes, extensions));
            return files;
        }

        public static List<string> GetFilesInZipByPrefixesAndExtensions(IEnumerable<string> prefixes, IEnumerable<string> extensions)
        {
            if (zipManager == null) return new List<string>();
            return zipManager.GetAllFilesByPrefixesAndExtensions(prefixes, extensions);
        }

        public static List<string> GetFilesNotInZipByPrefixesAndExtensions(IEnumerable<string> prefixes, IEnumerable<string> extensions)
        {
            if (cachedLocalFiles == null || !cachedLocalFiles.Any()) return new List<string>();

            return cachedLocalFiles
                .Where(file =>
                    prefixes.Any(prefix => file.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    && extensions.Any(extension => file.EndsWith(extension, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        public static List<string> GetFilesByExtensions(IEnumerable<string> extensions)
        {
            var files = GetFilesInZipByExtensions(extensions);
            files.AddRange(GetFilesNotInZipByExtensions(extensions));
            return files;
        }

        public static List<string> GetFilesInZipByExtensions(IEnumerable<string> extensions)
        {
            if (zipManager == null) return new List<string>();
            return zipManager.GetAllFilesByExtensions(extensions);
        }

        public static List<string> GetFilesNotInZipByExtensions(IEnumerable<string> extensions)
        {
            if (cachedLocalFiles == null || !cachedLocalFiles.Any()) return new List<string>();

            return cachedLocalFiles
                .Where(file => extensions.Any(extension => file.EndsWith(extension, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        public static async UniTask ExtractToFolderAsync(IEnumerable<string> fileNames, string destinationFolder, CancellationToken token = default)
        {
            if (zipManager == null) return;
            bool needAdd = _folders.Contains(destinationFolder);
            await UniTask.RunOnThreadPool(async () =>
            {
                foreach (var file in fileNames)
                {
                    await zipManager.ExtractToAsync(file, destinationFolder, token);
                    if (needAdd)
                    {
                        var fullName = Path.Combine(destinationFolder, Path.GetFileName(file));
                        if (!cachedLocalFiles.Contains(fullName))
                            cachedLocalFiles.Add(fullName);
                    }
                }
            });
        }

        public static bool TryExtractFileTo(string fileName, string saveFolder, out string zipName)
        {
            if (!ZipFileExists(fileName))
            {
                zipName = null;
                return false;
            }

            return zipManager.ExtractTo(fileName, saveFolder, out zipName);
        }

        public static void Dispose()
        {
            zipManager?.Dispose();
            zipManager = null;
        }

        private static async UniTask BuildLocalFileCacheAsync(IEnumerable<string> folders, CancellationToken token = default)
        {
            await UniTask.RunOnThreadPool(() =>
            {
                cachedLocalFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var distinctFolders = folders
                    .Select(p => Path.GetFullPath(p))
                    .Where(Directory.Exists)
                    .Distinct();

                foreach (var folder in distinctFolders)
                {
                    try
                    {
                        var files = Directory.GetFiles(folder, "*", SearchOption.TopDirectoryOnly);
                        foreach (var file in files)
                        {
                            var relative = Path.GetRelativePath(Directory.GetCurrentDirectory(), file);
                            var normalized = NormalizePath(relative);
                            cachedLocalFiles.Add(normalized);
                        }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                        if (DebugEnabled)
                            Debug.Log($"[FileManager] Cached {files.Length} files from folder: {folder}");
#endif
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"Skip folder: [{folder}] due to {e.Message}");
                    }
                }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                if (DebugEnabled)
                    Debug.Log($"[FileManager] Local file cache built, total files: {cachedLocalFiles.Count}");
#endif
            }, cancellationToken: token);
        }

        private static bool LocalFileExists(string path)
        {
            if (cachedLocalFiles == null) return false;
            return cachedLocalFiles.Contains(NormalizePath(path));
        }

        private static bool ZipFileExists(string path)
        {
            if (zipManager != null && zipManager.FileExists(path))
                return true;
            return false;
        }
    }
}