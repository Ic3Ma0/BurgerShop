using System.Collections;
using BurgerShop.Restaurant;
using BurgerShop.Customer;
using BurgerShop.Persistence;
using BurgerShop.Economy;
using BurgerShop.Building;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
namespace BurgerShop.Tests.EditMode
{
    public sealed class RestroomTests:SaveIsolatedGameplayTest
    {
        [Test] public void SaveCompatibilityAndDirtyValidation()
        {
            var old=new RestaurantSaveData{version=16,grillLevel=1};string hash=old.Checksum();
            old.restroomBuilt=true;old.restroomInvestment=300;old.restroomDirtyMask=3;Assert.That(old.Checksum(),Is.EqualTo(hash));
            old.version=17;Assert.That(old.IsValid,Is.True);Assert.That(old.Checksum(),Is.Not.EqualTo(hash));
            old.restroomDirtyMask=4;Assert.That(old.IsValid,Is.False);
        }
        [UnityTest] public IEnumerator StoreShowsLockAndCreditsPreviousInvestment()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");yield return new EnterPlayMode();
            var room=RestroomExpansion.Current;var wallet=room.GetComponent<RestaurantWallet>();var shop=Object.FindFirstObjectByType<FacilityShopHud>();
            wallet.RestoreProgress(1000,0);shop.Open();
            GameObject.Find("ExpansionsTab").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            var buy=GameObject.Find("BuyRestroom").GetComponent<UnityEngine.UI.Button>();
            Assert.That(buy.interactable,Is.False);Assert.That(buy.transform.Find("Detail").GetComponent<UnityEngine.UI.Text>().text,Does.Contain("Lv.4"));
            Assert.That(room.TryPurchase(),Is.False);shop.Close();
            room.GetComponent<BurgerShop.UI.SessionGoalTracker>().Restore(12,0,0,0);room.Pad.RestoreInvestment(100);
            wallet.RestoreProgress(199,0);Assert.That(room.TryPurchase(),Is.False);Assert.That(wallet.Coins,Is.EqualTo(199));
            wallet.RestoreProgress(500,0);shop.Open();shop.ShowExpansions();Assert.That(buy.transform.Find("Detail").GetComponent<UnityEngine.UI.Text>().text,Does.Contain("200"));
            yield return null;Canvas.ForceUpdateCanvases();
            var canvas=buy.GetComponentInParent<Canvas>();
            var camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
            var rect=buy.GetComponent<RectTransform>();
            var pointer=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center))};
            var hits=new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointer,hits);
            Assert.That(hits.Count,Is.GreaterThan(0));Assert.That(hits[0].gameObject,Is.EqualTo(buy.gameObject),"058: restroom expansion card must receive its click");
            var previousMode=canvas.renderMode;var previousCamera=canvas.worldCamera;float previousDistance=canvas.planeDistance;
            canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=Camera.main;canvas.planeDistance=1;
            var output=new RenderTexture(1000,1200,24);Camera.main.targetTexture=output;Canvas.ForceUpdateCanvases();Camera.main.Render();RenderTexture.active=output;
            var pixels=new Texture2D(1000,1200,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1000,1200),0,0);pixels.Apply();
            System.IO.File.WriteAllBytes("/tmp/bs053-card-shop.png",pixels.EncodeToPNG());Camera.main.targetTexture=null;RenderTexture.active=null;
            canvas.renderMode=previousMode;canvas.worldCamera=previousCamera;canvas.planeDistance=previousDistance;Object.Destroy(pixels);Object.Destroy(output);
            UnityEngine.EventSystems.ExecuteEvents.Execute(hits[0].gameObject,pointer,UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
            Assert.That(room.Built,Is.True);Assert.That(wallet.Coins,Is.EqualTo(300));Assert.That(room.Invested,Is.EqualTo(300));
            Assert.That(room.TryPurchase(),Is.False);Assert.That(wallet.Coins,Is.EqualTo(300));
            yield return new ExitPlayMode();
        }
        [UnityTest] public IEnumerator ReloadPreservesBuiltRoomAndDirtyCubicle()
        {
            Assert.That(new LocalSaveStore(SaveDirectory).Save(new RestaurantSaveData{version=17,grillLevel=1,shopRank=4,coins=500,restroomBuilt=true,restroomInvestment=RestroomExpansion.Cost,restroomDirtyMask=2}),Is.True);
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");yield return new EnterPlayMode();
            var room=RestroomExpansion.Current;
            Assert.That(room.Built,Is.True);Assert.That(room.Pad.IsPurchased,Is.True);Assert.That(room.DirtyMask,Is.EqualTo(2));
            Assert.That(room.GetComponent<RestaurantWallet>().Coins,Is.EqualTo(500));
            Assert.That(FacilityLayout.Current.Route(Vector3.zero,RestroomExpansion.UsePoint(0)),Is.Not.Null);
            yield return new ExitPlayMode();
        }
        [UnityTest] public IEnumerator PurchaseRoutesExclusiveUseCleaningAndSave()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");yield return new EnterPlayMode();
            var room=RestroomExpansion.Current;Assert.That(room.Built,Is.False);
            var wallet=room.GetComponent<RestaurantWallet>();var player=room.Pad.Player;
            wallet.RestoreProgress(1000,0);room.GetComponent<BurgerShop.UI.SessionGoalTracker>().Restore(4,0,0,0);
            Assert.That(room.Pad.gameObject.activeSelf,Is.False);
            var shop=Object.FindFirstObjectByType<FacilityShopHud>();shop.Open();
            GameObject.Find("ExpansionsTab").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            var buy=GameObject.Find("BuyRestroom").GetComponent<UnityEngine.UI.Button>();
            Assert.That(buy.interactable,Is.True);buy.onClick.Invoke();
            Assert.That(Time.timeScale,Is.Zero,"058: remain in planning page after purchase");shop.Close();
            Assert.That(Time.timeScale,Is.GreaterThan(0));
            Assert.That(room.Built,Is.True);Assert.That(wallet.Coins,Is.EqualTo(700));
            Assert.That(room.TryPurchase(),Is.False);Assert.That(wallet.Coins,Is.EqualTo(700));
            var layout=FacilityLayout.Current;
            Assert.That(layout.Route(Vector3.zero,RestroomExpansion.Door),Is.Not.Null);
            for(int i=0;i<2;i++)Assert.That(layout.Route(RestroomExpansion.Door,RestroomExpansion.UsePoint(i)),Is.Not.Null,"Cubicle "+i);
            Assert.That(layout.Route(RestroomExpansion.UsePoint(0),RestroomExpansion.Wash),Is.Not.Null);
            var a=CustomerAgent.Create(room.transform,100,Vector3.zero);var b=CustomerAgent.Create(room.transform,102,Vector3.zero);var c=CustomerAgent.Create(room.transform,104,Vector3.zero);
            Assert.That(room.TryVisit(a)&&room.TryVisit(b)&&room.TryVisit(c),Is.True);
            Assert.That(room.Acquire(a),Is.EqualTo(0));Assert.That(room.Acquire(b),Is.EqualTo(1));Assert.That(room.Acquire(c),Is.EqualTo(-1));
            room.FinishUse(a,0);Assert.That(room.IsDirty(0),Is.True);Assert.That(room.Acquire(c),Is.EqualTo(-1));
            Assert.That(room.Clean(0,2),Is.False);Assert.That(room.Clean(0,1),Is.True);Assert.That(room.Acquire(c),Is.EqualTo(0));
            room.FinishUse(c,0);room.Release(a);room.Release(b);room.Release(c);
            room.GetComponent<RestaurantPersistence>().Flush();
            Assert.That(new LocalSaveStore(SaveDirectory).Load(out var saved),Is.EqualTo(SaveLoadResult.Loaded));
            Assert.That(saved.restroomBuilt,Is.True);Assert.That(saved.restroomDirtyMask,Is.EqualTo(1));
            player.transform.position=RestroomExpansion.UsePoint(0)+Vector3.up;
            for(int i=0;i<210;i++){room.AdvancePlayerCleaning(1f/60f);}
            Assert.That(room.IsDirty(0),Is.False);
            // Full existing dining cycle, then restroom, hand washing and a routed exit.
            var hall=Object.FindFirstObjectByType<DiningArea>();
            foreach(var table in hall.Tables)table.gameObject.SetActive(true);
            player.transform.position=new Vector3(5,1,0);layout.RefreshNavigation();
            var diner=CustomerAgent.Create(room.transform,200,new Vector3(-7,0,3));
            diner.BeginDeparture(null,new[]{new Vector3(-12,0,-14),new Vector3(-12,0,-23),new Vector3(-33,0,-23)},10,hall);
            bool visited=false;
            for(int i=0;i<1800&&!diner.DepartureComplete;i++){diner.AdvanceDeparture(.1f);visited|=diner.IsUsingRestroom;}
            Assert.That(visited,Is.True);Assert.That(diner.DepartureComplete,Is.True,"Customer stuck at "+diner.transform.position);
            Assert.That(room.DirtyMask,Is.Not.Zero);Assert.That(room.VisitorCount,Is.Zero);
            // A hired employee must physically reach and clean, then leave the room.
            var bin=Object.FindFirstObjectByType<TrashBin>(FindObjectsInactive.Include);bin.gameObject.SetActive(true);
            var hiring=room.GetComponent<WorkerHiringZone>();hiring.RestoreWorkers(1,0,0);
            var worker=hiring.Workers[0];worker.transform.position=new Vector3(0,0,-10);
            bool cleanedByWorker=false;
            for(int i=0;i<1200&&room.DirtyMask!=0;i++){worker.Advance(.1f);cleanedByWorker|=worker.IsCleaningRestroom;}
            Assert.That(cleanedByWorker,Is.True);Assert.That(room.DirtyMask,Is.Zero,"Employee stuck at "+worker.transform.position);
            for(int i=0;i<200;i++)worker.Advance(.1f);
            Assert.That(worker.transform.position.z,Is.GreaterThan(-15),"Employee must return from restroom");
            var camera=Camera.main;camera.transform.position=new Vector3(-12,16,-29);camera.transform.LookAt(new Vector3(-.5f,0,-17));
            var rt=new RenderTexture(1000,750,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            var image=new Texture2D(1000,750,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1000,750),0,0);image.Apply();
            System.IO.File.WriteAllBytes("/tmp/bs053-restroom.png",image.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=null;Object.Destroy(image);Object.Destroy(rt);
            yield return new ExitPlayMode();
        }
    }
}
