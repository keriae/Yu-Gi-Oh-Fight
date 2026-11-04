using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using YgomSystem.ElementSystem;

namespace MDPro3.UI
{
    public class TimerHandler : MonoBehaviour
    {
        private bool duelStart;
        private bool duelEnd;
        private bool pause;
        private int timeLimit;
        private int m_time;
        private int Time
        {
            get { return m_time; }
            set
            {
                pastTime = 0;
                m_time = value;
            }
        }
        private int m_player = -1;
        public int Player
        {
            get { return m_player; }
            set
            {
                m_player = value;
                ChangePlayer();
            }
        }
        private float pastTime = 0;
        private ElementObjectManager manager;
        private TextMeshPro text;
        private Material material;

        public void SetTime(int player, int time)
        {
            Player = player;
            Time = time;
            pause = false;
        }

        public void SetTimeLimit(int limit)
        {
            timeLimit = limit;
        }

        public void Pause()
        {
            pause = true;
        }

        public void StopPause()
        {
            pause = false;
        }

        private void Awake()
        {
            manager = GetComponent<ElementObjectManager>();
            text = manager.GetElement<TextMeshPro>("Text");
            text.font = Program.instance.ui_.jpMenuTmpFont;
            material = manager.GetElement<Renderer>("Timer").materials[1];
            material.SetFloat("_AddTime", 0);
        }

        private void Update()
        {
            if (!duelStart || duelEnd || Player < 0)
                return;

            if (timeLimit == 0)
                DuelEnd();
            if (pastTime == 0f)
                text.text = Time.ToString();
            if (!pause || Player != 0)
            {
                pastTime += UnityEngine.Time.unscaledDeltaTime;
                int remainTime = Mathf.CeilToInt(Time - pastTime);
                text.text = remainTime.ToString();
                material.SetFloat("_MaxTime", Time - pastTime < 0 ? 0 : (Time - pastTime) / timeLimit);
            }
        }

        public void DuelStart()
        {
            text.gameObject.SetActive(true);
            Tools.PlayAnimation(transform, "StartToPhase1");
            duelStart = true;
            duelEnd = false;
        }

        public void DuelEnd()
        {
            text.gameObject.SetActive(false);
            duelEnd = true;
        }

        private void ChangePlayer()
        {
            if (Player == 0)
            {
                material.SetColor("_MaxTimeColor01", new Color(0.2117f, 0.6784f, 1f, 0f));
                material.SetColor("_MaxTimeColor02", new Color(0.1098f, 0.4745f, 1f, 0f));
            }
            else
            {
                material.SetColor("_MaxTimeColor01", new Color(1f, 0.2194f, 0.2117f, 0f));
                material.SetColor("_MaxTimeColor02", new Color(1f, 0.1362f, 0.1098f, 0f));
            }
        }
    }
}
