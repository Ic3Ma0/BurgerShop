using System;
using System.Reflection;
using BurgerShop.Core;
using BurgerShop.Customer;
using BurgerShop.Restaurant;
using NUnit.Framework;
using UnityEngine;

namespace BurgerShop.Tests.EditMode
{
    public sealed class Spec085ArrivalTests
    {
        GameObject root;
        [SetUp] public void Setup(){root=new GameObject("Spec085");}
        [TearDown] public void Cleanup(){UnityEngine.Object.DestroyImmediate(root);ShopLayout.CompactStart=false;ShopLayout.SmallFootprint=false;}
        static Vector3[] WorldPath(CustomerQueue q)
        {
            var local=(Vector3[])typeof(CustomerQueue).GetField("route",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(q);
            return Array.ConvertAll(local,q.transform.TransformPoint);
        }
        [TestCase(false)] [TestCase(true)]
        public void EnteringDoesNotOvershootQueueTailThenWalkBack(bool small)
        {
            ShopLayout.CompactStart=true;ShopLayout.SmallFootprint=small;
            var q=root.AddComponent<CustomerQueue>();q.Configure(ShopLayout.Entrance,ShopLayout.QueueEntry,ShopLayout.QueueSlots,ShopLayout.Counter);
            var path=WorldPath(q);var tail=ShopLayout.QueueSlots[2];
            int door=Array.FindIndex(path,p=>Vector3.Distance(p,RestaurantEntrance.Door)<.05f);
            Assert.That(door,Is.GreaterThanOrEqualTo(0));
            for(int i=door;i<path.Length-2;i++)
                Assert.That(path[i].z,Is.LessThanOrEqualTo(tail.z+.1f),"overshoots then returns: "+path[i]);
        }
        [Test]
        public void CurrentObstacleStillRequiresSafeDetourAfterShortening()
        {
            var layout=root.AddComponent<BurgerShop.Building.FacilityLayout>();
            layout.Configure(null,null,null,null,null,null,null,null);
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.SetParent(root.transform);
            wall.transform.position=new Vector3(-5,.8f,0);wall.transform.localScale=new Vector3(1,1.6f,8);
            Physics.SyncTransforms();
            var q=root.AddComponent<CustomerQueue>();
            var slots=new[]{new Vector3(0,0,6),new Vector3(0,0,4),new Vector3(0,0,2)};
            q.Configure(new Vector3(-10,0,-5),new Vector3(0,0,-5),slots,new Vector3(0,0,8),.1f,0);
            Assert.That(CustomerWalkPath.Clear(new Vector3(-10,0,-5),slots[2]),Is.False);
            for(int i=0;i<2400;i++)
            {
                q.Advance(1/60f);
                foreach(var c in q.Customers)Assert.That(CustomerWalkPath.Clear(c.transform.position,c.transform.position),Is.True);
            }
            Assert.That(q.ReadyCustomer,Is.Not.Null);
            for(int i=0;i<3;i++)Assert.That(q.Customers[i].transform.position,Is.EqualTo(slots[i]));
        }
        [Test]
        public void ObsoleteEntryDoesNotCauseDetourOrBacktracking()
        {
            var q=root.AddComponent<CustomerQueue>();var oldEntry=new Vector3(7,0,9);
            var slots=new[]{new Vector3(3,0,2),new Vector3(3,0,0),new Vector3(3,0,-2)};
            q.Configure(new Vector3(-7,0,-5),oldEntry,slots,new Vector3(3,0,4),.1f,0);
            var path=WorldPath(q);
            for(int i=0;i<path.Length-2;i++)Assert.That(path[i].z,Is.LessThanOrEqualTo(-2+.1f));
            Assert.That(Array.Exists(path,p=>Vector3.Distance(p,oldEntry)<.1f),Is.False);
            for(int i=0;i<2400;i++)q.Advance(1/60f);
            Assert.That(q.ReadyCustomer,Is.Not.Null);
            for(int i=0;i<3;i++){Assert.That(q.Customers[i].transform.position,Is.EqualTo(slots[i]));Assert.That(q.Customers[i].TicketNumber,Is.EqualTo(i+1));}
        }
    }
}
