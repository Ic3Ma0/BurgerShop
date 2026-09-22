using System.Collections.Generic;
using System.IO;
using BurgerShop.Economy;
using BurgerShop.Persistence;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using BurgerShop.UI;
using NUnit.Framework;
using UnityEngine;

namespace BurgerShop.Tests.EditMode
{
    public sealed class Spec076StarGateTasksTests
    {
        static readonly ShopGoalKind[] StarGateKinds =
        {
            ShopGoalKind.StatLinePeakTen, ShopGoalKind.StatLineBreadthEight, ShopGoalKind.StaffLinePeakTwelve,
            ShopGoalKind.FacilityUpgradeAgain, ShopGoalKind.AllTablesChosen, ShopGoalKind.StatLinePeakEighteen
        };

        GameObject root;
        SessionGoalTracker goals;
        RestaurantWallet wallet;
        BoostUpgradeZone boost;
        StaffUpgradeBoard staff;
        GrowthUpgrades growth;
        BurgerInventory player;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Spec076");
            wallet = root.AddComponent<RestaurantWallet>();
            player = new GameObject("Player").AddComponent<BurgerInventory>();
            player.transform.SetParent(root.transform, false);
            player.Configure();
            boost = root.AddComponent<BoostUpgradeZone>();
            staff = root.AddComponent<StaffUpgradeBoard>();
            growth = root.AddComponent<GrowthUpgrades>();
            goals = root.AddComponent<SessionGoalTracker>();
            goals.Configure(player, null, null, null, wallet, null);
            growth.Configure(wallet, goals, player, null);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(root);

        [Test]
        public void Ac01RetiredThresholdTasksAreNoLongerScheduled()
        {
            for(int rank=10;rank<=15;rank++)Assert.That(ShopRanks.Goals(rank),Is.Empty);
        }

        [Test]
        public void Ac02FifteenTasksRequireOneAndMatchSection31Thresholds()
        {
            for (int rank = 1; rank <= 9; rank++)
            {
                ShopGoal[] listed = ShopRanks.Goals(rank);
                Assert.That(listed, Has.Length.EqualTo(1), "rank " + rank);
                Assert.That(listed[0].Required, Is.EqualTo(1), "rank " + rank);
            }

            Assert.That(ShopRanks.MeetsThreshold(ShopGoalKind.StatLinePeakTen, 9, 9, 9, 9, 1, 0), Is.False);
            Assert.That(ShopRanks.MeetsThreshold(ShopGoalKind.StatLinePeakTen, 10, 0, 0, 0, 1, 0), Is.True);
            Assert.That(ShopRanks.MeetsThreshold(ShopGoalKind.StatLineBreadthEight, 8, 8, 8, 7, 1, 0), Is.False);
            Assert.That(ShopRanks.MeetsThreshold(ShopGoalKind.StatLineBreadthEight, 8, 8, 8, 8, 1, 0), Is.True);
            Assert.That(ShopRanks.MeetsThreshold(ShopGoalKind.StaffLinePeakTwelve, 20, 20, 11, 11, 1, 0), Is.False);
            Assert.That(ShopRanks.MeetsThreshold(ShopGoalKind.StaffLinePeakTwelve, 0, 0, 12, 0, 1, 0), Is.True);
            Assert.That(ShopRanks.MeetsThreshold(ShopGoalKind.FacilityUpgradeAgain, 0, 0, 0, 0, 1, 0), Is.False);
            Assert.That(ShopRanks.MeetsThreshold(ShopGoalKind.FacilityUpgradeAgain, 0, 0, 0, 0, 2, 0), Is.True);
            Assert.That(ShopRanks.MeetsThreshold(ShopGoalKind.AllTablesChosen, 0, 0, 0, 0, 1, 5), Is.False);
            Assert.That(ShopRanks.MeetsThreshold(ShopGoalKind.AllTablesChosen, 0, 0, 0, 0, 1, 6), Is.True);
            Assert.That(ShopRanks.MeetsThreshold(ShopGoalKind.StatLinePeakEighteen, 17, 17, 17, 17, 1, 0), Is.False);
            Assert.That(ShopRanks.MeetsThreshold(ShopGoalKind.StatLinePeakEighteen, 18, 0, 0, 0, 1, 0), Is.True);

        }

        [Test]
        public void Ac03RetiredStatTenDoesNotAwardBonus()
        {
            boost.RestoreTiers(9, 9);
            staff.RestoreTiers(9, 9);
            goals.Restore(10, 0, 0, 6);
            goals.RecordMilestone(ShopGoalKind.StatLinePeakTen);
            Assert.That(goals.MilestoneMask & (1 << 9), Is.Zero, "action-only grant is forbidden");
            Assert.That(goals.Stars, Is.EqualTo(6));

            boost.RestoreTiers(10, 9);
            goals.EvaluateStarGateTasks();
            Assert.That(goals.MilestoneMask & (1 << 9), Is.Zero);
            Assert.That(goals.Stars, Is.EqualTo(6));
            Assert.That(wallet.Coins, Is.Zero, "D3: no extra coin reward");
        }

