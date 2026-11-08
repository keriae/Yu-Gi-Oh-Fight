using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;


using UnityEngine.AddressableAssets;


namespace YuGiOhFight.Client.Controllers
{
    public class Servant : MonoBehaviour
    {
        [HideInInspector] public Servant returnServant;
        [HideInInspector] public Selectable lastSelectable;

        [HideInInspector] public bool showing;
        [HideInInspector] public bool inTransition;

        public Action returnAction;
        public virtual float TransitionTime => 0.4f;

        public virtual int Depth => 1;
        protected virtual bool ShowLine => false;
        protected virtual bool NeedExitButton => true;
        protected virtual float BlackAlpha => 0f;
        protected virtual float SubBlackAlpha => 0f;
        protected virtual string Label_UI => $"ServantUI/{GetType().Name}UI.prefab";

        public UI.ServantUI.ServantUI servantUI;
        private int clientLoadVersion;

        public virtual void Initialize()
        {
            Runtime.UserInput.OnMouseCursorHide += OnMouseCursorHide;
        }

        public void Show(int preDepth)
        {
            if(showing) return;

            showing = true;
            var loadVersion = ++clientLoadVersion;

            if (servantUI != null)
            {
                ApplyShowArrangement(preDepth);
            }
            else
            {
                inTransition = true;
                var handle = Addressables.InstantiateAsync(Label_UI);
                handle.Completed += (result) =>
                {
                    if (!showing || loadVersion != clientLoadVersion)
                    {
                        if (result.Result != null) Addressables.ReleaseInstance(result.Result);
                        return;
                    }
                    result.Result.transform.SetParent(transform, false);
                    servantUI = result.Result.GetComponent<UI.ServantUI.ServantUI>();
                    FirstLoadEvent();
                    ApplyShowArrangement(preDepth);
                };
            }
        }

        public void Hide(int nextDepth)
        {
            if (!showing) return;

            showing = false;
            ++clientLoadVersion;
            if (servantUI == null) { inTransition = false; return; }
            ApplyHideArrangement(nextDepth);
        }

        public virtual void PerFrameFunction()
        {
            if (!NeedResponseInput())
                return;

            if (Runtime.UserInput.MouseRightDown || Runtime.UserInput.WasCancelPressed)
                OnReturn();
        }

        /// <summary>
        /// 响应后退动作：鼠标右键、键盘Esc键、手柄East键等。
        /// </summary>
        public virtual void OnReturn()
        {
            Runtime.AudioManager.PlaySE("SE_MENU_CANCEL");
            if (returnAction != null)
            {
                returnAction.Invoke();
                return;
            }
            else
                OnExit();
        }

        /// <summary>
        /// 退出当前Servant。
        /// </summary>
        public virtual void OnExit()
        {
            if (Runtime.Program.instance.currentSubServant == this)
            {
                if (this is SettingServant)
                {
                    Hide(-2);
                    Runtime.Program.instance.currentSubServant = null;
                }
                else
                    Runtime.Program.instance.ShowSubServant(returnServant);
            }
            else
                Runtime.Program.instance.ShiftToServant(returnServant);
        }

        public virtual bool NeedResponseInput()
        {
            if (UI.Client.FightClient.BlocksServantInput) return false;
            if (!showing) return false;
            if (inTransition) return false;
            if (servantUI == null) return false;

            if (Runtime.UIManager.InputBlocker != null)
                return false;
            if (Runtime.Program.instance.ui_.currentPopup != null)
                return false;
            if (Runtime.Program.instance.ui_.currentPopupB != null)
                return false;
            if (Runtime.Program.instance.ui_.currentSidePanel != null)
                return false;
            if (Runtime.UserInput.InputFieldActivating())
                return false;
            return true;
        }

        public virtual void Select(bool forced = false)
        {
            if (!forced && !Runtime.UserInput.NeedDefaultSelect())
                return;
            if (lastSelectable != null)
                lastSelectable.Select();
            else if (servantUI != null)
                servantUI.SelectDefaultSelectable();
        }

        public virtual void JudgeInputBlockerExitMark(object o)
        {
        }

