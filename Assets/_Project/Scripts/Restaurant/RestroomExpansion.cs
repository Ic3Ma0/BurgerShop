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
    public sealed class RestroomExpansion : MonoBehaviour
    {
        public const int Cost=300, UnlockRank=ShopRanks.ExtraKitchenRank, Capacity=4;
        public const float UseSeconds=4, WashSeconds=1, CleanSeconds=3, MaxWaitSeconds=20;
        public static readonly Rect Floor=Rect.MinMaxRect(-5,-21,4,-14);
        public static readonly Vector3 Door=new Vector3(0,0,-15), Wash=new Vector3(2.8f,0,-16.8f);
        public static RestroomExpansion Current {get;private set;}
        public bool Built {get;private set;}
        public FacilityUnlockZone Pad {get;private set;}
        public int Invested=>Pad?.Invested??0;
        public bool Unlocked=>goals!=null&&goals.Allows(UnlockRank);
        public int Remaining=>Pad?.Remaining??Cost;
        public bool TryPurchase()
        {
            if(Built||!Unlocked||Pad==null||Pad.Wallet==null)return false;
            if(!Pad.Wallet.TrySpend(Remaining,()=>Pad.RestoreInvestment(Cost)))return false;
            goals?.AddUpgradeStars();
            GetComponent<RestaurantPersistence>()?.Flush();return Built;
        }
        public int VisitorCount=>visitors.Count;
        readonly List<CustomerAgent> visitors=new List<CustomerAgent>();
        readonly CustomerAgent[] occupants=new CustomerAgent[2];
        readonly Transform[] doors=new Transform[2];
        readonly TextMesh[] signs=new TextMesh[2];
        SessionGoalTracker goals;
        BurgerInventory player;
        readonly bool[] dirty=new bool[2];
        readonly float[] cleaning=new float[2];
        readonly RestaurantWorker[] cleaners=new RestaurantWorker[2];
        public int DirtyMask=>(dirty[0]?1:0)|(dirty[1]?2:0);
        public bool IsDirty(int index)=>index>=0&&index<2&&dirty[index];
        public static Vector3 UsePoint(int index)=>Seat(index)+Vector3.forward*.95f;
        Material cream,blue,white,dark;
        Transform area;
        public void Configure(RestaurantWallet wallet,BurgerInventory player,SessionGoalTracker tracker)
        {
            Current=this;goals=tracker;this.player=player;
            cream=RuntimeMaterials.Create(HudChrome.Cream);blue=RuntimeMaterials.Create(new Color(.3f,.65f,.75f));
            white=RuntimeMaterials.Create(Color.white);dark=RuntimeMaterials.Create(new Color(.15f,.23f,.28f));
            var owner=new GameObject("RestroomMaterials").AddComponent<BurgerVisual>();owner.transform.SetParent(transform,false);owner.OwnMaterials(cream,blue,white,dark);
            var point=ShopFixtures.CreateActionCircle(transform,"RestroomPurchase",new Vector3(0,.02f,-12.5f),HudChrome.Green);
            var label=Label(transform,"RestroomPurchaseLabel",new Vector3(0,2,-12.5f),"RESTROOM");
            Pad=point.gameObject.AddComponent<FacilityUnlockZone>();Pad.Configure(wallet,player,point,Cost,"RESTROOM",Build,label);
            Pad.SetStoreOnly();
        }
        void Update()
        {
            Pad?.SetRankVisible(goals!=null&&goals.Allows(UnlockRank));
            AdvancePlayerCleaning(Time.deltaTime);
            for(int i=visitors.Count-1;i>=0;i--)if(visitors[i]==null||!visitors[i].gameObject.activeInHierarchy)Release(visitors[i]);
            for(int i=0;i<2;i++)if(doors[i]!=null)
            {
                bool occupied=occupants[i]!=null;
                bool inside=occupied&&Vector3.Distance(occupants[i].transform.position,UsePoint(i))<.35f;
                doors[i].localRotation=Quaternion.RotateTowards(doors[i].localRotation,Quaternion.Euler(0,inside?0:80,0),180*Time.deltaTime);
                signs[i].text=dirty[i]?$"CLEAN {Mathf.CeilToInt(CleanSeconds-cleaning[i])}s":occupied?"BUSY":"FREE";
                signs[i].color=dirty[i]?HudChrome.Tomato:HudChrome.Ink;
            }
        }
        public void AdvancePlayerCleaning(float seconds)
        {
            for(int i=0;i<2;i++)if(dirty[i]&&player!=null&&Vector3.Distance(player.transform.position,UsePoint(i)+Vector3.up)<1.4f)Clean(i,seconds);
        }
        public void Restore(bool built,int investment,int mask=0)
        {
            dirty[0]=(mask&1)!=0;dirty[1]=(mask&2)!=0;
            if(built){Build();Pad.RestorePurchased();}else Pad.RestoreInvestment(investment);
        }
        public bool TryVisit(CustomerAgent customer)
        {
            if(!Built||customer==null||customer.TicketNumber%2!=0||visitors.Count>=Capacity)return false;
            if(visitors.Contains(customer))return true;
            if(Building.FacilityLayout.Current?.Route(customer.transform.position,Door)==null)return false;
            visitors.Add(customer);return true;
        }
        public int Acquire(CustomerAgent customer)
        {
            if(!visitors.Contains(customer))return -1;
            for(int i=0;i<2;i++)if(occupants[i]==customer)return i;
            for(int i=0;i<2;i++)if(occupants[i]==null&&!dirty[i]){occupants[i]=customer;return i;}
            return -1;
        }
        public Vector3 WaitPoint(CustomerAgent customer)=>new Vector3(0,0,-16.5f-Mathf.Max(0,visitors.IndexOf(customer)-2)*.9f);
        public static Vector3 Seat(int index)=>new Vector3(index==0?-3:1.5f,0,-19);
        public void Release(CustomerAgent customer)
        {visitors.Remove(customer);for(int i=0;i<2;i++)if(occupants[i]==customer)occupants[i]=null;}
        public void FinishUse(CustomerAgent customer,int index)
        {
            if(index<0||index>=2||occupants[index]!=customer)return;
            dirty[index]=true;cleaning[index]=0;occupants[index]=null;
            GetComponent<RestaurantPersistence>()?.Flush();
        }
        public int ClaimCleaning(RestaurantWorker worker)
        {
            for(int i=0;i<2;i++)if(dirty[i]&&(cleaners[i]==null||!cleaners[i].isActiveAndEnabled||cleaners[i]==worker)){cleaners[i]=worker;return i;}
            return -1;
        }
        public void ReleaseCleaner(RestaurantWorker worker){for(int i=0;i<2;i++)if(cleaners[i]==worker)cleaners[i]=null;}
        public bool Clean(int index,float seconds)
        {
            if(!IsDirty(index))return true;if(seconds<=0)return false;
            cleaning[index]+=seconds;
            if(cleaning[index]<CleanSeconds)return false;
            dirty[index]=false;cleaning[index]=0;cleaners[index]=null;
            GetComponent<RestaurantPersistence>()?.Flush();return true;
        }
        void Build()
        {
            if(Built)return;Built=true;
            area=new GameObject("RestroomExpansion").transform;area.SetParent(transform,false);
            var old=transform.Find("Wall-Z_W");
            if(old!=null)
            {
                var bounds=old.GetComponent<Collider>().bounds;old.gameObject.SetActive(false);
                Part("RestroomWallHallLeft",new Vector3((bounds.min.x-1.5f)/2,.6f,-15),new Vector3(-1.5f-bounds.min.x,1.2f,.4f),blue);
                Part("RestroomWallHallRight",new Vector3((bounds.max.x+1.5f)/2,.6f,-15),new Vector3(bounds.max.x-1.5f,1.2f,.4f),blue);
            }
            Part("RestroomFloor",new Vector3(-.5f,-.12f,-18),new Vector3(9,.24f,6.4f),cream);
            Part("RestroomWallBack",new Vector3(-.5f,.7f,-21),new Vector3(9,1.4f,.25f),blue);
            Part("RestroomWallWest",new Vector3(-5,.7f,-18),new Vector3(.25f,1.4f,6),blue);
            Part("RestroomWallEast",new Vector3(4,.7f,-18),new Vector3(.25f,1.4f,6),blue);
            Part("RestroomDoorHeader",new Vector3(0,2.5f,-15),new Vector3(3.3f,.6f,.45f),blue);
            Label(area,"RestroomSign",new Vector3(0,2.5f,-14.7f),"WC");
            for(int i=0;i<2;i++)
            {
                Vector3 seat=Seat(i);
                Part("ToiletPedestal",seat+new Vector3(0,.3f,-.45f),new Vector3(.65f,.6f,.85f),white);
                Part("ToiletBowl",seat+new Vector3(0,.65f,-.4f),new Vector3(.9f,.2f,1.1f),white,PrimitiveType.Sphere);
                Part("ToiletSeat",seat+new Vector3(0,.74f,-.4f),new Vector3(.62f,.03f,.75f),dark,PrimitiveType.Sphere,false);
                Part("ToiletTank",seat+new Vector3(0,.95f,-1.05f),new Vector3(.9f,.9f,.3f),white);
                Part("RestroomWallPartition",seat+new Vector3(-1.3f,.65f,-.5f),new Vector3(.15f,1.3f,2.5f),blue);
                var hinge=new GameObject("CubicleDoorHinge").transform;hinge.SetParent(area,false);hinge.position=seat+new Vector3(-1.1f,0,1.05f);doors[i]=hinge;
                var door=Part("CubicleDoor",hinge.position+new Vector3(1,.65f,0),new Vector3(2,1.3f,.1f),blue,PrimitiveType.Cube,false);door.SetParent(hinge,true);
                signs[i]=Label(area,"CubicleStatus",seat+new Vector3(0,1.7f,1.1f),"FREE");
            }
            Part("SinkCabinet",new Vector3(3.6f,.5f,-16.8f),new Vector3(.65f,1,1.2f),blue);
            Part("SinkBasin",new Vector3(3.6f,1.05f,-16.8f),new Vector3(.7f,.15f,1.25f),white);
            Part("SinkTap",new Vector3(3.7f,1.3f,-16.8f),new Vector3(.1f,.35f,.1f),dark);
            Building.FacilityLayout.Current?.RefreshNavigation();
            GetComponent<RestaurantPersistence>()?.Flush();
        }
        Transform Part(string name,Vector3 position,Vector3 scale,Material mat,PrimitiveType type=PrimitiveType.Cube,bool solid=true)
        {
            var part=GameObject.CreatePrimitive(type);part.name=name;part.transform.SetParent(area,false);part.transform.position=position;part.transform.localScale=scale;part.GetComponent<Renderer>().sharedMaterial=mat;
            if(!solid)part.GetComponent<Collider>().enabled=false;
            return part.transform;
        }
        TextMesh Label(Transform parent,string name,Vector3 point,string text)
        {
            var label=new GameObject(name).AddComponent<TextMesh>();label.transform.SetParent(parent,false);label.transform.position=point;
            label.text=text;label.anchor=TextAnchor.MiddleCenter;label.fontSize=48;label.characterSize=.08f;label.color=HudChrome.Ink;return label;
        }
        void OnDestroy(){if(Current==this)Current=null;}
    }
}
