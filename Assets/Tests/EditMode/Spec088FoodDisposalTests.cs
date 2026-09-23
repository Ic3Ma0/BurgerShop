using BurgerShop.Player;
using BurgerShop.Restaurant;
using NUnit.Framework;
using UnityEngine;
namespace BurgerShop.Tests.EditMode
{
 public sealed class Spec088FoodDisposalTests
 {
  GameObject root;BurgerInventory food;TrashBin bin;
  [SetUp] public void Setup()
  {
   root=new GameObject("DisposalTest");var player=new GameObject("Player");player.transform.SetParent(root.transform);
   food=player.AddComponent<BurgerInventory>();food.Configure(4);
   bin=TrashBin.Create(root.transform,Vector3.zero);bin.Configure(player.AddComponent<TrashInventory>());
  }
  [TearDown] public void Cleanup()=>Object.DestroyImmediate(root);
  void Add(CarriedItemKind kind)
  {
   var item=new GameObject(kind.ToString());item.transform.SetParent(root.transform);
   Assert.That(food.TryReceive(kind,item.transform),Is.True);
  }
  [TestCase(CarriedItemKind.Cola)] [TestCase(CarriedItemKind.Burger)]
  public void FullHandsEmptyGraduallyAndCanCollectBurgersAgain(CarriedItemKind kind)
  {
   for(int i=0;i<4;i++)Add(kind);
   Assert.That(food.IsFull,Is.True);bin.Advance(.01f);Assert.That(food.Count,Is.EqualTo(3));
   for(int i=0;i<20;i++)bin.Advance(.05f);
   Assert.That(food.Count,Is.Zero);Assert.That(Object.FindObjectsByType<TrashMotion>(FindObjectsSortMode.None),Is.Empty);
   var station=root.AddComponent<ProductionStation>();var output=new GameObject("Output");output.transform.SetParent(root.transform);
   station.Configure(output.transform,null,null);station.Advance(4);
   Assert.That(food.TryCollectFrom(station),Is.True);Assert.That(food.LooseCount,Is.EqualTo(1));
  }
  [Test] public void LeavingPausingAndDisabledBinDoNotDiscardMore()
  {
   Add(CarriedItemKind.Cola);Add(CarriedItemKind.Cola);
   food.transform.position=Vector3.right*5;bin.Advance(1);Assert.That(food.Count,Is.EqualTo(2));
   food.transform.position=Vector3.zero;bin.Advance(0);Assert.That(food.Count,Is.EqualTo(2));
   bin.enabled=false;bin.Advance(1);Assert.That(food.Count,Is.EqualTo(2));bin.enabled=true;
   bin.Advance(.01f);Assert.That(food.Count,Is.EqualTo(1));
   food.transform.position=Vector3.right*5;bin.Advance(1);Assert.That(food.Count,Is.EqualTo(1));
   Assert.That(Object.FindObjectsByType<TrashMotion>(FindObjectsSortMode.None),Is.Empty);
  }
  [Test] public void OtherPackagesAreNotDestroyed()
  {
   Add(CarriedItemKind.Boxed);Add(CarriedItemKind.Bagged);Add(CarriedItemKind.EmptyBag);Add(CarriedItemKind.RedParcel);
   bin.Advance(5);Assert.That(food.Count,Is.EqualTo(4));
  }
 }
}
