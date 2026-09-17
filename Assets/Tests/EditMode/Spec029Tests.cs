using BurgerShop.Customer;
using BurgerShop.Economy;
using BurgerShop.Persistence;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using BurgerShop.UI;
using NUnit.Framework;
using UnityEngine;

namespace BurgerShop.Tests.EditMode
{
    public sealed class Spec029Tests
    {
        GameObject root;
        ShopExpansion expansion;
        DiningArea dining;
        BurgerServingZone serving;
        WorkerHiringZone hiring;
        BurgerInventory player;
        RestaurantWallet wallet;
        SessionGoalTracker tracker;
        CustomerQueue queue;
        CounterStock stock;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Spec029");
            player = new GameObject("Player").AddComponent<BurgerInventory>();
            player.transform.SetParent(root.transform);
            player.Configure();
            Transform output = new GameObject("Output").transform;
            output.SetParent(root.transform);
            var grill = root.AddComponent<ProductionStation>();
            grill.Configure(output, null, null);
            wallet = root.AddComponent<RestaurantWallet>();
            queue = root.AddComponent<CustomerQueue>();
            queue.OrderQuantityFactory = () => 1;
            queue.Configure(ShopLayout.Entrance, ShopLayout.QueueEntry, ShopLayout.QueueSlots, ShopLayout.Counter);
            Transform point = new GameObject("Circle").transform;
            point.SetParent(root.transform);
            point.position = ShopLayout.ServingCircle;
            Transform anchor = new GameObject("Anchor").transform;
            stock = root.AddComponent<CounterStock>();
            stock.Configure(anchor, null);
            CounterDropZone drop = root.AddComponent<CounterDropZone>();
            drop.Configure(stock, point);
            dining = DiningArea.Create(root.transform, ShopLayout.Tables);
            serving = root.AddComponent<BurgerServingZone>();
            serving.Configure(queue, player, wallet, point, ShopLayout.Exit, stock, dining, drop);
            hiring = root.AddComponent<WorkerHiringZone>();
            hiring.Configure(grill, serving, wallet, player, root.transform, root.transform, ShopLayout.Aisle, drop);
            expansion = ShopExpansion.Create(root.transform, dining, serving, hiring, player, wallet);
            tracker = root.AddComponent<SessionGoalTracker>();
            tracker.Configure(player, grill, stock, queue, wallet, serving, dining, null, hiring, null, expansion);
            tracker.Restore(1, 0, 0);
            expansion.ApplyRank(1);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(root);

        void Hold(FacilityUnlockZone pad, float seconds)
        {
            player.transform.position = pad.PadPosition + Vector3.up;
            for (int i = 0; i < Mathf.CeilToInt(seconds * 60f); i++) pad.Advance(1f / 60f);
        }

