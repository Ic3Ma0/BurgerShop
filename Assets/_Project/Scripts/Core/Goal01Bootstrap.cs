using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using BurgerShop.UI;
using BurgerShop.Customer;
using BurgerShop.Economy;
using BurgerShop.Persistence;

namespace BurgerShop.Core
{
    public static class Goal01Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (Object.FindFirstObjectByType<PlayerMotor>() != null)
                return;

            InstallShop();
        }

        public static void RebuildInstalledShop()
        {
            var existing = Object.FindFirstObjectByType<PlayerMotor>();
            if (existing != null)
            {
                Transform root = existing.transform.parent;
                if (root != null) Object.DestroyImmediate(root.gameObject);
            }
            InstallShop();
        }

        static void InstallShop()
        {
            Transform root = new GameObject("Goal01").transform;
            Material floorMat = CreateLit(new Color(0.80f, 0.68f, 0.50f));
            Material wallMat = CreateLit(new Color(0.40f, 0.29f, 0.17f));
            Material hrFloor = CreateLit(new Color(0.72f, 0.70f, 0.62f));
            Material boostFloor = CreateLit(new Color(0.70f, 0.56f, 0.42f));
            Material playerMat = CreateLit(new Color(0.89f, 0.48f, 0.16f));

            StreetEnvironment.Build(root);
            ShopLayout.CreateFloor(root, floorMat);
            ShopLayout.CreateWalls(root, wallMat);
            HrOffice office = HrOffice.Create(root, wallMat, hrFloor);
            office.SetOpen(false);
            BoostRoom boostRoom = BoostRoom.Create(root, wallMat, boostFloor);
            boostRoom.SetAnnexOpen(false);
            Transform player = CreatePlayer(root, playerMat);
            BurgerInventory inventory = player.gameObject.AddComponent<BurgerInventory>();
            TrashInventory trashBag = player.gameObject.AddComponent<TrashInventory>();
            ConfigureCamera(player);
            CustomerQueue customers = CreateCustomers(root);
            DiningArea dining = DiningArea.Create(root, ShopLayout.Tables);
            dining.BindCollector(trashBag);
            TrashBin bin = TrashBin.Create(root, ShopLayout.TrashBin);
            bin.Configure(trashBag);
            CounterStock stock = ShopFixtures.CreateCounterStock(customers.transform, ShopLayout.CounterTop);
            RestaurantWallet wallet = root.gameObject.AddComponent<RestaurantWallet>();
            PartsWallet parts = root.gameObject.AddComponent<PartsWallet>();
            root.gameObject.AddComponent<SaleFeedback>().Configure(wallet);
            CashFloor cash = root.gameObject.AddComponent<CashFloor>();
            cash.Configure(wallet, player, ShopLayout.CounterCash);
            dining.BindCash(cash);
            ExpandableGrill starter = ExpandableGrill.CreateStarter(root, inventory, wallet);
            ProductionStation station = starter.Station;
            BurgerPickupZone pickup = starter.Pickup;
            GrillUpgradeZone upgrade = starter.Upgrade;
            BurgerServingZone serving = CreateServingZone(customers, inventory, wallet, stock, dining, cash);
            WorkerHiringZone hiring = CreateHiringZone(root, station, serving, pickup, inventory, wallet, dining, bin, office);
            PlayerMotor motor = player.GetComponent<PlayerMotor>();
            BoostUpgradeZone boost = CreateBoostZone(root, inventory, motor, wallet, boostRoom);
            ShopExpansion expansion = ShopExpansion.Create(root, dining, serving, hiring, inventory, wallet, cash);
            StaffUpgradeBoard staffUpgrades = root.gameObject.AddComponent<StaffUpgradeBoard>();
            staffUpgrades.Configure(wallet, hiring, inventory);
            TableUpgradeBoard tableUpgrades = root.gameObject.AddComponent<TableUpgradeBoard>();
            tableUpgrades.Configure(dining, expansion, wallet, inventory);
            SessionGoalTracker goals = root.gameObject.AddComponent<SessionGoalTracker>();
            goals.Configure(inventory, station, stock, customers, wallet, serving, dining, trashBag, hiring, boost, expansion);
            CreateJoystick(root, inventory, pickup, customers, wallet, upgrade, hiring, goals, trashBag, staffUpgrades,
                boost, null, tableUpgrades);
            FeedbackDirector.Build(Object.FindFirstObjectByType<Canvas>().transform.Find("SafeArea"), inventory);
            WorldLabelHud.Build(Object.FindFirstObjectByType<Canvas>().transform.Find("SafeArea"), player);
            expansion.BindUpgradeHud(Object.FindFirstObjectByType<UpgradeHud>());
            var growth = root.gameObject.AddComponent<GrowthUpgrades>();
            growth.Configure(wallet,goals,inventory,expansion);
            root.gameObject.AddComponent<BagLine>().Configure(wallet,goals,growth,hiring,inventory,cash);
            GrowthUpgradeHud.Build(Object.FindFirstObjectByType<Canvas>().transform.Find("SafeArea"),growth,goals,wallet);
            root.gameObject.AddComponent<CourierLine>().Configure(wallet,parts,inventory,cash,station,true);
            PartsHud.Build(Object.FindFirstObjectByType<Canvas>().transform.Find("SafeArea"),parts);
            var layout = root.gameObject.AddComponent<Building.FacilityLayout>();
            layout.Configure(inventory,wallet,parts,cash,dining,hiring,goals,expansion);
            Building.FacilityShopHud.Build(Object.FindFirstObjectByType<Canvas>().transform.Find("SafeArea"),layout);
            root.gameObject.AddComponent<RestroomExpansion>().Configure(wallet,inventory,goals);
            RestaurantPersistence persistence = root.gameObject.AddComponent<RestaurantPersistence>();
            persistence.Configure(wallet, upgrade, hiring, boost, expansion, staffUpgrades, goals, tables: tableUpgrades);
            root.gameObject.AddComponent<RestaurantArchitecture>().Configure();
            SaveSlotsHud.Build(Object.FindFirstObjectByType<Canvas>().transform.Find("SafeArea"), persistence);
            FacilityDetailsHud.Build(Object.FindFirstObjectByType<Canvas>().transform.Find("SafeArea"),layout,Object.FindFirstObjectByType<Building.FacilityShopHud>(),growth);
            UpgradeGuide.Build(Object.FindFirstObjectByType<Canvas>().transform.Find("SafeArea"), goals, upgrade, Object.FindFirstObjectByType<StarProgressHud>());
            CreateSaveHud(Object.FindFirstObjectByType<Canvas>().transform.Find("SafeArea"), persistence);
            OfflineSettleHud.Build(Object.FindFirstObjectByType<Canvas>().transform.Find("SafeArea"),persistence);
            player.gameObject.AddComponent<TemporaryPowerups>().Configure(inventory,motor,wallet,Object.FindFirstObjectByType<Canvas>().transform.Find("SafeArea"));
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            root.gameObject.AddComponent<AndroidDiagnostics>().Configure(wallet, inventory, hiring, upgrade);
#endif
        }

        static void CreateSaveHud(Transform canvas, RestaurantPersistence persistence)
        {
            GameObject hud = new GameObject("SaveStatus", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            hud.transform.SetParent(canvas, false);
            RectTransform rect = hud.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 8f);
            rect.sizeDelta = new Vector2(1000f, 28f);
            Text text = hud.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 18;
            text.alignment = TextAnchor.LowerCenter;
            text.color = new Color(0.17f, 0.21f, 0.2f, 0.45f);
            text.raycastTarget = false;
            hud.AddComponent<SaveHud>().Configure(persistence, text);
        }

        static WorkerHiringZone CreateHiringZone(Transform root, ProductionStation station, BurgerServingZone serving,
            BurgerPickupZone pickup, BurgerInventory inventory, RestaurantWallet wallet, DiningArea dining, TrashBin bin,
            HrOffice office)
        {
            WorkerHiringZone hiring = root.gameObject.AddComponent<WorkerHiringZone>();
            hiring.Configure(station, serving, wallet, inventory, pickup.PickupPoint, office.HirePoint,
                ShopLayout.Aisle, serving.DropZone, office.HireLabel, dining, bin);
            return hiring;
        }

        static BoostUpgradeZone CreateBoostZone(Transform root, BurgerInventory inventory, PlayerMotor motor,
            RestaurantWallet wallet, BoostRoom room)
        {
            BoostUpgradeZone boost = root.gameObject.AddComponent<BoostUpgradeZone>();
            boost.Configure(wallet, inventory, motor, room.BoostPoint, room.BoostLabel);
            return boost;
        }

        static BurgerServingZone CreateColaServing(Transform root, BurgerInventory inventory, RestaurantWallet wallet,
            DiningArea dining, CashFloor cash)
        {
            Transform area = new GameObject("ColaCustomerArea").transform;
            area.SetParent(root, false);
            Vector3 counterPosition = ShopLayout.ColaCounter;
            Material counter = CreateLit(new Color(0.22f, 0.42f, 0.62f));
            Material top = CreateLit(new Color(0.86f, 0.90f, 0.94f));
            CreateStationPart(area, "ColaCounter", counterPosition + Vector3.up * 0.5f, new Vector3(3.2f, 1f, 1.4f), counter);
            CreateStationPart(area, "ColaCounterTop", counterPosition + Vector3.up * 1.05f, new Vector3(3.35f, 0.12f, 1.55f), top);
            ShopFixtures.CreateStationLabel(area, "ColaCounterLabel", counterPosition + Vector3.up * 1.85f, "COLA");

            CustomerQueue queue = area.gameObject.AddComponent<CustomerQueue>();
            queue.Product = KitchenProduct.Cola;
            queue.Configure(ShopLayout.Entrance, ShopLayout.ColaQueueEntry, ShopLayout.ColaQueueSlots, counterPosition);

            CounterStock stock = ShopFixtures.CreateCounterStock(area, ShopLayout.ColaCounterTop, false, KitchenProduct.Cola);
            Transform circle = ShopFixtures.CreateCashierCircle(area, ShopLayout.ColaServingCircle);
            circle.name = "ColaCashierCircle";
            CounterDropZone drop = area.gameObject.AddComponent<CounterDropZone>();
            drop.Configure(stock, circle, 1.05f, 0.25f, false, KitchenProduct.Cola);
            BurgerServingZone serving = area.gameObject.AddComponent<BurgerServingZone>();
            serving.Configure(queue, inventory, wallet, circle, ShopLayout.Exit, stock, dining, drop, 10, cash);
            return serving;
        }

        static BurgerServingZone CreateServingZone(CustomerQueue queue, BurgerInventory inventory, RestaurantWallet wallet,
            CounterStock stock, DiningArea dining, CashFloor cash)
        {
            Transform circle = ShopFixtures.CreateCashierCircle(queue.transform, ShopLayout.ServingCircle);
            CounterDropZone drop = queue.gameObject.AddComponent<CounterDropZone>();
            drop.Configure(stock, circle);
            Vector3[] exit = ShopLayout.Exit;
            BurgerServingZone serving = queue.gameObject.AddComponent<BurgerServingZone>();
            serving.Configure(queue, inventory, wallet, circle, exit, stock, dining, drop, 10, cash);
            return serving;
        }

        static CustomerQueue CreateCustomers(Transform root)
        {
            Transform restaurant = new GameObject("CustomerArea").transform;
            restaurant.SetParent(root, false);
            Vector3 counterPosition = ShopLayout.Counter;
            Material counter = CreateLit(new Color(0.38f, 0.49f, 0.58f));
            Material top = CreateLit(new Color(0.90f, 0.88f, 0.78f));
            CreateStationPart(restaurant, "OrderCounter", counterPosition + Vector3.up * 0.5f, new Vector3(3.2f, 1f, 1.4f), counter);
            CreateStationPart(restaurant, "OrderCounterTop", counterPosition + Vector3.up * 1.05f, new Vector3(3.35f, 0.12f, 1.55f), top);

            Vector3 entrance = ShopLayout.Entrance;
            Vector3 queueEntry = ShopLayout.QueueEntry;
            Vector3[] slots = ShopLayout.QueueSlots;

            CustomerQueue queue = restaurant.gameObject.AddComponent<CustomerQueue>();
            queue.Configure(entrance, queueEntry, slots, counterPosition);
            return queue;
        }

        static GameObject CreateStationPart(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            ApplyMaterial(part, material);
            SolidOccupancy.Apply(part.GetComponent<Collider>(), true);
            return part;
        }

        static Transform CreatePlayer(Transform root, Material material)
        {
            GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.tag = "Player";
            player.transform.SetParent(root, false);
            player.transform.position = ShopLayout.PlayerSpawn;
            ApplyMaterial(player, material);

            Collider primitiveCollider = player.GetComponent<Collider>();
            if (primitiveCollider != null)
                Object.Destroy(primitiveCollider);

            CharacterController controller = player.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.4f;
            controller.center = new Vector3(0f, 0f, 0f);

            player.GetComponent<Renderer>().enabled=false;
            var white=CreateLit(new Color(.95f,.94f,.88f));var skin=CreateLit(new Color(.72f,.45f,.29f));var dark=CreateLit(new Color(.18f,.13f,.11f));
            BurgerShop.Restaurant.BagVisualFactory.Part(player.transform,"ChefJacket",PrimitiveType.Capsule,new Vector3(0,-.02f,0),new Vector3(.65f,.325f,.42f),white);
            BurgerShop.Restaurant.BagVisualFactory.Part(player.transform,"Head",PrimitiveType.Sphere,new Vector3(0,.53f,0),Vector3.one*.52f,skin);
            BurgerShop.Restaurant.BagVisualFactory.Part(player.transform,"ChefHat",PrimitiveType.Cylinder,new Vector3(0,.85f,0),new Vector3(.64f,.17f,.64f),white);
            BurgerShop.Restaurant.CourierVisuals.Part(player.transform,"Facing",new Vector3(0,.52f,.26f),Vector3.one*.12f,skin);
            BurgerShop.Restaurant.CourierVisuals.Part(player.transform,"Apron",new Vector3(0,-.23f,.235f),new Vector3(.48f,.6f,.045f),white);
            for(int i=0;i<3;i++)BurgerShop.Restaurant.CourierVisuals.Part(player.transform,"JacketButton",new Vector3(.12f,.16f-i*.13f,.225f),Vector3.one*.045f,dark);
            for(int i=-1;i<=1;i++)BurgerShop.Restaurant.BagVisualFactory.Part(player.transform,"HatPuff",PrimitiveType.Sphere,new Vector3(i*.18f,.99f,0),new Vector3(.36f,.3f,.48f),white);
            HumanoidVisual.Add(player.transform,-1f,white,skin,dark);

            player.AddComponent<PlayerMotor>();
            return player.transform;
        }

        static void ConfigureCamera(Transform player)
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.tag = "MainCamera";
                cameraObject.AddComponent<AudioListener>();
            }

            CameraFollow follow = camera.GetComponent<CameraFollow>();
            if (follow == null)
                follow = camera.gameObject.AddComponent<CameraFollow>();
            follow.SetTarget(player);
        }

        static void CreateJoystick(Transform root, BurgerInventory inventory, BurgerPickupZone pickup, CustomerQueue customers, RestaurantWallet wallet, GrillUpgradeZone upgrade, WorkerHiringZone hiring, SessionGoalTracker goals, TrashInventory trashBag, StaffUpgradeBoard staffUpgrades = null, BoostUpgradeZone playerBoost = null, GrillUpgradeZone colaUpgrade = null, TableUpgradeBoard tableUpgrades = null)
        {
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.transform.SetParent(root, false);
                eventSystem.AddComponent<EventSystem>();
                var uiModule = eventSystem.AddComponent<InputSystemUIInputModule>();
                if (InputSystem.actions != null)
                    uiModule.actionsAsset = InputSystem.actions;
            }

            GameObject canvasObject = new GameObject("JoystickCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(root, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            var safeArea = new GameObject("SafeArea", typeof(RectTransform));
            safeArea.transform.SetParent(canvasObject.transform, false);
            safeArea.AddComponent<SafeAreaFitter>();
            Transform uiRoot = safeArea.transform;

            Sprite circle = HudChrome.Circle();

            GameObject padObject = new GameObject("VirtualJoystick", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            padObject.transform.SetParent(uiRoot, false);
            RectTransform pad = padObject.GetComponent<RectTransform>();
            pad.anchorMin = new Vector2(0f, 0f);
            pad.anchorMax = new Vector2(0f, 0f);
            pad.pivot = new Vector2(0.5f, 0.5f);
            pad.anchoredPosition = new Vector2(220f, 240f);
            pad.sizeDelta = new Vector2(280f, 280f);
            Image padImage = padObject.GetComponent<Image>();
            padImage.sprite = circle;
            padImage.color = new Color(1f, .96f, .90f, .55f);
            padImage.raycastTarget = true;

            GameObject knobObject = new GameObject("Knob", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            knobObject.transform.SetParent(pad, false);
            RectTransform knob = knobObject.GetComponent<RectTransform>();
            knob.anchorMin = new Vector2(0.5f, 0.5f);
            knob.anchorMax = new Vector2(0.5f, 0.5f);
            knob.pivot = new Vector2(0.5f, 0.5f);
            knob.anchoredPosition = Vector2.zero;
            knob.sizeDelta = new Vector2(110f, 110f);
            Image knobImage = knobObject.GetComponent<Image>();
            knobImage.sprite = circle;
            knobImage.color = HudChrome.Tomato;
            knobImage.raycastTarget = false;

            padObject.AddComponent<VirtualJoystick>();

            HudChrome.BuildTopBand(uiRoot);
            StarProgressHud.Build(uiRoot, goals);
            TaskCapsuleHud.Build(uiRoot, goals);
            CreateCustomerHud(uiRoot, customers);
            SalesHud.Build(uiRoot, wallet);
            CreateUpgradeHud(uiRoot, upgrade);
            if (colaUpgrade != null)
                Object.FindFirstObjectByType<UpgradeHud>()?.AddZone(colaUpgrade);
            CreateStaffHud(uiRoot, hiring);
            if (staffUpgrades != null)
                StaffUpgradeHud.Build(uiRoot, staffUpgrades);
            if (playerBoost != null)
                PlayerUpgradeHud.Build(uiRoot, playerBoost);
            if (tableUpgrades != null)
                TableChangeHud.Build(uiRoot, tableUpgrades);
        }

        static void CreateStaffHud(Transform canvas, WorkerHiringZone hiring)
        {
            Text status = HudChrome.Label(canvas, "StaffStatus", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-24f, 196f), new Vector2(440f, 36f), 22, HudChrome.Ink, TextAnchor.LowerRight, true, true);

            Image background = HudChrome.Panel(canvas, "HiringPanel", new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-24f, 36f), new Vector2(460f, 150f), HudChrome.Cream, 0.9f);
            var group = background.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            Text details = HudChrome.Label(background.transform, "HiringStatus", new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(18f, -16f), new Vector2(424f, 100f), 28,
                HudChrome.Ink, TextAnchor.UpperLeft, true, true);
            Image progress = HudChrome.Panel(background.transform, "HiringProgress", Vector2.zero, Vector2.zero,
                new Vector2(18f, 16f), new Vector2(424f, 12f), new Color(0.23f, 0.83f, 0.94f), 0.4f);
            progress.type = Image.Type.Filled;
            progress.fillMethod = Image.FillMethod.Horizontal;
            background.gameObject.AddComponent<StaffHud>().Configure(hiring, status, details, progress, group);
        }

        static void CreateUpgradeHud(Transform canvas, GrillUpgradeZone upgrade)
        {
            Image background = HudChrome.Panel(canvas, "UpgradePanel", new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-24f, 36f), new Vector2(460f, 150f), HudChrome.Cream, 0.9f);
            var group = background.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            Text label = HudChrome.Label(background.transform, "UpgradeStatus", new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(18f, -16f), new Vector2(424f, 100f), 28,
                HudChrome.Ink, TextAnchor.UpperLeft, true, true);
            Image progress = HudChrome.Panel(background.transform, "UpgradeProgress", Vector2.zero, Vector2.zero,
                new Vector2(18f, 16f), new Vector2(424f, 12f), new Color(0.73f, 0.53f, 1f), 0.4f);
            progress.type = Image.Type.Filled;
            progress.fillMethod = Image.FillMethod.Horizontal;
            background.gameObject.AddComponent<UpgradeHud>().Configure(upgrade, label, progress, group);
        }

        static void CreateCustomerHud(Transform canvas, CustomerQueue customers)
        {
            Text text = HudChrome.Label(canvas, "CustomerStatus", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -320f), new Vector2(1000f, 70f), 22,
                new Color(0.68f, 0.88f, 1f), TextAnchor.UpperCenter, false, true);
            text.gameObject.AddComponent<CustomerQueueHud>().Configure(customers, text);
        }

        static Material CreateLit(Color color) => RuntimeMaterials.Create(color);

        static void ApplyMaterial(GameObject instance, Material material)
        {
            var renderer = instance.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = material;
        }
    }
}
