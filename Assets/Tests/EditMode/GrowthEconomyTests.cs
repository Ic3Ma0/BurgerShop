using System;
using System.IO;
using System.Linq;
using BurgerShop.Customer;
using BurgerShop.Economy;
using BurgerShop.Persistence;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using BurgerShop.UI;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;

namespace BurgerShop.Tests.EditMode
{
    public sealed class GrowthEconomyTests
    {
        GameObject root;BurgerInventory player;RestaurantWallet wallet;SessionGoalTracker goals;
        DiningArea dining;GrowthUpgrades growth;BagLine line;ShopExpansion expansion;
        WorkerHiringZone hiring;BurgerServingZone serving;ExpandableGrill grill;CustomerQueue queue;
        CashFloor cash;
        [SetUp] public void Setup()
        {
            root=new GameObject("GrowthEconomy");player=new GameObject("Player").AddComponent<BurgerInventory>();player.transform.SetParent(root.transform);player.transform.position=ShopLayout.PlayerSpawn;
            wallet=root.AddComponent<RestaurantWallet>();wallet.RestoreProgress(10000,0);
            dining=DiningArea.Create(root.transform,ShopLayout.Tables);
            cash=root.AddComponent<CashFloor>();cash.Configure(wallet,player.transform,ShopLayout.CounterCash);dining.BindCash(cash);
            grill=ExpandableGrill.CreateStarter(root.transform,player,wallet);
            queue=new GameObject("DiningQueue").AddComponent<CustomerQueue>();queue.transform.SetParent(root.transform);queue.CustomerKindFactory=()=>CustomerKind.Normal;queue.OrderQuantityFactory=()=>1;
            queue.Configure(ShopLayout.Entrance,ShopLayout.QueueEntry,ShopLayout.QueueSlots,ShopLayout.Counter);
            var stock=ShopFixtures.CreateCounterStock(queue.transform,ShopLayout.CounterTop);
            var point=new GameObject("ServePoint").transform;point.SetParent(root.transform);point.position=ShopLayout.ServingCircle;
            var drop=root.AddComponent<CounterDropZone>();drop.Configure(stock,point);
            serving=root.AddComponent<BurgerServingZone>();serving.Configure(queue,player,wallet,point,ShopLayout.Exit,stock,dining,drop,10,cash);
            var bin=TrashBin.Create(root.transform,ShopLayout.TrashBin);
            hiring=root.AddComponent<WorkerHiringZone>();hiring.Configure(grill.Station,serving,wallet,player,grill.Pickup.transform,root.transform,ShopLayout.Aisle,drop,null,dining,bin);
            // Worker pickup must use the actual authored pickup circle.
            var pickup=new GameObject("WorkerPickup").transform;pickup.SetParent(root.transform);pickup.position=ShopLayout.GrillPickup;
            hiring.Configure(grill.Station,serving,wallet,player,pickup,root.transform,ShopLayout.Aisle,drop,null,dining,bin);
            expansion=ShopExpansion.Create(root.transform,dining,serving,hiring,player,wallet,cash);
            goals=root.AddComponent<SessionGoalTracker>();goals.Configure(player,grill.Station,stock,queue,wallet,serving,dining,null,hiring,null,expansion);
            growth=root.AddComponent<GrowthUpgrades>();growth.Configure(wallet,goals,player,expansion);
            line=root.AddComponent<BagLine>();line.Configure(wallet,goals,growth,hiring,player,cash);
        }
        [TearDown] public void Cleanup()=>Object.DestroyImmediate(root);

        [Test] public void TablePurchaseAwardsOnceAndRankRequiresConfirmation()
        {
            wallet.RestoreProgress(200,0);goals.Restore(1,0,0,2,1,true);growth.Discover();
            var table=dining.Tables[0];table.LeaveMealTrash(0);
            Assert.That(growth.TryBuy("table-0",1),Is.True);
            Assert.That(wallet.Coins,Is.EqualTo(50));Assert.That(table.FurnitureLevel,Is.EqualTo(2));Assert.That(table.MealTip,Is.EqualTo(15));Assert.That(table.TrashCount,Is.EqualTo(2));
            Assert.That(table.transform.Find("FurnitureDetail"),Is.Not.Null);
            Assert.That(dining.Tables[1].FurnitureLevel,Is.EqualTo(1));Assert.That(goals.Stars,Is.EqualTo(4));Assert.That(goals.Rank,Is.EqualTo(1));
            Assert.That(growth.TryBuy("table-0",1),Is.False);Assert.That(growth.TryBuy("table-0",2),Is.False);Assert.That(goals.Stars,Is.EqualTo(4));
            Assert.That(goals.TryUpgradeRank(1),Is.True);Assert.That(goals.Rank,Is.EqualTo(2));Assert.That(goals.Stars,Is.Zero);Assert.That(wallet.Coins,Is.EqualTo(50));
            Assert.That(goals.TryUpgradeRank(1),Is.False);Assert.That(expansion.BoxingPad.RankVisible,Is.True);
            goals.Restore(1,0,0,7,1,true);goals.TryUpgradeRank(1);Assert.That(goals.Stars,Is.EqualTo(3));
        }

