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
using UnityEngine.TestTools;

namespace BurgerShop.Tests.EditMode
{
    public sealed class Spec043ShopTests : SaveIsolatedGameplayTest
    {
        [Test] public void EachCatalogItemConstructsAnInactiveFunctionalPreviewWithoutSpending()
        {
            var root=new GameObject("FactoryTest");
            try
            {
                var player=new GameObject("Actor").AddComponent<BurgerInventory>();player.transform.SetParent(root.transform);player.gameObject.AddComponent<TrashInventory>();
                var wallet=root.AddComponent<RestaurantWallet>();wallet.RestoreProgress(1000,0);
                var parts=root.AddComponent<PartsWallet>();var cash=root.AddComponent<CashFloor>();cash.Configure(wallet,player.transform,Vector3.zero);
                var dining=DiningArea.Create(root.transform,new Vector3[0]);
                var staging=new GameObject("Staging").transform;staging.SetParent(root.transform);staging.gameObject.SetActive(false);
                foreach(var offer in FacilityCatalog.Offers)
                {
                    var item=FacilityFactory.Create(staging,offer.Kind,"custom:"+offer.Kind,player,wallet,parts,cash,dining);
                    Assert.That(item.Available,Is.False,offer.Name);Assert.That(item.GetComponentsInChildren<MeshRenderer>(true).Length,Is.GreaterThan(0));
                    Assert.That(item.Size.x,Is.GreaterThan(0));Assert.That(wallet.Coins,Is.EqualTo(1000));
                    Object.DestroyImmediate(item.gameObject);
                }
                Assert.That(dining.TableCount,Is.Zero);
            }
            finally {Object.DestroyImmediate(root);}
        }
        [TestCase(false)] [TestCase(true)]
        public void PurchasedCounterAndMachineServeAfterRotation(bool cola)
        {
            var root=new GameObject("IndependentLine");
            try
            {
                var player=new GameObject("Actor").AddComponent<BurgerInventory>();player.transform.SetParent(root.transform);player.gameObject.AddComponent<TrashInventory>();
                var wallet=root.AddComponent<RestaurantWallet>();var parts=root.AddComponent<PartsWallet>();
                var cash=root.AddComponent<CashFloor>();cash.Configure(wallet,player.transform,Vector3.zero);
                var dining=DiningArea.Create(root.transform,new Vector3[0]);
                var staging=new GameObject("Staging").transform;staging.SetParent(root.transform);staging.gameObject.SetActive(false);
                var machine=FacilityFactory.Create(staging,cola?FacilityKind.ColaMachine:FacilityKind.BurgerMachine,"machine",player,wallet,parts,cash,dining);
                var counter=FacilityFactory.Create(staging,cola?FacilityKind.ColaCounter:FacilityKind.BurgerCounter,"counter",player,wallet,parts,cash,dining);
                staging.gameObject.SetActive(true);
                machine.transform.SetPositionAndRotation(new Vector3(8,0,6),Quaternion.Euler(0,90,0));
                counter.transform.SetPositionAndRotation(new Vector3(-4,0,-5),Quaternion.Euler(0,90,0));
                var production=machine.GetComponentInChildren<ProductionStation>();production.Advance(10);
                Assert.That(player.TryCollectFrom(production),Is.True);
                var serving=counter.GetComponentInChildren<BurgerServingZone>();player.transform.position=serving.ServingPosition;
                var queue=counter.GetComponentInChildren<BurgerShop.Customer.CustomerQueue>();queue.OrderQuantityFactory=()=>1;
                for(int i=0;i<400&&serving.CompletedOrders==0;i++){queue.Advance(.1f);serving.Advance(.1f);}
                Assert.That(serving.CompletedOrders,Is.EqualTo(1));Assert.That(wallet.CompletedSales,Is.EqualTo(1));
                Assert.That(player.Count,Is.Zero);Assert.That(serving.Stock.Count,Is.Zero);
            }
            finally {Object.DestroyImmediate(root);}
        }
        [Test] public void RotatedTableRetainsSeatsTrashAndUpgrade()
        {
            var table=DiningTable.Create(null,new Vector3(2,0,3));
            try
            {
                var local=table.transform.InverseTransformPoint(table.SeatPosition(0));table.ApplySet(TableSetId.Patio);table.LeaveMealTrash(0);
                table.transform.SetPositionAndRotation(new Vector3(-5,0,-2),Quaternion.Euler(0,90,0));
                Assert.That(Vector3.Distance(table.SeatPosition(0),table.transform.TransformPoint(local)),Is.LessThan(.001f));
                Assert.That(table.TrashCount,Is.EqualTo(2));Assert.That(table.SetId,Is.EqualTo(TableSetId.Patio));
            }
            finally {Object.DestroyImmediate(table.gameObject);}
        }
        [Test] public void LayoutSchemaRejectsCorruptCoordinatesAndKeepsV14ChecksumIndependent()
        {
            var data=new RestaurantSaveData{version=15,grillLevel=1,shopRank=1,layout=new[]{new FacilityPlacementRecord{id="custom:one",kind=0,x=2,z=3,yaw=90}}};
            Assert.That(data.IsValid,Is.True);string checksum=data.Checksum();data.layout[0].x=3;Assert.That(data.Checksum(),Is.Not.EqualTo(checksum));
            data.layout[0].x=float.NaN;Assert.That(data.IsValid,Is.False);
            data.version=14;string old=data.Checksum();data.layout=null;Assert.That(data.Checksum(),Is.EqualTo(old));Assert.That(data.IsValid,Is.True);
        }
        [UnityTest] public IEnumerator ShopBuysMovesRotatesCancelsAndSavesRepeatedTables()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");yield return new EnterPlayMode();
            var layout=Object.FindFirstObjectByType<FacilityLayout>();var goals=Object.FindFirstObjectByType<SessionGoalTracker>();
            goals.Restore(2,0,0);layout.Wallet.RestoreProgress(5000,0);layout.Discover();
            Assert.That(GameObject.Find("ShoppingCart"),Is.Not.Null);
            var hud=Object.FindFirstObjectByType<FacilityShopHud>();float previousScale=Time.timeScale;
            GameObject.Find("ShoppingCart").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.That(Time.timeScale,Is.Zero);Assert.That(GameObject.Find("Catalog"),Is.Not.Null);
            GameObject.Find("Buy_PairTable").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.That(GameObject.Find("FacilityGhost"),Is.Not.Null);Assert.That(layout.Candidate,Is.Not.Null);
            hud.Close();Assert.That(Time.timeScale,Is.EqualTo(previousScale));Assert.That(layout.Candidate,Is.Null);

