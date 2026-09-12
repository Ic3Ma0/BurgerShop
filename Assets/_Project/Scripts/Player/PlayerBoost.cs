using UnityEngine;

namespace BurgerShop.Player
{
    public static class PlayerBoost
    {
        public const int MaxLevel = 5;
        public const int BaseCarry = 4;
        public const int CarryPerLevel = 1;
        public const float BaseMoveSpeed = 5.5f;
        public const float SpeedBonusPerLevel = 0.15f;
        public static readonly int[] Costs = { 50, 150, 300, 450, 600 };

        public static int CarryCapacity(int level) =>
            BaseCarry + CarryPerLevel * Mathf.Clamp(level, 0, MaxLevel);

        public static float MoveSpeed(int level) =>
            BaseMoveSpeed * (1f + SpeedBonusPerLevel * Mathf.Clamp(level, 0, MaxLevel));

        public static int CostForNextLevel(int currentLevel) =>
            currentLevel < 0 || currentLevel >= MaxLevel ? 0 : Costs[currentLevel];

        public static int CostForNextTier(int currentTier) => CostForNextLevel(currentTier);

        public static bool IsMax(int tier) => tier >= MaxLevel;
    }
}
