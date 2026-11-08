using UnityEngine;


namespace YuGiOhFight.Client.UI
{
    public class SelectionToggle_SearchFilter : SelectionToggle
    {
        [Header("SelectionToggle SearchFilter")]
        public int code;
        public int subCode;
        public int group;
        public long filterCode;

        protected override void Awake()
        {
            base.Awake();
            if(code != 0)
            {
                var originText = GetButtonText();
                SetButtonText(Presentation.StringHelper.GetUnsafe(code));
                if(subCode != 0)
                {
                    if (subCode == 9999)
                        SetButtonText(Presentation.StringHelper.Race(filterCode));
                    else if (subCode == 1051 || subCode == 1052)
                        SetButtonText(MDPro3.InterString.Get(originText));
                    else
                    {
                        var title = GetButtonText();
                        if (MDPro3.Utility.Language.NeedBlankToAddWord())
                            title += " ";
                        title += Presentation.StringHelper.GetUnsafe(subCode);
                        SetButtonText(title);
                    }
                }
            }
        }

        protected override void CallHoverOnEvent()
        {
            base.CallHoverOnEvent();
            ((Popup.PopupSearchFilter)Runtime.Program.instance.ui_.currentPopupB).lastSelectedToggle = this;
        }
    }
}
