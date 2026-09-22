using System.Collections;
using System.Linq;
using BurgerShop.Core;
using BurgerShop.Customer;
using BurgerShop.Player;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace BurgerShop.Tests.EditMode
{
    public sealed class Spec083BigEaterTests : SaveIsolatedGameplayTest
    {
        [Test]
        public void EligibleRollsKeepSevenPercentBigEatersAndSpacingBlocksBursts()
        {
            int big=0,calling=0,normal=0;
            for(int i=0;i<100;i++)
            {
                var kind=new SpecialCustomerPolicy().Next(true,false,(i+.5f)/100f);
                if(kind==CustomerKind.BigEater)big++;else if(kind==CustomerKind.Calling)calling++;else normal++;
            }
            Assert.That(big,Is.EqualTo(7));Assert.That(calling,Is.EqualTo(8));Assert.That(normal,Is.EqualTo(85));
            var policy=new SpecialCustomerPolicy();
            Assert.That(policy.Next(false,false,.99f),Is.EqualTo(CustomerKind.Normal));
            Assert.That(policy.Next(true,true,.99f),Is.EqualTo(CustomerKind.Normal));
            Assert.That(policy.Next(true,false,.99f),Is.EqualTo(CustomerKind.BigEater));
            for(int i=0;i<3;i++)Assert.That(policy.Next(true,false,.99f),Is.EqualTo(CustomerKind.Normal));
            Assert.That(policy.Next(true,true,.99f),Is.EqualTo(CustomerKind.Normal));
            Assert.That(policy.Next(true,false,.99f),Is.EqualTo(CustomerKind.BigEater));
        }

        [Test]
        public void DedicatedSilhouetteLeavesQueueRootAndCollisionsUnchanged()
        {
            var root=new GameObject("BigEaterTest");
            try
            {
                var normal=CustomerAgent.Create(root.transform,1,Vector3.zero);
                var big=CustomerAgent.Create(root.transform,1,Vector3.right,kind:CustomerKind.BigEater);
                Assert.That(big.transform.Find("RoundBelly"),Is.Not.Null);
                Assert.That(big.transform.Find("Suspender"),Is.Not.Null);
                Assert.That(big.transform.Find("BurgerPrintTop"),Is.Not.Null);
                Assert.That(big.transform.Find("Apron"),Is.Null);
                Assert.That(normal.transform.Find("RoundBelly"),Is.Null);
                Assert.That(big.transform.Find("Body").localScale.x,Is.GreaterThan(normal.transform.Find("Body").localScale.x*1.3f));
                Assert.That(big.transform.localScale,Is.EqualTo(Vector3.one));
                Assert.That(big.transform.position,Is.EqualTo(Vector3.right));
                Assert.That(big.GetComponentsInChildren<Collider>(),Is.Empty);
                Assert.That(big.OrderSize,Is.EqualTo(10));
                Assert.That(big.GetComponentsInChildren<Renderer>().Count(r=>r.enabled),Is.LessThanOrEqualTo(24));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [UnityTest]
        public IEnumerator RenderBigEaterBesideRegularCustomers()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");yield return new EnterPlayMode();
            var gallery=new GameObject("BigOrderPreview").transform;
            var mat=RuntimeMaterials.Create(CharacterAppearance.Hex(0xD6DDD2));
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.SetParent(gallery,false);
            floor.transform.position=new Vector3(100,-.14f,0);floor.transform.localScale=new Vector3(20,.2f,20);floor.GetComponent<Renderer>().sharedMaterial=mat;
            for(int i=0;i<3;i++)
            {
                var person=CustomerAgent.Create(gallery,i+1,new Vector3(103-i*3,0,0),kind:i==1?CustomerKind.BigEater:CustomerKind.Normal);
                person.enabled=false;person.transform.rotation=Quaternion.Euler(0,-20,0);
            }
            var camera=Camera.main;camera.GetComponent<CameraFollow>().enabled=false;
            camera.orthographic=true;camera.orthographicSize=2.7f;camera.transform.position=new Vector3(100,5.5f,10);camera.transform.LookAt(new Vector3(100,1,0));
            var sun=Object.FindObjectsByType<Light>(FindObjectsSortMode.None).First(l=>l.type==LightType.Directional);
            sun.transform.rotation=Quaternion.Euler(48,155,0);sun.intensity=1.4f;
            yield return null;
            var target=new RenderTexture(1400,850,24);var previous=RenderTexture.active;
            var texture=new Texture2D(1400,850,TextureFormat.RGB24,false);
            camera.targetTexture=target;camera.Render();RenderTexture.active=target;
            texture.ReadPixels(new Rect(0,0,1400,850),0,0);texture.Apply();
            System.IO.File.WriteAllBytes("/tmp/burgershop-083-big-eater.png",texture.EncodeToPNG());
            RenderTexture.active=previous;camera.targetTexture=null;Object.Destroy(texture);Object.Destroy(target);Object.Destroy(mat);
            LogAssert.NoUnexpectedReceived();yield return new ExitPlayMode();
        }
    }
}
