using BurgerShop.Economy;
using BurgerShop.Persistence;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using BurgerShop.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace BurgerShop.Tests.EditMode
{
    public sealed class Spec075StarGateTests
    {
        static readonly int[] Caps =
        {
            4, 4, 5, 6, 6, 7, 8, 9, 10, 11, 12, 14, 16, 17, 20
        };

        GameObject root;
        SessionGoalTracker goals;
        RestaurantWallet wallet;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Spec075");
            wallet = root.AddComponent<RestaurantWallet>();
            goals = root.AddComponent<SessionGoalTracker>();
            goals.Configure(null, null, null, null, wallet, null);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(root);

        [Test]
        public void Ac01StarCapTableIsGeometricAndNonDecreasing()
        {
            for (int rank = 1; rank <= 15; rank++)
                Assert.That(ShopRanks.StarCap(rank), Is.EqualTo(Caps[rank - 1]), "rank " + rank);
            for (int rank = 2; rank <= 15; rank++)
                Assert.That(ShopRanks.StarCap(rank), Is.GreaterThanOrEqualTo(ShopRanks.StarCap(rank - 1)));
        }

        [Test]
        public void Ac02CumulativeDemandThroughSixteenStaysUnderSupplyBudget()
        {
            int total = 0;
            for (int rank = 1; rank <= 16; rank++)
                total += ShopRanks.StarCap(rank);
            Assert.That(total, Is.EqualTo(171));
            Assert.That(total, Is.LessThanOrEqualTo((int)(ShopRanks.StarSupplyCap * 0.8)));
        }

        [Test]
        public void Ac03OneStarShortDoesNotUpgradeOrSpend()
        {
            for (int rank = 1; rank <= ShopRanks.StarGateEnd; rank++)
            {
                int cap = ShopRanks.StarCap(rank);
                goals.Restore(rank, 0, 0, cap - 1);
                Assert.That(goals.TryUpgradeRank(rank), Is.False, "rank " + rank);
                Assert.That(goals.Rank, Is.EqualTo(rank), "rank " + rank);
                Assert.That(goals.Stars, Is.EqualTo(cap - 1), "rank " + rank);
            }
        }

        [Test]
        public void Ac04ExactStarsUpgradeWithoutMilestoneAndKeepRemainder()
        {
            for (int rank = 1; rank <= ShopRanks.StarGateEnd; rank++)
            {
                int cap = ShopRanks.StarCap(rank);
                goals.Restore(rank, 0, 0, cap + 3);
                Assert.That(goals.MilestoneComplete, Is.False, "milestone must not be required " + rank);
                Assert.That(goals.TryUpgradeRank(rank), Is.True, "rank " + rank);
                Assert.That(goals.Rank, Is.EqualTo(rank + 1), "rank " + rank);
                Assert.That(goals.Stars, Is.EqualTo(3), "rank " + rank);
            }
        }

        [Test]
        public void Ac05CycleFromSixteenIgnoresStarsAndUsesGold()
        {
            Assert.That(ShopRanks.CycleCost(16), Is.EqualTo(500));
            Assert.That(ShopRanks.CycleCost(17), Is.EqualTo(575));
            Assert.That(ShopRanks.CycleCost(18), Is.EqualTo(661));
            Assert.That(ShopRanks.CycleCost(19), Is.EqualTo(760));
            Assert.That(ShopRanks.CycleCost(20), Is.EqualTo(875));
            goals.Restore(16, 0, 0, 0);
            wallet.RestoreProgress(499, 0);
            Assert.That(goals.IsCycle, Is.True);
            Assert.That(goals.TryUpgradeRank(16), Is.False);
            Assert.That(wallet.Coins, Is.EqualTo(499));
            Assert.That(goals.Rank, Is.EqualTo(16));
            wallet.CollectCoins(1);
            Assert.That(goals.TryUpgradeRank(16), Is.True);
            Assert.That(wallet.Coins, Is.Zero);
            Assert.That(goals.Rank, Is.EqualTo(17));
            Assert.That(goals.Stars, Is.Zero);
        }

        [Test]
        public void Ac06PurchasesAndMilestonesAwardExactlyTwoStars()
        {
            var dining = DiningArea.Create(root.transform, ShopLayout.Tables);
            var carrier = new GameObject("StarCarrier").AddComponent<BurgerInventory>();
            carrier.transform.SetParent(root.transform, false);
            carrier.Configure();
            goals.Configure(carrier, null, null, null, wallet, null, dining);
            goals.Restore(2, 0, 0);
            dining.Tables[0].gameObject.SetActive(true);
            dining.Tables[1].gameObject.SetActive(true);

            var zone = dining.Tables[0].gameObject.AddComponent<TableUpgradeZone>();
            zone.Configure(dining.Tables[0], 0, wallet, carrier, dining.Tables[0].transform, null, null);
            wallet.RestoreProgress(500, 0);
            int before = goals.Stars;
            Assert.That(zone.TryBuySet(TableSetId.Diner, 0), Is.True);
            Assert.That(goals.Stars, Is.EqualTo(before + 2), "table change");

            var growth = root.AddComponent<GrowthUpgrades>();
            growth.Configure(wallet, goals, carrier, null);
            growth.Discover();
            before = goals.Stars;
            Assert.That(growth.TryBuy("table-1", 1), Is.True);
            Assert.That(goals.Stars, Is.EqualTo(before + 2), "facility upgrade");

            carrier.transform.position = ShopLayout.BoostPoint + Vector3.up;
            carrier.gameObject.AddComponent<CharacterController>();
            var motor = carrier.gameObject.AddComponent<PlayerMotor>();
            var boost = root.AddComponent<BoostUpgradeZone>();
            boost.Configure(wallet, carrier, motor, carrier.transform);
            before = goals.Stars;
            Assert.That(boost.TryBuySpeed(), Is.True, "attribute");
            Assert.That(goals.Stars, Is.EqualTo(before + 2), "attribute");

            before = goals.Stars;
            goals.RecordMilestone(ShopGoalKind.CleanTable);
            goals.RecordMilestone(ShopGoalKind.CleanTable);
            Assert.That(goals.Stars, Is.EqualTo(before + 2), "milestone once");
            Assert.That(goals.Stars, Is.Not.EqualTo(goals.StarCap));
            Assert.That(goals.Rank, Is.EqualTo(2));
        }

        [Test]
        public void Ac07EmptyCarryLevelAddsIndependentMoveSpeed()
        {
            Assert.That(PlayerBoost.CarryBonus(8), Is.EqualTo(PlayerBoost.CarryBonus(9)));
            Assert.That(PlayerBoost.CarryCapacity(8), Is.EqualTo(12));
            Assert.That(PlayerBoost.CarryCapacity(9), Is.EqualTo(12));
            float before = PlayerBoost.MoveSpeed(0, 8);
            float after = PlayerBoost.MoveSpeed(0, 9);
            Assert.That(after, Is.GreaterThan(before));
            Assert.That(after, Is.EqualTo(before * 1.03f).Within(0.0001f));
            Assert.That(StaffBoost.WalkSpeed(0, 9), Is.EqualTo(StaffBoost.WalkSpeed(0, 8) * 1.03f).Within(0.0001f));
        }

        [Test]
        public void Ac08UpgradeCopyShowsStarRewardAndShortage()
        {
            goals.Restore(1, 0, 0);
            Assert.That(goals.StarLabel, Does.Contain("⭐").And.Contain("0/4").And.Contain("Need 4 more stars"));
            Assert.That(goals.NextRankPreview, Does.Contain(ShopRanks.NextUnlock(1)).And.Contain("Need 4 more stars"));
            Assert.That(goals.BlockReason, Does.Contain("Need 4 more stars"));

            var canvas = new GameObject("Hud", typeof(RectTransform), typeof(Canvas));
            canvas.transform.SetParent(root.transform, false);
            var stars = StarProgressHud.Build(canvas.transform, goals);
            stars.RefreshNow();
            Assert.That(stars.transform.Find("StarBarBack/StarValue").GetComponent<Text>().text,
                Does.Contain("Need 4 more stars"));

            var player = new GameObject("HudPlayer").AddComponent<BurgerInventory>();
            player.transform.SetParent(root.transform, false);
            player.transform.position = ShopLayout.BoostPoint + Vector3.up;
            player.gameObject.AddComponent<CharacterController>();
            var motor = player.gameObject.AddComponent<PlayerMotor>();
            var boost = root.AddComponent<BoostUpgradeZone>();
            var label = new GameObject("BoostLabel").AddComponent<TextMesh>();
            label.transform.SetParent(root.transform, false);
            boost.Configure(wallet, player, motor, player.transform, label);
            Assert.That(label.text, Does.Contain(ShopRanks.StarRewardCopy));
            var hud = PlayerUpgradeHud.Build(canvas.transform, boost);
            hud.RefreshNow();
            Assert.That(hud.Popup.FirstLabel.text, Does.Contain(ShopRanks.StarRewardCopy));
            Assert.That(hud.Popup.SecondLabel.text, Does.Contain(ShopRanks.StarRewardCopy));

            var popup = TableChangePopup.Build(canvas.transform);
            popup.PaintChoices();
            Assert.That(popup.LevelLabel.text, Does.Contain(ShopRanks.StarRewardCopy));
            Assert.That(popup.SelectLabels[0].text, Does.Contain(ShopRanks.StarRewardCopy));
        }

        [Test]
        public void Ac09LegacyV14SaveKeepsRankAndStars()
        {
            var data = new RestaurantSaveData
            {
                version = 14, shopRank = 3, grillLevel = 1, coins = 321, upgradeStars = 2,
                milestoneMask = 3, legacyAccess = false, incomeRemainder = 0
            };
            Assert.That(data.IsValid, Is.True);
            goals.Restore(data.ResolvedShopRank, 0, 0, data.ResolvedUpgradeStars, data.ResolvedMilestones,
                data.ResolvedLegacyAccess, data.ResolvedIncomeRemainder);
            Assert.That(goals.Rank, Is.EqualTo(3));
            Assert.That(goals.Stars, Is.EqualTo(2));
            Assert.That(goals.TryUpgradeRank(3), Is.False);
            Assert.That(goals.Rank, Is.EqualTo(3));
            goals.AddUpgradeStars();
            Assert.That(goals.Stars, Is.EqualTo(4));
            Assert.That(goals.Rank, Is.EqualTo(3));
        }

        [Test]
        public void Ac10EconomyNumbersOutsideTheStarGateStayPut()
        {
            Assert.That(PlayerBoost.CostGrowth, Is.EqualTo(1.18d));
            Assert.That(PlayerBoost.StartingCost, Is.EqualTo(50));
            Assert.That(PlayerBoost.Costs[0], Is.EqualTo(50));
            Assert.That(PlayerBoost.MaxLevel, Is.EqualTo(20));
            Assert.That(OfflineEarnings.MaxMinutes, Is.EqualTo(480));
            Assert.That(ShopExpansion.GrillCost, Is.EqualTo(200));
            Assert.That(ShopRanks.IsMax(99), Is.False);
        }
    }
}
