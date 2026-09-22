using BurgerShop.Customer;
using BurgerShop.Economy;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using NUnit.Framework;
using UnityEngine;

namespace BurgerShop.Tests.EditMode
{
    public sealed class BurgerServingTests
    {
        GameObject root;
        CustomerQueue queue;
        BurgerInventory inventory;
        ProductionStation grill;
        BurgerServingZone serving;
        CounterStock stock;
        CounterDropZone drop;
        DiningTable table;
        RestaurantWallet wallet;
        CashFloor cash;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("ServingTest");
            queue = root.AddComponent<CustomerQueue>();
            queue.OrderQuantityFactory = () => 1;
            queue.Configure(new Vector3(-8f, 0f, -4f), new Vector3(-2f, 0f, -4f),
                new[] { new Vector3(-2f, 0f, 1.7f), new Vector3(-2f, 0f, -0.1f), new Vector3(-2f, 0f, -1.9f) }, new Vector3(-2f, 0f, 3.3f));
            GameObject player = new GameObject("Player");
            player.transform.SetParent(root.transform);
            inventory = player.AddComponent<BurgerInventory>();
            inventory.Configure();
            Transform output = new GameObject("Output").transform;
            output.SetParent(root.transform);
            output.position = new Vector3(5f, 0.8f, 9f);
            grill = root.AddComponent<ProductionStation>();
            grill.Configure(output, null, null);
            wallet = root.AddComponent<RestaurantWallet>();
            Transform point = new GameObject("ServingPoint").transform;
            point.SetParent(root.transform);
            point.position = new Vector3(0.9f, 0f, 3.3f);
            Transform anchor = new GameObject("StockAnchor").transform;
            anchor.SetParent(root.transform);
            anchor.position = new Vector3(-2f, 1.23f, 4f);
            stock = root.AddComponent<CounterStock>();
            stock.Configure(anchor, null);
            drop = root.AddComponent<CounterDropZone>();
            drop.Configure(stock, point);
            table = DiningTable.Create(root.transform, new Vector3(-5.6f, 0f, 0.2f));
            cash = root.AddComponent<CashFloor>();
            serving = root.AddComponent<BurgerServingZone>();
            serving.Configure(queue, inventory, wallet, point,
                new[] { new Vector3(-8.2f, 0f, -4.4f), new Vector3(-10.5f, 0f, -6.2f) }, stock, table, drop, 10, cash);
            cash.Configure(wallet, inventory.transform, CashFloor.CounterDropPosition(serving.ServingPosition));
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(root);

        void FillQueue()
        {
            for (int i = 0; i < 1200; i++) queue.Advance(1f / 60f);
        }

        void Load(int count = 1)
        {
            grill.Advance(12f);
            for (int i = 0; i < count; i++) inventory.TryCollectFrom(grill);
        }

        void CompleteHandoff()
        {
            Vector3 position = inventory.transform.position;
            inventory.transform.position = Vector3.zero;
            serving.Advance(BurgerServingZone.HandoffDuration);
            inventory.transform.position = position;
        }

        void AtCounter() => inventory.transform.position = serving.ServingPosition + Vector3.up;

        void AtCash() => inventory.transform.position = cash.NearestPilePosition;

        void CollectGroundCash()
        {
            if (cash.PileCount == 0) return;
            AtCash();
            serving.Advance(1f);
        }

