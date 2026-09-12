using BurgerShop.Economy;
using BurgerShop.Player;
using BurgerShop.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace BurgerShop.Tests.EditMode
{
    public sealed class HudLayoutTests
    {
        GameObject root;
        RectTransform safe;
        SessionGoalTracker tracker;
        RestaurantWallet wallet;
        StarProgressHud stars;
        SalesHud sales;
        TaskCapsuleHud capsule;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("HudLayout", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var canvasRect = (RectTransform)root.transform;
            canvasRect.pivot = Vector2.zero;
            canvasRect.anchorMin = canvasRect.anchorMax = Vector2.zero;
            canvasRect.anchoredPosition = Vector2.zero;
            canvasRect.sizeDelta = new Vector2(1080f, 1920f);
            canvasRect.position = Vector3.zero;
            canvasRect.localScale = Vector3.one;

            var safeObject = new GameObject("SafeArea", typeof(RectTransform));
            safeObject.transform.SetParent(root.transform, false);
            safe = (RectTransform)safeObject.transform;
            safe.pivot = Vector2.zero;
            safe.anchorMin = safe.anchorMax = Vector2.zero;
            safe.anchoredPosition = Vector2.zero;
            safe.sizeDelta = new Vector2(1080f, 1920f);

            var pad = new GameObject("VirtualJoystick", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            pad.transform.SetParent(safe, false);
            RectTransform padRect = pad.GetComponent<RectTransform>();
            padRect.anchorMin = padRect.anchorMax = Vector2.zero;
            padRect.pivot = new Vector2(0.5f, 0.5f);
            padRect.anchoredPosition = HudChrome.JoystickPosition;
            padRect.sizeDelta = HudChrome.JoystickSize;
            pad.GetComponent<Image>().raycastTarget = true;

            var knob = new GameObject("Knob", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            knob.transform.SetParent(pad.transform, false);
            knob.GetComponent<Image>().raycastTarget = false;

            tracker = root.AddComponent<SessionGoalTracker>();
            tracker.Configure(null, null, null, null, null, null);
            wallet = root.AddComponent<RestaurantWallet>();

            HudChrome.BuildTopBand(safe);
            stars = StarProgressHud.Build(safe, tracker);
            capsule = TaskCapsuleHud.Build(safe, tracker);
            sales = SalesHud.Build(safe, wallet);
            Canvas.ForceUpdateCanvases();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(root);

        [Test]
        public void OpeningHudShowsProgressWithoutFalseCelebration()
        {
            Text starValue = stars.transform.Find("StarBarBack/StarValue").GetComponent<Text>();
            Text title = capsule.transform.Find("TaskTitle").GetComponent<Text>();
            Text progress = capsule.transform.Find("TaskProgress").GetComponent<Text>();
            Text coins = sales.GetComponent<Text>();
            Assert.That(starValue.text, Is.EqualTo("Lv.1  0/3"));
            Assert.That(title.text, Is.EqualTo("Pick up a burger"));
            Assert.That(progress.text, Is.EqualTo("0/1"));
            Assert.That(coins.text, Is.EqualTo("0"));
            Assert.That(coins.text, Does.Not.Contain("SERVED").And.Not.Contain("COINS"));
            Assert.That(capsule.GetComponent<Image>().color, Is.EqualTo(HudChrome.CapsuleIdle));
            Assert.That(capsule.transform.Find("TaskBadge/TaskCheck").GetComponent<Image>().enabled, Is.False);
            Assert.That(sales.transform.Find("CoinIcon"), Is.Not.Null);
            Assert.That(stars.transform.Find("StarIcon").GetComponent<Image>().sprite, Is.EqualTo(HudChrome.Star()));
            Assert.That(title.fontStyle, Is.EqualTo(FontStyle.Bold));
            Assert.That(title.GetComponent<Outline>(), Is.Null);
        }

        [Test]
        public void CapsuleCopyFollowsTheSessionGoal()
        {
            tracker.Advance(2f);
            capsule.RefreshNow();
            stars.RefreshNow();
            Text title = capsule.transform.Find("TaskTitle").GetComponent<Text>();
            Text progress = capsule.transform.Find("TaskProgress").GetComponent<Text>();
            Assert.That(tracker.Title, Is.EqualTo("Pick up a burger"));
            Assert.That(title.text, Is.EqualTo("Pick up a burger"));
            Assert.That(progress.text, Is.EqualTo("0/1"));
            Assert.That(capsule.GetComponent<Image>().color, Is.EqualTo(HudChrome.CapsuleIdle));
            Assert.That(capsule.transform.Find("TaskBadge/TaskCheck").GetComponent<Image>().enabled, Is.False);
            Assert.That(capsule.transform.Find("TaskBadge/TaskGlyph").GetComponent<Image>().enabled, Is.True);
        }

        [Test]
        public void NewHudStaysOutOfTheJoystickKeepoutAndDoesNotRaycast()
        {
            RectTransform joystick = (RectTransform)safe.Find("VirtualJoystick");
            Assert.That(joystick.anchoredPosition, Is.EqualTo(HudChrome.JoystickPosition));
            Assert.That(joystick.sizeDelta, Is.EqualTo(HudChrome.JoystickSize));
            Assert.That(joystick.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(joystick.GetComponent<Image>().raycastTarget, Is.True);

            string[] widgets = { "TopChrome", "StarProgress", "TaskCapsule", "SalesStatus" };
            foreach (string name in widgets)
            {
                RectTransform widget = (RectTransform)safe.Find(name);
                Assert.That(widget, Is.Not.Null, name);
                Assert.That(HudChrome.OverlapsJoystickKeepout(widget, safe), Is.False, name + " overlaps joystick keepout");
            }

            foreach (Graphic graphic in safe.GetComponentsInChildren<Graphic>(true))
            {
                if (graphic.gameObject.name == "VirtualJoystick") continue;
                Assert.That(graphic.raycastTarget, Is.False, graphic.name);
            }
        }

        [Test]
        public void CarryAndCustomerStatusStayHiddenOnTheDefaultScreen()
        {
            var carry = new GameObject("CarryStatus", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            carry.transform.SetParent(safe, false);
            Text carryText = carry.GetComponent<Text>();
            carryText.font = HudChrome.Font();
            var inventory = carry.AddComponent<BurgerInventory>();
            inventory.Configure();
            carry.AddComponent<CarryHud>().Configure(inventory, null, carryText);
            Assert.That(carry.GetComponent<CanvasGroup>().alpha, Is.Zero);
            Assert.That(carryText.text, Does.Contain("0/4"));

            var customers = new GameObject("CustomerStatus", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            customers.transform.SetParent(safe, false);
            Text customerText = customers.GetComponent<Text>();
            customerText.font = HudChrome.Font();
            customers.AddComponent<CustomerQueueHud>().Configure(null, customerText);
            Assert.That(customers.GetComponent<CanvasGroup>().alpha, Is.Zero);
        }

        [Test]
        public void CoinHudHasNoIapPlus()
        {
            Assert.That(sales.transform.Find("+"), Is.Null);
            Assert.That(sales.transform.Find("Plus"), Is.Null);
            Assert.That(sales.GetComponent<Text>().text, Is.EqualTo("0"));
        }
    }
}