        [Test]
        public void Ac04RetiredBreadthDoesNotAwardBonus()
        {
            boost.RestoreTiers(7, 8);
            staff.RestoreTiers(7, 8);
            goals.Restore(11, 0, 0, 4);
            goals.EvaluateStarGateTasks();
            Assert.That(goals.MilestoneMask & (1 << 10), Is.Zero);

            boost.RestoreTiers(8, 8);
            goals.EvaluateStarGateTasks();
            Assert.That(goals.MilestoneMask & (1 << 10), Is.Zero, "min is still 7");
            Assert.That(goals.Stars, Is.EqualTo(4));

            staff.RestoreTiers(8, 8);
            goals.EvaluateStarGateTasks();
            Assert.That(goals.MilestoneMask & (1 << 10), Is.Zero);
            Assert.That(goals.Stars, Is.EqualTo(4));
        }

        [Test]
        public void Ac05RestoringSixthTableDoesNotCompleteRetiredTask()
        {
            TableUpgradeZone[] zones = BindTables(6);
            for (int i = 0; i < 5; i++)
                zones[i].Restore((int)TableSetId.Diner, TableSetCatalog.CostFor(TableSetId.Diner));
            goals.Restore(14, 0, 0, 3);
            goals.EvaluateStarGateTasks();
            Assert.That(goals.MilestoneComplete, Is.False);
            Assert.That(goals.Stars, Is.EqualTo(3));

            int before = goals.Stars;
            zones[5].Restore((int)TableSetId.Bistro, TableSetCatalog.CostFor(TableSetId.Bistro));
            goals.EvaluateStarGateTasks();
            Assert.That(goals.MilestoneComplete, Is.False);
            Assert.That(goals.MilestoneMask & (1 << 13), Is.Zero);
            Assert.That(goals.Stars, Is.EqualTo(before));
        }

        [Test]
        public void Ac06RecommendationReplacesSixTableTask()
        {
            BindTables(3);
            goals.Restore(14, 0, 0);
            Assert.That(goals.CapsuleTitle, Does.Contain("Upgrade"));
            Assert.That(goals.CapsuleTitle, Is.Not.Empty);
            Assert.That(goals.CapsuleTitle, Does.Not.Match(@"^Lv\.\d+$"));
        }

        [Test]
        public void Ac07V14SaveKeepsProgressAndLeavesNewBitsClear()
        {
            string dir = Path.Combine(Path.GetTempPath(), "BS076-" + System.Guid.NewGuid().ToString("N"));
            try
            {
                var store = new LocalSaveStore(dir);
                var data = new RestaurantSaveData
                {
                    version = 14, shopRank = 12, grillLevel = 1, coins = 777, upgradeStars = 9,
                    milestoneMask = 511, incomeRemainder = 0, colaLevel = 1
                };
                Assert.That(data.IsValid, Is.True);
                Assert.That(store.Save(data), Is.True);
                Assert.That(store.Load(out var loaded), Is.EqualTo(SaveLoadResult.Loaded));
                Assert.That(loaded.coins, Is.EqualTo(777));
                Assert.That(loaded.upgradeStars, Is.EqualTo(9));
                Assert.That(loaded.ResolvedShopRank, Is.EqualTo(12));
                Assert.That(loaded.milestoneMask & ~511, Is.Zero);
                Assert.That(RestaurantSaveData.CurrentVersion, Is.EqualTo(19));

                wallet.RestoreProgress(loaded.coins, 0);
                goals.Restore(loaded.ResolvedShopRank, 0, 0, loaded.ResolvedUpgradeStars,
                    loaded.ResolvedMilestones, loaded.ResolvedLegacyAccess, loaded.ResolvedIncomeRemainder);
                Assert.That(goals.Rank, Is.EqualTo(12));
                Assert.That(goals.Stars, Is.EqualTo(9));
                Assert.That(wallet.Coins, Is.EqualTo(777));
                Assert.That(goals.MilestoneMask & ~511, Is.Zero);
                Assert.That(goals.TryUpgradeRank(12), Is.False);
                while (goals.Stars < goals.StarCap) goals.AddUpgradeStars();
                Assert.That(goals.TryUpgradeRank(12), Is.True);
                Assert.That(goals.Rank, Is.EqualTo(13));
                Assert.That(wallet.Coins, Is.EqualTo(777));
            }
            finally
            {
                if (Directory.Exists(dir)) Directory.Delete(dir, true);
            }
        }