        [Test]
        public void Spec083BigEaterPaysOneHundredOnlyAfterTenBurgers()
        {
            queue.CustomerKindFactory = () => CustomerKind.BigEater;
            FillQueue();
            var customer = queue.ReadyCustomer;
            Assert.That(customer.OrderSize, Is.EqualTo(10));
            AtCounter();
            for (int delivered = 1; delivered <= 10; delivered++)
            {
                grill.Advance(12f);
                Assert.That(inventory.TryCollectFrom(grill), Is.True);
                Assert.That(stock.TryPlaceFrom(inventory), Is.True);
                serving.Advance(1f);
                CompleteHandoff();
                Assert.That(customer.RemainingQuantity, Is.EqualTo(10 - delivered));
                if (delivered < 10)
                {
                    Assert.That(wallet.CompletedSales, Is.Zero);
                    Assert.That(cash.GroundValue, Is.Zero);
                }
            }
            Assert.That(customer.PaidAmount, Is.EqualTo(100));
            Assert.That(cash.GroundValue, Is.EqualTo(100));
            Assert.That(wallet.CompletedSales, Is.EqualTo(1));
            serving.Advance(100f);
            Assert.That(cash.GroundValue, Is.EqualTo(100));
            Assert.That(wallet.CompletedSales, Is.EqualTo(1));
        }

        [Test]
        public void EmptyHandsOutOfRangeAndWalkingCustomersCannotCharge()
        {
            AtCounter();
            serving.Advance(1f);
            Load();
            queue.Advance(1.5f);
            serving.Advance(1f);
            Assert.That(wallet.Coins, Is.Zero);
            FillQueue();
            inventory.transform.position = Vector3.zero;
            serving.Advance(1f);
            Assert.That(wallet.Coins, Is.Zero);
            Assert.That(inventory.Count + stock.Count, Is.EqualTo(1));
            Assert.That(queue.Count, Is.EqualTo(3));
            while (inventory.Count > 0) inventory.TryTakeBurger();
            while (stock.Count > 0) stock.TryTakeBurger(out _);
            AtCounter();
            serving.Advance(1f);
            Assert.That(wallet.CompletedSales, Is.Zero);
            Assert.That(queue.Count, Is.EqualTo(3));
        }

        [Test]
        public void SaleTransfersOriginalBurgerAndPaysExactlyOnce()
        {
            FillQueue();
            Load(4);
            AtCounter();
            Transform burger = inventory.transform.Find("CarryStack").GetChild(3);
            Material material = burger.GetComponentInChildren<Renderer>().sharedMaterial;
            CustomerAgent customer = queue.ReadyCustomer;
            int notifications = 0;
            wallet.SaleRecorded += amount => { Assert.That(amount, Is.EqualTo(10)); notifications++; };
            serving.Advance(0.01f);
            CompleteHandoff();
            Assert.That(wallet.Coins, Is.Zero);
            Assert.That(cash.GroundValue, Is.EqualTo(10));
            Assert.That(wallet.CompletedSales, Is.EqualTo(1));
            Assert.That(inventory.Count, Is.EqualTo(3));
            Assert.That(stock.Count, Is.Zero);
            Assert.That(burger.parent, Is.EqualTo(customer.transform));
            Assert.That(burger.GetComponentInChildren<Renderer>().sharedMaterial, Is.SameAs(material));
            Assert.That(customer.PaidAmount, Is.EqualTo(10));
            Assert.That(customer.IsDeparting, Is.True);
            Assert.That(customer.transform.Find("OrderBubble").GetComponentInChildren<TextMesh>().text, Is.Empty);
            serving.Advance(100f);
            serving.Advance(100f);
            Assert.That(wallet.CompletedSales, Is.EqualTo(1));
            Assert.That(notifications, Is.Zero);
            Assert.That(wallet.Coins, Is.Zero);
            CollectGroundCash();
            Assert.That(wallet.Coins, Is.EqualTo(10));
            Assert.That(notifications, Is.EqualTo(1));
            Assert.That(queue.Count, Is.EqualTo(2));
        }

        [Test]
        public void HandsDoNotServeUntilBurgerIsOnTheCounter()
        {
            FillQueue();
            Load();
            AtCounter();
            Assert.That(serving.TryServeFrom(inventory), Is.False);
            Assert.That(wallet.Coins, Is.Zero);
            Assert.That(drop.TryDepositFrom(inventory), Is.True);
            Assert.That(stock.Count, Is.EqualTo(1));
            Assert.That(inventory.Count, Is.Zero);
            Assert.That(serving.TryServeFrom(inventory), Is.True);
            CompleteHandoff();
            Assert.That(wallet.Coins, Is.Zero);
            Assert.That(cash.GroundValue, Is.EqualTo(10));
            Assert.That(stock.Count, Is.Zero);
        }

