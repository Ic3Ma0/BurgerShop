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
        public static readonly Vector3 Counter=new Vector3(2,0,23),DropPoint=new Vector3(2,.02f,21),Stop=new Vector3(2,0,26);
        public static readonly Vector3 CashPoint=new Vector3(4,.06f,23),PartsPoint=new Vector3(5.5f,.06f,23);
        public const int Capacity=8,OrderQuantity=4,CoinsPerParcel=20,PartsPerParcel=1;
        public const float ProcessingSeconds=1f,TransferSeconds=.35f;
        readonly List<Transform> raw=new List<Transform>(),output=new List<Transform>(),stock=new List<Transform>();
        readonly List<BicycleCourier> queue=new List<BicycleCourier>(),leaving=new List<BicycleCourier>();
        readonly List<Transform> partsPiles=new List<Transform>();
        readonly List<int> partsAmounts=new List<int>();
        Transform area,machine,bin,inProcess,flight;
        BicycleCourier receiving;
        Vector3 flightFrom;
        Material blue,white,dark,gold,road;
        RestaurantWallet wallet;PartsWallet parts;BurgerInventory player;CashFloor cash;
        float work,transfer,spawn,handoff,cooldown,partPickup;
        bool paused,unfocused;
        public int InputCount=>raw.Count;
        public int OutputCount=>output.Count;
        public int ProcessingCount=>inProcess!=null?1:0;
        public int StockCount=>stock.Count;
        public int CompletedOrders {get;private set;}
        public int WaitingCount=>queue.Count;
        public BicycleCourier Front=>queue.Count>0?queue[0]:null;
        public int GroundParts {get{int sum=0;foreach(int n in partsAmounts)sum+=n;return sum;}}
        public void Configure(RestaurantWallet earnings,PartsWallet currency,BurgerInventory actor,CashFloor floor)
        {
            Current=this;wallet=earnings;parts=currency;player=actor;cash=floor;Build();
        }
        void Build()
        {
            area=new GameObject("NorthCourierArea").transform;area.SetParent(transform,false);
            blue=RuntimeMaterials.Create(new Color(.13f,.43f,.85f));white=RuntimeMaterials.Create(HudChrome.Cream);
            dark=RuntimeMaterials.Create(new Color(.22f,.27f,.32f));gold=RuntimeMaterials.Create(new Color(1,.65f,.08f));road=RuntimeMaterials.Create(new Color(.16f,.18f,.20f));
            area.gameObject.AddComponent<BurgerVisual>().OwnMaterials(blue,white,dark,gold,road);
            CourierVisuals.Part(area,"CourierFloor",new Vector3(0,-.1f,20.5f),new Vector3(30,.2f,11),white,true);
            CourierVisuals.Part(area,"BicycleRoad",new Vector3(0,-.07f,28.5f),new Vector3(30,.14f,6),road,true);
            foreach(var t in GetComponentsInChildren<Transform>())if(t.name=="Wall+Z")t.gameObject.SetActive(false);
            foreach(int side in new[]{-1,1})
            {
                CourierVisuals.Part(area,"OldNorthWall",new Vector3(side*8.4f,.75f,15),new Vector3(13.2f,1.5f,.4f),dark,true);
                CourierVisuals.Part(area,"CourierSideWall",new Vector3(side*15,.6f,20.5f),new Vector3(.3f,1.2f,11),blue,true);
            }
            for(int i=-12;i<=12;i+=3)
                CourierVisuals.Part(area,"RoadDash",new Vector3(i,.02f,28),new Vector3(1.2f,.025f,.08f),white);
            CourierVisuals.Part(area,"BikeStopLine",Stop+new Vector3(-1,.03f,0),new Vector3(.10f,.03f,2.2f),white);
            machine=new GameObject("BlueParcelMachine").transform;machine.SetParent(area,false);machine.position=Machine;
            CourierVisuals.Part(machine,"MachineBody",new Vector3(0,1.05f,0),new Vector3(2.5f,2.1f,1.6f),blue,true);
            CourierVisuals.Part(machine,"WhiteFrame",new Vector3(0,1.05f,-.86f),new Vector3(2.1f,1.8f,.16f),white);
            CourierVisuals.Part(machine,"Opening",new Vector3(0,1,-.97f),new Vector3(1.65f,1.25f,.08f),dark);
            for(int i=0;i<5;i++)CourierVisuals.Part(machine,"Roller",new Vector3(0,.45f+i*.22f,-1.02f),new Vector3(1.58f,.05f,.05f),white);
            CourierVisuals.Part(machine,"InputTray",new Vector3(0,.2f,-1.3f),new Vector3(1.8f,.18f,.8f),dark);
            CourierVisuals.Part(machine,"OutputTray",new Vector3(1.8f,.5f,0),new Vector3(1.1f,.14f,1.25f),dark);
            ShopFixtures.CreateActionCircle(area,"ParcelInput",InputPoint,HudChrome.Green);
            ShopFixtures.CreateActionCircle(area,"ParcelOutput",OutputPoint,HudChrome.Green);
            bin=new GameObject("YellowCourierPickup").transform;bin.SetParent(area,false);bin.position=Counter;
            CourierVisuals.Part(bin,"YellowBase",new Vector3(0,.42f,0),new Vector3(2.8f,.84f,1.8f),gold,true);
            CourierVisuals.Part(bin,"TrayFloor",new Vector3(0,.86f,0),new Vector3(2.5f,.06f,1.5f),dark);
            foreach(int side in new[]{-1,1})
            {
                CourierVisuals.Part(bin,"TraySide",new Vector3(side*1.3f,1,0),new Vector3(.16f,.3f,1.8f),gold);
                CourierVisuals.Part(bin,"TrayEnd",new Vector3(0,1,side*.82f),new Vector3(2.7f,.3f,.16f),gold);
            }
            ShopFixtures.CreateActionCircle(area,"ParcelStock",DropPoint,HudChrome.Green);
        }
        public void SetPaused(bool value)=>paused=value;
        public void Advance(float seconds)
        {
            if(seconds<=0||paused||unfocused||area==null)return;
            while(seconds>.00001f)
            {
                float dt=Mathf.Min(.05f,seconds);seconds-=dt;
                transfer=Mathf.Max(0,transfer-dt);cooldown=Mathf.Max(0,cooldown-dt);partPickup=Mathf.Max(0,partPickup-dt);
                TransferPlayer();Process(dt);AdvanceRiders(dt);AdvanceHandoff(dt);CollectParts();
            }
        }
        bool Near(Vector3 point)=>player!=null&&player.isActiveAndEnabled&&ShopLayout.Horizontal(player.transform.position,point)<=.95f;
        void TransferPlayer()
        {
            if(transfer>0)return;bool moved=false;
            if(Near(InputPoint)&&raw.Count<Capacity&&player.TryTakeBurger(out var burger))
            {raw.Add(burger);burger.SetParent(machine,true);Stack(raw,Machine+new Vector3(0,.35f,-1.25f),.17f);moved=true;}
            else if(Near(OutputPoint)&&output.Count>0&&!player.IsFull)
            {
                var parcel=output[output.Count-1];
                if(player.TryReceive(CarriedItemKind.RedParcel,parcel)){output.RemoveAt(output.Count-1);moved=true;}
            }
            else if(Near(DropPoint)&&stock.Count<Capacity&&player.TryTake(CarriedItemKind.RedParcel,out var parcel))
            {stock.Add(parcel);parcel.SetParent(bin,true);Stack(stock,Counter+new Vector3(-.6f,.91f,0),.28f);moved=true;}
            if(moved)transfer=TransferSeconds;
        }
        void Process(float dt)
        {
            if(inProcess!=null)
            {
                work+=dt;inProcess.localScale=Vector3.one*Mathf.Lerp(1,.35f,Mathf.Clamp01(work/ProcessingSeconds));
                if(work>=ProcessingSeconds)
                {
                    BurgerVisual.Release(inProcess.gameObject);inProcess=null;
                    output.Add(CourierVisuals.RedParcel(machine));Stack(output,Machine+new Vector3(1.8f,.6f,0),.28f);
                }
            }
            if(inProcess==null&&raw.Count>0&&output.Count<Capacity)
            {
                inProcess=raw[raw.Count-1];raw.RemoveAt(raw.Count-1);inProcess.position=Machine+new Vector3(0,.85f,-1.05f);work=0;
            }
        }
        void AdvanceRiders(float dt)
        {
            for(int i=leaving.Count-1;i>=0;i--)
            {
                var rider=leaving[i];rider.Advance(dt,Stop);
                if(rider.Finished){leaving.RemoveAt(i);BurgerVisual.Release(rider.gameObject);}
            }
            spawn+=dt;
            if(queue.Count<3&&spawn>=8)
            {
                spawn=0;queue.Add(BicycleCourier.Create(area,new Vector3(14,0,26),OrderQuantity));
            }
            for(int i=0;i<queue.Count;i++)queue[i].Advance(dt,Stop+Vector3.right*(i*4));
            if(receiving==null&&Front!=null&&Front.Arrived(Stop)&&stock.Count>0&&cooldown<=0&&wallet.CanCompleteSale())
            {
                receiving=Front;flight=stock[stock.Count-1];stock.RemoveAt(stock.Count-1);flightFrom=flight.position;handoff=0;
                flight.SetParent(receiving.transform,true);Stack(stock,Counter+new Vector3(-.6f,.91f,0),.28f);
            }
        }
        void AdvanceHandoff(float dt)
        {
            if(receiving==null)return;
            handoff+=dt;float t=Mathf.Clamp01(handoff/.45f);
            flight.position=Vector3.Lerp(flightFrom,receiving.transform.position+Vector3.up*1.1f,t)+Vector3.up*Mathf.Sin(t*Mathf.PI)*.45f;
            if(t<1)return;
            receiving.Receive(flight);flight=null;cooldown=.15f;
            if(receiving.Remaining==0)
            {
                queue.Remove(receiving);receiving.Depart();leaving.Add(receiving);
                wallet.RecordCompletedSale();CompletedOrders++;
                cash.DropAt(CashPoint,receiving.Quantity*CoinsPerParcel);
                var pile=CourierVisuals.PartsPile(area);pile.position=PartsPoint+new Vector3((partsPiles.Count%3)*.45f,0,(partsPiles.Count/3)*.45f);
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
                BurgerVisual.Release(partsPiles[i].gameObject);partsPiles.RemoveAt(i);partsAmounts.RemoveAt(i);partPickup=.25f;break;
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
