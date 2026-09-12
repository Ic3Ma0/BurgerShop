using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace BurgerShop.Persistence
{
    [Serializable]
    public sealed class RestaurantSaveData
    {
        public const int CurrentVersion = 9;

        public int version;
        public long coins;
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
        public int ResolvedTable0Set => version >= 8 ? table0Set : 0;
        public int ResolvedTable1Set => version >= 8 ? table1Set : 0;
        public int ResolvedTable2Set => version >= 8 ? table2Set : 0;
        public int ResolvedExtraTableSet => version >= 8 ? extraTableSet : 0;
        public int ResolvedTable0Investment => version >= 8 ? table0Investment : 0;
        public int ResolvedTable1Investment => version >= 8 ? table1Investment : 0;
        public int ResolvedTable2Investment => version >= 8 ? table2Investment : 0;
        public int ResolvedExtraTableInvestment => version >= 8 ? extraTableInvestment : 0;
        public bool ResolvedBoughtFourSeatTable => version >= 9 && boughtFourSeatTable;
        public bool ResolvedBoughtSquareTable => version >= 9 && boughtSquareTable;
        public int ResolvedFourSeatInvestment => version >= 9 ? fourSeatInvestment : 0;
        public int ResolvedSquareTableInvestment => version >= 9 ? squareTableInvestment : 0;
        public int ResolvedFourSeatSet => version >= 9 ? fourSeatSet : 0;
        public int ResolvedSquareTableSet => version >= 9 ? squareTableSet : 0;
        public int ResolvedFourSeatUpgradeInvestment => version >= 9 ? fourSeatUpgradeInvestment : 0;
        public int ResolvedSquareTableUpgradeInvestment => version >= 9 ? squareTableUpgradeInvestment : 0;

        public bool IsValid
        {
            get
            {
                if (coins < 0 || completedSales < 0 || grillLevel < 1 || grillLevel > 3) return false;
                if (workerDeliveries < 0 || workerDeliveries > completedSales || workerClears < 0) return false;
                if (version == 1)
                    return workerHired || workerDeliveries == 0;
                if (version < 2 || version > CurrentVersion) return false;
                if (hiredWorkerCount < 0 || hiredWorkerCount > 3) return false;
                if ((hiredWorkerCount > 0) != workerHired) return false;
                if (hiredWorkerCount == 0 && (workerDeliveries != 0 || workerClears != 0)) return false;
                if (version < 3) return true;
                if (boostLevel < 0 || boostLevel > 5) return false;
                if (extraGrillLevel < 0 || extraGrillLevel > 3) return false;
                if (boughtExtraGrill != extraGrillLevel > 0) return false;
                if (version < 4) return true;
                if (staffSpeedTier < 0 || staffSpeedTier > 5 || staffCarryTier < 0 || staffCarryTier > 5)
                    return false;
                if (version < 5) return true;
                if (playerSpeedTier < 0 || playerSpeedTier > 5 || playerCarryTier < 0 || playerCarryTier > 5)
                    return false;
                if (version < 7) return true;
                if (tableInvestment < 0 || tableInvestment > Restaurant.ShopExpansion.TableCost
                    || grillInvestment < 0 || grillInvestment > Restaurant.ShopExpansion.GrillCost
                    || counterInvestment < 0 || counterInvestment > Restaurant.ShopExpansion.CounterCost
                    || boxingInvestment < 0 || boxingInvestment > Restaurant.ShopExpansion.BoxingCost
                    || driveThruInvestment < 0 || driveThruInvestment > Restaurant.ShopExpansion.DriveThruCost)
                    return false;
                if (version < 8) return true;
                if (!Restaurant.TableSetCatalog.IsConsistent(table0Set, table0Investment)
                    || !Restaurant.TableSetCatalog.IsConsistent(table1Set, table1Investment)
                    || !Restaurant.TableSetCatalog.IsConsistent(table2Set, table2Investment)
                    || !Restaurant.TableSetCatalog.IsConsistent(extraTableSet, extraTableInvestment))
                    return false;
                if (!(boughtExtraTable || (extraTableSet == 0 && extraTableInvestment == 0)))
                    return false;
                if (version < 9) return true;
                if (fourSeatInvestment < 0 || fourSeatInvestment > Restaurant.ShopExpansion.FourSeatCost
                    || squareTableInvestment < 0 || squareTableInvestment > Restaurant.ShopExpansion.SquareTableCost)
                    return false;
                if (!Restaurant.TableSetCatalog.IsConsistent(fourSeatSet, fourSeatUpgradeInvestment)
                    || !Restaurant.TableSetCatalog.IsConsistent(squareTableSet, squareTableUpgradeInvestment))
                    return false;
                if (!boughtFourSeatTable && (fourSeatSet != 0 || fourSeatUpgradeInvestment != 0))
                    return false;
                return boughtSquareTable || (squareTableSet == 0 && squareTableUpgradeInvestment == 0);
            }
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
            // Versions 1-6 must retain their original checksum byte sequence.
            if (version >= 7)
                value += "|" + string.Join("|", tableInvestment.ToString(CultureInfo.InvariantCulture),
                    grillInvestment.ToString(CultureInfo.InvariantCulture), counterInvestment.ToString(CultureInfo.InvariantCulture),
                    boxingInvestment.ToString(CultureInfo.InvariantCulture), driveThruInvestment.ToString(CultureInfo.InvariantCulture));
            if (version >= 8)
                value += "|" + string.Join("|",
                    table0Set.ToString(CultureInfo.InvariantCulture), table1Set.ToString(CultureInfo.InvariantCulture),
                    table2Set.ToString(CultureInfo.InvariantCulture), extraTableSet.ToString(CultureInfo.InvariantCulture),
                    table0Investment.ToString(CultureInfo.InvariantCulture), table1Investment.ToString(CultureInfo.InvariantCulture),
                    table2Investment.ToString(CultureInfo.InvariantCulture), extraTableInvestment.ToString(CultureInfo.InvariantCulture));
            if (version >= 9)
                value += "|" + string.Join("|",
                    boughtFourSeatTable ? "1" : "0", boughtSquareTable ? "1" : "0",
                    fourSeatInvestment.ToString(CultureInfo.InvariantCulture),
                    squareTableInvestment.ToString(CultureInfo.InvariantCulture),
                    fourSeatSet.ToString(CultureInfo.InvariantCulture),
                    squareTableSet.ToString(CultureInfo.InvariantCulture),
                    fourSeatUpgradeInvestment.ToString(CultureInfo.InvariantCulture),
                    squareTableUpgradeInvestment.ToString(CultureInfo.InvariantCulture));
            using (SHA256 hash = SHA256.Create())
                return Convert.ToBase64String(hash.ComputeHash(Encoding.UTF8.GetBytes(value)));
        }
    }
}
