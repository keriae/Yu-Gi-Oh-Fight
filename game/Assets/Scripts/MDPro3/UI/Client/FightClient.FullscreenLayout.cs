using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace MDPro3.UI.Client
{
    internal partial class FightClient
    {
        private RectTransform clientBackground, clientHeader, clientFooter, clientModalBox, retainedPageLayout;
        private Vector2 clientModalSize, retainedPageSize;
        private bool fullscreenReflowPending;
        private float windowedDesignWidth = 1280, windowedDesignHeight = 720;

        private void QueueFullscreenReflow(bool entering)
        {
            if (entering) { windowedDesignWidth = W; windowedDesignHeight = H; }
            fullscreenReflowPending = true;
        }

        private void ApplyFullscreenReflow()
        {
            fullscreenReflowPending = false;
            if (Body == null) return;
            bool preservePage = page == "editor" || page == "settings" || page == "collection"
                || page == "animations" || page == "appearance" || ViewportInteractionActive();
            if (!preservePage) { Rebuild(); return; }
            float beforeWidth = W, beforeHeight = H;
            designWidth = UsesFullscreenLayout ? PreferredViewportWidth : windowedDesignWidth;
            designHeight = UsesFullscreenLayout ? PreferredViewportHeight : windowedDesignHeight;
            FitClientViewport();
            if (Mathf.Approximately(beforeWidth, W) && Mathf.Approximately(beforeHeight, H)) return;
            FightUI.Place(Body, Gap, BodyTop, BodyWidth, BodyHeight);
            if (clientBackground != null) FightUI.Place(clientBackground, 0, 0, W, H);
            if (page == "editor") ReflowEditorViewport(beforeWidth, beforeHeight);
            else if (page == "settings") ReflowSettingsViewport(beforeWidth, beforeHeight);
            else if (page == "collection" || page == "animations" || page == "appearance") ReflowLibraryViewport(beforeWidth, beforeHeight);
            else ReflowRetainedPage(beforeWidth, beforeHeight);
            string dialogOriginKey = dialogOriginFocus != null ? dialogOriginFocus.GetComponent<FightFocusKey>()?.Key : null;
            if (clientHeader != null) { RememberFocusForRefresh(clientHeader); clientHeader.gameObject.SetActive(false); Destroy(clientHeader.gameObject); }
            if (clientFooter != null) { RememberFocusForRefresh(clientFooter); clientFooter.gameObject.SetActive(false); Destroy(clientFooter.gameObject); }
            DrawHeader(); DrawFooter();
            if (!string.IsNullOrEmpty(dialogOriginKey) && (dialogOriginFocus == null || !dialogOriginFocus.activeInHierarchy))
            {
                var origin = root.GetComponentsInChildren<FightFocusKey>().FirstOrDefault(key => key.Key == dialogOriginKey
                    && key.GetComponent<Selectable>() != null && key.GetComponent<Selectable>().IsInteractable());
                if (origin != null) dialogOriginFocus = origin.gameObject;
            }
            if (modal != null)
            {
                FightUI.Place(modal, 0, 0, W, H);
                if (clientModalBox != null)
                {
                    float fit = Mathf.Min(1, Mathf.Min((W - 48) / clientModalSize.x, (H - 48) / clientModalSize.y));
                    FightUI.Place(clientModalBox, (W - clientModalSize.x * fit) / 2, (H - clientModalSize.y * fit) / 2,
                        clientModalSize.x, clientModalSize.y);
                    clientModalBox.localScale = new Vector3(fit, fit, 1);
                }
                modal.SetAsLastSibling();
            }
            Canvas.ForceUpdateCanvases();
#if FIGHT_UI_PROOF
            Debug.Log("[FightClientFullscreen] nativeOrEngine=" + UsesFullscreenLayout + " engine=" + Screen.fullScreen
                + " display=" + Screen.width + "x" + Screen.height + " layout=" + W + "x" + H + " page=" + page);
#endif
        }

        private void ReflowRetainedPage(float previousWidth, float previousHeight)
        {
            // A focused preparation form or an open confirmation stays intact.
            // Its reading column has a bounded scale while the surrounding shell
            // adopts fullscreen density; ordinary navigation renders the full layout.
            if (retainedPageLayout == null)
            {
                var children = Enumerable.Range(0, Body.childCount).Select(index => Body.GetChild(index)).ToArray();
                float previousGap = previousWidth < 1600 ? 16 : 24;
                float previousHeader = previousWidth < 1600 ? 88 : previousWidth >= 2400 ? 104 : 120;
                float previousFooter = previousWidth < 1600 ? 32 : previousWidth >= 2400 ? 36 : 48;
                float previousTop = page == "home" ? previousHeader + (previousWidth < 1600 ? 11 : 16) : previousHeader + previousGap;
                retainedPageSize = new Vector2(previousWidth - previousGap * 2, previousHeight - previousTop - previousFooter - previousGap);
                retainedPageLayout = FightUI.Node(Body, "Retained preparation layout", 0, 0, retainedPageSize.x, retainedPageSize.y);
                foreach (var child in children) child.SetParent(retainedPageLayout, false);
            }
            float fit = Mathf.Min(LargeViewport ? 1.25f : 1, Mathf.Min(BodyWidth / retainedPageSize.x, BodyHeight / retainedPageSize.y));
            FightUI.Place(retainedPageLayout, (BodyWidth - retainedPageSize.x * fit) / 2, (BodyHeight - retainedPageSize.y * fit) / 2,
                retainedPageSize.x, retainedPageSize.y);
            retainedPageLayout.localScale = new Vector3(fit, fit, 1);
        }
    }
}
