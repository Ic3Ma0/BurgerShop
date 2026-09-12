using BurgerShop.Player;
using UnityEngine;

namespace BurgerShop.Restaurant
{
    public sealed class CounterDropZone : MonoBehaviour
    {
        CounterStock stock;
        Transform dropPoint;
        [SerializeField, Min(0.1f)] float radius = 1.05f;
        [SerializeField, Min(0.05f)] float dropInterval = 0.35f;
        bool boxed;
        float cooldown;

        public CounterStock Stock => stock;
        public Vector3 DropPosition => dropPoint != null ? dropPoint.position : transform.position;
        public bool AcceptsBoxes => boxed;

        public void Configure(CounterStock counter, Transform point, float dropRadius = 1.05f, float interval = 0.25f,
            bool acceptBoxes = false)
        {
            stock = counter;
            dropPoint = point;
            radius = Mathf.Max(0.1f, dropRadius);
            dropInterval = Mathf.Max(0.35f, interval);
            boxed = acceptBoxes;
            cooldown = 0f;
        }

        public bool IsInRangeOf(Transform actor)
        {
            if (actor == null) return false;
            Vector3 offset = actor.position - DropPosition;
            offset.y = 0f;
            return offset.sqrMagnitude <= radius * radius;
        }

        public void Advance(float deltaTime)
        {
            if (deltaTime <= 0f) return;
            cooldown = Mathf.Max(0f, cooldown - deltaTime);
        }

        public bool TryDepositFrom(BurgerInventory carrier)
        {
            if (!isActiveAndEnabled || cooldown > 0f || stock == null || !stock.isActiveAndEnabled
                || carrier == null || !carrier.isActiveAndEnabled
                || (boxed ? carrier.BoxedCount : carrier.LooseCount) <= 0
                || !IsInRangeOf(carrier.transform)) return false;
            if (boxed ? !stock.TryPlaceBoxedFrom(carrier) : !stock.TryPlaceFrom(carrier)) return false;
            cooldown = dropInterval;
            return true;
        }
    }
}
