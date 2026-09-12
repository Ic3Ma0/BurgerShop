using System;
using System.Collections.Generic;
using BurgerShop.Customer;
using BurgerShop.Economy;
using BurgerShop.Player;
using UnityEngine;

namespace BurgerShop.Restaurant
{
    public sealed class DriveThruLane : MonoBehaviour
    {
        public const int ComboPrice = 15;
        public const int MaxCars = 3;
        public const float SellInterval = 0.75f;
        public const float HandoffDuration = 0.42f;
        static readonly Color CircleColor = new Color(0.24f, 0.77f, 0.46f);
        static readonly Color[] CarColors =
        {
            new Color(0.98f, 0.78f, 0.18f),
            new Color(0.55f, 0.72f, 0.88f),
            new Color(0.96f, 0.62f, 0.78f)
        };

        readonly List<LaneCar> cars = new List<LaneCar>(MaxCars);
        BoxingStation boxing;
        RestaurantWallet wallet;
        CashFloor cash;
        BurgerInventory player;
        Transform circle;
        float cooldown;
        float spawnWait;
        bool paused;
        public Func<int> OrderQuantityFactory { get; set; } = OrderQuantities.Drive;
        public CustomerOrder WaitingOrder
        {
            get
            {
                foreach (var car in cars) if (car != null && car.IsStoppedAtWindow) return car.Order;
                return SlotZeroCar()?.Order;
            }
        }
        public int CompletedOrders { get; private set; }
        public int DeliveredUnits { get; private set; }

        public Vector3 WindowPosition => circle != null ? circle.position : ShopLayout.DriveThruCircle;
        public Vector3 CashPosition => ShopLayout.DriveThruCash;
        public Vector3 HandoffOrigin => ShopLayout.DriveThruWindow + new Vector3(0f, 0.78f, 0.2f);
        public int CarCount => cars.Count;
        public bool IsHandoffActive
        {
            get
            {
                for (int i = 0; i < cars.Count; i++)
                    if (cars[i] != null && cars[i].IsReceiving) return true;
                return false;
            }
        }
        public bool HasWaitingCar
        {
            get
            {
                for (int i = 0; i < cars.Count; i++)
                    if (cars[i] != null && cars[i].IsAtWindow) return true;
                return false;
            }
        }
        public bool HasStoppedCarAtWindow
        {
            get
            {
                LaneCar car = StoppedWindowCar();
                return car != null && car.IsStoppedAtWindow;
            }
        }
        public Vector3 WaitingCarPosition
        {
            get
            {
                LaneCar car = SlotZeroCar();
                return car != null ? car.transform.position : ShopLayout.DriveThruQueue[0];
            }
        }
        public bool ReadyToSell => isActiveAndEnabled && !paused && cooldown <= 0f && !IsHandoffActive
            && HasStoppedCarAtWindow && boxing != null && boxing.PackageCount > 0
            && wallet != null && wallet.CanCompleteSale();

        public void BindBoxing(BoxingStation station) => boxing = station;

        public void Configure(BoxingStation station, RestaurantWallet earnings, CashFloor floor,
            BurgerInventory carrier)
        {
            boxing = station;
            wallet = earnings;
            cash = floor;
            player = carrier;
            cooldown = 0f;
            spawnWait = 0f;
        }

        public bool IsActorInRange(Transform actor)
        {
            if (actor == null) return false;
            Vector3 offset = actor.position - WindowPosition;
            offset.y = 0f;
            return offset.sqrMagnitude <= 1f;
        }

        void Update() => Advance(Time.deltaTime);

        public void Advance(float deltaTime)
        {
            if (deltaTime <= 0f || paused || !isActiveAndEnabled) return;
            cooldown = Mathf.Max(0f, cooldown - deltaTime);
            TickCars(deltaTime);
            TrySpawn(deltaTime);
            TrySellFrom(player);
            cash?.Advance(deltaTime);
        }

