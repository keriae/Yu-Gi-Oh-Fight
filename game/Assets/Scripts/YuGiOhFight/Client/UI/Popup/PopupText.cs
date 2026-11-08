using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace YuGiOhFight.Client.UI.Popup
{
    public class PopupText : Popup
    {
        [Header("Popup Text")]
        public HorizontalAlignmentOptions alignment = HorizontalAlignmentOptions.Center;
        [SerializeField] Scrollbar scrollbar;
        [SerializeField] RectTransform content;

        protected override void InitializeSelections()
        {
            base.InitializeSelections();
            var text = Manager.GetElement<TextMeshProUGUI>("Text");
            text.text = args[1];
            text.horizontalAlignment = alignment;

            text.GetComponent<ContentSizeFitter>().SetLayoutVertical();
            var preferredHeight = text.GetComponent<RectTransform>().rect.height + (PropertyOverride.PropertyOverrider.NeedMobileLayout() ? 40f : 32f);
            Manager.GetElement<LayoutElement>("EntryButtonsScrollView").preferredHeight = preferredHeight;
        }

        protected override void Update()
        {
            if (!NeedResponseInput())
                return;

            if ((Runtime.UserInput.MouseRightDown || Runtime.UserInput.WasCancelPressed) && cancelCallHide)
            {
                Runtime.AudioManager.PlaySE("SE_MENU_CANCEL");
                Hide();
            }

            if (Runtime.UserInput.RightScrollWheel.y != 0f)
            {
                scrollbar.value = Mathf.Clamp01(scrollbar.value + Runtime.UserInput.RightScrollWheel.y * 1000f * ScrollSpeed.WheelScale * Time.unscaledDeltaTime / content.rect.height);
            }
        }
    }
}
