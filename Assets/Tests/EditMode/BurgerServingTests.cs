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
        RestaurantWallet wallet;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("ServingTest");
            queue = root.AddComponent<CustomerQueue>();
            queue.Configure(new Vector3(-8f, 0f, -4f), new Vector3(-2f, 0f, -4f),
                new[] { new Vector3(-2f, 0f, 1.7f), new Vector3(-2f, 0f, -0.1f), new Vector3(-2f, 0f, -1.9f) }, new Vector3(-2f, 0f, 3.3f));
            GameObject player = new GameObject("Player");
            player.transform.SetParent(root.transform);
            inventory = player.AddComponent<BurgerInventory>();
            inventory.Configure();
            Transform output = new GameObject("Output").transform;
            output.SetParent(root.transform);
            grill = root.AddComponent<ProductionStation>();
            grill.Configure(output, null, null);
            wallet = root.AddComponent<RestaurantWallet>();
            Transform point = new GameObject("ServingPoint").transform;
            point.SetParent(root.transform);
            point.position = new Vector3(0.9f, 0f, 3.3f);
            serving = root.AddComponent<BurgerServingZone>();
            serving.Configure(queue, inventory, wallet, point,
                new[] { new Vector3(-4.4f, 0f, 1.7f), new Vector3(-7.8f, 0f, 1.7f), new Vector3(-7.8f, 0f, 5.8f) });
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

        void AtCounter() => inventory.transform.position = serving.ServingPosition + Vector3.up;

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
            Assert.That(inventory.Count, Is.EqualTo(1));
            Assert.That(queue.Count, Is.EqualTo(3));
            inventory.TryTakeBurger();
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
            Assert.That(wallet.Coins, Is.EqualTo(10));
            Assert.That(wallet.CompletedSales, Is.EqualTo(1));
            Assert.That(inventory.Count, Is.EqualTo(3));
            Assert.That(burger.parent, Is.EqualTo(customer.transform));
            Assert.That(burger.GetComponentInChildren<Renderer>().sharedMaterial, Is.SameAs(material));
            Assert.That(customer.PaidAmount, Is.EqualTo(10));
            Assert.That(customer.IsDeparting, Is.True);
            Assert.That(customer.transform.Find("OrderBubble").GetComponentInChildren<TextMesh>().text, Is.EqualTo("+10"));
            serving.Advance(100f);
            serving.Advance(100f);
            Assert.That(wallet.CompletedSales, Is.EqualTo(1));
            Assert.That(notifications, Is.EqualTo(1));
            Assert.That(queue.Count, Is.EqualTo(2));
        }

        [Test]
        public void PausedOrDisabledDependenciesLeaveAllBalancesUntouched()
        {
            FillQueue();
            Load();
            AtCounter();
            serving.Advance(0f);
            serving.Advance(-1f);
            foreach (Behaviour dependency in new Behaviour[] { queue, inventory, wallet })
            {
                dependency.enabled = false;
                serving.Advance(10f);
                dependency.enabled = true;
            }
            Assert.That(wallet.Coins, Is.Zero);
            Assert.That(queue.Count, Is.EqualTo(3));
            Assert.That(inventory.Count, Is.EqualTo(1));
        }

        [Test]
        public void QueueWaitsUntilDepartingCustomerStepsClear()
        {
            FillQueue();
            Load();
            AtCounter();
            CustomerAgent first = queue.ReadyCustomer;
            serving.Advance(0.01f);
            CustomerAgent second = queue.FrontCustomer;
            Vector3 waiting = second.transform.position;
            first.AdvanceDeparture(0f);
            for (int i = 0; i < 48; i++)
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
        public void DepartureContinuesAfterPlayerLeavesAndReleasesBurgerMaterials()
        {
            FillQueue();
            Load();
            AtCounter();
            CustomerAgent customer = queue.ReadyCustomer;
            Transform burger = inventory.transform.Find("CarryStack").GetChild(0);
            Material material = burger.GetComponentInChildren<Renderer>().sharedMaterial;
            serving.Advance(0.01f);
            inventory.transform.position = Vector3.zero;
            customer.AdvanceDeparture(0.2f);
            Assert.That(customer.IsDeparting, Is.True);
            Assert.That(material != null, Is.True);
            customer.AdvanceDeparture(0.3f);
            Assert.That(customer.transform.position.x, Is.LessThan(-2f));
            Assert.That(Vector3.Distance(burger.localPosition, new Vector3(0f, 0.85f, 0.6f)), Is.LessThan(0.001f));
            customer.AdvanceDeparture(100f);
            Assert.That(customer == null, Is.True);
            Assert.That(material == null, Is.True);
            serving.Advance(100f);
            Assert.That(wallet.Coins, Is.EqualTo(10));
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
                Assert.That(inventory.Count, Is.EqualTo(3 - i));
                Assert.That(wallet.Coins, Is.EqualTo(i * 10));
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
            Object.DestroyImmediate(customer.gameObject);
            FillQueue();
            serving.Advance(100f);
            Assert.That(queue.ReadyCustomer.TicketNumber, Is.EqualTo(2));
            Assert.That(wallet.Coins, Is.EqualTo(10));
        }

        [Test]
        public void InvalidPaymentsAndMissingExitDoNotStartTransactions()
        {
            Assert.That(wallet.RecordSale(0), Is.False);
            Assert.That(wallet.RecordSale(-10), Is.False);
            Assert.That(wallet.Coins, Is.Zero);
            Assert.That(wallet.CompletedSales, Is.Zero);
            Assert.Throws<System.ArgumentException>(() => serving.Configure(queue, inventory, wallet, null, new Vector3[0]));
        }
    }
}
