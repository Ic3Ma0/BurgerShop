using System;
using BurgerShop.Economy;
using BurgerShop.Player;
using UnityEngine;

namespace BurgerShop.Restaurant
{
    public sealed class ExpandableGrill : MonoBehaviour
    {
        public static readonly Vector3[] LookScales =
        {
            Vector3.one,
            new Vector3(1.22f, 1.22f, 1.22f),
            new Vector3(1.45f, 1.45f, 1.45f)
        };

        Transform[] looks;
        Transform output;
        TextMesh status;
        StationUpgradeFeedback feedback;
        int shown = 1;

        public ProductionStation Station { get; private set; }
        public BurgerPickupZone Pickup { get; private set; }
        public GrillUpgradeZone Upgrade { get; private set; }
        public Transform UpgradeSpot { get; private set; }
        public int VisualLevel => shown;
        public Transform ActiveLook => looks != null && shown >= 1 && shown <= looks.Length ? looks[shown - 1] : null;
        public string ActiveLookName => ActiveLook != null ? ActiveLook.name : "";
        public Vector3 ActiveLookScale => ActiveLook != null ? ActiveLook.localScale : Vector3.zero;
        public Transform OutputAnchor => output;
        public string StatusCopy => Station != null ? Station.StatusCopy : "";
        public int ActivePartCount
        {
            get
            {
                Transform look = ActiveLook;
                if (look == null) return 0;
                int count = 0;
                for (int i = 0; i < look.childCount; i++)
                    if (look.GetChild(i).gameObject.activeSelf) count++;
                return count;
            }
        }

        public void Bind(ProductionStation station, BurgerPickupZone pickup, GrillUpgradeZone upgrade,
            StationUpgradeFeedback visuals, Transform[] kits, Transform burgerOutput, TextMesh label)
        {
            Station = station;
            Pickup = pickup;
            Upgrade = upgrade;
            feedback = visuals;
            looks = kits;
            output = burgerOutput;
            status = label;
            if (feedback != null) feedback.HidePersistentMax = true;
            if (upgrade != null) upgrade.LevelApplied += HandleLevel;
            ApplyLook(upgrade != null ? upgrade.Level : 1, false);
        }

        void OnDestroy()
        {
            if (Upgrade != null) Upgrade.LevelApplied -= HandleLevel;
        }

        void HandleLevel(int level, bool animate) => ApplyLook(level, animate);

        public void ApplyLook(int level, bool animate)
        {
            shown = Mathf.Clamp(level, 1, 3);
            if (looks != null)
                for (int i = 0; i < looks.Length; i++)
                    if (looks[i] != null) looks[i].gameObject.SetActive(i == shown - 1);
            SnapOutputToTray();
            if (Station != null)
            {
                Station.AttachOutput(output);
                Station.AttachFill(ActiveLook != null ? ActiveLook.Find("ProgressFill") : null);
            }
            PlaceStatus();
            _ = animate;
        }

        public void SnapOutputToTray()
        {
            Transform tray = ActiveLook != null ? ActiveLook.Find("OutputTray") : null;
            if (output == null || tray == null) return;
            output.position = tray.TransformPoint(Vector3.up * 0.55f);
        }

        public static ExpandableGrill CreateStarter(Transform parent, BurgerInventory player, RestaurantWallet wallet)
        {
            return Create(parent, ShopLayout.Grill, player, wallet, "BurgerGrill", "PickupSpot",
                ShopLayout.UpgradeSpot, "GrillUpgradeSpot", new[] { 30, 60 }, new[] { 3f, 2f, 1.5f });
        }

        public static ExpandableGrill Create(Transform parent, Vector3 position, BurgerInventory player,
            RestaurantWallet wallet)
        {
            return Create(parent, position, player, wallet, "ExtraBurgerGrill", "ExtraPickupSpot",
                position + ShopLayout.ExtraGrillUpgradeOffset, "UpgradeSpot",
                new[] { ShopExpansion.ExtraGrillLv2Cost, ShopExpansion.ExtraGrillLv3Cost },
                new[] { 3f, 1.5f, 0.8f });
        }

