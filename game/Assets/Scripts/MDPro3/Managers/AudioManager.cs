using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace MDPro3
{
    public class AudioManager : Manager
    {

        #region Base

        public AudioSource seR;
        public AudioSource bgmR;
        public AudioSource voiceR;

        private static AudioSource se;
        private static AudioSource bgm;
        private static AudioSource voice;

        public const string BGM_MENU_MAIN = "BGM_MENU_01";

        public override void Initialize()
        {
            base.Initialize();
            se = seR;
            bgm = bgmR;
            voice = voiceR;
            AudioSettings.OnAudioConfigurationChanged += OnAudioConfigurationChanged;

            PlayBGM(BGM_MENU_MAIN);
        }

        private void OnAudioConfigurationChanged(bool deviceWasChanged)
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            if (deviceWasChanged)
            {
                AudioConfiguration config = AudioSettings.GetConfiguration();
                AudioSettings.Reset(config);
            }
            bgm.Play();
#endif
        }

        public static void SetSeVol(float vol)
        {
            se.volume = vol;
        }

        public static void SetBGMVol(float vol)
        {
            bgm.volume = vol * currentBGMScale;
        }

        public static void SetVoiceVol(float vol)
        {
            voice.volume = vol;
        }

        public static IEnumerator<AudioClip> LoadAudioFileAsync(string path, AudioType audioType)
        {
            string fullPath;
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
            fullPath = "file://" + Application.persistentDataPath + Program.STRING_SLASH + path;
#elif UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX
            fullPath = Path.Combine("file://" + Environment.CurrentDirectory, path);
#else
            fullPath = Path.Combine(Environment.CurrentDirectory, path);
#endif

            using var request = UnityWebRequestMultimedia.GetAudioClip(fullPath, audioType);
            var wait = request.SendWebRequest();

            while (!wait.isDone)
                yield return null;

            if (request.result == UnityWebRequest.Result.Success)
            {
                var audioClip = DownloadHandlerAudioClip.GetContent(request);
                yield return audioClip;
            }
        }

        public static async UniTask<AudioClip> LoadAudioFileUniAsync(string path, AudioType audioType)
        {
            string fullPath;
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
            fullPath = "file://" + Application.persistentDataPath + Program.STRING_SLASH + path;
#elif UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX
            fullPath = Path.Combine("file://" + Environment.CurrentDirectory, path);
#else
            fullPath = Path.Combine(Environment.CurrentDirectory, path);
#endif

            using var request = UnityWebRequestMultimedia.GetAudioClip(fullPath, audioType);
            await request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var audioClip = DownloadHandlerAudioClip.GetContent(request);
                return audioClip;
            }

            return null;
        }

        #endregion

        #region SE

        private struct LastSE
        {
            public float time;
            public string seName;
        }

        private static LastSE lastSE = new();
        public static string nextMuteSE;
        private static readonly Dictionary<string, bool> seAddressExists = new();
        private const string OneShotSuffix = "_oneshot";

        private static async UniTask<bool> HasSEAddress(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            if (seAddressExists.TryGetValue(path, out var exists))
                return exists;

            var handle = Addressables.LoadResourceLocationsAsync(path, typeof(AudioClip));
            try
            {
                await handle.Task;
                exists = handle.Status == AsyncOperationStatus.Succeeded
                         && handle.Result != null
                         && handle.Result.Count > 0;
            }
            catch
            {
                exists = false;
            }
            finally
            {
                if (handle.IsValid())
                    Addressables.Release(handle);
            }

            seAddressExists[path] = exists;
            return exists;
        }

        private static async UniTask<string> ResolveSEAddress(string path)
        {
            if (await HasSEAddress(path))
                return path;

            if (path.EndsWith(OneShotSuffix, StringComparison.OrdinalIgnoreCase))
            {
                var fallbackPath = path.Substring(0, path.Length - OneShotSuffix.Length);
                if (await HasSEAddress(fallbackPath))
                    return fallbackPath;
            }

            return null;
        }

        private static async UniTaskVoid PlaySEInternal(string path, float volumeScale)
        {
            var resolvedPath = await ResolveSEAddress(path);
            if (string.IsNullOrEmpty(resolvedPath))
                return;

            var handle = Addressables.LoadAssetAsync<AudioClip>(resolvedPath);
            try
            {
                await handle.Task;
            }
            catch
            {
                return;
            }

            if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null && se != null)
                se.PlayOneShot(handle.Result, volumeScale);
        }

        public static void PlaySE(string path, float volumeScale = 1)
        {
            if (string.IsNullOrEmpty(path))
                return;

            if (se == null)
                return;

            if (path == nextMuteSE)
            {
                nextMuteSE = string.Empty;
                return;
            }

            if (lastSE.time > 0 && lastSE.seName == path && Time.time - lastSE.time < 0.1f)
                return;

            lastSE.time = Time.time;
            lastSE.seName = path;
            _ = PlaySEInternal(path, volumeScale);
        }

        public void PlayShuffleSE()
        {
            List<string> ses = new()
            {
                "SE_CARD_MOVE_01",
                "SE_CARD_MOVE_02",
                "SE_CARD_MOVE_03",
                "SE_CARD_MOVE_04"
            };
            StartCoroutine(PlaySEGroup(ses));
        }

        private static IEnumerator PlaySEGroup(List<string> ses, float volumeScale = 1)
        {
            foreach (string s in ses)
            {
                var handle = Addressables.LoadAssetAsync<AudioClip>(s);
                while (!handle.IsDone)
                    yield return null;
                se.PlayOneShot(handle.Result, volumeScale);
                yield return new WaitForSeconds(handle.Result.length * 0.5f);
            }
        }

        public static void PlaySEClip(AudioClip clip, float volumeScale = 1)
        {
            se.PlayOneShot(clip, volumeScale);
        }

        public static void ResetSESource()
        {
            se.gameObject.SetActive(false);
            se.gameObject.SetActive(true);
        }

        #endregion

        #region Voice

        public static void PlayVoiceByResourcePath(string path)
        {
            var clip = Resources.Load<AudioClip>(path);
            if (clip != null)
                voice.PlayOneShot(clip);
        }

        public static void PlayVoice(AudioClip clip)
        {
            voice.PlayOneShot(clip);
        }

        #endregion

        #region BGM

        private enum BgmType
        {
            NORMAL,
            KEYCARD,
            CLIMAX
        }

        private static List<string> currentBGMs = new();
        private static string currentBGM = string.Empty;
        private static int bgmState = 0;
        private static float loopStart = 0;
        private static float loopEnd = 10;

        private struct BgmLoop
        {
            public string name;
            public float startTime;
            public float endTime;
        }

        private readonly static List<BgmLoop> loops = new()
        {
            new BgmLoop{name = "BGM_DUEL_NORMAL_01", startTime = 9.600f, endTime = 60 + 55.200f },
            new BgmLoop{name = "BGM_DUEL_NORMAL_02", startTime = 16.500f, endTime = 60 + 48.500f },
            new BgmLoop{name = "BGM_DUEL_NORMAL_03", startTime = 5.727f, endTime = 120 + 11.444f },
            new BgmLoop{name = "BGM_DUEL_NORMAL_04", startTime = 13.518f, endTime = 60 + 57.300f },
            new BgmLoop{name = "BGM_DUEL_NORMAL_05", startTime = 11.208f, endTime = 120 + 22.875f },
            new BgmLoop{name = "BGM_DUEL_NORMAL_06", startTime = 9.527f, endTime = 60 + 41.906f },
            new BgmLoop{name = "BGM_DUEL_NORMAL_07", startTime = 17.456f, endTime = 120 +9.247f },
            new BgmLoop{name = "BGM_DUEL_NORMAL_08", startTime = 18.400f, endTime = 120 + 12.400f },
            new BgmLoop{name = "BGM_DUEL_NORMAL_09", startTime = 6.200f, endTime = 60 +51.400f },
            new BgmLoop{name = "BGM_DUEL_NORMAL_10", startTime = 9.989f, endTime = 60 + 51.636f },
            new BgmLoop{name = "BGM_DUEL_NORMAL_11", startTime = 2.378f, endTime = 60 +29.650f },
            new BgmLoop{name = "BGM_DUEL_NORMAL_12", startTime = 7.500f, endTime = 60 +47.800f },
            new BgmLoop{name = "BGM_DUEL_NORMAL_13", startTime = 7.433f, endTime = 60 + 54.741f },
            new BgmLoop{name = "BGM_DUEL_NORMAL_14", startTime = 5.538f, endTime = 60 +34.142f },
            new BgmLoop{name = "BGM_DUEL_NORMAL_15", startTime = 8.455f, endTime = 60 +34.855f },
            new BgmLoop{name = "BGM_DUEL_NORMAL_16", startTime = 14.440f, endTime = 60 + 44.440f },

            new BgmLoop{name = "BGM_DUEL_KEYCARD_01", startTime = 11.744f, endTime = 60 + 49.390f },
            new BgmLoop{name = "BGM_DUEL_KEYCARD_02", startTime = 10.500f, endTime = 60 + 46.500f },
            new BgmLoop{name = "BGM_DUEL_KEYCARD_03", startTime = 13.697f, endTime = 60 + 38.150f },
            new BgmLoop{name = "BGM_DUEL_KEYCARD_04", startTime = 7.032f, endTime = 60 + 49.888f },
            new BgmLoop{name = "BGM_DUEL_KEYCARD_05", startTime = 12.495f, endTime = 60 +23.079f },
            new BgmLoop{name = "BGM_DUEL_KEYCARD_06", startTime = 11.400f, endTime = 60 + 38.400f },
            new BgmLoop{name = "BGM_DUEL_KEYCARD_07", startTime = 6.518f, endTime = 60 + 24.928f },
            new BgmLoop{name = "BGM_DUEL_KEYCARD_08", startTime = 13.783f, endTime = 60 + 57.727f },
            new BgmLoop{name = "BGM_DUEL_KEYCARD_09", startTime = 3.800f, endTime = 60 + 20.300f },
            new BgmLoop{name = "BGM_DUEL_KEYCARD_10", startTime = 17.599f, endTime = 60 + 40.508f },
            new BgmLoop{name = "BGM_DUEL_KEYCARD_11", startTime = 11.738f, endTime = 60 + 57.104f },
            new BgmLoop{name = "BGM_DUEL_KEYCARD_12", startTime = 13.630f, endTime = 60 + 45.684f },
            new BgmLoop{name = "BGM_DUEL_KEYCARD_13", startTime = 18.519f, endTime = 60 + 55.734f },
            new BgmLoop{name = "BGM_DUEL_KEYCARD_14", startTime = 2.269f, endTime = 60 + 35.830f },
            new BgmLoop{name = "BGM_DUEL_KEYCARD_15", startTime = 11.369f, endTime = 60 + 41.369f },
            new BgmLoop{name = "BGM_DUEL_KEYCARD_16", startTime = 6.348f, endTime = 60 + 36.151f },

            new BgmLoop{name = "BGM_DUEL_CLIMAX_01", startTime = 6.300f, endTime = 60 + 37.800f },
            new BgmLoop{name = "BGM_DUEL_CLIMAX_02", startTime = 12.883f, endTime = 60 + 53.958f },
            new BgmLoop{name = "BGM_DUEL_CLIMAX_03", startTime = 12.579f, endTime = 120 + 7.444f },
            new BgmLoop{name = "BGM_DUEL_CLIMAX_04", startTime = 3.325f, endTime = 60 + 31.047f },
            new BgmLoop{name = "BGM_DUEL_CLIMAX_05", startTime = 5.424f, endTime = 60 + 37.188f },
            new BgmLoop{name = "BGM_DUEL_CLIMAX_06", startTime = 5.896f, endTime = 60 + 26.184f },
            new BgmLoop{name = "BGM_DUEL_CLIMAX_07", startTime = 11.500f, endTime = 60 + 31.500f },
            new BgmLoop{name = "BGM_DUEL_CLIMAX_08", startTime = 15.547f, endTime = 60 + 48.505f },
            new BgmLoop{name = "BGM_DUEL_CLIMAX_09", startTime = 6.300f, endTime = 60 + 28.800f },
            new BgmLoop{name = "BGM_DUEL_CLIMAX_10", startTime = 2.500f, endTime = 60 + 34.500f },
            new BgmLoop{name = "BGM_DUEL_CLIMAX_11", startTime = 13.223f, endTime = 60 + 43.955f },
            new BgmLoop{name = "BGM_DUEL_CLIMAX_12", startTime = 6.448f, endTime = 60 + 34.252f },
            new BgmLoop{name = "BGM_DUEL_CLIMAX_13", startTime = 5.637f, endTime = 60 + 50.429f },
            new BgmLoop{name = "BGM_DUEL_CLIMAX_14", startTime = 12.169f, endTime = 60 + 48.165f },
            new BgmLoop{name = "BGM_DUEL_CLIMAX_15", startTime = 7.056f, endTime = 60 + 39.847f },
            new BgmLoop{name = "BGM_DUEL_CLIMAX_16", startTime = 9.606f, endTime = 60 + 28.067f },

            new BgmLoop{name = "BGM_DUEL_DC01_NORMAL", startTime = 3.830f, endTime = 120 + 1.649f },
            new BgmLoop{name = "BGM_DUEL_DC01_KEYCARD", startTime = 3.128f, endTime = 120 + 4.974f },
            new BgmLoop{name = "BGM_DUEL_DC01_CLIMAX", startTime = 7.650f, endTime = 120 + 2.620f },
            new BgmLoop{name = "BGM_DUEL_DC02_NORMAL", startTime = 6.892f, endTime = 120 + 4.404f },
            new BgmLoop{name = "BGM_DUEL_DC02_KEYCARD", startTime = 21.349f, endTime = 120 + 0.908f },
            new BgmLoop{name = "BGM_DUEL_DC02_CLIMAX", startTime = 1.858f, endTime = 60 + 55.380f },

            new BgmLoop{name = "BGM_DUEL_EX_01", startTime = 21.014f, endTime = 60 + 57.026f },
            new BgmLoop{name = "BGM_DUEL_EX_02_NORMAL", startTime = 2.466f, endTime = 60 + 47.193f },
            new BgmLoop{name = "BGM_DUEL_EX_02_KEYCARD", startTime = 6.941f, endTime = 60 + 46.766f },
            new BgmLoop{name = "BGM_DUEL_EX_02_CLIMAX", startTime = 2.346f, endTime = 60 + 43.210f },
            new BgmLoop{name = "BGM_DUEL_EX_03_NORMAL", startTime = 11.478f, endTime = 60 + 56.473f },
            new BgmLoop{name = "BGM_DUEL_EX_03_KEYCARD", startTime = 12.463f, endTime = 60 + 46.098f },
            new BgmLoop{name = "BGM_DUEL_EX_03_CLIMAX", startTime = 1.815f, endTime = 120 + 8.792f },
            new BgmLoop{name = "BGM_DUEL_EX_04_NORMAL", startTime = 3.391f, endTime = 60 + 46.250f },
            new BgmLoop{name = "BGM_DUEL_EX_04_KEYCARD", startTime = 3.139f, endTime = 60 + 30.436f },
            new BgmLoop{name = "BGM_DUEL_EX_04_CLIMAX", startTime = 3.063f, endTime = 60 + 52.417f },
            new BgmLoop{name = "BGM_DUEL_EX_05_NORMAL", startTime = 8.796f, endTime = 120 + 5.869f },
            new BgmLoop{name = "BGM_DUEL_EX_05_KEYCARD", startTime = 7.832f, endTime = 120 + 2.615f },
            new BgmLoop{name = "BGM_DUEL_EX_05_CLIMAX", startTime = 2.039f, endTime = 120 + 6.835f },
            new BgmLoop{name = "BGM_DUEL_EX_06_ALL", startTime = 4.950f, endTime = 180 + 7.141f },
            new BgmLoop{name = "BGM_DUEL_EX_07_PHASE_A", startTime = 20.499f, endTime = 120 + 25.909f },
            new BgmLoop{name = "BGM_DUEL_EX_07_PHASE_B", startTime = 7.757f, endTime = 120 + 27.756f },
            new BgmLoop{name = "BGM_DUEL_EX_08_ALL", startTime = 21.667f, endTime = 180 + 17.979f },
            new BgmLoop{name = "BGM_DUEL_EX_09_PHASE_A", startTime = 5.488f, endTime = 120 + 3.416f },
            new BgmLoop{name = "BGM_DUEL_EX_09_PHASE_B", startTime = 0.802f, endTime = 120 + 2.501f },
            new BgmLoop{name = "BGM_DUEL_EX_10_NORMAL", startTime = 0.0f, endTime = 60 + 27.832f },
            new BgmLoop{name = "BGM_DUEL_EX_10_KEYCARD", startTime = 09.512f, endTime = 60 + 30.265f },
            new BgmLoop{name = "BGM_DUEL_EX_10_CLIMAX", startTime = 1.516f, endTime = 60 + 31.898f },


            new BgmLoop{name = "BGM_DUEL_F01_ALL", startTime = 24.219f, endTime = 120 + 42.886f },
            new BgmLoop{name = "BGM_DUEL_F02_PHASE_A", startTime = 13.603f, endTime = 60 + 43.324f },
            new BgmLoop{name = "BGM_DUEL_F02_PHASE_B", startTime = 14.818f, endTime = 120 + 2.645f },
            new BgmLoop{name = "BGM_DUEL_F03_PHASE_A", startTime = 2.905f, endTime = 60 + 42.120f },
            new BgmLoop{name = "BGM_DUEL_F03_PHASE_B", startTime = 9.520f, endTime = 60 + 30.959f },
            new BgmLoop{name = "BGM_DUEL_F03_PHASE_C", startTime = 28.667f, endTime = 60 + 49.794f },
            new BgmLoop{name = "BGM_DUEL_F04_PHASE_A", startTime = 32.952f, endTime = 60 + 56.523f },
            new BgmLoop{name = "BGM_DUEL_F04_PHASE_B", startTime = 19.020f, endTime = 120 + 26.755f },
            new BgmLoop{name = "BGM_DUEL_F05_PHASE_A", startTime = 13.323f, endTime = 120 + 15.155f },
            new BgmLoop{name = "BGM_DUEL_F05_PHASE_B", startTime = 7.291f, endTime = 120 + 13.283f },
            new BgmLoop{name = "BGM_DUEL_F06_PHASE_A", startTime = 3.831f, endTime = 120 + 9.429f }, //fix 20260130
            new BgmLoop{name = "BGM_DUEL_F06_PHASE_B", startTime = 0.739f, endTime = 120 + 12.236f },
            new BgmLoop{name = "BGM_DUEL_F07_PHASE_A", startTime = 24.566f, endTime = 120 + 8.425f },
            new BgmLoop{name = "BGM_DUEL_F07_PHASE_B", startTime = 0.552f, endTime = 60 + 24.069f },
            new BgmLoop{name = "BGM_DUEL_F08_PHASE_A", startTime = 0.0f, endTime = 120 + 10.439f },
            new BgmLoop{name = "BGM_DUEL_F08_PHASE_B", startTime = 8.569f, endTime = 120 + 8.047f },
            new BgmLoop{name = "BGM_DUEL_F09_PHASE_A", startTime = 0.757f, endTime = 120 + 18.982f }, //Added 20260130
            new BgmLoop{name = "BGM_DUEL_F09_PHASE_B", startTime = 39.995f, endTime = 120 + 7.306f }, //Added 20260130
            new BgmLoop{name = "BGM_DUEL_F10_PHASE_A", startTime = 0.507f, endTime = 60 + 45.980f }, //Added 20260130
            new BgmLoop{name = "BGM_DUEL_F10_PHASE_B", startTime = 9.092f, endTime = 60 + 55.030f }, //Added 20260130
            new BgmLoop{name = "BGM_DUEL_F10_PHASE_C", startTime = 1.353f, endTime = 120 + 22.772f }, //Added 20260130
            new BgmLoop{name = "BGM_DUEL_F11_PHASE_A", startTime = 20.993f, endTime = 120 + 0.334f }, //Added 20260430
            new BgmLoop{name = "BGM_DUEL_F11_PHASE_B", startTime = 15.102f, endTime = 60 + 59.749f }, //Added 20260430
    
            new BgmLoop{name = "BGM_DUEL_RATE01_NORMAL", startTime = 1.119f, endTime = 120 + 5.607f },
            new BgmLoop{name = "BGM_DUEL_RATE01_KEYCARD", startTime = 1.761f, endTime = 60 + 55.984f },
            new BgmLoop{name = "BGM_DUEL_RATE01_CLIMAX", startTime = 0.822f, endTime = 60 + 53.925f },

            new BgmLoop{name = "BGM_MENU_01", startTime = 12.433f, endTime = 120 + 31.100f },
            new BgmLoop{name = "BGM_MENU_02", startTime = 15.687f, endTime = 120 + 2.354f },
            new BgmLoop{name = "BGM_MENU_RETRO", startTime = 0.000f, endTime = 120 + 18.666f },

            new BgmLoop{name = "BGM_DICERALLY", startTime = 17.720f, endTime = 60 + 12.578f },
            new BgmLoop{name = "BGM_MD_TEST_QUIZ", startTime = 0.226f, endTime = 49.559f },
            new BgmLoop{name = "BGM_SOLO_GATE", startTime = 10.053f, endTime = 60 + 36.454f },
            new BgmLoop{name = "BGM_TUTORIAL_01", startTime = 13.992f, endTime = 43.326f },
            new BgmLoop{name = "BGM_OUT_TUTORIAL_2", startTime = 7.480f, endTime = 60 + 22.480f },
        };


        private static readonly List<int> bgms = new()
        {
            1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16
        };

        private static readonly List<int> commonBgms = new()
        {
            1, 3, 7
        };

        private static readonly List<int> colosseumBgms = new()
        {
            1, 2, 3, 4, 5, 6, 7, 8
        };

        private static readonly List<int> cyberseBgms = new()
        {
            1, 2
        };

        private static readonly Dictionary<int, List<int>> fieldBGMs = new()
        {
            {1, new List<int>{ 2, 12, 29, 31 } }, //「森」「未界域－欧玛利亚大陆」「染上樱色的森林」「为白色所覆的森林」
            {2, new List<int>{ 4, 3, 21 } }, //「齿车街」「魔导书廊」「教导的圣堂」
            {3, new List<int>{ 1, 11 } }, //「仪式之间」「荒野的祭殿」
            {4, new List<int>{ 5, 6 } }, //「火山」「星遺物沉眠的废墟」
            {5, new List<int>{ 7 } }, //「异国之都」
            {6, new List<int>{ 15, 24 } }, //「鲜彩之苍海」「辉石的洞窟」
            {7, new List<int>{ 9 } }, //「摩天楼」
            {8, new List<int>{ 502 } }, // High School JP Championship 20260212
            {9, new List<int>{ 16, 17 } }, //「魔偶甜点城堡」「鬼计之馆」
            {10, new List<int>{  } },
            {11, new List<int>{ 26, 50 } }, //Ursarctic, E-Football(MD) 20260106
            {12, new List<int>{ 19, 25, 36 } }, //「突异变种进化研究所」「六世坏=天魔世界」「罪宝遗迹」
            {13, new List<int>{ 14 } }, //「相剑的灵峰」
            {14, new List<int>{ 22 } }, //「恶魔宫殿」
            {15, new List<int>{ 10, 35 } }, //「冻结的世界」「冻结的世界」
            {16, new List<int>{ 20, 27, 28 } }, //「古之决斗的记忆」「苏醒的天空殿」「青色眼睛的灵堂」
        };

        private static readonly Dictionary<int, List<string>> fieldColBGMs = new() // New list for Colosseum 20251203
		{
            {1, new List<string>{ "BGM_DUEL_EX_02_NORMAL", "BGM_DUEL_EX_02_KEYCARD", "BGM_DUEL_EX_02_CLIMAX" }}, // 角斗场
			{2, new List<string>{ "BGM_DUEL_DC01_NORMAL", "BGM_DUEL_DC01_KEYCARD", "BGM_DUEL_DC01_CLIMAX" }},    // DC 1st
			{3, new List<string>{ "BGM_DUEL_DC02_NORMAL", "BGM_DUEL_DC02_KEYCARD", "BGM_DUEL_DC02_CLIMAX" }},    // DC 2nd
			{4, new List<string>{ "BGM_DUEL_EX_04_NORMAL", "BGM_DUEL_EX_04_KEYCARD", "BGM_DUEL_EX_04_CLIMAX" }}, // Xyz CUP 1st
			{5, new List<string>{ "BGM_DUEL_EX_05_NORMAL", "BGM_DUEL_EX_05_KEYCARD", "BGM_DUEL_EX_05_CLIMAX" }}, // Xyz CUP 2nd
			{6, new List<string>{ "BGM_DUEL_EX_06_ALL" }},                                                       // WCQ2025 1st
			{7, new List<string>{ "BGM_DUEL_EX_07_PHASE_A", "BGM_DUEL_EX_07_PHASE_B" }},                         // WCQ2025 2nd
			{8, new List<string>{ "BGM_DUEL_EX_10_NORMAL", "BGM_DUEL_EX_10_KEYCARD", "BGM_DUEL_EX_10_CLIMAX" }}, // New Event 20260106?
		};

        private static readonly Dictionary<int, List<string>> fieldCybBGMs = new() // New list for Cyberse 20260106
		{
            {1, new List<string>{ "BGM_DUEL_NORMAL_12", "BGM_DUEL_KEYCARD_12", "BGM_DUEL_CLIMAX_12" }}, // 「电脑的宇宙」
			{2, new List<string>{ "BGM_DUEL_EX_01" }},    //World Legacy Last stage Cyberse Field 
		};

        private static readonly Dictionary<List<string>, List<int>> specialFieldBGMs = new()
        {
            {new List<string>{ "BGM_DUEL_EX_03_NORMAL", "BGM_DUEL_EX_03_KEYCARD", "BGM_DUEL_EX_03_CLIMAX" }, new List<int>{ 13 } }, //「WCS」
            {new List<string>{ "BGM_DUEL_EX_08_ALL" }, new List<int>{ 45 } }, //WCS2025 //added 20251203
            {new List<string>{ "BGM_DUEL_EX_09_PHASE_A", "BGM_DUEL_EX_09_PHASE_B" }, new List<int>{ 504 } }, //Synchro CUP 20251203
            //{new List<string>{ "BGM_DUEL_EX_10_NORMAL", "BGM_DUEL_EX_10_KEYCARD", "BGM_DUEL_EX_10_CLIMAX" }, new List<int>{  } }, //New Event?
            
            {new List<string>{ "BGM_DUEL_F01_ALL" }, new List<int>{ 38 } }, //「邪恶双子」
            {new List<string>{ "BGM_DUEL_F02_PHASE_A", "BGM_DUEL_F02_PHASE_B" }, new List<int>{ 34 } }, //Lost world 20260106
            {new List<string>{ "BGM_DUEL_F03_PHASE_A", "BGM_DUEL_F03_PHASE_B", "BGM_DUEL_F03_PHASE_C" }, new List<int>{ 40 } }, //「闪刀行动模拟区域」
            {new List<string>{ "BGM_DUEL_F04_PHASE_A", "BGM_DUEL_F04_PHASE_B" }, new List<int>{ 42 } }, //Despia 「烙印剧城 惨绝戏 暂定」
            {new List<string>{ "BGM_DUEL_F05_PHASE_A", "BGM_DUEL_F05_PHASE_B" }, new List<int>{ 41 } }, //Graveyard of Myriad Souls //added 20251203
            {new List<string>{ "BGM_DUEL_F06_PHASE_A", "BGM_DUEL_F06_PHASE_B" }, new List<int>{ 47 } }, //White Forest //added 20251203
            {new List<string>{ "BGM_DUEL_F07_PHASE_A", "BGM_DUEL_F07_PHASE_B" }, new List<int>{ 46 } }, //E-Football //added 20251203
            {new List<string>{ "BGM_DUEL_F08_PHASE_A", "BGM_DUEL_F08_PHASE_B" }, new List<int>{ 39 } }, //Labrynth //added 20251203
			{new List<string>{ "BGM_DUEL_F09_PHASE_A", "BGM_DUEL_F09_PHASE_B" }, new List<int>{ 48 } }, //K9 20260130
            {new List<string>{ "BGM_DUEL_F10_PHASE_A", "BGM_DUEL_F10_PHASE_B", "BGM_DUEL_F10_PHASE_C" }, new List<int>{ 49 } }, //IP 20260130
			{new List<string>{ "BGM_DUEL_F11_PHASE_A", "BGM_DUEL_F11_PHASE_B" }, new List<int>{ 51 } }, //Elfnote //added 20260430

            //{new List<string>{ "BGM_DUEL_RATE01_NORMAL", "BGM_DUEL_RATE01_KEYCARD", "BGM_DUEL_RATE01_CLIMAX" }, new List<int>{ } },

        };

        private static readonly List<string> rateBgms = new()//only Rate BGM 20251203
		{
            "BGM_DUEL_RATE01_NORMAL",
            "BGM_DUEL_RATE01_KEYCARD",
            "BGM_DUEL_RATE01_CLIMAX"
        };

        private static int GetFieldIdByFieldName(string fieldName)
        {
            return int.Parse(fieldName.Substring(4, 3));
        }

        private const float RateBgmChance = 0.05f; //Add Random rate for Rate BGM as 5%  20251203

        private static List<string> GetBgmsByFieldId(int fieldId)
        {
            //Add Rate condition as RANDOM Appear  20251203
            if (UnityEngine.Random.value < RateBgmChance)
            {
                return rateBgms;
            }
            foreach (var pair in fieldBGMs)
                if (pair.Value.Contains(fieldId))
                {
                    return new List<string>() { $"BGM_DUEL_NORMAL_{pair.Key:D2}", $"BGM_DUEL_KEYCARD_{pair.Key:D2}", $"BGM_DUEL_CLIMAX_{pair.Key:D2}" };
                }
            //Add Colosseum condition  20251203
            if (fieldId == 8)
            {
                var index = colosseumBgms[UnityEngine.Random.Range(0, colosseumBgms.Count)];
                if (fieldColBGMs.TryGetValue(index, out var colBgmList))
                {
                    return colBgmList;
                }
            }
            if (fieldId == 18)//add cyberseBgms 20260116 fixed
            {
                var index = cyberseBgms[UnityEngine.Random.Range(0, cyberseBgms.Count)];
                if (fieldCybBGMs.TryGetValue(index, out var cybBgmList))
                {
                    return cybBgmList;
                }
            }
            foreach (var pair in specialFieldBGMs)
                if (pair.Value.Contains(fieldId))
                {
                    return pair.Key;
                }
            var undefined = commonBgms[UnityEngine.Random.Range(0, commonBgms.Count)];
            return new List<string>() { $"BGM_DUEL_NORMAL_{undefined:D2}", $"BGM_DUEL_KEYCARD_{undefined:D2}", $"BGM_DUEL_CLIMAX_{undefined:D2}" };

        }

        public static void PlayBgmNormal(string filedName)
        {
            bgmState = 0;
            var fieldId = GetFieldIdByFieldName(filedName);
            currentBGMs = GetBgmsByFieldId(fieldId);
            if (currentBGMs.Count > 0)
                PlayBGM(currentBGMs[0]);
            if (currentBGMs.Count == 1) bgmState = 2; //20260522 Force to State 2 if there is only 1 BGM
        }

        public static void PlayBgmKeyCard()
        {
            if (bgmState > 0)
                return;

            bgmState = 1;
            if (currentBGMs.Count > 1)
                PlayBGM(currentBGMs[1]);
            if (currentBGMs.Count == 2) bgmState = 2; //20260522 Force to State 2 if there is only 2 BGMs
        }

        public static void PlayBgmClimax()
        {
            if (bgmState == 2)
                return;

            bgmState = 2;
            //20260522 Fix Climax BGM for some fields that have only 1 or 2 BGMs
            if (currentBGMs.Count >= 3) PlayBGM(currentBGMs[2]);
            else if (currentBGMs.Count == 2) PlayBGM(currentBGMs[1]);
        }

        private static float currentBGMScale = 1f;

        public static void PlayBGM(string path, float volumeScale = 1f)
        {
            currentBGMScale = volumeScale;
            var handle = Addressables.LoadAssetAsync<AudioClip>(path);
            handle.Completed += (result) =>
            {
                var volume = Program.instance.setting.GetBGMVolum() * currentBGMScale;
                DOTween.To(() => volume, x => bgm.volume = x, 0, 0.2f).OnComplete(() =>
                {
                    SetCurrentBGM(path, result.Result.length);
                    bgm.volume = volume;
                    bgm.clip = result.Result;
                    bgm.time = 0;
                    bgm.Play();
                });
            };
        }

        public static void StopBGM()
        {
            var volume = bgm.volume;
            DOTween.To(() => volume, x => bgm.volume = x, 0, 0.5f).OnComplete(() =>
            {
                bgm.Stop();
                bgm.volume = volume;
            });
        }

        public static void PlayRandomKeyCardBGM()
        {
            var randomID = bgms[UnityEngine.Random.Range(0, bgms.Count)];
            PlayBGM($"BGM_DUEL_KEYCARD_{randomID:D2}");
        }

        private static void SetCurrentBGM(string bgm, float bgmLength)
        {
            currentBGM = bgm;
            bool found = false;
            foreach (var loop in loops)
            {
                if (loop.name == currentBGM)
                {
                    found = true;
                    loopStart = loop.startTime;
                    loopEnd = loop.endTime;
                    break;
                }
            }
            if (!found)
            {
                loopStart = 0;
                loopEnd = bgmLength - 1;
            }
        }

        private void Update()
        {
            if (bgm == null)
                return;
            if (bgm.time > loopEnd)
                bgm.time = loopStart;
        }

        #endregion

    }
}
