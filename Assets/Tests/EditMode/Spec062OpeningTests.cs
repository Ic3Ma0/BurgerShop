using BurgerShop.Economy;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using BurgerShop.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace BurgerShop.Tests.EditMode
{
    public sealed class Spec062OpeningTests : SaveIsolatedGameplayTest
    {
        GameObject root;
        BurgerInventory inventory;
        ProductionStation grill;
        CounterStock stock;
        RestaurantWallet wallet;
        CashFloor cash;
        GrillUpgradeZone upgrade;
        SessionGoalTracker tracker;
        StarProgressHud stars;
        TaskCapsuleHud capsule;
        UpgradeGuide guide;

        [SetUp]
        public void SetUpKitchen()
        {
            root = new GameObject("Spec062");
            inventory = new GameObject("Player").AddComponent<BurgerInventory>();
            inventory.transform.SetParent(root.transform);
            inventory.Configure();
            Transform output = new GameObject("Output").transform;
            output.SetParent(root.transform);
            grill = root.AddComponent<ProductionStation>();
            grill.Configure(output, null, null);
            stock = root.AddComponent<CounterStock>();
            Transform anchor = new GameObject("Anchor").transform;
            anchor.SetParent(root.transform);
            stock.Configure(anchor, null);
            wallet = root.AddComponent<RestaurantWallet>();
            cash = root.AddComponent<CashFloor>();
            cash.Configure(wallet, inventory.transform, new Vector3(0.9f, 0f, 3.3f));
            Transform point = new GameObject("GrillUpgradeSpot").transform;
            point.SetParent(root.transform);
            point.position = ShopLayout.UpgradeSpot;
            upgrade = root.AddComponent<GrillUpgradeZone>();
            upgrade.Configure(grill, wallet, inventory, point);
            tracker = root.AddComponent<SessionGoalTracker>();
            tracker.Configure(inventory, grill, stock, null, wallet, null, null, null, null, null);
            root.AddComponent<GrowthUpgrades>().Configure(wallet, tracker, inventory, null);
            var canvas = new GameObject("Hud", typeof(RectTransform), typeof(Canvas));
            canvas.transform.SetParent(root.transform, false);
            stars = StarProgressHud.Build(canvas.transform, tracker);
            capsule = TaskCapsuleHud.Build(canvas.transform, tracker);
            guide = UpgradeGuide.Build(canvas.transform, tracker, upgrade, stars);
        }

        [TearDown]
        public void TearDownKitchen() => Object.DestroyImmediate(root);

        void Refresh()
        {
            tracker.Advance(0f);
            guide.Refresh();
            capsule.RefreshNow();
            stars.RefreshNow();
        }

        string CapsuleCopy => capsule.transform.Find("TaskTitle").GetComponent<Text>().text;
        string RankAction => stars.transform.Find("RankAction").GetComponent<Text>().text;

        static void AssertNotMoneyLoop(string copy)
        {
            Assert.That(copy, Does.Not.Contain("Collect"));
            Assert.That(copy, Does.Not.Contain("Stock"));
            Assert.That(copy, Does.Not.Contain("Serve"));
            Assert.That(copy, Does.Not.Contain("Pick up"));
            Assert.That(copy, Does.Not.Contain("cash").IgnoreCase);
            Assert.That(copy, Does.Not.Contain("sell").IgnoreCase);
        }

        [Test]
        public void NewSessionPointsAtGrillUpgradeEvenWithZeroCoins()
        {
            Refresh();
            Assert.That(wallet.Coins, Is.Zero);
            Assert.That(upgrade.NextCost, Is.EqualTo(30));
            Assert.That(tracker.Title, Is.EqualTo("Upgrade the burger machine"));
            Assert.That(tracker.CapsuleTitle, Is.EqualTo(ShopRanks.Opening.GrillUpgrade(30)));
            Assert.That(CapsuleCopy, Is.EqualTo(ShopRanks.Opening.GrillUpgrade(30)));
            Assert.That(tracker.Opening.Current, Is.EqualTo(OpeningGuide.Step.Upgrade));
            Assert.That(tracker.CapsuleProgress, Is.Zero);
            Assert.That(tracker.CapsuleRequired, Is.EqualTo(30));
            Assert.That(guide.HighlightsGrill, Is.True);
            Assert.That(RankAction, Is.EqualTo("Upgrade grill"));
            upgrade.UseDirectInteraction();
            guide.Refresh();
            Assert.That(upgrade.Pad.gameObject.activeSelf, Is.True);
            AssertNotMoneyLoop(CapsuleCopy);
            Assert.That(InteractionFocus.Build(capsule.transform.parent, inventory), Is.Null);
        }

        [Test]
        public void CollectStockAndCashDoNotSwitchTheUpgradeCopy()
        {
            grill.Advance(12f);
            Assert.That(inventory.TryCollectFrom(grill), Is.True);
            Refresh();
            Assert.That(tracker.CapsuleTitle, Is.EqualTo(ShopRanks.Opening.GrillUpgrade(30)));
            Assert.That(stock.TryPlaceFrom(inventory), Is.True);
            cash.DropAtCounter(CashFloor.CounterDrop);
            Refresh();
            Assert.That(tracker.CapsuleTitle, Is.EqualTo(ShopRanks.Opening.GrillUpgrade(30)));
            Assert.That(tracker.Opening.Current, Is.EqualTo(OpeningGuide.Step.Upgrade));
            AssertNotMoneyLoop(CapsuleCopy);
        }

        [Test]
        public void CompletingTheFirstGrillUpgradeRecordsProgressAndPointsAtRankUnlock()
        {
            tracker.RecordMilestone(ShopGoalKind.ServeCustomers);
            Assert.That(tracker.MilestoneComplete, Is.False);
            wallet.CollectCoins(30);
            Assert.That(upgrade.TryUpgrade(1), Is.True);
            Refresh();
            Assert.That(upgrade.Level, Is.EqualTo(2));
            Assert.That(tracker.MilestoneComplete, Is.True);
            Assert.That(tracker.Progress, Is.EqualTo(1));
            Assert.That(tracker.CanUpgrade, Is.True);
            Assert.That(tracker.CapsuleTitle, Is.EqualTo(ShopRanks.Opening.RankUp));
            Assert.That(CapsuleCopy, Is.EqualTo(ShopRanks.Opening.RankUp));
            Assert.That(guide.HighlightsGrill, Is.False);
            Assert.That(guide.HighlightsRankHud, Is.True);
            Assert.That(RankAction, Is.EqualTo("Unlock dining"));
            AssertNotMoneyLoop(CapsuleCopy);
            Assert.That(tracker.TryUpgradeRank(1), Is.True);
            Refresh();
            Assert.That(tracker.Opening.IsActive, Is.False);
            Assert.That(tracker.CapsuleTitle, Is.EqualTo("Clear a used dining table"));
        }

        [Test]
        public void SavedRankTwoSkipsOpeningGuidance()
        {
            tracker.Restore(2, 0, 0);
            Refresh();
            Assert.That(tracker.Opening.IsActive, Is.False);
            Assert.That(tracker.CapsuleTitle, Is.EqualTo("Clear a used dining table"));
            Assert.That(guide.HighlightsGrill, Is.False);
            Assert.That(RankAction, Is.EqualTo(""));
        }

        [Test]
        public void RestoredReadyRankOneStillPointsAtDiningUnlock()
        {
            upgrade.RestoreLevel(2);
            tracker.Restore(1, 1, 1, savedStars: 4, savedMilestones: 1);
            Refresh();
            Assert.That(tracker.Opening.IsActive, Is.True);
            Assert.That(tracker.CapsuleTitle, Is.EqualTo(ShopRanks.Opening.RankUp));
            Assert.That(CapsuleCopy, Is.EqualTo(ShopRanks.Opening.RankUp));
            Assert.That(guide.HighlightsGrill, Is.False);
            Assert.That(guide.HighlightsRankHud, Is.True);
            Assert.That(RankAction, Is.EqualTo("Unlock dining"));
            AssertNotMoneyLoop(CapsuleCopy);
        }

        [Test]
        public void GrillAlreadyUpgradedSkipsOpeningUnlessRankIsReady()
        {
            upgrade.RestoreLevel(2);
            tracker.Restore(1, 0, 0, ShopRanks.StarCap(1), 1);
            Refresh();
            Assert.That(tracker.MilestoneComplete, Is.True);
            Assert.That(tracker.CanUpgrade, Is.True);
            Assert.That(tracker.Rank, Is.EqualTo(1));
            Assert.That(tracker.Opening.IsActive, Is.True);
            Assert.That(tracker.Opening.Current, Is.EqualTo(OpeningGuide.Step.RankUp));
            Assert.That(tracker.CapsuleTitle, Is.EqualTo(ShopRanks.Opening.RankUp));
            Assert.That(CapsuleCopy, Is.EqualTo(ShopRanks.Opening.RankUp));
            Assert.That(guide.HighlightsGrill, Is.False);
            Assert.That(guide.HighlightsRankHud, Is.True);
            Assert.That(RankAction, Is.EqualTo("Unlock dining"));
            AssertNotMoneyLoop(CapsuleCopy);
            Assert.That(tracker.TryUpgradeRank(1), Is.True);
            Refresh();
            Assert.That(tracker.Rank, Is.EqualTo(2));
            Assert.That(tracker.Opening.IsActive, Is.False);
        }

        [Test]
        public void CapsuleSitsCompactUnderTheRankChip()
        {
            RectTransform star = (RectTransform)stars.transform;
            RectTransform task = (RectTransform)capsule.transform;
            Assert.That(task.anchorMin, Is.EqualTo(TaskCapsuleHud.LayoutAnchor));
            Assert.That(task.anchoredPosition, Is.EqualTo(TaskCapsuleHud.LayoutPosition));
            Assert.That(task.sizeDelta, Is.EqualTo(TaskCapsuleHud.LayoutSize));
            Assert.That(task.sizeDelta.x, Is.EqualTo(star.sizeDelta.x));
            Assert.That(task.sizeDelta.y, Is.LessThan(star.sizeDelta.y));
            Assert.That(((RectTransform)capsule.transform.Find("TaskBarBack")).sizeDelta.y, Is.EqualTo(TaskCapsuleHud.ProgressHairline));
        }
    }
}
