using System.Collections.Generic;
using UnityEngine;


namespace YuGiOhFight.Client.Duel
{
    public class GraveBehaviour : MonoBehaviour
    {
        public int controller;
        private Presentation.Fields.BgEffectManager manager;
        private Renderer stateRenderer;
        private Material stateMaterial;
        private static readonly int GraveMouseOver = Shader.PropertyToID("_GraveMouseOver");
        private static readonly int GravePressButton = Shader.PropertyToID("_GravePressButton");
        private static readonly int ExcludeMouseOver = Shader.PropertyToID("_ExcludeMouseOver");
        private static readonly int ExcludePressButton = Shader.PropertyToID("_ExcludePressButton");
        private bool materialStateInitialized;
        private bool lastGraveHover;
        private bool lastGravePress;
        private bool lastExcludeHover;
        private bool lastExcludePress;
        private BoxCollider graveCollider;
        private BoxCollider excludeCollider;
        private Vector3 graveInitialCenter, graveInitialSize, excludeInitialCenter, excludeInitialSize;
        private GameObject grave;
        private GameObject exclude;
        private bool graveCountShowing;
        private bool excludeCountShowing;
        private bool graveButtonsCreated;
        private bool excludeButtonsCreated;
        public List<GameCard.DuelButtonInfo> graveButtons = new();
        public List<UI.DuelButton> graveButtonObjs = new();
        public List<GameCard.DuelButtonInfo> excludeButtons = new();
        public List<UI.DuelButton> excludeButtonObjs = new();

        private void OnEnable()
        {
            materialStateInitialized = false;
        }

        private void OnDisable()
        {
            graveCountShowing = excludeCountShowing = false;
            var program = Runtime.Program.instance;
            if (program != null && program.ocgcore != null)
            {
                program.ocgcore.GetUI<UI.ServantUI.OcgCoreUI>()?.HidePlaceCount(this);
            }
        }

        private void Start()
        {
            manager = GetComponent<Presentation.Fields.BgEffectManager>();
            stateRenderer = manager.GetElement<Renderer>("Material01");
            grave = manager.GetElement<Transform>("GraveIn").parent.gameObject;
            graveCollider = grave.AddComponent<BoxCollider>();
            graveInitialCenter = new Vector3(0, 1, 0);
            graveInitialSize = new Vector3(6, 2, 6);
            graveCollider.center = graveInitialCenter;
            graveCollider.size = graveInitialSize;
            exclude = manager.GetElement<Transform>("ExcludeIn").parent.gameObject;
            excludeCollider = exclude.AddComponent<BoxCollider>();
            excludeInitialCenter = new Vector3(0, 1, 0);
            excludeInitialSize = new Vector3(6, 2, 6);
            excludeCollider.center = excludeInitialCenter;
            excludeCollider.size = excludeInitialSize;
        }

        // Save the exact values written by native initialization. Some stripped
        // Unity reference assemblies retain BoxCollider setters but no getters.
        internal bool TryGetInitialHitVolume(BoxCollider collider, out Vector3 center, out Vector3 size)
        {
            if (collider != null && collider == graveCollider)
            { center = graveInitialCenter; size = graveInitialSize; return true; }
            if (collider != null && collider == excludeCollider)
            { center = excludeInitialCenter; size = excludeInitialSize; return true; }
            center = Vector3.zero; size = Vector3.zero; return false;
        }

