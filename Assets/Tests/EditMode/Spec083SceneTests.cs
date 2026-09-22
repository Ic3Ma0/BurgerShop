using System.Collections;
using System.IO;
using BurgerShop.Building;
using BurgerShop.Customer;
using BurgerShop.Economy;
using BurgerShop.Persistence;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using BurgerShop.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace BurgerShop.Tests.EditMode
{
    public sealed class Spec083SceneTests : SaveIsolatedGameplayTest
    {
        [UnityTest] public IEnumerator SmallStoreChineseHudTradeAndExpansionRenderInPlay()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");yield return new EnterPlayMode();
            for(int i=0;i<12;i++)yield return null;
            var goals=Object.FindFirstObjectByType<SessionGoalTracker>();var hall=MainHallExpansion.Current;
            var wallet=goals.GetComponent<RestaurantWallet>();var persistence=goals.GetComponent<RestaurantPersistence>();
            foreach(Transform part in goals.transform)
                if(part.name.StartsWith("Wall")||part.name.Contains("DoorPlug"))Assert.That(part.gameObject.activeSelf,Is.False,part.name+" must not outline unowned land");
            Assert.That(hall.SmallFootprint,Is.True);Assert.That(hall.Bounds.width*hall.Bounds.height,Is.EqualTo(300));
            Assert.That(persistence.Flush(),Is.True);Assert.That(RestaurantPersistence.PeekActiveSnapshot().smallFootprint,Is.True);
            var queue=MainKitchen<CustomerQueue>();queue.SendMessage("OnApplicationFocus",true);
            var player=Object.FindFirstObjectByType<PlayerMotor>();player.enabled=false;player.GetComponent<CharacterController>().enabled=false;
            var canvas=Object.FindFirstObjectByType<Canvas>();
            yield return Capture(canvas,"/tmp/bs083-start-landscape.png",1280,720);
            yield return Capture(canvas,"/tmp/bs083-start-portrait.png",720,1280);
            yield return Overview(canvas,"/tmp/bs083-shop-overview.png");
            // Real scene geometry including the door and furniture; advance this isolated queue only.
            for(int i=0;i<3600&&queue.ReadyCustomer==null;i++)
            {
                queue.Advance(1/60f);
                foreach(var customer in queue.Customers)
                    Assert.That(ShopLayout.ContainsHall(customer.transform.position)||ShopLayout.IsSouthOfShop(customer.transform.position),Is.True,customer.transform.position.ToString());
                if(i%120==0)yield return null;
            }
            Assert.That(queue.ReadyCustomer,Is.Not.Null);
            var serving=MainKitchen<BurgerServingZone>();var station=MainKitchen<ProductionStation>();
            var inv=player.GetComponent<BurgerInventory>();station.Advance(3);Assert.That(inv.TryCollectFrom(station),Is.True);
            player.transform.position=serving.ServingPosition+Vector3.up;
            serving.Advance(.01f);serving.Advance(BurgerServingZone.HandoffDuration);
            Assert.That(goals.FirstOrderComplete,Is.True);Assert.That(goals.TryUpgradeRank(1),Is.True);
            var table=goals.GetComponentInChildren<DiningTable>();var trash=player.GetComponent<TrashInventory>();
            table.LeaveMealTrash(0);while(table.TrashCount>0)Assert.That(table.TryPickupTrash(trash),Is.True);
            yield return null;
            yield return Capture(canvas,"/tmp/bs083-investment-landscape.png",1280,720);
            yield return Capture(canvas,"/tmp/bs083-investment-portrait.png",720,1280);
            wallet.CollectCoins(30);Assert.That(MainKitchen<GrillUpgradeZone>().TryUpgrade(1),Is.True);Assert.That(goals.TryUpgradeRank(2),Is.True);
            Vector3 original=MainKitchen<ProductionStation>().transform.position;
            wallet.CollectCoins(150);Assert.That(hall.TryContribute(),Is.True);yield return null;
            Assert.That(MainKitchen<ProductionStation>().transform.position,Is.EqualTo(original));
            Assert.That(goals.transform.Find("Wall+Z").gameObject.activeSelf,Is.True);
            yield return Capture(canvas,"/tmp/bs083-expanded.png",1280,720);
            yield return Overview(canvas,"/tmp/bs083-expanded-overview.png");
            Assert.That(persistence.Flush(),Is.True);
            Assert.That(RestaurantPersistence.PeekActiveSnapshot().smallFootprint,Is.True);
            yield return new ExitPlayMode();
        }
        [UnityTest] public IEnumerator Version18KeepsItsLayoutWhenSwitchingWithANewSlot()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            var old=new RestaurantSaveData{version=18,grillLevel=1,coins=777,compactStart=true,mainHallInvestment=50};
            Assert.That(new LocalSaveStore(SaveDirectory).Save(old),Is.True);
            yield return new EnterPlayMode();for(int i=0;i<12;i++)yield return null;
            var hall=MainHallExpansion.Current;var persistence=hall.GetComponent<RestaurantPersistence>();int oldSlot=persistence.ActiveSlotId;
            Assert.That(hall.Bounds,Is.EqualTo(MainHallExpansion.PreviousStarterBounds));Assert.That(hall.Invested,Is.EqualTo(50));
            Assert.That(MainKitchen<ProductionStation>().transform.position,Is.EqualTo(new Vector3(4,0,1.5f)));
            Assert.That(persistence.Flush(),Is.True);var saved=RestaurantPersistence.PeekActiveSnapshot();
            Assert.That(saved.version,Is.EqualTo(19));Assert.That(saved.smallFootprint,Is.False);Assert.That(saved.coins,Is.EqualTo(777));
            Assert.That(persistence.StartNewGame(),Is.True);for(int i=0;i<30;i++)yield return null;
            Assert.That(MainHallExpansion.Current.SmallFootprint,Is.True);Assert.That(MainHallExpansion.Current.Bounds,Is.EqualTo(MainHallExpansion.StarterBounds));
            persistence=MainHallExpansion.Current.GetComponent<RestaurantPersistence>();Assert.That(persistence.SwitchToSlot(oldSlot),Is.True);
            for(int i=0;i<30;i++)yield return null;
            hall=MainHallExpansion.Current;Assert.That(hall.SmallFootprint,Is.False);Assert.That(hall.Bounds,Is.EqualTo(MainHallExpansion.PreviousStarterBounds));
            Assert.That(hall.Invested,Is.EqualTo(50));Assert.That(MainKitchen<ProductionStation>().transform.position,Is.EqualTo(new Vector3(4,0,1.5f)));
            yield return new ExitPlayMode();
        }
        [UnityTest] public IEnumerator DoorTrafficFacesMotionWhileEnteringAndLeaving()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");yield return new EnterPlayMode();for(int i=0;i<12;i++)yield return null;
            var queue=MainKitchen<CustomerQueue>();queue.enabled=false;queue.SendMessage("OnApplicationFocus",true);
            for(int i=0;i<600;i++)queue.Advance(1/60f);
            var outgoing=CustomerAgent.Create(queue.transform.parent,99,ShopLayout.Entrance,1);
            outgoing.enabled=false;outgoing.BeginDeparture(null,ShopLayout.Exit,0,alreadyReceived:true);
            var camera=Camera.main;camera.GetComponent<CameraFollow>().enabled=false;
            var focus=new Vector3(-12,0,-18);camera.transform.position=focus+new Vector3(-16,20,-16);camera.transform.LookAt(focus);
            camera.orthographic=true;camera.orthographicSize=8;
            Directory.CreateDirectory("/tmp/bs083-walk");
            for(int frame=0;frame<120;frame++)
            {
                for(int step=0;step<6;step++)
                {
                    queue.Advance(1/60f);
                    if(outgoing!=null&&!outgoing.DepartureComplete)
                    {
                        var from=outgoing.transform.position;outgoing.AdvanceDeparture(1/60f);
                        var delta=outgoing.transform.position-from;
                        if(delta.sqrMagnitude>.0001f)Assert.That(Vector3.Dot(delta.normalized,outgoing.transform.forward),Is.GreaterThan(.95f));
                    }
                }
                yield return null;
                var rt=new RenderTexture(640,640,24);var old=camera.targetTexture;var active=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
                var image=new Texture2D(640,640,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,640,640),0,0);image.Apply();File.WriteAllBytes($"/tmp/bs083-walk/{frame:000}.png",image.EncodeToPNG());
                camera.targetTexture=old;RenderTexture.active=active;Object.Destroy(image);Object.Destroy(rt);
            }
            for(int i=0;i<1800&&queue.ReadyCustomer==null;i++)queue.Advance(1/60f);
            Assert.That(queue.ReadyCustomer,Is.Not.Null,"After filming the doorway, complete the remaining indoor approach.");
            yield return new ExitPlayMode();
        }
        static IEnumerator Overview(Canvas canvas,string path)
        {
            var camera=Camera.main;var follow=camera.GetComponent<CameraFollow>();follow.enabled=false;
            var position=camera.transform.position;var rotation=camera.transform.rotation;bool ortho=camera.orthographic;float size=camera.orthographicSize;
            var bounds=MainHallExpansion.Current.Bounds;var center=new Vector3(bounds.center.x,0,bounds.center.y);
            camera.transform.position=center+new Vector3(-26,28,-26);camera.transform.LookAt(center);
            camera.orthographic=true;camera.orthographicSize=MainHallExpansion.Current.Built?23:17;
            yield return Capture(canvas,path,1280,900);
            camera.transform.SetPositionAndRotation(position,rotation);camera.orthographic=ortho;camera.orthographicSize=size;follow.enabled=true;
        }
        static IEnumerator Capture(Canvas canvas,string path,int width,int height)
        {
            var camera=Camera.main;var old=camera.targetTexture;var active=RenderTexture.active;
            var mode=canvas.renderMode;var previousCamera=canvas.worldCamera;float scale=canvas.scaleFactor;
            var scaler=canvas.GetComponent<CanvasScaler>();bool scalerEnabled=scaler.enabled;scaler.enabled=false;
            var safe=canvas.GetComponentInChildren<SafeAreaFitter>();safe.enabled=false;safe.Apply(new Rect(0,0,width,height),width,height);
            var rt=new RenderTexture(width,height,24);camera.targetTexture=rt;
            canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
            canvas.scaleFactor=Mathf.Sqrt(width/1080f*height/1920f);
            Object.FindFirstObjectByType<TaskCapsuleHud>().RefreshNow();Object.FindFirstObjectByType<StarProgressHud>().RefreshNow();
            Canvas.ForceUpdateCanvases();yield return null;Canvas.ForceUpdateCanvases();
            foreach(var text in Object.FindFirstObjectByType<TaskCapsuleHud>().GetComponentsInChildren<Text>())
                if(text.name=="TaskTitle"||text.name=="InvestmentDetail")
                {
                    var settings=text.GetGenerationSettings(text.rectTransform.rect.size);
                    text.cachedTextGenerator.Populate(text.text,settings);
                    Assert.That(text.cachedTextGenerator.characterCountVisible,Is.GreaterThanOrEqualTo(text.text.Replace("\n","").Length),text.name+": "+text.text);
                }
            camera.Render();RenderTexture.active=rt;
            var image=new Texture2D(width,height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());
            camera.targetTexture=old;RenderTexture.active=active;canvas.renderMode=mode;canvas.worldCamera=previousCamera;canvas.scaleFactor=scale;scaler.enabled=scalerEnabled;safe.enabled=true;
            Object.Destroy(image);Object.Destroy(rt);
        }
        [UnityTearDown] public IEnumerator ExitIfFailed(){if(Application.isPlaying)yield return new ExitPlayMode();}
    }
}
