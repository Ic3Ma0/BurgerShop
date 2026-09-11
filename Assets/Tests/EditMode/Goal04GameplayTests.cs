using System.Collections;
using BurgerShop.Customer;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace BurgerShop.Tests.EditMode
{
    public sealed class Goal04GameplayTests : SaveIsolatedGameplayTest
    {
        float previousCaptureDeltaTime;

        [UnityTest]
        public IEnumerator SampleSceneSpawnsOrdersAndRefillsQueueThroughUpdate()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            yield return new EnterPlayMode();
            previousCaptureDeltaTime = Time.captureDeltaTime;
            Time.captureDeltaTime = 1f / 60f;
            yield return null;
            CustomerQueue queue = Object.FindFirstObjectByType<CustomerQueue>();
            Assert.That(queue, Is.Not.Null);
            Assert.That(queue.Capacity, Is.EqualTo(3));
            Text hud = GameObject.Find("CustomerStatus").GetComponent<Text>();
            Assert.That(hud.text, Does.Contain("0/3"));
            Assert.That(queue.TryDequeueReadyCustomer(out _), Is.False);

            yield return WaitGameSeconds(16f);
            AssertWaitingQueue(queue, 1);
            Assert.That(hud.text, Does.Contain("3/3").And.Contain("1 BURGER"));
            yield return WaitGameSeconds(8f);
            Assert.That(Object.FindObjectsByType<CustomerAgent>(FindObjectsSortMode.None).Length, Is.EqualTo(3));

            // Exercise the Goal 05 handoff API, without simulating payment or delivery.
            for (int ticket = 1; ticket <= 3; ticket++)
            {
                Assert.That(queue.TryDequeueReadyCustomer(out CustomerAgent served), Is.True);
                Assert.That(served.TicketNumber, Is.EqualTo(ticket));
                Object.Destroy(served.gameObject);
                Assert.That(queue.ReadyCustomer, Is.Null);
                yield return null;
                Assert.That(hud.text, Does.Contain("2/3").And.Contain("Walking"));
                yield return WaitGameSeconds(10f);
                AssertWaitingQueue(queue, ticket + 1);
            }
            LogAssert.NoUnexpectedReceived();
            Time.captureDeltaTime = previousCaptureDeltaTime;
            yield return new ExitPlayMode();
        }

        static void AssertWaitingQueue(CustomerQueue queue, int firstTicket)
        {
            Assert.That(queue.Count, Is.EqualTo(3));
            Assert.That(queue.ReadyCustomer, Is.SameAs(queue.FrontCustomer));
            for (int i = 0; i < 3; i++)
            {
                CustomerAgent customer = queue.Customers[i];
                Assert.That(customer.TicketNumber, Is.EqualTo(firstTicket + i));
                Assert.That(customer.HasOrdered && customer.HasReachedSlot, Is.True);
                Assert.That(customer.OrderSize, Is.EqualTo(1));
                Transform bubble = customer.transform.Find("OrderBubble");
                Assert.That(bubble.gameObject.activeSelf, Is.True);
                Assert.That(Quaternion.Angle(bubble.rotation, Camera.main.transform.rotation), Is.LessThan(0.01f));
                Assert.That(customer.GetComponentsInChildren<Collider>(), Is.Empty);
            }
        }

        static IEnumerator WaitGameSeconds(float seconds)
        {
            float deadline = Time.time + seconds;
            while (Time.time < deadline) yield return null;
        }

        [UnityTearDown]
        public IEnumerator LeavePlayMode()
        {
            if (Application.isPlaying)
            {
                Time.captureDeltaTime = previousCaptureDeltaTime;
                yield return new ExitPlayMode();
            }
        }
    }
}
