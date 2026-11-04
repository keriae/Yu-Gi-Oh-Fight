using MDPro3;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MDPro3.UI
{
    public class UIHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public const string LABEL_LEFT = "Left";
        public const string LABEL_RIGHT = "Right";
        public const string LABEL_REMOVEDECK = "RemoveDeck";
        public const string LABEL_ADDBOOKMARK = "AddBookmark";
        public const string LABEL_CANNOTADDBOOKMARK = "CanNotAddBookmark";
        public const string LABEL_MAINDECK = "MainDeck";
        public const string LABEL_EXTRADECK = "ExtraDeck";
        public const string LABEL_SIDEDECK = "SideDeck";

        public static string HoveringLabel;

        [SerializeField] private float alpha = 0.2f;
        [SerializeField] private string label;
        [SerializeField] private Color hoverColor = Color.white;

        private bool _hover;
        public bool Hover => _hover;
        private bool useRuntimeHoverColor;
        private bool usePersistentOverlay;
        private Color runtimeHoverColor = Color.white;
        private Color persistentOverlayColor = Color.white;
        private float persistentOverlayAlpha = 1f;

        private Image m_Image;
        private Image Image => m_Image = m_Image != null 
            ? m_Image : GetComponent<Image>();

        private void OnDisable()
        {
            Hide();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hover = true;
            if(UserInput.Draging && Image != null)
            {
                var color = useRuntimeHoverColor ? runtimeHoverColor : hoverColor;
                color.a *= alpha;
                Image.color = color;
                HoveringLabel = label;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hover = false;
            if (Image != null)
            {
                if(HoveringLabel == label)
                    HoveringLabel = string.Empty;
                RefreshHoverColor();
            }
        }

        public void Hide()
        {
            if (Image != null)
                Image.color = Color.clear;
        }

        public void SetRuntimeHoverColor(Color color)
        {
            useRuntimeHoverColor = true;
            runtimeHoverColor = color;
            RefreshHoverColor();
        }

        public void ClearRuntimeHoverColor()
        {
            useRuntimeHoverColor = false;
            runtimeHoverColor = Color.white;
            RefreshHoverColor();
        }

        public void SetPersistentOverlay(Color color, float alphaMultiplier)
        {
            usePersistentOverlay = true;
            persistentOverlayColor = color;
            persistentOverlayAlpha = alphaMultiplier;
            RefreshHoverColor();
        }

        public void ClearPersistentOverlay()
        {
            usePersistentOverlay = false;
            persistentOverlayColor = Color.white;
            persistentOverlayAlpha = 1f;
            RefreshHoverColor();
        }

        private void RefreshHoverColor()
        {
            if (Image == null)
                return;
            if (_hover && UserInput.Draging)
            {
                var color = useRuntimeHoverColor ? runtimeHoverColor : hoverColor;
                color.a *= alpha;
                Image.color = color;
                HoveringLabel = label;
            }
            else if (usePersistentOverlay)
            {
                var color = persistentOverlayColor;
                color.a *= persistentOverlayAlpha;
                Image.color = color;
            }
            else
                Image.color = Color.clear;
        }
    }
}
