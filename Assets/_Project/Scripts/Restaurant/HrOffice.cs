using UnityEngine;

namespace BurgerShop.Restaurant
{
    public sealed class HrOffice : MonoBehaviour
    {
        public Transform HirePoint { get; private set; }
        public TextMesh HireLabel { get; private set; }
        public Vector3 DeskPosition => ShopLayout.HrDesk;
        public Vector3 ChairPosition => ShopLayout.HrChair;
        public Vector3 DoorPosition => ShopLayout.HrDoor;

        public static HrOffice Create(Transform parent, Material wall, Material floor)
        {
            GameObject root = new GameObject("HrOffice");
            root.transform.SetParent(parent, false);
            HrOffice office = root.AddComponent<HrOffice>();
            office.Build(wall, floor);
            office.SetOpen(true);
            return office;
        }

        public bool Contains(Vector3 point) => ShopLayout.ContainsHrOffice(point);

        public void SetOpen(bool open)
        {
            if (gameObject.activeSelf != open) gameObject.SetActive(open);
            ShopLayout.SealDoor(transform.parent, "HrDoorPlug", ShopLayout.HrDoor, ShopLayout.HrDoorPlugSize, !open);
        }

        void Build(Material wall, Material floor)
        {
            Vector3 center = ShopLayout.HrRoomCenter;
            CreatePart("HrFloor", PrimitiveType.Cube,
                new Vector3(center.x - 0.15f, -0.1f, center.z),
                new Vector3(ShopLayout.HrRoomDepth + 0.6f, 0.2f, ShopLayout.HrRoomWidth),
                floor, keepCollider: true);

            const float height = 1.2f;
            float farX = ShopLayout.WallHalf + ShopLayout.HrRoomDepth;
            float northZ = ShopLayout.HrDoorZ + ShopLayout.HrRoomWidth * 0.5f;
            float southZ = ShopLayout.HrDoorZ - ShopLayout.HrRoomWidth * 0.5f;
            float midX = (ShopLayout.WallHalf + farX) * 0.5f;

            CreatePart("HrWall+X", PrimitiveType.Cube,
                new Vector3(farX, height * 0.5f, ShopLayout.HrDoorZ),
                new Vector3(0.4f, height, ShopLayout.HrRoomWidth), wall, keepCollider: true);
            CreatePart("HrWall+Z", PrimitiveType.Cube,
                new Vector3(midX, height * 0.5f, northZ),
                new Vector3(ShopLayout.HrRoomDepth, height, 0.4f), wall, keepCollider: true);
            CreatePart("HrWall-Z", PrimitiveType.Cube,
                new Vector3(midX, height * 0.5f, southZ),
                new Vector3(ShopLayout.HrRoomDepth, height, 0.4f), wall, keepCollider: true);

            Material frame = BurgerShop.Core.RuntimeMaterials.Create(new Color(0.32f, 0.24f, 0.16f));
            CreatePart("HrDoorFrameL", PrimitiveType.Cube,
                new Vector3(ShopLayout.WallHalf, height * 0.5f, ShopLayout.HrDoorZ - ShopLayout.HrDoorHalf),
                new Vector3(0.18f, height, 0.18f), frame, keepCollider: true);
            CreatePart("HrDoorFrameR", PrimitiveType.Cube,
                new Vector3(ShopLayout.WallHalf, height * 0.5f, ShopLayout.HrDoorZ + ShopLayout.HrDoorHalf),
                new Vector3(0.18f, height, 0.18f), frame, keepCollider: true);

            Material wood = BurgerShop.Core.RuntimeMaterials.Create(new Color(0.55f, 0.38f, 0.22f));
            Material top = BurgerShop.Core.RuntimeMaterials.Create(new Color(0.72f, 0.58f, 0.38f));
            Material seat = BurgerShop.Core.RuntimeMaterials.Create(new Color(0.18f, 0.42f, 0.72f));
            Material steel = BurgerShop.Core.RuntimeMaterials.Create(new Color(0.35f, 0.38f, 0.42f));
            Material screen = BurgerShop.Core.RuntimeMaterials.Create(new Color(0.12f, 0.14f, 0.16f));
            Material shelf = BurgerShop.Core.RuntimeMaterials.Create(new Color(0.86f, 0.74f, 0.42f));
            Material cardboard = BurgerShop.Core.RuntimeMaterials.Create(new Color(0.76f, 0.58f, 0.34f));

            CreatePart("Desk", PrimitiveType.Cube,
                ShopLayout.HrDesk + new Vector3(0f, 0.72f, 0f),
                new Vector3(1.55f, 0.1f, 0.78f), top, keepCollider: true);
            CreatePart("DeskLeg", PrimitiveType.Cube,
                ShopLayout.HrDesk + new Vector3(0f, 0.34f, 0f),
                new Vector3(1.35f, 0.68f, 0.12f), wood, keepCollider: true);

            Transform chair = new GameObject("Chair").transform;
            chair.SetParent(transform, false);
            chair.position = ShopLayout.HrChair;
            Vector3 towardDesk = ShopLayout.HrDesk - ShopLayout.HrChair;
            towardDesk.y = 0f;
            chair.rotation = Quaternion.LookRotation(towardDesk);
            CreateChild(chair, "Seat", PrimitiveType.Cube, new Vector3(0f, 0.38f, 0f),
                new Vector3(0.62f, 0.12f, 0.58f), seat, true);
            CreateChild(chair, "Back", PrimitiveType.Cube, new Vector3(0f, 0.72f, -0.24f),
                new Vector3(0.62f, 0.55f, 0.1f), steel, true);
            CreateChild(chair, "PostL", PrimitiveType.Cube, new Vector3(-0.22f, 0.18f, 0.18f),
                new Vector3(0.08f, 0.36f, 0.08f), steel, true);
            CreateChild(chair, "PostR", PrimitiveType.Cube, new Vector3(0.22f, 0.18f, 0.18f),
                new Vector3(0.08f, 0.36f, 0.08f), steel, true);

            CreatePart("Computer", PrimitiveType.Cube,
                ShopLayout.HrDesk + new Vector3(0.15f, 0.86f, 0f),
                new Vector3(0.28f, 0.08f, 0.22f), screen, keepCollider: false);
            CreatePart("Screen", PrimitiveType.Cube,
                ShopLayout.HrDesk + new Vector3(0.15f, 1.12f, 0.02f),
                new Vector3(0.42f, 0.34f, 0.04f), screen, keepCollider: false);

            CreatePart("Shelf", PrimitiveType.Cube,
                new Vector3(center.x, 1.15f, northZ - 0.35f),
                new Vector3(2.2f, 1.6f, 0.28f), shelf, keepCollider: true);
            CreatePart("BoxA", PrimitiveType.Cube,
                new Vector3(center.x - 2.4f, 0.28f, southZ + 1.1f),
                new Vector3(0.55f, 0.55f, 0.55f), cardboard, keepCollider: true);
            CreatePart("BoxB", PrimitiveType.Cube,
                new Vector3(farX - 1.1f, 0.22f, southZ + 1.4f),
                new Vector3(0.7f, 0.44f, 0.5f), cardboard, keepCollider: true);

            GameObject hire = new GameObject("HrHirePoint");
            hire.transform.SetParent(transform, false);
            hire.transform.position = ShopLayout.HrHirePoint;
            HirePoint = hire.transform;

            HireLabel = new GameObject("HrHireLabel").AddComponent<TextMesh>();
            HireLabel.transform.SetParent(transform, false);
            HireLabel.transform.position = ShopLayout.HrDesk + new Vector3(0f, 1.55f, 0f);
            HireLabel.anchor = TextAnchor.MiddleCenter;
            HireLabel.alignment = TextAlignment.Center;
            HireLabel.fontSize = 36;
            HireLabel.characterSize = 0.07f;
            HireLabel.color = new Color(0.18f, 0.28f, 0.36f);
            HireLabel.text = "Hire staff\n50";

            TextMesh sign = new GameObject("HrHallSign").AddComponent<TextMesh>();
            sign.transform.SetParent(transform, false);
            sign.transform.position = new Vector3(ShopLayout.WallHalf - 1.7f, 1.45f, ShopLayout.HrDoorZ);
            sign.anchor = TextAnchor.MiddleCenter;
            sign.alignment = TextAlignment.Center;
            sign.fontSize = 42;
            sign.characterSize = 0.08f;
            sign.color = new Color(0.16f, 0.22f, 0.28f);
            sign.text = "HR";

            Material arrow = BurgerShop.Core.RuntimeMaterials.Create(new Color(0.22f, 0.28f, 0.34f));
            CreatePart("HrHallArrow", PrimitiveType.Cube,
                new Vector3(ShopLayout.WallHalf - 1.15f, 1.15f, ShopLayout.HrDoorZ),
                new Vector3(0.7f, 0.06f, 0.12f), arrow, keepCollider: false);
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

        static void CreateChild(Transform parent, string name, PrimitiveType type, Vector3 local,
            Vector3 scale, Material material, bool solid)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = local;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            SolidOccupancy.Apply(part.GetComponent<Collider>(), solid);
        }
    }
}
