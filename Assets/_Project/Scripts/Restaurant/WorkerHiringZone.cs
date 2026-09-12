using System.Collections.Generic;
using BurgerShop.Economy;
using BurgerShop.Player;
using UnityEngine;

namespace BurgerShop.Restaurant
{
    public sealed class WorkerHiringZone : MonoBehaviour
    {
        public const int MaxWorkers = 3;
        public static readonly int[] HireCosts = { 50, 100, 200 };

        ProductionStation grill;
        BurgerServingZone serving;
        CounterDropZone drop;
        DiningArea dining;
        TrashBin bin;
        RestaurantWallet wallet;
        BurgerInventory player;
        Transform pickupPoint;
        Transform hiringPoint;
        Vector3 aisleCorner;
        TextMesh marker;
        readonly List<RestaurantWorker> workers = new List<RestaurantWorker>();
        readonly List<ProductionStation> kitchens = new List<ProductionStation>(2);
        readonly List<Transform> kitchenPickups = new List<Transform>(2);
        readonly List<CounterDropZone> drops = new List<CounterDropZone>(2);
        BoxingStation boxing;
        DriveThruLane driveThru;
        StaffUpgradeBoard upgrades;
        float heldTime;
        bool purchasedThisVisit;
        SupplyLine nextSupply = SupplyLine.Dining;
        public const int SupplyBuffer = 4;
        public bool AllTablesDirty
        {
            get
            {
                if (dining == null || dining.TableCount == 0) return false;
                foreach (var table in dining.Tables) if (table != null && !table.IsDirty) return false;
                return true;
            }
        }
        // A partially served order owns its counter. Stock on a different counter cannot finish it.
        public int DiningSupplyDeficit => Mathf.Max(0, Mathf.Max(SupplyBuffer - (serving != null ? serving.TotalStock : 0),
            serving != null ? serving.ActiveOrderStockDeficit : 0) - ReservedFor(SupplyLine.Dining));
        public int BoxingSupplyDeficit => driveThru == null || boxing == null || !driveThru.isActiveAndEnabled || !boxing.isActiveAndEnabled ? 0
            : Mathf.Max(0, SupplyBuffer - boxing.OutputCount - boxing.ProcessingCount - boxing.PackageCount
                - HeldBoxes() - ReservedFor(SupplyLine.Boxing) - boxing.InputCount);

        int ReservedFor(SupplyLine line)
        {
            int total = 0;
            foreach (var worker in workers)
                if (worker != null && worker.isActiveAndEnabled && worker.SupplyTarget == line) total += worker.ReservedRaw;
            return total;
        }
        int HeldBoxes()
        {
            int total = 0;
            foreach (var worker in workers) if (worker != null && worker.Inventory != null) total += worker.Inventory.BoxedCount;
            return total;
        }
        public bool TryAssignSupply(RestaurantWorker worker)
        {
            int dine = DiningSupplyDeficit;
            int box = BoxingSupplyDeficit;
            if (worker == null || (dine == 0 && box == 0)) return false;
            SupplyLine line = dine > 0 && box > 0 ? nextSupply : dine > 0 ? SupplyLine.Dining : SupplyLine.Boxing;
            int amount = Mathf.Min(worker.Inventory.Capacity, line == SupplyLine.Dining ? dine : box);
            worker.AssignSupply(line, amount);
            nextSupply = line == SupplyLine.Dining ? SupplyLine.Boxing : SupplyLine.Dining;
            return true;
        }
        public bool TryAssignBoxTransport(RestaurantWorker worker)
        {
            if (worker == null || boxing == null || driveThru == null || !boxing.isActiveAndEnabled || !driveThru.isActiveAndEnabled) return false;
            int reserved = 0;
            foreach (var other in workers)
                if (other != null && other != worker && other.isActiveAndEnabled)
                    reserved += Mathf.Max(0, other.BoxPickupGoal - other.Inventory.BoxedCount);
            int available = boxing.OutputCount - reserved;
            int need = SupplyBuffer - boxing.PackageCount - HeldBoxes() - reserved;
            // Raw already on the table is committed to this line. Send an operator to finish it
            // when no unclaimed output is ready, instead of collecting another batch from a grill.
            int workAvailable = boxing.InputCount + boxing.ProcessingCount + boxing.OutputCount - reserved;
            int amount = Mathf.Min(worker.Inventory.Capacity, Mathf.Min(available > 0 ? available : workAvailable, need));
            if (amount <= 0) return false;
            worker.AssignBoxPickup(amount);
            return true;
        }
        const float Radius = 1.35f;
        const float HoldSeconds = 1.5f;

