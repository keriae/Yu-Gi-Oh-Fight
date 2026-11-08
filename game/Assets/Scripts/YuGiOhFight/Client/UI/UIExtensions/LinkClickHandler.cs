using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;


namespace YuGiOhFight.Client.UI
{
    [RequireComponent(typeof(TMP_Text))]
    public class LinkClickHandler : MonoBehaviour, IPointerClickHandler
    {
        public void OnPointerClick(PointerEventData eventData)
        {
            var tmp = GetComponent<TMP_Text>();
            int linkIndex = TMP_TextUtilities.FindIntersectingLink(tmp, eventData.position, Runtime.Program.instance.camera_.cameraUI);
            if (linkIndex != -1)
            {
                TMP_LinkInfo linkInfo = tmp.textInfo.linkInfo[linkIndex];
                Application.OpenURL(linkInfo.GetLinkID());
            }
        }
    }
}
