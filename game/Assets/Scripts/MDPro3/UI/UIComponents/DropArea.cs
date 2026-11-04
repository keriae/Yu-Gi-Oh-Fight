using System.Collections.Generic;
using UnityEngine;
using YgomSystem.ElementSystem;

namespace MDPro3.UI
{
    public class DropArea : MonoBehaviour
    {
        #region Elements

        private ElementObjectManager m_Manager;
        private ElementObjectManager Manager =>
            m_Manager = m_Manager != null ? m_Manager
            : GetComponentInParent<ElementObjectManager>();

        #endregion

        public bool active = true;

        private List<string> showLabels = new();

        protected void Awake()
        {
            UserInput.OnDragStart += Show;
            UserInput.OnDragEnd += Hide;
        }

        protected void OnDestroy()
        {
            UserInput.OnDragStart -= Show;
            UserInput.OnDragEnd -= Hide;
        }

        public void SetShowLabel(string label)
        {
            showLabels.Add(label);
        }

        public void ClearLabels()
        {
            showLabels.Clear();
        }

        private void Show()
        {
            if(!active || !gameObject.activeInHierarchy)
                return;
            foreach (var label in showLabels)
            {
                var part = Manager.GetElement(label);
                if (part != null)
                    part.SetActive(true);
            }
        }

        private void Hide()
        {
            if(!gameObject.activeInHierarchy)
                return;
            foreach (var element in Manager.serializedElements)
                element.gameObject.SetActive(false);
        }
    }
}
