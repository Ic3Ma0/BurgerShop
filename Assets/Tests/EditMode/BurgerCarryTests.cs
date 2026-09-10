using BurgerShop.Player;
using BurgerShop.Restaurant;
using NUnit.Framework;
using UnityEngine;

namespace BurgerShop.Tests.EditMode
{
    public sealed class BurgerCarryTests
    {
        GameObject root;
        ProductionStation station;
        BurgerInventory inventory;
        BurgerPickupZone pickup;
        Transform output;
        Transform stack;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("CarryTest");
            output = new GameObject("Output").transform;
            output.SetParent(root.transform);
            station = root.AddComponent<ProductionStation>();
            station.Configure(output, null, null, 1f, 8);

            GameObject player = new GameObject("Player");
            player.transform.SetParent(root.transform);
            inventory = player.AddComponent<BurgerInventory>();
            inventory.Configure();
            stack = player.transform.Find("CarryStack");
            pickup = root.AddComponent<BurgerPickupZone>();
            pickup.Configure(station, inventory, root.transform);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(root);

        [Test]
        public void EmptySourceCannotCreateCarriedStock()
        {
            Assert.That(inventory.TryCollectFrom(station), Is.False);
            Assert.That(inventory.Count, Is.Zero);
            Assert.That(stack.childCount, Is.Zero);
            Assert.That(inventory.TryTakeBurger(), Is.False);
        }

        [Test]
        public void PickupTransfersExistingVisualAndConservesStock()
        {
            station.Advance(3f);
            Transform original = output.GetChild(2);
            Material material = original.GetComponentInChildren<Renderer>().sharedMaterial;
            int notification = -1;
            inventory.CountChanged += count => notification = count;

            Assert.That(inventory.TryCollectFrom(station), Is.True);
            Assert.That(station.Stock + inventory.Count, Is.EqualTo(3));
            Assert.That(output.childCount, Is.EqualTo(2));
            Assert.That(stack.GetChild(0), Is.SameAs(original));
            Assert.That(original.GetComponentInChildren<Renderer>().sharedMaterial, Is.SameAs(material));
            Assert.That(notification, Is.EqualTo(1));
        }

        [Test]
        public void FullInventoryLeavesSourceStockUntouched()
        {
            station.Advance(8f);
            for (int i = 0; i < inventory.Capacity; i++)
                Assert.That(inventory.TryCollectFrom(station), Is.True);
            Assert.That(inventory.TryCollectFrom(station), Is.False);
            Assert.That(inventory.IsFull, Is.True);
            Assert.That(station.Stock, Is.EqualTo(4));
            Assert.That(output.childCount, Is.EqualTo(4));
            Assert.That(stack.childCount, Is.EqualTo(4));
        }

        [Test]
        public void TakingCarriedStockFreesCapacityAndRestacksNextPickup()
        {
            station.Advance(8f);
            for (int i = 0; i < 4; i++) inventory.TryCollectFrom(station);
            Assert.That(inventory.TryTakeBurger(), Is.True);
            Assert.That(inventory.TryTakeBurger(), Is.True);
            Assert.That(inventory.Count, Is.EqualTo(2));
            Assert.That(stack.childCount, Is.EqualTo(2));
            Assert.That(inventory.TryCollectFrom(station), Is.True);
            Assert.That(stack.GetChild(2).localPosition.y, Is.EqualTo(0.68f).Within(0.001f));
        }

        [Test]
        public void PickupUsesMarkedPointAndStopsWhenPlayerLeaves()
        {
            Transform point = new GameObject("PickupPoint").transform;
            point.SetParent(root.transform);
            point.localPosition = new Vector3(4f, 0f, 0f);
            pickup.Configure(station, inventory, point);
            station.Advance(4f);
            pickup.Advance(1f);
            Assert.That(inventory.Count, Is.Zero);

            inventory.transform.position = new Vector3(4f, 1f, 0f);
            pickup.Advance(0.01f);
            Assert.That(inventory.Count, Is.EqualTo(1));
            inventory.transform.position = Vector3.zero;
            pickup.Advance(10f);
            Assert.That(inventory.Count, Is.EqualTo(1));
            Assert.That(station.Stock, Is.EqualTo(3));
        }

        [Test]
        public void PickupRespectsIntervalAndDoesNotBurstAfterHitch()
        {
            station.Advance(8f);
            pickup.Advance(0.01f);
            pickup.Advance(0.1f);
            Assert.That(inventory.Count, Is.EqualTo(1));
            pickup.Advance(0.16f);
            Assert.That(inventory.Count, Is.EqualTo(2));
            pickup.Advance(10f);
            Assert.That(inventory.Count, Is.EqualTo(3));
        }

        [Test]
        public void DisabledSourceAndPausedTimeCannotTransferStock()
        {
            station.Advance(4f);
            station.enabled = false;
            pickup.Advance(1f);
            Assert.That(inventory.Count, Is.Zero);
            station.enabled = true;
            pickup.Advance(0f);
            pickup.Advance(-1f);
            Assert.That(inventory.Count, Is.Zero);
        }

        [Test]
        public void CarriedStackFollowsPlayerPositionAndRotation()
        {
            station.Advance(4f);
            for (int i = 0; i < 4; i++) inventory.TryCollectFrom(station);
            inventory.transform.position = new Vector3(2f, 1f, -3f);
            inventory.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            for (int i = 0; i < 4; i++)
            {
                Transform burger = stack.GetChild(i);
                Assert.That(Vector3.Distance(burger.position,
                    stack.TransformPoint(new Vector3(0f, i * 0.34f, 0f))), Is.LessThan(0.001f));
                Assert.That(Quaternion.Angle(burger.rotation, inventory.transform.rotation), Is.LessThan(0.001f));
                Assert.That(burger.GetComponentsInChildren<Collider>(), Is.Empty);
            }
        }

        [Test]
        public void BurgerMaterialsSurviveTransferAndAreReleasedWithBurger()
        {
            station.Advance(1f);
            Material material = output.GetChild(0).GetComponentInChildren<Renderer>().sharedMaterial;
            inventory.TryCollectFrom(station);
            Assert.That(material != null, Is.True);
            inventory.TryTakeBurger();
            Assert.That(material == null, Is.True);
        }
    }
}
