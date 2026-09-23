using BurgerShop.Building;
using BurgerShop.Customer;
using BurgerShop.Economy;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using BurgerShop.UI;
using NUnit.Framework;
using UnityEngine;

namespace BurgerShop.Tests.EditMode
{
    public sealed class Spec083EconomyTests
    {
        GameObject root;
        [SetUp] public void SetUp() { root = new GameObject("Economy083"); }
        [TearDown] public void TearDown() { Object.DestroyImmediate(root); }
        BurgerInventory Carrier(string name)
        { var go=new GameObject(name);go.transform.SetParent(root.transform);var p=go.AddComponent<BurgerInventory>();p.Configure();return p; }

        [TestCase(1,.75f)][TestCase(2,.65f)][TestCase(3,.55f)]
        public void RealWindowStartsShareOneCooldownOverlappingHandoff(int level,float interval)
        {
            var player=Carrier("Player");var other=Carrier("Other operator");
            var wallet=root.AddComponent<RestaurantWallet>();
            var facility=FacilityFactory.Create(root.transform,FacilityKind.CarCounter,"custom:00000000000000000000000000000001",player,wallet,null,null,null);
            var pack=facility.GetComponentInChildren<BoxingStation>();var lane=facility.GetComponentInChildren<DriveThruLane>();
            lane.ServiceLevel=level;lane.OrderQuantityFactory=()=>2;
            player.transform.position=Vector3.one*100;
            for(int i=0;i<2;i++)
            { Assert.That(player.TryReceive(CarriedItemKind.Boxed,new GameObject("Test box").transform),Is.True);Assert.That(pack.Package.TryPlaceBoxedFrom(player),Is.True); }
            for(int i=0;i<3600&&!lane.HasStoppedCarAtWindow;i++)lane.Advance(1f/60);
            Assert.That(lane.HasStoppedCarAtWindow,Is.True);
            player.transform.position=other.transform.position=lane.WindowPosition;
            Assert.That(lane.TrySellFrom(player),Is.True);
            Assert.That(lane.TrySellFrom(other),Is.False);
            player.transform.position=Vector3.one*100;
            lane.Advance(DriveThruLane.HandoffDuration+.001f);
            Assert.That(lane.IsHandoffActive,Is.False);
            Assert.That(lane.DeliveredUnits,Is.EqualTo(1));
            lane.Advance(interval-DriveThruLane.HandoffDuration-.002f);
            Assert.That(lane.TrySellFrom(other),Is.False,"cannot bypass the shared window clock");
            lane.Advance(.002f);
            Assert.That(lane.TrySellFrom(other),Is.True,"interval starts at dispatch, not after animation");
            var record=facility.Capture();Assert.That(record.level,Is.EqualTo(level));
            lane.ServiceLevel=1;facility.Apply(record);
            Assert.That(lane.ServiceInterval,Is.EqualTo(interval).Within(.0001f));
            Assert.That(FacilityUpgradeBenefit.Describe(FacilityKind.CarCounter,level),Does.Contain(interval.ToString("0.00")));
            Assert.That(FacilityCatalog.UpgradeCosts(FacilityKind.CarCounter),Is.EqualTo(new[]{100,200}));
        }

        [TestCase(0)][TestCase(49)][TestCase(80)][TestCase(120)]
        public void HiddenDirectTableAndFullyPaidChoiceUseFullOfflinePrice(int paid)
        {
            var table=DiningTable.Create(root.transform,Vector3.zero);
            var pad=new GameObject("TableUpgrade");pad.transform.SetParent(table.transform);
            var zone=pad.AddComponent<TableUpgradeZone>();zone.Configure(table,0,null,null,pad.transform,null,null);
            zone.Restore(0,paid);zone.UseDirectInteraction();
            Assert.That(OfflineUpgradeCostSource.CheapestUpgrade(root.transform),Is.EqualTo(50));
            Assert.That(TableSetCatalog.Due(TableSetId.Bistro,zone.Invested),Is.EqualTo(Mathf.Max(0,50-paid)));
            Assert.That(zone.Invested,Is.EqualTo(paid));
            table.ApplySet(TableSetId.Bistro);
            Assert.That(OfflineUpgradeCostSource.CheapestUpgrade(root.transform),Is.Zero);
        }

