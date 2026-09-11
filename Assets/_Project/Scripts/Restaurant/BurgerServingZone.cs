using System;
using BurgerShop.Customer;
using BurgerShop.Economy;
using BurgerShop.Player;
using UnityEngine;

namespace BurgerShop.Restaurant
{
    public sealed class BurgerServingZone : MonoBehaviour
    {
        CustomerQueue queue;
        BurgerInventory inventory;
        RestaurantWallet wallet;
        Transform servingPoint;
        Vector3[] exitRoute;
        float cooldown;
        [SerializeField, Min(0.1f)] float radius = 0.85f;
        [SerializeField, Min(0.1f)] float servingInterval = 0.75f;
        [SerializeField, Min(1)] int price = 10;

        public int Price => price;
        public Vector3 ServingPosition => servingPoint != null ? servingPoint.position : transform.position;
        public bool IsInRange
        {
            get
            {
                if (inventory == null) return false;
                Vector3 offset = inventory.transform.position - ServingPosition;
                offset.y = 0f;
                return offset.sqrMagnitude <= radius * radius;
            }
        }

        public void Configure(CustomerQueue customers, BurgerInventory carrier, RestaurantWallet earnings,
            Transform point, Vector3[] departureRoute, int burgerPrice = 10)
        {
            if (departureRoute == null || departureRoute.Length == 0)
                throw new ArgumentException("A departure route is required.", nameof(departureRoute));
            queue = customers;
            inventory = carrier;
            wallet = earnings;
            servingPoint = point;
            exitRoute = (Vector3[])departureRoute.Clone();
            price = Mathf.Max(1, burgerPrice);
            cooldown = 0f;
        }

        void Update() => Advance(Time.deltaTime);

        public void Advance(float deltaTime)
        {
            if (deltaTime <= 0f) return;
            cooldown = Mathf.Max(0f, cooldown - deltaTime);
            TryServeFrom(inventory);
        }

        // Player and staff use the same transaction and cooldown. Calling this
        // does not advance time, so extra carriers cannot accelerate the cashier.
        public bool TryServeFrom(BurgerInventory carrier)
        {
            if (!isActiveAndEnabled || cooldown > 0f || queue == null || !queue.isActiveAndEnabled
                || carrier == null || !carrier.isActiveAndEnabled || wallet == null || !wallet.isActiveAndEnabled
                || exitRoute == null || carrier.Count == 0
                || !wallet.CanRecordSale(price) || queue.ReadyCustomer == null) return false;
            Vector3 offset = carrier.transform.position - ServingPosition;
            offset.y = 0f;
            if (offset.sqrMagnitude > radius * radius) return false;

            // Validate before changing any state. All ownership changes happen on
            // this frame; repeat updates cannot serve the same customer twice.
            if (!queue.TryDequeueReadyCustomer(out CustomerAgent customer)) return false;
            cooldown = servingInterval;
            carrier.TryTakeBurger(out Transform burger);
            customer.BeginDeparture(burger, exitRoute, price);
            wallet.RecordSale(price);
            return true;
        }
    }
}
