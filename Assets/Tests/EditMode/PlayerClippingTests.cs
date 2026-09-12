using BurgerShop.Customer;
using BurgerShop.Core;
using BurgerShop.Customer;
using BurgerShop.Economy;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using NUnit.Framework;
using UnityEngine;

namespace BurgerShop.Tests.EditMode
{
    public sealed class PlayerClippingTests
    {
        GameObject root;

        [SetUp]
        public void SetUp() => root = new GameObject("PlayerClippingTest");

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            ShopLayout.ResetWingLock();
        }

        [Test]
        public void PairFourSeatAndSquareTablesBlockAndChairsFaceTheTop()
        {
            DiningTable pair = DiningTable.Create(root.transform, ShopLayout.Tables[0]);
            DiningTable four = DiningTable.Create(root.transform, ShopLayout.FourSeatTable, DiningTableKind.FourSeat);
            DiningTable square = DiningTable.Create(root.transform, ShopLayout.SquareTable, DiningTableKind.Square);
            Physics.SyncTransforms();

            AssertBlocks(pair.transform.Find("Top"));
            AssertBlocks(pair.transform.Find("ChairA/Seat"));
            AssertBlocks(pair.transform.Find("ChairA/Back"));
            AssertChairFaces(pair, "ChairA");
            AssertChairFaces(pair, "ChairB");
            AssertCapsuleOverlapsOccupancy(pair.Center, pair.transform);
            AssertCapsuleCastFromWestHits(pair);

            AssertBlocks(four.transform.Find("Top"));
            Assert.That(four.SeatCount, Is.EqualTo(4));
            AssertChairFaces(four, "ChairA");
            AssertChairFaces(four, "ChairB");
            AssertChairFaces(four, "ChairC");
            AssertChairFaces(four, "ChairD");
            AssertBlocks(four.transform.Find("ChairC/Seat"));
            AssertCapsuleOverlapsOccupancy(four.Center, four.transform);

            AssertBlocks(square.transform.Find("Top"));
            AssertBlocks(square.transform.Find("ChairA/Back"));
            Assert.That(square.transform.Find("ChairA/Back").localScale.y, Is.GreaterThan(0.75f));
            AssertChairFaces(square, "ChairA");
            AssertChairFaces(square, "ChairB");
            AssertCapsuleOverlapsOccupancy(square.Center, square.transform);
        }

        [Test]
        public void WallsCountersGrillsAndStationsBlockThePlayerCapsule()
        {
            Material mat = RuntimeMaterials.Create(new Color(0.5f, 0.4f, 0.3f));
            GameObject floor = ShopLayout.CreateFloor(root.transform, mat);
            ShopLayout.CreateWalls(root.transform, mat);
            GameObject counter = GameObject.CreatePrimitive(PrimitiveType.Cube);
            counter.name = "OrderCounter";
            counter.transform.SetParent(root.transform, false);
            counter.transform.position = ShopLayout.Counter + Vector3.up * 0.5f;
            counter.transform.localScale = new Vector3(3.2f, 1f, 1.4f);

            var player = root.AddComponent<BurgerInventory>();
            player.Configure();
            var wallet = root.AddComponent<RestaurantWallet>();
            ExpandableGrill grill = ExpandableGrill.CreateStarter(root.transform, player, wallet);
            BoxingStation boxing = BoxingStation.Create(root.transform);
            TrashBin bin = TrashBin.Create(root.transform, ShopLayout.TrashBin);
            DriveThruLane lane = DriveThruLane.Create(root.transform, boxing, wallet, null, player);
            Physics.SyncTransforms();

            AssertBlocks(floor.GetComponent<Collider>());
            AssertBlocks(root.transform.Find("Wall+Z").GetComponent<Collider>());
            AssertBlocks(root.transform.Find("Wall-X").GetComponent<Collider>());
            AssertBlocks(counter.GetComponent<Collider>());
            AssertBlocks(grill.ActiveLook.Find("Body").GetComponent<Collider>());
            AssertBlocks(boxing.transform.Find("BoxTable").GetComponent<Collider>());
            AssertBlocks(boxing.transform.Find("PackageDesk").GetComponent<Collider>());
            AssertBlocks(bin.transform.Find("Body").GetComponent<Collider>());
            AssertBlocks(boxing.transform.Find("PackageDesk").GetComponent<Collider>());
            Assert.That(SolidOccupancy.BlocksPlayer(grill.transform.Find("PickupSpot").GetComponent<Collider>()),
                Is.False);
        }

