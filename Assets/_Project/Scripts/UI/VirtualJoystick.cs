using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BurgerShop.UI
{
    public sealed class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, ICancelHandler
    {
        [SerializeField] RectTransform knob;
        [SerializeField] float range = 110f;

        RectTransform _pad;
        Canvas _canvas;
        int? activePointer;

        public static Vector2 Value { get; private set; }

        void Awake()
        {
            _pad = (RectTransform)transform;
            _canvas = GetComponentInParent<Canvas>();
            if (knob == null && transform.childCount > 0)
                knob = transform.GetChild(0) as RectTransform;
        }

        void OnDisable() => Release();
        void OnApplicationPause(bool paused) { if (paused) Release(); }
        void OnApplicationFocus(bool focused) { if (!focused) Release(); }

        void Release()
        {
            activePointer = null;
            Value = Vector2.zero;
            ResetKnob();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (activePointer.HasValue) return;
            activePointer = eventData.pointerId;
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (activePointer != eventData.pointerId) return;
            Camera eventCamera = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? _canvas.worldCamera
                : null;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _pad, eventData.position, eventCamera, out Vector2 local))
                return;

            Vector2 clamped = Vector2.ClampMagnitude(local, range);
            if (knob != null)
                knob.anchoredPosition = clamped;

            Vector2 raw = clamped / range;
            Value = MapInput(raw);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (activePointer == eventData.pointerId) Release();
        }

        public static Vector2 MapInput(Vector2 raw)
        {
            float magnitude=Mathf.Min(1f,raw.magnitude);
            return magnitude <= .10f ? Vector2.zero : raw.normalized*((magnitude-.10f)/.90f);
        }
        public void OnCancel(BaseEventData eventData) => Release();
        public bool HasPointer => activePointer.HasValue;

        void ResetKnob()
        {
            if (knob != null)
                knob.anchoredPosition = Vector2.zero;
        }
    }
}
