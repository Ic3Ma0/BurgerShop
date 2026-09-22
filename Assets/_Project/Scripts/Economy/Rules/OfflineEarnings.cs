using System;

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
    }
}
