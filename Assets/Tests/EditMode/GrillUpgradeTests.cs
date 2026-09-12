using BurgerShop.Economy;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using NUnit.Framework;
using UnityEngine;

namespace BurgerShop.Tests.EditMode
{
    public sealed class GrillUpgradeTests
    {
        GameObject root;
        ProductionStation grill;
        RestaurantWallet wallet;
        BurgerInventory player;
        GrillUpgradeZone upgrade;
        Transform output;
        Transform[] lamps;
        Transform[] extras;
        StationUpgradeFeedback feedback;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("UpgradeTest");
            grill = root.AddComponent<ProductionStation>();
            output = new GameObject("Output").transform;
            output.SetParent(root.transform);
            grill.Configure(output, null, null);
            wallet = root.AddComponent<RestaurantWallet>();
            GameObject carrier = new GameObject("Player");
            carrier.transform.SetParent(root.transform);
            player = carrier.AddComponent<BurgerInventory>();
            player.Configure();
            Transform point = new GameObject("UpgradeSpot").transform;
            point.SetParent(root.transform);
            point.position = new Vector3(2f, 0f, -2.8f);
            lamps = new Transform[2];
            for (int i = 0; i < lamps.Length; i++)
            {
                lamps[i] = new GameObject("Lamp").transform;
                lamps[i].SetParent(root.transform);
            }
            Transform visual = new GameObject("GrillVisual").transform;
            visual.SetParent(root.transform, false);
            extras = new Transform[2];
            for (int i = 0; i < extras.Length; i++)
            {
                extras[i] = new GameObject("Burner_" + (i + 1)).transform;
                extras[i].SetParent(visual, false);
            }
            feedback = StationUpgradeFeedback.Attach(visual, extras);
            upgrade = root.AddComponent<GrillUpgradeZone>();
            upgrade.Configure(grill, wallet, player, point, null, lamps, feedback);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(root);

        void Enter() => player.transform.position = upgrade.UpgradePosition + Vector3.up;

        void Leave()
        {
            player.transform.position = Vector3.zero;
            upgrade.Advance(0.1f);
        }

        void Wait(float seconds)
        {
            for (int i = 0; i < Mathf.CeilToInt(seconds * 60f); i++) upgrade.Advance(1f / 60f);
        }

        [Test]
        public void InvalidOrUnaffordableSpendingNeverChangesWalletOrSales()
        {
            wallet.RecordSale(30);
            int notifications = 0;
            wallet.CoinsSpent += _ => notifications++;
            Assert.That(wallet.TrySpend(-1), Is.False);
            Assert.That(wallet.TrySpend(0), Is.False);
            Assert.That(wallet.TrySpend(31), Is.False);
            Assert.That(wallet.Coins, Is.EqualTo(30));
            Assert.That(wallet.TrySpend(30), Is.True);
            Assert.That(wallet.Coins, Is.Zero);
            Assert.That(wallet.CompletedSales, Is.EqualTo(1));
            Assert.That(notifications, Is.EqualTo(1));
            Assert.That(wallet.TrySpend(1), Is.False);
        }

        [Test]
        public void InsufficientFundsAndOutOfRangeDoNotStartPurchase()
        {
            wallet.RecordSale(29);
            Enter();
            Wait(5f);
            Assert.That(upgrade.Level, Is.EqualTo(1));
            Assert.That(upgrade.MissingCoins, Is.EqualTo(1));
            Assert.That(upgrade.Progress, Is.Zero);
            wallet.RecordSale(1);
            Leave();
            Wait(5f);
            Assert.That(wallet.Coins, Is.EqualTo(30));
            Assert.That(grill.ProductionSeconds, Is.EqualTo(3f));
        }

