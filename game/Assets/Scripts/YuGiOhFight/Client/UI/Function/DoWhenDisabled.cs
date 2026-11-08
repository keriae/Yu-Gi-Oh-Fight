using System;


using UnityEngine;


namespace YuGiOhFight.Client.UI
{
    public class DoWhenDisabled : MonoBehaviour
    {
        public Action action;

        private void OnDisable()
        {
            action?.Invoke();
        }
    }
}
