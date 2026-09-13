using System.IO;
using BurgerShop.Customer;
using BurgerShop.Economy;
using BurgerShop.Persistence;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using BurgerShop.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace BurgerShop.Tests.EditMode
{
    public sealed class ColaProductTests
    {
        GameObject root;
        BurgerInventory inventory;
        RestaurantWallet wallet;
        ExpandableGrill burgerGrill;
        ExpandableGrill colaMachine;
        BurgerServingZone burgerServing;
        BurgerServingZone colaServing;
        CounterStock burgerStock;
        DiningTable table;
        CustomerQueue burgerQueue;
        CustomerQueue colaQueue;
        WorkerHiringZone hiring;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("ColaProductTest");
            GameObject player = new GameObject("Player");
            player.transform.SetParent(root.transform);
            inventory = player.AddComponent<BurgerInventory>();
            inventory.Configure();
            wallet = root.AddComponent<RestaurantWallet>();
            burgerGrill = ExpandableGrill.CreateStarter(root.transform, inventory, wallet);
            ShopLayout.OpenWing(root.transform, BurgerShop.Core.RuntimeMaterials.Create(Color.gray), BurgerShop.Core.RuntimeMaterials.Create(Color.gray));
            colaMachine = ExpandableGrill.CreateColaStarter(root.transform, inventory, wallet);

            burgerQueue = root.AddComponent<CustomerQueue>();
            burgerQueue.OrderQuantityFactory = () => 1;
            burgerQueue.Configure(ShopLayout.Entrance, ShopLayout.QueueEntry, ShopLayout.QueueSlots, ShopLayout.Counter);
            Transform burgerCircle = Point("BurgerCircle", ShopLayout.ServingCircle);
            Transform burgerAnchor = Point("BurgerAnchor", ShopLayout.CounterTop);
            burgerStock = root.AddComponent<CounterStock>();
            burgerStock.Configure(burgerAnchor, null);
            CounterDropZone burgerDrop = root.AddComponent<CounterDropZone>();
            burgerDrop.Configure(burgerStock, burgerCircle);
            table = DiningTable.Create(root.transform, ShopLayout.Tables[0]);
            burgerServing = root.AddComponent<BurgerServingZone>();
            burgerServing.Configure(burgerQueue, inventory, wallet, burgerCircle, ShopLayout.Exit, burgerStock, table,
                burgerDrop);

            Transform colaArea = new GameObject("ColaCustomerArea").transform;
            colaArea.SetParent(root.transform, false);
            colaQueue = colaArea.gameObject.AddComponent<CustomerQueue>();
            colaQueue.Product = KitchenProduct.Cola;
            colaQueue.OrderQuantityFactory = () => 1;
            colaQueue.Configure(ShopLayout.Entrance, ShopLayout.ColaQueueEntry, ShopLayout.ColaQueueSlots,
                ShopLayout.ColaCounter);
            CounterStock colaStock = ShopFixtures.CreateCounterStock(colaArea, ShopLayout.ColaCounterTop, false,
                KitchenProduct.Cola);
            Transform colaCircle = ShopFixtures.CreateCashierCircle(colaArea, ShopLayout.ColaServingCircle);
            CounterDropZone colaDrop = colaArea.gameObject.AddComponent<CounterDropZone>();
            colaDrop.Configure(colaStock, colaCircle, 1.05f, 0.25f, false, KitchenProduct.Cola);
            colaServing = colaArea.gameObject.AddComponent<BurgerServingZone>();
            colaServing.Configure(colaQueue, inventory, wallet, colaCircle, ShopLayout.Exit, colaStock, table, colaDrop);

            hiring = root.AddComponent<WorkerHiringZone>();
            hiring.Configure(burgerGrill.Station, burgerServing, wallet, inventory, burgerGrill.Pickup.PickupPoint,
                Point("Hire", Vector3.zero), ShopLayout.Aisle, burgerDrop, null, DiningArea.Wrap(table));
            hiring.RegisterCola(colaMachine.Station, colaMachine.Pickup.PickupPoint, colaServing, colaDrop);
        }

        [TearDown]
        public void TearDown() { Object.DestroyImmediate(root); ShopLayout.ResetWingLock(); }

        Transform Point(string name, Vector3 position)
        {
            Transform point = new GameObject(name).transform;
            point.SetParent(root.transform);
            point.position = position;
            return point;
        }

        void FillColaQueue()
        {
            for (int i = 0; i < 6000; i++) colaQueue.Advance(1f / 60f);
        }

        void CompleteColaHandoff()
        {
            Vector3 position = inventory.transform.position;
            inventory.transform.position = Vector3.zero;
            colaServing.Advance(BurgerServingZone.HandoffDuration);
            inventory.transform.position = position;
        }

        static Vector3 Flatten(Vector3 value)
        {
            value.y = 0f;
            return value;
        }

        [Test]
        public void ColaMachineAndCounterUseStarterGrillUpgradeAndZoneColors()
        {
            Assert.That(colaMachine.Station.Product, Is.EqualTo(KitchenProduct.Cola));
            Assert.That(colaMachine.Upgrade.NextCost, Is.EqualTo(30));
            Assert.That(colaMachine.Station.Capacity, Is.EqualTo(4));
            Assert.That(colaMachine.Station.ProductionSeconds, Is.EqualTo(3f));
            Assert.That(colaMachine.transform.position, Is.EqualTo(ShopLayout.Cola));
            Color pickup = colaMachine.Pickup.PickupPoint.GetComponent<Renderer>().sharedMaterial.color;
            Color upgrade = colaMachine.UpgradeSpot.GetComponent<Renderer>().sharedMaterial.color;
            Assert.That(pickup.g, Is.GreaterThan(pickup.r));
            Assert.That(pickup.g, Is.GreaterThan(pickup.b));
            Assert.That(upgrade.b, Is.GreaterThan(upgrade.g));
            Assert.That(upgrade.r, Is.GreaterThan(upgrade.g));
            Assert.That(ShopLayout.ColaServingCircle.x, Is.GreaterThan(ShopLayout.ColaCounter.x));
            Transform dash = colaServing.transform.Find("CashierCircle/Dash_1");
            Assert.That(dash, Is.Not.Null);
            Color white = dash.GetComponent<Renderer>().sharedMaterial.color;
            Assert.That(white.r, Is.GreaterThan(0.9f));
            Assert.That(white.g, Is.GreaterThan(0.9f));
            Assert.That(white.b, Is.GreaterThan(0.9f));
        }

        [Test]
        public void ReadyColaCustomerFacesTheColaCounter()
        {
            FillColaQueue();
            CustomerAgent customer = colaQueue.ReadyCustomer;
            Assert.That(customer, Is.Not.Null);
            Assert.That(customer.transform.Find("OrderBubble/ColaIcon"), Is.Not.Null);
            Vector3 toward = Flatten(ShopLayout.ColaCounter - customer.transform.position).normalized;
            Assert.That(Vector3.Dot(Flatten(customer.transform.forward), toward), Is.GreaterThan(0.9f));
        }

        [Test]
        public void CollectingColaDoesNotTouchBurgerStockAndSharesCarryCapacity()
        {
            burgerGrill.Station.Advance(12f);
            colaMachine.Station.Advance(12f);
            int burgers = burgerGrill.Station.Stock;
            Assert.That(inventory.TryCollectFrom(colaMachine.Station), Is.True);
            Assert.That(inventory.ColaCount, Is.EqualTo(1));
            Assert.That(inventory.LooseCount, Is.Zero);
            Assert.That(inventory.Count, Is.EqualTo(1));
            Assert.That(burgerGrill.Station.Stock, Is.EqualTo(burgers));
            Assert.That(colaMachine.Station.Stock, Is.EqualTo(3));
            Assert.That(inventory.TryCollectFrom(burgerGrill.Station), Is.True);
            Assert.That(inventory.LooseCount, Is.EqualTo(1));
            Assert.That(inventory.ColaCount, Is.EqualTo(1));
            Assert.That(inventory.TryBoxOne(), Is.True);
            Assert.That(inventory.BoxedCount, Is.EqualTo(1));
            Assert.That(inventory.ColaCount, Is.EqualTo(1));
            Assert.That(inventory.TryBoxOne(), Is.False);
            for (int i = inventory.Count; i < inventory.Capacity; i++)
                Assert.That(inventory.TryCollectFrom(colaMachine.Station), Is.True);
            Assert.That(inventory.IsFull, Is.True);
            Assert.That(inventory.TryCollectFrom(colaMachine.Station), Is.False);
            Assert.That(inventory.TryCollectFrom(burgerGrill.Station), Is.False);
        }

        [Test]
        public void ColaDepositsOnlyOnTheColaCounter()
        {
            colaMachine.Station.Advance(12f);
            burgerGrill.Station.Advance(12f);
            Assert.That(inventory.TryCollectFrom(colaMachine.Station), Is.True);
            Assert.That(inventory.TryCollectFrom(burgerGrill.Station), Is.True);
            inventory.transform.position = burgerServing.ServingPosition + Vector3.up;
            burgerServing.DropZone.Advance(1f);
            Assert.That(burgerServing.DropZone.TryDepositFrom(inventory), Is.True);
            Assert.That(burgerStock.Count, Is.EqualTo(1));
            Assert.That(inventory.ColaCount, Is.EqualTo(1));
            burgerServing.DropZone.Advance(1f);
            Assert.That(burgerServing.DropZone.TryDepositFrom(inventory), Is.False);
            inventory.transform.position = colaServing.ServingPosition + Vector3.up;
            colaServing.DropZone.Advance(1f);
            Assert.That(colaServing.DropZone.TryDepositFrom(inventory), Is.True);
            Assert.That(colaServing.TotalStock, Is.EqualTo(1));
            Assert.That(inventory.ColaCount, Is.Zero);
            Assert.That(burgerStock.Count, Is.EqualTo(1));
        }

        [Test]
        public void ServingColaPaysTenThenLeavesTheSameTrashAsABurger()
        {
            colaMachine.Station.Advance(12f);
            Assert.That(inventory.TryCollectFrom(colaMachine.Station), Is.True);
            FillColaQueue();
            inventory.transform.position = colaServing.ServingPosition + Vector3.up;
            Assert.That(colaServing.DropZone.TryDepositFrom(inventory), Is.True);
            Assert.That(colaServing.TotalStock, Is.EqualTo(1));
            CustomerAgent customer = colaQueue.ReadyCustomer;
            Assert.That(customer, Is.Not.Null);
            colaServing.Advance(1f);
            CompleteColaHandoff();
            Assert.That(wallet.CompletedSales, Is.EqualTo(1));
            Assert.That(customer.PaidAmount, Is.EqualTo(10));
            Assert.That(wallet.Coins, Is.Zero);
            customer.AdvanceDeparture(100f);
            Assert.That(table.TrashCount, Is.EqualTo(DiningTable.TrashPerGuest));
            Assert.That(table.IsDirty, Is.True);
            Assert.That(table.IsSeatBlocked(0), Is.True);
            Assert.That(table.IsSeatBlocked(1), Is.True);
        }

        [Test]
        public void ColaAndBurgerWhiteCirclesThrottleIndependently()
        {
            burgerGrill.Station.Advance(12f);
            colaMachine.Station.Advance(12f);
            inventory.TryCollectFrom(burgerGrill.Station);
            inventory.TryCollectFrom(colaMachine.Station);
            for (int i = 0; i < 1200; i++) burgerQueue.Advance(1f / 60f);
            FillColaQueue();
            inventory.transform.position = burgerServing.ServingPosition + Vector3.up;
            burgerServing.DropZone.Advance(1f);
            burgerServing.DropZone.TryDepositFrom(inventory);
            burgerServing.Advance(1f);
            burgerServing.Advance(BurgerServingZone.HandoffDuration);
            inventory.transform.position = colaServing.ServingPosition + Vector3.up;
            colaServing.DropZone.Advance(1f);
            Assert.That(colaServing.DropZone.TryDepositFrom(inventory), Is.True);
            Assert.That(colaServing.TryServeFrom(inventory), Is.True, "burger cooldown must not block the cola circle");
        }

        [Test]
        public void TwoCarriersCannotSkipTheColaCashierInterval()
        {
            GameObject helper = new GameObject("Helper");
            helper.transform.SetParent(root.transform);
            BurgerInventory other = helper.AddComponent<BurgerInventory>();
            other.Configure();
            colaMachine.Station.Advance(12f);
            inventory.TryCollectFrom(colaMachine.Station);
            other.TryCollectFrom(colaMachine.Station);
            FillColaQueue();
            inventory.transform.position = colaServing.ServingPosition + Vector3.up;
            other.transform.position = inventory.transform.position;
            colaServing.DropZone.Advance(1f);
            Assert.That(colaServing.DropZone.TryDepositFrom(inventory), Is.True);
            colaServing.DropZone.Advance(1f);
            Assert.That(colaServing.DropZone.TryDepositFrom(other), Is.True);
            Assert.That(colaServing.TryServeFrom(inventory), Is.True);
            CustomerAgent first = colaQueue.Customers.Count > 0 ? colaQueue.ReadyCustomer : null;
            Assert.That(colaServing.TryServeFrom(other), Is.False);
            colaServing.Advance(BurgerServingZone.HandoffDuration);
            first?.AdvanceDeparture(100f);
            FillColaQueue();
            inventory.transform.position = Vector3.zero;
            other.transform.position = colaServing.ServingPosition + Vector3.up;
            for (int i = 0; i < 20; i++) Assert.That(colaServing.TryServeFrom(other), Is.False);
            colaServing.Advance(0.14f);
            Assert.That(colaServing.TryServeFrom(other), Is.False);
            colaServing.Advance(0.02f);
            Assert.That(colaServing.TryServeFrom(other), Is.True);
        }

        [Test]
        public void ColaUpgradeMatchesStarterGrillAndWritesColaCopy()
        {
            wallet.RestoreProgress(90, 0);
            colaMachine.Upgrade.RestoreLevel(1);
            inventory.transform.position = colaMachine.Upgrade.UpgradePosition + Vector3.up;
            for (int i = 0; i < 90; i++) colaMachine.Upgrade.Advance(1f / 60f);
            Assert.That(colaMachine.Upgrade.Level, Is.EqualTo(2));
            Assert.That(wallet.Coins, Is.EqualTo(60));
            Assert.That(colaMachine.Station.ProductionSeconds, Is.EqualTo(2f));
            Assert.That(colaMachine.Station.Capacity, Is.EqualTo(6));
            Assert.That(colaMachine.ActiveLookName, Is.EqualTo("Look_Lv2"));
            Assert.That(colaMachine.ActiveLookScale, Is.EqualTo(ExpandableGrill.LookScales[1]));
            Assert.That(colaMachine.ActiveLook.Find("Nozzle_1"), Is.Not.Null);
            Assert.That(colaMachine.ActiveLook.Find("SelectionPanel"), Is.Not.Null);
            inventory.transform.position = Vector3.zero;
            colaMachine.Upgrade.Advance(0.1f);
            inventory.transform.position = colaMachine.Upgrade.UpgradePosition + Vector3.up;
            for (int i = 0; i < 90; i++) colaMachine.Upgrade.Advance(1f / 60f);
            Assert.That(colaMachine.Upgrade.Level, Is.EqualTo(3));
            Assert.That(wallet.Coins, Is.Zero);
            Assert.That(colaMachine.Station.ProductionSeconds, Is.EqualTo(1.5f));
            Assert.That(colaMachine.Station.Capacity, Is.EqualTo(8));
            Assert.That(colaMachine.StatusCopy, Does.Contain("COLA"));
            Assert.That(colaMachine.StatusCopy, Does.Contain("MAX"));
            Assert.That(colaMachine.StatusCopy, Does.Not.Contain("GRILL"));
            Assert.That(colaMachine.StatusCopy, Does.Not.Contain("FULL"));
            colaMachine.Station.Advance(20f);
            Assert.That(colaMachine.StatusCopy, Is.EqualTo("COLA 8/8  MAX"));
        }

        [Test]
        public void UpgradeHudUsesColaNounWhileStandingOnThePurpleCircle()
        {
            var canvas = new GameObject("Hud", typeof(RectTransform));
            canvas.transform.SetParent(root.transform, false);
            Text label = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text))
                .GetComponent<Text>();
            label.transform.SetParent(canvas.transform, false);
            Image fill = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image))
                .GetComponent<Image>();
            fill.transform.SetParent(canvas.transform, false);
            var group = canvas.AddComponent<CanvasGroup>();
            UpgradeHud hud = canvas.AddComponent<UpgradeHud>();
            hud.Configure(burgerGrill.Upgrade, label, fill, group);
            hud.AddZone(colaMachine.Upgrade);
            inventory.transform.position = colaMachine.Upgrade.UpgradePosition + Vector3.up;
            typeof(UpgradeHud).GetMethod("LateUpdate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(hud, null);
            Assert.That(label.text, Does.Contain("COLA LV"));
            Assert.That(label.text, Does.Not.Contain("GRILL LV"));
        }

        [Test]
        public void ColaPickupDoesNotCompleteBurgerPickupGoal()
        {
            var goals = root.AddComponent<SessionGoalTracker>();
            goals.Configure(inventory, burgerGrill.Station, burgerStock, root.GetComponent<CustomerQueue>(), wallet, burgerServing);
            colaMachine.Station.Advance(12f);
            inventory.TryCollectFrom(colaMachine.Station);
            goals.Advance(2f);
            Assert.That(goals.GoalProgress, Is.Zero);
            Assert.That(goals.IsCelebrating, Is.False);
        }

        [Test]
        public void EmptyHandsStillAskToPickUpABurgerWhenBothMachinesHaveStock()
        {
            burgerGrill.Station.Advance(12f);
            colaMachine.Station.Advance(12f);
            SessionGoalTracker goals = root.AddComponent<SessionGoalTracker>();
            goals.Configure(inventory, burgerGrill.Station, burgerStock, root.GetComponent<CustomerQueue>(), wallet,
                burgerServing, DiningArea.Wrap(table), null, null, null, null, colaServing, colaMachine.Station);
            goals.Advance(2f);
            Assert.That(goals.Title, Is.EqualTo("Complete a burger order"));
        }

        [Test]
        public void StaffStocksTheColaCounterFromSharedColaStock()
        {
            colaMachine.Station.Advance(12f);
            wallet.RestoreProgress(50, 0);
            inventory.transform.position = hiring.HiringPosition + Vector3.up;
            for (int i = 0; i < 90; i++) hiring.Advance(1f / 60f);
            inventory.transform.position = new Vector3(-8f, 0f, -8f);
            RestaurantWorker worker = hiring.Worker;
            Assert.That(worker, Is.Not.Null);
            for (int i = 0; i < 6000 && colaServing.TotalStock == 0; i++)
            {
                const float dt = 1f / 60f;
                colaMachine.Station.Advance(dt);
                colaQueue.Advance(dt);
                colaServing.Advance(dt);
                worker.Advance(dt);
            }
            Assert.That(colaServing.TotalStock + colaServing.CompletedOrders, Is.GreaterThan(0));
            Assert.That(burgerStock.Count, Is.Zero);
            Assert.That(worker.Inventory.LooseCount, Is.Zero);
        }

        [Test]
        public void DiningThenColaSupplyAlternatesAndSkipsEmptyMachines()
        {
            wallet.RestoreProgress(50, 0);
            inventory.transform.position = hiring.HiringPosition + Vector3.up;
            for (int i = 0; i < 90; i++) hiring.Advance(1f / 60f);
            inventory.transform.position = new Vector3(-8f, 0f, -8f);
            RestaurantWorker worker = hiring.Worker;
            Assert.That(worker, Is.Not.Null);
            burgerGrill.Station.Advance(12f);
            colaMachine.Station.Advance(12f);
            Assert.That(hiring.TryAssignSupply(worker), Is.True);
            Assert.That(worker.SupplyTarget, Is.EqualTo(SupplyLine.Dining));
            Assert.That(hiring.TryAssignSupply(worker), Is.True);
            Assert.That(worker.SupplyTarget, Is.EqualTo(SupplyLine.Cola));
            while (burgerGrill.Station.Stock > 0) burgerGrill.Station.TryTakeBurger();
            Assert.That(hiring.AnyGrillHasStock(), Is.False);
            Assert.That(hiring.TryAssignSupply(worker), Is.True);
            Assert.That(worker.SupplyTarget, Is.EqualTo(SupplyLine.Cola));
        }

        [Test]
        public void PersistenceWritesVersion10AndOldSavesKeepGrillProgressAtColaLv1()
        {
            string directory = Path.Combine(Path.GetTempPath(), "BurgerShopCola-" + System.Guid.NewGuid().ToString("N"));
            try
            {
                wallet.RestoreProgress(600, 18);
                burgerGrill.Upgrade.RestoreLevel(3);
                colaMachine.Upgrade.RestoreLevel(2);
                var persistence = root.AddComponent<RestaurantPersistence>();
                persistence.Configure(wallet, burgerGrill.Upgrade, hiring, null, null, null, null,
                    directory, cola: colaMachine.Upgrade);
                Assert.That(persistence.Flush(), Is.True);
                Assert.That(new LocalSaveStore(directory).Load(out RestaurantSaveData data),
                    Is.EqualTo(SaveLoadResult.Loaded));
                Assert.That(data.version, Is.EqualTo(RestaurantSaveData.CurrentVersion));
                Assert.That(data.colaLevel, Is.EqualTo(2));
                Assert.That(data.grillLevel, Is.EqualTo(3));
                Assert.That(data.coins, Is.EqualTo(600));

                var v7 = new RestaurantSaveData
                {
                    version = 7, coins = 600, completedSales = 18, grillLevel = 3, workerHired = false,
                    workerDeliveries = 0, hiredWorkerCount = 0
                };
                Assert.That(new LocalSaveStore(directory).Save(v7), Is.True);
                colaMachine.Upgrade.RestoreLevel(1);
                burgerGrill.Upgrade.RestoreLevel(1);
                wallet.RestoreProgress(0, 0);
                persistence.Configure(wallet, burgerGrill.Upgrade, hiring, null, null, null, null,
                    directory, cola: colaMachine.Upgrade);
                Assert.That(persistence.LoadResult, Is.EqualTo(SaveLoadResult.Loaded));
                Assert.That(wallet.Coins, Is.EqualTo(600));
                Assert.That(burgerGrill.Upgrade.Level, Is.EqualTo(3));
                Assert.That(colaMachine.Upgrade.Level, Is.EqualTo(1));
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }
    }
}