        public bool TrySellFrom(BurgerInventory carrier)
        {
            if (!ReadyToSell || carrier == null || !carrier.isActiveAndEnabled
                || !IsActorInRange(carrier.transform))
                return false;
            LaneCar car = StoppedWindowCar();
            if (car == null || boxing == null || !boxing.TryIssueCombo(out Transform box))
                return false;
            cooldown = SellInterval;
            car.BeginHandoff(box, HandoffOrigin, carrier);
            return true;
        }

        LaneCar SlotZeroCar()
        {
            for (int i = 0; i < cars.Count; i++)
                if (cars[i] != null && cars[i].Slot == 0 && !cars[i].HasLeft) return cars[i];
            return null;
        }

        LaneCar StoppedWindowCar()
        {
            for (int i = 0; i < cars.Count; i++)
                if (cars[i] != null && cars[i].IsStoppedAtWindow && !cars[i].IsReceiving)
                    return cars[i];
            return null;
        }

        void TrySpawn(float deltaTime)
        {
            spawnWait = Mathf.Max(0f, spawnWait - deltaTime);
            if (cars.Count >= MaxCars || spawnWait > 0f) return;
            int slot = FirstFreeSlot();
            if (slot < 0) return;
            cars.Add(LaneCar.Create(transform, slot, CarColors[cars.Count % CarColors.Length], OrderQuantityFactory()));
            spawnWait = 1.4f;
        }

        int FirstFreeSlot()
        {
            for (int slot = 0; slot < MaxCars; slot++)
            {
                bool taken = false;
                for (int i = 0; i < cars.Count; i++)
                    if (cars[i] != null && cars[i].Slot == slot && !cars[i].IsLeaving) { taken = true; break; }
                if (!taken) return slot;
            }
            return -1;
        }

        void TickCars(float deltaTime)
        {
            for (int i = cars.Count - 1; i >= 0; i--)
            {
                LaneCar car = cars[i];
                if (car == null)
                {
                    cars.RemoveAt(i);
                    continue;
                }
                int before = car.Order.Delivered;
                car.Advance(deltaTime);
                DeliveredUnits += car.Order.Delivered - before;
                if (car.ConsumeHandoffComplete() && car.Order.TrySettle())
                {
                    wallet.RecordCompletedSale();
                    CompletedOrders++;
                    UI.FeedbackDirector.Current?.World(car.transform.position,"",.45f,car.LastServer != null ? car.LastServer.transform : null);
                    cash?.DropAt(CashPosition, ComboPrice * car.Order.Quantity);
                    car.LastServer?.GetComponent<RestaurantWorker>()?.RecordCompletedOrder(true);
                }
                if (!car.IsLeaving) continue;
                if (car.HasLeft)
                {
                    BurgerVisual.Release(car.gameObject);
                    cars.RemoveAt(i);
                }
            }
            PullForward();
        }

        void OnApplicationPause(bool value) => paused = value;

        void PullForward()
        {
            for (int slot = 0; slot < MaxCars; slot++)
            {
                bool filled = false;
                for (int i = 0; i < cars.Count; i++)
                    if (cars[i] != null && cars[i].Slot == slot && !cars[i].IsLeaving) { filled = true; break; }
                if (filled) continue;
                LaneCar next = null;
                int best = MaxCars;
                for (int i = 0; i < cars.Count; i++)
                {
                    LaneCar car = cars[i];
                    if (car == null || car.IsLeaving || car.Slot <= slot || car.Slot >= best) continue;
                    best = car.Slot;
                    next = car;
                }
                next?.AssignSlot(slot);
            }
        }

        public static DriveThruLane Create(Transform parent, BoxingStation station, RestaurantWallet earnings,
            CashFloor floor, BurgerInventory carrier)
        {
            GameObject root = new GameObject("DriveThruLane");
            root.transform.SetParent(parent, false);
            DriveThruLane lane = root.AddComponent<DriveThruLane>();
            lane.Build();
            lane.Configure(station, earnings, floor, carrier);
            return lane;
        }

