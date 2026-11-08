using MDPro3.Utility;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Playables;
using YuGiOhFight.Client.Presentation.Elements;
using MDPro3;
using MDPro3.Duel;


namespace YuGiOhFight.Client.Utility
{
    public static class Tools
    {
        public static void ChangeLayer(global::UnityEngine.GameObject go, string layer, bool setAllChildrenActivate = false)
        {
            //Debug.Log("Change Layer: " + go.name + "-> " + layer);
            foreach (global::UnityEngine.Transform t in go.transform.GetComponentsInChildren<global::UnityEngine.Transform>(true))
            {
                if (setAllChildrenActivate) t.gameObject.SetActive(true);
                t.gameObject.layer = global::UnityEngine.LayerMask.NameToLayer(layer);
            }
        }

        public static void ChangeLayer(global::UnityEngine.GameObject go, int layerMask, bool setAllChildrenActivate = false)
        {
            //Debug.Log("Change Layer: " + go.name + "-> " + layer);
            foreach (global::UnityEngine.Transform t in go.transform.GetComponentsInChildren<global::UnityEngine.Transform>(true))
            {
                if (setAllChildrenActivate) t.gameObject.SetActive(true);
                t.gameObject.layer = layerMask;
            }
        }

        public static void ChangeSortingLayer(global::UnityEngine.GameObject go, string sortingLayer)
        {
            foreach (var renderer in go.GetComponentsInChildren<global::UnityEngine.Renderer>(true))
                renderer.sortingLayerName = sortingLayer;
        }

        public static void ChangeMaterialRenderQueue(global::UnityEngine.GameObject root, int queue)
        {
            foreach (var renderer in root.GetComponentsInChildren<global::UnityEngine.Renderer>(true))
                renderer.material.renderQueue = queue;
        }

        public static void PlayAnimation(global::UnityEngine.Transform animationContainer, string animationName)
        {
            if (animationContainer == null) return;
            global::UnityEngine.Animator[] animators = animationContainer.GetComponentsInChildren<global::UnityEngine.Animator>();
            foreach (global::UnityEngine.Animator animator in animators)
            {
                if (animator == null || string.IsNullOrEmpty(animationName))
                    continue;

                var hasTrigger = false;
                var parameters = animator.parameters;
                for (var i = 0; i < parameters.Length; i++)
                {
                    var param = parameters[i];
                    if (param.type == global::UnityEngine.AnimatorControllerParameterType.Trigger && param.name == animationName)
                    {
                        hasTrigger = true;
                        break;
                    }
                }

                if (hasTrigger)
                    animator.SetTrigger(animationName);
            }
        }

        public static void PlayParticle(global::UnityEngine.Transform particleContainer, string particleName)
        {
            if (particleContainer == null) return;
            foreach (var child in particleContainer.GetComponentsInChildren<global::UnityEngine.Transform>(true))
                if (child.name.ToLower().Contains(particleName.ToLower()))
                    foreach (var p in child.GetComponentsInChildren<global::UnityEngine.ParticleSystem>(true))
                        p.Play();
        }

        public static void SetAnimatorTimescale(global::UnityEngine.Transform container, float timeScale)
        {
            foreach (var animator in container.GetComponentsInChildren<global::UnityEngine.Animator>(true))
                animator.speed = timeScale;
        }

        public static void SetParticleSystemSimulationSpeed(global::UnityEngine.Transform container, float timeScale)
        {
            foreach(var particle in container.GetComponentsInChildren<global::UnityEngine.ParticleSystem>(true))
            {
                var main = particle.main;
                main.simulationSpeed = timeScale;
            }
        }

        public static void SetPlayableDirectorUnscaledGameTime(global::UnityEngine.Transform container)
        {
            foreach (var director in container.GetComponentsInChildren<global::UnityEngine.Playables.PlayableDirector>(true))
                director.timeUpdateMode = global::UnityEngine.Playables.DirectorUpdateMode.UnscaledGameTime;
        }

