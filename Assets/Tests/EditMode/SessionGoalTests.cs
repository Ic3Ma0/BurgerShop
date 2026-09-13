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

        [Test] public void OpeningDoesNotCelebratePreplacedFurniture()
        {
            Assert.That(tracker.Title,Is.EqualTo("Complete a burger order"));
            Assert.That(tracker.Progress,Is.Zero);Assert.That(tracker.Stars,Is.Zero);
            Assert.That(tracker.IsCelebrating,Is.False);
        }
        [Test] public void PickupIsNotAnOrderAndMilestoneAwardsExactlyOnce()
        {
            grill.Advance(12);inventory.TryCollectFrom(grill);tracker.Advance(1);
            Assert.That(tracker.Progress,Is.Zero);
            tracker.RecordMilestone(ShopGoalKind.ServeCustomers);
            tracker.RecordMilestone(ShopGoalKind.ServeCustomers);
            Assert.That(tracker.Stars,Is.EqualTo(2));Assert.That(tracker.Progress,Is.EqualTo(1));
            Assert.That(tracker.TryUpgradeRank(1),Is.False);
            tracker.AddUpgradeStars();Assert.That(tracker.TryUpgradeRank(1),Is.True);
            Assert.That(tracker.Stars,Is.Zero);
        }
        [Test] public void CleaningRecordsTheRankTwoMilestone()
        {
            tracker.Restore(2,0,0);table.LeaveMealTrash(0);
            Assert.That(table.TryPickupTrash(trash),Is.True);
            Assert.That(tracker.MilestoneComplete,Is.True);
        }
        [Test] public void LockedFutureEventsCannotGrantStars()
        {
            tracker.RecordMilestone(ShopGoalKind.CourierOrder);
            tracker.RecordMilestone(ShopGoalKind.WorkerOrder);
            Assert.That(tracker.MilestoneMask,Is.Zero);Assert.That(tracker.Stars,Is.Zero);
        }
        [Test] public void OrdinaryActivitiesDoNotReplaceCurrentMilestone()
        {
            tracker.Restore(6,0,0);table.LeaveMealTrash(0);tracker.Advance(1);
            Assert.That(tracker.Title,Is.EqualTo("Complete a drive-thru order"));
            Assert.That(tracker.Progress,Is.Zero);
        }
    }
}
