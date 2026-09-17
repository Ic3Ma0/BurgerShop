using BurgerShop.Core;
using BurgerShop.Customer;
using BurgerShop.Economy;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using BurgerShop.UI;
using NUnit.Framework;
using UnityEngine;

namespace BurgerShop.Tests.EditMode
{
    public sealed class Spec065Tests
    {
        GameObject root;

        [SetUp]
        public void SetUp() => root = new GameObject("Spec065");

        [TearDown]
        public void TearDown()
        {
            ShopLayout.ResetWingLock();
            Object.DestroyImmediate(root);
        }

        [Test]
        public void IndoorWalkAlwaysReturnsAPathThroughTheDoorAndAroundTheCounter()
        {
            Vector3[] enter = ShopLayout.Walk(RestaurantEntrance.Outside, ShopLayout.QueueSlots[0]);
            Assert.That(enter, Is.Not.Null.And.Not.Empty);
            Assert.That(System.Array.Exists(enter,
                p => Vector3.Distance(Flat(p), Flat(RestaurantEntrance.Door)) < 0.2f));
            Assert.That(enter[enter.Length - 1], Is.EqualTo(ShopLayout.QueueSlots[0]));

            Vector3[] toTable = ShopLayout.IndoorRoute(ShopLayout.QueueSlots[0], ShopLayout.Tables[0] + new Vector3(-1.15f, 0f, 0f));
            Assert.That(toTable, Is.Not.Empty);
            for (int i = 0; i < toTable.Length; i++)
            {
                Vector3 p = toTable[i];
                Assert.That(p.x > -4.8f && p.x < -1.2f && p.z > 3.2f && p.z < 4.8f, Is.False,
                    "counter clip at " + p);
            }
        }

        [Test]
        public void OneAndTwoItemOrdersLeaveTheServingPoint()
        {
            var serving = ServingRig(1);
            var queue = serving.GetComponent<CustomerQueue>();
            FillQueue(queue);
            Load(serving, 1);
            AtCounter(serving);
            CustomerAgent one = queue.ReadyCustomer;
            Vector3 slot = one.transform.position;
            serving.Advance(0.01f);
            serving.Advance(BurgerServingZone.HandoffDuration);
            Assert.That(one.IsDeparting, Is.True);
            one.AdvanceDeparture(0.4f);
            for (int i = 0; i < 60; i++) one.AdvanceDeparture(1f / 60f);
            Vector3 moved = one.transform.position - slot;
            moved.y = 0f;
            Assert.That(moved.magnitude, Is.GreaterThan(0.4f));

            Object.DestroyImmediate(root);
            root = new GameObject("Spec065-two");
            serving = ServingRig(2);
            queue = serving.GetComponent<CustomerQueue>();
            FillQueue(queue);
            Load(serving, 2);
            AtCounter(serving);
            CustomerAgent two = queue.ReadyCustomer;
            Vector3 twoSlot = two.transform.position;
            serving.Advance(0.01f);
            serving.Advance(BurgerServingZone.HandoffDuration);
            serving.Advance(1f);
            serving.Advance(BurgerServingZone.HandoffDuration);
            Assert.That(two.Order.IsComplete, Is.True);
            Assert.That(two.IsDeparting, Is.True);
            two.AdvanceDeparture(0.4f);
            for (int i = 0; i < 60; i++) two.AdvanceDeparture(1f / 60f);
            Vector3 twoMoved = two.transform.position - twoSlot;
            twoMoved.y = 0f;
            Assert.That(twoMoved.magnitude, Is.GreaterThan(0.4f));
        }

        [Test]
        public void DiningGuestWalksToASeatWithoutEnteringWalls()
        {
            Material wall = RuntimeMaterials.Create(new Color(0.4f, 0.3f, 0.2f));
            ShopLayout.CreateWalls(root.transform, wall);
            DiningArea dining = DiningArea.Create(root.transform, ShopLayout.Tables);
            CustomerAgent guest = CustomerAgent.Create(root.transform, 1, ShopLayout.QueueSlots[0]);
            Assert.That(dining.TryAssignSeat(guest, out _, out _, out _), Is.True);
            dining.Tables[0].Release(guest);
            guest.BeginDeparture(null, ShopLayout.Exit, 10, dining, true);
            Assert.That(dining.OccupiedSeats, Is.EqualTo(1));
            Physics.SyncTransforms();
            bool sat = false;
            for (int i = 0; i < 900; i++)
            {
                guest.AdvanceDeparture(1f / 60f);
                Assert.That(ShopLayout.OccupiesWall(guest.transform.position), Is.False,
                    "wall clip at " + guest.transform.position);
                if (guest.IsEating)
                {
                    sat = true;
                    break;
                }
            }
            Assert.That(guest.IsDeparting, Is.True);
            Assert.That(sat, Is.True);
            Assert.That(dining.OccupiedSeats, Is.EqualTo(1));
        }