        [Test] public void NewShopGatesDiningAndAllExpansionUntilMilestoneAndStars()
        {
            Assert.That(tracker.StarLabel, Is.EqualTo("⭐ 0/4  Need 4 more stars"));
            Assert.That(tracker.Title, Is.EqualTo("Upgrade the burger machine"));
            Assert.That(expansion.WingPad.RankVisible, Is.False);
            Assert.That(expansion.GrillPad.RankVisible, Is.False);
            Assert.That(dining.Tables[0].gameObject.activeSelf, Is.False);
            tracker.Restore(1,0,0,4);
            Assert.That(tracker.TryUpgradeRank(1), Is.True);
            Assert.That(dining.Tables[0].gameObject.activeSelf, Is.True);
            Assert.That(expansion.WingPad.RankVisible, Is.False);
        }
        [Test] public void LockedGrillPadDoesNotAcceptInvestment()
        {
            wallet.RestoreProgress(200,0);Hold(expansion.GrillPad,3);
            Assert.That(expansion.GrillPad.Invested, Is.Zero);
            Assert.That(wallet.Coins, Is.EqualTo(200));
        }
        [Test] public void BuildingDoesNotAutomaticallyAdvanceRank()
        {
            tracker.Restore(4,0,0);wallet.RestoreProgress(200,0);
            Hold(expansion.GrillPad,3);
            Assert.That(expansion.HasExtraGrill, Is.True);
            Assert.That(tracker.Rank, Is.EqualTo(4));
            Assert.That(tracker.MilestoneComplete, Is.False);
            expansion.ExtraGrillUpgrade.Station.Advance(3.1f);
            Assert.That(tracker.MilestoneComplete, Is.True);
            Assert.That(tracker.Stars, Is.EqualTo(4));
            Assert.That(tracker.Rank, Is.EqualTo(4));
            Assert.That(tracker.CanUpgrade, Is.False);
        }
        [Test] public void RestoreDoesNotReplayRankUp()
        {
            tracker.Restore(2,0,0);tracker.Advance(2);
            Assert.That(tracker.IsCelebrating,Is.False);
            Assert.That(expansion.BoxingPad.RankVisible,Is.False);
            Assert.That(dining.Tables[0].gameObject.activeSelf,Is.True);
        }
        [Test] public void OldV7FullShopKeepsRankAndAccessWithoutMax()
        {
            var data=new RestaurantSaveData {version=7,grillLevel=3,coins=12345,boughtBoxingStation=true,boughtDriveThru=true,
                boughtExtraTable=true,boughtExtraGrill=true,extraGrillLevel=1,boughtExtraCounter=true};
            Assert.That(data.IsValid,Is.True);
            Assert.That(data.ResolvedShopRank,Is.EqualTo(6));
            tracker.Restore(data.ResolvedShopRank,0,0,0,data.ResolvedMilestones,data.ResolvedLegacyAccess);
            Assert.That(tracker.StarLabel,Does.Not.Contain("MAX"));
            Assert.That(tracker.MilestoneComplete,Is.True);
            Assert.That(tracker.Stars,Is.Zero);
            Assert.That(expansion.WingPad.RankVisible,Is.True);
        }
        [Test] public void OldV7GrillInvestmentKeepsThePadAndRemaining()
        {
            expansion.RestoreInvestments(0,120,0,0,0);tracker.Restore(3,0,0);
            Assert.That(expansion.GrillPad.RankVisible,Is.True);
            Assert.That(expansion.GrillPad.Remaining,Is.EqualTo(80));
        }
        [Test] public void HistoricalSaleCountDoesNotInventMilestone()
        {
            wallet.RestoreProgress(0,10);tracker.Restore(1,0,0);tracker.Advance(1);
            Assert.That(tracker.Progress,Is.Zero);
            tracker.RecordMilestone(ShopGoalKind.UpgradeGrill);
            Assert.That(tracker.Progress,Is.EqualTo(1));
            Assert.That(tracker.Stars,Is.EqualTo(2));
            tracker.RecordMilestone(ShopGoalKind.UpgradeGrill);
            Assert.That(tracker.Stars,Is.EqualTo(2));
            Assert.That(tracker.Rank,Is.EqualTo(1));
        }
        [Test] public void DirtyTableNoLongerReplacesTheRankMilestone()
        {
            tracker.Restore(2,0,0,ShopRanks.StarCap(2)-2);
            var table=dining.Tables[0];var bag=player.gameObject.AddComponent<TrashInventory>();
            table.LeaveMealTrash(0);
            Assert.That(tracker.Title,Is.EqualTo("Clear a used dining table"));
            Assert.That(table.TryPickupTrash(bag),Is.True);
            while(table.TrashCount>0)Assert.That(table.TryPickupTrash(bag),Is.True);
            Assert.That(tracker.Rank,Is.EqualTo(2));
            Assert.That(tracker.CanUpgrade,Is.True);
            Assert.That(tracker.TryUpgradeRank(2),Is.True);
            Assert.That(tracker.Rank,Is.EqualTo(3));
            Assert.That(tracker.Title,Is.EqualTo("Let staff complete an order"));
            Assert.That(tracker.Stars,Is.Zero);
        }
        [Test] public void UnlockThresholdsAreExplicitAndLegacyImpliedRanksStayStable()
        {
            Assert.That(ShopRanks.Implied(true,0,true,0,true,0,true,0,true,0),Is.EqualTo(6));
            Assert.That(ShopRanks.PadUnlocked(3,"GRILL"),Is.False);
            Assert.That(ShopRanks.PadUnlocked(4,"GRILL"),Is.True);
            Assert.That(ShopRanks.PadUnlocked(5,"LANE"),Is.False);
            Assert.That(ShopRanks.PadUnlocked(6,"LANE"),Is.True);
            Assert.That(ShopRanks.PadUnlocked(2,"TABLE"),Is.False);
            Assert.That(ShopRanks.PadUnlocked(6,"WING"),Is.False);
            Assert.That(ShopRanks.PadUnlocked(7,"WING"),Is.True);
            Assert.That(ShopRanks.PadUnlocked(7,"FOUR"),Is.True);
        }
    }
}
