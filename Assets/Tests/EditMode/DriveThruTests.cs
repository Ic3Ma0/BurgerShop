using System;
using System.IO;
using BurgerShop.Customer;
using BurgerShop.Economy;
using BurgerShop.Persistence;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using BurgerShop.UI;
using NUnit.Framework;
using UnityEngine;

namespace BurgerShop.Tests.EditMode
{
    public sealed class DriveThruTests
    {
        GameObject root;
        ShopExpansion expansion;
        DiningArea dining;
        BurgerServingZone serving;
        WorkerHiringZone hiring;
        BurgerInventory player;
        RestaurantWallet wallet;
        CounterStock stock;
        CustomerQueue queue;
        ProductionStation grill;
        CashFloor cash;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("DriveThruTest");
            player = new GameObject("Player").AddComponent<BurgerInventory>();
            player.transform.SetParent(root.transform);
            player.Configure();
            Transform output = new GameObject("Output").transform;
            output.SetParent(root.transform);
            grill = root.AddComponent<ProductionStation>();
            grill.Configure(output, null, null);
            wallet = root.AddComponent<RestaurantWallet>();
            queue = root.AddComponent<CustomerQueue>();
            queue.OrderQuantityFactory = () => 1;
            queue.Configure(ShopLayout.Entrance, ShopLayout.QueueEntry, ShopLayout.QueueSlots, ShopLayout.Counter);
            Transform point = new GameObject("Circle").transform;
            point.SetParent(root.transform);
            point.position = ShopLayout.ServingCircle;
            Transform anchor = new GameObject("Anchor").transform;
            anchor.SetParent(root.transform);
            stock = root.AddComponent<CounterStock>();
            stock.Configure(anchor, null);
            CounterDropZone drop = root.AddComponent<CounterDropZone>();
            drop.Configure(stock, point);
            dining = DiningArea.Create(root.transform, ShopLayout.Tables);
            cash = root.AddComponent<CashFloor>();
            serving = root.AddComponent<BurgerServingZone>();
            serving.Configure(queue, player, wallet, point, ShopLayout.Exit, stock, dining, drop, 10, cash);
            cash.Configure(wallet, player.transform, ShopLayout.CounterCash);
            dining.BindCash(cash);
            hiring = root.AddComponent<WorkerHiringZone>();
            hiring.Configure(grill, serving, wallet, player, root.transform, root.transform, ShopLayout.Aisle, drop,
                null, dining, null);
            expansion = ShopExpansion.Create(root.transform, dining, serving, hiring, player, wallet, cash);
        }

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(root);

        void Hold(FacilityUnlockZone pad, float seconds)
        {
            player.transform.position = pad.PadPosition + Vector3.up;
            for (int i = 0; i < Mathf.CeilToInt(seconds * 60f); i++) pad.Advance(1f / 60f);
        }

        void GiveLoose(int count = 1)
        {
            grill.Advance(12f);
            for (int i = 0; i < count; i++)
                Assert.That(player.TryCollectFrom(grill), Is.True);
        }

        void UnlockBoxing()
        {
            wallet.RestoreProgress(Math.Max(wallet.Coins, ShopExpansion.BoxingCost), wallet.CompletedSales);
            Hold(expansion.BoxingPad, 3f);
            Assert.That(expansion.HasBoxing, Is.True);
        }

        void UnlockLane()
        {
            wallet.RestoreProgress(Math.Max(wallet.Coins, ShopExpansion.DriveThruCost), wallet.CompletedSales);
            Hold(expansion.DriveThruPad, 3f);
            Assert.That(expansion.HasDriveThru, Is.True);
            expansion.DriveThru.OrderQuantityFactory = () => 1;
        }

        void WaitForCar(float seconds = 5f)
        {
            for (int i = 0; i < Mathf.CeilToInt(seconds * 60f); i++)
            {
                expansion.DriveThru.Advance(1f / 60f);
                if (expansion.DriveThru.HasWaitingCar) return;
            }
        }

        void StockPackageFromHand()
        {
            player.transform.position = expansion.Boxing.CirclePosition + Vector3.up;
            expansion.Boxing.Advance(2f);
            player.transform.position = expansion.Boxing.DropPosition + Vector3.up;
            expansion.Boxing.Advance(0.3f);
        }

        void BoxAndStockOne()
        {
            GiveLoose();
            UnlockBoxing();
            player.transform.position = expansion.Boxing.CirclePosition + Vector3.up;
            Assert.That(expansion.Boxing.TryBoxFrom(player), Is.True);
            StockPackageFromHand();
            Assert.That(player.BoxedCount, Is.Zero);
            Assert.That(expansion.Boxing.PackageCount, Is.EqualTo(1));
        }

