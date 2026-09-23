using System.Reflection;
using BurgerShop.Building;
using BurgerShop.Customer;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using NUnit.Framework;
using UnityEngine;

namespace BurgerShop.Tests.EditMode
{
    public sealed class Spec090ObstacleTests
    {
        GameObject root;
        FacilityLayout layout;
        [SetUp] public void Setup()
        {
            root=new GameObject("ObstacleTest");layout=root.AddComponent<FacilityLayout>();
            layout.Configure(null,null,null,null,null,null,null,null);
        }
        [TearDown] public void Cleanup(){Object.DestroyImmediate(root);}
        GameObject Obstacle(PrimitiveType type,Vector3 position,Vector3 scale,float yaw=0)
        {
            var o=GameObject.CreatePrimitive(type);o.transform.SetParent(root.transform);
            o.transform.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));o.transform.localScale=scale;
            Physics.SyncTransforms();layout.RefreshNavigation();return o;
        }
        void AssertPath(Vector3 from,Vector3 to)
        {
            var path=layout.Route(from,to);Assert.That(path,Is.Not.Null);
            foreach(var p in path){Assert.That(ActorObstacles.Clear(from,p),Is.True,from+" -> "+p);from=p;}
            Assert.That(Vector3.Distance(from,to),Is.LessThan(.01f));
        }
        [TestCase(PrimitiveType.Cube,0f)] [TestCase(PrimitiveType.Cube,37f)] [TestCase(PrimitiveType.Cylinder,0f)]
        public void RoutesAroundSolidIncludingRotatedAndRoundBodies(PrimitiveType type,float yaw)
        {
            Obstacle(type,new Vector3(0,.8f,0),new Vector3(2,1.6f,4),yaw);
            Assert.That(ActorObstacles.Clear(new Vector3(-5,1.05f,0),new Vector3(5,1.05f,0)),Is.False);
            AssertPath(new Vector3(-5,1.05f,0),new Vector3(5,1.05f,0));
        }
        [Test] public void CommittedObstacleInvalidatesCachedRoute()
        {
            AssertPath(new Vector3(-5,0,0),new Vector3(5,0,0));
            var body=Obstacle(PrimitiveType.Cube,new Vector3(0,.8f,0),new Vector3(2,1.6f,4));
            AssertPath(new Vector3(-5,0,0),new Vector3(5,0,0));
            body.transform.position=new Vector3(3,.8f,0);Physics.SyncTransforms();layout.RefreshNavigation();
            AssertPath(new Vector3(-5,0,0),new Vector3(5,0,0));
        }
        [Test] public void OccupiedInteractionTargetIsNotSilentlyReplaced()
        {
            Obstacle(PrimitiveType.Cube,new Vector3(0,.8f,0),Vector3.one);
            Assert.That(layout.Route(new Vector3(-5,0,0),Vector3.zero),Is.Null);
        }
        [Test] public void FullyBlockedRouteNeverFallsBackThroughWall()
        {
            Obstacle(PrimitiveType.Cube,new Vector3(0,1,0),new Vector3(1,2,80));
            Assert.That(layout.Route(new Vector3(-5,0,0),new Vector3(5,0,0)),Is.Null);
            Assert.That(ShopLayout.Walk(new Vector3(-5,0,0),new Vector3(5,0,0)),Is.Empty);
        }
        [Test] public void CustomerLongFrameDetoursInsteadOfCrossingFurniture()
        {
            Obstacle(PrimitiveType.Cube,new Vector3(0,.8f,0),new Vector3(2,1.6f,4));
            var customer=CustomerAgent.Create(root.transform,1,new Vector3(-5,0,0),1,CustomerKind.Normal,KitchenProduct.Burger);
            var move=typeof(CustomerAgent).GetMethod("MoveOnPath",BindingFlags.Instance|BindingFlags.NonPublic);
            var dest=new Vector3(5,0,0);
            for(int i=0;i<200&&!customer.HasReachedSlot;i++)
            {
                var before=customer.transform.position;
                move.Invoke(customer,new object[]{dest,10f,true,dest,.1f});
                Assert.That(ActorObstacles.Clear(before,customer.transform.position),Is.True);
                Assert.That(Vector3.Distance(before,customer.transform.position),Is.LessThanOrEqualTo(.351f));
            }
            Assert.That(customer.HasReachedSlot,Is.True);
        }
        [Test] public void RelayoutDoesNotTeleportCustomer()
        {
            var start=new Vector3(-5,0,0);
            var customer=CustomerAgent.Create(root.transform,1,start,1,CustomerKind.Normal,KitchenProduct.Burger);
            typeof(CustomerAgent).GetMethod("MoveOnPath",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(customer,
                new object[]{new Vector3(5,0,0),10f,true,Vector3.zero,0f});
            Assert.That(customer.transform.position,Is.EqualTo(start));Assert.That(customer.HasReachedSlot,Is.False);
        }
        [Test] public void EnlargedMachineRefreshesNavigation()
        {
            var machine=ExpandableGrill.CreateStarter(root.transform,null,null);
            machine.transform.position=Vector3.zero;Physics.SyncTransforms();layout.RefreshNavigation();
            AssertPath(new Vector3(-5,0,0),new Vector3(5,0,0));
            int revision=layout.Revision;machine.ApplyLook(3,false);
            Assert.That(layout.Revision,Is.GreaterThan(revision));
            AssertPath(new Vector3(-5,0,0),new Vector3(5,0,0));
        }
        [TestCase(false)] [TestCase(true)]
        public void PlayerCannotWalkThroughActualMachineOrTable(bool table)
        {
            if(table)DiningTable.Create(root.transform,Vector3.zero);
            else ExpandableGrill.CreateStarter(root.transform,null,null).transform.position=Vector3.zero;
            Physics.SyncTransforms();
            var actor=new GameObject("Player");actor.transform.SetParent(root.transform);actor.transform.position=new Vector3(-5,1.05f,0);
            var cc=actor.AddComponent<CharacterController>();cc.height=2;cc.radius=.4f;
            for(int i=0;i<100;i++)cc.Move(Vector3.right*.1f);
            Assert.That(actor.transform.position.x,Is.LessThan(-.5f));
        }
        [Test] public void PlayerControllerCannotCrossNewSolid()
        {
            Obstacle(PrimitiveType.Cube,new Vector3(0,.8f,0),new Vector3(2,1.6f,4));
            var actor=new GameObject("Player");actor.transform.SetParent(root.transform);actor.transform.position=new Vector3(-5,1.05f,0);
            var cc=actor.AddComponent<CharacterController>();cc.height=2;cc.radius=.4f;
            for(int i=0;i<100;i++)cc.Move(Vector3.right*.1f);
            Assert.That(actor.transform.position.x,Is.LessThan(-1.3f));
        }
    }
}
