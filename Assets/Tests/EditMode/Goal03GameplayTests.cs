using System.Collections;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using BurgerShop.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace BurgerShop.Tests.EditMode
{
    public sealed class Goal03GameplayTests
    {
        InputSettings.BackgroundBehavior previousBackgroundBehavior;
        InputSettings.EditorInputBehaviorInPlayMode previousEditorInputBehavior;
        bool settingsCaptured;
        Keyboard testKeyboard;
        Goal03InputDriver inputDriver;
        float previousCaptureDeltaTime;

        [UnityTest]
        public IEnumerator SampleSceneProducesPicksUpAndCarriesWithKeyboardAndJoystick()
        {
            // Run on the real entry scene, including its runtime bootstrap.
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            yield return new EnterPlayMode();
            yield return null;

            // Batch Mode runs uncapped. Use a real gameplay timestep so movement is
            // above CharacterController.minMoveDistance even on a very fast host.
            previousCaptureDeltaTime = Time.captureDeltaTime;
            Time.captureDeltaTime = 1f / 60f;

            // Keep the singleton settings object alive across Play Mode teardown.
            // Replacing it with a temporary clone leaves the editor reload hook
            // referencing a destroyed object between consecutive scene tests.
            previousBackgroundBehavior = InputSystem.settings.backgroundBehavior;
            previousEditorInputBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            settingsCaptured = true;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            testKeyboard = InputSystem.AddDevice<Keyboard>();

            ProductionStation station = Object.FindFirstObjectByType<ProductionStation>();
            BurgerInventory inventory = Object.FindFirstObjectByType<BurgerInventory>();
            BurgerPickupZone pickup = Object.FindFirstObjectByType<BurgerPickupZone>();
            Assert.That(station, Is.Not.Null);
            Assert.That(inventory, Is.Not.Null);
            Assert.That(pickup, Is.Not.Null);
            Transform output = station.transform.Find("BurgerOutput");
            Transform stack = inventory.transform.Find("CarryStack");
            Text hud = GameObject.Find("CarryStatus").GetComponent<Text>();
            inputDriver = new Goal03InputDriver(testKeyboard);

            float deadline = Time.time + 4f;
            while (station.Stock == 0 && Time.time < deadline) yield return null;
            Assert.That(station.Stock, Is.GreaterThanOrEqualTo(1), "Grill must produce through Update.");
            Assert.That(inventory.Count, Is.Zero, "No pickup away from the marked spot.");
            station.Advance(12f);
            Assert.That(station.Stock, Is.EqualTo(4));

            Vector3 start = inventory.transform.position;
            deadline = Time.time + 2f;
            while (!pickup.IsInRange && Time.time < deadline)
            {
                inputDriver.State = new KeyboardState(Key.W, Key.D);
                yield return null;
            }
            inputDriver.State = new KeyboardState();
            yield return null;
            Assert.That(pickup.IsInRange, Is.True, $"Keyboard must reach pickup. Start={start}, end={inventory.transform.position}, target={pickup.PickupPosition}");
            Assert.That(Vector3.Distance(start, inventory.transform.position), Is.GreaterThan(4f));

            deadline = Time.time + 2f;
            while (!inventory.IsFull && Time.time < deadline) yield return null;
            Assert.That(inventory.Count, Is.EqualTo(4));
            Assert.That(station.Stock, Is.Zero);
            Assert.That(output.childCount, Is.Zero);
            Assert.That(stack.childCount, Is.EqualTo(4));
            yield return null;
            Assert.That(hud.text, Does.Contain("4/4").And.Contain("FULL"));
            Assert.That(stack.GetComponentsInChildren<Collider>(), Is.Empty);

            station.Advance(12f);
            yield return WaitGameSeconds(0.4f);
            Assert.That(station.Stock, Is.EqualTo(4), "Full player must not consume grill stock.");
            Assert.That(output.childCount, Is.EqualTo(4));

            // Exercise deferred destruction: two removals in one frame must remove two visuals.
            pickup.enabled = false;
            inventory.TryTakeBurger();
            inventory.TryTakeBurger();
            station.TryTakeBurger();
            station.TryTakeBurger();
            yield return null;
            Assert.That(inventory.Count, Is.EqualTo(2));
            Assert.That(stack.childCount, Is.EqualTo(2));
            Assert.That(station.Stock, Is.EqualTo(2));
            Assert.That(output.childCount, Is.EqualTo(2));

            pickup.enabled = true;
            deadline = Time.time + 1f;
            while (!inventory.IsFull && Time.time < deadline) yield return null;
            Assert.That(inventory.IsFull, Is.True, "Freeing capacity must resume automatic pickup.");
            Assert.That(stack.childCount, Is.EqualTo(4));

            start = inventory.transform.position;
            deadline = Time.time + 0.7f;
            while (Time.time < deadline)
            {
                inputDriver.State = new KeyboardState(Key.A, Key.S);
                yield return null;
            }
            inputDriver.State = new KeyboardState();
            yield return null;
            Assert.That(Vector3.Distance(start, inventory.transform.position), Is.GreaterThan(3f), "Loaded player must keep moving.");
            Assert.That(pickup.IsInRange, Is.False);

            VirtualJoystick joystick = Object.FindFirstObjectByType<VirtualJoystick>();
            RectTransform pad = (RectTransform)joystick.transform;
            Canvas canvas = joystick.GetComponentInParent<Canvas>();
            Vector2 center = RectTransformUtility.WorldToScreenPoint(null, pad.position);
            PointerEventData pointer = new PointerEventData(EventSystem.current)
            {
                position = center + Vector2.left * 110f * canvas.scaleFactor
            };
            start = inventory.transform.position;
            joystick.OnPointerDown(pointer);
            yield return WaitGameSeconds(0.35f);
            joystick.OnPointerUp(pointer);
            Assert.That(Vector3.Distance(start, inventory.transform.position), Is.GreaterThan(1f));
            yield return WaitGameSeconds(0.25f);
            start = inventory.transform.position;
            yield return WaitGameSeconds(0.5f);
            Assert.That(Vector3.Distance(start, inventory.transform.position), Is.LessThan(0.02f), "Releasing input stops the player.");
            Assert.That(inventory.Count, Is.EqualTo(4));
            Assert.That(stack.childCount, Is.EqualTo(4));
            Assert.That(Vector3.Distance(stack.position, inventory.transform.TransformPoint(new Vector3(0f, 0.1f, 0.8f))), Is.LessThan(0.001f));

            Vector3 cameraOffset = Quaternion.Euler(0f, 45f, 0f) * Vector3.back * 13f;
            cameraOffset.y = 11f;
            Assert.That(Vector3.Distance(Camera.main.transform.position, inventory.transform.position + cameraOffset), Is.LessThan(0.05f));
            LogAssert.NoUnexpectedReceived();
            RestoreInput();
            yield return new ExitPlayMode();
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
                RestoreInput();
                yield return new ExitPlayMode();
            }
        }

        void RestoreInput()
        {
            Time.captureDeltaTime = previousCaptureDeltaTime;
            if (inputDriver != null)
                inputDriver.Dispose();
            inputDriver = null;
            if (testKeyboard != null && testKeyboard.added)
                InputSystem.RemoveDevice(testKeyboard);
            testKeyboard = null;
            if (settingsCaptured)
            {
                InputSystem.settings.backgroundBehavior = previousBackgroundBehavior;
                InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInputBehavior;
                settingsCaptured = false;
            }
        }
    }
}
