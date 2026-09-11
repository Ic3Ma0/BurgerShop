using UnityEngine;

namespace BurgerShop.UI
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        Rect lastArea;
        Vector2Int lastSize;
        void OnEnable() => Refresh();
        void Update() => Refresh();
        void Refresh()
        {
            var size = new Vector2Int(Screen.width, Screen.height);
            Rect area = Screen.safeArea;
            if (area == lastArea && size == lastSize) return;
            Apply(area, size.x, size.y);
            lastArea = area;
            lastSize = size;
        }

        public void Apply(Rect area, int width, int height)
        {
            if (width <= 0 || height <= 0) return;
            if (area.width <= 0f || area.height <= 0f) area = new Rect(0, 0, width, height);
            RectTransform rect = (RectTransform)transform;
            rect.anchorMin = new Vector2(Mathf.Clamp01(area.xMin / width), Mathf.Clamp01(area.yMin / height));
            rect.anchorMax = new Vector2(Mathf.Clamp01(area.xMax / width), Mathf.Clamp01(area.yMax / height));
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