        [Test]
        public void MoneyPadsStayWalkableAndBoughtTablesThenBlock()
        {
            var player = new GameObject("Player").AddComponent<BurgerInventory>();
            player.transform.SetParent(root.transform);
            player.Configure();
            var wallet = root.AddComponent<RestaurantWallet>();
            var queue = root.AddComponent<CustomerQueue>();
            queue.OrderQuantityFactory = () => 1;
            queue.Configure(ShopLayout.Entrance, ShopLayout.QueueEntry, ShopLayout.QueueSlots, ShopLayout.Counter);
            Transform circle = new GameObject("Circle").transform;
            circle.SetParent(root.transform);
            circle.position = ShopLayout.ServingCircle;
            var stock = root.AddComponent<CounterStock>();
            stock.Configure(new GameObject("Anchor").transform, null);
            var drop = root.AddComponent<CounterDropZone>();
            drop.Configure(stock, circle);
            DiningArea dining = DiningArea.Create(root.transform, ShopLayout.Tables);
            var serving = root.AddComponent<BurgerServingZone>();
            serving.Configure(queue, player, wallet, circle, ShopLayout.Exit, stock, dining, drop);
            var hiring = root.AddComponent<WorkerHiringZone>();
            var grill = root.AddComponent<ProductionStation>();
            grill.Configure(new GameObject("Out").transform, null, null);
            hiring.Configure(grill, serving, wallet, player, root.transform, root.transform, ShopLayout.Aisle, drop);
            ShopExpansion expansion = ShopExpansion.Create(root.transform, dining, serving, hiring, player, wallet);
            expansion.Restore(false, false, false, 0, false, false, false, false, true);
            Physics.SyncTransforms();

            Assert.That(expansion.WingPad.GetComponent<Collider>(), Is.Null);
            Assert.That(expansion.TablePad.GetComponent<Collider>(), Is.Null);
            Assert.That(expansion.FourSeatPad.GetComponent<Collider>(), Is.Null);
            Assert.That(expansion.SquarePad.GetComponent<Collider>(), Is.Null);
            Assert.That(expansion.TablePad.transform.Find("Coin_0").GetComponent<Collider>(), Is.Null);
            Assert.That(SolidOccupancy.BlocksPlayer(
                expansion.TablePad.transform.Find("FacilityIcon_TABLE/Top").GetComponent<Collider>()), Is.False);
            AssertCapsuleDoesNotOverlapOccupancy(ShopLayout.TableUnlock, expansion.TablePad.transform);

            wallet.RestoreProgress(350, 0);
            player.transform.position = expansion.FourSeatPad.PadPosition + Vector3.up;
            for (int i = 0; i < 180; i++) expansion.FourSeatPad.Advance(1f / 60f);
            player.transform.position = expansion.SquarePad.PadPosition + Vector3.up;
            for (int i = 0; i < 180; i++) expansion.SquarePad.Advance(1f / 60f);
            Physics.SyncTransforms();
            AssertCapsuleOverlapsOccupancy(expansion.FourSeatTable.Center, expansion.FourSeatTable.transform);
            AssertCapsuleOverlapsOccupancy(expansion.SquareTable.Center, expansion.SquareTable.transform);
            AssertChairFaces(expansion.FourSeatTable, "ChairD");
            AssertChairFaces(expansion.SquareTable, "ChairA");

            expansion.Restore(false, false, true, 1);
            Physics.SyncTransforms();
            AssertBlocks(expansion.transform.Find("ExtraOrderCounter/OrderCounter"));
            Assert.That(SolidOccupancy.BlocksPlayer(
                expansion.transform.Find("ExtraOrderCounter/ExtraCashierCircle").GetComponent<Collider>()),
                Is.False);
        }

