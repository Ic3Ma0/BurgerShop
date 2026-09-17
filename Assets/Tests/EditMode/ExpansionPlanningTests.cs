using System.Collections;
using System.IO;
using System.Linq;
using BurgerShop.Building;
using BurgerShop.Core;
using BurgerShop.Economy;
using BurgerShop.Persistence;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using BurgerShop.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace BurgerShop.Tests.EditMode
{
    public sealed class ExpansionPlanningTests : SaveIsolatedGameplayTest
    {
        [TestCase(FacilityKind.BurgerMachine)] [TestCase(FacilityKind.BurgerCounter)]
        public void ExtraProductionMatchesLevelFourPromise(FacilityKind kind)
        {
            Assert.That(FacilityCatalog.IsUnlocked(kind,3,false),Is.False);
            Assert.That(FacilityCatalog.IsUnlocked(kind,4,false),Is.True);
            Assert.That(FacilityCatalog.IsUnlocked(kind,1,true),Is.True,"Keep legacy access");
        }

        [Test]
        public void PlotDashesWaitForRankAndDressAnnexFloors()
        {
            var root = new GameObject("ExpansionPlots");
            try
            {
                Material wall = RuntimeMaterials.Create(new Color(0.40f, 0.29f, 0.17f));
                Material floor = RuntimeMaterials.Create(new Color(0.72f, 0.70f, 0.62f));
                ShopLayout.CreateFloor(root.transform, floor);
                ShopLayout.CreateWalls(root.transform, wall);
                var office = HrOffice.Create(root.transform, wall, floor);
                office.SetOpen(false);
                var bay = BoostRoom.Create(root.transform, wall, floor);
                bay.SetAnnexOpen(false);
                var goals = root.AddComponent<SessionGoalTracker>();
                var architecture = root.AddComponent<RestaurantArchitecture>();
                architecture.Configure();
                Assert.That(Named(root, "KitchenTiles"), Is.Not.Null);
                Assert.That(Named(root, "DiningTiles"), Is.Not.Null);
                Assert.That(Named(root, "HrFloor"), Is.Not.Null);
                Assert.That(Named(root, "HrFloor").Find("WorkTiles"), Is.Not.Null, "Staff office uses the shared work floor");
                Assert.That(root.transform.Find("HrDoorPlug/ArchitectureSkirting"), Is.Not.Null);
                Assert.That(Named(root, "DrinksPlannedLand").gameObject.activeSelf, Is.False);
                Assert.That(Named(root, "RestroomPlannedLand").gameObject.activeSelf, Is.False);
                Assert.That(Named(root, "TakeawayPlannedLand").gameObject.activeSelf, Is.False);
                goals.Restore(4, 0, 0);
                Assert.That(Named(root, "RestroomPlannedLand").gameObject.activeSelf, Is.True);
                Assert.That(Named(root, "DrinksPlannedLand").gameObject.activeSelf, Is.False);
                Assert.That(Named(root, "TakeawayPlannedLand").gameObject.activeSelf, Is.False);
                goals.Restore(7, 0, 0);
                Assert.That(Named(root, "DrinksPlannedLand").gameObject.activeSelf, Is.True);
                Assert.That(Named(root, "RestroomPlannedLand").gameObject.activeSelf, Is.True);
                goals.Restore(10, 0, 0);
                Assert.That(Named(root, "TakeawayPlannedLand").gameObject.activeSelf, Is.True,
                    "West land outline appears at Lv.10 until the workshop exists");
            }
            finally
            {
                ShopLayout.ResetWingLock();
                Object.DestroyImmediate(root);
            }
        }

        static Transform Named(GameObject root, string name)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == name) return child;
            return null;
        }
        [UnityTest]
        public IEnumerator PlanningPurchasePreservesPosesOwnershipAndRoutes()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            yield return new EnterPlayMode();
            var layout=Object.FindFirstObjectByType<FacilityLayout>();
            var shop=Object.FindFirstObjectByType<FacilityShopHud>();
            var wing=Object.FindFirstObjectByType<ShopExpansion>();
            var architecture=Object.FindFirstObjectByType<RestaurantArchitecture>();
            var goals=Object.FindFirstObjectByType<SessionGoalTracker>();
            var room=Object.FindFirstObjectByType<RestroomExpansion>();
            var wallet=layout.Wallet;
            var motor=Object.FindFirstObjectByType<PlayerMotor>();motor.enabled=false;
            shop.Open();shop.ShowExpansions();
            var drinks=GameObject.Find("Expand_0");
            var restroomCard=GameObject.Find("BuyRestroom");
            var west=GameObject.Find("Expand_2");
            Assert.That(drinks.GetComponent<Button>().interactable,Is.False);
            Assert.That(drinks.transform.Find("Description").GetComponent<Text>().text,Does.Contain("300+200+250=750"));
            Assert.That(drinks.transform.Find("Description").GetComponent<Text>().text,Does.Contain("sold separately"));
            Assert.That(drinks.transform.Find("Detail").GetComponent<Text>().text,Does.Contain("Lv.7"));
            Assert.That(restroomCard.transform.Find("Description").GetComponent<Text>().text,Does.Contain("Complete room 300"));
            Assert.That(restroomCard.transform.Find("Detail").GetComponent<Text>().text,Does.Contain("Lv.4"));
            Assert.That(west.transform.Find("Description").GetComponent<Text>().text,Does.Contain("250+200+300=750"));
            Assert.That(west.transform.Find("Detail").GetComponent<Text>().text,Does.Contain("Lv.10"));
            Assert.That(GameObject.Find("DrinksPlannedLand"),Is.Null,"Rank 1 must not show drinks plot marks");
            Assert.That(wing.TryPurchaseWing(),Is.False,"No bypass of rank requirement");
            Assert.That(layout.Floors().Any(r=>r.Contains(new Vector2(30,6))),Is.False);
            var original=layout.Instances.ToDictionary(f=>f.Id,f=>f.transform.position);
            goals.Restore(7,0,0);            wallet.RestoreProgress(249,0);
            Assert.That(GameObject.Find("DrinksPlannedLand"),Is.Not.Null);
            wing.WingPad.RestoreInvestment(50); // historical partially funded plot
            Assert.That(wing.TryPurchaseWing(),Is.False);
            shop.ShowExpansions();
            Assert.That(drinks.transform.Find("Detail").GetComponent<Text>().text,Does.Contain("Remaining 250"));
            Assert.That(drinks.transform.Find("Description").GetComponent<Text>().text,Does.Contain("Need 1 more"));
            wallet.CollectCoins(1);shop.ShowExpansions();
            var buy=GameObject.Find("Expand_0").GetComponent<Button>();
            Assert.That(buy.interactable,Is.True);buy.onClick.Invoke();
            Assert.That(wing.HasWing,Is.True);Assert.That(wallet.Coins,Is.Zero);
            Assert.That(wing.HasColaMachine||wing.HasColaBar,Is.False,"Land does not secretly include equipment");
            Assert.That(buy.interactable,Is.False);buy.onClick.Invoke();Assert.That(wallet.Coins,Is.Zero);
            Assert.That(GameObject.Find("DrinksPlannedLand"),Is.Null);
            Assert.That(GameObject.Find("DrinkServiceTiles"),Is.Not.Null);
            Assert.That(layout.Floors().Any(r=>r.Contains(new Vector2(30,6))),Is.True);
            foreach(var f in layout.Instances)if(original.TryGetValue(f.Id,out var pose))Assert.That(f.transform.position,Is.EqualTo(pose));
            // Decorative meshes contain no active collider; doorway and corridor retain their routes.
            yield return null;
            Physics.SyncTransforms();layout.RefreshNavigation();
            Assert.That(layout.Route(new Vector3(12,0,-6.5f),new Vector3(26,0,-4)),Is.Not.Null);
            Assert.That(Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Any(c=>c.enabled&&
                (c.name.StartsWith("Architecture")||c.name=="PlotMark"||c.name=="FloorBorder")),Is.False);
            wallet.RestoreProgress(300,0);shop.ShowExpansions();
            GameObject.Find("BuyRestroom").GetComponent<Button>().onClick.Invoke();
            Assert.That(room.Built,Is.True);Assert.That(wallet.Coins,Is.Zero);
            goals.Restore(10,0,0);shop.ShowExpansions();
            GameObject.Find("Expand_2").GetComponent<Button>().onClick.Invoke();
            Assert.That(BagLine.Current.Expanded,Is.True);Assert.That(wallet.Coins,Is.Zero,"West land remains included");
            Assert.That(GameObject.Find("TakeawayPlannedLand"),Is.Null);
            Assert.That(GameObject.Find("RestroomPlannedLand"),Is.Null);
            Assert.That(west.transform.Find("Detail").GetComponent<Text>().text,Is.EqualTo("Built"));
            Assert.That(west.transform.Find("Description").GetComponent<Text>().text,Does.Contain("250+200+300=750"));
            yield return null;
            layout.RefreshNavigation();Assert.That(layout.Route(new Vector3(-12,0,0),new Vector3(-20,0,0)),Is.Not.Null);
            int count=architecture.GetComponentsInChildren<MeshRenderer>(true).Length;
            architecture.Refresh();architecture.Refresh();
            Assert.That(architecture.GetComponentsInChildren<MeshRenderer>(true).Length,Is.EqualTo(count));
            shop.Close();
            Assert.That(Object.FindFirstObjectByType<RestaurantPersistence>().Flush(),Is.True);
            yield return new ExitPlayMode();
            yield return new EnterPlayMode();
            Assert.That(Object.FindFirstObjectByType<ShopExpansion>().HasWing,Is.True);
            Assert.That(Object.FindFirstObjectByType<RestroomExpansion>().Built,Is.True);
            Assert.That(BagLine.Current.Expanded,Is.True);
            Assert.That(GameObject.Find("DrinkServiceTiles"),Is.Not.Null);
            Assert.That(GameObject.Find("DrinksPlannedLand"),Is.Null);
            LogAssert.NoUnexpectedReceived();
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator CaptureReadmeSupermarketAtPhoneResolution()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            yield return new EnterPlayMode();
            Object.FindFirstObjectByType<PlayerMotor>().enabled=false;
            Object.FindFirstObjectByType<SessionGoalTracker>().Restore(7,0,0);
            var shop=Object.FindFirstObjectByType<FacilityShopHud>();
            shop.Open();shop.ShowExpansions();
            var camera=Camera.main;
            var canvas=shop.GetComponentInParent<Canvas>();
            var cameraData=camera.GetComponents<Component>().First(c=>c.GetType().Name=="UniversalAdditionalCameraData");
            var serialized=new SerializedObject(cameraData);
            serialized.FindProperty("m_RenderPostProcessing").boolValue=false;serialized.ApplyModifiedPropertiesWithoutUndo();
            canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
            const int width=2160,height=3840;
            var target=new RenderTexture(width,height,24){antiAliasing=8};
            camera.targetTexture=target;camera.aspect=width/(float)height;
            yield return null;Canvas.ForceUpdateCanvases();
            if(shop!=null)shop.SendMessage("FitCatalog",SendMessageOptions.DontRequireReceiver);
            Canvas.ForceUpdateCanvases();
            camera.Render();
            var previous=RenderTexture.active;RenderTexture.active=target;
            var pixels=new Texture2D(width,height,TextureFormat.RGB24,false);
            pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();
            Directory.CreateDirectory("Logs/readme");
            File.WriteAllBytes("Logs/readme/supermarket.png",pixels.EncodeToPNG());
            RenderTexture.active=previous;camera.targetTexture=null;
            Object.Destroy(pixels);Object.Destroy(target);shop.Close();
            yield return new ExitPlayMode();
        }
        [UnityTearDown] public IEnumerator LeavePlay(){if(Application.isPlaying)yield return new ExitPlayMode();}
    }
}
