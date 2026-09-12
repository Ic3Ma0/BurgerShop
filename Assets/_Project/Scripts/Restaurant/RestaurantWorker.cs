using BurgerShop.Player;
using UnityEngine;

namespace BurgerShop.Restaurant
{
    public enum WorkerState
    {
        ToGrill, Collecting, ToCounter, Serving, ToTrash, CollectingTrash, ToBin, Dumping,
        ToBoxing, Boxing, ToPackage, Packing, ToWindow, SellingWindow, ToBagMachine, BagMachine, ToBagTable, BagTable, ToBagCounter, BagCounter
    }
    public enum SupplyLine { None, Dining, Boxing, Bag, Cola }
    public enum WorkerJob { Idle, Collect, Stock, Serve, ServeCola, Clean, Box, Pack, DriveSell, Bag, BagSell }

    [ExecuteAlways]
    public sealed class RestaurantWorker : MonoBehaviour
    {
        static readonly WorkerLook[] Looks =
        {
            new WorkerLook('A', new Color(0.13f, 0.58f, 0.64f), new Color(0.98f, 0.96f, 0.89f),
                true, false, true, false, false, 1f, 0.94f, new Vector3(0.62f, 0.1f, 0.62f)),
            new WorkerLook('B', new Color(0.86f, 0.42f, 0.16f), new Color(0.28f, 0.16f, 0.10f),
                false, true, false, true, false, 0.88f, 0.82f, new Vector3(0.52f, 0.28f, 0.52f)),
            new WorkerLook('C', new Color(0.42f, 0.28f, 0.62f), new Color(0.93f, 0.86f, 0.62f),
                true, false, false, false, true, 1.14f, 1.08f, new Vector3(0.48f, 0.22f, 0.48f))
        };

        ProductionStation grill;
        BurgerServingZone serving;
        CounterDropZone drop;
        DiningArea dining;
        TrashBin bin;
        WorkerHiringZone crew;
        Transform pickupPoint;
        Vector3 aisleCorner;
        Vector3[] route;
        DiningTable cleaningTable;
        int waypoint;
        float pickupCooldown;
        Material[] ownedMaterials;
        TextMesh label;
        float walkSpeed = StaffBoost.WalkSpeed(0);
        BurgerShop.Customer.CustomerOrder serviceOrder;
        bool preferWindow;
        bool preferBag=true;
        BagLine Bag=>crew!=null?crew.BagLine:null;
        internal void RecordBagOrder(){CompletedDeliveries++;preferBag=false;}
        public SupplyLine SupplyTarget { get; private set; }
        public int ReservedRaw { get; private set; }
        public int BoxPickupGoal { get; private set; }
        public bool IsBoxOperator => BoxingReady && DriveThru != null && DriveThru.isActiveAndEnabled && Job == WorkerJob.Box;
        public bool WantsBoxPickup => BoxPickupGoal > (Inventory != null ? Inventory.BoxedCount : 0);
        internal void AssignSupply(SupplyLine line, int quantity)
        {
            SupplyTarget = line;
            ReservedRaw = quantity;
            BoxPickupGoal = line == SupplyLine.Boxing ? quantity : 0;
        }
        internal void AssignBoxPickup(int quantity) { SupplyTarget = SupplyLine.None; ReservedRaw = 0; BoxPickupGoal = quantity; }
        internal void RawDeposited(int count) => ReservedRaw = Mathf.Max(0, ReservedRaw - count);
        void ClearSupply() { SupplyTarget = SupplyLine.None; ReservedRaw = 0; }


        public BurgerInventory Inventory { get; private set; }
        public TrashInventory Trash { get; private set; }
        public WorkerState State { get; private set; }
        public WorkerJob Job { get; private set; }
        public int Slot { get; private set; }
        public char StyleLetter { get; private set; }
        public Color UniformColor { get; private set; }
        public float HeightScale { get; private set; }
        public bool WearsHat { get; private set; }
        public DiningTable AssignedTable => cleaningTable;
        public float WalkSpeed => walkSpeed;
        public int CompletedDeliveries { get; private set; }
        public int CompletedClears { get; private set; }

