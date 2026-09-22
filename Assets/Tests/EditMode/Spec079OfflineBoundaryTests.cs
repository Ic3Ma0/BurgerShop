using System;
using BurgerShop.Economy;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using BurgerShop.UI;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BurgerShop.Tests.EditMode
{
    public sealed class Spec079OfflineBoundaryTests
    {
        const long Seen = 600000000000000000L;
        static readonly long EightHours = TimeSpan.FromHours(8).Ticks;

        [Test]
        public void RulesCompileWithoutTheSceneAssemblyOrUnity()
        {
            var assembly = typeof(OfflineSettlement).Assembly;
            Assert.That(assembly.GetName().Name, Is.EqualTo("BurgerShop.Economy.Rules"));
            foreach (var reference in assembly.GetReferencedAssemblies())
            {
                Assert.That(reference.Name, Does.Not.StartWith("Unity"));
                Assert.That(reference.Name, Does.Not.StartWith("BurgerShop.Runtime"));
            }
        }

        [Test]
        public void FirstSessionOnlyInitializesTime()
        {
            var plan = OfflineSettlement.Calculate(123, 0, Seen, default, 1000, 3, 20, 20);
            Assert.That(plan.Coins, Is.EqualTo(123));
            Assert.That(plan.LastSeenUtcTicks, Is.EqualTo(Seen));
            Assert.That(plan.Receipt.Visible, Is.False);
        }

        [Test]
        public void PlanIsRepeatableAndOnlyConsumingItsTimestampPreventsAnotherPayment()
        {
            var receipt = new OfflineReceipt(17, 60, 1, false);
            var first = OfflineSettlement.Calculate(123, Seen, Seen + EightHours, receipt, 1000, 3, 10, 10);
            var retry = OfflineSettlement.Calculate(123, Seen, Seen + EightHours, receipt, 1000, 3, 10, 10);
            Assert.That(first.Coins, Is.EqualTo(5941));
            Assert.That(retry.Coins, Is.EqualTo(first.Coins), "computing a plan cannot consume progress");
            Assert.That(receipt.Grant, Is.EqualTo(17), "input receipt stays unchanged");
            Assert.That(first.Receipt.Grant, Is.EqualTo(5818));
            Assert.That(first.Receipt.StaffCount, Is.EqualTo(3));
            Assert.That(first.Receipt.Visible, Is.True);
            var repeated = OfflineSettlement.Calculate(first.Coins, first.LastSeenUtcTicks,
                first.LastSeenUtcTicks, first.Receipt, 1000, 3, 10, 10);
            Assert.That(repeated.Coins, Is.EqualTo(first.Coins));
            Assert.That(repeated.Receipt, Is.EqualTo(first.Receipt));
        }

        [Test]
        public void ClockRollbackKeepsUnacknowledgedReceiptAndRebasesTime()
        {
            var receipt = new OfflineReceipt(42, EightHours, 2, true);
            var plan = OfflineSettlement.Calculate(100, Seen, Seen - EightHours, receipt, 1000, 3, 20, 20);
            Assert.That(plan.Coins, Is.EqualTo(100));
            Assert.That(plan.LastSeenUtcTicks, Is.EqualTo(Seen - EightHours));
            Assert.That(plan.Receipt, Is.EqualTo(receipt));
        }

        [Test]
        public void LongAbsenceCapsMoneyButKeepsActualReceiptDuration()
        {
            long month = TimeSpan.FromDays(30).Ticks;
            var plan = OfflineSettlement.Calculate(100, Seen, Seen + month, default, 1000, 3, 20, 20);
            Assert.That(plan.Coins, Is.EqualTo(8100));
            Assert.That(plan.Receipt.Ticks, Is.EqualTo(month));
        }

        [TestCase(0, 1000)]
        [TestCase(3, 0)]
        public void NoWorkersOrNoUpgradeStillProducesZeroIncomeReceipt(int staff, int cost)
        {
            var plan = OfflineSettlement.Calculate(100, Seen, Seen + EightHours, default, cost, staff, 20, 20);
            Assert.That(plan.Coins, Is.EqualTo(100));
            Assert.That(plan.Receipt.Visible, Is.True);
            Assert.That(plan.Receipt.Grant, Is.Zero);
        }

        [Test]
        public void PositiveSettlementReplacesRatherThanAddsToThePreviousReceipt()
        {
            var old = new OfflineReceipt(42, 30, 1, true);
            var plan = OfflineSettlement.Calculate(100, Seen, Seen + EightHours, old, 1000, 3, 20, 20);
            Assert.That(plan.Receipt.Grant, Is.EqualTo(8000));
            Assert.That(plan.Receipt.Ticks, Is.EqualTo(EightHours));
        }

        [Test]
        public void ReceiptShowsOnlyTheCreditThatFitsInTheWallet()
        {
            var plan = OfflineSettlement.Calculate(long.MaxValue - 2, Seen, Seen + EightHours,
                default, int.MaxValue, 3, 20, 20);
            Assert.That(plan.Coins, Is.EqualTo(long.MaxValue));
            Assert.That(plan.Receipt.Grant, Is.EqualTo(2));
        }

        [Test]
        public void SceneQuotesRespectStaffAccessPlayerTiersAndCycleCost()
        {
            var root = new GameObject("OfflineQuoteTest");
            try
            {
                Assert.That(OfflineUpgradeCostSource.CheapestUpgrade(root.transform), Is.Zero);
                var player = root.AddComponent<BoostUpgradeZone>();
                player.RestoreTiers(20, 19);
                var staff = root.AddComponent<StaffUpgradeBoard>();
                var goals = root.AddComponent<SessionGoalTracker>();
                goals.Restore(1, 0, 0);
                Assert.That(OfflineUpgradeCostSource.CheapestUpgrade(root.transform),
                    Is.EqualTo(PlayerBoost.CostForNextTier(19)), "locked staff is not an available offer");
                goals.Restore(ShopRanks.HireRank, 0, 0);
                Assert.That(OfflineUpgradeCostSource.CheapestUpgrade(root.transform), Is.EqualTo(PlayerBoost.CostForNextTier(19)), "083: no staff means staff upgrades are ineligible");
                player.RestoreTiers(20, 20);
                staff.RestoreTiers(20, 20);
                Assert.That(OfflineUpgradeCostSource.CheapestUpgrade(root.transform), Is.Zero);
                goals.Restore(ShopRanks.ContentEnd + 1, 0, 0);
                Assert.That(OfflineUpgradeCostSource.CheapestUpgrade(root.transform), Is.EqualTo(goals.CycleCost));
                goals.Restore(int.MaxValue, 0, 0);
                Assert.That(OfflineUpgradeCostSource.CheapestUpgrade(root.transform), Is.Zero);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void SceneQuoteUsesFullTablePriceAndSkipsHiddenOrChosenTables()
        {
            var root = new GameObject("OfflineTableQuote");
            try
            {
                var table = DiningTable.Create(root.transform, Vector3.zero);
                var zone = table.gameObject.AddComponent<TableUpgradeZone>();
                zone.Configure(table, 0, null, null, table.transform, null, null);
                zone.Restore(0, 30);
                Assert.That(OfflineUpgradeCostSource.CheapestUpgrade(root.transform), Is.EqualTo(50), "083: investment credit must not shrink offline valuation");
                table.gameObject.SetActive(false);
                Assert.That(OfflineUpgradeCostSource.CheapestUpgrade(root.transform), Is.Zero);
                table.gameObject.SetActive(true);
                table.ApplySet(TableSetId.Bistro);
                Assert.That(OfflineUpgradeCostSource.CheapestUpgrade(root.transform), Is.Zero);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
