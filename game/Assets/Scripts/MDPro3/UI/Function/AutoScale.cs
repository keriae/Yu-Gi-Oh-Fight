using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MDPro3;

namespace MDPro3.UI
{
    public class AutoScale : MonoBehaviour
    {
        private float width;
        private float height;

        void Awake()
        {
            width = transform.localScale.x;
            height = transform.localScale.y;
            Scale();
            SystemEvent.OnResolutionChange += Scale;
        }

        private void Scale()
        {
            if (Screen.height == 0)
                return;
            float screenAspect = (float)Screen.width / Screen.height;
            if (screenAspect > 16 / 9f)
                transform.localScale = new Vector3(width * screenAspect * 9 / 16, height * screenAspect * 9 / 16, transform.localScale.z);
            else
                transform.localScale = new Vector3(width, height, transform.localScale.z);
            //transform.localScale = new Vector3(width * screenAspect * 9 / 16, height, 1f);
        }

        private void OnDestroy()
        {
            SystemEvent.OnResolutionChange -= Scale;
        }
    }
}

