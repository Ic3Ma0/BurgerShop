using BurgerShop.Economy;
using BurgerShop.Restaurant;
using BurgerShop.UI;
using UnityEngine;

namespace BurgerShop.Persistence
{
    public sealed class RestaurantPersistence : MonoBehaviour
    {
        public const string EditorDirectoryKey = "BurgerShop.Tests.SaveDirectory";
        RestaurantWallet wallet;
        GrillUpgradeZone upgrade;
        WorkerHiringZone hiring;
        BoostUpgradeZone boost;
        ShopExpansion expansion;
        StaffUpgradeBoard staffUpgrades;
        SessionGoalTracker goals;
        PartsWallet partsWallet;
        GrillUpgradeZone colaUpgrade;
        TableUpgradeBoard tableUpgrades;
        LocalSaveStore store;
        string lastChecksum;
        float elapsed;
        bool ready;
        bool saveRequested;

        public SaveLoadResult LoadResult { get; private set; }
        public string Status { get; private set; } = "NEW GAME - AUTOSAVE ON";
        public string FilePath => store?.FilePath;
        public int RestoredRank { get; private set; } = ShopRanks.Min;
        public int RestoredGoalIndex { get; private set; }
        public int RestoredGoalProgress { get; private set; }

        public void Configure(RestaurantWallet earnings, GrillUpgradeZone grill, WorkerHiringZone staff, string directory = null)
            => Configure(earnings, grill, staff, null, null, null, directory);

        public void Configure(RestaurantWallet earnings, GrillUpgradeZone grill, WorkerHiringZone staff,
            BoostUpgradeZone playerBoost, string directory = null)
            => Configure(earnings, grill, staff, playerBoost, null, null, directory);

        public void Configure(RestaurantWallet earnings, GrillUpgradeZone grill, WorkerHiringZone staff,
            BoostUpgradeZone playerBoost, ShopExpansion shop, string directory = null)
            => Configure(earnings, grill, staff, playerBoost, shop, null, directory);

        public void Configure(RestaurantWallet earnings, GrillUpgradeZone grill, WorkerHiringZone staff,
            BoostUpgradeZone playerBoost, ShopExpansion shop, StaffUpgradeBoard upgrades, string directory = null)
            => Configure(earnings, grill, staff, playerBoost, shop, upgrades, null, directory);

