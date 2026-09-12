using UnityEngine;

namespace BurgerShop.Restaurant
{
    public sealed class BoostRoom : MonoBehaviour
    {
        public Transform BoostPoint { get; private set; }
        public TextMesh BoostLabel { get; private set; }
        public Vector3 StationPosition => ShopLayout.BoostStation;
        public Vector3 DoorPosition => ShopLayout.BoostDoor;

        public static BoostRoom Create(Transform parent, Material wall, Material floor)
        {
            GameObject root = new GameObject("BoostRoom");
            root.transform.SetParent(parent, false);
            BoostRoom room = root.AddComponent<BoostRoom>();
            room.Build(wall, floor);
            return room;
        }

        public bool Contains(Vector3 point) => ShopLayout.ContainsBoostRoom(point);

        void Build(Material wall, Material floor)
        {
            Vector3 center = ShopLayout.BoostRoomCenter;
            CreatePart("BoostFloor", PrimitiveType.Cube,
                new Vector3(center.x, -0.1f, center.z),
                new Vector3(ShopLayout.BoostRoomWidth, 0.2f, ShopLayout.BoostRoomDepth + 0.6f),
                floor, keepCollider: true);

            const float height = 1.2f;
            float farZ = -ShopLayout.WallHalf - ShopLayout.BoostRoomDepth;
            float eastX = ShopLayout.BoostDoorX + ShopLayout.BoostRoomWidth * 0.5f;
            float westX = ShopLayout.BoostDoorX - ShopLayout.BoostRoomWidth * 0.5f;
            float midZ = ( -ShopLayout.WallHalf + farZ) * 0.5f;

            CreatePart("BoostWall-Z", PrimitiveType.Cube,
                new Vector3(ShopLayout.BoostDoorX, height * 0.5f, farZ),
                new Vector3(ShopLayout.BoostRoomWidth, height, 0.4f), wall, keepCollider: true);
            CreatePart("BoostWall+X", PrimitiveType.Cube,
                new Vector3(eastX, height * 0.5f, midZ),
                new Vector3(0.4f, height, ShopLayout.BoostRoomDepth), wall, keepCollider: true);
            CreatePart("BoostWall-X", PrimitiveType.Cube,
                new Vector3(westX, height * 0.5f, midZ),
                new Vector3(0.4f, height, ShopLayout.BoostRoomDepth), wall, keepCollider: true);

            Material frame = BurgerShop.Core.RuntimeMaterials.Create(new Color(0.32f, 0.24f, 0.16f));
            CreatePart("BoostDoorFrameL", PrimitiveType.Cube,
                new Vector3(ShopLayout.BoostDoorX - ShopLayout.BoostDoorHalf, height * 0.5f, -ShopLayout.WallHalf),
                new Vector3(0.18f, height, 0.18f), frame, keepCollider: true);
            CreatePart("BoostDoorFrameR", PrimitiveType.Cube,
                new Vector3(ShopLayout.BoostDoorX + ShopLayout.BoostDoorHalf, height * 0.5f, -ShopLayout.WallHalf),
                new Vector3(0.18f, height, 0.18f), frame, keepCollider: true);

            Material steel = BurgerShop.Core.RuntimeMaterials.Create(new Color(0.38f, 0.40f, 0.44f));
            Material pad = BurgerShop.Core.RuntimeMaterials.Create(new Color(0.86f, 0.42f, 0.18f));
            Material mat = BurgerShop.Core.RuntimeMaterials.Create(new Color(0.22f, 0.24f, 0.28f));
            Vector3 station = ShopLayout.BoostStation;
            CreatePart("TrainingMat", PrimitiveType.Cube,
                station + new Vector3(0f, 0.03f, 0.85f),
                new Vector3(1.8f, 0.06f, 1.4f), mat, keepCollider: false);
            CreatePart("TrainingPad", PrimitiveType.Cube,
                station + new Vector3(0f, 0.22f, 0f),
                new Vector3(1.35f, 0.16f, 0.48f), pad, keepCollider: true);
            CreatePart("TrainingPostL", PrimitiveType.Cube,
                station + new Vector3(-0.55f, 0.7f, -0.12f),
                new Vector3(0.1f, 1.2f, 0.1f), steel, keepCollider: true);
            CreatePart("TrainingPostR", PrimitiveType.Cube,
                station + new Vector3(0.55f, 0.7f, -0.12f),
                new Vector3(0.1f, 1.2f, 0.1f), steel, keepCollider: true);
            CreatePart("TrainingBar", PrimitiveType.Cube,
                station + new Vector3(0f, 1.18f, -0.12f),
                new Vector3(1.35f, 0.08f, 0.08f), steel, keepCollider: true);

            GameObject point = new GameObject("BoostPoint");
            point.transform.SetParent(transform, false);
            point.transform.position = ShopLayout.BoostPoint;
            BoostPoint = point.transform;

            BoostLabel = new GameObject("BoostLabel").AddComponent<TextMesh>();
            BoostLabel.transform.SetParent(transform, false);
            BoostLabel.transform.position = station + new Vector3(0f, 1.7f, 0.2f);
            BoostLabel.anchor = TextAnchor.MiddleCenter;
            BoostLabel.alignment = TextAlignment.Center;
            BoostLabel.fontSize = 36;
            BoostLabel.characterSize = 0.07f;
            BoostLabel.color = new Color(0.36f, 0.22f, 0.12f);
            BoostLabel.text = "Player upgrades";

            TextMesh training = new GameObject("TrainingSign").AddComponent<TextMesh>();
            training.transform.SetParent(transform, false);
            training.transform.position = new Vector3(ShopLayout.BoostDoorX, 1.35f, farZ + 0.35f);
            training.anchor = TextAnchor.MiddleCenter;
            training.alignment = TextAlignment.Center;
            training.fontSize = 36;
            training.characterSize = 0.07f;
            training.color = new Color(0.28f, 0.18f, 0.12f);
            training.text = "Training";

            TextMesh sign = new GameObject("BoostHallSign").AddComponent<TextMesh>();
            sign.transform.SetParent(transform, false);
            sign.transform.position = new Vector3(ShopLayout.BoostDoorX, 1.45f, -ShopLayout.WallHalf + 1.7f);
            sign.anchor = TextAnchor.MiddleCenter;
            sign.alignment = TextAlignment.Center;
            sign.fontSize = 42;
            sign.characterSize = 0.08f;
            sign.color = new Color(0.28f, 0.16f, 0.10f);
            sign.text = "Boost";
        }

        GameObject CreatePart(string name, PrimitiveType type, Vector3 position, Vector3 scale,
            Material material, bool keepCollider)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(transform, false);
            part.transform.position = position;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            Collider collider = part.GetComponent<Collider>();
            SolidOccupancy.Apply(collider, keepCollider);
            return part;
        }
    }
}
