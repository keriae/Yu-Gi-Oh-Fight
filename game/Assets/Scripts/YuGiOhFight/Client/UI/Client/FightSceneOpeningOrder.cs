using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace YuGiOhFight.Client.UI.Client
{
    // Two AI-rendered reliefs share one atlas. Loading both sides never chooses
    // an outcome; only the authoritative lock beat assigns the displayed seal.
    internal sealed class FightSceneOpeningOrder : MonoBehaviour
    {
        private Texture2D texture;
        private readonly Sprite[] outcomes = new Sprite[2];
        private RectTransform seal;
        private Image image;
        private TextMeshProUGUI fallback;
        private string scene;
        private bool revealed, released;

        internal static FightSceneOpeningOrder Create(RectTransform parent, string id)
        {
            var node = FightUI.Node(parent, "Scene order relief", 0, 0, 1, 1);
            FightUI.Stretch(node);
            var result = node.gameObject.AddComponent<FightSceneOpeningOrder>();
            result.scene = id;
            result.seal = FightUI.Node(node, "Unassigned order seal", 0, 0, 520, 210);
            result.seal.anchorMin = result.seal.anchorMax = result.seal.pivot = Vector2.one * .5f;
            result.seal.anchoredPosition = new Vector2(0, -182);
            result.image = result.seal.gameObject.AddComponent<Image>();
            result.image.preserveAspect = true; result.image.raycastTarget = false; result.image.color = Color.clear;
            try { result.Load(); }
            catch (Exception error)
            {
                result.ReleaseArt();
                Debug.LogWarning("[FightClient] scene order relief unavailable (" + error.GetType().Name + "); using native order text.");
            }
            return result;
        }

        private void Load()
        {
            texture = new Texture2D(2, 2, TextureFormat.RGBA32, false) { name = scene + " authoritative order reliefs" };
            string file = Duel.DuelSceneCatalog.ContentFile("duel-scenes/" + scene + "-opening-order-v3.png");
            if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(file), false) || texture.width < 1000 || texture.height < 300)
                throw new InvalidDataException("Order relief atlas is invalid.");
            texture.wrapMode = TextureWrapMode.Clamp; texture.filterMode = FilterMode.Bilinear;
            var pixels = texture.GetPixels32();
            for (int side = 0; side < 2; side++)
            {
                int left = texture.width * side / 2, right = texture.width * (side + 1) / 2;
                int minX = right, maxX = left - 1, minY = texture.height, maxY = -1, empty = 0;
                for (int y = 0; y < texture.height; y++)
                    for (int x = left; x < right; x++)
                    {
                        if (pixels[y * texture.width + x].a <= 8) { empty++; continue; }
                        minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x);
                        minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y);
                    }
                if (maxX < minX || maxY < minY || empty < (right - left) * texture.height * .1f)
                    throw new InvalidDataException("Order relief requires an isolated transparent margin.");
                minX = Mathf.Max(left, minX - 2); maxX = Mathf.Min(right - 1, maxX + 2);
                minY = Mathf.Max(0, minY - 2); maxY = Mathf.Min(texture.height - 1, maxY + 2);
                outcomes[side] = Sprite.Create(texture, new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1),
                    Vector2.one * .5f, 100, 0, SpriteMeshType.FullRect);
            }
            texture.Apply(false, true);
        }

        internal void Reveal(bool first)
        {
            if (revealed || released) return;
            revealed = true;
            image.sprite = outcomes[first ? 0 : 1];
            if (image.sprite != null) return;
            // Only a missing production asset uses text. The order is never
            // made invisible in the hierarchy while the mechanism is moving.
            fallback = FightUI.Text(seal, first ? "FIRST" : "SECOND", 0, 0, 460, 80, 40,
                scene == Duel.DuelSceneCatalog.EgyptId ? new Color(.96f, .79f, .46f) : new Color(.73f, .88f, .95f),
                TextAlignmentOptions.Center);
            FightUI.Stretch(fallback.rectTransform);
            fallback.raycastTarget = false; fallback.outlineWidth = .07f;
        }

        internal void Paint(float revealTime, float alpha)
        {
            if (released || seal == null) return;
            var parent = transform.parent as RectTransform;
            float fit = parent == null ? 1 : Mathf.Min(parent.rect.width / 1280, parent.rect.height / 720);
            float arrive = Smooth(revealTime / .22f);
            seal.localScale = Vector3.one * Mathf.Max(.2f, fit);
            seal.anchoredPosition = new Vector2(0, Mathf.Lerp(-190, -182, arrive));
            seal.localRotation = Quaternion.identity;
            float opacity = revealed ? Mathf.Clamp01(alpha) * arrive : 0;
            image.color = new Color(1, 1, 1, opacity);
            if (fallback != null) { var c = fallback.color; c.a = opacity; fallback.color = c; }
        }

        private static float Smooth(float value)
        {
            value = Mathf.Clamp01(value); return value * value * value * (value * (value * 6 - 15) + 10);
        }

        private void ReleaseArt()
        {
            for (int index = 0; index < outcomes.Length; index++)
            {
                if (outcomes[index] != null) Destroy(outcomes[index]);
                outcomes[index] = null;
            }
            if (texture != null) Destroy(texture);
            texture = null;
        }

        private void Release()
        {
            if (released) return;
            released = true; ReleaseArt();
        }
        private void OnDisable() => Release();
        private void OnDestroy() => Release();
    }
}
