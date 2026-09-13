using System;
using System.Collections.Generic;
using BurgerShop.Core;
using BurgerShop.Customer;
using BurgerShop.Economy;
using BurgerShop.Persistence;
using BurgerShop.Player;
using BurgerShop.UI;
using UnityEngine;

namespace BurgerShop.Restaurant
{
    public sealed class BagLine : MonoBehaviour
    {
        public static BagLine Current {get;private set;}
        public static readonly Vector3 Machine=new Vector3(-24,0,5), Workbench=new Vector3(-24,0,0), Counter=new Vector3(-24,0,-5);
        public static readonly Vector3 MachinePoint=new Vector3(-22,.02f,5), WorkPoint=new Vector3(-22,.02f,0), CounterPoint=new Vector3(-22,.02f,-5);
        public Transform MachineRoot => machineModel;
        public Transform WorkRoot => tableModel;
        public Transform CounterRoot => counterModel;
        public Vector3 MachinePosition => machineModel != null ? machineModel.TransformPoint(new Vector3(2,.02f,0)) : MachinePoint;
        public Vector3 WorkPosition => tableModel != null ? tableModel.TransformPoint(new Vector3(2,.02f,0)) : WorkPoint;
        public Vector3 ServingPosition => counterModel != null ? counterModel.TransformPoint(new Vector3(2,.02f,0)) : CounterPoint;
        string instancePrefix = "";
        public bool Expanded {get;private set;}
        public bool MachineBuilt {get;private set;}
        public bool TableBuilt {get;private set;}
        public bool CounterBuilt {get;private set;}
        public FacilityUnlockZone MachinePad {get;private set;}
        public FacilityUnlockZone TablePad {get;private set;}
        public FacilityUnlockZone CounterPad {get;private set;}
        public int MachineLevel=1,TableLevel=1,CounterLevel=1;
        public float ProductionSeconds=>3f-.5f*(MachineLevel-1);
        public float ProcessingSeconds=>.6f-.1f*(TableLevel-1);
        public int EmptyStock=>empty.Count;
        public int InputBurgers=>raw.Count;
        public int InputBags=>bags.Count;
        public int OutputCount=>output.Count;
        public int StockCount=>stock.Count;
        public int ProcessingCount=>processing?1:0;
        public CustomerQueue Queue {get;private set;}
        public int CompletedOrders {get;private set;}
        public bool ReadyToSell=>CounterBuilt&&!paused&&!unfocused&&flightCustomer==null&&serviceCooldown<=0&&StockCount>0&&Queue!=null&&Queue.ReadyCustomer!=null;
        public bool HasWork=>OutputCount>0||InputBurgers>0||processing;
        readonly List<Transform> empty=new List<Transform>(),raw=new List<Transform>(),bags=new List<Transform>(),output=new List<Transform>(),stock=new List<Transform>();
        readonly Dictionary<BurgerInventory,float> transferCooldown=new Dictionary<BurgerInventory,float>();
        RestaurantWallet wallet;SessionGoalTracker goals;GrowthUpgrades growth;WorkerHiringZone crew;BurgerInventory player;CashFloor cash;
        Transform area,machineModel,tableModel,counterModel,inProcess;
        TextMesh machineLabel,tableLabel,counterLabel,expandLabel;
        Material paper,dark;
        float produced,processAge,serviceCooldown,flightAge;
        bool processing,paused,unfocused;
        CustomerAgent flightCustomer;Transform flightItem;Vector3 flightOrigin;RestaurantWorker flightWorker;
        public Func<int> OrderQuantityFactory {get;set;}=()=>UnityEngine.Random.value<.7f?1:2;

