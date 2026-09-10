using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BurgerShop.UI
{
    public sealed class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] RectTransform knob;
        [SerializeField] float range = 110f;
        [SerializeField] float deadzone = 0.08f;

        RectTransform _pad;
        Canvas _canvas;

        public static Vector2 Value { get; private set; }

        void Awake()
        {
            _pad = (RectTransform)transform;
            _canvas = GetComponentInParent<Canvas>();
            if (knob == null && transform.childCount > 0)
                knob = transform.GetChild(0) as RectTransform;
        }

        void OnDisable()
        {
            Value = Vector2.zero;
            ResetKnob();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
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
            Value = raw.magnitude < deadzone ? Vector2.zero : Vector2.ClampMagnitude(raw, 1f);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Value = Vector2.zero;
            ResetKnob();
        }

        void ResetKnob()
        {
            if (knob != null)
                knob.anchoredPosition = Vector2.zero;
        }
    }
}
