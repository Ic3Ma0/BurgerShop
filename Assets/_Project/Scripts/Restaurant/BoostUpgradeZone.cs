using BurgerShop.Economy;
using BurgerShop.Player;
using UnityEngine;

namespace BurgerShop.Restaurant
{
    public sealed class BoostUpgradeZone : MonoBehaviour
    {
        RestaurantWallet wallet;
        BurgerInventory player;
        PlayerMotor motor;
        TextMesh markerLabel;
        bool purchasing;
        public PlayerMotor Player => motor;

        public long Coins => wallet != null ? wallet.Coins : 0;
        public int SpeedTier { get; private set; }
        public int CarryTier { get; private set; }
        public int MaxLevel => PlayerBoost.MaxLevel;
        public int SpeedCost => PlayerBoost.CostForNextTier(SpeedTier);
        public int CarryCost => PlayerBoost.CostForNextTier(CarryTier);
        public bool SpeedIsMax => PlayerBoost.IsMax(SpeedTier);
        public bool CarryIsMax => PlayerBoost.IsMax(CarryTier);
        public bool CanAffordSpeed => !SpeedIsMax && wallet != null && wallet.Coins >= SpeedCost;
        public bool CanAffordCarry => !CarryIsMax && wallet != null && wallet.Coins >= CarryCost;
        public bool IsMaxLevel => SpeedIsMax && CarryIsMax;
        public int NextCost
        {
            get
            {
                if (SpeedIsMax && CarryIsMax) return 0;
                if (SpeedIsMax) return CarryCost;
                if (CarryIsMax) return SpeedCost;
                return Mathf.Min(SpeedCost, CarryCost);
            }
        }
        public bool HasVisitedRoom { get; private set; }
        public bool IsPlayerInRange =>
            player != null && ShopLayout.ContainsBoostUpgradeRange(player.transform.position);

        public void Configure(RestaurantWallet earnings, BurgerInventory carrier, PlayerMotor movement,
            Transform point, TextMesh label = null)
        {
            wallet = earnings;
            player = carrier;
            motor = movement;
            markerLabel = label;
            _ = point;
            ApplyToPlayer();
            RefreshVisuals();
        }

        void Update() => Advance(Time.deltaTime);

        public void RestoreTiers(int speedTier, int carryTier)
        {
            if (speedTier < 0 || speedTier > MaxLevel)
                throw new System.ArgumentOutOfRangeException(nameof(speedTier));
            if (carryTier < 0 || carryTier > MaxLevel)
                throw new System.ArgumentOutOfRangeException(nameof(carryTier));
            SpeedTier = speedTier;
            CarryTier = carryTier;
            ApplyToPlayer();
            RefreshVisuals();
        }

        public void RestoreLevel(int level) => RestoreTiers(level, level);

        public bool TryBuySpeed(int expectedTier = -1) => TryBuy(true, expectedTier);

        public bool TryBuyCarry(int expectedTier = -1) => TryBuy(false, expectedTier);

        public void Advance(float deltaTime)
        {
            _ = deltaTime;
            if (player != null && ShopLayout.ContainsBoostUpgradeRange(player.transform.position))
                HasVisitedRoom = true;
        }

        bool TryBuy(bool speed, int expectedTier)
        {
            if (purchasing || player == null || wallet == null) return false;
            if (expectedTier >= 0 && expectedTier != (speed ? SpeedTier : CarryTier)) return false;
            bool max = speed ? SpeedIsMax : CarryIsMax;
            int cost = speed ? SpeedCost : CarryCost;
            if (max || wallet.Coins < cost) return false;
            purchasing = true;
            try
            {
                if (!wallet.TrySpend(cost, () =>
                {
                    if (speed) SpeedTier++; else CarryTier++;
                    ApplyToPlayer();
                    RefreshVisuals();
                    GetComponent<UI.SessionGoalTracker>()?.AddUpgradeStars();
                    GetComponent<UI.SessionGoalTracker>()?.EvaluateStarGateTasks();
                })) return false;
            }
            finally { purchasing = false; }
            UI.FeedbackDirector.Current?.Success(player.transform.position,"Level Up!",player.transform);
            return true;
        }

        void ApplyToPlayer()
        {
            motor?.ApplyBoostTiers(SpeedTier, CarryTier);
            player?.ApplyBoostLevel(CarryTier);
        }

        void LateUpdate()
        {
            if (markerLabel == null) return;
            markerLabel.gameObject.SetActive(false);
            if (Camera.main != null) markerLabel.transform.rotation = Camera.main.transform.rotation;
        }

        void RefreshVisuals()
        {
            if (markerLabel == null) return;
            markerLabel.text = IsMaxLevel ? $"Boost MAX\n{MaxLevel} / {MaxLevel}" : $"Player upgrades\n{ShopRanks.StarRewardCopy}";
        }
    }
}
