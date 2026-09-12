using System;
using BurgerShop.Customer;
using BurgerShop.Economy;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BurgerShop.Tests.EditMode
{
    public sealed class Spec024Tests
    {
        GameObject root;
        BurgerInventory player;
        ProductionStation grill;
        CustomerQueue queue;
        RestaurantWallet wallet;
        CounterStock stock;
        CounterDropZone drop;
        BurgerServingZone serving;
        DiningArea dining;
        CashFloor cash;
        ShopExpansion expansion;
        WorkerHiringZone crew;
        int quantity = 3;

        [SetUp] public void SetUp()
        {
            quantity = 3;
            root = new GameObject("MultiOrderScenario");
            player = new GameObject("Player").AddComponent<BurgerInventory>();
            player.transform.SetParent(root.transform); player.Configure();
            grill = root.AddComponent<ProductionStation>();
            grill.Configure(Point("GrillOutput", ShopLayout.Grill), null, null);
            queue = root.AddComponent<CustomerQueue>();
            queue.OrderQuantityFactory = () => quantity;
            queue.Configure(ShopLayout.Entrance, ShopLayout.QueueEntry, ShopLayout.QueueSlots, ShopLayout.Counter);
            wallet = root.AddComponent<RestaurantWallet>();
            stock = root.AddComponent<CounterStock>();
            stock.Configure(Point("Stock", ShopLayout.CounterTop), null);
            drop = root.AddComponent<CounterDropZone>();
            var circle = Point("Serving", ShopLayout.ServingCircle);
            drop.Configure(stock, circle);
            dining = DiningArea.Create(root.transform, ShopLayout.Tables);
            cash = root.AddComponent<CashFloor>();
            cash.Configure(wallet, player.transform, ShopLayout.CounterCash);
            serving = root.AddComponent<BurgerServingZone>();
            serving.Configure(queue, player, wallet, circle, ShopLayout.Exit, stock, dining, drop, 10, cash);
            crew = root.AddComponent<WorkerHiringZone>();
            crew.Configure(grill, serving, wallet, player, root.transform, root.transform, ShopLayout.Aisle, drop);
            expansion = ShopExpansion.Create(root.transform, dining, serving, crew, player, wallet, cash);
        }
        [TearDown] public void TearDown() => Object.DestroyImmediate(root);
        Transform Point(string name, Vector3 position)
        {
            var point = new GameObject(name).transform; point.SetParent(root.transform); point.position = position; return point;
        }
        void Fill(CounterStock pile, int count, bool boxed = false)
        {
            for (int i = 0; i < count; i++)
            {
                grill.Advance(12f);
                Assert.That(player.TryCollectFrom(grill), Is.True);
                if (boxed) { player.TryBoxOne(); Assert.That(pile.TryPlaceBoxedFrom(player), Is.True); }
                else Assert.That(pile.TryPlaceFrom(player), Is.True);
            }
        }
        void WaitForCustomer()
        {
            for (int i = 0; i < 2400 && queue.ReadyCustomer == null; i++)
            {
                foreach (var c in root.GetComponentsInChildren<CustomerAgent>()) c.AdvanceDeparture(1f / 60);
                queue.Advance(1f / 60);
            }
            Assert.That(queue.ReadyCustomer, Is.Not.Null);
        }
        void FinishOne(BurgerInventory actor)
        {
            actor.transform.position = serving.NearestServePosition(actor.transform.position, true) + Vector3.up;
            Assert.That(serving.TryServeFrom(actor), Is.True);
            player.transform.position = Vector3.zero;
            serving.Advance(BurgerServingZone.HandoffDuration);
        }
        void Cooldown() { player.transform.position = Vector3.zero; serving.Advance(BurgerServingZone.BetweenItems + 0.001f); }

        [Test] public void AC01ThreeItemsWaitForTheLastBurgerAndSettleExactly30Once()
        {
            WaitForCustomer(); Fill(stock, 2);
            CustomerAgent customer = queue.ReadyCustomer;
            FinishOne(player);
            Assert.That(customer.RemainingQuantity, Is.EqualTo(2));
            Assert.That(wallet.CompletedSales, Is.Zero);
            Cooldown(); FinishOne(player);
            Assert.That(customer.RemainingQuantity, Is.EqualTo(1));
            Assert.That(customer.IsDeparting, Is.False);
            Assert.That(dining.OccupiedSeats, Is.Zero);
            Assert.That(cash.GroundValue, Is.Zero);
            Assert.That(stock.Count, Is.Zero);
            Cooldown(); player.transform.position = serving.ServingPosition;
            Assert.That(serving.TryServeFrom(player), Is.False);
            Fill(stock, 1); FinishOne(player);
            Assert.That(customer.RemainingQuantity, Is.Zero);
            Assert.That(wallet.CompletedSales, Is.EqualTo(1));
            Assert.That(cash.GroundValue, Is.EqualTo(30));
            Assert.That(cash.PileCount, Is.EqualTo(1));
            Assert.That(wallet.Coins, Is.Zero);
            serving.Advance(5f);
            Assert.That(cash.GroundValue, Is.EqualTo(30));
            Assert.That(wallet.CompletedSales, Is.EqualTo(1));
        }

        [Test] public void AC02TwoBoxCarRemainsParkedUntilSecondArrival()
        {
            expansion.Restore(false, false, false, 0, true, true);
            var lane = expansion.DriveThru;
            lane.OrderQuantityFactory = () => 2;
            Fill(expansion.Boxing.Package, 1, true);
            for (int i = 0; i < 600 && !lane.HasStoppedCarAtWindow; i++) lane.Advance(1f / 60);
            var order = lane.WaitingOrder;
            player.transform.position = lane.WindowPosition + Vector3.up;
            Assert.That(lane.TrySellFrom(player), Is.True);
            Assert.That(lane.TrySellFrom(player), Is.False);
            Assert.That(order.Remaining, Is.EqualTo(2));
            Vector3 parked = lane.WaitingCarPosition;
            player.transform.position = Vector3.zero;
            lane.Advance(DriveThruLane.HandoffDuration);
            Assert.That(order.Remaining, Is.EqualTo(1));
            Assert.That(lane.WaitingCarPosition, Is.EqualTo(parked));
            Assert.That(lane.HasStoppedCarAtWindow, Is.True);
            Assert.That(wallet.CompletedSales, Is.Zero);
            Assert.That(cash.GroundValue, Is.Zero);
            Fill(expansion.Boxing.Package, 1, true);
            player.transform.position = lane.WindowPosition;
            Assert.That(lane.TrySellFrom(player), Is.False, "0.75 seconds between starts");
            player.transform.position = Vector3.zero; lane.Advance(0.34f);
            player.transform.position = lane.WindowPosition;
            Assert.That(lane.TrySellFrom(player), Is.True);
            player.transform.position = Vector3.zero; lane.Advance(DriveThruLane.HandoffDuration);
            Assert.That(order.IsSettled, Is.True);
            Assert.That(lane.CompletedOrders, Is.EqualTo(1));
            Assert.That(wallet.CompletedSales, Is.EqualTo(1));
            Assert.That(cash.GroundValue, Is.EqualTo(30));
            Assert.That(cash.PileCount, Is.EqualTo(1));
            Assert.That(expansion.Boxing.PackageCount, Is.Zero);
        }

        [TestCase(true)] [TestCase(false)]
        public void AC03FourItemsKeepProgressAndAttributeOnlyTheFinalItem(bool workerFinishes)
        {
            quantity = 4; crew.RestoreWorkers(2, 0, 0);
            WaitForCustomer(); Fill(stock, 4);
            var customer = queue.ReadyCustomer;
            FinishOne(player); Cooldown(); FinishOne(crew.Workers[0].Inventory);
            player.transform.position = Vector3.zero;
            serving.Advance(2f);
            Assert.That(customer.RemainingQuantity, Is.EqualTo(2));
            Assert.That(crew.TotalDeliveries, Is.Zero);
            FinishOne(crew.Workers[1].Inventory); Cooldown();
            FinishOne(workerFinishes ? crew.Workers[1].Inventory : player);
            Assert.That(crew.Workers[0].CompletedDeliveries, Is.Zero);
            Assert.That(crew.Workers[1].CompletedDeliveries, Is.EqualTo(workerFinishes ? 1 : 0));
            Assert.That(wallet.CompletedSales, Is.EqualTo(1));
            Assert.That(cash.GroundValue, Is.EqualTo(40));
            Assert.That(stock.Count, Is.Zero);
        }

        [Test] public void AC04TwentyOrdersWithThreeContendersConserveAllFiftyBurgers()
        {
            serving.Configure(queue, player, wallet, Point("NoDiningCircle", ShopLayout.ServingCircle),
                ShopLayout.Exit, stock, (DiningArea)null, drop, 10, cash);
            int next = 0; queue.OrderQuantityFactory = () => (next++ % 4) + 1;
            crew.RestoreWorkers(2, 0, 0);
            Fill(stock, 50);
            int expectedUnits = 0;
            for (int orderIndex = 0; orderIndex < 20; orderIndex++)
            {
                WaitForCustomer(); var customer = queue.ReadyCustomer;
                int size = customer.OrderSize;
                for (int unit = 0; unit < size; unit++)
                {
                    Cooldown();
                    BurgerInventory[] actors = { player, crew.Workers[0].Inventory, crew.Workers[1].Inventory };
                    int winner = (orderIndex + unit) % 3;
                    foreach (var actor in actors) actor.transform.position = serving.ServingPosition + Vector3.up;
                    Assert.That(serving.TryServeFrom(actors[winner]), Is.True);
                    for (int i = 0; i < actors.Length; i++) Assert.That(serving.TryServeFrom(actors[i]), Is.False);
                    Assert.That(stock.Count + serving.DeliveredUnits + 1, Is.EqualTo(50));
                    Assert.That(customer.RemainingQuantity, Is.EqualTo(size - unit));
                    player.transform.position = Vector3.zero;
                    serving.Advance(BurgerServingZone.HandoffDuration);
                    expectedUnits++;
                    Assert.That(stock.Count + serving.DeliveredUnits, Is.EqualTo(50));
                    Assert.That(serving.DeliveredUnits, Is.EqualTo(expectedUnits));
                    Assert.That(wallet.CompletedSales, Is.EqualTo(orderIndex + (unit == size - 1 ? 1 : 0)));
                }
            }
            Assert.That(stock.Count, Is.Zero);
            Assert.That(serving.CompletedOrders, Is.EqualTo(20));
            Assert.That(cash.PileCount, Is.EqualTo(20));
            Assert.That(cash.GroundValue, Is.EqualTo(500));
            Assert.That(wallet.Coins, Is.Zero);
        }

        [Test] public void AC05CounterOwnershipCannotStealStockAndFourItemsUseOneSeatAndOneTip()
        {
            quantity = 4;
            expansion.Restore(false, false, true, 0);
            WaitForCustomer(); Fill(stock, 1); Fill(expansion.ExtraStock, 3);
            var customer = queue.ReadyCustomer;
            FinishOne(player); Cooldown();
            player.transform.position = ShopLayout.ExtraServingCircle;
            Assert.That(serving.TryServeFrom(player), Is.False);
            Assert.That(expansion.ExtraStock.Count, Is.EqualTo(3));
            Assert.That(serving.ReadyToSell, Is.False);
            Assert.That(serving.NearestDropPosition(ShopLayout.ExtraServingCircle), Is.EqualTo(drop.DropPosition));
            Fill(stock, 3);
            for (int i = 0; i < 3; i++) { Cooldown(); FinishOne(player); }
            Assert.That(dining.OccupiedSeats, Is.EqualTo(1));
            Assert.That(cash.GroundValue, Is.EqualTo(40));
            for (int i = 0; i < 2400 && customer != null; i++) customer.AdvanceDeparture(1f / 60);
            Assert.That(cash.GroundValue, Is.EqualTo(50));
            Assert.That(dining.OccupiedSeats, Is.Zero);
            int trash = 0;
            foreach (var table in dining.Tables) trash += table.TrashCount;
            Assert.That(trash, Is.EqualTo(DiningTable.TrashPerGuest));
            Assert.That(expansion.ExtraStock.Count, Is.EqualTo(3));
        }

        [Test] public void AC06PauseFreezesBothInFlightDeliveriesAndResumeDoesNotReplay()
        {
            WaitForCustomer(); Fill(stock, 1);
            var customer = queue.ReadyCustomer;
            player.transform.position = serving.ServingPosition;
            Assert.That(serving.TryServeFrom(player), Is.True);
            player.transform.position = Vector3.zero;
            serving.Advance(0.1f);
            Pause(serving, true); serving.Advance(10f);
            Assert.That(customer.Order.Delivered, Is.Zero);
            Assert.That(customer.Order.InFlight, Is.True);
            Pause(serving, false); serving.Advance(0.35f);
            Assert.That(customer.Order.Delivered, Is.EqualTo(1));
            serving.Advance(10f);
            Assert.That(customer.Order.Delivered, Is.EqualTo(1));
            expansion.Restore(false, false, false, 0, true, true);
            var lane = expansion.DriveThru; lane.OrderQuantityFactory = () => 2;
            Fill(expansion.Boxing.Package, 1, true);
            for (int i = 0; i < 600 && !lane.HasStoppedCarAtWindow; i++) lane.Advance(1f / 60);
            player.transform.position = lane.WindowPosition; Assert.That(lane.TrySellFrom(player), Is.True);
            player.transform.position = Vector3.zero; lane.Advance(0.1f);
            Pause(lane, true); lane.Advance(10f);
            Assert.That(lane.WaitingOrder.Delivered, Is.Zero);
            Pause(lane, false); lane.Advance(0.32f);
            Assert.That(lane.WaitingOrder.Delivered, Is.EqualTo(1));
            Assert.That(wallet.CompletedSales, Is.Zero);
            Assert.That(cash.GroundValue, Is.Zero);
        }
        static void Pause(MonoBehaviour component, bool pause) => component.GetType()
            .GetMethod("OnApplicationPause", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .Invoke(component, new object[] { pause });

        [Test] public void AC07AllQuantitiesAndConfiguredWeightsAreCoveredWithoutStatisticalSampling()
        {
            Assert.That(OrderQuantities.DiningWeights, Is.EqualTo(new[] { 50, 30, 15, 5 }));
            Assert.That(OrderQuantities.DriveWeights, Is.EqualTo(new[] { 60, 40 }));
            int[] diningBuckets = new int[4]; int[] driveBuckets = new int[2];
            for (int roll = 0; roll < 100; roll++)
            {
                diningBuckets[OrderQuantities.DiningForRoll(roll) - 1]++;
                driveBuckets[OrderQuantities.DriveForRoll(roll) - 1]++;
            }
            Assert.That(diningBuckets, Is.EqualTo(new[] { 50, 30, 15, 5 }));
            Assert.That(driveBuckets, Is.EqualTo(new[] { 60, 40 }));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CustomerOrder(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CustomerOrder(5));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CustomerOrder(3, 2));
            int calls = 0; queue.OrderQuantityFactory = () => { calls++; return 4; };
            WaitForCustomer(); var first = queue.ReadyCustomer;
            for (int i = 0; i < 2400; i++) queue.Advance(1f / 60);
            Assert.That(calls, Is.EqualTo(queue.Capacity));
            Assert.That(queue.ReadyCustomer, Is.SameAs(first));
            Assert.That(first.OrderSize, Is.EqualTo(4));
            Assert.That(first.transform.Find("OrderBubble").GetComponentInChildren<TextMesh>().text, Is.EqualTo("x4"));
        }
    }
}