        public void BindCrew(WorkerHiringZone staff)=>crew=staff;
        public void Configure(RestaurantWallet earnings,SessionGoalTracker tracker,GrowthUpgrades upgrades,WorkerHiringZone staff,BurgerInventory actor,CashFloor floor)
        {
            Current=this;wallet=earnings;goals=tracker;growth=upgrades;crew=staff;player=actor;cash=floor;crew?.RegisterBagLine(this);
        }
        public bool TryExpand()
        {
            if(Expanded||goals==null||!goals.Allows(ShopRanks.ContentEnd))return false;
            BuildArea();GetComponent<RestaurantPersistence>()?.Flush();return true;
        }
        void BuildArea()
        {
            if(Expanded)return;Expanded=true;
            area=new GameObject("WestExpansion").transform;area.SetParent(transform,false);
            paper=RuntimeMaterials.Create(new Color(.92f,.82f,.64f));dark=RuntimeMaterials.Create(new Color(.22f,.27f,.3f));
            area.gameObject.AddComponent<BurgerVisual>().OwnMaterials(paper,dark);
            BagVisualFactory.Part(area,"WestFloor",PrimitiveType.Cube,new Vector3(-21,-.1f,0),new Vector3(12,.2f,18),paper,true);
            foreach(var t in GetComponentsInChildren<Transform>())if(t.name=="Wall-X")t.gameObject.SetActive(false);
            foreach(float z in new[]{-8.25f,8.25f})BagVisualFactory.Part(area,"OldWestWall",PrimitiveType.Cube,new Vector3(-15,.75f,z),new Vector3(.4f,1.5f,13.5f),dark,true);
            BagVisualFactory.Part(area,"WestWall",PrimitiveType.Cube,new Vector3(-27,.75f,0),new Vector3(.4f,1.5f,18),dark,true);
            foreach(float z in new[]{-9f,9f})BagVisualFactory.Part(area,"ExpansionEdge",PrimitiveType.Cube,new Vector3(-21,.75f,z),new Vector3(12,1.5f,.4f),dark,true);
            MachinePad=MakePad("BAGS",Machine,250,()=>BuildMachine());TablePad=MakePad("BAG TABLE",Workbench,200,()=>BuildTable());CounterPad=MakePad("PICKUP",Counter,300,()=>BuildCounter());
            RefreshPads();if(expandLabel!=null)expandLabel.gameObject.SetActive(false);
        }
        FacilityUnlockZone MakePad(string name,Vector3 point,int price,Action unlocked)
        {
            var obj=new GameObject("Buy_"+name);obj.transform.SetParent(area,false);
            var pad=ShopFixtures.CreateActionCircle(obj.transform,"Pad",point+Vector3.up*.02f,HudChrome.Gold);
            var label=ShopFixtures.CreateStationLabel(obj.transform,"BuyLabel",point+Vector3.up*1.3f,name);
            var zone=obj.AddComponent<FacilityUnlockZone>();zone.Configure(wallet,player,pad,price,name,()=>{unlocked();RefreshPads();GetComponent<RestaurantPersistence>()?.Flush();},label);return zone;
        }
        void RefreshPads(){MachinePad.SetRankVisible(true);TablePad.SetRankVisible(MachineBuilt);CounterPad.SetRankVisible(TableBuilt);}
        Transform Model(string name,Vector3 point)
        {
            var root=new GameObject(name).transform;root.SetParent(area,false);root.position=point;
            BagVisualFactory.Part(root,"Base",PrimitiveType.Cube,new Vector3(0,.4f,0),new Vector3(1.5f,.8f,1.1f),dark,true);return root;
        }
        void BuildMachine()
        {
            if(MachineBuilt)return;MachineBuilt=true;machineModel=Model("BagMachine",Machine);
            var roll=BagVisualFactory.Part(machineModel,"PaperRoll",PrimitiveType.Cylinder,new Vector3(0,1.2f,0),new Vector3(.8f,.6f,.8f),paper);roll.transform.localRotation=Quaternion.Euler(0,0,90);
            BagVisualFactory.Part(machineModel,"FoldingPress",PrimitiveType.Cube,new Vector3(0,.85f,-.3f),new Vector3(1.1f,.12f,.45f),paper);
            machineLabel=ShopFixtures.CreateStationLabel(machineModel,"BagMachineLabel",Machine+Vector3.up*2,"BAGS 0/8");
            ShopFixtures.CreateActionCircle(machineModel,"BagCollect",MachinePoint,HudChrome.Green);
            Register("bag-machine","Bag machine",machineModel,Machine+new Vector3(-1.8f,.02f,0),n=>$"Bag every {3f-.5f*(n-1):0.0}s",n=>MachineLevel=n);
        }
        void BuildTable()
        {
            if(TableBuilt)return;TableBuilt=true;tableModel=Model("BagTable",Workbench);
            BagVisualFactory.Part(tableModel,"InputDivider",PrimitiveType.Cube,new Vector3(0,.87f,0),new Vector3(.08f,.18f,1.1f),paper);
            tableLabel=ShopFixtures.CreateStationLabel(tableModel,"BagTableLabel",Workbench+Vector3.up*1.8f,"Need burgers");
            ShopFixtures.CreateActionCircle(tableModel,"BagWork",WorkPoint,HudChrome.Green);
            Register("bag-table","Bagging table",tableModel,Workbench+new Vector3(-1.8f,.02f,0),n=>$"Packing {.6f-.1f*(n-1):0.00}s",n=>TableLevel=n);
        }
        void BuildCounter()
        {
            if(CounterBuilt)return;CounterBuilt=true;counterModel=Model("PickupCounter",Counter);
            counterLabel=ShopFixtures.CreateStationLabel(counterModel,"BagCounterLabel",Counter+Vector3.up*1.8f,"PICKUP 0/8");
            ShopFixtures.CreateActionCircle(counterModel,"PickupServe",CounterPoint,Color.white);
            Register("bag-counter","Pickup counter",counterModel,Counter+new Vector3(-1.8f,.02f,0),n=>$"Item every {.6f-.05f*(n-1):0.00}s",n=>CounterLevel=n);
            Queue=new GameObject("PickupQueue").AddComponent<CustomerQueue>();Queue.transform.SetParent(counterModel,true);
            Queue.CustomerKindFactory=()=>CustomerKind.Normal;Queue.OrderQuantityFactory=()=>OrderQuantityFactory();
            Queue.Configure(new Vector3(-18,0,-8),new Vector3(-19,0,-7),new[]{new Vector3(-24,0,-6.8f),new Vector3(-22,0,-6.8f),new Vector3(-20,0,-6.8f)},Counter,6,6);
        }
        void Register(string id,string title,Transform target,Vector3 pos,Func<int,string> benefit,Action<int> apply)
        {
            growth?.Register(new GrowthUpgrades.Offer{Id=instancePrefix+id,Title=title,Target=target,Position=pos,Costs=new[]{150,300},Benefit=benefit,Apply=n=>
            {
                apply(n);
                if(id=="bag-counter"||id=="bag-table")
                {
                    CounterTierVisual.Create(target,"CounterAppearance",target.position,1.5f,1.1f,.8f,FoodIcon.Bagged,n);
                    return;
                }
                var old=target.Find("TierBars");if(old!=null){old.gameObject.SetActive(false);BurgerVisual.Release(old.gameObject);}
                var bars=new GameObject("TierBars").transform;bars.SetParent(target,false);
                for(int i=1;i<n;i++)BagVisualFactory.Part(bars,"Trim",PrimitiveType.Cube,new Vector3(.6f,.85f+i*.12f,0),new Vector3(.1f,.08f,.8f),paper);
            }});
        }
        public void Advance(float seconds)
        {
            if(seconds<=0||paused||unfocused||!Expanded)return;
            // One clock owns the recipe and transfers, irrespective of operator count.
            while(seconds>.00001f)
            {
                float dt=Mathf.Min(.05f,seconds);seconds-=dt;
                if(MachineBuilt&&empty.Count<8)
                {
                    produced+=dt;if(produced>=ProductionSeconds){produced-=ProductionSeconds;var item=BagVisualFactory.Create(machineModel,false);empty.Add(item);Restack(empty,machineModel.TransformPoint(new Vector3(.8f,.2f,0)),.1f);}
                }
                TickActor(player,dt);
                if(crew!=null)foreach(var w in crew.Workers)if(w!=null&&(w.Job==WorkerJob.Bag||w.Job==WorkerJob.BagSell))TickActor(w.Inventory,dt);
                bool staffed=AtWork(player);
                if(crew!=null)foreach(var w in crew.Workers)if(w!=null&&w.Job==WorkerJob.Bag&&AtWork(w.Inventory))staffed=true;
                if(TableBuilt&&staffed)
                {
                    if(processing)
                    {
                        processAge+=dt;inProcess.localScale=Vector3.one*Mathf.Lerp(.5f,1,Mathf.Clamp01(processAge/ProcessingSeconds));
                        if(processAge>=ProcessingSeconds){output.Add(inProcess);inProcess=null;processing=false;Restack(output,tableModel.TransformPoint(new Vector3(.7f,1,0)),.22f);}
                    }
                    if(!processing&&raw.Count>0&&bags.Count>0&&output.Count<8)
                    {
                        DestroyLast(raw);DestroyLast(bags);processing=true;processAge=0;
                        inProcess=BagVisualFactory.Create(tableModel,true);inProcess.position=tableModel.TransformPoint(Vector3.up*1.1f);
                    }
                }
                serviceCooldown=Mathf.Max(0,serviceCooldown-dt);AdvanceSale(dt);
            }
            if(machineLabel!=null)machineLabel.text=$"BAGS {EmptyStock}/8";
            if(tableLabel!=null)tableLabel.text=$"BURGER {InputBurgers} · BAGS {InputBags}\n"+(processing?"Packing":InputBurgers==0?"Need burgers":InputBags==0?"Need bags":OutputCount>=8?"FULL":"Ready")+$" · OUT {OutputCount}";
            if(counterLabel!=null)counterLabel.text=$"PICKUP {StockCount}/8";
            if(Queue!=null)foreach(var c in Queue.Customers)
            {
                var label=c.GetComponentInChildren<TextMesh>(true);label.name="BagOrderQuantity";
                var icon=c.transform.Find("OrderBubble/BurgerIcon");if(icon!=null&&icon.gameObject.activeSelf)
                {icon.gameObject.SetActive(false);var bag=BagVisualFactory.Create(icon.parent,true);bag.localPosition=icon.localPosition;bag.localScale=Vector3.one*.55f;}
            }
        }
        bool AtWork(BurgerInventory actor)=>actor!=null&&actor.isActiveAndEnabled&&ShopLayout.Horizontal(actor.transform.position,WorkPosition)<=.9f;
        void TickActor(BurgerInventory actor,float dt)
        {
            if(actor==null||!actor.isActiveAndEnabled)return;
            transferCooldown.TryGetValue(actor,out float cooldown);cooldown=Mathf.Max(0,cooldown-dt);
            if(cooldown>0){transferCooldown[actor]=cooldown;return;}
            bool moved=false;
            if(MachineBuilt&&ShopLayout.Horizontal(actor.transform.position,MachinePosition)<=.9f&&empty.Count>0&&!actor.IsFull)
            {var item=empty[empty.Count-1];if(actor.TryReceive(CarriedItemKind.EmptyBag,item)){empty.RemoveAt(empty.Count-1);moved=true;}}
            else if(TableBuilt&&AtWork(actor))
            {
                if(actor.LooseCount>0&&raw.Count<8&&actor.TryTakeBurger(out var burger))
                {raw.Add(burger);burger.SetParent(tableModel,true);Restack(raw,tableModel.TransformPoint(new Vector3(-.5f,1,0)),.12f);actor.GetComponent<RestaurantWorker>()?.RawDeposited(1);moved=true;}
                else if(actor.EmptyBagCount>0&&bags.Count<8&&actor.TryTake(CarriedItemKind.EmptyBag,out var bag))
                {bags.Add(bag);bag.SetParent(tableModel,true);Restack(bags,tableModel.TransformPoint(new Vector3(0,1,0)),.12f);moved=true;}
                else if(actor.LooseCount==0&&actor.EmptyBagCount==0&&output.Count>0&&!actor.IsFull)
                {var item=output[output.Count-1];if(actor.TryReceive(CarriedItemKind.Bagged,item)){output.RemoveAt(output.Count-1);moved=true;}}
            }
            else if(CounterBuilt&&ShopLayout.Horizontal(actor.transform.position,ServingPosition)<=.9f)
            {
                if(stock.Count<8&&actor.TryTake(CarriedItemKind.Bagged,out var item))
                {stock.Add(item);item.SetParent(counterModel,true);Restack(stock,counterModel.TransformPoint(Vector3.up),.18f);moved=true;}
                TryServe(actor);
            }
            transferCooldown[actor]=moved?.35f:0;
        }
        public bool TryServe(BurgerInventory actor)
        {
            if(!ReadyToSell||actor==null||ShopLayout.Horizontal(actor.transform.position,ServingPosition)>.9f||!wallet.CanCompleteSale())return false;
            var c=Queue.ReadyCustomer;if(!c.Order.TryReserve())return false;
            flightCustomer=c;flightItem=stock[stock.Count-1];stock.RemoveAt(stock.Count-1);flightOrigin=counterModel.InverseTransformPoint(flightItem.position);flightItem.SetParent(c.transform,true);flightAge=0;flightWorker=actor.GetComponent<RestaurantWorker>();return true;
        }
        void AdvanceSale(float dt)
        {
            if(flightCustomer==null)return;flightAge+=dt;float t=Mathf.Clamp01(flightAge/.45f);
            flightItem.position=Vector3.Lerp(counterModel.TransformPoint(flightOrigin),flightCustomer.transform.position+Vector3.up,t)+Vector3.up*Mathf.Sin(t*Mathf.PI)*.5f;
            if(t<1)return;
            var c=flightCustomer;c.ReceiveItem(flightItem);serviceCooldown=.15f-.05f*(CounterLevel-1);
            if(c.Order.TrySettle())
            {
                Queue.TryDequeueReadyCustomer(out _);int money=c.OrderSize*20;
                c.BeginDeparture(flightItem,new[]{counterModel.TransformPoint(new Vector3(-2,0,-3)),counterModel.TransformPoint(new Vector3(6,0,-3))},money,null,true);
                wallet.RecordCompletedSale();cash?.DropAt(counterModel.TransformPoint(new Vector3(1,0,-1)),money);CompletedOrders++;
                FeedbackDirector.Current?.World(c.transform.position,"",.45f);flightWorker?.RecordBagOrder();
            }
            flightCustomer=null;flightItem=null;flightWorker=null;
        }
        static void Restack(List<Transform> items,Vector3 origin,float step){for(int i=0;i<items.Count;i++)items[i].position=origin+Vector3.up*i*step;}
        static void DestroyLast(List<Transform> list){var t=list[list.Count-1];list.RemoveAt(list.Count-1);if(t!=null)BurgerVisual.Release(t.gameObject);}
        public static BagLine CreateFacility(Transform parent, Building.FacilityKind kind, string id,
            RestaurantWallet wallet, SessionGoalTracker goals, GrowthUpgrades growth, WorkerHiringZone crew, BurgerInventory player, CashFloor cash)
        {
            var root = new GameObject("CustomBagFacility").transform; root.SetParent(parent,false);
            var line = root.gameObject.AddComponent<BagLine>();
            line.wallet=wallet; line.goals=goals; line.growth=growth; line.crew=crew; line.player=player; line.cash=cash;
            line.instancePrefix=id+":"; line.Expanded=true; line.area=root;
            line.paper=RuntimeMaterials.Create(new Color(.92f,.82f,.64f)); line.dark=RuntimeMaterials.Create(new Color(.22f,.27f,.3f));
            root.gameObject.AddComponent<BurgerVisual>().OwnMaterials(line.paper,line.dark);
            if(kind==Building.FacilityKind.BagMachine)line.BuildMachine();
            else if(kind==Building.FacilityKind.BagTable)line.BuildTable();
            else if(kind==Building.FacilityKind.BagCounter)line.BuildCounter();
            else throw new ArgumentOutOfRangeException(nameof(kind));
            return line;
        }
        public void Restore(RestaurantSaveData data)
        {
            if(data==null||data.version<9||!data.westExpanded)return;BuildArea();
            if(data.bagMachineBuilt){BuildMachine();MachinePad.RestorePurchased();}
            if(data.bagTableBuilt){BuildTable();TablePad.RestorePurchased();}
            if(data.bagCounterBuilt){BuildCounter();CounterPad.RestorePurchased();}
            MachinePad.RestoreInvestment(data.bagMachineInvestment);TablePad.RestoreInvestment(data.bagTableInvestment);CounterPad.RestoreInvestment(data.bagCounterInvestment);RefreshPads();
        }
        void Update()
        {
            if(!Expanded&&goals!=null&&goals.Allows(ShopRanks.ContentEnd)&&expandLabel==null)
                expandLabel=ShopFixtures.CreateStationLabel(transform,"ExpansionMarker",new Vector3(-14,1.4f,-2),"WEST EXPANSION\nTap star → Expand");
            Advance(Time.deltaTime);
        }
        void OnApplicationPause(bool value)=>paused=value;
        void OnApplicationFocus(bool value)=>unfocused=!value;
        void OnDestroy(){if(Current==this)Current=null;}
    }
}