        [Test]
        public void OutsideTheWhiteCircleCannotServeEvenWithCounterStock()
        {
            FillQueue();
            Load();
            AtCounter();
            drop.TryDepositFrom(inventory);
            inventory.transform.position = Vector3.zero;
            serving.Advance(1f);
            Assert.That(wallet.Coins, Is.Zero);
            Assert.That(stock.Count, Is.EqualTo(1));
            AtCounter();
            serving.Advance(1f);
            CompleteHandoff();
            Assert.That(wallet.Coins, Is.Zero);
            Assert.That(cash.GroundValue, Is.EqualTo(10));
        }

        [Test]
        public void PausedOrDisabledDependenciesLeaveAllBalancesUntouched()
        {
            FillQueue();
            Load();
            AtCounter();
            serving.Advance(0f);
            serving.Advance(-1f);
            foreach (Behaviour dependency in new Behaviour[] { queue, inventory, wallet, stock })
            {
                dependency.enabled = false;
                serving.Advance(10f);
                dependency.enabled = true;
            }
            Assert.That(wallet.Coins, Is.Zero);
            Assert.That(queue.Count, Is.EqualTo(3));
            Assert.That(inventory.Count + stock.Count, Is.EqualTo(1));
        }

        [Test]
        public void QueueWaitsUntilDepartingCustomerStepsClear()
        {
            FillQueue();
            Load();
            AtCounter();
            CustomerAgent first = queue.ReadyCustomer;
            serving.Advance(0.01f);
            CompleteHandoff();
            CustomerAgent second = queue.FrontCustomer;
            Vector3 waiting = second.transform.position;
            first.AdvanceDeparture(0f);
            for (int i = 0; i < 24; i++)
            {
                first.AdvanceDeparture(1f / 60f);
                queue.Advance(1f / 60f);
                Assert.That(second.transform.position, Is.EqualTo(waiting));
            }
            for (int i = 0; i < 90; i++)
            {
                first.AdvanceDeparture(1f / 60f);
                queue.Advance(1f / 60f);
                Assert.That(Vector3.Distance(first.transform.position, second.transform.position), Is.GreaterThan(0.7f));
            }
            Assert.That(second.HasReachedSlot, Is.True);
            Assert.That(queue.ReadyCustomer, Is.SameAs(second));
        }

        [Test]
        public void ServedCustomerEatsAtTheTableThenLeaves()
        {
            FillQueue();
            Load();
            AtCounter();
            CustomerAgent customer = queue.ReadyCustomer;
            serving.Advance(0.01f);
            CompleteHandoff();
            Assert.That(customer.IsDining || customer.IsDeparting, Is.True);
            customer.AdvanceDeparture(0.4f);
            for (int i = 0; i < 180; i++) customer.AdvanceDeparture(1f / 60f);
            Assert.That(customer.IsEating, Is.True);
            Assert.That(Vector3.Distance(customer.transform.position, table.transform.position), Is.LessThan(1.3f));
            customer.AdvanceDeparture(5.1f);
            Assert.That(customer.IsEating, Is.False);
            customer.AdvanceDeparture(100f);
            Assert.That(customer == null, Is.True);
            Assert.That(table.OccupiedSeats, Is.Zero);
        }

