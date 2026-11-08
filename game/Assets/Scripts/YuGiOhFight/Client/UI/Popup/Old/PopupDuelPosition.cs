using Cysharp.Threading.Tasks;


using UnityEngine;
using UnityEngine.UI;


namespace YuGiOhFight.Client.UI
{
    public class PopupDuelPosition : PopupDuel
    {
        [Header("Popup Duel Position Reference")]
        public RawImage positionAttack;
        public RawImage positionDefense;
        public RawImage positionDefenseDown;

        public Button btnPositionAttack;
        public Button btnPositionDefense;
        public Button btnPositionDefenseDown;

        public int code;
        public int count;
        public int option1;
        public int option2;

        public override void InitializeSelections()
        {
            _ = RefreshCard(code);
            if (count == 3)
            {
                btnPositionAttack.onClick.AddListener(OnAttack);
                btnPositionDefense.onClick.AddListener(OnDefense);
                btnPositionDefenseDown.onClick.AddListener(OnDefenseDown);
            }
            else
            {
                bool p1 = false;
                bool p2 = false;
                bool p3 = false;

                if (option1 == 1)
                {
                    btnPositionAttack.onClick.AddListener(OnAttack);
                    positionAttack.GetComponent<RectTransform>().anchoredPosition = new Vector2(-150, 0);
                    p1 = true;
                }
                else if (option1 == 2)
                {
                    btnPositionAttack.onClick.AddListener(OnAttackDown);
                    positionAttack.GetComponent<RectTransform>().anchoredPosition = new Vector2(-150, 0);
                    p1 = true;
                }
                else
                {
                    btnPositionDefense.onClick.AddListener(OnDefense);
                    positionDefense.GetComponent<RectTransform>().anchoredPosition = new Vector2(-150, 0);
                    p2 = true;
                }

                if (option2 == 4)
                {
                    btnPositionDefense.onClick.AddListener(OnDefense);
                    positionDefense.GetComponent<RectTransform>().anchoredPosition = new Vector2(150, 0);
                    p2 = true;
                }
                else
                {
                    btnPositionDefenseDown.onClick.AddListener(OnDefenseDown);
                    positionDefenseDown.GetComponent<RectTransform>().anchoredPosition = new Vector2(150, 0);
                    p3 = true;
                }

                if (!p1)
                    Destroy(positionAttack.gameObject);
                if (!p2)
                    Destroy(positionDefense.gameObject);
                if (!p3)
                    Destroy(positionDefenseDown.gameObject);
            }
        }
        private async UniTask RefreshCard(int code)
        {
            var texture = await Presentation.CardImageLoader.LoadCardAsync(code, false, destroyCancellationToken);
            var mat = MaterialLoader.GetCardMaterial(code);
            if (positionAttack != null)
            {
                if (option1 == 1)
                {
                    positionAttack.material = mat;
                    positionAttack.texture = texture;
                }
                else
                    positionAttack.material = Controllers.OcgCore.myProtector;
            }
            if (positionDefense != null)
            {
                positionDefense.material = mat;
                positionDefense.texture = texture;
            }
            if (positionDefenseDown != null)
                positionDefenseDown.material = Controllers.OcgCore.myProtector;
        }

        private void OnAttack()
        {
            Hide();
            Runtime.Network.BinaryMaster p = new Runtime.Network.BinaryMaster();
            p.writer.Write(1);
            SendReturn(p.Get());
        }
        private void OnAttackDown()
        {
            Hide();
            Runtime.Network.BinaryMaster p = new Runtime.Network.BinaryMaster();
            p.writer.Write(2);
            SendReturn(p.Get());
        }
        private void OnDefense()
        {
            Hide();
            Runtime.Network.BinaryMaster p = new Runtime.Network.BinaryMaster();
            p.writer.Write(4);
            SendReturn(p.Get());
        }
        private void OnDefenseDown()
        {
            Hide();
            Runtime.Network.BinaryMaster p = new Runtime.Network.BinaryMaster();
            p.writer.Write(8);
            SendReturn(p.Get());
        }
    }
}
