using System.Collections;
using System.Linq;
using BurgerShop.Building;
using BurgerShop.Economy;
using BurgerShop.Persistence;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using BurgerShop.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BurgerShop.Tests.EditMode
{
    public sealed class FacilityDirectPurchaseTests
    {
        GameObject root;
        RestaurantWallet wallet;
        BurgerInventory player;
        [SetUp] public void Setup()
        {
            root = new GameObject("DirectPurchaseTest");
            wallet = root.AddComponent<RestaurantWallet>();
            player = root.AddComponent<BurgerInventory>(); player.Configure();
        }
        [TearDown] public void Cleanup() { Object.DestroyImmediate(root); }
        [TestCase(KitchenProduct.Burger)] [TestCase(KitchenProduct.Cola)]
        public void DirectGrillNeverChargesForStandingAndCommitsBeforeWalletEvent(KitchenProduct kind)
        {
            var output = new GameObject("Output").transform; output.SetParent(root.transform);
            var station = root.AddComponent<ProductionStation>(); station.Configure(output,null,null);
            var point = new GameObject("UpgradeSpot").transform; point.SetParent(root.transform);
            var zone = root.AddComponent<GrillUpgradeZone>(); zone.Configure(station,wallet,player,point,kind:kind);
            zone.UseDirectInteraction(); wallet.RestoreProgress(29,0);
            for(int i=0;i<600;i++)zone.Advance(.1f);
            Assert.That(zone.Level,Is.EqualTo(1)); Assert.That(wallet.Coins,Is.EqualTo(29));
            Assert.That(point.gameObject.activeSelf,Is.False); Assert.That(zone.TryUpgrade(1),Is.False);
            wallet.CollectCoins(61);
            int observed=0;wallet.CoinsSpent += _ => {observed=zone.Level;Assert.That(zone.TryUpgrade(zone.Level),Is.False,"Wallet callbacks cannot reenter a direct purchase");};
            Assert.That(zone.TryUpgrade(1),Is.True);Assert.That(wallet.Coins,Is.EqualTo(60));Assert.That(observed,Is.EqualTo(2));
            Assert.That(zone.TryUpgrade(1),Is.False,"An old UI event must not buy the next tier");
            Assert.That(zone.TryUpgrade(2),Is.True); Assert.That(station.ProductionSeconds,Is.EqualTo(1.5f));
            Assert.That(station.Capacity,Is.EqualTo(8)); Assert.That(wallet.Coins,Is.Zero);
            Assert.That(zone.TryUpgrade(3),Is.False);
        }
        [TestCase(0,TableSetId.Bistro)] [TestCase(25,TableSetId.Diner)] [TestCase(80,TableSetId.Patio)]
        public void TableBuyCreditsExistingInvestmentAndRecordsExactlyOneChoice(int invested,TableSetId choice)
        {
            var table=DiningTable.Create(root.transform,Vector3.zero);
            var pad=new GameObject("TableUpgrade");pad.transform.SetParent(table.transform);
            var zone=pad.AddComponent<TableUpgradeZone>();int changes=0;
            zone.Configure(table,0,wallet,player,pad.transform,null,()=>changes++);
            zone.Restore(0,invested);zone.UseDirectInteraction();zone.SetRankVisible(true);
            wallet.RestoreProgress(80-invested,0);
            for(int i=0;i<60;i++)zone.Advance(.1f);
            Assert.That(zone.Invested,Is.EqualTo(invested)); Assert.That(pad.activeSelf,Is.False);
            Assert.That(zone.TryBuySet(TableSetId.Starter,invested),Is.False);
            Assert.That(zone.TryBuySet(choice,invested+1),Is.False);
            Assert.That(zone.TryBuySet(choice,invested),Is.True);
            Assert.That(zone.Invested,Is.EqualTo(80));Assert.That(zone.PendingChoice,Is.False);
            Assert.That(table.SetId,Is.EqualTo(choice));Assert.That(wallet.Coins,Is.Zero);Assert.That(changes,Is.EqualTo(1));
            Assert.That(zone.TryBuySet(choice,80),Is.False);Assert.That(changes,Is.EqualTo(1));
        }
        [Test]
        public void InsufficientTableMoneyDoesNotLosePartialCredit()
        {
            var table=DiningTable.Create(root.transform,Vector3.zero);
            var zone=table.gameObject.AddComponent<TableUpgradeZone>();zone.Configure(table,0,wallet,player,null,null,null);
            zone.Restore(0,25);wallet.RestoreProgress(54,0);
            Assert.That(zone.TryBuySet(TableSetId.Diner,25),Is.False);
            Assert.That(zone.Invested,Is.EqualTo(25));Assert.That(wallet.Coins,Is.EqualTo(54));Assert.That(zone.SetId,Is.EqualTo(TableSetId.Starter));
        }
        [Test]
        public void GestureRejectsDragLongPressOtherFacilityUiAndMultitouch()
        {
            var item=root.AddComponent<FacilityInstance>();var gesture=new FacilityTapGesture();var p=Vector2.one*100;
            gesture.Begin(item,p,0,false);Assert.That(gesture.Release(item,p,.2f,false),Is.SameAs(item));
            gesture.Begin(item,p,0,false);gesture.Track(p+Vector2.right*40,.1f,false);
            Assert.That(gesture.Release(item,p,.2f,false),Is.Null,"Returning to the start cannot undo a drag");
            gesture.Begin(item,p,0,false);Assert.That(gesture.Release(item,p,1,false),Is.Null);
            gesture.Begin(item,p,0,true);Assert.That(gesture.Release(item,p,.2f,false),Is.Null);
            gesture.Begin(item,p,0,false);gesture.Track(p,.1f,true);Assert.That(gesture.Release(item,p,.2f,false),Is.Null);
            gesture.Begin(item,p,0,false);Assert.That(gesture.Release(null,p,.2f,false),Is.Null);
        }
        [Test]
        public void EveryTableShapeHasDistinctCachedStylePhotos()
        {
            foreach(var kind in new[]{FacilityKind.PairTable,FacilityKind.FourSeatTable,FacilityKind.SquareTable})
            {
                var photos=TableSetCatalog.Choices.Select(s=>TableSetThumbnails.Get(kind,s)).ToArray();
                Assert.That(photos,Has.All.Not.Null);Assert.That(photos.Distinct().Count(),Is.EqualTo(3));
            }
        }
    }

    public sealed class FacilityDetailsPlayTests : SaveIsolatedGameplayTest
    {
        InputSettings.BackgroundBehavior previousBackground;
        InputSettings.EditorInputBehaviorInPlayMode previousEditorInput;
        float previousStep;
        bool captured;
        Mouse mouse;
        Touchscreen touchscreen;
        void SetupInput()
        {
            previousBackground=InputSystem.settings.backgroundBehavior;
            previousEditorInput=InputSystem.settings.editorInputBehaviorInPlayMode;
            previousStep=Time.captureDeltaTime;Time.captureDeltaTime=1f/60f;captured=true;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        }
        void RestoreInput()
        {
            if(mouse!=null&&mouse.added)InputSystem.RemoveDevice(mouse);mouse=null;
            if(touchscreen!=null&&touchscreen.added)InputSystem.RemoveDevice(touchscreen);touchscreen=null;
            if(!captured)return;captured=false;
            Time.captureDeltaTime=previousStep;
            InputSystem.settings.backgroundBehavior=previousBackground;
            InputSystem.settings.editorInputBehaviorInPlayMode=previousEditorInput;
        }
        [UnityTest]
        public IEnumerator TapUpgradeMoveAndSaveUseTheSameFacility()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");yield return new EnterPlayMode();
            var layout=Object.FindFirstObjectByType<FacilityLayout>();var hud=FacilityDetailsHud.Current;
            var shop=Object.FindFirstObjectByType<FacilityShopHud>();var goals=Object.FindFirstObjectByType<SessionGoalTracker>();
            goals.Restore(2,0,0);layout.Wallet.RestoreProgress(5000,0);layout.Discover();
            var item=layout.Instances.Single(f=>f.Id=="grill-main");var grill=item.GetComponentInChildren<GrillUpgradeZone>();
            Assert.That(grill.DirectInteraction,Is.True);
            layout.Player.transform.position=grill.UpgradePosition+Vector3.up;
            for(int i=0;i<60;i++)grill.Advance(.1f);
            Assert.That(grill.Level,Is.EqualTo(1));Assert.That(layout.Wallet.Coins,Is.EqualTo(5000));
            // Actual pointer events through the HUD, with a fixed camera and the real model colliders.
            var camera=Camera.main;camera.GetComponent<CameraFollow>().enabled=false;
            camera.transform.position=item.transform.position+new Vector3(-10,14,-10);camera.transform.LookAt(item.transform.position+Vector3.up);
            Physics.SyncTransforms();yield return null;
            var point=(Vector2)camera.WorldToScreenPoint(grill.Station.transform.position+Vector3.up);
            Assert.That(hud.FacilityAt(point),Is.SameAs(item));
            SetupInput();mouse=InputSystem.AddDevice<Mouse>();
            InputSystem.QueueStateEvent(mouse,new MouseState{position=point}.WithButton(MouseButton.Left));yield return null;yield return null;
            InputSystem.QueueStateEvent(mouse,new MouseState{position=point});yield return null;yield return null;
            Assert.That(hud.IsOpen,Is.True);Assert.That(hud.Selected,Is.SameAs(item));Assert.That(Time.timeScale,Is.Zero);
            Click("Upgrade");Click("Upgrade");Assert.That(grill.Level,Is.EqualTo(2));Assert.That(layout.Wallet.Coins,Is.EqualTo(4970));
            Assert.That(goals.Stars,Is.EqualTo(2));Click("CloseDetails");Assert.That(Time.timeScale,Is.EqualTo(1));
            shop.Open();Click("MoveExisting");Click("Move_grill-main");
            Assert.That(hud.IsOpen,Is.True);Assert.That(shop.IsOpen,Is.False);Click("MoveFacility");
            Assert.That(layout.Moving,Is.SameAs(item));Assert.That(Time.timeScale,Is.Zero);shop.Close();Assert.That(Time.timeScale,Is.EqualTo(1));
            var tableItem=layout.Instances.Single(f=>f.Id=="table-0");var table=tableItem.GetComponentInChildren<TableUpgradeZone>(true);
            table.Restore(0,25);Assert.That(hud.Open(tableItem),Is.True);Click("ChooseSet1");
            Assert.That(table.SetId,Is.EqualTo(TableSetId.Diner));Assert.That(layout.Wallet.Coins,Is.EqualTo(4915));
            Assert.That(goals.Stars,Is.EqualTo(4));hud.Close();
            Assert.That(Object.FindFirstObjectByType<RestaurantPersistence>().Flush(),Is.True);
            Assert.That(new LocalSaveStore(SaveDirectory).Load(out var data),Is.EqualTo(SaveLoadResult.Loaded));
            Assert.That(data.layout.Single(r=>r.id=="table-0").tableSet,Is.EqualTo((int)TableSetId.Diner));
            RestoreInput();yield return new ExitPlayMode();
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");yield return new EnterPlayMode();
            layout=Object.FindFirstObjectByType<FacilityLayout>();
            Assert.That(layout.Instances.Single(f=>f.Id=="grill-main").GetComponentInChildren<GrillUpgradeZone>().Level,Is.EqualTo(2));
            Assert.That(layout.Instances.Single(f=>f.Id=="table-0").GetComponentInChildren<TableUpgradeZone>(true).SetId,Is.EqualTo(TableSetId.Diner));
            LogAssert.NoUnexpectedReceived();yield return new ExitPlayMode();
        }
        [UnityTest]
        public IEnumerator DetailsFitPhoneAndDesktopAndRestorePauseOnDisable()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");yield return new EnterPlayMode();
            var layout=Object.FindFirstObjectByType<FacilityLayout>();var hud=FacilityDetailsHud.Current;
            Object.FindFirstObjectByType<SessionGoalTracker>().Restore(2,0,0);layout.Wallet.RestoreProgress(0,0);layout.Wallet.CollectCoins(1000);layout.Discover();
            var canvas=hud.GetComponentInParent<Canvas>();var camera=Camera.main;
            var cameraData=camera.GetComponents<Component>().First(c=>c.GetType().Name=="UniversalAdditionalCameraData");
            var serialized=new UnityEditor.SerializedObject(cameraData);serialized.FindProperty("m_RenderPostProcessing").boolValue=false;serialized.ApplyModifiedPropertiesWithoutUndo();
            canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
            foreach(var size in new[]{new Vector2Int(1080,1920),new Vector2Int(1600,1000)})
            {
                var target=new RenderTexture(size.x,size.y,24);camera.targetTexture=target;
                foreach(var id in new[]{"grill-main","table-0"})
                {
                    Assert.That(hud.Open(layout.Instances.Single(f=>f.Id==id)),Is.True);
                    yield return null;Canvas.ForceUpdateCanvases();hud.SendMessage("Fit");Canvas.ForceUpdateCanvases();
                    var sheet=(RectTransform)hud.transform.Find("DetailsSheet");var space=(RectTransform)hud.transform;
                    var bounds=HudChrome.LocalRect(sheet,space);
                    Assert.That(bounds.xMin,Is.GreaterThanOrEqualTo(0));Assert.That(bounds.yMin,Is.GreaterThanOrEqualTo(0));
                    Assert.That(bounds.xMax,Is.LessThanOrEqualTo(space.rect.width));Assert.That(bounds.yMax,Is.LessThanOrEqualTo(space.rect.height));
                    Capture(camera,target,"/tmp/bs055-"+id+"-"+size.x+".png");hud.Close();
                }
                camera.targetTexture=null;Object.Destroy(target);
            }
            Time.timeScale=.5f;hud.Open(layout.Instances.Single(f=>f.Id=="grill-main"));hud.enabled=false;Assert.That(Time.timeScale,Is.EqualTo(.5f));Time.timeScale=1;
            LogAssert.NoUnexpectedReceived();yield return new ExitPlayMode();
        }
        [UnityTest]
        public IEnumerator TouchTapOpensButDragMultitouchAndJoystickDoNot()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");yield return new EnterPlayMode();
            SetupInput();touchscreen=InputSystem.AddDevice<Touchscreen>();
            var layout=Object.FindFirstObjectByType<FacilityLayout>();var hud=FacilityDetailsHud.Current;
            var item=layout.Instances.Single(f=>f.Id=="grill-main");var camera=Camera.main;
            camera.GetComponent<CameraFollow>().enabled=false;camera.transform.position=item.transform.position+new Vector3(-10,14,-10);
            camera.transform.LookAt(item.transform.position+Vector3.up);Physics.SyncTransforms();yield return null;
            var point=(Vector2)camera.WorldToScreenPoint(item.transform.position+Vector3.up);
            Touch(1,UnityEngine.InputSystem.TouchPhase.Began,point);yield return null;yield return null;
            Touch(1,UnityEngine.InputSystem.TouchPhase.Moved,point+Vector2.right*80);yield return null;yield return null;
            Touch(1,UnityEngine.InputSystem.TouchPhase.Ended,point);yield return null;yield return null;
            Assert.That(hud.IsOpen,Is.False,"Dragging past a facility is not a selection");
            Touch(2,UnityEngine.InputSystem.TouchPhase.Began,point);yield return null;yield return null;
            Touch(3,UnityEngine.InputSystem.TouchPhase.Began,point+Vector2.right*20);yield return null;yield return null;
            Touch(3,UnityEngine.InputSystem.TouchPhase.Ended,point+Vector2.right*20);yield return null;yield return null;
            Touch(2,UnityEngine.InputSystem.TouchPhase.Ended,point);yield return null;yield return null;
            Assert.That(hud.IsOpen,Is.False,"A multi-finger gesture cannot finish as a tap");
            var joystick=Object.FindFirstObjectByType<VirtualJoystick>();Canvas.ForceUpdateCanvases();
            var center=RectTransformUtility.WorldToScreenPoint(null,joystick.transform.position);
            Touch(4,UnityEngine.InputSystem.TouchPhase.Began,center);yield return null;yield return null;
            Touch(4,UnityEngine.InputSystem.TouchPhase.Moved,point);yield return null;yield return null;
            Touch(4,UnityEngine.InputSystem.TouchPhase.Ended,point);yield return null;yield return null;
            Assert.That(hud.IsOpen,Is.False);Assert.That(joystick.HasPointer,Is.False);
            Touch(5,UnityEngine.InputSystem.TouchPhase.Began,point);yield return null;yield return null;
            Touch(5,UnityEngine.InputSystem.TouchPhase.Ended,point);yield return null;yield return null;
            Assert.That(hud.IsOpen,Is.True,"A short touch should open the same detail sheet as a mouse click");
            hud.Close();Assert.That(Time.timeScale,Is.EqualTo(1));RestoreInput();yield return new ExitPlayMode();
        }
        void Touch(int id,UnityEngine.InputSystem.TouchPhase phase,Vector2 position)=>InputSystem.QueueStateEvent(touchscreen,new TouchState{touchId=id,phase=phase,position=position});

        [UnityTest]
        public IEnumerator AllExistingUpgradeKindsShareDetailsAndKeepTheirPrices()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");yield return new EnterPlayMode();
            var layout=Object.FindFirstObjectByType<FacilityLayout>();var hud=FacilityDetailsHud.Current;
            var goals=Object.FindFirstObjectByType<SessionGoalTracker>();goals.Restore(10,0,0);
            var expansion=Object.FindFirstObjectByType<ShopExpansion>();
            expansion.Restore(true,true,true,1,true,true,true,true,true,true,true,1);
            Object.FindFirstObjectByType<BagLine>().Restore(new RestaurantSaveData{version=RestaurantSaveData.CurrentVersion,westExpanded=true,bagMachineBuilt=true,bagTableBuilt=true,bagCounterBuilt=true});
            layout.Discover();layout.Wallet.RestoreProgress(10000,0);
            var growth=Object.FindFirstObjectByType<GrowthUpgrades>();growth.Discover();
            foreach(var id in new[]{"grill-main","grill-extra","cola-machine","counter-main","counter-extra","boxing","bag-machine","bag-table","bag-counter"})
            {
                var item=layout.Instances.Single(f=>f.Id==id);var grill=item.GetComponentInChildren<GrillUpgradeZone>(true);
                int before=grill!=null?grill.Level:growth.Level(id);int cost=grill!=null?grill.NextCost:growth.Offers.Single(o=>o.Id==id).Costs[before-1];
                long money=layout.Wallet.Coins;int stars=goals.Stars;
                Assert.That(hud.Open(item),Is.True,id);Click("Upgrade");
                Assert.That(grill!=null?grill.Level:growth.Level(id),Is.EqualTo(before+1),id);
                Assert.That(layout.Wallet.Coins,Is.EqualTo(money-cost),id);Assert.That(goals.Stars,Is.EqualTo(stars+2),id);hud.Close();
            }
            foreach(var zone in layout.GetComponentsInChildren<TableUpgradeZone>(true))Assert.That(zone.DirectInteraction,Is.True);
            foreach(var zone in layout.GetComponentsInChildren<GrillUpgradeZone>(true))Assert.That(zone.DirectInteraction,Is.True);
            LogAssert.NoUnexpectedReceived();yield return new ExitPlayMode();
        }
        static void Click(string name)=>GameObject.Find(name).GetComponent<Button>().onClick.Invoke();
        static void Capture(Camera camera,RenderTexture target,string path)
        {
            camera.Render();var previous=RenderTexture.active;RenderTexture.active=target;
            var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();
            System.IO.File.WriteAllBytes(path,image.EncodeToPNG());RenderTexture.active=previous;Object.Destroy(image);
        }
        [UnityTearDown]public IEnumerator Leave(){RestoreInput();Time.timeScale=1;if(Application.isPlaying)yield return new ExitPlayMode();}
    }
}
