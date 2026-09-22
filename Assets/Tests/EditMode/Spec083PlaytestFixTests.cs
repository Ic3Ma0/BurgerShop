using System.Collections.Generic;
using BurgerShop.Core;
using BurgerShop.Customer;
using BurgerShop.Persistence;
using BurgerShop.Restaurant;
using BurgerShop.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace BurgerShop.Tests.EditMode
{
    public sealed class Spec083PlaytestFixTests
    {
        GameObject root;
        [SetUp] public void Setup(){root=new GameObject("Spec083");}
        [TearDown] public void Cleanup(){Object.DestroyImmediate(root);ShopLayout.CompactStart=false;ShopLayout.SmallFootprint=false;}
        [Test] public void NewStoreHasOnlyOwnedPerimeterAndOldSavesKeepTheirSize()
        {
            var mat=RuntimeMaterials.Create(Color.white);
            ShopLayout.CreateFloor(root.transform,mat);ShopLayout.CreateWalls(root.transform,mat);
            var hall=root.AddComponent<MainHallExpansion>();hall.Initialize(true,false,0);
            Assert.That(hall.Bounds.width*hall.Bounds.height,Is.EqualTo(300));
            Assert.That(root.transform.Find("Wall+Z").gameObject.activeSelf,Is.False);
            var north=root.transform.Find("MainHallConstruction/WallSmallNorth");Assert.That(north.position.z,Is.EqualTo(0));
            hall.Restore(true,false,40,false);Assert.That(hall.Bounds,Is.EqualTo(MainHallExpansion.PreviousStarterBounds));
            Assert.That(hall.Remaining,Is.EqualTo(110));Assert.That(ShopLayout.Grill,Is.EqualTo(new Vector3(4,0,1.5f)));
            hall.Restore(false,true,0,false);Assert.That(hall.Bounds,Is.EqualTo(MainHallExpansion.FullBounds));
            Assert.That(root.transform.Find("Wall+Z").gameObject.activeSelf,Is.True);Object.DestroyImmediate(mat);
        }
        [Test] public void LayoutProfileIsSavedAndOldSchemasNeverShrink()
        {
            var d=new RestaurantSaveData{version=19,compactStart=true,smallFootprint=true};
            string checksum=d.Checksum();Assert.That(d.ResolvedSmallFootprint,Is.True);
            d.smallFootprint=false;Assert.That(d.Checksum(),Is.Not.EqualTo(checksum));
            d.smallFootprint=true;d.version=18;Assert.That(d.ResolvedSmallFootprint,Is.False);
            d.version=17;Assert.That(d.ResolvedMainHallBuilt,Is.True);
        }
        [Test] public void PathsHaveStablePersonalLanesRoundedTurnsAndExactDestinations()
        {
            var source=new[]{new Vector3(-8,0,-5),new Vector3(0,0,-5),new Vector3(0,0,2),new Vector3(4,0,2)};
            var a=new CustomerWalkPath(source,1);var b=new CustomerWalkPath(source,2);var again=new CustomerWalkPath(source,1);
            Assert.That(a.Points.Length,Is.GreaterThan(source.Length));
            Assert.That(a.At(a.Length),Is.EqualTo(source[3]));Assert.That(a.At(0),Is.EqualTo(source[0]));
            Assert.That(Vector3.Distance(a.At(a.Length*.45f),b.At(b.Length*.45f)),Is.GreaterThan(.05f));
            Assert.That(again.Points,Is.EqualTo(a.Points));
            var random=Random.state;new CustomerWalkPath(source,2);Assert.That(Random.state,Is.EqualTo(random));
            Vector3 previous=a.At(0),heading=Vector3.zero;
            for(float d=.04f;d<a.Length;d+=.04f){var p=a.At(d);var dir=(p-previous).normalized;if(heading!=Vector3.zero)Assert.That(Vector3.Angle(dir,heading),Is.LessThan(18));heading=dir;previous=p;}
        }
        [Test] public void SmoothingDoesNotCutThroughAnObstacle()
        {
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.SetParent(root.transform);wall.transform.position=new Vector3(.85f,.7f,.85f);wall.transform.localScale=new Vector3(1,1.4f,1);
            Physics.SyncTransforms();var source=new[]{new Vector3(-4,0,0),Vector3.zero,new Vector3(0,0,4)};
            var path=new CustomerWalkPath(source,4);
            for(float d=0;d<path.Length;d+=.06f)Assert.That(CustomerWalkPath.Clear(path.At(d),path.At(d)),Is.True,path.At(d).ToString());
        }
        [Test] public void QueueWalkersFaceTheirActualMotionAroundBendsAndKeepOrder()
        {
            var queue=root.AddComponent<CustomerQueue>();queue.OrderQuantityFactory=()=>1;
            var slots=new[]{new Vector3(3,0,6),new Vector3(3,0,3),new Vector3(3,0,0)};
            queue.Configure(new Vector3(-7,0,-5),new Vector3(3,0,-5),slots,new Vector3(3,0,8),.1f,0);
            var positions=new Dictionary<CustomerAgent,Vector3>();
            for(int t=0;t<1800;t++)
            {
                foreach(var c in queue.Customers)positions[c]=c.transform.position;
                queue.Advance(1/60f);
                foreach(var c in queue.Customers)if(positions.TryGetValue(c,out var old))
                {
                    var delta=c.transform.position-old;
                    if(delta.sqrMagnitude>.000001f)Assert.That(Vector3.Dot(c.transform.forward,delta.normalized),Is.GreaterThan(.995f));
                }
                for(int i=1;i<queue.Count;i++)Assert.That(Vector3.Distance(queue.Customers[i].transform.position,queue.Customers[i-1].transform.position),Is.GreaterThanOrEqualTo(.84f));
            }
            Assert.That(queue.ReadyCustomer,Is.Not.Null);Assert.That(queue.Count,Is.EqualTo(3));
            for(int i=0;i<3;i++){Assert.That(queue.Customers[i].TicketNumber,Is.EqualTo(i+1));Assert.That(queue.Customers[i].transform.position,Is.EqualTo(slots[i]));}
        }
        [Test] public void ChineseHudHasBoundedTypographyAndNoTaskDescriptionInStarBar()
        {
            var canvas=root.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            var goals=root.AddComponent<SessionGoalTracker>();goals.Configure(null,null,null,null,null,null);
            var hud=TaskCapsuleHud.Build(root.transform,goals);var stars=StarProgressHud.Build(root.transform,goals);
            Assert.That(stars.transform.Find("RankAction"),Is.Null);
            Assert.That(stars.transform.Find("StarBarBack/StarValue").GetComponent<Text>().text,Is.EqualTo("0/2"));
            string[] content={"先取一个汉堡","站到收银处，完成首单","金币扩建员工移动清理星升级"};
            var font=HudChrome.ChineseFont();Assert.That(font.name,Does.Contain("Noto"));
            foreach(var str in content){font.RequestCharactersInTexture(str,20);foreach(char c in str)Assert.That(font.HasCharacter(c),Is.True,c.ToString());}
            foreach(var text in hud.GetComponentsInChildren<Text>())
            {
                Assert.That(text.fontSize,Is.LessThanOrEqualTo(20));
                if(text.name=="TaskTitle"||text.name=="InvestmentDetail")Assert.That(text.horizontalOverflow,Is.EqualTo(HorizontalWrapMode.Wrap));
            }
        }
    }
}
