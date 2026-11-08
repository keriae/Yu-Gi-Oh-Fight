using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace YuGiOhFight.Client.UI.Popup
{
    public class PopupConfirm : Popup
    {
        [SerializeField] Scrollbar scrollbar;
        [SerializeField] RectTransform content;

        protected override void InitializeSelections()
        {
            base.InitializeSelections();
            var tmp = Manager.GetElement<TextMeshProUGUI>("FrameText");
            tmp.text = args[1];
            if (args.Count > 2)
                tmp.alignment = TextAlignmentOptions.MidlineLeft;

            tmp.GetComponent<ContentSizeFitter>().SetLayoutVertical();
            var height = tmp.rectTransform.rect.height;
            if(height < 34.65f) height = 34.65f;
            if(height > 600f) height = 600f;
            Manager.GetElement<LayoutElement>("ScrollView").preferredHeight = height;
        }

        public override void Show()
        {
            base.Show();
            Runtime.AudioManager.PlaySE("SE_SYS_VERIFY");
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
