using System.Linq;
using BurgerShop.Economy;
using BurgerShop.Persistence;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using BurgerShop.UI;
using NUnit.Framework;
using UnityEngine;

namespace BurgerShop.Tests.EditMode
{
    public sealed class Spec080OpeningRevisionTests
    {
        GameObject root;
        SessionGoalTracker goals;
        RestaurantWallet wallet;
        GrillUpgradeZone grill;
        [SetUp] public void SetUp()
        {
            root=new GameObject("Spec080");wallet=root.AddComponent<RestaurantWallet>();
            var player=new GameObject("Player").AddComponent<BurgerInventory>();player.transform.SetParent(root.transform);player.Configure();
            var station=root.AddComponent<ProductionStation>();station.Configure(root.transform,null,null);
            grill=root.AddComponent<GrillUpgradeZone>();grill.Configure(station,wallet,player,root.transform);
            goals=root.AddComponent<SessionGoalTracker>();goals.Configure(player,station,null,null,wallet,null);
            root.AddComponent<GrowthUpgrades>().Configure(wallet,goals,player,null);
        }
        [TearDown] public void TearDown(){Object.DestroyImmediate(root);ShopLayout.CompactStart=false;}
        [Test] public void FirstSaleAwardsOnceAndRequiresExplicitClick()
        {
            goals.RecordMilestone(ShopGoalKind.ServeCustomers);
            Assert.That(goals.Stars,Is.EqualTo(2));Assert.That(goals.Rank,Is.EqualTo(1));Assert.That(grill.Level,Is.EqualTo(1));
            goals.RecordMilestone(ShopGoalKind.ServeCustomers);Assert.That(goals.Stars,Is.EqualTo(2));
            Assert.That(goals.TryUpgradeRank(1),Is.True);Assert.That(goals.Stars,Is.Zero);
            Assert.That(goals.TryUpgradeRank(1),Is.False);Assert.That(goals.Rank,Is.EqualTo(2));
        }
        [Test] public void OldGrillBitDoesNotReplaceActualFirstSale()
        {
            goals.Restore(1,0,0,8,1);Assert.That(goals.CanUpgrade,Is.False);
            goals.RecordMilestone(ShopGoalKind.ServeCustomers);Assert.That(goals.CanUpgrade,Is.True);Assert.That(goals.Stars,Is.EqualTo(10));
        }
        [TestCase(true)][TestCase(false)] public void CleanAndInvestmentWorkInEitherOrder(bool cleanFirst)
        {
            goals.Restore(2,0,0);wallet.RestoreProgress(30,1);
            if(cleanFirst)goals.NotifyCleanTable();
            Assert.That(grill.TryUpgrade(1),Is.True);
            if(!cleanFirst)goals.NotifyCleanTable();
            Assert.That(goals.Stars,Is.EqualTo(4));Assert.That(wallet.Coins,Is.Zero);
            goals.NotifyCleanTable();Assert.That(goals.Stars,Is.EqualTo(4));Assert.That(goals.TryUpgradeRank(2),Is.True);
        }
        [Test] public void CleanPointsToRealThirtyCoinUpgradeAndExactShortfall()
        {
            goals.Restore(2,0,0);goals.NotifyCleanTable();wallet.RestoreProgress(10,1);
            Assert.That(goals.ShowsInvestment,Is.True);Assert.That(goals.Investments.Current.Cost,Is.EqualTo(30));
            Assert.That(goals.Investments.Detail,Does.Contain("3s → 2s").And.Contain("20 more").And.Contain("2 stars to upgrade"));
            Assert.That(goals.CapsuleProgress,Is.EqualTo(2));Assert.That(goals.CapsuleRequired,Is.EqualTo(4));
        }
        [Test] public void CompletedGrillIsNeverRecommended()
        {
            goals.Restore(2,0,0,2,2);grill.RestoreLevel(3);goals.Investments.Refresh(true);
            Assert.That(goals.Investments.Current,Is.Null);
            Assert.That(goals.CapsuleTitle,Does.Not.Contain("Pick up"));
        }
        [TestCase(0,1,false,0)][TestCase(1,1,true,2)][TestCase(4,2,true,2)]
        public void OldRankOneMigrationDistinguishesSalesFromUpgrade(int sales,int level,bool complete,int expectedStars)
        {
            var save=new RestaurantSaveData{version=17,shopRank=1,completedSales=sales,grillLevel=level,upgradeStars=0,milestoneMask=level>1?1:0};
            Assert.That(save.ResolvedFirstOrder,Is.EqualTo(complete));Assert.That(save.OpeningStars,Is.EqualTo(expectedStars));
            var migrated=new RestaurantSaveData{version=18,shopRank=2,firstOrderComplete=save.ResolvedFirstOrder,upgradeStars=0};
            Assert.That(migrated.OpeningStars,Is.Zero,"no reward replay after migration");
        }
        [Test] public void StarBudgetChangesOnlyFirstGate()
        {
            int[] expected={2,4,5,6,6,7,8,9,10,11,12,14,16,17,20};
            Assert.That(Enumerable.Range(1,15).Select(ShopRanks.StarCap),Is.EqualTo(expected));
            Assert.That(expected.Take(14).Sum(),Is.EqualTo(127));Assert.That(expected.Sum(),Is.EqualTo(147));
        }
        [Test] public void RetiredThresholdsCannotMintStarsButOldAwardsRemain()
        {
            for(int rank=10;rank<=15;rank++)
            {
                goals.Restore(rank,0,0,7,ShopRanks.MilestoneMask);
                goals.EvaluateStarGateTasks();goals.RecordMilestone(ShopGoalKind.StatLinePeakTen);
                Assert.That(ShopRanks.Goals(rank),Is.Empty);Assert.That(goals.Stars,Is.EqualTo(7));
                Assert.That(goals.MilestoneMask,Is.EqualTo(ShopRanks.MilestoneMask));
            }
        }
        [Test] public void HydrationCannotAwardFirstOrder()
        {
            root.AddComponent<RestaurantPersistence>();goals.RecordMilestone(ShopGoalKind.ServeCustomers);
            Assert.That(goals.FirstOrderComplete,Is.False);Assert.That(goals.Stars,Is.Zero);
        }
    }
}
