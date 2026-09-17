using System;
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
    public sealed class Spec073DriveThruSpawnTests
    {
        GameObject root;
        ShopExpansion expansion;
        DiningArea dining;
        BurgerServingZone serving;
        WorkerHiringZone hiring;
        BurgerInventory player;
        RestaurantWallet wallet;
        CounterStock stock;
        CustomerQueue queue;
        ProductionStation grill;
        CashFloor cash;
        SessionGoalTracker goals;
        BoostRoom bay;
        PartsWallet parts;
        FacilityLayout layout;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Spec073");
            Material wall = RuntimeMaterials.Create(new Color(0.40f, 0.29f, 0.17f));
            Material floor = RuntimeMaterials.Create(new Color(0.70f, 0.56f, 0.42f));
            ShopLayout.CreateFloor(root.transform, floor);
            ShopLayout.CreateWalls(root.transform, wall);
            bay = BoostRoom.Create(root.transform, wall, floor);
            bay.SetAnnexOpen(false);

            player = new GameObject("Player").AddComponent<BurgerInventory>();
            player.transform.SetParent(root.transform);
            player.transform.position = ShopLayout.PlayerSpawn;
            player.Configure();
            Transform output = new GameObject("Output").transform;
            output.SetParent(root.transform);
            grill = root.AddComponent<ProductionStation>();
            grill.Configure(output, null, null);
            wallet = root.AddComponent<RestaurantWallet>();
            parts = root.AddComponent<PartsWallet>();
            queue = root.AddComponent<CustomerQueue>();
            queue.OrderQuantityFactory = () => 1;
            queue.Configure(ShopLayout.Entrance, ShopLayout.QueueEntry, ShopLayout.QueueSlots, ShopLayout.Counter);
            Transform point = new GameObject("Circle").transform;
            point.SetParent(root.transform);
            point.position = ShopLayout.ServingCircle;
            GameObject burgerCounter = new GameObject("BurgerCounter");
            burgerCounter.transform.SetParent(root.transform, false);
            burgerCounter.transform.position = ShopLayout.Counter;
            Transform anchor = new GameObject("Anchor").transform;
            anchor.SetParent(burgerCounter.transform);
            anchor.position = ShopLayout.CounterTop;
            stock = burgerCounter.AddComponent<CounterStock>();
            stock.Configure(anchor, null);
            CounterDropZone drop = burgerCounter.AddComponent<CounterDropZone>();
            drop.Configure(stock, point);
            dining = DiningArea.Create(root.transform, ShopLayout.Tables);
            cash = root.AddComponent<CashFloor>();
            serving = root.AddComponent<BurgerServingZone>();
            serving.Configure(queue, player, wallet, point, ShopLayout.Exit, stock, dining, drop, 10, cash);
            cash.Configure(wallet, player.transform, ShopLayout.CounterCash);
            hiring = root.AddComponent<WorkerHiringZone>();
            hiring.Configure(grill, serving, wallet, player, root.transform, root.transform, ShopLayout.Aisle, drop,
                null, dining, null);
            expansion = ShopExpansion.Create(root.transform, dining, serving, hiring, player, wallet, cash);
            goals = root.AddComponent<SessionGoalTracker>();
            goals.Configure(player, grill, stock, queue, wallet, serving, dining, null, hiring, null, expansion);
            layout = root.AddComponent<FacilityLayout>();
            layout.Configure(player, wallet, parts, cash, dining, hiring, goals, expansion);
        }

        [TearDown]
        public void TearDown()
        {
            ShopLayout.ResetWingLock();
            UnityEngine.Object.DestroyImmediate(root);
        }

        void Hold(FacilityUnlockZone pad, float seconds)
        {
            player.transform.position = pad.PadPosition + Vector3.up;
            for (int i = 0; i < Mathf.CeilToInt(seconds * 60f); i++) pad.Advance(1f / 60f);
        }

        void BuyBoxingAndLane()
        {
            wallet.RestoreProgress(Math.Max(wallet.Coins, ShopExpansion.BoxingCost + ShopExpansion.DriveThruCost),
                wallet.CompletedSales);
            Hold(expansion.BoxingPad, 3f);
            Assert.That(expansion.HasBoxing, Is.True);
            Hold(expansion.DriveThruPad, 3f);
            Assert.That(expansion.HasDriveThru, Is.True);
            expansion.DriveThru.OrderQuantityFactory = () => 1;
        }

        void WaitForCar(float seconds = 5f)
        {
            for (int i = 0; i < Mathf.CeilToInt(seconds * 60f); i++)
            {
                expansion.DriveThru.Advance(1f / 60f);
                if (expansion.DriveThru.HasWaitingCar) return;
            }
        }

        static bool PathOnSouthRoad(Vector3[] path)
        {
            if (path == null || path.Length == 0) return false;
            for (int i = 0; i < path.Length; i++)
            {
                if (path[i].z >= -ShopLayout.WallHalf) return false;
                if (float.IsNaN(path[i].x) || float.IsInfinity(path[i].x)) return false;
            }
            return true;
        }

        [Test]
        public void RankSixWithDriveThruPurchasedOpensTheLaneAndSpawnsCars()
        {
            goals.Restore(6, 0, 0);
            BuyBoxingAndLane();
            DriveThruLane lane = expansion.DriveThru;
            Assert.That(lane, Is.Not.Null);
            Assert.That(lane.isActiveAndEnabled, Is.True);
            Assert.That(lane.IsOpen, Is.True);
            Assert.That(lane.SpawnAllowed, Is.True);
            Assert.That(lane.SpawnPath, Is.Not.Empty);
            Assert.That(PathOnSouthRoad(lane.SpawnPath), Is.True);
            Assert.That(root.transform.Find("BoostDoorPlug"), Is.Null);
            WaitForCar();
            Assert.That(lane.HasWaitingCar, Is.True);
            Assert.That(lane.CarCount, Is.GreaterThan(0));
        }

        [Test]
        public void RankFiveWithoutPurchaseDoesNotSpawnCars()
        {
            goals.Restore(5, 0, 0);
            Assert.That(expansion.HasDriveThru, Is.False);
            Assert.That(UnityEngine.Object.FindFirstObjectByType<DriveThruLane>(), Is.Null);
            Assert.That(bay.transform.Find("CarServiceFloor").gameObject.activeSelf, Is.True,
                "064 opens the south bay at Rank 5");
            Assert.That(expansion.DriveThruPad.RankVisible, Is.False);
        }

        [Test]
        public void CompletingADriveThruServeRecordsCarOrder()
        {
            goals.Restore(6, 0, 0);
            grill.Advance(12f);
            Assert.That(player.TryCollectFrom(grill), Is.True);
            BuyBoxingAndLane();
            player.transform.position = expansion.Boxing.CirclePosition + Vector3.up;
            Assert.That(expansion.Boxing.TryBoxFrom(player), Is.True);
            expansion.Boxing.Advance(2f);
            player.transform.position = expansion.Boxing.DropPosition + Vector3.up;
            expansion.Boxing.Advance(0.3f);
            Assert.That(expansion.Boxing.PackageCount, Is.EqualTo(1));
            player.transform.position = Vector3.zero;
            WaitForCar();
            player.transform.position = expansion.DriveThru.WindowPosition + Vector3.up;
            Assert.That(expansion.DriveThru.TrySellFrom(player), Is.True);
            for (int i = 0; i < 120 && expansion.DriveThru.IsHandoffActive; i++)
                expansion.DriveThru.Advance(1f / 60f);
            Assert.That(goals.MilestoneComplete, Is.True);
            Assert.That(goals.Stars, Is.GreaterThanOrEqualTo(2));
            Assert.That(goals.Rank, Is.EqualTo(6));
        }

        [Test]
        public void RankSixWithBoxingOnlyOpensTheLaneAndSpawnsCars()
        {
            wallet.RestoreProgress(2000, 0);
            expansion.Restore(false, false, false, 0, true, false);
            goals.Restore(6, 0, 0);
            layout.Discover();
            Assert.That(expansion.HasBoxing, Is.True);
            Assert.That(expansion.HasDriveThru, Is.True,
                "074: Rank 6 boxing/car bay is the drive-thru window; cars must spawn without a second SKU");
            Assert.That(expansion.DriveThru.IsOpen, Is.True);
            Assert.That(expansion.DriveThru.SpawnAllowed, Is.True);
            Assert.That(PathOnSouthRoad(expansion.DriveThru.SpawnPath), Is.True);
            expansion.DriveThru.OrderQuantityFactory = () => 1;
            WaitForCar();
            Assert.That(expansion.DriveThru.HasWaitingCar, Is.True);
        }
    }
}
