using BurgerShop.Economy;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using BurgerShop.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace BurgerShop.Tests.EditMode
{
    public sealed class Spec070RankUpClickTests
    {
        GameObject root;
        SessionGoalTracker goals;
        RestaurantWallet wallet;
        DiningTable table;
        TrashInventory trash;
        StarProgressHud stars;
        TaskCapsuleHud capsule;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Spec070");
            wallet = root.AddComponent<RestaurantWallet>();
            goals = root.AddComponent<SessionGoalTracker>();
            goals.Configure(null, null, null, null, wallet, null);
            table = DiningTable.Create(root.transform, ShopLayout.Tables[0]);
            trash = root.AddComponent<TrashInventory>();
            table.BindCollector(trash);
            var canvas = new GameObject("Hud", typeof(RectTransform), typeof(Canvas));
            canvas.transform.SetParent(root.transform, false);
            stars = StarProgressHud.Build(canvas.transform, goals);
            capsule = TaskCapsuleHud.Build(canvas.transform, goals);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(root);

        void Refresh()
        {
            goals.Advance(0f);
            capsule.RefreshNow();
            stars.RefreshNow();
        }

        string CapsuleCopy => capsule.transform.Find("TaskTitle").GetComponent<Text>().text;
        string RankAction => stars.transform.Find("RankAction").GetComponent<Text>().text;

        [Test]
        public void RankOneStillNeedsTheUpgradeClick()
        {
            goals.Restore(1, 0, 0, ShopRanks.StarCap(1), 1, firstOrder:true);
            Refresh();
            Assert.That(goals.CanUpgrade, Is.True);
            Assert.That(goals.Rank, Is.EqualTo(1));
            goals.Advance(0.01f);
            Assert.That(goals.Rank, Is.EqualTo(1));
            Assert.That(goals.TryUpgradeRank(1), Is.True);
            Assert.That(goals.Rank, Is.EqualTo(2));
            Assert.That(goals.Title, Is.EqualTo(ShopRanks.Goals(2)[0].Title));
        }

        [Test]
        public void ClearingAUsedTableWaitsForUpgradeClickBeforeHireStaff()
        {
            goals.Restore(2, 0, 0, ShopRanks.StarCap(2) - 2);
            Refresh();
            Assert.That(goals.Title, Is.EqualTo("Clear a used dining table"));
            Assert.That(ShopRanks.StarCap(2), Is.EqualTo(4));
            Assert.That(ShopLayout.Tables.Length, Is.EqualTo(2), "starter dining is two tables");
            Assert.That(TableUpgradeBoard.StarterTableCount, Is.EqualTo(2));

            table.LeaveMealTrash(0);
            Assert.That(table.TryPickupTrash(trash), Is.True);
            Assert.That(goals.Rank, Is.EqualTo(2), "partial pickup must not rank up");
            Assert.That(goals.MilestoneComplete, Is.False);
            while (table.TrashCount > 0)
                Assert.That(table.TryPickupTrash(trash), Is.True);

            Refresh();
            Assert.That(table.TrashCount, Is.Zero);
            Assert.That(goals.Rank, Is.EqualTo(2));
            Assert.That(goals.Stars, Is.EqualTo(ShopRanks.StarCap(2)));
            Assert.That(goals.CanUpgrade, Is.True);
            Assert.That(goals.Allows(3), Is.False);
            Assert.That(goals.CapsuleTitle, Is.EqualTo(ShopRanks.RankUpCapsule(2)));
            Assert.That(CapsuleCopy, Is.EqualTo(ShopRanks.RankUpCapsule(2)));
            Assert.That(goals.StarLabel, Is.EqualTo("Lv.2 Ready"));
            Assert.That(goals.GuideCopy, Is.EqualTo("Upgrade"));
            Assert.That(RankAction, Is.EqualTo("Upgrade"));

            Assert.That(goals.TryUpgradeRank(2), Is.True);
            Refresh();
            Assert.That(goals.Rank, Is.EqualTo(3));
            Assert.That(goals.Allows(3), Is.True);
            Assert.That(goals.CanUpgrade, Is.False);
            Assert.That(goals.Title, Is.EqualTo("Let staff complete an order"));
            Assert.That(goals.CapsuleTitle, Does.Contain("Hire your first employee").And.Contain("50"));
            Assert.That(CapsuleCopy, Does.Contain("Hire your first employee"));
            Assert.That(goals.Opening.IsActive, Is.False);
        }

        [Test]
        public void ShortStarsStayShortOnTickAndStillNeedAClick()
        {
            goals.Restore(2, 0, 0, 2, 1 << 1);
            Assert.That(goals.MilestoneComplete, Is.True);
            Assert.That(goals.Stars, Is.LessThan(goals.StarCap));
            Assert.That(goals.CanUpgrade, Is.False);

            goals.Advance(0.01f);
            Refresh();

            Assert.That(goals.Rank, Is.EqualTo(2));
            Assert.That(goals.Stars, Is.EqualTo(2));
            Assert.That(goals.CanUpgrade, Is.False);
            goals.AddUpgradeStars();
            Refresh();
            Assert.That(goals.CanUpgrade, Is.True);
            Assert.That(goals.TryUpgradeRank(2), Is.True);
            Assert.That(goals.Rank, Is.EqualTo(3));
        }

        [Test]
        public void WorkerOrderFillsStarsAndWaitsForUpgradeClick()
        {
            goals.Restore(3, 0, 0);
            Refresh();
            Assert.That(goals.Title, Is.EqualTo("Let staff complete an order"));
            Assert.That(ShopRanks.StarCap(3), Is.EqualTo(5));
            Assert.That(goals.CanUpgrade, Is.False);

            goals.RecordMilestone(ShopGoalKind.WorkerOrder);
            goals.RecordMilestone(ShopGoalKind.WorkerOrder);
            Refresh();

            Assert.That(goals.Rank, Is.EqualTo(3));
            Assert.That(goals.Stars, Is.EqualTo(2));
            Assert.That(goals.CanUpgrade, Is.False);
            Assert.That(goals.Allows(4), Is.False);
            goals.AddUpgradeStars();
            goals.AddUpgradeStars();
            Refresh();
            Assert.That(goals.CanUpgrade, Is.True);
            Assert.That(goals.CapsuleTitle, Is.EqualTo(ShopRanks.RankUpCapsule(3)));
            Assert.That(CapsuleCopy, Is.EqualTo(ShopRanks.RankUpCapsule(3)));
            Assert.That(goals.GuideCopy, Is.EqualTo("Upgrade"));
            Assert.That(RankAction, Is.EqualTo("Upgrade"));
            Assert.That(goals.StarLabel, Is.EqualTo("Lv.3 Ready"));

            Assert.That(goals.TryUpgradeRank(3), Is.True);
            Refresh();
            Assert.That(goals.Rank, Is.EqualTo(4));
            Assert.That(goals.Title, Is.EqualTo(ShopRanks.Goals(4)[0].Title));
            Assert.That(goals.Title, Is.Not.Null.And.Not.Empty);
            Assert.That(goals.CapsuleTitle, Is.EqualTo(ShopRanks.Goals(4)[0].Title));
            Assert.That(CapsuleCopy, Is.EqualTo(ShopRanks.Goals(4)[0].Title));
            Assert.That(goals.Opening.IsActive, Is.False);
            Assert.That(ShopRanks.Goals(4)[0].Kind, Is.EqualTo(ShopGoalKind.ExtraProduction));
            Assert.That(ShopLayout.Tables.Length, Is.EqualTo(2));
        }

        [Test]
        public void ContentGoalsFollow080FirstSaleRevision()
        {
            Assert.That(ShopRanks.Goals(1)[0].Kind, Is.EqualTo(ShopGoalKind.ServeCustomers));
            Assert.That(ShopRanks.Goals(2)[0].Kind, Is.EqualTo(ShopGoalKind.CleanTable));
            Assert.That(ShopRanks.Goals(3)[0].Kind, Is.EqualTo(ShopGoalKind.WorkerOrder));
            Assert.That(ShopRanks.Goals(4)[0].Title, Is.EqualTo("Produce on the second grill"));
            Assert.That(ShopRanks.NextUnlock(3), Does.Contain("Second grill"));
            Assert.That(ShopLayout.Tables.Length, Is.EqualTo(2));
        }
    }
}