        [Test]
        public void HoldToBuyConsumesExactPriceAndLightsGrill()
        {
            wallet.RecordSale(40);
            Enter();
            Wait(0.75f);
            Assert.That(upgrade.Progress, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(wallet.Coins, Is.EqualTo(40));
            Wait(0.75f);
            Assert.That(upgrade.Level, Is.EqualTo(2));
            Assert.That(wallet.Coins, Is.EqualTo(10));
            Assert.That(wallet.CompletedSales, Is.EqualTo(1));
            Assert.That(grill.ProductionSeconds, Is.EqualTo(2f));
            Assert.That(grill.Capacity, Is.EqualTo(6));
            Assert.That(lamps[0].gameObject.activeSelf, Is.True);
            Assert.That(lamps[1].gameObject.activeSelf, Is.False);
            Assert.That(upgrade.NextCost, Is.EqualTo(60));
        }

        [Test]
        public void StayingOnSpotCannotChainPurchasesAndMaxLevelStopsSpending()
        {
            wallet.RecordSale(120);
            Enter();
            Wait(20f);
            Assert.That(upgrade.Level, Is.EqualTo(2));
            Assert.That(wallet.Coins, Is.EqualTo(90));
            Leave();
            Enter();
            Wait(1.5f);
            Assert.That(upgrade.Level, Is.EqualTo(3));
            Assert.That(grill.ProductionSeconds, Is.EqualTo(1.5f));
            Assert.That(grill.Capacity, Is.EqualTo(8));
            Assert.That(wallet.Coins, Is.EqualTo(30));
            Assert.That(lamps[1].gameObject.activeSelf, Is.True);
            Leave();
            Enter();
            Wait(20f);
            Assert.That(upgrade.IsMaxLevel, Is.True);
            Assert.That(upgrade.NextCost, Is.Zero);
            Assert.That(wallet.Coins, Is.EqualTo(30));
        }

        [Test]
        public void LeavingLosingFundsAndDisablingCancelPartialHold()
        {
            wallet.RecordSale(30);
            Enter();
            Wait(1f);
            Leave();
            Enter();
            Wait(0.6f);
            Assert.That(upgrade.Level, Is.EqualTo(1));
            wallet.TrySpend(10);
            upgrade.Advance(0.1f);
            Assert.That(upgrade.Progress, Is.Zero);
            wallet.RecordSale(10);
            Wait(0.6f);
            grill.enabled = false;
            upgrade.Advance(0.1f);
            Assert.That(upgrade.Progress, Is.Zero);
            grill.enabled = true;
            Wait(0.6f);
            upgrade.enabled = false;
            upgrade.Advance(5f);
            Assert.That(upgrade.Progress, Is.Zero);
            upgrade.enabled = true;
            Wait(1.5f);
            Assert.That(wallet.Coins, Is.Zero);
            Assert.That(upgrade.Level, Is.EqualTo(2));
        }

        [Test]
        public void PauseAndSingleLongFrameCannotInstantlyPurchase()
        {
            wallet.RecordSale(30);
            Enter();
            upgrade.Advance(0f);
            upgrade.Advance(-10f);
            Assert.That(upgrade.Progress, Is.Zero);
            upgrade.Advance(100f);
            Assert.That(upgrade.Progress, Is.LessThan(0.1f));
            Assert.That(wallet.Coins, Is.EqualTo(30));
            Wait(1.4f);
            Assert.That(upgrade.Level, Is.EqualTo(2));
        }

        [Test]
        public void SpendingNotificationCannotReenterAndBuyAgain()
        {
            wallet.RecordSale(120);
            wallet.CoinsSpent += _ => Wait(5f);
            Enter();
            Wait(1.5f);
            Assert.That(wallet.Coins, Is.EqualTo(90));
            Assert.That(upgrade.Level, Is.EqualTo(2));
        }

        [Test]
        public void FasterProductionPreservesProgressAndExistingStock()
        {
            grill.Advance(4.5f);
            Assert.That(grill.Stock, Is.EqualTo(1));
            Assert.That(grill.NormalizedProgress, Is.EqualTo(0.5f).Within(0.001f));
            Transform firstBurger = output.GetChild(0);
            grill.SetProductionSeconds(2f);
            Assert.That(grill.Stock, Is.EqualTo(1));
            Assert.That(output.GetChild(0), Is.SameAs(firstBurger));
            Assert.That(grill.NormalizedProgress, Is.EqualTo(0.5f).Within(0.001f));
            grill.Advance(0.99f);
            Assert.That(grill.Stock, Is.EqualTo(1));
            grill.Advance(0.02f);
            Assert.That(grill.Stock, Is.EqualTo(2));
            Assert.That(output.childCount, Is.EqualTo(2));
        }

        [Test]
        public void UpgradingFullGrillDoesNotGrantFreeStockOrStoredProductionTime()
        {
            grill.Advance(100f);
            grill.SetProductionSeconds(1.5f);
            Assert.That(grill.Stock, Is.EqualTo(4));
            grill.TryTakeBurger();
            grill.Advance(1.49f);
            Assert.That(grill.Stock, Is.EqualTo(3));
            grill.Advance(0.02f);
            Assert.That(grill.Stock, Is.EqualTo(4));
            Assert.That(output.childCount, Is.EqualTo(4));
        }

        [Test]
        public void InvalidProductionIntervalsDoNotChangeProgress()
        {
            grill.Advance(1.5f);
            foreach (float invalid in new[] { 0f, -1f, float.NaN, float.PositiveInfinity })
                Assert.Throws<System.ArgumentOutOfRangeException>(() => grill.SetProductionSeconds(invalid));
            Assert.That(grill.ProductionSeconds, Is.EqualTo(3f));
            Assert.That(grill.NormalizedProgress, Is.EqualTo(0.5f).Within(0.001f));
        }

        [Test]
        public void UpgradeRaisesStockCapFromFourToSixToEight()
        {
            Assert.That(ProductionStation.CapacityForLevel(1), Is.EqualTo(4));
            Assert.That(ProductionStation.CapacityForLevel(2), Is.EqualTo(6));
            Assert.That(ProductionStation.CapacityForLevel(3), Is.EqualTo(8));
            grill.Advance(100f);
            Assert.That(grill.Stock, Is.EqualTo(4));
            wallet.RecordSale(90);
            Enter();
            Wait(1.5f);
            Assert.That(grill.Capacity, Is.EqualTo(6));
            grill.Advance(100f);
            Assert.That(grill.Stock, Is.EqualTo(6));
            Leave();
            Enter();
            Wait(1.5f);
            Assert.That(grill.Capacity, Is.EqualTo(8));
            grill.Advance(100f);
            Assert.That(grill.Stock, Is.EqualTo(8));
        }

        [Test]
        public void UpgradeRevealsANewPartPopsLvThenPunchSettles()
        {
            Assert.That(extras[0].gameObject.activeSelf, Is.False);
            Assert.That(feedback.ShowsMax, Is.False);
            wallet.RecordSale(30);
            Enter();
            Wait(1.5f);
            Assert.That(upgrade.Level, Is.EqualTo(2));
            Assert.That(lamps[0].gameObject.activeSelf, Is.True);
            Assert.That(extras[0].gameObject.activeSelf, Is.True);
            Assert.That(extras[1].gameObject.activeSelf, Is.False);
            Assert.That(feedback.ActivePartCount, Is.EqualTo(1));
            Assert.That(feedback.PopupText, Is.EqualTo("LV2"));
            Assert.That(feedback.IsPopupPlaying, Is.True);
            Assert.That(feedback.IsPunching, Is.True);
            upgrade.Advance(0.08f);
            Assert.That(feedback.PunchScale, Is.GreaterThan(1.02f));
            upgrade.Advance(0.4f);
            Assert.That(feedback.IsPunching, Is.False);
            Assert.That(feedback.PunchScale, Is.EqualTo(1f).Within(0.001f));
            Assert.That(extras[0].gameObject.activeSelf, Is.True);
        }

        [Test]
        public void MaxUpgradeLeavesStaticMaxAndRestoreDoesNotReplay()
        {
            wallet.RecordSale(90);
            Enter();
            Wait(1.5f);
            Leave();
            Enter();
            Wait(1.5f);
            Assert.That(upgrade.IsMaxLevel, Is.True);
            Assert.That(extras[1].gameObject.activeSelf, Is.True);
            Assert.That(feedback.PopupText, Is.EqualTo("LV3"));
            Assert.That(feedback.ShowsMax, Is.True);
            upgrade.Advance(1f);
            Assert.That(feedback.IsPopupPlaying, Is.False);
            Assert.That(feedback.IsPunching, Is.False);
            Assert.That(feedback.ShowsMax, Is.True);
            Wait(5f);
            Assert.That(feedback.IsPunching, Is.False);
            Assert.That(feedback.IsPopupPlaying, Is.False);
            Assert.That(wallet.Coins, Is.Zero);

            upgrade.RestoreLevel(1);
            Assert.That(extras[0].gameObject.activeSelf, Is.False);
            Assert.That(feedback.ShowsMax, Is.False);
            Assert.That(feedback.IsPunching, Is.False);
            upgrade.RestoreLevel(3);
            Assert.That(extras[0].gameObject.activeSelf, Is.True);
            Assert.That(extras[1].gameObject.activeSelf, Is.True);
            Assert.That(feedback.ShowsMax, Is.True);
            Assert.That(feedback.IsPunching, Is.False);
            Assert.That(feedback.IsPopupPlaying, Is.False);
        }
    }
}