        [Test] public void RecommendationRequiresPersistentEvidenceAndDoesNotFlap()
        {
            var rule=new InvestmentPriority();
            foreach(var need in new[]{InvestmentNeed.Production,InvestmentNeed.Transport,InvestmentNeed.Service,InvestmentNeed.Seats})
            {
                string line=need.ToString();
                rule.Observe(need,line,true,0);rule.Observe(need,line,true,9.99f);
                Assert.That(rule.Priority(need,line,false),Is.EqualTo(2));
                rule.Observe(need,line,false,10);rule.Observe(need,line,true,11);
                rule.Observe(need,line,true,21);
                Assert.That(rule.Priority(need,line,false),Is.EqualTo(1));
                rule.Observe(need,line,false,22);rule.Observe(need,line,false,31.99f);
                Assert.That(rule.Priority(need,line,false),Is.EqualTo(1));
                rule.Observe(need,line,false,32);Assert.That(rule.Priority(need,line,false),Is.EqualTo(2));
            }
            rule.Observe(InvestmentNeed.Service,"Burger",true,0);rule.Observe(InvestmentNeed.Service,"Burger",true,10);
            Assert.That(rule.Priority(InvestmentNeed.Production,"Burger",false),Is.GreaterThan(rule.Priority(InvestmentNeed.Service,"Burger",false)));
            Assert.That(rule.Priority(InvestmentNeed.None,"",true),Is.Zero,"missing business components come first");
        }

        [TestCase(0,200)][TestCase(1,250)][TestCase(4,490)]
        public void QuotesUsePrepurchaseCountAndSeparateCredit(int owned,int full)
        {
            var quote=new FacilityPurchaseQuote(FacilityKind.BurgerMachine,owned,30);
            Assert.That(quote.BasePrice,Is.EqualTo(200));Assert.That(quote.FullPrice,Is.EqualTo(full));
            Assert.That(quote.Invested,Is.EqualTo(30));Assert.That(quote.Due,Is.EqualTo(full-30));
            Assert.That(quote.Equals(new FacilityPurchaseQuote(FacilityKind.BurgerMachine,owned+1,30)),Is.False);
        }

        [Test] public void LiveObservationDistinguishesSupplyTransportAndServiceIncludingHandoffs()
        {
            var actor=Carrier("Operator");
            var wallet=root.AddComponent<RestaurantWallet>();
            var queue=root.AddComponent<CustomerQueue>();
            queue.OrderQuantityFactory=()=>1;
            queue.Configure(new Vector3(0,0,-5),new Vector3(0,0,-4),
                new[]{Vector3.zero,new Vector3(0,0,-1.5f),new Vector3(0,0,-3)},Vector3.right);
            for(int i=0;i<1800;i++)queue.Advance(1f/60);
            Assert.That(queue.Count,Is.EqualTo(3));
            Assert.That(queue.ReadyCustomer,Is.Not.Null);
            var stock=root.AddComponent<CounterStock>();stock.Configure(null,null);
            var service=root.AddComponent<BurgerServingZone>();
            service.Configure(queue,actor,wallet,actor.transform,new[]{Vector3.one*10},stock,(DiningArea)null);
            var machine=root.AddComponent<ProductionStation>();machine.Configure(null,null,null);
            var priority=new InvestmentPriority();

            InvestmentObservation.Read(service,priority,0);InvestmentObservation.Read(service,priority,10);
            Assert.That(priority.Persistent(InvestmentNeed.Production,"Burger"),Is.True);
            machine.Advance(3);Assert.That(actor.TryCollectFrom(machine),Is.True);
            Assert.That(machine.Stock,Is.Zero,"carried food still counts as available upstream stock");
            InvestmentObservation.Read(service,priority,20);InvestmentObservation.Read(service,priority,30);
            Assert.That(priority.Persistent(InvestmentNeed.Production,"Burger"),Is.False);
            Assert.That(priority.Persistent(InvestmentNeed.Transport,"Burger"),Is.True);

            Assert.That(stock.TryPlaceFrom(actor),Is.True);
            machine.Advance(3);Assert.That(actor.TryCollectFrom(machine),Is.True);Assert.That(stock.TryPlaceFrom(actor),Is.True);
            InvestmentObservation.Read(service,priority,40);
            Assert.That(service.TryServeFrom(actor),Is.True);Assert.That(service.IsHandoffActive,Is.True);
            InvestmentObservation.Read(service,priority,50);
            Assert.That(priority.Persistent(InvestmentNeed.Service,"Burger"),Is.True,"handoff animation must not erase waiting demand");
            Assert.That(priority.Persistent(InvestmentNeed.Transport,"Burger"),Is.False);
            service.Advance(BurgerServingZone.HandoffDuration);
            queue.Advance(.01f);
            Assert.That(queue.FrontCustomer.HasReachedSlot,Is.False,"next customer is stepping forward");
            InvestmentObservation.Read(service,priority,60);InvestmentObservation.Read(service,priority,70);
            Assert.That(priority.Persistent(InvestmentNeed.Service,"Burger"),Is.True,"advancing the queue is still service demand");
            Assert.That(priority.Priority(InvestmentNeed.Service,"Burger",false),
                Is.LessThan(priority.Priority(InvestmentNeed.Production,"Burger",false)));
        }

