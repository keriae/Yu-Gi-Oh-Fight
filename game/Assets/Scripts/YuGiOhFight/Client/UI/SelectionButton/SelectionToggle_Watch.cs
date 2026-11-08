using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

using System.Collections.Generic;
using System.Text;

using UnityEngine.EventSystems;


using Cysharp.Threading.Tasks;


namespace YuGiOhFight.Client.UI
{
    public class SelectionToggle_Watch : SelectionToggle_ScrollRectItem
    {
        #region Elements

        private TextMeshProUGUI m_Player0Label;
        private TextMeshProUGUI Player0Label =>
            m_Player0Label = m_Player0Label != null ? m_Player0Label
            : Manager.GetElement<TextMeshProUGUI>("Player0Name");

        private TextMeshProUGUI m_Player1Label;
        private TextMeshProUGUI Player1Label =>
            m_Player1Label = m_Player1Label != null ? m_Player1Label
            : Manager.GetElement<TextMeshProUGUI>("Player1Name");

        #endregion


        [Header("SelectionToggle Watch")]


        public string roomId;
        public string roomTitile;
        public string player0Name;
        public string player1Name;
        public string arena;
        public Services.MyCardRoomOptions options = new();

        public override void Refresh()
        {
            base.Refresh();
            Player0Label.text = GetSafeDisplayName(player0Name, Player0Label);
            Player1Label.text = GetSafeDisplayName(player1Name, Player1Label);
        }

        private static string GetSafeDisplayName(string name, TextMeshProUGUI label)
        {
            if (string.IsNullOrEmpty(name))
                return string.Empty;
            if (label == null || label.font == null)
                return name;

            if (label.font.HasCharacters(name, out uint[] missingCharacters, true, true))
                return name;

            if (missingCharacters == null || missingCharacters.Length == 0)
                return name;

            var missingSet = new HashSet<uint>(missingCharacters);
            var builder = new StringBuilder(name.Length);
            for (int i = 0; i < name.Length; i++)
            {
                var code = name[i];
                builder.Append(missingSet.Contains(code) ? '?' : name[i]);
            }
            return builder.ToString();
        }

        protected override async UniTask RefreshAsync()
        {
            refreshed = false;
            var cancellationToken = cts?.Token ?? destroyCancellationToken;
            var face0 = Manager.GetElement<RawImage>("Face0");
            var face1 = Manager.GetElement<RawImage>("Face1");

            if (face0 != null)
                face0.texture = Controllers.Appearance.defaultFace0.texture;
            if (face1 != null)
                face1.texture = Controllers.Appearance.defaultFace1.texture;

            try
            {
                var avatar0 = await Services.MyCard.GetAvatarAsync(player0Name, cancellationToken);
                if (!cancellationToken.IsCancellationRequested && face0 != null)
                    face0.texture = avatar0;

                var avatar1 = await Services.MyCard.GetAvatarAsync(player1Name, cancellationToken);
                if (!cancellationToken.IsCancellationRequested && face1 != null)
                    face1.texture = avatar1;

                refreshed = true;
            }
            catch (OperationCanceledException)
            {
            }
            catch (MissingReferenceException)
            {
            }
        }

        protected override void CallToggleOnEvent()
        {
            base.CallToggleOnEvent();
            Runtime.Program.instance.online.lastSelectedWatchItem = this;
        }

        protected override void CallSubmitEvent()
        {
            Runtime.AudioManager.PlaySE("SE_MENU_DECIDE");
            base.CallSubmitEvent();
            _ = WaitPasswordToJoin();
        }

        private async UniTask WaitPasswordToJoin()
        {
            var password = await Services.MyCard.GetJoinRoomPassword(options, roomId, Services.MyCard.account.user.id);
            Runtime.Network.TcpHelper.LinkStart(Services.MyCard.duelUrl, Services.MyCard.account.user.username, Services.MyCard.athleticPort.ToString(), password, false, null);
        }

        protected override void OnClick()
        {
            Runtime.Program.instance.online.lastSelectedWatchItem = this;
            CallSubmitEvent();
        }

        protected override void ToggleOn()
        {
            isOn = true;
        }
        public override void ToggleOnNow()
        {
            isOn = true;
        }
        protected override void ToggleOff()
        {
            isOn = false;
        }
        public override void ToggleOffNow()
        {
            isOn = false;
        }

        protected override void OnNavigation(AxisEventData eventData)
        {
            var selfIndex = index;
            if (selfIndex < 0)
                selfIndex = transform.GetSiblingIndex();

            var count = Runtime.Program.instance.online.GetUI<ServantUI.OnlineServantUI>()
                .PageMyCard.WatchList.superScrollView.items.Count;
            var columes = Runtime.Program.instance.online.GetUI<ServantUI.OnlineServantUI>()
                .PageMyCard.WatchList.superScrollView.GetColumnCount();

            var targetIndex = selfIndex + 1;

            if (eventData.moveDir == MoveDirection.Left)
            {
                if (selfIndex % columes == 0)
                {
                    Runtime.Program.instance.online.GetUI<ServantUI.OnlineServantUI>()
                        .PageMyCard.ButtonDeckSelector.GetSelectable().Select();
                    return;
                }
                targetIndex = index - 1;
            }
            else if (eventData.moveDir == MoveDirection.Right)
            {
                if (selfIndex % columes == columes - 1 || index == count - 1)
                {
                    Runtime.Program.instance.online.GetUI<ServantUI.OnlineServantUI>().PageMyCard.SelectDefault();
                    return;
                }
            }
            else if (eventData.moveDir == MoveDirection.Up)
                targetIndex = selfIndex - columes;
            else if (eventData.moveDir == MoveDirection.Down)
            {
                var lastLineLeft = count % columes;
                var bound = count - lastLineLeft - 1;
                if (lastLineLeft == 0)
                    bound -= columes;
                if (selfIndex > bound)
                    return;
                targetIndex = index + columes;
            }

            if (targetIndex < 0)
                return;
            if (targetIndex >= count)
                targetIndex = count - 1;
            if (targetIndex == index)
                return;

            for (int i = 0; i < transform.parent.childCount; i++)
            {
                var child = transform.parent.GetChild(i);
                if (!child.gameObject.activeSelf)
                    continue;

                var buttonIndex = child.GetComponent<SelectionButton>().index;
                if (buttonIndex < 0)
                    buttonIndex = i;

                if (buttonIndex == targetIndex)
                {
                    Runtime.UserInput.NextSelectionIsAxis = true;
                    EventSystem.current.SetSelectedGameObject(transform.parent.GetChild(i).gameObject);
                    break;
                }
            }

        }
    }
}
