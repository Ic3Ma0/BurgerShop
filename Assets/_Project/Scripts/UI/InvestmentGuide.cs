using System;
using System.Collections.Generic;
using System.Linq;
using BurgerShop.Building;
using BurgerShop.Economy;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using UnityEngine;

namespace BurgerShop.UI
{
    // Read-only scene adapter. Existing offers remain the sole owners of prices and purchases.
    public sealed class InvestmentGuide
    {
        public sealed class Offer
        {
            public string Id, Title, Benefit;
            public int Cost;
            Vector3 target;
            public Component Actor;
            public Vector3 Target { get => Actor != null ? Actor.transform.position : target; set => target = value; }
            public FacilityInstance Facility;
            public InvestmentNeed Need;
            public string Line = "";
            public bool Prerequisite;
            public bool ShopPurchase;
            public bool AwardsStars = true;
        }
        readonly SessionGoalTracker goals;
        readonly RestaurantWallet wallet;
        string pinned;
        int pinnedCost;
        float refreshAt;
        readonly InvestmentPriority priority = new InvestmentPriority();
        bool manual;
        List<Offer> offers=new List<Offer>();
        Offer current;
        public InvestmentGuide(SessionGoalTracker tracker,RestaurantWallet earnings){goals=tracker;wallet=earnings;}
        public Offer Current {get{Refresh();return current;}}
        public string Title=>Current?.Title??"No valid star source · progression needs review";
        public string Detail
        {
            get
            {
                var offer=Current;
                if(offer==null)return "No eligible upgrade found; check land and staff access. This is not a sell-more task.";
                long missing=Math.Max(0,offer.Cost-(wallet?.Coins??0));
                string reason=priority.Persistent(offer.Need,offer.Line)?Reason(offer.Need)+"\n":"";
                return $"{reason}{offer.Benefit}\n{offer.Cost} coins"+(offer.AwardsStars?" · +2 stars":"")+$" · {goals.MissingStars} stars to upgrade"
                    +(missing>0?$"\nEarn {missing} more coins":offer.Actor != null ? "\nTap the character or this card to upgrade" : "\nTap to locate · other investments also earn stars");
            }
        }
        public void Next(){Refresh(true);if(offers.Count==0)return;int i=offers.FindIndex(o=>o.Id==pinned);Pin(offers[(i+1)%offers.Count]);manual=true;}
        static string Reason(InvestmentNeed need)=>need==InvestmentNeed.Production?"Production is short of demand":need==InvestmentNeed.Transport?"Stock is waiting for transport":need==InvestmentNeed.Service?"Stock ready · customers are waiting":"Clean seats are full";
        void Pin(Offer offer){current=offer;pinned=offer?.Id;pinnedCost=offer?.Cost??0;}
        public void Refresh(bool force=false)
        {
            if(!force&&Time.time<refreshAt&&current!=null)return;
            refreshAt=Time.time+.25f;
            InvestmentObservation.Read(goals,priority,Time.time);
            offers=ReadOffers();
            var retained=offers.Find(o=>o.Id==pinned&&o.Cost==pinnedCost);
            int Score(Offer o)=>priority.Priority(o.Need,o.Line,o.Prerequisite);
            var best=offers.OrderBy(Score).ThenBy(o=>o.Cost>(wallet?.Coins??0)).ThenBy(o=>o.Cost).ThenBy(o=>o.Id,StringComparer.Ordinal).FirstOrDefault();
            if(retained!=null&&(manual||best==null||Score(retained)<=Score(best))){current=retained;return;}
            manual=false;Pin(best);
        }
        List<Offer> ReadOffers()
        {
            var result=new List<Offer>();
            void Add(string id,string title,string benefit,int cost,Vector3 target,FacilityInstance facility=null,InvestmentNeed need=InvestmentNeed.None,string line="",bool prerequisite=false,bool shop=false,bool stars=true,Component actor=null)
            {if(cost>=0)result.Add(new Offer{Id=id,Title=title,Benefit=benefit,Cost=cost,Target=target,Facility=facility,Need=need,Line=line,Prerequisite=prerequisite,ShopPurchase=shop,AwardsStars=stars,Actor=actor});}
            foreach(var grill in goals.GetComponentsInChildren<GrillUpgradeZone>())
                if(grill.IsAvailable&&!grill.IsMaxLevel)
                {
                    var facility=grill.GetComponentInParent<FacilityInstance>();
                    string id=facility!=null?facility.Id:grill.ProductNoun+grill.UpgradePosition;
                    Add(id,"Upgrade "+(grill.Product==KitchenProduct.Burger?"burger machine":"cola machine"),
                        $"Production {grill.CurrentProductionSeconds:0.##}s → {grill.NextProductionSeconds:0.##}s",grill.NextCost,grill.UpgradePosition,facility,InvestmentNeed.Production,grill.Product.ToString());
                }
            var hall=goals.GetComponent<MainHallExpansion>();
            if(hall!=null&&!hall.Built&&goals.Rank>=MainHallExpansion.UnlockRank)
                Add("main-hall","Expand main hall",$"More workspace · staff access · land {hall.Remaining} + first staff {WorkerHiringZone.HireCosts[0]}",hall.Remaining,MainHallExpansion.PurchasePoint);
            var player=goals.GetComponent<BoostUpgradeZone>();
            if(player!=null)
            {
                if(!player.SpeedIsMax)Add("player-speed","Upgrade player speed",$"Speed {PlayerBoost.MoveSpeed(player.SpeedTier,player.CarryTier):0.00} → {PlayerBoost.MoveSpeed(player.SpeedTier+1,player.CarryTier):0.00}",player.SpeedCost,Vector3.zero,null,InvestmentNeed.Transport,TransportLine(),actor:player.Player);
                if(!player.CarryIsMax)Add("player-carry","Upgrade player carry",$"Carry {PlayerBoost.CarryCapacity(player.CarryTier)} → {PlayerBoost.CarryCapacity(player.CarryTier+1)}"+(PlayerBoost.IsEmptyCarryLevel(player.CarryTier+1)?" · +3% empty speed":""),player.CarryCost,Vector3.zero,null,InvestmentNeed.Transport,TransportLine(),actor:player.Player);
            }
            var staff=goals.GetComponent<StaffUpgradeBoard>();
            if(staff!=null&&staff.CanUpgradeStaff)
            {
                if(!staff.SpeedIsMax)Add("staff-speed","Upgrade staff speed",$"Speed {StaffBoost.WalkSpeed(staff.SpeedTier,staff.CarryTier):0.00} → {StaffBoost.WalkSpeed(staff.SpeedTier+1,staff.CarryTier):0.00}",staff.SpeedCost,Vector3.zero,null,InvestmentNeed.Transport,TransportLine(),actor:staff.FirstWorker);
                if(!staff.CarryIsMax)Add("staff-carry","Upgrade staff carry",$"Carry {StaffBoost.CarryCapacity(staff.CarryTier)} → {StaffBoost.CarryCapacity(staff.CarryTier+1)}"+(PlayerBoost.IsEmptyCarryLevel(staff.CarryTier+1)?" · +3% empty speed":""),staff.CarryCost,Vector3.zero,null,InvestmentNeed.Transport,TransportLine(),actor:staff.FirstWorker);
            }
            foreach(var table in goals.GetComponentsInChildren<TableUpgradeZone>(true))
                if(table.Table!=null&&table.Table.gameObject.activeInHierarchy&&!table.HasChosenSet)
                    foreach(var id in TableSetCatalog.Choices)
                    {
                        var set=TableSetCatalog.Get(id);
                        var facility=table.GetComponentInParent<FacilityInstance>();
                        Add($"table-{facility?.Id??table.SlotIndex.ToString()}-{(int)id}","Upgrade table · "+set.Name,
                            $"Meal pay {TableSetCatalog.StarterPay} → {set.MealPay} · eat {TableSetCatalog.StarterEatSeconds:0.#}s → {set.EatSeconds:0.#}s",
                            TableSetCatalog.Due(id,table.Invested),table.PadPosition,facility);
                    }
            var growth=goals.GetComponent<GrowthUpgrades>();
            if(growth!=null)foreach(var offer in growth.Offers)
            {
                int level=growth.Level(offer.Id);
                if(offer.Target==null||!offer.Target.gameObject.activeInHierarchy||level<1||level>offer.Costs.Length)continue;
                var stock=offer.Target.GetComponentInParent<CounterStock>()??offer.Target.GetComponentInChildren<CounterStock>();
                Add(offer.Id,"Upgrade "+offer.Title,$"{offer.Benefit(level)} → {offer.Benefit(level+1)}",offer.Costs[level-1],offer.Position,offer.Target.GetComponentInParent<FacilityInstance>(),stock!=null?InvestmentNeed.Service:InvestmentNeed.None,stock!=null?stock.Product.ToString():"");
            }
            var hiring=goals.GetComponent<WorkerHiringZone>();
            if(hiring!=null&&hiring.IsAvailable&&!hiring.IsFull)
                Add("hire-staff","Hire staff","Automate supply, serving and cleaning",hiring.HireCost,hiring.HiringPosition,null,InvestmentNeed.Transport,TransportLine(),hiring.HiredCount==0,stars:false);
            var layout=goals.GetComponent<FacilityLayout>();
            if(layout!=null)
            {
                var opening=new BusinessOpeningQuote(layout);
                foreach(var offer in FacilityCatalog.Offers)
                {
                    if(!layout.Unlocked(offer.Kind))continue;
                    bool cola=offer.Kind==FacilityKind.ColaMachine||offer.Kind==FacilityKind.ColaCounter;
                    bool bag=offer.Kind==FacilityKind.BagMachine||offer.Kind==FacilityKind.BagTable||offer.Kind==FacilityKind.BagCounter;
                    bool box=offer.Kind==FacilityKind.BlueBoxTable;
                    bool seat=offer.Kind==FacilityKind.PairTable||offer.Kind==FacilityKind.FourSeatTable||offer.Kind==FacilityKind.SquareTable;
                    bool production=offer.Kind==FacilityKind.BurgerMachine||offer.Kind==FacilityKind.ColaMachine;
                    bool missing=(cola||bag||box)&&layout.Owned(offer.Kind)==0;
                    if(!missing&&!seat&&!production)continue;
                    Add("buy-"+offer.Kind,"Buy "+offer.Name,cola?opening.ColaCopy:bag?opening.BagCopy:box?opening.BlueCopy:seat?"Add dining seats":"Add a production station",
                        layout.Price(offer.Kind),ShopLayout.BoostPoint,null,seat?InvestmentNeed.Seats:production?InvestmentNeed.Production:InvestmentNeed.None,
                        seat?"dining":cola?KitchenProduct.Cola.ToString():KitchenProduct.Burger.ToString(),missing,true);
                }
            }
            return result;
        }
        string TransportLine()=>priority.Persistent(InvestmentNeed.Transport,KitchenProduct.Cola.ToString())?KitchenProduct.Cola.ToString():KitchenProduct.Burger.ToString();
    }
}
