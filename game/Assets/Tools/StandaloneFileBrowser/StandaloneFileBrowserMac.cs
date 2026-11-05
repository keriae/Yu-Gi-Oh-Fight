#if UNITY_STANDALONE_OSX

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SFB {
    public class StandaloneFileBrowserMac : IStandaloneFileBrowser {
        private static Action<string[]> _openFileCb;
        private static Action<string[]> _openFolderCb;
        private static Action<string> _saveFileCb;
        private static readonly object DialogQueueLock = new object();
        private static readonly Queue<Action> DialogQueue = new Queue<Action>();
        private static bool dialogActive;

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void AsyncCallback(string path);

        // Keep marshalled delegates alive until their asynchronous native callbacks.
        private static readonly AsyncCallback NativeOpenFileCallback = openFileCb;
        private static readonly AsyncCallback NativeOpenFolderCallback = openFolderCb;
        private static readonly AsyncCallback NativeSaveFileCallback = saveFileCb;

        [AOT.MonoPInvokeCallback(typeof(AsyncCallback))]
        private static void openFileCb(string result) {
            var callback = _openFileCb;
            _openFileCb = null;
            try { callback?.Invoke(result.Split((char)28)); }
            finally { CompleteDialog(); }
        }

        [AOT.MonoPInvokeCallback(typeof(AsyncCallback))]
        private static void openFolderCb(string result) {
            var callback = _openFolderCb;
            _openFolderCb = null;
            try { callback?.Invoke(result.Split((char)28)); }
            finally { CompleteDialog(); }
        }

        [AOT.MonoPInvokeCallback(typeof(AsyncCallback))]
        private static void saveFileCb(string result) {
            var callback = _saveFileCb;
            _saveFileCb = null;
            try { callback?.Invoke(result); }
            finally { CompleteDialog(); }
        }

        private static void EnqueueDialog(Action request) {
            Action next = null;
            lock (DialogQueueLock) {
                DialogQueue.Enqueue(request);
                if (!dialogActive) {
                    dialogActive = true;
                    next = DialogQueue.Dequeue();
                }
            }
            if (next != null) StartDialog(next);
        }

        private static void StartDialog(Action request) {
            try { request(); }
            catch {
                _openFileCb = null;
                _openFolderCb = null;
                _saveFileCb = null;
                CompleteDialog();
                throw;
            }
        }

        private static void CompleteDialog() {
            Action next = null;
            lock (DialogQueueLock) {
                if (DialogQueue.Count > 0) next = DialogQueue.Dequeue();
                else dialogActive = false;
            }
            if (next != null) StartDialog(next);
        }

        private static void BeginSynchronousDialog() {
            lock (DialogQueueLock) {
                if (dialogActive) throw new InvalidOperationException("A file dialog is already active. Wait for its callback before opening a synchronous dialog.");
                dialogActive = true;
            }
        }

        [DllImport("StandaloneFileBrowser")]
        private static extern IntPtr DialogOpenFilePanel(string title, string directory, string extension, bool multiselect);
        [DllImport("StandaloneFileBrowser")]
        private static extern void DialogOpenFilePanelAsync(string title, string directory, string extension, bool multiselect, AsyncCallback callback);
        [DllImport("StandaloneFileBrowser")]
        private static extern IntPtr DialogOpenFolderPanel(string title, string directory, bool multiselect);
        [DllImport("StandaloneFileBrowser")]
        private static extern void DialogOpenFolderPanelAsync(string title, string directory, bool multiselect, AsyncCallback callback);
        [DllImport("StandaloneFileBrowser")]
        private static extern IntPtr DialogSaveFilePanel(string title, string directory, string defaultName, string extension);
        [DllImport("StandaloneFileBrowser")]
        private static extern void DialogSaveFilePanelAsync(string title, string directory, string defaultName, string extension, AsyncCallback callback);

        public string[] OpenFilePanel(string title, string directory, ExtensionFilter[] extensions, bool multiselect) {
            var filters = GetFilterFromFileExtensionList(extensions);
            BeginSynchronousDialog();
            try { return Marshal.PtrToStringAnsi(DialogOpenFilePanel(title, directory, filters, multiselect)).Split((char)28); }
            finally { CompleteDialog(); }
        }

        public void OpenFilePanelAsync(string title, string directory, ExtensionFilter[] extensions, bool multiselect, Action<string[]> cb) {
            var filters = GetFilterFromFileExtensionList(extensions);
            EnqueueDialog(() => {
                _openFileCb = cb;
                DialogOpenFilePanelAsync(title, directory, filters, multiselect, NativeOpenFileCallback);
            });
        }

        public string[] OpenFolderPanel(string title, string directory, bool multiselect) {
            BeginSynchronousDialog();
            try { return Marshal.PtrToStringAnsi(DialogOpenFolderPanel(title, directory, multiselect)).Split((char)28); }
            finally { CompleteDialog(); }
        }

        public void OpenFolderPanelAsync(string title, string directory, bool multiselect, Action<string[]> cb) {
            EnqueueDialog(() => {
                _openFolderCb = cb;
                DialogOpenFolderPanelAsync(title, directory, multiselect, NativeOpenFolderCallback);
            });
        }

        public string SaveFilePanel(string title, string directory, string defaultName, ExtensionFilter[] extensions) {
            var filters = GetFilterFromFileExtensionList(extensions);
            BeginSynchronousDialog();
            try { return Marshal.PtrToStringAnsi(DialogSaveFilePanel(title, directory, defaultName, filters)); }
            finally { CompleteDialog(); }
        }

        public void SaveFilePanelAsync(string title, string directory, string defaultName, ExtensionFilter[] extensions, Action<string> cb) {
            var filters = GetFilterFromFileExtensionList(extensions);
            EnqueueDialog(() => {
                _saveFileCb = cb;
                DialogSaveFilePanelAsync(title, directory, defaultName, filters, NativeSaveFileCallback);
            });
        }

        private static string GetFilterFromFileExtensionList(ExtensionFilter[] extensions) {
            if (extensions == null || extensions.Length == 0) return "";
            var filters = new List<string>();
            foreach (var filter in extensions) {
                if (filter.Extensions == null || filter.Extensions.Length == 0) continue;
                filters.Add((filter.Name ?? "") + ";" + string.Join(",", filter.Extensions));
            }
            return string.Join("|", filters);
        }
    }
}

#endif
