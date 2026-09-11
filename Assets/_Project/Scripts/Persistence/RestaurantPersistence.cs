using BurgerShop.Economy;
using BurgerShop.Restaurant;
using UnityEngine;

namespace BurgerShop.Persistence
{
    public sealed class RestaurantPersistence : MonoBehaviour
    {
        public const string EditorDirectoryKey = "BurgerShop.Tests.SaveDirectory";
        RestaurantWallet wallet;
        GrillUpgradeZone upgrade;
        WorkerHiringZone hiring;
        LocalSaveStore store;
        string lastChecksum;
        float elapsed;
        bool ready;

        public SaveLoadResult LoadResult { get; private set; }
        public string Status { get; private set; } = "NEW GAME - AUTOSAVE ON";
        public string FilePath => store?.FilePath;

        public void Configure(RestaurantWallet earnings, GrillUpgradeZone grill, WorkerHiringZone staff, string directory = null)
        {
            wallet = earnings;
            upgrade = grill;
            hiring = staff;
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
                hiring.RestoreWorker(data.workerHired, data.workerDeliveries);
                lastChecksum = data.Checksum();
            }
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
            if (elapsed < 2f) return;
            elapsed = 0f;
            Flush();
        }

        public bool Flush()
        {
            if (!ready || wallet == null || upgrade == null || hiring == null || !store.CanWrite) return false;
            var data = new RestaurantSaveData
            {
                version = 1, coins = wallet.Coins, completedSales = wallet.CompletedSales,
                grillLevel = upgrade.Level, workerHired = hiring.IsHired,
                workerDeliveries = hiring.Worker != null ? hiring.Worker.CompletedDeliveries : 0
            };
            string checksum = data.Checksum();
            if (checksum == lastChecksum) return true;
            if (!store.Save(data)) { Status = "SAVE FAILED - RETRYING"; return false; }
            lastChecksum = checksum;
            Status = "PROGRESS SAVED";
            return true;
        }

        void OnApplicationPause(bool paused) { if (paused) Flush(); }
        void OnApplicationFocus(bool focused) { if (!focused) Flush(); }
        void OnApplicationQuit() => Flush();
    }
}
