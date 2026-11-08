using TMPro;
using UnityEngine;


namespace YuGiOhFight.Client.UI
{
    public class InputFiledChangeAutoFocus : MonoBehaviour
    {
        private TMP_InputField m_InputField;
        private TMP_InputField InputField =>
            m_InputField = m_InputField != null ? m_InputField
            : GetComponent<TMP_InputField>();

        private void Awake()
        {
            Runtime.UserInput.OnMouseCursorHide += SetAutoFocus;
        }

        private void OnDestroy()
        {
            Runtime.UserInput.OnMouseCursorHide -= SetAutoFocus;
        }

        private void SetAutoFocus()
        {
            if (InputField != null)
                InputField.shouldActivateOnSelect = Cursor.lockState == CursorLockMode.None;
        }
    }
}
