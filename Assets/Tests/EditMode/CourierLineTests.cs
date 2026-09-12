using System;
using System.IO;
using BurgerShop.Customer;
using BurgerShop.Economy;
using BurgerShop.Persistence;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;

namespace BurgerShop.Tests.EditMode
{
    public sealed class CourierLineTests
    {
        GameObject root;BurgerInventory player;RestaurantWallet wallet;PartsWallet parts;CashFloor cash;CourierLine line;ProductionStation grill;
        [SetUp] public void Setup()
        {
            root=new GameObject("CourierTest");player=new GameObject("Player").AddComponent<BurgerInventory>();player.transform.SetParent(root.transform);player.Configure(16);
            wallet=root.AddComponent<RestaurantWallet>();parts=root.AddComponent<PartsWallet>();cash=root.AddComponent<CashFloor>();cash.Configure(wallet,player.transform,Vector3.zero);
            grill=root.AddComponent<ProductionStation>();grill.Configure(null,null,null,1,16);
            line=root.AddComponent<CourierLine>();line.Configure(wallet,parts,player,cash);
        }
        [TearDown] public void Cleanup()=>Object.DestroyImmediate(root);
        void Collect(int n){grill.Advance(n);for(int i=0;i<n;i++)Assert.That(player.TryCollectFrom(grill),Is.True);}
        void Step(float time){while(time>0){float dt=Mathf.Min(.05f,time);line.Advance(dt);time-=dt;}}
        void Produce(int n)
        {
            Collect(n);player.transform.position=CourierLine.InputPoint;Step(n*.35f+1);
            player.transform.position=Vector3.zero;Step(n+1);
        }
        [Test] public void RecipeConservesItemsAndBackpressureKeepsCapacity()
        {
            Produce(16);
            Assert.That(line.OutputCount,Is.EqualTo(8));Assert.That(line.InputCount+line.ProcessingCount+player.LooseCount,Is.EqualTo(8));
            Assert.That(line.ProcessingCount,Is.Zero);Assert.That(line.StockCount,Is.Zero);
            player.transform.position=CourierLine.OutputPoint;Step(3);
            Assert.That(player.RedParcelCount,Is.GreaterThan(0));
            Assert.That(line.InputCount+line.OutputCount+line.ProcessingCount+player.LooseCount+player.RedParcelCount,Is.EqualTo(16));
        }
        [Test] public void WrongFoodIsNotConsumedByMachineOrYellowTray()
        {
            var cola=root.AddComponent<ProductionStation>();cola.Configure(null,null,null,1,1,KitchenProduct.Cola);cola.Advance(1);player.TryCollectFrom(cola);
            var box=BoxVisualFactory.Create(root.transform,0);player.TryReceive(CarriedItemKind.Boxed,box);
            player.transform.position=CourierLine.InputPoint;Step(2);player.transform.position=CourierLine.DropPoint;Step(2);
            Assert.That(player.ColaCount,Is.EqualTo(1));Assert.That(player.BoxedCount,Is.EqualTo(1));Assert.That(line.InputCount+line.StockCount,Is.Zero);
        }
        [Test] public void ShortOrderWaitsThenDropsCashAndPartsExactlyOnce()
        {
            Produce(4);player.transform.position=CourierLine.OutputPoint;Step(2);
            Assert.That(player.RedParcelCount,Is.EqualTo(4));player.transform.position=Vector3.zero;Step(15);
            var customer=line.Front;Assert.That(customer,Is.Not.Null);
            player.transform.position=CourierLine.DropPoint;Step(.05f);player.transform.position=Vector3.zero;Step(2);
            Assert.That(customer.Received,Is.EqualTo(1));Assert.That(line.CompletedOrders,Is.Zero);Assert.That(cash.GroundValue+line.GroundParts,Is.Zero);
            player.transform.position=CourierLine.DropPoint;Step(2);player.transform.position=Vector3.zero;Step(4);
            Assert.That(customer.Received,Is.EqualTo(4));Assert.That(line.CompletedOrders,Is.EqualTo(1));Assert.That(wallet.CompletedSales,Is.EqualTo(1));
            Assert.That(cash.GroundValue,Is.EqualTo(80));Assert.That(line.GroundParts,Is.EqualTo(4));Assert.That(parts.Balance,Is.Zero);Assert.That(wallet.Coins,Is.Zero);
            Step(20);Assert.That(line.CompletedOrders,Is.EqualTo(1));
            player.transform.position=CourierLine.PartsPoint;Step(.3f);Assert.That(parts.Balance,Is.EqualTo(4));Assert.That(line.GroundParts,Is.Zero);
            player.transform.position=CourierLine.CashPoint;cash.Advance(1);Assert.That(wallet.Coins,Is.EqualTo(80));
        }
        [Test] public void PauseFreezesProductionAndRiderQueue()
        {
            Collect(1);player.transform.position=CourierLine.InputPoint;Step(.1f);line.SetPaused(true);Step(20);
            Assert.That(line.OutputCount,Is.Zero);Assert.That(line.ProcessingCount,Is.EqualTo(1));Assert.That(line.WaitingCount,Is.Zero);
            line.SetPaused(false);player.transform.position=Vector3.zero;Step(2);Assert.That(line.OutputCount,Is.EqualTo(1));
        }
        [Test] public void PartsRejectOverflowAndVersionTenChecksumIgnoresNewCurrency()
        {
            parts.Restore(long.MaxValue);Assert.That(parts.TryCollect(1),Is.False);Assert.That(parts.Balance,Is.EqualTo(long.MaxValue));
            var old=new RestaurantSaveData{version=10,coins=99,grillLevel=2,colaLevel=3};string checksum=old.Checksum();old.parts=999;
            Assert.That(old.Checksum(),Is.EqualTo(checksum));Assert.That(old.ResolvedParts,Is.Zero);
            old.version=11;Assert.That(old.IsValid,Is.True);old.parts=-1;Assert.That(old.IsValid,Is.False);
        }
        [Test] public void PersistenceRestoresPartsAlongsideExistingProgress()
        {
            string dir=Path.Combine(Path.GetTempPath(),"CourierSave-"+Guid.NewGuid().ToString("N"));
            try
            {
                var upgrade=root.AddComponent<GrillUpgradeZone>();upgrade.Configure(grill,wallet,player,null,null);
                var crew=root.AddComponent<WorkerHiringZone>();var persistence=root.AddComponent<RestaurantPersistence>();
                persistence.Configure(wallet,upgrade,crew,dir);wallet.RestoreProgress(123,2);parts.TryCollect(7);
                Assert.That(persistence.Flush(),Is.True);parts.Restore(0);wallet.RestoreProgress(0,0);
                persistence.Configure(wallet,upgrade,crew,dir);Assert.That(parts.Balance,Is.EqualTo(7));Assert.That(wallet.Coins,Is.EqualTo(123));
                Assert.That(new LocalSaveStore(dir).Load(out var saved),Is.EqualTo(SaveLoadResult.Loaded));Assert.That(saved.version,Is.EqualTo(11));
            }
            finally{if(Directory.Exists(dir))Directory.Delete(dir,true);}
        }
    }
}