        void FinishHandoff()
        {
            for (int i = 0; i < 120 && expansion.DriveThru.IsHandoffActive; i++)
                expansion.DriveThru.Advance(1f / 60f);
            Assert.That(expansion.DriveThru.IsHandoffActive, Is.False);
        }

        static float Horizontal(Vector3 a, Vector3 b)
        {
            a.y = b.y = 0f;
            return Vector3.Distance(a, b);
        }

        [Test]
        public void LockedStationCannotBoxLooseBurgers()
        {
            GiveLoose();
            Assert.That(expansion.HasBoxing, Is.False);
            Assert.That(player.LooseCount, Is.EqualTo(1));
            player.transform.position = ShopLayout.BoxingCircle + Vector3.up;
            for (int i = 0; i < 30; i++) expansion.BoxingPad.Advance(1f / 60f);
            Assert.That(player.LooseCount, Is.EqualTo(1));
            Assert.That(player.BoxedCount, Is.Zero);
            Assert.That(GameObject.Find("BoxingStation"), Is.Null);
            Assert.That(expansion.Boxing, Is.Null);
        }

        [Test]
        public void UnlockingBoxingConvertsLooseIntoABox()
        {
            GiveLoose();
            UnlockBoxing();
            Assert.That(GameObject.Find("BoxingCircle"), Is.Not.Null);
            Assert.That(GameObject.Find("PackageDesk"), Is.Not.Null);
            Assert.That(player.LooseCount, Is.EqualTo(1));
            Assert.That(player.BoxedCount, Is.Zero);
            player.transform.position = expansion.Boxing.CirclePosition + Vector3.up;
            Assert.That(expansion.Boxing.TryBoxFrom(player), Is.True);
            Assert.That(player.LooseCount, Is.Zero);
            Assert.That(player.BoxedCount, Is.Zero, "raw first belongs to the table");
            Assert.That(expansion.Boxing.InputCount, Is.EqualTo(1));
            expansion.Boxing.Advance(2f);
            Assert.That(player.BoxedCount, Is.EqualTo(1));
        }

        [Test]
        public void BoxedBurgerStocksThePackageCounter()
        {
            GiveLoose();
            UnlockBoxing();
            player.transform.position = expansion.Boxing.CirclePosition + Vector3.up;
            expansion.Boxing.TryBoxFrom(player);
            StockPackageFromHand();
            Assert.That(player.BoxedCount, Is.Zero);
            Assert.That(expansion.Boxing.PackageCount, Is.EqualTo(1));
        }

        [Test]
        public void WindowSaleDropsFifteenThatMustBePickedUp()
        {
            BoxAndStockOne();
            UnlockLane();
            player.transform.position = Vector3.zero;
            WaitForCar();
            Assert.That(expansion.DriveThru.HasWaitingCar, Is.True);
            long coins = wallet.Coins;
            int sales = wallet.CompletedSales;
            player.transform.position = expansion.DriveThru.WindowPosition + Vector3.up;
            Assert.That(expansion.DriveThru.TrySellFrom(player), Is.True);
            Assert.That(expansion.Boxing.PackageCount, Is.Zero);
            Assert.That(wallet.CompletedSales, Is.EqualTo(sales));
            Assert.That(wallet.Coins, Is.EqualTo(coins));
            Assert.That(expansion.DriveThru.IsHandoffActive, Is.True);
            FinishHandoff();
            Assert.That(wallet.CompletedSales, Is.EqualTo(sales + 1));
            Assert.That(cash.GroundValue, Is.EqualTo(15));
            player.transform.position = expansion.DriveThru.CashPosition + Vector3.up;
            cash.Advance(CashPickup.FlyDuration + 0.1f);
            Assert.That(wallet.Coins, Is.EqualTo(coins + 15));
        }

