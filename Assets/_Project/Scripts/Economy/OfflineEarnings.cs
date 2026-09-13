using System;
using BurgerShop.Restaurant;
using BurgerShop.UI;
using UnityEngine;
namespace BurgerShop.Economy
{
    public static class OfflineEarnings
    {
        public const int MaxMinutes=480;
        public static decimal MaxEquivalents(int staffCount,int speedTier,int carryTier)
        {
            if(staffCount<0||staffCount>3||speedTier<0||speedTier>20||carryTier<0||carryTier>20)throw new ArgumentOutOfRangeException();
            return 8m*(staffCount/3m)*(1m+0.03m*(speedTier+carryTier))/2.2m;
        }
        public static long Grant(int nextUpgradeCost,int staffCount,int speedTier,int carryTier,decimal offlineMinutes)
        {
            if(nextUpgradeCost<=0||offlineMinutes<0)return 0;
            decimal maxEquivalents=MaxEquivalents(staffCount,speedTier,carryTier);
            decimal timeFactor=Math.Min(offlineMinutes,480m)/480m;
            return (long)decimal.Floor(nextUpgradeCost*maxEquivalents*timeFactor);
        }
        public static long ElapsedTicks(long previous,long now)=>previous<=0||now<previous?0:now-previous;
        public static int CheapestUpgrade(Component root)
        {
            int cheapest=int.MaxValue;bool found=false;
            void Add(int cost){if(cost>0){cheapest=Math.Min(cheapest,cost);found=true;}}
            var goals=root.GetComponent<SessionGoalTracker>();
            foreach(var zone in root.GetComponentsInChildren<GrillUpgradeZone>())if(zone.IsAvailable)Add(zone.NextCost);
            var player=root.GetComponent<BoostUpgradeZone>();if(player!=null){Add(player.SpeedCost);Add(player.CarryCost);}
            var staff=root.GetComponent<StaffUpgradeBoard>();
            if(staff!=null&&(goals?.Allows(3)??true)){Add(staff.SpeedCost);Add(staff.CarryCost);}
            foreach(var table in root.GetComponentsInChildren<TableUpgradeZone>())
                if(!table.IsPurchased&&!table.HasChosenSet&&table.Table!=null&&table.Table.gameObject.activeInHierarchy)Add(table.Cost);
            var growth=root.GetComponent<GrowthUpgrades>();
            if(growth!=null)foreach(var offer in growth.Offers)
            {int level=growth.Level(offer.Id);if(offer.Target!=null&&offer.Target.gameObject.activeInHierarchy&&level>0&&level<=offer.Costs.Length)Add(offer.Costs[level-1]);}
            if(goals!=null&&goals.IsCycle&&goals.Rank<int.MaxValue)Add(goals.CycleCost);
            return found?cheapest:0;
        }
    }
}
