using System.Collections.Generic;
using BurgerShop.Player;
using UnityEngine;

namespace BurgerShop.Restaurant
{
    public sealed class BoxingStation : MonoBehaviour
    {
        public const float BoxInterval = 0.35f;
        public int WorkLevel { get; set; } = 1;
        public static float SecondsForLevel(int level) => BoxInterval - .05f*(Mathf.Clamp(level,1,3)-1);
        public float ProcessingSeconds => SecondsForLevel(WorkLevel);
        public const float WorkRadius = 0.9f;
        public const int InputCapacity = 8;
        public const int OutputCapacity = 8;
        static readonly Color CircleColor = new Color(0.24f, 0.77f, 0.46f);
        sealed class Operator
        {
            public BurgerInventory Inventory;
            public float Cooldown;
        }
        sealed class Transfer
        {
            public Transform Item;
            public BurgerInventory To;
            public Vector3 Origin;
            public float Age;
        }
        readonly List<Operator> operators = new List<Operator>();
        readonly List<Transform> raw = new List<Transform>();
        readonly List<Transform> boxes = new List<Transform>();
        readonly List<Transfer> incoming = new List<Transfer>();
        readonly List<Transfer> outgoing = new List<Transfer>();
        BurgerInventory player;
        public Transform WorkRoot { get; private set; }
        public Transform CounterRoot { get; private set; }
        Transform circle;
        Transform inputAnchor;
        Transform outputAnchor;
        Transform processingRaw;
        Transform processingBox;
        Transform lid;
        Vector3 processingStart;
        TextMesh rawLabel;
        TextMesh boxLabel;
        TextMesh processLabel;
        float processingAge;
        bool processing;
        bool paused;

        public CounterStock Package { get; private set; }
        public CounterDropZone Drop { get; private set; }
        public Vector3 CirclePosition => circle != null ? circle.position : ShopLayout.BoxingCircle;
        public Vector3 DropPosition => Drop != null ? Drop.DropPosition : ShopLayout.PackageDrop;
        public int PackageCount => Package != null ? Package.Count : 0;
        // Arriving raw is owned by the input and reserves a slot, but cannot be processed before arrival.
        public int InputCount => raw.Count + incoming.Count;
        public int OutputCount => boxes.Count;
        public int ProcessingCount => processing ? 1 : 0;
        public int TableCount => InputCount + OutputCount + ProcessingCount;
        public bool InputFull => InputCount >= InputCapacity;
        public bool OutputFull => OutputCount + ProcessingCount >= OutputCapacity;
        public float ProcessingProgress => processing ? Mathf.Clamp01(processingAge / ProcessingSeconds) : 0f;
        public int TotalRawReceived { get; private set; }
        public int TotalProcessed { get; private set; }
        public int TotalBoxPickups { get; private set; }
        public int PendingTransfers => incoming.Count + outgoing.Count;

        public bool TryIssueCombo(out Transform box)
        {
            box = null;
            return Package != null && Package.isActiveAndEnabled && Package.TryTakeBoxed(out box);
        }
        public void BindPlayer(BurgerInventory carrier) { player = carrier; RegisterOperator(carrier); }
        public void RegisterOperator(BurgerInventory carrier)
        {
            if (carrier == null) return;
            foreach (var op in operators) if (op.Inventory == carrier) return;
            operators.Add(new Operator { Inventory = carrier });
        }
        public bool IsActorInRange(Transform actor) => actor != null && ShopLayout.Horizontal(actor.position, CirclePosition) <= WorkRadius;
        bool CanOperate(BurgerInventory carrier)
        {
            if (carrier == null || !carrier.isActiveAndEnabled || !IsActorInRange(carrier.transform)) return false;
            var worker = carrier.GetComponent<RestaurantWorker>();
            return worker == null || (worker.isActiveAndEnabled && worker.IsBoxOperator);
        }
        bool HasOperator()
        {
            if (WorkRoot != null && !WorkRoot.gameObject.activeInHierarchy) return false;
            foreach (var op in operators) if (CanOperate(op.Inventory)) return true;
            return false;
        }
        void Update() => Advance(Time.deltaTime);
        void OnApplicationPause(bool value) => paused = value;
        public void Advance(float deltaTime)
        {
            if (deltaTime <= 0f || paused || !isActiveAndEnabled) return;
            // One station clock, independent of how many carriers are registered.
            while (deltaTime > 0.00001f)
            {
                float step = Mathf.Min(deltaTime, 0.05f);
                TickTransfers(step);
                TickProcessing(step, HasOperator());
                for (int i = operators.Count - 1; i >= 0; i--)
                {
                    Operator op = operators[i];
                    if (op.Inventory == null) { operators.RemoveAt(i); continue; }
                    op.Cooldown = Mathf.Max(0, op.Cooldown - step);
                    TryTransfer(op);
                }
                Drop?.Advance(step);
                Drop?.TryDepositFrom(player);
                deltaTime -= step;
            }
            RefreshLabels();
        }

