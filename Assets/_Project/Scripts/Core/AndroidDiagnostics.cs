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

        public void Configure(RestaurantWallet earnings, BurgerInventory carrier, WorkerHiringZone staff, GrillUpgradeZone grill)
        {
            wallet = earnings; player = carrier; hiring = staff; upgrade = grill;
            started = Time.realtimeSinceStartup;
            serving = FindFirstObjectByType<BurgerServingZone>();
            queue = FindFirstObjectByType<CustomerQueue>();
        }
        void Update()
        {
            if (Application.platform != RuntimePlatform.Android || wallet == null || Time.realtimeSinceStartup - started < 2f) return;
            float dt = Time.unscaledDeltaTime;
            if (dt <= 0f || dt > 1f) return;
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
                fps = frameTimes.Count / elapsed,
                p95ms = frameTimes[Mathf.Min(frameTimes.Count - 1, Mathf.CeilToInt(frameTimes.Count * 0.95f) - 1)],
                unityMemoryMB = Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f),
                litShader = Resources.Load<Material>("RestaurantLit").shader.name,
                cameraPitch = camera != null ? camera.transform.eulerAngles.x : -1f
            };
            Debug.Log("BURGER_SHOP_DEVICE " + JsonUtility.ToJson(sample));
            elapsed = 0f; frameTimes.Clear();
        }
        void OnApplicationPause(bool paused)
        {
            if (!paused) started = Time.realtimeSinceStartup;
            elapsed = 0f; frameTimes.Clear();
        }
        [Serializable] sealed class WorkerSnapshot
        {
            public string job, state, line;
            public int reserved, raw, boxes;
            public float x, z;
        }
        [Serializable] sealed class Snapshot
        {
            public string litShader;
            public float seconds, x, y, z, fps, p95ms, unityMemoryMB, cameraPitch;
            public long coins;
            public int sales, carry, level, deliveries, clears, loose, boxes, dining, diningStock, diningRemaining, raw, processing, output, pack, processed, drive, driveRemaining;
            public WorkerSnapshot[] workers;
            public bool staff;
        }
    }
}
#endif
