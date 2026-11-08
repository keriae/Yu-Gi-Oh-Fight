using System;


using UnityEngine;
using UnityEngine.Playables;


namespace YuGiOhFight.Client.UI
{
    public class DoWhenPlayableDirectorStop : MonoBehaviour
    {
        public Action action;
        PlayableDirector director;
        void Start()
        {
            director = GetComponent<PlayableDirector>();
        }

        void Update()
        {
            if (director.state != PlayState.Playing)
            {
                action?.Invoke();
                enabled = false;
            }
        }
    }
}
