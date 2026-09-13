using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace BurgerShop.Persistence
{
    [Serializable]
    public sealed class RestaurantSaveData
    {
        public const int CurrentVersion = 15;

        public int version;
        public Building.FacilityPlacementRecord[] layout;
        public int milestoneMask;
        public bool legacyAccess;
        public int incomeRemainder;
        public int ResolvedMilestones => version >= 14 ? milestoneMask : Restaurant.ShopRanks.CompletedThrough(ResolvedShopRank);
        public bool ResolvedLegacyAccess => version < 14 || legacyAccess;
        public int ResolvedIncomeRemainder => version >= 14 ? incomeRemainder : 0;
        public long coins;
        public long parts;
        public long ResolvedParts => version >= 11 ? parts : 0;
        public int completedSales;
        public int grillLevel;
        public bool workerHired;
        public int workerDeliveries;
        public int hiredWorkerCount;
        public int workerClears;
        public int boostLevel;
        public bool boughtExtraTable;
        public bool boughtExtraGrill;
        public bool boughtExtraCounter;
        public int extraGrillLevel;
        public int staffSpeedTier;
        public int staffCarryTier;
        public int playerSpeedTier;
        public int playerCarryTier;
        public bool boughtBoxingStation;
        public bool boughtDriveThru;
        public int tableInvestment;
        public int grillInvestment;
        public int counterInvestment;
        public int boxingInvestment;
        public int driveThruInvestment;
        public int colaLevel = 1;
        public int ResolvedColaLevel => version >= 10 ? colaLevel : 1;
        public int shopRank = 1;
        public int goalIndex;
        public int goalProgress;
        public int upgradeStars;
        public bool westExpanded,bagMachineBuilt,bagTableBuilt,bagCounterBuilt;
        public int bagMachineInvestment,bagTableInvestment,bagCounterInvestment;
        public Restaurant.FacilityLevelRecord[] facilityLevels;
        public int ResolvedUpgradeStars => version >= 9 ? upgradeStars : (int)Math.Min(int.MaxValue, 2L*ResolvedGoalIndex + 2L*(grillLevel-1) + 2L*Math.Max(0,ResolvedExtraGrillLevel-1));
        public int table0Set;
        public int table1Set;
        public int table2Set;
        public int extraTableSet;
        public int table0Investment;
        public int table1Investment;
        public int table2Investment;
        public int extraTableInvestment;
        public bool boughtFourSeatTable;
        public bool boughtSquareTable;
        public int fourSeatInvestment;
        public int squareTableInvestment;
        public int fourSeatSet;
        public int squareTableSet;
        public int fourSeatUpgradeInvestment;
        public int squareTableUpgradeInvestment;

        public bool boughtSideWing;
        public int wingInvestment;
        public bool boughtColaMachine;
        public bool boughtColaCounter;
        public int colaMachineInvestment;
        public int colaCounterInvestment;

        public int ResolvedHiredCount => version >= 2
            ? hiredWorkerCount
            : workerHired ? 1 : 0;

        public int ResolvedBoostLevel => version >= 3 ? boostLevel : 0;
        public bool ResolvedBoughtExtraTable => version >= 3 && boughtExtraTable;
        public bool ResolvedBoughtExtraGrill => version >= 3 && boughtExtraGrill;
        public bool ResolvedBoughtExtraCounter => version >= 3 && boughtExtraCounter;
        public int ResolvedExtraGrillLevel => version >= 3 ? extraGrillLevel : 0;
        public int ResolvedStaffSpeedTier => version >= 4 ? staffSpeedTier : 0;
        public int ResolvedStaffCarryTier => version >= 4 ? staffCarryTier : 0;
        public int ResolvedPlayerSpeedTier => version >= 5 ? playerSpeedTier : ResolvedBoostLevel;
        public int ResolvedPlayerCarryTier => version >= 5 ? playerCarryTier : ResolvedBoostLevel;
        public bool ResolvedBoughtBoxingStation => version >= 6 && boughtBoxingStation;
        public bool ResolvedBoughtDriveThru => version >= 6 && boughtDriveThru;
        public int ResolvedShopRank
        {
            get
            {
                if(version >= 9) return shopRank;
                int implied = Restaurant.ShopRanks.Implied(ResolvedBoughtExtraTable, version >= 7 ? tableInvestment : 0,
                    ResolvedBoughtBoxingStation, version >= 7 ? boxingInvestment : 0,
                    ResolvedBoughtExtraGrill, version >= 7 ? grillInvestment : 0,
                    ResolvedBoughtExtraCounter, version >= 7 ? counterInvestment : 0,
                    ResolvedBoughtDriveThru, version >= 7 ? driveThruInvestment : 0);
                if (version >= 8 && shopRank >= Restaurant.ShopRanks.Min)
                    return Math.Max(implied, Math.Min(Restaurant.ShopRanks.LegacyMax, shopRank));
                return implied;
            }
        }
        public int ResolvedGoalIndex => version >= 8 ? Math.Max(0, goalIndex) : 0;
        public int ResolvedGoalProgress => version >= 8 ? Math.Max(0, goalProgress) : 0;
        public int ResolvedTable0Set => version >= 12 ? table0Set : 0;
        public int ResolvedTable1Set => version >= 12 ? table1Set : 0;
        public int ResolvedTable2Set => version >= 12 ? table2Set : 0;
        public int ResolvedExtraTableSet => version >= 12 ? extraTableSet : 0;
        public int ResolvedTable0Investment => version >= 12 ? table0Investment : 0;
        public int ResolvedTable1Investment => version >= 12 ? table1Investment : 0;
        public int ResolvedTable2Investment => version >= 12 ? table2Investment : 0;
        public int ResolvedExtraTableInvestment => version >= 12 ? extraTableInvestment : 0;
        public bool ResolvedBoughtFourSeatTable => version >= 12 && boughtFourSeatTable;
        public bool ResolvedBoughtSquareTable => version >= 12 && boughtSquareTable;
        public int ResolvedFourSeatInvestment => version >= 12 ? fourSeatInvestment : 0;
        public int ResolvedSquareTableInvestment => version >= 12 ? squareTableInvestment : 0;
        public int ResolvedFourSeatSet => version >= 12 ? fourSeatSet : 0;
        public int ResolvedSquareTableSet => version >= 12 ? squareTableSet : 0;
        public int ResolvedFourSeatUpgradeInvestment => version >= 12 ? fourSeatUpgradeInvestment : 0;
        public int ResolvedSquareTableUpgradeInvestment => version >= 12 ? squareTableUpgradeInvestment : 0;

        public bool ResolvedBoughtSideWing => version >= 13 ? boughtSideWing : version >= 10 || ResolvedBoughtExtraTable;
        public int ResolvedWingInvestment => version >= 13 ? wingInvestment : 0;
        public bool ResolvedBoughtColaMachine => version >= 13 ? boughtColaMachine : version >= 10;
        public bool ResolvedBoughtColaCounter => version >= 13 ? boughtColaCounter : version >= 10;
        public int ResolvedColaMachineInvestment => version >= 13 ? colaMachineInvestment : 0;
        public int ResolvedColaCounterInvestment => version >= 13 ? colaCounterInvestment : 0;

        public bool IsValid
        {
            get
            {
                if(version>=15 && !ValidLayout())return false;
                if(version>=14 && (milestoneMask<0 || (milestoneMask & ~Restaurant.ShopRanks.MilestoneMask)!=0
                    || incomeRemainder<0 || incomeRemainder>=Restaurant.ShopRanks.IncomeDenominator))return false;
                if(version>=11 && parts<0)return false;
                if (coins < 0 || completedSales < 0 || grillLevel < 1 || grillLevel > 3) return false;
                if (workerDeliveries < 0 || workerDeliveries > completedSales || workerClears < 0) return false;
                if (version == 1)
                    return workerHired || workerDeliveries == 0;
                if (version >= 13)
                {
                if (wingInvestment < 0 || wingInvestment > Restaurant.ShopExpansion.WingCost
                    || colaMachineInvestment < 0 || colaMachineInvestment > Restaurant.ShopExpansion.GrillCost
                    || colaCounterInvestment < 0 || colaCounterInvestment > Restaurant.ShopExpansion.CounterCost)
                    return false;
                if (colaLevel < 1 || colaLevel > 3) return false;
                if (!boughtColaMachine && colaLevel != 1) return false;
                if ((boughtColaMachine || boughtColaCounter) && !boughtSideWing) return false;
                if (!boughtSideWing && (boughtColaMachine || boughtColaCounter)) return false;
                }
                if (version < 2 || version > CurrentVersion) return false;
                if (hiredWorkerCount < 0 || hiredWorkerCount > 3) return false;
                if ((hiredWorkerCount > 0) != workerHired) return false;
                if (hiredWorkerCount == 0 && (workerDeliveries != 0 || workerClears != 0)) return false;
                if (version < 3) return true;
                if (boostLevel < 0 || boostLevel > Player.PlayerBoost.MaxLevel) return false;
                if (extraGrillLevel < 0 || extraGrillLevel > 3) return false;
                if (boughtExtraGrill != extraGrillLevel > 0) return false;
                if (version < 4) return true;
                if (staffSpeedTier < 0 || staffSpeedTier > Restaurant.StaffBoost.MaxTier || staffCarryTier < 0 || staffCarryTier > Restaurant.StaffBoost.MaxTier)
                    return false;
                if (version < 5) return true;
                if (playerSpeedTier < 0 || playerSpeedTier > Player.PlayerBoost.MaxLevel || playerCarryTier < 0 || playerCarryTier > Player.PlayerBoost.MaxLevel)
                    return false;
                if (version < 7) return true;
                if (tableInvestment < 0 || tableInvestment > Restaurant.ShopExpansion.TableCost
                    || grillInvestment < 0 || grillInvestment > Restaurant.ShopExpansion.GrillCost
                    || counterInvestment < 0 || counterInvestment > Restaurant.ShopExpansion.CounterCost
                    || boxingInvestment < 0 || boxingInvestment > Restaurant.ShopExpansion.BoxingCost
                    || driveThruInvestment < 0 || driveThruInvestment > Restaurant.ShopExpansion.DriveThruCost)
                    return false;
                if(version>=12)
                {
                if (!Restaurant.TableSetCatalog.IsConsistent(table0Set, table0Investment)
                    || !Restaurant.TableSetCatalog.IsConsistent(table1Set, table1Investment)
                    || !Restaurant.TableSetCatalog.IsConsistent(table2Set, table2Investment)
                    || !Restaurant.TableSetCatalog.IsConsistent(extraTableSet, extraTableInvestment))
                    return false;
                if (!(boughtExtraTable || (extraTableSet == 0 && extraTableInvestment == 0)))
                    return false;
                if (fourSeatInvestment < 0 || fourSeatInvestment > Restaurant.ShopExpansion.FourSeatCost
                    || squareTableInvestment < 0 || squareTableInvestment > Restaurant.ShopExpansion.SquareTableCost)
                    return false;
                if (!Restaurant.TableSetCatalog.IsConsistent(fourSeatSet, fourSeatUpgradeInvestment)
                    || !Restaurant.TableSetCatalog.IsConsistent(squareTableSet, squareTableUpgradeInvestment))
                    return false;
                if (!boughtFourSeatTable && (fourSeatSet != 0 || fourSeatUpgradeInvestment != 0))
                    return false;
                if(!boughtSquareTable && (squareTableSet != 0 || squareTableUpgradeInvestment != 0))return false;
                }
                if (version >= 10 && (colaLevel < 1 || colaLevel > 3)) return false;
                if (version < 8) return true;
                if(version >= 9 && (upgradeStars < 0 || !ValidFacilityLevels() || !ValidBagLine()))return false;
                return shopRank >= Restaurant.ShopRanks.Min && (version>=14 || shopRank <= Restaurant.ShopRanks.LegacyMax)
                    && goalIndex >= 0 && goalProgress >= 0;
            }
        }

        bool ValidLayout()
        {
            if(layout==null)return true;
            var ids=new System.Collections.Generic.HashSet<string>();
            foreach(var row in layout)if(row==null||!row.IsValid||!ids.Add(row.id))return false;
            return true;
        }
        bool ValidBagLine()
        {
            if(bagMachineInvestment<0||bagMachineInvestment>250||bagTableInvestment<0||bagTableInvestment>200||bagCounterInvestment<0||bagCounterInvestment>300)return false;
            if(!westExpanded&&(bagMachineBuilt||bagTableBuilt||bagCounterBuilt||bagMachineInvestment>0||bagTableInvestment>0||bagCounterInvestment>0))return false;
            if(!bagMachineBuilt&&(bagTableBuilt||bagTableInvestment>0))return false;
            if(!bagTableBuilt&&(bagCounterBuilt||bagCounterInvestment>0))return false;
            return !westExpanded || (version>=14 ? legacyAccess || shopRank>=Restaurant.ShopRanks.ContentEnd : shopRank>=6);
        }

        bool ValidFacilityLevels()
        {
            if(facilityLevels==null)return true;
            var seen=new System.Collections.Generic.HashSet<string>();
            foreach(var row in facilityLevels)
                if(row==null||!Restaurant.GrowthUpgrades.ValidId(row.id)||!seen.Add(row.id)||row.level<1||row.level>(row.id.StartsWith("table-")?4:3))return false;
            return true;
        }

        internal string Checksum()
        {
            string value = version <= 1
                ? string.Join("|", version.ToString(CultureInfo.InvariantCulture),
                    coins.ToString(CultureInfo.InvariantCulture), completedSales.ToString(CultureInfo.InvariantCulture),
                    grillLevel.ToString(CultureInfo.InvariantCulture), workerHired ? "1" : "0",
                    workerDeliveries.ToString(CultureInfo.InvariantCulture))
                : version == 2
                ? string.Join("|", version.ToString(CultureInfo.InvariantCulture),
                    coins.ToString(CultureInfo.InvariantCulture), completedSales.ToString(CultureInfo.InvariantCulture),
                    grillLevel.ToString(CultureInfo.InvariantCulture), workerHired ? "1" : "0",
                    workerDeliveries.ToString(CultureInfo.InvariantCulture),
                    hiredWorkerCount.ToString(CultureInfo.InvariantCulture),
                    workerClears.ToString(CultureInfo.InvariantCulture))
                : version == 3
                ? string.Join("|", version.ToString(CultureInfo.InvariantCulture),
                    coins.ToString(CultureInfo.InvariantCulture), completedSales.ToString(CultureInfo.InvariantCulture),
                    grillLevel.ToString(CultureInfo.InvariantCulture), workerHired ? "1" : "0",
                    workerDeliveries.ToString(CultureInfo.InvariantCulture),
                    hiredWorkerCount.ToString(CultureInfo.InvariantCulture),
                    workerClears.ToString(CultureInfo.InvariantCulture),
                    boostLevel.ToString(CultureInfo.InvariantCulture),
                    boughtExtraTable ? "1" : "0",
                    boughtExtraGrill ? "1" : "0",
                    boughtExtraCounter ? "1" : "0",
                    extraGrillLevel.ToString(CultureInfo.InvariantCulture))
                : version == 4
                ? string.Join("|", version.ToString(CultureInfo.InvariantCulture),
                    coins.ToString(CultureInfo.InvariantCulture), completedSales.ToString(CultureInfo.InvariantCulture),
                    grillLevel.ToString(CultureInfo.InvariantCulture), workerHired ? "1" : "0",
                    workerDeliveries.ToString(CultureInfo.InvariantCulture),
                    hiredWorkerCount.ToString(CultureInfo.InvariantCulture),
                    workerClears.ToString(CultureInfo.InvariantCulture),
                    boostLevel.ToString(CultureInfo.InvariantCulture),
                    boughtExtraTable ? "1" : "0",
                    boughtExtraGrill ? "1" : "0",
                    boughtExtraCounter ? "1" : "0",
                    extraGrillLevel.ToString(CultureInfo.InvariantCulture),
                    staffSpeedTier.ToString(CultureInfo.InvariantCulture),
                    staffCarryTier.ToString(CultureInfo.InvariantCulture))
                : version == 5
                ? string.Join("|", version.ToString(CultureInfo.InvariantCulture),
                    coins.ToString(CultureInfo.InvariantCulture), completedSales.ToString(CultureInfo.InvariantCulture),
                    grillLevel.ToString(CultureInfo.InvariantCulture), workerHired ? "1" : "0",
                    workerDeliveries.ToString(CultureInfo.InvariantCulture),
                    hiredWorkerCount.ToString(CultureInfo.InvariantCulture),
                    workerClears.ToString(CultureInfo.InvariantCulture),
                    boostLevel.ToString(CultureInfo.InvariantCulture),
                    boughtExtraTable ? "1" : "0",
                    boughtExtraGrill ? "1" : "0",
                    boughtExtraCounter ? "1" : "0",
                    extraGrillLevel.ToString(CultureInfo.InvariantCulture),
                    staffSpeedTier.ToString(CultureInfo.InvariantCulture),
                    staffCarryTier.ToString(CultureInfo.InvariantCulture),
                    playerSpeedTier.ToString(CultureInfo.InvariantCulture),
                    playerCarryTier.ToString(CultureInfo.InvariantCulture))
                : string.Join("|", version.ToString(CultureInfo.InvariantCulture),
                    coins.ToString(CultureInfo.InvariantCulture), completedSales.ToString(CultureInfo.InvariantCulture),
                    grillLevel.ToString(CultureInfo.InvariantCulture), workerHired ? "1" : "0",
                    workerDeliveries.ToString(CultureInfo.InvariantCulture),
                    hiredWorkerCount.ToString(CultureInfo.InvariantCulture),
                    workerClears.ToString(CultureInfo.InvariantCulture),
                    boostLevel.ToString(CultureInfo.InvariantCulture),
                    boughtExtraTable ? "1" : "0",
                    boughtExtraGrill ? "1" : "0",
                    boughtExtraCounter ? "1" : "0",
                    extraGrillLevel.ToString(CultureInfo.InvariantCulture),
                    staffSpeedTier.ToString(CultureInfo.InvariantCulture),
                    staffCarryTier.ToString(CultureInfo.InvariantCulture),
                    playerSpeedTier.ToString(CultureInfo.InvariantCulture),
                    playerCarryTier.ToString(CultureInfo.InvariantCulture),
                    boughtBoxingStation ? "1" : "0",
                    boughtDriveThru ? "1" : "0");
            // Versions 1-7 must retain their original checksum byte sequence.
            if (version >= 7)
                value += "|" + string.Join("|", tableInvestment.ToString(CultureInfo.InvariantCulture),
                    grillInvestment.ToString(CultureInfo.InvariantCulture), counterInvestment.ToString(CultureInfo.InvariantCulture),
                    boxingInvestment.ToString(CultureInfo.InvariantCulture), driveThruInvestment.ToString(CultureInfo.InvariantCulture));
            if (version >= 8)
                value += "|" + string.Join("|", shopRank.ToString(CultureInfo.InvariantCulture),
                    goalIndex.ToString(CultureInfo.InvariantCulture), goalProgress.ToString(CultureInfo.InvariantCulture));
            if(version >= 9)
            {
                value += "|"+upgradeStars.ToString(CultureInfo.InvariantCulture);
                value += "|"+(westExpanded?"1":"0")+"|"+(bagMachineBuilt?"1":"0")+"|"+(bagTableBuilt?"1":"0")+"|"+(bagCounterBuilt?"1":"0");
                value += "|"+bagMachineInvestment.ToString(CultureInfo.InvariantCulture)+"|"+bagTableInvestment.ToString(CultureInfo.InvariantCulture)+"|"+bagCounterInvestment.ToString(CultureInfo.InvariantCulture);
                if(facilityLevels!=null)foreach(var row in facilityLevels)value += "|"+row.id+":"+row.level.ToString(CultureInfo.InvariantCulture);
            }
            if(version >= 10) value += "|" + colaLevel.ToString(CultureInfo.InvariantCulture);
            if(version>=11)value += "|" + parts.ToString(CultureInfo.InvariantCulture);
            if(version>=12)
                value += "|" + string.Join("|",
                    table0Set.ToString(CultureInfo.InvariantCulture), table1Set.ToString(CultureInfo.InvariantCulture),
                    table2Set.ToString(CultureInfo.InvariantCulture), extraTableSet.ToString(CultureInfo.InvariantCulture),
                    table0Investment.ToString(CultureInfo.InvariantCulture), table1Investment.ToString(CultureInfo.InvariantCulture),
                    table2Investment.ToString(CultureInfo.InvariantCulture), extraTableInvestment.ToString(CultureInfo.InvariantCulture));
            if (version >= 12)
                value += "|" + string.Join("|",
                    boughtFourSeatTable ? "1" : "0", boughtSquareTable ? "1" : "0",
                    fourSeatInvestment.ToString(CultureInfo.InvariantCulture),
                    squareTableInvestment.ToString(CultureInfo.InvariantCulture),
                    fourSeatSet.ToString(CultureInfo.InvariantCulture),
                    squareTableSet.ToString(CultureInfo.InvariantCulture),
                    fourSeatUpgradeInvestment.ToString(CultureInfo.InvariantCulture),
                    squareTableUpgradeInvestment.ToString(CultureInfo.InvariantCulture));
            if (version >= 13)
                value += "|" + string.Join("|",
                    boughtSideWing ? "1" : "0",
                    wingInvestment.ToString(CultureInfo.InvariantCulture),
                    boughtColaMachine ? "1" : "0", boughtColaCounter ? "1" : "0",
                    colaMachineInvestment.ToString(CultureInfo.InvariantCulture),
                    colaCounterInvestment.ToString(CultureInfo.InvariantCulture),
                    colaLevel.ToString(CultureInfo.InvariantCulture));
            if(version>=14) value += "|"+milestoneMask.ToString(CultureInfo.InvariantCulture)+"|"+(legacyAccess?"1":"0")+"|"+incomeRemainder.ToString(CultureInfo.InvariantCulture);
            if(version>=15)
            {
                value += "|layout:"+(layout?.Length??0).ToString(CultureInfo.InvariantCulture);
                if(layout!=null)foreach(var row in layout)
                    value += "|"+row.id+":"+row.kind.ToString(CultureInfo.InvariantCulture)+":"+(row.purchased?"1":"0")+":"+
                        row.x.ToString("R",CultureInfo.InvariantCulture)+":"+row.z.ToString("R",CultureInfo.InvariantCulture)+":"+
                        row.yaw.ToString("R",CultureInfo.InvariantCulture)+":"+row.level.ToString(CultureInfo.InvariantCulture)+":"+row.tableSet.ToString(CultureInfo.InvariantCulture)+":"+row.investment.ToString(CultureInfo.InvariantCulture);
            }
            using (SHA256 hash = SHA256.Create())
                return Convert.ToBase64String(hash.ComputeHash(Encoding.UTF8.GetBytes(value)));
        }
    }
}
