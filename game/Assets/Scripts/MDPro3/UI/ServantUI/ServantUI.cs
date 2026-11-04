using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using YgomSystem.ElementSystem;
using MDPro3.Servant;

namespace MDPro3.UI.ServantUI
{
    /// <summary>
    /// Servant的UI部件，在parentServant Show时激活，Hide时摧毁或者SetActive(false);
    /// 因此尽量不要在此类中调用协程。
    /// 主要用途是初始化UI和给按钮分配方法。
    /// </summary>
    [RequireComponent(typeof(ElementObjectManager))]
    public class ServantUI : MonoBehaviour
    {

        #region Elements

        private ElementObjectManager m_Manager;
        public ElementObjectManager Manager =>
            m_Manager = m_Manager != null ? m_Manager
            : GetComponent<ElementObjectManager>();

        private const string LABEL_RT_ROOT = "Root";
        private RectTransform m_Root;
        public RectTransform Root =>
            m_Root = m_Root != null ? m_Root
            : Manager.GetElement<RectTransform>(LABEL_RT_ROOT);

        private CanvasGroup m_CG;
        public virtual CanvasGroup CG =>
            m_CG = m_CG != null ? m_CG
            : Root.GetComponent<CanvasGroup>();

        private const string LABEL_TXT_TITLE = "TextTitle";
        private TextMeshProUGUI m_Title;
        protected TextMeshProUGUI Title =>
            m_Title = m_Title != null ? m_Title
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TXT_TITLE);

        private const string LABEL_RT_LEFTPART = "LeftPart";
        private RectTransform m_LeftPart;
        protected RectTransform LeftPart =>
            m_LeftPart = m_LeftPart != null ? m_LeftPart
            : Manager.GetElement<RectTransform>(LABEL_RT_LEFTPART);

        private const string LABEL_RT_RIGHTPART = "RightPart";
        private RectTransform m_RightPart;
        protected RectTransform RightPart =>
            m_RightPart = m_RightPart != null ? m_RightPart
            : Manager.GetElement<RectTransform>(LABEL_RT_RIGHTPART);

        #endregion

        [Header("Servant UI")]
        [SerializeField] protected Selectable defaultSelectable;

        protected Servant.Servant parentServant;

        public virtual void Initialize(Servant.Servant servant)
        {
            parentServant = servant;
            UIManager.Translate(gameObject);
        }

        public virtual void OnBack()
        {
            Program.instance.ExitCurrentServant();
        }

        public virtual void SelectDefaultSelectable()
        {
            if (defaultSelectable != null)
                defaultSelectable.Select();
        }

        public virtual void ResetUI()
        {
            KillTransitionTweens();
            Root.anchoredPosition3D = Vector3.zero;
            Root.localEulerAngles = Vector3.zero;
            CG.alpha = 1f;
            CG.blocksRaycasts = true;
            ResetLeftAndRightParts();
        }

        public virtual void ShutDown()
        {
            KillTransitionTweens();
            gameObject.SetActive(false);
        }

        public virtual void Show(bool cover)
        {
            if (HaveTwoParts())
                Show_Push();
            else
            {
                if(cover)
                    Show_Cover();
                else
                    Show_Uncover();
            }
            ShowEvent();
        }

        public virtual void ShowEvent()
        {
        }

        public virtual void AfterShowEvent()
        {
        }

        public virtual void Hide(bool cover)
        {
            if (HaveTwoParts())
                Hide_Pop();
            else
            {
                if(cover)
                    Hide_Cover();
                else
                    Hide_Uncover();
            }
            HideEvent();
        }

        protected virtual void HideEvent()
        {
        }

        protected virtual void AfterHideEvent()
        {
        }

        protected T GetServant<T>() where T : Servant.Servant
        {
            return parentServant as T;
        }

        protected bool HaveTwoParts()
        {
            return LeftPart != null && RightPart != null;
        }

        protected virtual void ResetLeftAndRightParts()
        {
            if (!HaveTwoParts())
                return;

            LeftPart.anchoredPosition = Vector2.zero;
            RightPart.anchoredPosition = Vector2.zero;
        }

        protected virtual void Show_Push()
        {
            KillTransitionTweens();
            gameObject.SetActive(true);
            CG.alpha = 0f;
            CG.blocksRaycasts = false;

            DOTween.Sequence()
                .SetTarget(gameObject)
                .SetUpdate(true)
                .AppendInterval(0.2f)
                .Append(CG.DOFade(1f, 0.4f).SetEase(Ease.Linear))
                .OnComplete(() =>
                {
                    CG.blocksRaycasts = true;
                    AfterShowEvent();
                });

            LeftPart.anchoredPosition = new Vector2(-1500f, 0f);
            DOTween.Sequence()
                .SetTarget(gameObject)
                .SetUpdate(true)
                .AppendInterval(0.2f)
                .Append(LeftPart.DOAnchorPosX(0f, 0.4f).SetEase(Ease.OutQuart));

            RightPart.anchoredPosition = new Vector2(1500f, 0f);
            DOTween.Sequence()
                .SetTarget(gameObject)
                .SetUpdate(true)
                .AppendInterval(0.2f)
                .Append(RightPart.DOAnchorPosX(0f, 0.4f).SetEase(Ease.OutQuart));
        }

