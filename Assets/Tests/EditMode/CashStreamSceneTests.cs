using System.Collections;
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
    public sealed class CashStreamSceneTests:SaveIsolatedGameplayTest
    {
        [UnityTest] public IEnumerator RenderBeforeDuringAndAfterTheBanknoteStream()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");yield return new EnterPlayMode();
            var cash=Object.FindFirstObjectByType<CashFloor>();cash.enabled=false;
            var player=Object.FindFirstObjectByType<PlayerMotor>();player.enabled=false;
            player.transform.position=new Vector3(0,1.05f,-7);
            var camera=Camera.main;camera.GetComponent<CameraFollow>().enabled=false;
            camera.transform.position=new Vector3(-5,8,-13);camera.transform.LookAt(new Vector3(.3f,1,-7));camera.orthographicSize=4;
            // Keep the capture viewport active while receipt particles are projected, not just during ReadPixels.
            var target=new RenderTexture(720,1280,24);camera.targetTexture=target;
            foreach(var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                if(canvas.isRootCanvas){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;}
            cash.DropAt(new Vector3(.65f,.04f,-7),1000);
            yield return null;GameplayEvidence.Capture("../spec057/before.png");
            cash.Advance(.6f);yield return null;GameplayEvidence.Capture("../spec057/during.png");
            Assert.That(cash.GroundValue,Is.GreaterThan(0).And.LessThan(1000));
            cash.Advance(2);yield return new WaitForSeconds(.5f);GameplayEvidence.Capture("../spec057/after.png");
            Assert.That(cash.GroundValue,Is.Zero);
            camera.targetTexture=null;Object.Destroy(target);
            yield return new ExitPlayMode();
        }

        [UnityTest] public IEnumerator SharedCountersDoNotAccelerateCashAndPausePreservesIt()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            yield return new EnterPlayMode();
            var cash=Object.FindFirstObjectByType<CashFloor>();
            var player=Object.FindFirstObjectByType<PlayerMotor>();
            var persistence=Object.FindFirstObjectByType<RestaurantPersistence>();
            var wallet=persistence.GetComponent<RestaurantWallet>();
            var feedback=BurgerShop.UI.FeedbackDirector.Current;
            // Change this scene instance only; do not write the user's Sound preference during tests.
            var soundProperty=typeof(BurgerShop.UI.FeedbackDirector).GetProperty("SoundEnabled");
            soundProperty.SetValue(feedback,true);
            feedback.SendMessage("OnApplicationFocus",true);
            feedback.SendMessage("OnApplicationPause",false);
            player.enabled=false;player.transform.position=new Vector3(0,1.05f,-7);
            wallet.RestoreProgress(0,0);
            // Dynamic counters bind the same floor. Before 057 each of these ticked it again every frame.
            for(int i=0;i<7;i++)
            {
                var counter=new GameObject("Additional cash caller "+i);counter.transform.SetParent(cash.transform);
                counter.AddComponent<BurgerServingZone>().BindCash(cash);
            }
            var receipts=new System.Collections.Generic.List<int>();wallet.SaleRecorded+=receipts.Add;
            cash.DropAt(new Vector3(.6f,.04f,-7),1000);
            float started=Time.time;
            while(Time.time-started<.5f)yield return null;
            Assert.That(wallet.Coins,Is.GreaterThan(0).And.LessThan(500),"Extra counters must not speed up the stream");
            Assert.That(receipts.Count,Is.GreaterThan(1));
            var sources=feedback.GetComponents<AudioSource>();
            Assert.That(System.Array.Exists(sources,s=>s.clip!=null&&s.clip.name=="BanknoteCollect"&&s.pitch>1f),Is.True,"Actual cash arrivals schedule rising-pitch banknote audio");
            long pausedCash=wallet.Coins;int remaining=cash.GroundValue;
            Time.timeScale=0;
            yield return new WaitForSecondsRealtime(.15f);
            Assert.That(wallet.Coins,Is.EqualTo(pausedCash));Assert.That(cash.GroundValue,Is.EqualTo(remaining));
            Time.timeScale=1;
            float resumed=Time.time;
            while(cash.GroundValue>0&&Time.time-resumed<3)yield return null;
            Assert.That(wallet.Coins,Is.EqualTo(1000));Assert.That(cash.GroundValue,Is.Zero);
            persistence.Flush();new LocalSaveStore(SaveDirectory).Load(out var saved);
            Assert.That(saved.coins,Is.EqualTo(1000),"The exact collected total is saved");
            soundProperty.SetValue(feedback,false);
            Assert.That(feedback.RequestSound(BurgerShop.UI.FeedbackSound.Cash),Is.False,"Muted mode never schedules cash audio");
            yield return new ExitPlayMode();
        }
    }
}