        [Test]
        public void DepartureContinuesAfterPlayerLeavesAndReleasesBurgerMaterials()
        {
            FillQueue();
            Load();
            AtCounter();
            CustomerAgent customer = queue.ReadyCustomer;
            Transform burger = inventory.transform.Find("CarryStack").GetChild(0);
            Material material = burger.GetComponentInChildren<Renderer>().sharedMaterial;
            serving.Advance(0.01f);
            CompleteHandoff();
            inventory.transform.position = Vector3.zero;
            customer.AdvanceDeparture(0.2f);
            Assert.That(customer.IsDeparting, Is.True);
            Assert.That(material != null, Is.True);
            customer.AdvanceDeparture(0.3f);
            Assert.That(customer.transform.position.x, Is.LessThan(-2f));
            customer.AdvanceDeparture(100f);
            Assert.That(customer == null, Is.True);
            Assert.That(material == null, Is.True);
            serving.Advance(100f);
            Assert.That(wallet.Coins, Is.Zero);
            Assert.That(cash.GroundValue, Is.EqualTo(20));
            Assert.That(wallet.CompletedSales, Is.EqualTo(1));
        }

        [Test]
        public void ThreeSalesConserveBurgersCoinsAndQueueOrder()
        {
            FillQueue();
            Load(3);
            AtCounter();
            for (int i = 1; i <= 3; i++)
            {
                CustomerAgent customer = queue.ReadyCustomer;
                Assert.That(customer.TicketNumber, Is.EqualTo(i));
                serving.Advance(1f);
                CompleteHandoff();
                Assert.That(wallet.Coins, Is.Zero);
                Assert.That(cash.GroundValue, Is.GreaterThanOrEqualTo(i * 10));
                customer.AdvanceDeparture(100f);
                FillQueue();
            }
            serving.Advance(100f);
            Assert.That(wallet.CompletedSales, Is.EqualTo(3));
            Assert.That(queue.Count, Is.EqualTo(3));
            Assert.That(queue.FrontCustomer.TicketNumber, Is.EqualTo(4));
            Assert.That(grill.Stock, Is.EqualTo(1));
        }

        [Test]
        public void DestroyedDepartingCustomerDoesNotPayAgainOrBlockQueue()
        {
            FillQueue();
            Load();
            AtCounter();
            CustomerAgent customer = queue.ReadyCustomer;
            serving.Advance(0.1f);
            CompleteHandoff();
            Object.DestroyImmediate(customer.gameObject);
            FillQueue();
            serving.Advance(100f);
            Assert.That(queue.ReadyCustomer.TicketNumber, Is.EqualTo(2));
            Assert.That(wallet.Coins, Is.Zero);
            Assert.That(cash.GroundValue, Is.EqualTo(10));
        }

        [Test]
        public void InvalidPaymentsAndMissingExitDoNotStartTransactions()
        {
            Assert.That(wallet.RecordSale(0), Is.False);
            Assert.That(wallet.RecordSale(-10), Is.False);
            Assert.That(wallet.Coins, Is.Zero);
            Assert.That(wallet.CompletedSales, Is.Zero);
            Assert.Throws<System.ArgumentException>(() => serving.Configure(queue, inventory, wallet, null, new Vector3[0], stock, table, drop));
        }

        [Test]
        public void CounterAcceptsFiveOrMoreBurgersAndShowsTheCount()
        {
            Transform anchor = root.transform.Find("StockAnchor");
            Transform badge = new GameObject("CounterBadge").transform;
            badge.SetParent(root.transform, false);
            TextMesh label = new GameObject("CounterHud").AddComponent<TextMesh>();
            label.transform.SetParent(badge, false);
            stock.Configure(anchor, label);
            AtCounter();
            DepositUntil(5);
            Assert.That(stock.IsFull, Is.False);
            Assert.That(stock.Count, Is.EqualTo(5));
            Assert.That(label.text, Is.EqualTo("5"));
            Assert.That(label.gameObject.activeSelf, Is.True);

            DepositUntil(12);
            Assert.That(stock.IsFull, Is.False);
            Assert.That(stock.Count, Is.EqualTo(12));
            Assert.That(label.text, Is.EqualTo("12"));
            Assert.That(anchor.childCount, Is.EqualTo(12));
            Vector3 first = anchor.GetChild(0).localPosition;
            for (int i = 0; i < anchor.childCount; i++)
            {
                Vector3 local = anchor.GetChild(i).localPosition;
                Assert.That(local.x, Is.EqualTo(first.x).Within(0.001f));
                Assert.That(local.z, Is.EqualTo(first.z).Within(0.001f));
                Assert.That(local.y, Is.EqualTo(i * CounterStock.LayerHeight).Within(0.001f));
            }
        }

