using System;
using System.IO;
using BurgerShop.Customer;
using BurgerShop.Economy;
using BurgerShop.Persistence;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using BurgerShop.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BurgerShop.Tests.EditMode
{
    public sealed class Spec078SlotSwitchIsolationTests
    {
        string directory;
        GameObject root;
        BurgerInventory player;
        RestaurantWallet wallet;
        SessionGoalTracker goals;
        DiningArea dining;
        WorkerHiringZone hiring;
        ExpandableGrill grill;
        ShopExpansion expansion;
        RestaurantPersistence persistence;

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "BurgerShopSpec078-" + Guid.NewGuid().ToString("N"));
            SessionState.SetString(RestaurantPersistence.EditorDirectoryKey, directory);
        }

        [TearDown]
        public void TearDown()
        {
            SessionState.EraseString(RestaurantPersistence.EditorDirectoryKey);
            if (root != null) Object.DestroyImmediate(root);
            ShopLayout.ResetWingLock();
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
            Assert.That(directory, Does.Not.Contain(Application.persistentDataPath));
        }

        [Test]
        public void SwitchToSlotDoesNotApplyTargetOntoTheLiveWorld()
        {
            Assert.That(new LocalSaveStore(directory).Save(Progress(1500, 5)), Is.True);
            BuildKitchen();
            Bind();
            int slotA = persistence.ActiveSlotId;
            Assert.That(wallet.Coins, Is.EqualTo(1500));
            Assert.That(persistence.StartNewGame(), Is.True);
            Assert.That(persistence.Phase, Is.EqualTo(SaveSessionPhase.Hydrating));
            Assert.That(wallet.Coins, Is.EqualTo(1500));
            Assert.That(goals.Rank, Is.EqualTo(5));
            ReloadKitchen();
            int slotB = persistence.ActiveSlotId;
            wallet.RestoreProgress(40, 0);
            goals.Restore(2, 0, 0);
            Assert.That(persistence.Flush(), Is.True);

            Assert.That(persistence.SwitchToSlot(slotA), Is.True);
            Assert.That(persistence.ActiveSlotId, Is.EqualTo(slotA));
            Assert.That(persistence.Phase, Is.EqualTo(SaveSessionPhase.Hydrating));
            Assert.That(wallet.Coins, Is.EqualTo(40));
            Assert.That(goals.Rank, Is.EqualTo(2));
            Assert.That(persistence.Flush(), Is.False);
            Assert.That(persistence.SwitchToSlot(slotB), Is.False);
            Assert.That(new LocalSaveStore(directory, SaveSlotStore.LegacyFileName).Load(out var fileA), Is.EqualTo(SaveLoadResult.Loaded));
            Assert.That(fileA.coins, Is.EqualTo(1500));

            ReloadKitchen();
            Assert.That(persistence.Phase, Is.EqualTo(SaveSessionPhase.Running));
            Assert.That(wallet.Coins, Is.EqualTo(1500));
            Assert.That(goals.Rank, Is.EqualTo(5));
        }

        [Test]
        public void ExtraGrillOnADoesNotFollowSwitchIntoB()
        {
            Assert.That(new LocalSaveStore(directory).Save(Progress(220, 3, extraGrill: true)), Is.True);
            BuildKitchen();
            Bind();
            int slotA = persistence.ActiveSlotId;
            Assert.That(expansion.HasExtraGrill, Is.True);
            Assert.That(persistence.Flush(), Is.True);

            Assert.That(persistence.StartNewGame(), Is.True);
            Assert.That(expansion.HasExtraGrill, Is.True, "old world must stay dirty until rebuild");
            ReloadKitchen();
            int slotB = persistence.ActiveSlotId;
            Assert.That(slotB, Is.Not.EqualTo(slotA));
            Assert.That(expansion.HasExtraGrill, Is.False);
            Assert.That(persistence.Flush(), Is.True);
            Assert.That(LoadSlot(slotB).boughtExtraGrill, Is.False);

            Assert.That(persistence.SwitchToSlot(slotA), Is.True);
            Assert.That(expansion.HasExtraGrill, Is.False, "must not merge A onto live B");
            Assert.That(persistence.Flush(), Is.False);
            Assert.That(LoadSlot(slotB).boughtExtraGrill, Is.False);
            ReloadKitchen();
            Assert.That(expansion.HasExtraGrill, Is.True);

            Assert.That(persistence.SwitchToSlot(slotB), Is.True);
            Assert.That(expansion.HasExtraGrill, Is.True, "live world A is not overwritten before rebuild");
            Assert.That(wallet.Coins, Is.EqualTo(220));
            persistence.Advance(2f);
            goals.EvaluateStarGateTasks();
            Assert.That(persistence.Flush(), Is.False);
            Assert.That(LoadSlot(slotB).boughtExtraGrill, Is.False);

            ReloadKitchen();
            Assert.That(persistence.Phase, Is.EqualTo(SaveSessionPhase.Running));
            Assert.That(expansion.HasExtraGrill, Is.False);
            Assert.That(persistence.Flush(), Is.True);
            var savedB = LoadSlot(slotB);
            Assert.That(savedB.boughtExtraGrill, Is.False);
            Assert.That(savedB.extraGrillLevel, Is.Zero);
            Assert.That(LoadSlot(slotA).boughtExtraGrill, Is.True);
        }

        [Test]
        public void FailedFlushAbortsSwitchAndKeepsWorldA()
        {
            Assert.That(new LocalSaveStore(directory).Save(Progress(300, 2, extraGrill: true)), Is.True);
            BuildKitchen();
            Bind();
            int slotA = persistence.ActiveSlotId;
            Assert.That(persistence.StartNewGame(), Is.True);
            ReloadKitchen();
            int slotB = persistence.ActiveSlotId;
            Assert.That(persistence.SwitchToSlot(slotA), Is.True);
            ReloadKitchen();
            Assert.That(expansion.HasExtraGrill, Is.True);
            Assert.That(persistence.Phase, Is.EqualTo(SaveSessionPhase.Running));

            wallet.RestoreProgress(wallet.Coins + 11, 0);
            string blocker = persistence.FilePath + ".tmp";
            Directory.CreateDirectory(blocker);
            try
            {
                Assert.That(persistence.SwitchToSlot(slotB), Is.False);
                Assert.That(persistence.ActiveSlotId, Is.EqualTo(slotA));
                Assert.That(persistence.Phase, Is.EqualTo(SaveSessionPhase.Running));
                Assert.That(root, Is.Not.Null);
                Assert.That(expansion.HasExtraGrill, Is.True);
                Assert.That(wallet.Coins, Is.EqualTo(311));
                Assert.That(LoadSlot(slotB).boughtExtraGrill, Is.False);
            }
            finally
            {
                if (Directory.Exists(blocker)) Directory.Delete(blocker, true);
            }
        }

        RestaurantSaveData LoadSlot(int id)
        {
            string name = id <= 1 ? SaveSlotStore.LegacyFileName : SaveSlotStore.NumberedFileName(id);
            Assert.That(new LocalSaveStore(directory, name).Load(out var data), Is.EqualTo(SaveLoadResult.Loaded));
            return data;
        }

        static RestaurantSaveData Progress(long coins, int rank, bool extraGrill = false) => new RestaurantSaveData
        {
            version = RestaurantSaveData.CurrentVersion,
            coins = coins,
            grillLevel = 1,
            shopRank = rank,
            colaLevel = 1,
            boughtExtraGrill = extraGrill,
            extraGrillLevel = extraGrill ? 1 : 0
        };

        void ReloadKitchen()
        {
            if (root != null) Object.DestroyImmediate(root);
            ShopLayout.ResetWingLock();
            BuildKitchen();
            Bind();
        }

        void Bind()
        {
            persistence.Configure(wallet, grill.Upgrade, hiring, null, expansion, null, goals, directory);
        }

        void BuildKitchen()
        {
            root = new GameObject("Spec078");
            player = new GameObject("Player").AddComponent<BurgerInventory>();
            player.transform.SetParent(root.transform);
            player.transform.position = ShopLayout.PlayerSpawn;
            player.Configure();
            wallet = root.AddComponent<RestaurantWallet>();
            wallet.RestoreProgress(200, 0);
            dining = DiningArea.Create(root.transform, ShopLayout.Tables);
            var cash = root.AddComponent<CashFloor>();
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
            expansion = ShopExpansion.Create(root.transform, dining, serving, hiring, player, wallet, cash);
            goals = root.AddComponent<SessionGoalTracker>();
            goals.Configure(player, grill.Station, stock, queue, wallet, serving, dining, null, hiring, null, expansion);
            persistence = root.AddComponent<RestaurantPersistence>();
            persistence.UtcNowTicks = () => 600000000000000000L;
            root.AddComponent<TrashInventory>();
        }
    }
}
