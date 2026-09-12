using BurgerShop.Player;
using UnityEngine;

namespace BurgerShop.Restaurant
{
    public static class StaffBoost
    {
        public const int MaxTier = 5;
        public const int BaseCarry = 2;
        public const int CarryPerTier = 1;
        public const float BaseWalkSpeed = 3.8f;
        public const float SpeedBonusPerTier = 0.15f;
        public static readonly int[] Costs = PlayerBoost.Costs;

        public static int CarryCapacity(int tier) =>
            BaseCarry + CarryPerTier * Mathf.Clamp(tier, 0, MaxTier);

        public static float WalkSpeed(int tier) =>
            BaseWalkSpeed * (1f + SpeedBonusPerTier * Mathf.Clamp(tier, 0, MaxTier));

        public static int CostForNextTier(int currentTier) =>
            currentTier < 0 || currentTier >= MaxTier ? 0 : Costs[currentTier];

        public static bool IsMax(int tier) => tier >= MaxTier;
    }
}
