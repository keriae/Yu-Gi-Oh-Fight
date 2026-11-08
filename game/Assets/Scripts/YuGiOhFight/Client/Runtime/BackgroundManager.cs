using System.Collections.Generic;
using UnityEngine;


using System.IO;


using Cysharp.Threading.Tasks;

using Spine.Unity;
using UnityEngine.Playables;


namespace YuGiOhFight.Client.Runtime
{
    public class BackgroundManager : Manager
    {
        GameObject back;
        int cid;
        readonly List<Material> stationaryMaterials = new List<Material>();
        public static readonly Dictionary<int, string> backgrounds = new Dictionary<int, string>()
        {
            {1, "Classic" },
            {2, "Classic2" },
            {11, "ClassicRed" },
            {5, "ClassicPink" },
            {4, "ClassicPinkShine" },
            //{6, "ClassicWhite" },
            {7, "WCS" },
            {8, "Shop" },
            {9, "Knowledge" },
            {3, "PurpleDarkFantasy" },
            {10, "DeepDarkFantasy" },
            //{13, "New" },
            {12, "DIY Green" },
            {14, "Neos" },
            {16, "Classic Grid" },
            {17, "4th Anniversary" },

            {50, "DIY Classic" },
            {51, "DIY Classic2" },
            {52, "DIY Red" },
            {53, "DIY Pink" },
            {54, "DIY PinkShine" },
            //{55, "DIY Purple" },
            {56, "DIY WCS" },
            {57, "DIY SHOP" },
            {58, "DIY Knowledge" },
            {59, "DIY DeepDarkFantasy" },
        };

        public void Refresh()
        {
            Change(cid);
        }

        public void Change(int id)
        {
            Destroy(back);
            ClearStationaryMaterials();

            if (id == 0)
            {
                var random = Random.Range(0, backgrounds.Count);
                id = Utility.Tools.GetNthDictionaryElement(backgrounds, random).Key;
            }

            cid = id;
            if (id == 50)
                id = 1;
            if (id == 51)
                id = 2;
            if (id == 52)
                id = 11;
            if (id == 53)
                id = 5;
            if (id == 54)
                id = 4;
            if (id == 55)
                id = 6;
            if (id == 56)
                id = 7;
            if (id == 57)
                id = 8;
            if (id == 58)
                id = 9;
            if (id == 59)
                id = 10;

            var endString = id.ToString("D4");
            back = ABLoader.LoadFromFolder<SpriteRenderer>("MasterDuel/Background/Back" + endString, true, true);
            if (back.TryGetComponent<Presentation.Effects.SpriteScaler>(out Presentation.Effects.SpriteScaler spriteScaler))
            {
                spriteScaler.isApplyOnUpdate = true;
                spriteScaler.SetFitMode(Presentation.Effects.SpriteScaler.FitMode.FitHighestResolutionMaintainAspectRatio);
            }
            Utility.Tools.ChangeLayer(back, "2D");
            back.transform.SetParent(transform, false);
            KeepBackgroundStationary(back);
            KeepBackgroundMaterialsStationary();

            if(id == 12 || cid >= 50)
            {
                _ = SetDIYBGAsync(back.GetComponent<SpriteRenderer>(), id);
            }
        }

        internal static void KeepBackgroundStationary(GameObject root)
        {
            foreach (var particle in root.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = particle.main;
                main.playOnAwake = false;
                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            foreach (Presentation.UI.Tween tween in root.GetComponentsInChildren<Presentation.UI.Tween>(true))
            {
                tween.Stop();
                tween.enabled = false;
            }
            foreach (Presentation.UI.TweenLauncher launcher in root.GetComponentsInChildren<Presentation.UI.TweenLauncher>(true))
                launcher.enabled = false;
            foreach (var animator in root.GetComponentsInChildren<Animator>(true))
            {
                animator.Update(0f);
                animator.speed = 0f;
                animator.enabled = false;
            }
            foreach (var director in root.GetComponentsInChildren<PlayableDirector>(true))
            {
                director.playOnAwake = false;
                director.Pause();
                director.enabled = false;
            }
            // Leave Spine rendering enabled so its initial mesh can be generated.
            foreach (var skeleton in root.GetComponentsInChildren<SkeletonAnimation>(true))
                skeleton.timeScale = 0f;
            foreach (var skeleton in root.GetComponentsInChildren<SkeletonGraphic>(true))
                skeleton.timeScale = 0f;
        }

        private void KeepBackgroundMaterialsStationary()
        {
            foreach (var renderer in back.GetComponentsInChildren<SpriteRenderer>(true))
            {
                var materials = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < materials.Length; i++)
                {
                    var material = materials[i];
                    if (material == null || !material.HasProperty("_LineSpeed1")
                        || !material.HasProperty("_LineSpeed2")
                        || !material.HasProperty("_RandomLineSpeed"))
                        continue;

                    // Back0009 scrolls in its shader; never modify the shared asset.
                    var stationary = new Material(material);
                    stationary.SetFloat("_LineSpeed1", 0f);
                    stationary.SetFloat("_LineSpeed2", 0f);
                    stationary.SetFloat("_RandomLineSpeed", 0f);
                    materials[i] = stationary;
                    stationaryMaterials.Add(stationary);
                    changed = true;
                }
                if (changed)
                    renderer.sharedMaterials = materials;
            }
        }

        private void ClearStationaryMaterials()
        {
            foreach (var material in stationaryMaterials)
                Destroy(material);
            stationaryMaterials.Clear();
        }

        private void OnDestroy()
        {
            ClearStationaryMaterials();
        }

        private async UniTask SetDIYBGAsync(SpriteRenderer renderer, int id)
        {
            var bg = Program.PATH_DIY + "Background";
            if (File.Exists(bg + Program.EXPANSION_PNG))
                bg += Program.EXPANSION_PNG;
            else if (File.Exists(bg + Program.EXPANSION_JPG))
                bg += Program.EXPANSION_JPG;
            else
                return;

            var pic = await TextureManager.LoadPicFromFileAsync(bg);
            int targetHeight = 1080;
            Texture2D scaledTexture;
            if(pic.height != targetHeight)
            {
                int newWidth = Mathf.RoundToInt(pic.width * (targetHeight / (float)pic.height));
                scaledTexture = Presentation.TextureProcessor.ResizeTexture2D(pic, newWidth, targetHeight);
            }
            else
                scaledTexture = pic;

            if (renderer != null)
                renderer.sprite = TextureManager.Texture2Sprite(scaledTexture);
            if(id == 9)
                renderer.material.SetTexture("_MainTex01", scaledTexture);
        }

        public int GetIDByName(string bgName)
        {
            var id = 0;
            if(bgName == MDPro3.InterString.Get("随机"))
                return 0;
            foreach (var background in backgrounds)
            {
                if(bgName == background.Value)
                {
                    id = background.Key;
                    break;
                }
            }
            return id;
        }
    }
}