        void Build()
        {
            Material asphalt = BurgerShop.Core.RuntimeMaterials.Create(new Color(0.16f, 0.18f, 0.22f));
            Material line = BurgerShop.Core.RuntimeMaterials.Create(new Color(0.92f, 0.93f, 0.96f), true);
            Material body = BurgerShop.Core.RuntimeMaterials.Create(new Color(0.92f, 0.93f, 0.96f));
            Material top = BurgerShop.Core.RuntimeMaterials.Create(new Color(0.86f, 0.22f, 0.22f));
            Material steel = BurgerShop.Core.RuntimeMaterials.Create(new Color(0.35f, 0.38f, 0.42f));

            Vector3 window = ShopLayout.DriveThruWindow;
            Part("WindowDesk", PrimitiveType.Cube, window + Vector3.up * 0.32f, new Vector3(2.8f, 0.64f, 0.85f), body);
            Part("WindowTop", PrimitiveType.Cube, window + Vector3.up * 0.68f, new Vector3(2.95f, 0.08f, 0.95f), top);
            Part("WindowPad", PrimitiveType.Cube, window + new Vector3(0.85f, 0.82f, -0.12f),
                new Vector3(0.36f, 0.16f, 0.2f), steel);
            ShopFixtures.CreateStationLabel(transform, "WindowLabel", window + new Vector3(0f, 1.25f, 0f), "WINDOW");
            circle = ShopFixtures.CreateActionCircle(transform, "DriveThruCircle", ShopLayout.DriveThruCircle,
                CircleColor);

            Vector3 road = ShopLayout.DriveThruRoad;
            Part("Road", PrimitiveType.Cube, road + Vector3.up * 0.04f, new Vector3(18.4f, 0.08f, 3.4f), asphalt);
            float dashWest = road.x - 8.6f;
            for (int i = 0; i < 9; i++)
                Part("LaneDash_" + i, PrimitiveType.Cube,
                    new Vector3(dashWest + i * 2.1f, 0.09f, road.z), new Vector3(1.1f, 0.02f, 0.12f), line);

            TextMesh mark = new GameObject("DriveThruMark").AddComponent<TextMesh>();
            mark.transform.SetParent(transform, false);
            mark.transform.position = new Vector3(window.x - 1.2f, 0.12f, road.z + 0.85f);
            mark.transform.rotation = Quaternion.Euler(90f, 90f, 0f);
            mark.anchor = TextAnchor.MiddleCenter;
            mark.alignment = TextAlignment.Center;
            mark.characterSize = 0.12f;
            mark.fontSize = 36;
            mark.color = new Color(0.94f, 0.96f, 1f);
            mark.text = "DRIVE-THRU";

            Vector3 stopAt = new Vector3(ShopLayout.DriveThruExit.x + 1.8f, 0f, road.z + 1.35f);
            TextMesh stop = new GameObject("StopSign").AddComponent<TextMesh>();
            stop.transform.SetParent(transform, false);
            stop.transform.position = stopAt + Vector3.up * 1.15f;
            stop.anchor = TextAnchor.MiddleCenter;
            stop.characterSize = 0.08f;
            stop.fontSize = 32;
            stop.color = new Color(0.95f, 0.22f, 0.22f);
            stop.text = "STOP";
            Part("StopPost", PrimitiveType.Cube, stopAt + Vector3.up * 0.55f,
                new Vector3(0.12f, 1.1f, 0.12f), steel);
        }

        void Part(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(transform, false);
            part.transform.position = position;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            Collider collider = part.GetComponent<Collider>();
            collider.enabled = false;
            BurgerVisual.Release(collider);
        }

