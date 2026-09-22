using System;
using BurgerShop.Customer;
using BurgerShop.Economy;
using BurgerShop.Persistence;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using UnityEngine;

namespace BurgerShop.UI
{
    public sealed class SessionGoalTracker : MonoBehaviour
    {
        RestaurantWallet wallet;
        CounterStock stockForOpening; ProductionStation stationForOpening; BurgerServingZone cashierForOpening;
        BurgerInventory inventory;
        DiningArea dining;
        ShopExpansion expansion;
        WorkerHiringZone hiring;
        [NonSerialized] int rank=ShopRanks.Min;
        int milestoneMask;
        bool legacyAccess;
        public bool FirstOrderComplete { get; private set; }
        public InvestmentGuide Investments { get; private set; }
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
        public bool IsCycle=>ShopRanks.IsCycleRank(rank);
        public bool MilestoneComplete=>rank==1?FirstOrderComplete:IsCycle||(milestoneMask & (1<<(rank-1)))!=0;
        public int GoalIndex=>MilestoneComplete?1:0;
        public int GoalProgress=>MilestoneComplete?1:0;
        public int Stars {get;private set;}
        public int StarCap=>ShopRanks.StarCap(rank);
        public int MissingStars=>ShopRanks.MissingStars(Stars,rank);
        public bool IsMaxRank=>false;
        public int CycleCost=>ShopRanks.CycleCost(rank);
        public long MissingCoins=>Math.Max(0,(long)CycleCost-(wallet?.Coins??0));
        public bool CanUpgrade=>rank<int.MaxValue && (IsCycle?wallet!=null&&wallet.Coins>=CycleCost:Stars>=StarCap&&(rank!=1||FirstOrderComplete));
        public string Title
        {
            get
            {
                if(IsCycle)return "Grow cash income";
                ShopGoal[] listed=ShopRanks.Goals(rank);
                return listed!=null&&listed.Length>0?listed[0].Title:ShopRanks.NextUnlock(rank);
            }
        }
        public int Progress=>MilestoneComplete?1:0;
        public int Required=>1;
        public bool IsCelebrating=>celebration>0;
        public bool IsRankingUp {get;private set;}
        public string StarLabel=>CanUpgrade?$"Lv.{rank} Ready":IsCycle?$"Lv.{rank} · Need {MissingCoins:N0}":$"⭐ {Stars}/{StarCap}  Need {MissingStars} more stars";
        public float ProgressFraction=>IsCycle?(float)Math.Min(1,(wallet?.Coins??0)/(double)Math.Max(1,CycleCost)):Mathf.Clamp01(Stars/(float)Math.Max(1,StarCap));
        public string BlockReason=>CanUpgrade?"Upgrade":IsCycle?$"Need {MissingCoins:N0} coins":$"Need {MissingStars} more stars";
        public string NextRankPreview=>MainHallExpansion.HasAccess?ShopRanks.NextRankPreview(rank,Stars,MissingCoins).Replace("Expand main hall & hire staff","Hire staff"):ShopRanks.NextRankPreview(rank,Stars,MissingCoins);
        public string GuideCopy=>CanUpgrade?(rank==ShopRanks.Min?"Unlock dining":"Upgrade"):rank==ShopRanks.Min&&!FirstOrderComplete?"Sell your first burger":"";
        public OpeningGuide Opening {get;private set;}
        public string LoopCopy
        {
            get
            {
                if(dining!=null&&dining.HasTrashOnTables)return "Clear a used dining table";
                if(inventory!=null&&inventory.Count>0)return "Serve a waiting customer";
                return ShopRanks.LoopHintCopy;
            }
        }
        bool CurrentTaskVisible
        {
            get
            {
                if(IsCycle||MilestoneComplete)return false;
                ShopGoal[] listed=ShopRanks.Goals(rank);
                if(listed==null||listed.Length==0)return false;
                return CurrentTaskReachable(listed[0].Kind);
            }
        }
        public bool ShowsInvestment => !CanUpgrade && rank>=2 && !IsCycle && !CurrentTaskVisible && TeachingPrerequisite==null;
        public string TeachingPrerequisite
        {
            get
            {
                var hall=GetComponent<MainHallExpansion>();
                if(rank>=3&&hall!=null&&!hall.Built)return $"Expand main hall · {hall.Remaining} coins · then hire staff {WorkerHiringZone.HireCosts[0]}";
                if(rank==3&&(hiring?.HiredCount??0)==0)return $"Hire your first employee · {hiring?.HireCost??50} coins";
                if(rank==4&&expansion!=null&&!expansion.HasExtraGrill)return $"Second burger machine · {GetComponent<Building.FacilityLayout>()?.Price(Building.FacilityKind.BurgerMachine)??ShopExpansion.GrillCost} coins · supermarket";
                if(rank==5&&expansion!=null&&!expansion.HasBoxing)return $"Blue-box packing · {GetComponent<Building.FacilityLayout>()?.Price(Building.FacilityKind.BlueBoxTable)??ShopExpansion.BoxingCost} coins · supermarket";
                if(rank==6&&expansion!=null&&!expansion.HasBoxing)return "Car orders need blue boxes · build packing";
                var layout=GetComponent<Building.FacilityLayout>();
                if(rank==7&&layout!=null)
                {
                    var quote=new Economy.BusinessOpeningQuote(layout);
                    if(quote.Cola>0)return quote.ColaCopy;
                }
                return null;
            }
        }
        public string CapsuleTitle=>Opening!=null&&Opening.IsActive?Opening.Title
            :CanUpgrade?ShopRanks.RankUpCapsule(rank)
            :TeachingPrerequisite!=null?TeachingPrerequisite
            :CurrentTaskVisible||IsCycle?Title
            :Investments?.Title??"Choose an investment";
        public int CapsuleProgress=>Opening!=null&&Opening.IsActive?Opening.Progress:CanUpgrade?1:ShowsInvestment?Stars:Progress;
        public int CapsuleRequired=>Opening!=null&&Opening.IsActive?Opening.Required:CanUpgrade?1:ShowsInvestment?StarCap:Required;

