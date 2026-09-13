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
        InstallDriveThru, CleanTable, WorkerOrder, ExtraProduction, CarOrder, ColaOrder, CourierOrder, AutomatedOrder
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
        public const int LegacyMax = 6;

        public const int ContentEnd = 10;
        public const int MilestoneMask = (1 << (ContentEnd - 1)) - 1;
        public const int CycleBaseCost = 500;
        public const int CycleCostStep = 250;
        public const int IncomeDenominator = 50;
        public static bool IsMax(int rank) => false;
        public static int CycleCost(int rank) => (int)Math.Min(int.MaxValue,
            CycleBaseCost + CycleCostStep * Math.Max(0L, (long)rank - ContentEnd));
        public static int CompletedThrough(int rank) => (1 << Math.Min(ContentEnd - 1, Math.Max(0,rank))) - 1;
        public static ShopGoal[] Goals(int rank)
        {
            switch(rank)
            {
                case 1: return new[]{new ShopGoal(ShopGoalKind.ServeCustomers,"Complete a burger order",1)};
                case 2: return new[]{new ShopGoal(ShopGoalKind.CleanTable,"Clear a used dining table",1)};
                case 3: return new[]{new ShopGoal(ShopGoalKind.WorkerOrder,"Let staff complete an order",1)};
                case 4: return new[]{new ShopGoal(ShopGoalKind.ExtraProduction,"Produce on the second grill",1)};
                case 5: return new[]{new ShopGoal(ShopGoalKind.BoxBurger,"Pack a blue box",1)};
                case 6: return new[]{new ShopGoal(ShopGoalKind.CarOrder,"Complete a drive-thru order",1)};
                case 7: return new[]{new ShopGoal(ShopGoalKind.ColaOrder,"Sell a cup of cola",1)};
                case 8: return new[]{new ShopGoal(ShopGoalKind.CourierOrder,"Complete a courier order",1)};
                case 9: return new[]{new ShopGoal(ShopGoalKind.AutomatedOrder,"Fulfil an automated courier order",1)};
                default: return Array.Empty<ShopGoal>();
            }
        }
        public static string NextUnlock(int rank)
        {
            string[] labels={"Dining & cleaning","Hire staff","Second grill & counter","Blue-box packing",
                "Drive-thru","Cola side wing","Courier delivery","Conveyor automation","West bag area"};
            return rank >= 1 && rank < ContentEnd ? labels[rank-1] : "+2% cash income & shop sign";
        }
        public static int StarCap(int rank) => rank < ContentEnd ? 2 + 2 * Math.Max(1,rank) : 0;
        public static bool PadUnlocked(int rank,string title)
        {
            switch(title)
            {
                case "TABLE": return rank>=2;
                case "BOX": return rank>=5;
                case "GRILL": case "COUNTER": return rank>=4;
                case "LANE": return rank>=6;
                case "WING": case "COLA": case "BAR": case "FOUR": case "SQUARE": return rank>=7;
                default: return false;
            }
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
            if (lane) rank = Math.Max(rank, LegacyMax);
            else if (laneInv > 0) rank = Math.Max(rank, 5);
            return rank;
        }
    }
}
