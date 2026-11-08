using System.Collections.Generic;
using UnityEngine;


namespace YuGiOhFight.Client.UI
{
    public class Tabs : MonoBehaviour
    {
        public List<Tab> tabs = new List<Tab>();

        private void Start()
        {
            foreach (Tab tab in tabs)
                Runtime.SystemEvent.OnResolutionChange += tab.AdjustSize;
        }

        public void Tab(Tab tab)
        {
            foreach (Tab t in tabs)
            {
                if (t != tab)
                    t.CancelSelect();
            }
        }
        public void AdjustSize()
        {
            foreach (Tab tab in tabs)
                tab.AdjustSize();
        }
    }
}
