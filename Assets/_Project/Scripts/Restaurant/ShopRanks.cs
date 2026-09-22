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
        InstallDriveThru, CleanTable, WorkerOrder, ExtraProduction, CarOrder, ColaOrder, CourierOrder, AutomatedOrder,
        UpgradeGrill,
        StatLinePeakTen, StatLineBreadthEight, StaffLinePeakTwelve,
        FacilityUpgradeAgain, AllTablesChosen, StatLinePeakEighteen
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
        public const int StarGateEnd = 15;
        public const int CycleStartRank = 16;
        public const int MilestoneBitCount = StarGateEnd;
        public const int MilestoneMask = (1 << MilestoneBitCount) - 1;
        public const int StatPeakTen = 10;
        public const int StatBreadthEight = 8;
        public const int StaffPeakTwelve = 12;
        public const int StatPeakEighteen = 18;
        public const int RequiredTableSets = 6;
        public const int FacilityUpgradeLevel = 2;
        public const int CycleBaseCost = 500;
        public const int CycleCostStep = 250;
        public const double CycleCostGrowth = 1.15d;
        public const int StarSupplyCap = 248;
        public const double StarCapBase = 4d;
        public const double StarCapGrowth = 1.12d;
        public const int IncomeDenominator = 50;
        public const string StarRewardCopy = "+2 ⭐";
        public const string LoopHintCopy = "Pick up a burger";

        // Player-visible rank at which listed content is available (after the previous Upgrade click).
        public const int DiningRank = 2;
        public const int HireRank = 3;
        public const int ExtraKitchenRank = 4;
        public const int BoxingRank = 5;
        public const int DriveThruRank = 6;
        public const int ColaWingRank = 7;
        public const int CourierRank = 8;
        public const int AutomationRank = 9;
        public const int WestRank = ContentEnd;

        public static bool IsMax(int rank) => false;
        public static bool IsCycleRank(int rank) => rank >= CycleStartRank;
        public static int CycleCost(int rank)
        {
            if (rank < CycleStartRank) return 0;
            int steps = rank - CycleStartRank;
            double logCost = Math.Log(CycleBaseCost) + steps * Math.Log(CycleCostGrowth);
            if (logCost >= Math.Log(int.MaxValue)) return int.MaxValue;
            return Math.Max(1, (int)Math.Round(CycleBaseCost * Math.Pow(CycleCostGrowth, steps),
                MidpointRounding.AwayFromZero));
        }
        // Legacy v13-and-earlier fills only the original 9 teaching bits; Lv.10–15 stay 0 so they can be made up.
        public static int CompletedThrough(int rank) => (1 << Math.Min(ContentEnd - 1, Math.Max(0, rank))) - 1;
        public static ShopGoal[] Goals(int rank)
        {
            switch(rank)
            {
                case 1: return new[]{new ShopGoal(ShopGoalKind.ServeCustomers,"Sell your first burger",1)};
                case 2: return new[]{new ShopGoal(ShopGoalKind.CleanTable,"Clear a used dining table",1)};
                case 3: return new[]{new ShopGoal(ShopGoalKind.WorkerOrder,"Let staff complete an order",1)};
                case 4: return new[]{new ShopGoal(ShopGoalKind.ExtraProduction,"Produce on the second grill",1)};
                case 5: return new[]{new ShopGoal(ShopGoalKind.BoxBurger,"Pack a blue box",1)};
                case 6: return new[]{new ShopGoal(ShopGoalKind.CarOrder,"Complete a drive-thru order",1)};
                case 7: return new[]{new ShopGoal(ShopGoalKind.ColaOrder,"Sell a cup of cola",1)};
                case 8: return new[]{new ShopGoal(ShopGoalKind.CourierOrder,"Complete a courier order",1)};
                case 9: return new[]{new ShopGoal(ShopGoalKind.AutomatedOrder,"Fulfil an automated courier order",1)};
                // 080: later ranks recommend real investments; no threshold-task awards.
                default: return Array.Empty<ShopGoal>();
            }
        }
        public static bool IsThresholdGoal(ShopGoalKind kind)
        {
            switch (kind)
            {
                case ShopGoalKind.StatLinePeakTen:
                case ShopGoalKind.StatLineBreadthEight:
                case ShopGoalKind.StaffLinePeakTwelve:
                case ShopGoalKind.FacilityUpgradeAgain:
                case ShopGoalKind.AllTablesChosen:
                case ShopGoalKind.StatLinePeakEighteen:
                    return true;
                default:
                    return false;
            }
        }
        public static bool MeetsThreshold(ShopGoalKind kind, int playerSpeed, int playerCarry,
            int staffSpeed, int staffCarry, int maxFacilityLevel, int chosenTableSets)
        {
            int maxStat = Math.Max(Math.Max(playerSpeed, playerCarry), Math.Max(staffSpeed, staffCarry));
            int minStat = Math.Min(Math.Min(playerSpeed, playerCarry), Math.Min(staffSpeed, staffCarry));
            int maxStaff = Math.Max(staffSpeed, staffCarry);
            switch (kind)
            {
                case ShopGoalKind.StatLinePeakTen: return maxStat >= StatPeakTen;
                case ShopGoalKind.StatLineBreadthEight: return minStat >= StatBreadthEight;
                case ShopGoalKind.StaffLinePeakTwelve: return maxStaff >= StaffPeakTwelve;
                case ShopGoalKind.FacilityUpgradeAgain: return maxFacilityLevel >= FacilityUpgradeLevel;
                case ShopGoalKind.AllTablesChosen: return chosenTableSets >= RequiredTableSets;
                case ShopGoalKind.StatLinePeakEighteen: return maxStat >= StatPeakEighteen;
                default: return false;
            }
        }
        public static int NextIncomePercent(int rank) => 2 * Math.Max(0, rank + 1 - ContentEnd);
        public static string NextUnlock(int rank)
        {
            string[] labels={"Dining, cleaning & trash bins","Expand main hall & hire staff","Second grill, burger counter & restroom",
                "Blue-box packing","Drive-thru counter","Cola lounge, four-seat & square tables",
                "Courier tray & red-box packing","Conveyor automation","West bag machines & pickup"};
            return rank >= 1 && rank < ContentEnd
                ? labels[rank-1]
                : $"+{NextIncomePercent(rank)}% cash income & shop sign";
        }
        public static int StarCap(int rank)
        {
            if (rank <= Min) return 2;
            int n = Math.Max(1, rank);
            double logCap = Math.Log(StarCapBase) + (n - 1) * Math.Log(StarCapGrowth);
            if (logCap >= Math.Log(int.MaxValue)) return int.MaxValue;
            return Math.Max(0, (int)Math.Round(StarCapBase * Math.Pow(StarCapGrowth, n - 1),
                MidpointRounding.AwayFromZero));
        }
        public static int MissingStars(int stars, int rank) => Math.Max(0, StarCap(rank) - Math.Max(0, stars));
        public static string NextRankPreview(int rank, int stars, long missingCoins)
        {
            string unlock = NextUnlock(rank);
            if (IsCycleRank(rank))
                return $"Need {missingCoins:N0} coins → Lv.{(long)rank + 1}, unlock {unlock}";
            return $"Need {MissingStars(stars, rank)} more stars → Lv.{rank + 1}, unlock {unlock}";
        }
        public static class Opening
        {
            public const string RankUp = "Tap Lv.1 Ready — unlock dining";
            public static string GrillUpgrade(int cost) => $"Upgrade the grill — {cost} coins";
        }
        public static string RankUpCapsule(int rank) => $"Tap Lv.{rank} Ready";

        public static bool PadUnlocked(int rank,string title)
        {
            switch(title)
            {
                case "TABLE": return rank>=ColaWingRank;
                case "BOX": return rank>=BoxingRank;
                case "GRILL": case "COUNTER": return rank>=ExtraKitchenRank;
                case "LANE": return rank>=DriveThruRank;
                case "WING": case "COLA": case "BAR": case "FOUR": case "SQUARE": return rank>=ColaWingRank;
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
