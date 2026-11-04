using Cysharp.Threading.Tasks;
using DG.Tweening;
using KonamiCommonIAB;
using MDPro3.Servant;
using MDPro3.UI;
using MDPro3.Utility;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace MDPro3
{
    public class MessageManager : Manager
    {
        public GameObject messageCast;
        public GameObject messageToast;
        public GameObject messageCard;

        static MessageManager instance;
        static List<GameObject> items = new List<GameObject>();
        static readonly float transitionTime = 0.3f;
        static readonly float existTime = 3f;

        public static string messageFromSubString = string.Empty;

        public override void Initialize()
        {
            base.Initialize();
            instance = this;
            var handle = Addressables.LoadAssetAsync<GameObject>("UI/MessageCast.prefab");
            handle.Completed += (result) =>
            {
                messageCast = result.Result;
            };
            handle = Addressables.LoadAssetAsync<GameObject>("UI/MessageToast.prefab");
            handle.Completed += (result) =>
            {
                messageToast = result.Result;
            };
            handle = Addressables.LoadAssetAsync<GameObject>("UI/MessageCard.prefab");
            handle.Completed += (result) =>
            {
                messageCard = result.Result;
            };
        }
        public override void PerFrameFunction()
        {
            base.PerFrameFunction();
            if(messageFromSubString != string.Empty)
            {
                Cast(messageFromSubString);
                messageFromSubString= string.Empty;
            }
        }

        public void CastCard(int code)
        {
            CameraManager.UIBlurPlus();
            var item = Instantiate(Program.instance.message_.messageCard);
            DOTweenLifecycle.AttachKillOnDestroy(item);
            Program.instance.ocgcore.AllGameObjects.Add(item);
            item.transform.SetParent(instance.transform, false);
            _ = RefreshAsync(item, code);
        }

        private async UniTask RefreshAsync(GameObject item, int code)
        {
            var ri = item.GetComponent<RawImage>();
            ri.texture = await CardImageLoader.LoadCardAsync(code, false, ri.destroyCancellationToken);
            ri.material = MaterialLoader.GetCardMaterial(code);

            var rect = item.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(200, -160);
            rect.DOAnchorPosX(-50f, transitionTime);
            DOTween.To(v => { }, 0, 0, transitionTime + existTime).OnComplete(() =>
            {
                rect.DOAnchorPosX(200f, transitionTime); // TODO: tween in tween
            });
            DOTween.To(v => { }, 0, 0, existTime + transitionTime * 2).OnComplete(() =>
            {
                Destroy(item);
                CameraManager.UIBlurMinus();
            });
        }

        static List<string> cachedMessage = new List<string>();

        public static void Toast(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;
            CameraManager.UIBlurPlus();
            var item = Instantiate(Program.instance.message_.messageToast);
            DOTweenLifecycle.AttachKillOnDestroy(item);
            item.transform.SetParent(instance.transform, false);
            item.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = message;
            Destroy(item, 2f);
            DOTween.To(v => { }, 0f, 0f, 2f).OnComplete(() =>
            {
                Destroy(item);
                CameraManager.UIBlurMinus();
            });
        }

        public static void Cast(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;
            if (items.Count > 10)
                return;
            if (Program.instance.message_.messageCast == null)
            {
                cachedMessage.Add(message);
                return;
            }

            if(cachedMessage.Count > 0)
            {
                var ms = new List<string>(cachedMessage);
                cachedMessage.Clear();
                foreach(var m in ms)
                    Cast(m);
            }

            CameraManager.UIBlurPlus();
            var item = Instantiate(Program.instance.message_.messageCast);
            DOTweenLifecycle.AttachKillOnDestroy(item);
            item.transform.SetParent(instance.transform, false);
            item.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = message;
            RectTransform rect = item.GetComponent<RectTransform>();
            int id = items.Count;
            items.Add(item);
            rect.anchoredPosition = new Vector2(900, -160 - id * 120);
            rect.DOAnchorPosX(-10f, transitionTime);
            DOTween.To(v => { }, 0, 0, existTime + id).OnComplete(() =>
            {
                rect.DOAnchorPosX(900f, transitionTime); // TODO: tween in tween
            });
            DOTween.To(v => { }, 0, 0, existTime + transitionTime + id).OnComplete(() =>
            {
                items.Remove(item);
                Destroy(item);
                MoveUp();
                CameraManager.UIBlurMinus();
            });
        }

        static void MoveUp()
        {
            foreach (var item in items)
            {
                var rect = item.GetComponent<RectTransform>();
                rect.DOAnchorPosY(rect.anchoredPosition.y + 120f, transitionTime);
            }
        }
    }
}