        public void ApplyStaffTiers(int speedTier, int carryTier)
        {
            walkSpeed = StaffBoost.WalkSpeed(speedTier);
            Inventory?.Configure(StaffBoost.CarryCapacity(carryTier));
        }
        internal void RecordCompletedOrder(bool window = false)
        {
            CompletedDeliveries++;preferBag=true;
            preferWindow = !window;
        }
        internal void RestoreDeliveries(int count) => CompletedDeliveries = count;
        internal void RestoreClears(int count) => CompletedClears = count;
        public string Activity => !isActiveAndEnabled || !DependenciesReady ? "Paused"
            : Job == WorkerJob.Idle ? "Ready to work"
            : State == WorkerState.ToGrill ? (SupplyTarget == SupplyLine.Cola ? "Walking to cola" : "Walking to grill")
            : State == WorkerState.Collecting ? (SupplyTarget == SupplyLine.Cola ? "Waiting for cola" : "Waiting for burgers")
            : State == WorkerState.ToCounter && (Job == WorkerJob.Serve || Job == WorkerJob.ServeCola) ? "Walking to serve"
            : State == WorkerState.ToCounter ? "Carrying to counter"
            : State == WorkerState.ToTrash ? "Walking to a dirty table"
            : State == WorkerState.CollectingTrash ? "Clearing a table"
            : State == WorkerState.ToBin ? "Taking trash to the bin"
            : State == WorkerState.Dumping ? "Dumping trash"
            : State == WorkerState.ToBoxing || State == WorkerState.Boxing ? "Boxing a burger"
            : State == WorkerState.ToPackage || State == WorkerState.Packing ? "Stocking the package counter"
            : State == WorkerState.ToWindow || State == WorkerState.SellingWindow ? "Selling a combo"
            : Job == WorkerJob.ServeCola ? "Serving a cola customer"
            : Job == WorkerJob.Serve ? "Serving a customer"
            : Inventory != null && Inventory.Count > 0 ? "Stocking the counter" : "Waiting to serve";
        bool DependenciesReady => grill != null && grill.isActiveAndEnabled && serving != null
            && serving.isActiveAndEnabled && Inventory != null && Inventory.isActiveAndEnabled && pickupPoint != null
            && drop != null && drop.isActiveAndEnabled && drop.Stock != null && drop.Stock.isActiveAndEnabled;
        bool CanClean => dining != null && dining.isActiveAndEnabled && bin != null && bin.isActiveAndEnabled
            && Trash != null && Trash.isActiveAndEnabled;
        bool CarryingFood => Inventory != null && Inventory.Count > 0;
        bool CarryingLoose => Inventory != null && Inventory.LooseCount > 0;
        bool CarryingCola => Inventory != null && Inventory.ColaCount > 0;
        bool CarryingBoxed => Inventory != null && Inventory.BoxedCount > 0;
        bool CarryingTrash => Trash != null && Trash.Count > 0;
        bool UsingCola => Job == WorkerJob.ServeCola || SupplyTarget == SupplyLine.Cola || CarryingCola;
        BoxingStation Boxing => crew != null ? crew.Boxing : null;
        DriveThruLane DriveThru => crew != null ? crew.DriveThru : null;
        BurgerServingZone ColaLine => crew != null ? crew.ColaServing : null;
        CounterDropZone ColaDropLine => crew != null ? crew.ColaDrop : null;
        BurgerServingZone ActiveServing => UsingCola && ColaLine != null ? ColaLine : serving;
        CounterDropZone ActiveDrop => UsingCola && ColaDropLine != null ? ColaDropLine : drop;
        bool BoxingReady => Boxing != null && Boxing.isActiveAndEnabled;
        bool DriveThruReady => DriveThru != null && DriveThru.isActiveAndEnabled && DriveThru.ReadyToSell
            && (crew == null || crew.MayGoWindow(this));
        DiningTable DirtyTable => crew != null ? crew.ReservedTable(this) : dining != null ? dining.FindDirtyTable() : null;

        public void Configure(ProductionStation source, BurgerServingZone cashier, Transform pickup,
            Vector3 corner, BurgerInventory inventory, CounterDropZone dropZone,
            DiningArea diningArea = null, TrashBin trashBin = null, TrashInventory trashBag = null,
            WorkerHiringZone hiring = null, int slot = 0)
        {
            grill = source;
            serving = cashier;
            pickupPoint = pickup;
            aisleCorner = corner;
            Inventory = inventory;
            drop = dropZone;
            dining = diningArea;
            bin = trashBin;
            Trash = trashBag;
            crew = hiring;
            Slot = Mathf.Clamp(slot, 0, Looks.Length - 1);
            ChooseJob();
        }

        void Update()
        {
            if (Application.isPlaying) Advance(Time.deltaTime);
        }

