using System;
using System.Collections.Generic;
using BurgerShop.Customer;
using BurgerShop.Economy;
using BurgerShop.Persistence;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using BurgerShop.UI;
using UnityEngine;

namespace BurgerShop.Building
{
    public sealed class FacilityLayout : MonoBehaviour
    {
        public static FacilityLayout Current {get;private set;}
        readonly Dictionary<string,FacilityInstance> instances=new Dictionary<string,FacilityInstance>();
        readonly Dictionary<string,FacilityPlacementRecord> pending=new Dictionary<string,FacilityPlacementRecord>();
        BurgerInventory player;RestaurantWallet wallet;PartsWallet parts;CashFloor cash;DiningArea dining;WorkerHiringZone hiring;SessionGoalTracker goals;
        ShopExpansion expansion;
        Transform staging;
        float scan;
        LayoutNavigation navigation;
        Vector3[][] plannedConveyors;
        int navigationRevision=-1;
        public void RefreshNavigation(){navigation=null;navigationRevision=-1;Revision++;}
        public Vector3[] Route(Vector3 from,Vector3 to)
        {
            if(navigation==null||navigationRevision!=Revision){Physics.SyncTransforms();var floors=Floors();if(RestroomExpansion.Current?.Built==true)floors.Add(RestroomExpansion.Floor);floors.Add(Rect.MinMaxRect(-36,-25,-10.2f,-21));floors.Add(Rect.MinMaxRect(-13.8f,-23.5f,-10.2f,-14));navigation=new LayoutNavigation(floors,NavigationObstacles());navigationRevision=Revision;}
            return navigation.Route(from,to);
        }
        List<PlacementFootprint> NavigationObstacles()
        {
            var result=new List<PlacementFootprint>();
            foreach(var c in GetComponentsInChildren<BoxCollider>())
            {
                if(!c.enabled||c.isTrigger||c.GetComponentInParent<PlayerMotor>()!=null||c.GetComponentInParent<CustomerAgent>()!=null||c.GetComponentInParent<RestaurantWorker>()!=null)continue;
                var b=c.bounds;if(b.max.y<.2f||b.min.y>1.8f||c.name.Contains("Floor")||c.name=="Road"||c.name.Contains("Lane"))continue;
                result.Add(new PlacementFootprint(new Vector2(b.center.x,b.center.z),new Vector2(b.size.x,b.size.z),0));
            }
            return result;
        }
        public IReadOnlyCollection<FacilityInstance> Instances=>instances.Values;
        public BurgerInventory Player=>player;
        public RestaurantWallet Wallet=>wallet;
        public FacilityInstance Candidate {get;private set;}
        public FacilityInstance Moving {get;private set;}
        public event Action Changed;
        public bool Committing {get;private set;}
        public bool Editing=>Candidate!=null||Moving!=null;
        public string LastError {get;private set;}="";
        public int Revision {get;private set;}
        public bool HasCustomLayout {get;private set;}
        public void Configure(BurgerInventory actor,RestaurantWallet earnings,PartsWallet currency,CashFloor floor,DiningArea hall,WorkerHiringZone staff,SessionGoalTracker tracker,ShopExpansion shop)
        {
            Current=this;player=actor;wallet=earnings;parts=currency;cash=floor;dining=hall;hiring=staff;goals=tracker;expansion=shop;
            staging=new GameObject("PlacementStaging").transform;staging.SetParent(transform,false);staging.gameObject.SetActive(false);
        }
        public bool HasStock(KitchenProduct product)=>CollectTarget(product,out _,out _);
        public bool CollectTarget(KitchenProduct product,out ProductionStation station,out Transform pickup)
        {
            station=null;pickup=null;
            foreach(var g in GetComponentsInChildren<ExpandableGrill>())if(g.Station.Product==product&&g.Station.Stock>0&&(station==null||g.Station.Stock>station.Stock))
            {station=g.Station;pickup=g.Pickup.PickupPoint;}
            return station!=null;
        }
        public int SupplyDeficit(SupplyLine line)
        {
            int deficit=0;
            if(line==SupplyLine.Dining||line==SupplyLine.Cola)
                foreach(var cashier in GetComponentsInChildren<BurgerServingZone>())
                {
                    bool cola=cashier.GetComponent<CustomerQueue>()?.Product==KitchenProduct.Cola;
                    if(cola!=(line==SupplyLine.Cola))continue;
                    deficit+=Mathf.Max(0,Mathf.Max(WorkerHiringZone.SupplyBuffer-cashier.TotalStock,cashier.ActiveOrderStockDeficit));
                }
            if(line==SupplyLine.Bag)
            {
                foreach(var b in GetComponentsInChildren<BagLine>())
                {if(b.CounterBuilt)deficit+=Mathf.Max(0,WorkerHiringZone.SupplyBuffer-b.StockCount);if(b.TableBuilt)deficit-=b.OutputCount+b.ProcessingCount+b.InputBurgers;}
            }
            if(line==SupplyLine.Boxing)
            {
                foreach(var lane in GetComponentsInChildren<DriveThruLane>())deficit+=Mathf.Max(0,WorkerHiringZone.SupplyBuffer-(lane.PackingStock?.PackageCount??0));
                foreach(var b in GetComponentsInChildren<BoxingStation>())if(b.WorkRoot.gameObject.activeInHierarchy)deficit-=b.TableCount;
            }
            return Mathf.Max(0,deficit);
        }
        public BurgerServingZone ChooseServing(bool cola,Vector3 from)
        {
            BurgerServingZone best=null;float score=float.MinValue;
            foreach(var line in GetComponentsInChildren<BurgerServingZone>())
            {
                if((line.GetComponent<CustomerQueue>()?.Product==KitchenProduct.Cola)!=cola)continue;
                float next=(line.ReadyToSell?1000:0)+(line.Stock!=null?8-line.Stock.Count:0)*20-Vector3.Distance(from,line.ServingPosition);
                if(next>score){score=next;best=line;}
            }
            return best;
        }
        public BoxingStation ChooseBox(Vector3 from)
        {
            BoxingStation best=null;float score=float.MinValue;
            foreach(var b in GetComponentsInChildren<BoxingStation>())
            {if(!b.WorkRoot.gameObject.activeInHierarchy)continue;float next=b.TableCount*20-Vector3.Distance(from,b.CirclePosition);if(next>score){score=next;best=b;}}
            return best;
        }
        public DriveThruLane ChooseDrive(Vector3 from)
        {
            DriveThruLane best=null;float score=float.MinValue;
            foreach(var lane in GetComponentsInChildren<DriveThruLane>())
            {float next=(lane.ReadyToSell?1000:0)+(8-(lane.PackingStock?.PackageCount??0))*20-Vector3.Distance(from,lane.WindowPosition);if(next>score){score=next;best=lane;}}
            return best;
        }
        public BagLine ChooseBag(FacilityKind kind,Vector3 from)
        {
            BagLine best=null;float score=float.MinValue;
            foreach(var b in GetComponentsInChildren<BagLine>())
            {
                bool built=kind==FacilityKind.BagMachine?b.MachineBuilt:kind==FacilityKind.BagTable?b.TableBuilt:b.CounterBuilt;
                if(!built)continue;
                float next=kind==FacilityKind.BagMachine?b.EmptyStock*20-Vector3.Distance(from,b.MachinePosition):kind==FacilityKind.BagTable?
                    (b.HasWork?100:0)-Vector3.Distance(from,b.WorkPosition):(b.ReadyToSell?1000:0)+(8-b.StockCount)*20-Vector3.Distance(from,b.ServingPosition);
                if(next>score){score=next;best=b;}
            }
            return best;
        }
        public bool Unlocked(FacilityKind kind)=>FacilityCatalog.IsUnlocked(kind,goals?.Rank??1,goals?.LegacyAccess??false);
        public int Owned(FacilityKind kind){int count=0;foreach(var f in instances.Values)if(f!=null&&f.Kind==kind&&f.Available)count++;return count;}
        FacilityUnlockZone OriginalPad(FacilityKind kind)
        {
            FacilityUnlockZone pad=null;
            switch(kind)
            {
                case FacilityKind.PairTable:pad=expansion.TablePad;break;
                case FacilityKind.FourSeatTable:pad=expansion.FourSeatPad;break;
                case FacilityKind.SquareTable:pad=expansion.SquarePad;break;
                case FacilityKind.BurgerMachine:pad=expansion.GrillPad;break;
                case FacilityKind.ColaMachine:pad=expansion.ColaPad;break;
                case FacilityKind.BurgerCounter:pad=expansion.CounterPad;break;
                case FacilityKind.ColaCounter:pad=expansion.ColaBarPad;break;
                case FacilityKind.BlueBoxTable:pad=expansion.BoxingPad;break;
                case FacilityKind.CarCounter:pad=expansion.DriveThruPad;break;
                case FacilityKind.BagMachine:pad=BagLine.Current?.MachinePad;break;
                case FacilityKind.BagTable:pad=BagLine.Current?.TablePad;break;
                case FacilityKind.BagCounter:pad=BagLine.Current?.CounterPad;break;
            }
            return pad!=null&&!pad.IsPurchased?pad:null;
        }
        static string OriginalId(FacilityKind kind)
        {
            switch(kind)
            {
                case FacilityKind.PairTable:return "table-extra";case FacilityKind.FourSeatTable:return "table-four";case FacilityKind.SquareTable:return "table-square";
                case FacilityKind.BurgerMachine:return "grill-extra";case FacilityKind.ColaMachine:return "cola-machine";
                case FacilityKind.BurgerCounter:return "counter-extra";case FacilityKind.ColaCounter:return "cola-counter";
                case FacilityKind.BlueBoxTable:return "boxing";case FacilityKind.CarCounter:return "car-counter";
                case FacilityKind.BagMachine:return "bag-machine";case FacilityKind.BagTable:return "bag-table";case FacilityKind.BagCounter:return "bag-counter";
                default:return "";
            }
        }
        public int Price(FacilityKind kind)=>Mathf.Max(1,FacilityCatalog.Price(kind,Owned(kind))-(OriginalPad(kind)?.Invested??0));
        public FacilityInstance BeginPurchase(FacilityKind kind)
        {
            Cancel();Discover();if(!Unlocked(kind)){LastError="Not unlocked";return null;}
            Candidate=FacilityFactory.Create(staging,kind,"custom:"+Guid.NewGuid().ToString("N"),player,wallet,parts,cash,dining);
            return Candidate;
        }
        public bool BeginMove(FacilityInstance instance)
        {Cancel();if(instance==null||!instances.ContainsKey(instance.Id)||!instance.Available)return false;Moving=instance;return true;}
        public void Cancel()
        {if(Candidate!=null){Candidate.gameObject.SetActive(false);BurgerVisual.Release(Candidate.gameObject);}Candidate=null;Moving=null;LastError="";}
        public List<Rect> Floors()
        {
            var floors=new List<Rect>{Rect.MinMaxRect(-14.8f,-14.8f,14.8f,14.8f)};
            if(expansion!=null&&expansion.HasWing)
            {floors.Add(Rect.MinMaxRect(14,-8,23.4f,-4.3f));floors.Add(Rect.MinMaxRect(23.4f,-8,37.8f,11.3f));}
            if(CourierLine.Current!=null&&CourierLine.Current.AreaOpen){floors.Add(Rect.MinMaxRect(-14.8f,14,14.8f,39.8f));floors.Add(Rect.MinMaxRect(-30,39,30,44));}
            if(BagLine.Current!=null&&BagLine.Current.Expanded)floors.Add(Rect.MinMaxRect(-26.8f,-8.8f,-14,8.8f));
            bool southBay=(goals!=null&&goals.Allows(ShopRanks.BoxingRank))||(expansion!=null&&(expansion.HasBoxing||expansion.HasDriveThru));
            if(southBay)floors.Add(Rect.MinMaxRect(6.9f,-27.5f,15.1f,-14));
            // South street is z≈-30 ±4; car lane width 3.4 around z=-29. The old
            // [-32,-27.5] strip left a 0.2m gap so RoadFits rejected the authored pose.
            if((goals!=null&&goals.Allows(ShopRanks.DriveThruRank))||(expansion!=null&&expansion.HasDriveThru))
                floors.Add(Rect.MinMaxRect(-37f,-34f,31f,-25.5f));
            return floors;
        }
        public bool CanPlace(FacilityInstance subject,Vector3 position,float yaw,bool checkAccess=false)
        {
            LastError="";
            if(subject==null||!PlacementGeometry.Finite(position.x)||!PlacementGeometry.Finite(position.z)||!PlacementGeometry.Finite(yaw))return Fail("Invalid position");
            var floor=Floors();var shape=subject.Footprint(position,yaw);
            if(!PlacementGeometry.CoveredByFloor(shape,floor))return Fail("Keep the whole facility inside owned land");
            var obstacles=new List<PlacementFootprint>();
            foreach(var f in instances.Values)
            {
                if(f==null||f==subject||!f.Available||(subject==Candidate&&OriginalPad(subject.Kind)!=null&&f.Id==OriginalId(subject.Kind)))continue;
                var occupied=f.Footprint(f.transform.position,f.transform.eulerAngles.y);
                if(PlacementGeometry.Overlaps(shape,occupied,.1f))return Fail("Overlaps another facility");
                obstacles.Add(occupied);
                var lane=Road(f);if(lane!=null)foreach(var roadShape in lane.Footprints(f.transform.position,f.transform.eulerAngles.y))
                {if(PlacementGeometry.Overlaps(shape,roadShape))return Fail("Keep the vehicle lane clear");obstacles.Add(roadShape);}
            }
            // Fixed room walls remain fixed when facilities move.
            foreach(var c in GetComponentsInChildren<BoxCollider>())
            {
                if(!c.enabled||c.isTrigger||c.GetComponentInParent<FacilityInstance>()!=null||c.GetComponentInParent<PlayerMotor>()!=null||c.GetComponentInParent<CustomerAgent>()!=null||c.GetComponentInParent<RestaurantWorker>()!=null)continue;
                if(!c.name.Contains("Wall")&&!c.name.Contains("Desk")&&!c.name.Contains("Landmark"))continue;
                if(subject.Kind==FacilityKind.CarCounter)
                {
                    if(c.name.StartsWith("ServiceWindow"))continue;
                    // Boxing's pack desk is the authored drive-thru counter. Until the lane
                    // is bought it is not a FacilityInstance, so CanPlace would treat it as a
                    // fixed wall and the supermarket could never complete Rank 6's purchase.
                    var pack=expansion!=null?expansion.Boxing:null;
                    if(pack!=null&&pack.CounterRoot!=null&&c.transform.IsChildOf(pack.CounterRoot))continue;
                }
                var b=c.bounds;var fixedShape=new PlacementFootprint(new Vector2(b.center.x,b.center.z),new Vector2(b.size.x,b.size.z),0);
                if(PlacementGeometry.Overlaps(shape,fixedShape,.1f))return Fail("Overlaps a fixed wall");
                obstacles.Add(fixedShape);
            }
            if(subject.Id!="parcel-machine"&&subject.Id!="courier-tray")
                foreach(var line in GetComponentsInChildren<CourierLine>())foreach(var path in line.ConveyorPaths)
                    foreach(var belt in new VehicleRoadLayout(path,1.4f).Footprints(Vector3.zero,0))
                    {if(PlacementGeometry.Overlaps(shape,belt))return Fail("Keep the conveyor clear");obstacles.Add(belt);}
            if(!RoadFits(subject,position,yaw,floor,obstacles))return Fail("The entire vehicle lane needs free owned space");
            var rotation=Quaternion.Euler(0,yaw,0);
            if(shape.Contains(new Vector2(0,-13),1.4f))return Fail("Keep the restroom entrance clear");
            foreach(var local in subject.Ports)
            {
                var p=position+rotation*local;
                if(!PlacementGeometry.CoveredByFloor(new PlacementFootprint(new Vector2(p.x,p.z),Vector2.one*.8f,0),floor))return Fail("Keep work and seat positions inside the shop");
                foreach(var obstacle in obstacles)if(obstacle.Contains(new Vector2(p.x,p.z),.4f))return Fail("A work or seat position is blocked");
            }
            if(checkAccess)
            {
                var actorPoint=new Vector2(player.transform.position.x,player.transform.position.z);
                foreach(var actor in GetComponentsInChildren<CustomerAgent>())if(actor.GetComponentInParent<FacilityInstance>()!=subject&&shape.Contains(new Vector2(actor.transform.position.x,actor.transform.position.z),.35f))return Fail("Wait for the customer to pass");
                foreach(var actor in GetComponentsInChildren<RestaurantWorker>())if(shape.Contains(new Vector2(actor.transform.position.x,actor.transform.position.z),.4f))return Fail("Wait for the employee to pass");
                if(shape.Contains(actorPoint,.4f))return Fail("Move away from the placement first");
                if(!PlanConveyors(subject,position,yaw,out plannedConveyors))return Fail("No clear conveyor connection, or loaded chain is too short");
                // Compare before/after using the SAME geometry. Runtime collider paths differ from
                // conservative placement envelopes (roads, work assemblies, conveyor reservations).
                var beforeObstacles=new List<PlacementFootprint>(obstacles);
                if(subject==Moving)beforeObstacles.Add(subject.Footprint(subject.transform.position,subject.transform.eulerAngles.y));
                var before=new LayoutNavigation(floor,beforeObstacles);
                obstacles.Add(shape);
                var destinations=new List<Vector2>();
                foreach(var local in subject.Ports){var p=position+rotation*local;destinations.Add(new Vector2(p.x,p.z));}
                // Do not allow the new placement to cut off existing service ports.
                foreach(var f in instances.Values)if(f!=null&&f!=subject&&f.Available)
                    foreach(var local in f.Ports){var p=f.transform.TransformPoint(local);if(!shape.Contains(new Vector2(p.x,p.z),.4f))continue;return Fail("Blocks an existing work position");}
                // Chairs occupy their sitting point; their outside approach is checked through the table's wait point.
                if(subject.Kind<=FacilityKind.SquareTable&&destinations.Count>1)destinations.RemoveRange(1,destinations.Count-1);
                var access=new LayoutNavigation(floor,obstacles);
                foreach(var entry in new[]{ShopLayout.Entrance,new Vector3(14,0,0),new Vector3(11,0,-14),new Vector3(0,0,-13)})
                    if(!access.CanReach(player.transform.position,entry)&&before.CanReach(player.transform.position,entry))return Fail("Keep room entrances reachable");
                foreach(var f in instances.Values)if(f!=null&&f!=subject&&f.Available)
                {
                    int count=f.Kind<=FacilityKind.SquareTable?Mathf.Min(1,f.Ports.Count):f.Ports.Count;
                    for(int i=0;i<count;i++){var port=f.transform.TransformPoint(f.Ports[i]);
                        if(!access.CanReach(player.transform.position,port)&&before.CanReach(player.transform.position,port))
                            return Fail("Blocks access to "+FacilityCatalog.Get(f.Kind).Name+". Drag to leave a wider path.");}
                }
                if(destinations.Exists(p=>!access.CanReach(player.transform.position,new Vector3(p.x,0,p.y))))
                    return Fail("Leave a walkable route to the facility");
            }
            return true;
        }
        bool PlanConveyors(FacilityInstance subject,Vector3 position,float yaw,out Vector3[][] planned)
        {
            planned=null;var courier=GetComponent<CourierLine>();
            if(courier==null||!courier.AutomationEnabled||subject.Purchased||
                (subject.Id!="parcel-machine"&&subject.Id!="courier-tray"))return true;
            planned=courier.PlannedConveyors(subject.transform,position,yaw);
            var blocked=new List<PlacementFootprint>();
            foreach(var f in instances.Values)if(f.Available&&f.Id!="parcel-machine"&&f.Id!="courier-tray")blocked.Add(f.Footprint(f.transform.position,f.transform.eulerAngles.y));
            foreach(var c in GetComponentsInChildren<BoxCollider>())
            {
                if(!c.enabled||c.isTrigger||!c.name.Contains("Wall")||c.bounds.max.y<=1f)continue;
                var b=c.bounds;blocked.Add(new PlacementFootprint(new Vector2(b.center.x,b.center.z),new Vector2(b.size.x,b.size.z),0));
            }
            var floors=Floors();var pathfinder=new LayoutNavigation(floors,blocked,.72f);
            foreach(int i in new[]{0,2})
            {
                var start=planned[i][0];var end=planned[i][1];var route=pathfinder.Route(start,end);if(route==null)return false;
                var clean=new List<Vector3>{start};foreach(var p in route)if(Vector3.Distance(clean[clean.Count-1],p)>.01f)clean.Add(p);
                if(clean.Count<2)return false;
                planned[i]=CourierConveyor.Rounded(clean.ToArray());
                for(int n=1;n<planned[i].Length;n++)
                {
                    var road=new VehicleRoadLayout(new[]{planned[i][n-1],planned[i][n]},1.4f);
                    if(!road.CanPlace(Vector3.zero,0,floors,blocked))return false;
                }
            }
            return courier.CanApplyConveyors(planned);
        }
        bool Fail(string error){LastError=error;return false;}
        static VehicleRoadLayout Road(FacilityInstance f)
        {
            if(f.Kind!=FacilityKind.CarCounter&&f.Kind!=FacilityKind.CourierTray)return null;
            Vector3 pivot=f.Kind==FacilityKind.CarCounter?ShopLayout.PackageCounter:CourierLine.Counter;
            Vector3[] points=f.Kind==FacilityKind.CarCounter?new[]{ShopLayout.DriveThruSpawn-pivot,ShopLayout.DriveThruExit-pivot}:
                Array.ConvertAll(CourierRoad.FullPath,p=>p-pivot);
            return new VehicleRoadLayout(points,f.Kind==FacilityKind.CarCounter?VehicleRoadLayout.CarWidth:VehicleRoadLayout.CourierWidth);
        }
        static bool RoadFits(FacilityInstance f,Vector3 position,float yaw,List<Rect> floors,List<PlacementFootprint> obstacles)
        {
            var road=Road(f);return road==null||road.CanPlace(position,yaw,floors,obstacles);
        }
        public bool Confirm(Vector3 position,float yaw)
        {
            var chosen=Candidate!=null?Candidate:Moving;
            if(!CanPlace(chosen,position,yaw,true))return false;
            bool buying=Candidate!=null;
            bool spentOnPurchase=buying;
            var original=buying?OriginalPad(chosen.Kind):null;
            if(buying&&!Unlocked(chosen.Kind))return Fail("Not unlocked");
            if(buying&&wallet.Coins<Price(chosen.Kind))return Fail("Not enough coins");
            Committing=true;
            try {
            if(buying&&!wallet.TrySpend(Price(chosen.Kind)))return Fail("Not enough coins");
            if(original!=null)
            {
                var kind=chosen.Kind;
                original.RestoreInvestment(original.Cost);Discover();
                if(instances.TryGetValue(OriginalId(kind),out var existing))
                {
                    chosen=existing;
                    Candidate.gameObject.SetActive(false);BurgerVisual.Release(Candidate.gameObject);Candidate=null;
                    buying=false;
                }
            }
            chosen.transform.SetPositionAndRotation(new Vector3(position.x,0,position.z),Quaternion.Euler(0,yaw,0));
            if(Candidate!=null)
            {
                chosen.transform.SetParent(transform,true);chosen.gameObject.SetActive(true);
                instances.Add(chosen.Id,chosen);Activate(chosen);
            }
            chosen.Moved?.Invoke();if(plannedConveyors!=null)GetComponent<CourierLine>()?.ApplyConveyors(plannedConveyors);Candidate=null;Moving=null;HasCustomLayout=true;Revision++;
            } finally { Committing=false; }
            if(spentOnPurchase)goals?.AddUpgradeStars();
            GetComponent<RestaurantPersistence>()?.Flush();Changed?.Invoke();return true;
        }
        void Activate(FacilityInstance f)
        {
            foreach(var table in f.GetComponentsInChildren<DiningTable>()){dining.RegisterTable(table);GetComponent<TableUpgradeBoard>()?.RegisterCustom(table);}
            var growth=GetComponent<GrowthUpgrades>();
            var costs=FacilityCatalog.UpgradeCosts(f.Kind);
            if(growth!=null&&costs.Length>0)
            {
                var stock=f.GetComponentInChildren<CounterStock>();var boxing=f.GetComponentInChildren<BoxingStation>();var bag=f.GetComponentInChildren<BagLine>();
                growth.Register(new GrowthUpgrades.Offer {Id=f.Id,Title=FacilityCatalog.Get(f.Kind).Name,Target=f.transform,Position=f.transform.TransformPoint(new Vector3(-2,.02f,0)),Costs=costs,
                    Benefit=n=>"Facility level "+n,Apply=n=>
                    {
                        if(stock!=null)stock.ServiceLevel=n;
                        if(boxing!=null)boxing.WorkLevel=n;
                        if(bag!=null){if(f.Kind==FacilityKind.BagMachine)bag.MachineLevel=n;else if(f.Kind==FacilityKind.BagTable)bag.TableLevel=n;else bag.CounterLevel=n;}
                        var trim=f.transform.Find("PurchasedTier");if(trim!=null){trim.gameObject.SetActive(false);BurgerVisual.Release(trim.gameObject);}
                        trim=new GameObject("PurchasedTier").transform;trim.SetParent(f.transform,false);
                        var gold=Core.RuntimeMaterials.Create(HudChrome.Gold);trim.gameObject.AddComponent<BurgerVisual>().OwnMaterials(gold);
                        for(int i=1;i<n;i++)CourierVisuals.Part(trim,"Tier",new Vector3(0,1.12f+i*.12f,.55f),new Vector3(1.8f,.07f,.10f),gold);
                    }});
            }
            foreach(var grill in f.GetComponentsInChildren<ExpandableGrill>())
            {
                if(grill.Station.Product==KitchenProduct.Burger)hiring?.RegisterKitchen(grill.Station,grill.Pickup.PickupPoint);
                FindFirstObjectByType<UpgradeHud>()?.AddZone(grill.Upgrade);
            }
            foreach(var lane in f.GetComponentsInChildren<DriveThruLane>(true))
            {
                expansion?.AdoptDriveThru(lane);
                var pack=f.GetComponentInChildren<BoxingStation>()??expansion?.Boxing;
                lane.BindBoxing(pack);
            }
        }
        FacilityInstance Wrap(string id,FacilityKind kind,Transform model,Vector3 pivot)
        {
            if(model==null)return null;
            if(instances.TryGetValue(id,out var known))return known;
            var wrapper=new GameObject("Layout_"+id).transform;wrapper.SetParent(model.parent,false);wrapper.position=pivot;
            model.SetParent(wrapper,true);
            foreach(var feedback in model.GetComponentsInChildren<StationUpgradeFeedback>(true))feedback.RebasePlacement();
            var instance=wrapper.gameObject.AddComponent<FacilityInstance>();instance.Configure(id,kind,false);instances.Add(id,instance);Revision++;
            FacilityFactory.AddPorts(instance);
            if(pending.TryGetValue(id,out var record)){instance.Apply(record);pending.Remove(id);}
            return instance;
        }
        public void Discover()
        {
            if(dining==null)return;
            for(int i=0;i<dining.TableCount;i++)
            {
                var table=dining.Tables[i];if(table==null||table.GetComponentInParent<FacilityInstance>()!=null)continue;
                string id=i<ShopLayout.Tables.Length?"table-"+i:table==expansion.ExtraTable?"table-extra":table==expansion.FourSeatTable?"table-four":"table-square";
                Wrap(id,(FacilityKind)(int)table.Kind,table.transform,table.Center);
            }
            foreach(var grill in GetComponentsInChildren<ExpandableGrill>(true))
            {
                if(grill.GetComponentInParent<FacilityInstance>()!=null)continue;
                bool cola=grill.Station.Product==KitchenProduct.Cola;
                Wrap(cola?"cola-machine":grill==expansion.ExtraGrill?"grill-extra":"grill-main",cola?FacilityKind.ColaMachine:FacilityKind.BurgerMachine,grill.transform,grill.transform.position);
            }
            foreach(var stock in GetComponentsInChildren<CounterStock>(true))
            {
                if(stock.GetComponentInParent<FacilityInstance>()!=null||stock.GetComponentInParent<BoxingStation>()!=null)continue;
                if(stock.GetComponent<FacilityLayout>()!=null||stock.GetComponent<ShopExpansion>()!=null)continue;
                if(stock==expansion.ExtraStock&&stock.GetComponent<BurgerServingZone>()==null)
                {
                    BurgerServingZone main=null;foreach(var line in GetComponentsInChildren<BurgerServingZone>())if(line.Stock!=stock&&line.DropZone.Product==KitchenProduct.Burger){main=line;break;}
                    if(main!=null&&!main.DetachCounter(stock))continue;
                    var queue=stock.gameObject.AddComponent<CustomerQueue>();
                    var center=stock.CounterPosition;center.y=0;
                    queue.Configure(center+new Vector3(-4,0,0),center+new Vector3(-3.5f,0,0),new[]{center+new Vector3(0,0,-1.8f),center+new Vector3(-1.5f,0,-1.8f),center+new Vector3(-3,0,-1.8f)},center);
                    var circle=new GameObject("ServiceAnchor").transform;circle.SetParent(stock.transform,true);circle.position=expansion.ExtraDrop.DropPosition;
                    stock.gameObject.AddComponent<BurgerServingZone>().Configure(queue,player,wallet,circle,ShopLayout.Exit,stock,dining,expansion.ExtraDrop,10,cash);
                }
                bool cola=stock.GetComponent<CustomerQueue>()?.Product==KitchenProduct.Cola;
                Vector3 p=stock.CounterPosition;p.y=0;
                Wrap(cola?"cola-counter":stock==expansion.ExtraStock?"counter-extra":"counter-main",cola?FacilityKind.ColaCounter:FacilityKind.BurgerCounter,stock.transform,p);
            }
            var box=expansion.Boxing;
            if(box!=null)
                Wrap("boxing",FacilityKind.BlueBoxTable,box.WorkRoot,ShopLayout.BoxingTable);
            if(expansion!=null&&expansion.HasDriveThru)
            {
                Transform model=box!=null?box.CounterRoot:expansion.DriveThru.transform;
                Vector3 pivot=box!=null?ShopLayout.PackageCounter:ShopLayout.DriveThruWindow;
                var counter=Wrap("car-counter",FacilityKind.CarCounter,model,pivot);
                if(expansion.DriveThru!=null&&expansion.DriveThru.GetComponentInParent<FacilityInstance>()==null)
                {
                    expansion.DriveThru.AttachToWindow(counter.transform);
                    FacilityFactory.AddPorts(counter);
                }
            }
            var bag=GetComponent<BagLine>();
            if(bag!=null)
            {Wrap("bag-machine",FacilityKind.BagMachine,bag.MachineRoot,BagLine.Machine);Wrap("bag-table",FacilityKind.BagTable,bag.WorkRoot,BagLine.Workbench);Wrap("bag-counter",FacilityKind.BagCounter,bag.CounterRoot,BagLine.Counter);}
            var courier=GetComponent<CourierLine>();
            if(courier!=null)
            {Wrap("parcel-machine",FacilityKind.RedBoxMachine,courier.MachineRoot,CourierLine.Machine);Wrap("courier-tray",FacilityKind.CourierTray,courier.RiderRoot,CourierLine.Counter);}
            foreach(var b in GetComponentsInChildren<BoxingStation>())foreach(var worker in hiring.Workers)if(worker!=null)b.RegisterOperator(worker.Inventory);
            foreach(var b in GetComponentsInChildren<BagLine>())b.BindCrew(hiring);
            foreach(var bin in GetComponentsInChildren<TrashBin>(true))if(bin.GetComponentInParent<FacilityInstance>()==null)Wrap("trash-bin",FacilityKind.TrashBin,bin.transform,bin.transform.position);
        }
        public FacilityPlacementRecord[] Capture()
        {
            var records=new List<FacilityPlacementRecord>(pending.Values);
            foreach(var instance in instances.Values)if(instance!=null)records.Add(instance.Capture());
            records.Sort((a,b)=>string.CompareOrdinal(a.id,b.id));return records.ToArray();
        }
        public void Restore(FacilityPlacementRecord[] records)
        {
            Discover();if(records==null)return;
            foreach(var row in records)
            {
                if(!row.purchased&&row.id=="boxing"&&Mathf.Abs(row.x+9)<.01f&&Mathf.Abs(row.z+9)<.01f){row.x=ShopLayout.BoxingTable.x;row.z=ShopLayout.BoxingTable.z;}
                if(!row.purchased&&row.id=="car-counter"&&Mathf.Abs(row.x+9)<.01f&&Mathf.Abs(row.z+14.35f)<.01f){row.x=ShopLayout.PackageCounter.x;row.z=ShopLayout.PackageCounter.z;}
                // The authored courier station moved to the public street in spec 045.
                // Preserve deliberately customized poses; migrate only the old default.
                if(row.id=="courier-tray"&&!row.purchased&&Mathf.Abs(row.x-2)<.01f&&Mathf.Abs(row.z-23)<.01f&&Mathf.Abs(row.yaw)<.01f)row.z=CourierLine.Counter.z;
                if(row.purchased)
                {
                    HasCustomLayout=true;
                    if(instances.ContainsKey(row.id))continue;
                    var instance=FacilityFactory.Create(staging,(FacilityKind)row.kind,row.id,player,wallet,parts,cash,dining);
                    instance.transform.SetParent(transform,true);instance.Apply(row);instance.gameObject.SetActive(true);instances.Add(row.id,instance);Activate(instance);instance.Apply(row);
                }
                else if(instances.TryGetValue(row.id,out var existing))
                {if(Vector3.Distance(existing.transform.position,new Vector3(row.x,0,row.z))>.01f||Mathf.Abs(Mathf.DeltaAngle(existing.transform.eulerAngles.y,row.yaw))>.01f)HasCustomLayout=true;existing.Apply(row);}
                else pending[row.id]=row;
            }
            var courier=GetComponent<CourierLine>();
            if(courier!=null&&courier.ConveyorEndpointsChanged&&instances.TryGetValue("parcel-machine",out var machine)&&PlanConveyors(machine,machine.transform.position,machine.transform.eulerAngles.y,out var routes))courier.ApplyConveyors(routes);
            Revision++;
        }
        void LateUpdate()
        {
            if(expansion==null)return;
            foreach(var offer in FacilityCatalog.Offers)OriginalPad(offer.Kind)?.SetStoreOnly();
        }
        void Update(){scan+=Time.unscaledDeltaTime;if(scan<.5f)return;scan=0;Discover();}
        void OnDestroy(){if(Current==this)Current=null;}
    }
}