        [Test] public void DirtyTablesDoNotProduceABuyMoreSeatsRecommendation()
        {
            var area=root.AddComponent<DiningArea>();
            var table=DiningTable.Create(root.transform,Vector3.zero);area.Configure(new[]{table});
            for(int i=0;i<table.SeatCount;i++)
            {
                var guest=CustomerAgent.Create(root.transform,i+1,Vector3.one*10,1);
                Assert.That(table.TryAssignSeat(guest,out _,out _),Is.True);
            }
            var priority=new InvestmentPriority();
            InvestmentObservation.Read(area,priority,0);InvestmentObservation.Read(area,priority,10);
            Assert.That(priority.Persistent(InvestmentNeed.Seats,"dining"),Is.True);
            table.LeaveMealTrash(0);
            InvestmentObservation.Read(area,priority,20);InvestmentObservation.Read(area,priority,30);
            Assert.That(priority.Persistent(InvestmentNeed.Seats,"dining"),Is.False);
        }

        [Test] public void PurchasedTierRestoresWithoutPaymentAndUpgradePublishesCommittedState()
        {
            const string id="custom:08300000000000000000000000000009";
            var actor=Carrier("Player");var wallet=root.AddComponent<RestaurantWallet>();
            var goals=root.AddComponent<SessionGoalTracker>();
            var growth=root.AddComponent<GrowthUpgrades>();growth.Configure(wallet,goals,actor,null);
            int applied=0;
            growth.Register(new GrowthUpgrades.Offer{Id=id,Target=root.transform,Costs=new[]{100,200},Apply=n=>applied=n});
            wallet.RestoreProgress(500,0);
            growth.RestorePurchasedLevel(id,2);
            Assert.That(growth.Level(id),Is.EqualTo(2));Assert.That(applied,Is.EqualTo(2));
            Assert.That(wallet.Coins,Is.EqualTo(500));Assert.That(goals.Stars,Is.Zero);
            wallet.CoinsSpent+=amount=>
            {
                Assert.That(amount,Is.EqualTo(200));Assert.That(growth.Level(id),Is.EqualTo(3));
                Assert.That(applied,Is.EqualTo(3));Assert.That(goals.Stars,Is.EqualTo(2));
                Assert.That(growth.TryBuy(id,3),Is.False);
            };
            Assert.That(growth.TryBuy(id,1),Is.False);
            Assert.That(growth.TryBuy(id,2),Is.True);Assert.That(wallet.Coins,Is.EqualTo(300));
            growth.RestorePurchasedLevel(id,1);
            Assert.That(growth.Level(id),Is.EqualTo(3),"an older duplicate snapshot cannot lower an owned tier");
            Assert.That(applied,Is.EqualTo(3));Assert.That(goals.Stars,Is.EqualTo(2));
        }
    }
}