        sealed class LaneCar : MonoBehaviour
        {
            public CustomerOrder Order { get; private set; }
            public BurgerInventory LastServer { get; private set; }
            Transform bubble;
            TextMesh quantityLabel;
            public int Slot { get; private set; }
            public bool IsWaiting { get; private set; }
            public bool IsReceiving { get; private set; }
            public bool IsLeaving { get; private set; }
            public bool HasLeft { get; private set; }
            public bool IsAtWindow => Slot == 0 && (IsWaiting || IsReceiving) && !IsLeaving && !HasLeft;
            public bool IsStoppedAtWindow => IsAtWindow && HorizontalToWindow() <= 0.12f;

            Vector3[] route;
            int waypoint;
            float speed = 5.2f;
            Transform inbound;
            Vector3 boxOrigin;
            float handoffAge;
            bool handoffComplete;
            static readonly Vector3 CarWindowLocal = new Vector3(0f, 0.52f, 0.48f);

            public static LaneCar Create(Transform parent, int slot, Color color, int quantity)
            {
                GameObject root = new GameObject("LaneCar_" + slot);
                root.transform.SetParent(parent, false);
                root.transform.position = ShopLayout.DriveThruSpawn;
                LaneCar car = root.AddComponent<LaneCar>();
                car.Order = new CustomerOrder(quantity, 2);
                car.Build(color);
                car.BuildBubble();
                car.AssignSlot(slot);
                return car;
            }

            public void AssignSlot(int slot)
            {
                Slot = Mathf.Clamp(slot, 0, ShopLayout.DriveThruQueue.Length - 1);
                IsWaiting = false;
                IsReceiving = false;
                IsLeaving = false;
                speed = 5.2f;
                route = new[] { ShopLayout.DriveThruQueue[Slot] };
                waypoint = 0;
            }

            public void BeginHandoff(Transform box, Vector3 from, BurgerInventory server)
            {
                Order.TryReserve();
                LastServer = server;
                inbound = box;
                boxOrigin = from;
                if (box != null)
                {
                    box.SetParent(transform, true);
                    box.position = from;
                }
                IsReceiving = true;
                IsWaiting = true;
                IsLeaving = false;
                handoffAge = 0f;
                handoffComplete = false;
                speed = 0f;
            }

            public bool ConsumeHandoffComplete()
            {
                if (!handoffComplete) return false;
                handoffComplete = false;
                return true;
            }

            public void Advance(float deltaTime)
            {
                if (HasLeft || route == null || deltaTime <= 0f) return;
                if (IsReceiving)
                {
                    TickHandoff(deltaTime);
                    return;
                }
                if (waypoint >= route.Length)
                {
                    if (IsLeaving) HasLeft = true;
                    else
                    {
                        IsWaiting = Slot == 0;
                        speed = 0f;
                    }
                    return;
                }
                Vector3 target = route[waypoint];
                Vector3 offset = target - transform.position;
                offset.y = 0f;
                float step = speed * deltaTime;
                if (offset.sqrMagnitude <= step * step)
                {
                    transform.position = new Vector3(target.x, transform.position.y, target.z);
                    waypoint++;
                    return;
                }
                Vector3 dir = offset.normalized;
                transform.position += dir * step;
                if (dir.sqrMagnitude > 0.0001f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir),
                        1f - Mathf.Exp(-10f * deltaTime));
            }

            void TickHandoff(float deltaTime)
            {
                handoffAge += deltaTime;
                float t = Mathf.Clamp01(handoffAge / HandoffDuration);
                float eased = t * t * (3f - 2f * t);
                Vector3 dest = transform.TransformPoint(CarWindowLocal);
                if (inbound != null)
                {
                    inbound.position = Vector3.Lerp(boxOrigin, dest, eased)
                        + Vector3.up * (0.55f * Mathf.Sin(t * Mathf.PI));
                    inbound.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.55f, t);
                }
                if (t < 1f) return;
                if (inbound != null)
                {
                    inbound.SetParent(transform, false);
                    inbound.localPosition = CarWindowLocal;
                    inbound.localScale = Vector3.one * 0.55f;
                }
                Order.Receive();
                if (inbound != null) BurgerVisual.Release(inbound.gameObject);
                inbound = null;
                IsReceiving = false;
                quantityLabel.text = "x" + Order.Remaining;
                if (!Order.IsComplete) return;
                bubble.gameObject.SetActive(false);
                IsWaiting = false;
                IsLeaving = true;
                handoffComplete = true;
                speed = 5.2f;
                route = new[] { ShopLayout.DriveThruExit };
                waypoint = 0;
            }

