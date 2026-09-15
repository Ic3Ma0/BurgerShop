using System.Collections.Generic;
using BurgerShop.Core;
using BurgerShop.Customer;
using BurgerShop.Economy;
using BurgerShop.Persistence;
using BurgerShop.Player;
using BurgerShop.UI;
using UnityEngine;
using UnityEngine.UI;

namespace BurgerShop.Restaurant
{
    public sealed class RestroomExpansion : MonoBehaviour
    {
        public const int Cost=300, UnlockRank=4, Capacity=4;
        public const int CleanBotCost=50;
        public const float UseSeconds=4, WashSeconds=1, CleanSeconds=3, MaxWaitSeconds=20, CleanBotDuration=60, CleanerRadius=1.1f;
        public static readonly Rect Floor=Rect.MinMaxRect(-5,-21,4,-14);
        public static readonly Vector3 Door=new Vector3(0,0,-15), Wash=new Vector3(2.70f,0,-16.7f);
        public static RestroomExpansion Current {get;private set;}
        public bool Built {get;private set;}
        public FacilityUnlockZone Pad {get;private set;}
        public int Invested=>Pad?.Invested??0;
        public bool Unlocked=>goals!=null&&goals.Allows(UnlockRank);
        public int Remaining=>Pad?.Remaining??Cost;
        public float CleanBotRemaining {get;private set;}
        public string CleanBotTimerCopy=>RestroomFurniture.FormatTimer(CleanBotRemaining);
        public bool TryPurchase()
        {
            if(Built||!Unlocked||Pad==null||Pad.Wallet==null)return false;
            if(!Pad.Wallet.TrySpend(Remaining,()=>Pad.RestoreInvestment(Cost)))return false;
            GetComponent<RestaurantPersistence>()?.Flush();return Built;
        }
        public int VisitorCount=>visitors.Count;
        readonly List<CustomerAgent> visitors=new List<CustomerAgent>();
        readonly CustomerAgent[] occupants=new CustomerAgent[2];
        readonly Transform[] doors=new Transform[RestroomFurniture.StallCount];
        readonly Transform[] dirtyMarks=new Transform[2];
        readonly TextMesh[] signs=new TextMesh[2];
        SessionGoalTracker goals;
        BurgerInventory player;
        RestaurantWallet wallet;
        Text cleanBotTimer;
        float cleanerDwell;
        readonly bool[] dirty=new bool[2];
        readonly float[] cleaning=new float[2];
        readonly RestaurantWorker[] cleaners=new RestaurantWorker[2];
        public int DirtyMask=>(dirty[0]?1:0)|(dirty[1]?2:0);
        public bool IsDirty(int index)=>index>=0&&index<2&&dirty[index];
        public static Vector3 UsePoint(int index)=>Seat(index)+Vector3.forward*.9f;
        public static Vector3 Seat(int index)=>RestroomFurniture.StallSeat(index);
        public static Vector3 CleanerPad=>RestroomFurniture.CleanerPad;
        Material cream,wall,white,dark,wood,woodDoor,yellow,red,steel,ink,green;
        Transform area;
        public void Configure(RestaurantWallet earnings,BurgerInventory player,SessionGoalTracker tracker)
        {
            Current=this;goals=tracker;this.player=player;wallet=earnings;
            cream=RuntimeMaterials.Create(HudChrome.Cream);wall=RuntimeMaterials.Create(new Color(.93f,.90f,.82f));
            white=RuntimeMaterials.Create(Color.white);dark=RuntimeMaterials.Create(new Color(.16f,.18f,.20f));
            wood=RuntimeMaterials.Create(RestroomFurniture.Wood);woodDoor=RuntimeMaterials.Create(RestroomFurniture.WoodDoor);
            yellow=RuntimeMaterials.Create(RestroomFurniture.LockerYellow);red=RuntimeMaterials.Create(RestaurantStyle.Red);
            steel=RuntimeMaterials.Create(RestaurantStyle.Steel);ink=RuntimeMaterials.Create(RestaurantStyle.Ink);
            green=RuntimeMaterials.Create(new Color(.28f,.78f,.36f));
            var owner=new GameObject("RestroomMaterials").AddComponent<BurgerVisual>();owner.transform.SetParent(transform,false);
            owner.OwnMaterials(cream,wall,white,dark,wood,woodDoor,yellow,red,steel,ink,green);
            var point=ShopFixtures.CreateActionCircle(transform,"RestroomPurchase",new Vector3(0,.02f,-12.5f),HudChrome.Green);
            var label=Label(transform,"RestroomPurchaseLabel",new Vector3(0,2,-12.5f),"RESTROOM");
            Pad=point.gameObject.AddComponent<FacilityUnlockZone>();Pad.Configure(wallet,player,point,Cost,"RESTROOM",Build,label);
            Pad.SetStoreOnly();
        }
        void Update()
        {
            Pad?.SetRankVisible(goals!=null&&goals.Allows(UnlockRank));
            AdvancePlayerCleaning(Time.deltaTime);
            AdvanceCleanBot(Time.deltaTime);
            for(int i=visitors.Count-1;i>=0;i--)if(visitors[i]==null||!visitors[i].gameObject.activeInHierarchy)Release(visitors[i]);
            for(int i=0;i<doors.Length;i++)if(doors[i]!=null)
            {
                bool inside=i<2&&occupants[i]!=null&&Vector3.Distance(occupants[i].transform.position,UsePoint(i))<.35f;
                doors[i].localRotation=Quaternion.RotateTowards(doors[i].localRotation,Quaternion.Euler(0,inside?0:RestroomFurniture.DoorAjarYaw[i],0),180*Time.deltaTime);
                if(i>1)continue;
                signs[i].text=dirty[i]?$"CLEAN {Mathf.CeilToInt(CleanSeconds-cleaning[i])}s":occupants[i]!=null?"BUSY":"FREE";
                signs[i].color=dirty[i]?HudChrome.Tomato:HudChrome.Ink;
                if(dirtyMarks[i]!=null)dirtyMarks[i].gameObject.SetActive(dirty[i]);
            }
        }
        public void AdvancePlayerCleaning(float seconds)
        {
            for(int i=0;i<2;i++)if(dirty[i]&&player!=null&&Vector3.Distance(player.transform.position,UsePoint(i)+Vector3.up)<1.4f)Clean(i,seconds);
        }
        public void AdvanceCleanBot(float seconds)
        {
            if(!Built||seconds<=0)return;
            if(CleanBotRemaining>0)
            {
                CleanBotRemaining=Mathf.Max(0,CleanBotRemaining-seconds);
                for(int i=0;i<2;i++)if(dirty[i])Clean(i,seconds);
                RefreshChip();
                return;
            }
            if(player==null){RefreshChip();return;}
            Vector3 offset=player.transform.position-CleanerPad;offset.y=0;
            if(offset.sqrMagnitude<=CleanerRadius*CleanerRadius)
            {
                cleanerDwell+=seconds;
                if(cleanerDwell>=FacilityUnlockZone.EntryDelay){TryHireCleanBot();cleanerDwell=0;}
            }
            else cleanerDwell=0;
            RefreshChip();
        }
        public bool TryHireCleanBot()
        {
            if(!Built||CleanBotRemaining>0||wallet==null)return false;
            if(!wallet.TrySpend(CleanBotCost))return false;
            CleanBotRemaining=CleanBotDuration;
            RefreshChip();
            return true;
        }
        public void Restore(bool built,int investment,int mask=0)
        {
            dirty[0]=(mask&1)!=0;dirty[1]=(mask&2)!=0;
            if(built){Build();Pad.RestorePurchased();}else Pad.RestoreInvestment(investment);
            if(dirtyMarks[0]!=null){dirtyMarks[0].gameObject.SetActive(dirty[0]);dirtyMarks[1].gameObject.SetActive(dirty[1]);}
        }
        public bool TryVisit(CustomerAgent customer)
        {
            if(!Built||customer==null||customer.TicketNumber%2!=0||visitors.Count>=Capacity)return false;
            if(visitors.Contains(customer))return true;
            var layout=Building.FacilityLayout.Current;
            if(layout!=null&&layout.Route(customer.transform.position,Door)==null)return false;
            visitors.Add(customer);return true;
        }
        public int Acquire(CustomerAgent customer)
        {
            if(!visitors.Contains(customer))return -1;
            for(int i=0;i<2;i++)if(occupants[i]==customer)return i;
            for(int i=0;i<2;i++)if(occupants[i]==null&&!dirty[i]){occupants[i]=customer;return i;}
            return -1;
        }
        public Vector3 WaitPoint(CustomerAgent customer)=>new Vector3(.35f,0,-16.55f-Mathf.Max(0,visitors.IndexOf(customer)-2)*.9f);
        public void Release(CustomerAgent customer)
        {visitors.Remove(customer);for(int i=0;i<2;i++)if(occupants[i]==customer)occupants[i]=null;}
        public void FinishUse(CustomerAgent customer,int index)
        {
            if(index<0||index>=2||occupants[index]!=customer)return;
            dirty[index]=true;cleaning[index]=0;occupants[index]=null;
            if(dirtyMarks[index]!=null)dirtyMarks[index].gameObject.SetActive(true);
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
            if(dirtyMarks[index]!=null)dirtyMarks[index].gameObject.SetActive(false);
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
                Part("RestroomWallHallLeft",new Vector3((bounds.min.x-1.5f)/2,.6f,-15),new Vector3(-1.5f-bounds.min.x,1.2f,.4f),wall);
                Part("RestroomWallHallRight",new Vector3((bounds.max.x+1.5f)/2,.6f,-15),new Vector3(bounds.max.x-1.5f,1.2f,.4f),wall);
            }
            Part("RestroomFloor",new Vector3(-.5f,-.12f,-18),new Vector3(9,.24f,6.4f),cream);
            Part("RestroomWallBack",new Vector3(-.5f,.7f,-21),new Vector3(9,1.4f,.25f),wall);
            Part("RestroomWallWest",new Vector3(-5,.7f,-18),new Vector3(.25f,1.4f,6),wall);
            Part("RestroomWallEast",new Vector3(4,.7f,-18),new Vector3(.25f,1.4f,6),wall);
            Part("RestroomDoorHeader",new Vector3(0,2.5f,-15),new Vector3(3.3f,.6f,.45f),wall);
            Part("RestroomEntryDoor",new Vector3(1.72f,.95f,-15.12f),new Vector3(.9f,1.9f,.08f),dark,PrimitiveType.Cube,false);
            cleanBotTimer=RestroomFurniture.Build(area,wood,woodDoor,cream,white,dark,yellow,red,steel,ink,green,doors);
            for(int i=0;i<2;i++)
            {
                signs[i]=Label(area,"CubicleStatus",Seat(i)+new Vector3(0,1.7f,1.05f),"FREE");
                dirtyMarks[i]=area.Find("Stall_"+i+"/StallDirtyMark");
                if(dirtyMarks[i]!=null)dirtyMarks[i].gameObject.SetActive(dirty[i]);
            }
            RefreshChip();
            Building.FacilityLayout.Current?.RefreshNavigation();
            GetComponent<RestaurantPersistence>()?.Flush();
        }
        void RefreshChip()
        {
            if(cleanBotTimer!=null)cleanBotTimer.text=CleanBotTimerCopy;
        }
        Transform Part(string name,Vector3 position,Vector3 scale,Material mat,PrimitiveType type=PrimitiveType.Cube,bool solid=true)
        {
            var part=GameObject.CreatePrimitive(type);part.name=name;part.transform.SetParent(area,false);part.transform.position=position;part.transform.localScale=scale;part.GetComponent<Renderer>().sharedMaterial=mat;
            SolidOccupancy.Apply(part.GetComponent<Collider>(),solid);
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