        public void Configure(RestaurantWallet earnings, GrillUpgradeZone grill, WorkerHiringZone staff,
            BoostUpgradeZone playerBoost, ShopExpansion shop, StaffUpgradeBoard upgrades, SessionGoalTracker tracker,
            string directory = null, GrillUpgradeZone cola = null, TableUpgradeBoard tables = null)
        {
            if(partsWallet!=null)partsWallet.Changed-=RequestSave;
            partsWallet=GetComponent<PartsWallet>();
            wallet = earnings;
            upgrade = grill;
            hiring = staff;
            boost = playerBoost;
            if (expansion != null) expansion.PurchaseCompleted -= RequestSave;
            expansion = shop;
            if (expansion != null) expansion.PurchaseCompleted += RequestSave;
            staffUpgrades = upgrades;
            if (goals != null) goals.ProgressChanged -= RequestSave;
            goals = tracker;
            colaUpgrade = cola;
            if (goals != null) goals.ProgressChanged += RequestSave;
            if (tableUpgrades != null) tableUpgrades.Changed -= RequestSave;
            tableUpgrades = tables;
            if (tableUpgrades != null) tableUpgrades.Changed += RequestSave;
            if (directory == null)
            {
                directory = Application.persistentDataPath;
#if UNITY_EDITOR
                string testDirectory = UnityEditor.SessionState.GetString(EditorDirectoryKey, "");
                if (!string.IsNullOrEmpty(testDirectory)) directory = testDirectory;
#endif
            }
            store = new LocalSaveStore(directory);
            LoadResult = store.Load(out RestaurantSaveData data);
            if (data != null)
            {
                wallet.RestoreProgress(data.coins, data.completedSales);
                upgrade.RestoreLevel(data.grillLevel);
                staffUpgrades?.RestoreTiers(data.ResolvedStaffSpeedTier, data.ResolvedStaffCarryTier);
                hiring.RestoreWorkers(data.ResolvedHiredCount, data.workerDeliveries, data.workerClears);
                staffUpgrades?.ApplyToHired();
                boost?.RestoreTiers(data.ResolvedPlayerSpeedTier, data.ResolvedPlayerCarryTier);
                expansion?.Restore(data.ResolvedBoughtExtraTable, data.ResolvedBoughtExtraGrill,
                    data.ResolvedBoughtExtraCounter, data.ResolvedExtraGrillLevel,
                    data.ResolvedBoughtBoxingStation, data.ResolvedBoughtDriveThru,
                    data.ResolvedBoughtFourSeatTable, data.ResolvedBoughtSquareTable,
                    data.ResolvedBoughtSideWing || data.ResolvedBoughtExtraTable
                        || data.ResolvedBoughtFourSeatTable || data.ResolvedBoughtSquareTable
                        || data.ResolvedBoughtColaMachine || data.ResolvedBoughtColaCounter,
                    data.ResolvedBoughtColaMachine, data.ResolvedBoughtColaCounter, data.ResolvedColaLevel);
                if (data.version >= 7)
                    expansion?.RestoreInvestments(data.tableInvestment, data.grillInvestment, data.counterInvestment,
                        data.boxingInvestment, data.driveThruInvestment,
                        data.ResolvedFourSeatInvestment, data.ResolvedSquareTableInvestment,
                        data.ResolvedWingInvestment, data.ResolvedColaMachineInvestment,
                        data.ResolvedColaCounterInvestment);
                tableUpgrades?.Restore(data.ResolvedTable0Set, data.ResolvedTable1Set, data.ResolvedTable2Set,
                    data.ResolvedExtraTableSet, data.ResolvedTable0Investment, data.ResolvedTable1Investment,
                    data.ResolvedTable2Investment, data.ResolvedExtraTableInvestment,
                    data.ResolvedFourSeatSet, data.ResolvedSquareTableSet,
                    data.ResolvedFourSeatUpgradeInvestment, data.ResolvedSquareTableUpgradeInvestment);
                colaUpgrade?.RestoreLevel(data.ResolvedColaLevel);
                lastChecksum = data.Checksum();
                RestoredRank = data.ResolvedShopRank;
                RestoredGoalIndex = data.ResolvedGoalIndex;
                RestoredGoalProgress = data.ResolvedGoalProgress;
            }
            else
            {
                RestoredRank = ShopRanks.Min;
                RestoredGoalIndex = 0;
                RestoredGoalProgress = 0;
            }
            partsWallet?.Restore(data?.ResolvedParts ?? 0);
            if(partsWallet!=null)partsWallet.Changed+=RequestSave;
            goals?.Restore(RestoredRank, RestoredGoalIndex, RestoredGoalProgress, data?.ResolvedUpgradeStars ?? 0);
            GetComponent<BagLine>()?.Restore(data);
            GetComponent<GrowthUpgrades>()?.Restore(data?.facilityLevels, data?.grillLevel ?? 1, data?.ResolvedExtraGrillLevel ?? 0, data?.ResolvedColaLevel ?? 1);
            if (goals != null)
                expansion?.ApplyRank(goals.Rank);
            Status = LoadResult == SaveLoadResult.Loaded ? "PROGRESS RESTORED"
                : LoadResult == SaveLoadResult.RecoveredBackup ? "BACKUP RESTORED"
                : LoadResult == SaveLoadResult.NewerVersion ? "NEWER SAVE - SAVING DISABLED"
                : LoadResult == SaveLoadResult.Unreadable ? "SAVE UNREADABLE - FILES KEPT"
                : LoadResult == SaveLoadResult.Unavailable ? "SAVE UNAVAILABLE"
                : "NEW GAME - AUTOSAVE ON";
            ready = true;
            // Repair a damaged primary from the validated backup on the next save.
            if (LoadResult == SaveLoadResult.RecoveredBackup) lastChecksum = null;
        }

        void LateUpdate() => Advance(Time.unscaledDeltaTime);

        public void Advance(float seconds)
        {
            if (!ready || seconds <= 0f) return;
            elapsed += seconds;
            if (!saveRequested && elapsed < 2f) return;
            elapsed = 0f;
            Flush();
        }

