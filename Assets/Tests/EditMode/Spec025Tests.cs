using System;
using System.IO;
using System.Text;
using BurgerShop.Customer;
using BurgerShop.Economy;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BurgerShop.Tests.EditMode
{
    public sealed class Spec025Tests
    {
        GameObject root;
        BurgerInventory player;
        ProductionStation grill;
        CustomerQueue queue;
        RestaurantWallet wallet;
        CounterStock stock;
        CounterDropZone drop;
        BurgerServingZone serving;
        DiningArea dining;
        CashFloor cash;
        ShopExpansion expansion;
        WorkerHiringZone crew;
        BoxingStation box => expansion.Boxing;
        int produced;

        [SetUp] public void SetUp()
        {
            produced = 0;
            root = new GameObject("BoxingFlowScenario");
            player = new GameObject("Player").AddComponent<BurgerInventory>();
            player.transform.SetParent(root.transform); player.Configure();
            player.transform.position = new Vector3(10, 1, -4);
            grill = root.AddComponent<ProductionStation>();
            grill.Configure(Point("GrillOutput", ShopLayout.Grill), null, null, 0.1f, 8);
            queue = root.AddComponent<CustomerQueue>(); queue.OrderQuantityFactory = () => 1;
            queue.Configure(ShopLayout.Entrance, ShopLayout.QueueEntry, ShopLayout.QueueSlots, ShopLayout.Counter);
            wallet = root.AddComponent<RestaurantWallet>();
            stock = root.AddComponent<CounterStock>(); stock.Configure(Point("Stock", ShopLayout.CounterTop), null);
            drop = root.AddComponent<CounterDropZone>();
            var circle = Point("Serving", ShopLayout.ServingCircle); drop.Configure(stock, circle);
            dining = DiningArea.Create(root.transform, ShopLayout.Tables);
            var trash = player.gameObject.AddComponent<TrashInventory>(); trash.Configure(); dining.BindCollector(trash);
            var bin = TrashBin.Create(root.transform, ShopLayout.TrashBin); bin.Configure(trash);
            cash = root.AddComponent<CashFloor>(); cash.Configure(wallet, player.transform, ShopLayout.CounterCash);
            serving = root.AddComponent<BurgerServingZone>();
            serving.Configure(queue, player, wallet, circle, ShopLayout.Exit, stock, dining, drop, 10, cash);
            crew = root.AddComponent<WorkerHiringZone>();
            crew.Configure(grill, serving, wallet, player, Point("Pickup", ShopLayout.GrillPickup),
                Point("Hire", ShopLayout.HrHirePoint), ShopLayout.Aisle, drop, null, dining, bin);
            expansion = ShopExpansion.Create(root.transform, dining, serving, crew, player, wallet, cash);
            expansion.Restore(false, false, false, 0, true, false);
        }
        [TearDown] public void TearDown() => Object.DestroyImmediate(root);
        Transform Point(string name, Vector3 position)
        {
            var point = new GameObject(name).transform; point.SetParent(root.transform); point.position = position; return point;
        }
        void Produce(float seconds)
        {
            int before = grill.Stock; grill.Advance(seconds); produced += grill.Stock - before;
        }
        void Give(BurgerInventory carrier, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Produce(1f); Assert.That(carrier.TryCollectFrom(grill), Is.True);
            }
        }
        void OpenLane()
        {
            expansion.Restore(false, false, false, 0, true, true);
            expansion.DriveThru.OrderQuantityFactory = () => 2;
        }
        void Step(float seconds, bool workers = false, bool production = false)
        {
            for (int frame = 0; frame < Mathf.CeilToInt(seconds * 60); frame++)
            {
                const float dt = 1f / 60;
                if (production) Produce(dt);
                queue.Advance(dt); serving.Advance(dt); box.Advance(dt); expansion.DriveThru?.Advance(dt);
                if (workers) foreach (var worker in crew.Workers) worker.Advance(dt);
                foreach (var table in dining.Tables) table.Advance(dt);
                foreach (var customer in root.GetComponentsInChildren<CustomerAgent>()) customer.AdvanceDeparture(dt);
                AssertConservation();
            }
        }
        void AssertConservation()
        {
            int carried = 0;
            foreach (var carrier in root.GetComponentsInChildren<BurgerInventory>())
            {
                Assert.That(carrier.Count, Is.LessThanOrEqualTo(carrier.Capacity));
                Assert.That(carrier.Count, Is.GreaterThanOrEqualTo(0));
                carried += carrier.Count;
            }
            int flight = (serving.IsHandoffActive ? 1 : 0) + (expansion.DriveThru != null && expansion.DriveThru.IsHandoffActive ? 1 : 0);
            int delivered = serving.DeliveredUnits + (expansion.DriveThru != null ? expansion.DriveThru.DeliveredUnits : 0);
            Assert.That(grill.Stock + serving.TotalStock + box.TableCount + box.PackageCount + carried + flight + delivered,
                Is.EqualTo(produced), "every produced burger must have one owner");
            Assert.That(box.InputCount, Is.InRange(0, 8));
            Assert.That(box.OutputCount + box.ProcessingCount, Is.InRange(0, 8));
            Assert.That(box.ProcessingCount, Is.InRange(0, 1));
            Assert.That(box.TotalProcessed, Is.LessThanOrEqualTo(box.TotalRawReceived));
        }
        static void BeginBoxWork(RestaurantWorker worker, Vector3 position, int goal)
        {
            worker.transform.position = position + Vector3.up;
            WorkerCommand(worker, "AssignBoxPickup", goal);
            typeof(RestaurantWorker).GetMethod("Begin", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(worker, new object[] { WorkerJob.Box, WorkerState.ToBoxing });
        }
        static void WorkerCommand(RestaurantWorker worker, string name, params object[] args) =>
            typeof(RestaurantWorker).GetMethod(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(worker, args);

        RestaurantWorker FillOutput(int count)
        {
            if (crew.HiredCount == 0) crew.RestoreWorkers(1, 0, 0);
            var worker = crew.Worker; BeginBoxWork(worker, box.CirclePosition, 0);
            for (int i = 0; i < count; i++) { Give(worker.Inventory, 1); Step(0.7f); }
            Assert.That(box.OutputCount, Is.EqualTo(count));
            return worker;
        }

        [Test] public void AC01FourRawBurgersTravelThroughTheTableBeforeReturningAsBoxes()
        {
            Give(player, 4); player.transform.position = box.CirclePosition;
            Assert.That(box.TryBoxFrom(player), Is.True);
            Assert.That(player.LooseCount, Is.EqualTo(3)); Assert.That(player.BoxedCount, Is.Zero);
            Assert.That(box.InputCount, Is.EqualTo(1)); Assert.That(box.TotalProcessed, Is.Zero);
            Step(0.3f);
            Assert.That(box.ProcessingCount, Is.EqualTo(1)); Assert.That(player.BoxedCount, Is.Zero);
            Step(4f);
            Assert.That(player.BoxedCount, Is.EqualTo(4)); Assert.That(player.LooseCount, Is.Zero);
            Assert.That(box.TableCount, Is.Zero); Assert.That(box.TotalProcessed, Is.EqualTo(4));
            Assert.That(box.PackageCount, Is.Zero, "holding boxes at BOX must not fill PACK");
        }

        [Test] public void AC02LeavingPausesOnlyProcessingAndOutputCapacityReservesTheInFlightSlot()
        {
            Give(player, 4); player.transform.position = box.CirclePosition;
            box.TryBoxFrom(player); Step(0.3f);
            float progress = box.ProcessingProgress; int total = box.TableCount;
            player.transform.position = new Vector3(10, 1, -4); Step(3f);
            Assert.That(box.ProcessingProgress, Is.EqualTo(progress));
            Assert.That(box.TableCount, Is.EqualTo(total)); Assert.That(box.PendingTransfers, Is.Zero);
            player.transform.position = box.CirclePosition; Step(3f);
            Assert.That(player.BoxedCount, Is.EqualTo(4));
            player.transform.position = box.DropPosition; Step(1.5f);
            Assert.That(box.PackageCount, Is.EqualTo(4));
            player.transform.position = new Vector3(10, 1, -4);
            OpenLane(); var worker = FillOutput(8);
            Give(worker.Inventory, 2); Step(1f);
            Assert.That(box.OutputCount, Is.EqualTo(8)); Assert.That(box.ProcessingCount, Is.Zero);
            Assert.That(box.InputCount, Is.EqualTo(2));
            int completed = box.TotalProcessed;
            BeginBoxWork(worker, box.CirclePosition, 1); Step(0.5f);
            Assert.That(box.TotalProcessed, Is.EqualTo(completed + 1));
            Assert.That(worker.Inventory.BoxedCount, Is.EqualTo(1));
            Assert.That(box.OutputCount, Is.EqualTo(8)); Assert.That(box.InputCount, Is.EqualTo(1));
        }

        [Test] public void AC03ThreeOperatorsProcessAndCollect30WithoutMultiplyingSpeedOrCapacity()
        {
            OpenLane(); crew.RestoreWorkers(2, 0, 0);
            player.Configure(6); player.transform.position = box.CirclePosition;
            foreach (var worker in crew.Workers)
            { worker.ApplyStaffTiers(1, 2); BeginBoxWork(worker, box.CirclePosition, worker.Inventory.Capacity); }
            BurgerInventory[] carriers = { player, crew.Workers[0].Inventory, crew.Workers[1].Inventory };
            int supplied = 0;
            for (int frame = 0; frame < 2400 && (supplied < 30 || box.TableCount > 0 || box.PendingTransfers > 0); frame++)
            {
                foreach (var carrier in carriers)
                {
                    while (carrier.BoxedCount > carrier.IncomingBoxes) Assert.That(box.Package.TryPlaceBoxedFrom(carrier), Is.True);
                    if (supplied < 30 && carrier.Count < carrier.Capacity - 1) { Give(carrier, 1); supplied++; }
                }
                Step(1f / 60);
                Assert.That(box.TotalProcessed, Is.LessThanOrEqualTo(Mathf.FloorToInt((frame + 1) / 60f / BoxingStation.BoxInterval)));
            }
            foreach (var carrier in carriers)
                while (carrier.BoxedCount > carrier.IncomingBoxes) box.Package.TryPlaceBoxedFrom(carrier);
            Assert.That(supplied, Is.EqualTo(30));
            Assert.That(box.TotalProcessed, Is.EqualTo(30)); Assert.That(box.TotalBoxPickups, Is.EqualTo(30));
            Assert.That(box.PackageCount, Is.EqualTo(30)); Assert.That(box.TableCount, Is.Zero);
            AssertConservation();
        }

        [Test] public void AC04BoxingWithoutDriveThruNeverDivertsEmployeesFromTenDiningOrders()
        {
            Step(12f, false, true); crew.RestoreWorkers(1, 0, 0);
            for (int second = 0; second < 300 && serving.CompletedOrders < 10; second++) Step(1f, true, true);
            Assert.That(serving.CompletedOrders, Is.GreaterThanOrEqualTo(10));
            Assert.That(box.TotalRawReceived, Is.Zero); Assert.That(box.TotalProcessed, Is.Zero);
            Assert.That(box.PackageCount, Is.Zero); Assert.That(crew.Worker.CompletedDeliveries, Is.EqualTo(serving.CompletedOrders));
        }

        [TestCase(1)] [TestCase(3)]
        public void AC05BothLinesSellDuring120SecondsOfAutonomousWork(int staff)
        {
            queue.OrderQuantityFactory = () => 3;
            OpenLane(); Step(12f, false, true);
            Assert.That(queue.ReadyCustomer, Is.Not.Null); Assert.That(expansion.DriveThru.HasStoppedCarAtWindow, Is.True);
            Assert.That(stock.Count + box.TableCount + box.PackageCount, Is.Zero);
            crew.RestoreWorkers(staff, 0, 0);
            var trace = new StringBuilder("seconds,staff,state,job,line,reservedRaw,raw,box,x,z\n");
            string[] last = new string[staff];
            for (int frame = 0; frame < 7200; frame++)
            {
                Step(1f / 60, true, true);
                for (int i = 0; i < staff; i++)
                {
                    var w = crew.Workers[i];
                    string state = $"{i},{w.State},{w.Job},{w.SupplyTarget},{w.ReservedRaw},{w.Inventory.LooseCount},{w.Inventory.BoxedCount}";
                    if (state != last[i])
                    {
                        trace.AppendLine(((frame + 1) / 60f).ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + "," + state
                            + "," + w.transform.position.x.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)
                            + "," + w.transform.position.z.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
                        last[i] = state;
                    }
                    Assert.That(ShopLayout.ContainsPlayable(w.transform.position), Is.True);
                }
            }
            string directory = Path.GetFullPath("Logs/spec023-025"); Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, $"spec025-crew-{staff}-trace.csv"), trace.ToString());
            File.WriteAllText(Path.Combine(directory, $"spec025-crew-{staff}-summary.json"), JsonUtility.ToJson(new RunSummary
            {
                staff = staff, seconds = 120, diningOrders = serving.CompletedOrders,
                driveOrders = expansion.DriveThru.CompletedOrders, diningStock = stock.Count,
                raw = box.InputCount, processing = box.ProcessingCount, output = box.OutputCount,
                pack = box.PackageCount, produced = produced, processed = box.TotalProcessed,
                deliveries = crew.TotalDeliveries, clears = crew.TotalClears
            }, true));
            Assert.That(serving.CompletedOrders, Is.GreaterThan(0), "dining must not starve");
            Assert.That(expansion.DriveThru.CompletedOrders, Is.GreaterThan(0), "boxes must reach PACK and a staffed window");
            Assert.That(crew.TotalDeliveries, Is.EqualTo(serving.CompletedOrders + expansion.DriveThru.CompletedOrders));
        }
        [Serializable] sealed class RunSummary
        {
            public int staff, seconds, diningOrders, driveOrders, diningStock, raw, processing, output, pack, produced, processed, deliveries, clears;
        }

        [Test] public void AC06ReadyOutputIsTransportedBeforeMoreRawAndRequiresAWindowOperator()
        {
            OpenLane(); var worker = FillOutput(2);
            worker.transform.position = ShopLayout.Aisle;
            WorkerCommand(worker, "AssignBoxPickup", 0);
            Assert.That(crew.TryAssignBoxTransport(worker), Is.True);
            Assert.That(worker.BoxPickupGoal, Is.EqualTo(2)); Assert.That(worker.ReservedRaw, Is.Zero);
            BeginBoxWork(worker, box.CirclePosition, 2); Step(1.5f);
            Assert.That(worker.Inventory.BoxedCount, Is.EqualTo(2)); Assert.That(box.PackageCount, Is.Zero);
            worker.transform.position = box.DropPosition;
            for (int i = 0; i < 60; i++) { Step(1f / 60); box.Drop.TryDepositFrom(worker.Inventory); }
            Assert.That(box.PackageCount, Is.EqualTo(2));
            worker.transform.position = ShopLayout.Aisle; Step(5f);
            Assert.That(expansion.DriveThru.CompletedOrders, Is.Zero); Assert.That(box.PackageCount, Is.EqualTo(2));
            player.transform.position = expansion.DriveThru.WindowPosition; Step(2f);
            Assert.That(expansion.DriveThru.CompletedOrders, Is.EqualTo(1));
            Assert.That(expansion.DriveThru.DeliveredUnits, Is.EqualTo(2)); Assert.That(box.PackageCount, Is.Zero);
        }

        [Test] public void OtherCounterStockCannotStrandAPartiallyDeliveredOrder()
        {
            queue.OrderQuantityFactory = () => 3;
            expansion.Restore(false, false, true, 0, true, false);
            for (int i = 0; i < 4; i++) { Give(player, 1); expansion.ExtraStock.TryPlaceFrom(player); }
            Give(player, 1); stock.TryPlaceFrom(player);
            Step(12f); var order = queue.ReadyCustomer.Order;
            player.transform.position = serving.ServingPosition;
            Step(0.5f);
            Assert.That(order.Delivered, Is.EqualTo(1));
            Assert.That(serving.TotalStock, Is.EqualTo(4));
            Assert.That(serving.ServiceableStock, Is.Zero);
            Assert.That(crew.DiningSupplyDeficit, Is.EqualTo(2));
            player.transform.position = new Vector3(10, 1, -4);
            crew.RestoreWorkers(1, 0, 0);
            for (int i = 0; i < 70 && !order.IsSettled; i++) Step(1f, true, true);
            Assert.That(order.IsSettled, Is.True);
            Assert.That(crew.TotalDeliveries, Is.GreaterThanOrEqualTo(1));
        }

        [Test] public void RawLeftOnTheTableGetsAnOperatorWithoutCollectingMoreRawFirst()
        {
            OpenLane(); Give(player, 1); player.transform.position = box.CirclePosition;
            box.TryBoxFrom(player); player.transform.position = new Vector3(10, 1, -4); Step(0.3f);
            Assert.That(box.InputCount, Is.EqualTo(1)); Assert.That(box.ProcessingCount, Is.Zero);
            crew.RestoreWorkers(1, 0, 0);
            Assert.That(crew.Worker.Job, Is.EqualTo(WorkerJob.Box));
            Assert.That(crew.Worker.ReservedRaw, Is.Zero);
            Step(35f, true, true);
            Assert.That(box.TotalProcessed, Is.GreaterThanOrEqualTo(1));
            Assert.That(box.PackageCount + expansion.DriveThru.DeliveredUnits, Is.GreaterThanOrEqualTo(1));
        }

        [Test] public void AC07AllDirtyTablesAndFullRawInputOutputCannotPermanentlyTrapTheCrew()
        {
            OpenLane(); var maker = FillOutput(8);
            for (int i = 0; i < 4; i++) { Give(maker.Inventory, 2); Step(0.7f); }
            Assert.That(box.InputCount, Is.EqualTo(8)); Assert.That(box.OutputCount, Is.EqualTo(8));
            crew.RestoreWorkers(3, 0, 0);
            foreach (var table in dining.Tables) table.LeaveMealTrash(0);
            foreach (var w in crew.Workers)
            {
                Give(w.Inventory, w.Inventory.Capacity);
                BeginBoxWork(w, box.CirclePosition, w.Inventory.Capacity);
                WorkerCommand(w, "AssignSupply", SupplyLine.Boxing, w.Inventory.LooseCount);
            }
            int cleared = 0;
            for (int second = 0; second < 120; second++)
            {
                Step(1f, true, true);
                foreach (var table in dining.Tables) if (!table.IsDirty) cleared++;
            }
            Assert.That(cleared, Is.GreaterThan(0));
            Assert.That(crew.TotalClears, Is.GreaterThan(0));
            Assert.That(expansion.DriveThru.CompletedOrders + serving.CompletedOrders, Is.GreaterThan(0));
        }

        [Test] public void AC08PauseFreezesTransfersAndThreeInteractionZonesAreDisjointAndReachable()
        {
            Give(player, 1); player.transform.position = box.CirclePosition; box.TryBoxFrom(player);
            typeof(BoxingStation).GetMethod("OnApplicationPause", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(box, new object[] { true });
            box.Advance(10f);
            Assert.That(box.PendingTransfers, Is.EqualTo(1)); Assert.That(box.TotalProcessed, Is.Zero);
            typeof(BoxingStation).GetMethod("OnApplicationPause", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(box, new object[] { false });
            Step(2f); Assert.That(player.BoxedCount, Is.EqualTo(1));
            OpenLane(); var lane = expansion.DriveThru;
            Assert.That(ShopLayout.Horizontal(box.CirclePosition, box.DropPosition), Is.GreaterThan(1.8f));
            Assert.That(ShopLayout.Horizontal(box.CirclePosition, lane.WindowPosition), Is.GreaterThan(1.9f));
            Assert.That(ShopLayout.Horizontal(box.DropPosition, lane.WindowPosition), Is.GreaterThan(1.9f));
            foreach (var position in new[] { box.CirclePosition, box.DropPosition, lane.WindowPosition })
            {
                Assert.That(ShopLayout.ContainsHall(position), Is.True);
                player.transform.position = position;
                int active = (box.IsActorInRange(player.transform) ? 1 : 0) + (box.Drop.IsInRangeOf(player.transform) ? 1 : 0)
                    + (lane.IsActorInRange(player.transform) ? 1 : 0);
                Assert.That(active, Is.EqualTo(1));
            }
            Assert.That(root.transform.Find("ShopExpansion/BoxingStation/RawCount").GetComponent<TextMesh>().text, Does.StartWith("RAW"));
            Assert.That(root.transform.Find("ShopExpansion/BoxingStation/BoxCount").GetComponent<TextMesh>().text, Does.StartWith("BOX"));
        }
    }
}
