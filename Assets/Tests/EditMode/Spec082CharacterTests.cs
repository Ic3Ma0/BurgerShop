using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BurgerShop.Core;
using BurgerShop.Customer;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace BurgerShop.Tests.EditMode
{
    public sealed class Spec082CharacterTests : SaveIsolatedGameplayTest
    {
        [Test]
        public void CustomerWardrobeHasTwelveDistinctStableOutfitsWithoutUsingGameplayRandom()
        {
            var original=Random.state;
            var root=new GameObject("CastTest");
            try
            {
                Random.InitState(482);var state=Random.state;
                var signatures=new HashSet<string>();
                for(int ticket=1;ticket<=12;ticket++)
                {
                    var look=CharacterAppearance.Customer(ticket);
                    signatures.Add(look.Name+look.HairStyle+look.Accessory);
                    var actor=new GameObject("Actor").transform;actor.SetParent(root.transform,false);
                    CharacterVisualFactory.Customer(actor,ticket);
                    Assert.That(actor.GetComponentsInChildren<Collider>().Length,Is.Zero);
                    Assert.That(actor.Find("Eye"),Is.Not.Null);
                    Assert.That(actor.Find("NameBadge"),Is.Null);
                    Assert.That(actor.GetComponentsInChildren<Renderer>().Count(r=>r.enabled),Is.LessThanOrEqualTo(24));
                    Assert.That(CharacterAppearance.Customer(ticket+12).Name,Is.EqualTo(look.Name));
                    Assert.That(CharacterAppearance.SkinIndex(ticket+12),Is.Not.EqualTo(CharacterAppearance.SkinIndex(ticket)));
                }
                Assert.That(signatures.Count,Is.EqualTo(12));
                Assert.That(JsonUtility.ToJson(Random.state),Is.EqualTo(JsonUtility.ToJson(state)));
            }
            finally { Object.DestroyImmediate(root);Random.state=original; }
        }

        [Test]
        public void SpecialCustomersKeepTheirOrdersAndHaveMatchingVisuals()
        {
            var root=new GameObject("SpecialCustomers");
            try
            {
                var eater=CustomerAgent.Create(root.transform,1,Vector3.zero,kind:CustomerKind.BigEater);
                var caller=CustomerAgent.Create(root.transform,2,Vector3.zero,kind:CustomerKind.Calling);
                Assert.That(eater.transform.Find("Body").localScale.x,Is.GreaterThan(.8f));
                Assert.That(caller.transform.Find("Phone"),Is.Not.Null);
                Assert.That(Quaternion.Angle(caller.transform.Find("RightArm").localRotation,Quaternion.Euler(-150,0,0)),Is.LessThan(.1f));
                Assert.That(eater.Kind,Is.EqualTo(CustomerKind.BigEater));
                Assert.That(caller.Kind,Is.EqualTo(CustomerKind.Calling));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void VisualAssetsAreReleasedAndRootTransformStaysOwnedByGameplay()
        {
            var root=new GameObject("Character");
            root.transform.position=new Vector3(4,1.05f,6);root.transform.rotation=Quaternion.Euler(0,75,0);root.transform.localScale=Vector3.one*.88f;
            CharacterVisualFactory.Staff(root.transform,1);
            Assert.That(root.transform.position,Is.EqualTo(new Vector3(4,1.05f,6)));
            Assert.That(root.transform.localScale,Is.EqualTo(Vector3.one*.88f));
            Assert.That(Quaternion.Angle(root.transform.rotation,Quaternion.Euler(0,75,0)),Is.LessThan(.01f));
            Assert.That(root.transform.Find("NameBadge"),Is.Not.Null);
            Assert.That(root.transform.Find("Hair"),Is.Not.Null);
            Assert.That(root.transform.Find("Hat"),Is.Null);
            var materials=root.GetComponentsInChildren<Renderer>().Select(r=>r.sharedMaterial).Distinct().ToArray();
            var meshes=root.GetComponentsInChildren<MeshFilter>().Where(f=>f.name=="CharacterSurface").Select(f=>f.sharedMesh).ToArray();
            Object.DestroyImmediate(root);
            Assert.That(materials.All(m=>m==null),Is.True);
            Assert.That(meshes.Length,Is.GreaterThan(0));
            Assert.That(meshes.All(m=>m==null),Is.True);
        }

        [UnityTest]
        public IEnumerator SampleSceneAndCastGalleryUseNewPeople()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");yield return new EnterPlayMode();
            var player=Object.FindFirstObjectByType<PlayerMotor>();
            Assert.That(player.GetComponent<CharacterVisualResources>(),Is.Not.Null);
            Assert.That(player.transform.Find("ChefHat"),Is.Not.Null);
            Assert.That(player.GetComponent<CharacterController>().radius,Is.EqualTo(.4f));
            var camera=Camera.main;camera.GetComponent<CameraFollow>().enabled=false;
            Capture(camera,"/tmp/burgershop-082-scene.png",1600,1000);
            var gallery=new GameObject("CharacterGallery").transform;
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.SetParent(gallery,false);
            floor.transform.position=new Vector3(100,-.16f,0);floor.transform.localScale=new Vector3(22,.25f,24);
            var floorMat=RuntimeMaterials.Create(CharacterAppearance.Hex(0xC9DDD7));floor.GetComponent<Renderer>().sharedMaterial=floorMat;
            player.enabled=false;player.GetComponent<CharacterController>().enabled=false;
            player.transform.position=new Vector3(95.5f,1,6);player.transform.rotation=Quaternion.Euler(0,-15,0);
            for(int slot=0;slot<3;slot++)
            {
                var worker=RestaurantWorker.Create(gallery,new Vector3(98.5f+slot*3,0,6),null,null,null,Vector3.zero,null,slot:slot);
                worker.enabled=false;worker.transform.rotation=Quaternion.Euler(0,-15,0);
                Assert.That(worker.GetComponent<CharacterVisualResources>(),Is.Not.Null);
            }
            for(int ticket=1;ticket<=12;ticket++)
            {
                int i=ticket-1;
                var customer=CustomerAgent.Create(gallery,ticket,new Vector3(95.5f+(i%4)*3,0,2-(i/4)*4));
                customer.enabled=false;customer.transform.rotation=Quaternion.Euler(0,-15,0);
                Assert.That(customer.GetComponent<CharacterVisualResources>(),Is.Not.Null);
            }
            foreach(var label in gallery.GetComponentsInChildren<TextMesh>())label.GetComponent<Renderer>().enabled=false;
            var sun=Object.FindObjectsByType<Light>(FindObjectsSortMode.None).First(l=>l.type==LightType.Directional);
            sun.transform.rotation=Quaternion.Euler(48,155,0);sun.intensity=1.4f;
            camera.orthographic=true;camera.orthographicSize=7.2f;camera.transform.position=new Vector3(100,13,22);camera.transform.LookAt(new Vector3(100,.6f,0));
            var walking=gallery.GetComponentsInChildren<CustomerAgent>()[0].transform;
            for(int frame=0;frame<6;frame++){walking.position+=Vector3.right*.06f;yield return null;}
            Assert.That(Quaternion.Angle(walking.Find("LeftLeg").localRotation,Quaternion.identity),Is.GreaterThan(1));
            // Let the display return to its standing pose before recording the lineup.
            for(int frame=0;frame<25;frame++)yield return null;
            yield return null;
            Capture(camera,"/tmp/burgershop-082-cast.png",1600,1400);
            foreach(var customer in gallery.GetComponentsInChildren<CustomerAgent>())customer.gameObject.SetActive(false);
            camera.orthographicSize=3.6f;camera.transform.position=new Vector3(100,5.3f,14);camera.transform.LookAt(new Vector3(100,1,6));
            Capture(camera,"/tmp/burgershop-082-team.png",1800,900);
            Object.Destroy(floorMat);
            LogAssert.NoUnexpectedReceived();yield return new ExitPlayMode();
        }

        static void Capture(Camera camera,string path,int width,int height)
        {
            var target=new RenderTexture(width,height,24);var previousTarget=camera.targetTexture;var previousActive=RenderTexture.active;
            var texture=new Texture2D(width,height,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture=target;camera.Render();RenderTexture.active=target;
                texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();
                System.IO.File.WriteAllBytes(path,texture.EncodeToPNG());
            }
            finally { camera.targetTexture=previousTarget;RenderTexture.active=previousActive;Object.Destroy(texture);Object.Destroy(target); }
        }
    }
}