        public void Advance(float deltaTime)
        {
            if (deltaTime <= 0f || !isActiveAndEnabled || !DependenciesReady) return;
            Trash?.AdvanceDumps(deltaTime);
            if (State == WorkerState.ToGrill || State == WorkerState.ToCounter
                || State == WorkerState.ToTrash || State == WorkerState.ToBin
                || State == WorkerState.ToBoxing || State == WorkerState.ToPackage
                || State == WorkerState.ToWindow || State == WorkerState.ToBagMachine || State == WorkerState.ToBagTable || State == WorkerState.ToBagCounter)
            {
                if (MoveAlongRoute(deltaTime))
                {
                    State = State == WorkerState.ToGrill ? WorkerState.Collecting
                        : State == WorkerState.ToCounter ? WorkerState.Serving
                        : State == WorkerState.ToTrash ? WorkerState.CollectingTrash
                        : State == WorkerState.ToBin ? WorkerState.Dumping
                        : State == WorkerState.ToBoxing ? WorkerState.Boxing
                        : State == WorkerState.ToPackage ? WorkerState.Packing
                        : State == WorkerState.ToBagMachine ? WorkerState.BagMachine
                        : State == WorkerState.ToBagTable ? WorkerState.BagTable
                        : State == WorkerState.ToBagCounter ? WorkerState.BagCounter
                        : WorkerState.SellingWindow;
                    pickupCooldown = 0f;
                }
                KeepInPlayable();
                return;
            }
            if(State==WorkerState.BagMachine||State==WorkerState.BagTable||State==WorkerState.BagCounter){TickBag();KeepInPlayable();return;}
            if (State == WorkerState.Collecting)
            {
                TickCollecting(deltaTime);
                KeepInPlayable();
                return;
            }
            if (State == WorkerState.CollectingTrash)
            {
                TickCleaning(deltaTime);
                KeepInPlayable();
                return;
            }
            if (State == WorkerState.Dumping)
            {
                TickDumping(deltaTime);
                KeepInPlayable();
                return;
            }
            if (State == WorkerState.Boxing)
            {
                TickBoxing(deltaTime);
                KeepInPlayable();
                return;
            }
            if (State == WorkerState.Packing)
            {
                TickPacking(deltaTime);
                KeepInPlayable();
                return;
            }
            if (State == WorkerState.SellingWindow)
            {
                TickWindow();
                KeepInPlayable();
                return;
            }
            TickServing();
            KeepInPlayable();
        }

        void TickBoxing(float deltaTime)
        {
            if (!IsBoxOperator) { ClearSupply(); ChooseJob(); return; }
            if (!Boxing.IsActorInRange(transform)) { Begin(WorkerJob.Box, WorkerState.ToBoxing); return; }
            // The station owns all transfer and processing clocks. The worker only chooses when to leave.
            if (CarryingLoose && Inventory.IsFull && Boxing.InputFull && Boxing.OutputFull)
            {
                AssignSupply(SupplyLine.Dining, Inventory.LooseCount);
                Begin(WorkerJob.Stock, WorkerState.ToCounter);
                return;
            }
            if (Inventory.IncomingBoxes > 0) return;
            if (CarryingLoose) return;
            if (CarryingBoxed && (Inventory.IsFull || Inventory.BoxedCount >= BoxPickupGoal || Boxing.TableCount == 0))
            {
                ClearSupply();
                Begin(WorkerJob.Pack, WorkerState.ToPackage);
                return;
            }
            if (!CarryingBoxed && Boxing.TableCount == 0)
            {
                BoxPickupGoal = 0; ClearSupply(); ChooseJob();
            }
        }

        void TickPacking(float deltaTime)
        {
            if (!BoxingReady || Boxing.Drop == null)
            {
                ChooseJob();
                return;
            }
            if (!Boxing.Drop.IsInRangeOf(transform))
            {
                Begin(WorkerJob.Pack, WorkerState.ToPackage);
                return;
            }
            Boxing.Drop.TryDepositFrom(Inventory);
            if (!CarryingBoxed)
            {
                BoxPickupGoal = 0; ClearSupply(); ChooseJob();
            }
        }

        void TickWindow()
        {
            if (DriveThru == null || !DriveThru.isActiveAndEnabled) { ChooseJob(); return; }
            if (!DriveThru.IsActorInRange(transform)) { Begin(WorkerJob.DriveSell, WorkerState.ToWindow); return; }
            if (serviceOrder != null && serviceOrder.IsSettled) { serviceOrder = null; ChooseJob(); return; }
            if (DriveThru.TrySellFrom(Inventory)) serviceOrder = DriveThru.WaitingOrder;
            if (DriveThru.IsHandoffActive || (DriveThru.HasStoppedCarAtWindow && Boxing != null && Boxing.PackageCount > 0)) return;
            serviceOrder = null; ChooseJob();
        }

