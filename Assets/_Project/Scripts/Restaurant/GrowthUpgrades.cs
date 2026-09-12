using System;
using System.Collections.Generic;
using BurgerShop.Economy;
using BurgerShop.Player;
using BurgerShop.UI;
using UnityEngine;

namespace BurgerShop.Restaurant
{
    [Serializable] public sealed class FacilityLevelRecord { public string id; public int level=1; }

    public sealed class GrowthUpgrades : MonoBehaviour
    {
        public sealed class Offer
        {
            public string Id,Title;
            public Transform Target;
            public Vector3 Position;
            public int[] Costs;
            public Func<int,string> Benefit;
            public Action<int> Apply;
        }
        readonly Dictionary<string,int> levels=new Dictionary<string,int>();
        readonly Dictionary<string,Offer> offers=new Dictionary<string,Offer>();
        RestaurantWallet wallet;
        SessionGoalTracker goals;
        ShopExpansion expansion;
        public BurgerInventory Player { get; private set; }
        public IEnumerable<Offer> Offers=>offers.Values;
        public int Level(string id)=>levels.TryGetValue(id,out int n)?n:1;
        public static bool ValidId(string id)=>id=="cola-machine"||id=="grill-main"||id=="grill-extra"||id=="table-0"||id=="table-1"||id=="table-2"||id=="table-extra"||id=="counter-main"||id=="counter-extra"||id=="boxing"||id=="bag-machine"||id=="bag-table"||id=="bag-counter";
        public void Configure(RestaurantWallet earnings,SessionGoalTracker tracker,BurgerInventory player,ShopExpansion shop)
        {
            wallet=earnings;goals=tracker;Player=player;expansion=shop;
            if(expansion!=null)expansion.PurchaseCompleted+=Discover;
            Discover();
        }
        public void Register(Offer offer)
        {
            if(offers.ContainsKey(offer.Id))return;
            offers.Add(offer.Id,offer);
            ShopFixtures.CreateActionCircle(offer.Target,"Upgrade_"+offer.Id,offer.Position,HudChrome.Gold);
            offer.Apply(Level(offer.Id));
        }
        public void Discover()
        {
            foreach(var table in GetComponentsInChildren<DiningTable>())
            {
                string id=null;
                for(int i=0;i<ShopLayout.Tables.Length;i++)if(ShopLayout.Horizontal(table.Center,ShopLayout.Tables[i])<.1f)id="table-"+i;
                if(ShopLayout.Horizontal(table.Center,ShopLayout.ExtraTable)<.1f)id="table-extra";
                if(id==null)continue;
                Register(new Offer{Id=id,Title="Table & chairs",Target=table.transform,Position=table.Center+new Vector3(1.6f,.02f,0),Costs=new[]{150,300,600},Benefit=n=>$"Style {n} · Tip {10+(n-1)*5}",Apply=table.SetFurnitureLevel});
            }
            foreach(var stock in GetComponentsInChildren<CounterStock>())
            {
                bool main=ShopLayout.Horizontal(stock.CounterPosition,ShopLayout.CounterTop)<.2f;
                bool extra=ShopLayout.Horizontal(stock.CounterPosition,ShopLayout.ExtraCounterTop)<.2f;
                if(!main&&!extra)continue;
                string counterId=main?"counter-main":"counter-extra";
                if(offers.ContainsKey(counterId))continue;
                var visual=new GameObject(counterId+"Upgrades").transform;visual.SetParent(stock.transform,false);visual.position=stock.CounterPosition;
                Register(new Offer{Id=main?"counter-main":"counter-extra",Title="Dining counter",Target=visual,Position=stock.CounterPosition+new Vector3(-1.8f,-stock.CounterPosition.y+.02f,0),Costs=new[]{100,200},Benefit=n=>$"Item every {(.6f-.05f*(n-1)):0.00}s",Apply=n=>{stock.ServiceLevel=n;ShowDetail(visual,n);}});
            }
            if(expansion!=null&&expansion.Boxing!=null)
            {
                var b=expansion.Boxing;
                Register(new Offer{Id="boxing",Title="Boxing table",Target=b.transform,Position=ShopLayout.BoxingTable+new Vector3(2.5f,.02f,0),Costs=new[]{120,240},Benefit=n=>$"Packing {(.35f-.05f*(n-1)):0.00}s",Apply=n=>{b.WorkLevel=n;ShowDetail(b.transform,n);}});
            }
        }
        public bool TryBuy(string id,int expectedLevel)
        {
            if(!offers.TryGetValue(id,out var offer)||offer.Target==null||!offer.Target.gameObject.activeInHierarchy)return false;
            int level=Level(id);
            if(level!=expectedLevel||level>offer.Costs.Length||wallet==null||goals==null)return false;
            if(!wallet.TrySpend(offer.Costs[level-1]))return false;
            levels[id]=level+1;offer.Apply(level+1);goals.AddUpgradeStars();
            FeedbackDirector.Current?.Success(offer.Target.position,"Level Up!",Player!=null?Player.transform:null);
            VisualMeshPulse.Play(offer.Target);
            GetComponent<Persistence.RestaurantPersistence>()?.Flush();
            return true;
        }
        public void RecordGrill(GrillUpgradeZone grill)
        {
            string id=ShopLayout.Horizontal(grill.UpgradePosition,ShopLayout.ColaUpgrade)<1?"cola-machine":ShopLayout.Horizontal(grill.UpgradePosition,ShopLayout.ExtraGrillUpgrade)<1?"grill-extra":"grill-main";
            if(grill.Level<=Level(id))return;
            levels[id]=grill.Level;goals?.AddUpgradeStars();
            GetComponent<Persistence.RestaurantPersistence>()?.Flush();
        }
        public void Restore(FacilityLevelRecord[] records,int mainGrill,int extraGrill,int colaLevel=1)
        {
            levels.Clear();
            if(records!=null)foreach(var row in records)levels[row.id]=row.level;
            levels["cola-machine"]=colaLevel;levels["grill-main"]=mainGrill;levels["grill-extra"]=Mathf.Max(1,extraGrill);
            Discover();foreach(var offer in offers.Values)offer.Apply(Level(offer.Id));
        }
        public FacilityLevelRecord[] Capture()
        {
            var keys=new List<string>(levels.Keys);keys.Sort(StringComparer.Ordinal);
            var result=new List<FacilityLevelRecord>();foreach(string key in keys)result.Add(new FacilityLevelRecord{id=key,level=levels[key]});return result.ToArray();
        }
        static void ShowDetail(Transform target,int level)
        {
            var old=target.Find("UpgradeDetail");if(old!=null){old.gameObject.SetActive(false);BurgerVisual.Release(old.gameObject);}
            if(level<=1)return;
            var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name="UpgradeDetail";obj.transform.SetParent(target,false);
            obj.transform.localPosition=new Vector3(0,.8f,.25f);obj.transform.localScale=new Vector3(.8f,.12f*(level-1),.12f);
            var mat=Core.RuntimeMaterials.Create(HudChrome.Gold);obj.GetComponent<Renderer>().sharedMaterial=mat;obj.AddComponent<BurgerVisual>().OwnMaterials(mat);
            var c=obj.GetComponent<Collider>();c.enabled=false;BurgerVisual.Release(c);
        }
        void OnDestroy(){if(expansion!=null)expansion.PurchaseCompleted-=Discover;}
    }
}