        public virtual T GetUI<T>() where T : UI.ServantUI.ServantUI
        {
            return servantUI as T;
        }

        protected void LoadUI()
        {
            var handle = Addressables.InstantiateAsync(Label_UI);
            handle.Completed += (result) =>
            {
                result.Result.transform.SetParent(transform, false);
                servantUI = result.Result.GetComponent<UI.ServantUI.ServantUI>();
                FirstLoadEvent();
            };
        }

        protected virtual void FirstLoadEvent()
        {
            servantUI.Initialize(this);
        }

        protected virtual void ApplyShowArrangement(int preDepth)
        {
            inTransition = true;
            var presentationVersion = clientLoadVersion;
            UI.ServantUI.ServantUI presentationUI = servantUI;

            bool fromDuel =
                Runtime.Program.instance.currentServant == this
                && preDepth == -1;

            if (fromDuel)
            {
                servantUI.ShowEvent();
                DOTween.To(v => { }, 0, 0, Runtime.Program.instance.ocgcore.TransitionTime)
                    .SetUpdate(true)
                    .OnComplete(() =>
                {
                    if (!showing || presentationVersion != clientLoadVersion
                        || presentationUI == null || servantUI != presentationUI) return;
                    servantUI.gameObject.SetActive(true);
                    inTransition = false;
                    servantUI.ResetUI();

                    if (NeedExitButton)
                        Runtime.UIManager.ShowExitButton(0);
                    else
                        Runtime.UIManager.HideExitButton(0);

                    if (ShowLine)
                        Runtime.UIManager.ShowLine(0);
                    else
                        Runtime.UIManager.HideLine(0);

                    Runtime.UIManager.ShowBlackBack(BlackAlpha, 0f);
                    servantUI.AfterShowEvent();
                    AfterShowingEvent();
                });
            }
            else
            {
                servantUI.gameObject.SetActive(true);
                servantUI.Show(preDepth > Depth);

                if (NeedExitButton)
                    Runtime.UIManager.ShowExitButton(TransitionTime);
                else
                    Runtime.UIManager.HideExitButton(TransitionTime);
                if (ShowLine)
                    Runtime.UIManager.ShowLine(TransitionTime);
                else
                    Runtime.UIManager.HideLine(TransitionTime);

                Runtime.UIManager.ShowBlackBack(
                    Runtime.Program.instance.currentServant is OcgCore
                    ? SubBlackAlpha : BlackAlpha
                    , TransitionTime
                    , () =>
                    {
                        if (!showing || presentationVersion != clientLoadVersion
                            || presentationUI == null || servantUI != presentationUI) return;
                        inTransition = false;
                        AfterShowingEvent();
                    });
            }
        }

        protected virtual void AfterShowingEvent()
        {
            if (Runtime.UserInput.NeedDefaultSelect())
                Select();
        }

        protected virtual void ApplyHideArrangement(int nextDepth)
        {
            inTransition = true;
            var presentationVersion = clientLoadVersion;
            UI.ServantUI.ServantUI presentationUI = servantUI;

            bool toDuel = nextDepth == -1;

            if (toDuel)
            {
                DOTween.To(v => { }, 0, 0, TransitionTime)
                    .SetUpdate(true)
                    .OnComplete(() =>
                {
                    if (showing || presentationVersion != clientLoadVersion
                        || presentationUI == null || servantUI != presentationUI) return;
                    servantUI.ShutDown();
                    inTransition = false;
                });
            }
            else
            {
                servantUI.Hide(nextDepth > Depth);
                DOTween.To(v => { }, 0, 0, TransitionTime)
                    .SetUpdate(true)
                    .OnComplete(() =>
                {
                    if (showing || presentationVersion != clientLoadVersion
                        || presentationUI == null || servantUI != presentationUI) return;
                    inTransition = false;
                    AfterHidingEvent();
                });
            }
        }

        protected virtual void AfterHidingEvent()
        {
        }

        protected virtual void OnMouseCursorHide()
        {
            if (NeedResponseInput())
                Select();
        }

    }
}
