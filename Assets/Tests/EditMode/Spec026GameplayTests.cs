using System.Collections;
using System.IO;
using BurgerShop.Persistence;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using BurgerShop.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace BurgerShop.Tests.EditMode
{
    public sealed class Spec026GameplayTests : SaveIsolatedGameplayTest
    {
        [UnityTest]
        public IEnumerator SampleSceneThemeAndUpgradeStatesAreRenderedFromRealData()
        {
            new LocalSaveStore(SaveDirectory).Save(new RestaurantSaveData{version=7,coins=12345,grillLevel=3,boughtBoxingStation=true,boughtDriveThru=true,boxingInvestment=150,driveThruInvestment=250});
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            yield return new EnterPlayMode();
            Time.captureDeltaTime=1f/60;
            var player=Object.FindFirstObjectByType<PlayerMotor>();
            for(int i=0;i<180;i++)yield return null;
            Assert.That(Object.FindFirstObjectByType<SessionGoalTracker>().IsCelebrating,Is.False);
            GameplayEvidence.Capture("../spec026-028/template-hud.png");
            Move(player,new Vector3(-8,1.05f,-7.2f));
            for(int i=0;i<60;i++)yield return null;
            GameplayEvidence.Capture("../spec026-028/template-boxing.png");
            Move(player,ShopLayout.BoostPoint+Vector3.up);
            for(int i=0;i<30;i++)yield return null;
            var hud=Object.FindFirstObjectByType<PlayerUpgradeHud>();
            Assert.That(hud.IsVisible,Is.True);
            Assert.That(hud.Popup.FirstLabel.text,Does.Contain("100%").And.Contain("115%"));
            GameplayEvidence.Capture("../spec026-028/template-upgrade.png");
            yield return new ExitPlayMode();
        }
        static void Move(PlayerMotor player,Vector3 to)
        {
            var controller=player.GetComponent<CharacterController>();controller.enabled=false;player.transform.position=to;controller.enabled=true;
            Object.FindFirstObjectByType<CameraFollow>().Snap();
        }
    }
}