        [Test]
        public void StockedPackageSellsAtTheWindowWithoutHoldingABox()
        {
            BoxAndStockOne();
            UnlockLane();
            player.transform.position = Vector3.zero;
            WaitForCar();
            Assert.That(player.BoxedCount, Is.Zero);
            Assert.That(expansion.Boxing.PackageCount, Is.EqualTo(1));
            Assert.That(expansion.DriveThru.HasStoppedCarAtWindow, Is.True);
            Assert.That(Horizontal(expansion.DriveThru.WaitingCarPosition, ShopLayout.DriveThruQueue[0]),
                Is.LessThan(0.12f));
            player.transform.position = expansion.DriveThru.WindowPosition + Vector3.up;
            Assert.That(expansion.DriveThru.TrySellFrom(player), Is.True);
            Assert.That(expansion.Boxing.PackageCount, Is.Zero);
            Assert.That(player.BoxedCount, Is.Zero);
            Assert.That(expansion.DriveThru.IsHandoffActive, Is.True);
            Assert.That(cash.GroundValue, Is.Zero);
            Assert.That(Horizontal(expansion.DriveThru.WaitingCarPosition, ShopLayout.DriveThruQueue[0]),
                Is.LessThan(0.12f));
            Vector3 parked = expansion.DriveThru.WaitingCarPosition;
            expansion.DriveThru.Advance(DriveThruLane.HandoffDuration * 0.5f);
            Assert.That(expansion.DriveThru.IsHandoffActive, Is.True);
            Assert.That(Horizontal(expansion.DriveThru.WaitingCarPosition, parked), Is.LessThan(0.02f));
            Assert.That(cash.GroundValue, Is.Zero);
            FinishHandoff();
            Assert.That(expansion.DriveThru.IsHandoffActive, Is.False);
            Assert.That(cash.GroundValue, Is.EqualTo(15));
            expansion.DriveThru.Advance(0.2f);
            Assert.That(expansion.DriveThru.WaitingCarPosition.x, Is.LessThan(parked.x - 0.2f));
        }

        [Test]
        public void EmptyPackageKeepsTheCarStoppedAtTheWindow()
        {
            UnlockBoxing();
            UnlockLane();
            player.transform.position = Vector3.zero;
            WaitForCar();
            Assert.That(expansion.Boxing.PackageCount, Is.Zero);
            Assert.That(expansion.DriveThru.HasStoppedCarAtWindow, Is.True);
            Vector3 parked = expansion.DriveThru.WaitingCarPosition;
            player.transform.position = expansion.DriveThru.WindowPosition + Vector3.up;
            Assert.That(expansion.DriveThru.TrySellFrom(player), Is.False);
            for (int i = 0; i < 180; i++) expansion.DriveThru.Advance(1f / 60f);
            Assert.That(expansion.DriveThru.HasStoppedCarAtWindow, Is.True);
            Assert.That(Horizontal(expansion.DriveThru.WaitingCarPosition, parked), Is.LessThan(0.02f));
            Assert.That(cash.GroundValue, Is.Zero);
        }

        [Test]
        public void StationLabelsAreBoxPackAndWindow()
        {
            UnlockBoxing();
            UnlockLane();
            TextMesh box = GameObject.Find("BoxingLabel").GetComponent<TextMesh>();
            TextMesh pack = GameObject.Find("PackageLabel").GetComponent<TextMesh>();
            TextMesh window = GameObject.Find("WindowLabel").GetComponent<TextMesh>();
            Assert.That(box.text, Is.EqualTo("BOX"));
            Assert.That(pack.text, Is.EqualTo("PACK"));
            Assert.That(window.text, Is.EqualTo("WINDOW"));
            Assert.That(box.text, Does.Not.Contain("15"));
            Assert.That(pack.text, Does.Not.Contain("15"));
            Assert.That(window.text, Does.Not.Contain("15"));
        }

        [Test]
        public void DineInStillUsesLooseBurgersAfterBoxingUnlock()
        {
            GiveLoose();
            UnlockBoxing();
            Assert.That(player.LooseCount, Is.EqualTo(1));
            player.transform.position = serving.DropZone.DropPosition + Vector3.up;
            serving.DropZone.Advance(0.01f);
            Assert.That(serving.DropZone.TryDepositFrom(player), Is.True);
            Assert.That(stock.Count, Is.EqualTo(1));
            for (int i = 0; i < 1200; i++) queue.Advance(1f / 60f);
            player.transform.position = serving.ServingPosition + Vector3.up;
            Assert.That(serving.TryServeFrom(player), Is.True);
            serving.Advance(BurgerServingZone.HandoffDuration);
            Assert.That(wallet.CompletedSales, Is.EqualTo(1));
            Assert.That(wallet.Coins, Is.Zero);
            Assert.That(cash.GroundValue, Is.EqualTo(10));
        }

