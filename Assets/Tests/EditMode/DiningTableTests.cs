using BurgerShop.Customer;
using BurgerShop.Restaurant;
using NUnit.Framework;
using UnityEngine;

namespace BurgerShop.Tests.EditMode
{
    public sealed class DiningTableTests
    {
        GameObject root;
        DiningTable table;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("DiningTableTest");
            table = DiningTable.Create(root.transform, new Vector3(-5.6f, 0f, 0.2f));
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(root);

        [Test]
        public void BothChairsFaceTheTableWithBacksOutside()
        {
            Assert.That(table.SeatCount, Is.EqualTo(2));
            AssertChairFacesTable(table, "ChairA");
            AssertChairFacesTable(table, "ChairB");
        }

        [Test]
        public void ReversedChairYawWouldFailTheFacingAssertion()
        {
            Transform chair = table.transform.Find("ChairA");
            Vector3 towardTable = Flatten(table.Center - chair.position).normalized;
            chair.localRotation = Quaternion.LookRotation(-towardTable);
            Assert.That(FacesTable(table, chair), Is.False);
            Assert.That(BackIsOutside(table, chair), Is.False);
        }

        [Test]
        public void ShopTablesEachHaveTwoChairsFacingTheTop()
        {
            DiningArea area = DiningArea.Create(root.transform, DiningArea.ShopPositions);
            Assert.That(area.TableCount, Is.EqualTo(2));
            Assert.That(area.SeatCount, Is.EqualTo(4));
            Assert.That(area.Tables[0].Center, Is.EqualTo(ShopLayout.Tables[0]));
            Assert.That(area.Tables[1].Center, Is.EqualTo(ShopLayout.Tables[1]));
            for (int i = 0; i < area.TableCount; i++)
            {
                DiningTable dining = area.Tables[i];
                Assert.That(dining.SeatCount, Is.EqualTo(2), "table " + i);
                AssertChairFacesTable(dining, "ChairA");
                AssertChairFacesTable(dining, "ChairB");
            }
        }