        // Compatibility entry point: starts a physical transfer, never converts food on a carrier.
        public bool TryBoxFrom(BurgerInventory carrier)
        {
            if (paused || !isActiveAndEnabled || !CanOperate(carrier)) return false;
            RegisterOperator(carrier);
            foreach (var op in operators) if (op.Inventory == carrier) return TryTransfer(op);
            return false;
        }
        bool TryTransfer(Operator op)
        {
            BurgerInventory carrier = op.Inventory;
            if (op.Cooldown > 0.00001f || !CanOperate(carrier)) return false;
            if (carrier.LooseCount > 0 && !InputFull)
            {
                if (!carrier.TryTakeBurger(out Transform item)) return false;
                Vector3 origin = item.position;
                item.SetParent(WorkRoot, true);
                incoming.Add(new Transfer { Item = item, Origin = WorkRoot.InverseTransformPoint(origin) });
                TotalRawReceived++;
                carrier.GetComponent<RestaurantWorker>()?.RawDeposited(1);
                op.Cooldown = BoxInterval;
                RefreshLabels();
                return true;
            }
            var worker = carrier.GetComponent<RestaurantWorker>();
            if (boxes.Count == 0 || carrier.IsFull || (worker != null && !worker.WantsBoxPickup)) return false;
            if (!carrier.TryReserveIncomingBox()) return false;
            Transform box = boxes[boxes.Count - 1]; boxes.RemoveAt(boxes.Count - 1);
            Vector3 from = box.position;
            box.SetParent(carrier.transform, true);
            outgoing.Add(new Transfer { Item = box, To = carrier, Origin = WorkRoot.InverseTransformPoint(from) });
            TotalBoxPickups++;
            op.Cooldown = BoxInterval;
            RefreshLabels();
            return true;
        }
        void TickTransfers(float seconds)
        {
            for (int i = incoming.Count - 1; i >= 0; i--)
            {
                Transfer flight = incoming[i]; flight.Age += seconds;
                float t = Mathf.Clamp01(flight.Age / BoxInterval);
                flight.Item.position = Vector3.Lerp(WorkRoot.TransformPoint(flight.Origin), inputAnchor.position + Vector3.up * (raw.Count * 0.14f), t)
                    + Vector3.up * (0.4f * Mathf.Sin(t * Mathf.PI));
                if (t < 1f) continue;
                flight.Item.SetParent(inputAnchor, true);
                flight.Item.localPosition = Vector3.up * (raw.Count * 0.14f);
                flight.Item.localRotation = Quaternion.identity;
                flight.Item.localScale = Vector3.one * 0.7f;
                raw.Add(flight.Item); incoming.RemoveAt(i);
            }
            for (int i = outgoing.Count - 1; i >= 0; i--)
            {
                Transfer flight = outgoing[i]; flight.Age += seconds;
                if (flight.To == null) { outgoing.RemoveAt(i); continue; }
                float t = Mathf.Clamp01(flight.Age / BoxInterval);
                Vector3 target = flight.To.transform.TransformPoint(new Vector3(0, 0.3f, 0.8f));
                flight.Item.position = Vector3.Lerp(WorkRoot.TransformPoint(flight.Origin), target, t) + Vector3.up * (0.4f * Mathf.Sin(t * Mathf.PI));
                if (t < 1f) continue;
                flight.To.ReceiveReservedBox(flight.Item);
                outgoing.RemoveAt(i);
            }
        }
        void TickProcessing(float seconds, bool staffed)
        {
            if (!staffed) return;
            if (processing)
            {
                processingAge += seconds;
                float t = ProcessingProgress;
                processingRaw.position = Vector3.Lerp(WorkRoot.TransformPoint(processingStart), processingBox.position + Vector3.up * 0.12f, t);
                processingRaw.localScale = Vector3.one * Mathf.Lerp(0.7f, 0.42f, t);
                if (lid != null)
                {
                    lid.localRotation = Quaternion.Euler(Mathf.Lerp(-100f, 0f, t), 0, 0);
                    lid.localPosition = Vector3.Lerp(new Vector3(0, 0.6f, 0.25f), new Vector3(0, 0.28f, 0), t);
                }
                if (t < 1f) return;
                BurgerVisual.Release(processingRaw.gameObject); processingRaw = null;
                processingBox.SetParent(outputAnchor, false);
                processingBox.localPosition = Vector3.up * (boxes.Count * 0.22f);
                processingBox.localScale = Vector3.one * 0.75f;
                boxes.Add(processingBox); processingBox = null;
                GetComponentInParent<UI.SessionGoalTracker>()?.RecordMilestone(ShopGoalKind.BoxBurger);
                processing = false; TotalProcessed++;
            }
            if (raw.Count == 0 || OutputFull) return;
            processingRaw = raw[raw.Count - 1]; raw.RemoveAt(raw.Count - 1);
            processingStart = WorkRoot.InverseTransformPoint(processingRaw.position);
            processingRaw.SetParent(WorkRoot, true);
            processingBox = BoxVisualFactory.Create(WorkRoot, 0);
            processingBox.name = "BoxInProcess";
            processingBox.position = WorkRoot.TransformPoint(Vector3.up * 1.15f);
            lid = processingBox.Find("Lid");
            lid.localRotation = Quaternion.Euler(-100, 0, 0);
            lid.localPosition = new Vector3(0, 0.6f, 0.25f);
            processing = true; processingAge = 0;
        }
        void RefreshLabels()
        {
            if (rawLabel != null) rawLabel.text = $"RAW\n{InputCount}/{InputCapacity}";
            if (boxLabel != null) boxLabel.text = $"BOX\n{OutputCount}/{OutputCapacity}";
            if (processLabel != null) processLabel.text = processing ? "PACKING 1" : "PACKING 0";
        }
        void LateUpdate()
        {
            if (Camera.main == null) return;
            var rotation = Camera.main.transform.rotation;
            if (rawLabel != null) rawLabel.transform.rotation = rotation;
            if (boxLabel != null) boxLabel.transform.rotation = rotation;
            if (processLabel != null) processLabel.transform.rotation = rotation;
        }
        Transform StockAnchor(string name, Vector3 position)
        {
            var anchor = new GameObject(name).transform; anchor.SetParent(transform, false); anchor.position = position; return anchor;
        }
        TextMesh StockLabel(string name, Vector3 position)
        {
            var label = new GameObject(name).AddComponent<TextMesh>(); label.transform.SetParent(transform, false);
            label.transform.position = position; label.anchor = TextAnchor.MiddleCenter;
            label.fontSize = 48; label.characterSize = 0.055f; label.color = Color.white;
            return label;
        }

