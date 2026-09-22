using System.Collections;
using System.IO;
using BurgerShop.Building;
using BurgerShop.Customer;
using BurgerShop.Economy;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using BurgerShop.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace BurgerShop.Tests.EditMode
{
    // Natural money flow in the actual scene. The test supplies only walking and legal purchase decisions.
    // This is an automated route, not a human timing or a full 0–15 pacing acceptance.
    public sealed class EconomyPacingDriver : MonoBehaviour
    {
        public System.Action Tick;
        void Update()=>Tick?.Invoke();
    }

    public sealed class Spec083EconomyPacingTests : SaveIsolatedGameplayTest
    {
        [TearDown] public void RestoreClock()=>Time.timeScale=1;
        [UnityTest, Explicit("Manual economy sampling: normal-speed route, isolated save, about seven minutes"), Timeout(900000)] public IEnumerator NewGameToFirstEmployeeWithCollectedIncomeTrace()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");yield return new EnterPlayMode();for(int boot=0;boot<12;boot++)yield return null;
            Assert.That(FacilityLayout.Current,Is.Not.Null,"runtime layout booted");
            // Allocate captured trace state only after the Play Mode domain reload.
            yield return TraceEconomy();
            yield return new ExitPlayMode();
        }

        IEnumerator TraceEconomy()
        {
            Time.timeScale=1;Random.InitState(830922);
            var layout=FacilityLayout.Current;
            Assert.That(layout != null, Is.True, "live layout");
            var goals=layout.GetComponent<SessionGoalTracker>();
            Assert.That(goals != null, Is.True, "goals");
            var wallet=layout.Wallet;
            Assert.That(wallet != null, Is.True, "wallet");
            var cash=layout.GetComponent<CashFloor>();
            Assert.That(cash != null, Is.True, "cash floor");
            var hiring=layout.GetComponent<WorkerHiringZone>();
            Assert.That(hiring != null, Is.True, "hiring");
            var hall=layout.GetComponent<MainHallExpansion>();
            Assert.That(hall != null, Is.True, "hall");
            var inventory=layout.Player;
            Assert.That(inventory != null, Is.True, "player");
            var motor=inventory.GetComponent<PlayerMotor>();var controller=inventory.GetComponent<CharacterController>();
            var grill=MainKitchen<GrillUpgradeZone>();var pickup=MainKitchen<BurgerPickupZone>();var serving=MainKitchen<BurgerServingZone>();
            var trash=inventory.GetComponent<TrashInventory>();var dining=layout.GetComponentInChildren<DiningArea>();
            var bin=layout.GetComponentInChildren<TrashBin>(true);
            Assert.That(grill != null, Is.True, "main grill");
            Assert.That(pickup != null, Is.True, "main pickup");
            Assert.That(serving != null, Is.True, "main serving");
            Assert.That(dining != null, Is.True, "dining");
            Assert.That(bin != null, Is.True, "bin");
            Assert.That(wallet.Coins,Is.Zero);Assert.That(cash.GroundValue,Is.Zero);Assert.That(goals.Rank,Is.EqualTo(1));
            motor.enabled=false;
            foreach(var queue in layout.GetComponentsInChildren<CustomerQueue>())queue.SendMessage("OnApplicationFocus",true);
            const string file="/tmp/bs083-economy-natural.tsv";
            File.WriteAllText(file,"seconds\tevent\trank\twallet\tfloor\tincome\tspent\tstaff\tgrill\tx\tz\n");
            float start=Time.time, nextLog=0, postStart=-1;long income=0,spent=0,postIncome=0;
            wallet.SaleRecorded+=amount=>income+=amount;wallet.CoinsSpent+=amount=>spent+=amount;
            void Log(string message)=>File.AppendAllText(file,$"{Time.time-start:0.00}\t{message}\t{goals.Rank}\t{wallet.Coins}\t{cash.GroundValue}\t{income}\t{spent}\t{hiring.HiredCount}\t{grill.Level}\t{inventory.transform.position.x:0.00}\t{inventory.transform.position.z:0.00}\n");
            Vector3 oldTarget=Vector3.one*999;Vector3[] route=null;int waypoint=0;string oldAction="";
            Log("start;seed=830922;normal speed;no funded grants;menu pause=0");
            bool complete=false;
            var driver=inventory.gameObject.AddComponent<EconomyPacingDriver>();
            driver.Tick=()=>
            {
                if(Time.time-start>=600){complete=true;return;}
                if(goals.Rank<3&&goals.CanUpgrade){int before=goals.Rank;Assert.That(goals.TryUpgradeRank(before),Is.True);Log("rank-up");}
                if(goals.Rank==2&&grill.Level==1&&wallet.Coins>=grill.NextCost)
                {Assert.That(grill.TryUpgrade(1),Is.True);Log("buy production 30");}
                if(goals.Rank==3&&!hall.Built&&wallet.Coins>=hall.Remaining)
                {Assert.That(hall.TryContribute(),Is.True);Log("buy main hall");}
                if(hiring.HiredCount>0&&postStart<0&&cash.GroundValue==0)
                {postStart=Time.time;postIncome=income;Log("post-hire window start;floor=0");}
                if(postStart>=0&&Time.time-postStart>=180){Log($"post-hire window end;R={(income-postIncome)*60d/(Time.time-postStart):0.00}");complete=true;return;}
                Vector3 target;string action;
                var dirty=dining.FindDirtyTable();
                if(hall.Built&&hiring.HiredCount==0&&wallet.Coins>=hiring.HireCost)
                {target=hiring.HiringPosition;action="hire";}
                else if(cash.GroundValue>0){target=cash.NearestPilePosition;action="collect";}
                else if(trash.Count>0){target=bin.DropPosition+new Vector3(.8f,0,0);action="dump";}
                else if(dirty!=null){target=dirty.Center+new Vector3(1.1f,0,-.35f);action="clean";}
                else if(serving.ServiceableStock>0&&serving.HasReadyCustomer){target=serving.ServingPosition;action="serve";}
                else if(inventory.LooseCount>0){target=serving.DropZone.DropPosition;action="stock";}
                else {target=pickup.PickupPosition;action="pickup";}
                target.y=0;
                if(action!=oldAction){Log(action);oldAction=action;}
                if(Vector3.Distance(target,oldTarget)>.1f)
                {
                    oldTarget=target;
                    // The player walks around the dining chairs via the clear east aisle.
                    // This fixture does not teleport through furniture or disable collisions.
                    route=action=="hire" ? new[]{new Vector3(inventory.transform.position.x,0,-9),new Vector3(10,0,-9),new Vector3(10,0,0),target}
                        :inventory.transform.position.x>15 ? new[]{new Vector3(10,0,0),new Vector3(10,0,-9),new Vector3(-8,0,-9),target}
                        :action=="dump" ? new[]{new Vector3(-8,0,inventory.transform.position.z),new Vector3(-8,0,target.z),target}
                        :action=="clean" ? new[]{new Vector3(-8,0,inventory.transform.position.z),new Vector3(-8,0,target.z),target}
                        :layout.Route(inventory.transform.position,target);
                    waypoint=0;
                }
                Vector3 next=route!=null&&waypoint<route.Length?route[waypoint]:target;
                Vector3 delta=next-inventory.transform.position;delta.y=0;
                if(delta.magnitude<.3f&&route!=null&&waypoint<route.Length){waypoint++;return;}
                if(delta.magnitude>.05f){controller.SimpleMove(delta.normalized*Mathf.Min(motor.MoveSpeed,delta.magnitude/Mathf.Max(.001f,Time.deltaTime)));inventory.transform.rotation=Quaternion.LookRotation(delta);}
                if(Time.time-start>=nextLog){Log("sample");nextLog+=10;}
            };
            while(!complete)yield return null;
            Object.Destroy(driver);
            Log("end");
            Assert.That(hiring.HiredCount,Is.GreaterThan(0),"route must reach the first employee before measuring the post-hire window");
            Assert.That(wallet.Coins,Is.EqualTo(income-spent),"normal collected inflow must reconcile separately from purchases");
            TestContext.WriteLine(File.ReadAllText(file));
            motor.enabled=true;Time.timeScale=1;
        }
    }
}
