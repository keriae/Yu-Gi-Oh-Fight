/// Credit zero3growlithe
/// sourced from: http://forum.unity3d.com/threads/scripts-useful-4-6-scripts-collection.264161/page-2#post-2011648

///Edit by MDPro3

using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace MDPro3.UI
{
    [RequireComponent(typeof(ScrollRect))]
    [AddComponentMenu("UI/Extensions/UIScrollToSelection")]
    public class UIScrollToSelection : MonoBehaviour
    {

        //*** ATTRIBUTES ***//
        [SerializeField]
        private ScrollType scrollDirection = ScrollType.VERTICAL;

        public float topPadding = 0f;
        public float bottomPadding = 0f;
        public float leftPadding = 0f;
        public float rightPadding = 0f;

        //*** PROPERTIES ***//
        // REFERENCES
        protected RectTransform ScrollContentRect
        {
            get { return TargetScrollRect != null ? TargetScrollRect.content : null; }
        }

        // SETTINGS
        protected ScrollType ScrollDirection
        {
            get { return scrollDirection; }
        }

        // CACHED REFERENCES
        protected RectTransform ScrollWindow { get; set; }
        protected ScrollRect TargetScrollRect { get; set; }

        // SCROLLING
        protected EventSystem CurrentEventSystem
        {
            get { return EventSystem.current; }
        }
        protected GameObject LastCheckedGameObject { get; set; }
        protected GameObject CurrentSelectedGameObject
            => EventSystem.current.currentSelectedGameObject;
        protected RectTransform CurrentTargetRectTransform { get; set; }

        //*** METHODS - PROTECTED ***//
        protected virtual void Awake()
        {
            TargetScrollRect = GetComponent<ScrollRect>();
            ScrollWindow = TargetScrollRect.GetComponent<RectTransform>();
        }

        protected virtual void Update()
        {
            UpdateReferences();
            ScrollRectToLevelSelection();
        }

        protected virtual void OnDisable()
        {
            KillScrollTweens();
        }

        protected virtual void OnDestroy()
        {
            KillScrollTweens();
        }

        //*** METHODS - PRIVATE ***//
        private void UpdateReferences()
        {
            // update current selected rect transform
            if (CurrentSelectedGameObject != LastCheckedGameObject)
            {
                CurrentTargetRectTransform = (CurrentSelectedGameObject != null) ?
                    CurrentSelectedGameObject.GetComponent<RectTransform>() :
                    null;

                if (CurrentTargetRectTransform != null
                    && !CurrentTargetRectTransform.IsChildOf(ScrollContentRect))
                    CurrentTargetRectTransform = null;
            }
            else
                CurrentTargetRectTransform = null;

            LastCheckedGameObject = CurrentSelectedGameObject;
        }

        private void ScrollRectToLevelSelection()
        {
            // check main references
            bool referencesAreIncorrect = (TargetScrollRect == null || ScrollContentRect == null || ScrollWindow == null);

            if (referencesAreIncorrect == true || Cursor.lockState == CursorLockMode.None)
            {
                return;
            }

            RectTransform selection = CurrentTargetRectTransform;

            // check if scrolling is possible
            if (selection == null)
            {
                return;
            }

            // depending on selected scroll direction move the scroll rect to selection
            switch (ScrollDirection)
            {
                case ScrollType.VERTICAL:
                    UpdateVerticalScrollPosition(selection);
                    break;
                case ScrollType.HORIZONTAL:
                    UpdateHorizontalScrollPosition(selection);
                    break;
                case ScrollType.BOTH:
                    UpdateVerticalScrollPosition(selection);
                    UpdateHorizontalScrollPosition(selection);
                    break;
            }
        }

        public void VerticalScrollTo(RectTransform selection)
        {
            UpdateVerticalScrollPosition(selection, 0f);
        }

        private Tweener verticalTweener;
        private void UpdateVerticalScrollPosition(RectTransform selection, float duration = 0.1f)
        {
            float elementHeight = selection.rect.height;
            float selectionPosition = -selection.anchoredPosition.y - (elementHeight * (1f - selection.pivot.y));

            var parent = selection.parent as RectTransform;
            while (parent != ScrollContentRect)
            {
                selectionPosition += -parent.anchoredPosition.y
                    - (parent.rect.height * (1f - parent.pivot.y));
                parent = parent.parent as RectTransform;
            }

            float maskHeight = ScrollWindow.rect.height;
            float listAnchorPosition = ScrollContentRect.anchoredPosition.y;

            float offlimitsValue = GetVerticalScrollOffset(selectionPosition, listAnchorPosition, elementHeight, maskHeight);

            if (verticalTweener != null && verticalTweener.IsActive())
                verticalTweener.Kill();
            verticalTweener = ScrollContentRect.DOAnchorPosY(ScrollContentRect.anchoredPosition.y - offlimitsValue, duration);
        }

        private Tweener horizontalTweener;
        private void UpdateHorizontalScrollPosition(RectTransform selection, float duration = 0.1f)
        {
            float elementWidth = selection.rect.width;
            float selectionPosition = selection.anchoredPosition.x - (elementWidth * selection.pivot.x);

            var parent = selection.parent as RectTransform;
            while (parent != ScrollContentRect)
            {
                selectionPosition += parent.anchoredPosition.x
                    - (parent.rect.width * parent.pivot.x);
                parent = parent.parent as RectTransform;
            }

            float maskWidth = ScrollWindow.rect.width;
            float listAnchorPosition = -ScrollContentRect.anchoredPosition.x;
            float offlimitsValue = GetHorizontalScrollOffset(selectionPosition, listAnchorPosition, elementWidth, maskWidth);

            if (horizontalTweener != null && horizontalTweener.IsActive())
                horizontalTweener.Kill();
            horizontalTweener = ScrollContentRect.DOAnchorPosX(ScrollContentRect.anchoredPosition.x + offlimitsValue, duration);
        }

        private void KillScrollTweens()
        {
            DOTweenLifecycle.Kill(verticalTweener);
            verticalTweener = null;
            DOTweenLifecycle.Kill(horizontalTweener);
            horizontalTweener = null;

            if (ScrollContentRect != null)
                DOTweenLifecycle.KillTarget(ScrollContentRect);
        }

        private float GetHorizontalScrollOffset(float position, float contentAnchorPosition, float targetLength, float maskLength)
        {
            if (position < contentAnchorPosition + leftPadding)
            {
                return contentAnchorPosition + leftPadding - position;
            }
            else if (position + targetLength > contentAnchorPosition + maskLength - rightPadding)
            {
                return (contentAnchorPosition + maskLength - rightPadding)
                    - (position + targetLength);
            }
            return 0;
        }

        private float GetVerticalScrollOffset(float position, float contentAnchorPosition, float targetLength, float maskLength)
        {
            if (position < contentAnchorPosition + topPadding)
            {
                return contentAnchorPosition + topPadding - position;
            }
            else if (position + targetLength > contentAnchorPosition + maskLength - bottomPadding)
            {
                return (contentAnchorPosition + maskLength - bottomPadding)
                    - (position + targetLength);
            }
            return 0f;
        }

        //*** ENUMS ***//
        public enum ScrollType
        {
            VERTICAL,
            HORIZONTAL,
            BOTH
        }
    }
}