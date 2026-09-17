using System;
using System.IO;
using BurgerShop.Building;
using BurgerShop.Customer;
using BurgerShop.Economy;
using BurgerShop.Persistence;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using BurgerShop.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BurgerShop.Tests.EditMode
{
    public sealed class Spec068SaveSlotTests
    {
        string directory;
        GameObject root;
        BurgerInventory player;
        RestaurantWallet wallet;
        SessionGoalTracker goals;
        DiningArea dining;
        WorkerHiringZone hiring;
        ExpandableGrill grill;
        CashFloor cash;
        RestaurantPersistence persistence;
        TableUpgradeBoard tables;
        float previousTimeScale;

        [SetUp]
        public void SetUp()
        {
            previousTimeScale = Time.timeScale;
            directory = Path.Combine(Path.GetTempPath(), "BurgerShopSpec068-" + Guid.NewGuid().ToString("N"));
            SessionState.SetString(RestaurantPersistence.EditorDirectoryKey, directory);
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = previousTimeScale;
            SessionState.EraseString(RestaurantPersistence.EditorDirectoryKey);
            if (root != null) Object.DestroyImmediate(root);
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
            Assert.That(directory, Does.Not.Contain(Application.persistentDataPath));
        }

        [Test]
        public void LegacyFileWithoutManifestMigratesToSlot1KeepingCoinsAndRank()
        {
            var original = Progress(4321, 10);
            Assert.That(new LocalSaveStore(directory).Save(original), Is.True);
            Assert.That(File.Exists(Path.Combine(directory, "restaurant-slots.json")), Is.False);

            BuildKitchen();
            persistence.Configure(wallet, grill.Upgrade, hiring, null, null, null, goals, directory);
            Assert.That(wallet.Coins, Is.EqualTo(4321));
            Assert.That(goals.Rank, Is.EqualTo(10));
            Assert.That(persistence.ActiveSlotId, Is.EqualTo(1));
            Assert.That(File.Exists(Path.Combine(directory, SaveSlotStore.ManifestFileName)), Is.True);
            Assert.That(File.Exists(Path.Combine(directory, SaveSlotStore.LegacyFileName)), Is.True);
            Assert.That(persistence.SlotSummaries.Length, Is.EqualTo(1));
            Assert.That(persistence.SlotSummaries[0].id, Is.EqualTo(1));
            Assert.That(persistence.SlotSummaries[0].coins, Is.EqualTo(4321));
            Assert.That(persistence.SlotSummaries[0].shopRank, Is.EqualTo(10));
            Assert.That(persistence.FilePath, Does.Not.Contain(Application.persistentDataPath));
            Assert.That(persistence.FilePath, Does.EndWith(SaveSlotStore.LegacyFileName));
        }

        [Test]
        public void NewGameFlushesOldSlotAndStartsRankOneZeroCoins()
        {
            Assert.That(new LocalSaveStore(directory).Save(Progress(888, 4)), Is.True);
            BuildKitchen();
            persistence.Configure(wallet, grill.Upgrade, hiring, null, null, null, goals, directory);
            Assert.That(wallet.Coins, Is.EqualTo(888));
            Assert.That(goals.Rank, Is.EqualTo(4));

            Assert.That(persistence.StartNewGame(), Is.True);
            Assert.That(persistence.ActiveSlotId, Is.EqualTo(2));
            Assert.That(goals.Rank, Is.EqualTo(ShopRanks.Min));
            Assert.That(wallet.Coins, Is.EqualTo(0));
            Assert.That(goals.Opening != null && goals.Opening.IsActive, Is.True);

            Assert.That(new LocalSaveStore(directory, SaveSlotStore.LegacyFileName).Load(out var old), Is.EqualTo(SaveLoadResult.Loaded));
            Assert.That(old.coins, Is.EqualTo(888));
            Assert.That(old.shopRank, Is.EqualTo(4));
            Assert.That(File.Exists(Path.Combine(directory, SaveSlotStore.NumberedFileName(2))), Is.False);
            Assert.That(persistence.FilePath, Does.Not.Contain(Application.persistentDataPath));
        }

        [Test]
        public void SwitchAToBToARestoresACoinsAndRank()
        {
            Assert.That(new LocalSaveStore(directory).Save(Progress(1500, 5)), Is.True);
            BuildKitchen();
            persistence.Configure(wallet, grill.Upgrade, hiring, null, null, null, goals, directory);
            int slotA = persistence.ActiveSlotId;
            Assert.That(persistence.StartNewGame(), Is.True);
            int slotB = persistence.ActiveSlotId;
            Assert.That(slotB, Is.Not.EqualTo(slotA));
            wallet.RestoreProgress(40, 0);
            goals.Restore(2, 0, 0);
            Assert.That(persistence.Flush(), Is.True);

            Assert.That(persistence.SwitchToSlot(slotA), Is.True);
            Assert.That(wallet.Coins, Is.EqualTo(1500));
            Assert.That(goals.Rank, Is.EqualTo(5));

            Assert.That(persistence.SwitchToSlot(slotB), Is.True);
            Assert.That(wallet.Coins, Is.EqualTo(40));
            Assert.That(goals.Rank, Is.EqualTo(2));

            Assert.That(persistence.SwitchToSlot(slotA), Is.True);
            Assert.That(wallet.Coins, Is.EqualTo(1500));
            Assert.That(goals.Rank, Is.EqualTo(5));
        }

        [Test]
        public void GearSitsBesideSupermarketOnTheRight()
        {
            root = new GameObject("Spec068Hud", typeof(RectTransform), typeof(Canvas));
            var canvas = (RectTransform)root.transform;
            canvas.sizeDelta = new Vector2(1080f, 1920f);
            var layout = root.AddComponent<FacilityLayout>();
            FacilityShopHud.Build(canvas, layout);
            var save = root.AddComponent<RestaurantPersistence>();
            var hud = SaveSlotsHud.Build(canvas, save);
            var cart = GameObject.Find("ShoppingCart").GetComponent<RectTransform>();
            var gear = GameObject.Find("SettingsGear").GetComponent<RectTransform>();
            Assert.That(cart, Is.Not.Null);
            Assert.That(gear, Is.Not.Null);
            Assert.That(gear.parent, Is.EqualTo(cart.parent));
            Assert.That(gear.sizeDelta, Is.EqualTo(cart.sizeDelta));
            Assert.That(gear.anchoredPosition.y, Is.EqualTo(cart.anchoredPosition.y));
            Assert.That(gear.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(cart.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(gear.anchoredPosition.x, Is.LessThan(cart.anchoredPosition.x));
            Assert.That(Mathf.Abs(gear.anchoredPosition.x - cart.anchoredPosition.x), Is.EqualTo(cart.sizeDelta.x + 8f).Within(0.5f));
            Assert.That(gear.Find("GearIcon"), Is.Not.Null);

            float previous = Time.timeScale;
            if (previous <= 0f) { Time.timeScale = 1f; previous = 1f; }
            hud.Open();
            Assert.That(hud.IsOpen, Is.True);
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(GameObject.Find("SavesPanel"), Is.Not.Null);
            hud.Close();
            Assert.That(Time.timeScale, Is.EqualTo(previous));
            Assert.That(hud.IsOpen, Is.False);
        }

        [Test]
        public void IsolatedDirectoryNeverTouchesPersistentDataPath()
        {
            BuildKitchen();
            persistence.Configure(wallet, grill.Upgrade, hiring, null, null, null, goals, directory);
            persistence.Flush();
            Assert.That(persistence.FilePath, Does.StartWith(directory));
            Assert.That(persistence.FilePath, Does.Not.Contain(Application.persistentDataPath));
            Assert.That(File.Exists(persistence.FilePath), Is.True);
            StringAssert.StartsWith(directory, persistence.FilePath);
        }

        [Test]
        public void DeleteRefusesCurrentAndLastSlot()
        {
            Assert.That(new LocalSaveStore(directory).Save(Progress(100, 3)), Is.True);
            BuildKitchen();
            persistence.Configure(wallet, grill.Upgrade, hiring, null, null, null, goals, directory);
            int slotA = persistence.ActiveSlotId;
            Assert.That(persistence.StartNewGame(), Is.True);
            Assert.That(persistence.Flush(), Is.True);
            int slotB = persistence.ActiveSlotId;
            Assert.That(persistence.CanDeleteSlot(slotB), Is.False);
            Assert.That(persistence.DeleteSlot(slotB), Is.False);
            Assert.That(File.Exists(Path.Combine(directory, SaveSlotStore.LegacyFileName)), Is.True);

            Assert.That(persistence.CanDeleteSlot(slotA), Is.True);
            Assert.That(persistence.DeleteSlot(slotA), Is.True);
            Assert.That(File.Exists(Path.Combine(directory, SaveSlotStore.LegacyFileName)), Is.False);
            Assert.That(persistence.CanDeleteSlot(slotB), Is.False);
            Assert.That(persistence.DeleteSlot(slotB), Is.False);
            Assert.That(persistence.SlotSummaries.Length, Is.EqualTo(1));
        }

        static RestaurantSaveData Progress(long coins, int rank) => new RestaurantSaveData
        {
            version = RestaurantSaveData.CurrentVersion,
            coins = coins,
            grillLevel = 1,
            shopRank = rank,
            colaLevel = 1
        };

        void BuildKitchen()
        {
            root = new GameObject("Spec068");
            player = new GameObject("Player").AddComponent<BurgerInventory>();
            player.transform.SetParent(root.transform);
            player.transform.position = ShopLayout.PlayerSpawn;
            wallet = root.AddComponent<RestaurantWallet>();
            wallet.RestoreProgress(200, 0);
            dining = DiningArea.Create(root.transform, ShopLayout.Tables);
            cash = root.AddComponent<CashFloor>();
            cash.Configure(wallet, player.transform, ShopLayout.CounterCash);
            dining.BindCash(cash);
            grill = ExpandableGrill.CreateStarter(root.transform, player, wallet);
            var queue = new GameObject("DiningQueue").AddComponent<CustomerQueue>();
            queue.transform.SetParent(root.transform);
            queue.CustomerKindFactory = () => CustomerKind.Normal;
            queue.OrderQuantityFactory = () => 1;
            queue.Configure(ShopLayout.Entrance, ShopLayout.QueueEntry, ShopLayout.QueueSlots, ShopLayout.Counter);
            var stock = ShopFixtures.CreateCounterStock(queue.transform, ShopLayout.CounterTop);
            var point = new GameObject("ServePoint").transform;
            point.SetParent(root.transform);
            point.position = ShopLayout.ServingCircle;
            var drop = root.AddComponent<CounterDropZone>();
            drop.Configure(stock, point);
            var serving = root.AddComponent<BurgerServingZone>();
            serving.Configure(queue, player, wallet, point, ShopLayout.Exit, stock, dining, drop, 10, cash);
            var bin = TrashBin.Create(root.transform, ShopLayout.TrashBin);
            hiring = root.AddComponent<WorkerHiringZone>();
            var pickup = new GameObject("WorkerPickup").transform;
            pickup.SetParent(root.transform);
            pickup.position = ShopLayout.GrillPickup;
            hiring.Configure(grill.Station, serving, wallet, player, pickup, root.transform, ShopLayout.Aisle, drop, null, dining, bin);
            goals = root.AddComponent<SessionGoalTracker>();
            goals.Configure(player, grill.Station, stock, queue, wallet, serving, dining, null, hiring);
            tables = root.AddComponent<TableUpgradeBoard>();
            tables.Configure(dining, null, wallet, player);
            persistence = root.AddComponent<RestaurantPersistence>();
            persistence.UtcNowTicks = () => 600000000000000000L;
            root.AddComponent<TrashInventory>();
        }
    }
}
