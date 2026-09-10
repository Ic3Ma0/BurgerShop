using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using BurgerShop.Player;
using BurgerShop.Restaurant;
using BurgerShop.UI;

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
            CreateProductionStation(root);
            Transform player = CreatePlayer(root, playerMat);
            ConfigureCamera(player);
            CreateJoystick(root);
        }

        static void CreateProductionStation(Transform root)
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
            labelObject.transform.localPosition = new Vector3(0f, 2.25f, 0f);
            TextMesh label = labelObject.AddComponent<TextMesh>();
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = 0.12f;
            label.fontSize = 42;
            label.color = new Color(1f, 0.92f, 0.72f);

            ProductionStation production = station.gameObject.AddComponent<ProductionStation>();
            production.Configure(output, fillObject.transform, label, 3f, 4);
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

        static void CreateJoystick(Transform root)
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
