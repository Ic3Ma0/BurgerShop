using BurgerShop.Customer;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using NUnit.Framework;
using UnityEngine;

namespace BurgerShop.Tests.EditMode
{
    public sealed class GrowthGameplayTests
    {
        GameObject root;
        [SetUp] public void Setup() => root = new GameObject("GrowthTest");
        [TearDown] public void Cleanup() => Object.DestroyImmediate(root);

        [Test] public void PermanentPaceAndTemporaryCapacityDoNotCompound()
        {
            Assert.That(PlayerBoost.MoveSpeed(0),Is.EqualTo(4.675f).Within(.0001f));
            Assert.That(StaffBoost.WalkSpeed(3),Is.EqualTo(3.8f*1.45f*.85f).Within(.0001f));
            var inv=root.AddComponent<BurgerInventory>();inv.Configure(4);
            var buff=root.AddComponent<TemporaryPowerups>();buff.Configure(inv,null,null,null);
            var station=root.AddComponent<ProductionStation>();station.Configure(root.transform,null,null);
            buff.Activate(PowerupKind.Gloves);
            Assert.That(inv.Capacity,Is.EqualTo(8));Assert.That(inv.Count,Is.Zero);
            for(int i=0;i<8;i++){station.Advance(10);Assert.That(inv.TryCollectFrom(station),Is.True);}
            buff.Advance(30);Assert.That(inv.Capacity,Is.EqualTo(4));Assert.That(inv.Count,Is.EqualTo(8));
            Assert.That(inv.TryCollectFrom(station),Is.False);
            for(int i=0;i<5;i++) inv.TryTakeBurger();
            Assert.That(inv.TryCollectFrom(station),Is.True);Assert.That(inv.Count,Is.EqualTo(4));
            buff.Activate(PowerupKind.Gloves);inv.ApplyBoostLevel(1);Assert.That(inv.Capacity,Is.EqualTo(9));
            buff.Advance(12);buff.Activate(PowerupKind.Gloves);Assert.That(buff.GlovesRemaining,Is.EqualTo(30));
            buff.Activate(PowerupKind.Skates);buff.SetPaused(true);buff.Advance(20);
            Assert.That(buff.SkatesRemaining,Is.EqualTo(30));Assert.That(buff.GlovesRemaining,Is.EqualTo(30));
        }

        [Test] public void CallsBlockUntilReminderOrNaturalTimeout()
        {
            var customer=CustomerAgent.Create(root.transform,1,Vector3.zero,2,CustomerKind.Calling);
            customer.AssignSlot(0);customer.MoveOnPath(Vector3.zero,1,true,Vector3.forward,.1f);
            customer.AdvanceCalling(.1f,null);customer.AdvanceCalling(7.9f,null);
            Assert.That(customer.CanAcceptOrder,Is.False);
            var player=new GameObject("Player");player.transform.SetParent(root.transform);
            customer.AdvanceCalling(.05f,player.transform);Assert.That(customer.CanAcceptOrder,Is.False);
            customer.AdvanceCalling(.06f,player.transform);Assert.That(customer.CanAcceptOrder,Is.True);
            var other=CustomerAgent.Create(root.transform,2,Vector3.zero,1,CustomerKind.Calling);
            other.AssignSlot(0);other.MoveOnPath(Vector3.zero,1,true,Vector3.forward,.1f);
            other.AdvanceCalling(.5f,player.transform);other.AdvanceCalling(.1f,null);
            other.AdvanceCalling(.5f,player.transform);Assert.That(other.CanAcceptOrder,Is.False);
            other.AdvanceCalling(.5f,player.transform);Assert.That(other.CanAcceptOrder,Is.True);
        }

        [Test] public void BigOrderAndSpecialSpacingRemainBounded()
        {
            var c=CustomerAgent.Create(root.transform,1,Vector3.zero,1,CustomerKind.BigEater);
            Assert.That(c.OrderSize,Is.EqualTo(10));
            for(int i=0;i<4;i++){Assert.That(c.Order.TryReserve(),Is.True);c.ReceiveItem(null);}
            Assert.That(c.RemainingQuantity,Is.EqualTo(6));Assert.That(c.Order.TrySettle(),Is.False);
            for(int i=0;i<6;i++){c.Order.TryReserve();c.ReceiveItem(null);}
            Assert.That(c.Order.TrySettle(),Is.True);Assert.That(c.Order.TrySettle(),Is.False);
            var policy=new SpecialCustomerPolicy();
            Assert.That(policy.Next(false,false,.99f),Is.EqualTo(CustomerKind.Normal));
            Assert.That(policy.Next(true,false,.9f),Is.EqualTo(CustomerKind.Calling));
            for(int i=0;i<3;i++)Assert.That(policy.Next(true,false,.99f),Is.EqualTo(CustomerKind.Normal));
            Assert.That(policy.Next(true,true,.99f),Is.EqualTo(CustomerKind.Normal));
            Assert.That(policy.Next(true,false,.99f),Is.EqualTo(CustomerKind.BigEater));
        }
    }
}
