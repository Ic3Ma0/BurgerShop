using System;
using System.Collections;
using BurgerShop.Economy;
using BurgerShop.Persistence;
using BurgerShop.Restaurant;
using BurgerShop.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;
namespace BurgerShop.Tests.EditMode
{
    public sealed class Spec051OfflineTests:SaveIsolatedGameplayTest
    {
        static long now;
        static long FakeNow()=>now;
        [Test] public void AC02_04_06_07_ExactFormula()
        {
            Assert.That(OfflineEarnings.MaxEquivalents(3,20,20),Is.EqualTo(8m));
            Assert.That(OfflineEarnings.Grant(1000,3,10,10,480),Is.EqualTo(5818));
            Assert.That(OfflineEarnings.Grant(1000,3,20,20,43200),Is.EqualTo(8000));
            Assert.That(OfflineEarnings.Grant(1000,3,20,20,10),Is.EqualTo(166));
            Assert.That(OfflineEarnings.Grant(1000,0,20,20,480),Is.Zero);
        }
        [Test] public void AC08_AllStaffTiersAndSamplePricesRespectEightUpgradeBound()
        {
            foreach(int cost in new[]{30,50,80,500,1330,10000,int.MaxValue})
            for(int count=0;count<=3;count++)for(int speed=0;speed<=20;speed++)for(int carry=0;carry<=20;carry++)
            foreach(decimal minutes in new[]{0m,10m,480m,43200m})
                Assert.That(OfflineEarnings.Grant(cost,count,speed,carry,minutes),Is.InRange(0L,8L*cost));
        }
        [Test] public void AC09_ActiveOnlineReferenceBeatsOfflineAtSampleProgressPoints()
        {
            var root=new GameObject("OnlineReference");
            try
            {
                var station=root.AddComponent<ProductionStation>();var wallet=root.AddComponent<RestaurantWallet>();
                foreach(int cost in new[]{30,50,80,500,1330})foreach(int minutes in new[]{10,480})
                {
                    station.Configure(null,null,null,3,4);wallet.RestoreProgress(0,0);
                    for(int t=0;t<minutes*60;t++){station.Advance(1);while(station.TryTakeBurger())Assert.That(wallet.RecordSale(CashFloor.CounterDrop),Is.True);}
                    Assert.That(OfflineEarnings.Grant(cost,3,20,20,minutes),Is.LessThan(wallet.Coins));
                }
            }
            finally{Object.DestroyImmediate(root);}
        }
        [Test] public void AC09_StrictUniversalClaimHasZeroTimeCounterexample()
        {
            Assert.That(OfflineEarnings.Grant(50,3,20,20,0),Is.Zero);
            // This documents a counterexample, NOT a passing universal AC-09 assertion.
            Assert.That(OfflineEarnings.Grant(50,3,20,20,0)<0,Is.False);
        }
        [Test] public void AC11_OldChecksumsIgnoreNewFields()
        {
            foreach(int version in new[]{1,14,15})
            {
                var data=new RestaurantSaveData{version=version,grillLevel=1};string old=data.Checksum();
                data.lastSeenUtcTicks=DateTime.UtcNow.Ticks;data.offlineReceiptGrant=42;
                Assert.That(data.Checksum(),Is.EqualTo(old));
            }
            var current=new RestaurantSaveData{version=16,grillLevel=1};string checksum=current.Checksum();current.lastSeenUtcTicks=1;
            Assert.That(current.Checksum(),Is.Not.EqualTo(checksum));
        }
        [UnityTest] public IEnumerator AC11_V14Migration()
        {
            SeedLegacy(14);EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            yield return new EnterPlayMode();VerifyMigration();yield return new ExitPlayMode();
        }
        [UnityTest] public IEnumerator AC11_V15Migration()
        {
            SeedLegacy(15);EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            yield return new EnterPlayMode();VerifyMigration();yield return new ExitPlayMode();
        }
        void SeedLegacy(int version)
        {
            if(Application.isPlaying)return; // EnterPlayMode replays the outer test enumerator.
            var data=new RestaurantSaveData{version=version,coins=1234,completedSales=21,grillLevel=2,
                workerHired=true,hiredWorkerCount=3,staffSpeedTier=10,staffCarryTier=10,shopRank=6};
            Assert.That(new LocalSaveStore(SaveDirectory).Save(data),Is.True);
        }
        void VerifyMigration()
        {
            var p=Object.FindFirstObjectByType<RestaurantPersistence>();var store=new LocalSaveStore(SaveDirectory);
            Assert.That(p,Is.Not.Null);
            Assert.That(store.Load(out var migrated),Is.EqualTo(SaveLoadResult.Loaded));
            Assert.That(migrated.version,Is.EqualTo(RestaurantSaveData.CurrentVersion));Assert.That(migrated.coins,Is.EqualTo(1234));
            Assert.That(migrated.completedSales,Is.EqualTo(21));Assert.That(migrated.grillLevel,Is.EqualTo(2));
            Assert.That(migrated.hiredWorkerCount,Is.EqualTo(3));Assert.That(migrated.staffSpeedTier,Is.EqualTo(10));
            Assert.That(migrated.shopRank,Is.GreaterThanOrEqualTo(6));Assert.That(p.OfflineGrant,Is.Zero);
            now=migrated.lastSeenUtcTicks+TimeSpan.FromHours(8).Ticks;p.UtcNowTicks=FakeNow;
            Assert.That(p.SettleOffline(),Is.True);Assert.That(p.OfflineGrant,Is.GreaterThan(0));
        }
        [UnityTest] public IEnumerator ContinueReceivesPointerClickAndDismissesWithoutPayingAgain()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");yield return new EnterPlayMode();
            var p=Object.FindFirstObjectByType<RestaurantPersistence>();
            var store=new LocalSaveStore(SaveDirectory);store.Load(out var saved);
            now=saved.lastSeenUtcTicks+TimeSpan.FromHours(8).Ticks;p.UtcNowTicks=FakeNow;
            Assert.That(p.SettleOffline(),Is.True);yield return null;
            Canvas.ForceUpdateCanvases();
            var button=GameObject.Find("OfflineSettlement/AfterHoursCard/Continue");
            // Find by the panel, because the scene Canvas has a SafeArea parent.
            if(button==null)button=GameObject.Find("AfterHoursCard").transform.Find("Continue").gameObject;
            var canvas=button.GetComponentInParent<Canvas>();
            var rect=button.GetComponent<RectTransform>();
            var camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
            var events=UnityEngine.EventSystems.EventSystem.current;
            var pointer=new UnityEngine.EventSystems.PointerEventData(events)
            {position=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center)),button=UnityEngine.EventSystems.PointerEventData.InputButton.Left};
            var hits=new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();events.RaycastAll(pointer,hits);
            Assert.That(hits.Count,Is.GreaterThan(0));Assert.That(hits[0].gameObject,Is.EqualTo(button),"Continue must receive the click ahead of the overlay");
            long coins=p.GetComponent<RestaurantWallet>().Coins;
            UnityEngine.EventSystems.ExecuteEvents.Execute(hits[0].gameObject,pointer,UnityEngine.EventSystems.ExecuteEvents.pointerDownHandler);
            UnityEngine.EventSystems.ExecuteEvents.Execute(hits[0].gameObject,pointer,UnityEngine.EventSystems.ExecuteEvents.pointerUpHandler);
            UnityEngine.EventSystems.ExecuteEvents.Execute(hits[0].gameObject,pointer,UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
            Assert.That(p.OfflineVisible,Is.False);Assert.That(button.activeInHierarchy,Is.False);
            Assert.That(p.GetComponent<RestaurantWallet>().Coins,Is.EqualTo(coins));
            Assert.That(store.Load(out var dismissed),Is.EqualTo(SaveLoadResult.Loaded));Assert.That(dismissed.offlineReceiptVisible,Is.False);
            yield return new ExitPlayMode();
        }
        [UnityTest] public IEnumerator AC01_03_05_10_12_AtomicCreditClockRollbackAndReceipt()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");yield return new EnterPlayMode();
            var p=Object.FindFirstObjectByType<RestaurantPersistence>();var wallet=p.GetComponent<RestaurantWallet>();
            var store=new LocalSaveStore(SaveDirectory);Assert.That(store.Load(out var initial),Is.EqualTo(SaveLoadResult.Loaded));
            Assert.That(initial.lastSeenUtcTicks,Is.GreaterThan(0));Assert.That(p.OfflineGrant,Is.Zero);
            now=initial.lastSeenUtcTicks;p.UtcNowTicks=FakeNow;
            now+=TimeSpan.FromHours(8).Ticks;Assert.That(p.SettleOffline(),Is.True);Assert.That(p.OfflineGrant,Is.Zero);Assert.That(p.OfflineStaffCount,Is.Zero);
            Assert.That(p.OfflineVisible,Is.True);Assert.That(p.DismissOfflineReceipt(),Is.True);
            p.GetComponent<WorkerHiringZone>().RestoreWorkers(3,0,0);p.GetComponent<StaffUpgradeBoard>().RestoreTiers(10,10);
            p.Flush();store.Load(out var before);int cheapest=OfflineUpgradeCostSource.CheapestUpgrade(p);
            long expected=OfflineEarnings.Grant(cheapest,3,10,10,480);now+=TimeSpan.FromHours(8).Ticks;
            Assert.That(p.SettleOffline(),Is.True);Assert.That(wallet.Coins-before.coins,Is.EqualTo(expected));
            store.Load(out var after);Assert.That(after.coins,Is.EqualTo(wallet.Coins));
            before.version=after.version;before.coins=after.coins;before.lastSeenUtcTicks=after.lastSeenUtcTicks;
            before.offlineReceiptGrant=after.offlineReceiptGrant;before.offlineReceiptTicks=after.offlineReceiptTicks;before.offlineReceiptStaffCount=after.offlineReceiptStaffCount;before.offlineReceiptVisible=after.offlineReceiptVisible;
            Assert.That(before.Checksum(),Is.EqualTo(after.Checksum()),"Only cash/time/receipt may change");
            long credited=wallet.Coins;Assert.That(p.SettleOffline(),Is.True);Assert.That(wallet.Coins,Is.EqualTo(credited));
            Assert.That(new LocalSaveStore(SaveDirectory).Load(out var durable),Is.EqualTo(SaveLoadResult.Loaded));Assert.That(durable.coins,Is.EqualTo(credited));Assert.That(durable.offlineReceiptVisible,Is.True);
            p.DismissOfflineReceipt();now-=TimeSpan.FromDays(1).Ticks;Assert.That(p.SettleOffline(),Is.True);Assert.That(wallet.Coins,Is.EqualTo(credited));
            store.Load(out var reset);Assert.That(reset.lastSeenUtcTicks,Is.EqualTo(now));
            now+=TimeSpan.FromHours(8).Ticks;p.SettleOffline();Assert.That(wallet.Coins,Is.EqualTo(credited+expected));
            // Simulate an unwritable temp file without changing OS permissions or the real save.
            p.DismissOfflineReceipt();long beforeFailure=wallet.Coins;
            System.IO.Directory.CreateDirectory(store.FilePath+".tmp");now+=TimeSpan.FromHours(8).Ticks;
            Assert.That(p.SettleOffline(),Is.False);Assert.That(wallet.Coins,Is.EqualTo(beforeFailure));
            System.IO.Directory.Delete(store.FilePath+".tmp");Assert.That(p.Flush(),Is.True);
            Assert.That(wallet.Coins,Is.EqualTo(beforeFailure+expected));
            p.DismissOfflineReceipt();p.SendMessage("OnApplicationFocus",true);
            p.SendMessage("OnApplicationPause",true);p.SendMessage("OnApplicationFocus",false);
            now+=TimeSpan.FromHours(8).Ticks;long beforeResume=wallet.Coins;
            p.SendMessage("OnApplicationPause",false);p.SendMessage("OnApplicationFocus",true);
            Assert.That(wallet.Coins,Is.EqualTo(beforeResume+expected));
            p.SendMessage("OnApplicationFocus",true);p.SendMessage("OnApplicationPause",false);
            Assert.That(wallet.Coins,Is.EqualTo(beforeResume+expected));
            yield return null;Assert.That(GameObject.Find("AfterHoursCard"),Is.Not.Null);
            var canvas=GameObject.Find("OfflineSettlement").GetComponentInParent<Canvas>();
            canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=Camera.main;canvas.planeDistance=1;
            var texture=new RenderTexture(1280,720,24);Camera.main.targetTexture=texture;Canvas.ForceUpdateCanvases();Camera.main.Render();
            RenderTexture.active=texture;var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);
            pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();
            System.IO.File.WriteAllBytes("/tmp/bs051-receipt.png",pixels.EncodeToPNG());
            Camera.main.targetTexture=null;RenderTexture.active=null;Object.Destroy(pixels);Object.Destroy(texture);
            yield return new ExitPlayMode();
        }
    }
}
