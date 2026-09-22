using BurgerShop.Building;
using BurgerShop.Core;
using BurgerShop.Economy;
using BurgerShop.Persistence;
using BurgerShop.UI;
using UnityEngine;

namespace BurgerShop.Restaurant
{
    // One owner for the first land purchase. Geometry/UI consume this state; restore never awards stars.
    public sealed class MainHallExpansion : MonoBehaviour
    {
        public const int Cost=150, UnlockRank=3;
        public static readonly Rect StarterBounds=Rect.MinMaxRect(-15,-15,5,0);
        public static readonly Rect PreviousStarterBounds=Rect.MinMaxRect(-15,-15,8,5);
        public static readonly Rect FullBounds=Rect.MinMaxRect(-15,-15,15,15);
        public static Vector3 PurchasePoint=>ShopLayout.SmallFootprint?new Vector3(-5,0,-1.3f):new Vector3(2,0,3.7f);
        public static MainHallExpansion Current {get;private set;}
        public bool CompactStart {get;private set;}
        public bool SmallFootprint {get;private set;}
        public bool Built {get;private set;}
        public int Invested {get;private set;}
        public int Remaining=>Built?0:Cost-Invested;
        public Rect Bounds=>Built?FullBounds:SmallFootprint?StarterBounds:PreviousStarterBounds;
        public Rect UsableBounds=>Rect.MinMaxRect(Bounds.xMin+.2f,Bounds.yMin+.2f,Bounds.xMax-.2f,Bounds.yMax-.2f);
        Transform barriers;
        TextMesh sign;
        public static bool HasAccess=>Current==null||Current.Built;
        public void Initialize(bool compact,bool built,int invested,bool small=true)
        {
            Current=this;SmallFootprint=compact&&small;ShopLayout.SmallFootprint=SmallFootprint;CompactStart=compact;ShopLayout.CompactStart=compact;Built=built||!compact;Invested=Built?Cost:Mathf.Clamp(invested,0,Cost);
            RefreshGeometry();
        }
        void OnDestroy(){if(Current==this){Current=null;ShopLayout.SmallFootprint=false;}}
        public void Restore(bool compact,bool built,int invested,bool small=true)=>Initialize(compact,built,invested,small);
        public bool TryContribute()
        {
            var goals=GetComponent<SessionGoalTracker>();var wallet=GetComponent<RestaurantWallet>();
            if(Built||goals==null||goals.Rank<UnlockRank||wallet==null||wallet.Coins<=0)return false;
            int amount=(int)System.Math.Min(Remaining,wallet.Coins);
            if(!wallet.TrySpend(amount,()=>{
                Invested+=amount;
                if(Invested==Cost){Built=true;goals.AddUpgradeStars();}
            }))return false;
            RefreshGeometry();goals.ApplyUnlocks();
            GetComponent<FacilityLayout>()?.RefreshNavigation();
            GetComponent<RestaurantPersistence>()?.Flush();
            return true;
        }
        void Update()
        {
            if(sign==null)return;
            var goals=GetComponent<SessionGoalTracker>();
            sign.text=goals==null||goals.Rank<UnlockRank?"3级可扩建":$"扩建 {Remaining} 金币 · +2 星";
        }
        void RefreshGeometry()
        {
            bool hadSmallWalls=barriers!=null;
            if(barriers!=null){barriers.gameObject.SetActive(false);BurgerVisual.Release(barriers.gameObject);}
            // The building ends at the owned land, not at the final master plan.
            if(!Built||hadSmallWalls)
                foreach(Transform child in transform)
                    if(child.name.StartsWith("Wall")||child.name=="EntranceWallLeft"||child.name=="WingDoorPlug"||child.name=="HrDoorPlug"||child.name=="BoostDoorPlug")child.gameObject.SetActive(Built);
            barriers=null;sign=null;
            var floor=transform.Find("Floor");
            if(floor!=null)
            {
                floor.position=new Vector3(Bounds.center.x,-.15f,Bounds.center.y);
                floor.localScale=new Vector3(Bounds.width,.3f,Bounds.height);
                GetComponent<RestaurantArchitecture>()?.RedressMainFloor(floor);
            }
            if(Built)return;
            barriers=new GameObject("MainHallConstruction").transform;barriers.SetParent(transform,false);
            var material=RuntimeMaterials.Create(new Color(.7f,.56f,.35f));
            barriers.gameObject.AddComponent<BurgerVisual>().OwnMaterials(material);
            var b=Bounds;
            Wall("WallSmallNorth",new Vector3(b.center.x,.6f,b.yMax),new Vector3(b.width,1.2f,.4f),material,true);
            Wall("WallSmallEast",new Vector3(b.xMax,.6f,b.center.y),new Vector3(.4f,1.2f,b.height),material,true);
            Wall("WallSmallWest",new Vector3(b.xMin,.6f,b.center.y),new Vector3(.4f,1.2f,b.height),material,true);
            Wall("WallSmallEntranceLeft",new Vector3(-14.5f,.6f,-15),new Vector3(1,1.2f,.4f),material,true);
            Wall("WallSmallSouth",new Vector3((-10+b.xMax)*.5f,.6f,-15),new Vector3(b.xMax+10,1.2f,.4f),material,true);
            // Reserved lots remain protected; never remove the continuous safety ground.
            Wall("NorthReserved",new Vector3(0,.5f,(b.yMax+15)*.5f),new Vector3(30,1,15-b.yMax),material,false);
            Wall("EastReserved",new Vector3((b.xMax+15)*.5f,.5f,b.center.y),new Vector3(15-b.xMax,1,b.height),material,false);
            var label=new GameObject("MainHallSign");label.transform.SetParent(barriers,false);label.transform.position=PurchasePoint+Vector3.up*1.6f;
            sign=label.AddComponent<TextMesh>();sign.font=HudChrome.ChineseFont();label.GetComponent<MeshRenderer>().sharedMaterial=sign.font.material;sign.fontSize=40;sign.characterSize=.065f;sign.anchor=TextAnchor.MiddleCenter;sign.color=new Color(.3f,.23f,.14f);label.transform.rotation=Quaternion.Euler(45,45,0);
            Update();
        }
        void Wall(string name,Vector3 pos,Vector3 size,Material material,bool visible)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(barriers,false);go.transform.position=pos;go.transform.localScale=size;
            go.GetComponent<Renderer>().sharedMaterial=material;go.GetComponent<Renderer>().enabled=visible;SolidOccupancy.Apply(go.GetComponent<Collider>(),true);
        }
    }
}
