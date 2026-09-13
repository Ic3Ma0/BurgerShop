using BurgerShop.Building;
using BurgerShop.Economy;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using NUnit.Framework;
using UnityEngine;

namespace BurgerShop.Tests.EditMode
{
    public sealed class Spec043RoadTests
    {
        GameObject root;
        [SetUp] public void Setup()=>root=new GameObject("RoadRelayout");
        [TearDown] public void Cleanup()=>Object.DestroyImmediate(root);
        [Test] public void RoadSurfaceAndWaypointsShareTranslatedRotatedPose()
        {
            var road=new VehicleRoadLayout(new[]{Vector3.zero,new Vector3(8,0,0)},VehicleRoadLayout.CarWidth);
            var points=road.WorldPoints(new Vector3(2,0,3),90);
            Assert.That(Vector3.Distance(points[1],new Vector3(2,0,-5)),Is.LessThan(.001f));
            Assert.That(road.CanPlace(Vector3.zero,0,new[]{new Rect(-12,-12,24,24)},null),Is.True);
            Assert.That(road.CanPlace(new Vector3(11,0,0),0,new[]{new Rect(-12,-12,24,24)},null),Is.False);
            Assert.That(road.CanPlace(Vector3.zero,0,new[]{new Rect(-12,-12,24,24)},new[]{new PlacementFootprint(new Vector2(4,0),Vector2.one,0)}),Is.False);
        }
        [Test] public void RotatingLiveCarLaneKeepsCarsAtNewWindowAndPreservesOrders()
        {
            var actor=new GameObject("Actor").AddComponent<BurgerInventory>();actor.transform.SetParent(root.transform);
            var wallet=root.AddComponent<RestaurantWallet>();var cash=root.AddComponent<CashFloor>();cash.Configure(wallet,actor.transform,Vector3.zero);
            var lane=DriveThruLane.Create(root.transform,null,wallet,cash,actor);lane.OrderQuantityFactory=()=>2;
            for(int i=0;i<200;i++)lane.Advance(.1f);
            Assert.That(lane.HasStoppedCarAtWindow,Is.True);
            var order=lane.WaitingOrder;
            lane.transform.SetPositionAndRotation(new Vector3(5,0,6),Quaternion.Euler(0,90,0));
            lane.Advance(.1f);
            Assert.That(lane.HasStoppedCarAtWindow,Is.True);
            Assert.That(lane.WaitingOrder,Is.SameAs(order));Assert.That(order.Remaining,Is.EqualTo(2));
            Assert.That(Vector3.Distance(lane.WaitingCarPosition,lane.transform.TransformPoint(ShopLayout.DriveThruQueue[0])),Is.LessThan(.01f));
            Assert.That(Vector3.Distance(lane.CashPosition,lane.transform.TransformPoint(ShopLayout.DriveThruCash)),Is.LessThan(.001f));
            Assert.That(wallet.CompletedSales,Is.Zero);
        }
        [Test] public void ConveyorLoadsAndMovingSlatsStayOnRelocatedBelt()
        {
            var material=BurgerShop.Core.RuntimeMaterials.Create(Color.gray);root.AddComponent<BurgerVisual>().OwnMaterials(material);
            var belt=new CourierConveyor(root.transform,"Belt",new[]{Vector3.zero,new Vector3(4,0,0)},material,material);
            var item=new GameObject("Cargo").transform;belt.LoadItem(item);belt.Advance(.2f,_=>false);
            var before=item.position;
            root.transform.SetPositionAndRotation(new Vector3(5,0,6),Quaternion.Euler(0,90,0));
            belt.Advance(0,_=>false);
            Assert.That(Vector3.Distance(item.position,root.transform.TransformPoint(before)),Is.LessThan(.001f));
            Assert.That(belt.Count,Is.EqualTo(1));
        }
        [Test] public void RelocatedCourierRoadStillCollectsParcelsAndPaysOnlyOnce()
        {
            var actor=new GameObject("Actor").AddComponent<BurgerInventory>();actor.transform.SetParent(root.transform);actor.Configure(8);
            var wallet=root.AddComponent<RestaurantWallet>();var parts=root.AddComponent<PartsWallet>();
            var cash=root.AddComponent<CashFloor>();cash.Configure(wallet,actor.transform,Vector3.zero);
            var line=root.AddComponent<CourierLine>();line.Configure(wallet,parts,actor,cash);
            line.LayoutRoot.SetPositionAndRotation(new Vector3(5,0,6),Quaternion.Euler(0,90,0));
            for(int i=0;i<4;i++)actor.TryReceive(CarriedItemKind.RedParcel,CourierVisuals.RedParcel(root.transform));
            actor.transform.position=line.LayoutRoot.TransformPoint(CourierLine.DropPoint);
            for(int i=0;i<600;i++)line.Advance(.05f);
            Assert.That(line.CompletedOrders,Is.EqualTo(1));Assert.That(cash.GroundValue,Is.EqualTo(80));
            Assert.That(line.GroundParts,Is.EqualTo(4));Assert.That(actor.RedParcelCount,Is.Zero);
            actor.transform.position=line.LayoutRoot.TransformPoint(CourierLine.PartsPoint);
            line.Advance(.3f);Assert.That(parts.Balance,Is.EqualTo(4));
            actor.transform.position=line.LayoutRoot.TransformPoint(CourierLine.CashPoint);cash.Advance(1);
            Assert.That(wallet.Coins,Is.EqualTo(80));
            for(int i=0;i<300;i++)line.Advance(.05f);
            Assert.That(line.CompletedOrders,Is.EqualTo(1));
        }
        [Test] public void RoadCannotBridgeALockedGapBetweenItsEndpoints()
        {
            var road=new VehicleRoadLayout(new[]{new Vector3(2,0,3),new Vector3(10,0,3)},2);
            Assert.That(road.CanPlace(Vector3.zero,0,new[]{new Rect(0,0,4,6),new Rect(8,0,4,6)},null),Is.False);
        }
    }
}
