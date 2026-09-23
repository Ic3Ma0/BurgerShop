using BurgerShop.Building;
using BurgerShop.Core;
using BurgerShop.Persistence;
using BurgerShop.Player;
using UnityEngine;
using UnityEngine.UI;

namespace BurgerShop.UI
{
    public sealed class SaveSlotsHud : MonoBehaviour
    {
        RestaurantPersistence persistence;
        Transform backdrop, panel, list, confirm;
        Text confirmCopy;
        Button newGame, saveNow;
        int pendingDelete;
        bool open;
        float previousTimeScale = 1f;
        public bool IsOpen => open;
        public static SaveSlotsHud Current { get; private set; }

        public static SaveSlotsHud Build(Transform parent, RestaurantPersistence save)
        {
            var root = new GameObject("SaveSlots", typeof(RectTransform)).transform;
            root.SetParent(parent, false);
            var rect = (RectTransform)root;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var hud = root.gameObject.AddComponent<SaveSlotsHud>();
            hud.persistence = save;
            Current = hud;
            hud.BuildUi(parent);
            return hud;
        }

        void BuildUi(Transform canvas)
        {
            var cart = canvas.Find("FacilityShop/ShoppingCart") as RectTransform;
            if (cart == null) cart = GameObject.Find("ShoppingCart")?.GetComponent<RectTransform>();
            Vector2 pos = cart != null ? cart.anchoredPosition + new Vector2(-(cart.sizeDelta.x + 8f), 0f) : new Vector2(-556f, -24f);
            Vector2 size = cart != null ? cart.sizeDelta : new Vector2(96f, 96f);
            Transform gearParent = cart != null ? cart.parent : transform;
            var gear = Button(gearParent, "SettingsGear", "", Vector2.one, pos, size, Toggle);
            HudChrome.Icon(gear.transform, "GearIcon", HudChrome.Gear(), Vector2.one * .5f, Vector2.one * .5f, Vector2.zero, new Vector2(56f, 56f), HudChrome.Ink);

            backdrop = HudChrome.Panel(transform, "SavesBackdrop", Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Color(.07f, .10f, .11f, .52f)).transform;
            var shade = (RectTransform)backdrop;
            shade.anchorMax = Vector2.one;
            shade.offsetMin = shade.offsetMax = Vector2.zero;
            backdrop.GetComponent<Image>().raycastTarget = true;

            panel = HudChrome.Panel(transform, "SavesPanel", Vector2.one * .5f, Vector2.one * .5f, Vector2.zero, new Vector2(920f, 1100f), HudChrome.Cream).transform;
            HudChrome.Label(panel, "Title", new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0f, -24f), new Vector2(800f, 48f), 34, HudChrome.Ink, TextAnchor.MiddleCenter, true, false).text = "Saves";
            saveNow = Button(panel, "SaveNow", "Save now", new Vector2(.5f, 1f), new Vector2(-220f, -96f), new Vector2(320f, 64f), SaveNow);
            newGame = Button(panel, "NewGame", "New game", new Vector2(.5f, 1f), new Vector2(220f, -96f), new Vector2(320f, 64f), StartNewGame);
            Button(panel, "Close", "Close", new Vector2(.5f, 0f), new Vector2(0f, 28f), new Vector2(240f, 64f), Close);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D)).transform;
            viewport.SetParent(panel, false);
            var vr = (RectTransform)viewport;
            vr.anchorMin = Vector2.zero;
            vr.anchorMax = Vector2.one;
            vr.offsetMin = new Vector2(26f, 110f);
            vr.offsetMax = new Vector2(-26f, -180f);
            viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
            list = new GameObject("Slots", typeof(RectTransform)).transform;
            list.SetParent(viewport, false);
            var cr = (RectTransform)list;
            cr.anchorMin = new Vector2(0f, 1f);
            cr.anchorMax = Vector2.one;
            cr.pivot = new Vector2(.5f, 1f);
            cr.sizeDelta = Vector2.zero;
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = vr;
            scroll.content = cr;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            confirm = HudChrome.Panel(panel, "DeleteConfirm", Vector2.one * .5f, Vector2.one * .5f, Vector2.zero, new Vector2(640f, 280f), HudChrome.Cream).transform;
            confirmCopy = HudChrome.Label(confirm, "Copy", new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0f, -28f), new Vector2(580f, 120f), 26, HudChrome.Ink, TextAnchor.MiddleCenter, true, true);
            Button(confirm, "CancelDelete", "Cancel", new Vector2(.5f, 0f), new Vector2(-140f, 28f), new Vector2(220f, 64f), () => confirm.gameObject.SetActive(false));
            Button(confirm, "ConfirmDelete", "Delete", new Vector2(.5f, 0f), new Vector2(140f, 28f), new Vector2(220f, 64f), ConfirmDelete);
            confirm.gameObject.SetActive(false);
            panel.gameObject.SetActive(false);
            backdrop.gameObject.SetActive(false);
        }

        Button Button(Transform parent, string name, string text, Vector2 anchor, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction clicked)
        {
            var image = HudChrome.Panel(parent, name, anchor, anchor, pos, size, HudChrome.Cream);
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(clicked);
            HudChrome.Label(image.transform, "Text", Vector2.zero, Vector2.one, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero, 25, HudChrome.Ink, TextAnchor.MiddleCenter, true, true).text = text;
            var label = image.GetComponentInChildren<Text>();
            var r = label.rectTransform;
            r.offsetMin = new Vector2(12f, 6f);
            r.offsetMax = new Vector2(-12f, -6f);
            return button;
        }

        public void Toggle()
        {
            if (open) Close();
            else Open();
        }

        public void Open()
        {
            if (open) return;
            var shop = FindFirstObjectByType<FacilityShopHud>();
            if (shop != null && shop.IsOpen) shop.Close();
            open = true;
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            persistence?.Flush();
            confirm.gameObject.SetActive(false);
            panel.gameObject.SetActive(true);
            backdrop.gameObject.SetActive(true);
            Refresh();
            Fit();
        }

        public void Close()
        {
            if (!open) return;
            open = false;
            Time.timeScale = previousTimeScale;
            panel.gameObject.SetActive(false);
            backdrop.gameObject.SetActive(false);
            confirm.gameObject.SetActive(false);
        }

        void SaveNow()
        {
            persistence?.SaveNow();
            Refresh();
        }

        void StartNewGame()
        {
            if (persistence == null || !persistence.CanStartNewGame) return;
            if (!persistence.StartNewGame()) return;
            Close();
        }

        void LoadSlot(int id)
        {
            if (persistence == null || id == persistence.ActiveSlotId) return;
            if (!persistence.SwitchToSlot(id)) return;
            Close();
        }

        void AskDelete(int id)
        {
            pendingDelete = id;
            confirmCopy.text = "Delete " + SaveSlotStore.Label(id) + "?\nThis shop will be removed.";
            confirm.gameObject.SetActive(true);
        }

        void ConfirmDelete()
        {
            persistence?.DeleteSlot(pendingDelete);
            confirm.gameObject.SetActive(false);
            Refresh();
        }

        void Refresh()
        {
            if (list == null || persistence == null) return;
            for (int i = list.childCount - 1; i >= 0; i--) BurgerShop.Restaurant.BurgerVisual.Release(list.GetChild(i).gameObject);
            var rows = persistence.SlotSummaries;
            float pitch = 108f;
            for (int i = 0; i < rows.Length; i++)
            {
                var slot = rows[i];
                bool current = slot.id == persistence.ActiveSlotId;
                var row = HudChrome.Panel(list, "Slot_" + slot.id, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -i * pitch), new Vector2(840f, 96f), current ? HudChrome.Green : HudChrome.TrackNavy);
                row.raycastTarget = false;
                string mark = current ? "Now" : "";
                HudChrome.Label(row.transform, "Summary", new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(20f, 0f), new Vector2(420f, 80f), 26, current ? Color.white : HudChrome.Ink, TextAnchor.MiddleLeft, true, false).text =
                    SaveSlotStore.Label(slot.id) + "  Lv." + Mathf.Max(1, slot.shopRank) + "  " + slot.coins.ToString("N0") + (string.IsNullOrEmpty(mark) ? "" : "  " + mark);
                if (!current)
                {
                    var load = Button(row.transform, "Load_" + slot.id, "Load", new Vector2(1f, .5f), new Vector2(-250f, 0f), new Vector2(150f, 56f), () => LoadSlot(slot.id));
                    load.targetGraphic.color = HudChrome.Cream;
                    if (persistence.CanDeleteSlot(slot.id))
                    {
                        var delete = Button(row.transform, "Delete_" + slot.id, "Delete", new Vector2(1f, .5f), new Vector2(-80f, 0f), new Vector2(150f, 56f), () => AskDelete(slot.id));
                        delete.targetGraphic.color = HudChrome.Cream;
                    }
                }
            }
            ((RectTransform)list).sizeDelta = new Vector2(0f, rows.Length * pitch);
            if (newGame != null)
            {
                newGame.interactable = persistence.CanStartNewGame;
                newGame.targetGraphic.color = newGame.interactable ? HudChrome.Cream : HudChrome.TrackNavy;
            }
        }

        void Update()
        {
            if (!open) return;
            Fit();
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) Close();
        }

        void Fit()
        {
            var available = ((RectTransform)transform).rect.size;
            var sheet = (RectTransform)panel;
            float scale = Mathf.Min(1f, (available.x - 40f) / sheet.sizeDelta.x,
                (available.y - 60f) / sheet.sizeDelta.y);
            sheet.localScale = Vector3.one * Mathf.Max(.1f, scale);
        }

        void OnDisable()
        {
            if (open) Close();
        }

        void OnDestroy()
        {
            if (Current == this) Current = null;
            if (open) Close();
        }
    }

    public static class ShopSlotReload
    {
        public static void Queue() => Goal01Bootstrap.RequestInstalledShopRebuild();
    }
}
