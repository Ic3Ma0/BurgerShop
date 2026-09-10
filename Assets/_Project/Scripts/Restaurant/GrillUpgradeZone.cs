using BurgerShop.Economy;
using BurgerShop.Player;
using UnityEngine;

namespace BurgerShop.Restaurant
{
    public sealed class GrillUpgradeZone : MonoBehaviour
    {
        ProductionStation station;
        RestaurantWallet wallet;
        BurgerInventory player;
        Transform upgradePoint;
        TextMesh markerLabel;
        Transform[] levelIndicators;
        [SerializeField, Min(0.1f)] float radius = 1f;
        [SerializeField, Min(0.1f)] float holdSeconds = 1.5f;
        float heldTime;

        public int Level { get; private set; } = 1;
        public int MaxLevel => 3;
        public bool IsMaxLevel => Level >= MaxLevel;
        public int NextCost => IsMaxLevel ? 0 : Level == 1 ? 30 : 60;
        public float CurrentProductionSeconds => station != null ? station.ProductionSeconds : 3f;
        public float NextProductionSeconds => IsMaxLevel ? CurrentProductionSeconds : Level == 1 ? 2f : 1.5f;
        public float Progress => Mathf.Clamp01(heldTime / holdSeconds);
        public bool PurchasedThisVisit { get; private set; }
        public long MissingCoins => wallet != null ? System.Math.Max(0, NextCost - wallet.Coins) : NextCost;
        public Vector3 UpgradePosition => upgradePoint != null ? upgradePoint.position : transform.position;
        public bool IsAvailable => isActiveAndEnabled && station != null && station.isActiveAndEnabled && wallet != null
            && wallet.isActiveAndEnabled && player != null && player.isActiveAndEnabled;
        public bool IsInRange
        {
            get
            {
                if (player == null) return false;
                Vector3 offset = player.transform.position - UpgradePosition;
                offset.y = 0f;
                return offset.sqrMagnitude <= radius * radius;
            }
        }

        public void Configure(ProductionStation target, RestaurantWallet earnings, BurgerInventory carrier,
            Transform point, TextMesh label = null, Transform[] indicators = null)
        {
            station = target;
            wallet = earnings;
            player = carrier;
            upgradePoint = point;
            markerLabel = label;
            levelIndicators = indicators;
            heldTime = 0f;
            RefreshVisuals();
        }

        void Update() => Advance(Time.deltaTime);

        public void Advance(float deltaTime)
        {
            if (deltaTime <= 0f) return;
            if (!IsInRange)
            {
                heldTime = 0f;
                PurchasedThisVisit = false;
                return;
            }
            if (!IsAvailable || IsMaxLevel || PurchasedThisVisit || MissingCoins > 0)
            {
                heldTime = 0f;
                return;
            }
            // A long frame should not turn merely crossing the zone into a purchase.
            heldTime += Mathf.Min(deltaTime, 0.1f);
            if (heldTime + 0.0001f < holdSeconds) return;
            int cost = NextCost;
            float seconds = NextProductionSeconds;
            PurchasedThisVisit = true;
            heldTime = 0f;
            if (!wallet.TrySpend(cost))
            {
                PurchasedThisVisit = false;
                return;
            }
            Level++;
            station.SetProductionSeconds(seconds);
            RefreshVisuals();
        }

        void OnDisable() => heldTime = 0f;

        void LateUpdate()
        {
            if (markerLabel == null) return;
            // The nearby panel takes over while standing here, keeping text off the player.
            markerLabel.gameObject.SetActive(!IsInRange);
            if (Camera.main != null) markerLabel.transform.rotation = Camera.main.transform.rotation;
        }

        void RefreshVisuals()
        {
            if (markerLabel != null)
                markerLabel.text = IsMaxLevel ? $"GRILL LV {Level}\nMAX LEVEL" : $"UPGRADE\n{NextCost} COINS";
            if (levelIndicators != null)
                for (int i = 0; i < levelIndicators.Length; i++)
                    if (levelIndicators[i] != null) levelIndicators[i].gameObject.SetActive(i < Level - 1);
        }
    }
}
