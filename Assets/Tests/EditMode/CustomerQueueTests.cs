using BurgerShop.Customer;
using NUnit.Framework;
using UnityEngine;

namespace BurgerShop.Tests.EditMode
{
    public sealed class CustomerQueueTests
    {
        GameObject root;
        CustomerQueue queue;
        readonly Vector3[] slots = { new Vector3(0f, 0f, 6f), new Vector3(0f, 0f, 4f), new Vector3(0f, 0f, 2f) };

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("QueueTest");
            queue = root.AddComponent<CustomerQueue>();
            Configure();
        }

        void Configure(float interval = 4f) => queue.Configure(new Vector3(-5f, 0f, -2f),
            new Vector3(0f, 0f, -2f), slots, new Vector3(0f, 0f, 8f), interval);

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(root);

        [Test]
        public void SpawnDelayAndPausedTimeDoNotCreateExtraCustomers()
        {
            queue.Advance(0f);
            queue.Advance(-1f);
            queue.Advance(1.49f);
            Assert.That(queue.Count, Is.Zero);
            queue.Advance(0.02f);
            Assert.That(queue.Count, Is.EqualTo(1));
            Vector3 start = queue.FrontCustomer.transform.position;
            queue.Advance(0f);
            Assert.That(queue.FrontCustomer.transform.position, Is.EqualTo(start));
            Assert.That(queue.TryDequeueReadyCustomer(out _), Is.False);
        }

        [Test]
        public void WalkingCustomersReserveCapacityAndOrderOnlyOnArrival()
        {
            queue.Advance(1.5f);
            CustomerAgent first = queue.FrontCustomer;
            Assert.That(first.HasOrdered, Is.False);
            Assert.That(first.transform.Find("OrderBubble").gameObject.activeSelf, Is.False);
            AdvanceSeconds(20f);
            Assert.That(queue.Count, Is.EqualTo(3));
            for (int i = 0; i < 3; i++)
            {
                CustomerAgent customer = queue.Customers[i];
                Assert.That(customer.TicketNumber, Is.EqualTo(i + 1));
                Assert.That(customer.QueueIndex, Is.EqualTo(i));
                Assert.That(customer.OrderSize, Is.EqualTo(1));
                Assert.That(customer.HasReachedSlot && customer.HasOrdered, Is.True);
                Assert.That(Vector3.Distance(customer.transform.position, slots[i]), Is.LessThan(0.001f));
                Assert.That(customer.transform.Find("OrderBubble").gameObject.activeSelf, Is.True);
            }
            queue.Advance(1000f);
            Assert.That(queue.Count, Is.EqualTo(3));
            Assert.That(root.GetComponentsInChildren<CustomerAgent>().Length, Is.EqualTo(3));
        }

        [Test]
        public void AggressiveSpawningMaintainsGapAndArrivalOrderAroundCorner()
        {
            Configure(0.1f);
            for (int step = 0; step < 600; step++)
            {
                queue.Advance(1f / 60f);
                Assert.That(queue.Count, Is.LessThanOrEqualTo(3));
                for (int i = 1; i < queue.Count; i++)
                {
                    CustomerAgent leader = queue.Customers[i - 1];
                    CustomerAgent follower = queue.Customers[i];
                    Assert.That(leader.TicketNumber, Is.LessThan(follower.TicketNumber));
                    Assert.That(leader.DistanceAlongPath - follower.DistanceAlongPath, Is.GreaterThanOrEqualTo(queue.MinimumGap - 0.001f));
                    // A 90-degree bend shortens geometric distance, but bodies must stay clear.
                    Assert.That(Vector3.Distance(leader.transform.position, follower.transform.position), Is.GreaterThan(0.7f));
                }
            }
            Assert.That(queue.Customers[2].HasReachedSlot, Is.True);
        }

        [Test]
        public void LargeFrameDoesNotBurstSpawnOrOvertake()
        {
            queue.Advance(100f);
            Assert.That(queue.Count, Is.EqualTo(1));
            queue.Advance(100f);
            Assert.That(queue.Count, Is.EqualTo(2));
            queue.Advance(100f);
            Assert.That(queue.Count, Is.EqualTo(3));
            queue.Advance(100f);
            for (int i = 0; i < 3; i++)
                Assert.That(queue.Customers[i].transform.position, Is.EqualTo(slots[i]));
        }

