using BurgerShop.Player;
using UnityEngine;

namespace BurgerShop.Restaurant
{
    public static class StaffBoost
    {
        public const int MaxTier = PlayerBoost.MaxLevel;
        public const int BaseCarry = 2;
        public const int CarryPerTier = 1;
        public const float BaseWalkSpeed = 3.8f;
        public const float SpeedBonusPerTier = 0.15f;
        public static readonly int[] Costs = PlayerBoost.Costs;

        public static int CarryCapacity(int tier) =>
            BaseCarry + PlayerBoost.CarryBonus(tier);

        public static float WalkSpeed(int tier) =>
            0.85f * BaseWalkSpeed * PlayerBoost.SpeedMultiplier(tier);

        public static int CostForNextTier(int currentTier) =>
            currentTier < 0 || currentTier >= MaxTier ? 0 : Costs[currentTier];

        public static bool IsMax(int tier) => tier >= MaxTier;
    }
}