        [Test]
        public void PlayerUsesABlockingCharacterControllerWithoutANoseCollider()
        {
            GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.transform.SetParent(root.transform, false);
            player.transform.position = ShopLayout.PlayerSpawn;
            Object.DestroyImmediate(player.GetComponent<Collider>());
            CharacterController controller = player.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.4f;
            controller.center = Vector3.zero;
            GameObject nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nose.name = "Facing";
            nose.transform.SetParent(player.transform, false);
            Object.DestroyImmediate(nose.GetComponent<Collider>());
            player.AddComponent<PlayerMotor>();

            Assert.That(controller.height, Is.EqualTo(2f));
            Assert.That(controller.radius, Is.EqualTo(0.4f));
            Assert.That(controller.enabled, Is.True);
            Assert.That(nose.GetComponent<Collider>(), Is.Null);
            Assert.That(player.GetComponent<PlayerMotor>(), Is.Not.Null);
        }

        [Test]
        public void HrDeskAndBoostGearBlockWhileDoorwaysStayOpen()
        {
            Material wall = RuntimeMaterials.Create(new Color(0.4f, 0.3f, 0.2f));
            Material floor = RuntimeMaterials.Create(new Color(0.7f, 0.6f, 0.5f));
            ShopLayout.CreateWalls(root.transform, wall);
            HrOffice office = HrOffice.Create(root.transform, wall, floor);
            BoostRoom boost = BoostRoom.Create(root.transform, wall, floor);
            Physics.SyncTransforms();

            AssertBlocks(office.transform.Find("Desk").GetComponent<Collider>());
            AssertBlocks(office.transform.Find("Chair/Seat").GetComponent<Collider>());
            Vector3 towardDesk = Flatten(ShopLayout.HrDesk - office.transform.Find("Chair").position);
            Vector3 chairForward = Flatten(office.transform.Find("Chair").forward);
            Assert.That(Vector3.Dot(chairForward.normalized, towardDesk.normalized), Is.GreaterThan(0.9f));
            AssertBlocks(boost.transform.Find("TrainingPad").GetComponent<Collider>());
            Assert.That(ShopLayout.ContainsHrDoorway(new Vector3(ShopLayout.WallHalf, 0f, ShopLayout.HrDoorZ)), Is.True);
            Assert.That(root.transform.Find("Wall+X"), Is.Null);
            Assert.That(Physics.CheckBox(ShopLayout.HrDoor + Vector3.up * 0.6f, new Vector3(0.2f, 0.4f, 0.9f)), Is.False);
            Assert.That(Physics.CheckBox(ShopLayout.BoostDoor + Vector3.up * 0.6f, new Vector3(0.9f, 0.4f, 0.2f)),
                Is.False);
        }

        [Test]
        public void SideDoorPlugBlocksUntilTheWingIsBoughtThenColaBodiesBlock()
        {
            Material wall = RuntimeMaterials.Create(new Color(0.4f, 0.3f, 0.2f));
            ShopLayout.CreateWalls(root.transform, wall);
            Physics.SyncTransforms();
            AssertBlocks(root.transform.Find("WingDoorPlug").GetComponent<Collider>());
            Assert.That(Physics.CheckBox(ShopLayout.SideDoor + Vector3.up * 0.6f, new Vector3(0.2f, 0.4f, 1.2f)),
                Is.True);

            var player = new GameObject("Player").AddComponent<BurgerInventory>();
            player.transform.SetParent(root.transform);
            player.Configure();
            var wallet = root.AddComponent<RestaurantWallet>();
            var queue = root.AddComponent<CustomerQueue>();
            queue.OrderQuantityFactory = () => 1;
            queue.Configure(ShopLayout.Entrance, ShopLayout.QueueEntry, ShopLayout.QueueSlots, ShopLayout.Counter);
            Transform circle = new GameObject("Circle").transform;
            circle.SetParent(root.transform);
            circle.position = ShopLayout.ServingCircle;
            var stock = root.AddComponent<CounterStock>();
            stock.Configure(new GameObject("Anchor").transform, null);
            var drop = root.AddComponent<CounterDropZone>();
            drop.Configure(stock, circle);
            DiningArea dining = DiningArea.Create(root.transform, ShopLayout.Tables);
            var serving = root.AddComponent<BurgerServingZone>();
            serving.Configure(queue, player, wallet, circle, ShopLayout.Exit, stock, dining, drop);
            var hiring = root.AddComponent<WorkerHiringZone>();
            var grill = root.AddComponent<ProductionStation>();
            grill.Configure(new GameObject("Out").transform, null, null);
            hiring.Configure(grill, serving, wallet, player, root.transform, root.transform, ShopLayout.Aisle, drop);
            ShopExpansion expansion = ShopExpansion.Create(root.transform, dining, serving, hiring, player, wallet);
            expansion.Restore(false, false, false, 0, false, false, false, false, true, true, true);
            Physics.SyncTransforms();
            Assert.That(root.transform.Find("WingDoorPlug"), Is.Null);
            Assert.That(Physics.CheckBox(ShopLayout.SideDoor + Vector3.up * 0.6f, new Vector3(0.2f, 0.4f, 1.2f)),
                Is.False);
            AssertBlocks(expansion.ColaMachine.ActiveLook.Find("Body"));
            AssertBlocks(expansion.transform.Find("ColaCustomerArea/ColaCounter"));
            Assert.That(SolidOccupancy.BlocksPlayer(expansion.ColaMachine.Pickup.PickupPoint.GetComponent<Collider>()),
                Is.False);
            Assert.That(SolidOccupancy.BlocksPlayer(expansion.ColaMachine.UpgradeSpot.GetComponent<Collider>()),
                Is.False);
            Assert.That(SolidOccupancy.BlocksPlayer(
                expansion.transform.Find("ColaCustomerArea/ColaCashierCircle/Dash_1").GetComponent<Collider>()),
                Is.False);
        }