        protected virtual void Show_Cover()
        {
            KillTransitionTweens();
            gameObject.SetActive(true);
            CG.alpha = 0f;
            CG.blocksRaycasts = false;

            DOTween.Sequence()
                .SetTarget(gameObject)
                .SetUpdate(true)
                .AppendInterval(0.15f)
                .Append(CG.DOFade(1f, 0.3f).SetEase(Ease.Linear))
                .OnComplete(() =>
                {
                    CG.blocksRaycasts = true;
                    AfterShowEvent();
                });

            Root.anchoredPosition3D = new Vector3(-240f, 0f, -360f);
            DOTween.Sequence()
                .SetTarget(gameObject)
                .SetUpdate(true)
                .AppendInterval(0.15f)
                .Append(Root.DOAnchorPos3D(Vector3.zero, 0.5f).SetEase(Ease.OutQuart));
            Root.localEulerAngles = new Vector3(0f, 15f, 0f);
            DOTween.Sequence()
                .SetTarget(gameObject)
                .SetUpdate(true)
                .AppendInterval(0.15f)
                .Append(Root.DOLocalRotate(Vector3.zero, 0.5f).SetEase(Ease.OutQuart));
        }

        protected virtual void Show_Uncover()
        {
            KillTransitionTweens();
            gameObject.SetActive(true);
            CG.alpha = 0f;
            CG.blocksRaycasts = false;

            DOTween.Sequence()
                .SetTarget(gameObject)
                .SetUpdate(true)
                .AppendInterval(0.15f)
                .Append(CG.DOFade(1f, 0.3f).SetEase(Ease.Linear))
                .OnComplete(() =>
                {
                    CG.blocksRaycasts = true;
                    AfterShowEvent();
                });

            Root.anchoredPosition3D = new Vector3(240f, 0f, 360f);
            DOTween.Sequence()
                .SetTarget(gameObject)
                .SetUpdate(true)
                .AppendInterval(0.15f)
                .Append(Root.DOAnchorPos3D(Vector3.zero, 0.5f).SetEase(Ease.OutQuart));
            Root.localEulerAngles = new Vector3(0f, -15f, 0f);
            DOTween.Sequence()
                .SetTarget(gameObject)
                .SetUpdate(true)
                .AppendInterval(0.15f)
                .Append(Root.DOLocalRotate(Vector3.zero, 0.5f).SetEase(Ease.OutQuart));
        }

        protected virtual void Hide_Pop()
        {
            KillTransitionTweens();
            CG.blocksRaycasts = false;
            CG.DOFade(0f, 0.25f)
                .SetEase(Ease.Linear)
                .SetUpdate(true)
                .OnComplete(() =>
            {
                gameObject.SetActive(false);
                AfterHideEvent();
            });

            LeftPart.DOAnchorPosX(-1500f, 0.25f).SetEase(Ease.InCubic).SetUpdate(true);
            RightPart.DOAnchorPosX(1500f, 0.25f).SetEase(Ease.InCubic).SetUpdate(true);
        }

        protected virtual void Hide_Cover()
        {
            KillTransitionTweens();
            CG.blocksRaycasts = false;
            CG.DOFade(0f, 0.2f)
                .SetEase(Ease.Linear)
                .SetUpdate(true)
                .OnComplete(() =>
            {
                gameObject.SetActive(false);
                AfterHideEvent();
            });

            Root.DOAnchorPos3D(new Vector3(-240f, 0f, -360f), 0.2f).SetEase(Ease.InCubic).SetUpdate(true);
            Root.DOLocalRotate(new Vector3(0f, 15f, 0f), 0.2f).SetEase(Ease.InCubic).SetUpdate(true);
        }

        protected virtual void Hide_Uncover()
        {
            KillTransitionTweens();
            CG.blocksRaycasts = false;
            CG.DOFade(0f, 0.2f)
                .SetEase(Ease.Linear)
                .SetUpdate(true)
                .OnComplete(() =>
            {
                gameObject.SetActive(false);
                AfterHideEvent();
            });

            Root.DOAnchorPos3D(new Vector3(240f, 0f, 360f), 0.2f).SetEase(Ease.InCubic).SetUpdate(true);
            Root.DOLocalRotate(new Vector3(0f, -15f, 0f), 0.2f).SetEase(Ease.InCubic).SetUpdate(true);
        }

        protected virtual void OnDestroy()
        {
            KillTransitionTweens();
        }

        protected virtual void KillTransitionTweens()
        {
            DOTween.Kill(gameObject, false);
            DOTweenLifecycle.KillTarget(m_Root);
            DOTweenLifecycle.KillTarget(m_CG);
            DOTweenLifecycle.KillTarget(m_LeftPart);
            DOTweenLifecycle.KillTarget(m_RightPart);
        }

    }
}