        private void Update()
        {
            var graveHover = Runtime.UserInput.HoverObject == grave;
            var excludeHover = Runtime.UserInput.HoverObject == exclude;
            UpdateMaterialState(graveHover, graveHover && Runtime.UserInput.MouseLeftPressing,
                excludeHover, excludeHover && Runtime.UserInput.MouseLeftPressing);

            if (graveHover)
            {
                if (Runtime.UserInput.MouseLeftUp)
                    GraveOnClick();
            }
            else
            {
                if (Runtime.UserInput.MouseLeftUp)
                    HideGraveButtons();
                if (graveCountShowing)
                {
                    graveCountShowing = false;
                    Runtime.Program.instance.ocgcore.GetUI<UI.ServantUI.OcgCoreUI>().HidePlaceCount(this);
                }
            }

            if (excludeHover)
            {
                if (Runtime.UserInput.MouseLeftUp)
                    ExcludeOnClick();
            }
            else
            {
                if (Runtime.UserInput.MouseLeftUp)
                    HideExcludeButtons();
                if (excludeCountShowing)
                {
                    excludeCountShowing = false;
                    Runtime.Program.instance.ocgcore.GetUI<UI.ServantUI.OcgCoreUI>().HidePlaceCount(this);
                }
            }

            // Integrated instruments already display their live pile counts.
            // Keep native hover/click/buttons above, but retire the duplicate overlay.
            if (Runtime.Program.instance.ocgcore.DuelBGManager.UsesThemeFixtures)
            {
                if (graveCountShowing || excludeCountShowing)
                    Runtime.Program.instance.ocgcore.GetUI<UI.ServantUI.OcgCoreUI>().HidePlaceCount(this);
                graveCountShowing = excludeCountShowing = false;
                return;
            }

            if (graveHover)
                if (!graveCountShowing)
                {
                    graveCountShowing = true;
                    Runtime.Program.instance.ocgcore.GetUI<UI.ServantUI.OcgCoreUI>().ShowLocationCount(new GPS { location = (uint)MDPro3.Duel.YGOSharp.CardLocation.Grave, controller = (uint)controller }, this);
                }
            if (excludeHover)
                if (!excludeCountShowing)
                {
                    excludeCountShowing = true;
                    Runtime.Program.instance.ocgcore.GetUI<UI.ServantUI.OcgCoreUI>().ShowLocationCount(new GPS { location = (uint)MDPro3.Duel.YGOSharp.CardLocation.Removed, controller = (uint)controller }, this);
                }
        }

        private void UpdateMaterialState(bool graveHover, bool gravePress, bool excludeHover, bool excludePress)
        {
            var background = Runtime.Program.instance.ocgcore.DuelBGManager;
            if (background.UsesThemeFixtures) return;
            // Refresh if a resource or future appearance change replaces this renderer's material.
            // The per-frame path otherwise avoids element searches and unchanged shader writes.
            if (stateMaterial == null || stateRenderer.sharedMaterial != stateMaterial)
            {
                stateMaterial = stateRenderer.material;
                materialStateInitialized = false;
            }
            if (!materialStateInitialized || graveHover != lastGraveHover)
                stateMaterial.SetFloat(GraveMouseOver, graveHover ? 1f : 0f);
            if (!materialStateInitialized || gravePress != lastGravePress)
                stateMaterial.SetFloat(GravePressButton, gravePress ? 1f : 0f);
            if (!materialStateInitialized || excludeHover != lastExcludeHover)
                stateMaterial.SetFloat(ExcludeMouseOver, excludeHover ? 1f : 0f);
            if (!materialStateInitialized || excludePress != lastExcludePress)
                stateMaterial.SetFloat(ExcludePressButton, excludePress ? 1f : 0f);
            lastGraveHover = graveHover;
            lastGravePress = gravePress;
            lastExcludeHover = excludeHover;
            lastExcludePress = excludePress;
            materialStateInitialized = true;
        }

        private void GraveOnClick()
        {
            Runtime.AudioManager.PlaySE("SE_DUEL_SELECT");

            List<GameCard> cards = Runtime.Program.instance.ocgcore.GCS_GetLocationCards(controller, (int)MDPro3.Duel.YGOSharp.CardLocation.Grave);
            Runtime.Program.instance.ocgcore.GetUI<UI.ServantUI.OcgCoreUI>().CardList.Show(cards, MDPro3.Duel.YGOSharp.CardLocation.Grave, controller);

            if (Runtime.Program.instance.ocgcore.returnAction != null)
                return;
            if (!graveButtonsCreated)
            {
                bool spsummmon = false;
                bool activate = false;
                foreach (GameCard card in Controllers.OcgCore.cards)
                    if ((card.p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.Grave) > 0)
                        if (card.p.controller == controller)
                            foreach (GameCard.DuelButtonInfo btn in card.buttons)
                            {
                                if (btn.type == UI.ButtonType.Activate)
                                    activate = true;
                                if (btn.type == UI.ButtonType.SpSummon)
                                    spsummmon = true;
                            }
                if (activate)
                {
                    int response = -1;
                    graveButtons.Add(new GameCard.DuelButtonInfo() { response = new List<int>() { response }, hint = MDPro3.InterString.Get("发动效果"), type = UI.ButtonType.Activate });
                }
                if (spsummmon)
                {
                    int response = -2;
                    graveButtons.Add(new GameCard.DuelButtonInfo() { response = new List<int>() { response }, hint = MDPro3.InterString.Get("特殊召唤"), type = UI.ButtonType.SpSummon });
                }
                CreateGraveButtons();
            }
            else
                ShowGraveButtons();
        }