        [Test]
        public void Ac08RepeatingACompletedThresholdDoesNotGrantAgain()
        {
            boost.RestoreTiers(10, 10);
            staff.RestoreTiers(10, 10);
            goals.Restore(10, 0, 0, 4);
            goals.EvaluateStarGateTasks();
            int mask = goals.MilestoneMask;
            int stars = goals.Stars;
            goals.EvaluateStarGateTasks();
            goals.RecordMilestone(ShopGoalKind.StatLinePeakTen);
            boost.RestoreTiers(11, 11);
            goals.EvaluateStarGateTasks();
            Assert.That(goals.MilestoneMask, Is.EqualTo(mask));
            Assert.That(goals.Stars, Is.EqualTo(stars));
        }

        [Test]
        public void Ac09RanksOneThroughFifteenAlwaysShowCapsuleCopy()
        {
            for (int rank = 1; rank <= 15; rank++)
            {
                goals.Restore(rank, 0, 0);
                Assert.That(goals.CapsuleTitle, Is.Not.Null.And.Not.Empty, "rank " + rank);
                Assert.That(goals.CapsuleTitle.Trim(), Is.Not.EqualTo("Lv." + rank), "rank " + rank);
                Assert.That(goals.CapsuleTitle, Does.Not.Match(@"^Lv\.\d+$"), "rank " + rank);
            }
        }

        [Test]
        public void Ac10NextUnlockPercentsRiseFromTenThroughFifteen()
        {
            Assert.That(ShopRanks.NextUnlock(10), Does.Contain("+2%"));
            Assert.That(ShopRanks.NextUnlock(11), Does.Contain("+4%").And.Not.Contain("+2%"));
            Assert.That(ShopRanks.NextUnlock(12), Does.Contain("+6%"));
            Assert.That(ShopRanks.NextUnlock(15), Does.Contain("+12%"));
            Assert.That(ShopRanks.NextUnlock(10), Is.Not.EqualTo(ShopRanks.NextUnlock(15)));
            Assert.That(ShopRanks.NextUnlock(15), Does.Not.Contain("+2%"));
        }

        [Test]
        public void Ac11StarGatePricesAndCycleStayUntouched()
        {
            Assert.That(ShopRanks.StarGateEnd, Is.EqualTo(15));
            Assert.That(ShopRanks.CycleStartRank, Is.EqualTo(16));
            Assert.That(ShopRanks.StarSupplyCap, Is.EqualTo(248));
            Assert.That(ShopRanks.StarCap(15), Is.EqualTo(20));
            Assert.That(ShopRanks.CycleCost(16), Is.EqualTo(500));
            Assert.That(PlayerBoost.CostGrowth, Is.EqualTo(1.18d));
            Assert.That(ShopExpansion.GrillCost, Is.EqualTo(200));
            Assert.That(OfflineEarnings.MaxMinutes, Is.EqualTo(480));
            Assert.That(ShopRanks.IsMax(99), Is.False);
            Assert.That(RestaurantSaveData.CurrentVersion, Is.GreaterThanOrEqualTo(17));
            Assert.That(ShopRanks.MilestoneBitCount, Is.EqualTo(15));
            Assert.That(ShopRanks.MilestoneMask, Is.EqualTo((1 << 15) - 1));
            Assert.That(ShopRanks.CompletedThrough(15) & ~511, Is.Zero, "old-save migration must leave bits 10–15 clear");
        }

        [Test]
        public void RetiredFacilityAndStaffThresholdsDoNotAward()
        {
            boost.RestoreTiers(20, 20);
            staff.RestoreTiers(11, 11);
            goals.Restore(12, 0, 0, 2);
            goals.EvaluateStarGateTasks();
            Assert.That(goals.MilestoneMask & (1 << 11), Is.Zero, "player peak must not finish the staff task");

            staff.RestoreTiers(12, 11);
            goals.EvaluateStarGateTasks();
            Assert.That(goals.MilestoneMask & (1 << 11), Is.Zero);

            goals.Restore(13, 0, 0, 2);
            goals.EvaluateStarGateTasks();
            Assert.That(goals.MilestoneMask & (1 << 12), Is.Zero);
            growth.Restore(new[] { new FacilityLevelRecord { id = "counter-main", level = 2 } }, 1, 0, 1);
            goals.EvaluateStarGateTasks();
            Assert.That(goals.MilestoneMask & (1 << 12), Is.Zero);
            Assert.That(wallet.Coins, Is.Zero);
        }

        TableUpgradeZone[] BindTables(int count)
        {
            var zones = new TableUpgradeZone[count];
            for (int i = 0; i < count; i++)
            {
                var table = DiningTable.Create(root.transform, new Vector3(i * 3f, 0f, 0f));
                var zone = table.gameObject.AddComponent<TableUpgradeZone>();
                zone.Configure(table, i, wallet, player, table.transform, null, null);
                zones[i] = zone;
            }
            return zones;
        }
    }
}
