using System.Collections;
using System.IO;
using BurgerShop.Economy;
using BurgerShop.Persistence;
using BurgerShop.Player;
using BurgerShop.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace BurgerShop.Tests.EditMode
{
    public sealed class Goal09GameplayTests : SaveIsolatedGameplayTest
    {
        Touchscreen touchscreen;
        float previousStep;
        InputSettings.BackgroundBehavior previousBackground;
        InputSettings.EditorInputBehaviorInPlayMode previousEditorInput;
        bool captured;

        [UnityTest]
        public IEnumerator TouchMovesPlayerIgnoresOtherFingerAndReleasesOnBackground()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            yield return new EnterPlayMode();
            previousStep = Time.captureDeltaTime;
            Time.captureDeltaTime = 1f / 60f;
            previousBackground = InputSystem.settings.backgroundBehavior;
            previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            captured = true;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            touchscreen = InputSystem.AddDevice<Touchscreen>();
            yield return null;
            Canvas.ForceUpdateCanvases();
            var joystick = Object.FindFirstObjectByType<VirtualJoystick>();
            var player = Object.FindFirstObjectByType<PlayerMotor>();
            var pad = (RectTransform)joystick.transform;
            Vector2 center = RectTransformUtility.WorldToScreenPoint(null, pad.position);
            Vector2 right = RectTransformUtility.WorldToScreenPoint(null, pad.TransformPoint(new Vector3(90, 0, 0)));
            Vector2 left = RectTransformUtility.WorldToScreenPoint(null, pad.TransformPoint(new Vector3(-90, 0, 0)));
            Vector3 origin = player.transform.position;
            Touch(1, TouchPhase.Began, center); yield return null; yield return null;
            Touch(1, TouchPhase.Moved, right); yield return null; yield return null;
            Assert.That(VirtualJoystick.Value.x, Is.GreaterThan(0.5f));
            for (int frame = 0; frame < 30; frame++) yield return null;
            Assert.That(Vector3.Distance(origin, player.transform.position), Is.GreaterThan(1f));
            Touch(2, TouchPhase.Began, left); yield return null; yield return null;
            Assert.That(VirtualJoystick.Value.x, Is.GreaterThan(0.5f));
            Touch(2, TouchPhase.Ended, left); yield return null; yield return null;
            Assert.That(VirtualJoystick.Value.x, Is.GreaterThan(0.5f));
            // Simulate the lifecycle message delivered by Android when the app is backgrounded.
            joystick.SendMessage("OnApplicationPause", true);
            Assert.That(VirtualJoystick.Value, Is.EqualTo(Vector2.zero));
            Touch(1, TouchPhase.Ended, right); yield return null; yield return null;
            origin = player.transform.position;
            for (int frame = 0; frame < 30; frame++) yield return null;
            Vector3 displacement = player.transform.position - origin; displacement.y = 0;
            Assert.That(displacement.magnitude, Is.LessThan(0.01f));
            Touch(3, TouchPhase.Began, center); yield return null; yield return null;
            Touch(3, TouchPhase.Moved, left); yield return null; yield return null;
            Assert.That(VirtualJoystick.Value.x, Is.LessThan(-0.5f));
            Touch(3, TouchPhase.Ended, left); yield return null; yield return null;
            Assert.That(VirtualJoystick.Value, Is.EqualTo(Vector2.zero));
            var wallet = Object.FindFirstObjectByType<RestaurantWallet>();
            var persistence = Object.FindFirstObjectByType<RestaurantPersistence>();
            wallet.RecordSale(10);
            persistence.SendMessage("OnApplicationPause", true);
            Assert.That(new LocalSaveStore(Path.GetDirectoryName(persistence.FilePath)).Load(out var data), Is.EqualTo(SaveLoadResult.Loaded));
            Assert.That(data.coins, Is.EqualTo(10));
            LogAssert.NoUnexpectedReceived();
            Restore();
            yield return new ExitPlayMode();
        }

        void Touch(int id, TouchPhase phase, Vector2 position) => InputSystem.QueueStateEvent(touchscreen,
            new TouchState { touchId = id, phase = phase, position = position });

        void Restore()
        {
            Time.captureDeltaTime = previousStep;
            if (touchscreen != null && touchscreen.added) InputSystem.RemoveDevice(touchscreen);
            touchscreen = null;
            if (captured)
            {
                InputSystem.settings.backgroundBehavior = previousBackground;
                InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput;
                captured = false;
            }
        }
        [UnityTearDown] public IEnumerator LeavePlayMode()
        {
            if (Application.isPlaying) { Restore(); yield return new ExitPlayMode(); }
        }
    }
}