        [Test] public void UpgradePathEarnsEnoughStarsWithoutAutomaticRanks()
        {
            goals.Restore(1,0,0,0,511,true);growth.Discover();
            for(int i=0;i<3;i++)for(int n=1;n<4;n++)Assert.That(growth.TryBuy("table-"+i,n),Is.True);
            Assert.That(goals.Stars,Is.EqualTo(18));Assert.That(goals.Rank,Is.EqualTo(1));
            Assert.That(goals.TryUpgradeRank(1),Is.True);Assert.That(goals.TryUpgradeRank(2),Is.True);Assert.That(goals.TryUpgradeRank(3),Is.True);
            expansion.Restore(true,true,true,1,true,false);growth.Discover();
            foreach(string id in new[]{"table-extra","counter-main","counter-extra","boxing"})
                for(int n=1;n<(id.StartsWith("table")?4:3);n++)Assert.That(growth.TryBuy(id,n),Is.True,id);
            foreach(var g in new[]{grill.Upgrade,expansion.ExtraGrillUpgrade})
                for(int n=2;n<=3;n++){g.RestoreLevel(n);growth.RecordGrill(g);growth.RecordGrill(g);}
            Assert.That(goals.Stars,Is.EqualTo(26));Assert.That(goals.TryUpgradeRank(4),Is.True);Assert.That(goals.TryUpgradeRank(5),Is.True);
            Assert.That(goals.Rank,Is.EqualTo(6));Assert.That(goals.Stars,Is.EqualTo(4));
        }

        [Test] public void OldSaveCompensationIsOneTimeAndVersionNineKeepsManualRank()
        {
            var old=new RestaurantSaveData{version=8,coins=777,grillLevel=3,shopRank=4,goalIndex=1,goalProgress=2,boughtExtraGrill=true,extraGrillLevel=2};
            Assert.That(old.IsValid,Is.True);Assert.That(old.ResolvedUpgradeStars,Is.EqualTo(8));
            string dir=Path.Combine(Path.GetTempPath(),"BurgerGrowth-"+Guid.NewGuid().ToString("N"));
            try
            {
                var store=new LocalSaveStore(dir);Assert.That(store.Save(old),Is.True);
                var persistence=root.AddComponent<RestaurantPersistence>();persistence.Configure(wallet,grill.Upgrade,hiring,null,expansion,null,goals,dir);
                Assert.That(goals.Rank,Is.EqualTo(4));Assert.That(goals.Stars,Is.EqualTo(8));Assert.That(goals.GoalProgress,Is.EqualTo(1));
                Assert.That(persistence.Flush(),Is.True);store.Load(out var saved);Assert.That(saved.version,Is.EqualTo(RestaurantSaveData.CurrentVersion));Assert.That(saved.ResolvedUpgradeStars,Is.EqualTo(8));
                persistence.Configure(wallet,grill.Upgrade,hiring,null,expansion,null,goals,dir);Assert.That(goals.Stars,Is.EqualTo(8));
                goals.Restore(1,0,0,3,1,true);Assert.That(persistence.Flush(),Is.True);store.Load(out saved);
                Assert.That(saved.ResolvedShopRank,Is.EqualTo(1)); // Existing ownership must not silently rank up a v9 save.
                Assert.That(saved.boughtExtraGrill,Is.True);
            }
            finally{if(Directory.Exists(dir))Directory.Delete(dir,true);}
        }

        void OpenBagLine()
        {
            goals.Restore(9,0,0);Assert.That(line.TryExpand(),Is.False);goals.Restore(10,0,0);Assert.That(line.Expanded,Is.True);Assert.That(line.TryExpand(),Is.False);
            Assert.That(line.TablePad.RankVisible,Is.False);
            line.MachinePad.RestoreInvestment(250);Assert.That(line.TablePad.RankVisible,Is.True);
            line.TablePad.RestoreInvestment(200);line.CounterPad.RestoreInvestment(300);
            Assert.That(line.CounterBuilt,Is.True);Assert.That(ShopLayout.ContainsPlayable(new Vector3(-22,0,0)),Is.True);
            Assert.That(dining.Tables[0].Center,Is.EqualTo(ShopLayout.Tables[0]));
        }

