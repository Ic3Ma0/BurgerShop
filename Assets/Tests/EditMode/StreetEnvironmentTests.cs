using System.Collections;
using BurgerShop.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor.SceneManagement;

namespace BurgerShop.Tests.EditMode
{
    public sealed class StreetEnvironmentTests : SaveIsolatedGameplayTest
    {
        [UnityTest] public IEnumerator ExteriorSeamsSupportPlayerAndBoundaryStopsEscape()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");yield return new EnterPlayMode();
            var player=Object.FindFirstObjectByType<BurgerShop.Player.PlayerMotor>();player.enabled=false;
            var controller=player.GetComponent<CharacterController>();
            float previous=Time.captureDeltaTime;Time.captureDeltaTime=1f/60f;
            // Walk off the entrance, between pavement/grass, and onto both public roads.
            foreach(var start in new[]{new Vector3(-12,1,-20),new Vector3(-7,1,-21),new Vector3(11,1,-27),new Vector3(2,1,39)})
            {
                controller.enabled=false;player.transform.position=start;controller.enabled=true;
                for(int i=0;i<120;i++){controller.Move((Vector3.back*3+Vector3.down*4)/60f);yield return null;}
                Assert.That(player.transform.position.y,Is.GreaterThan(.5f),"Fell at "+start);
                Assert.That(controller.isGrounded,Is.True,"Not grounded after crossing "+start+" at "+player.transform.position);
            }
            var bounds=StreetEnvironment.WalkBounds;
            foreach(var direction in new[]{Vector3.left,Vector3.right,Vector3.back,Vector3.forward})
            {
                var start=new Vector3(direction.x<0?bounds.xMin+2:direction.x>0?bounds.xMax-2:0,1,
                    direction.z<0?bounds.yMin+2:direction.z>0?bounds.yMax-2:0);
                controller.enabled=false;player.transform.position=start;controller.enabled=true;
                for(int i=0;i<120;i++){controller.Move((direction*5+Vector3.down*4)/60f);yield return null;}
                Assert.That(bounds.Contains(new Vector2(player.transform.position.x,player.transform.position.z)),Is.True);
                Assert.That(player.transform.position.y,Is.GreaterThan(.5f));
            }
            Time.captureDeltaTime=previous;yield return new ExitPlayMode();
        }
        [UnityTest] public IEnumerator NavigationStillConnectsHallAfterPurchase()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");yield return new EnterPlayMode();yield return null;
            var layout=Object.FindFirstObjectByType<BurgerShop.Building.FacilityLayout>();
            var hud=Object.FindFirstObjectByType<BurgerShop.Building.FacilityShopHud>();
            int revision=layout.Revision;hud.Open();Assert.That(Time.timeScale,Is.Zero);
            hud.SendMessage("Done");yield return null;yield return null;
            Assert.That(Time.timeScale,Is.GreaterThan(0));Assert.That(layout.Editing,Is.False);
            Assert.That(layout.Revision,Is.GreaterThan(revision));
            Assert.That(layout.Route(Vector3.zero,new Vector3(-10,0,0)),Is.Not.Null,"Hall path must remain available");
            yield return new ExitPlayMode();
        }
        [Test] public void StreetPickupPositionRemainsSaveable()
        {
            var row=new BurgerShop.Building.FacilityPlacementRecord{id="courier-tray",kind=(int)BurgerShop.Building.FacilityKind.CourierTray,x=2,z=38,yaw=0,level=1};
            Assert.That(row.IsValid,Is.True);row.z=41;Assert.That(row.IsValid,Is.False);
        }
        [UnityTest] public IEnumerator DoubleDoorsOpenBothWaysAndSettleClosed()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");yield return new EnterPlayMode();
            float previous=Time.captureDeltaTime;Time.captureDeltaTime=1f/60f;
            var player=Object.FindFirstObjectByType<BurgerShop.Player.PlayerMotor>();player.enabled=false;
            var hinge=GameObject.Find("RestaurantEntrance").transform.Find("DoorHinge");
            player.transform.position=RestaurantEntrance.Door+Vector3.back;
            for(int i=0;i<45;i++)yield return null;
            Assert.That(Mathf.DeltaAngle(0,hinge.localEulerAngles.y),Is.LessThan(-70));
            player.transform.position=Vector3.zero;
            for(int i=0;i<60;i++)yield return null;
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(0,hinge.localEulerAngles.y)),Is.LessThan(3));
            player.transform.position=RestaurantEntrance.Door+Vector3.forward;
            for(int i=0;i<45;i++)yield return null;
            Assert.That(Mathf.DeltaAngle(0,hinge.localEulerAngles.y),Is.GreaterThan(70));
            Time.captureDeltaTime=previous;yield return new ExitPlayMode();
        }
        [TestCase(.46f)][TestCase(1.78f)][TestCase(2.4f)]
        public void CameraCornersStayOnFiniteGroundWhenPanningToEdges(float aspect)
        {
            var go=new GameObject("BoundaryCamera",typeof(Camera));
            try
            {
                var camera=go.GetComponent<Camera>();camera.aspect=aspect;go.transform.position=new Vector3(0,13,-16);
                go.transform.LookAt(Vector3.up*1.1f);
                foreach(var direction in new[]{new Vector3(-1000,13,-1000),new Vector3(1000,13,1000),new Vector3(-1000,13,1000),new Vector3(1000,13,-1000)})
                {
                    go.transform.position=StreetEnvironment.ClampCamera(camera,direction);
                    for(int i=0;i<4;i++)
                    {
                        var ray=camera.ViewportPointToRay(new Vector3(i%2,i/2,0));
                        Assert.That(new Plane(Vector3.up,new Vector3(0,StreetEnvironment.GroundHeight,0)).Raycast(ray,out float distance),Is.True);
                        var p=ray.GetPoint(distance);var bounds=StreetEnvironment.GroundBounds;
                        Assert.That(p.x,Is.InRange(bounds.xMin-.01f,bounds.xMax+.01f));
                        Assert.That(p.z,Is.InRange(bounds.yMin-.01f,bounds.yMax+.01f));
                    }
                }
            }
            finally{Object.DestroyImmediate(go);}
        }
        [UnityTest] public IEnumerator StreetSceneRendersWithFiniteGroundSupport()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");yield return new EnterPlayMode();yield return null;
            Object.FindFirstObjectByType<BurgerShop.UI.SessionGoalTracker>().Restore(10,0,0);
            Object.FindFirstObjectByType<BurgerShop.Restaurant.ShopExpansion>().Restore(false,false,false,1,true,true);
            yield return null;
            Assert.That(GameObject.Find("EntranceRail"),Is.Null);
            Assert.That(GameObject.Find("WalkEnd"),Is.Null);
            var lane=Object.FindFirstObjectByType<BurgerShop.Restaurant.DriveThruLane>();
            Assert.That(lane.transform.Find("Road"),Is.Null,"Original car sales must use the public street");
            Assert.That(BurgerShop.Restaurant.ShopLayout.DriveThruQueue[0].z,Is.InRange(-34f,-26f));
            var queue=MainKitchen<BurgerShop.Customer.CustomerQueue>();
            var path=(Vector3[])typeof(BurgerShop.Customer.CustomerQueue).GetField("route",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(queue);
            Assert.That(Vector3.Distance(queue.transform.TransformPoint(path[0]),RestaurantEntrance.Outside),Is.LessThan(.01f));
            Assert.That(Vector3.Distance(queue.transform.TransformPoint(path[2]),RestaurantEntrance.Door),Is.LessThan(.01f));
            Assert.That(GameObject.Find("RestaurantEntrance"),Is.Not.Null);
            var courier=Object.FindFirstObjectByType<BurgerShop.Restaurant.CourierLine>();
            Assert.That(courier.RiderRoot.Find("ExpressRoad").GetComponentsInChildren<MeshRenderer>().Length,Is.LessThan(8),"Shared street must not recreate the old loop surface");
            Assert.That(BurgerShop.Restaurant.CourierRoad.FullPath.Length,Is.EqualTo(2));
            Assert.That(BurgerShop.Restaurant.CourierLine.Stop.z,Is.InRange(39f,47f));
            var street=GameObject.Find("StreetEnvironment");Assert.That(street,Is.Not.Null);
            Assert.That(street.transform.Find("StreetGroundSupport").GetComponent<BoxCollider>().enabled,Is.True);
            var camera=Camera.main;var output=new RenderTexture(1280,800,24);camera.targetTexture=output;
            for(int i=0;i<300;i++)lane.Advance(.1f);
            foreach(var mesh in lane.GetComponentsInChildren<MeshRenderer>())
                if(mesh.name.StartsWith("Wheel_"))Assert.That(mesh.bounds.min.y,Is.EqualTo(-.23f).Within(.03f),"Wheel must contact public road");
            Assert.That(GameObject.Find("ServiceWindowLandmarkSill"),Is.Not.Null);
            Assert.That(GameObject.Find("CustomerSidewalk"),Is.Null,"No raised overlapping sidewalk slab");
            camera.transform.position=new Vector3(-20,30,-30);camera.transform.LookAt(new Vector3(0,0,-10));
            camera.transform.position=StreetEnvironment.ClampCamera(camera,camera.transform.position);
            camera.Render();RenderTexture.active=output;
            var texture=new Texture2D(1280,800,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1280,800),0,0);texture.Apply();
            System.IO.File.WriteAllBytes("/tmp/burgershop-street.png",texture.EncodeToPNG());
            var player=Object.FindFirstObjectByType<BurgerShop.Player.PlayerMotor>();player.enabled=false;player.transform.position=new Vector3(0,1,0);
            var person=BurgerShop.Customer.CustomerAgent.Create(null,1,new Vector3(2,0,0));
            var cash=new GameObject("StackPreview").AddComponent<BurgerShop.Restaurant.CashFloor>();cash.Configure(null,null,new Vector3(-2,.04f,0));
            for(int i=0;i<20;i++)cash.DropAtCounter();
            Assert.That(player.transform.Find("LeftLeg"),Is.Not.Null);Assert.That(person.transform.Find("LeftArm"),Is.Not.Null);
            camera.transform.position=new Vector3(-5,7,-7);camera.transform.LookAt(new Vector3(0,.7f,0));camera.Render();
            texture.ReadPixels(new Rect(0,0,1280,800),0,0);texture.Apply();
            System.IO.File.WriteAllBytes("/tmp/burgershop-people-cash.png",texture.EncodeToPNG());
            Object.Destroy(person.gameObject);Object.Destroy(cash.gameObject);
            RenderTexture.active=null;camera.targetTexture=null;Object.Destroy(texture);Object.Destroy(output);
            LogAssert.NoUnexpectedReceived();yield return new ExitPlayMode();
        }
    }
}