        [Test]
        public void DirtyTablesSendGuestsToTheWaitPointAndACleanNeighborGetsTheSeat()
        {
            Material wall = RuntimeMaterials.Create(new Color(0.4f, 0.3f, 0.2f));
            ShopLayout.CreateWalls(root.transform, wall);
            DiningArea dining = DiningArea.Create(root.transform, ShopLayout.Tables);
            dining.Tables[0].LeaveMealTrash(0);
            CustomerAgent guest = CustomerAgent.Create(root.transform, 1, ShopLayout.QueueSlots[0]);
            Assert.That(dining.TryAssignSeat(guest, out DiningTable assigned, out Vector3 wait, out int seat), Is.True);
            Assert.That(assigned, Is.SameAs(dining.Tables[1]));
            Assert.That(seat, Is.GreaterThanOrEqualTo(0));
            dining.Tables[1].Release(guest);

            dining.Tables[1].LeaveMealTrash(0);
            Assert.That(dining.TryAssignSeat(guest, out assigned, out wait, out seat), Is.False);
            Assert.That(seat, Is.EqualTo(-1));
            Assert.That(wait, Is.EqualTo(assigned != null ? assigned.WaitPosition : dining.WaitPosition));

            guest.BeginDeparture(null, ShopLayout.Exit, 10, dining, true);
            bool reachedWait = false;
            Vector3 waitPoint = wait;
            for (int i = 0; i < 900; i++)
            {
                guest.AdvanceDeparture(1f / 60f);
                Assert.That(ShopLayout.OccupiesWall(guest.transform.position), Is.False,
                    "wall clip at " + guest.transform.position);
                Assert.That(guest.IsEating, Is.False);
                Assert.That(dining.OccupiedSeats, Is.Zero);
                Vector3 offset = guest.transform.position - waitPoint;
                offset.y = 0f;
                if (offset.magnitude < 0.15f)
                {
                    reachedWait = true;
                    break;
                }
            }
            Assert.That(reachedWait, Is.True);
        }