            var kind=FacilityKind.PairTable;long before=layout.Wallet.Coins;
            layout.BeginPurchase(kind);layout.Cancel();Assert.That(layout.Wallet.Coins,Is.EqualTo(before));
            layout.BeginPurchase(kind);Assert.That(layout.Confirm(new Vector3(100,0,0),0),Is.False);Assert.That(layout.Wallet.Coins,Is.EqualTo(before));
            int price=layout.Price(kind);Assert.That(layout.Confirm(new Vector3(7,0,-5),90),Is.True,layout.LastError);
            Assert.That(layout.Wallet.Coins,Is.EqualTo(before-price));var bought=layout.Instances.Single(f=>f.Purchased);
            var table=bought.GetComponentInChildren<DiningTable>();table.LeaveMealTrash(0);
            Assert.That(layout.BeginMove(bought),Is.True);Assert.That(layout.Confirm(new Vector3(9,0,-8),180),Is.True,layout.LastError);
            Assert.That(table.TrashCount,Is.EqualTo(2));Assert.That(layout.Wallet.Coins,Is.EqualTo(before-price));
            layout.BeginPurchase(kind);Assert.That(layout.Confirm(new Vector3(3,0,-10),0),Is.True,layout.LastError);
            Assert.That(layout.Instances.Count(f=>f.Purchased),Is.EqualTo(2));
            var save=Object.FindFirstObjectByType<RestaurantPersistence>();Assert.That(save.Flush(),Is.True);
            Assert.That(new LocalSaveStore(SaveDirectory).Load(out var data),Is.EqualTo(SaveLoadResult.Loaded));
            Assert.That(data.layout.Count(r=>r.purchased),Is.EqualTo(2));
            LogAssert.NoUnexpectedReceived();
            yield return new ExitPlayMode();
        }
        [UnityTest] public IEnumerator DoneCommitsFrozenPreviewAndUiGesturesNeverMoveIt()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");yield return new EnterPlayMode();
            var layout=Object.FindFirstObjectByType<FacilityLayout>();
            Object.FindFirstObjectByType<SessionGoalTracker>().Restore(2,0,0);layout.Wallet.RestoreProgress(5000,0);
            var hud=Object.FindFirstObjectByType<FacilityShopHud>();float scale=Time.timeScale;
            hud.Open();GameObject.Find("Buy_PairTable").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.That(GameObject.Find("Confirm"),Is.Null,"Place is replaced by Done");
            long coins=layout.Wallet.Coins;int price=layout.Price(FacilityKind.PairTable);
            // The player's occupied position passed the old cheap preview but failed Done.
            hud.MovePreviewPointer(Camera.main.WorldToScreenPoint(layout.Player.transform.position-new Vector3(0,layout.Player.transform.position.y,0)),true,true,false);
            hud.MovePreviewPointer(Vector2.zero,false,false,false);hud.SendMessage("LateUpdate");
            var invalidColor=GameObject.Find("FacilityGhost").GetComponentInChildren<MeshRenderer>().sharedMaterial.color;
            Assert.That(invalidColor.r,Is.GreaterThan(invalidColor.g),"Full validation must mark invalid poses red BEFORE Done");
            Vector2 screen=Camera.main.WorldToScreenPoint(new Vector3(7,0,-5));
            hud.MovePreviewPointer(screen,true,true,false);
            var frozen=hud.PreviewPosition;
            Assert.That(Vector3.Distance(frozen,new Vector3(7,0,-5)),Is.LessThan(.01f));
            hud.MovePreviewPointer(screen+Vector2.one*200,false,false,false);
            Assert.That(hud.PreviewPosition,Is.EqualTo(frozen),"Release freezes the item while the pointer travels to Done");
            hud.MovePreviewPointer(Vector2.zero,true,true,true);
            hud.MovePreviewPointer(Vector2.one*100,false,true,false);
            Assert.That(hud.PreviewPosition,Is.EqualTo(frozen),"A gesture started on UI cannot drag into the world");
            GameObject.Find("RotateRight").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            hud.SendMessage("LateUpdate");
            Assert.That(GameObject.Find("FacilityGhost").transform.eulerAngles.y,Is.EqualTo(15).Within(.01f));
            var validColor=GameObject.Find("FacilityGhost").GetComponentInChildren<MeshRenderer>().sharedMaterial.color;
            Assert.That(validColor.g,Is.GreaterThan(validColor.r),"Dragging to a legal pose must recover from red");
            Assert.That(hud.PreviewPosition,Is.EqualTo(frozen));
            GameObject.Find("Done").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.That(layout.Candidate,Is.Null);Assert.That(layout.Wallet.Coins,Is.EqualTo(coins-price));
            var bought=layout.Instances.Single(f=>f.Purchased);
            Assert.That(Vector3.Distance(bought.transform.position,frozen),Is.LessThan(.01f));
            Assert.That(bought.transform.eulerAngles.y,Is.EqualTo(15).Within(.01f));
            Assert.That(Time.timeScale,Is.EqualTo(scale));
            // Invalid placement keeps the editor open and does not charge.
            hud.Open();GameObject.Find("Buy_PairTable").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            coins=layout.Wallet.Coins;
            hud.MovePreviewPointer(Camera.main.WorldToScreenPoint(new Vector3(100,0,0)),true,true,false);
            GameObject.Find("Done").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.That(layout.Candidate,Is.Not.Null);Assert.That(layout.Wallet.Coins,Is.EqualTo(coins));
            Assert.That(Time.timeScale,Is.Zero);hud.Close();
            LogAssert.NoUnexpectedReceived();yield return new ExitPlayMode();
        }
        [UnityTest] public IEnumerator DesktopMatchesApprovedPreviewHoverClickAndUiFreeze()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");yield return new EnterPlayMode();
            var layout=Object.FindFirstObjectByType<FacilityLayout>();
            Object.FindFirstObjectByType<SessionGoalTracker>().Restore(2,0,0);layout.Wallet.RestoreProgress(5000,0);
            var hud=Object.FindFirstObjectByType<FacilityShopHud>();hud.Open();
            GameObject.Find("Buy_PairTable").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            var screen=(Vector2)Camera.main.WorldToScreenPoint(new Vector3(7,0,-5));
            int price=layout.Price(FacilityKind.PairTable);
            hud.MoveDesktopPointer(screen,false,false);
            var pose=hud.PreviewPosition;
            Assert.That(Vector3.Distance(pose,new Vector3(7,0,-5)),Is.LessThan(.01f));
            Assert.That(layout.Wallet.Coins,Is.EqualTo(5000));
            hud.MoveDesktopPointer(Vector2.zero,true,true);
            Assert.That(hud.PreviewPosition,Is.EqualTo(pose));Assert.That(layout.Candidate,Is.Not.Null);
            hud.MoveDesktopPointer(screen,true,false);
            Assert.That(layout.Candidate,Is.Null);Assert.That(layout.Wallet.Coins,Is.EqualTo(5000-price));
            Assert.That(Vector3.Distance(layout.Instances.Single(f=>f.Purchased).transform.position,pose),Is.LessThan(.01f));
            hud.MoveDesktopPointer(screen,true,false);Assert.That(layout.Wallet.Coins,Is.EqualTo(5000-price));
            Assert.That(Time.timeScale,Is.Zero,"Desktop preview remains in arrange mode after click-placement");
            GameObject.Find("Done").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.That(GameObject.Find("PlacementControls"),Is.Null);
            LogAssert.NoUnexpectedReceived();yield return new ExitPlayMode();
        }
        [UnityTest] public IEnumerator SavedRepeatedTablesRebuildWithPoseAndUpgradeWithoutDuplication()
        {
            var data=new RestaurantSaveData {version=15,coins=4000,grillLevel=1,shopRank=2,milestoneMask=1,
                layout=new[]{new FacilityPlacementRecord{id="custom:11111111111111111111111111111111",kind=0,purchased=true,x=9,z=-8,yaw=180,tableSet=(int)TableSetId.Patio,investment=TableSetCatalog.UpgradeCost},
                    new FacilityPlacementRecord{id="custom:22222222222222222222222222222222",kind=0,purchased=true,x=3,z=-10,yaw=0}}};
            bool written=new LocalSaveStore(SaveDirectory).Save(data);Assert.That(written,Is.True);
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");yield return new EnterPlayMode();
            var layout=Object.FindFirstObjectByType<FacilityLayout>();Assert.That(layout,Is.Not.Null);
            var restored=layout.Instances.Single(f=>f.Id=="custom:11111111111111111111111111111111");
            Assert.That(restored.transform.position,Is.EqualTo(new Vector3(9,0,-8)));Assert.That(restored.transform.eulerAngles.y,Is.EqualTo(180).Within(.01));
            Assert.That(restored.GetComponentInChildren<DiningTable>().SetId,Is.EqualTo(TableSetId.Patio));
            Assert.That(layout.Instances.Count(f=>f.Purchased),Is.EqualTo(2));
            layout.Restore(layout.Capture());Assert.That(layout.Instances.Count(f=>f.Purchased),Is.EqualTo(2));
            LogAssert.NoUnexpectedReceived();yield return new ExitPlayMode();
        }
        [UnityTearDown] public IEnumerator LeavePlay(){if(Application.isPlaying)yield return new ExitPlayMode();}
    }
}
