using System;
using System.IO;
using BurgerShop.Persistence;
using NUnit.Framework;
using UnityEditor;

namespace BurgerShop.Tests.EditMode
{
    public abstract class SaveIsolatedGameplayTest
    {
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