        [Test]
        public void EnteringCustomersUseTheDoorAndMissTheWalls()
        {
            Material wall = RuntimeMaterials.Create(new Color(0.4f, 0.3f, 0.2f));
            ShopLayout.CreateWalls(root.transform, wall);
            var queue = root.AddComponent<CustomerQueue>();
            queue.OrderQuantityFactory = () => 1;
            queue.Configure(ShopLayout.Entrance, ShopLayout.QueueEntry, ShopLayout.QueueSlots, ShopLayout.Counter, 0.1f, 0f);
            Physics.SyncTransforms();
            bool sawDoor = false;
            for (int i = 0; i < 3600 && queue.ReadyCustomer == null; i++)
            {
                queue.Advance(1f / 60f);
                for (int c = 0; c < queue.Count; c++)
                {
                    Vector3 p = queue.Customers[c].transform.position;
                    Assert.That(ShopLayout.OccupiesWall(p), Is.False, "wall clip at " + p);
                    if (Vector3.Distance(Flat(p), Flat(RestaurantEntrance.Door)) < 1.2f) sawDoor = true;
                }
            }
            Assert.That(queue.ReadyCustomer, Is.Not.Null);
            Assert.That(sawDoor, Is.True);
            var path = (Vector3[])typeof(CustomerQueue).GetField("route",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(queue);
            Assert.That(System.Array.Exists(path,
                p => Vector3.Distance(queue.transform.TransformPoint(p), RestaurantEntrance.Door) < 0.05f));
        }

        [Test]
        public void RankOneBootstrapSealsHrAndBoostDoorsAndLeavesTheEntranceOpen()
        {
            Material wall = RuntimeMaterials.Create(new Color(0.40f, 0.29f, 0.17f));
            Material floor = RuntimeMaterials.Create(new Color(0.72f, 0.70f, 0.62f));
            ShopLayout.CreateFloor(root.transform, floor);
            ShopLayout.CreateWalls(root.transform, wall);
            HrOffice office = HrOffice.Create(root.transform, wall, floor);
            office.SetOpen(false);
            BoostRoom bay = BoostRoom.Create(root.transform, wall, floor);
            bay.SetAnnexOpen(false);
            var goals = root.AddComponent<SessionGoalTracker>();
            goals.Restore(1, 0, 0);
            Physics.SyncTransforms();

            Assert.That(goals.Allows(3), Is.False);
            Assert.That(goals.Allows(5), Is.False);
            Assert.That(root.transform.Find("HrDoorPlug"), Is.Not.Null);
            Assert.That(root.transform.Find("BoostDoorPlug"), Is.Not.Null);
            Assert.That(Physics.CheckBox(ShopLayout.HrDoor + Vector3.up * 0.6f, new Vector3(0.2f, 0.4f, 0.9f)), Is.True);
            Assert.That(Physics.CheckBox(ShopLayout.BoostDoor + Vector3.up * 0.6f, new Vector3(0.9f, 0.4f, 0.2f)),
                Is.True);
            Assert.That(Physics.CheckBox(RestaurantEntrance.Door + Vector3.up * 0.6f, new Vector3(0.25f, 0.4f, 0.12f)),
                Is.False, "main door must stay open");
        }

        [Test]
        public void ClearingAUsedTableCompletesRankTwoAndRanksUp()
        {
            var wallet = root.AddComponent<RestaurantWallet>();
            var goals = root.AddComponent<SessionGoalTracker>();
            goals.Configure(null, null, null, null, wallet, null);
            DiningTable table = DiningTable.Create(root.transform, ShopLayout.Tables[0]);
            var trash = root.AddComponent<TrashInventory>();
            table.BindCollector(trash);
            goals.Restore(2, 0, 0, ShopRanks.StarCap(2) - 2);
            Assert.That(goals.Title, Is.EqualTo("Clear a used dining table"));
            Assert.That(goals.CanUpgrade, Is.False);

            table.LeaveMealTrash(0);
            Assert.That(table.TryPickupTrash(trash), Is.True);
            if (table.TrashCount > 0) table.TryPickupTrash(trash);

            Assert.That(table.TrashCount, Is.Zero);
            Assert.That(goals.Rank, Is.EqualTo(2));
            Assert.That(goals.Stars, Is.EqualTo(ShopRanks.StarCap(2)));
            Assert.That(goals.CanUpgrade, Is.True);
            Assert.That(goals.CapsuleTitle, Is.EqualTo(ShopRanks.RankUpCapsule(2)));
            Assert.That(goals.StarLabel, Does.Contain("Ready"));
            Assert.That(goals.TryUpgradeRank(2), Is.True);
            Assert.That(goals.Rank, Is.EqualTo(3));
            Assert.That(goals.Title, Is.EqualTo(ShopRanks.Goals(3)[0].Title));
            Assert.That(goals.CapsuleTitle, Is.EqualTo(ShopRanks.Goals(3)[0].Title));
            Assert.That(goals.StarLabel, Does.Contain("Need 5 more stars"));
            Assert.That(goals.StarLabel, Does.Not.Contain("Ready"));
        }

        BurgerServingZone ServingRig(int quantity)
        {
            var queue = root.AddComponent<CustomerQueue>();
            queue.OrderQuantityFactory = () => quantity;
            queue.Configure(new Vector3(-8f, 0f, -4f), new Vector3(-2f, 0f, -4f),
                new[] { new Vector3(-2f, 0f, 1.7f), new Vector3(-2f, 0f, -0.1f), new Vector3(-2f, 0f, -1.9f) },
                new Vector3(-2f, 0f, 3.3f));
            GameObject player = new GameObject("Player");
            player.transform.SetParent(root.transform);
            BurgerInventory inventory = player.AddComponent<BurgerInventory>();
            inventory.Configure();
            Transform output = new GameObject("Output").transform;
            output.SetParent(root.transform);
            var grill = root.AddComponent<ProductionStation>();
            grill.Configure(output, null, null);
            var wallet = root.AddComponent<RestaurantWallet>();
            Transform point = new GameObject("ServingPoint").transform;
            point.SetParent(root.transform);
            point.position = new Vector3(0.9f, 0f, 3.3f);
            Transform anchor = new GameObject("StockAnchor").transform;
            anchor.SetParent(root.transform);
            var stock = root.AddComponent<CounterStock>();
            stock.Configure(anchor, null);
            var drop = root.AddComponent<CounterDropZone>();
            drop.Configure(stock, point);
            DiningTable table = DiningTable.Create(root.transform, new Vector3(-5.6f, 0f, 0.2f));
            var serving = root.AddComponent<BurgerServingZone>();
            serving.Configure(queue, inventory, wallet, point,
                new[] { new Vector3(-8.2f, 0f, -4.4f), new Vector3(-10.5f, 0f, -6.2f) }, stock, table, drop);
            return serving;
        }

        static void FillQueue(CustomerQueue queue)
        {
            for (int i = 0; i < 1200; i++) queue.Advance(1f / 60f);
        }

        static void Load(BurgerServingZone serving, int count)
        {
            var grill = serving.GetComponent<ProductionStation>();
            var inventory = serving.GetComponentInChildren<BurgerInventory>();
            grill.Advance(12f);
            for (int i = 0; i < count; i++) inventory.TryCollectFrom(grill);
        }

        static void AtCounter(BurgerServingZone serving)
        {
            serving.GetComponentInChildren<BurgerInventory>().transform.position = serving.ServingPosition + Vector3.up;
        }

        static Vector3 Flat(Vector3 p) => new Vector3(p.x, 0f, p.z);
    }
}