        void TickCollecting(float deltaTime)
        {
            ResolveKitchen();
            if (Job == WorkerJob.Idle)
            {
                ChooseJob();
                if (State != WorkerState.Collecting) return;
            }
            if (grill.Stock == 0 && Inventory.Count == 0)
            {
                if (ReservedRaw > 0) return;
                ChooseJob();
                return;
            }
            pickupCooldown = Mathf.Max(0f, pickupCooldown - deltaTime);
            Vector3 offset = transform.position - CollectStand();
            offset.y = 0f;
            if (offset.sqrMagnitude > 1f) { Begin(WorkerJob.Collect, WorkerState.ToGrill); return; }
            if (!Inventory.IsFull && pickupCooldown <= 0f && Inventory.TryCollectFrom(grill))
                pickupCooldown = 0.35f;
            int held = SupplyTarget == SupplyLine.Cola ? Inventory.ColaCount : Inventory.LooseCount;
            int target = crew != null ? Mathf.Max(1, ReservedRaw) : Inventory.Capacity;
            if (Inventory.IsFull || held >= target || (Inventory.Count > 0 && grill.Stock == 0))
            {
                if (crew != null) ReservedRaw = Mathf.Min(ReservedRaw, held);
                if (Bag!=null && Bag.CounterBuilt && SupplyTarget==SupplyLine.Bag && CarryingLoose)
                    Begin(WorkerJob.Bag,WorkerState.ToBagTable);
                else if (BoxingReady && SupplyTarget == SupplyLine.Boxing && CarryingLoose)
                {
                    BoxPickupGoal = Inventory.LooseCount;
                    Begin(WorkerJob.Box, WorkerState.ToBoxing);
                }
                else
                    Begin(WorkerJob.Stock, WorkerState.ToCounter);
            }
        }

        void TickServing()
        {
            Vector3 servingOffset = transform.position - ServingStand();
            servingOffset.y = 0f;
            if (servingOffset.sqrMagnitude > 0.7f * 0.7f)
            {
                WorkerJob travel = CarryingFood ? WorkerJob.Stock
                    : Job == WorkerJob.ServeCola ? WorkerJob.ServeCola
                    : Job == WorkerJob.Serve ? WorkerJob.Serve
                    : WorkerJob.Stock;
                Begin(travel, WorkerState.ToCounter);
                return;
            }
            int before = Inventory.LooseCount + Inventory.ColaCount;
            BurgerServingZone cashier = ActiveServing;
            CounterDropZone pile = ActiveDrop;
            if (cashier != null) cashier.TryDepositFrom(Inventory);
            else pile?.TryDepositFrom(Inventory);
            RawDeposited(before - Inventory.LooseCount - Inventory.ColaCount);
            if (serviceOrder != null && serviceOrder.IsSettled && !CarryingLoose && !CarryingCola)
            { serviceOrder = null; ClearSupply(); ChooseJob(); return; }
            if (cashier != null && cashier.TryServeFrom(Inventory))
                serviceOrder = cashier.ActiveCustomer.Order;
            if (CarryingBoxed) { Begin(WorkerJob.Pack, WorkerState.ToPackage); return; }
            if (CarryingCola)
            {
                if (!UsingCola) Begin(WorkerJob.Stock, WorkerState.ToCounter);
                return;
            }
            if (CarryingLoose)
            {
                if (UsingCola) Begin(WorkerJob.Stock, WorkerState.ToCounter);
                return;
            }
            ClearSupply();
            if (cashier != null && (cashier.IsHandoffActive || (cashier.HasReadyCustomer && cashier.ServiceableStock > 0)))
            {
                Job = cashier == ColaLine ? WorkerJob.ServeCola : WorkerJob.Serve;
                return;
            }
            serviceOrder = null; ChooseJob();
        }

        void TickCleaning(float deltaTime)
        {
            if (cleaningTable == null || !cleaningTable.isActiveAndEnabled)
            {
                cleaningTable = DirtyTable;
                if (cleaningTable == null)
                {
                    if (CarryingTrash) Begin(WorkerJob.Clean, WorkerState.ToBin);
                    else ChooseJob();
                    return;
                }
            }
            cleaningTable.Advance(deltaTime);
            pickupCooldown = Mathf.Max(0f, pickupCooldown - deltaTime);
            if (!cleaningTable.IsActorInRange(transform))
            {
                Begin(WorkerJob.Clean, WorkerState.ToTrash);
                return;
            }
            if (cleaningTable.IsDirty && pickupCooldown <= 0f
                && cleaningTable.TryBeginPickup(Trash, out TrashMotion started))
            {
                pickupCooldown = 0.35f;
                started.Advance(deltaTime);
            }
            if (!cleaningTable.IsDirty && !cleaningTable.HasPendingPickups)
            {
                if (CarryingTrash) Begin(WorkerJob.Clean, WorkerState.ToBin);
                else ChooseJob();
            }
        }

