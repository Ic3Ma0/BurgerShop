using System.Reflection;
using BurgerShop.Building;
using BurgerShop.Customer;
using BurgerShop.Restaurant;
using NUnit.Framework;
using UnityEngine;

namespace BurgerShop.Tests.EditMode
{
    public sealed class Spec090ReviewTests
    {
        [Test] public void NearbyReachableGridDoesNotMakeBlockedFinalLegReachable()
        {
            var floors=new[]{new Rect(-6,-6,12,12)};
            var obstacles=new[]{new PlacementFootprint(Vector2.zero,Vector2.one,0)};
            var nav=new LayoutNavigation(floors,obstacles);
            var from=new Vector3(-4,0,0);var blocked=new Vector3(.7f,0,0);
            Assert.That(nav.CanReach(from,new Vector3(1.5f,0,0)),Is.True,"Nearby floor really is reachable");
            Assert.That(nav.CanReach(from,blocked),Is.False,"The body cannot fit on the final leg");
            Assert.That(nav.Route(from,blocked),Is.Null);
            var target=new Vector3(2,0,1);var route=nav.Route(from,target);
            Assert.That(nav.CanReach(from,target),Is.True);
            Assert.That(route[route.Length-1],Is.EqualTo(target));
        }
        [TestCase(DiningTableKind.Pair,0,0)] [TestCase(DiningTableKind.Pair,90,1)]
        [TestCase(DiningTableKind.FourSeat,0,0)] [TestCase(DiningTableKind.FourSeat,90,1)]
        [TestCase(DiningTableKind.FourSeat,0,2)] [TestCase(DiningTableKind.FourSeat,90,3)]
        [TestCase(DiningTableKind.Square,90,0)] [TestCase(DiningTableKind.Square,0,1)]
        public void ActualChairMealHasContinuousMountAndDismount(DiningTableKind kind,float yaw,int slot)
        {
            var root=new GameObject("ReviewMeal");
            try
            {
                var table=DiningTable.Create(root.transform,Vector3.zero,kind);
                table.transform.rotation=Quaternion.Euler(0,yaw,0);
                Physics.SyncTransforms();
                for(int i=0;i<slot;i++)
                {
                    var occupied=CustomerAgent.Create(root.transform,20+i,new Vector3(6,0,6),1);
                    Assert.That(table.TryAssignSeat(occupied,out _,out _),Is.True);
                }
                var guest=CustomerAgent.Create(root.transform,1,new Vector3(-5,0,-4),1);
                guest.BeginDeparture(null,new[]{new Vector3(-5,0,-4)},0,DiningArea.Wrap(table),alreadyReceived:true);
                var phase=typeof(CustomerAgent).GetField("phase",BindingFlags.Instance|BindingFlags.NonPublic);
                bool mounting=false,eating=false,leaving=false,departed=false;int mountFrames=0,leaveFrames=0;
                for(int i=0;i<3600&&!guest.DepartureComplete;i++)
                {
                    var before=guest.transform.position;
                    guest.AdvanceDeparture(1f/60f);
                    if(guest==null){departed=true;break;}
                    Assert.That(Vector3.Distance(before,guest.transform.position),Is.LessThanOrEqualTo(1.92f/60f+.0001f),"Jump during "+phase.GetValue(guest));
                    var state=phase.GetValue(guest).ToString();
                    if(state=="Seating"){mounting=true;mountFrames++;Assert.That(guest.IsDining,Is.True);}
                    if(state=="Eating")eating=true;
                    if(state=="LeavingSeat"){leaving=true;leaveFrames++;Assert.That(guest.IsDining,Is.True);}
                }
                Assert.That(mounting&&eating&&leaving,Is.True,"Must exercise all three meal phases");
                Assert.That(mountFrames,Is.GreaterThan(5));Assert.That(leaveFrames,Is.GreaterThan(5));
                if(!departed&&!guest.DepartureComplete)
                {
                    var path=(Vector3[])typeof(CustomerAgent).GetField("exitRoute",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(guest);
                    TestContext.WriteLine("Exit path: "+string.Join(", ",path));
                }
                Assert.That(departed||guest.DepartureComplete,Is.True,"Must leave, not freeze at a chair: "+(guest!=null?phase.GetValue(guest)+" @ "+guest.transform.position:"destroyed"));
                Assert.That(table.TrashCount,Is.EqualTo(2));Assert.That(table.OccupiedSeats,Is.EqualTo(slot));
            }
            finally{Object.DestroyImmediate(root);}
        }
    }
}
