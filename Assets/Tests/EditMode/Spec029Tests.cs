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

        [Test]
        public void NewShopShowsRankOneAndOnlyTheTablePad()
        {
            Assert.That(tracker.StarLabel, Is.EqualTo("Lv.1  0/3"));
            Assert.That(tracker.Title, Is.EqualTo("Pick up a burger"));
            Assert.That(expansion.TablePad.RankVisible, Is.True);
            Assert.That(expansion.BoxingPad.RankVisible, Is.False);
            Assert.That(expansion.GrillPad.RankVisible, Is.False);
            Assert.That(expansion.CounterPad.RankVisible, Is.False);
            Assert.That(expansion.DriveThruPad.RankVisible, Is.False);
            Assert.That(expansion.GrillPad.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void LockedGrillPadDoesNotAcceptInvestment()
        {
            wallet.RestoreProgress(200, 0);
            Hold(expansion.GrillPad, 3f);
            Assert.That(expansion.GrillPad.Invested, Is.Zero);
            Assert.That(wallet.Coins, Is.EqualTo(200));
            Assert.That(expansion.HasExtraGrill, Is.False);
        }

        [Test]
        public void InstallingTheTableRanksUpAndUnlocksBoxing()
        {
            wallet.RestoreProgress(150, 0);
            tracker.Restore(1, 2, 0);
            Assert.That(tracker.Title, Does.StartWith("Install a table"));
            Hold(expansion.TablePad, 3f);
            tracker.Advance(0.01f);
            Assert.That(tracker.IsCelebrating, Is.True);
            tracker.Advance(0.7f);
            Assert.That(tracker.IsRankingUp, Is.True);
            Assert.That(tracker.Rank, Is.EqualTo(2));
            Assert.That(expansion.BoxingPad.RankVisible, Is.True);
            Assert.That(expansion.GrillPad.RankVisible, Is.False);
            Assert.That(wallet.Coins, Is.Zero);
            tracker.Advance(1f);
            Assert.That(tracker.IsRankingUp, Is.False);
            Assert.That(tracker.Title, Is.EqualTo("Serve customers"));
            Assert.That(tracker.StarLabel, Is.EqualTo("Lv.2  0/2"));
        }

        [Test]
        public void RestoreDoesNotReplayRankUp()
        {
            tracker.Restore(2, 0, 0);
            tracker.Advance(2f);
            Assert.That(tracker.IsCelebrating, Is.False);
            Assert.That(tracker.IsRankingUp, Is.False);
            Assert.That(tracker.Rank, Is.EqualTo(2));
            Assert.That(expansion.BoxingPad.RankVisible, Is.True);
        }

        [Test]
        public void OldV7FullShopResolvesToMaxWithoutSpending()
        {
            var data = new RestaurantSaveData
            {
                version = 7, coins = 12345, grillLevel = 3, boughtBoxingStation = true, boughtDriveThru = true,
                boxingInvestment = 150, driveThruInvestment = 250, boughtExtraTable = true, boughtExtraGrill = true,
                extraGrillLevel = 1, boughtExtraCounter = true
            };
            Assert.That(data.IsValid, Is.True);
            Assert.That(data.ResolvedShopRank, Is.EqualTo(ShopRanks.Max));
            Assert.That(data.ResolvedGoalIndex, Is.Zero);
            tracker.Restore(data.ResolvedShopRank, data.ResolvedGoalIndex, data.ResolvedGoalProgress);
            Assert.That(tracker.StarLabel, Is.EqualTo("MAX"));
            Assert.That(tracker.IsCelebrating, Is.False);
            Assert.That(wallet.Coins, Is.Zero);
        }

        [Test]
        public void OldV7GrillInvestmentKeepsThePadAndRemaining()
        {
            var data = new RestaurantSaveData
            {
                version = 7, coins = 80, grillLevel = 1, grillInvestment = 120
            };
            Assert.That(data.ResolvedShopRank, Is.EqualTo(3));
            expansion.RestoreInvestments(0, 120, 0, 0, 0);
            tracker.Restore(data.ResolvedShopRank, 0, 0);
            expansion.ApplyRank(tracker.Rank);
            Assert.That(expansion.GrillPad.RankVisible, Is.True);
            Assert.That(expansion.GrillPad.Remaining, Is.EqualTo(80));
        }

        [Test]
        public void ServeGoalIgnoresSalesCompletedBeforeItBecameActive()
        {
            wallet.RestoreProgress(0, 10);
            tracker.Restore(1, 0, 0);
            var grill = root.GetComponent<ProductionStation>();
            grill.Advance(12f);
            Assert.That(player.TryCollectFrom(grill), Is.True);
            tracker.Advance(0.01f);
            tracker.Advance(2f);
            Assert.That(tracker.Title, Is.EqualTo("Serve customers"));
            Assert.That(tracker.Progress, Is.Zero);
            Assert.That(wallet.RecordCompletedSale(), Is.True);
            tracker.Advance(0.01f);
            Assert.That(tracker.Progress, Is.EqualTo(1));
            Assert.That(tracker.Required, Is.EqualTo(3));
        }

        [Test]
        public void DirtyTableInterruptsButSalesStillCount()
        {
            var table = DiningTable.Create(root.transform, new Vector3(-5.6f, 0f, 0.2f));
            var trash = player.gameObject.AddComponent<TrashInventory>();
            table.BindCollector(trash);
            tracker.Configure(player, root.GetComponent<ProductionStation>(), stock, queue, wallet, serving,
                DiningArea.Wrap(table), trash, hiring, null, expansion);
            tracker.Restore(1, 1, 0);
            table.LeaveMealTrash(0);
            tracker.Advance(0.01f);
            Assert.That(tracker.Title, Is.EqualTo("Clear the table"));
            Assert.That(wallet.RecordCompletedSale(), Is.True);
            tracker.Advance(0.01f);
            Assert.That(tracker.Title, Is.EqualTo("Clear the table"));
            Assert.That(tracker.GoalProgress, Is.EqualTo(1));
        }

        [Test]
        public void ImpliedRankNeverTakesBoughtFacilitiesAway()
        {
            Assert.That(ShopRanks.Implied(true, 0, true, 0, false, 0, false, 0, false, 0), Is.EqualTo(3));
            Assert.That(ShopRanks.Implied(false, 0, false, 40, false, 0, false, 0, false, 0), Is.EqualTo(2));
            Assert.That(ShopRanks.Implied(true, 0, true, 0, true, 0, true, 0, true, 0), Is.EqualTo(6));
            Assert.That(ShopRanks.PadUnlocked(1, "GRILL"), Is.False);
            Assert.That(ShopRanks.PadUnlocked(3, "GRILL"), Is.True);
        }
    }
}
