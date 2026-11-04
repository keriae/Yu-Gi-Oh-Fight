using DG.Tweening;
using MDPro3.Duel.YGOSharp;
using MDPro3.Servant;
using MDPro3.UI.ServantUI;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using YgomSystem.ElementSystem;

namespace MDPro3.UI
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
        int turns = -1;

        public static bool battlePhase;
        public static bool main2Phase;
        public static bool endPhase;

        static ElementObjectManager manager;

        private void Start()
        {
            manager = GetComponent<ElementObjectManager>();

            commonPart = manager.GetElement<Transform>("Button");
            textMain = manager.GetElement<TextMeshPro>("Text");
            textMain.font = Program.instance.ui_.jpMenuTmpFont;
            textMain.text = "Main1";

            textAbove = manager.GetElement<TextMeshPro>("Text03");
            textAbove.font = Program.instance.ui_.jpMenuTmpFont;
            textAbove.text = "Turn 1";

            textBelow = manager.GetElement<TextMeshPro>("Text02");
            textBelow.font = Program.instance.ui_.jpMenuTmpFont;
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
            if (battlePhase || main2Phase || endPhase)
            {
                playerMaterial.SetFloat("_Active", 1);

                //Click
                if (!EventSystem.current.IsPointerOverGameObject() 
                    && UserInput.HoverObject == collider_.gameObject 
                    && UserInput.MouseLeftUp)
                {
                    if (Program.instance.ocgcore.currentPopup == null)
                    {
                        var tasks = new List<string>
                        {OcgCore.duelPhase.ToString()};

                        if (battlePhase)
                            tasks.Add(DuelPhase.BattleStart.ToString());
                        if (main2Phase)
                            tasks.Add(DuelPhase.Main2.ToString());
                        if (endPhase)
                            tasks.Add(DuelPhase.End.ToString());
                        Program.instance.ocgcore.GetUI<OcgCoreUI>().ShowPopupPhase(tasks);
                        Program.instance.ocgcore.GetUI<OcgCoreUI>().CardList.Hide();
                    }
                }

                if (!EventSystem.current.IsPointerOverGameObject()
                    && UserInput.HoverObject == collider_.gameObject 
                    && UserInput.MouseLeftPressing)
                    playerMaterial.SetFloat("_PressButton", 1);
                else
                    playerMaterial.SetFloat("_PressButton", 0);

                //MouseOver
                if (!EventSystem.current.IsPointerOverGameObject() 
                    && UserInput.HoverObject == collider_.gameObject 
                    && !hover)
                {
                    hover = true;
                    DOTween.To(() => mouseOver, x => mouseOver = x, 1, 0.2f);
                }
                else if (!EventSystem.current.IsPointerOverGameObject()
                    && UserInput.HoverObject != collider_ 
                    && hover)
                {
                    hover = false;
                    DOTween.To(() => mouseOver, x => mouseOver = x, 0, 0.2f);
                }
                playerMaterial.SetFloat("_MouseOver", mouseOver);
            }
            else
            {
                playerMaterial.SetFloat("_Active", 0);
            }
        }

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
            manager.GetElement("HintEffect").SetActive(false);
        }
    }
}
