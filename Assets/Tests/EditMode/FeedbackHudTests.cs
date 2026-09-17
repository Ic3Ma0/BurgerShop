using BurgerShop.Economy;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using BurgerShop.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace BurgerShop.Tests.EditMode
{
    public sealed class FeedbackHudTests
    {
        GameObject root;
        RectTransform safe;
        SessionGoalTracker tracker;
        RestaurantWallet wallet;
        SalesHud sales;
        StarProgressHud stars;
        TaskCapsuleHud capsule;
        BurgerInventory inventory;
        ProductionStation grill;
        CounterStock stock;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("FeedbackHud", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            var canvasRect = (RectTransform)root.transform;
            canvasRect.sizeDelta = new Vector2(1080f, 1920f);
            var safeObject = new GameObject("SafeArea", typeof(RectTransform));
            safeObject.transform.SetParent(root.transform, false);
            safe = (RectTransform)safeObject.transform;
            safe.sizeDelta = new Vector2(1080f, 1920f);

            inventory = new GameObject("Player").AddComponent<BurgerInventory>();
            inventory.transform.SetParent(root.transform);
            inventory.Configure();
            Transform output = new GameObject("Output").transform;
            output.SetParent(root.transform);
            grill = root.AddComponent<ProductionStation>();
            grill.Configure(output, null, null);
            Transform anchor = new GameObject("Anchor").transform;
            anchor.SetParent(root.transform);
            Transform badge = new GameObject("Badge").transform;
            badge.SetParent(root.transform, false);
            TextMesh count = new GameObject("Count").AddComponent<TextMesh>();
            count.transform.SetParent(badge, false);
            stock = root.AddComponent<CounterStock>();
            stock.Configure(anchor, count);

            tracker = root.AddComponent<SessionGoalTracker>();
            tracker.Configure(inventory, grill, stock, null, null, null);
            wallet = root.AddComponent<RestaurantWallet>();
            stars = StarProgressHud.Build(safe, tracker);
            capsule = TaskCapsuleHud.Build(safe, tracker);
            sales = SalesHud.Build(safe, wallet);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(root);

        [Test]
        public void CoinHudPunchesAndRollsThenSettlesAfterAdvance()
        {
            wallet.CollectCoins(10);
            sales.Advance(0.08f);
            Assert.That(sales.IsPunching, Is.True);
            Assert.That(sales.PunchScale, Is.GreaterThan(1.05f));
            Assert.That(sales.DisplayedCoins, Is.LessThan(10));
            sales.Advance(0.4f);
            Assert.That(sales.IsPunching, Is.False);
            Assert.That(sales.PunchScale, Is.EqualTo(1f));
            Assert.That(sales.DisplayedCoins, Is.EqualTo(10));
            Assert.That(sales.GetComponent<Text>().text, Does.Contain("10"));
        }

        [Test]
        public void SpendingUpdatesTheAuthoritativeNumberWithoutIncomeFeedback()
        {
            wallet.CollectCoins(30);
            sales.Advance(0.4f);
            wallet.TrySpend(30);
            sales.Advance(0.08f);
            Text label = sales.GetComponent<Text>();
            Assert.That(label.text, Is.EqualTo("0"));
            Assert.That(label.color, Is.EqualTo(HudChrome.Ink));
            Assert.That(sales.IsPunching, Is.True);
            sales.Advance(0.4f);
            Assert.That(sales.IsPunching, Is.False);
        }

        [Test]
        public void StarTaskAndCounterPunchThenSettleAfterAdvance()
        {
            tracker.Advance(2f);
            stars.Advance(0f);
            capsule.Advance(0f);
            Assert.That(stars.IsPunching, Is.False);
            Assert.That(capsule.IsProgressPunching, Is.False);
            Assert.That(stock.IsPunching, Is.False);

            grill.Advance(12f);
            Assert.That(inventory.TryCollectFrom(grill), Is.True);
            tracker.Advance(0.01f);
            Assert.That(tracker.Stars, Is.Zero);
            tracker.RecordMilestone(ShopGoalKind.UpgradeGrill);
            stars.Advance(0.08f);
            capsule.Advance(0.08f);
            Assert.That(tracker.Stars, Is.EqualTo(2));
            Assert.That(stars.IsPunching, Is.True);
            Assert.That(stars.PunchScale, Is.GreaterThan(1.05f));
            Assert.That(capsule.IsProgressPunching, Is.True);
            Assert.That(capsule.ProgressPunchScale, Is.GreaterThan(1.05f));
            stars.Advance(0.4f);
            capsule.Advance(0.4f);
            Assert.That(stars.IsPunching, Is.False);
            Assert.That(capsule.IsProgressPunching, Is.False);
            Assert.That(stars.PunchScale, Is.EqualTo(1f));

            Assert.That(stock.TryPlaceFrom(inventory), Is.True);
            stock.Advance(0.08f);
            Assert.That(stock.IsPunching, Is.True);
            Assert.That(stock.PunchScale, Is.GreaterThan(1.05f));
            stock.Advance(0.4f);
            Assert.That(stock.IsPunching, Is.False);
            Assert.That(stock.PunchScale, Is.EqualTo(1f));
        }
    }
}
