#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections.Generic;
using BurgerShop.Economy;
using BurgerShop.Customer;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using UnityEngine;
using UnityEngine.Profiling;

namespace BurgerShop.Core
{
    // Read-only development-build telemetry for connected-device acceptance.
    public sealed class AndroidDiagnostics : MonoBehaviour
    {
        RestaurantWallet wallet;
        BurgerInventory player;
        WorkerHiringZone hiring;
        GrillUpgradeZone upgrade;
        BurgerServingZone serving;
        CustomerQueue queue;
        readonly List<float> frameTimes = new List<float>(256);
        float elapsed;
        float started;
        float traceAt;
        float slowStreak, maxSlowStreak;
        Vector2 previousInput;

        public void Configure(RestaurantWallet earnings, BurgerInventory carrier, WorkerHiringZone staff, GrillUpgradeZone grill)
        {
            wallet = earnings; player = carrier; hiring = staff; upgrade = grill;
            started = Time.realtimeSinceStartup;
            if(Application.platform==RuntimePlatform.Android && UI.FeedbackDirector.Current!=null)
                UI.FeedbackDirector.Current.DecorationsEnabled=!System.IO.File.Exists(System.IO.Path.Combine(Application.persistentDataPath,"qa-no-feedback.flag"));
            serving = FindFirstObjectByType<BurgerServingZone>();
            queue = FindFirstObjectByType<CustomerQueue>();
        }
        void Update()
        {
            if (Application.platform != RuntimePlatform.Android || wallet == null || !Application.isFocused || Time.realtimeSinceStartup - started < 2f) return;
            var motor=player.GetComponent<PlayerMotor>();
            if(motor!=null && (motor.LastInput!=previousInput || (motor.LastInput.sqrMagnitude>0 && Time.time>=traceAt)))
            {
                previousInput=motor.LastInput;traceAt=Time.time+.05f;
                Debug.Log("BURGER_SHOP_INPUT " + JsonUtility.ToJson(new InputTrace{t=Time.time,x=motor.LastInput.x,y=motor.LastInput.y,speed=motor.CommandedVelocity.magnitude,yaw=motor.transform.eulerAngles.y,cameraYaw=Camera.main.transform.eulerAngles.y,cameraPitch=Camera.main.transform.eulerAngles.x}));
            }
            float dt = Time.unscaledDeltaTime;
            if(dt<=0f)return;
            slowStreak=dt>.025f?slowStreak+dt:0;maxSlowStreak=Mathf.Max(maxSlowStreak,slowStreak);
            frameTimes.Add(dt * 1000f);
            elapsed += dt;
            if (elapsed < 2f) return;
            frameTimes.Sort();
            Camera camera = Camera.main;
            var box = hiring.Boxing;
            var lane = hiring.DriveThru;
            var workers = new WorkerSnapshot[hiring.Workers.Count];
            for (int i = 0; i < workers.Length; i++)
            {
                var w = hiring.Workers[i];
                workers[i] = new WorkerSnapshot { job = w.Job.ToString(), state = w.State.ToString(), line = w.SupplyTarget.ToString(),
                    reserved = w.ReservedRaw, raw = w.Inventory.LooseCount, boxes = w.Inventory.BoxedCount,
                    x = w.transform.position.x, z = w.transform.position.z };
            }
            var sample = new Snapshot
            {
                seconds = Time.realtimeSinceStartup, x = player.transform.position.x, z = player.transform.position.z, y = player.transform.position.y,
                coins = wallet.Coins, sales = wallet.CompletedSales, carry = player.Count,
                level = upgrade.Level, staff = hiring.IsHired,
                deliveries = hiring.TotalDeliveries, clears = hiring.TotalClears, workers = workers,
                loose = player.LooseCount, boxes = player.BoxedCount, dining = serving != null ? serving.CompletedOrders : 0,
                diningStock = serving != null ? serving.TotalStock : 0,
                diningRemaining = queue != null && queue.ReadyCustomer != null ? queue.ReadyCustomer.RemainingQuantity : 0,
                raw = box != null ? box.InputCount : 0, processing = box != null ? box.ProcessingCount : 0,
                output = box != null ? box.OutputCount : 0, pack = box != null ? box.PackageCount : 0,
                processed = box != null ? box.TotalProcessed : 0, drive = lane != null ? lane.CompletedOrders : 0,
                driveRemaining = lane != null && lane.WaitingOrder != null ? lane.WaitingOrder.Remaining : 0,
                maxSlowFrameStreak=maxSlowStreak,
                fps = frameTimes.Count / elapsed,
                p95ms = frameTimes[Mathf.Min(frameTimes.Count - 1, Mathf.CeilToInt(frameTimes.Count * 0.95f) - 1)],
                unityMemoryMB = Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f),
                litShader = Resources.Load<Material>("RestaurantLit").shader.name,
                cameraPitch = camera != null ? camera.transform.eulerAngles.x : -1f,
                prompts=UI.FeedbackDirector.Current!=null?UI.FeedbackDirector.Current.ActivePrompts:0,
                voices=UI.FeedbackDirector.Current!=null?UI.FeedbackDirector.Current.ActiveVoices:0,
                peakPrompts=UI.FeedbackDirector.Current!=null?UI.FeedbackDirector.Current.MaxPromptsSeen:0,
                peakVoices=UI.FeedbackDirector.Current!=null?UI.FeedbackDirector.Current.MaxVoicesSeen:0
            };
            Debug.Log("BURGER_SHOP_DEVICE " + JsonUtility.ToJson(sample));
            elapsed = 0f; frameTimes.Clear();
        }
        void OnApplicationPause(bool paused)
        {
            if (!paused) started = Time.realtimeSinceStartup;
            elapsed = 0f; frameTimes.Clear();slowStreak=0;
        }
        void OnApplicationFocus(bool focused)
        {
            // System recorder and notification shade focus loss is not a game frame stall.
            started=Time.realtimeSinceStartup;elapsed=0f;frameTimes.Clear();slowStreak=0f;
        }
        [Serializable] sealed class InputTrace {public float t,x,y,speed,yaw,cameraYaw,cameraPitch;}
        [Serializable] sealed class WorkerSnapshot
        {
            public string job, state, line;
            public int reserved, raw, boxes;
            public float x, z;
        }
        [Serializable] sealed class Snapshot
        {
            public string litShader;
            public float maxSlowFrameStreak;
            public float seconds, x, y, z, fps, p95ms, unityMemoryMB, cameraPitch;
            public long coins;
            public int prompts,voices,peakPrompts,peakVoices;
            public int sales, carry, level, deliveries, clears, loose, boxes, dining, diningStock, diningRemaining, raw, processing, output, pack, processed, drive, driveRemaining;
            public WorkerSnapshot[] workers;
            public bool staff;
        }
    }
}
#endif
