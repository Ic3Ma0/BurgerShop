using System;
using System.IO;
using BurgerShop.Persistence;
using NUnit.Framework;
using UnityEditor;

namespace BurgerShop.Tests.EditMode
{
    public abstract class SaveIsolatedGameplayTest
    {
        protected static T MainKitchen<T>() where T : UnityEngine.Component
        {
            foreach(var item in UnityEngine.Object.FindObjectsByType<T>(UnityEngine.FindObjectsSortMode.None))
            {
                if(item is BurgerShop.Customer.CustomerQueue q && q.Product == BurgerShop.Restaurant.KitchenProduct.Burger && q.transform.parent.name != "BagLine") return item;
                if(item is BurgerShop.Restaurant.BurgerServingZone zone && zone.DropZone != null && zone.DropZone.Product == BurgerShop.Restaurant.KitchenProduct.Burger) return item;
                if(item is BurgerShop.Restaurant.ProductionStation station && station.Product == BurgerShop.Restaurant.KitchenProduct.Burger && UnityEngine.Vector3.Distance(station.transform.position, BurgerShop.Restaurant.ShopLayout.Grill) < .1f) return item;
                if(item is BurgerShop.Restaurant.GrillUpgradeZone upgrade && UnityEngine.Vector3.Distance(upgrade.UpgradePosition, BurgerShop.Restaurant.ShopLayout.UpgradeSpot) < .1f) return item;
                if(item is BurgerShop.Restaurant.BurgerPickupZone pickup && BurgerShop.Restaurant.ShopLayout.Horizontal(pickup.PickupPosition, BurgerShop.Restaurant.ShopLayout.GrillPickup) < .1f) return item;
            }
            return null;
        }
        protected string SaveDirectory => SessionState.GetString(RestaurantPersistence.EditorDirectoryKey, "");
        const string PreviousKey = "BurgerShop.Tests.PreviousSaveDirectory";
        const string TestKey = "BurgerShop.Tests.SaveOwner";

        [SetUp]
        public void IsolateSave()
        {
            // The EditMode runner repeats SetUp after an EnterPlayMode domain reload.
            string owner = TestContext.CurrentContext.Test.FullName;
            if (SessionState.GetString(TestKey, "") == owner) return;
            SessionState.SetString(PreviousKey, SaveDirectory);
            string directory = Path.Combine(Path.GetTempPath(), "BurgerShopTests-" + Guid.NewGuid().ToString("N"));
            SessionState.SetString(RestaurantPersistence.EditorDirectoryKey, directory);
            SessionState.SetString(TestKey, owner);
        }

        [TearDown]
        public void RestoreSaveDirectory()
        {
            string directory = SaveDirectory;
            SessionState.SetString(RestaurantPersistence.EditorDirectoryKey, SessionState.GetString(PreviousKey, ""));
            SessionState.EraseString(PreviousKey);
            SessionState.EraseString(TestKey);
            if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }
}