        public static global::UnityEngine.Playables.PlayableDirector GetPlayableDirectorInChildren(global::UnityEngine.Transform container)
        {
            global::UnityEngine.Playables.PlayableDirector returnValue = null;
            for (int i = 0 ; i < container.childCount; i++)
            {
                if (container.GetChild(i).GetComponent<global::UnityEngine.Playables.PlayableDirector>() != null)
                    returnValue = container.GetChild(i).GetComponent<global::UnityEngine.Playables.PlayableDirector>();
                else
                    global::UnityEngine.Object.Destroy(container.GetChild(i).gameObject);
            }
            return returnValue;
        }

        public static int CompareTime(object x, object y)
        {
            if (x == null && y == null) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            var xInfo = (global::System.IO.FileInfo)x;
            var yInfo = (global::System.IO.FileInfo)y;
            return yInfo.LastWriteTime.CompareTo(xInfo.LastWriteTime);
        }

        public static int CompareName(object x, object y)
        {
            if (x == null && y == null) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            var xInfo = (global::System.IO.FileInfo)x;
            var yInfo = (global::System.IO.FileInfo)y;
            return xInfo.FullName.CompareTo(yInfo.FullName);
        }

        public static string GetTimeString()
        {
            return global::System.DateTime.Now.ToString("MM-dd「HH：mm：ss」");
        }

        public static global::System.Collections.Generic.List<global::System.String> GetLocalIPv4()
        {
            var host = global::System.Net.Dns.GetHostEntry(global::System.Net.Dns.GetHostName());
            var returnValue = new global::System.Collections.Generic.List<global::System.String>();
            foreach(var address in  host.AddressList)
                if (address.AddressFamily == global::System.Net.Sockets.AddressFamily.InterNetwork)
                    returnValue.Add(address.ToString() ?? "127.0.0.1");
            return returnValue;
        }