        void TickDumping(float deltaTime)
        {
            if (bin == null || Trash == null)
            {
                ChooseJob();
                return;
            }
            if (!bin.IsActorInRange(transform))
            {
                Begin(WorkerJob.Clean, WorkerState.ToBin);
                return;
            }
            pickupCooldown = Mathf.Max(0f, pickupCooldown - deltaTime);
            if (pickupCooldown <= 0f && Trash.Count > 0 && bin.TryDumpFrom(Trash, out TrashMotion started))
            {
                pickupCooldown = 0.35f;
                CompletedClears++;
                started.Advance(deltaTime);
            }
            if (Trash.Count == 0)
                ChooseJob();
        }

        void TickBag()
        {
            if(Bag==null||!Bag.CounterBuilt){ClearSupply();ChooseJob();return;}
            if(State==WorkerState.BagCounter)
            {
                if(Inventory.BaggedCount>0)return;
                if(Job==WorkerJob.BagSell&&Bag.ReadyToSell){Bag.TryServe(Inventory);return;}
                // An in-flight handoff is owned by the line; its completion records the worker.
                if(Job==WorkerJob.BagSell&&Bag.Queue.ReadyCustomer!=null&&Bag.StockCount>0)return;
                ClearSupply();ChooseJob();return;
            }
            if(State==WorkerState.BagMachine)
            {
                int need=Mathf.Max(0,Bag.InputBurgers-Bag.InputBags);
                if(Inventory.IsFull||Inventory.EmptyBagCount>=Mathf.Min(Inventory.Capacity,need)||(Inventory.EmptyBagCount>0&&Bag.EmptyStock==0))Begin(WorkerJob.Bag,WorkerState.ToBagTable);
                else if(need==0){ClearSupply();ChooseJob();}
                return;
            }
            if(Inventory.LooseCount>0||Inventory.EmptyBagCount>0)return;
            if(Inventory.BaggedCount>0){Begin(WorkerJob.Bag,WorkerState.ToBagCounter);return;}
            if(Bag.InputBurgers>Bag.InputBags&&Bag.InputBags==0){Begin(WorkerJob.Bag,WorkerState.ToBagMachine);return;}
            if(!Bag.HasWork){ClearSupply();ChooseJob();}
        }

        void ChooseJob()
        {
            if(Bag!=null&&Bag.CounterBuilt)
            {
                if(Inventory!=null&&Inventory.BaggedCount>0){Begin(WorkerJob.Bag,WorkerState.ToBagCounter);return;}
                if(Inventory!=null&&Inventory.EmptyBagCount>0){Begin(WorkerJob.Bag,WorkerState.ToBagTable);return;}
                if(CarryingLoose&&SupplyTarget==SupplyLine.Bag){Begin(WorkerJob.Bag,WorkerState.ToBagTable);return;}
            }
            if (CarryingBoxed && BoxingReady) { Begin(WorkerJob.Pack, WorkerState.ToPackage); return; }
            if (CarryingCola)
            {
                AssignSupply(SupplyLine.Cola, Inventory.ColaCount);
                Begin(WorkerJob.Stock, WorkerState.ToCounter);
                return;
            }
            if (CarryingLoose)
            {
                if (BoxingReady && DriveThru != null && SupplyTarget == SupplyLine.Boxing)
                    Begin(WorkerJob.Box, WorkerState.ToBoxing);
                else
                {
                    AssignSupply(SupplyLine.Dining, Inventory.LooseCount);
                    Begin(WorkerJob.Stock, WorkerState.ToCounter);
                }
                return;
            }
            if (CarryingTrash) { Begin(WorkerJob.Clean, WorkerState.ToBin); return; }
            ClearSupply(); BoxPickupGoal = 0;
            DiningTable dirty = CanClean ? DirtyTable : null;
            if (dirty != null && crew != null && crew.AllTablesDirty)
            {
                cleaningTable = dirty; Begin(WorkerJob.Clean, WorkerState.ToTrash); return;
            }
            bool dine = serving != null && serving.ReadyToSell && (crew == null || crew.MayGoServe(this));
            bool colaSell = ColaLine != null && ColaLine.ReadyToSell && (crew == null || crew.MayGoServeCola(this));
            bool window = DriveThruReady;
            bool bagReady=Bag!=null&&Bag.ReadyToSell&&crew!=null&&crew.BagAvailableFor(this);
            if(bagReady&&(preferBag||(!dine&&!colaSell&&!window))){cleaningTable=null;Begin(WorkerJob.BagSell,WorkerState.ToBagCounter);return;}
            if (window && (!(dine || colaSell) || preferWindow))
            { cleaningTable = null; Begin(WorkerJob.DriveSell, WorkerState.ToWindow); return; }
            if (dine)
            { cleaningTable = null; Begin(WorkerJob.Serve, WorkerState.ToCounter); return; }
            if (colaSell)
            { cleaningTable = null; Begin(WorkerJob.ServeCola, WorkerState.ToCounter); return; }
            if (window)
            { cleaningTable = null; Begin(WorkerJob.DriveSell, WorkerState.ToWindow); return; }
            if (dirty != null)
            { cleaningTable = dirty; Begin(WorkerJob.Clean, WorkerState.ToTrash); return; }
            cleaningTable = null;
            if (crew != null && crew.TryAssignBoxTransport(this))
            { Begin(WorkerJob.Box, WorkerState.ToBoxing); return; }
            if(Bag!=null&&Bag.HasWork&&crew.BagAvailableFor(this)){Begin(WorkerJob.Bag,WorkerState.ToBagTable);return;}
            bool grillHas = crew != null ? crew.AnyGrillHasStock() : grill != null && grill.isActiveAndEnabled && grill.Stock > 0;
            bool colaHas = crew != null && crew.AnyColaHasStock();
            if ((grillHas || colaHas) && Inventory != null && !Inventory.IsFull && (crew == null || crew.TryAssignSupply(this)))
            { Begin(WorkerJob.Collect, WorkerState.ToGrill); return; }
            Begin(WorkerJob.Idle, WorkerState.ToGrill);
        }