        [Test]
        public void TakingABurgerFliesTheCounterVisualNotASpawnAtTheOrigin()
        {
            FillQueue();
            Transform anchor = root.transform.Find("StockAnchor");
            DepositUntil(12);
            Assert.That(anchor.childCount, Is.EqualTo(12));
            Transform piled = anchor.GetChild(anchor.childCount - 1);
            Vector3 pilePos = piled.position;
            Assert.That(pilePos.y, Is.GreaterThan(1f));
            Assert.That(Vector3.Distance(pilePos, Vector3.zero), Is.GreaterThan(2f));
            Assert.That(Vector3.Distance(pilePos, grill.OutputAnchor.position), Is.GreaterThan(2f));

            AtCounter();
            Assert.That(serving.TryServeFrom(inventory), Is.True);
            Assert.That(stock.Count, Is.EqualTo(11));
            Assert.That(anchor.childCount, Is.EqualTo(11));
            Assert.That(piled.parent, Is.EqualTo(queue.ReadyCustomer.transform));
            Assert.That(Vector3.Distance(piled.position, pilePos), Is.LessThan(0.05f));

            serving.Advance(0.08f);
            Assert.That(piled.gameObject.activeInHierarchy, Is.True);
            Assert.That(Vector3.Distance(piled.position, pilePos), Is.LessThan(Vector3.Distance(piled.position, Vector3.zero)));
            Assert.That(Vector3.Distance(piled.position, pilePos),
                Is.LessThan(Vector3.Distance(piled.position, grill.OutputAnchor.position)));
        }

        [Test]
        public void TakingABoxedItemReturnsTheSameCounterTransform()
        {
            Transform anchor = root.transform.Find("StockAnchor");
            grill.Advance(12f);
            Assert.That(inventory.TryCollectFrom(grill), Is.True);
            Assert.That(inventory.TryBoxOne(), Is.True);
            Assert.That(stock.TryPlaceBoxedFrom(inventory), Is.True);
            Assert.That(anchor.childCount, Is.EqualTo(1));
            Transform boxed = anchor.GetChild(0);
            Vector3 pilePos = boxed.position;
            Assert.That(stock.TryTakeBoxed(out Transform taken), Is.True);
            Assert.That(taken, Is.SameAs(boxed));
            Assert.That(stock.Count, Is.Zero);
            Assert.That(anchor.childCount, Is.Zero);
            Assert.That(Vector3.Distance(taken.position, pilePos), Is.LessThan(0.01f));
            Assert.That(Vector3.Distance(taken.position, Vector3.zero), Is.GreaterThan(2f));
        }