        private void ExcludeOnClick()
        {
            Runtime.AudioManager.PlaySE("SE_DUEL_SELECT");

            List<GameCard> cards = Runtime.Program.instance.ocgcore.GCS_GetLocationCards(controller, (int)MDPro3.Duel.YGOSharp.CardLocation.Removed);
            Runtime.Program.instance.ocgcore.GetUI<UI.ServantUI.OcgCoreUI>().CardList.Show(cards, MDPro3.Duel.YGOSharp.CardLocation.Removed, controller);

            if (Runtime.Program.instance.ocgcore.returnAction != null)
                return;
            if (!excludeButtonsCreated)
            {
                bool spsummmon = false;
                bool activate = false;
                foreach (GameCard card in Controllers.OcgCore.cards)
                    if ((card.p.location & (uint)MDPro3.Duel.YGOSharp.CardLocation.Removed) > 0)
                        if (card.p.controller == controller)
                            foreach (GameCard.DuelButtonInfo btn in card.buttons)
                            {
                                if (btn.type == UI.ButtonType.Activate)
                                    activate = true;
                                if (btn.type == UI.ButtonType.SpSummon)
                                    spsummmon = true;
                            }
                if (activate)
                {
                    int response = -1;
                    excludeButtons.Add(new GameCard.DuelButtonInfo() { response = new List<int>() { response }, hint = MDPro3.InterString.Get("发动效果"), type = UI.ButtonType.Activate });
                }
                if (spsummmon)
                {
                    int response = -2;
                    excludeButtons.Add(new GameCard.DuelButtonInfo() { response = new List<int>() { response }, hint = MDPro3.InterString.Get("特殊召唤"), type = UI.ButtonType.SpSummon });
                }
                CreateExcludeButtons();
            }
            else
                ShowExcludeButtons();
        }

        private void ShowGraveButtons()
        {
            foreach (UI.DuelButton button in graveButtonObjs)
                button.Show();
        }

        private void ShowExcludeButtons()
        {
            foreach (UI.DuelButton button in excludeButtonObjs)
                button.Show();
        }

        private void HideGraveButtons()
        {
            foreach (UI.DuelButton button in graveButtonObjs)
                button.Hide();
        }

        private void HideExcludeButtons()
        {
            foreach (UI.DuelButton button in excludeButtonObjs)
                button.Hide();
        }

        private void CreateGraveButtons()
        {
            if (graveButtonsCreated || Runtime.Program.instance.ocgcore.returnAction != null || graveButtons.Count == 0)
                return;
            for (int i = 0; i < graveButtons.Count; i++)
            {
                var obj = Runtime.ABLoader.LoadMasterDuelGameObject("DuelButton");
                UI.DuelButton mono = obj.GetComponent<UI.DuelButton>();
                graveButtonObjs.Add(mono);
                mono.response = graveButtons[i].response;
                mono.hint = graveButtons[i].hint;
                mono.type = graveButtons[i].type;
                mono.id = i;
                mono.buttonsCount = graveButtons.Count;
                mono.cookieCard = null;
                mono.location = (uint)MDPro3.Duel.YGOSharp.CardLocation.Grave;
                mono.controller = (uint)controller;
                mono.Show();
            }
            graveButtonsCreated = true;
        }

        private void CreateExcludeButtons()
        {
            if (excludeButtonsCreated || Runtime.Program.instance.ocgcore.returnAction != null || excludeButtons.Count == 0)
                return;
            for (int i = 0; i < excludeButtons.Count; i++)
            {
                var obj = Runtime.ABLoader.LoadMasterDuelGameObject("DuelButton");
                UI.DuelButton mono = obj.GetComponent<UI.DuelButton>();
                excludeButtonObjs.Add(mono);
                mono.response = excludeButtons[i].response;
                mono.hint = excludeButtons[i].hint;
                mono.type = excludeButtons[i].type;
                mono.id = i;
                mono.buttonsCount = excludeButtons.Count;
                mono.cookieCard = null;
                mono.location = (uint)MDPro3.Duel.YGOSharp.CardLocation.Removed;
                mono.controller = (uint)controller;
                mono.Show();
            }
            excludeButtonsCreated = true;
        }

        public void ClearGraveButtons()
        {
            foreach (UI.DuelButton go in graveButtonObjs)
                Destroy(go.gameObject);
            graveButtonObjs.Clear();
            graveButtons.Clear();
            graveButtonsCreated = false;
        }

        public void ClearExcludeButtons()
        {
            foreach (UI.DuelButton go in excludeButtonObjs)
                Destroy(go.gameObject);
            excludeButtonObjs.Clear();
            excludeButtons.Clear();
            excludeButtonsCreated = false;
        }

    }
}
