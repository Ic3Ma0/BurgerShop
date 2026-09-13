using System;
using BurgerShop.Customer;
using BurgerShop.Economy;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using UnityEngine;

namespace BurgerShop.UI
{
    public sealed class SessionGoalTracker : MonoBehaviour
    {
        RestaurantWallet wallet;
        BurgerInventory inventory;
        DiningArea dining;
        ShopExpansion expansion;
        WorkerHiringZone hiring;
        [NonSerialized] int rank=ShopRanks.Min;
        int milestoneMask;
        bool legacyAccess;
        int incomeRemainder;
        float celebration;
        int appliedRank=-1;
        Transform sign;
        Material signMaterial;
        public event Action ProgressChanged;
        public int Rank=>rank;
        public int MilestoneMask=>milestoneMask;
        public bool LegacyAccess=>legacyAccess;
        public int IncomeRemainder=>incomeRemainder;
        public bool IsCycle=>rank>=ShopRanks.ContentEnd;
        public bool MilestoneComplete=>IsCycle||(milestoneMask & (1<<(rank-1)))!=0;
        public int GoalIndex=>MilestoneComplete?1:0;
        public int GoalProgress=>MilestoneComplete?1:0;
        public int Stars {get;private set;}
        public int StarCap=>ShopRanks.StarCap(rank);
        public bool IsMaxRank=>false;
        public int CycleCost=>ShopRanks.CycleCost(rank);
        public long MissingCoins=>Math.Max(0,(long)CycleCost-(wallet?.Coins??0));
        public bool CanUpgrade=>rank<int.MaxValue && (IsCycle?wallet!=null&&wallet.Coins>=CycleCost:MilestoneComplete&&Stars>=StarCap);
        public string Title=>IsCycle?"Grow cash income":ShopRanks.Goals(rank)[0].Title;
        public int Progress=>MilestoneComplete?1:0;
        public int Required=>1;
        public bool IsCelebrating=>celebration>0;
        public bool IsRankingUp {get;private set;}
        public string StarLabel=>CanUpgrade?$"Lv.{rank} Ready":IsCycle?$"Lv.{rank} · Need {MissingCoins:N0}":$"Lv.{rank}  {Stars}/{StarCap}";
        public float ProgressFraction=>IsCycle?(float)Math.Min(1,(wallet?.Coins??0)/(double)CycleCost):Mathf.Clamp01(Stars/(float)StarCap);
        public string BlockReason=>CanUpgrade?"Upgrade":IsCycle?$"Need {MissingCoins:N0} coins":!MilestoneComplete?"Complete milestone first":$"Need {StarCap-Stars} stars";

