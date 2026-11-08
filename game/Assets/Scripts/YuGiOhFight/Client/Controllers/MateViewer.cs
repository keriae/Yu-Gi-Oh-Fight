using DG.Tweening;


using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;


using System.Text.RegularExpressions;


using Cysharp.Threading.Tasks;


namespace YuGiOhFight.Client.Controllers
{
    public class MateViewer : Servant
    {

        [HideInInspector] public UI.SelectionToggle_Mate lastSelectedMateItem;
        [HideInInspector] public Presentation.Mate mate;

        private Camera targetCamera;
        private Vector2 clickInPosition;
        private Vector3 mateAngel;
        private Vector3 matePosition;
        private float oSize;
        private float clickInTime;
        private bool clickInLeft;
        private bool clickInRight;

        #region Servant

        protected override bool ShowLine => false;
        public override int Depth => 1;

        public override void Initialize()
        {
            returnServant = Runtime.Program.instance.menu;
            base.Initialize();
            targetCamera = Runtime.Program.instance.camera_.cameraDuelOverlay2D;
            LoadSeData();
        }

        protected override void ApplyShowArrangement(int preDepth)
        {
            base.ApplyShowArrangement(preDepth);
            Runtime.Program.instance.camera_.light.SetActive(true);
            Runtime.Program.instance.camera_.light.transform.GetChild(0).localEulerAngles = new Vector3(123f, -28f, -40f);
            Runtime.Program.instance.camera_.light.transform.GetChild(1).localEulerAngles = new Vector3(-80f, -140f, 0f);
            Runtime.CameraManager.DuelOverlay2DPlus();
            CameraReset();
            Runtime.AudioManager.PlayBGM("BGM_OUT_TUTORIAL_2", 0.5f);
            Runtime.UserInput.SetMoveRepeatSpeed(2f);
        }

        protected override void ApplyHideArrangement(int preDepth)
        {
            base.ApplyHideArrangement(preDepth);
            Runtime.CameraManager.DuelOverlay2DMinus();
            Runtime.Program.instance.camera_.light.SetActive(false);
            Runtime.Program.instance.camera_.light.transform.GetChild(0).localEulerAngles = new Vector3(96f, -28f, -40f);
            Runtime.Program.instance.camera_.light.transform.GetChild(1).localEulerAngles = new Vector3(-15f, -45f, 0f);
            if (mate != null)
                Destroy(mate.gameObject);
            Runtime.AudioManager.ResetSESource();
            Runtime.AudioManager.PlaySE("SE_MENU_CANCEL");
            Runtime.AudioManager.PlayBGM(Runtime.AudioManager.BGM_MENU_MAIN);
            Runtime.UserInput.SetMoveRepeatSpeed(1f);
        }

        public override void PerFrameFunction()
        {
            if (!showing) return;
            if(NeedResponseInput())
            {
                if (Runtime.UserInput.MouseRightDown || Runtime.UserInput.WasCancelPressed)
                    OnReturn();
                if (Runtime.UserInput.WasGamepadButtonWestPressed)
                    GetUI<UI.ServantUI.MateViewerUI>().FocusOnInputField();
                if (mate == null)
                    return;

                if (Runtime.UserInput.WasGamepadButtonNorthPressed)
                    GetUI<UI.ServantUI.MateViewerUI>().OnMateTap();

                var leftOffset = (UI.PropertyOverride.PropertyOverrider.NeedMobileLayout() ? 532f : 432f) * Screen.height / 1080f;

                if (Runtime.UserInput.MouseLeftDown && Runtime.UserInput.MousePos.x > leftOffset)
                {
                    var widthOffset = Screen.width - leftOffset;

                    if (Runtime.UserInput.MousePos.x > leftOffset + widthOffset / 2f)
                    {
                        clickInRight = true;
                        clickInTime = Time.time;
                        mateAngel = mate.transform.eulerAngles;
                        clickInPosition = Runtime.UserInput.MousePos;
                        oSize = targetCamera.orthographicSize;
                    }
                    else
                    {
                        clickInLeft = true;
                        clickInTime = Time.time;
                        clickInPosition = Runtime.UserInput.MousePos;
                        matePosition = mate.transform.position;
                    }
                }
                if (Runtime.UserInput.MouseLeftPressing && clickInLeft)
                {
                    var x = matePosition.x + (clickInPosition.x - Runtime.UserInput.MousePos.x) * 0.01f;
                    var y = matePosition.y + (Runtime.UserInput.MousePos.y - clickInPosition.y) * 0.02f;
                    if (x > 10) x = 10;
                    if (x < -10) x = -10;
                    if (y > 0) y = 0;
                    if (y < -20) y = -20;
                    mate.transform.position = new Vector3(x, y, matePosition.z);
                }
                if (Runtime.UserInput.MouseLeftPressing && clickInRight)
                {
                    mate.transform.eulerAngles = mateAngel +
                        new Vector3(0, (clickInPosition.x - Runtime.UserInput.MousePos.x) * 0.2f, 0);
                    var size = oSize + (clickInPosition.y - Runtime.UserInput.MousePos.y) * 0.01f;
                    if (size < 5) size = 5;
                    if (size > 20) size = 20;
                    targetCamera.orthographicSize = size;
                    targetCamera.transform.localPosition = new Vector3(0, size * 0.95f, 200);
                }
                if (Runtime.UserInput.MouseLeftUp && (clickInLeft || clickInRight))
                {
                    clickInLeft = false;
                    clickInRight = false;
                    if ((Time.time - clickInTime) < 0.2f)
                    {
                        mate.Play(Presentation.Mate.MateAction.Tap);
                        mate.ActiveCamera(Presentation.Mate.MateAction.Tap, targetCamera.gameObject.layer);
                    }
                }

                if(Runtime.UserInput.LeftScrollWheel != Vector2.zero)
                {
                    var x = mate.transform.position.x - Runtime.UserInput.LeftScrollWheel.x * Time.unscaledDeltaTime * 50f;
                    if (x > 10) x = 10f;
                    if (x < -10) x = -10;
                    var y = mate.transform.position.y + Runtime.UserInput.LeftScrollWheel.y * Time.unscaledDeltaTime * 50f;
                    if (y > 0) y = 0;
                    if (y < -20) y = -20;
                    mate.transform.position = new Vector3(x, y, mate.transform.position.z);
                }
                if(Runtime.UserInput.RightScrollWheel != Vector2.zero)
                {
                    var x = Runtime.UserInput.RightScrollWheel.x * Time.unscaledDeltaTime * 200f;
                    mate.transform.Rotate(Vector3.up, -x);

                    if(Mathf.Abs( Runtime.UserInput.RightScrollWheel.y) > 0.3f)
                    {
                        var size = targetCamera.orthographicSize - Runtime.UserInput.RightScrollWheel.y * Time.unscaledDeltaTime * 30f;
                        if (size < 5) size = 5;
                        if (size > 20) size = 20;
                        targetCamera.orthographicSize = size;
                        targetCamera.transform.localPosition = new Vector3(0, size * 0.95f, 200);
                    }
                }
            }
        }

