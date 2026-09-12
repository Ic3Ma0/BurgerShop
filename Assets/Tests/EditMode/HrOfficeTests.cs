using BurgerShop.Core;
using BurgerShop.Customer;
using BurgerShop.Economy;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using NUnit.Framework;
using UnityEngine;

namespace BurgerShop.Tests.EditMode
{
    public sealed class HrOfficeTests
    {
        GameObject root;
        HrOffice office;
        WorkerHiringZone hiring;
        RestaurantWallet wallet;
        BurgerInventory player;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("HrOfficeTest");
            Material wall = RuntimeMaterials.Create(new Color(0.45f, 0.32f, 0.18f));
            Material floor = RuntimeMaterials.Create(new Color(0.76f, 0.62f, 0.42f));
            ShopLayout.CreateFloor(root.transform, floor);
            ShopLayout.CreateWalls(root.transform, wall);
            office = HrOffice.Create(root.transform, wall, floor);

            var grill = root.AddComponent<ProductionStation>();
            Transform output = new GameObject("Output").transform;
            output.SetParent(root.transform);
            grill.Configure(output, null, null);
            var queue = root.AddComponent<CustomerQueue>();
            queue.OrderQuantityFactory = () => 1;
            queue.Configure(ShopLayout.Entrance, ShopLayout.QueueEntry, ShopLayout.QueueSlots, ShopLayout.Counter);
            wallet = root.AddComponent<RestaurantWallet>();
            GameObject carrier = new GameObject("Player");
            carrier.transform.SetParent(root.transform);
            player = carrier.AddComponent<BurgerInventory>();
            player.Configure();
            Transform servingPoint = Point("ServingPoint", ShopLayout.ServingCircle);
            Transform pickupPoint = Point("PickupPoint", ShopLayout.Grill + new Vector3(1.05f, 0f, -2.1f));
            Transform anchor = Point("StockAnchor", ShopLayout.CounterTop);
            var stock = root.AddComponent<CounterStock>();
            stock.Configure(anchor, null);
            var drop = root.AddComponent<CounterDropZone>();
            drop.Configure(stock, servingPoint);
            var cashier = root.AddComponent<BurgerServingZone>();
            cashier.Configure(queue, player, wallet, servingPoint, ShopLayout.Exit, stock, (DiningArea)null, drop);
            hiring = root.AddComponent<WorkerHiringZone>();
            hiring.Configure(grill, cashier, wallet, player, pickupPoint, office.HirePoint, ShopLayout.Aisle, drop,
                office.HireLabel);
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

        [Test]
        public void ShopHasHrRoomAndNoCyanHiringCylinder()
        {
            Assert.That(GameObject.Find("StaffHiringSpot"), Is.Null);
            Assert.That(root.transform.Find("StaffHiringSpot"), Is.Null);
            Assert.That(office.transform.Find("HrWall+X"), Is.Not.Null);
            Assert.That(office.transform.Find("HrWall+Z"), Is.Not.Null);
            Assert.That(office.transform.Find("HrWall-Z"), Is.Not.Null);
            Assert.That(office.transform.Find("Desk"), Is.Not.Null);
            Assert.That(office.transform.Find("Chair"), Is.Not.Null);
            Assert.That(Vector3.Distance(office.HirePoint.position, ShopLayout.HrHirePoint), Is.LessThan(0.001f));
            Assert.That(office.HireLabel.text, Does.Contain("Hire staff").IgnoreCase);
            Assert.That(office.HireLabel.text, Does.Not.Contain("Kingshot"));
            Assert.That(office.HireLabel.text, Does.Not.Contain("Doughnut"));
            Assert.That(GameObject.Find("HrHallSign").GetComponent<TextMesh>().text, Is.EqualTo("HR"));
        }

        [Test]
        public void DoorwayIsOpenAndRoomSitsOutsideTheHall()
        {
            Physics.SyncTransforms();
            Assert.That(Physics.CheckBox(ShopLayout.HrDoor + Vector3.up * 0.6f, new Vector3(0.2f, 0.4f, 0.9f)), Is.False);
            Assert.That(ShopLayout.HrRoomCenter.x, Is.GreaterThan(ShopLayout.WallHalf));
            Assert.That(ShopLayout.HrDesk.x, Is.GreaterThan(ShopLayout.WallHalf));
            Assert.That(Horizontal(ShopLayout.HrDesk, ShopLayout.Grill), Is.GreaterThan(8f));
            Assert.That(Horizontal(ShopLayout.HrDesk, ShopLayout.Counter), Is.GreaterThan(8f));
            Assert.That(Horizontal(ShopLayout.HrDesk, ShopLayout.Tables[0]), Is.GreaterThan(8f));
            Assert.That(ShopLayout.ContainsHrOffice(ShopLayout.HrHirePoint), Is.True);
            Assert.That(ShopLayout.ContainsHrOffice(ShopLayout.HiringSpot), Is.False);
        }

        [Test]
        public void ChairFacesTheDeskWithTheBackOutside()
        {
            Transform chair = office.transform.Find("Chair");
            Vector3 towardDesk = Flatten(ShopLayout.HrDesk - chair.position);
            Vector3 forward = Flatten(chair.forward);
            Assert.That(Vector3.Dot(forward.normalized, towardDesk.normalized), Is.GreaterThan(0.9f));
            Transform back = chair.Find("Back");
            Assert.That(Vector3.Distance(Flatten(back.position), Flatten(ShopLayout.HrDesk)),
                Is.GreaterThan(Vector3.Distance(Flatten(chair.position), Flatten(ShopLayout.HrDesk))));
        }

        [Test]
        public void HallCircleCannotHireAndDeskHoldHiresOneWorker()
        {
            wallet.RestoreProgress(50, 0);
            player.transform.position = ShopLayout.HiringSpot + Vector3.up;
            Hold(3f);
            Assert.That(hiring.IsHired, Is.False);
            Assert.That(hiring.IsInRange, Is.False);
            Assert.That(wallet.Coins, Is.EqualTo(50));
            Assert.That(root.GetComponentsInChildren<RestaurantWorker>(), Is.Empty);

            player.transform.position = ShopLayout.HrHirePoint + Vector3.up;
            Hold(1.5f);
            Assert.That(hiring.IsHired, Is.True);
            Assert.That(hiring.HiredCount, Is.EqualTo(1));
            Assert.That(wallet.Coins, Is.Zero);
            Assert.That(hiring.HasVisitedOffice, Is.True);
            Assert.That(hiring.Worker, Is.Not.Null);
            Assert.That(hiring.Worker.gameObject.activeInHierarchy, Is.True);
            Assert.That(hiring.Worker.transform.position.x, Is.LessThan(ShopLayout.WallHalf));
            Assert.That(ShopLayout.ContainsHrOffice(hiring.Worker.transform.position), Is.False);
        }

        static float Horizontal(Vector3 a, Vector3 b)
        {
            a.y = b.y = 0f;
            return Vector3.Distance(a, b);
        }

        static Vector3 Flatten(Vector3 value)
        {
            value.y = 0f;
            return value;
        }
    }
}