        [Test]
        public void TwoTablesOfferFourSeatsThenAWaitAtTheEdge()
        {
            DiningArea area = DiningArea.Create(root.transform, DiningArea.ShopPositions);
            var guests = new CustomerAgent[5];
            for (int i = 0; i < 4; i++)
            {
                guests[i] = new GameObject("Guest_" + i).AddComponent<CustomerAgent>();
                guests[i].transform.SetParent(root.transform, false);
                Assert.That(area.TryAssignSeat(guests[i], out DiningTable seated, out _, out int seat), Is.True);
                Assert.That(seated, Is.Not.Null);
                Assert.That(seat, Is.GreaterThanOrEqualTo(0));
            }
            Assert.That(area.Tables[0].OccupiedSeats, Is.EqualTo(2));
            Assert.That(area.Tables[1].OccupiedSeats, Is.EqualTo(2));
            guests[4] = new GameObject("Guest_wait").AddComponent<CustomerAgent>();
            guests[4].transform.SetParent(root.transform, false);
            Assert.That(area.TryAssignSeat(guests[4], out _, out Vector3 wait, out int waitingSeat), Is.False);
            Assert.That(waitingSeat, Is.EqualTo(-1));
            Assert.That(area.OccupiedSeats, Is.EqualTo(4));
            Assert.That(wait.x, Is.LessThan(area.Tables[0].Center.x));
            area.Tables[1].Release(guests[2]);
            Assert.That(area.TryAssignSeat(guests[4], out DiningTable opened, out _, out int taken), Is.True);
            Assert.That(opened, Is.SameAs(area.Tables[1]));
            Assert.That(taken, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void ApplyingASetKeepsChairsFacingTheTableAndRaisesPay()
        {
            Assert.That(table.MealPay, Is.EqualTo(10));
            Assert.That(table.EatSeconds, Is.EqualTo(3f));
            table.ApplySet(TableSetId.Patio);
            Assert.That(table.MealPay, Is.EqualTo(16));
            Assert.That(table.EatSeconds, Is.EqualTo(3.6f));
            Assert.That(table.FurnitureLevel, Is.EqualTo(2));
            AssertChairFacesTable(table, "ChairA");
            AssertChairFacesTable(table, "ChairB");
            Color top = table.transform.Find("Top").GetComponent<Renderer>().sharedMaterial.color;
            Assert.That(top.b, Is.GreaterThan(top.r));
        }

        [Test]
        public void AddedExtraTableHasTwoChairsFacingTheTop()
        {
            DiningArea area = DiningArea.Create(root.transform, DiningArea.ShopPositions);
            DiningTable extra = area.AddTable(ShopLayout.ExtraTable);
            Assert.That(area.TableCount, Is.EqualTo(3));
            Assert.That(extra.SeatCount, Is.EqualTo(2));
            Assert.That(extra.Kind, Is.EqualTo(DiningTableKind.Pair));
            Assert.That(extra.Center, Is.EqualTo(ShopLayout.ExtraTable));
            AssertChairFacesTable(extra, "ChairA");
            AssertChairFacesTable(extra, "ChairB");
        }

        [Test]
        public void FourSeatTableHasFourChairsFacingTheTop()
        {
            DiningTable four = DiningTable.Create(root.transform, ShopLayout.FourSeatTable, DiningTableKind.FourSeat);
            Assert.That(four.SeatCount, Is.EqualTo(4));
            Assert.That(four.Kind, Is.EqualTo(DiningTableKind.FourSeat));
            Assert.That(four.Center, Is.EqualTo(ShopLayout.FourSeatTable));
            Vector3 top = four.transform.Find("Top").localScale;
            Assert.That(top.z, Is.GreaterThan(top.x * 1.4f));
            AssertChairFacesTable(four, "ChairA");
            AssertChairFacesTable(four, "ChairB");
            AssertChairFacesTable(four, "ChairC");
            AssertChairFacesTable(four, "ChairD");
            four.ApplySet(TableSetId.Patio);
            Assert.That(four.MealPay, Is.EqualTo(16));
            AssertChairFacesTable(four, "ChairC");
            AssertChairFacesTable(four, "ChairD");
        }

        [Test]
        public void SquareTableHasTallBackedChairsFacingTheTop()
        {
            DiningTable square = DiningTable.Create(root.transform, ShopLayout.SquareTable, DiningTableKind.Square);
            DiningTable pair = DiningTable.Create(root.transform, Vector3.zero);
            Assert.That(square.SeatCount, Is.EqualTo(2));
            Assert.That(square.Kind, Is.EqualTo(DiningTableKind.Square));
            Assert.That(square.Center, Is.EqualTo(ShopLayout.SquareTable));
            Vector3 squareTop = square.transform.Find("Top").localScale;
            Vector3 pairTop = pair.transform.Find("Top").localScale;
            Assert.That(squareTop.x, Is.EqualTo(squareTop.z));
            Assert.That(squareTop.x, Is.LessThan(pairTop.x));
            float squareBack = square.transform.Find("ChairA/Back").localScale.y;
            float pairBack = pair.transform.Find("ChairA/Back").localScale.y;
            Assert.That(squareBack, Is.GreaterThan(0.75f));
            Assert.That(pairBack, Is.LessThan(0.6f));
            Assert.That(squareBack, Is.GreaterThan(pairBack));
            Assert.That(square.transform.Find("ChairA/BackCushion"), Is.Not.Null);
            AssertChairFacesTable(square, "ChairA");
            AssertChairFacesTable(square, "ChairB");
            square.ApplySet(TableSetId.Bistro);
            AssertChairFacesTable(square, "ChairA");
            AssertChairFacesTable(square, "ChairB");
        }

        void AssertChairFacesTable(DiningTable dining, string name)
        {
            Transform chair = dining.transform.Find(name);
            Assert.That(chair, Is.Not.Null, name + " missing on " + dining.name);
            Assert.That(FacesTable(dining, chair), Is.True, name + " must look toward the tabletop; reversed chairs fail this");
            Assert.That(BackIsOutside(dining, chair), Is.True, name + " backrest must sit outside the tabletop, not against it");
        }

        bool FacesTable(DiningTable dining, Transform chair)
        {
            Vector3 towardTable = Flatten(dining.Center - chair.position);
            Vector3 forward = Flatten(chair.forward);
            if (towardTable.sqrMagnitude < 0.0001f || forward.sqrMagnitude < 0.0001f) return false;
            return Vector3.Dot(forward.normalized, towardTable.normalized) > 0.9f;
        }

        bool BackIsOutside(DiningTable dining, Transform chair)
        {
            Transform back = chair.Find("Back");
            if (back == null) return false;
            Vector3 center = Flatten(dining.Center);
            return Vector3.Distance(Flatten(back.position), center) >
                   Vector3.Distance(Flatten(chair.position), center);
        }

        static Vector3 Flatten(Vector3 value)
        {
            value.y = 0f;
            return value;
        }
    }
}
