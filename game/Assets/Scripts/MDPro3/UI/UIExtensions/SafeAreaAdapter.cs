using UnityEngine;
using UnityEngine.UI;

namespace MDPro3.UI
{
    public class SafeAreaAdapter : MonoBehaviour
    {
        public enum Mode
        {
            None = 0,
            SafeArea = 1,
            MinOnly = 2,
            MaxOnly = 3,
            MinOutside = 4,
            MaxOutside = 5
        }

        [SerializeField]
        private Mode verticalMode = Mode.SafeArea;

        [SerializeField]
        private Mode horizontalMode = Mode.SafeArea;

        private RectTransform _rectTransform;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            SystemEvent.OnSafeAreaUpdate += ApplySafeArea;
            ApplySafeArea();
        }

        private void OnDestroy()
        {
            SystemEvent.OnSafeAreaUpdate -= ApplySafeArea;
        }

        private void ApplySafeArea()
        {
            if (_rectTransform == null)
            {
                Debug.LogError("RectTransform is null");
                return;
            }

            Rect safeArea = Screen.safeArea;
            if (Screen.height == 0 || safeArea.width == 0 || safeArea.height == 0)
                return;

            var width = Screen.width * 1080 / Screen.height;

            float minX = safeArea.position.x * width / safeArea.width;
            float maxX = (safeArea.position.x + safeArea.width - Screen.width) * width / safeArea.width;
            float minY = safeArea.position.y * 1080f / safeArea.height;
            float maxY = (safeArea.position.y + safeArea.height - Screen.height) * 1080f / safeArea.height;

            Vector2 finalOffsetMin = Vector2.zero;
            Vector2 finalOffsetMax = Vector2.zero;

            switch (horizontalMode)
            {
                case Mode.None:
                    finalOffsetMin.x = 0;
                    finalOffsetMax.x = 0;
                    break;
                case Mode.SafeArea:
                    finalOffsetMin.x = minX;
                    finalOffsetMax.x = maxX;
                    break;
                case Mode.MinOnly:
                    finalOffsetMin.x = minX;
                    finalOffsetMax.x = 0;
                    break;
                case Mode.MaxOnly:
                    finalOffsetMin.x = 0;
                    finalOffsetMax.x = maxX;
                    break;
                case Mode.MinOutside:
                    finalOffsetMin.x = -minX;
                    finalOffsetMax.x = maxX;
                    break;
                case Mode.MaxOutside:
                    finalOffsetMin.x = minX;
                    finalOffsetMax.x = -maxX;
                    break;
                default:
                    finalOffsetMin.x = minX;
                    finalOffsetMax.x = maxX;
                    break;
            }

            switch (verticalMode)
            {
                case Mode.None:
                    finalOffsetMin.y = 0;
                    finalOffsetMax.y = 0;
                    break;
                case Mode.SafeArea:
                    finalOffsetMin.y = minY;
                    finalOffsetMax.y = maxY;
                    break;
                case Mode.MinOnly:
                    finalOffsetMin.y = minY;
                    finalOffsetMax.y = 0;
                    break;
                case Mode.MaxOnly:
                    finalOffsetMin.y = 0;
                    finalOffsetMax.y = maxY;
                    break;
                case Mode.MinOutside:
                    finalOffsetMin.y = -minY;
                    finalOffsetMax.y = maxY;
                    break;
                case Mode.MaxOutside:
                    finalOffsetMin.y = minY;
                    finalOffsetMax.y = -maxY;
                    break;
                default:
                    finalOffsetMin.y = minY;
                    finalOffsetMax.y = maxY;
                    break;
            }

            _rectTransform.offsetMin = finalOffsetMin;
            _rectTransform.offsetMax = finalOffsetMax;
        }

        public static float GetSafeAreaRightOffset()
        {
            return (Screen.width - (Screen.safeArea.x + Screen.safeArea.width)) * Screen.height / 1080f;
        }

        public static float GetSafeAreaLeftOffset() 
        {
            return Screen.safeArea.x * Screen.height / 1080f;
        }
    }
}
