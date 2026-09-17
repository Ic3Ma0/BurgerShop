using System.Collections.Generic;
using BurgerShop.Core;
using BurgerShop.Economy;
using BurgerShop.Player;
using BurgerShop.UI;
using UnityEngine;

namespace BurgerShop.Restaurant
{
    public sealed class CourierLine : MonoBehaviour
    {
        public static CourierLine Current {get;private set;}
        public static readonly Vector3 Machine=new Vector3(-7,0,21),InputPoint=new Vector3(-7,.02f,19),OutputPoint=new Vector3(-4.7f,.02f,21);
        public static readonly Vector3 Counter=new Vector3(2,0,38),DropPoint=new Vector3(2,.02f,36),Stop=new Vector3(2,0,41);
        public static readonly Vector3 CashPoint=new Vector3(4,.06f,38),PartsPoint=new Vector3(5.5f,.06f,38);
        public const int Capacity=8,OrderQuantity=4,CoinsPerParcel=20,PartsPerParcel=1;
        public const float ProcessingSeconds=1f,TransferSeconds=.35f;
        readonly List<Transform> raw=new List<Transform>(),output=new List<Transform>(),stock=new List<Transform>();
        readonly List<BicycleCourier> queue=new List<BicycleCourier>(),leaving=new List<BicycleCourier>();
        readonly List<Transform> partsPiles=new List<Transform>();
        readonly List<int> partsAmounts=new List<int>();
        Transform area,machine,bin,inProcess,flight;
        Transform riderRoot;
        bool hasMachine=true, hasTray=true;
        public Transform MachineRoot=>machine;
        public Transform RiderRoot=>riderRoot;
        public Transform TrayRoot=>bin;
        BicycleCourier receiving;
        Vector3 flightFrom;
        Material blue,white,dark,gold,road;
        RestaurantWallet wallet;PartsWallet parts;BurgerInventory player;CashFloor cash;
        float work,transfer,spawn,handoff,cooldown,partPickup;
        bool paused,unfocused;
        int spawnedRiders;
        ProductionStation source;
        bool dedicatedSource;
        public ProductionStation ConveyorSource=>source;
        public static readonly Vector3 ProcessorPosition=new Vector3(3,0,20);
        const float ProcessorSeconds=3f;
        CourierConveyor intakeBelt,parcelBelt,machineBelt;
        readonly List<TrashMotion> partsFlights=new List<TrashMotion>();
        float feedClock;
        public int InTransitCount => (intakeBelt?.Count??0)+(parcelBelt?.Count??0);
        public int InputCount=>raw.Count;
        public int OutputCount=>output.Count;
        public int ProcessingCount=>inProcess!=null?1:0;
        public int StockCount=>stock.Count;
        public int CompletedOrders {get;private set;}
        public int WaitingCount=>queue.Count;
        public BicycleCourier Front=>queue.Count>0?queue[0]:null;
        public int GroundParts {get{int sum=0;foreach(int n in partsAmounts)sum+=n;return sum;}}
        public void Configure(RestaurantWallet earnings,PartsWallet currency,BurgerInventory actor,CashFloor floor,ProductionStation burgerSource=null,bool independentProduction=false)
        {
            Current=this;wallet=earnings;parts=currency;player=actor;cash=floor;source=burgerSource;dedicatedSource=independentProduction;Build();
            if(source!=null && (GetComponent<SessionGoalTracker>()?.Allows(ShopRanks.AutomationRank)??true))BuildAutomation();
            ApplyAccess(GetComponent<SessionGoalTracker>()?.Allows(ShopRanks.CourierRank)??true, GetComponent<SessionGoalTracker>()?.Allows(ShopRanks.AutomationRank)??true);
        }
        public bool AutomationEnabled=>intakeBelt!=null;
        public IEnumerable<Vector3[]> ConveyorPaths {get {if(intakeBelt!=null)yield return intakeBelt.WorldPath;if(machineBelt!=null)yield return machineBelt.WorldPath;if(parcelBelt!=null)yield return parcelBelt.WorldPath;}}
        public Transform LayoutRoot=>area;
        public bool AreaOpen=>area!=null&&area.gameObject.activeSelf;
        public void ApplyAccess(bool open,bool automate)
        {
            if(area==null)return;
            area.gameObject.SetActive(open);
            foreach(Transform part in transform)if(part.name=="Wall+Z")part.gameObject.SetActive(!open);
            if(open&&automate&&source!=null&&intakeBelt==null)BuildAutomation();
        }
        void Build(bool scenery=true)
        {
            area=new GameObject("NorthCourierArea").transform;area.SetParent(transform,false);
            blue=RuntimeMaterials.Create(new Color(.13f,.43f,.85f));white=RuntimeMaterials.Create(HudChrome.Cream);
            dark=RuntimeMaterials.Create(new Color(.22f,.27f,.32f));gold=RuntimeMaterials.Create(new Color(1,.65f,.08f));road=RuntimeMaterials.Create(new Color(.16f,.18f,.20f));
            area.gameObject.AddComponent<BurgerVisual>().OwnMaterials(blue,white,dark,gold,road);
            riderRoot=new GameObject("CourierServiceUnit").transform; riderRoot.SetParent(area,false);
            if(hasTray) CourierRoad.Build(riderRoot,road,white,scenery);
            if(scenery)
            {
            CourierVisuals.Part(area,"CourierFloor",new Vector3(0,-.1f,27.5f),new Vector3(30,.2f,25),white,true);

            foreach(var t in GetComponentsInChildren<Transform>())if(t.name=="Wall+Z")t.gameObject.SetActive(false);
            foreach(int side in new[]{-1,1})
            {
                CourierVisuals.Part(area,"OldNorthWall",new Vector3(side*8.4f,.75f,15),new Vector3(13.2f,1.5f,.4f),dark,true);
                CourierVisuals.Part(area,"CourierSideWall",new Vector3(side*15,.6f,27.5f),new Vector3(.3f,1.2f,25),blue,true);
            }
            }
            machine=new GameObject("BlueParcelMachine").transform;machine.SetParent(area,false);machine.position=Machine;
            CourierVisuals.Part(machine,"MachineBody",new Vector3(0,1.05f,0),new Vector3(2.5f,2.1f,1.6f),blue,true);
            CourierVisuals.Part(machine,"WhiteFrame",new Vector3(0,1.05f,-.86f),new Vector3(2.1f,1.8f,.16f),white);
            CourierVisuals.Part(machine,"Opening",new Vector3(0,1,-.97f),new Vector3(1.65f,1.25f,.08f),dark);
            for(int i=0;i<5;i++)CourierVisuals.Part(machine,"Roller",new Vector3(0,.45f+i*.22f,-1.02f),new Vector3(1.58f,.05f,.05f),white);
            CourierVisuals.Part(machine,"InputTray",new Vector3(0,.2f,-1.3f),new Vector3(1.8f,.18f,.8f),dark);
            CourierVisuals.Part(machine,"OutputTray",new Vector3(1.8f,.5f,0),new Vector3(1.1f,.14f,1.25f),dark);
            ShopFixtures.CreateActionCircle(machine,"ParcelInput",InputPoint,HudChrome.Green);
            ShopFixtures.CreateActionCircle(machine,"ParcelOutput",OutputPoint,HudChrome.Green);
            bin=new GameObject("YellowCourierPickup").transform;bin.SetParent(riderRoot,false);bin.position=Counter;
            CourierVisuals.Part(bin,"YellowBase",new Vector3(0,.42f,0),new Vector3(2.8f,.84f,1.8f),gold,true);
            CourierVisuals.Part(bin,"TrayFloor",new Vector3(0,.86f,0),new Vector3(2.5f,.06f,1.5f),dark);
            foreach(int side in new[]{-1,1})
            {
                CourierVisuals.Part(bin,"TraySide",new Vector3(side*1.3f,1,0),new Vector3(.16f,.3f,1.8f),gold);
                CourierVisuals.Part(bin,"TrayEnd",new Vector3(0,1,side*.82f),new Vector3(2.7f,.3f,.16f),gold);
            }
            ShopFixtures.CreateActionCircle(bin,"ParcelStock",DropPoint,HudChrome.Green);
        }
        public static CourierLine CreateFacility(Transform parent, bool packing, RestaurantWallet wallet, PartsWallet parts, BurgerInventory player, CashFloor cash)
        {
            var root=new GameObject(packing?"CustomParcelMachine":"CustomCourierTray").transform;
            root.SetParent(parent,false);
            var line=root.gameObject.AddComponent<CourierLine>();
            line.wallet=wallet;line.parts=parts;line.player=player;line.cash=cash;
            line.hasMachine=packing;line.hasTray=!packing;line.Build(false);
            line.machine.gameObject.SetActive(packing);line.riderRoot.gameObject.SetActive(!packing);
            return line;
        }
        void BuildAutomation()
        {
            if(dedicatedSource)
            {
                var processor=new GameObject("CourierBurgerProcessor").transform;processor.SetParent(area,false);processor.position=ProcessorPosition;
                CourierVisuals.Part(processor,"ProcessorLandmarkBase",new Vector3(0,.5f,0),new Vector3(2.8f,1,2.4f),dark,true);
                CourierVisuals.Part(processor,"OvenShell",new Vector3(0,1.35f,-.3f),new Vector3(2.6f,1.2f,1.7f),gold,true);
                CourierVisuals.Part(processor,"OvenMouth",new Vector3(0,1.3f,.57f),new Vector3(1.9f,.65f,.08f),dark);
                CourierVisuals.Part(processor,"SteelTop",new Vector3(0,2,-.3f),new Vector3(2.8f,.16f,1.9f),white);
                foreach(float x in new[]{-.9f,.9f})CourierVisuals.Part(processor,"Vent",new Vector3(x,2.35f,-.7f),new Vector3(.3f,.7f,.3f),dark);
                var outputAnchor=new GameObject("ProcessorOutput").transform;outputAnchor.SetParent(processor,false);outputAnchor.localPosition=new Vector3(0,1.1f,.8f);
                source=processor.gameObject.AddComponent<ProductionStation>();source.Configure(outputAnchor,null,null,ProcessorSeconds,Capacity);
            }
            Vector3 start=source.transform.TransformPoint(new Vector3(0,1.1f,1));
            intakeBelt=new CourierConveyor(area,"BurgerIntakeChain",CourierConveyor.Rounded(start,
                new Vector3(start.x,1.1f,24),new Vector3(-3,1.1f,24),new Vector3(-3,1.1f,18),
                Machine+new Vector3(0,1.1f,-1.05f)),dark,white);
            parcelBelt=new CourierConveyor(area,"RedParcelChain",CourierConveyor.Rounded(
                Machine+new Vector3(0,1.1f,1.05f),new Vector3(-7,1.1f,23),
                Counter+new Vector3(-1.35f,1.1f,0)),dark,white);
            machineBelt=new CourierConveyor(area,"InternalPackagingChain",new[]{Machine+new Vector3(0,1.1f,-1.05f),Machine+new Vector3(0,1.1f,1.05f)},dark,white);
            foreach(string name in new[]{"ParcelInput","ParcelOutput","ParcelStock"})
                (machine.Find(name) ?? bin.Find(name))?.gameObject.SetActive(false);
            // Replace the blue block with a tunnel whose mouth is aligned to the moving burgers.
            machine.Find("MachineBody").gameObject.SetActive(false);
            machine.Find("Opening").gameObject.SetActive(false);
            machine.Find("WhiteFrame").gameObject.SetActive(false);
            foreach(Transform part in machine)if(part.name=="Roller")part.gameObject.SetActive(false);
            CourierVisuals.Part(machine,"TunnelRoof",new Vector3(0,1.95f,0),new Vector3(2.5f,.4f,1.6f),blue);
            machine.Find("InputTray").gameObject.SetActive(false);
            machine.Find("OutputTray").gameObject.SetActive(false);
            foreach(int side in new[]{-1,1})
                CourierVisuals.Part(machine,"TunnelPillar",new Vector3(side*1.02f,.95f,0),new Vector3(.42f,1.6f,1.6f),blue);
            RememberLayout();
        }
        Vector3 lastSource,lastMachine,lastBin;Quaternion lastSourceRotation,lastMachineRotation,lastBinRotation;
        void RememberLayout(){lastSource=source.transform.position;lastMachine=machine.position;lastBin=bin.position;lastSourceRotation=source.transform.rotation;lastMachineRotation=machine.rotation;lastBinRotation=bin.rotation;}
        public bool ConveyorEndpointsChanged=>intakeBelt!=null&&(lastSource!=source.transform.position||lastMachine!=machine.position||lastBin!=bin.position||lastSourceRotation!=source.transform.rotation||lastMachineRotation!=machine.rotation||lastBinRotation!=bin.rotation);
        public Vector3[][] PlannedConveyors(Transform moved,Vector3 position,float yaw)
        {
            if(intakeBelt==null||source==null)return null;
            Vector3 P(Transform part,Vector3 local)
            {
                var world=part.TransformPoint(local);
                return moved!=null&&(part==moved||part.IsChildOf(moved))?position+Quaternion.Euler(0,yaw,0)*moved.InverseTransformPoint(world):world;
            }
            var start=P(source.transform,new Vector3(0,1.1f,1));
            var inlet=P(machine,new Vector3(0,1.1f,-1.05f));var outlet=P(machine,new Vector3(0,1.1f,1.05f));
            var end=P(bin,new Vector3(-1.35f,1.1f,0));
            return new[]{new[]{start,inlet},new[]{inlet,outlet},new[]{outlet,end}};
        }
        public bool CanApplyConveyors(Vector3[][] routes)=>routes!=null&&intakeBelt.CanReroute(routes[0])&&machineBelt.CanReroute(routes[1])&&parcelBelt.CanReroute(routes[2]);
        public void ApplyConveyors(Vector3[][] routes)
        {
            if(!CanApplyConveyors(routes))return;
            intakeBelt.Reroute(routes[0]);machineBelt.Reroute(routes[1]);parcelBelt.Reroute(routes[2]);RememberLayout();
        }
        public void RefreshConveyors()
        {
            if(!ConveyorEndpointsChanged)return;
            Vector3 start=source.transform.TransformPoint(new Vector3(0,1.1f,1));
            Vector3 inlet=machine.TransformPoint(new Vector3(0,1.1f,-1.05f));
            Vector3 outlet=machine.TransformPoint(new Vector3(0,1.1f,1.05f));
            Vector3 end=bin.TransformPoint(new Vector3(-1.35f,1.1f,0));
            intakeBelt.Reroute(CourierConveyor.Rounded(start,start+source.transform.forward*2,
                inlet-machine.forward*2,inlet));
            machineBelt.Reroute(new[]{inlet,outlet});
            parcelBelt.Reroute(CourierConveyor.Rounded(outlet,outlet+machine.forward*2,end-bin.right*2,end));RememberLayout();
        }
        void AdvanceAutomation(float dt)
        {
            machineBelt.Advance(dt,item=>false);
            intakeBelt.Advance(dt,item=>
            {
                if(raw.Count>=Capacity)return false;
                item.SetParent(machine,true);raw.Add(item);Stack(raw,machine.TransformPoint(new Vector3(0,1.1f,-1.05f)),.17f);return true;
            });
            parcelBelt.Advance(dt,item=>
            {
                if(stock.Count>=Capacity)return false;
                item.SetParent(bin,true);stock.Add(item);Stack(stock,bin.TransformPoint(new Vector3(-.6f,.91f,0)),.28f);return true;
            });
            feedClock=Mathf.Max(0,feedClock-dt);
            if(feedClock<=0&&source.Product==KitchenProduct.Burger&&source.Stock>(dedicatedSource?0:1)&&raw.Count+intakeBelt.Count<Capacity&&intakeBelt.CanLoad)
            {
                if(source.TryTakeBurger(out var item))
                {if(item==null)item=BurgerVisualFactory.Create(area,0);intakeBelt.LoadItem(item);feedClock=1f;}
            }
            if(output.Count>0&&parcelBelt.CanLoad)
            {
                var item=output[0];output.RemoveAt(0);parcelBelt.LoadItem(item);
            }
        }
        public void SetPaused(bool value)=>paused=value;
        public void Advance(float seconds)
        {
            if(seconds<=0||paused||unfocused||!AreaOpen)return;
            while(seconds>.00001f)
            {
                float dt=Mathf.Min(.05f,seconds);seconds-=dt;
                transfer=Mathf.Max(0,transfer-dt);cooldown=Mathf.Max(0,cooldown-dt);partPickup=Mathf.Max(0,partPickup-dt);
                if(intakeBelt==null)TransferPlayer();else AdvanceAutomation(dt);
            for(int i=partsFlights.Count-1;i>=0;i--)
            {var motion=partsFlights[i];if(motion==null){partsFlights.RemoveAt(i);continue;}motion.Advance(dt);if(motion==null||motion.IsFinished)partsFlights.RemoveAt(i);}
                if(hasMachine)Process(dt);if(hasTray){AdvanceRiders(dt);AdvanceHandoff(dt);CollectParts();}
            }
        }
        bool Near(Vector3 point)=>player!=null&&player.isActiveAndEnabled&&ShopLayout.Horizontal(player.transform.position,point)<=.95f;
        void TransferPlayer()
        {
            if(transfer>0)return;bool moved=false;
            if(hasMachine&&Near(machine.TransformPoint(InputPoint-Machine))&&raw.Count<Capacity&&player.TryTakeBurger(out var burger))
            {raw.Add(burger);burger.SetParent(machine,true);Stack(raw,machine.TransformPoint(new Vector3(0,.35f,-1.25f)),.17f);moved=true;}
            else if(hasMachine&&Near(machine.TransformPoint(OutputPoint-Machine))&&output.Count>0&&!player.IsFull)
            {
                var parcel=output[output.Count-1];
                if(player.TryReceive(CarriedItemKind.RedParcel,parcel)){output.RemoveAt(output.Count-1);moved=true;}
            }
            else if(hasTray&&Near(bin.TransformPoint(DropPoint-Counter))&&stock.Count<Capacity&&player.TryTake(CarriedItemKind.RedParcel,out var parcel))
            {stock.Add(parcel);parcel.SetParent(bin,true);Stack(stock,bin.TransformPoint(new Vector3(-.6f,.91f,0)),.28f);moved=true;}
            if(moved)transfer=TransferSeconds;
        }
        void Process(float dt)
        {
            if(inProcess!=null)
            {
                work+=dt;
                if(source!=null)inProcess.position=Vector3.Lerp(machine.TransformPoint(new Vector3(0,1.1f,-1.05f)),machine.TransformPoint(new Vector3(0,1.1f,1.05f)),Mathf.Clamp01(work/ProcessingSeconds));
                inProcess.localScale=Vector3.one*Mathf.Lerp(1,.35f,Mathf.Clamp01(work/ProcessingSeconds));
                if(work>=ProcessingSeconds)
                {
                    BurgerVisual.Release(inProcess.gameObject);inProcess=null;
                    output.Add(CourierVisuals.RedParcel(machine));Stack(output,source!=null?machine.TransformPoint(new Vector3(0,1.1f,1.05f)):machine.TransformPoint(new Vector3(1.8f,.6f,0)),.28f);
                }
            }
            if(inProcess==null&&raw.Count>0&&output.Count+(parcelBelt?.Count??0)<Capacity)
            {
                inProcess=raw[raw.Count-1];raw.RemoveAt(raw.Count-1);inProcess.position=machine.TransformPoint(new Vector3(0,.85f,-1.05f));work=0;
            }
        }
        void AdvanceRiders(float dt)
        {
            for(int i=leaving.Count-1;i>=0;i--)
            {
                var rider=leaving[i];rider.Advance(dt,riderRoot.TransformPoint(Stop));
                if(rider.Finished){leaving.RemoveAt(i);BurgerVisual.Release(rider.gameObject);}
            }
            spawn+=dt;
            if(queue.Count<3&&spawn>=8)
            {
                spawn=0;queue.Add(BicycleCourier.Create(riderRoot,riderRoot.TransformPoint(CourierRoad.FullPath[0]),OrderQuantity,(spawnedRiders++%2)==1));
            }
            for(int i=0;i<queue.Count;i++)queue[i].Advance(dt,riderRoot.TransformPoint(Stop+Vector3.right*(i*4)));
            if(receiving==null&&Front!=null&&Front.Arrived(riderRoot.TransformPoint(Stop))&&stock.Count>0&&cooldown<=0&&wallet.CanCompleteSale())
            {
                receiving=Front;flight=stock[stock.Count-1];stock.RemoveAt(stock.Count-1);flightFrom=riderRoot.InverseTransformPoint(flight.position);handoff=0;
                flight.SetParent(receiving.transform,true);Stack(stock,bin.TransformPoint(new Vector3(-.6f,.91f,0)),.28f);
            }
        }
        void AdvanceHandoff(float dt)
        {
            if(receiving==null)return;
            handoff+=dt;float t=Mathf.Clamp01(handoff/.45f);
            flight.position=Vector3.Lerp(riderRoot.TransformPoint(flightFrom),receiving.transform.position+Vector3.up*1.1f,t)+Vector3.up*Mathf.Sin(t*Mathf.PI)*.45f;
            if(t<1)return;
            receiving.Receive(flight);flight=null;cooldown=.15f;
            if(receiving.Remaining==0)
            {
                queue.Remove(receiving);receiving.Depart();leaving.Add(receiving);
                wallet.RecordCompletedSale();CompletedOrders++;
                GetComponentInParent<SessionGoalTracker>()?.RecordMilestone(ShopGoalKind.CourierOrder);
                if(AutomationEnabled)GetComponentInParent<SessionGoalTracker>()?.RecordMilestone(ShopGoalKind.AutomatedOrder);
                cash.DropAt(riderRoot.TransformPoint(CashPoint),receiving.Quantity*CoinsPerParcel);
                var pile=CourierVisuals.PartsPile(riderRoot);pile.position=riderRoot.TransformPoint(PartsPoint+new Vector3((partsPiles.Count%3)*.45f,0,(partsPiles.Count/3)*.45f));
                partsPiles.Add(pile);partsAmounts.Add(receiving.Quantity*PartsPerParcel);
            }
            receiving=null;
        }
        void CollectParts()
        {
            if(player==null||partPickup>0)return;
            for(int i=partsPiles.Count-1;i>=0;i--)
            {
                if(ShopLayout.Horizontal(player.transform.position,partsPiles[i].position)>.85f||!parts.TryCollect(partsAmounts[i]))continue;
                var collected=partsPiles[i];partsPiles.RemoveAt(i);partsAmounts.RemoveAt(i);
                var motion=collected.gameObject.AddComponent<TrashMotion>();
                motion.Launch(player.transform,new Vector3(0,1.45f,.1f),Vector3.up*.25f,Vector3.one*.15f,()=>BurgerVisual.Release(collected.gameObject),.18f);
                collected.SetParent(area,true);partsFlights.Add(motion);partPickup=.04f;break;
            }
        }
        static void Stack(List<Transform> items,Vector3 point,float spacing)
        {for(int i=0;i<items.Count;i++)items[i].position=point+Vector3.up*(i*spacing);}
        void Update()=>Advance(Time.deltaTime);
        void OnApplicationPause(bool value)=>paused=value;
        void OnApplicationFocus(bool value)=>unfocused=!value;
        void OnDestroy(){if(Current==this)Current=null;}
    }
}