        public bool Flush()
        {
            if (!ready || wallet == null || upgrade == null || hiring == null || !store.CanWrite) return false;
            var data = new RestaurantSaveData
            {
                version = RestaurantSaveData.CurrentVersion, coins = wallet.Coins, completedSales = wallet.CompletedSales,
                parts = partsWallet != null ? partsWallet.Balance : 0,
                grillLevel = upgrade.Level, workerHired = hiring.IsHired,
                workerDeliveries = hiring.TotalDeliveries,
                hiredWorkerCount = hiring.HiredCount,
                workerClears = hiring.TotalClears,
                boostLevel = 0,
                boughtExtraTable = expansion != null && expansion.HasExtraTable,
                boughtExtraGrill = expansion != null && expansion.HasExtraGrill,
                boughtExtraCounter = expansion != null && expansion.HasExtraCounter,
                extraGrillLevel = expansion != null ? expansion.ExtraGrillLevel : 0,
                staffSpeedTier = staffUpgrades != null ? staffUpgrades.SpeedTier : 0,
                staffCarryTier = staffUpgrades != null ? staffUpgrades.CarryTier : 0,
                playerSpeedTier = boost != null ? boost.SpeedTier : 0,
                playerCarryTier = boost != null ? boost.CarryTier : 0,
                boughtBoxingStation = expansion != null && expansion.HasBoxing,
                boughtDriveThru = expansion != null && expansion.HasDriveThru,
                tableInvestment = expansion?.TableInvested ?? 0,
                grillInvestment = expansion?.GrillPad?.Invested ?? 0,
                counterInvestment = expansion?.CounterPad?.Invested ?? 0,
                boxingInvestment = expansion?.BoxingPad?.Invested ?? 0,
                driveThruInvestment = expansion?.DriveThruPad?.Invested ?? 0,
                shopRank = goals != null ? goals.Rank : RestoredRank,
                goalIndex = goals != null ? goals.GoalIndex : RestoredGoalIndex,
                goalProgress = goals != null ? goals.GoalProgress : RestoredGoalProgress,
                upgradeStars = goals != null ? goals.Stars : 0,
                facilityLevels = GetComponent<GrowthUpgrades>()?.Capture(),
                westExpanded = GetComponent<BagLine>()?.Expanded ?? false,
                bagMachineBuilt = GetComponent<BagLine>()?.MachineBuilt ?? false,
                bagTableBuilt = GetComponent<BagLine>()?.TableBuilt ?? false,
                bagCounterBuilt = GetComponent<BagLine>()?.CounterBuilt ?? false,
                bagMachineInvestment = GetComponent<BagLine>()?.MachinePad?.Invested ?? 0,
                bagTableInvestment = GetComponent<BagLine>()?.TablePad?.Invested ?? 0,
                bagCounterInvestment = GetComponent<BagLine>()?.CounterPad?.Invested ?? 0,

                table0Set = tableUpgrades?.SetAt(0) ?? 0,
                table1Set = tableUpgrades?.SetAt(1) ?? 0,
                table2Set = tableUpgrades?.SetAt(2) ?? 0,
                extraTableSet = tableUpgrades?.SetAt(3) ?? 0,
                table0Investment = tableUpgrades?.InvestedAt(0) ?? 0,
                table1Investment = tableUpgrades?.InvestedAt(1) ?? 0,
                table2Investment = tableUpgrades?.InvestedAt(2) ?? 0,
                extraTableInvestment = tableUpgrades?.InvestedAt(3) ?? 0,
                boughtFourSeatTable = expansion != null && expansion.HasFourSeatTable,
                boughtSquareTable = expansion != null && expansion.HasSquareTable,
                fourSeatInvestment = expansion?.FourSeatInvested ?? 0,
                squareTableInvestment = expansion?.SquareInvested ?? 0,
                fourSeatSet = tableUpgrades?.SetAt(4) ?? 0,
                squareTableSet = tableUpgrades?.SetAt(5) ?? 0,
                fourSeatUpgradeInvestment = tableUpgrades?.InvestedAt(4) ?? 0,
                squareTableUpgradeInvestment = tableUpgrades?.InvestedAt(5) ?? 0,
                boughtSideWing = expansion != null ? expansion.HasWing : colaUpgrade != null,
                wingInvestment = expansion?.WingPad?.Invested ?? 0,
                boughtColaMachine = expansion != null ? expansion.HasColaMachine : colaUpgrade != null,
                boughtColaCounter = expansion != null ? expansion.HasColaBar : colaUpgrade != null,
                colaMachineInvestment = expansion?.ColaInvested ?? 0,
                colaCounterInvestment = expansion?.ColaBarInvested ?? 0,
                colaLevel = expansion != null ? expansion.ColaLevel : colaUpgrade != null ? colaUpgrade.Level : 1
            };
            string checksum = data.Checksum();
            if (checksum == lastChecksum) return true;
            if (!store.Save(data)) { Status = "SAVE FAILED - RETRYING"; return false; }
            lastChecksum = checksum;
            saveRequested = false;
            Status = "PROGRESS SAVED";
            return true;
        }

        void RequestSave() => saveRequested = true;
        void OnDestroy()
        {
            if (expansion != null) expansion.PurchaseCompleted -= RequestSave;
            if (goals != null) goals.ProgressChanged -= RequestSave;
            if(partsWallet!=null)partsWallet.Changed-=RequestSave;
            if (tableUpgrades != null) tableUpgrades.Changed -= RequestSave;
        }
        void OnApplicationPause(bool paused) { if (paused) Flush(); }
        void OnApplicationFocus(bool focused) { if (!focused) Flush(); }
        void OnApplicationQuit() => Flush();
    }
}
