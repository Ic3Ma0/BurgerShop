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
    public sealed class Goal06GameplayTests
    {
        float previousStep;
        InputSettings.BackgroundBehavior previousBackground;
        InputSettings.EditorInputBehaviorInPlayMode previousEditorInput;
        bool inputCaptured;
        Keyboard keyboard;
        Goal03InputDriver input;

        [UnityTest]
        public IEnumerator EarnSpendUpgradeAndKeepServingInRealScene()
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
            ProductionStation grill = Object.FindFirstObjectByType<ProductionStation>();
            GrillUpgradeZone upgrade = Object.FindFirstObjectByType<GrillUpgradeZone>();
            Text sales = GameObject.Find("SalesStatus").GetComponent<Text>();
            Text upgradeText = GameObject.Find("UpgradeStatus").GetComponent<Text>();
            CanvasGroup panel = GameObject.Find("UpgradePanel").GetComponent<CanvasGroup>();

            yield return WalkTo(inventory.transform, upgrade.UpgradePosition);
            yield return WaitSeconds(2f);
            Assert.That(upgrade.Level, Is.EqualTo(1));
            Assert.That(wallet.Coins, Is.Zero);
            Assert.That(upgradeText.text, Does.Contain("Need 30 more coins"));
            Assert.That(panel.alpha, Is.EqualTo(1f));
            yield return WalkTo(inventory.transform, new Vector3(0.9f, 0f, 0.4f));
            Assert.That(panel.alpha, Is.Zero);
            yield return WaitSeconds(16f);

            for (int trip = 1; trip <= 3; trip++)
            {
                yield return CollectAndServe(inventory, pickup, serving, wallet, trip);
                Assert.That(wallet.Coins, Is.EqualTo(trip * 10));
            }
            yield return WalkTo(inventory.transform, upgrade.UpgradePosition);
            float deadline = Time.time + 4f;
            while (upgrade.Level == 1 && Time.time < deadline) yield return null;
            yield return null;
            Assert.That(upgrade.Level, Is.EqualTo(2));
            Assert.That(wallet.Coins, Is.Zero);
            Assert.That(wallet.CompletedSales, Is.EqualTo(3));
            Assert.That(grill.ProductionSeconds, Is.EqualTo(2f));
            Assert.That(sales.text, Does.Contain("-30").And.Not.Contain("+-"));
            Assert.That(upgradeText.text, Does.Contain("Upgraded!").And.Contain("60 COINS"));
            Assert.That(grill.transform.Find("UpgradeLamp_1").gameObject.activeSelf, Is.True);
            Assert.That(grill.transform.Find("UpgradeLamp_2").gameObject.activeSelf, Is.False);
            yield return WaitSeconds(10f);
            Assert.That(upgrade.Level, Is.EqualTo(2));
            Assert.That(wallet.Coins, Is.Zero);

            // Free one slot on a full grill to measure a fresh cycle through Update.
            Assert.That(grill.Stock, Is.EqualTo(grill.Capacity));
            grill.TryTakeBurger();
            float started = Time.time;
            deadline = started + 3f;
            while (grill.Stock < grill.Capacity && Time.time < deadline) yield return null;
            Assert.That(grill.Stock, Is.EqualTo(grill.Capacity));
            Assert.That(Time.time - started, Is.InRange(1.9f, 2.15f));

            yield return WalkTo(inventory.transform, new Vector3(0.9f, 0f, 0.4f));
            yield return CollectAndServe(inventory, pickup, serving, wallet, 4);
            Assert.That(wallet.Coins, Is.EqualTo(10), "New earnings remain available after spending the first 30 coins.");
            Assert.That(upgrade.Level, Is.EqualTo(2));
            yield return WaitSeconds(12f);
            Assert.That(queue.Count, Is.EqualTo(3));
            Assert.That(queue.ReadyCustomer.TicketNumber, Is.EqualTo(5));
            Assert.That(Object.FindObjectsByType<CustomerAgent>(FindObjectsSortMode.None).Length, Is.EqualTo(3));
            LogAssert.NoUnexpectedReceived();
            Restore();
            yield return new ExitPlayMode();
        }

        IEnumerator CollectAndServe(BurgerInventory inventory, BurgerPickupZone pickup,
            BurgerServingZone serving, RestaurantWallet wallet, int sale)
        {
            yield return WalkTo(inventory.transform, pickup.PickupPosition);
            float deadline = Time.time + 5f;
            while (inventory.Count == 0 && Time.time < deadline) yield return null;
            Assert.That(inventory.Count, Is.GreaterThan(0));
            yield return WalkTo(inventory.transform, new Vector3(0.9f, 0f, 0.4f));
            int carried = inventory.Count;
            yield return WalkTo(inventory.transform, serving.ServingPosition);
            deadline = Time.time + 5f;
            while (wallet.CompletedSales < sale && Time.time < deadline) yield return null;
            Assert.That(wallet.CompletedSales, Is.EqualTo(sale));
            Assert.That(inventory.Count, Is.EqualTo(carried - 1));
            yield return WalkTo(inventory.transform, new Vector3(0.9f, 0f, 0.4f));
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
