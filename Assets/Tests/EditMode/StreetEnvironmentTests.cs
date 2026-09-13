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
        [Test] public void StreetPickupPositionRemainsSaveable()
        {
            var row=new BurgerShop.Building.FacilityPlacementRecord{id="courier-tray",kind=(int)BurgerShop.Building.FacilityKind.CourierTray,x=2,z=38,yaw=0,level=1};
            Assert.That(row.IsValid,Is.True);row.z=41;Assert.That(row.IsValid,Is.False);
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
        [UnityTest] public IEnumerator StreetSceneRendersWithoutAddingWalkableOrBuildableLand()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");yield return new EnterPlayMode();yield return null;
            Object.FindFirstObjectByType<BurgerShop.UI.SessionGoalTracker>().Restore(10,0,0);
            yield return null;
            var queue=MainKitchen<BurgerShop.Customer.CustomerQueue>();
            var path=(Vector3[])typeof(BurgerShop.Customer.CustomerQueue).GetField("route",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(queue);
            Assert.That(Vector3.Distance(queue.transform.TransformPoint(path[0]),RestaurantEntrance.Outside),Is.LessThan(.01f));
            Assert.That(Vector3.Distance(queue.transform.TransformPoint(path[1]),RestaurantEntrance.Door),Is.LessThan(.01f));
            Assert.That(GameObject.Find("RestaurantEntrance"),Is.Not.Null);
            var courier=Object.FindFirstObjectByType<BurgerShop.Restaurant.CourierLine>();
            Assert.That(courier.RiderRoot.Find("ExpressRoad").GetComponentsInChildren<MeshRenderer>().Length,Is.LessThan(8),"Shared street must not recreate the old loop surface");
            Assert.That(BurgerShop.Restaurant.CourierRoad.FullPath.Length,Is.EqualTo(2));
            Assert.That(BurgerShop.Restaurant.CourierLine.Stop.z,Is.InRange(39f,47f));
            var street=GameObject.Find("StreetEnvironment");Assert.That(street,Is.Not.Null);
            Assert.That(street.GetComponentsInChildren<Collider>().Length,Is.Zero);
            var camera=Camera.main;var output=new RenderTexture(1280,800,24);camera.targetTexture=output;
            camera.transform.position=new Vector3(-20,26,-25);camera.transform.LookAt(new Vector3(0,0,5));
            camera.transform.position=StreetEnvironment.ClampCamera(camera,camera.transform.position);
            camera.Render();RenderTexture.active=output;
            var texture=new Texture2D(1280,800,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1280,800),0,0);texture.Apply();
            System.IO.File.WriteAllBytes("/tmp/burgershop-street.png",texture.EncodeToPNG());
            RenderTexture.active=null;camera.targetTexture=null;Object.Destroy(texture);Object.Destroy(output);
            LogAssert.NoUnexpectedReceived();yield return new ExitPlayMode();
        }
    }
}
