using System;
using System.Collections;
using UnityEngine;

namespace MDPro3.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class ConditionalAlignmentController : MonoBehaviour
    {
        [Serializable]
        public struct AlignmentPreset
        {
            [Tooltip("锚点最小值（左下角）")]
            public Vector2 anchorMin;
            [Tooltip("锚点最大值（右上角）")]
            public Vector2 anchorMax;
            [Tooltip("相对锚点的偏移量")]
            public Vector2 anchoredPosition;
        }

        [Header("子节点配置")]
        [SerializeField] private RectTransform targetChild;

        [Header("布局参数")]
        [SerializeField, Tooltip("触发布局变化的宽度阈值")]
        private float widthThreshold = 512f;

        [Header("宽屏模式布局")]
        [SerializeField]
        private AlignmentPreset wideScreenPreset = new()
        {
            anchorMin = new Vector2(0.5f, 0.5f),
            anchorMax = new Vector2(0.5f, 0.5f),
            anchoredPosition = Vector2.zero
        };

        [Header("窄屏模式布局")]
        [SerializeField]
        private AlignmentPreset narrowScreenPreset = new AlignmentPreset
        {
            anchorMin = new Vector2(0f, 0.5f),
            anchorMax = new Vector2(0f, 0.5f),
            anchoredPosition = new Vector2(256f, 0)
        };

        private RectTransform _parentRect;

        private void Awake() => Initialize();

        //private void OnValidate() => Initialize();

        private void OnEnable()
        {
            SystemEvent.OnResolutionChange += Initialize;
        }

        private void OnDisable()
        {
            SystemEvent.OnResolutionChange -= Initialize;
        }


        private void Initialize()
        {
            if (_parentRect == null)
                _parentRect = GetComponent<RectTransform>();

            StartCoroutine(UpdateChildAlignment());
        }

        private IEnumerator UpdateChildAlignment()
        {
            if (targetChild == null) yield break;

            while (_parentRect.rect.width < 0f)
                yield return null;
            var preset = _parentRect.rect.width > 
                widthThreshold ? wideScreenPreset : narrowScreenPreset;
            targetChild.anchorMin = preset.anchorMin;
            targetChild.anchorMax = preset.anchorMax;
            targetChild.anchoredPosition = preset.anchoredPosition;
        }
    }
}