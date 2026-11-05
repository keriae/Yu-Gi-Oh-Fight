using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MDPro3.UI.Client
{
    // The medallion keeps a circular hit area, including with transparent artwork.
    internal sealed class FightDuelLaunchGraphic : MaskableGraphic, ICanvasRaycastFilter
    {
        internal bool HasArtwork;
        internal float Glow;
        public bool IsRaycastLocationValid(Vector2 point, Camera camera)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, point, camera, out var local)) return false;
            return (local - rectTransform.rect.center).sqrMagnitude <= Mathf.Pow(Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * .48f, 2);
        }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            var rect = rectTransform.rect; float radius = Mathf.Min(rect.width, rect.height) * .5f;
            Ring(mesh, rect.center, radius * .96f, radius * .925f, new Color(.22f, .79f, 1, .08f + Glow * .42f));
            if (HasArtwork) return;
            Ring(mesh, rect.center, radius * .89f, radius * .84f, FightUI.GoldBorder);
            Ring(mesh, rect.center, radius * .80f, radius * .76f, FightUI.Gold);
            Ring(mesh, rect.center, radius * .74f, 0, FightUI.ActionFill);
            Ring(mesh, rect.center, radius * .73f, radius * .71f, FightUI.Cyan);
        }
        private static void Ring(VertexHelper mesh, Vector2 center, float outer, float inner, Color tint)
        {
            const int segments = 128;
            for (int i = 0; i < segments; i++)
            {
                float a = i * (float)Math.PI * 2 / segments, b = (i + 1) * (float)Math.PI * 2 / segments;
                int start = mesh.currentVertCount;
                mesh.AddVert(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * outer, tint, Vector2.zero);
                mesh.AddVert(center + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * outer, tint, Vector2.zero);
                mesh.AddVert(center + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * inner, tint, Vector2.zero);
                mesh.AddVert(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * inner, tint, Vector2.zero);
                mesh.AddTriangle(start, start + 1, start + 2); mesh.AddTriangle(start + 2, start + 3, start);
            }
        }
    }

    internal sealed class FightDuelLaunchFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        internal Button Button;
        internal RectTransform Visual;
        internal FightDuelLaunchGraphic Halo;
        internal RawImage Artwork;
        internal TextMeshProUGUI Title;
        private bool hover, selected, pressed;
        private float focus;
        public void OnPointerEnter(PointerEventData e) => hover = true;
        public void OnPointerExit(PointerEventData e) { hover = false; pressed = false; }
        public void OnPointerDown(PointerEventData e) { if (e.button == PointerEventData.InputButton.Left) pressed = true; }
        public void OnPointerUp(PointerEventData e) => pressed = false;
        public void OnSelect(BaseEventData e) => selected = true;
        public void OnDeselect(BaseEventData e) => selected = false;
        private void Update()
        {
            if (Button == null || Visual == null) return;
            bool ready = Button.IsInteractable();
            focus = Mathf.Lerp(focus, ready && (hover || selected) ? 1 : 0, Mathf.Clamp01(Time.unscaledDeltaTime * 12));
            Visual.localScale = Vector3.one * (pressed && ready ? .975f : 1 + focus * .035f);
            Halo.Glow = ready ? .12f + focus * .7f : 0; Halo.SetVerticesDirty();
            if (Artwork != null) Artwork.color = ready ? Color.Lerp(Color.white, new Color(1.13f, 1.16f, 1.2f), focus)
                : new Color(.44f, .50f, .56f, .88f);
            if (Title != null) Title.color = ready ? Color.white : new Color(.67f, .70f, .74f, .8f);
        }
    }

    internal partial class FightClient
    {
        private Button DrawDuelLaunch(Transform parent, float x, float y, float size, Action launch, bool ready, string actionDescription, string issue)
        {
            var node = FightUI.Node(parent, "VS · 进入对战", x, y, size, size);
            var hit = node.gameObject.AddComponent<FightDuelLaunchGraphic>(); hit.color = Color.white;
            var visual = FightUI.Node(node, "Duel medallion", 0, 0, size, size);
            visual.pivot = new Vector2(.5f, .5f); visual.anchoredPosition = new Vector2(size * .5f, -size * .5f);
            var artwork = FightUI.Asset(visual, "duel-vs-medallion.png", 0, 0, size, size, false);
            hit.HasArtwork = artwork.texture != null;
            var title = FightUI.HeroTitle(visual, "VS", size * .2f, size * .28f, size * .6f, size * .4f, size * .23f);
            title.alignment = TextAlignmentOptions.Center; title.characterSpacing = 4; title.raycastTarget = false;
            var button = node.gameObject.AddComponent<Button>(); button.targetGraphic = hit; button.transition = Selectable.Transition.None;
            button.interactable = ready; button.onClick.AddListener(() => launch?.Invoke());
            var feedback = node.gameObject.AddComponent<FightDuelLaunchFeedback>();
            feedback.Button = button; feedback.Visual = visual; feedback.Halo = hit; feedback.Artwork = artwork;
            feedback.Title = title;
            FightUI.FocusKey(button, "battle:begin"); FightUI.Hint(button.gameObject, ready ? actionDescription : issue);
            return button;
        }
    }
}
