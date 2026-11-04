using Cysharp.Threading.Tasks;
using MDPro3.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;

namespace MDPro3.IO
{
    public static class ResourceManager
    {
        #region Initialize

        private static FileGroupConfig fileGroups;
        public static async UniTask InitializeAsync(Action onBackground = null, Action onMainThread = null)
        {
            fileGroups = Json.LoadFromFile<FileGroupConfig>(FileGroupConfig.SavePath);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (FileManager.DebugEnabled)
                Debug.Log($"[ResourceManager] Loaded file group config from {FileGroupConfig.SavePath}");
#endif

            await InitializeFileManagerAsync(fileGroups, default);
            await UnzipAllCardVideos(default);
            await CacheIniTextsAsync();
            await CacheAllCdbInZipToTempAsync();

            await UniTask.RunOnThreadPool(() =>
            {
                CacheOverFrameDic();
                CacheCardVideoMap();
                onBackground?.Invoke();
            });

            if (onMainThread != null)
            {
                await UniTask.SwitchToMainThread();
                onMainThread.Invoke();
            }
        }

        private static async UniTask InitializeFileManagerAsync(FileGroupConfig fileGroups, CancellationToken token = default)
        {
            List<string> folders = new();
            List<string> zips = new();
            List<string> zipFolders = new();
            foreach (var group in fileGroups.GetList())
            {
                foreach (var path in group.Paths)
                {
                    if (path.EndsWith("/"))
                    {
                        if (Directory.Exists(path))
                            folders.Add(path);
                        continue;
                    }
                    if (fileGroups.IsZipFile(path) && File.Exists(path))
                        zips.Add(path);
                }
                foreach (var ext in group.Extensions)
                {
                    if (fileGroups.IsZipFile(ext))
                    {
                        foreach (var p in group.Paths)
                            if (Directory.Exists(p))
                                zipFolders.Add(p);
                    }
                }
            }

            folders = folders.Distinct().ToList();
            zips = zips.Distinct().ToList();
            zipFolders = zipFolders.Distinct().ToList();

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (FileManager.DebugEnabled)
            {
                Debug.Log($"[ResourceManager] Init FileManager - folders: {folders.Count}, zips: {zips.Count}, zipFolders: {zipFolders.Count}");
            }
#endif

            await FileManager.InitializeAsync(zips, zipFolders, folders, fileGroups.zipExtensions, token);
        }

        #endregion

        #region Common

        public static async UniTask<List<string>> CacheFilesToTempByExtensionsAsync(
            IEnumerable<string> extensions,
            string subFolder
            , CancellationToken token = default)
        {
            var paths = FileManager.GetFilesInZipByExtensions(extensions);
            var folder = string.IsNullOrEmpty(subFolder) ? Program.PATH_TEMP_FOLDER
                : Path.Combine(Program.PATH_TEMP_FOLDER, subFolder);
            await FileManager.ExtractToFolderAsync(paths, folder, token);

            var cachedFiles = new List<string>();
            foreach (var path in paths)
            {
                var newPath = Path.Combine(folder, Path.GetFileName(path));
                if (File.Exists(newPath))
                    cachedFiles.Add(newPath);
            }
            return cachedFiles;
        }

        public static List<string> GetTextsByExtensions(
            IEnumerable<string> extensions)
        {
            var value = new List<string>();
            var files = FileManager.GetFilesByExtensions(extensions);
            foreach (var file in files)
            {
                try
                {
                    var bytes = FileManager.GetFile(file);
                    value.Add(Encoding.UTF8.GetString(bytes));
                }
                catch { continue; }
            }

            return value;
        }