        [Test]
        public void ThreeDequeueCyclesAdvanceFollowersAndRefillInFifoOrder()
        {
            AdvanceSeconds(20f);
            for (int ticket = 1; ticket <= 3; ticket++)
            {
                Assert.That(queue.TryDequeueReadyCustomer(out CustomerAgent served), Is.True);
                Assert.That(served.TicketNumber, Is.EqualTo(ticket));
                Assert.That(served.QueueIndex, Is.EqualTo(-1));
                Assert.That(served.transform.Find("OrderBubble").gameObject.activeSelf, Is.False);
                Object.DestroyImmediate(served.gameObject);
                Assert.That(queue.Count, Is.EqualTo(2));
                Assert.That(queue.ReadyCustomer, Is.Null, "Follower must first walk to the counter.");
                Assert.That(queue.TryDequeueReadyCustomer(out _), Is.False);
                AdvanceSeconds(12f);
                Assert.That(queue.Count, Is.EqualTo(3));
                Assert.That(queue.FrontCustomer.TicketNumber, Is.EqualTo(ticket + 1));
                Assert.That(queue.Customers[2].TicketNumber, Is.EqualTo(ticket + 3));
                Assert.That(queue.Customers[2].HasReachedSlot, Is.True);
            }
        }

        [Test]
        public void FullQueueResumesWithFullIntervalInsteadOfStoredTime()
        {
            AdvanceSeconds(20f);
            queue.Advance(1000f);
            queue.TryDequeueReadyCustomer(out CustomerAgent served);
            Object.DestroyImmediate(served.gameObject);
            queue.Advance(3.9f);
            Assert.That(queue.Count, Is.EqualTo(2));
            queue.Advance(0.11f);
            Assert.That(queue.Count, Is.EqualTo(3));
            Assert.That(queue.Customers[2].TicketNumber, Is.EqualTo(4));
            Assert.That(queue.Customers[2].HasOrdered, Is.False);
        }

        [Test]
        public void DestroyedCustomerReleasesReservationAndCompactsQueue()
        {
            AdvanceSeconds(20f);
            Object.DestroyImmediate(queue.Customers[1].gameObject);
            Assert.That(queue.Count, Is.EqualTo(2));
            Assert.That(queue.Customers[1].TicketNumber, Is.EqualTo(3));
            Assert.That(queue.Customers[1].QueueIndex, Is.EqualTo(1));
            AdvanceSeconds(12f);
            Assert.That(queue.Count, Is.EqualTo(3));
            Assert.That(queue.Customers[2].TicketNumber, Is.EqualTo(4));
            Assert.That(queue.Customers[1].transform.position, Is.EqualTo(slots[1]));
        }

        [Test]
        public void DestroyingCustomerReleasesBodyAndOrderIconMaterials()
        {
            queue.Advance(1.5f);
            CustomerAgent first = queue.FrontCustomer;
            Material body = first.transform.Find("Body").GetComponent<Renderer>().sharedMaterial;
            Material icon = first.transform.Find("OrderBubble/BurgerIcon/TopBun").GetComponent<Renderer>().sharedMaterial;
            Object.DestroyImmediate(first.gameObject);
            Assert.That(queue.Count, Is.Zero);
            Assert.That(body == null, Is.True);
            Assert.That(icon == null, Is.True);
        }

        [Test]
        public void InvalidOrOccupiedConfigurationCannotReplaceQueue()
        {
            Assert.Throws<System.ArgumentException>(() => queue.Configure(Vector3.zero, Vector3.zero, new Vector3[0], Vector3.zero));
            Assert.Throws<System.ArgumentException>(() => queue.Configure(Vector3.zero, Vector3.zero, new[] { Vector3.zero, Vector3.right }, Vector3.zero));
            queue.Advance(1.5f);
            Assert.Throws<System.InvalidOperationException>(() => Configure());
            Assert.That(queue.Count, Is.EqualTo(1));
        }

        void AdvanceSeconds(float seconds)
        {
            for (int i = 0; i < Mathf.CeilToInt(seconds * 60); i++) queue.Advance(1f / 60f);
        }
    }
}