        void Begin(WorkerJob job, WorkerState travel)
        {
            Job = job;
            Vector3 dest = DestinationFor(travel);
            Vector3 offset = dest - transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude <= 1f)
            {
                State = travel == WorkerState.ToGrill ? WorkerState.Collecting
                    : travel == WorkerState.ToCounter ? WorkerState.Serving
                    : travel == WorkerState.ToTrash ? WorkerState.CollectingTrash
                    : travel == WorkerState.ToBin ? WorkerState.Dumping
                    : travel == WorkerState.ToBoxing ? WorkerState.Boxing
                    : travel == WorkerState.ToPackage ? WorkerState.Packing
                    : travel == WorkerState.ToWindow ? WorkerState.SellingWindow
                    : travel == WorkerState.ToBagMachine ? WorkerState.BagMachine
                    : travel == WorkerState.ToBagTable ? WorkerState.BagTable
                    : travel == WorkerState.ToBagCounter ? WorkerState.BagCounter
                    : WorkerState.Collecting;
                pickupCooldown = 0f;
                return;
            }
            StartTrip(travel);
        }

        void StartTrip(WorkerState state)
        {
            State = state;
            Vector3 destination = DestinationFor(state);
            if(destination.x < -15 || transform.position.x < -15)
            {
                var points=new System.Collections.Generic.List<Vector3>();
                bool fromWest=transform.position.x < -15, toWest=destination.x < -15;
                if(fromWest){points.Add(AtHeight(new Vector3(-20,0,transform.position.z)));points.Add(AtHeight(new Vector3(-20,0,0)));}
                if(fromWest&&!toWest){points.Add(AtHeight(new Vector3(-16,0,0)));points.Add(AtHeight(new Vector3(-14,0,0)));points.Add(AtHeight(new Vector3(-14,0,-2)));points.Add(AtHeight(aisleCorner));}
                if(!fromWest&&toWest){points.Add(AtHeight(aisleCorner));points.Add(AtHeight(new Vector3(-14,0,-2)));points.Add(AtHeight(new Vector3(-14,0,0)));points.Add(AtHeight(new Vector3(-16,0,0)));points.Add(AtHeight(new Vector3(-20,0,0)));}
                if(toWest)points.Add(AtHeight(new Vector3(-20,0,destination.z)));
                points.Add(AtHeight(destination));route=points.ToArray();
            }
            else if (state == WorkerState.ToBoxing || state == WorkerState.ToPackage || state == WorkerState.ToWindow)
            {
                // Use the west side of BOX/PACK for local trips instead of returning to the hall every time.
                float southZ = state == WorkerState.ToWindow ? -13.3f : destination.z;
                if (transform.position.z < -6f)
                    route = new[] { AtHeight(new Vector3(-11.3f, 0, transform.position.z)),
                        AtHeight(new Vector3(-11.3f, 0, southZ)), AtHeight(destination) };
                else if (state == WorkerState.ToBoxing)
                    route = new[] { AtHeight(aisleCorner), AtHeight(destination) };
                else
                    route = new[] { AtHeight(aisleCorner), AtHeight(new Vector3(-11.3f, 0, -6.5f)),
                        AtHeight(new Vector3(-11.3f, 0, southZ)), AtHeight(destination) };
            }
            else route = new[] { AtHeight(aisleCorner), AtHeight(destination) };
            if (ShopLayout.WingUnlocked && (destination.x > ShopLayout.WallHalf || transform.position.x > ShopLayout.WallHalf))
                route = System.Array.ConvertAll(ShopLayout.WingRoute(transform.position, destination), AtHeight);
            waypoint = 0;
        }