        public int HiredCount { get; private set; }
        public bool HasVisitedOffice { get; private set; }
        public bool IsFull => HiredCount >= MaxWorkers;
        public int HireCost => IsFull ? 0 : HireCosts[HiredCount];
        public bool IsHired => HiredCount > 0;
        public bool PurchasedThisVisit => purchasedThisVisit;
        public RestaurantWorker Worker => FirstLiveWorker();
        public IReadOnlyList<RestaurantWorker> Workers => workers;
        public float Progress => Mathf.Clamp01(heldTime / HoldSeconds);
        public long MissingCoins => IsFull || wallet == null ? 0 : System.Math.Max(0, HireCost - wallet.Coins);
        public Vector3 HiringPosition => hiringPoint != null ? hiringPoint.position : transform.position;
        public int TotalDeliveries
        {
            get
            {
                int total = 0;
                for (int i = 0; i < workers.Count; i++)
                    if (workers[i] != null) total += workers[i].CompletedDeliveries;
                return total;
            }
        }
        public int TotalClears
        {
            get
            {
                int total = 0;
                for (int i = 0; i < workers.Count; i++)
                    if (workers[i] != null) total += workers[i].CompletedClears;
                return total;
            }
        }
        public bool IsAvailable => isActiveAndEnabled && grill != null && grill.isActiveAndEnabled
            && serving != null && serving.isActiveAndEnabled && wallet != null && wallet.isActiveAndEnabled
            && player != null && player.isActiveAndEnabled && pickupPoint != null
            && drop != null && drop.isActiveAndEnabled;
        public bool IsInRange
        {
            get
            {
                if (player == null) return false;
                Vector3 offset = player.transform.position - HiringPosition;
                offset.y = 0f;
                return offset.sqrMagnitude <= Radius * Radius;
            }
        }

        public void Configure(ProductionStation source, BurgerServingZone cashier, RestaurantWallet earnings,
            BurgerInventory carrier, Transform pickup, Transform point, Vector3 aisle, CounterDropZone dropZone,
            TextMesh label = null, DiningArea hall = null, TrashBin trashBin = null)
        {
            grill = source;
            serving = cashier;
            drop = dropZone;
            dining = hall;
            bin = trashBin;
            wallet = earnings;
            player = carrier;
            pickupPoint = pickup;
            hiringPoint = point;
            aisleCorner = aisle;
            marker = label;
            heldTime = 0f;
            purchasedThisVisit = false;
            HasVisitedOffice = false;
            kitchens.Clear();
            kitchenPickups.Clear();
            drops.Clear();
            if (source != null) kitchens.Add(source);
            if (pickup != null) kitchenPickups.Add(pickup);
            if (dropZone != null) drops.Add(dropZone);
        }

        public void BindUpgrades(StaffUpgradeBoard board) => upgrades = board;

        public void RegisterKitchen(ProductionStation station, Transform pickup)
        {
            if (station != null && !kitchens.Contains(station))
            {
                kitchens.Add(station);
                kitchenPickups.Add(pickup);
            }
        }

        public void RegisterDrop(CounterDropZone extra)
        {
            if (extra != null && !drops.Contains(extra)) drops.Add(extra);
        }

        public BoxingStation Boxing => boxing;
        public DriveThruLane DriveThru => driveThru;

        public void RegisterBoxing(BoxingStation station)
        {
            boxing = station;
            foreach (var worker in workers) if (worker != null) boxing?.RegisterOperator(worker.Inventory);
        }

        public void RegisterDriveThru(DriveThruLane lane) => driveThru = lane;

        public bool MayGoWindow(RestaurantWorker worker)
        {
            for (int i = 0; i < workers.Count; i++)
            {
                RestaurantWorker other = workers[i];
                if (other == null || other == worker || !other.isActiveAndEnabled) continue;
                if (other.Job == WorkerJob.DriveSell) return false;
            }
            return true;
        }

        public bool AnyGrillHasStock()
        {
            for (int i = 0; i < kitchens.Count; i++)
                if (kitchens[i] != null && kitchens[i].isActiveAndEnabled && kitchens[i].Stock > 0)
                    return true;
            return grill != null && grill.isActiveAndEnabled && grill.Stock > 0;
        }

        public bool TryGetCollectTarget(out ProductionStation station, out Transform pickup)
        {
            station = grill;
            pickup = pickupPoint;
            for (int i = 0; i < kitchens.Count; i++)
            {
                if (kitchens[i] == null || !kitchens[i].isActiveAndEnabled || kitchens[i].Stock <= 0) continue;
                station = kitchens[i];
                pickup = i < kitchenPickups.Count && kitchenPickups[i] != null ? kitchenPickups[i] : pickupPoint;
                return true;
            }
            return station != null && pickup != null;
        }

