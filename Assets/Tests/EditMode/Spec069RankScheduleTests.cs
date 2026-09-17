using BurgerShop.Building;
using BurgerShop.Core;
using BurgerShop.Customer;
using BurgerShop.Economy;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using BurgerShop.UI;
using NUnit.Framework;
using UnityEngine;

namespace BurgerShop.Tests.EditMode
{
    public sealed class Spec069RankScheduleTests
    {
        static readonly ShopGoalKind[] MilestoneKinds =
        {
            ShopGoalKind.UpgradeGrill, ShopGoalKind.CleanTable, ShopGoalKind.WorkerOrder,
            ShopGoalKind.ExtraProduction, ShopGoalKind.BoxBurger, ShopGoalKind.CarOrder,
            ShopGoalKind.ColaOrder, ShopGoalKind.CourierOrder, ShopGoalKind.AutomatedOrder
        };

        GameObject root;
        SessionGoalTracker goals;
        RestaurantWallet wallet;
        ShopExpansion expansion;
        DiningArea dining;
        HrOffice office;
        BoostRoom bay;
        BagLine west;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Spec069");
            Material wall = RuntimeMaterials.Create(new Color(0.40f, 0.29f, 0.17f));
            Material floor = RuntimeMaterials.Create(new Color(0.72f, 0.70f, 0.62f));
            ShopLayout.CreateFloor(root.transform, floor);
            ShopLayout.CreateWalls(root.transform, wall);
            office = HrOffice.Create(root.transform, wall, floor);
            office.SetOpen(false);
            bay = BoostRoom.Create(root.transform, wall, floor);
            bay.SetAnnexOpen(false);
            var player = new GameObject("Player").AddComponent<BurgerInventory>();
            player.transform.SetParent(root.transform);
            player.Configure();
            wallet = root.AddComponent<RestaurantWallet>();
            dining = DiningArea.Create(root.transform, ShopLayout.Tables);
            var queue = root.AddComponent<CustomerQueue>();
            queue.OrderQuantityFactory = () => 1;
            queue.Configure(ShopLayout.Entrance, ShopLayout.QueueEntry, ShopLayout.QueueSlots, ShopLayout.Counter);
            Transform point = new GameObject("Circle").transform;
            point.SetParent(root.transform);
            point.position = ShopLayout.ServingCircle;
            Transform anchor = new GameObject("Anchor").transform;
            var stock = root.AddComponent<CounterStock>();
            stock.Configure(anchor, null);
            var drop = root.AddComponent<CounterDropZone>();
            drop.Configure(stock, point);
            var serving = root.AddComponent<BurgerServingZone>();
            serving.Configure(queue, player, wallet, point, ShopLayout.Exit, stock, dining, drop);
            Transform output = new GameObject("Output").transform;
            output.SetParent(root.transform);
            var grill = root.AddComponent<ProductionStation>();
            grill.Configure(output, null, null);
            var hiring = root.AddComponent<WorkerHiringZone>();
            hiring.Configure(grill, serving, wallet, player, root.transform, root.transform, ShopLayout.Aisle, drop);
            expansion = ShopExpansion.Create(root.transform, dining, serving, hiring, player, wallet);
            west = root.AddComponent<BagLine>();
            goals = root.AddComponent<SessionGoalTracker>();
            goals.Configure(player, grill, stock, queue, wallet, serving, dining, null, hiring, null, expansion);
            west.Configure(wallet, goals, null, hiring, player, null);
            goals.Restore(1, 0, 0);
        }

        [TearDown]
        public void TearDown()
        {
            ShopLayout.ResetWingLock();
            Object.DestroyImmediate(root);
        }

