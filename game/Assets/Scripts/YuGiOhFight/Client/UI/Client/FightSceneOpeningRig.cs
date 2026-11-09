using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace YuGiOhFight.Client.UI.Client
{
    // Authored components stay assembled; one restrained activation beat
    // avoids pretending flat artwork is fully articulated 3D machinery.
    internal sealed class FightSceneOpeningRig : MonoBehaviour
    {
        private sealed class Part
        {
            internal RectTransform Rect;
            internal Image Image;
            internal Color LitColor;
        }

        private readonly List<Sprite> sprites = new();
        private Texture2D atlas, shadowTexture;
        private Image contact;
        private RectTransform stage;
        private Part body, left, right, core, outer, inner, leftWeight, rightWeight;
        private string scene;
        private bool released, revealed, first;
        private Color revealColor;
        private float visibility;
        private float revealedAt;
        private readonly List<SoundCue> sounds = new();

        private sealed class SoundCue
        {
            internal AsyncOperationHandle<AudioClip> Handle;
            internal float Progress, Volume;
            internal bool Played;
        }

        internal static bool TryCreate(RectTransform parent, string id, out FightSceneOpeningRig rig)
        {
            rig = null;
            RectTransform node = null;
            try
            {
                node = FightUI.Node(parent, "Independent ceremony components", 0, 0, 1, 1);
                FightUI.Stretch(node);
                var candidate = node.gameObject.AddComponent<FightSceneOpeningRig>();
                candidate.Load(id);
                rig = candidate;
                return true;
            }
            catch (Exception error)
            {
                if (node != null) { node.gameObject.SetActive(false); Destroy(node.gameObject); }
                Debug.LogWarning("[FightClient] layered opening unavailable (" + error.GetType().Name + "); using native opening text.");
                return false;
            }
        }

        private void Load(string id)
        {
            scene = id;
            atlas = ReadTexture(id + "-opening-parts-v2.png", true);
            if (atlas.width < 900 || atlas.height < 600)
                throw new InvalidDataException("Opening atlas is too small.");
            var pixels = atlas.GetPixels32();
            for (int index = 0; index < 6; index++) sprites.Add(CellSprite(index, pixels));
            // The transparent mechanism stays inside the actual scene. An
            // unrelated full-screen close-up would break the camera's travel.
            stage = FightUI.Node(transform, "Ceremony assembly", 0, 0, 900, 500);
            stage.anchorMin = stage.anchorMax = stage.pivot = new Vector2(.5f, .54f);
            stage.anchoredPosition = Vector2.zero;
            BuildContactShadow(pixels);
            if (id == Duel.DuelSceneCatalog.EgyptId) BuildEgypt();
            else BuildWingRig(id == Duel.DuelSceneCatalog.CityId);
            // Load once while the establishing shot is visible. Playback never
            // awaits a clip, so a retired ceremony cannot emit a late sound.
            Sound(id == Duel.DuelSceneCatalog.EgyptId ? "SE_COIN_THROW" : "SE_CARD_MOVE_01", .14f, .22f);
            Sound("SE_COIN_DECIDE", 2, .36f);
            Paint(0, 0);
        }

        private void Sound(string address, float progress, float volume)
        {
            sounds.Add(new SoundCue { Handle = Addressables.LoadAssetAsync<AudioClip>(address),
                Progress = progress, Volume = volume });
        }

        private void PlaySounds(float progress, bool outcome = false)
        {
            if (released) return;
            foreach (var sound in sounds)
            {
                if (sound.Played || (sound.Progress > 1 ? !outcome : progress < sound.Progress)) continue;
                // A slow/missing clip is skipped at its authored beat rather
                // than sounding later during a different shot or duel.
                sound.Played = true;
                if (sound.Handle.IsValid() && sound.Handle.IsDone
                    && sound.Handle.Status == AsyncOperationStatus.Succeeded && sound.Handle.Result != null)
                    Runtime.AudioManager.PlaySEClip(sound.Handle.Result, sound.Volume);
            }
        }

        private void BuildContactShadow(Color32[] input)
        {
            // Derive a soft floor contact from the actual base's alpha, rather
            // than painting another object or placing a rectangular dark card.
            const int width = 128, height = 48;
            var source = sprites[0].rect;
            var alpha = new float[width * height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    int sx = (int)(source.x + Mathf.Clamp01((x - 12f) / (width - 24f)) * (source.width - 1));
                    int sy = (int)(source.y + Mathf.Clamp01((y - 6f) / (height - 12f)) * (source.height - 1));
                    if (x >= 12 && x < width - 12 && y >= 6 && y < height - 6)
                        alpha[y * width + x] = input[sy * atlas.width + sx].a / 255f;
                }
            for (int pass = 0; pass < 3; pass++)
            {
                var horizontal = new float[alpha.Length];
                var vertical = new float[alpha.Length];
                for (int y = 0; y < height; y++)
                    for (int x = 0; x < width; x++)
                    {
                        float sum = 0;
                        for (int dx = -4; dx <= 4; dx++)
                            if (x + dx >= 0 && x + dx < width) sum += alpha[y * width + x + dx];
                        horizontal[y * width + x] = sum / 9;
                    }
                for (int y = 0; y < height; y++)
                    for (int x = 0; x < width; x++)
                    {
                        float sum = 0;
                        for (int dy = -3; dy <= 3; dy++)
                            if (y + dy >= 0 && y + dy < height) sum += horizontal[(y + dy) * width + x];
                        vertical[y * width + x] = sum / 7;
                    }
                alpha = vertical;
            }
            var pixels = new Color32[alpha.Length];
            for (int index = 0; index < pixels.Length; index++)
                pixels[index] = new Color32(0, 0, 0, (byte)(Mathf.Clamp01(alpha[index]) * 210));
            shadowTexture = new Texture2D(width, height, TextureFormat.RGBA32, false)
                { name = "Ceremony base alpha contact", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            shadowTexture.SetPixels32(pixels); shadowTexture.Apply(false, true);
            var sprite = Sprite.Create(shadowTexture, new Rect(0, 0, width, height), Vector2.one * .5f, 100);
            sprites.Add(sprite);
            var rect = FightUI.Node(stage, "Soft base contact", 0, 0, scene == Duel.DuelSceneCatalog.EgyptId ? 210 : 350, 65);
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f;
            rect.anchoredPosition = new Vector2(0, scene == Duel.DuelSceneCatalog.EgyptId ? -227 : -245);
            contact = rect.gameObject.AddComponent<Image>(); contact.sprite = sprite;
            contact.color = Color.clear; contact.raycastTarget = false;
        }

        private static Texture2D ReadTexture(string name, bool readable)
        {
            string file = Duel.DuelSceneCatalog.ContentFile("duel-scenes/" + name);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false) { name = name };
            try
            {
                if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(file), !readable))
                    throw new InvalidDataException("Opening image cannot be decoded.");
                texture.wrapMode = TextureWrapMode.Clamp; texture.filterMode = FilterMode.Bilinear;
                return texture;
            }
            catch { Destroy(texture); throw; }
        }

        private Sprite CellSprite(int index, Color32[] pixels)
        {
            int column = index % 3, row = index / 3;
            int x0 = atlas.width * column / 3, x1 = atlas.width * (column + 1) / 3;
            int y0 = atlas.height * (1 - row) / 2, y1 = atlas.height * (2 - row) / 2;
            int minX = x1, minY = y1, maxX = x0 - 1, maxY = y0 - 1, empty = 0;
            for (int y = y0; y < y1; y++)
                for (int x = x0; x < x1; x++)
                {
                    if (pixels[y * atlas.width + x].a <= 8) { empty++; continue; }
                    minX = Mathf.Min(minX, x); minY = Mathf.Min(minY, y);
                    maxX = Mathf.Max(maxX, x); maxY = Mathf.Max(maxY, y);
                }
            if (maxX < minX || maxY < minY || empty < (x1 - x0) * (y1 - y0) * .12f)
                throw new InvalidDataException("Opening component has no usable transparent margin.");
            minX = Mathf.Max(x0, minX - 2); minY = Mathf.Max(y0, minY - 2);
            maxX = Mathf.Min(x1 - 1, maxX + 2); maxY = Mathf.Min(y1 - 1, maxY + 2);
            return Sprite.Create(atlas, new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1),
                new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect);
        }

        private Part Add(int cell, string name, Transform parent, Vector2 size, Vector2? pivot = null)
        {
            var rect = FightUI.Node(parent, name, 0, 0, size.x, size.y);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = pivot ?? new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero;
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprites[cell]; image.preserveAspect = true;
            image.raycastTarget = false; image.color = Color.clear;
            return new Part { Rect = rect, Image = image };
        }

        private void BuildWingRig(bool city)
        {
            outer = Add(4, "Independent outer gimbal", stage, new Vector2(360, 360));
            inner = Add(5, "Independent optical rotor", stage, new Vector2(275, 275));
            body = Add(0, "Authorization cradle", stage, new Vector2(city ? 350 : 320, 230));
            left = Add(1, "Left hinged blade assembly", stage, new Vector2(355, 230), new Vector2(.95f, .5f));
            right = Add(2, "Right hinged blade assembly", stage, new Vector2(355, 230), new Vector2(.05f, .5f));
            core = Add(3, "Independent authorization jewel", stage, new Vector2(190, 245));
        }

        private void BuildEgypt()
        {
            outer = Add(5, "Sacred pivot halo", stage, new Vector2(225, 225));
            body = Add(0, "Upright balance support", stage, new Vector2(250, 325));
            inner = Add(1, "Articulated balance beam", stage, new Vector2(610, 190));
            left = Add(2, "Left hanging pan", inner.Rect, new Vector2(180, 225), new Vector2(.5f, 1));
            right = Add(2, "Right hanging pan", inner.Rect, new Vector2(180, 225), new Vector2(.5f, 1));
            leftWeight = Add(4, "Independent left ceremonial weight", left.Rect, new Vector2(90, 65));
            rightWeight = Add(4, "Independent right ceremonial weight", right.Rect, new Vector2(90, 65));
            core = Add(3, "Falling Millennium puzzle", stage, new Vector2(175, 200));
        }

        private static float Ease(float value)
        {
            value = Mathf.Clamp01(value); return value * value * value * (value * (value * 6 - 15) + 10);
        }
        private static float Segment(float progress, float begin, float end) => Ease((progress - begin) / (end - begin));
        private void Pose(Part part, Vector2 position, float z = 0, float x = 0, float y = 0, float scale = 1, float alpha = 1)
        {
            if (part == null) return;
            part.Rect.anchoredPosition = position;
            var rotation = Quaternion.Euler(x, y, z);
            part.Rect.localRotation = rotation;
            part.Rect.localScale = Vector3.one * scale;
            // The authored relief remains intact. A small facing response makes
            // an edge-on hinge recede, then brings its metal back into the key.
            bool egypt = scene == Duel.DuelSceneCatalog.EgyptId;
            var key = egypt ? new Vector3(-.45f, .22f, 1).normalized : new Vector3(-.3f, .15f, 1).normalized;
            float facing = Mathf.Clamp01(Vector3.Dot(rotation * Vector3.forward, key));
            bool jewel = ReferenceEquals(part, core);
            var shade = Color.Lerp(jewel ? new Color(.97f, .985f, 1)
                : egypt ? new Color(.94f, .90f, .81f) : new Color(.92f, .96f, 1), Color.white, facing);
            shade *= jewel ? .97f + .03f * facing : .87f + .13f * facing;
            shade.a = visibility * Mathf.Clamp01(alpha);
            part.LitColor = shade; part.Image.color = shade;
        }

        internal void Paint(float progress, float alpha)
        {
            if (released || stage == null) return;
            visibility = Mathf.Clamp01(alpha);
            var parent = transform.parent as RectTransform;
            float fit = parent == null ? 1 : Mathf.Min(parent.rect.width / 1280, parent.rect.height / 720);
            float activation = Ease(progress);
            stage.localScale = Vector3.one * Mathf.Max(.2f, fit) * Mathf.Lerp(.82f, .84f, activation);
            stage.anchoredPosition = new Vector2(0, Mathf.Lerp(45, 50, activation));
            if (scene == Duel.DuelSceneCatalog.EgyptId)
            {
                Pose(body, new Vector2(0, -65));
                Pose(outer, new Vector2(0, 85), x: 12, y: -6);
                Pose(inner, new Vector2(0, 76));
                Pose(left, new Vector2(-247, -54)); Pose(right, new Vector2(247, -54));
                Pose(leftWeight, new Vector2(0, -30)); Pose(rightWeight, new Vector2(0, -30));
                Pose(core, new Vector2(0, -40), x: 2, y: -5);
            }
            else
            {
                bool city = scene == Duel.DuelSceneCatalog.CityId;
                Pose(body, new Vector2(0, -130));
                Pose(left, new Vector2(city ? -65 : -58, city ? 12 : 8), city ? -13 : -8, y: -8);
                Pose(right, new Vector2(city ? 65 : 58, city ? 12 : 8), city ? 13 : 8, y: 8);
                Pose(outer, new Vector2(0, 35), x: city ? -8 : 12, y: city ? 13 : 0);
                Pose(inner, new Vector2(0, 35), x: city ? 6 : -6, y: city ? -12 : -8);
                Pose(core, new Vector2(0, 28), x: 3, y: city ? -7 : -8);
            }
            contact.color = new Color(1, 1, 1, visibility * .5f);
            if (visibility > .05f) PlaySounds(progress);
            if (revealed && core != null)
            {
                float time = Mathf.Max(0, Services.ReplayVideoExport.UnscaledClock - revealedAt);
                float lockLight = Segment(time, 0, .22f);
                var color = Color.Lerp(core.LitColor, core.LitColor * revealColor, lockLight);
                color.a = visibility; core.Image.color = color;
                float side = first ? -1 : 1;
                if (scene == Duel.DuelSceneCatalog.EgyptId)
                {
                    inner.Rect.localRotation *= Quaternion.Euler(0, 0, side * lockLight * 3);
                    left.Rect.localRotation *= Quaternion.Euler(0, 0, -side * lockLight * 3);
                    right.Rect.localRotation *= Quaternion.Euler(0, 0, -side * lockLight * 3);
                }
            }
        }

        internal void Reveal(bool first)
        {
            if (released || revealed) return;
            revealed = true; this.first = first;
            revealedAt = Services.ReplayVideoExport.UnscaledClock;
            revealColor = scene == Duel.DuelSceneCatalog.EgyptId ? new Color(1, .96f, .82f)
                : scene == Duel.DuelSceneCatalog.CityId ? new Color(.86f, .95f, 1) : new Color(.87f, .96f, 1);
            PlaySounds(1, true);
        }

        private void Release()
        {
            if (released) return;
            released = true;
            foreach (var sprite in sprites) if (sprite != null) Destroy(sprite);
            sprites.Clear();
            foreach (var sound in sounds) if (sound.Handle.IsValid()) Addressables.Release(sound.Handle);
            sounds.Clear();
            if (atlas != null) Destroy(atlas);
            if (shadowTexture != null) Destroy(shadowTexture);
            atlas = null;
        }
        private void OnDisable() => Release();
        private void OnDestroy() => Release();
    }
}
