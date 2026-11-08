using System.Collections.Generic;
using UnityEngine;


namespace YuGiOhFight.Client.UI
{
    public class DropArea : MonoBehaviour
    {
        #region Elements

        private Presentation.Elements.ElementObjectManager m_Manager;
        private Presentation.Elements.ElementObjectManager Manager =>
            m_Manager = m_Manager != null ? m_Manager
            : GetComponentInParent<Presentation.Elements.ElementObjectManager>();

        #endregion

        public bool active = true;

        private List<string> showLabels = new();

        protected void Awake()
        {
            Runtime.UserInput.OnDragStart += Show;
            Runtime.UserInput.OnDragEnd += Hide;
        }

        protected void OnDestroy()
        {
            Runtime.UserInput.OnDragStart -= Show;
            Runtime.UserInput.OnDragEnd -= Hide;
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
            foreach (Presentation.Elements.ElementObject element in Manager.serializedElements)
                element.gameObject.SetActive(false);
        }
    }
}
