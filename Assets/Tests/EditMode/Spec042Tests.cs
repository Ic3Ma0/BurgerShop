using BurgerShop.Economy;
using BurgerShop.Persistence;
using BurgerShop.Restaurant;
using BurgerShop.UI;
using NUnit.Framework;
using UnityEngine;

namespace BurgerShop.Tests.EditMode
{
    public sealed class Spec042Tests
    {
        GameObject root;
        SessionGoalTracker goals;
        RestaurantWallet wallet;
        [SetUp] public void Setup()
        {
            root=new GameObject("Spec042");
            wallet=root.AddComponent<RestaurantWallet>();
            goals=root.AddComponent<SessionGoalTracker>();
            goals.Configure(null,null,null,null,wallet,null);
        }
        [TearDown] public void Cleanup()=>Object.DestroyImmediate(root);

        [Test] public void ContentRanksUseAStarGateWithoutFillingFromMilestones()
        {
            int total=0;
            for(int rank=1;rank<=ShopRanks.StarGateEnd;rank++)
            {
                int requirement=ShopRanks.StarCap(rank);total+=requirement;
                goals.Restore(rank,0,0,requirement-1);
                Assert.That(goals.TryUpgradeRank(rank),Is.False,"short stars, rank "+rank);
                if(rank<ShopRanks.ContentEnd)
                {
                    goals.Restore(rank,0,0);
                    var kind=ShopRanks.Goals(rank)[0].Kind;
                    goals.RecordMilestone(kind);goals.RecordMilestone(kind);
                    Assert.That(goals.Rank,Is.EqualTo(rank),"milestone must not auto-rank "+rank);
                    Assert.That(goals.Stars,Is.EqualTo(2),"milestone awards +2 only "+rank);
                    Assert.That(goals.CanUpgrade,Is.False,"milestone does not fill "+rank);
                }
                goals.Restore(rank,0,0,requirement);
                Assert.That(goals.TryUpgradeRank(rank),Is.True,"rank "+rank);
                Assert.That(goals.Stars,Is.Zero);
                Assert.That(goals.Rank,Is.EqualTo(rank+1));
                Assert.That(goals.TryUpgradeRank(rank),Is.False,"stale click");
            }
            Assert.That(total,Is.EqualTo(149));
        }
        [Test] public void CycleSpendsCoinsPreservesStarsAndChangesSign()
        {
            goals.Restore(16,0,0,17);wallet.RestoreProgress(499,0);
            Assert.That(goals.TryUpgradeRank(16),Is.False);
            wallet.CollectCoins(1);Assert.That(goals.TryUpgradeRank(16),Is.True);
            Assert.That(wallet.Coins,Is.Zero);Assert.That(goals.Stars,Is.EqualTo(17));
            Assert.That(goals.CycleCost,Is.EqualTo(575));Assert.That(goals.IsMaxRank,Is.False);
            StringAssert.Contains("SHOP 17",root.GetComponentInChildren<TextMesh>().text);
            StringAssert.Contains("Cash +14%",root.GetComponentInChildren<TextMesh>().text);
        }
        [Test] public void SmallCashDropsAccumulateBonusAndRestoreTheirRemainder()
        {
            goals.Restore(11,0,0);
            Assert.That(goals.AddIncomeBonus(10),Is.EqualTo(10));
            Assert.That(goals.IncomeRemainder,Is.EqualTo(10));
            goals.Restore(11,0,0,0,0,false,goals.IncomeRemainder);
            int cash=0;for(int i=0;i<4;i++)cash+=goals.AddIncomeBonus(10);
            Assert.That(cash,Is.EqualTo(41));Assert.That(goals.IncomeRemainder,Is.Zero);
            goals.Restore(12,0,0);Assert.That(goals.AddIncomeBonus(50),Is.EqualTo(52));
        }
        [Test] public void LegacySaveMigrationExemptsPastAndCurrentWithoutRewardingAgain()
        {
            var old=new RestaurantSaveData{version=13,shopRank=3,grillLevel=1,coins=321,upgradeStars=7};
            Assert.That(old.IsValid,Is.True);
            Assert.That(old.ResolvedMilestones,Is.EqualTo(7));Assert.That(old.ResolvedLegacyAccess,Is.True);
            goals.Restore(old.ResolvedShopRank,0,0,old.ResolvedUpgradeStars,old.ResolvedMilestones,old.ResolvedLegacyAccess);
            goals.RecordMilestone(ShopGoalKind.WorkerOrder);
            Assert.That(goals.Stars,Is.EqualTo(7));Assert.That(goals.Allows(10),Is.True);
            var current=new RestaurantSaveData{version=14,shopRank=3,grillLevel=1,coins=321,upgradeStars=7,
                milestoneMask=goals.MilestoneMask,legacyAccess=goals.LegacyAccess,incomeRemainder=49};
            Assert.That(current.IsValid,Is.True);Assert.That(current.ResolvedMilestones,Is.EqualTo(7));
            current.milestoneMask=1<<ShopRanks.MilestoneBitCount;Assert.That(current.IsValid,Is.False);
            current.milestoneMask=1<<(ShopRanks.StatPeakTen-1);Assert.That(current.IsValid,Is.True);
            current.milestoneMask=7;current.incomeRemainder=50;Assert.That(current.IsValid,Is.False);
        }
        [Test] public void LockedDiningAlsoHidesItsInvestmentPadsAndRestoresOnUnlock()
        {
            var dining=DiningArea.Create(root.transform,ShopLayout.Tables);
            var board=root.AddComponent<TableUpgradeBoard>();board.Configure(dining,null,wallet,null);
            goals.Configure(null,null,null,null,wallet,null,dining);
            Assert.That(dining.Tables[0].gameObject.activeSelf,Is.False);
            Assert.That(board.ZoneAt(0).gameObject.activeSelf,Is.False);
            goals.Restore(2,0,0);
            Assert.That(dining.Tables[0].gameObject.activeSelf,Is.True);
            Assert.That(board.ZoneAt(0).gameObject.activeSelf,Is.True);
        }
        [Test] public void EveryStageHasEnoughPreviouslyUnlockedStarSources()
        {
            int required=0;
            for(int stage=1;stage<=ShopRanks.StarGateEnd;stage++)
            {
                required+=ShopRanks.StarCap(stage);
                int available=2*2*BurgerShop.Player.PlayerBoost.MaxLevel+stage*2;
                if(stage>=3)available+=2*2*StaffBoost.MaxTier;
                Assert.That(available,Is.GreaterThanOrEqualTo(required),"stage "+stage);
            }
        }
        [Test] public void BonusAppliesToNewGroundCashWithoutRepricingExistingPiles()
        {
            var cash=root.AddComponent<CashFloor>();cash.Configure(wallet,null,Vector3.zero);
            goals.Restore(10,0,0);var before=cash.DropAtCounter(50);
            goals.Restore(11,0,0);var after=cash.DropAtCounter(50);
            Assert.That(before.Value,Is.EqualTo(50));Assert.That(after.Value,Is.EqualTo(51));
            Assert.That(wallet.Coins,Is.Zero,"drop is not collection");
        }
        [Test] public void VersionFourteenRoundTripsWithoutRepeatingLegacyMigration()
        {
            string dir=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"BS042-"+System.Guid.NewGuid());
            try
            {
                var store=new LocalSaveStore(dir);
                var data=new RestaurantSaveData{version=13,shopRank=3,grillLevel=1,coins=987,upgradeStars=9};
                Assert.That(store.Save(data),Is.True);Assert.That(store.Load(out var old),Is.EqualTo(SaveLoadResult.Loaded));
                data.version=14;data.milestoneMask=old.ResolvedMilestones;data.legacyAccess=old.ResolvedLegacyAccess;
                data.incomeRemainder=31;data.shopRank=4;
                Assert.That(store.Save(data),Is.True);Assert.That(store.Load(out var saved),Is.EqualTo(SaveLoadResult.Loaded));
                Assert.That(saved.ResolvedMilestones,Is.EqualTo(7),"new stage is not exempted on v14 reload");
                Assert.That(saved.ResolvedIncomeRemainder,Is.EqualTo(31));
                Assert.That(saved.coins,Is.EqualTo(987));Assert.That(saved.upgradeStars,Is.EqualTo(9));
                Assert.That(saved.ResolvedLegacyAccess,Is.True);
            }
            finally {if(System.IO.Directory.Exists(dir))System.IO.Directory.Delete(dir,true);}
        }
        [Test] public void LargeRanksAndCashDoNotOverflow()
        {
            goals.Restore(int.MaxValue,0,0);wallet.RestoreProgress(int.MaxValue,0);
            Assert.That(goals.CycleCost,Is.EqualTo(int.MaxValue));
            Assert.That(goals.TryUpgradeRank(int.MaxValue),Is.False);
            Assert.That(goals.AddIncomeBonus(int.MaxValue),Is.EqualTo(int.MaxValue));
            Assert.That(new RestaurantSaveData{version=14,shopRank=int.MaxValue,grillLevel=1}.IsValid,Is.True);
        }
    }
}
