using DG.Tweening;


using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;


namespace YuGiOhFight.Client.UI
{
    public class PhaseButtonHandler : MonoBehaviour
    {
        static Transform commonPart;
        static Transform playerPart;
        static Transform opponentPart;
        static TextMeshPro textMain;
        static TextMeshPro textBelow;
        static TextMeshPro textAbove;

        Material playerMaterial;
        Material opponentMaterial;
        Collider collider_;
        bool hover;
        float mouseOver;
        Tween hoverTween;

        public static bool battlePhase;
        public static bool main2Phase;
        public static bool endPhase;

        static Presentation.Elements.ElementObjectManager manager;

        private void Start()
        {
            manager = GetComponent<Presentation.Elements.ElementObjectManager>();

            commonPart = manager.GetElement<Transform>("Button");
            textMain = manager.GetElement<TextMeshPro>("Text");
            textMain.font = Runtime.Program.instance.ui_.jpMenuTmpFont;
            textMain.text = "Main1";

            textAbove = manager.GetElement<TextMeshPro>("Text03");
            textAbove.font = Runtime.Program.instance.ui_.jpMenuTmpFont;
            textAbove.text = "Turn 1";

            textBelow = manager.GetElement<TextMeshPro>("Text02");
            textBelow.font = Runtime.Program.instance.ui_.jpMenuTmpFont;
            textBelow.fontSize = 10f;
            textBelow.text = "";

            playerPart = manager.GetElement<Transform>("PlayerBase");
            opponentPart = manager.GetElement<Transform>("OpponentBase");
            playerMaterial = playerPart.GetComponent<Renderer>().material;
            opponentMaterial = opponentPart.GetComponent<Renderer>().material;
            opponentMaterial.SetFloat("_SwitchTurn", 1);
            collider_ = commonPart.GetComponent<Collider>();
        }

        private void Update()
        {
            var program = Runtime.Program.instance;
            var core = program != null ? program.ocgcore : null;
            bool available = (battlePhase || main2Phase || endPhase)
                && collider_ != null && collider_.enabled && collider_.gameObject.activeInHierarchy
                && core != null && core.showing && program.currentServant == core;
            bool blocked = EventSystem.current == null || EventSystem.current.IsPointerOverGameObject()
                || Runtime.UIManager.InputBlocker != null || Client.FightDuelMenu.BlocksInput || Client.FightOpeningCoin.IsShowing
                || core != null && core.currentPopup != null
                || program != null && program.ui_ != null
                    && (program.ui_.currentPopup != null || program.ui_.currentPopupB != null);
            if (!available || blocked || playerMaterial == null)
            {
                ResetFeedback();
                return;
            }
            playerMaterial.SetFloat("_Active", 1);
            bool hit = Runtime.UserInput.HoverObject == collider_.gameObject;
            SetHovered(hit);
            playerMaterial.SetFloat("_PressButton", hit && Runtime.UserInput.MouseLeftPressing ? 1 : 0);

            // Keep the original phase list and response entry point. UI layers
            // above have already rejected both feedback and pointer submission.
            if (hit && Runtime.UserInput.MouseLeftUp)
            {
                var tasks = new List<string>
                {Controllers.OcgCore.duelPhase.ToString()};

                if (battlePhase)
                    tasks.Add(MDPro3.Duel.YGOSharp.DuelPhase.BattleStart.ToString());
                if (main2Phase)
                    tasks.Add(MDPro3.Duel.YGOSharp.DuelPhase.Main2.ToString());
                if (endPhase)
                    tasks.Add(MDPro3.Duel.YGOSharp.DuelPhase.End.ToString());
                core.GetUI<ServantUI.OcgCoreUI>().ShowPopupPhase(tasks);
                core.GetUI<ServantUI.OcgCoreUI>().CardList.Hide();
                ResetFeedback();
            }
        }

        private void SetHovered(bool hit)
        {
            if (hover == hit) return;
            hover = hit;
            hoverTween?.Kill();
            hoverTween = DOTween.To(() => mouseOver, value =>
            {
                mouseOver = value;
                if (playerMaterial != null) playerMaterial.SetFloat("_MouseOver", value);
            }, hit ? 1 : 0, .2f).SetUpdate(true).SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnComplete(() => hoverTween = null);
        }

        private void ResetFeedback()
        {
            hoverTween?.Kill(); hoverTween = null;
            hover = false; mouseOver = 0;
            if (playerMaterial != null)
            {
                playerMaterial.SetFloat("_Active", 0);
                playerMaterial.SetFloat("_PressButton", 0);
                playerMaterial.SetFloat("_MouseOver", 0);
            }
        }

        private void OnDisable() => ResetFeedback();
        private void OnDestroy() => ResetFeedback();

        public static void TurnChange(bool me, int turns)
        {
            SetTextAbove("Turn " + turns.ToString());
            commonPart.localScale = Vector3.zero;
            commonPart.DOScale(Vector3.one, 0.3f);
            if (me)
            {
                playerPart.DOScale(Vector3.one, 0.3f);
                opponentPart.DOScale(Vector3.one * 0.1f, 0.3f);
            }
            else
            {
                playerPart.DOScale(Vector3.one * 0.1f, 0.3f);
                opponentPart.DOScale(Vector3.one, 0.3f);
            }
        }

        public static void SetTextMain(string text)
        {
            textMain.text = text;
        }
        public static void SetTextAbove(string text)
        {
            textAbove.text = text;
        }
        public static void SetTextBelow(string text)
        {
            textBelow.text = text;
        }

        public static void SetHint()
        {
            manager.GetElement("HintEffect").SetActive(true);
        }

        public static void CloseHint()
        {
            if (manager == null) return;
            var hint = manager.GetElement("HintEffect");
            if (hint != null) hint.SetActive(false);
        }
    }
}