        [Test]
        public void ContentGoalsAndNextUnlockMatchTheOneToFifteenTable()
        {
            Assert.That(ShopLayout.Tables.Length, Is.EqualTo(2));
            Assert.That(TableUpgradeBoard.StarterTableCount, Is.EqualTo(2));
            for (int rank = 1; rank <= 9; rank++)
            {
                ShopGoal[] listed = ShopRanks.Goals(rank);
                Assert.That(listed, Is.Not.Empty, "rank " + rank);
                Assert.That(listed[0].Kind, Is.EqualTo(MilestoneKinds[rank - 1]), "rank " + rank);
            }
            Assert.That(ShopRanks.Goals(10), Is.Not.Empty);
            Assert.That(ShopRanks.Goals(10)[0].Kind, Is.EqualTo(ShopGoalKind.StatLinePeakTen));
            Assert.That(ShopRanks.Goals(15)[0].Kind, Is.EqualTo(ShopGoalKind.StatLinePeakEighteen));
            Assert.That(ShopRanks.NextUnlock(1), Does.Contain("Dining").IgnoreCase);
            Assert.That(ShopRanks.NextUnlock(2), Does.Contain("Hire").IgnoreCase);
            Assert.That(ShopRanks.NextUnlock(3).ToLowerInvariant(), Does.Contain("grill"));
            Assert.That(ShopRanks.NextUnlock(3).ToLowerInvariant(), Does.Contain("counter"));
            Assert.That(ShopRanks.NextUnlock(3).ToLowerInvariant(), Does.Contain("restroom"));
            Assert.That(ShopRanks.NextUnlock(4).ToLowerInvariant(), Does.Contain("box"));
            Assert.That(ShopRanks.NextUnlock(5).ToLowerInvariant(), Does.Contain("drive"));
            Assert.That(ShopRanks.NextUnlock(6).ToLowerInvariant(), Does.Contain("cola"));
            Assert.That(ShopRanks.NextUnlock(6).ToLowerInvariant(), Does.Contain("four"));
            Assert.That(ShopRanks.NextUnlock(6).ToLowerInvariant(), Does.Contain("square"));
            Assert.That(ShopRanks.NextUnlock(7).ToLowerInvariant(), Does.Contain("courier"));
            Assert.That(ShopRanks.NextUnlock(7).ToLowerInvariant(), Does.Not.Contain("cola"));
            Assert.That(ShopRanks.NextUnlock(8).ToLowerInvariant(), Does.Contain("conveyor"));
            Assert.That(ShopRanks.NextUnlock(9).ToLowerInvariant(), Does.Contain("west"));
        }

