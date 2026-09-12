using BurgerShop.Customer;
using BurgerShop.Economy;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using BurgerShop.UI;
using NUnit.Framework;
using UnityEngine;

namespace BurgerShop.Tests.EditMode
{
    public sealed class SessionGoalTests
    {
        GameObject root;
        BurgerInventory inventory;
        ProductionStation grill;
        CounterStock stock;
        CustomerQueue queue;
        RestaurantWallet wallet;
        BurgerServingZone serving;
        SessionGoalTracker tracker;
        DiningTable table;
        TrashInventory trash;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("GoalTest");
            inventory = new GameObject("Player").AddComponent<BurgerInventory>();
            inventory.transform.SetParent(root.transform);
            inventory.Configure();
            Transform output = new GameObject("Output").transform;
            output.SetParent(root.transform);
            grill = root.AddComponent<ProductionStation>();
            grill.Configure(output, null, null);
            stock = root.AddComponent<CounterStock>();
            Transform anchor = new GameObject("Anchor").transform;
            anchor.SetParent(root.transform);
            stock.Configure(anchor, null);
            queue = root.AddComponent<CustomerQueue>();
            queue.OrderQuantityFactory = () => 1;
            queue.Configure(new Vector3(-8f, 0f, -4f), new Vector3(-2f, 0f, -4f),
                new[] { new Vector3(-2f, 0f, 1.7f), new Vector3(-2f, 0f, -0.1f), new Vector3(-2f, 0f, -1.9f) }, new Vector3(-2f, 0f, 3.3f));
            wallet = root.AddComponent<RestaurantWallet>();
            Transform point = new GameObject("Circle").transform;
            point.SetParent(root.transform);
            point.position = new Vector3(0.9f, 0f, 3.3f);
            CounterDropZone drop = root.AddComponent<CounterDropZone>();
            drop.Configure(stock, point);
            table = DiningTable.Create(root.transform, new Vector3(-5.6f, 0f, 0.2f));
            trash = inventory.gameObject.AddComponent<TrashInventory>();
            table.BindCollector(trash);
            serving = root.AddComponent<BurgerServingZone>();
            serving.Configure(queue, inventory, wallet, point, new[] { new Vector3(-8f, 0f, -4f) }, stock, table, drop);
            tracker = root.AddComponent<SessionGoalTracker>();
            tracker.Configure(inventory, grill, stock, queue, wallet, serving, DiningArea.Wrap(table), trash);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(root);

        [Test]
        public void OpeningDoesNotCelebratePreplacedFurniture()
        {
            Assert.That(tracker.Title, Is.EqualTo("Pick up a burger"));
            Assert.That(tracker.Progress, Is.Zero);
            Assert.That(tracker.Required, Is.EqualTo(1));
            Assert.That(tracker.IsCelebrating, Is.False);
            Assert.That(tracker.Stars, Is.EqualTo(0));
            Assert.That(tracker.StarLabel, Is.EqualTo("Lv.1  0/4"));
            tracker.Advance(2f);
            Assert.That(tracker.IsCelebrating, Is.False);
            Assert.That(tracker.Title, Is.EqualTo("Pick up a burger"));
            Assert.That(tracker.Progress, Is.Zero);
        }

        [Test]
        public void PickupThenThreeSalesAdvanceGuidanceWithoutStars()
        {
            tracker.Advance(2f);
            grill.Advance(12f);
            inventory.TryCollectFrom(grill);
            tracker.Advance(0.01f);
            Assert.That(tracker.Title, Is.EqualTo("Pick up a burger"));
            Assert.That(tracker.Progress, Is.EqualTo(1));
            Assert.That(tracker.IsCelebrating, Is.True);
            Assert.That(tracker.Stars, Is.EqualTo(0));
            tracker.Advance(2f);
            Assert.That(tracker.Title, Is.EqualTo("Serve customers"));
            Assert.That(tracker.Progress, Is.Zero);
            Assert.That(tracker.Required, Is.EqualTo(3));
            for (int n = 0; n < 3; n++)
            {
                Assert.That(wallet.RecordCompletedSale(), Is.True);
                tracker.Advance(0.01f);
            }
            Assert.That(tracker.IsCelebrating, Is.True);
            Assert.That(tracker.Stars, Is.EqualTo(0));
            tracker.Advance(2f);
            Assert.That(tracker.Title, Is.EqualTo("Install a table"));
            Assert.That(wallet.CompletedSales, Is.EqualTo(3));
        }

        [Test]
        public void DirtyTableAsksToClearThenTakeTrashToTheBin()
        {
            tracker.Advance(2f);
            table.LeaveMealTrash(0);
            tracker.Advance(0.01f);
            Assert.That(tracker.Title, Is.EqualTo("Clear the table"));
            inventory.transform.position = table.Center;
            table.Advance(1f);
            table.Advance(1f);
            Assert.That(table.TrashCount, Is.Zero);
            tracker.Advance(0.01f);
            Assert.That(tracker.Title, Is.EqualTo("Take trash to the bin"));
        }

        [Test]
        public void CapsuleAsksToOpenTheHrOfficeThenHireAWorker()
        {
            WorkerHiringZone staff = root.AddComponent<WorkerHiringZone>();
            staff.Configure(grill, serving, wallet, inventory, root.transform, root.transform, Vector3.zero, serving.DropZone);
            wallet.RestoreProgress(50, 0);
            tracker.Configure(inventory, grill, stock, queue, wallet, serving, DiningArea.Wrap(table), trash, staff);
            tracker.Restore(ShopRanks.Max, 0, 0);
            tracker.Advance(2f);
            Assert.That(tracker.Title, Is.EqualTo("Open the HR office"));
            inventory.transform.position = ShopLayout.HrHirePoint + Vector3.up;
            staff.Advance(0.01f);
            Assert.That(staff.HasVisitedOffice, Is.True);
            tracker.Advance(0.01f);
            Assert.That(tracker.Title, Does.Contain("Hire a worker"));
            Assert.That(tracker.Title, Does.Contain("50"));
        }

        [Test]
        public void CapsuleAsksToOpenTheBoostRoomThenUpgradeCarry()
        {
            inventory.gameObject.AddComponent<CharacterController>();
            PlayerMotor motor = inventory.gameObject.AddComponent<PlayerMotor>();
            Transform point = new GameObject("BoostPoint").transform;
            point.SetParent(root.transform);
            point.position = ShopLayout.BoostPoint;
            BoostUpgradeZone boost = root.AddComponent<BoostUpgradeZone>();
            boost.Configure(wallet, inventory, motor, point);
            wallet.RestoreProgress(50, 0);
            tracker.Configure(inventory, grill, stock, queue, wallet, serving, DiningArea.Wrap(table), trash, null, boost);
            tracker.Restore(ShopRanks.Max, 0, 0);
            tracker.Advance(2f);
            Assert.That(tracker.Title, Is.EqualTo("Open the boost room"));
            inventory.transform.position = ShopLayout.BoostPoint + Vector3.up;
            boost.Advance(0.01f);
            Assert.That(boost.HasVisitedRoom, Is.True);
            tracker.Advance(0.01f);
            Assert.That(tracker.Title, Is.EqualTo("Upgrade carry"));
        }
    }
}
