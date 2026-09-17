using BurgerShop.Core;
using BurgerShop.Restaurant;
using BurgerShop.UI;
using NUnit.Framework;
using UnityEngine;

namespace BurgerShop.Tests.EditMode
{
    public sealed class Spec064StartingShopTests
    {
        GameObject root;
        HrOffice office;
        BoostRoom bay;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Spec064");
            Material wall = RuntimeMaterials.Create(new Color(0.40f, 0.29f, 0.17f));
            Material floor = RuntimeMaterials.Create(new Color(0.72f, 0.70f, 0.62f));
            ShopLayout.CreateFloor(root.transform, floor);
            ShopLayout.CreateWalls(root.transform, wall);
            office = HrOffice.Create(root.transform, wall, floor);
            office.SetOpen(false);
            bay = BoostRoom.Create(root.transform, wall, floor);
            bay.SetAnnexOpen(false);
        }

        [TearDown]
        public void TearDown()
        {
            ShopLayout.ResetWingLock();
            Object.DestroyImmediate(root);
        }

        [Test]
        public void RankOneHasNoHrCarBayLandmarksOrOpenAnnexDoors()
        {
            var goals = root.AddComponent<SessionGoalTracker>();
            goals.Restore(1, 0, 0);
            Physics.SyncTransforms();
            Assert.That(office.gameObject.activeInHierarchy, Is.False);
            Assert.That(bay.transform.Find("CarServiceFloor").gameObject.activeSelf, Is.False);
            Assert.That(bay.transform.Find("CarServiceSide").gameObject.activeSelf, Is.False);
            Assert.That(GameObject.Find("Landmark_0"), Is.Null);
            Assert.That(GameObject.Find("Landmark_1"), Is.Null);
            Assert.That(GameObject.Find("Landmark_2"), Is.Null);
            Assert.That(GameObject.Find("Landmark_3"), Is.Null);
            Assert.That(root.transform.Find("HrDoorPlug"), Is.Not.Null);
            Assert.That(root.transform.Find("BoostDoorPlug"), Is.Not.Null);
            Assert.That(Physics.CheckBox(ShopLayout.HrDoor + Vector3.up * 0.6f, new Vector3(0.2f, 0.4f, 0.9f)), Is.True);
            Assert.That(Physics.CheckBox(ShopLayout.BoostDoor + Vector3.up * 0.6f, new Vector3(0.9f, 0.4f, 0.2f)),
                Is.True);
            Assert.That(bay.BoostPoint, Is.Not.Null);
            Assert.That(bay.BoostPoint.gameObject.activeInHierarchy, Is.True);
        }

        [Test]
        public void RankTwoDiningStillHasNoHrOrCarBay()
        {
            var goals = root.AddComponent<SessionGoalTracker>();
            goals.Restore(2, 0, 0);
            Assert.That(office.gameObject.activeInHierarchy, Is.False);
            Assert.That(bay.transform.Find("CarServiceFloor").gameObject.activeSelf, Is.False);
            Assert.That(root.transform.Find("HrDoorPlug"), Is.Not.Null);
            Assert.That(root.transform.Find("BoostDoorPlug"), Is.Not.Null);
        }

        [Test]
        public void RankThreeOpensHrAndRankFiveOpensTheCarBay()
        {
            var goals = root.AddComponent<SessionGoalTracker>();
            goals.Restore(3, 0, 0);
            Physics.SyncTransforms();
            Assert.That(office.gameObject.activeInHierarchy, Is.True);
            Assert.That(office.transform.Find("Desk"), Is.Not.Null);
            Assert.That(root.transform.Find("HrDoorPlug"), Is.Null);
            Assert.That(Physics.CheckBox(ShopLayout.HrDoor + Vector3.up * 0.6f, new Vector3(0.2f, 0.4f, 0.9f)),
                Is.False);
            Assert.That(bay.transform.Find("CarServiceFloor").gameObject.activeSelf, Is.False);

            goals.Restore(5, 0, 0);
            Physics.SyncTransforms();
            Assert.That(bay.transform.Find("CarServiceFloor").gameObject.activeSelf, Is.True);
            Assert.That(root.transform.Find("BoostDoorPlug"), Is.Null);
            Assert.That(Physics.CheckBox(ShopLayout.BoostDoor + Vector3.up * 0.6f, new Vector3(0.9f, 0.4f, 0.2f)),
                Is.False);
        }

        [Test]
        public void RankOneHidesDrinksPlotUntilRankSeven()
        {
            var goals = root.AddComponent<SessionGoalTracker>();
            var architecture = root.AddComponent<BurgerShop.Core.RestaurantArchitecture>();
            architecture.Configure();
            Transform drinks = root.transform.Find("DrinksPlannedLand");
            Assert.That(drinks, Is.Not.Null);
            Assert.That(drinks.gameObject.activeSelf, Is.False);
            goals.Restore(7, 0, 0);
            architecture.Refresh();
            Assert.That(drinks.gameObject.activeSelf, Is.True);
        }
    }
}
