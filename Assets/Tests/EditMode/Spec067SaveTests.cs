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
using UnityEngine;
using Object = UnityEngine.Object;

namespace BurgerShop.Tests.EditMode
{
    public sealed class Spec067SaveTests
    {
        string directory;
        LocalSaveStore store;
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

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "BurgerShopSpec067-" + Guid.NewGuid().ToString("N"));
            store = new LocalSaveStore(directory);
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null) Object.DestroyImmediate(root);
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }

        [Test]
        public void Version17RoundTripRestoresBusinessFields()
        {
            var original = BusinessSnapshot();
            Assert.That(original.IsValid, Is.True);
            Assert.That(store.Save(original), Is.True);

            Assert.That(new LocalSaveStore(directory).Load(out var loaded), Is.EqualTo(SaveLoadResult.Loaded));
            Assert.That(loaded.version, Is.EqualTo(RestaurantSaveData.CurrentVersion));
            Assert.That(loaded.version, Is.EqualTo(17));
            Assert.That(loaded.coins, Is.EqualTo(4321));
            Assert.That(loaded.shopRank, Is.EqualTo(10));
            Assert.That(loaded.upgradeStars, Is.EqualTo(4));
            Assert.That(loaded.milestoneMask, Is.EqualTo(ShopRanks.MilestoneMask));
            Assert.That(loaded.hiredWorkerCount, Is.EqualTo(2));
            Assert.That(loaded.playerSpeedTier, Is.EqualTo(3));
            Assert.That(loaded.playerCarryTier, Is.EqualTo(2));
            Assert.That(loaded.staffSpeedTier, Is.EqualTo(1));
            Assert.That(loaded.staffCarryTier, Is.EqualTo(4));
            Assert.That(loaded.table0Set, Is.EqualTo((int)TableSetId.Bistro));
            Assert.That(loaded.table0Investment, Is.EqualTo(50));
            Assert.That(loaded.table1Set, Is.EqualTo((int)TableSetId.Diner));
            Assert.That(loaded.table1Investment, Is.EqualTo(80));
            Assert.That(loaded.table2Set, Is.EqualTo((int)TableSetId.Patio));
            Assert.That(loaded.table2Investment, Is.EqualTo(120));
            Assert.That(loaded.boughtSideWing, Is.True);
            Assert.That(loaded.wingInvestment, Is.EqualTo(ShopExpansion.WingCost));
            Assert.That(loaded.restroomBuilt, Is.True);
            Assert.That(loaded.restroomInvestment, Is.EqualTo(RestroomExpansion.Cost));
            Assert.That(loaded.westExpanded, Is.True);
            Assert.That(loaded.layout, Is.Not.Null);
            Assert.That(loaded.layout.Length, Is.EqualTo(1));
            Assert.That(loaded.layout[0].id, Is.EqualTo("pair-1"));
            Assert.That(loaded.layout[0].kind, Is.EqualTo((int)FacilityKind.PairTable));
            Assert.That(loaded.layout[0].tableSet, Is.EqualTo((int)TableSetId.Bistro));
            Assert.That(loaded.layout[0].investment, Is.EqualTo(50));
            Assert.That(store.FilePath, Does.Not.Contain(Application.persistentDataPath));
        }

        [Test]
        public void Version14LoadKeepsCoinsAndStarsWithoutCharging()
        {
            var old = new RestaurantSaveData
            {
                version = 14, shopRank = 3, grillLevel = 1, coins = 321, upgradeStars = 7,
                milestoneMask = 0, incomeRemainder = 0, colaLevel = 1
            };
            Assert.That(old.IsValid, Is.True);
            Assert.That(store.Save(old), Is.True);

            BuildKitchen();
            persistence.Configure(wallet, grill.Upgrade, hiring, null, null, null, goals, directory);
            Assert.That(wallet.Coins, Is.EqualTo(321));
            Assert.That(goals.Stars, Is.EqualTo(7));
            Assert.That(goals.Rank, Is.EqualTo(3));
            Assert.That(wallet.Coins, Is.EqualTo(321), "reload must not charge again");
        }

        [Test]
        public void CorruptPrimaryRecoversBackupSnapshot()
        {
            var first = BusinessSnapshot();
            first.coins = 888;
            Assert.That(store.Save(first), Is.True);
            first.coins = 777;
            Assert.That(store.Save(first), Is.True);
            string bytes = File.ReadAllText(store.FilePath).Replace("\"coins\": 777", "\"coins\": 1");
            File.WriteAllText(store.FilePath, bytes);

            Assert.That(new LocalSaveStore(directory).Load(out var recovered), Is.EqualTo(SaveLoadResult.RecoveredBackup));
            Assert.That(recovered.coins, Is.EqualTo(888));
            Assert.That(recovered.shopRank, Is.EqualTo(10));
        }

        [Test]
        public void FlushOmitsSessionTrashAndCashPiles()
        {
            BuildKitchen();
            persistence.Configure(wallet, grill.Upgrade, hiring, null, null, null, goals, directory);
            dining.Tables[0].LeaveMealTrash(0);
            Assert.That(dining.Tables[0].TrashCount, Is.GreaterThan(0));
            cash.DropAtCounter(10);
            Assert.That(cash.PileCount, Is.GreaterThan(0));
            Assert.That(persistence.Flush(), Is.True);

            string json = File.ReadAllText(store.FilePath);
            StringAssert.DoesNotContain("trashCount", json);
            StringAssert.DoesNotContain("cashPiles", json);
            StringAssert.DoesNotContain("openingStep", json);
            StringAssert.DoesNotContain("SoundEnabled", json);
            StringAssert.DoesNotContain("PileCount", json);

            Object.DestroyImmediate(root);
            root = null;
            BuildKitchen();
            persistence.Configure(wallet, grill.Upgrade, hiring, null, null, null, goals, directory);
            Assert.That(dining.Tables[0].TrashCount, Is.Zero);
            Assert.That(cash.PileCount, Is.Zero);
            Assert.That(wallet.Coins, Is.EqualTo(200));
        }

        [Test]
        public void CleanTableRankUpFlushesShopRankWithoutHeartbeat()
        {
            BuildKitchen();
            persistence.Configure(wallet, grill.Upgrade, hiring, null, null, null, goals, directory);
            goals.Restore(2, 0, 0, ShopRanks.StarCap(2) - 2);
            Assert.That(persistence.Flush(), Is.True);
            Assert.That(new LocalSaveStore(directory).Load(out var before), Is.EqualTo(SaveLoadResult.Loaded));
            Assert.That(before.shopRank, Is.EqualTo(2));

            dining.Tables[0].LeaveMealTrash(0);
            var trash = root.GetComponent<TrashInventory>() ?? root.AddComponent<TrashInventory>();
            dining.Tables[0].BindCollector(trash);
            while (dining.Tables[0].TrashCount > 0)
                Assert.That(dining.Tables[0].TryPickupTrash(trash), Is.True);

            Assert.That(goals.Rank, Is.EqualTo(2));
            Assert.That(goals.CanUpgrade, Is.True);
            Assert.That(new LocalSaveStore(directory).Load(out var ready), Is.EqualTo(SaveLoadResult.Loaded));
            Assert.That(ready.shopRank, Is.EqualTo(2));
            Assert.That(ready.upgradeStars, Is.EqualTo(ShopRanks.StarCap(2)));
            Assert.That(goals.TryUpgradeRank(2), Is.True);
            Assert.That(goals.Rank, Is.EqualTo(3));
            Assert.That(new LocalSaveStore(directory).Load(out var after), Is.EqualTo(SaveLoadResult.Loaded));
            Assert.That(after.shopRank, Is.EqualTo(3));
            Assert.That(after.upgradeStars, Is.Zero);
        }

        [Test]
        public void PauseFlushesUnwrittenSpendImmediately()
        {
            BuildKitchen();
            wallet.RestoreProgress(200, 0);
            persistence.Configure(wallet, grill.Upgrade, hiring, null, null, null, goals, directory);
            Assert.That(persistence.Flush(), Is.True);
            Assert.That(wallet.TrySpend(30), Is.True);
            Assert.That(wallet.Coins, Is.EqualTo(170));
            Assert.That(new LocalSaveStore(directory).Load(out var pending), Is.EqualTo(SaveLoadResult.Loaded));
            Assert.That(pending.coins, Is.EqualTo(200), "spend waits for LateUpdate or pause");

            typeof(RestaurantPersistence)
                .GetMethod("OnApplicationPause", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(persistence, new object[] { true });
            Assert.That(new LocalSaveStore(directory).Load(out var saved), Is.EqualTo(SaveLoadResult.Loaded));
            Assert.That(saved.coins, Is.EqualTo(170));
        }

        static RestaurantSaveData BusinessSnapshot() => new RestaurantSaveData
        {
            version = RestaurantSaveData.CurrentVersion,
            coins = 4321,
            completedSales = 12,
            grillLevel = 2,
            shopRank = 10,
            upgradeStars = 4,
            milestoneMask = ShopRanks.MilestoneMask,
            hiredWorkerCount = 2,
            workerHired = true,
            workerDeliveries = 5,
            workerClears = 2,
            playerSpeedTier = 3,
            playerCarryTier = 2,
            staffSpeedTier = 1,
            staffCarryTier = 4,
            table0Set = (int)TableSetId.Bistro,
            table0Investment = 50,
            table1Set = (int)TableSetId.Diner,
            table1Investment = 80,
            table2Set = (int)TableSetId.Patio,
            table2Investment = 120,
            boughtSideWing = true,
            wingInvestment = ShopExpansion.WingCost,
            restroomBuilt = true,
            restroomInvestment = RestroomExpansion.Cost,
            westExpanded = true,
            colaLevel = 1,
            layout = new[]
            {
                new FacilityPlacementRecord
                {
                    id = "pair-1", kind = (int)FacilityKind.PairTable, purchased = true,
                    x = 2f, z = 3f, yaw = 15f, level = 2,
                    tableSet = (int)TableSetId.Bistro,
                    investment = 50
                }
            }
        };

        void BuildKitchen()
        {
            root = new GameObject("Spec067");
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
            root.AddComponent<TrashInventory>();
        }
    }
}
