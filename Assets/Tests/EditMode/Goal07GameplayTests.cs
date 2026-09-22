using System.Collections;
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
    public sealed class Goal07GameplayTests : SaveIsolatedGameplayTest
    {
        float previousStep;
        InputSettings.BackgroundBehavior previousBackground;
        InputSettings.EditorInputBehaviorInPlayMode previousEditorInput;
        bool inputCaptured;
        Keyboard keyboard;
        Goal03InputDriver input;

        [UnityTest]
        public IEnumerator EarnHireAndShareWorkWithAnAutonomousEmployeeInRealScene()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            yield return new EnterPlayMode();
            // This scenario tests facilities after their rank unlock.
            Object.FindFirstObjectByType<BurgerShop.UI.SessionGoalTracker>().Restore(3,0,0);
            // Keep legacy kitchen scenarios independent of the autonomous courier economy.
            Object.FindFirstObjectByType<CourierLine>()?.SetPaused(true);
            GameObject.Find("ColaCustomerArea")?.SetActive(false);
            MainKitchen<BurgerShop.Customer.CustomerQueue>().OrderQuantityFactory = () => 1;
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
            BurgerInventory inventory = Object.FindFirstObjectByType<PlayerMotor>().GetComponent<BurgerInventory>();
            BurgerServingZone serving = MainKitchen<BurgerServingZone>();
            BurgerPickupZone pickup = MainKitchen<BurgerPickupZone>();
            RestaurantWallet wallet = Object.FindFirstObjectByType<RestaurantWallet>();
            WorkerHiringZone hiring = Object.FindFirstObjectByType<WorkerHiringZone>();
            Assert.That(GameObject.Find("StaffHiringSpot"), Is.Null);
            Assert.That(Object.FindFirstObjectByType<HrOffice>(), Is.Not.Null);
            Text status = GameObject.Find("StaffStatus").GetComponent<Text>();
            Text detail = GameObject.Find("HiringStatus").GetComponent<Text>();
            CanvasGroup panel = GameObject.Find("HiringPanel").GetComponent<CanvasGroup>();

            Assert.That(Object.FindObjectsByType<RestaurantWorker>(FindObjectsSortMode.None), Is.Empty);
            yield return WalkTo(inventory.transform, hiring.HiringPosition);
            yield return WaitSeconds(2f);
            Assert.That(hiring.IsHired, Is.False);
            Assert.That(wallet.Coins, Is.Zero);
            Assert.That(detail.text, Does.Contain("还差 50 金币"));
            Assert.That(panel.alpha, Is.EqualTo(1f));
            yield return WalkTo(inventory.transform, ShopLayout.Aisle);
            yield return WaitSeconds(16f);
            for (int trip = 1; trip <= 5; trip++)
                yield return CollectAndServe(inventory, pickup, serving, wallet, trip);
            Assert.That(wallet.Coins, Is.EqualTo(50));
            yield return WalkTo(inventory.transform, hiring.HiringPosition);
            float deadline = Time.time + 4f;
            while (!hiring.IsHired && Time.time < deadline) yield return null;
            yield return null;
            Assert.That(hiring.IsHired, Is.True);
            Assert.That(wallet.Coins, Is.Zero);
            Assert.That(wallet.CompletedSales, Is.EqualTo(5));
            Assert.That(Object.FindObjectsByType<RestaurantWorker>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            RestaurantWorker worker = hiring.Worker;
            Assert.That(worker.Inventory.Capacity, Is.EqualTo(2));
            yield return WalkTo(inventory.transform, Vector3.zero);
            yield return null;
            Assert.That(panel.alpha, Is.Zero);
            Vector3 parkedPlayer = inventory.transform.position;
            Vector3 workerStart = worker.transform.position;
            bool workerMoved = false;
            // Include repeated trips to clean tables and dispose of trash.
            deadline = Time.time + 240f;
            while (worker.CompletedDeliveries < 6 && Time.time < deadline)
            {
                workerMoved |= Vector3.Distance(worker.transform.position, workerStart) > 2f;
                Assert.That(worker.Inventory.Count, Is.InRange(0, 2));
                yield return null;
            }
            Assert.That(worker.CompletedDeliveries, Is.GreaterThanOrEqualTo(6), $"state={worker.State}, job={worker.Job}, position={worker.transform.position}, bag={worker.Inventory.Count}, clears={worker.CompletedClears}");
            Assert.That(workerMoved, Is.True);
            Vector3 playerMovement = inventory.transform.position - parkedPlayer;
            playerMovement.y = 0f;
            Assert.That(playerMovement.magnitude, Is.LessThan(0.01f));
            Assert.That(wallet.CompletedSales, Is.EqualTo(5 + worker.CompletedDeliveries));
            Assert.That(wallet.Coins, Is.Zero, "Parked player must not receive worker-dropped cash.");
            yield return null;
            Assert.That(status.text, Does.Contain("已送达 " + worker.CompletedDeliveries));

            // Resume manual work while the employee is still using the same stock and cashier.
            yield return WalkTo(inventory.transform, ShopLayout.Aisle);
            yield return WalkTo(inventory.transform, pickup.PickupPosition);
            deadline = Time.time + 10f;
            while (inventory.Count == 0 && Time.time < deadline) yield return null;
            Assert.That(inventory.Count, Is.GreaterThan(0));
            yield return WalkTo(inventory.transform, ShopLayout.Aisle);
            yield return WalkTo(inventory.transform, serving.ServingPosition);
            deadline = Time.time + 15f;
            while (wallet.CompletedSales - worker.CompletedDeliveries <= 5 && Time.time < deadline) yield return null;
            Assert.That(wallet.CompletedSales - worker.CompletedDeliveries, Is.GreaterThan(5));
            Assert.That(wallet.Coins, Is.GreaterThanOrEqualTo(0));
            Assert.That(hiring.IsHired, Is.True);
            Assert.That(Object.FindObjectsByType<RestaurantWorker>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
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
            // After the initial three guests, allow a new customer to walk in from the entrance.
            deadline = Time.time + 15f;
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
