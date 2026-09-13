using System;
using System.Collections;
using System.IO;
using System.Text;
using BurgerShop.Customer;
using BurgerShop.Economy;
using BurgerShop.Persistence;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace BurgerShop.Tests.EditMode
{
    public sealed class Spec025GameplayTests : SaveIsolatedGameplayTest
    {
        Keyboard keyboard;
        Goal03InputDriver input;
        InputSettings.BackgroundBehavior previousBackground;
        InputSettings.EditorInputBehaviorInPlayMode previousEditorInput;

        [UnityTest]
        public IEnumerator OldBoxingPurchaseGetsTheNewTableAndKeyboardRouteReachesPackAndWindow()
        {
            var old = new RestaurantSaveData
            {
                version = 6, coins = 80, completedSales = 18, grillLevel = 3, workerHired = true,
                workerDeliveries = 7, hiredWorkerCount = 1, workerClears = 3,
                staffSpeedTier = 1, playerSpeedTier = 2, playerCarryTier = 1,
                boughtBoxingStation = true, boughtExtraTable = true, boughtExtraGrill = true, extraGrillLevel = 2
            };
            Directory.CreateDirectory(SaveDirectory);
            File.WriteAllText(new LocalSaveStore(SaveDirectory).FilePath, "{\"data\":" + JsonUtility.ToJson(old)
                + ",\"checksum\":\"mCEYqNRMQFrbW31D0JfUEDdbAN5FCkLh/qx+eWmdLfI=\"}");
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            yield return new EnterPlayMode();
            // Keep legacy kitchen scenarios independent of the autonomous courier economy.
            Object.FindFirstObjectByType<CourierLine>()?.SetPaused(true);
            Time.captureDeltaTime = 1f / 60;
            var player = Object.FindFirstObjectByType<PlayerMotor>();
            var inventory = player.GetComponent<BurgerInventory>();
            var crew = Object.FindFirstObjectByType<WorkerHiringZone>(); crew.Worker.enabled = false;
            var expansion = Object.FindFirstObjectByType<ShopExpansion>();
            var wallet = Object.FindFirstObjectByType<RestaurantWallet>();
            var box = expansion.Boxing;
            Assert.That(box, Is.Not.Null); Assert.That(wallet.Coins, Is.EqualTo(80));
            Assert.That(crew.TotalDeliveries, Is.EqualTo(7)); Assert.That(crew.TotalClears, Is.EqualTo(3));
            Assert.That(player.BoostLevel, Is.EqualTo(2));
            foreach (var upgrade in Object.FindObjectsByType<GrillUpgradeZone>(FindObjectsSortMode.None))
                Assert.That(upgrade.Level, Is.EqualTo(upgrade.Product == KitchenProduct.Cola ? 1 : ShopLayout.Horizontal(upgrade.UpgradePosition, ShopLayout.UpgradeSpot) < 0.1f ? 3 : 2));
            Assert.That(expansion.HasExtraGrill && expansion.HasExtraTable, Is.True);
            StartInput();
            yield return WalkTo(player.transform, ShopLayout.GrillPickup);
            yield return WaitSeconds(5f);
            int quantity = inventory.Count;
            Assert.That(quantity, Is.GreaterThanOrEqualTo(2));
            yield return WalkTo(player.transform, ShopLayout.Aisle);
            yield return WalkTo(player.transform, new Vector3(-8, 0, 0));
            yield return WalkTo(player.transform, box.CirclePosition);
            GameplayEvidence.Capture("spec025-table-processing-editor.png");
            yield return WaitSeconds(4f);
            Assert.That(inventory.BoxedCount, Is.EqualTo(quantity));
            Assert.That(box.TotalProcessed, Is.EqualTo(quantity)); Assert.That(box.TableCount, Is.Zero);
            GameplayEvidence.Capture("spec025-boxes-collected-editor.png");
            // Reach PACK around the west ends of both desks using actual PlayerMotor collision/movement.
            yield return WalkTo(player.transform, new Vector3(-11.3f, 0, -7.2f));
            yield return WalkTo(player.transform, box.DropPosition);
            yield return WaitSeconds(2f);
            Assert.That(inventory.Count, Is.Zero); Assert.That(box.PackageCount, Is.EqualTo(quantity));
            Assert.That(wallet.CompletedSales, Is.EqualTo(18));
            GameplayEvidence.Capture("spec025-pack-stock-editor.png");
            expansion.Restore(true, true, false, 2, true, true);
            var lane = expansion.DriveThru; lane.OrderQuantityFactory = () => 2;
            yield return WaitSeconds(8f);
            Assert.That(lane.CompletedOrders, Is.Zero);
            var order = lane.WaitingOrder; Assert.That(order.Quantity, Is.EqualTo(2));
            yield return WalkTo(player.transform, new Vector3(-11.3f, 0, -13.3f));
            yield return WalkTo(player.transform, lane.WindowPosition);
            float deadline = Time.time + 3f;
            while (!order.IsSettled && Time.time < deadline) yield return null;
            Assert.That(order.IsSettled, Is.True); Assert.That(lane.CompletedOrders, Is.EqualTo(1));
            Assert.That(lane.DeliveredUnits, Is.EqualTo(2)); Assert.That(box.PackageCount, Is.EqualTo(quantity - 2));
            Assert.That(wallet.Coins, Is.EqualTo(80)); Assert.That(wallet.CompletedSales, Is.EqualTo(19));
            GameplayEvidence.Capture("spec025-window-complete-editor.png");
            Assert.That(Object.FindFirstObjectByType<RestaurantPersistence>().Flush(), Is.True);
            StopInput();
            yield return new ExitPlayMode();
            yield return new EnterPlayMode();
            // Keep legacy kitchen scenarios independent of the autonomous courier economy.
            Object.FindFirstObjectByType<CourierLine>()?.SetPaused(true);
            Time.captureDeltaTime = 1f / 60;
            crew = Object.FindFirstObjectByType<WorkerHiringZone>(); crew.Worker.enabled = false;
            expansion = Object.FindFirstObjectByType<ShopExpansion>(); box = expansion.Boxing;
            player = Object.FindFirstObjectByType<PlayerMotor>();
            wallet = Object.FindFirstObjectByType<RestaurantWallet>();
            Assert.That(wallet.Coins, Is.EqualTo(80)); Assert.That(wallet.CompletedSales, Is.EqualTo(19));
            Assert.That(crew.TotalDeliveries, Is.EqualTo(7)); Assert.That(crew.TotalClears, Is.EqualTo(3));
            Assert.That(expansion.HasBoxing && expansion.HasDriveThru && expansion.HasExtraGrill && expansion.HasExtraTable, Is.True);
            Assert.That(player.BoostLevel, Is.EqualTo(2));
            Assert.That(box.TableCount + box.PackageCount + player.GetComponent<BurgerInventory>().Count, Is.Zero);
            Assert.That(box.TotalProcessed, Is.Zero);
            LogAssert.NoUnexpectedReceived();
            Time.captureDeltaTime = 0;
            yield return new ExitPlayMode();
        }

        [UnityTest] public IEnumerator OneEmployeeRunsBothLinesInSampleSceneFor120Seconds() => CrewRun(1);
        [UnityTest] public IEnumerator ThreeEmployeesRunBothLinesInSampleSceneFor120Seconds() => CrewRun(3);
        IEnumerator CrewRun(int count)
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            yield return new EnterPlayMode();
            // Keep legacy kitchen scenarios independent of the autonomous courier economy.
            Object.FindFirstObjectByType<CourierLine>()?.SetPaused(true);
            Time.captureDeltaTime = 1f / 60;
            var queue = MainKitchen<CustomerQueue>(); queue.OrderQuantityFactory = () => 3;
            var serving = MainKitchen<BurgerServingZone>();
            var expansion = Object.FindFirstObjectByType<ShopExpansion>();
            expansion.Restore(false, false, false, 0, true, true);
            var lane = expansion.DriveThru; lane.OrderQuantityFactory = () => 2;
            var grill = MainKitchen<ProductionStation>();
            // Adequate source supply is an explicit AC-05 setup, not a release balance change.
            grill.SetCapacity(8); grill.SetProductionSeconds(0.1f);
            yield return WaitSeconds(12f);
            Assert.That(queue.ReadyCustomer, Is.Not.Null); Assert.That(lane.HasStoppedCarAtWindow, Is.True);
            Assert.That(serving.TotalStock + expansion.Boxing.TableCount + expansion.Boxing.PackageCount, Is.Zero);
            var crew = Object.FindFirstObjectByType<WorkerHiringZone>(); crew.RestoreWorkers(count, 0, 0);
            var trace = new StringBuilder("seconds,staff,state,job,line,reservedRaw,raw,box,x,z\n");
            string[] last = new string[count]; float start = Time.time;
            while (Time.time - start < 120f)
            {
                yield return null;
                for (int i = 0; i < count; i++)
                {
                    var w = crew.Workers[i];
                    string state = $"{i},{w.State},{w.Job},{w.SupplyTarget},{w.ReservedRaw},{w.Inventory.LooseCount},{w.Inventory.BoxedCount}";
                    if (state != last[i])
                    {
                        trace.AppendLine(F(Time.time - start) + "," + state + "," + F(w.transform.position.x) + "," + F(w.transform.position.z));
                        last[i] = state;
                    }
                    Assert.That(ShopLayout.ContainsPlayable(w.transform.position), Is.True);
                    Assert.That(w.Inventory.Count, Is.LessThanOrEqualTo(w.Inventory.Capacity));
                }
            }
            var box = expansion.Boxing;
            string prefix = Path.GetFullPath($"Logs/spec023-025/spec025-play-crew-{count}");
            Directory.CreateDirectory(Path.GetDirectoryName(prefix));
            File.WriteAllText(prefix + "-trace.csv", trace.ToString());
            File.WriteAllText(prefix + "-summary.json", JsonUtility.ToJson(new Summary {
                staff = count, seconds = Time.time - start, dining = serving.CompletedOrders, drive = lane.CompletedOrders,
                diningStock = serving.TotalStock, raw = box.InputCount, processing = box.ProcessingCount,
                output = box.OutputCount, pack = box.PackageCount, deliveries = crew.TotalDeliveries, clears = crew.TotalClears
            }, true));
            Assert.That(serving.CompletedOrders, Is.GreaterThan(0)); Assert.That(lane.CompletedOrders, Is.GreaterThan(0));
            Assert.That(crew.TotalDeliveries, Is.EqualTo(serving.CompletedOrders + lane.CompletedOrders + (crew.ColaServing != null ? crew.ColaServing.CompletedOrders : 0)));
            var player = Object.FindFirstObjectByType<PlayerMotor>(); player.enabled = false;
            player.transform.position = ShopLayout.BoxingCircle + Vector3.up;
            Camera.main.GetComponent<CameraFollow>().Snap();
            GameplayEvidence.Capture($"spec025-play-crew-{count}-editor.png");
            LogAssert.NoUnexpectedReceived();
            Time.captureDeltaTime = 0;
            yield return new ExitPlayMode();
        }
        [Serializable] sealed class Summary
        {
            public int staff, dining, drive, diningStock, raw, processing, output, pack, deliveries, clears;
            public float seconds;
        }
        static string F(float value) => value.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        static IEnumerator WaitSeconds(float seconds) { float until = Time.time + seconds; while (Time.time < until) yield return null; }
        void StartInput()
        {
            previousBackground = InputSystem.settings.backgroundBehavior;
            previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            keyboard = InputSystem.AddDevice<Keyboard>(); input = new Goal03InputDriver(keyboard);
        }
        void StopInput()
        {
            if (input == null) return;
            input.Dispose(); input = null;
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            keyboard = null;
            InputSystem.settings.backgroundBehavior = previousBackground;
            InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput;
        }
        IEnumerator WalkTo(Transform player, Vector3 target)
        {
            float deadline = Time.time + 12f;
            while (Time.time < deadline)
            {
                Vector3 offset = target - player.position; offset.y = 0;
                if (offset.magnitude < 0.18f) break;
                if (Mathf.Abs(offset.x) > Mathf.Abs(offset.z))
                    input.State = offset.x > 0 ? new KeyboardState(Key.W, Key.D) : new KeyboardState(Key.S, Key.A);
                else input.State = offset.z > 0 ? new KeyboardState(Key.W, Key.A) : new KeyboardState(Key.S, Key.D);
                yield return null;
            }
            input.State = new KeyboardState();
            Assert.That(ShopLayout.Horizontal(player.position, target), Is.LessThan(0.22f), $"Keyboard route blocked at {player.position}, target {target}");
            yield return null;
        }
        [UnityTearDown] public IEnumerator LeavePlayMode()
        {
            if (Application.isPlaying) { StopInput(); Time.captureDeltaTime = 0; yield return new ExitPlayMode(); }
        }
    }
}
