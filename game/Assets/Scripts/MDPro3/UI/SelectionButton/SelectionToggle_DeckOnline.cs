using UnityEngine;
using static YgomSystem.UI.ColorContainer;
using YgomSystem.UI;
using TMPro;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using MDPro3.Servant;
using MDPro3.UI.ServantUI;
using MDPro3.Utility;
using Cysharp.Threading.Tasks;

namespace MDPro3.UI
{
    public class SelectionToggle_DeckOnline : SelectionToggle_Deck
    {
        [HideInInspector] public string deckAuthor;
        [HideInInspector] public string deckId;
        [HideInInspector] public string lastDate;
        [HideInInspector] public int like;

        public override void Refresh()
        {
            Manager.GetElement("DeckCaseIcon").SetActive(true);
            Manager.GetElement("TextDeckName").SetActive(true);
            Manager.GetElement("IconAddDeck").SetActive(false);
            Manager.GetElement("SelectedStateToggle").SetActive(false);

            Manager.GetElement<TextMeshProUGUI>("TextDeckName").text = deckName;
            Manager.GetElement<TextMeshProUGUI>("TextDeckAuthor").text = "by " + deckAuthor;
            Manager.GetElement<TextMeshProUGUI>("TextDeckDate").text = lastDate;
            Manager.GetElement<TextMeshProUGUI>("TextDeckLike").text = like.ToString();

            _ = RefreshDeckCaseAsync();

            refreshed = false;
            if (pickuping)
                StartRefresh();
        }

        protected override async UniTask RefreshAsync()
        {
            await UniTask.WaitWhile(() => Program.instance.onlineDeckViewer.inTransition);
            Material pMat = null;
            var cardImage0 = Manager.GetElement<RawImage>("CardImage0");
            if (card0 != 0)
            {
                cardImage0.texture = await CardImageLoader.LoadCardAsync(card0, true);
                //cardImage0.material = await MaterialLoader.LoadCardMaterialAsync(card0, cts.Token);
            }
            else
            {
                if (pMat == null)
                    pMat = await ABLoader.LoadProtectorMaterial(protector, cts.Token);
                cardImage0.texture = null;
                cardImage0.material = pMat;
            }

            var cardImage1 = Manager.GetElement<RawImage>("CardImage1");
            if (card1 != 0)
            {
                cardImage1.texture = await CardImageLoader.LoadCardAsync(card1, true);
                //cardImage1.material = await MaterialLoader.LoadCardMaterialAsync(card1, cts.Token);
            }
            else
            {
                if (pMat == null)
                    pMat = await ABLoader.LoadProtectorMaterial(protector, cts.Token);
                cardImage1.texture = null;
                cardImage1.material = pMat;
            }

            var cardImage2 = Manager.GetElement<RawImage>("CardImage2");
            if (card2 != 0)
            {
                cardImage2.texture = await CardImageLoader.LoadCardAsync(card2, true);
                //cardImage2.material = await MaterialLoader.LoadCardMaterialAsync(card2, cts.Token);
            }
            else
            {
                if (pMat == null)
                    pMat = await ABLoader.LoadProtectorMaterial(protector, cts.Token);
                cardImage2.texture = null;
                cardImage2.material = pMat;
            }
        }

        protected override async UniTask RefreshDeckCaseAsync()
        {
            try
            {
                await UniTask.WaitWhile(
                    () => Program.instance.deckSelector.inTransition,
                    cancellationToken: destroyCancellationToken);

                int siblingIndex = transform.GetSiblingIndex();
                for (int i = 0; i < siblingIndex; i++)
                    await UniTask.Yield(cancellationToken: destroyCancellationToken);

                if (this == null || gameObject == null)
                    return;

                var sprite = await Program.items.LoadDeckCaseIconAsync(deckCase, "_L_SD");
                if (this == null || gameObject == null || sprite == null)
                    return;

                Manager.GetElement<Image>("DeckImage").sprite = sprite;
            }
            catch (System.OperationCanceledException)
            {
            }
            catch (MissingReferenceException)
            {
            }
        }

        protected override void OnClick()
        {
            AudioManager.PlaySE(SoundLabelClick);
            //Program.instance.editDeck.onlineDeckID = deckId;
            //Program.instance.editDeck.SwitchCondition(EditDeck.Condition.OnlineDeck);
            //Program.instance.ShiftToServant(Program.instance.editDeck);

            DeckEditor.onlineDeckID = deckId;
            Program.instance.deckEditor.SwitchCondition(DeckEditor.Condition.OnlineDeck, deckName);
            Program.instance.ShiftToServant(Program.instance.deckEditor);
        }

        protected override void OnSelect(bool playSE)
        {
            HoverOn();
            if (playSE)
                AudioManager.PlaySE(SoundLabelSelectedGamePad);
            Program.instance.currentServant.lastSelectable = Selectable;
            Program.instance.onlineDeckViewer.lastSelectedDeckItem = this;
            SetColor(SelectMode.Selected, hovering ? StatusMode.Enter : StatusMode.Normal, Selectable.interactable);
        }

        public override void ShowPickup(bool forced = false)
        {
            if (forced)
                forcedPickup = true;
            if (pickuping)
                return;
            pickuping = true;

            ApplyShowPickup();
        }

        public override void HidePickup(bool forced = false)
        {
            if (forced)
                forcedPickup = false;
            if (!pickuping)
                return;
            if (forcedPickup)
                return;
            pickuping = false;

            ApplyHidePickup();
        }

        protected override int GetButtonsCount()
        {
            return Program.instance.onlineDeckViewer
                .GetUI<OnlineDeckViewerUI>().superScrollView.items.Count;
        }

        protected override int GetColumnsCount()
        {
            return Program.instance.onlineDeckViewer
                .GetUI<OnlineDeckViewerUI>().superScrollView.GetColumnCount();
        }
    }
}

