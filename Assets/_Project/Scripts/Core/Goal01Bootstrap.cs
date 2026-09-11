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
        const float FloorSize = 20f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (Object.FindFirstObjectByType<PlayerMotor>() != null)
                return;

            Transform root = new GameObject("Goal01").transform;
            Material floorMat = CreateLit(new Color(0.76f, 0.62f, 0.42f));
            Material wallMat = CreateLit(new Color(0.45f, 0.32f, 0.18f));
            Material playerMat = CreateLit(new Color(0.89f, 0.48f, 0.16f));
            Material markerMat = CreateLit(new Color(0.22f, 0.55f, 0.38f));

            CreateFloor(root, floorMat);
            CreateWalls(root, wallMat);
            CreateMarkers(root, markerMat);
            ProductionStation station = CreateProductionStation(root);
            Transform player = CreatePlayer(root, playerMat);
            BurgerInventory inventory = player.gameObject.AddComponent<BurgerInventory>();
            BurgerPickupZone pickup = CreatePickupZone(station, inventory);
            ConfigureCamera(player);
            CustomerQueue customers = CreateCustomers(root);
            RestaurantWallet wallet = root.gameObject.AddComponent<RestaurantWallet>();
            root.gameObject.AddComponent<SaleFeedback>().Configure(wallet);
            BurgerServingZone serving = CreateServingZone(customers, inventory, wallet);
            GrillUpgradeZone upgrade = CreateUpgradeZone(root, station, inventory, wallet);
            WorkerHiringZone hiring = CreateHiringZone(root, station, serving, pickup, inventory, wallet);
            RestaurantPersistence persistence = root.gameObject.AddComponent<RestaurantPersistence>();
            persistence.Configure(wallet, upgrade, hiring);
            CreateJoystick(root, inventory, pickup, customers, wallet, upgrade, hiring);
            CreateSaveHud(Object.FindFirstObjectByType<Canvas>().transform, persistence);
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
            text.fontSize = 20;
            text.alignment = TextAnchor.LowerCenter;
            text.color = new Color(0.86f, 0.93f, 0.85f);
            text.raycastTarget = false;
            hud.AddComponent<SaveHud>().Configure(persistence, text);
        }

        static WorkerHiringZone CreateHiringZone(Transform root, ProductionStation station, BurgerServingZone serving,
            BurgerPickupZone pickup, BurgerInventory inventory, RestaurantWallet wallet)
        {
            GameObject spot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            spot.name = "StaffHiringSpot";
            spot.transform.SetParent(root, false);
            spot.transform.position = new Vector3(0f, 0.02f, -5.5f);
            spot.transform.localScale = new Vector3(2f, 0.02f, 2f);
            spot.GetComponent<Collider>().enabled = false;
            Object.Destroy(spot.GetComponent<Collider>());
            ApplyMaterial(spot, CreateLit(new Color(0.12f, 0.76f, 0.87f)));
            TextMesh marker = new GameObject("HiringMarkerLabel").AddComponent<TextMesh>();
            marker.transform.SetParent(root, false);
            marker.transform.position = spot.transform.position + Vector3.up * 1.3f;
            marker.fontSize = 36;
            marker.characterSize = 0.07f;
            marker.anchor = TextAnchor.MiddleCenter;
            marker.alignment = TextAlignment.Center;
            marker.color = new Color(0.78f, 0.96f, 1f);
            WorkerHiringZone hiring = root.gameObject.AddComponent<WorkerHiringZone>();
            hiring.Configure(station, serving, wallet, inventory, pickup.PickupPoint, spot.transform,
                new Vector3(0.9f, 0f, 0.4f), marker);
            return hiring;
        }

        static GrillUpgradeZone CreateUpgradeZone(Transform root, ProductionStation station, BurgerInventory inventory, RestaurantWallet wallet)
        {
            GameObject spot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            spot.name = "GrillUpgradeSpot";
            spot.transform.SetParent(root, false);
            spot.transform.position = new Vector3(2f, 0.02f, -2.8f);
            spot.transform.localScale = new Vector3(2f, 0.02f, 2f);
            spot.GetComponent<Collider>().enabled = false;
            Object.Destroy(spot.GetComponent<Collider>());
            ApplyMaterial(spot, CreateLit(new Color(0.60f, 0.36f, 0.90f)));

            GameObject labelObject = new GameObject("UpgradeMarkerLabel");
            labelObject.transform.SetParent(root, false);
            labelObject.transform.position = spot.transform.position + Vector3.up * 1.35f;
            TextMesh label = labelObject.AddComponent<TextMesh>();
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontSize = 36;
            label.characterSize = 0.07f;
            label.color = new Color(0.94f, 0.85f, 1f);

            Transform[] lamps = new Transform[2];
            Material lampMaterial = CreateLit(new Color(0.40f, 0.82f, 1f));
            for (int i = 0; i < lamps.Length; i++)
            {
                GameObject lamp = CreateStationPart(station.transform, $"UpgradeLamp_{i + 1}",
                    new Vector3(-1.3f + i * 0.45f, 1.3f, 0.8f), new Vector3(0.25f, 0.25f, 0.25f), lampMaterial);
                lamp.GetComponent<Collider>().enabled = false;
                Object.Destroy(lamp.GetComponent<Collider>());
                lamps[i] = lamp.transform;
            }
            GrillUpgradeZone upgrade = root.gameObject.AddComponent<GrillUpgradeZone>();
            upgrade.Configure(station, wallet, inventory, spot.transform, label, lamps);
            return upgrade;
        }

        static BurgerServingZone CreateServingZone(CustomerQueue queue, BurgerInventory inventory, RestaurantWallet wallet)
        {
            GameObject spot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            spot.name = "ServingSpot";
            spot.transform.SetParent(queue.transform, false);
            spot.transform.position = new Vector3(0.9f, 0.02f, 3.3f);
            spot.transform.localScale = new Vector3(1.7f, 0.02f, 1.7f);
            spot.GetComponent<Collider>().enabled = false;
            Object.Destroy(spot.GetComponent<Collider>());
            ApplyMaterial(spot, CreateLit(new Color(1f, 0.70f, 0.16f)));

            Vector3[] exit = { new Vector3(-4.4f, 0f, 1.7f), new Vector3(-7.8f, 0f, 1.7f), new Vector3(-7.8f, 0f, 5.8f) };
            BurgerServingZone serving = queue.gameObject.AddComponent<BurgerServingZone>();
            serving.Configure(queue, inventory, wallet, spot.transform, exit);
            GameObject exitMarker = CreateStationPart(queue.transform, "CustomerExit", exit[exit.Length - 1] + Vector3.up * 0.02f,
                new Vector3(1.4f, 0.04f, 1.4f), CreateLit(new Color(0.65f, 0.75f, 0.48f)));
            exitMarker.GetComponent<Collider>().enabled = false;
            Object.Destroy(exitMarker.GetComponent<Collider>());
            return serving;
        }

        static CustomerQueue CreateCustomers(Transform root)
        {
            Transform restaurant = new GameObject("CustomerArea").transform;
            restaurant.SetParent(root, false);
            Vector3 counterPosition = new Vector3(-2f, 0f, 3.3f);
            Material counter = CreateLit(new Color(0.38f, 0.49f, 0.58f));
            Material slotsMaterial = CreateLit(new Color(0.42f, 0.70f, 0.93f));
            Material top = CreateLit(new Color(0.90f, 0.88f, 0.78f));
            CreateStationPart(restaurant, "OrderCounter", counterPosition + Vector3.up * 0.5f, new Vector3(3.2f, 1f, 1.4f), counter);
            CreateStationPart(restaurant, "OrderCounterTop", counterPosition + Vector3.up * 1.05f, new Vector3(3.35f, 0.12f, 1.55f), top);

            Vector3 entrance = new Vector3(-8.2f, 0f, -4.4f);
            Vector3 queueEntry = new Vector3(-2f, 0f, -4.4f);
            Vector3[] slots = { new Vector3(-2f, 0f, 1.7f), new Vector3(-2f, 0f, -0.1f), new Vector3(-2f, 0f, -1.9f) };
            for (int i = 0; i < slots.Length; i++)
            {
                GameObject marker = CreateStationPart(restaurant, $"QueueSlot_{i + 1}", slots[i] + Vector3.up * 0.015f,
                    new Vector3(1.1f, 0.02f, 1.1f), slotsMaterial);
                marker.GetComponent<Collider>().enabled = false;
                Object.Destroy(marker.GetComponent<Collider>());
            }
            GameObject entranceMarker = CreateStationPart(restaurant, "CustomerEntrance", entrance + Vector3.up * 0.025f,
                new Vector3(1.4f, 0.04f, 1.4f), slotsMaterial);
            entranceMarker.GetComponent<Collider>().enabled = false;
            Object.Destroy(entranceMarker.GetComponent<Collider>());

            CustomerQueue queue = restaurant.gameObject.AddComponent<CustomerQueue>();
            queue.Configure(entrance, queueEntry, slots, counterPosition);
            return queue;
        }

        static ProductionStation CreateProductionStation(Transform root)
        {
            Transform station = new GameObject("BurgerGrill").transform;
            station.SetParent(root, false);
            station.position = new Vector3(4.5f, 0f, 2.5f);

            Material steel = CreateLit(new Color(0.24f, 0.28f, 0.31f));
            Material grill = CreateLit(new Color(0.08f, 0.09f, 0.10f));
            Material tray = CreateLit(new Color(0.63f, 0.68f, 0.70f));
            Material progress = CreateLit(new Color(0.25f, 0.87f, 0.34f));

            CreateStationPart(station, "Counter", new Vector3(0f, 0.55f, 0f), new Vector3(3.4f, 1.1f, 2.2f), steel);
            CreateStationPart(station, "GrillTop", new Vector3(-0.55f, 1.15f, 0f), new Vector3(1.8f, 0.16f, 1.7f), grill);
            CreateStationPart(station, "OutputTray", new Vector3(1.05f, 1.16f, 0f), new Vector3(0.95f, 0.12f, 1.5f), tray);

            Transform output = new GameObject("BurgerOutput").transform;
            output.SetParent(station, false);
            output.localPosition = new Vector3(1.05f, 1.27f, 0f);

            CreateStationPart(station, "ProgressBack", new Vector3(0f, 1.55f, -1.12f), new Vector3(1.5f, 0.14f, 0.08f), grill);
            GameObject fillObject = CreateStationPart(station, "ProgressFill", new Vector3(-0.75f, 1.55f, -1.17f), new Vector3(0f, 0.1f, 0.1f), progress);

            GameObject labelObject = new GameObject("GrillStatus");
            labelObject.transform.SetParent(station, false);
            labelObject.transform.localPosition = new Vector3(0f, 3.2f, 0f);
            TextMesh label = labelObject.AddComponent<TextMesh>();
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = 0.12f;
            label.fontSize = 42;
            label.color = new Color(1f, 0.92f, 0.72f);

            ProductionStation production = station.gameObject.AddComponent<ProductionStation>();
            production.Configure(output, fillObject.transform, label, 3f, 4);
            return production;
        }

        static BurgerPickupZone CreatePickupZone(ProductionStation station, BurgerInventory inventory)
        {
            GameObject spot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            spot.name = "PickupSpot";
            spot.transform.SetParent(station.transform, false);
            spot.transform.localPosition = new Vector3(1.05f, 0.015f, -2.1f);
            spot.transform.localScale = new Vector3(2f, 0.015f, 2f);
            Collider collider = spot.GetComponent<Collider>();
            collider.enabled = false;
            Object.Destroy(collider);
            ApplyMaterial(spot, CreateLit(new Color(0.24f, 0.77f, 0.46f)));

            BurgerPickupZone zone = station.gameObject.AddComponent<BurgerPickupZone>();
            zone.Configure(station, inventory, spot.transform);
            return zone;
        }

        static GameObject CreateStationPart(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            ApplyMaterial(part, material);
            return part;
        }

        static void CreateFloor(Transform root, Material material)
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(root, false);
            floor.transform.position = new Vector3(0f, -0.1f, 0f);
            floor.transform.localScale = new Vector3(FloorSize, 0.2f, FloorSize);
            ApplyMaterial(floor, material);
        }

        static void CreateWalls(Transform root, Material material)
        {
            const float half = FloorSize * 0.5f;
            const float height = 1.2f;
            CreateWall(root, "Wall+Z", new Vector3(0f, height * 0.5f, half), new Vector3(FloorSize, height, 0.4f), material);
            CreateWall(root, "Wall-Z", new Vector3(0f, height * 0.5f, -half), new Vector3(FloorSize, height, 0.4f), material);
            CreateWall(root, "Wall+X", new Vector3(half, height * 0.5f, 0f), new Vector3(0.4f, height, FloorSize), material);
            CreateWall(root, "Wall-X", new Vector3(-half, height * 0.5f, 0f), new Vector3(0.4f, height, FloorSize), material);
        }

        static void CreateWall(Transform root, string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(root, false);
            wall.transform.position = position;
            wall.transform.localScale = scale;
            ApplyMaterial(wall, material);
        }

        static void CreateMarkers(Transform root, Material material)
        {
            float inset = FloorSize * 0.5f - 2f;
            Vector3[] corners =
            {
                new Vector3(inset, 0.35f, inset),
                new Vector3(-inset, 0.35f, inset),
                new Vector3(inset, 0.35f, -inset),
                new Vector3(-inset, 0.35f, -inset)
            };

            for (int i = 0; i < corners.Length; i++)
            {
                GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                marker.name = "Landmark_" + i;
                marker.transform.SetParent(root, false);
                marker.transform.position = corners[i];
                marker.transform.localScale = new Vector3(1.2f, 0.7f, 1.2f);
                ApplyMaterial(marker, material);
            }
        }

        static Transform CreatePlayer(Transform root, Material material)
        {
            GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.tag = "Player";
            player.transform.SetParent(root, false);
            player.transform.position = new Vector3(0f, 1.05f, 0f);
            ApplyMaterial(player, material);

            Collider primitiveCollider = player.GetComponent<Collider>();
            if (primitiveCollider != null)
                Object.Destroy(primitiveCollider);

            CharacterController controller = player.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.4f;
            controller.center = new Vector3(0f, 0f, 0f);

            GameObject nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nose.name = "Facing";
            nose.transform.SetParent(player.transform, false);
            nose.transform.localPosition = new Vector3(0f, 0.35f, 0.42f);
            nose.transform.localScale = new Vector3(0.28f, 0.22f, 0.35f);
            ApplyMaterial(nose, CreateLit(new Color(0.18f, 0.18f, 0.2f)));
            Collider noseCollider = nose.GetComponent<Collider>();
            if (noseCollider != null)
                Object.Destroy(noseCollider);

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

        static void CreateJoystick(Transform root, BurgerInventory inventory, BurgerPickupZone pickup, CustomerQueue customers, RestaurantWallet wallet, GrillUpgradeZone upgrade, WorkerHiringZone hiring)
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

            Sprite circle = CreateCircleSprite();

            GameObject padObject = new GameObject("VirtualJoystick", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            padObject.transform.SetParent(canvasObject.transform, false);
            RectTransform pad = padObject.GetComponent<RectTransform>();
            pad.anchorMin = new Vector2(0f, 0f);
            pad.anchorMax = new Vector2(0f, 0f);
            pad.pivot = new Vector2(0.5f, 0.5f);
            pad.anchoredPosition = new Vector2(220f, 240f);
            pad.sizeDelta = new Vector2(280f, 280f);
            Image padImage = padObject.GetComponent<Image>();
            padImage.sprite = circle;
            padImage.color = new Color(1f, 1f, 1f, 0.22f);
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
            knobImage.color = new Color(1f, 0.92f, 0.78f, 0.9f);
            knobImage.raycastTarget = false;

            padObject.AddComponent<VirtualJoystick>();

            CreateHint(canvasObject.transform);
            CreateCarryHud(canvasObject.transform, inventory, pickup, upgrade, hiring);
            CreateCustomerHud(canvasObject.transform, customers);
            CreateSalesHud(canvasObject.transform, wallet);
            CreateUpgradeHud(canvasObject.transform, upgrade);
            CreateStaffHud(canvasObject.transform, hiring);
        }

        static void CreateStaffHud(Transform canvas, WorkerHiringZone hiring)
        {
            GameObject statusObject = new GameObject("StaffStatus", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            statusObject.transform.SetParent(canvas, false);
            RectTransform statusRect = statusObject.GetComponent<RectTransform>();
            statusRect.anchorMin = statusRect.anchorMax = statusRect.pivot = new Vector2(0.5f, 1f);
            statusRect.anchoredPosition = new Vector2(0f, -405f);
            statusRect.sizeDelta = new Vector2(1000f, 100f);
            Text status = statusObject.GetComponent<Text>();
            status.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            status.fontSize = 26;
            status.alignment = TextAnchor.UpperCenter;
            status.color = new Color(0.68f, 0.96f, 1f);
            status.raycastTarget = false;

            GameObject panelObject = new GameObject("HiringPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            panelObject.transform.SetParent(canvas, false);
            RectTransform rect = panelObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-32f, 40f);
            rect.sizeDelta = new Vector2(600f, 220f);
            Image background = panelObject.GetComponent<Image>();
            background.color = new Color(0.05f, 0.17f, 0.22f, 0.94f);
            background.raycastTarget = false;
            CanvasGroup group = panelObject.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            GameObject textObject = new GameObject("HiringStatus", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(panelObject.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = textRect.anchorMax = textRect.pivot = new Vector2(0f, 1f);
            textRect.anchoredPosition = new Vector2(20f, -20f);
            textRect.sizeDelta = new Vector2(560f, 160f);
            Text details = textObject.GetComponent<Text>();
            details.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            details.fontSize = 27;
            details.color = new Color(0.86f, 0.98f, 1f);
            details.raycastTarget = false;
            GameObject bar = new GameObject("HiringProgress", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bar.transform.SetParent(panelObject.transform, false);
            RectTransform barRect = bar.GetComponent<RectTransform>();
            barRect.anchorMin = barRect.anchorMax = barRect.pivot = Vector2.zero;
            barRect.anchoredPosition = new Vector2(20f, 20f);
            barRect.sizeDelta = new Vector2(560f, 14f);
            Image progress = bar.GetComponent<Image>();
            progress.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), Vector2.zero);
            progress.type = Image.Type.Filled;
            progress.fillMethod = Image.FillMethod.Horizontal;
            progress.color = new Color(0.23f, 0.83f, 0.94f);
            progress.raycastTarget = false;
            panelObject.AddComponent<StaffHud>().Configure(hiring, status, details, progress, group);
        }

        static void CreateUpgradeHud(Transform canvas, GrillUpgradeZone upgrade)
        {
            GameObject panelObject = new GameObject("UpgradePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            panelObject.transform.SetParent(canvas, false);
            RectTransform rect = panelObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-32f, 40f);
            rect.sizeDelta = new Vector2(600f, 220f);
            Image background = panelObject.GetComponent<Image>();
            background.color = new Color(0.16f, 0.10f, 0.24f, 0.92f);
            background.raycastTarget = false;
            CanvasGroup group = panelObject.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            GameObject textObject = new GameObject("UpgradeStatus", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(panelObject.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = textRect.anchorMax = textRect.pivot = new Vector2(0f, 1f);
            textRect.anchoredPosition = new Vector2(20f, -20f);
            textRect.sizeDelta = new Vector2(560f, 160f);
            Text label = textObject.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 27;
            label.color = new Color(0.95f, 0.91f, 1f);
            label.raycastTarget = false;

            GameObject bar = new GameObject("UpgradeProgress", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bar.transform.SetParent(panelObject.transform, false);
            RectTransform barRect = bar.GetComponent<RectTransform>();
            barRect.anchorMin = barRect.anchorMax = barRect.pivot = Vector2.zero;
            barRect.anchoredPosition = new Vector2(20f, 20f);
            barRect.sizeDelta = new Vector2(560f, 14f);
            Image progress = bar.GetComponent<Image>();
            progress.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), Vector2.zero);
            progress.type = Image.Type.Filled;
            progress.fillMethod = Image.FillMethod.Horizontal;
            progress.color = new Color(0.73f, 0.53f, 1f);
            progress.raycastTarget = false;
            panelObject.AddComponent<UpgradeHud>().Configure(upgrade, label, progress, group);
        }

        static void CreateSalesHud(Transform canvas, RestaurantWallet wallet)
        {
            GameObject hud = new GameObject("SalesStatus", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            hud.transform.SetParent(canvas, false);
            RectTransform rect = hud.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-36f, -32f);
            rect.sizeDelta = new Vector2(240f, 160f);
            Text text = hud.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 30;
            text.alignment = TextAnchor.UpperRight;
            text.color = new Color(1f, 0.83f, 0.26f);
            text.raycastTarget = false;
            hud.AddComponent<SalesHud>().Configure(wallet, text);
        }

        static void CreateCustomerHud(Transform canvas, CustomerQueue customers)
        {
            GameObject hud = new GameObject("CustomerStatus", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            hud.transform.SetParent(canvas, false);
            RectTransform rect = hud.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -280f);
            rect.sizeDelta = new Vector2(1000f, 110f);
            Text text = hud.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 28;
            text.alignment = TextAnchor.UpperCenter;
            text.color = new Color(0.68f, 0.88f, 1f);
            text.raycastTarget = false;
            hud.AddComponent<CustomerQueueHud>().Configure(customers, text);
        }

        static void CreateCarryHud(Transform canvas, BurgerInventory inventory, BurgerPickupZone pickup, GrillUpgradeZone upgrade, WorkerHiringZone hiring)
        {
            GameObject hud = new GameObject("CarryStatus", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            hud.transform.SetParent(canvas, false);
            RectTransform rect = hud.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -128f);
            rect.sizeDelta = new Vector2(1000f, 130f);
            Text text = hud.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 32;
            text.alignment = TextAnchor.UpperCenter;
            text.raycastTarget = false;
            hud.AddComponent<CarryHud>().Configure(inventory, pickup, text, upgrade, hiring);
        }

        static void CreateHint(Transform canvas)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font == null)
                return;

            GameObject hintObject = new GameObject("MoveHint", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            hintObject.transform.SetParent(canvas, false);
            RectTransform rect = hintObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -48f);
            rect.sizeDelta = new Vector2(900f, 80f);
            Text text = hintObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = 36;
            text.alignment = TextAnchor.UpperCenter;
            text.color = new Color(1f, 0.97f, 0.9f, 0.9f);
            text.raycastTarget = false;
            text.text = "WASD / joystick to move";
        }

        static Material CreateLit(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");
            var material = new Material(shader);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
            return material;
        }

        static void ApplyMaterial(GameObject instance, Material material)
        {
            var renderer = instance.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = material;
        }

        static Sprite CreateCircleSprite()
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            float center = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / center;
                    texture.SetPixel(x, y, d <= 1f ? Color.white : Color.clear);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 64f);
        }
    }
}