        public void Configure(BurgerInventory carrier,ProductionStation station,CounterStock stock,CustomerQueue customers,
            RestaurantWallet earnings,BurgerServingZone cashier,DiningArea hall=null,TrashInventory trashBag=null,
            WorkerHiringZone staff=null,BoostUpgradeZone playerBoost=null,ShopExpansion shop=null,
            BurgerServingZone colaCashier=null,ProductionStation cola=null)
        {
            inventory=carrier;wallet=earnings;dining=hall;hiring=staff;expansion=shop;
            stockForOpening=stock;stationForOpening=station;cashierForOpening=cashier;
            Investments=new InvestmentGuide(this,earnings);
            if(Opening==null)Opening=new OpeningGuide();
            Opening.Bind(wallet,MainGrill(),this,inventory,stockForOpening,stationForOpening,cashierForOpening);
            ApplyUnlocks();
        }

        GrillUpgradeZone MainGrill()
        {
            GrillUpgradeZone best=GetComponent<GrillUpgradeZone>();
            float bestD=best!=null?0f:float.MaxValue;
            foreach(var zone in GetComponentsInChildren<GrillUpgradeZone>(true))
            {
                if(zone.Product!=KitchenProduct.Burger)continue;
                float d=ShopLayout.Horizontal(zone.UpgradePosition,ShopLayout.UpgradeSpot);
                if(best==null||d<bestD){best=zone;bestD=d;}
            }
            return best;
        }
        public void Restore(int nextRank,int nextGoalIndex,int nextGoalProgress,int savedStars=0,
            int savedMilestones=0,bool keepLegacyAccess=false,int savedRemainder=0,bool firstOrder=false)
        {
            rank=Math.Max(ShopRanks.Min,nextRank);Stars=Math.Max(0,savedStars);FirstOrderComplete=firstOrder;
            milestoneMask=savedMilestones & ShopRanks.MilestoneMask;
            legacyAccess=keepLegacyAccess;incomeRemainder=Mathf.Clamp(savedRemainder,0,ShopRanks.IncomeDenominator-1);
            celebration=0;IsRankingUp=false;appliedRank=-1;ApplyUnlocks();
        }
        public bool Allows(int requiredRank)=>(requiredRank<3||MainHallExpansion.HasAccess)&&(legacyAccess||rank>=requiredRank);
        public void RecordMilestone(ShopGoalKind kind)
        {
            var persistence=GetComponentInParent<RestaurantPersistence>();
            if(persistence!=null&&persistence.Phase!=SaveSessionPhase.Running)return;
            if(kind==ShopGoalKind.ServeCustomers)
            {
                if(FirstOrderComplete)return;
                FirstOrderComplete=true;
                if(rank==1){AddUpgradeStars();celebration=.6f;ProgressChanged?.Invoke();FlushSave();}
                return;
            }
            for(int stage=1;stage<=ShopRanks.StarGateEnd;stage++)
            {
                ShopGoal[] listed=ShopRanks.Goals(stage);
                if(listed==null||listed.Length==0||listed[0].Kind!=kind||!Allows(stage))continue;
                if(ShopRanks.IsThresholdGoal(kind)&&!ThresholdMet(kind))continue;
                GrantMilestone(stage);
            }
        }
        public void EvaluateStarGateTasks()
        {
            for(int stage=1;stage<=ShopRanks.StarGateEnd;stage++)
            {
                ShopGoal[] listed=ShopRanks.Goals(stage);
                if(listed==null||listed.Length==0||!ShopRanks.IsThresholdGoal(listed[0].Kind))continue;
                if(!Allows(stage)||!ThresholdMet(listed[0].Kind))continue;
                GrantMilestone(stage);
            }
        }
        void GrantMilestone(int stage)
        {
            int bit=1<<(stage-1);
            if((milestoneMask&bit)!=0)return;
            milestoneMask|=bit;AddUpgradeStars();celebration=.6f;
            var save=GetComponentInParent<RestaurantPersistence>();
            if(save==null||save.Phase==SaveSessionPhase.Running)
                FeedbackDirector.Current?.World(inventory!=null?inventory.transform.position:transform.position,"完成目标 +2 星",.9f);
            ProgressChanged?.Invoke();
            FlushSave();
        }
        bool ThresholdMet(ShopGoalKind kind)
        {
            var boost=GetComponentInChildren<BoostUpgradeZone>(true);
            var staff=GetComponentInChildren<StaffUpgradeBoard>(true);
            var growth=GetComponentInChildren<GrowthUpgrades>(true);
            return ShopRanks.MeetsThreshold(kind,
                boost!=null?boost.SpeedTier:0,boost!=null?boost.CarryTier:0,
                staff!=null?staff.SpeedTier:0,staff!=null?staff.CarryTier:0,
                growth!=null?growth.HighestTryBuyLevel:1,
                ChosenTableSets());
        }
        int ChosenTableSets()
        {
            int chosen=0;
            foreach(var zone in GetComponentsInChildren<TableUpgradeZone>(true))
                if(zone!=null&&zone.SetId!=TableSetId.Starter)chosen++;
            return chosen;
        }
        int UnlockedTableZones()
        {
            int count=0;
            foreach(var zone in GetComponentsInChildren<TableUpgradeZone>(true))
                if(zone!=null&&zone.Table!=null&&zone.Table.gameObject.activeInHierarchy)count++;
            return count;
        }
        bool CurrentTaskReachable(ShopGoalKind kind)=>
            kind!=ShopGoalKind.AllTablesChosen||UnlockedTableZones()>=ShopRanks.RequiredTableSets;
        public void NotifyCleanTable()
        {
            bool already=(milestoneMask&2)!=0;
            RecordMilestone(ShopGoalKind.CleanTable);
            if(!already&&(milestoneMask&2)!=0)
                FeedbackDirector.Current?.World(inventory!=null?inventory.transform.position:transform.position,$"清桌 +2 星\n还差 {MissingStars} 星可升级",2f);
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
            var save=GetComponentInParent<RestaurantPersistence>();
            if(save==null||save.Phase==SaveSessionPhase.Running)
                FeedbackDirector.Current?.Success(inventory!=null?inventory.transform.position:transform.position,"店铺升级！",inventory!=null?inventory.transform:null);
            ProgressChanged?.Invoke();FlushSave();return true;
        }