        public static ExpandableGrill Create(Transform parent, Vector3 position, BurgerInventory player,
            RestaurantWallet wallet, string rootName, string pickupName, Vector3 upgradeWorld,
            string upgradeSpotName, int[] costs, float[] cookSeconds)
        {
            Transform root = new GameObject(rootName).transform;
            root.SetParent(parent, false);
            root.position = position;

            Transform[] kits =
            {
                BuildLook(root, 1, new Color(0.22f, 0.26f, 0.30f), new Color(0.08f, 0.09f, 0.10f), 1, 0, 1),
                BuildLook(root, 2, new Color(0.72f, 0.38f, 0.14f), new Color(0.95f, 0.42f, 0.10f), 2, 1, 2),
                BuildLook(root, 3, new Color(0.78f, 0.14f, 0.16f), new Color(0.82f, 0.84f, 0.88f), 3, 2, 4)
            };

            Transform output = new GameObject("BurgerOutput").transform;
            output.SetParent(root, false);
            TextMesh label = NewLabel(root, "GrillStatus", StatusLocal(1),
                new Color(1f, 0.92f, 0.72f), 36, 0.09f);
            ProductionStation station = root.gameObject.AddComponent<ProductionStation>();
            station.Configure(output, kits[0].Find("ProgressFill"), label, cookSeconds[0],
                ProductionStation.CapacityForLevel(1));

            GameObject spot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            spot.name = pickupName;
            spot.transform.SetParent(root, false);
            spot.transform.localPosition = ShopLayout.GrillPickupLocal;
            spot.transform.localScale = new Vector3(2f, 0.015f, 2f);
            Collider pickupCollider = spot.GetComponent<Collider>();
            pickupCollider.enabled = false;
            BurgerVisual.Release(pickupCollider);
            spot.GetComponent<Renderer>().sharedMaterial = BurgerShop.Core.RuntimeMaterials.Create(new Color(0.24f, 0.77f, 0.46f));
            BurgerPickupZone pickup = root.gameObject.AddComponent<BurgerPickupZone>();
            pickup.Configure(station, player, spot.transform);

            Vector3 upgradeLocal = upgradeWorld - position;
            GameObject upgradeSpot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            upgradeSpot.name = upgradeSpotName;
            upgradeSpot.transform.SetParent(root, false);
            upgradeSpot.transform.localPosition = upgradeLocal;
            upgradeSpot.transform.localScale = new Vector3(2f, 0.02f, 2f);
            Collider upgradeCollider = upgradeSpot.GetComponent<Collider>();
            upgradeCollider.enabled = false;
            BurgerVisual.Release(upgradeCollider);
            upgradeSpot.GetComponent<Renderer>().sharedMaterial =
                BurgerShop.Core.RuntimeMaterials.Create(new Color(0.60f, 0.36f, 0.90f));
            TextMesh upgradeLabel = NewLabel(root, "UpgradeMarker",
                upgradeLocal + Vector3.up * 1.35f, new Color(0.94f, 0.85f, 1f), 32, 0.06f);
            StationUpgradeFeedback visuals = StationUpgradeFeedback.Attach(root, Array.Empty<Transform>());
            visuals.HidePersistentMax = true;
            GrillUpgradeZone upgrade = root.gameObject.AddComponent<GrillUpgradeZone>();
            upgrade.Configure(station, wallet, player, upgradeSpot.transform, upgradeLabel, null, visuals,
                costs, cookSeconds);

            ExpandableGrill visual = root.gameObject.AddComponent<ExpandableGrill>();
            visual.UpgradeSpot = upgradeSpot.transform;
            visual.Bind(station, pickup, upgrade, visuals, kits, output, label);
            return visual;
        }