        [Test]
        public void StaffBoxesOrSellsAtTheWindow()
        {
            wallet.RestoreProgress(500, 0);
            Hold(expansion.BoxingPad, 3f);
            player.transform.position = Vector3.zero;
            expansion.BoxingPad.Advance(0.1f);
            Hold(expansion.DriveThruPad, 3f);
            grill.Advance(12f);
            player.transform.position = hiring.HiringPosition + Vector3.up;
            for (int i = 0; i < 90; i++) hiring.Advance(1f / 60f);
            RestaurantWorker worker = hiring.Worker;
            Assert.That(worker, Is.Not.Null);
            bool boxedOrSold = false;
            for (int i = 0; i < 3600 && !boxedOrSold; i++)
            {
                const float dt = 1f / 60f;
                grill.Advance(dt);
                queue.Advance(dt);
                serving.Advance(dt);
                expansion.Boxing.Advance(dt);
                expansion.DriveThru.Advance(dt);
                worker.Advance(dt);
                if (worker.Inventory.BoxedCount > 0 || expansion.Boxing.PackageCount > 0
                    || worker.Job == WorkerJob.Box || worker.Job == WorkerJob.Pack
                    || worker.Job == WorkerJob.DriveSell || wallet.CompletedSales > 0)
                    boxedOrSold = true;
            }
            Assert.That(boxedOrSold, Is.True);
            Assert.That(wallet.Coins, Is.EqualTo(50));
        }

        [Test]
        public void OldSaveLeavesBoxingAndLaneUnbought()
        {
            var upgrade = root.AddComponent<GrillUpgradeZone>();
            upgrade.Configure(grill, wallet, player, root.transform);
            string directory = Path.Combine(Path.GetTempPath(), "BurgerShopLane-" + System.Guid.NewGuid().ToString("N"));
            try
            {
                var v5 = new RestaurantSaveData
                {
                    version = 5, coins = 80, completedSales = 4, grillLevel = 1, workerHired = false,
                    hiredWorkerCount = 0, playerSpeedTier = 1, playerCarryTier = 0
                };
                Assert.That(v5.ResolvedBoughtBoxingStation, Is.False);
                Assert.That(v5.ResolvedBoughtDriveThru, Is.False);
                expansion.Restore(v5.ResolvedBoughtExtraTable, v5.ResolvedBoughtExtraGrill,
                    v5.ResolvedBoughtExtraCounter, v5.ResolvedExtraGrillLevel,
                    v5.ResolvedBoughtBoxingStation, v5.ResolvedBoughtDriveThru);
                Assert.That(expansion.HasBoxing, Is.False);
                Assert.That(expansion.HasDriveThru, Is.False);
                Assert.That(GameObject.Find("BoxingStation"), Is.Null);
                Assert.That(GameObject.Find("DriveThruLane"), Is.Null);

                wallet.RestoreProgress(400, 4);
                UnlockBoxing();
                UnlockLane();
                var persistence = root.AddComponent<RestaurantPersistence>();
                persistence.Configure(wallet, upgrade, hiring, null, expansion, directory);
                Assert.That(persistence.Flush(), Is.True);
                Assert.That(new LocalSaveStore(directory).Load(out RestaurantSaveData data),
                    Is.EqualTo(SaveLoadResult.Loaded));
                Assert.That(data.version, Is.EqualTo(RestaurantSaveData.CurrentVersion));
                Assert.That(data.boughtBoxingStation, Is.True);
                Assert.That(data.boughtDriveThru, Is.True);
                Assert.That(data.coins, Is.EqualTo(0));
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }

        [Test]
        public void CapsuleAsksToBoxStockAndSellCombo()
        {
            GiveLoose();
            UnlockBoxing();
            var tracker = root.AddComponent<SessionGoalTracker>();
            tracker.Configure(player, grill, stock, queue, wallet, serving, dining, null, null, null, expansion);
            tracker.Advance(2f);
            Assert.That(tracker.Title, Is.EqualTo("Box the burger"));
            player.transform.position = expansion.Boxing.CirclePosition + Vector3.up;
            expansion.Boxing.TryBoxFrom(player);
            tracker.Advance(0.01f);
            Assert.That(tracker.Title, Is.EqualTo("Pack the burgers"));
            Assert.That(tracker.IsCelebrating, Is.False);
            expansion.Boxing.Advance(2f);
            tracker.Advance(0.01f);
            Assert.That(tracker.Title, Is.EqualTo("Box the burger"));
            Assert.That(tracker.IsCelebrating, Is.True);
            tracker.Advance(2f);
            Assert.That(tracker.Title, Is.EqualTo("Stock the package counter"));
        }
    }
}
