using System.Collections;
using System.Linq;
using BurgerShop.Building;
using BurgerShop.Persistence;
using BurgerShop.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
namespace BurgerShop.Tests.EditMode
{
    public sealed class PlacementAlignmentTests:SaveIsolatedGameplayTest
    {
        static FacilityInstance Box(string name,Vector3 center,Vector3 size)
        {
            var go=new GameObject(name);var box=go.AddComponent<BoxCollider>();box.center=center;box.size=size;
            var f=go.AddComponent<FacilityInstance>();f.Configure(name,FacilityKind.BurgerMachine,true);return f;
        }
        [Test] public void RotatedOffsetModelsAlignEdgesAndPreserveFacing()
        {
            var a=Box("A",new Vector3(.3f,0,.2f),new Vector3(3,2,2));
            var b=Box("B",new Vector3(-.2f,0,.1f),new Vector3(3,2,3));
            try
            {
                b.transform.rotation=Quaternion.Euler(0,30,0);
                var c=b.Footprint(Vector3.zero,30).Center;Vector3 center=new Vector3(c.x,0,c.y);
                Vector3 desired=center+Quaternion.Euler(0,30,0)*new Vector3(3.15f,0,.6f);
                Vector3 raw=desired-Quaternion.Euler(0,30,0)*a.LocalCenter;
                Assert.That(PlacementAlignment.TryPose(a,raw,33,b,out var pose),Is.True);
                Assert.That(pose.Yaw,Is.EqualTo(30).Within(.001f));
                var shape=a.Footprint(pose.Position,pose.Yaw);var reference=b.Footprint(b.transform.position,30);
                Assert.That(Vector2.Dot(shape.Center-reference.Center,reference.Up)+shape.HalfSize.y,Is.EqualTo(reference.HalfSize.y).Within(.001f));
                Assert.That(PlacementGeometry.Overlaps(shape,reference,.1f),Is.False);
                Assert.That(PlacementAlignment.TryPose(a,desired-Quaternion.Euler(0,210,0)*a.LocalCenter,210,b,out var reverse),Is.True);
                Assert.That(Mathf.Abs(Mathf.DeltaAngle(210,reverse.Yaw)),Is.LessThan(.001f),"Do not flip facing by 180 degrees");
            }
            finally{Object.DestroyImmediate(a.gameObject);Object.DestroyImmediate(b.gameObject);}
        }
        [Test] public void FarSkewedAndMisalignedPosesAreFree()
        {
            var a=Box("A",Vector3.zero,new Vector3(3,2,2));var b=Box("B",Vector3.zero,new Vector3(3,2,2));
            try
            {
                Assert.That(PlacementAlignment.TryPose(a,new Vector3(3.14f,0,.1f),0,b,out _),Is.True);
                Assert.That(PlacementAlignment.TryPose(a,new Vector3(6,0,.1f),0,b,out _),Is.False);
                Assert.That(PlacementAlignment.TryPose(a,new Vector3(3.14f,0,.4f),0,b,out _),Is.False);
                Assert.That(PlacementAlignment.TryPose(a,new Vector3(3.14f,0,.1f),15,b,out _),Is.False);
                Assert.That(PlacementAlignment.TryPose(a,Vector3.zero,0,a,out _),Is.False);
            }
            finally{Object.DestroyImmediate(a.gameObject);Object.DestroyImmediate(b.gameObject);}
        }
        [Test] public void FastPassNeverLocksDwellAndBreakawayHaveHysteresis()
        {
            var hold=new AlignmentHold();
            for(int i=0;i<60;i++)Assert.That(hold.ShouldAttempt(new Vector3(i*.1f,0,0),1f/60f),Is.False);
            hold.Reset();Assert.That(hold.ShouldAttempt(Vector3.zero,0),Is.False);
            Assert.That(hold.ShouldAttempt(Vector3.zero,.2f),Is.False);Assert.That(hold.ShouldAttempt(Vector3.zero,.06f),Is.True);
            hold.Lock(Vector3.zero);hold.ShouldAttempt(new Vector3(.3f,0,0),1);Assert.That(hold.Active,Is.True);
            hold.ShouldAttempt(new Vector3(.6f,0,0),1);Assert.That(hold.Active,Is.False);
        }
        [UnityTest] public IEnumerator PreviewAlignsThenDoneSavesShownPoseWithoutUiDrift()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");yield return new EnterPlayMode();
            var layout=FacilityLayout.Current;var hud=Object.FindFirstObjectByType<FacilityShopHud>();
            Object.FindFirstObjectByType<SessionGoalTracker>().Restore(2,0,0);layout.Wallet.RestoreProgress(5000,0);
            layout.BeginPurchase(FacilityKind.PairTable);Assert.That(layout.Confirm(new Vector3(4,0,-7),0),Is.True,layout.LastError);
            var reference=layout.Instances.Single(f=>f.Purchased);
            hud.Open();GameObject.Find("Buy_PairTable").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            var candidate=layout.Candidate;
            reference.transform.position=new Vector3(14,0,-7);layout.RefreshNavigation();
            var outside=new Vector3(14+reference.Size.x+1,0,-6.88f);
            hud.MovePreviewPointer(Camera.main.WorldToScreenPoint(outside),true,true,false);hud.AdvanceAlignment(0);hud.AdvanceAlignment(.3f);
            Assert.That(hud.IsAligned,Is.False,"Do not snap into unowned space");Assert.That(Vector3.Distance(hud.PreviewPosition,outside),Is.LessThan(.001f));
            reference.transform.position=new Vector3(4,0,-7);layout.RefreshNavigation();
            // Keep a modest aisle gap rather than pushing the chair's use point into another table.
            var raw=new Vector3(4.12f,0,-7+reference.Size.y+1);
            Assert.That(PlacementAlignment.TryPose(candidate,raw,0,reference,out var proposed),Is.True);
            Assert.That(layout.CanPlace(candidate,proposed.Position,proposed.Yaw,true),Is.True,layout.LastError);
            hud.MovePreviewPointer(Camera.main.WorldToScreenPoint(raw),true,true,false);
            hud.AdvanceAlignment(0);hud.AdvanceAlignment(.3f);
            Assert.That(hud.IsAligned,Is.True);Assert.That(hud.PreviewPosition.x,Is.EqualTo(4).Within(.001f));
            var pull=raw+Vector3.right*.7f;hud.MovePreviewPointer(Camera.main.WorldToScreenPoint(pull),true,true,false);
            Assert.That(hud.IsAligned,Is.False,"Dragging out must release before a same-frame click can commit");
            hud.MovePreviewPointer(Camera.main.WorldToScreenPoint(raw),true,true,false);hud.AdvanceAlignment(.3f);Assert.That(hud.IsAligned,Is.True);
            hud.SendMessage("LateUpdate");
            var camera=Camera.main;camera.transform.position=new Vector3(9,13,-20);camera.transform.LookAt(new Vector3(6,0,-7));
            var target=new RenderTexture(1100,800,24);camera.targetTexture=target;camera.Render();RenderTexture.active=target;
            var pixels=new Texture2D(1100,800,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1100,800),0,0);pixels.Apply();
            System.IO.File.WriteAllBytes("/tmp/bs056-alignment.png",pixels.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=null;Object.Destroy(pixels);Object.Destroy(target);
            var frozen=hud.PreviewPosition;float yaw=hud.PreviewYaw;
            hud.MoveDesktopPointer(Vector2.zero,false,true);hud.AdvanceAlignment(1);
            Assert.That(hud.PreviewPosition,Is.EqualTo(frozen));
            GameObject.Find("Done").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            FacilityPlacementRecord row=null;foreach(var f in layout.Instances)if(f.Purchased&&f!=reference)row=f.Capture();
            Assert.That(row,Is.Not.Null);
            Assert.That(row.x,Is.EqualTo(frozen.x).Within(.001f));Assert.That(row.z,Is.EqualTo(frozen.z).Within(.001f));Assert.That(row.yaw,Is.EqualTo(yaw).Within(.001f));
            Assert.That(Object.FindFirstObjectByType<RestaurantPersistence>().Flush(),Is.True);
            yield return new ExitPlayMode();
        }
    }
}
