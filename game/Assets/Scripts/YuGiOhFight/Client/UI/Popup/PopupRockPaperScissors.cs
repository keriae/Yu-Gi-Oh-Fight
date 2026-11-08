using Cysharp.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;


namespace YuGiOhFight.Client.UI.Popup
{
    public class PopupRockPaperScissors : Popup
    {

        protected override void Initialize()
        {
            base.Initialize();
            cancelCallHide = false;
            Manager.GetElement<Button>("RockButton").onClick.AddListener(() => { Runtime.Network.TcpHelper.CtosMessage_HandResult(2); Hide(); });
            Manager.GetElement<Button>("PaperButton").onClick.AddListener(() => { Runtime.Network.TcpHelper.CtosMessage_HandResult(3); Hide(); });
            Manager.GetElement<Button>("ScissorsButton").onClick.AddListener(() => { Runtime.Network.TcpHelper.CtosMessage_HandResult(1); Hide(); });
        }

        private void Start()
        {
            _ = LoadAsync();
        }

        private async UniTask LoadAsync()
        {
            var pic = await Runtime.TextureManager.LoadPicFromFileAsync(Runtime.Program.PATH_DIY + "Rock.png");
            if (pic != null)
            {
                var rock = Manager.GetElement<RawImage>("RockButton");
                rock.texture = pic;
                rock.color = Color.white;
            }

            pic = await Runtime.TextureManager.LoadPicFromFileAsync(Runtime.Program.PATH_DIY + "Paper.png");
            if (pic != null)
            {
                var paper = Manager.GetElement<RawImage>("PaperButton");
                paper.texture = pic;
                paper.color = Color.white;
            }

            pic = await Runtime.TextureManager.LoadPicFromFileAsync(Runtime.Program.PATH_DIY + "Scissors.png");
            if (pic != null)
            {
                var scissors = Manager.GetElement<RawImage>("ScissorsButton");
                scissors.texture = pic;
                scissors.color = Color.white;
            }
        }
    }
}
