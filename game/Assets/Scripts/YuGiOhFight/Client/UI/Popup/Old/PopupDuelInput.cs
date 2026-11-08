using System;
using UnityEngine;
using UnityEngine.UI;


namespace YuGiOhFight.Client.UI
{
    public class PopupDuelInput : PopupDuel
    {
        [Header("Popup Duel Input")]
        public Action<string> confirmAction;
        public Action cancelAction;
        public InputField input;

        public InputValidation.ValidationType validationType;

        public override void InitializeSelections()
        {
            base.InitializeSelections();
            btnConfirm.transform.GetChild(0).GetComponent<Text>().text = selections[1];
            btnCancel.transform.GetChild(0).GetComponent<Text>().text = selections[2];
            input.text = selections[3];
            input.GetComponent<InputValidation>().type = validationType;
            if (cancelAction == null)
            {
                btnCancel.gameObject.SetActive(false);
                float height = btnConfirm.GetComponent<RectTransform>().anchoredPosition.y;
                btnConfirm.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, height);
            }

            Controllers.OcgCore.inputMode = true;
            Runtime.Program.instance.ocgcore.GreenBackgroundOff();
        }

        public override void OnConfirm()
        {
            base.OnConfirm();
            confirmAction?.Invoke(input.text);
            Hide();
        }
        public override void OnCancel()
        {
            base.OnCancel();
            cancelAction?.Invoke();
            Hide();
        }

        private void OnDestroy()
        {
            Controllers.OcgCore.inputMode = false;
        }
    }
}
