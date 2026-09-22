using System;
using System.Collections.Generic;
using BurgerShop.Building;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BurgerShop.UI
{
    // One pointer gesture owns selection. A rejected drag cannot become a tap again on release.
    internal sealed class FacilityTapGesture
    {
        internal const float MaxSeconds = .4f;
        internal const float MaxTravel = 14f;
        FacilityInstance pressed;
        Vector2 origin;
        float began;
        internal void Cancel() => pressed = null;
        internal void Begin(FacilityInstance item, Vector2 point, float now, bool blocked)
        { pressed = blocked ? null : item; origin = point; began = now; }
        internal void Track(Vector2 point, float now, bool blocked, float scale = 1f)
        {
            if (blocked || now - began > MaxSeconds || Vector2.Distance(point, origin) > MaxTravel * scale) Cancel();
        }
        internal FacilityInstance Release(FacilityInstance item, Vector2 point, float now, bool blocked, float scale = 1f)
        {
            Track(point, now, blocked, scale);
            var result = pressed != null && pressed == item ? pressed : null;
            Cancel(); return result;
        }
    }

    public sealed class FacilityDetailsHud : MonoBehaviour
    {
        public static FacilityDetailsHud Current { get; private set; }
        public bool IsOpen => selected != null;
        public FacilityInstance Selected => selected;
        FacilityLayout layout;
        FacilityShopHud shop;
        GrowthUpgrades growth;
        VirtualJoystick joystick;
        readonly FacilityTapGesture gesture = new FacilityTapGesture();
        readonly List<RaycastResult> uiHits = new List<RaycastResult>();
        FacilityInstance selected;
        GrillUpgradeZone grill;
        TableUpgradeZone table;
        GrowthUpgrades.Offer offer;
        RectTransform sheet;
        GameObject shade;
        Text title, levelText, benefit, price, feedback, upgradeText;
        Image photo, cashIcon;
        Button upgrade;
        readonly Button[] choices = new Button[3];
        readonly Text[] choiceNames = new Text[3], choiceStats = new Text[3], choicePrices = new Text[3];
        readonly Image[] choicePhotos = new Image[3];
        int expectedLevel, expectedInvestment;
        float previousTimeScale, nextPurchase;
        bool ownsPause, followWasEnabled;
        CameraFollow follow;
        long paintedCoins = long.MinValue;

        public static FacilityDetailsHud Build(Transform parent, FacilityLayout facilities, FacilityShopHud store, GrowthUpgrades upgrades)
        {
            var go = new GameObject("FacilityDetails", typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var hud = go.AddComponent<FacilityDetailsHud>();
            hud.layout = facilities; hud.shop = store; hud.growth = upgrades;
            hud.joystick = FindFirstObjectByType<VirtualJoystick>();
            Current = hud;
            foreach (var zone in facilities.GetComponentsInChildren<GrillUpgradeZone>(true)) zone.UseDirectInteraction();
            foreach (var zone in facilities.GetComponentsInChildren<TableUpgradeZone>(true)) zone.UseDirectInteraction();
            upgrades.UseDirectInteraction();
            hud.BuildUi(); return hud;
        }

        static Text Label(Transform parent, string name, Vector2 position, Vector2 size, int font = 30, bool bold = false)
        {
            var text = HudChrome.Label(parent, name, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(.5f, 1),
                position, size, font, HudChrome.Ink, TextAnchor.UpperLeft, bold, false);
            text.horizontalOverflow = HorizontalWrapMode.Wrap; return text;
        }
        static Button Button(Transform parent, string name, string copy, Vector2 position, Vector2 size, Color color, UnityEngine.Events.UnityAction action)
        {
            var image = HudChrome.Panel(parent, name, new Vector2(.5f, 0), new Vector2(.5f, 0), position, size, color);
            image.raycastTarget = true;
            var b = image.gameObject.AddComponent<Button>(); b.targetGraphic = image; b.onClick.AddListener(action);
            HudChrome.Label(b.transform, "Text", Vector2.zero, Vector2.one, Vector2.one * .5f, Vector2.zero, new Vector2(-16,-8),
                30, color == HudChrome.Green ? Color.white : HudChrome.Ink, TextAnchor.MiddleCenter, true, false).text = copy;
            return b;
        }
        void BuildUi()
        {
            var background = HudChrome.Panel(transform, "DetailsBackdrop", Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Color(.06f,.09f,.08f,.5f));
            background.raycastTarget = true; background.rectTransform.anchorMax = Vector2.one;
            background.rectTransform.offsetMin = background.rectTransform.offsetMax = Vector2.zero; shade = background.gameObject;
            var plate = HudChrome.Panel(transform, "DetailsSheet", new Vector2(.5f,.5f), new Vector2(.5f,.5f), Vector2.zero, new Vector2(920,670), HudChrome.Cream);
            plate.raycastTarget = true; sheet = plate.rectTransform;
            title = Label(sheet, "Title", new Vector2(-50,-30), new Vector2(740,50), 38, true);
            Button(sheet, "CloseDetails", "Close", new Vector2(355,580), new Vector2(144,66), HudChrome.TrackNavy, Close);
            photo = HudChrome.Icon(sheet, "FacilityPhoto", null, new Vector2(0,1), new Vector2(0,1), new Vector2(30,-100), new Vector2(330,230), Color.white);
            levelText = Label(sheet, "Level", new Vector2(170,-110), new Vector2(440,48), 34, true);
            benefit = Label(sheet, "Benefit", new Vector2(170,-168), new Vector2(440,155));
            price = Label(sheet, "Price", new Vector2(24,-375), new Vector2(786,60), 34, true);
            var cash = cashIcon = FoodIcons.Add(sheet, FoodIcon.Coin, Vector2.zero, 48);
            cash.rectTransform.anchorMin = cash.rectTransform.anchorMax = new Vector2(0,1);
            cash.rectTransform.anchoredPosition = new Vector2(58,-397);
            feedback = Label(sheet, "Feedback", new Vector2(0,-451), new Vector2(840,65), 28);
            upgrade = Button(sheet, "Upgrade", "Upgrade", new Vector2(202,28), new Vector2(434,86), HudChrome.Green, BuyUpgrade);
            upgradeText = upgrade.GetComponentInChildren<Text>();
            Button(sheet, "MoveFacility", "Move", new Vector2(-230,28), new Vector2(368,86), HudChrome.TrackNavy, Move);
            for (int i = 0; i < choices.Length; i++)
            {
                int index = i;
                var card = Button(sheet, "ChooseSet"+i, "", new Vector2((i-1)*280,156), new Vector2(266,370), Color.white, () => BuyTable(index));
                choices[i] = card;
                choiceNames[i] = Label(card.transform, "SetName", new Vector2(0,-14), new Vector2(234,40), 30, true);
                choicePhotos[i] = HudChrome.Icon(card.transform,"SetPhoto",null,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-54),new Vector2(238,142),Color.white);
                choiceStats[i] = Label(card.transform,"SetBenefit",new Vector2(0,-204),new Vector2(234,76),28);
                var pill = HudChrome.Panel(card.transform,"SelectPrice",new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,12),new Vector2(234,68),HudChrome.Green);
                choicePrices[i] = HudChrome.Label(pill.transform,"Text",Vector2.zero,Vector2.one,Vector2.one*.5f,Vector2.zero,new Vector2(-10,-4),28,Color.white,TextAnchor.MiddleCenter,true,false);
            }
            shade.SetActive(false); sheet.gameObject.SetActive(false);
        }

        public bool Open(FacilityInstance instance)
        {
            if (layout.Editing || !FacilityShopHud.CanSelect(instance)) return false;
            if (shop.IsOpen) shop.Close();
            if (!ownsPause)
            {
                previousTimeScale = Time.timeScale; Time.timeScale = 0; ownsPause = true;
                follow = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
                if (follow != null) { followWasEnabled = follow.enabled; follow.enabled = false; }
            }
            joystick?.OnCancel(null); gesture.Cancel(); selected = instance;
            grill = instance.GetComponentInChildren<GrillUpgradeZone>(true);
            table = instance.GetComponentInChildren<TableUpgradeZone>(true);
            offer = null;
            foreach (var candidate in growth.Offers)
                if (candidate.Target != null && (candidate.Target == instance.transform || candidate.Target.IsChildOf(instance.transform))) { offer = candidate; break; }
            title.text = FacilityCatalog.Get(instance.Kind).Name;
            photo.sprite = FacilityThumbnails.Get(instance.Kind);
            feedback.text = table != null && !table.HasChosenSet && table.Invested > 0 ? $"{table.Invested:N0} already paid · only the remaining amount is charged" : "";
            feedback.color = HudChrome.Ink; nextPurchase = 0;
            transform.SetAsLastSibling(); shade.SetActive(true); sheet.gameObject.SetActive(true);
            Refresh(); Fit(); return true;
        }
        void Refresh()
        {
            if (selected == null) return;
            paintedCoins = layout.Wallet.Coins;
            bool tableChoice = table != null && !table.HasChosenSet;
            expectedLevel = grill != null ? grill.Level : offer != null ? growth.Level(offer.Id) : selected.Capture().level;
            expectedInvestment = table != null ? table.Invested : 0;
            levelText.text = tableChoice ? "Level 1 → 2  ·  " + ShopRanks.StarRewardCopy : "Level " + expectedLevel;
            levelText.rectTransform.anchoredPosition = tableChoice ? new Vector2(0,-96) : new Vector2(170,-110);
            levelText.rectTransform.sizeDelta = tableChoice ? new Vector2(840,48) : new Vector2(440,48);
            if(table != null && table.HasChosenSet) photo.sprite = TableSetThumbnails.Get(selected.Kind,table.SetId);
            bool canUpgrade = grill != null ? !grill.IsMaxLevel : offer != null && expectedLevel <= offer.Costs.Length;
            int cost = tableChoice ? TableSetCatalog.MinCost : canUpgrade ? grill != null ? grill.NextCost : offer.Costs[expectedLevel-1] : 0;
            long missing = Math.Max(0, cost - layout.Wallet.Coins);
            if (grill != null)
                benefit.text = $"{grill.CurrentProductionSeconds:0.##}s / {grill.ItemNoun}" + (canUpgrade ? $" → {grill.NextProductionSeconds:0.##}s\nStock {grill.Station.Capacity} → {ProductionStation.CapacityForLevel(expectedLevel+1, grill.Product)}\n{ShopRanks.StarRewardCopy}" : "\nAll available upgrades installed");
            else if (table != null)
            {
                var current = TableSetCatalog.Get(table.SetId);
                benefit.text = $"{current.Name}\n{current.MealPay} / meal · {current.EatSeconds:0.##}s";
            }
            else if (offer != null)
                benefit.text = DescribeOffer(expectedLevel) + (canUpgrade ? "\n→ " + DescribeOffer(expectedLevel+1) + "\n" + ShopRanks.StarRewardCopy : "\nAll available upgrades installed");
            else benefit.text = "Ready for service\nChoose Move to rearrange";
            photo.gameObject.SetActive(!tableChoice); levelText.gameObject.SetActive(true); benefit.gameObject.SetActive(!tableChoice);
            price.text = tableChoice ? (table.Invested > 0
                    ? $"{table.Invested:N0} already paid · each tier charges only the remaining amount"
                    : "Value 50 · Turnover 80 · Premium 120")
                : canUpgrade ? $"{cost:N0}   ·   " + (missing > 0 ? $"Need {missing:N0} more" : $"Balance {layout.Wallet.Coins:N0}") : "No further upgrades for this facility";
            price.rectTransform.anchoredPosition = new Vector2(24, tableChoice ? -538 : -375);
            cashIcon.rectTransform.anchoredPosition = new Vector2(58,tableChoice ? -560 : -397);
            feedback.rectTransform.anchoredPosition = new Vector2(0,tableChoice ? -595 : -451);
            sheet.sizeDelta = new Vector2(920,tableChoice ? 810 : 670);
            ((RectTransform)sheet.Find("CloseDetails")).anchoredPosition = new Vector2(355,tableChoice ? 720 : 580);
            upgrade.gameObject.SetActive(!tableChoice && canUpgrade); upgrade.interactable = missing == 0;
            upgradeText.text = missing == 0 ? "Upgrade" : "Not enough cash";
            var move = (RectTransform)sheet.Find("MoveFacility");
            move.anchoredPosition = new Vector2(!tableChoice && canUpgrade ? -230 : 0,28);
            move.sizeDelta = new Vector2(!tableChoice && canUpgrade ? 368 : 840,86);
            for (int i = 0; i < choices.Length; i++)
            {
                choices[i].gameObject.SetActive(tableChoice); if (!tableChoice) continue;
                var set = TableSetCatalog.Get(TableSetCatalog.Choices[i]);
                choiceNames[i].text = set.Name + " · " + set.TierLabel;
                choicePhotos[i].sprite = TableSetThumbnails.Get(selected.Kind, set.Id);
                var starter = TableSetCatalog.Get(TableSetId.Starter);
                int due = TableSetCatalog.Due(set.Id, table.Invested);
                long setMissing = Math.Max(0, due - layout.Wallet.Coins);
                choiceStats[i].text = $"{set.TierLabel} · {set.Cost:N0}\n{starter.MealPay} → {set.MealPay} / meal\n{starter.EatSeconds:0.#}s → {set.EatSeconds:0.#}s · {set.SpeedLabel}";
                choicePrices[i].text = due == 0 ? "Choose · Paid · " + ShopRanks.StarRewardCopy : setMissing == 0 ? $"Upgrade\n{due:N0} · {ShopRanks.StarRewardCopy}" : $"Need {setMissing:N0}";
                choices[i].interactable = setMissing == 0;
                choiceStats[i].fontSize = 24;
                choiceStats[i].rectTransform.sizeDelta = new Vector2(234, 96);
                var pillImg = choices[i].transform.Find("SelectPrice")?.GetComponent<Image>();
                if (pillImg != null) pillImg.color = setMissing == 0 ? HudChrome.Green : HudChrome.TrackNavy;
                ((RectTransform)choices[i].transform).anchoredPosition = new Vector2((i-1)*280,290);
            }
        }
        string DescribeOffer(int level)
        {
            // Read the same timing formulas used by service, rather than copy catalog marketing text.
            switch (selected.Kind)
            {
                case FacilityKind.BurgerCounter: case FacilityKind.ColaCounter:
                    return $"Item every {BurgerServingZone.HandoffDuration + BurgerServingZone.CooldownForLevel(level):0.00}s";
                case FacilityKind.BlueBoxTable: return $"Packing {BoxingStation.SecondsForLevel(level):0.00}s";
                case FacilityKind.BagMachine: return $"Bag every {BagLine.ProductionForLevel(level):0.0}s";
                case FacilityKind.BagTable: return $"Packing {BagLine.ProcessingForLevel(level):0.00}s";
                case FacilityKind.BagCounter: return $"Item every {BurgerServingZone.HandoffDuration + BagLine.CooldownForLevel(level):0.00}s";
                case FacilityKind.CarCounter: return FacilityUpgradeBenefit.Describe(selected.Kind,level);
                default: return offer.Benefit(level);
            }
        }
        public void BuyUpgrade()
        {
            if (!IsOpen || Time.unscaledTime < nextPurchase) return;
            nextPurchase = Time.unscaledTime + .3f;
            bool bought = grill != null ? grill.TryUpgrade(expectedLevel) : offer != null && growth.TryBuy(offer.Id, expectedLevel);
            feedback.text = bought ? "Upgraded!" : "Upgrade unavailable — check cash and level";
            feedback.color = bought ? HudChrome.Green : HudChrome.Tomato;
            Refresh();
        }
        public void BuyTable(int index)
        {
            if (!IsOpen || table == null || index < 0 || index >= choices.Length || Time.unscaledTime < nextPurchase) return;
            nextPurchase = Time.unscaledTime + .3f;
            bool bought = table.TryBuySet(TableSetCatalog.Choices[index], expectedInvestment);
            feedback.text = bought ? "New table set installed!" : "Unable to buy this set — check remaining cash";
            feedback.color = bought ? HudChrome.Green : HudChrome.Tomato;
            Refresh(); Fit();
        }
        public void Move()
        {
            if (!IsOpen) return;
            var item = selected; Close(); shop.Open(); shop.MoveExisting(item);
        }
        public void Close()
        {
            selected = null; gesture.Cancel();
            if (sheet != null) sheet.gameObject.SetActive(false);
            if (shade != null) shade.SetActive(false);
            if (!ownsPause) return;
            ownsPause = false; Time.timeScale = previousTimeScale;
            if (follow != null) follow.enabled = followWasEnabled;
        }
        void Fit()
        {
            var size = ((RectTransform)transform).rect.size;
            float scale = Mathf.Min(1f, (size.x-40)/sheet.sizeDelta.x, (size.y-80)/sheet.sizeDelta.y);
            sheet.localScale = Vector3.one * Mathf.Max(.1f,scale);
        }
        internal FacilityInstance FacilityAt(Vector2 point)
        {
            if (Camera.main == null) return null;
            var hits = Physics.RaycastAll(Camera.main.ScreenPointToRay(point), 500, ~0, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (a,b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                if (hit.collider.GetComponentInParent<Customer.CustomerAgent>() != null || hit.collider.GetComponentInParent<RestaurantWorker>() != null
                    || hit.collider.GetComponentInParent<PlayerMotor>() != null) continue;
                var instance = hit.collider.GetComponentInParent<FacilityInstance>();
                return FacilityShopHud.CanSelect(instance) ? instance : null;
            }
            return null;
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
            if (IsOpen)
            {
                if (!FacilityShopHud.CanSelect(selected)) { Close(); return; }
                if (Keyboard.current?.escapeKey.wasPressedThisFrame == true) { Close(); return; }
                if (paintedCoins != layout.Wallet.Coins) Refresh();
                Fit(); return;
            }
            if (shop.IsOpen || layout.Editing || Time.timeScale <= 0) { gesture.Cancel(); return; }
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
            if (down) gesture.Begin(blocked ? null : FacilityAt(point), point, Time.unscaledTime, blocked);
            gesture.Track(point, Time.unscaledTime, blocked, scale);
            if (up)
            {
                var item = gesture.Release(blocked ? null : FacilityAt(point), point, Time.unscaledTime, blocked, scale);
                if (item != null) Open(item);
            }
        }
        void OnApplicationFocus(bool focused) { if (!focused) gesture.Cancel(); }
        void OnApplicationPause(bool paused) { if (paused) gesture.Cancel(); }
        void OnDisable() => Close();
        void OnDestroy() { Close(); if (Current == this) Current = null; }
    }
}