        Vector3 DestinationFor(WorkerState state)
        {
            if (Job == WorkerJob.Idle)
                return IdleStand();
            if(state==WorkerState.ToBagMachine)return BagLine.MachinePoint;
            if(state==WorkerState.ToBagTable)return BagLine.WorkPoint;
            if(state==WorkerState.ToBagCounter)return BagLine.CounterPoint;
            if (state == WorkerState.ToGrill)
                return CollectStand();
            if (state == WorkerState.ToCounter)
                return ServingStand();
            if (state == WorkerState.ToBoxing)
                return Boxing != null ? Boxing.CirclePosition : transform.position;
            if (state == WorkerState.ToPackage)
                return Boxing != null ? Boxing.DropPosition : transform.position;
            if (state == WorkerState.ToWindow)
                return DriveThru != null ? DriveThru.WindowPosition : transform.position;
            if (state == WorkerState.ToTrash)
            {
                if (cleaningTable == null) cleaningTable = DirtyTable;
                return cleaningTable != null ? cleaningTable.Center : transform.position;
            }
            if (state == WorkerState.ToBin)
                return bin != null ? bin.DropPosition : transform.position;
            return transform.position;
        }

        void ResolveKitchen()
        {
            if (crew != null && crew.TryGetCollectTarget(this, out ProductionStation source, out Transform pickup))
            {
                grill = source;
                pickupPoint = pickup;
            }
        }

        Vector3 CollectStand()
        {
            ResolveKitchen();
            Vector3 point = pickupPoint != null ? pickupPoint.position : transform.position;
            return point + new Vector3((Slot - 1) * 0.75f, 0f, 0.1f);
        }

        Vector3 ServingStand()
        {
            BurgerServingZone cashier = ActiveServing;
            CounterDropZone pile = ActiveDrop;
            Vector3 point = cashier != null
                ? Job == WorkerJob.Stock
                    ? cashier.NearestDropPosition(transform.position)
                    : cashier.NearestServePosition(transform.position, true)
                : pile != null ? pile.DropPosition : transform.position;
            if (Job == WorkerJob.Stock)
                return point + new Vector3((Slot - 1) * 0.35f, 0f, -0.15f);
            return point;
        }

        Vector3 IdleStand()
        {
            if (Slot == 0) return CollectStand();
            if (Slot == 1)
            {
                Vector3 serve = drop != null ? drop.DropPosition : serving != null ? serving.ServingPosition : aisleCorner;
                return serve + new Vector3(1.1f, 0f, -1.2f);
            }
            return aisleCorner + new Vector3(-1.8f, 0f, -1.2f);
        }

        Vector3 AtHeight(Vector3 point) => new Vector3(point.x, transform.position.y, point.z);

        bool MoveAlongRoute(float deltaTime)
        {
            float remaining = WalkSpeed * deltaTime;
            while (waypoint < route.Length)
            {
                Vector3 offset = route[waypoint] - transform.position;
                float distance = offset.magnitude;
                if (distance > 0.001f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(offset), 1f - Mathf.Exp(-12f * deltaTime));
                if (remaining < distance)
                {
                    transform.position += offset.normalized * remaining;
                    return false;
                }
                transform.position = route[waypoint++];
                remaining -= distance;
            }
            return true;
        }

        void KeepInPlayable()
        {
            Vector3 point = transform.position;
            Vector3 clamped = ShopLayout.ClampPlayable(point);
            if ((clamped - point).sqrMagnitude < 0.0001f) return;
            transform.position = new Vector3(clamped.x, point.y, clamped.z);
        }

        void LateUpdate()
        {
            if (label == null || Inventory == null) return;
            label.text = Trash != null && Trash.Count > 0
                ? $"STAFF {StyleLetter} T{Trash.Count}"
                : $"STAFF {StyleLetter} {Inventory.Count}/{Inventory.Capacity}";
            if (Camera.main != null) label.transform.rotation = Camera.main.transform.rotation;
        }

        void OnDestroy()
        {
            if (ownedMaterials == null) return;
            for (int i = 0; i < ownedMaterials.Length; i++)
            {
                Material material = ownedMaterials[i];
                ownedMaterials[i] = null;
                if (material == null) continue;
                if (Application.isPlaying) Object.Destroy(material);
                else Object.DestroyImmediate(material);
            }
        }

