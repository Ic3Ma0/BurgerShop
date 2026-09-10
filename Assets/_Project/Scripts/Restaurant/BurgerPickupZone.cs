using BurgerShop.Player;
using UnityEngine;

namespace BurgerShop.Restaurant
{
    public sealed class BurgerPickupZone : MonoBehaviour
    {
        [SerializeField] ProductionStation station;
        [SerializeField] BurgerInventory inventory;
        [SerializeField] Transform pickupPoint;
        [SerializeField, Min(0.1f)] float radius = 1f;
        [SerializeField, Min(0.05f)] float pickupInterval = 0.25f;
        float cooldown;

        public Vector3 PickupPosition => pickupPoint != null ? pickupPoint.position : transform.position;
        public bool IsInRange
        {
            get
            {
                if (inventory == null)
                    return false;
                Vector3 offset = inventory.transform.position - PickupPosition;
                offset.y = 0f;
                return offset.sqrMagnitude <= radius * radius;
            }
        }

        public void Configure(ProductionStation source, BurgerInventory carrier, Transform point,
            float pickupRadius = 1f, float interval = 0.25f)
        {
            station = source;
            inventory = carrier;
            pickupPoint = point;
            radius = Mathf.Max(0.1f, pickupRadius);
            pickupInterval = Mathf.Max(0.05f, interval);
            cooldown = 0f;
        }

        void Update() => Advance(Time.deltaTime);

        public void Advance(float deltaTime)
        {
            if (deltaTime <= 0f)
                return;

            if (station == null || !station.isActiveAndEnabled || inventory == null
                || !inventory.isActiveAndEnabled || !IsInRange)
            {
                cooldown = 0f;
                return;
            }

            cooldown = Mathf.Max(0f, cooldown - deltaTime);
            // Transfer at most one per frame: a hitch must not make the stack jump to full.
            if (cooldown <= 0f && inventory.TryCollectFrom(station))
                cooldown = pickupInterval;
        }
    }
}