        public override void Select(bool forced = false)
        {
            if (!forced && !Runtime.UserInput.NeedDefaultSelect())
                return;
            if (lastSelectedMateItem == null)
                return;

            lastSelectedMateItem.GetSelectable().Select();
        }

        #endregion

        public void LoadMates()
        {
            if (servantUI == null)
                return;

            GetUI<UI.ServantUI.MateViewerUI>().Load();
        }

        public void SelectLastMateItem()
        {
            Runtime.UserInput.NextSelectionIsAxis = true;
            Select();
        }

        private void CameraReset()
        {
            targetCamera.transform.localPosition =
                new Vector3(0, 20 * 0.95f, 200);
            targetCamera.transform.localEulerAngles =
                new Vector3(0, 180, 0);
            targetCamera.orthographicSize = 20;
        }

        public void ViewMate(int code)
        {
            if (mate != null)
            {
                if (mate.code == code)
                    return;
                else
                    Destroy(mate.gameObject);
            }
            _ = LoadMateAsync(code);
        }

        private async UniTask LoadMateAsync(int code)
        {
            mate = await Runtime.ABLoader.LoadMateAsync(code);
            Utility.Tools.ChangeLayer(mate.gameObject, targetCamera.gameObject.layer);
            await UniTask.WaitForSeconds(0.1f);
            Runtime.AudioManager.ResetSESource();
            mate.gameObject.SetActive(true);
            mate.Play(Presentation.Mate.MateAction.Entry);
            mate.ActiveCamera(Presentation.Mate.MateAction.Entry, targetCamera.gameObject.layer);
            CameraReset();
        }

        #region CrossDuel Se Label Data

        private static readonly List<CrossDuelSeLabelData> cdSeData = new();

        public struct CrossDuelSeLabelData
        {
            public string name;
            public string label1;
            public float start1;
            public string label2;
            public float start2;
            public string label3;
            public float start3;
        }

        private void LoadSeData()
        {
            var handle = Addressables.LoadAssetAsync<TextAsset>("EffectSeLabelData");
            handle.Completed += (result) =>
            {
                var content = handle.Result.text;
                var lines = Regex.Split(content, Runtime.Program.STRING_LINE_BREAK);
                for(int i = 1; i < lines.Length; i++)
                {
                    var splits = lines[i].Split(',');
                    if (splits.Length == 7)
                    {
                        CrossDuelSeLabelData seData = new CrossDuelSeLabelData();
                        seData.name = splits[0];
                        seData.label1 = splits[1];
                        if (string.IsNullOrEmpty(splits[2]))
                            seData.start1 = 0;
                        else
                            seData.start1 = float.Parse(splits[2]);
                        seData.label2 = splits[3];
                        if (string.IsNullOrEmpty(splits[4]))
                            seData.start2 = 0;
                        else
                            seData.start2 = float.Parse(splits[4]);
                        seData.label3 = splits[5];
                        if (string.IsNullOrEmpty(splits[6]))
                            seData.start3 = 0;
                        else
                            seData.start3 = float.Parse(splits[6]);
                        cdSeData.Add(seData);
                    }
                }
            };
        }

        public static void PlayCrossDuelSe(string name)
        {
            CrossDuelSeLabelData data = new();
            bool found = false;
            foreach(CrossDuelSeLabelData seData in cdSeData)
                if (seData.name == name)
                {
                    data = seData;
                    found = true;
                }
            if (!found)
                return;
            if (!string.IsNullOrEmpty(data.label1))
            {
                DOTween.To(v => { }, 0, 0, data.start1).OnComplete(() =>
                {
                    Runtime.AudioManager.PlaySE(data.label1.ToUpper());
                });
            }
            if (!string.IsNullOrEmpty(data.label2))
            {
                DOTween.To(v => { }, 0, 0, data.start2).OnComplete(() =>
                {
                    Runtime.AudioManager.PlaySE(data.label2.ToUpper());
                });
            }
            if (!string.IsNullOrEmpty(data.label3))
            {
                DOTween.To(v => { }, 0, 0, data.start3).OnComplete(() =>
                {
                    Runtime.AudioManager.PlaySE(data.label3.ToUpper());
                });
            }
        }

        #endregion

    }
}
