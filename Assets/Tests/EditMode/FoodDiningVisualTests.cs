using System.Collections;
using BurgerShop.Core;
using BurgerShop.Restaurant;
using BurgerShop.Player;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace BurgerShop.Tests.EditMode
{
    public sealed class FoodDiningVisualTests : SaveIsolatedGameplayTest
    {
        [Test]
        public void BurgersFitTheExistingStackAndTransferWithoutChangingTheirGeometry()
        {
            var root = new GameObject("FoodVisualTest");
            try
            {
                var output = new GameObject("Output").transform; output.SetParent(root.transform, false);
                var station = root.AddComponent<ProductionStation>(); station.Configure(output, null, null);
                station.Advance(6);
                Assert.That(station.Stock, Is.EqualTo(2));
                var first = output.GetChild(0); var second = output.GetChild(1);
                Bounds a = BoundsOf(first), b = BoundsOf(second);
                Assert.That(a.min.y, Is.EqualTo(0).Within(.001f));
                Assert.That(a.max.y, Is.LessThan(b.min.y), "Consecutive burgers must not overlap at the existing .34 spacing");
                Assert.That(a.size.x, Is.LessThanOrEqualTo(.65f));
                Assert.That(first.GetComponentsInChildren<Collider>(), Is.Empty);
                Mesh bun = first.Find("TopBun").GetComponent<MeshFilter>().sharedMesh;
                Assert.That(second.Find("TopBun").GetComponent<MeshFilter>().sharedMesh, Is.SameAs(bun));
                var bag = root.AddComponent<BurgerInventory>(); bag.Configure(4);
                Assert.That(bag.TryCollectFrom(station), Is.True);
                Assert.That(bag.Count, Is.EqualTo(1));
                Assert.That(station.Stock, Is.EqualTo(1));
                Assert.That(bag.TryTakeBurger(out Transform carried), Is.True);
                Assert.That(carried.Find("TopBun").GetComponent<MeshFilter>().sharedMesh, Is.SameAs(bun));
                Object.DestroyImmediate(carried.gameObject);
                Assert.That(bun != null, Is.True, "Shared geometry must outlive a consumed burger");
            }
            finally { Object.DestroyImmediate(root); }
        }

        [UnityTest]
        public IEnumerator DiningStylesAndFoodRenderInUnity()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity"); yield return new EnterPlayMode();
            var gallery = new GameObject("FoodDiningGallery").transform;
            RestaurantStyle.Block(gallery,"Floor",new Vector3(100,-.12f,0),new Vector3(20,.2f,20),RuntimeMaterials.Create(RestaurantStyle.Cream));
            for (int style=0; style<4; style++) for (int kind=0; kind<3; kind++)
            {
                var table=DiningTable.Create(gallery,new Vector3(94.8f+kind*5,0,5.4f-style*3.6f),(DiningTableKind)kind);
                var oldSeat=table.SeatPosition(0)-table.Center;
                table.ApplySet((TableSetId)style);
                table.transform.rotation=Quaternion.Euler(0,90,0);
                Assert.That(Vector3.Distance(table.SeatPosition(0),table.Center+table.transform.rotation*oldSeat),Is.LessThan(.001f));
                table.transform.rotation=Quaternion.identity;
                Assert.That(table.transform.Find("Top").GetComponent<BoxCollider>().size,Is.EqualTo(Vector3.one));
            }
            var camera=Camera.main; camera.GetComponent<CameraFollow>().enabled=false;
            camera.orthographic=true; camera.orthographicSize=9.3f;
            camera.transform.position=new Vector3(112,19,-19); camera.transform.LookAt(new Vector3(100,.3f,0));
            yield return null;
            Capture(camera,"/tmp/burgershop-tables.png");
            camera.orthographicSize=4.6f;camera.transform.position=new Vector3(104,8,-14);camera.transform.LookAt(new Vector3(99.8f,.55f,-5.4f));
            Capture(camera,"/tmp/burgershop-tables-detail.png");

            RestaurantStyle.Block(gallery,"FoodDisplay",new Vector3(130,-.06f,0),new Vector3(8,.1f,8),RuntimeMaterials.Create(RestaurantStyle.Cream));
            var output=new GameObject("BurgerStack").transform;output.SetParent(gallery,false);output.position=new Vector3(130.6f,0,0);
            var station=output.gameObject.AddComponent<ProductionStation>();station.Configure(output,null,null);station.Advance(9);station.enabled=false;
            var single=new GameObject("SingleBurger").transform;single.SetParent(gallery,false);single.position=new Vector3(129.5f,0,-.3f);
            var one=single.gameObject.AddComponent<ProductionStation>();one.Configure(single,null,null);one.Advance(3);one.enabled=false;
            camera.orthographicSize=1.25f;camera.transform.position=new Vector3(131.8f,2.3f,-3.5f);camera.transform.LookAt(new Vector3(130,.36f,0));
            Capture(camera,"/tmp/burgershop-burgers.png");
            LogAssert.NoUnexpectedReceived();yield return new ExitPlayMode();
        }

        static Bounds BoundsOf(Transform root)
        {
            var renderers=root.GetComponentsInChildren<Renderer>();var result=renderers[0].bounds;
            foreach(var renderer in renderers)result.Encapsulate(renderer.bounds);
            return result;
        }

        static void Capture(Camera camera,string path)
        {
            var target=new RenderTexture(1600,1100,24);camera.targetTexture=target;RenderTexture previous=RenderTexture.active;
            camera.Render();RenderTexture.active=target;
            var texture=new Texture2D(1600,1100,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1600,1100),0,0);texture.Apply();
            System.IO.File.WriteAllBytes(path,texture.EncodeToPNG());RenderTexture.active=previous;camera.targetTexture=null;Object.Destroy(texture);Object.Destroy(target);
        }
    }
}
