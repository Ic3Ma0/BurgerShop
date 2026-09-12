using System.Collections;
using System.IO;
using BurgerShop.Economy;
using BurgerShop.Persistence;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace BurgerShop.Tests.EditMode
{
    public sealed class Spec023GameplayTests : SaveIsolatedGameplayTest
    {
        [UnityTest]
        public IEnumerator ScenePartialInvestmentPersistsAndBuildsOnceAfterResume()
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
            wallet.RestoreProgress(120, 0);
            player.transform.position = expansion.GrillPad.PadPosition + Vector3.up;
            for (int frame = 0; frame < 120; frame++) yield return null;
            Assert.That(wallet.Coins, Is.Zero);
            Assert.That(expansion.GrillPad.Remaining, Is.EqualTo(80));
            var label = expansion.transform.Find("GrillUnlockPadLabel");
            Assert.That(label.gameObject.activeInHierarchy, Is.True);
            Assert.That(label.GetComponent<TextMesh>().text, Is.EqualTo("GRILL\nRemaining 80"));
            string evidence = Path.GetFullPath("Logs/spec023-025");
            Directory.CreateDirectory(evidence);
            GameplayEvidence.Capture("spec023-partial-editor.png");
            yield return null; yield return null;
            persistence.SendMessage("OnApplicationPause", true);
            Assert.That(new LocalSaveStore(SaveDirectory).Load(out var saved), Is.EqualTo(SaveLoadResult.Loaded));
            Assert.That(saved.grillInvestment, Is.EqualTo(120));
            Assert.That(saved.coins, Is.Zero);
            expansion.GrillPad.SendMessage("OnApplicationPause", true);
            wallet.CollectCoins(100);
            expansion.GrillPad.Advance(10f);
            Assert.That(wallet.Coins, Is.EqualTo(100));
            expansion.GrillPad.SendMessage("OnApplicationPause", false);
            for (int frame = 0; frame < 120; frame++) yield return null;
            Assert.That(wallet.Coins, Is.EqualTo(20));
            Assert.That(expansion.HasExtraGrill, Is.True);
            Assert.That(new LocalSaveStore(SaveDirectory).Load(out saved), Is.EqualTo(SaveLoadResult.Loaded));
            Assert.That(saved.boughtExtraGrill, Is.True);
            Assert.That(saved.grillInvestment, Is.EqualTo(200));
            Assert.That(saved.coins, Is.EqualTo(20));
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
