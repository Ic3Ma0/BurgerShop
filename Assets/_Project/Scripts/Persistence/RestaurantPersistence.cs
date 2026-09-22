using BurgerShop.Core;
using BurgerShop.Economy;
using BurgerShop.Restaurant;
using BurgerShop.UI;
using UnityEngine;

namespace BurgerShop.Persistence
{
    public enum SaveSessionPhase
    {
        Running,
        Quiescing,
        Hydrating,
        LoadFailed
    }

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
        SaveSlotStore slots;
        string saveDirectory;
        string lastChecksum;
        float elapsed;
        bool ready;
        bool saveRequested;
        bool quiesceFlush;
        SaveSessionPhase phase = SaveSessionPhase.Hydrating;
        public System.Func<long> UtcNowTicks=()=>System.DateTime.UtcNow.Ticks;
        long lastSeen;
        bool appPaused,appUnfocused,away;
        bool settlementPending;
        public long OfflineGrant {get;private set;}
        public long OfflineTicks {get;private set;}
        public int OfflineStaffCount {get;private set;}
        public bool OfflineVisible {get;private set;}


        public SaveLoadResult LoadResult { get; private set; }
        public SaveSessionPhase Phase => phase;
        public string Status { get; private set; } = "NEW GAME - AUTOSAVE ON";
        public string FilePath => store?.FilePath;
        public int ActiveSlotId => slots != null ? slots.ActiveSlotId : 1;
        public SaveSlotInfo[] SlotSummaries => slots != null ? slots.Slots : System.Array.Empty<SaveSlotInfo>();
        public bool CanStartNewGame => slots != null && slots.CanStartNewGame;
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
            phase = SaveSessionPhase.Hydrating;
            ready = false;
            quiesceFlush = false;
            if(partsWallet!=null)partsWallet.Changed-=RequestSave;
            partsWallet=GetComponent<PartsWallet>();
            if (wallet != null) wallet.CoinsSpent -= OnSpend;
            wallet = earnings;
            if (wallet != null) wallet.CoinsSpent += OnSpend;
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
            saveDirectory = directory;
            slots = new SaveSlotStore(directory);
            store = new LocalSaveStore(directory, slots.ActiveFileName);
            ApplyLoadedSnapshot();
        }

        public static string RuntimeDirectory()
        {
#if UNITY_EDITOR
            string isolated=UnityEditor.SessionState.GetString(EditorDirectoryKey, "");
            if(!string.IsNullOrEmpty(isolated))return isolated;
#endif
            return Application.persistentDataPath;
        }
        public static RestaurantSaveData PeekActiveSnapshot()
        {
            string directory=RuntimeDirectory();var slots=new SaveSlotStore(directory);
            new LocalSaveStore(directory,slots.ActiveFileName).Load(out var data);return data;
        }
        public bool CanDeleteSlot(int id) => slots != null && slots.CanDelete(id);

        public bool SaveNow() => Flush();

        public bool StartNewGame()
        {
            if (!ready || slots == null || phase != SaveSessionPhase.Running || !slots.CanStartNewGame) return false;
            if (!QuiesceAndFlushCurrent()) return false;
            if (slots.TryCreateSlot(out _) != SaveSlotOpResult.Success)
            {
                phase = SaveSessionPhase.Running;
                return false;
            }
            BindActiveStore();
            phase = SaveSessionPhase.Hydrating;
            Goal01Bootstrap.RequestInstalledShopRebuild();
            return true;
        }

        public bool SwitchToSlot(int id)
        {
            if (!ready || slots == null || phase != SaveSessionPhase.Running || id == ActiveSlotId) return false;
            if (!QuiesceAndFlushCurrent()) return false;
            if (slots.TryActivate(id) != SaveSlotOpResult.Success)
            {
                phase = SaveSessionPhase.Running;
                return false;
            }
            BindActiveStore();
            phase = SaveSessionPhase.Hydrating;
            Goal01Bootstrap.RequestInstalledShopRebuild();
            return true;
        }

        bool QuiesceAndFlushCurrent()
        {
            phase = SaveSessionPhase.Quiescing;
            quiesceFlush = true;
            bool saved = store != null && store.CanWrite && Flush();
            quiesceFlush = false;
            if (!saved)
            {
                phase = SaveSessionPhase.Running;
                return false;
            }
            RememberActiveSlot();
            return true;
        }

        public bool DeleteSlot(int id) => slots != null && slots.TryDelete(id) == SaveSlotOpResult.Success;

        void BindActiveStore()
        {
            store = new LocalSaveStore(saveDirectory, slots.ActiveFileName);
            lastChecksum = null;
        }

        void RememberActiveSlot()
        {
            if (slots == null || wallet == null) return;
            int rank = goals != null ? goals.Rank : RestoredRank;
            slots.RecordActive(rank < 1 ? ShopRanks.Min : rank, wallet.Coins, UtcNowTicks());
        }

        void ApplyLoadedSnapshot()
        {
            LoadResult = store.Load(out RestaurantSaveData data);
            if (data != null) ApplySave(data);
            else if (LoadResult == SaveLoadResult.NewGame) ApplyNewGame();
            else ApplyMissingKeepRuntime();
            ready = true;
            if (LoadResult == SaveLoadResult.RecoveredBackup) lastChecksum = null;
            if (LoadResult == SaveLoadResult.NewerVersion || LoadResult == SaveLoadResult.Unreadable
                || LoadResult == SaveLoadResult.Unavailable)
            {
                phase = SaveSessionPhase.LoadFailed;
                return;
            }
            if (data != null || LoadResult == SaveLoadResult.NewGame) SettleOffline();
            phase = SaveSessionPhase.Running;
        }

        void ApplyMissingKeepRuntime()
        {
            RestoredRank = ShopRanks.Min;
            RestoredGoalIndex = 0;
            RestoredGoalProgress = 0;
            partsWallet?.Restore(0);
            if (partsWallet != null)
            {
                partsWallet.Changed -= RequestSave;
                partsWallet.Changed += RequestSave;
            }
            goals?.Restore(RestoredRank, RestoredGoalIndex, RestoredGoalProgress, 0, 0, false, 0);
            GetComponent<BagLine>()?.Restore(null);
            GetComponent<GrowthUpgrades>()?.Restore(null, 1, 0, 1);
            if (goals != null) goals.ApplyUnlocks();
            GetComponent<Building.FacilityLayout>()?.Restore(null);
            GetComponent<RestroomExpansion>()?.Restore(false, 0, 0);
            Status = LoadResult == SaveLoadResult.NewerVersion ? "NEWER SAVE - SAVING DISABLED"
                : LoadResult == SaveLoadResult.Unreadable ? "SAVE UNREADABLE - FILES KEPT"
                : LoadResult == SaveLoadResult.Unavailable ? "SAVE UNAVAILABLE"
                : "NEW GAME - AUTOSAVE ON";
            lastSeen = 0;
            OfflineGrant = 0;
            OfflineTicks = 0;
            OfflineStaffCount = 0;
            OfflineVisible = false;
        }

        void ApplyNewGame()
        {
            LoadResult = SaveLoadResult.NewGame;
            lastChecksum = null;
            lastSeen = 0;
            OfflineGrant = 0;
            OfflineTicks = 0;
            OfflineStaffCount = 0;
            OfflineVisible = false;
            RestoredRank = ShopRanks.Min;
            RestoredGoalIndex = 0;
            RestoredGoalProgress = 0;
            wallet?.RestoreProgress(0, 0);
            if (upgrade != null) upgrade.RestoreLevel(1);
            staffUpgrades?.RestoreTiers(0, 0);
            hiring?.RestoreWorkers(0, 0);
            staffUpgrades?.ApplyToHired();
            boost?.RestoreTiers(0, 0);
            colaUpgrade?.RestoreLevel(1);
            partsWallet?.Restore(0);
            goals?.Restore(ShopRanks.Min, 0, 0, 0, 0, false, 0);
            GetComponent<GrowthUpgrades>()?.Restore(null, 1, 0, 1);
            if (goals != null) goals.ApplyUnlocks();
            GetComponent<Building.FacilityLayout>()?.Restore(null);
            GetComponent<RestroomExpansion>()?.Restore(false, 0, 0);
            tableUpgrades?.Restore(0, 0, 0, 0, 0, 0, 0, 0);
            Status = "NEW GAME - AUTOSAVE ON";
            GetComponent<Core.RestaurantArchitecture>()?.Refresh();
        }

        void ApplySave(RestaurantSaveData data)
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
            partsWallet?.Restore(data.ResolvedParts);
            if (partsWallet != null)
            {
                partsWallet.Changed -= RequestSave;
                partsWallet.Changed += RequestSave;
            }
            GetComponent<MainHallExpansion>()?.Restore(data.version>=18&&data.compactStart,data.ResolvedMainHallBuilt,data.version>=18?data.mainHallInvestment:0,data.ResolvedSmallFootprint);
            goals?.Restore(RestoredRank, RestoredGoalIndex, RestoredGoalProgress, data.OpeningStars, data.ResolvedMilestones, data.ResolvedLegacyAccess, data.ResolvedIncomeRemainder,data.ResolvedFirstOrder);
            GetComponent<BagLine>()?.Restore(data);
            GetComponent<GrowthUpgrades>()?.Restore(data.facilityLevels, data.grillLevel, data.ResolvedExtraGrillLevel, data.ResolvedColaLevel);
            if (goals != null)
                goals.ApplyUnlocks();
            GetComponent<Building.FacilityLayout>()?.Restore(data.version >= 15 ? data.layout : null);
            GetComponent<RestroomExpansion>()?.Restore(data.version>=17&&data.restroomBuilt,data.version>=17?data.restroomInvestment:0,data.version>=17?data.restroomDirtyMask:0);
            Status = LoadResult == SaveLoadResult.Loaded ? "PROGRESS RESTORED"
                : LoadResult == SaveLoadResult.RecoveredBackup ? "BACKUP RESTORED"
                : LoadResult == SaveLoadResult.NewerVersion ? "NEWER SAVE - SAVING DISABLED"
                : LoadResult == SaveLoadResult.Unreadable ? "SAVE UNREADABLE - FILES KEPT"
                : LoadResult == SaveLoadResult.Unavailable ? "SAVE UNAVAILABLE"
                : "NEW GAME - AUTOSAVE ON";
            lastSeen=data.version>=16?data.lastSeenUtcTicks:0;
            OfflineGrant=data.version>=16?data.offlineReceiptGrant:0;
            OfflineTicks=data.version>=16?data.offlineReceiptTicks:0;
            OfflineStaffCount=data.version>=16?data.offlineReceiptStaffCount:0;
            OfflineVisible=data.version>=16&&data.offlineReceiptVisible;
        }

        void LateUpdate() => Advance(Time.unscaledDeltaTime);

        public void Advance(float seconds)
        {
            if (phase != SaveSessionPhase.Running || !ready || away || seconds <= 0f) return;
            elapsed += seconds;
            if (!saveRequested && elapsed < 2f) return;
            elapsed = 0f;
            Flush();
        }

        public bool Flush()
        {
            if (phase == SaveSessionPhase.Quiescing)
            {
                if (!quiesceFlush) return false;
            }
            else if (phase != SaveSessionPhase.Running) return false;
            if (GetComponent<Building.FacilityLayout>()?.Committing==true) return false;
            if (!ready || wallet == null || upgrade == null || hiring == null || store == null || !store.CanWrite) return false;
            if(settlementPending&&!SettleOffline())return false;
            var data=Capture();
            if(!away)data.lastSeenUtcTicks=UtcNowTicks();
            string checksum = data.Checksum();
            if (checksum == lastChecksum)
            {
                RememberActiveSlot();
                return true;
            }
            if (!store.Save(data)) { Status = "SAVE FAILED - RETRYING"; return false; }
            lastSeen=data.lastSeenUtcTicks;
            lastChecksum = checksum;
            saveRequested = false;
            Status = "PROGRESS SAVED";
            RememberActiveSlot();
            return true;
        }

        RestaurantSaveData Capture()
        {
            return new RestaurantSaveData
            {
                restroomBuilt=GetComponent<RestroomExpansion>()?.Built??false,
                restroomInvestment=GetComponent<RestroomExpansion>()?.Invested??0,
                restroomDirtyMask=GetComponent<RestroomExpansion>()?.DirtyMask??0,
                lastSeenUtcTicks=lastSeen,offlineReceiptGrant=OfflineGrant,offlineReceiptTicks=OfflineTicks,
                offlineReceiptStaffCount=OfflineStaffCount,offlineReceiptVisible=OfflineVisible,
                layout = GetComponent<Building.FacilityLayout>()?.Capture(),
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
                milestoneMask = goals?.MilestoneMask ?? 0,
                firstOrderComplete = goals?.FirstOrderComplete ?? false,
                compactStart = GetComponent<MainHallExpansion>()?.CompactStart ?? false,
                smallFootprint = GetComponent<MainHallExpansion>()?.SmallFootprint ?? false,
                mainHallBuilt = GetComponent<MainHallExpansion>()?.Built ?? true,
                mainHallInvestment = GetComponent<MainHallExpansion>()?.Invested ?? 0,
                legacyAccess = goals?.LegacyAccess ?? false,
                incomeRemainder = goals?.IncomeRemainder ?? 0,
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
        }
        public bool SettleOffline()
        {
            if(phase==SaveSessionPhase.LoadFailed)return false;
            if(phase==SaveSessionPhase.Quiescing&&!quiesceFlush)return false;
            if(phase!=SaveSessionPhase.Running&&phase!=SaveSessionPhase.Hydrating&&phase!=SaveSessionPhase.Quiescing)return false;
            if(!ready||store==null||!store.CanWrite)return false;
            long now=UtcNowTicks();
            var data=Capture();
            var plan=OfflineSettlement.Calculate(data.coins,lastSeen,now,
                new OfflineReceipt(OfflineGrant,OfflineTicks,OfflineStaffCount,OfflineVisible),
                lastSeen==0?0:OfflineUpgradeCostSource.CheapestUpgrade(this),hiring.HiredCount,
                staffUpgrades?.SpeedTier??0,staffUpgrades?.CarryTier??0);
            // Balance and consumed timestamp share the same atomic file replacement.
            // The panel is a persisted receipt for money already credited, not another claim.
            data.lastSeenUtcTicks=plan.LastSeenUtcTicks;
            data.coins=plan.Coins;
            data.offlineReceiptVisible=plan.Receipt.Visible;data.offlineReceiptGrant=plan.Receipt.Grant;
            data.offlineReceiptTicks=plan.Receipt.Ticks;data.offlineReceiptStaffCount=plan.Receipt.StaffCount;
            if(!store.Save(data)){settlementPending=true;Status="SAVE FAILED - OFFLINE PAYMENT NOT APPLIED";return false;}
            settlementPending=false;
            lastSeen=now;OfflineGrant=data.offlineReceiptGrant;OfflineTicks=data.offlineReceiptTicks;
            OfflineStaffCount=data.offlineReceiptStaffCount;OfflineVisible=data.offlineReceiptVisible;
            lastChecksum=data.Checksum();wallet.RestoreProgress(data.coins,data.completedSales);return true;
        }
        public bool DismissOfflineReceipt()
        {
            bool previous=OfflineVisible;OfflineVisible=false;
            if(Flush())return true;OfflineVisible=previous;return false;
        }
        void SetAway(bool value)
        {
            if(phase!=SaveSessionPhase.Running)return;
            if(value==away)return;
            if(value){Flush();away=true;}
            else{away=false;SettleOffline();}
        }
        void RequestSave() => saveRequested = true;
        void OnSpend(int _) => RequestSave();
        void OnDestroy()
        {
            if (wallet != null) wallet.CoinsSpent -= OnSpend;
            if (expansion != null) expansion.PurchaseCompleted -= RequestSave;
            if (goals != null) goals.ProgressChanged -= RequestSave;
            if(partsWallet!=null)partsWallet.Changed-=RequestSave;
            if (tableUpgrades != null) tableUpgrades.Changed -= RequestSave;
        }
        void OnApplicationPause(bool paused) {appPaused=paused;SetAway(appPaused||appUnfocused); }
        void OnApplicationFocus(bool focused) {appUnfocused=!focused;SetAway(appPaused||appUnfocused); }
        void OnApplicationQuit()
        {
            if (phase == SaveSessionPhase.Running) Flush();
        }
    }
}