        void Update() => Advance(Time.deltaTime);

        public void RestoreWorker(bool hired, int deliveries) => RestoreWorkers(hired ? 1 : 0, deliveries);

        public void RestoreWorkers(int count, int deliveries, int clears = 0)
        {
            if (count < 0 || count > MaxWorkers) throw new System.ArgumentOutOfRangeException(nameof(count));
            if (deliveries < 0 || clears < 0 || (count == 0 && (deliveries != 0 || clears != 0)))
                throw new System.ArgumentOutOfRangeException(nameof(deliveries));
            DespawnAll();
            HiredCount = 0;
            heldTime = 0f;
            purchasedThisVisit = false;
            HasVisitedOffice = false;
            for (int i = 0; i < count; i++) SpawnWorker();
            if (workers.Count > 0)
            {
                workers[0].RestoreDeliveries(deliveries);
                workers[0].RestoreClears(clears);
            }
        }

        public void Advance(float deltaTime)
        {
            if (deltaTime <= 0f) return;
            if (player != null && ShopLayout.ContainsHrOffice(player.transform.position))
                HasVisitedOffice = true;
            if (!IsInRange)
            {
                heldTime = 0f;
                purchasedThisVisit = false;
                return;
            }
            if (IsFull || purchasedThisVisit || !IsAvailable || MissingCoins > 0)
            {
                heldTime = 0f;
                return;
            }
            heldTime += Mathf.Min(deltaTime, 0.1f);
            if (heldTime + 0.0001f < HoldSeconds) return;
            int cost = HireCost;
            purchasedThisVisit = true;
            heldTime = 0f;
            if (!wallet.TrySpend(cost))
            {
                purchasedThisVisit = false;
                return;
            }
            SpawnWorker();
        }

        void SpawnWorker()
        {
            int slot = HiredCount;
            RestaurantWorker worker = RestaurantWorker.Create(transform, StaffSpawn(slot), grill, serving,
                pickupPoint, aisleCorner, drop, dining, bin, this, slot);
            workers.Add(worker);
            HiredCount++;
            upgrades?.ApplyTo(worker);
            boxing?.RegisterOperator(worker.Inventory);
        }

        Vector3 StaffSpawn(int slot)
        {
            Vector3 hire = HiringPosition;
            if (ShopLayout.ContainsHrOffice(hire) || hire.x >= ShopLayout.WallHalf - 0.25f)
                return ShopLayout.StaffEntry(slot);
            return hire + new Vector3(-1.4f, 0f, (slot % 3 - 1) * 0.7f);
        }

        void DespawnAll()
        {
            for (int i = 0; i < workers.Count; i++)
                if (workers[i] != null)
                    BurgerVisual.Release(workers[i].gameObject);
            workers.Clear();
        }

        public DiningTable ReservedTable(RestaurantWorker worker)
        {
            if (dining == null || !dining.isActiveAndEnabled || dining.TableCount == 0) return null;
            if (worker != null && worker.AssignedTable != null && worker.AssignedTable.IsDirty)
                return worker.AssignedTable;
            for (int t = 0; t < dining.TableCount; t++)
            {
                DiningTable table = dining.Tables[t];
                if (table == null || !table.IsDirty) continue;
                bool taken = false;
                for (int i = 0; i < workers.Count; i++)
                {
                    RestaurantWorker other = workers[i];
                    if (other == null || other == worker || !other.isActiveAndEnabled) continue;
                    if (other.AssignedTable == table) { taken = true; break; }
                }
                if (!taken) return table;
            }
            return null;
        }

        public bool MayGoServe(RestaurantWorker worker)
        {
            for (int i = 0; i < workers.Count; i++)
            {
                RestaurantWorker other = workers[i];
                if (other == null || other == worker || !other.isActiveAndEnabled) continue;
                if (other.Job == WorkerJob.Serve) return false;
            }
            return true;
        }

        RestaurantWorker FirstLiveWorker()
        {
            for (int i = 0; i < workers.Count; i++)
                if (workers[i] != null) return workers[i];
            return null;
        }

        void OnDisable() => heldTime = 0f;

        void LateUpdate()
        {
            if (marker == null) return;
            marker.text = IsFull ? "Staff full\n3 / 3" : $"Hire staff\n{HireCost}";
            marker.gameObject.SetActive(!IsInRange);
            if (Camera.main != null) marker.transform.rotation = Camera.main.transform.rotation;
        }
    }
}