        public static BoxingStation Create(Transform parent, bool withWorktable = true, bool withCounter = true)
        {
            GameObject root = new GameObject("BoxingStation");
            root.transform.SetParent(parent, false);
            BoxingStation station = root.AddComponent<BoxingStation>();
            station.Build();
            CounterTierVisual.Create(station.transform,"PackingAppearance",ShopLayout.PackageCounter,2.8f,1.4f,1.05f,BurgerShop.UI.FoodIcon.Box).Follow(station);
            CounterTierVisual.Create(station.transform,"WorktableAppearance",ShopLayout.BoxingTable,4.2f,1.4f,1.05f,BurgerShop.UI.FoodIcon.Box).Follow(station);
            station.WorkRoot = new GameObject("PackingWorkUnit").transform;
            station.WorkRoot.SetParent(station.transform, false);
            station.WorkRoot.position = ShopLayout.BoxingTable;
            station.CounterRoot = new GameObject("PackingCounterUnit").transform;
            station.CounterRoot.SetParent(station.transform, false);
            station.CounterRoot.position = ShopLayout.PackageCounter;
            var children = new List<Transform>();
            foreach (Transform child in station.transform) children.Add(child);
            foreach (var child in children)
            {
                if (child == station.WorkRoot || child == station.CounterRoot) continue;
                bool counter = child.name.StartsWith("Package") || child.name.StartsWith("CounterStock") || child.name == "PackingAppearance";
                child.SetParent(counter ? station.CounterRoot : station.WorkRoot, true);
            }
            station.WorkRoot.gameObject.SetActive(withWorktable);
            station.CounterRoot.gameObject.SetActive(withCounter);
            station.Package.enabled = withCounter;
            station.Drop.enabled = withCounter;
            return station;
        }

