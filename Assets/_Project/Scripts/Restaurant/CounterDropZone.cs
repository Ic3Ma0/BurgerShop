using BurgerShop.Player;
using UnityEngine;

namespace BurgerShop.Restaurant
{
    public sealed class CounterDropZone : MonoBehaviour
    {
        CounterStock stock;
        Transform dropPoint;
        [SerializeField, Min(0.1f)] float radius = 1.05f;
        [SerializeField, Min(0.05f)] float dropInterval = 0.25f;
        bool boxed;
        KitchenProduct product = KitchenProduct.Burger;
        float cooldown;

        public CounterStock Stock => stock;
        public Vector3 DropPosition => dropPoint != null ? dropPoint.position : transform.position;
        public bool AcceptsBoxes => boxed;
        public KitchenProduct Product => product;

        public void Configure(CounterStock counter, Transform point, float dropRadius = 1.05f, float interval = 0.25f,
            bool acceptBoxes = false, KitchenProduct kind = KitchenProduct.Burger)
        {
            stock = counter;
            dropPoint = point;
            radius = Mathf.Max(0.1f, dropRadius);
            dropInterval = Mathf.Max(0.05f, interval);
            boxed = acceptBoxes;
            product = kind;
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
                || HeldMatching(carrier) <= 0
                || !IsInRangeOf(carrier.transform)) return false;
            bool placed = boxed ? stock.TryPlaceBoxedFrom(carrier)
                : product == KitchenProduct.Cola ? stock.TryPlaceColaFrom(carrier)
                : stock.TryPlaceFrom(carrier);
            if (!placed) return false;
            cooldown = dropInterval;
            return true;
        }

        int HeldMatching(BurgerInventory carrier)
        {
            if (carrier == null) return 0;
            if (boxed) return carrier.BoxedCount;
            if (product == KitchenProduct.Cola) return carrier.ColaCount;
            return carrier.LooseCount;
        }
    }
}
