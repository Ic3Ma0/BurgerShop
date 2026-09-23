using System.Collections.Generic;
using System.IO;
using BurgerShop.Customer;
using BurgerShop.Economy;
using BurgerShop.Persistence;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using NUnit.Framework;
using UnityEngine;

namespace BurgerShop.Tests.EditMode
{
    public sealed class RestaurantWorkerTests
    {
        GameObject root;
        ProductionStation grill;
        CustomerQueue queue;
        RestaurantWallet wallet;
        BurgerInventory player;
        BurgerServingZone cashier;
        CounterStock stock;
        CounterDropZone drop;
        DiningTable table;
        WorkerHiringZone hiring;
        Transform output;
        Transform pickupPoint;
        Transform hiringPoint;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("StaffTest");
            grill = root.AddComponent<ProductionStation>();
            output = new GameObject("Output").transform;
            output.SetParent(root.transform);
            grill.Configure(output, null, null);
            queue = root.AddComponent<CustomerQueue>();
            queue.OrderQuantityFactory = () => 1;
            queue.Configure(new Vector3(-8.2f, 0f, -4.4f), new Vector3(-2f, 0f, -4.4f),
                new[] { new Vector3(-2f, 0f, 1.7f), new Vector3(-2f, 0f, -0.1f), new Vector3(-2f, 0f, -1.9f) }, new Vector3(-2f, 0f, 3.3f));
            wallet = root.AddComponent<RestaurantWallet>();
            GameObject carrier = new GameObject("Player");
            carrier.transform.SetParent(root.transform);
            player = carrier.AddComponent<BurgerInventory>();
            player.Configure();
            Transform servingPoint = Point("ServingPoint", new Vector3(0.9f, 0f, 3.3f));
            pickupPoint = Point("PickupPoint", new Vector3(5.55f, 0f, 0.4f));
            hiringPoint = Point("HiringPoint", new Vector3(0f, 0f, -5.5f));
            Transform anchor = Point("StockAnchor", new Vector3(-1.4f, 1.2f, 3.3f));
            stock = root.AddComponent<CounterStock>();
            stock.Configure(anchor, null);
            drop = root.AddComponent<CounterDropZone>();
            drop.Configure(stock, servingPoint);
            table = DiningTable.Create(root.transform, new Vector3(-5.6f, 0f, 0.2f));
            cashier = root.AddComponent<BurgerServingZone>();
            cashier.Configure(queue, player, wallet, servingPoint,
                new[] { new Vector3(-8.2f, 0f, -4.4f), new Vector3(-10.5f, 0f, -6.2f) }, stock, table, drop);
            hiring = root.AddComponent<WorkerHiringZone>();
            hiring.Configure(grill, cashier, wallet, player, pickupPoint, hiringPoint, new Vector3(0.9f, 0f, 0.4f), drop);
        }