            float HorizontalToWindow()
            {
                Vector3 a = transform.position;
                Vector3 b = ShopLayout.DriveThruQueue[0];
                a.y = b.y = 0f;
                return Vector3.Distance(a, b);
            }

            void BuildBubble()
            {
                bubble = new GameObject("CarOrderBubble").transform;
                bubble.SetParent(transform, false);
                bubble.localPosition = Vector3.up * 1.8f;
                var background = GameObject.CreatePrimitive(PrimitiveType.Cube);
                background.name = "Background";
                background.transform.SetParent(bubble, false);
                background.transform.localScale = new Vector3(1.15f, 0.6f, 0.035f);
                var collider = background.GetComponent<Collider>(); collider.enabled = false; BurgerVisual.Release(collider);
                Material white = Core.RuntimeMaterials.Create(new Color(1, 0.98f, 0.9f), true);
                background.GetComponent<Renderer>().sharedMaterial = white;
                background.AddComponent<BurgerVisual>().OwnMaterials(white);
                var icon = BoxVisualFactory.Create(bubble, 0);
                icon.localPosition = new Vector3(-0.28f, -0.1f, -0.15f);
                icon.localScale = Vector3.one * 0.65f;
                quantityLabel = new GameObject("OrderQuantity").AddComponent<TextMesh>();
                quantityLabel.transform.SetParent(bubble, false);
                quantityLabel.transform.localPosition = new Vector3(0.25f, 0, -0.06f);
                quantityLabel.anchor = TextAnchor.MiddleCenter;
                quantityLabel.fontSize = 48;
                quantityLabel.characterSize = 0.12f;
                quantityLabel.color = new Color(0.22f, 0.15f, 0.08f);
                quantityLabel.text = "x" + Order.Quantity;
            }

            void LateUpdate() { if (bubble != null && Camera.main != null) bubble.rotation = Camera.main.transform.rotation; }

            void Build(Color color)
            {
                Material paint = BurgerShop.Core.RuntimeMaterials.Create(color);
                Material dark = BurgerShop.Core.RuntimeMaterials.Create(new Color(0.12f, 0.12f, 0.14f));
                Material glass = BurgerShop.Core.RuntimeMaterials.Create(new Color(0.55f, 0.72f, 0.82f));
                AddPart("Body", PrimitiveType.Cube, new Vector3(0f, 0.22f, 0f), new Vector3(1.15f, 0.32f, 2.05f), paint);
                AddPart("Cabin", PrimitiveType.Cube, new Vector3(0f, 0.48f, -0.18f), new Vector3(0.95f, 0.28f, 0.95f), glass);
                for (int i = 0; i < 4; i++)
                {
                    float x = i % 2 == 0 ? -0.42f : 0.42f;
                    float z = i < 2 ? 0.62f : -0.62f;
                    AddPart("Wheel_" + i, PrimitiveType.Cylinder, new Vector3(x, 0.12f, z),
                        new Vector3(0.22f, 0.08f, 0.22f), dark);
                    transform.Find("Wheel_" + i).localRotation = Quaternion.Euler(0f, 0f, 90f);
                }
            }

            void AddPart(string name, PrimitiveType type, Vector3 local, Vector3 scale, Material material)
            {
                GameObject part = GameObject.CreatePrimitive(type);
                part.name = name;
                part.transform.SetParent(transform, false);
                part.transform.localPosition = local;
                part.transform.localScale = scale;
                part.GetComponent<Renderer>().sharedMaterial = material;
                Collider collider = part.GetComponent<Collider>();
                collider.enabled = false;
                BurgerVisual.Release(collider);
            }
        }
    }
}