        [Test] public void BagRecipeConservesBothIngredientsAndTwoBagOrderSettlesOnce()
        {
            OpenBagLine();player.Configure(8);line.OrderQuantityFactory=()=>2;
            for(int i=0;i<3;i++){grill.Station.Advance(4);Assert.That(player.TryCollectFrom(grill.Station),Is.True);}
            for(int i=0;i<2;i++)Assert.That(player.TryReceive(CarriedItemKind.EmptyBag,BagVisualFactory.Create(root.transform,false)),Is.True);
            player.transform.position=BagLine.WorkPoint+Vector3.up;
            for(int i=0;i<100;i++)line.Advance(.05f);
            Assert.That(player.BaggedCount+line.OutputCount,Is.EqualTo(2));Assert.That(line.InputBurgers,Is.EqualTo(1));Assert.That(line.InputBags,Is.Zero);
            // Stage a two-bag order with exactly one item at the counter.
            player.TryTake(CarriedItemKind.Bagged,out var held);held.SetParent(root.transform);
            player.transform.position=BagLine.CounterPoint+Vector3.up;
            for(int i=0;i<600;i++){line.Queue.Advance(.05f);line.Advance(.05f);}
            Assert.That(line.Queue.FrontCustomer.RemainingQuantity,Is.EqualTo(1));Assert.That(line.CompletedOrders,Is.Zero);Assert.That(wallet.CompletedSales,Is.Zero);
            Assert.That(player.TryReceive(CarriedItemKind.Bagged,held),Is.True);
            for(int i=0;i<30;i++)line.Advance(.05f);
            Assert.That(line.CompletedOrders,Is.EqualTo(1));Assert.That(wallet.CompletedSales,Is.EqualTo(1));Assert.That(dining.OccupiedSeats,Is.Zero);
            Assert.That(root.GetComponentsInChildren<CashPickup>().Sum(x=>x.Value),Is.EqualTo(40));
        }

        [Test] public void OneWorkerCanCompleteAllThreeLines()
        {
            OpenBagLine();expansion.Restore(true,false,false,0,true,true);growth.Discover();hiring.RestoreWorkers(1,0);
            player.transform.position=new Vector3(10,1,-3);
            for(int i=0;i<16000;i++)
            {
                const float dt=.05f;grill.Station.Advance(dt);queue.Advance(dt);serving.Advance(dt);
                expansion.Boxing.Advance(dt);expansion.DriveThru.Advance(dt);line.Queue.Advance(dt);line.Advance(dt);
                foreach(var c in root.GetComponentsInChildren<CustomerAgent>())if(c.IsDeparting)c.AdvanceDeparture(dt);
                foreach(var table in dining.Tables)table.Advance(dt);
                hiring.Worker.Advance(dt);
                if(serving.CompletedOrders>0&&expansion.DriveThru.CompletedOrders>0&&line.CompletedOrders>0)return;
            }
            Assert.Fail($"dining={serving.CompletedOrders} drive={expansion.DriveThru.CompletedOrders} bag={line.CompletedOrders} worker={hiring.Worker.State}/{hiring.Worker.Job} pos={hiring.Worker.transform.position} raw={line.InputBurgers} bags={line.InputBags} output={line.OutputCount} stock={line.StockCount}");
        }

        [Test] public void DiningTipIsLockedOnSeatingAndBagConstructionRestoresPartially()
        {
            var table=dining.Tables[0];
            var guest=CustomerAgent.Create(root.transform,1,table.Center,1);
            guest.BeginDeparture(null,new[]{Vector3.zero},10,DiningArea.Wrap(table),true);
            guest.AdvanceDeparture(1);Assert.That(guest.IsEating,Is.True);
            table.SetFurnitureLevel(4);guest.AdvanceDeparture(5);
            Assert.That(root.GetComponentsInChildren<CashPickup>().Sum(x=>x.Value),Is.EqualTo(10));
            Assert.That(table.MealTip,Is.EqualTo(25));Assert.That(table.TrashCount,Is.EqualTo(2));
            var saved=new RestaurantSaveData{version=9,coins=444,grillLevel=1,shopRank=6,upgradeStars=13,westExpanded=true,bagMachineBuilt=true,bagMachineInvestment=250,bagTableInvestment=70,
                facilityLevels=new[]{new FacilityLevelRecord{id="bag-machine",level=2},new FacilityLevelRecord{id="table-0",level=4}}};
            Assert.That(saved.IsValid,Is.True);line.Restore(saved);growth.Restore(saved.facilityLevels,1,0);
            Assert.That(line.MachineLevel,Is.EqualTo(2));Assert.That(line.TableBuilt,Is.False);Assert.That(line.TablePad.Invested,Is.EqualTo(70));Assert.That(line.TablePad.Remaining,Is.EqualTo(130));Assert.That(line.CounterPad.RankVisible,Is.False);Assert.That(line.EmptyStock,Is.Zero);
        }
    }
}
