using BurgerShop.Economy;
using BurgerShop.Restaurant;
using UnityEngine;

namespace BurgerShop.UI
{
    // Rank-1 pointer at the first paid upgrade, then rank-up. Not a money-loop script.
    public sealed class OpeningGuide
    {
        public enum Step { Upgrade, RankUp, Done }

        RestaurantWallet wallet;
        GrillUpgradeZone grill;
        SessionGoalTracker tracker;

        public void Bind(RestaurantWallet earnings, GrillUpgradeZone upgrade, SessionGoalTracker goals)
        {
            wallet = earnings;
            grill = upgrade;
            tracker = goals;
        }

        public bool HasPaidUpgrade => grill != null && grill.Level > 1;

        public bool IsActive
        {
            get
            {
                if (tracker == null || tracker.Rank > ShopRanks.Min || grill == null) return false;
                if (HasPaidUpgrade) return tracker.CanUpgrade;
                return true;
            }
        }

        public Step Current
        {
            get
            {
                if (!IsActive) return Step.Done;
                if (tracker.CanUpgrade) return Step.RankUp;
                return Step.Upgrade;
            }
        }

        public string Title
        {
            get
            {
                if (Current == Step.RankUp) return ShopRanks.Opening.RankUp;
                if (Current == Step.Upgrade) return ShopRanks.Opening.GrillUpgrade(Mathf.Max(1, grill.NextCost));
                return tracker != null ? tracker.Title : ShopRanks.Goals(ShopRanks.Min)[0].Title;
            }
        }

        public int Progress
        {
            get
            {
                if (Current == Step.Upgrade)
                    return (int)Mathf.Min(grill.NextCost, wallet != null ? wallet.Coins : 0);
                if (Current == Step.RankUp) return tracker.Stars;
                return tracker != null ? tracker.Progress : 0;
            }
        }

        public int Required
        {
            get
            {
                if (Current == Step.Upgrade) return Mathf.Max(1, grill.NextCost);
                if (Current == Step.RankUp) return Mathf.Max(1, tracker.StarCap);
                return 1;
            }
        }
    }
}
