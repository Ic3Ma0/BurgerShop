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
        StationUpgradeFeedback feedback;
        int[] costs = { 30, 60 };
        float[] seconds = { 3f, 2f, 1.5f };
        KitchenProduct product = KitchenProduct.Burger;
        [SerializeField, Min(0.1f)] float radius = 1f;
        [SerializeField, Min(0.1f)] float holdSeconds = 1.5f;
        float heldTime;
        bool purchasing;
        bool openingHighlight;

        public bool DirectInteraction { get; private set; }
        public Transform Pad => upgradePoint;
        public event System.Action<int, bool> LevelApplied;
        public ProductionStation Station => station;
        public int Level { get; private set; } = 1;
        public KitchenProduct Product => product;
        public string ProductNoun => product == KitchenProduct.Cola ? "COLA" : "GRILL";
        public string ItemNoun => product == KitchenProduct.Cola ? "cup" : "burger";
        public int MaxLevel => seconds != null && seconds.Length > 0 ? seconds.Length : 3;
        public bool IsMaxLevel => Level >= MaxLevel;
        public int NextCost => IsMaxLevel || costs == null || Level < 1 || Level > costs.Length ? 0 : costs[Level - 1];
        public float CurrentProductionSeconds => station != null ? station.ProductionSeconds : SecondsFor(Level);
        public float NextProductionSeconds => IsMaxLevel ? CurrentProductionSeconds : SecondsFor(Level + 1);
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
            Transform point, TextMesh label = null, Transform[] indicators = null, StationUpgradeFeedback visuals = null,
            int[] levelCosts = null, float[] productionSeconds = null, KitchenProduct kind = KitchenProduct.Burger)
        {
            station = target;
            wallet = earnings;
            player = carrier;
            upgradePoint = point;
            markerLabel = label;
            levelIndicators = indicators;
            feedback = visuals;
            product = kind;
            if (levelCosts != null && levelCosts.Length >= 1) costs = (int[])levelCosts.Clone();
            if (productionSeconds != null && productionSeconds.Length >= 1) seconds = (float[])productionSeconds.Clone();
            heldTime = 0f;
            ApplyStationStats();
            RefreshVisuals();
            feedback?.ShowLevel(Level);
            if (UI.FacilityDetailsHud.Current != null) UseDirectInteraction();
        }

        public void UseDirectInteraction()
        {
            DirectInteraction = true;
            heldTime = 0f;
            if (upgradePoint != null) upgradePoint.gameObject.SetActive(openingHighlight);
            if (markerLabel != null) markerLabel.gameObject.SetActive(false);
        }

        public void SetOpeningHighlight(bool on)
        {
            openingHighlight = on;
            if (upgradePoint != null) upgradePoint.gameObject.SetActive(on || !DirectInteraction);
        }

        public bool TryUpgrade(int expectedLevel)
        {
            if (purchasing || !IsAvailable || IsMaxLevel || Level != expectedLevel) return false;
            purchasing = true;
            try
            {
                return wallet.TrySpend(NextCost, () =>
                {
                    Level++;
                    ApplyStationStats();
                    RefreshVisuals();
                    feedback?.PlayUpgrade(Level);
                    LevelApplied?.Invoke(Level, true);
                    GetComponentInParent<GrowthUpgrades>()?.RecordGrill(this);
                    if (product == KitchenProduct.Burger && Level == 2)
                        GetComponentInParent<UI.SessionGoalTracker>()?.RecordMilestone(ShopGoalKind.UpgradeGrill);
                });
            }
            finally { purchasing = false; }
        }

        void Update() => Advance(Time.deltaTime);

        public void RestoreLevel(int level)
        {
            if (level < 1 || level > MaxLevel) throw new System.ArgumentOutOfRangeException(nameof(level));
            Level = level;
            heldTime = 0f;
            PurchasedThisVisit = false;
            ApplyStationStats();
            RefreshVisuals();
            feedback?.ShowLevel(Level);
            LevelApplied?.Invoke(Level, false);
        }

        public void Advance(float deltaTime)
        {
            feedback?.Advance(deltaTime);
            if ((DirectInteraction && !openingHighlight) || deltaTime <= 0f) return;
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
            heldTime = 0f;
            PurchasedThisVisit = true;
            if (!TryUpgrade(Level)) PurchasedThisVisit = false;
        }

        float SecondsFor(int level)
        {
            if (seconds == null || seconds.Length == 0) return 3f;
            int index = Mathf.Clamp(level, 1, seconds.Length) - 1;
            return seconds[index];
        }

        void ApplyStationStats()
        {
            if (station == null) return;
            station.SetCapacity(ProductionStation.CapacityForLevel(Level, product));
            station.SetProductionSeconds(SecondsFor(Level));
            station.SetMaxedTier(IsMaxLevel);
        }

        void OnDisable() => heldTime = 0f;

        void LateUpdate()
        {
            if (DirectInteraction && !openingHighlight) { UseDirectInteraction(); return; }
            if (markerLabel == null) return;
            // The nearby panel takes over while standing here, keeping text off the player.
            markerLabel.gameObject.SetActive(!IsInRange);
            if (Camera.main != null) markerLabel.transform.rotation = Camera.main.transform.rotation;
        }

        void RefreshVisuals()
        {
            if (markerLabel != null)
                markerLabel.text = IsMaxLevel ? $"{ProductNoun} LV {Level}\nMAX LEVEL" : $"UPGRADE\n{NextCost} COINS\n{ShopRanks.StarRewardCopy}";
            if (levelIndicators != null)
                for (int i = 0; i < levelIndicators.Length; i++)
                    if (levelIndicators[i] != null) levelIndicators[i].gameObject.SetActive(i < Level - 1);
        }
    }
}