        void FlushSave() => GetComponentInParent<RestaurantPersistence>()?.Flush();
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
            EvaluateStarGateTasks();
        }
        public void ApplyUnlocks()
        {
            expansion?.ApplyRank(MainHallExpansion.HasAccess?rank:Math.Min(rank,2));
            if(dining!=null)
                foreach(var table in dining.Tables)if(table!=null)table.gameObject.SetActive(Allows(ShopRanks.DiningRank));
            foreach(var zone in GetComponentsInChildren<TableUpgradeZone>(true))
                zone.SetRankVisible(Allows(ShopRanks.DiningRank));
            var bin=GetComponentInChildren<TrashBin>(true);if(bin!=null)bin.gameObject.SetActive(Allows(ShopRanks.DiningRank));
            var courier=GetComponent<CourierLine>();
            courier?.ApplyAccess(Allows(ShopRanks.CourierRank),Allows(ShopRanks.AutomationRank));
            GetComponentInChildren<HrOffice>(true)?.SetOpen(Allows(ShopRanks.HireRank));
            var bay=GetComponentInChildren<BoostRoom>(true);
            bay?.SetAnnexOpen(Allows(ShopRanks.BoxingRank)||(expansion!=null&&(expansion.HasBoxing||expansion.HasDriveThru)));
            expansion?.EnsureDriveThruOpen();
            var bag=GetComponent<BagLine>();
            if(bag!=null&&Allows(ShopRanks.WestRank)&&!bag.Expanded)bag.TryExpand();
            GetComponent<BurgerShop.Core.RestaurantArchitecture>()?.Refresh();
            appliedRank=courier!=null?rank:-1;
            if(Opening==null)Opening=new OpeningGuide();
            Opening.Bind(wallet,MainGrill(),this,inventory,stockForOpening,stationForOpening,cashierForOpening);
            ReconcileStarterUpgrade();
            EvaluateStarGateTasks();
            RefreshSign();
        }
        void ReconcileStarterUpgrade()
        {
            if(rank!=ShopRanks.Min||MilestoneComplete)return;
            if(ShopRanks.Goals(rank)[0].Kind!=ShopGoalKind.UpgradeGrill)return;
            foreach(var zone in GetComponentsInChildren<GrillUpgradeZone>(true))
            {
                if(zone==null||zone.Product!=KitchenProduct.Burger||zone.Level<=1)continue;
                RecordMilestone(ShopGoalKind.UpgradeGrill);
                return;
            }
        }
        void RefreshSign()
        {
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
                Transform row=new GameObject("SignStars").transform;row.SetParent(sign,false);
                row.localPosition=new Vector3(0,.7f,-.08f);
                for(int i=0;i<8;i++)
                {
                    var star=GameObject.CreatePrimitive(PrimitiveType.Cube);star.name="SignStar"+i;
                    star.transform.SetParent(row,false);
                    star.transform.localScale=new Vector3(.18f,.18f,.06f);
                    star.transform.localPosition=new Vector3((i-3.5f)*.32f,0,0);
                    var starCol=star.GetComponent<Collider>();starCol.enabled=false;BurgerVisual.Release(starCol);
                    var starMat=Core.RuntimeMaterials.Create(HudChrome.Gold);
                    star.GetComponent<Renderer>().sharedMaterial=starMat;star.AddComponent<BurgerVisual>().OwnMaterials(starMat);
                }
            }
            var bounds=MainHallExpansion.Current?.Bounds??MainHallExpansion.FullBounds;
            // Keep the sign attached to the current building, including before the first expansion.
            sign.position=new Vector3(bounds.center.x,1.55f,bounds.yMax-.24f);
            float t=Mathf.Clamp01((rank-1)/14f);
            sign.localScale=Vector3.one*(.72f+.28f*t);
            signMaterial.color=Color.HSVToRGB(((rank-1)%12)/12f,.4f+.25f*t,.42f+.38f*t);
            Transform stars=sign.Find("SignStars");
            int lit=Mathf.Clamp(rank,1,8);
            if(stars!=null)for(int i=0;i<stars.childCount;i++)stars.GetChild(i).gameObject.SetActive(i<lit);
            sign.GetComponentInChildren<TextMesh>().text=rank>ShopRanks.ContentEnd
                ?$"SHOP {rank}\nCash +{2L*(rank-ShopRanks.ContentEnd)}%"
                :$"SHOP {rank}";
        }
    }
}
