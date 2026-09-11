using BurgerShop.Economy;
using BurgerShop.Player;
using UnityEngine;

namespace BurgerShop.Restaurant
{
    public sealed class WorkerHiringZone : MonoBehaviour
    {
        ProductionStation grill;
        BurgerServingZone serving;
        RestaurantWallet wallet;
        BurgerInventory player;
        Transform pickupPoint;
        Transform hiringPoint;
        Vector3 aisleCorner;
        TextMesh marker;
        float heldTime;
        const float Radius = 1f;
        const float HoldSeconds = 1.5f;

        public int HireCost => 50;
        public bool IsHired { get; private set; }
        public RestaurantWorker Worker { get; private set; }
        public float Progress => Mathf.Clamp01(heldTime / HoldSeconds);
        public long MissingCoins => wallet != null ? System.Math.Max(0, HireCost - wallet.Coins) : HireCost;
        public Vector3 HiringPosition => hiringPoint != null ? hiringPoint.position : transform.position;
        public bool IsAvailable => isActiveAndEnabled && grill != null && grill.isActiveAndEnabled
            && serving != null && serving.isActiveAndEnabled && wallet != null && wallet.isActiveAndEnabled
            && player != null && player.isActiveAndEnabled && pickupPoint != null;
        public bool IsInRange
        {
            get
            {
                if (player == null) return false;
                Vector3 offset = player.transform.position - HiringPosition;
                offset.y = 0f;
                return offset.sqrMagnitude <= Radius * Radius;
            }
        }

        public void Configure(ProductionStation source, BurgerServingZone cashier, RestaurantWallet earnings,
            BurgerInventory carrier, Transform pickup, Transform point, Vector3 aisle, TextMesh label = null)
        {
            grill = source;
            serving = cashier;
            wallet = earnings;
            player = carrier;
            pickupPoint = pickup;
            hiringPoint = point;
            aisleCorner = aisle;
            marker = label;
            heldTime = 0f;
        }

        void Update() => Advance(Time.deltaTime);

        public void RestoreWorker(bool hired, int deliveries)
        {
            if (deliveries < 0 || (!hired && deliveries != 0)) throw new System.ArgumentOutOfRangeException(nameof(deliveries));
            if (!hired) return;
            IsHired = true;
            heldTime = 0f;
            if (Worker == null)
                Worker = RestaurantWorker.Create(transform, HiringPosition, grill, serving, pickupPoint, aisleCorner);
            Worker.RestoreDeliveries(deliveries);
        }

        public void Advance(float deltaTime)
        {
            if (deltaTime <= 0f) return;
            if (IsHired || !IsAvailable || !IsInRange || MissingCoins > 0) { heldTime = 0f; return; }
            heldTime += Mathf.Min(deltaTime, 0.1f);
            if (heldTime + 0.0001f < HoldSeconds) return;
            heldTime = 0f;
            IsHired = true;
            if (!wallet.TrySpend(HireCost)) { IsHired = false; return; }
            Worker = RestaurantWorker.Create(transform, HiringPosition, grill, serving, pickupPoint, aisleCorner);
        }

        void OnDisable() => heldTime = 0f;

        void LateUpdate()
        {
            if (marker == null) return;
            marker.text = IsHired ? "STAFF HIRED" : $"HIRE STAFF\n{HireCost} COINS";
            marker.gameObject.SetActive(!IsInRange);
            if (Camera.main != null) marker.transform.rotation = Camera.main.transform.rotation;
        }
    }
}
