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
    public sealed class Goal06GameplayTests : SaveIsolatedGameplayTest
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
            // Keep legacy kitchen scenarios independent of the autonomous courier economy.
            Object.FindFirstObjectByType<CourierLine>()?.SetPaused(true);
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
            BurgerServingZone serving = MainKitchen<BurgerServingZone>();
            BurgerPickupZone pickup = MainKitchen<BurgerPickupZone>();
            CustomerQueue queue = MainKitchen<CustomerQueue>();
            queue.OrderQuantityFactory = () => 1;
            RestaurantWallet wallet = Object.FindFirstObjectByType<RestaurantWallet>();
            ProductionStation grill = MainKitchen<ProductionStation>();
            GrillUpgradeZone upgrade = MainKitchen<GrillUpgradeZone>();
            Text sales = GameObject.Find("SalesStatus").GetComponent<Text>();
            Text upgradeText = GameObject.Find("UpgradeStatus").GetComponent<Text>();
            CanvasGroup panel = GameObject.Find("UpgradePanel").GetComponent<CanvasGroup>();

            yield return WalkTo(inventory.transform, new Vector3(2.5f, 0, 12f));
            yield return WalkTo(inventory.transform, upgrade.UpgradePosition);
            yield return WaitSeconds(2f);
            Assert.That(upgrade.Level, Is.EqualTo(1));
            Assert.That(wallet.Coins, Is.Zero);
            Assert.That(upgradeText.text, Does.Contain("Need 30 more coins"));
            Assert.That(panel.alpha, Is.EqualTo(1f));
            yield return WalkTo(inventory.transform, new Vector3(2.5f, 0, 12f));
            yield return WalkTo(inventory.transform, ShopLayout.Aisle);
            Assert.That(panel.alpha, Is.Zero);
            yield return WaitSeconds(16f);

            for (int trip = 1; trip <= 3; trip++)
            {
                yield return CollectAndServe(inventory, pickup, serving, wallet, trip);
                Assert.That(wallet.Coins, Is.EqualTo(trip * 10));
            }
            yield return WalkTo(inventory.transform, new Vector3(2.5f, 0, 12f));
            yield return WalkTo(inventory.transform, upgrade.UpgradePosition);
            float deadline = Time.time + 4f;
            while (upgrade.Level == 1 && Time.time < deadline) yield return null;
            yield return null;
            Assert.That(upgrade.Level, Is.EqualTo(2));
            Assert.That(wallet.Coins, Is.Zero);
            Assert.That(wallet.CompletedSales, Is.EqualTo(3));
            Assert.That(grill.ProductionSeconds, Is.EqualTo(2f));
            Assert.That(sales.text, Is.EqualTo("0"));
            Assert.That(upgradeText.text, Does.Contain("Upgraded!").And.Contain("60 COINS"));
            ExpandableGrill visual = grill.GetComponent<ExpandableGrill>();
            Assert.That(visual, Is.Not.Null);
            Assert.That(visual.ActiveLookName, Is.EqualTo("Look_Lv2"));
            Assert.That(visual.ActiveLook.Find("Lamp_1"), Is.Not.Null);
            Assert.That(visual.ActiveLook.Find("Chimney"), Is.Not.Null);
            Assert.That(grill.Capacity, Is.EqualTo(6));
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

            yield return WalkTo(inventory.transform, new Vector3(2.5f, 0, 12f));
            yield return WalkTo(inventory.transform, ShopLayout.Aisle);
            yield return CollectAndServe(inventory, pickup, serving, wallet, 4);
            Assert.That(wallet.Coins, Is.EqualTo(10), "New earnings remain available after spending the first 30 coins.");
            Assert.That(upgrade.Level, Is.EqualTo(2));
            // Earlier seating may leave the fourth guest waiting for a dirty table.
            // Clear an actual table through player movement before asserting eventual departure.
            var hall = Object.FindFirstObjectByType<DiningArea>();
            var dirty = hall.FindDirtyTable();
            if (dirty != null)
            {
                Vector3 westAisle = new Vector3(-6f, 0, 0);
                Vector3 besideTable = new Vector3(-6f, 0, dirty.Center.z);
                yield return WalkTo(inventory.transform, westAisle);
                yield return WalkTo(inventory.transform, besideTable);
                yield return WalkTo(inventory.transform, dirty.Center + Vector3.right * 0.95f);
                yield return WaitSeconds(2f);
                yield return WalkTo(inventory.transform, besideTable);
                yield return WalkTo(inventory.transform, westAisle);
                yield return WalkTo(inventory.transform, ShopLayout.Aisle);
            }
            yield return WaitSeconds(18f);
            Assert.That(queue.Count, Is.EqualTo(3));
            Assert.That(queue.ReadyCustomer.TicketNumber, Is.EqualTo(5));
            Assert.That(queue.Count, Is.EqualTo(3));
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
            yield return WalkTo(inventory.transform, ShopLayout.Aisle);
            int carried = inventory.Count;
            long coinsBefore = wallet.Coins;
            yield return WalkTo(inventory.transform, serving.ServingPosition);
            deadline = Time.time + 5f;
            while (wallet.CompletedSales < sale && Time.time < deadline) yield return null;
            Assert.That(wallet.CompletedSales, Is.EqualTo(sale));
            Assert.That(inventory.Count, Is.LessThan(carried));
            CashFloor cash = Object.FindFirstObjectByType<CashFloor>();
            yield return WalkTo(inventory.transform, cash.CounterOrigin);
            deadline = Time.time + 5f;
            while (wallet.Coins < coinsBefore + 10 && Time.time < deadline) yield return null;
            Assert.That(wallet.Coins, Is.EqualTo(coinsBefore + 10));
            yield return WalkTo(inventory.transform, ShopLayout.Aisle);
        }

        IEnumerator WalkTo(Transform player, Vector3 target)
        {
            float deadline = Time.time + 12f;
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
