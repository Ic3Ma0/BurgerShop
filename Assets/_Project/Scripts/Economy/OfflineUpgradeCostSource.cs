using System;
using BurgerShop.Restaurant;
using BurgerShop.UI;
using UnityEngine;

namespace BurgerShop.Economy
{
    // Read-only adapter: facility eligibility belongs to the existing upgrade systems.
    public static class OfflineUpgradeCostSource
    {
        public static int CheapestUpgrade(Component root)
        {
            int cheapest=int.MaxValue;bool found=false;
            void Add(int cost){if(cost>0){cheapest=Math.Min(cheapest,cost);found=true;}}
            var goals=root.GetComponent<SessionGoalTracker>();
            foreach(var zone in root.GetComponentsInChildren<GrillUpgradeZone>())if(zone.IsAvailable)Add(zone.NextCost);
            var player=root.GetComponent<BoostUpgradeZone>();if(player!=null){Add(player.SpeedCost);Add(player.CarryCost);}
            var staff=root.GetComponent<StaffUpgradeBoard>();
            if(staff!=null&&(goals?.Allows(ShopRanks.HireRank)??true)){Add(staff.SpeedCost);Add(staff.CarryCost);}
            foreach(var table in root.GetComponentsInChildren<TableUpgradeZone>())
                if(!table.IsPurchased&&!table.HasChosenSet&&table.Table!=null&&table.Table.gameObject.activeInHierarchy)
                {
                    int dueMin=0;
                    foreach(var id in TableSetCatalog.Choices)
                    {
                        int due=TableSetCatalog.Due(id,table.Invested);
                        if(due>0&&(dueMin==0||due<dueMin))dueMin=due;
                    }
                    if(dueMin>0)Add(dueMin);
                }
            var growth=root.GetComponent<GrowthUpgrades>();
            if(growth!=null)foreach(var offer in growth.Offers)
            {int level=growth.Level(offer.Id);if(offer.Target!=null&&offer.Target.gameObject.activeInHierarchy&&level>0&&level<=offer.Costs.Length)Add(offer.Costs[level-1]);}
            if(goals!=null&&goals.IsCycle&&goals.Rank<int.MaxValue)Add(goals.CycleCost);
            return found?cheapest:0;
        }
    }
}
