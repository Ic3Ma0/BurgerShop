using System;

namespace BurgerShop.Restaurant
{
    public enum ShopGoalKind
    {
        PickupBurger,
        ServeCustomers,
        InstallTable,
        InstallBoxing,
        BoxBurger,
        InstallGrill,
        InstallCounter,
        InstallDriveThru
    }

    public readonly struct ShopGoal
    {
        public readonly ShopGoalKind Kind;
        public readonly string Title;
        public readonly int Required;

        public ShopGoal(ShopGoalKind kind, string title, int required)
        {
            Kind = kind;
            Title = title;
            Required = Math.Max(1, required);
        }
    }

    public static class ShopRanks
    {
        public const int Min = 1;
        public const int Max = 6;

        public static bool IsMax(int rank) => rank >= Max;

        public static ShopGoal[] Goals(int rank)
        {
            switch (rank)
            {
                case 1:
                    return new[]
                    {
                        new ShopGoal(ShopGoalKind.PickupBurger, "Pick up a burger", 1),
                        new ShopGoal(ShopGoalKind.ServeCustomers, "Serve customers", 3),
                        new ShopGoal(ShopGoalKind.InstallTable, "Install a table", 1)
                    };
                case 2:
                    return new[]
                    {
                        new ShopGoal(ShopGoalKind.ServeCustomers, "Serve customers", 5),
                        new ShopGoal(ShopGoalKind.InstallBoxing, "Install a boxing table", 1)
                    };
                case 3:
                    return new[]
                    {
                        new ShopGoal(ShopGoalKind.BoxBurger, "Box the burger", 1),
                        new ShopGoal(ShopGoalKind.InstallGrill, "Install a grill", 1)
                    };
                case 4:
                    return new[]
                    {
                        new ShopGoal(ShopGoalKind.ServeCustomers, "Serve customers", 5),
                        new ShopGoal(ShopGoalKind.InstallCounter, "Install a counter", 1)
                    };
                case 5:
                    return new[]
                    {
                        new ShopGoal(ShopGoalKind.ServeCustomers, "Serve customers", 5),
                        new ShopGoal(ShopGoalKind.InstallDriveThru, "Install a drive-thru", 1)
                    };
                default:
                    return Array.Empty<ShopGoal>();
            }
        }

        public static int StarCap(int rank)
        {
            ShopGoal[] goals = Goals(rank);
            return goals.Length == 0 ? 1 : goals.Length;
        }

        public static bool PadUnlocked(int rank, string title)
        {
            if (string.IsNullOrEmpty(title)) return false;
            if (title == "TABLE") return rank >= 1;
            if (title == "BOX") return rank >= 2;
            if (title == "GRILL") return rank >= 3;
            if (title == "COUNTER") return rank >= 4;
            if (title == "LANE") return rank >= 5;
            return rank >= Max;
        }

        public static int Implied(bool table, int tableInv, bool boxing, int boxInv, bool grill, int grillInv,
            bool counter, int counterInv, bool lane, int laneInv)
        {
            int rank = Min;
            if (table) rank = Math.Max(rank, 2);
            if (boxing) rank = Math.Max(rank, 3);
            else if (boxInv > 0) rank = Math.Max(rank, 2);
            if (grill) rank = Math.Max(rank, 4);
            else if (grillInv > 0) rank = Math.Max(rank, 3);
            if (counter) rank = Math.Max(rank, 5);
            else if (counterInv > 0) rank = Math.Max(rank, 4);
            if (lane) rank = Math.Max(rank, Max);
            else if (laneInv > 0) rank = Math.Max(rank, 5);
            return rank;
        }
    }
}