        void Build()
        {
            Material body = BurgerShop.Core.RuntimeMaterials.Create(new Color(.12f,.21f,.26f));
            Material top = BurgerShop.Core.RuntimeMaterials.Create(new Color(.38f,.70f,.78f));
            Material board = BurgerShop.Core.RuntimeMaterials.Create(new Color(0.93f, 0.82f, 0.62f));
            Material steel = BurgerShop.Core.RuntimeMaterials.Create(new Color(0.35f, 0.38f, 0.42f));
            Vector3 table = ShopLayout.BoxingTable;
            Part("BoxTable", table + Vector3.up * 0.5f, new Vector3(4.2f,1f,1.4f), body);
            Part("BoxTableTop", table + Vector3.up * 1.05f, new Vector3(4.3f,.1f,1.5f), top);
            inputAnchor = StockAnchor("RawInput", table + new Vector3(1.2f,1.18f,0));
            outputAnchor = StockAnchor("BoxOutput", table + new Vector3(-1.2f,1.18f,0));
            rawLabel = StockLabel("RawCount", table + new Vector3(-1.15f, 3.7f, 0));
            boxLabel = StockLabel("BoxCount", table + new Vector3(1.15f, 3.7f, 0));
            processLabel = StockLabel("ProcessCount", table + new Vector3(0, 1.7f, -0.25f));
            processLabel.characterSize = 0.045f;
            ShopFixtures.CreateStationLabel(transform, "BoxingLabel", table + new Vector3(0, 4.85f, 0), "BOX");
            RefreshLabels();

            circle = ShopFixtures.CreateActionCircle(transform, "BoxingCircle", ShopLayout.BoxingCircle, CircleColor);
            Vector3 desk = ShopLayout.PackageCounter;
            Part("PackageDesk", desk + Vector3.up * 0.5f, new Vector3(2.8f,1f,1.4f), body);
            Part("PackageDeskTop", desk + Vector3.up * 1.05f, new Vector3(2.9f,.1f,1.5f), top);
            Part("PackageRegister",desk+new Vector3(.72f,1.20f,0),new Vector3(.5f,.18f,.40f),steel);
            Part("PackageScreen",desk+new Vector3(.72f,1.43f,.12f),new Vector3(.5f,.40f,.10f),steel);
            ShopFixtures.CreateStationLabel(transform, "PackageLabel", desk + new Vector3(0f, 2.95f, 0f), "PACK");

            Transform dropPoint = ShopFixtures.CreateActionCircle(transform, "PackageDrop", ShopLayout.PackageDrop, CircleColor);
            Package = ShopFixtures.CreateCounterStock(transform, ShopLayout.PackageCounterTop, true);
            Package.transform.Find("CounterStockAnchor").position=desk+new Vector3(-.70f,1.18f,0);
            Drop = gameObject.AddComponent<CounterDropZone>();
            Drop.Configure(Package, dropPoint, 0.9f, 0.25f, true);
        }

        void Part(string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(transform, false);
            part.transform.position = position;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            SolidOccupancy.Apply(part.GetComponent<Collider>(), true);
        }
    }
}