        internal static RestaurantWorker Create(Transform parent, Vector3 position, ProductionStation grill,
            BurgerServingZone cashier, Transform pickup, Vector3 aisle, CounterDropZone dropZone,
            DiningArea dining = null, TrashBin trashBin = null, WorkerHiringZone crew = null, int slot = 0)
        {
            slot = Mathf.Clamp(slot, 0, Looks.Length - 1);
            WorkerLook look = Looks[slot];
            GameObject root = new GameObject("RestaurantWorker" + look.Letter);
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(position.x, 1.05f, position.z);
            root.transform.localScale = Vector3.one * look.Height;
            Vector3 facing = slot == 0 ? Vector3.left : slot == 1 ? Vector3.forward : Vector3.back;
            root.transform.rotation = Quaternion.LookRotation(facing);
            RestaurantWorker worker = root.AddComponent<RestaurantWorker>();
            Material uniform = MaterialFor(look.Uniform);
            Material accent = MaterialFor(look.Accent);
            Material skin = MaterialFor(new Color(0.93f, 0.71f, 0.49f));
            worker.ownedMaterials = new[] { uniform, accent, skin };
            worker.StyleLetter = look.Letter;
            worker.UniformColor = look.Uniform;
            worker.HeightScale = look.Height;
            worker.WearsHat = look.HasHat;
            worker.Slot = slot;
            Part(root.transform, "Uniform", PrimitiveType.Capsule, new Vector3(0f, -0.3f, 0f), new Vector3(0.65f, 0.7f, 0.65f), uniform);
            Part(root.transform, "Head", PrimitiveType.Sphere, new Vector3(0f, 0.65f, 0f), Vector3.one * 0.5f, skin);
            if (look.HasHat)
                Part(root.transform, "Hat", PrimitiveType.Cylinder, new Vector3(0f, look.HatY, 0f), look.HatScale, accent);
            if (look.HasHair)
                Part(root.transform, "Hair", PrimitiveType.Sphere, new Vector3(0f, 0.82f, 0f), look.HatScale, accent);
            if (look.HasApron)
                Part(root.transform, "Apron", PrimitiveType.Cube, new Vector3(0f, -0.13f, 0.32f), new Vector3(0.48f, 0.55f, 0.08f), accent);
            if (look.HasBow)
                Part(root.transform, "Bow", PrimitiveType.Cube, new Vector3(0f, 0.15f, 0.34f), new Vector3(0.28f, 0.12f, 0.08f), accent);
            if (look.HasScarf)
                Part(root.transform, "Scarf", PrimitiveType.Cube, new Vector3(0f, 0.22f, 0.28f), new Vector3(0.55f, 0.12f, 0.18f), accent);
            Part(root.transform, "Nose", PrimitiveType.Sphere, new Vector3(0f, 0.64f, 0.25f), Vector3.one * 0.12f, skin);
            worker.label = new GameObject("WorkerLabel").AddComponent<TextMesh>();
            worker.label.transform.SetParent(root.transform, false);
            worker.label.transform.localPosition = new Vector3(0f, 1.95f, 0f);
            worker.label.anchor = TextAnchor.MiddleCenter;
            worker.label.fontSize = 36;
            worker.label.characterSize = 0.07f;
            worker.label.color = Color.white;
            BurgerInventory inventory = root.AddComponent<BurgerInventory>();
            inventory.Configure(StaffBoost.BaseCarry);
            TrashInventory trashBag = root.AddComponent<TrashInventory>();
            trashBag.Configure();
            worker.Configure(grill, cashier, pickup, aisle, inventory, dropZone, dining, trashBin, trashBag, crew, slot);
            return worker;
        }

        static Material MaterialFor(Color color) => BurgerShop.Core.RuntimeMaterials.Create(color);

        static void Part(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            Collider collider = part.GetComponent<Collider>();
            collider.enabled = false;
            BurgerVisual.Release(collider);
        }

        readonly struct WorkerLook
        {
            public readonly char Letter;
            public readonly Color Uniform;
            public readonly Color Accent;
            public readonly bool HasHat;
            public readonly bool HasHair;
            public readonly bool HasApron;
            public readonly bool HasBow;
            public readonly bool HasScarf;
            public readonly float Height;
            public readonly float HatY;
            public readonly Vector3 HatScale;

            public WorkerLook(char letter, Color uniform, Color accent, bool hat, bool hair, bool apron, bool bow,
                bool scarf, float height, float hatY, Vector3 hatScale)
            {
                Letter = letter;
                Uniform = uniform;
                Accent = accent;
                HasHat = hat;
                HasHair = hair;
                HasApron = apron;
                HasBow = bow;
                HasScarf = scarf;
                Height = height;
                HatY = hatY;
                HatScale = hatScale;
            }
        }
    }
}