        public static string[] SplitWithPreservedQuotes(string input)
        {
            global::System.Collections.Generic.List<global::System.String> result = new global::System.Collections.Generic.List<global::System.String>();
            int start = 0;
            bool inQuotes = false;

            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == '\"')
                {
                    inQuotes = !inQuotes;
                }
                else if (input[i] == ' ' && !inQuotes)
                {
                    result.Add(input.Substring(start, i - start));
                    start = i + 1;
                }
            }
            result.Add(input.Substring(start));
            for (int i = 0; i < result.Count; i++)
                result[i] = result[i].Replace("\"", "");
            return result.ToArray();
        }

        public static global::System.Collections.Generic.KeyValuePair<TKey, TValue> GetNthDictionaryElement<TKey, TValue>(global::System.Collections.Generic.Dictionary<TKey, TValue> dic, int n)
        {
            if (n < 0)
                n = 0;
            if (n >= dic.Count)
                n = dic.Count - 1;
            var enumerator = dic.GetEnumerator();
            for (int i = 0; i < n + 1; i++)
                enumerator.MoveNext();
            return enumerator.Current;
        }

        public static global::System.Collections.Generic.KeyValuePair<TKey, TValue> GetRandomDictionaryElement<TKey, TValue>(global::System.Collections.Generic.Dictionary<TKey, TValue> dic)
        {
            return GetNthDictionaryElement(dic, global::UnityEngine.Random.Range(0, dic.Count));
        }

        public static bool StringIsAlphaNumeric(string input)
        {
            global::System.Text.RegularExpressions.Regex regex = new global::System.Text.RegularExpressions.Regex("^[A-Za-z0-9]+$");
            return regex.IsMatch(input);
        }
        public static bool StringIsLowerAlphaNumeric(string input)
        {
            global::System.Text.RegularExpressions.Regex regex = new global::System.Text.RegularExpressions.Regex("^[a-z0-9]+$");
            return regex.IsMatch(input);
        }

        public static int GetLocalDeckCount()
        {
            return global::System.IO.Directory.GetFiles(global::YuGiOhFight.Client.Runtime.Program.PATH_DECK, "*.ydk").Length;
        }

        public static global::System.DateTime GetLocalDeckLastEditTime()
        {
            global::System.DateTime dateTime = global::System.DateTime.MinValue;
            foreach(var file in global::System.IO.Directory.GetFiles(global::YuGiOhFight.Client.Runtime.Program.PATH_DECK, "*.ydk"))
            {
                var fileInfo = new global::System.IO.FileInfo(file);
                if(fileInfo.LastWriteTime > dateTime)
                    dateTime = fileInfo.LastWriteTime;
            }
            return dateTime;
        }

        public static global::UnityEngine.GameObject GetHighestY(this global::System.Collections.Generic.List<global::UnityEngine.GameObject> list)
        {
            if (list == null || list.Count == 0)
                return null;

            return list
                .Where(go => go != null)
                .OrderByDescending(go => go.transform.position.y)
                .FirstOrDefault();
        }

        public static global::UnityEngine.GameObject GetLowestY(this global::System.Collections.Generic.List<global::UnityEngine.GameObject> list)
        {
            if (list == null || list.Count == 0)
                return null;

            return list
                .Where(go => go != null)
                .OrderBy(go => go.transform.position.y)
                .FirstOrDefault();
        }

        #region Online

        public static async global::System.Threading.Tasks.Task<global::UnityEngine.Texture2D> DownloadImageAsync(string url, global::System.Threading.CancellationToken cancellationToken = default)
        {
            using var request = global::UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url);
            request.SetRequestHeader("User-Agent", "MDPro3/" + global::UnityEngine.Application.version + " (" + global::System.Environment.OSVersion.ToString() + "); Unity/" + global::UnityEngine.Application.unityVersion);

            var send = request.SendWebRequest();
            while (!send.isDone)
                await global::MDPro3.Utility.TaskUtility.WaitOneFrame(cancellationToken);
            if (!global::UnityEngine.Application.isPlaying)
                return null;

            cancellationToken.ThrowIfCancellationRequested();

            if (request.result == global::UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                return global::UnityEngine.Networking.DownloadHandlerTexture.GetContent(request);
            }
            else
            {
                global::UnityEngine.Debug.LogErrorFormat("Image [{0}]: {1}", url, request.error);
                return null;
            }
        }

        #endregion

        public static global::UnityEngine.Vector3 GetDeckModelTopPosition(global::YuGiOhFight.Client.Presentation.Elements.ElementObjectManager manager)
        {
            global::YuGiOhFight.Client.Presentation.Elements.ElementObjectManager subManager = manager.GetElement<global::YuGiOhFight.Client.Presentation.Elements.ElementObjectManager>("CardShuffleTop");
            var returnValue = subManager.GetElement<global::UnityEngine.Transform>("CardModel01_back").position;
            var position = subManager.GetElement<global::UnityEngine.Transform>("CardModel02_back").position;
            if (position.y > returnValue.y)
                returnValue = position;
            position = subManager.GetElement<global::UnityEngine.Transform>("CardModel03_back").position;
            if (position.y > returnValue.y)
                returnValue = position;
            position = subManager.GetElement<global::UnityEngine.Transform>("CardModel04_back").position;
            if (position.y > returnValue.y)
                returnValue = position;
            return returnValue;
        }

        public static void ClearDirectoryRecursively(global::System.IO.DirectoryInfo directory)
        {
            foreach (var file in directory.GetFiles())
                file.Delete();
            foreach (var subDir in directory.GetDirectories())
            {
                ClearDirectoryRecursively(subDir);
                subDir.Delete();
            }
        }

        public static float CalculateWeightedDistance(global::UnityEngine.Vector3 a, global::UnityEngine.Vector3 b, char priorityAxis)
        {
            bool isPriorityX = global::System.Char.ToLower(priorityAxis) == 'x';
            bool isPriorityY = global::System.Char.ToLower(priorityAxis) == 'y';
            bool isPriorityZ = global::System.Char.ToLower(priorityAxis) == 'z';

            float deltaX = global::UnityEngine.Mathf.Abs(a.x - b.x);
            float deltaY = global::UnityEngine.Mathf.Abs(a.y - b.y);
            float deltaZ = global::UnityEngine.Mathf.Abs(a.z - b.z);

            if (isPriorityX)
            {
                deltaY *= 10;
                deltaZ *= 10;
            }
            else if (isPriorityY)
            {
                deltaX *= 10;
                deltaZ *= 10;
            }
            else if (isPriorityZ)
            {
                deltaX *= 10;
                deltaY *= 10;
            }

            return global::UnityEngine.Mathf.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);
        }

        public static bool InLastRow(int index, int  counts, int columes)
        {
            if (columes <= 0)
            {
                throw new global::System.ArgumentException("columes must be greater than 0");
            }

            int lastRowStartIndex = counts - (counts % columes);
            if (counts % columes == 0)
            {
                lastRowStartIndex = counts - columes;
            }

            return index >= lastRowStartIndex;
        }

        #region Pools

        private static readonly global::System.Collections.Concurrent.ConcurrentBag<global::System.IO.MemoryStream> streamPool = new();

        public static global::System.IO.MemoryStream GetStream()
        {
            if (streamPool.TryTake(out var stream))
            {
                stream.SetLength(0);
                return stream;
            }
            return new global::System.IO.MemoryStream();
        }

        public static void ReturnStream(global::System.IO.MemoryStream stream)
        {
            streamPool.Add(stream);
        }

        #endregion

        #region Path

        public static string GetPlatformFilePath(string path)
        {
            return "file://" + GetPlatformPath(path);
        }

        public static string GetPlatformPath(string path)
        {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX
            return global::System.IO.Path.Combine(global::System.Environment.CurrentDirectory, path);
#elif UNITY_ANDROID || UNITY_IOS
            return Path.Combine(Application.persistentDataPath, path);
#else
            return path;
#endif
        }

        #endregion

        #region Json

        public static string FormatJsonString(string jsonString)
        {
            try
            {
                var obj = global::Newtonsoft.Json.JsonConvert.DeserializeObject(jsonString);
                return global::Newtonsoft.Json.JsonConvert.SerializeObject(obj, global::Newtonsoft.Json.Formatting.Indented);
            }
            catch
            {
                return jsonString;
            }
        }

        #endregion

        #region Screen

        public static float GetScreenAspectRatio()
        {
            return (float)global::UnityEngine.Screen.width / global::UnityEngine.Screen.height;
        }

        public static bool IsAspectRatioWidescreen()
        {
            return GetScreenAspectRatio() >= 1.77f; // 16:9
        }

        #endregion

        #region Text

        public static string RemoveFirstLines(string text, int linesToRemove)
        {
            if (string.IsNullOrEmpty(text) || linesToRemove <= 0)
                return text;

            string[] lines = text.Split(new[] { "\r\n", "\n" }, global::System.StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length < linesToRemove)
                return text;

            var remainingLines = lines.Skip(linesToRemove).ToArray();
            string newLine = text.Contains("\r\n") ? "\r\n" : "\n";
            return string.Join(newLine, remainingLines);
        }

        public static string MergeWithNewLine(string str1, string str2)
        {
            var nonEmpty = new[] { str1, str2 }
                .Where(s => !string.IsNullOrWhiteSpace(s));
            return string.Join(global::System.Environment.NewLine, nonEmpty);
        }

        public static string GetNormalizedNumText(int num, int maxNum = 99)
        {
            if (num < 0)
                return "0";
            else if (num > maxNum)
                return maxNum.ToString() + "+";
            else
                return num.ToString();
        }

        public static int? ExtractLeadingInt(this string input)
        {
            if (string.IsNullOrEmpty(input))
                return null;

            int i = 0;
            while (i < input.Length && char.IsDigit(input[i]))
            {
                i++;
            }

            if (i == 0)
                return null;

            string numberPart = input.Substring(0, i);
            if (int.TryParse(numberPart, out int result))
                return result;
            else
                return null;
        }

        #endregion

        #region Navigation

        public static global::UnityEngine.Transform GetNearest(this global::UnityEngine.Transform self, global::System.Collections.Generic.IEnumerable<global::UnityEngine.Transform> transforms)
        {
            return transforms
                .Where(t => t != null)
                .OrderBy(t => global::UnityEngine.Vector3.Distance(self.position, t.position))
                .FirstOrDefault();
        }

        #endregion

    }
}
