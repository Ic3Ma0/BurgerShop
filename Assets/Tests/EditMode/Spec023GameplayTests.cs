using System.Collections;
using System.IO;
using BurgerShop.Economy;
using BurgerShop.Persistence;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using BurgerShop.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace BurgerShop.Tests.EditMode
{
    public sealed class Spec023GameplayTests : SaveIsolatedGameplayTest
    {
        [UnityTest]
        public IEnumerator SceneLegacyInvestmentIsCreditedOnceByTheShop()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            yield return new EnterPlayMode();
            Time.captureDeltaTime = 1f / 60f;
            var expansion = Object.FindFirstObjectByType<ShopExpansion>();
            var motor = Object.FindFirstObjectByType<PlayerMotor>();
            var player = motor.GetComponent<BurgerInventory>();
            var wallet = Object.FindFirstObjectByType<RestaurantWallet>();
            var persistence = Object.FindFirstObjectByType<RestaurantPersistence>();
            motor.enabled = false;
            Object.FindFirstObjectByType<SessionGoalTracker>().Restore(4, 0, 0);
            expansion.ApplyRank(4);
            // This investment was made under the pre-shop flow. 043 replaces ground purchases,
            // but must preserve every coin already invested by old saves.
            expansion.GrillPad.RestoreInvestment(120);
            wallet.RestoreProgress(0,0);
            persistence.Flush();
            Assert.That(new LocalSaveStore(SaveDirectory).Load(out var saved), Is.EqualTo(SaveLoadResult.Loaded));
            Assert.That(saved.grillInvestment, Is.EqualTo(120));
            var layout=Object.FindFirstObjectByType<BurgerShop.Building.FacilityLayout>();
            Assert.That(layout.Price(BurgerShop.Building.FacilityKind.BurgerMachine),Is.EqualTo(130));
            layout.BeginPurchase(BurgerShop.Building.FacilityKind.BurgerMachine);
            Assert.That(layout.Confirm(new Vector3(10,0,-5),0),Is.False);
            Assert.That(wallet.Coins,Is.Zero);
            wallet.CollectCoins(150);
            Assert.That(layout.Confirm(new Vector3(10,0,-5),0),Is.True,layout.LastError);
            Assert.That(wallet.Coins, Is.EqualTo(20));
            Assert.That(expansion.HasExtraGrill, Is.True);
            Assert.That(new LocalSaveStore(SaveDirectory).Load(out saved), Is.EqualTo(SaveLoadResult.Loaded));
            Assert.That(saved.boughtExtraGrill, Is.True);
            Assert.That(saved.grillInvestment, Is.EqualTo(200));
            Assert.That(saved.coins, Is.EqualTo(20));
            Assert.That(layout.Price(BurgerShop.Building.FacilityKind.BurgerMachine),Is.GreaterThan(200));
            GameplayEvidence.Capture("spec023-built-editor.png");
            yield return null; yield return null;
            LogAssert.NoUnexpectedReceived();
            Time.captureDeltaTime = 0;
            yield return new ExitPlayMode();
        }
        [UnityTearDown] public IEnumerator LeavePlayMode()
        {
            if (Application.isPlaying) { Time.captureDeltaTime = 0; yield return new ExitPlayMode(); }
        }
    }
}
