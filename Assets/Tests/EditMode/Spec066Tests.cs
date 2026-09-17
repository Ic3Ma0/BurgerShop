using BurgerShop.Economy;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using BurgerShop.UI;
using NUnit.Framework;
using UnityEngine;

namespace BurgerShop.Tests.EditMode
{
    public sealed class Spec066Tests
    {
        GameObject root;
        SessionGoalTracker goals;
        RestaurantWallet wallet;
        DiningTable table;
        TrashInventory trash;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Spec066");
            wallet = root.AddComponent<RestaurantWallet>();
            goals = root.AddComponent<SessionGoalTracker>();
            goals.Configure(null, null, null, null, wallet, null);
            table = DiningTable.Create(root.transform, ShopLayout.Tables[0]);
            trash = root.AddComponent<TrashInventory>();
            table.BindCollector(trash);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(root);

        [Test]
        public void ClearingAUsedTableWaitsForUpgradeClickBeforeHireStaff()
        {
            goals.Restore(2, 0, 0, ShopRanks.StarCap(2) - 2);
            Assert.That(goals.Title, Is.EqualTo("Clear a used dining table"));
            Assert.That(ShopRanks.StarCap(2), Is.EqualTo(4));

            table.LeaveMealTrash(0);
            Assert.That(table.TryPickupTrash(trash), Is.True);
            Assert.That(goals.Rank, Is.EqualTo(2), "partial pickup must not rank up");
            Assert.That(goals.MilestoneComplete, Is.False);
            while (table.TrashCount > 0)
                Assert.That(table.TryPickupTrash(trash), Is.True);

            Assert.That(table.TrashCount, Is.Zero);
            Assert.That(goals.Rank, Is.EqualTo(2));
            Assert.That(goals.Stars, Is.EqualTo(4));
            Assert.That(goals.CanUpgrade, Is.True);
            Assert.That(goals.CapsuleTitle, Is.EqualTo(ShopRanks.RankUpCapsule(2)));
            Assert.That(goals.StarLabel, Is.EqualTo("Lv.2 Ready"));
            Assert.That(goals.GuideCopy, Is.EqualTo("Upgrade"));
            Assert.That(goals.Allows(3), Is.False);

            Assert.That(goals.TryUpgradeRank(2), Is.True);
            Assert.That(goals.Rank, Is.EqualTo(3));
            Assert.That(goals.Title, Is.EqualTo("Let staff complete an order"));
            Assert.That(goals.CapsuleTitle, Is.EqualTo("Let staff complete an order"));
            Assert.That(goals.StarLabel, Is.EqualTo("⭐ 0/5  Need 5 more stars"));
            Assert.That(goals.Allows(3), Is.True);
            Assert.That(goals.CanUpgrade, Is.False);
        }

        [Test]
        public void ShortStarsWithCleanTableMilestoneCatchUpOnTick()
        {
            goals.Restore(2, 0, 0, 2, 1 << 1);
            Assert.That(goals.MilestoneComplete, Is.True);
            Assert.That(goals.Stars, Is.LessThan(goals.StarCap));
            Assert.That(goals.CanUpgrade, Is.False);

            goals.Advance(0.01f);

            Assert.That(goals.Rank, Is.EqualTo(2));
            Assert.That(goals.Stars, Is.EqualTo(2));
            Assert.That(goals.CanUpgrade, Is.False);
        }

        [Test]
        public void ShortStarsWithCleanTableMilestoneCatchUpOnNextClean()
        {
            goals.Restore(2, 0, 0, 2, 1 << 1);
            Assert.That(goals.MilestoneComplete, Is.True);
            Assert.That(goals.Stars, Is.EqualTo(2));

            table.LeaveMealTrash(0);
            while (table.TrashCount > 0)
                Assert.That(table.TryPickupTrash(trash), Is.True);

            Assert.That(goals.Rank, Is.EqualTo(2));
            Assert.That(goals.Stars, Is.EqualTo(2));
            Assert.That(goals.CanUpgrade, Is.False);
        }

        [Test]
        public void OtherRanksKeepAManualRankUpClick()
        {
            goals.Restore(1, 0, 0, ShopRanks.StarCap(1), 1);
            Assert.That(goals.CanUpgrade, Is.True);
            goals.Advance(0.01f);
            Assert.That(goals.Rank, Is.EqualTo(1));
            Assert.That(goals.TryUpgradeRank(1), Is.True);
            Assert.That(goals.Rank, Is.EqualTo(2));
            Assert.That(goals.Title, Is.EqualTo("Clear a used dining table"));
        }
    }
}