        public static List<string> GetTextsByExtensions(
            IEnumerable<string> extensions
            , IEnumerable<string> excludes)
        {
            var value = GetTextsByExtensions(extensions);
            return value.Where(v => excludes.Any(e => v.Contains(e, System.StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        public static async UniTask<List<string>> GetTextsByExtensionsAsync(
            IEnumerable<string> extensions
            , CancellationToken token = default)
        {
            var value = new List<string>();
            var files = FileManager.GetFilesByExtensions(extensions);
            foreach (var file in files)
            {
                try
                {
                    var bytes = await FileManager.GetFileAsync(file, token);
                    value.Add(Encoding.UTF8.GetString(bytes));
                }
                catch { continue; }
            }

            return value;
        }

        #endregion

        #region Card

        private static async UniTask<Texture2D> GetCardTexture(CardFileGroup fileGroup, int code, CancellationToken token = default)
        {
            byte[] bytes = null;
            await UniTask.RunOnThreadPool(async () =>
            {
                if (!fileGroup.CardExists(code))
                    return;
                var path = fileGroup.GetCardPath(code);
                if (string.IsNullOrEmpty(path))
                    return;
                bytes = await FileManager.GetFileAsync(path, token);
            }, cancellationToken: token);

            if (bytes != null)
            {
                Texture2D tex = new(0, 0);
                tex.LoadImage(bytes);
                return tex;
            }
            return null;
        }

        public static async UniTask<Texture2D> GetCardIllustAsync(int code, CancellationToken token = default)
        {
            var tex = await GetCardTexture(fileGroups.CardIllust2, code, token);
            if (tex != null) return tex;
            return await GetCardTexture(fileGroups.CardIllust, code, token);
        }

        public static async UniTask<Texture2D> GetCardCloseupAsync(int code, CancellationToken token = default)
        {
            return await GetCardTexture(fileGroups.CardCloseup, code, token);
        }

        public static async UniTask<Texture2D> GetCardOverFrameAsync(int code, CancellationToken token = default)
        {
            return await GetCardTexture(fileGroups.CardOverframe, code, token);
        }

        public static async UniTask<Texture2D> GetCardOverFrame2Async(int code, CancellationToken token = default)
        {
            return await GetCardTexture(fileGroups.CardOverframe2, code, token);
        }

        public static async UniTask<Texture2D> GetCardImageAsync(int code, CancellationToken token = default)
        {
            return await GetCardTexture(fileGroups.CardPicture, code, token);
        }

        private static readonly HashSet<int> _overFrameDic = new();

        private static void CacheOverFrameDic()
        {
            _overFrameDic.Clear();
            var allOverframes = FileManager.GetFilesByPrefixesAndExtensions(fileGroups.CardOverframe.Paths, fileGroups.CardOverframe.Extensions);
            allOverframes.AddRange(FileManager.GetFilesByPrefixesAndExtensions(fileGroups.CardOverframe2.Paths, fileGroups.CardOverframe2.Extensions));
            foreach (var of in allOverframes)
                if (int.TryParse(Path.GetFileNameWithoutExtension(of), out var code))
                    if (!_overFrameDic.Contains(code))
                        _overFrameDic.Add(code);

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (FileManager.DebugEnabled)
                Debug.Log($"[ResourceManager] OverFrame codes cached: {_overFrameDic.Count}");
#endif
        }

        public static bool CardHasOverFrame(int code)
        {
            return _overFrameDic.Contains(code);
        }

        private static async UniTask UnzipAllCardVideos(CancellationToken token)
        {
            if (fileGroups == null
                || fileGroups.CardVideo.Paths == null
                || fileGroups.CardVideo.Paths.Length == 0) return;

            var prefixes = new List<string>();
            foreach (var path in fileGroups.CardVideo.Paths)
                if (path.EndsWith("/"))
                    prefixes.Add(path);

            var extensions = fileGroups.CardVideo.Extensions;
            var allVideoFilesInZip = FileManager.GetFilesInZipByPrefixesAndExtensions(prefixes, extensions);
            var destinationFolder = fileGroups.CardVideo.Paths[0];
            if (!Directory.Exists(destinationFolder)) Directory.CreateDirectory(destinationFolder);
            var videoFilesNeedUnzip = allVideoFilesInZip.Where(f => !File.Exists(Path.Combine(destinationFolder, Path.GetFileName(f))));
            await FileManager.ExtractToFolderAsync(videoFilesNeedUnzip, destinationFolder, token);

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (FileManager.DebugEnabled)
                Debug.Log($"[ResourceManager] Unzipped {videoFilesNeedUnzip.Count()} card videos to {destinationFolder}");
#endif
        }

        private static readonly HashSet<int> _cardVideoDic = new();
        private static void CacheCardVideoMap()
        {
            _cardVideoDic.Clear();
            var allVideos = FileManager.GetFilesByPrefixesAndExtensions(fileGroups.CardVideo.Paths, fileGroups.CardVideo.Extensions);
            foreach (var video in allVideos)
                if (int.TryParse(Path.GetFileNameWithoutExtension(video), out var code))
                    if (!_cardVideoDic.Contains(code))
                        _cardVideoDic.Add(code);

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (FileManager.DebugEnabled)
                Debug.Log($"[ResourceManager] Card video codes cached: {_cardVideoDic.Count}");
#endif
        }

        public static bool CardHasVideo(int code)
        {
            return _cardVideoDic.Contains(code);
        }

        public static void ClearCardVideos()
        {
            _cardVideoDic.Clear();
        }

        #endregion

        #region ini

        private static List<string> iniContents = new();

        private static async UniTask CacheIniTextsAsync()
        {
            iniContents = await GetTextsByExtensionsAsync(new string[] { ".ini" });
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (FileManager.DebugEnabled)
                Debug.Log($"[ResourceManager] Cached {iniContents.Count} ini texts");
#endif
        }

        public static List<string> GetAllIniTexts()
        {
            return iniContents;
        }

        #endregion

        #region Cdb for windbot

        private static List<string> cachedTempCdbs = new();

        private static async UniTask CacheAllCdbInZipToTempAsync()
        {
            cachedTempCdbs?.Clear();
            cachedTempCdbs = await CacheFilesToTempByExtensionsAsync(new string[] { ".cdb" }, "WindBot");
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (FileManager.DebugEnabled)
                Debug.Log($"[ResourceManager] Cached {cachedTempCdbs.Count} cdb files to temp");
#endif
        }

        public static List<string> GetAllCdbInZipPaths()
        {
            return cachedTempCdbs;
        }

        #endregion
    }
}