using System.Collections.Generic;
using BurgerShop.Customer;
using BurgerShop.Economy;
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
        WorkerHiringZone hiring;
        Transform output;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("StaffTest");
            grill = root.AddComponent<ProductionStation>();
            output = new GameObject("Output").transform;
            output.SetParent(root.transform);
            grill.Configure(output, null, null);
            queue = root.AddComponent<CustomerQueue>();
            queue.Configure(new Vector3(-8.2f, 0f, -4.4f), new Vector3(-2f, 0f, -4.4f),
                new[] { new Vector3(-2f, 0f, 1.7f), new Vector3(-2f, 0f, -0.1f), new Vector3(-2f, 0f, -1.9f) }, new Vector3(-2f, 0f, 3.3f));
            wallet = root.AddComponent<RestaurantWallet>();
            GameObject carrier = new GameObject("Player");
            carrier.transform.SetParent(root.transform);
            player = carrier.AddComponent<BurgerInventory>();
            player.Configure();
            Transform servingPoint = Point("ServingPoint", new Vector3(0.9f, 0f, 3.3f));
            Transform pickupPoint = Point("PickupPoint", new Vector3(5.55f, 0f, 0.4f));
            Transform hiringPoint = Point("HiringPoint", new Vector3(0f, 0f, -5.5f));
            cashier = root.AddComponent<BurgerServingZone>();
            cashier.Configure(queue, player, wallet, servingPoint,
                new[] { new Vector3(-4.4f, 0f, 1.7f), new Vector3(-7.8f, 0f, 1.7f), new Vector3(-7.8f, 0f, 5.8f) });
            hiring = root.AddComponent<WorkerHiringZone>();
            hiring.Configure(grill, cashier, wallet, player, pickupPoint, hiringPoint, new Vector3(0.9f, 0f, 0.4f));
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
            for (int i = 0; i < 5; i++) wallet.RecordSale(10);
            player.transform.position = hiring.HiringPosition + Vector3.up;
            Hold(1.5f);
            player.transform.position = Vector3.zero;
            Assert.That(hiring.Worker, Is.Not.Null);
            return hiring.Worker;
        }

        void FillQueue()
        {
            for (int i = 0; i < 1200; i++) queue.Advance(1f / 60f);
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
            Assert.That(worker.State, Is.EqualTo(WorkerState.Serving));
            Assert.That(worker.Inventory.Count, Is.EqualTo(1));
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
            worker.Advance(0.25f);
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
            CustomerAgent customer = queue.ReadyCustomer;
            BurgerInventory first = playerFirst ? player : worker.Inventory;
            BurgerInventory second = playerFirst ? worker.Inventory : player;
            Assert.That(cashier.TryServeFrom(first), Is.True);
            Assert.That(cashier.TryServeFrom(second), Is.False);
            Assert.That(wallet.Coins, Is.EqualTo(10));
            Assert.That(customer.PaidAmount, Is.EqualTo(10));
            Assert.That(first.Count, Is.Zero);
            Assert.That(second.Count, Is.EqualTo(1));
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
            CustomerAgent first = queue.ReadyCustomer;
            cashier.TryServeFrom(player);
            first.AdvanceDeparture(100f);
            queue.Advance(1f);
            Assert.That(queue.ReadyCustomer, Is.Not.Null);
            for (int i = 0; i < 100; i++) Assert.That(cashier.TryServeFrom(worker.Inventory), Is.False);
            cashier.Advance(0.74f);
            Assert.That(cashier.TryServeFrom(worker.Inventory), Is.False);
            cashier.Advance(0.02f);
            Assert.That(cashier.TryServeFrom(worker.Inventory), Is.True);
            Assert.That(wallet.Coins, Is.EqualTo(20));
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
            Assert.That(wallet.Coins, Is.EqualTo(worker.CompletedDeliveries * 10));
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
    }
}
