using System.Collections;
using BurgerShop.Core;
using BurgerShop.Restaurant;
using BurgerShop.Customer;
using BurgerShop.Player;
using BurgerShop.Economy;
using BurgerShop.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
namespace BurgerShop.Tests.EditMode
{
    public sealed class RestaurantIdentityTests : SaveIsolatedGameplayTest
    {
        [UnityTest] public IEnumerator EquipmentAndPeopleUseRestaurantSilhouettes()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");yield return new EnterPlayMode();
            var gallery=new GameObject("RestaurantIdentityGallery").transform;
            var wallet=gallery.gameObject.AddComponent<RestaurantWallet>();
            var player=Object.FindFirstObjectByType<PlayerMotor>();player.enabled=false;player.GetComponent<CharacterController>().enabled=false;
            player.transform.position=new Vector3(95,1,-7);player.transform.rotation=Quaternion.Euler(0,180,0);
            var carrier=player.GetComponent<BurgerInventory>();
            Assert.That(player.GetComponent<Renderer>().enabled,Is.False);
            foreach(string limb in new[]{"LeftArm","RightArm","LeftLeg","RightLeg"})Assert.That(player.transform.Find(limb),Is.Not.Null);
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.SetParent(gallery,false);floor.transform.position=new Vector3(100,-.12f,0);floor.transform.localScale=new Vector3(20,.2f,22);floor.GetComponent<Renderer>().sharedMaterial=RuntimeMaterials.Create(RestaurantStyle.Cream);
            for(int level=1;level<=3;level++)
            {
                var grill=ExpandableGrill.CreateStarter(gallery,carrier,wallet);grill.Upgrade.RestoreLevel(level);grill.transform.position=new Vector3(94+5*(level-1),0,5);
                var cola=ExpandableGrill.CreateColaStarter(gallery,carrier,wallet);cola.Upgrade.RestoreLevel(level);cola.transform.position=new Vector3(94+5*(level-1),0,0);
                Assert.That(grill.ActiveLook.Find("GrillLid_"+level),Is.Not.Null);
                Assert.That(cola.ActiveLook.Find("Nozzle_"+(level-1)),Is.Not.Null);
                Assert.That(cola.ActiveLook.Find("GrillTop"),Is.Null);
                Assert.That(grill.ActiveLook.Find("Body").GetComponent<Renderer>().bounds.size.y,Is.LessThan(1.2f));
                grill.Station.Advance(3);cola.Station.Advance(3);
                Assert.That(grill.OutputAnchor.position.y,Is.GreaterThanOrEqualTo(grill.ActiveLook.Find("OutputTray").GetComponent<Renderer>().bounds.max.y));
                var counter=new GameObject("CounterDisplay").transform;counter.SetParent(gallery,false);counter.position=new Vector3(94+5*(level-1),0,-4);
                RestaurantStyle.Block(counter,"CabinetBody",new Vector3(0,.5f,0),new Vector3(3.2f,1,1.4f),RuntimeMaterials.Create(RestaurantStyle.Ink));
                CounterTierVisual.Create(counter,"CounterAppearance",counter.position,3.2f,1.4f,1.05f,level==2?FoodIcon.Box:FoodIcon.Burger,level);
            }
            var customer=CustomerAgent.Create(gallery,1,new Vector3(98,0,-7));customer.enabled=false;
            var worker=RestaurantWorker.Create(gallery,new Vector3(101,0,-7),null,null,null,Vector3.zero,null);worker.enabled=false;
            foreach(var actor in new[]{customer.transform,worker.transform})foreach(string limb in new[]{"LeftArm","RightArm","LeftLeg","RightLeg"})Assert.That(actor.Find(limb),Is.Not.Null);
            var cash=CashPickup.Create(gallery,new Vector3(103,.04f,-7),10,0);
            Assert.That(cash.Visual.Find("BillFace").GetComponent<Renderer>().sharedMaterial.mainTexture,Is.SameAs(BanknoteLook.Texture));
            var icon=FoodIcons.Get(FoodIcon.Coin);Assert.That(icon,Is.Not.Null);
            foreach(var label in gallery.GetComponentsInChildren<TextMesh>())label.GetComponent<Renderer>().enabled=false;
            var camera=Camera.main;camera.GetComponent<CameraFollow>().enabled=false;
            camera.orthographic=true;camera.orthographicSize=10.5f;camera.transform.position=new Vector3(110,18,-19);camera.transform.LookAt(new Vector3(99,.4f,0));
            yield return null;
            Capture(camera,"/tmp/burgershop-identity.png");
            camera.orthographicSize=4.3f;camera.transform.position=new Vector3(102,10,-14);camera.transform.LookAt(new Vector3(99,.7f,-6));
            Capture(camera,"/tmp/burgershop-people-money.png");
            LogAssert.NoUnexpectedReceived();yield return new ExitPlayMode();
        }
        static void Capture(Camera camera,string path)
        {
            var target=new RenderTexture(1600,1100,24);camera.targetTexture=target;camera.Render();RenderTexture.active=target;
            var texture=new Texture2D(1600,1100,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1600,1100),0,0);texture.Apply();
            System.IO.File.WriteAllBytes(path,texture.EncodeToPNG());RenderTexture.active=null;camera.targetTexture=null;Object.Destroy(texture);Object.Destroy(target);
        }
    }
}
