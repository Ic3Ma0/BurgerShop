using BurgerShop.Persistence;
using UnityEditor;
using System.IO;
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
    public sealed class Goal08GameplayTests : SaveIsolatedGameplayTest
    {
        float previousStep;
        InputSettings.BackgroundBehavior previousBackground;
        InputSettings.EditorInputBehaviorInPlayMode previousEditorInput;
        bool inputCaptured;
        Keyboard keyboard;
        Goal03InputDriver input;

        [UnityTest]
        public IEnumerator EarnHireUpgradeThenRestartAndContinueFromSavedProgress()
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
            var inventory = Object.FindFirstObjectByType<PlayerMotor>().GetComponent<BurgerInventory>();
            var serving = MainKitchen<BurgerServingZone>();
            var pickup = MainKitchen<BurgerPickupZone>();
            var wallet = Object.FindFirstObjectByType<RestaurantWallet>();
            var hiring = Object.FindFirstObjectByType<WorkerHiringZone>();
            var upgrade = MainKitchen<GrillUpgradeZone>();
            var persistence = Object.FindFirstObjectByType<RestaurantPersistence>();
            Assert.That(persistence.LoadResult, Is.EqualTo(SaveLoadResult.NewGame));
            yield return WaitSeconds(16f);
            for (int trip = 1; trip <= 5; trip++)
                yield return CollectAndServe(inventory, pickup, serving, wallet, trip);
            yield return WalkTo(inventory.transform, hiring.HiringPosition);
            float deadline = Time.time + 4f;
            while (!hiring.IsHired && Time.time < deadline) yield return null;
            Assert.That(hiring.IsHired, Is.True);
            yield return WalkTo(inventory.transform, Vector3.zero);
            // Include repeated trips to clean tables and dispose of trash.
            deadline = Time.time + 150f;
            while ((hiring.Worker == null || hiring.Worker.CompletedDeliveries < 4) && Time.time < deadline)
                yield return null;
            CashFloor cash = Object.FindFirstObjectByType<CashFloor>();
            yield return WalkTo(inventory.transform, cash.CounterOrigin);
            while (wallet.Coins < 30 && Time.time < deadline) yield return null;
            Assert.That(hiring.Worker, Is.Not.Null);
            Assert.That(hiring.Worker.CompletedDeliveries, Is.GreaterThanOrEqualTo(4), $"state={hiring.Worker.State}, job={hiring.Worker.Job}, position={hiring.Worker.transform.position}, bag={hiring.Worker.Inventory.Count}, clears={hiring.Worker.CompletedClears}");
            Assert.That(wallet.Coins, Is.GreaterThanOrEqualTo(30));
            yield return WalkTo(inventory.transform, new Vector3(2.5f, 0, 12f));
            yield return WalkTo(inventory.transform, upgrade.UpgradePosition);
            deadline = Time.time + 4f;
            while (upgrade.Level < 2 && Time.time < deadline) yield return null;
            Assert.That(upgrade.Level, Is.EqualTo(2));
            // Pause the employee so the expected snapshot stays unchanged during editor shutdown.
            hiring.Worker.enabled = false;
            // Autosave uses unscaled wall time, independent of the test's captured simulation step.
            float savedBy = Time.realtimeSinceStartup + 2.2f;
            while (Time.realtimeSinceStartup < savedBy) yield return null;
            Assert.That(persistence.Status, Is.EqualTo("PROGRESS SAVED"));
            Assert.That(new LocalSaveStore(SaveDirectory).Load(out var expected), Is.EqualTo(SaveLoadResult.Loaded));
            Assert.That(expected.coins, Is.EqualTo(wallet.Coins));
            Assert.That(expected.grillLevel, Is.EqualTo(2));
            Assert.That(expected.workerHired, Is.True);
            Assert.That(expected.workerDeliveries, Is.GreaterThanOrEqualTo(4));
            SessionState.SetString("BurgerShop.Tests.ExpectedProgress", JsonUtility.ToJson(expected));
            Restore();
            yield return new ExitPlayMode();

            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            yield return new EnterPlayMode();
            // Keep legacy kitchen scenarios independent of the autonomous courier economy.
            Object.FindFirstObjectByType<CourierLine>()?.SetPaused(true);
            GameObject.Find("ColaCustomerArea")?.SetActive(false);
            MainKitchen<BurgerShop.Customer.CustomerQueue>().OrderQuantityFactory = () => 1;
            previousStep = Time.captureDeltaTime;
            Time.captureDeltaTime = 1f / 60f;
            yield return null;
            expected = JsonUtility.FromJson<RestaurantSaveData>(SessionState.GetString("BurgerShop.Tests.ExpectedProgress", ""));
            wallet = Object.FindFirstObjectByType<RestaurantWallet>();
            hiring = Object.FindFirstObjectByType<WorkerHiringZone>();
            upgrade = MainKitchen<GrillUpgradeZone>();
            persistence = Object.FindFirstObjectByType<RestaurantPersistence>();
            Assert.That(persistence.LoadResult, Is.EqualTo(SaveLoadResult.Loaded));
            Assert.That(wallet.Coins, Is.EqualTo(expected.coins), "Restoring staff and upgrades must not charge again.");
            Assert.That(wallet.CompletedSales, Is.EqualTo(expected.completedSales));
            Assert.That(upgrade.Level, Is.EqualTo(2));
            Assert.That(MainKitchen<ProductionStation>().ProductionSeconds, Is.EqualTo(2f));
            Assert.That(hiring.IsHired, Is.True);
            Assert.That(hiring.Worker.CompletedDeliveries, Is.EqualTo(expected.workerDeliveries));
            Assert.That(Object.FindObjectsByType<RestaurantWorker>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            deadline = Time.time + 45f;
            while (hiring.Worker.CompletedDeliveries <= expected.workerDeliveries && Time.time < deadline) yield return null;
            Assert.That(hiring.Worker.CompletedDeliveries, Is.GreaterThan(expected.workerDeliveries));
            Assert.That(wallet.Coins, Is.EqualTo(expected.coins), "Uncollected worker cash must not auto-credit.");
            Assert.That(wallet.CompletedSales, Is.GreaterThan(expected.completedSales));
            Assert.That(persistence.Flush(), Is.True);
            Assert.That(new LocalSaveStore(SaveDirectory).Load(out var continued), Is.EqualTo(SaveLoadResult.Loaded));
            Assert.That(continued.coins, Is.EqualTo(wallet.Coins));
            Assert.That(File.Exists(persistence.FilePath + ".bak"), Is.True);
            LogAssert.NoUnexpectedReceived();
            Restore();
            yield return new ExitPlayMode();
            SessionState.EraseString("BurgerShop.Tests.ExpectedProgress");
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
            CashFloor floor = Object.FindFirstObjectByType<CashFloor>();
            yield return WalkTo(inventory.transform, floor.CounterOrigin);
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
