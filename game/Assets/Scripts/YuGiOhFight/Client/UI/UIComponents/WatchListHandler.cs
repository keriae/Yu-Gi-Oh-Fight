using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;


namespace YuGiOhFight.Client.UI
{
    public class WatchListHandler : MonoBehaviour
    {
        public ScrollRect scrollRect;
        public TMP_InputField inputSearch;

        public SuperScrollView superScrollView;
        private List<Services.MyCardRoom> rooms;
        private readonly List<string[]> tasks = new();

        private void OnEnable()
        {
            Print();
        }

        private void OnDisable()
        {
            Clear();
        }

        private List<string[]> GetSearchedTasks()
        {
            var returnValue = new List<string[]>();
            foreach (var task in tasks)
                if (task[2].Contains(inputSearch.text) || task[3].Contains(inputSearch.text))
                    returnValue.Add(task);
            return returnValue;
        }

        public void SetRooms(List<Services.MyCardRoom> rooms)
        {
            this.rooms = rooms;
            tasks.Clear();
            foreach (Services.MyCardRoom room in rooms)
            {
                var task = new string[18]
                {
                    room.id,
                    room.title,
                    room.users[0].username,
                    room.users[1].username,
                    room.arena,

                    room.options.lflist.ToString(),
                    room.options.rule.ToString(),
                    room.options.mode.ToString(),
                    room.options.duel_rule.ToString(),
                    room.options.no_check_deck ? "T" : "F",
                    room.options.no_shuffle_deck ? "T" : "F",
                    room.options.start_lp.ToString(),
                    room.options.start_hand.ToString(),
                    room.options.draw_count.ToString(),
                    room.options.time_limit.ToString(),
                    room.options.no_watch ? "T" : "F",
                    room.options.auto_death ? "T" : "F",
                    room.options.replay_mode.ToString()
                };
                tasks.Add(task);
            }

            Print();
        }

        public void CreateRoom(Services.MyCardRoom room)
        {
            var task = new string[18]
            {
                    room.id,
                    room.title,
                    room.users[0].username,
                    room.users[1].username,
                    room.arena,

                    room.options.lflist.ToString(),
                    room.options.rule.ToString(),
                    room.options.mode.ToString(),
                    room.options.duel_rule.ToString(),
                    room.options.no_check_deck ? "T" : "F",
                    room.options.no_shuffle_deck ? "T" : "F",
                    room.options.start_lp.ToString(),
                    room.options.start_hand.ToString(),
                    room.options.draw_count.ToString(),
                    room.options.time_limit.ToString(),
                    room.options.no_watch ? "T" : "F",
                    room.options.auto_death ? "T" : "F",
                    room.options.replay_mode.ToString()
            };
            tasks.Add(task);
            if (superScrollView != null && gameObject.activeInHierarchy)
                superScrollView.Add(task);
        }
        public void UpdateRoom(Services.MyCardRoom room)
        {
            var task = new string[18]
            {
                    room.id,
                    room.title,
                    room.users[0].username,
                    room.users[1].username,
                    room.arena,

                    room.options.lflist.ToString(),
                    room.options.rule.ToString(),
                    room.options.mode.ToString(),
                    room.options.duel_rule.ToString(),
                    room.options.no_check_deck ? "T" : "F",
                    room.options.no_shuffle_deck ? "T" : "F",
                    room.options.start_lp.ToString(),
                    room.options.start_hand.ToString(),
                    room.options.draw_count.ToString(),
                    room.options.time_limit.ToString(),
                    room.options.no_watch ? "T" : "F",
                    room.options.auto_death ? "T" : "F",
                    room.options.replay_mode.ToString()
            };
            for(int i = 0; i < tasks.Count; i++)
            {
                if (tasks[i][0] == task[0])
                {
                    for (var j = 1; j < task.Length; j++)
                        tasks[i][j] = task[j];
                    if (superScrollView != null && gameObject.activeInHierarchy)
                        superScrollView.UpdateAt(i, task);
                    break;
                }
            }
        }
        public void DeleteRoom(string roomId)
        {
            for (var i = 0; i < rooms.Count; i++)
            {
                if (rooms[i].id == roomId)
                {
                    rooms.Remove(rooms[i]);
                    if (superScrollView != null && gameObject.activeInHierarchy)
                        superScrollView.RemoveAt(i);
                    break;
                }
            }
        }

        public void Print()
        {
            if (!gameObject.activeInHierarchy)
                return;
            superScrollView?.Clear();
            var handle = Addressables.LoadAssetAsync<GameObject>("UI/ItemWatch.prefab");
            handle.Completed += (result) =>
            {
                var itemWidth = PropertyOverride.PropertyOverrider.NeedMobileLayout() ? 370f : 300f;
                var itemHeight = PropertyOverride.PropertyOverrider.NeedMobileLayout() ? 178f : 140f;
                var topPadding = PropertyOverride.PropertyOverrider.NeedMobileLayout() ? 18f : 14f;
                var bottomPadding = PropertyOverride.PropertyOverrider.NeedMobileLayout() ? 64f : 54f;

                superScrollView = new SuperScrollView
                (
                    -1,
                    itemWidth,
                    itemHeight,
                    topPadding,
                    bottomPadding,
                    result.Result,
                    ItemOnListRefresh,
                    scrollRect
                    );
                superScrollView.Print(GetSearchedTasks());
                if(superScrollView.gameObjects.Count > 0)
                {
                    Runtime.Program.instance.online.lastSelectedWatchItem = superScrollView.gameObjects[0].GetComponent<SelectionToggle_Watch>();
                }
            };
        }

        private void ItemOnListRefresh(string[] task, GameObject item)
        {
            SelectionToggle_Watch handler = item.GetComponent<SelectionToggle_Watch>();
            handler.roomId = task[0];
            handler.roomTitile = task[1];
            handler.player0Name = task[2];
            handler.player1Name = task[3];
            handler.arena = task[4];

            handler.options.lflist = int.Parse(task[5]);
            handler.options.rule = int.Parse(task[6]);
            handler.options.mode = int.Parse(task[7]);
            handler.options.duel_rule = int.Parse(task[8]);
            handler.options.no_check_deck = task[9] == "T";
            handler.options.no_shuffle_deck = task[10] == "T";
            handler.options.start_lp = int.Parse(task[11]);
            handler.options.start_hand = int.Parse(task[12]);
            handler.options.draw_count = int.Parse(task[13]);
            handler.options.time_limit = int.Parse(task[14]);
            handler.options.no_watch = task[15] == "T";
            handler.options.auto_death = task[16] == "T";
            handler.options.replay_mode = int.Parse(task[17]);

            handler.Refresh();
        }

        public void Clear()
        {
            superScrollView?.Clear();
        }
    }
}
