using BurgerShop.Core;
using BurgerShop.Customer;
using BurgerShop.Restaurant;
using NUnit.Framework;
using UnityEngine;

namespace BurgerShop.Tests.EditMode
{
    public sealed class Spec063CustomerPathTests
    {
        GameObject root;

        [SetUp]
        public void SetUp() => root = new GameObject("Spec063");

        [TearDown]
        public void TearDown()
        {
            ShopLayout.ResetWingLock();
            Object.DestroyImmediate(root);
        }

        [Test]
        public void IndoorRouteFromTheStreetGoesThroughTheDoor()
        {
            Vector3[] route = ShopLayout.IndoorRoute(RestaurantEntrance.Outside, ShopLayout.QueueSlots[0]);
            Assert.That(route, Is.Not.Empty);
            Assert.That(System.Array.Exists(route, p => Vector3.Distance(Flat(p), Flat(RestaurantEntrance.Door)) < 0.2f));
            Assert.That(System.Array.Exists(route, p => Vector3.Distance(Flat(p), Flat(ShopLayout.Entrance)) < 0.2f));
            Assert.That(route[route.Length - 1], Is.EqualTo(ShopLayout.QueueSlots[0]));
            int doors = 0;
            for (int i = 0; i < route.Length; i++)
                if (Vector3.Distance(Flat(route[i]), Flat(RestaurantEntrance.Door)) < 0.2f) doors++;
            Assert.That(doors, Is.EqualTo(1), "street IndoorRoute must not retrace the door");
        }

        [Test]
        public void ExitRouteStartsAtTheDoorCorridor()
        {
            Assert.That(ShopLayout.Exit[0], Is.EqualTo(ShopLayout.Entrance));
            Assert.That(ShopLayout.Exit[1], Is.EqualTo(RestaurantEntrance.Door));
            Vector3[] leaving = ShopLayout.IndoorRoute(ShopLayout.Tables[0], RestaurantEntrance.Outside);
            Assert.That(System.Array.Exists(leaving, p => Vector3.Distance(Flat(p), Flat(RestaurantEntrance.Door)) < 0.2f));
        }

        [Test]
        public void EnteringCustomersStayOnTheDoorCorridorAndMissTheWalls()
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
            Assert.That(Vector3.Distance(queue.transform.TransformPoint(path[0]), RestaurantEntrance.Outside), Is.LessThan(0.05f));
            Assert.That(System.Array.Exists(path,
                p => Vector3.Distance(queue.transform.TransformPoint(p), RestaurantEntrance.Door) < 0.05f));
        }

        static Vector3 Flat(Vector3 p) => new Vector3(p.x, 0f, p.z);
    }
}
