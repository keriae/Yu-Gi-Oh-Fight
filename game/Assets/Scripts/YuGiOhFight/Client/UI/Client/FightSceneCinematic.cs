using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace YuGiOhFight.Client.UI.Client
{
    // The order is read from Start/ReloadField. This ceremony never randomizes
    // it, writes a preference, or submits a protocol response.
    internal static class FightSceneCinematic
    {
        private const float Establish = .45f, Activation = .85f, Hold = .92f;
        private const float FadeOut = .16f, CutHold = .05f, FadeIn = .28f;
        private static object owner;
        private static Runtime.CameraManager cameras;
        private static Camera main, overlay;
        private static Vector3 overviewPosition;
        private static float overviewFov;
        private static Quaternion overviewRotation;
        private static CanvasGroup hud;
        private static float hudAlpha;
        private static bool hudBlocks, hudInteractable;
        private static RectTransform root;
        private static int generation;
        private static Vector3 cameraPosition;
        private static Quaternion cameraRotation;
        private static float cameraFov, hudTargetAlpha;
        private static bool gameplayHidden;
        private static float gameplayReveal;
        private static readonly Dictionary<Renderer, bool> gameplayRenderers = new();
        private static float Smooth(float value)
        { value = Mathf.Clamp01(value); return value * value * value * (value * (value * 6 - 15) + 10); }
        internal static bool IsShowing => owner != null;
        internal static float GameplayRevealProgress => !IsShowing ? 1 : gameplayReveal;
        internal static bool GameplayVisible => GameplayRevealProgress > .0001f;

        internal static void Prepare(object entryOwner, string id)
        {
            Cancel(owner);
            if (!Duel.DuelSceneCatalog.IsIntegratedTheme(id)) return;
            cameras = Runtime.Program.instance?.camera_;
            if (cameras == null || cameras.cameraMain == null) return;
            owner = entryOwner;
            ++generation;
            gameplayHidden = true; gameplayReveal = 0;
            HideGameplay();
            main = cameras.cameraMain; overlay = cameras.cameraDuelOverlay3D;
            if (cameras.GetComponent<FightSceneCinematicGuard>() == null) cameras.gameObject.AddComponent<FightSceneCinematicGuard>();
            main.transform.DOKill();
            if (overlay != null) overlay.transform.DOKill();
            overviewPosition = id == Duel.DuelSceneCatalog.AcademyId ? new Vector3(0, 46, -112)
                : id == Duel.DuelSceneCatalog.EgyptId ? new Vector3(0, 48, -112) : new Vector3(0, 68, -140);
            var target = id == Duel.DuelSceneCatalog.AcademyId ? new Vector3(0, 6, 12)
                : id == Duel.DuelSceneCatalog.EgyptId ? new Vector3(0, 8, 20) : new Vector3(0, 30, 20);
            overviewFov = id == Duel.DuelSceneCatalog.CityId ? 56 : 46;
            overviewRotation = Quaternion.LookRotation(target - overviewPosition, Vector3.up);
            SetCamera(overviewPosition, overviewRotation, overviewFov);
            Runtime.Program.instance.ocgcore?.DuelBGManager?.SetSceneOpeningProgress(0);
            hud = Runtime.Program.instance.ocgcore?.GetUI<ServantUI.OcgCoreUI>()?.CG;
            if (hud != null)
            {
                // Entry may still be fading in. A completed active duel HUD
                // always settles at its native visible alpha.
                hudAlpha = 1; hudBlocks = true; hudInteractable = true; hudTargetAlpha = 0;
                hud.alpha = 0; hud.blocksRaycasts = false; hud.interactable = false;
            }
        }

        internal static async UniTask PlayAsync(object entryOwner, string id, bool first, Func<bool> stillCurrent)
        {
            if (stillCurrent != null && !stillCurrent()) { Cancel(entryOwner); return; }
            if (!ReferenceEquals(owner, entryOwner)) Prepare(entryOwner, id);
            if (owner == null || Runtime.Program.instance?.ui_?.popup == null) { Cancel(entryOwner); return; }
            var entryCamera = cameras;
            int entryGeneration = generation;
            try
            {
                // A dedicated overlay keeps the ceremony above the native card
                // overlay camera, including scripted lessons with a prebuilt hand.
                var canvasRoot = new GameObject("Scene opening ceremony", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                root = canvasRoot.GetComponent<RectTransform>();
                root.SetParent(Runtime.Program.instance.transform, false);
                var canvas = canvasRoot.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 32000;
                var scaler = canvasRoot.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = .5f;
                var hit = root.gameObject.AddComponent<Image>();
                hit.color = Color.clear; hit.raycastTarget = true;
                var group = root.gameObject.AddComponent<CanvasGroup>();
                group.blocksRaycasts = true; group.interactable = false;
                FightSceneOpeningRig.TryCreate(root, id, out var rig);
                var top = Plate(root, "Upper letterbox");
                top.anchorMin = new Vector2(0, 1); top.anchorMax = Vector2.one;
                top.offsetMin = new Vector2(0, -28); top.offsetMax = Vector2.zero;
                var bottom = Plate(root, "Lower letterbox");
                bottom.anchorMin = Vector2.zero; bottom.anchorMax = new Vector2(1, 0);
                bottom.offsetMin = Vector2.zero; bottom.offsetMax = new Vector2(0, 28);
                var result = FightSceneOpeningOrder.Create(root, id);
                // A brief dip to black hides the switch between authored views.
                // The field arrives at its native scale and camera, avoiding
                // long camera travel through unrelated 2D perspective layers.
                var coverRect = FightUI.Node(root, "Battle transition", 0, 0, 1, 1);
                FightUI.Stretch(coverRect);
                var cover = coverRect.gameObject.AddComponent<Image>();
                cover.raycastTarget = false; cover.color = Color.clear;
                float reveal = Establish + Activation;
                float cut = reveal + Hold;
                float change = cut + FadeOut;
                float uncover = change + CutHold;
                float duration = uncover + FadeIn;
                bool revealed = false, changed = false;
                float started = Time.unscaledTime;
                while (generation == entryGeneration && ReferenceEquals(owner, entryOwner) && cameras == entryCamera && Time.unscaledTime - started < duration)
                {
                    if (stillCurrent != null && !stillCurrent()) break;
                    float elapsed = Time.unscaledTime - started;
                    float activation = Mathf.Clamp01((elapsed - Establish) / Activation);
                    float intro = Smooth((elapsed - Establish + .15f) / .36f);
                    float transition = Smooth((elapsed - cut) / FadeOut);
                    float arrival = Smooth((elapsed - uncover) / FadeIn);
                    if (!revealed && elapsed >= reveal)
                    {
                        revealed = true; rig?.Reveal(first); result.Reveal(first);
                    }
                    rig?.Paint(activation, intro);
                    result.Paint(Mathf.Max(0, elapsed - reveal), 1);
                    if (!changed && elapsed >= change)
                    {
                        changed = true;
                        Runtime.Program.instance.ocgcore?.DuelBGManager?.SetSceneOpeningProgress(1);
                        SetCamera(Runtime.CameraManager.mainCameraDefaultPosition,
                            Quaternion.Euler(Runtime.CameraManager.mainCameraDefaultAngle), Runtime.CameraManager.BattleFieldOfView());
                        RestoreGameplay();
                    }
                    if (!changed)
                    {
                        // A shallow push preserves the establishing shot's
                        // perspective; all gameplay remains hidden until cut.
                        float push = Smooth(elapsed / cut);
                        SetCamera(overviewPosition + overviewRotation * Vector3.forward * (push * 2.5f),
                            overviewRotation, overviewFov - push * .75f);
                    }
                    cover.color = new Color(.004f, .007f, .011f, elapsed < uncover ? transition : 1 - arrival);
                    top.gameObject.SetActive(!changed); bottom.gameObject.SetActive(!changed);
                    hudTargetAlpha = arrival * hudAlpha;
                    if (hud != null) hud.alpha = hudTargetAlpha;
                    if (changed)
                    {
                        rig?.gameObject.SetActive(false);
                        result.gameObject.SetActive(false);
                    }
                    await UniTask.Yield();
                }
            }
            catch (Exception error)
            {
                Debug.LogWarning("[FightClient] scene opening unavailable (" + error.GetType().Name + "); continuing duel entry.");
            }
            finally
            {
                // A retired run cannot restore the cameras/HUD of a new duel.
                if (generation == entryGeneration && ReferenceEquals(owner, entryOwner) && cameras == entryCamera) Cancel(entryOwner);
            }
        }

        private static RectTransform Plate(Transform parent, string name)
        {
            var rect = FightUI.Node(parent, name, 0, 0, 0, 70);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(.004f, .009f, .018f, .92f); image.raycastTarget = false;
            return rect;
        }

        private static void SetCamera(Vector3 position, Quaternion rotation, float fov)
        {
            cameraPosition = position; cameraRotation = rotation; cameraFov = fov;
            ApplyCamera();
        }

        private static void ApplyCamera()
        {
            var position = cameraPosition; var rotation = cameraRotation; var fov = cameraFov;
            if (main != null) { main.transform.SetLocalPositionAndRotation(position, rotation); main.fieldOfView = fov; }
            if (overlay != null && !Runtime.CameraManager.overlaySticking)
            { overlay.transform.SetLocalPositionAndRotation(position, rotation); overlay.fieldOfView = fov; }
        }

        internal static void GuardFrame()
        {
            if (owner == null) return;
            ApplyCamera();
            if (gameplayHidden) HideGameplay();
            if (hud != null) { hud.alpha = hudTargetAlpha; hud.blocksRaycasts = false; hud.interactable = false; }
        }

        internal static void Cancel(object entryOwner)
        {
            if (owner == null || !ReferenceEquals(owner, entryOwner)) return;
            RestoreGameplay();
            SetCamera(Runtime.CameraManager.mainCameraDefaultPosition, Quaternion.Euler(Runtime.CameraManager.mainCameraDefaultAngle), 30);
            if (Runtime.Program.instance?.camera_ == cameras) Runtime.CameraManager.ChangeCameraFOV();
            Runtime.Program.instance?.ocgcore?.DuelBGManager?.SetSceneOpeningProgress(1);
            if (hud != null) { hud.alpha = hudAlpha; hud.blocksRaycasts = hudBlocks; hud.interactable = hudInteractable; }
            if (root != null) { root.gameObject.SetActive(false); UnityEngine.Object.Destroy(root.gameObject); }
            ++generation;
            owner = null; cameras = null; main = overlay = null; hud = null; root = null;
        }

        private static void HideGameplay()
        {
            var bg = Runtime.Program.instance?.ocgcore?.DuelBGManager;
            if (bg != null)
            {
                HideRenderers(bg.myDeck?.gameObject); HideRenderers(bg.myExtra?.gameObject);
                HideRenderers(bg.opDeck?.gameObject); HideRenderers(bg.opExtra?.gameObject);
            }
            foreach (var card in Controllers.OcgCore.cards)
                if (card != null) HideRenderers(card.model);
        }

        private static void HideRenderers(GameObject visual)
        {
            if (visual == null) return;
            foreach (var renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                if (!gameplayRenderers.ContainsKey(renderer)) gameplayRenderers.Add(renderer, renderer.enabled);
                renderer.enabled = false;
            }
        }

        private static void RestoreGameplay()
        {
            foreach (var entry in gameplayRenderers)
                if (entry.Key != null) entry.Key.enabled = entry.Value;
            gameplayRenderers.Clear(); gameplayHidden = false; gameplayReveal = 1;
        }
    }

    internal sealed class FightSceneCinematicGuard : MonoBehaviour
    {
        private void LateUpdate() => FightSceneCinematic.GuardFrame();
    }

}
