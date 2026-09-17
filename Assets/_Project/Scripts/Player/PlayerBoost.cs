using UnityEngine;

namespace BurgerShop.Player
{
    public static class PlayerBoost
    {
        public const int MaxLevel = 20;
        public const int BaseCarry = 4;
        public const int CarryPerLevel = 1;
        public const float BaseMoveSpeed = 5.5f;
        public const float SpeedBonusPerLevel = 0.15f;
        public const int SpeedSoftLevel = 6;
        public const float LateSpeedBonus = 0.08f;
        public const int EmptyCarryStart = 9;
        public const float EmptyCarrySpeedBonus = 0.03f;
        public const double CostGrowth = 1.18;
        public const int StartingCost = 50;
        public static readonly int[] Costs = BuildCosts();

        static int[] BuildCosts()
        {
            var costs = new int[MaxLevel];
            for (int n = 0; n < costs.Length; n++)
                costs[n] = (int)System.Math.Round(StartingCost * System.Math.Pow(CostGrowth, n) / 10d,
                    System.MidpointRounding.AwayFromZero) * 10;
            return costs;
        }

        public static bool IsEmptyCarryLevel(int level) =>
            level >= EmptyCarryStart && (level % 2 == 1);

        public static int EmptyCarryCount(int level)
        {
            int n = Mathf.Clamp(level, 0, MaxLevel);
            return n < EmptyCarryStart ? 0 : (n - (EmptyCarryStart - 2)) / 2;
        }

        public static int CarryBonus(int level)
        {
            int n = Mathf.Clamp(level, 0, MaxLevel);
            return (Mathf.Min(n, EmptyCarryStart - 1) + Mathf.Max(0, n - (EmptyCarryStart - 1)) / 2)
                * CarryPerLevel;
        }

        public static float SpeedMultiplier(int level)
        {
            int n = Mathf.Clamp(level, 0, MaxLevel);
            return 1f + SpeedBonusPerLevel * Mathf.Min(n, SpeedSoftLevel)
                + LateSpeedBonus * Mathf.Max(0, n - SpeedSoftLevel);
        }

        public static float CarrySpeedMultiplier(int carryLevel) =>
            1f + EmptyCarrySpeedBonus * EmptyCarryCount(carryLevel);

        public static int CarryCapacity(int level) =>
            BaseCarry + CarryBonus(level);

        public static float MoveSpeed(int speedLevel) =>
            MoveSpeed(speedLevel, 0);

        public static float MoveSpeed(int speedLevel, int carryLevel) =>
            0.85f * BaseMoveSpeed * SpeedMultiplier(speedLevel) * CarrySpeedMultiplier(carryLevel);

        public static int CostForNextLevel(int currentLevel) =>
            currentLevel < 0 || currentLevel >= MaxLevel ? 0 : Costs[currentLevel];

        public static int CostForNextTier(int currentTier) => CostForNextLevel(currentTier);

        public static bool IsMax(int tier) => tier >= MaxLevel;
    }
}