        static Transform BuildLook(Transform parent, int level, Color body, Color heat, int decks, int chimneys,
            int lamps)
        {
            Transform look = new GameObject("Look_Lv" + level).transform;
            look.SetParent(parent, false);
            look.localScale = LookScales[level - 1];
            look.gameObject.SetActive(level == 1);
            Material steel = BurgerShop.Core.RuntimeMaterials.Create(body);
            Material grill = BurgerShop.Core.RuntimeMaterials.Create(heat);
            Material tray = BurgerShop.Core.RuntimeMaterials.Create(new Color(0.63f, 0.68f, 0.70f));
            Material lamp = BurgerShop.Core.RuntimeMaterials.Create(new Color(0.40f, 0.82f, 1f));
            Material chrome = BurgerShop.Core.RuntimeMaterials.Create(new Color(0.82f, 0.84f, 0.88f));
            BodyMetrics(level, out float width, out float height, out float depth, out float bodyTop);
            Part(look, "Body", PrimitiveType.Cube, new Vector3(0f, 0.55f * height, 0f),
                new Vector3(width, height, depth), steel);
            for (int i = 0; i < decks; i++)
                Part(look, i == 0 ? "GrillTop" : "GrillDeck_" + (i + 1), PrimitiveType.Cube,
                    new Vector3(-0.35f, bodyTop + 0.08f + i * 0.22f, 0f),
                    new Vector3(width * 0.55f, 0.10f + i * 0.02f, depth * 0.72f), grill);
            Part(look, "OutputTray", PrimitiveType.Cube, new Vector3(width * 0.48f, bodyTop + 0.10f, 0f),
                new Vector3(0.95f, 0.12f, 1.4f), tray);
            for (int i = 0; i < chimneys; i++)
            {
                float x = chimneys == 1 ? -0.15f : (i == 0 ? -0.55f : 0.25f);
                Part(look, chimneys == 1 ? "Chimney" : (i == 0 ? "ChimneyL" : "ChimneyR"), PrimitiveType.Cylinder,
                    new Vector3(x, bodyTop + 0.85f + level * 0.08f, 0.35f),
                    new Vector3(0.28f, 0.55f + level * 0.12f, 0.28f), chrome);
            }
            for (int i = 0; i < lamps; i++)
                Part(look, "Lamp_" + (i + 1), PrimitiveType.Sphere,
                    new Vector3(-0.9f + i * 0.42f, bodyTop + 0.42f + level * 0.04f, depth * 0.38f),
                    Vector3.one * (0.16f + level * 0.03f), lamp);
            if (level == 3)
                Part(look, "Beacon", PrimitiveType.Cylinder, new Vector3(0.15f, bodyTop + 1.35f, 0f),
                    new Vector3(0.22f, 0.28f, 0.22f), BurgerShop.Core.RuntimeMaterials.Create(new Color(1f, 0.85f, 0.2f)));
            Part(look, "ProgressBack", PrimitiveType.Cube, new Vector3(0f, bodyTop + 0.55f, -depth * 0.52f),
                new Vector3(1.5f, 0.14f, 0.08f), grill);
            Part(look, "ProgressFill", PrimitiveType.Cube, new Vector3(-0.75f, bodyTop + 0.55f, -depth * 0.54f),
                new Vector3(0f, 0.1f, 0.1f), BurgerShop.Core.RuntimeMaterials.Create(new Color(0.25f, 0.87f, 0.34f)));
            return look;
        }

        public static void BodyMetrics(int level, out float width, out float height, out float depth, out float bodyTop)
        {
            int tier = Mathf.Clamp(level, 1, 3);
            width = 2.4f + (tier - 1) * 0.7f;
            height = 1.0f + (tier - 1) * 0.28f;
            depth = 1.6f + (tier - 1) * 0.35f;
            bodyTop = 1.05f * height;
        }

        void PlaceStatus()
        {
            if (status == null) return;
            status.transform.localPosition = StatusLocal(shown);
        }

        static Vector3 StatusLocal(int level)
        {
            BodyMetrics(level, out _, out float height, out _, out float bodyTop);
            float scale = LookScales[Mathf.Clamp(level, 1, 3) - 1].y;
            return new Vector3(-1.35f, bodyTop * scale + 0.72f, 0.15f);
        }

        static void Part(Transform parent, string name, PrimitiveType type, Vector3 local, Vector3 scale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = local;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            Collider collider = part.GetComponent<Collider>();
            collider.enabled = false;
            BurgerVisual.Release(collider);
        }

        static TextMesh NewLabel(Transform parent, string name, Vector3 localPosition, Color color, int fontSize,
            float characterSize)
        {
            TextMesh label = new GameObject(name).AddComponent<TextMesh>();
            label.transform.SetParent(parent, false);
            label.transform.localPosition = localPosition;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontSize = fontSize;
            label.characterSize = characterSize;
            label.color = color;
            return label;
        }
    }
}
