using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace YuGiOhFight.Client.UI
{
    public class SelectionButton_RoomPlayer : SelectionButton
    {
        [Header("SelectionButton RoomPlayer")]
        public int playerIndex;

        protected override void CallHoverOnEvent()
        {
            base.CallHoverOnEvent();
            if(Controllers.RoomServant.IsHost && playerIndex != Controllers.RoomServant.SelfType)
                Manager.GetElement("KickIcon").SetActive(true);
        }

        protected override void CallHoverOffEvent()
        {
            base.CallHoverOffEvent();
            Manager.GetElement("KickIcon").SetActive(false);
        }

        protected override void OnSubmit()
        {
            base.OnSubmit();
            if (Controllers.RoomServant.IsHost)
            {
                if (playerIndex != Controllers.RoomServant.SelfType)
                    Runtime.Program.instance.room.GetUI<ServantUI.RoomServantUI>().OnKick(playerIndex);
                else
                    Runtime.Program.instance.room.GetUI<ServantUI.RoomServantUI>().OnReady();
            }
        }

        public Image GetAvatar()
        {
            return Manager.GetElement<Image>("Avatar");
        }

        public void SetReadyIcon(bool ready)
        {
            Manager.GetElement("ReadyIcon").SetActive(ready);
        }

        public void SetButtonTextColor(Color color)
        {
            Manager.GetElement<TextMeshProUGUI>("ButtonText").color = color;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Manager.GetElement("KickIcon").SetActive(false);
        }
    }
}