        [Test]
        public void ServedCustomersSitAtSecondAndThirdTablesThenWaitWhenFull()
        {
            DiningArea area = DiningArea.Create(root.transform, new[]
            {
                new Vector3(-5.6f, 0f, 0.2f),
                new Vector3(-5.6f, 0f, -2.5f),
                new Vector3(-8.1f, 0f, 0.2f)
            });
            Transform point = root.transform.Find("ServingPoint");
            serving.Configure(queue, inventory, wallet, point,
                new[] { new Vector3(-8.2f, 0f, -4.4f), new Vector3(-10.5f, 0f, -6.2f) }, stock, area, drop);
            FillQueue();
            Load(4);
            AtCounter();
            var seated = new System.Collections.Generic.List<CustomerAgent>();
            for (int n = 0; n < 6; n++)
            {
                if (queue.ReadyCustomer == null) FillQueue();
                if (inventory.Count == 0)
                {
                    grill.Advance(12f);
                    Load(2);
                }
                CustomerAgent customer = queue.ReadyCustomer;
                Assert.That(customer, Is.Not.Null, "queue was not ready for seat " + n);
                serving.Advance(1f);
                CompleteHandoff();
                Assert.That(customer.IsDeparting, Is.True);
                SitAtTable(customer);
                seated.Add(customer);
            }
            Assert.That(area.OccupiedSeats, Is.EqualTo(6));
            Assert.That(area.Tables[0].OccupiedSeats, Is.EqualTo(2));
            Assert.That(area.Tables[1].OccupiedSeats, Is.EqualTo(2));
            Assert.That(area.Tables[2].OccupiedSeats, Is.EqualTo(2));
            Assert.That(seated.Count, Is.EqualTo(6));

            FillQueue();
            if (inventory.Count == 0)
            {
                grill.Advance(12f);
                Load();
            }
            CustomerAgent waiter = queue.ReadyCustomer;
            Assert.That(waiter, Is.Not.Null);
            serving.Advance(1f);
            CompleteHandoff();
            Assert.That(waiter.IsDeparting, Is.True);
            waiter.AdvanceDeparture(0.4f);
            for (int i = 0; i < 240; i++) waiter.AdvanceDeparture(1f / 60f);
            Assert.That(waiter.IsEating, Is.False);
            Assert.That(waiter.IsDining, Is.True);
            Assert.That(waiter.gameObject.activeSelf, Is.True);
            Vector3 waitOffset = waiter.transform.position - area.WaitPosition;
            waitOffset.y = 0f;
            Assert.That(waitOffset.magnitude, Is.LessThan(0.15f));
        }

        [Test]
        public void LastBurgerOfATwoItemOrderLeavesTheCounter()
        {
            queue.OrderQuantityFactory = () => 2;
            FillQueue();
            Load(2);
            AtCounter();
            CustomerAgent customer = queue.ReadyCustomer;
            Vector3 slot = customer.transform.position;
            serving.Advance(0.01f);
            CompleteHandoff();
            Assert.That(customer.IsDeparting, Is.False);
            Assert.That(customer.RemainingQuantity, Is.EqualTo(1));
            Assert.That(queue.ReadyCustomer, Is.SameAs(customer));
            serving.Advance(1f);
            CompleteHandoff();
            Assert.That(customer.Order.IsComplete, Is.True);
            Assert.That(customer.IsDeparting, Is.True);
            Assert.That(queue.FrontCustomer, Is.Not.SameAs(customer));
            customer.AdvanceDeparture(0.4f);
            for (int i = 0; i < 60; i++) customer.AdvanceDeparture(1f / 60f);
            Vector3 moved = customer.transform.position - slot;
            moved.y = 0f;
            Assert.That(moved.magnitude, Is.GreaterThan(0.4f));
        }

        void DepositUntil(int total)
        {
            AtCounter();
            while (stock.Count < total)
            {
                if (inventory.Count == 0)
                {
                    grill.Advance(12f);
                    int need = Mathf.Min(inventory.Capacity, total - stock.Count);
                    for (int i = 0; i < need; i++)
                        Assert.That(inventory.TryCollectFrom(grill), Is.True);
                }
                drop.Advance(1f);
                Assert.That(drop.TryDepositFrom(inventory), Is.True, "counter must accept burger " + (stock.Count + 1));
            }
        }

        void SitAtTable(CustomerAgent customer)
        {
            customer.AdvanceDeparture(0.4f);
            for (int i = 0; i < 240; i++)
            {
                customer.AdvanceDeparture(1f / 60f);
                queue.Advance(1f / 60f);
            }
            Assert.That(customer.IsEating, Is.True);
        }
    }
}