        static void AssertBlocks(Transform part)
        {
            Assert.That(part, Is.Not.Null);
            AssertBlocks(part.GetComponent<Collider>());
        }

        static void AssertBlocks(Collider collider)
        {
            Assert.That(SolidOccupancy.BlocksPlayer(collider), Is.True, collider != null ? collider.name : "missing");
        }

        static void AssertChairFaces(DiningTable table, string name)
        {
            Transform chair = table.transform.Find(name);
            Assert.That(chair, Is.Not.Null);
            Vector3 toward = Flatten(table.Center - chair.position);
            Vector3 forward = Flatten(chair.forward);
            Assert.That(Vector3.Dot(forward.normalized, toward.normalized), Is.GreaterThan(0.9f), name);
            Transform back = chair.Find("Back");
            Assert.That(Vector3.Distance(Flatten(back.position), Flatten(table.Center)),
                Is.GreaterThan(Vector3.Distance(Flatten(chair.position), Flatten(table.Center))));
        }

        static void AssertCapsuleOverlapsOccupancy(Vector3 center, Transform occupancy)
        {
            Physics.SyncTransforms();
            Assert.That(OccupancyOverlapsCapsule(center, occupancy), Is.True, occupancy.name);
        }

        static void AssertCapsuleDoesNotOverlapOccupancy(Vector3 center, Transform occupancy)
        {
            Physics.SyncTransforms();
            Assert.That(OccupancyOverlapsCapsule(center, occupancy), Is.False, occupancy.name);
        }

        static bool OccupancyOverlapsCapsule(Vector3 center, Transform occupancy)
        {
            Vector3 p1 = center + Vector3.up * 0.6f;
            Vector3 p2 = center + Vector3.down * 0.6f;
            Collider[] hits = Physics.OverlapCapsule(p1, p2, 0.4f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
                if (hits[i] != null && (hits[i].transform == occupancy || hits[i].transform.IsChildOf(occupancy)))
                    return true;
            return false;
        }

        static void AssertCapsuleCastFromWestHits(DiningTable table)
        {
            Physics.SyncTransforms();
            Vector3 origin = table.Center + new Vector3(-2.2f, 0.9f, 0f);
            Vector3 p1 = origin + Vector3.up * 0.6f;
            Vector3 p2 = origin + Vector3.down * 0.6f;
            Vector3 direction = Flatten(table.Center - origin).normalized;
            bool hit = Physics.CapsuleCast(p1, p2, 0.4f, direction, out RaycastHit info, 3f, ~0,
                QueryTriggerInteraction.Ignore);
            Assert.That(hit, Is.True, table.name);
            Assert.That(info.collider.transform.IsChildOf(table.transform), Is.True, info.collider.name);
            Assert.That(info.collider.isTrigger, Is.False);
        }

        static Vector3 Flatten(Vector3 value)
        {
            value.y = 0f;
            return value;
        }
    }
}
