using System;
using System.Collections.Generic;
using BurgerShop.Building;
using BurgerShop.Core;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BurgerShop.UI
{
    // One pointer gesture owns selection. A rejected drag cannot become a tap again on release.
    internal sealed class WorldTapGesture
    {
        internal const float MaxSeconds = .4f;
        internal const float MaxTravel = 14f;
        Component pressed;
        Vector2 origin;
        float began;
        internal void Cancel() => pressed = null;
        internal void Begin(Component item, Vector2 point, float now, bool blocked)
        { pressed = blocked ? null : item; origin = point; began = now; }
        internal void Track(Vector2 point, float now, bool blocked, float scale = 1f)
        {
            if (blocked || now - began > MaxSeconds || Vector2.Distance(point, origin) > MaxTravel * scale) Cancel();
        }
        internal Component Release(Component item, Vector2 point, float now, bool blocked, float scale = 1f)
        {
            Track(point, now, blocked, scale);
            // A walking character can leave the original pixel between down and up.
            // Keep its identity captured on press, while the same drag/time/UI guards still apply.
            var result = pressed != null && (pressed == item || pressed is RestaurantWorker || pressed is PlayerMotor) ? pressed : null;
            Cancel(); return result;
        }
    }

    // Routes one gesture to one visible scene target. No colliders are added to workers.
    public sealed class WorldDetailsInput : MonoBehaviour
    {
        FacilityDetailsHud facilities;
        FacilityLayout layout;
        FacilityShopHud shop;
        PlayerUpgradeHud playerHud;
        StaffUpgradeHud staffHud;
        VirtualJoystick joystick;
        readonly WorldTapGesture gesture = new WorldTapGesture();
        readonly List<RaycastResult> uiHits = new List<RaycastResult>();

        public static void Build(GameObject host, FacilityDetailsHud details, FacilityLayout items, FacilityShopHud store)
        {
            var input = host.AddComponent<WorldDetailsInput>();
            input.facilities = details; input.layout = items; input.shop = store;
            input.playerHud = FindFirstObjectByType<PlayerUpgradeHud>();
            input.staffHud = FindFirstObjectByType<StaffUpgradeHud>();
            input.joystick = FindFirstObjectByType<VirtualJoystick>();
        }

        internal Component TargetAt(Vector2 point)
        {
            if (Camera.main == null) return null;
            var ray = Camera.main.ScreenPointToRay(point);
            float nearest = 500f;
            Component target = null;
            var hits = Physics.RaycastAll(ray, nearest, ~0, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (a,b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                // Actor visuals below handle selection and occlusion, including collider-free workers.
                if (hit.collider.GetComponentInParent<CharacterVisualResources>() != null) continue;
                nearest = hit.distance;
                var item = hit.collider.GetComponentInParent<FacilityInstance>();
                target = FacilityShopHud.CanSelect(item) ? item : null;
                break;
            }
            foreach (var actor in FindObjectsByType<CharacterVisualResources>(FindObjectsSortMode.None))
            {
                bool found = false;
                Bounds bounds = default;
                foreach (var renderer in actor.GetComponentsInChildren<MeshRenderer>())
                {
                    if (!renderer.enabled || renderer.name != "CharacterSurface") continue;
                    if (found) bounds.Encapsulate(renderer.bounds);
                    else { bounds = renderer.bounds; found = true; }
                }
                if (!found || !bounds.IntersectRay(ray, out var distance) || distance >= nearest) continue;
                nearest = distance;
                var player = actor.GetComponent<PlayerMotor>();
                var worker = actor.GetComponent<RestaurantWorker>();
                target = player != null && playerHud != null && playerHud.CanSelect(player) ? (Component)player
                    : worker != null && staffHud != null && staffHud.CanSelect(worker) ? worker : null;
            }
            return target;
        }
        bool OverUi(Vector2 pointer)
        {
            if (EventSystem.current == null) return false;
            uiHits.Clear(); EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = pointer }, uiHits);
            foreach (var hit in uiHits) if (hit.module is GraphicRaycaster) return true;
            return false;
        }
        void Update()
        {
            if (facilities.IsOpen || StatUpgradePopup.Current != null || shop.IsOpen || layout.Editing || Time.timeScale <= 0) { gesture.Cancel(); return; }
            var touch = Touchscreen.current?.primaryTouch;
            bool touchEvent = touch != null && (touch.press.isPressed || touch.press.wasReleasedThisFrame);
            var mouse = Mouse.current;
            if (!touchEvent && mouse == null) return;
            Vector2 point = touchEvent ? touch.position.ReadValue() : mouse.position.ReadValue();
            bool down = touchEvent ? touch.press.wasPressedThisFrame : mouse.leftButton.wasPressedThisFrame;
            bool up = touchEvent ? touch.press.wasReleasedThisFrame : mouse.leftButton.wasReleasedThisFrame;
            int fingers = 0;
            if (Touchscreen.current != null) foreach (var t in Touchscreen.current.touches) if (t.press.isPressed) fingers++;
            bool blocked = fingers > 1 || joystick != null && joystick.HasPointer || VirtualJoystick.Value.sqrMagnitude > .001f || OverUi(point);
            float scale = GetComponentInParent<Canvas>()?.scaleFactor ?? 1;
            if (down) gesture.Begin(blocked ? null : TargetAt(point), point, Time.unscaledTime, blocked);
            gesture.Track(point, Time.unscaledTime, blocked, scale);
            if (up)
            {
                var item = gesture.Release(blocked ? null : TargetAt(point), point, Time.unscaledTime, blocked, scale);
                if (item is FacilityInstance facility) facilities.Open(facility);
                else if (item is PlayerMotor) playerHud.Open();
                else if (item is RestaurantWorker worker) staffHud.Open(worker);
            }
        }
        void OnApplicationFocus(bool focused) { if (!focused) gesture.Cancel(); }
        void OnApplicationPause(bool paused) { if (paused) gesture.Cancel(); }
        void OnDisable() => gesture.Cancel();
    }
}
