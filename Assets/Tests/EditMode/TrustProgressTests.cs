using System.Collections;
using BurgerShop.Building;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using BurgerShop.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace BurgerShop.Tests.EditMode
{
    public sealed class TrustProgressTests : SaveIsolatedGameplayTest
    {
        [Test]
        public void EveryPaidCarryLevelRaisesCapacityImmediately()
        {
            Assert.That(PlayerBoost.CarryCapacity(8), Is.EqualTo(12));
            Assert.That(PlayerBoost.CarryCapacity(9), Is.EqualTo(12));
            Assert.That(PlayerBoost.CarryCapacity(PlayerBoost.MaxLevel), Is.EqualTo(18));
            Assert.That(StaffBoost.CarryCapacity(8), Is.EqualTo(10));
            Assert.That(StaffBoost.CarryCapacity(9), Is.EqualTo(10));
            Assert.That(StaffBoost.CarryCapacity(StaffBoost.MaxTier), Is.EqualTo(16));
            for (int level = 0; level < 8; level++)
            {
                Assert.That(PlayerBoost.CarryCapacity(level + 1), Is.EqualTo(PlayerBoost.CarryCapacity(level) + 1));
                Assert.That(StaffBoost.CarryCapacity(level + 1), Is.EqualTo(StaffBoost.CarryCapacity(level) + 1));
            }
            for (int level = 8; level < PlayerBoost.MaxLevel; level++)
            {
                int expected = PlayerBoost.IsEmptyCarryLevel(level + 1) ? 0 : 1;
                Assert.That(PlayerBoost.CarryCapacity(level + 1) - PlayerBoost.CarryCapacity(level), Is.EqualTo(expected));
                Assert.That(StaffBoost.CarryCapacity(level + 1) - StaffBoost.CarryCapacity(level), Is.EqualTo(expected));
            }
        }

        [Test]
        public void RankPreviewMatchesShopUnlockRanks()
        {
            Assert.That(FacilityCatalog.Get(FacilityKind.PairTable).Rank, Is.EqualTo(2));
            Assert.That(FacilityCatalog.Get(FacilityKind.TrashBin).Rank, Is.EqualTo(2));
            Assert.That(FacilityCatalog.Get(FacilityKind.BurgerMachine).Rank, Is.EqualTo(4));
            Assert.That(FacilityCatalog.Get(FacilityKind.BurgerCounter).Rank, Is.EqualTo(4));
            Assert.That(RestroomExpansion.UnlockRank, Is.EqualTo(4));
            Assert.That(FacilityCatalog.Get(FacilityKind.BlueBoxTable).Rank, Is.EqualTo(5));
            Assert.That(FacilityCatalog.Get(FacilityKind.CarCounter).Rank, Is.EqualTo(6));
            Assert.That(FacilityCatalog.Get(FacilityKind.FourSeatTable).Rank, Is.EqualTo(7));
            Assert.That(FacilityCatalog.Get(FacilityKind.SquareTable).Rank, Is.EqualTo(7));
            Assert.That(FacilityCatalog.Get(FacilityKind.CourierTray).Rank, Is.EqualTo(8));
            Assert.That(ShopRanks.NextUnlock(1).ToLowerInvariant(), Does.Contain("dining"));
            Assert.That(ShopRanks.NextUnlock(3).ToLowerInvariant(), Does.Contain("grill"));
            Assert.That(ShopRanks.NextUnlock(3).ToLowerInvariant(), Does.Contain("counter"));
            Assert.That(ShopRanks.NextUnlock(3).ToLowerInvariant(), Does.Contain("restroom"));
        }

        [Test]
        public void TableTiersHaveThreePricesAndDistinctJobs()
        {
            var value = TableSetCatalog.Get(TableSetId.Bistro);
            var turnover = TableSetCatalog.Get(TableSetId.Diner);
            var premium = TableSetCatalog.Get(TableSetId.Patio);
            Assert.That(value.Cost, Is.LessThan(turnover.Cost));
            Assert.That(turnover.Cost, Is.LessThan(premium.Cost));
            Assert.That(value.MealPay, Is.EqualTo(turnover.MealPay));
            Assert.That(value.EatSeconds, Is.GreaterThan(turnover.EatSeconds));
            Assert.That(premium.MealPay, Is.GreaterThan(turnover.MealPay));
            Assert.That(premium.EatSeconds, Is.GreaterThan(turnover.EatSeconds));
            Assert.That(TableSetCatalog.IsConsistent((int)TableSetId.Patio, 80), Is.True, "Legacy 80 still counts as paid");
            Assert.That(TableSetCatalog.IsConsistent((int)TableSetId.Bistro, 50), Is.True);
            Assert.That(TableSetCatalog.IsConsistent((int)TableSetId.Patio, 120), Is.True);
            Assert.That(TableSetCatalog.IsConsistent((int)TableSetId.Bistro, 40), Is.False);
        }

        [UnityTest]
        public IEnumerator RankThreeShopPreviewsLockedSecondGrill()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            yield return new EnterPlayMode();
            var layout = Object.FindFirstObjectByType<FacilityLayout>();
            var shop = Object.FindFirstObjectByType<FacilityShopHud>();
            var goals = Object.FindFirstObjectByType<SessionGoalTracker>();
            goals.Restore(3, 0, 0);
            layout.Wallet.RestoreProgress(5000, 0);
            shop.Open();
            var card = GameObject.Find("Buy_BurgerMachine");
            Assert.That(card, Is.Not.Null);
            Assert.That(card.activeInHierarchy, Is.True);
            Assert.That(card.GetComponent<Button>().interactable, Is.False);
            Assert.That(card.transform.Find("Detail").GetComponent<Text>().text, Does.Contain("4级"));
            Assert.That(layout.BeginPurchase(FacilityKind.BurgerMachine), Is.Null);
            shop.Close();
            goals.Restore(4, 0, 0);
            shop.Open();
            Assert.That(GameObject.Find("Buy_BurgerMachine").GetComponent<Button>().interactable, Is.True);
            LogAssert.NoUnexpectedReceived();
            yield return new ExitPlayMode();
        }
    }
}
