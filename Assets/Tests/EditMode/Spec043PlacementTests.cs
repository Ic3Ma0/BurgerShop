using BurgerShop.Building;
using NUnit.Framework;
using UnityEngine;

namespace BurgerShop.Tests.EditMode
{
    public sealed class Spec043PlacementTests
    {
        [Test] public void LocalRasterizationMatchesFullGridForRotatedObstacles()
        {
            var floors=new[]{new Rect(-15,-15,30,30),new Rect(15,-8,23,20)};
            var obstacles=new System.Collections.Generic.List<PlacementFootprint>();
            var random=new System.Random(43);
            for(int i=0;i<100;i++)obstacles.Add(new PlacementFootprint(
                new Vector2((float)random.NextDouble()*60-20,(float)random.NextDouble()*40-20),
                new Vector2(.1f+(float)random.NextDouble()*6,.1f+(float)random.NextDouble()*6),i*15));
            var timer=System.Diagnostics.Stopwatch.StartNew();
            var nav=new LayoutNavigation(floors,obstacles);timer.Stop();double optimized=timer.Elapsed.TotalMilliseconds;
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var actual=(bool[])typeof(LayoutNavigation).GetField("walkable",flags).GetValue(nav);
            int width=(int)typeof(LayoutNavigation).GetField("width",flags).GetValue(nav);
            var expected=new bool[actual.Length];timer.Restart();
            for(int i=0;i<actual.Length;i++)
            {
                var cell=new PlacementFootprint(new Vector2(-15+(i%width+.5f)*.5f,-15+(i/width+.5f)*.5f),Vector2.one*.8f,0);
                bool clear=PlacementGeometry.CoveredByFloor(cell,floors);
                if(clear)foreach(var obstacle in obstacles)if(PlacementGeometry.Overlaps(cell,obstacle)){clear=false;break;}
                expected[i]=clear;
            }
            timer.Stop();
            CollectionAssert.AreEqual(expected,actual,"Optimization must preserve every walkable cell, including rotated edges and floor seams.");
            TestContext.WriteLine($"Grid {actual.Length} cells / {obstacles.Count} obstacles: optimized {optimized:F2} ms, full scan {timer.Elapsed.TotalMilliseconds:F2} ms");
        }
        [Test] public void RotatedCornerCannotEscapeFloorAlthoughCenterIsInside()
        {
            var floor=new[]{new Rect(0,0,10,10)};
            Assert.That(PlacementGeometry.CoveredByFloor(new PlacementFootprint(new Vector2(9,5),new Vector2(2,2),0),floor),Is.True);
            Assert.That(PlacementGeometry.CoveredByFloor(new PlacementFootprint(new Vector2(9,5),new Vector2(2,2),45),floor),Is.False);
        }
        [Test] public void FourValidCornersDoNotPermitBridgingLockedLand()
        {
            var floors=new[]{new Rect(0,0,2,4),new Rect(3,0,2,4)};
            var footprint=new PlacementFootprint(new Vector2(2.5f,2),new Vector2(4,2),0);
            foreach(var corner in footprint.Corners)
                Assert.That(floors[0].Contains(corner)||floors[1].Contains(corner),Is.True);
            Assert.That(PlacementGeometry.CoveredByFloor(footprint,floors),Is.False);
        }
        [Test] public void AdjacentUnlockedFloorsCoverOneFacilityWithoutDoubleCounting()
        {
            var footprint=new PlacementFootprint(new Vector2(2,2),new Vector2(3,2),15);
            Assert.That(PlacementGeometry.CoveredByFloor(footprint,new[]{new Rect(0,0,2,4),new Rect(2,0,2,4)}),Is.True);
            Assert.That(PlacementGeometry.CoveredByFloor(footprint,new[]{new Rect(0,0,2,4),new Rect(0,0,2,4)}),Is.False);
        }
        [Test] public void RotatedCollisionAndRequiredClearanceAreEnforced()
        {
            var a=new PlacementFootprint(Vector2.zero,new Vector2(4,1),45);
            var overlap=new PlacementFootprint(new Vector2(1,1),Vector2.one,0);
            var outside=new PlacementFootprint(new Vector2(4,0),Vector2.one,0);
            Assert.That(PlacementGeometry.Overlaps(a,overlap),Is.True);
            Assert.That(PlacementGeometry.Overlaps(a,outside),Is.False);
            var b=new PlacementFootprint(new Vector2(1.2f,0),Vector2.one,0);
            Assert.That(PlacementGeometry.Overlaps(new PlacementFootprint(Vector2.zero,Vector2.one,0),b,.3f),Is.True);
        }
        [Test] public void CorruptGeometryIsNeverAccepted()
        {
            foreach(float bad in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity})
            {
                var shape=new PlacementFootprint(new Vector2(bad,0),Vector2.one,0);
                Assert.That(PlacementGeometry.CoveredByFloor(shape,new[]{new Rect(-10,-10,20,20)}),Is.False);
                Assert.That(PlacementGeometry.Overlaps(shape,new PlacementFootprint(Vector2.zero,Vector2.one,0)),Is.True);
            }
            Assert.That(PlacementGeometry.CoveredByFloor(new PlacementFootprint(Vector2.zero,Vector2.zero,0),new[]{new Rect(-10,-10,20,20)}),Is.False);
        }
        [Test] public void FurnitureCannotSealAnOtherwiseOpenServiceRoute()
        {
            var floor=new[]{new Rect(0,0,12,12)};
            Vector2 start=new Vector2(2,6),destination=new Vector2(10,6);
            Assert.That(PlacementAccess.AllReachable(floor,new PlacementFootprint[0],start,new[]{destination}),Is.True);
            Assert.That(PlacementAccess.AllReachable(floor,new[]{new PlacementFootprint(new Vector2(6,6),new Vector2(1,12),0)},start,new[]{destination}),Is.False);
            Assert.That(PlacementAccess.AllReachable(floor,new[]{new PlacementFootprint(new Vector2(6,6),new Vector2(1,4),0)},start,new[]{destination}),Is.True);
        }
        [Test] public void DisconnectedRoomsAndInvalidDestinationsAreRejected()
        {
            var floor=new[]{new Rect(0,0,6,6),new Rect(8,0,6,6)};
            Assert.That(PlacementAccess.AllReachable(floor,null,new Vector2(2,2),new[]{new Vector2(10,2)}),Is.False);
            Assert.That(PlacementAccess.AllReachable(floor,null,new Vector2(2,2),new[]{new Vector2(float.NaN,2)}),Is.False);
        }
        [Test] public void RepeatPurchasesIncreasePriceAndOverflowSafely()
        {
            Assert.That(FacilityCatalog.Price(FacilityKind.BurgerMachine,0),Is.EqualTo(200));
            Assert.That(FacilityCatalog.Price(FacilityKind.BurgerMachine,1),Is.EqualTo(250));
            Assert.That(FacilityCatalog.Price(FacilityKind.BurgerMachine,2),Is.EqualTo(320));
            Assert.That(FacilityCatalog.Price(FacilityKind.BurgerMachine,int.MaxValue),Is.EqualTo(int.MaxValue));
            foreach(var offer in FacilityCatalog.Offers)
            {
                int previous=0;
                for(int i=0;i<100;i++)
                {int price=FacilityCatalog.Price(offer.Kind,i);Assert.That(price,Is.GreaterThanOrEqualTo(previous));previous=price;}
            }
        }
        [Test] public void LockedProductsStayLockedAndLegacyAccessIsPreserved()
        {
            Assert.That(FacilityCatalog.IsUnlocked(FacilityKind.ColaMachine,6,false),Is.False);
            Assert.That(FacilityCatalog.IsUnlocked(FacilityKind.ColaMachine,7,false),Is.True);
            Assert.That(FacilityCatalog.IsUnlocked(FacilityKind.ColaMachine,1,true),Is.True);
            Assert.That(FacilityCatalog.Offers.Count,Is.EqualTo(15));
        }
    }
}
