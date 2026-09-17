using BurgerShop.Core;
using BurgerShop.Customer;
using BurgerShop.Restaurant;
using NUnit.Framework;
using UnityEngine;

namespace BurgerShop.Tests.EditMode
{
    public sealed class Spec071CustomerEnterTests
    {
        GameObject root;
        const float AxisEps = 0.08f;
        const float RetraceMin = 1f;

        [SetUp]
        public void SetUp() => root = new GameObject("Spec071");

        [TearDown]
        public void TearDown()
        {
            ShopLayout.ResetWingLock();
            Object.DestroyImmediate(root);
        }

        [Test]
        public void OutdoorToIndoorPathIsAxisAligned()
        {
            Vector3[] walk = ShopLayout.Walk(RestaurantEntrance.Outside, ShopLayout.QueueSlots[0]);
            AssertCardinal(Prefix(RestaurantEntrance.Outside, walk));
            Assert.That(System.Array.Exists(walk,
                p => Vector3.Distance(Flat(p), Flat(RestaurantEntrance.Door)) < 0.2f));

            var queue = root.AddComponent<CustomerQueue>();
            queue.OrderQuantityFactory = () => 1;
            queue.Configure(ShopLayout.Entrance, ShopLayout.QueueEntry, ShopLayout.QueueSlots, ShopLayout.Counter, 0.1f, 0f);
            AssertCardinal(QueueWorldPath(queue));
        }

        [Test]
        public void IndoorPathToAQueueSlotDoesNotRetraceTheAisle()
        {
            Vector3[] walk = ShopLayout.Walk(RestaurantEntrance.Outside, ShopLayout.QueueSlots[0]);
            AssertNoAisleRetrace(Prefix(RestaurantEntrance.Outside, walk), AfterDoor(walk));

            var queue = root.AddComponent<CustomerQueue>();
            queue.OrderQuantityFactory = () => 1;
            queue.Configure(ShopLayout.Entrance, ShopLayout.QueueEntry, ShopLayout.QueueSlots, ShopLayout.Counter, 0.1f, 0f);
            Vector3[] path = QueueWorldPath(queue);
            int door = AfterDoor(path);
            AssertNoAisleRetrace(path, door);
            int doors = 0;
            for (int i = 0; i < path.Length; i++)
                if (Vector3.Distance(Flat(path[i]), Flat(RestaurantEntrance.Door)) < 0.2f) doors++;
            Assert.That(doors, Is.EqualTo(1), "Arrival plus Walk must not replay the door corridor");
        }

        [Test]
        public void EnteringPathMissesTheCounterVolume()
        {
            Vector3[] walk = ShopLayout.Walk(RestaurantEntrance.Outside, ShopLayout.QueueSlots[0]);
            AssertMissesCounter(Prefix(RestaurantEntrance.Outside, walk));

            var queue = root.AddComponent<CustomerQueue>();
            queue.OrderQuantityFactory = () => 1;
            queue.Configure(ShopLayout.Entrance, ShopLayout.QueueEntry, ShopLayout.QueueSlots, ShopLayout.Counter, 0.1f, 0f);
            AssertMissesCounter(QueueWorldPath(queue));
        }

        [Test]
        public void ConcatenatingArrivalWithAStreetWalkDoesNotDoubleTheDoor()
        {
            var points = new System.Collections.Generic.List<Vector3>(RestaurantEntrance.Arrival);
            ShopLayout.ConcatWalk(points, ShopLayout.IndoorRoute(RestaurantEntrance.Outside, ShopLayout.QueueSlots[0]));
            int doors = 0;
            for (int i = 0; i < points.Count; i++)
                if (Vector3.Distance(Flat(points[i]), Flat(RestaurantEntrance.Door)) < 0.2f) doors++;
            Assert.That(doors, Is.EqualTo(1));
            AssertCardinal(points.ToArray());
            AssertNoAisleRetrace(points.ToArray(), AfterDoor(points.ToArray()));
        }

        static Vector3[] QueueWorldPath(CustomerQueue queue)
        {
            var local = (Vector3[])typeof(CustomerQueue).GetField("route",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(queue);
            var world = new Vector3[local.Length];
            for (int i = 0; i < local.Length; i++)
                world[i] = queue.transform.TransformPoint(local[i]);
            return world;
        }

        static Vector3[] Prefix(Vector3 from, Vector3[] walk)
        {
            if (walk == null || walk.Length == 0) return new[] { from };
            if (Vector3.Distance(Flat(from), Flat(walk[0])) < 0.05f) return walk;
            var all = new Vector3[walk.Length + 1];
            all[0] = from;
            System.Array.Copy(walk, 0, all, 1, walk.Length);
            return all;
        }

        static int AfterDoor(Vector3[] path)
        {
            for (int i = 0; i < path.Length; i++)
                if (Vector3.Distance(Flat(path[i]), Flat(RestaurantEntrance.Door)) < 0.35f)
                    return i;
            return 0;
        }

        static void AssertCardinal(Vector3[] path)
        {
            Assert.That(path, Is.Not.Null.And.Not.Empty);
            for (int i = 1; i < path.Length; i++)
            {
                float dx = Mathf.Abs(path[i].x - path[i - 1].x);
                float dz = Mathf.Abs(path[i].z - path[i - 1].z);
                Assert.That(dx < AxisEps || dz < AxisEps,
                    "diagonal " + path[i - 1] + " -> " + path[i]);
            }
        }

        static void AssertNoAisleRetrace(Vector3[] path, int start)
        {
            start = Mathf.Clamp(start, 0, Mathf.Max(0, path.Length - 1));
            for (int i = start; i < path.Length - 2; i++)
            {
                Vector3 a = path[i], b = path[i + 1], c = path[i + 2];
                bool sameZ = Mathf.Abs(a.z - b.z) < AxisEps && Mathf.Abs(b.z - c.z) < AxisEps;
                bool sameX = Mathf.Abs(a.x - b.x) < AxisEps && Mathf.Abs(b.x - c.x) < AxisEps;
                float ab = sameZ ? b.x - a.x : sameX ? b.z - a.z : 0f;
                float bc = sameZ ? c.x - b.x : sameX ? c.z - b.z : 0f;
                if (!(sameX || sameZ) || ab * bc >= 0f) continue;
                Assert.That(Mathf.Abs(ab) < RetraceMin || Mathf.Abs(bc) < RetraceMin,
                    "aisle retrace " + a + " -> " + b + " -> " + c);
            }
        }

        static void AssertMissesCounter(Vector3[] path)
        {
            for (int i = 1; i < path.Length; i++)
            {
                Vector3 a = path[i - 1], b = path[i];
                for (int s = 1; s <= 12; s++)
                {
                    float t = s / 13f;
                    Vector3 p = Vector3.Lerp(a, b, t);
                    bool desk = p.x > -4.8f && p.x < -1.2f && p.z > 3.2f && p.z < 4.8f;
                    bool extra = p.x > -4.8f && p.x < -1.2f && p.z > 7.2f && p.z < 8.8f;
                    Assert.That(desk || extra, Is.False, "counter clip at " + p);
                }
            }
        }

        static Vector3 Flat(Vector3 p) => new Vector3(p.x, 0f, p.z);
    }
}
