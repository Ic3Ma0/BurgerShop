using System.Collections;
using System.Linq;
using BurgerShop.Building;
using BurgerShop.Persistence;
using BurgerShop.Restaurant;
using BurgerShop.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace BurgerShop.Tests.EditMode
{
    public sealed class Spec054ShopPresentationTests : SaveIsolatedGameplayTest
    {
        [Test]
        public void AllCatalogOffersHaveDistinctReusableModelPhotos()
        {
            var photos=FacilityCatalog.Offers.Select(f=>FacilityThumbnails.Get(f.Kind)).ToArray();
            Assert.That(FacilityThumbnails.Restroom,Is.Not.Null);
            Assert.That(photos,Has.All.Not.Null);
            Assert.That(photos.Distinct().Count(),Is.EqualTo(FacilityCatalog.Offers.Count));
            foreach(var offer in FacilityCatalog.Offers)
                Assert.That(FacilityThumbnails.Get(offer.Kind),Is.SameAs(FacilityThumbnails.Get(offer.Kind)));
        }

        [UnityTest]
        public IEnumerator ExistingFacilitiesCanBeSelectedMovedCancelledAndRestored()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");yield return new EnterPlayMode();
            var layout=Object.FindFirstObjectByType<FacilityLayout>();
            Object.FindFirstObjectByType<SessionGoalTracker>().Restore(2,0,0);
            layout.Wallet.RestoreProgress(5000,0);layout.Discover();
            var hud=Object.FindFirstObjectByType<FacilityShopHud>();
            var original=layout.Instances.Single(f=>f.Id=="grill-main");
            var grill=original.GetComponentInChildren<ExpandableGrill>();grill.Upgrade.RestoreLevel(2);grill.Station.Advance(6);
            int stock=grill.Station.Stock;var pose=original.transform.position;var pickupLocal=original.transform.InverseTransformPoint(grill.Pickup.PickupPosition);
            int total=layout.Instances.Count;long money=layout.Wallet.Coins;
            var renderers=original.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled&&r.GetComponent<MeshFilter>()!=null).ToArray();
            hud.Open();Click("MoveExisting");
            Assert.That(hud.IsOwnedPage,Is.True);Assert.That(GameObject.Find("Move_grill-main"),Is.Not.Null);
            Assert.That(GameObject.Find("Move_grill-extra"),Is.Null,"Unbuilt original facilities are not owned cards");
            Assert.That(layout.Candidate,Is.Null);Assert.That(layout.Instances.Count,Is.EqualTo(total));Assert.That(layout.Wallet.Coins,Is.EqualTo(money));
            SelectOwnedForMove("grill-main");Assert.That(layout.Moving,Is.SameAs(original));
            Assert.That(renderers.All(r=>!r.enabled),Is.True,"Moving previews must not leave a second original model visible");
            hud.MovePreviewPointer(Camera.main.WorldToScreenPoint(new Vector3(7,0,-5)),true,true,false);
            Click("RotateRight");Click("Cancel");
            Assert.That(original.transform.position,Is.EqualTo(pose));Assert.That(renderers.All(r=>r.enabled),Is.True);
            Assert.That(layout.Moving,Is.Null);Assert.That(hud.IsOwnedPage,Is.True);
            SelectOwnedForMove("grill-main");
            var destination=new Vector3(7,0,-5);
            hud.MovePreviewPointer(Camera.main.WorldToScreenPoint(destination),true,true,false);
            hud.MovePreviewPointer(Vector2.zero,false,false,false);
            Click("RotateRight");
            var frozen=hud.PreviewPosition;
            hud.MovePreviewPointer(Vector2.zero,true,true,true);
            Assert.That(hud.PreviewPosition,Is.EqualTo(frozen));
            Click("Done");
            Assert.That(layout.Moving,Is.Null,layout.LastError);Assert.That(Time.timeScale,Is.EqualTo(1));
            Assert.That(Vector3.Distance(original.transform.position,destination),Is.LessThan(.01f));
            Assert.That(original.transform.eulerAngles.y,Is.EqualTo(15).Within(.01f));
            Assert.That(grill.Upgrade.Level,Is.EqualTo(2));Assert.That(grill.Station.Stock,Is.EqualTo(stock));
            Assert.That(Vector3.Distance(grill.Pickup.PickupPosition,original.transform.TransformPoint(pickupLocal)),Is.LessThan(.01f));
            Assert.That(renderers.All(r=>r.enabled),Is.True);Assert.That(layout.Wallet.Coins,Is.EqualTo(money));
            Assert.That(layout.Instances.Count,Is.EqualTo(total));
            Assert.That(Object.FindFirstObjectByType<RestaurantPersistence>().Flush(),Is.True);
            Assert.That(new LocalSaveStore(SaveDirectory).Load(out var data),Is.EqualTo(SaveLoadResult.Loaded));
            Assert.That(data.layout.Single(r=>r.id=="grill-main").yaw,Is.EqualTo(15).Within(.01f));
            yield return new ExitPlayMode();
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");yield return new EnterPlayMode();
            layout=Object.FindFirstObjectByType<FacilityLayout>();
            original=layout.Instances.Single(f=>f.Id=="grill-main");
            Assert.That(Vector3.Distance(original.transform.position,new Vector3(7,0,-5)),Is.LessThan(.01f));
            Assert.That(original.transform.eulerAngles.y,Is.EqualTo(15).Within(.01f));
            Assert.That(original.GetComponentInChildren<ExpandableGrill>().Upgrade.Level,Is.EqualTo(2));
            LogAssert.NoUnexpectedReceived();yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator CatalogRendersPhotosAndOwnedCardsAtPhoneAndDesktopSizes()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");yield return new EnterPlayMode();
            var hud=Object.FindFirstObjectByType<FacilityShopHud>();var layout=Object.FindFirstObjectByType<FacilityLayout>();
            Object.FindFirstObjectByType<SessionGoalTracker>().Restore(10,0,0);layout.Wallet.RestoreProgress(0,0);layout.Wallet.CollectCoins(5000);layout.Discover();
            hud.Open();
            Assert.That(GameObject.Find("PlacementControls"),Is.Null,"Catalog and placement controls must not overlap");
            Assert.That(GameObject.Find("Buy_BurgerMachine").transform.Find("PhotoBackground/ProductPhoto").GetComponent<Image>().sprite,Is.Not.Null);
            var canvas=hud.GetComponentInParent<Canvas>();var camera=Camera.main;
            // Screen-overlay UI normally renders after post processing. The offscreen
            // camera-mode capture must also avoid tone-mapping the UI's authored colors.
            var cameraData=camera.GetComponents<Component>().First(c=>c.GetType().Name=="UniversalAdditionalCameraData");
            var serialized=new UnityEditor.SerializedObject(cameraData);
            serialized.FindProperty("m_RenderPostProcessing").boolValue=false;serialized.ApplyModifiedPropertiesWithoutUndo();
            canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
            var target=new RenderTexture(1080,1920,24);camera.targetTexture=target;
            yield return null;Canvas.ForceUpdateCanvases();hud.SendMessage("FitCatalog");Canvas.ForceUpdateCanvases();
            CheckCards(hud);Capture(camera,target,"/tmp/burgershop-supermarket-phone.png");
            Click("MoveExisting");Canvas.ForceUpdateCanvases();CheckCards(hud);
            Assert.That(GameObject.Find("BuyRestroom"),Is.Null,"Fixed room expansions do not belong in movable facilities");
            Capture(camera,target,"/tmp/burgershop-owned-phone.png");
            camera.targetTexture=null;Object.Destroy(target);
            target=new RenderTexture(1600,1000,24);camera.targetTexture=target;
            yield return null;Canvas.ForceUpdateCanvases();hud.SendMessage("FitCatalog");Canvas.ForceUpdateCanvases();CheckCards(hud);
            Capture(camera,target,"/tmp/burgershop-owned-desktop.png");
            SelectOwnedForMove("grill-main");
            hud.MovePreviewPointer(camera.WorldToScreenPoint(new Vector3(7,0,-5)),true,true,false);
            hud.MovePreviewPointer(Vector2.zero,false,false,false);hud.SendMessage("LateUpdate");Canvas.ForceUpdateCanvases();
            Capture(camera,target,"/tmp/burgershop-move-existing.png");
            camera.targetTexture=null;Object.Destroy(target);hud.Close();
            LogAssert.NoUnexpectedReceived();yield return new ExitPlayMode();
        }
        static void CheckCards(FacilityShopHud hud)
        {
            var panel=(RectTransform)hud.transform.Find("Catalog");var space=(RectTransform)hud.transform;
            var bounds=HudChrome.LocalRect(panel,space);
            Assert.That(bounds.xMin,Is.GreaterThanOrEqualTo(0));Assert.That(bounds.xMax,Is.LessThanOrEqualTo(space.rect.width));
            Assert.That(bounds.yMin,Is.GreaterThanOrEqualTo(0));Assert.That(bounds.yMax,Is.LessThanOrEqualTo(space.rect.height));
            foreach(var image in panel.GetComponentsInChildren<Image>().Where(i=>i.name=="ProductPhoto"))Assert.That(image.sprite,Is.Not.Null);
        }
        static void SelectOwnedForMove(string id)
        {
            Click("Move_"+id);
            Assert.That(FacilityDetailsHud.Current.IsOpen,Is.True);
            Assert.That(FacilityDetailsHud.Current.Selected.Id,Is.EqualTo(id));
            Assert.That(Time.timeScale,Is.Zero,"Opening details from the catalog keeps business paused");
            Click("MoveFacility");
            Assert.That(FacilityDetailsHud.Current.IsOpen,Is.False);
            Assert.That(Time.timeScale,Is.Zero,"Moving from details keeps business paused");
        }
        static void Click(string name)=>GameObject.Find(name).GetComponent<Button>().onClick.Invoke();
        static void Capture(Camera camera,RenderTexture target,string path)
        {
            camera.Render();var old=RenderTexture.active;RenderTexture.active=target;
            var texture=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
            texture.ReadPixels(new Rect(0,0,target.width,target.height),0,0);texture.Apply();System.IO.File.WriteAllBytes(path,texture.EncodeToPNG());
            RenderTexture.active=old;Object.Destroy(texture);
        }
        [UnityTearDown] public IEnumerator LeavePlay(){if(Application.isPlaying)yield return new ExitPlayMode();}
    }
}
