using System.Collections;
using System.Linq;
using BurgerShop.Building;
using BurgerShop.Economy;
using BurgerShop.Persistence;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using BurgerShop.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BurgerShop.Tests.EditMode
{
    public sealed class Spec084CharacterUpgradeTests : SaveIsolatedGameplayTest
    {
        Mouse mouse;
        Touchscreen touch;
        InputSettings.BackgroundBehavior background;
        InputSettings.EditorInputBehaviorInPlayMode editorInput;
        bool inputCaptured;
        void SetupInput()
        {
            background = InputSystem.settings.backgroundBehavior;
            editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            inputCaptured = true;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            mouse = InputSystem.AddDevice<Mouse>();
            touch = InputSystem.AddDevice<Touchscreen>();
        }
        static Vector2 Frame(Transform actor)
        {
            var camera = Camera.main;
            camera.GetComponent<CameraFollow>().enabled = false;
            camera.transform.position = actor.position + new Vector3(-7, 9, -9);
            camera.transform.LookAt(actor.position);
            Physics.SyncTransforms();
            return camera.WorldToScreenPoint(actor.position);
        }
        IEnumerator Click(Vector2 point)
        {
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(MouseButton.Left));
            yield return null; yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
            yield return null; yield return null;
        }
        void Touch(int id, UnityEngine.InputSystem.TouchPhase phase, Vector2 point) =>
            InputSystem.QueueStateEvent(touch, new TouchState { touchId = id, phase = phase, position = point });

        [UnityTest]
        public IEnumerator MouseSelectsPlayerAndStaffAtAnyLocationAndPersistsSharedUpgrades()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity"); yield return new EnterPlayMode();
            yield return UpgradeActors();
            CleanupInput(); yield return new ExitPlayMode();
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity"); yield return new EnterPlayMode();
            Assert.That(Object.FindFirstObjectByType<BoostUpgradeZone>().SpeedTier, Is.EqualTo(1));
            Assert.That(Object.FindFirstObjectByType<StaffUpgradeBoard>().CarryTier, Is.EqualTo(1));
            Assert.That(Object.FindFirstObjectByType<PlayerUpgradeHud>().IsVisible, Is.False);
            LogAssert.NoUnexpectedReceived(); yield return new ExitPlayMode();
        }

        IEnumerator UpgradeActors()
        {
            SetupInput();
            var player = Object.FindFirstObjectByType<PlayerMotor>();
            var playerHud = Object.FindFirstObjectByType<PlayerUpgradeHud>();
            var staffHud = Object.FindFirstObjectByType<StaffUpgradeHud>();
            var boost = Object.FindFirstObjectByType<BoostUpgradeZone>();
            var staff = Object.FindFirstObjectByType<StaffUpgradeBoard>();
            var wallet = Object.FindFirstObjectByType<RestaurantWallet>();
            var goals = Object.FindFirstObjectByType<SessionGoalTracker>();
            goals.Restore(3, 0, 0);
            Object.FindFirstObjectByType<MainHallExpansion>().Restore(true, true, MainHallExpansion.Cost);
            wallet.RestoreProgress(1000, 0);
            var hiring = Object.FindFirstObjectByType<WorkerHiringZone>(); hiring.RestoreWorkers(3, 0);
            foreach (var worker in hiring.Workers) worker.enabled = false;
            player.enabled = false;
            player.transform.position = ShopLayout.BoostPoint + Vector3.up;
            yield return null;
            Assert.That(playerHud.IsVisible, Is.False);
            player.transform.position = ShopLayout.HrHirePoint + Vector3.up;
            yield return null;
            Assert.That(staffHud.IsVisible, Is.False);
            player.transform.position = new Vector3(0, 1, -8);
            var point = Frame(player.transform); yield return null;
            var selection = Object.FindFirstObjectByType<WorldDetailsInput>();
            Assert.That(selection.TargetAt(point), Is.SameAs(player));
            Time.timeScale = .5f;
            yield return Click(point);
            Assert.That(playerHud.IsVisible, Is.True);
            Assert.That(staffHud.IsVisible, Is.False);
            Assert.That(FacilityDetailsHud.Current.IsOpen, Is.False);
            Assert.That(Time.timeScale, Is.Zero);
            bool reentered = true;
            System.Action<int> reentryProbe = _ => reentered = boost.TryBuySpeed();
            wallet.CoinsSpent += reentryProbe;
            playerHud.ClickSpeed(); playerHud.ClickSpeed();
            Assert.That(boost.SpeedTier, Is.EqualTo(1));
            Assert.That(wallet.Coins, Is.EqualTo(950));
            Assert.That(goals.Stars, Is.EqualTo(2));
            Assert.That(reentered, Is.False);
            Assert.That(boost.TryBuySpeed(0), Is.False, "A stale tier must not charge the next price");
            playerHud.ClickClose();
            Assert.That(Time.timeScale, Is.EqualTo(.5f));
            Assert.That(Camera.main.GetComponent<CameraFollow>().enabled, Is.False);
            playerHud.ClickCarry(); Assert.That(boost.CarryTier, Is.Zero, "Hidden cards cannot purchase");
            // Remove the reentry probe before testing a separate owner.
            wallet.CoinsSpent -= reentryProbe;
            wallet.RestoreProgress(950, 0);
            var picked = hiring.Workers[1]; picked.transform.position = new Vector3(3, 1.05f, -8);
            point = Frame(picked.transform); yield return null;
            Assert.That(selection.TargetAt(point), Is.SameAs(picked));
            Assert.That(picked.GetComponentsInChildren<Collider>(), Is.Empty, "Selection must not add navigation obstacles");
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(MouseButton.Left));
            yield return null; yield return null;
            picked.transform.position += Vector3.right * 1.2f; // Target keeps moving during a short press.
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
            yield return null; yield return null;
            Assert.That(staffHud.IsVisible, Is.True);
            Assert.That(staffHud.Popup.Subtitle.text, Does.Contain("all hired staff"));
            staffHud.ClickCarry(); staffHud.ClickCarry();
            Assert.That(staff.CarryTier, Is.EqualTo(1));
            foreach (var worker in hiring.Workers) Assert.That(worker.Inventory.Capacity, Is.EqualTo(StaffBoost.CarryCapacity(1)));
            Assert.That(goals.Stars, Is.EqualTo(4));
            // Switching existing cards restores pause ownership before capturing it again.
            var layout = Object.FindFirstObjectByType<FacilityLayout>();
            Assert.That(FacilityDetailsHud.Current.Open(layout.Instances.First(f => f.Id == "grill-main")), Is.True);
            Assert.That(staffHud.IsVisible, Is.False);
            FacilityDetailsHud.Current.Close(); Assert.That(Time.timeScale, Is.EqualTo(.5f));
            Assert.That(playerHud.Open(), Is.True); playerHud.enabled = false;
            Assert.That(Time.timeScale, Is.EqualTo(.5f));
            Assert.That(Object.FindFirstObjectByType<RestaurantPersistence>().Flush(), Is.True);
            Assert.That(new LocalSaveStore(SaveDirectory).Load(out var saved), Is.EqualTo(SaveLoadResult.Loaded));
            Assert.That(saved.playerSpeedTier, Is.EqualTo(1)); Assert.That(saved.staffCarryTier, Is.EqualTo(1));
            Time.timeScale = 1;
            yield return null;
        }

        [UnityTest]
        public IEnumerator TouchDragUiMultitouchAndPlacementNeverBecomeActorTaps()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity"); yield return new EnterPlayMode();
            SetupInput();
            var player = Object.FindFirstObjectByType<PlayerMotor>(); player.enabled = false;
            player.transform.position = new Vector3(0, 1, -8);
            var hud = Object.FindFirstObjectByType<PlayerUpgradeHud>();
            var selection = Object.FindFirstObjectByType<WorldDetailsInput>();
            var point = Frame(player.transform); yield return null;
            Assert.That(selection.TargetAt(point), Is.SameAs(player));
            Touch(1, UnityEngine.InputSystem.TouchPhase.Began, point); yield return null; yield return null;
            Touch(1, UnityEngine.InputSystem.TouchPhase.Moved, point + Vector2.right * 90); yield return null; yield return null;
            Touch(1, UnityEngine.InputSystem.TouchPhase.Ended, point); yield return null; yield return null;
            Assert.That(hud.IsVisible, Is.False);
            Touch(2, UnityEngine.InputSystem.TouchPhase.Began, point); yield return null; yield return null;
            Touch(3, UnityEngine.InputSystem.TouchPhase.Began, point + Vector2.right * 30); yield return null; yield return null;
            Touch(3, UnityEngine.InputSystem.TouchPhase.Ended, point); yield return null; yield return null;
            Touch(2, UnityEngine.InputSystem.TouchPhase.Ended, point); yield return null; yield return null;
            Assert.That(hud.IsVisible, Is.False);
            var joystick = Object.FindFirstObjectByType<VirtualJoystick>();
            var center = RectTransformUtility.WorldToScreenPoint(null, joystick.transform.position);
            Touch(4, UnityEngine.InputSystem.TouchPhase.Began, center); yield return null; yield return null;
            Touch(4, UnityEngine.InputSystem.TouchPhase.Moved, point); yield return null; yield return null;
            Touch(4, UnityEngine.InputSystem.TouchPhase.Ended, point); yield return null; yield return null;
            Assert.That(hud.IsVisible, Is.False);
            // A real obstacle in front of the model blocks selection, including a collider-free employee.
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = Vector3.Lerp(Camera.main.transform.position, player.transform.position, .7f);
            wall.transform.localScale = Vector3.one * 2; Physics.SyncTransforms();
            Assert.That(selection.TargetAt(point), Is.Null);
            Object.Destroy(wall); yield return null;
            var shop = Object.FindFirstObjectByType<FacilityShopHud>(); shop.Open();
            yield return Click(point); Assert.That(hud.IsVisible, Is.False); shop.Close();
            Touch(5, UnityEngine.InputSystem.TouchPhase.Began, point); yield return null; yield return null;
            Touch(5, UnityEngine.InputSystem.TouchPhase.Ended, point); yield return null; yield return null;
            Assert.That(hud.IsVisible, Is.True);
            Assert.That(Time.timeScale, Is.Zero);
            hud.Popup.enabled = false; Assert.That(Time.timeScale, Is.EqualTo(1));
            CleanupInput(); LogAssert.NoUnexpectedReceived(); yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator CharacterCardsFitPortraitAndLandscapeAndInvestmentFollowsActor()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity"); yield return new EnterPlayMode();
            var playerHud = Object.FindFirstObjectByType<PlayerUpgradeHud>();
            var staffHud = Object.FindFirstObjectByType<StaffUpgradeHud>();
            var wallet = Object.FindFirstObjectByType<RestaurantWallet>(); wallet.RestoreProgress(1000, 0);
            var goals = Object.FindFirstObjectByType<SessionGoalTracker>(); goals.Restore(3, 0, 0);
            Object.FindFirstObjectByType<MainHallExpansion>().Restore(true, true, MainHallExpansion.Cost);
            Object.FindFirstObjectByType<WorkerHiringZone>().RestoreWorkers(1, 0);
            for (int i = 0; i < 100 && goals.Investments.Current?.Id != "player-speed"; i++) goals.Investments.Next();
            var offer = goals.Investments.Current;
            Assert.That(offer.Id, Is.EqualTo("player-speed"));
            Assert.That(offer.Actor, Is.SameAs(Object.FindFirstObjectByType<PlayerMotor>()));
            offer.Actor.transform.position += Vector3.right;
            Assert.That(offer.Target, Is.EqualTo(offer.Actor.transform.position));
            var capsule = Object.FindFirstObjectByType<TaskCapsuleHud>();
            goals.Restore(8, 0, 0, savedMilestones: ShopRanks.MilestoneMask); // No first-hire teaching takes priority over the selected investment.
            for (int i = 0; i < 100 && goals.Investments.Current?.Id != "player-speed"; i++) goals.Investments.Next();
            capsule.GetComponent<Button>().onClick.Invoke();
            Assert.That(playerHud.IsVisible, Is.True); playerHud.ClickClose();
            Object.FindFirstObjectByType<SalesHud>().Advance(2f);
            var camera = Camera.main; var canvas = playerHud.GetComponentInParent<Canvas>();
            var data = camera.GetComponents<Component>().First(c => c.GetType().Name == "UniversalAdditionalCameraData");
            var serialized = new UnityEditor.SerializedObject(data);
            serialized.FindProperty("m_RenderPostProcessing").boolValue = false; serialized.ApplyModifiedPropertiesWithoutUndo();
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
            foreach (var size in new[] { new Vector2Int(1080,1920), new Vector2Int(1600,1000) })
            {
                var target = new RenderTexture(size.x, size.y, 24); camera.targetTexture = target;
                foreach (bool staff in new[] { false, true })
                {
                    Assert.That(staff ? staffHud.Open() : playerHud.Open(), Is.True);
                    yield return null; Canvas.ForceUpdateCanvases();
                    var popup = staff ? staffHud.Popup : playerHud.Popup;
                    popup.SendMessage("Fit"); Canvas.ForceUpdateCanvases();
                    var parent = (RectTransform)popup.transform.parent;
                    var bounds = HudChrome.LocalRect(popup.Panel, parent);
                    var area = parent.rect;
                    Assert.That(bounds.xMin, Is.GreaterThanOrEqualTo(area.xMin)); Assert.That(bounds.xMax, Is.LessThanOrEqualTo(area.xMax));
                    Assert.That(bounds.yMin, Is.GreaterThanOrEqualTo(area.yMin)); Assert.That(bounds.yMax, Is.LessThanOrEqualTo(area.yMax));
                    camera.Render(); var previous = RenderTexture.active; RenderTexture.active = target;
                    var image = new Texture2D(size.x, size.y, TextureFormat.RGB24, false);
                    image.ReadPixels(new Rect(0,0,size.x,size.y),0,0); image.Apply();
                    System.IO.File.WriteAllBytes($"/tmp/bs084-{(staff ? "staff" : "player")}-{size.x}.png", image.EncodeToPNG());
                    RenderTexture.active = previous; Object.Destroy(image); popup.Dismiss();
                }
                camera.targetTexture = null; Object.Destroy(target);
            }
            LogAssert.NoUnexpectedReceived(); yield return new ExitPlayMode();
        }
        void CleanupInput()
        {
            if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
            if (touch != null && touch.added) InputSystem.RemoveDevice(touch);
            mouse = null; touch = null;
            if (!inputCaptured) return; inputCaptured = false;
            InputSystem.settings.backgroundBehavior = background;
            InputSystem.settings.editorInputBehaviorInPlayMode = editorInput;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            CleanupInput(); StatUpgradePopup.Current?.Dismiss(); Time.timeScale = 1;
            if (UnityEditor.EditorApplication.isPlaying) yield return new ExitPlayMode();
        }
    }
}
