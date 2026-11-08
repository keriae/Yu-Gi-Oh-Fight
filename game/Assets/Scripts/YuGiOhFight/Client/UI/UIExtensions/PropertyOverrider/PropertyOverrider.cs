using UnityEngine;


namespace YuGiOhFight.Client.UI.PropertyOverride
{
    public abstract class PropertyOverrider : MonoBehaviour
    {
        [SerializeField] protected bool decideByUser = true;

        public static bool NeedMobileLayout()
        {
            var mode = MDPro3.Config.GetFloat("Layout", 0f);
            if (mode == 0f)
                return MDPro3.Utility.DeviceInfo.OnMobile();
            else if (mode == 1f)
                return false;
            else
                return true;
        }

        protected virtual void Awake()
        {
            Override();
        }

        protected virtual void DefaultOverride()
        {
        }
        protected virtual void MobileOverride()
        {
        }


        public virtual void Override()
        {
            if (decideByUser)
            {
                if (NeedMobileLayout())
                    MobileOverride();
                else
                    DefaultOverride();
            }
            else
            {
                if (MDPro3.Utility.DeviceInfo.OnMobile())
                    MobileOverride();
                else
                    DefaultOverride();
            }
        }
    }
}
