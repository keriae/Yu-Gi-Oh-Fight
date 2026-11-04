using MDPro3.Duel.YGOSharp;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YgomSystem.ElementSystem;

namespace MDPro3.UI
{
    [RequireComponent(typeof(ElementObjectManager))]
    public class UIWidget : MonoBehaviour
    {

        #region Elements

        private ElementObjectManager manager;
        protected ElementObjectManager Manager =>
            manager = manager != null ? manager
            : GetComponent<ElementObjectManager>();

        private CanvasGroup cg;
        protected virtual CanvasGroup CG =>
            cg = cg != null ? cg
            : GetComponent<CanvasGroup>();
        private RectTransform rect;
        protected RectTransform Rect =>
            rect = rect != null ? rect
            : GetComponent<RectTransform>();

        private const string LABEL_RT_ROOT = "Root";
        private RectTransform m_Root;
        protected RectTransform Root =>
            m_Root = m_Root != null ? m_Root 
            : Manager.GetElement<RectTransform>(LABEL_RT_ROOT);
        private CanvasGroup m_RootCG;
        protected CanvasGroup RootCG =>
            m_RootCG = m_RootCG != null ? m_RootCG
            : Root.GetComponent<CanvasGroup>();

        private const string LABEL_RT_WINDOW = "Window";
        private RectTransform m_Window;
        protected RectTransform Window =>
            m_Window = m_Window != null ? m_Window 
            : Manager.GetElement<RectTransform>(LABEL_RT_WINDOW);
        private CanvasGroup m_WindowCG;
        protected CanvasGroup WindowCG =>
            m_WindowCG = m_WindowCG != null ? m_WindowCG
            : Window.GetComponent<CanvasGroup>();

        protected const string LABEL_CG_BG = "BG";
        private CanvasGroup m_BG;
        protected CanvasGroup BG =>
            m_BG = m_BG != null ? m_BG
            : Manager.GetElement<CanvasGroup>(LABEL_CG_BG);

        private const string LABEL_CG_BUTTONGROUP = "ButtonGroup";
        private CanvasGroup m_ButtonGroup;
        protected CanvasGroup ButtonGroup =>
            m_ButtonGroup = m_ButtonGroup != null ? m_ButtonGroup
            : Manager.GetElement<CanvasGroup>(LABEL_CG_BUTTONGROUP);

        #endregion

        [SerializeField] protected Selectable defaultSelectable;
        [SerializeField] protected bool needTranslate = true;
        protected virtual void Awake()
        {
            if(needTranslate)
                UIManager.Translate(gameObject);
        }

        #region Input Response

        [HideInInspector] public bool responseInput;
        public void SetResponse(bool response)
        {
            responseInput = response;
        }

        public virtual void Select(bool forced = false)
        {
            if(forced || UserInput.NeedDefaultSelect())
                if(defaultSelectable != null)
                    defaultSelectable.Select();
        }

        #endregion

        protected virtual void OnDestroy()
        {
            DOTweenLifecycle.KillFor(gameObject);
        }
    }
}
