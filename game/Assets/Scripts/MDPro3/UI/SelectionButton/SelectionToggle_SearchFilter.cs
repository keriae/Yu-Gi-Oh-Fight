using MDPro3.Utility;
using UnityEngine;

namespace MDPro3.UI
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
                SetButtonText(StringHelper.GetUnsafe(code));
                if(subCode != 0)
                {
                    if (subCode == 9999)
                        SetButtonText(StringHelper.Race(filterCode));
                    else if (subCode == 1051 || subCode == 1052)
                        SetButtonText(InterString.Get(originText));
                    else
                    {
                        var title = GetButtonText();
                        if (Language.NeedBlankToAddWord())
                            title += " ";
                        title += StringHelper.GetUnsafe(subCode);
                        SetButtonText(title);
                    }
                }
            }
        }

        protected override void CallHoverOnEvent()
        {
            base.CallHoverOnEvent();
            ((Popup.PopupSearchFilter)Program.instance.ui_.currentPopupB).lastSelectedToggle = this;
        }
    }
}