        [Test]
        public void UnlockRanksOpenOnlyAfterTheMatchingUpgradeClick()
        {
            Assert.That(ShopRanks.PadUnlocked(2, "TABLE"), Is.False, "no Rank 2 extra pair pad");
            Assert.That(ShopRanks.PadUnlocked(ShopRanks.ColaWingRank, "TABLE"), Is.True);
            Assert.That(FacilityCatalog.Get(FacilityKind.TrashBin).Rank, Is.EqualTo(ShopRanks.DiningRank));
            Assert.That(FacilityCatalog.Get(FacilityKind.BurgerMachine).Rank, Is.EqualTo(ShopRanks.ExtraKitchenRank));
            Assert.That(FacilityCatalog.Get(FacilityKind.BurgerCounter).Rank, Is.EqualTo(ShopRanks.ExtraKitchenRank));
            Assert.That(RestroomExpansion.UnlockRank, Is.EqualTo(ShopRanks.ExtraKitchenRank));
            Assert.That(FacilityCatalog.Get(FacilityKind.BlueBoxTable).Rank, Is.EqualTo(ShopRanks.BoxingRank));
            Assert.That(FacilityCatalog.Get(FacilityKind.CarCounter).Rank, Is.EqualTo(ShopRanks.DriveThruRank));
            Assert.That(FacilityCatalog.Get(FacilityKind.FourSeatTable).Rank, Is.EqualTo(ShopRanks.ColaWingRank));
            Assert.That(FacilityCatalog.Get(FacilityKind.SquareTable).Rank, Is.EqualTo(ShopRanks.ColaWingRank));
            Assert.That(FacilityCatalog.Get(FacilityKind.ColaMachine).Rank, Is.EqualTo(ShopRanks.ColaWingRank));
            Assert.That(FacilityCatalog.Get(FacilityKind.CourierTray).Rank, Is.EqualTo(ShopRanks.CourierRank));
            Assert.That(FacilityCatalog.Get(FacilityKind.RedBoxMachine).Rank, Is.EqualTo(ShopRanks.CourierRank));
            Assert.That(FacilityCatalog.Get(FacilityKind.BagMachine).Rank, Is.EqualTo(ShopRanks.WestRank));

            goals.Restore(1, 0, 0);
            Assert.That(goals.Allows(ShopRanks.DiningRank), Is.False);
            Assert.That(office.gameObject.activeInHierarchy, Is.False);
            FillAndClick(1);
            Assert.That(goals.Rank, Is.EqualTo(2));
            Assert.That(goals.Allows(ShopRanks.DiningRank), Is.True);
            Assert.That(goals.Allows(ShopRanks.HireRank), Is.False);

            FillAndClick(2);
            Assert.That(goals.Rank, Is.EqualTo(3));
            Assert.That(office.gameObject.activeInHierarchy, Is.True);
            Assert.That(goals.Allows(ShopRanks.ExtraKitchenRank), Is.False);

            FillAndClick(3);
            Assert.That(expansion.GrillPad.RankVisible, Is.True);
            Assert.That(expansion.CounterPad.RankVisible, Is.True);
            Assert.That(expansion.BoxingPad.RankVisible, Is.False);

            FillAndClick(4);
            Assert.That(expansion.BoxingPad.RankVisible, Is.True);
            Assert.That(expansion.DriveThruPad.RankVisible, Is.False);
            Assert.That(bay.transform.Find("CarServiceFloor").gameObject.activeSelf, Is.True);

            FillAndClick(5);
            Assert.That(expansion.DriveThruPad.RankVisible, Is.True);
            Assert.That(expansion.WingPad.RankVisible, Is.False);
            Assert.That(FacilityCatalog.IsUnlocked(FacilityKind.FourSeatTable, 6, false), Is.False);
            Assert.That(FacilityCatalog.IsUnlocked(FacilityKind.SquareTable, 6, false), Is.False);

            FillAndClick(6);
            Assert.That(goals.Rank, Is.EqualTo(7));
            Assert.That(expansion.WingPad.RankVisible, Is.True);
            Assert.That(ShopRanks.PadUnlocked(7, "FOUR"), Is.True);
            Assert.That(ShopRanks.PadUnlocked(7, "SQUARE"), Is.True);
            Assert.That(ShopRanks.PadUnlocked(7, "WING"), Is.True);
            Assert.That(FacilityCatalog.IsUnlocked(FacilityKind.CourierTray, 7, false), Is.False);
            Assert.That(FacilityCatalog.IsUnlocked(FacilityKind.RedBoxMachine, 7, false), Is.False);

            FillAndClick(7);
            Assert.That(goals.Rank, Is.EqualTo(8));
            Assert.That(FacilityCatalog.IsUnlocked(FacilityKind.CourierTray, 8, false), Is.True);
            Assert.That(goals.Allows(ShopRanks.AutomationRank), Is.False);

            FillAndClick(8);
            Assert.That(goals.Allows(ShopRanks.AutomationRank), Is.True);
            Assert.That(goals.Allows(ShopRanks.WestRank), Is.False);
            Assert.That(west.Expanded, Is.False);

            FillAndClick(9);
            Assert.That(goals.Rank, Is.EqualTo(10));
            Assert.That(goals.Allows(ShopRanks.WestRank), Is.True);
            Assert.That(west.Expanded, Is.True);
        }

        [Test]
        public void MilestonesStillWaitForTheUpgradeClick()
        {
            for (int rank = 1; rank <= 9; rank++)
            {
                goals.Restore(rank, 0, 0, ShopRanks.StarCap(rank));
                goals.RecordMilestone(MilestoneKinds[rank - 1]);
                goals.Advance(0.01f);
                Assert.That(goals.Rank, Is.EqualTo(rank), "070 click required at rank " + rank);
                Assert.That(goals.CanUpgrade, Is.True, "rank " + rank);
                Assert.That(goals.TryUpgradeRank(rank), Is.True, "rank " + rank);
                Assert.That(goals.Rank, Is.EqualTo(rank + 1));
            }
        }

        void FillAndClick(int rank)
        {
            goals.Restore(rank, 0, 0, ShopRanks.StarCap(rank));
            Assert.That(goals.TryUpgradeRank(rank), Is.True, "click rank " + rank);
        }
    }
}
