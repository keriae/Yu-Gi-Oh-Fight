using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MDPro3.UI 
{
    public class PopupRockPaperScissors : PopupBase
    {
        [Header("Popup Rock Paper Scissors Reference")]
        public Button rock;
        public Button paper;
        public Button scissors;

        private void Start()
        {
            rock.onClick.AddListener(() => { TcpHelper.CtosMessage_HandResult(2); Hide(); });
            paper.onClick.AddListener(() => { TcpHelper.CtosMessage_HandResult(3); Hide(); });
            scissors.onClick.AddListener(() => { TcpHelper.CtosMessage_HandResult(1); Hide(); });
            _ = LoadAsync();
        }

        private async UniTask LoadAsync()
        {
            rock.GetComponent<RawImage>().texture = await TextureManager.LoadPicFromFileAsync(Program.PATH_DIY + "Rock.png");
            rock.GetComponent<RawImage>().color = Color.white;
            paper.GetComponent<RawImage>().texture = await TextureManager.LoadPicFromFileAsync(Program.PATH_DIY + "Paper.png");
            paper.GetComponent<RawImage>().color = Color.white;
            scissors.GetComponent<RawImage>().texture = await TextureManager.LoadPicFromFileAsync(Program.PATH_DIY + "Scissors.png");
            scissors.GetComponent<RawImage>().color = Color.white;
        }
    }
}