        Transform Point(string name, Vector3 position)
        {
            Transform point = new GameObject(name).transform;
            point.SetParent(root.transform);
            point.position = position;
            return point;
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(root);

        void Hold(float seconds)
        {
            for (int i = 0; i < Mathf.CeilToInt(seconds * 60f); i++) hiring.Advance(1f / 60f);
        }

        RestaurantWorker Hire()
        {
            HireNext();
            Assert.That(hiring.Worker, Is.Not.Null);
            return hiring.Worker;
        }

        void HireNext()
        {
            int need = Mathf.Max(1, hiring.HireCost);
            while (wallet.Coins < need) wallet.RecordSale(10);
            player.transform.position = hiring.HiringPosition + Vector3.up;
            Hold(1.5f);
            player.transform.position = Vector3.zero;
            hiring.Advance(0.1f);
        }

        void AdvanceStaff(float seconds)
        {
            int steps = Mathf.Max(1, Mathf.CeilToInt(seconds * 60f));
            for (int i = 0; i < steps; i++)
            {
                const float dt = 1f / 60f;
                grill.Advance(dt);
                queue.Advance(dt);
                cashier.Advance(dt);
                for (int w = 0; w < hiring.Workers.Count; w++)
                    if (hiring.Workers[w] != null) hiring.Workers[w].Advance(dt);
            }
        }

        void FillQueue()
        {
            for (int i = 0; i < 1200; i++) queue.Advance(1f / 60f);
        }

        [Test]
        public void WorkerRoutesAroundNewObstacleAndStillDelivers()
        {
            var worker=Hire();
            var layout=root.AddComponent<BurgerShop.Building.FacilityLayout>();
            layout.Configure(player,wallet,null,null,null,hiring,null,null);
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.SetParent(root.transform);
            wall.transform.position=new Vector3(3, .8f,1);wall.transform.localScale=new Vector3(1,1.6f,4);
            Physics.SyncTransforms();layout.RefreshNavigation();
            for(int i=0;i<3600;i++)
            {
                var before=worker.transform.position;
                AdvanceStaff(1f/60f);
                Assert.That(ActorObstacles.Clear(before,worker.transform.position),Is.True,"employee crossed a body");
                if(i==1200){wall.transform.position=new Vector3(2,.8f,-2);Physics.SyncTransforms();layout.RefreshNavigation();}
            }
            Assert.That(stock.Count+cashier.CompletedOrders,Is.GreaterThan(0),"employee must deliver, not merely stop safely");
        }

        [TestCase(.71f)]
        [TestCase(.85f)]
        [TestCase(.99f)]
        public void WorkerClosesCounterArrivalGapInsteadOfLoopingInPlace(float distance)
        {
            var worker=Hire();
            typeof(RestaurantWorker).GetProperty("Job").SetValue(worker,WorkerJob.Serve);
            typeof(RestaurantWorker).GetProperty("State").SetValue(worker,WorkerState.Serving);
            var stand=(Vector3)typeof(RestaurantWorker).GetMethod("ServingStand",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(worker,null);
            worker.transform.position=new Vector3(stand.x+distance,1.05f,stand.z);
            worker.Advance(.02f);
            Assert.That(worker.State,Is.EqualTo(WorkerState.ToCounter),"Outside serving range must start walking, not re-enter Serving in place");
            bool reached=false;
            for(int i=0;i<1500;i++)
            {
                worker.Advance(.02f);
                if(ShopLayout.Horizontal(worker.transform.position,stand)<=.7f){reached=true;break;}
            }
            Assert.That(reached,Is.True,"Worker must reach the actual serving radius");
        }

        [Test]
        public void HiringNeedsFundsRangeAndAnUninterruptedHold()
        {
            wallet.RecordSale(49);
            player.transform.position = hiring.HiringPosition + Vector3.up;
            Hold(3f);
            Assert.That(hiring.IsHired, Is.False);
            Assert.That(hiring.MissingCoins, Is.EqualTo(1));
            wallet.RecordSale(1);
            player.transform.position = Vector3.zero;
            Hold(3f);
            Assert.That(hiring.Progress, Is.Zero);
            player.transform.position = hiring.HiringPosition + Vector3.up;
            Hold(1f);
            player.transform.position = Vector3.zero;
            hiring.Advance(0.01f);
            player.transform.position = hiring.HiringPosition + Vector3.up;
            Hold(0.6f);
            Assert.That(hiring.IsHired, Is.False);
            Hold(0.9f);
            Assert.That(hiring.IsHired, Is.True);
            Assert.That(wallet.Coins, Is.Zero);
        }

        [Test]
        public void HireChargesOnceEvenWithReentryOrSpendingCallback()
        {
            wallet.RecordSale(100);
            wallet.CoinsSpent += _ => Hold(3f);
            player.transform.position = hiring.HiringPosition + Vector3.up;
            Hold(5f);
            Assert.That(wallet.Coins, Is.EqualTo(50));
            Assert.That(wallet.CompletedSales, Is.EqualTo(1));
            player.transform.position = Vector3.zero;
            hiring.Advance(0.1f);
            player.transform.position = hiring.HiringPosition + Vector3.up;
            Hold(5f);
            Assert.That(root.GetComponentsInChildren<RestaurantWorker>().Length, Is.EqualTo(1));
            Assert.That(wallet.Coins, Is.EqualTo(50));
            Assert.That(hiring.Worker.Inventory.Capacity, Is.EqualTo(2));
            Assert.That(hiring.Worker.GetComponentsInChildren<Collider>(), Is.Empty);
        }

        [Test]
        public void PausedDisabledAndHitchFramesCannotInstantlyHire()
        {
            wallet.RecordSale(50);
            player.transform.position = hiring.HiringPosition + Vector3.up;
            hiring.Advance(0f);
            hiring.Advance(-1f);
            hiring.Advance(100f);
            Assert.That(hiring.Progress, Is.LessThan(0.1f));
            foreach (Behaviour dependency in new Behaviour[] { grill, cashier, wallet, player, hiring })
            {
                dependency.enabled = false;
                Hold(2f);
                dependency.enabled = true;
            }
            Assert.That(hiring.IsHired, Is.False);
            Assert.That(wallet.Coins, Is.EqualTo(50));
        }

        [Test]
        public void WorkerWaitsForStockAndKeepsBurgerWhenNoCustomerExists()
        {
            RestaurantWorker worker = Hire();
            worker.Advance(100f);
            for (int i = 0; i < 100; i++) worker.Advance(0.1f);
            Assert.That(worker.State, Is.EqualTo(WorkerState.Collecting));
            Assert.That(worker.Inventory.Count, Is.Zero);
            Assert.That(wallet.Coins, Is.Zero);
            grill.Advance(3f);
            worker.Advance(0.1f);
            Assert.That(worker.Inventory.Count, Is.EqualTo(1));
            Assert.That(worker.State, Is.EqualTo(WorkerState.ToCounter));
            worker.Advance(100f);
            worker.Advance(100f);
            Assert.That(worker.Inventory.Count, Is.Zero);
            Assert.That(stock.Count, Is.EqualTo(1));
            Assert.That(wallet.Coins, Is.Zero);
        }

        [Test]
        public void WorkerCarriesAtMostTwoAndSharesOriginalGrillStockWithPlayer()
        {
            RestaurantWorker worker = Hire();
            grill.Advance(12f);
            var originals = new HashSet<int>();
            foreach (Transform burger in output) originals.Add(burger.GetInstanceID());
            worker.Advance(100f);
            worker.Advance(0.1f);
            Assert.That(player.TryCollectFrom(grill), Is.True);
            worker.Advance(0.35f);
            Assert.That(player.TryCollectFrom(grill), Is.True);
            Assert.That(worker.Inventory.Count, Is.EqualTo(2));
            Assert.That(player.Count, Is.EqualTo(2));
            Assert.That(grill.Stock, Is.Zero);
            Assert.That(player.TryCollectFrom(grill), Is.False);
            Assert.That(worker.Inventory.TryCollectFrom(grill), Is.False);
            var transferred = new HashSet<int>();
            foreach (Transform burger in player.transform.Find("CarryStack")) transferred.Add(burger.GetInstanceID());
            foreach (Transform burger in worker.transform.Find("CarryStack")) transferred.Add(burger.GetInstanceID());
            Assert.That(transferred.SetEquals(originals), Is.True);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void TwoCarriersCannotChargeSameCustomerTwice(bool playerFirst)
        {
            RestaurantWorker worker = Hire();
            FillQueue();
            grill.Advance(6f);
            player.TryCollectFrom(grill);
            worker.Inventory.TryCollectFrom(grill);
            player.transform.position = cashier.ServingPosition + Vector3.up;
            worker.transform.position = player.transform.position;
            Assert.That(drop.TryDepositFrom(player), Is.True);
            drop.Advance(1f);
            Assert.That(drop.TryDepositFrom(worker.Inventory), Is.True);
            CustomerAgent customer = queue.ReadyCustomer;
            BurgerInventory first = playerFirst ? player : worker.Inventory;
            BurgerInventory second = playerFirst ? worker.Inventory : player;
            Assert.That(cashier.TryServeFrom(first), Is.True);
            Assert.That(cashier.TryServeFrom(second), Is.False);
            cashier.Advance(BurgerServingZone.HandoffDuration);
            Assert.That(wallet.Coins, Is.Zero);
            Assert.That(customer.PaidAmount, Is.EqualTo(10));
            Assert.That(stock.Count, Is.EqualTo(1));
            Assert.That(queue.Count, Is.EqualTo(2));
        }

        [Test]
        public void ExtraCarrierCallsDoNotAdvanceSharedCashierCooldown()
        {
            RestaurantWorker worker = Hire();
            FillQueue();
            grill.Advance(6f);
            player.TryCollectFrom(grill);
            worker.Inventory.TryCollectFrom(grill);
            player.transform.position = cashier.ServingPosition + Vector3.up;
            worker.transform.position = player.transform.position;
            drop.TryDepositFrom(player);
            drop.Advance(1f);
            drop.TryDepositFrom(worker.Inventory);
            CustomerAgent first = queue.ReadyCustomer;
            cashier.TryServeFrom(player);
            cashier.Advance(BurgerServingZone.HandoffDuration);
            first.AdvanceDeparture(100f);
            queue.Advance(1f);
            Assert.That(queue.ReadyCustomer, Is.Not.Null);
            player.transform.position = Vector3.zero;
            worker.transform.position = cashier.ServingPosition + Vector3.up;
            for (int i = 0; i < 100; i++) Assert.That(cashier.TryServeFrom(worker.Inventory), Is.False);
            cashier.Advance(0.14f);
            Assert.That(cashier.TryServeFrom(worker.Inventory), Is.False);
            cashier.Advance(0.02f);
            Assert.That(cashier.TryServeFrom(worker.Inventory), Is.True);
            cashier.Advance(BurgerServingZone.HandoffDuration);
            Assert.That(wallet.Coins, Is.Zero);
            Assert.That(wallet.CompletedSales, Is.EqualTo(7));
        }

        [Test]
        public void PausingWorkerOrGrillPreservesCarriedStockAndRoute()
        {
            RestaurantWorker worker = Hire();
            grill.Advance(3f);
            worker.Advance(100f);
            worker.Advance(0.1f);
            Vector3 position = worker.transform.position;
            worker.Advance(0f);
            worker.Advance(-1f);
            worker.enabled = false;
            worker.Advance(100f);
            worker.enabled = true;
            grill.enabled = false;
            worker.Advance(100f);
            grill.enabled = true;
            Assert.That(worker.transform.position, Is.EqualTo(position));
            Assert.That(worker.Inventory.Count, Is.EqualTo(1));
            worker.Advance(0.2f);
            Assert.That(Vector3.Distance(worker.transform.position, position), Is.GreaterThan(0.5f));
        }

        [Test]
        public void AutonomousTripsPayUniqueCustomersAndStayOutsideCounters()
        {
            RestaurantWorker worker = Hire();
            var paidTickets = new HashSet<int>();
            for (int frame = 0; frame < 3600; frame++)
            {
                const float dt = 1f / 60f;
                grill.Advance(dt);
                queue.Advance(dt);
                foreach (CustomerAgent customer in root.GetComponentsInChildren<CustomerAgent>())
                    if (customer.IsDeparting) customer.AdvanceDeparture(dt);
                cashier.Advance(dt);
                worker.Advance(dt);
                foreach (CustomerAgent customer in root.GetComponentsInChildren<CustomerAgent>())
                    if (customer.IsDeparting) paidTickets.Add(customer.TicketNumber);
                Vector3 p = worker.transform.position;
                Assert.That(p.x > 2.4f && p.x < 6.6f && p.z > 1f && p.z < 4f, Is.False, "Worker must not cut through the grill.");
                Assert.That(p.x > -4.1f && p.x < 0.05f && p.z > 2.1f && p.z < 4.5f, Is.False, "Worker must not cut through the order counter.");
                Assert.That(worker.Inventory.Count, Is.LessThanOrEqualTo(2));
                Assert.That(queue.Count, Is.LessThanOrEqualTo(3));
            }
            Assert.That(worker.CompletedDeliveries, Is.GreaterThanOrEqualTo(6));
            Assert.That(wallet.Coins, Is.Zero, "Workers must not vacuum floor cash.");
            Assert.That(wallet.CompletedSales, Is.EqualTo(5 + worker.CompletedDeliveries));
            Assert.That(paidTickets.Count, Is.EqualTo(worker.CompletedDeliveries));
        }

        [Test]
        public void DestroyingWorkerReleasesOwnedMaterialsWithoutChargingAgain()
        {
            RestaurantWorker worker = Hire();
            Material uniform = worker.transform.Find("Uniform").GetComponent<Renderer>().sharedMaterial;
            grill.Advance(3f);
            worker.Advance(100f);
            worker.Advance(0.1f);
            Material burger = worker.transform.Find("CarryStack").GetComponentInChildren<Renderer>().sharedMaterial;
            Object.DestroyImmediate(worker.gameObject);
            Assert.That(uniform == null, Is.True);
            Assert.That(burger == null, Is.True);
            player.transform.position = hiring.HiringPosition + Vector3.up;
            wallet.RecordSale(50);
            Hold(3f);
            Assert.That(wallet.Coins, Is.EqualTo(50));
            Assert.That(hiring.IsHired, Is.True);
            Assert.That(hiring.Worker == null, Is.True);
        }

        [Test]
        public void ThreeHireTiersCost50Then100Then200AndCapAtThree()
        {
            wallet.RecordSale(350);
            player.transform.position = hiring.HiringPosition + Vector3.up;
            Hold(1.5f);
            Assert.That(hiring.HiredCount, Is.EqualTo(1));
            Assert.That(hiring.HireCost, Is.EqualTo(100));
            Assert.That(wallet.Coins, Is.EqualTo(300));
            Hold(3f);
            Assert.That(hiring.HiredCount, Is.EqualTo(1));
            player.transform.position = Vector3.zero;
            hiring.Advance(0.1f);
            player.transform.position = hiring.HiringPosition + Vector3.up;
            Hold(1.5f);
            Assert.That(hiring.HiredCount, Is.EqualTo(2));
            Assert.That(hiring.HireCost, Is.EqualTo(200));
            Assert.That(wallet.Coins, Is.EqualTo(200));
            player.transform.position = Vector3.zero;
            hiring.Advance(0.1f);
            player.transform.position = hiring.HiringPosition + Vector3.up;
            Hold(1.5f);
            Assert.That(hiring.HiredCount, Is.EqualTo(3));
            Assert.That(hiring.IsFull, Is.True);
            Assert.That(wallet.Coins, Is.Zero);
            Assert.That(root.GetComponentsInChildren<RestaurantWorker>().Length, Is.EqualTo(3));
            player.transform.position = Vector3.zero;
            hiring.Advance(0.1f);
            wallet.RecordSale(200);
            player.transform.position = hiring.HiringPosition + Vector3.up;
            Hold(3f);
            Assert.That(hiring.HiredCount, Is.EqualTo(3));
            Assert.That(root.GetComponentsInChildren<RestaurantWorker>().Length, Is.EqualTo(3));
        }

        [Test]
        public void LegacySaveHiredFlagRestoresOneWorker()
        {
            hiring.RestoreWorker(true, 7);
            Assert.That(hiring.HiredCount, Is.EqualTo(1));
            Assert.That(hiring.IsHired, Is.True);
            Assert.That(hiring.Worker, Is.Not.Null);
            Assert.That(hiring.Worker.CompletedDeliveries, Is.EqualTo(7));
        }

        [Test]
        public void RestoreThreeWorkersFromHiredCount()
        {
            hiring.RestoreWorkers(3, 4, 2);
            Assert.That(hiring.HiredCount, Is.EqualTo(3));
            Assert.That(hiring.TotalDeliveries, Is.EqualTo(4));
            Assert.That(hiring.TotalClears, Is.EqualTo(2));
            Assert.That(root.GetComponentsInChildren<RestaurantWorker>().Length, Is.EqualTo(3));
        }

        [Test]
        public void WorkerClearsDirtyTableAndDumpsAtTheBin()
        {
            DiningArea hall = DiningArea.Wrap(table);
            TrashBin trashBin = TrashBin.Create(root.transform, new Vector3(-6f, 0f, -3f));
            hiring.Configure(grill, cashier, wallet, player, pickupPoint, hiringPoint, new Vector3(0.9f, 0f, 0.4f),
                drop, null, hall, trashBin);
            RestaurantWorker worker = Hire();
            table.LeaveMealTrash(0);
            Assert.That(table.IsDirty, Is.True);
            for (int i = 0; i < 3600; i++)
            {
                const float dt = 1f / 60f;
                table.Advance(dt);
                trashBin.Advance(dt);
                worker.Advance(dt);
            }
            Assert.That(table.TrashCount, Is.Zero);
            Assert.That(worker.Trash.Count, Is.Zero);
            Assert.That(table.OutstandingTrash, Is.Zero);
            Assert.That(table.HasAvailableSeat, Is.True);
            Assert.That(worker.CompletedClears, Is.GreaterThan(0));
        }

        [Test]
        public void TwoHiredWorkersHaveDifferentColorsFacingAndFeatures()
        {
            Hire();
            HireNext();
            Assert.That(hiring.Workers.Count, Is.EqualTo(2));
            RestaurantWorker a = hiring.Workers[0];
            RestaurantWorker b = hiring.Workers[1];
            Assert.That(a.gameObject.activeInHierarchy, Is.True);
            Assert.That(b.gameObject.activeInHierarchy, Is.True);
            Assert.That(a.UniformColor, Is.Not.EqualTo(b.UniformColor));
            Assert.That(Vector3.Dot(a.transform.forward, b.transform.forward), Is.LessThan(0.5f));
            Assert.That(a.HeightScale, Is.Not.EqualTo(b.HeightScale));
            Assert.That(a.WearsHat, Is.Not.EqualTo(b.WearsHat));
            Assert.That(a.StyleLetter, Is.EqualTo('A'));
            Assert.That(b.StyleLetter, Is.EqualTo('B'));
            Assert.That(a.transform.Find("Hat"), Is.Not.Null);
            Assert.That(b.transform.Find("Hair"), Is.Not.Null);
            Assert.That(b.transform.Find("Hat"), Is.Null);
        }

        [Test]
        public void TwoHiredWorkersStayActiveAfterWorkingAndRestore()
        {
            Hire();
            HireNext();
            RestaurantWorker[] staff = { hiring.Workers[0], hiring.Workers[1] };
            Assert.That(staff[0], Is.Not.Null);
            Assert.That(staff[1], Is.Not.Null);
            AdvanceStaff(4f);
            for (int i = 0; i < 240; i++)
            {
                staff[0].Advance(1f / 60f);
                staff[1].Advance(1f / 60f);
            }
            Assert.That(staff[0] != null && staff[0].gameObject.activeInHierarchy, Is.True);
            Assert.That(staff[1] != null && staff[1].gameObject.activeInHierarchy, Is.True);
            Assert.That(root.GetComponentsInChildren<RestaurantWorker>().Length, Is.EqualTo(2));

            string directory = Path.Combine(Path.GetTempPath(), "BurgerShopStaff-" + System.Guid.NewGuid().ToString("N"));
            try
            {
                var upgrade = root.AddComponent<GrillUpgradeZone>();
                upgrade.Configure(grill, wallet, player, hiringPoint);
                var persistence = root.AddComponent<RestaurantPersistence>();
                persistence.Configure(wallet, upgrade, hiring, directory);
                Assert.That(persistence.Flush(), Is.True);
                hiring.RestoreWorkers(2, 0);
                hiring.RestoreWorkers(2, 0);
                Assert.That(root.GetComponentsInChildren<RestaurantWorker>().Length, Is.EqualTo(2),
                    "Restore must replace staff, not leave orphans or wipe them.");
                hiring.RestoreWorkers(0, 0);
                Assert.That(root.GetComponentsInChildren<RestaurantWorker>(), Is.Empty);
                Assert.That(new LocalSaveStore(directory).Load(out RestaurantSaveData data), Is.EqualTo(SaveLoadResult.Loaded));
                Assert.That(data.version, Is.EqualTo(RestaurantSaveData.CurrentVersion));
                Assert.That(data.hiredWorkerCount, Is.EqualTo(2));
                Assert.That(data.ResolvedBoostLevel, Is.EqualTo(0));
                hiring.RestoreWorkers(data.ResolvedHiredCount, data.workerDeliveries, data.workerClears);
                RestaurantWorker[] restored = root.GetComponentsInChildren<RestaurantWorker>();
                Assert.That(restored.Length, Is.EqualTo(2));
                Assert.That(restored[0].gameObject.activeInHierarchy, Is.True);
                Assert.That(restored[1].gameObject.activeInHierarchy, Is.True);
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }

        [Test]
        public void DirtyTableSendsAtLeastOneWorkerToClean()
        {
            DiningArea hall = DiningArea.Wrap(table);
            TrashBin trashBin = TrashBin.Create(root.transform, new Vector3(-6f, 0f, -3f));
            hiring.Configure(grill, cashier, wallet, player, pickupPoint, hiringPoint, new Vector3(0.9f, 0f, 0.4f),
                drop, null, hall, trashBin);
            Hire();
            HireNext();
            table.LeaveMealTrash(0);
            Assert.That(table.IsDirty, Is.True);
            bool cleaning = false;
            for (int i = 0; i < 1800 && !cleaning; i++)
            {
                const float dt = 1f / 60f;
                table.Advance(dt);
                trashBin.Advance(dt);
                for (int w = 0; w < hiring.Workers.Count; w++)
                {
                    RestaurantWorker worker = hiring.Workers[w];
                    if (worker == null) continue;
                    worker.Advance(dt);
                    if (worker.Job == WorkerJob.Clean || worker.State == WorkerState.ToTrash
                        || worker.State == WorkerState.CollectingTrash || worker.State == WorkerState.ToBin
                        || worker.State == WorkerState.Dumping)
                        cleaning = true;
                }
            }
            Assert.That(cleaning, Is.True);
        }

        [Test]
        public void GrillStockSendsAWorkerToCollectBurgers()
        {
            Hire();
            HireNext();
            grill.Advance(12f);
            Assert.That(grill.Stock, Is.GreaterThan(0));
            bool collected = false;
            for (int i = 0; i < 1800 && !collected; i++)
            {
                const float dt = 1f / 60f;
                for (int w = 0; w < hiring.Workers.Count; w++)
                {
                    RestaurantWorker worker = hiring.Workers[w];
                    if (worker == null) continue;
                    worker.Advance(dt);
                    if (worker.Inventory.Count > 0 || worker.Job == WorkerJob.Collect || worker.Job == WorkerJob.Stock)
                        collected = true;
                }
            }
            Assert.That(collected, Is.True);
            Assert.That(hiring.Workers[0].gameObject.activeInHierarchy, Is.True);
            Assert.That(hiring.Workers[1].gameObject.activeInHierarchy, Is.True);
        }

        [Test]
        public void CounterStockAndReadyCustomerSendsAWorkerToServe()
        {
            FillQueue();
            grill.Advance(12f);
            player.transform.position = cashier.ServingPosition + Vector3.up;
            for (int i = 0; i < 4; i++)
                Assert.That(player.TryCollectFrom(grill), Is.True);
            while (player.Count > 0)
            {
                drop.Advance(1f);
                Assert.That(drop.TryDepositFrom(player), Is.True);
            }
            Assert.That(stock.Count, Is.GreaterThan(0));
            Assert.That(queue.ReadyCustomer, Is.Not.Null);
            Hire();
            HireNext();
            bool served = false;
            for (int i = 0; i < 2400 && !served; i++)
            {
                const float dt = 1f / 60f;
                queue.Advance(dt);
                cashier.Advance(dt);
                for (int w = 0; w < hiring.Workers.Count; w++)
                    if (hiring.Workers[w] != null) hiring.Workers[w].Advance(dt);
                if (hiring.TotalDeliveries > 0) served = true;
            }
            Assert.That(served, Is.True);
            Assert.That(root.GetComponentsInChildren<RestaurantWorker>().Length, Is.EqualTo(2));
        }
    }
}
