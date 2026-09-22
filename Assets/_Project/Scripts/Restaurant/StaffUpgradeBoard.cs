using BurgerShop.Economy;
using BurgerShop.Player;
using UnityEngine;

namespace BurgerShop.Restaurant
{
    public sealed class StaffUpgradeBoard : MonoBehaviour
    {
        RestaurantWallet wallet;
        WorkerHiringZone hiring;
        BurgerInventory player;
        bool purchasing;

        public long Coins => wallet != null ? wallet.Coins : 0;
        public int SpeedTier { get; private set; }
        public int CarryTier { get; private set; }
        public int SpeedCost => StaffBoost.CostForNextTier(SpeedTier);
        public int CarryCost => StaffBoost.CostForNextTier(CarryTier);
        public bool SpeedIsMax => StaffBoost.IsMax(SpeedTier);
        public bool CarryIsMax => StaffBoost.IsMax(CarryTier);
        public bool HasStaff => hiring != null && hiring.HiredCount > 0;
        public bool CanUpgradeStaff => HasStaff && MainHallExpansion.HasAccess
            && (GetComponent<UI.SessionGoalTracker>()?.Allows(ShopRanks.HireRank) ?? true);
        public bool CanAffordSpeed => CanUpgradeStaff && !SpeedIsMax && wallet != null && wallet.Coins >= SpeedCost;
        public bool CanAffordCarry => CanUpgradeStaff && !CarryIsMax && wallet != null && wallet.Coins >= CarryCost;
        public bool IsPlayerInRange =>
            (GetComponent<UI.SessionGoalTracker>()?.Allows(ShopRanks.HireRank)??true) && player != null && ShopLayout.ContainsHrUpgradeRange(player.transform.position);

        public void Configure(RestaurantWallet earnings, WorkerHiringZone staff, BurgerInventory carrier)
        {
            wallet = earnings;
            hiring = staff;
            player = carrier;
            hiring?.BindUpgrades(this);
            ApplyToHired();
        }

        public void RestoreTiers(int speedTier, int carryTier)
        {
            if (speedTier < 0 || speedTier > StaffBoost.MaxTier)
                throw new System.ArgumentOutOfRangeException(nameof(speedTier));
            if (carryTier < 0 || carryTier > StaffBoost.MaxTier)
                throw new System.ArgumentOutOfRangeException(nameof(carryTier));
            SpeedTier = speedTier;
            CarryTier = carryTier;
            ApplyToHired();
        }

        public bool TryBuySpeed() => TryBuy(true);

        public bool TryBuyCarry() => TryBuy(false);

        public void ApplyTo(RestaurantWorker worker)
        {
            worker?.ApplyStaffTiers(SpeedTier, CarryTier);
        }

        public void ApplyToHired()
        {
            if (hiring == null) return;
            for (int i = 0; i < hiring.Workers.Count; i++)
                ApplyTo(hiring.Workers[i]);
        }

        bool TryBuy(bool speed)
        {
            if (purchasing || !CanUpgradeStaff || !IsPlayerInRange || wallet == null) return false;
            bool max = speed ? SpeedIsMax : CarryIsMax;
            int cost = speed ? SpeedCost : CarryCost;
            if (max || wallet.Coins < cost) return false;
            purchasing = true;
            try
            {
                if (!wallet.TrySpend(cost, () =>
                {
                    if (speed) SpeedTier++; else CarryTier++;
                    ApplyToHired();
                    GetComponent<UI.SessionGoalTracker>()?.AddUpgradeStars();
                    GetComponent<UI.SessionGoalTracker>()?.EvaluateStarGateTasks();
                })) return false;
            }
            finally { purchasing = false; }
            UI.FeedbackDirector.Current?.Success(player.transform.position,"Level Up!",player.transform);
            return true;
        }
    }
}
