using System.Collections;
using System.Collections.Generic;
using BurgerShop.Customer;
using BurgerShop.Economy;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace BurgerShop.Tests.EditMode
{
    public sealed class Goal05GameplayTests : SaveIsolatedGameplayTest
    {
        float previousStep;
        InputSettings.BackgroundBehavior previousBackground;
        InputSettings.EditorInputBehaviorInPlayMode previousEditorInput;
        bool inputCaptured;
        Keyboard keyboard;
        Goal03InputDriver input;

        [UnityTest]
        public IEnumerator KeyboardCompletesThreePickupServePaymentAndDepartureTrips()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            yield return new EnterPlayMode();
            previousStep = Time.captureDeltaTime;
            Time.captureDeltaTime = 1f / 60f;
            previousBackground = InputSystem.settings.backgroundBehavior;
            previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            inputCaptured = true;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            keyboard = InputSystem.AddDevice<Keyboard>();
            input = new Goal03InputDriver(keyboard);
            yield return null;
            BurgerInventory inventory = Object.FindFirstObjectByType<BurgerInventory>();
            BurgerServingZone serving = Object.FindFirstObjectByType<BurgerServingZone>();
            BurgerPickupZone pickup = Object.FindFirstObjectByType<BurgerPickupZone>();
            CustomerQueue queue = Object.FindFirstObjectByType<CustomerQueue>();
            RestaurantWallet wallet = Object.FindFirstObjectByType<RestaurantWallet>();
            Text sales = GameObject.Find("SalesStatus").GetComponent<Text>();
            Assert.That(wallet.Coins, Is.Zero);
            Assert.That(Object.FindFirstObjectByType<SaleFeedback>().GetComponent<AudioSource>().clip.samples, Is.GreaterThan(0));
            yield return WaitSeconds(16f);
            var servedCustomers = new List<CustomerAgent>();

            for (int trip = 1; trip <= 3; trip++)
            {
                // Stay in the aisle below both counters, then approach the marked spots.
                yield return WalkTo(inventory.transform, new Vector3(0.9f, 0f, 0.4f));
                yield return WalkTo(inventory.transform, pickup.PickupPosition);
                float deadline = Time.time + 5f;
                while (inventory.Count == 0 && Time.time < deadline) yield return null;
                Assert.That(inventory.Count, Is.GreaterThan(0));
                yield return WalkTo(inventory.transform, new Vector3(0.9f, 0f, 0.4f));
                int carried = inventory.Count;
                CustomerAgent customer = queue.ReadyCustomer;
                Assert.That(customer, Is.Not.Null);
                servedCustomers.Add(customer);
                yield return WalkTo(inventory.transform, serving.ServingPosition);
                deadline = Time.time + 5f;
                while (wallet.CompletedSales < trip && Time.time < deadline) yield return null;
                Assert.That(wallet.CompletedSales, Is.EqualTo(trip));
                Assert.That(wallet.Coins, Is.EqualTo(trip * 10));
                Assert.That(inventory.Count, Is.EqualTo(carried - 1));
                Assert.That(customer.IsDeparting, Is.True);
                Assert.That(customer.PaidAmount, Is.EqualTo(10));
                Assert.That(customer.GetComponentInChildren<BurgerVisual>(), Is.Not.Null);
                yield return null;
                Assert.That(sales.text, Does.Contain($"COINS {trip * 10}").And.Contain($"SERVED {trip}"));
                yield return WalkTo(inventory.transform, new Vector3(0.9f, 0f, 0.4f));
            }

            yield return WaitSeconds(12f);
            foreach (CustomerAgent customer in servedCustomers) Assert.That(customer == null, Is.True, "Served customer must reach the exit and be destroyed.");
            Assert.That(wallet.Coins, Is.EqualTo(30));
            Assert.That(wallet.CompletedSales, Is.EqualTo(3));
            Assert.That(queue.Count, Is.EqualTo(3));
            Assert.That(queue.FrontCustomer.TicketNumber, Is.EqualTo(4));
            Assert.That(queue.ReadyCustomer, Is.Not.Null);
            Assert.That(Object.FindObjectsByType<CustomerAgent>(FindObjectsSortMode.None).Length, Is.EqualTo(3));
            LogAssert.NoUnexpectedReceived();
            Restore();
            yield return new ExitPlayMode();
        }

        IEnumerator WalkTo(Transform player, Vector3 target)
        {
            float deadline = Time.time + 8f;
            while (Time.time < deadline)
            {
                Vector3 offset = target - player.position;
                offset.y = 0f;
                if (offset.magnitude < 0.18f) break;
                if (Mathf.Abs(offset.x) > Mathf.Abs(offset.z))
                    input.State = offset.x > 0 ? new KeyboardState(Key.W, Key.D) : new KeyboardState(Key.S, Key.A);
                else
                    input.State = offset.z > 0 ? new KeyboardState(Key.W, Key.A) : new KeyboardState(Key.S, Key.D);
                yield return null;
            }
            input.State = new KeyboardState();
            Vector3 remaining = target - player.position;
            remaining.y = 0f;
            Assert.That(remaining.magnitude, Is.LessThan(0.22f), $"Keyboard route blocked at {player.position}, target {target}");
            yield return null;
        }

        static IEnumerator WaitSeconds(float seconds)
        {
            float deadline = Time.time + seconds;
            while (Time.time < deadline) yield return null;
        }

        void Restore()
        {
            Time.captureDeltaTime = previousStep;
            input?.Dispose();
            input = null;
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            keyboard = null;
            if (inputCaptured)
            {
                InputSystem.settings.backgroundBehavior = previousBackground;
                InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput;
                inputCaptured = false;
            }
        }

        [UnityTearDown]
        public IEnumerator LeavePlayMode()
        {
            if (Application.isPlaying)
            {
                Restore();
                yield return new ExitPlayMode();
            }
        }
    }
}