        public void Configure(BurgerInventory carrier,ProductionStation station,CounterStock stock,CustomerQueue customers,
            RestaurantWallet earnings,BurgerServingZone cashier,DiningArea hall=null,TrashInventory trashBag=null,
            WorkerHiringZone staff=null,BoostUpgradeZone playerBoost=null,ShopExpansion shop=null,
            BurgerServingZone colaCashier=null,ProductionStation cola=null)
        {
            inventory=carrier;wallet=earnings;dining=hall;hiring=staff;expansion=shop;
            ApplyUnlocks();
        }
        public void Restore(int nextRank,int nextGoalIndex,int nextGoalProgress,int savedStars=0,
            int savedMilestones=0,bool keepLegacyAccess=false,int savedRemainder=0)
        {
            rank=Math.Max(ShopRanks.Min,nextRank);Stars=Math.Max(0,savedStars);
            milestoneMask=savedMilestones & ShopRanks.MilestoneMask;
            legacyAccess=keepLegacyAccess;incomeRemainder=Mathf.Clamp(savedRemainder,0,ShopRanks.IncomeDenominator-1);
            celebration=0;IsRankingUp=false;appliedRank=-1;ApplyUnlocks();
        }
        public bool Allows(int requiredRank)=>legacyAccess||rank>=requiredRank;
        public void RecordMilestone(ShopGoalKind kind)
        {
            for(int stage=1;stage<ShopRanks.ContentEnd;stage++)
            {
                if(!Allows(stage)||ShopRanks.Goals(stage)[0].Kind!=kind)continue;
                int bit=1<<(stage-1);
                if((milestoneMask&bit)!=0)continue;
                milestoneMask|=bit;AddUpgradeStars();celebration=.6f;
                FeedbackDirector.Current?.World(inventory!=null?inventory.transform.position:transform.position,"Milestone +2 stars",.9f);
                GetComponent<BurgerShop.Persistence.RestaurantPersistence>()?.Flush();
            }
        }
        public void AddUpgradeStars()
        {
            Stars=(int)Math.Min(int.MaxValue,(long)Stars+2);ProgressChanged?.Invoke();
        }
        public bool TryUpgradeRank(int expectedRank)
        {
            if(rank!=expectedRank||!CanUpgrade)return false;
            if(IsCycle) {if(!wallet.TrySpend(CycleCost))return false;}
            else Stars-=StarCap;
            rank++;ApplyUnlocks();celebration=.9f;IsRankingUp=true;
            FeedbackDirector.Current?.Success(inventory!=null?inventory.transform.position:transform.position,"Rank Up!",inventory!=null?inventory.transform:null);
            ProgressChanged?.Invoke();GetComponent<BurgerShop.Persistence.RestaurantPersistence>()?.Flush();return true;
        }
        public int AddIncomeBonus(int amount)
        {
            if(amount<=0||rank<=ShopRanks.ContentEnd)return amount;
            long numerator=(long)amount*(rank-ShopRanks.ContentEnd)+incomeRemainder;
            long bonus=numerator/ShopRanks.IncomeDenominator;
            incomeRemainder=(int)(numerator%ShopRanks.IncomeDenominator);
            ProgressChanged?.Invoke();
            return (int)Math.Min(int.MaxValue,(long)amount+bonus);
        }
        void LateUpdate()=>Advance(Time.deltaTime);
        public void Advance(float deltaTime)
        {
            if(deltaTime<0)return;
            celebration=Mathf.Max(0,celebration-deltaTime);if(celebration==0)IsRankingUp=false;
            // Bootstrap finishes creating the optional lines after Configure.
            if(appliedRank!=rank)ApplyUnlocks();
        }
        public void ApplyUnlocks()
        {
            expansion?.ApplyRank(rank);
            if(dining!=null)
                foreach(var table in dining.Tables)if(table!=null)table.gameObject.SetActive(Allows(2));
            foreach(var zone in GetComponentsInChildren<TableUpgradeZone>(true))
                zone.SetRankVisible(Allows(2));
            var bin=GetComponentInChildren<TrashBin>(true);if(bin!=null)bin.gameObject.SetActive(Allows(2));
            var courier=GetComponent<CourierLine>();
            courier?.ApplyAccess(Allows(8),Allows(9));
            var bag=GetComponent<BagLine>();
            if(bag!=null&&Allows(10)&&!bag.Expanded)bag.TryExpand();
            appliedRank=courier!=null?rank:-1;
            RefreshSign();
        }
        void RefreshSign()
        {
            if(rank<ShopRanks.ContentEnd)return;
            if(sign==null)
            {
                sign=new GameObject("ShopRankSign").transform;sign.SetParent(transform,false);
                sign.position=new Vector3(0,3.6f,14.8f);
                var plate=GameObject.CreatePrimitive(PrimitiveType.Cube);plate.transform.SetParent(sign,false);
                plate.transform.localScale=new Vector3(4,.9f,.12f);
                var collider=plate.GetComponent<Collider>();collider.enabled=false;BurgerVisual.Release(collider);
                signMaterial=Core.RuntimeMaterials.Create(HudChrome.Gold);
                plate.GetComponent<Renderer>().sharedMaterial=signMaterial;plate.AddComponent<BurgerVisual>().OwnMaterials(signMaterial);
                var text=new GameObject("ShopRankCopy").AddComponent<TextMesh>();text.transform.SetParent(sign,false);
                text.transform.localPosition=new Vector3(0,0,-.08f);text.anchor=TextAnchor.MiddleCenter;
                text.characterSize=.12f;text.fontSize=48;text.color=Color.white;
            }
            signMaterial.color=Color.HSVToRGB(((rank-10)%12)/12f,.55f,.7f);
            sign.GetComponentInChildren<TextMesh>().text=$"SHOP {rank}\nCash +{2L*Math.Max(0,rank-10)}%";
        }
    }
}
