using UnityEngine;

namespace BurgerShop.Restaurant
{
    /// <summary>
    /// Hall is 30×30 (walls ±15). Zones, 3-unit pitch, aisles ≥ 2.5:
    ///   +Z north
    ///   -X dining (2×2 tables) | center counters + queue | +X kitchen (two grills)
    ///   z=0 east–west aisle into HR door (x=+15)
    ///   south wall: BOX → PACK → WINDOW, road west, Boost door east
    /// </summary>
    public static class ShopLayout
    {
        public const float PreviousFloorSize = 20f;
        public const float Scale = 1.5f;
        public const float FloorSize = PreviousFloorSize * Scale;
        public const float WallHalf = FloorSize * 0.5f;
        public const float Grid = 3f;
        public const float AisleMin = 2.5f;
        public const float GrillUpgradeNorth = 2.4f;
        public static readonly Vector3 GrillPickupLocal = new Vector3(1.15f, 0.015f, -2.2f);
        public static readonly Vector3 StaffCircleOffset = new Vector3(3.6f, 0.02f, 0f);

        // Kitchen: +X, north. Trays / green pickups face the south aisle. Purple upgrades sit north of each grill.
        public static readonly Vector3 Grill = new Vector3(5f, 0f, 9f);
        public static readonly Vector3 UpgradeSpot = Grill + new Vector3(0f, 0.02f, GrillUpgradeNorth);
        public static readonly Vector3 ExtraGrill = new Vector3(12f, 0f, 9f);
        public static readonly Vector3 GrillUnlock = Pad(ExtraGrill);
        public static readonly Vector3 ExtraGrillUpgradeOffset = new Vector3(0f, 0.02f, GrillUpgradeNorth);
        public static readonly Vector3 ExtraGrillUpgrade = ExtraGrill + ExtraGrillUpgradeOffset;
        public static Vector3 GrillPickup => Grill + GrillPickupLocal;
        public static Vector3 ExtraGrillPickup => ExtraGrill + GrillPickupLocal;

        // Dine-in counters on the center axis, facing customers to the south. White circles on the kitchen (+X) staff side.
        // Second counter stacks north so the kitchen pickup lane stays clear.
        public static readonly Vector3 Counter = new Vector3(-3f, 0f, 4f);
        public static readonly Vector3 CounterTop = new Vector3(Counter.x, 1.05f, Counter.z);
        public static readonly Vector3 ServingCircle = new Vector3(0.6f, 0.02f, 4f);
        public static readonly Vector3 CounterCash = ServingCircle + CashFloor.CounterOffsetFromServing;
        public static readonly Vector3 ExtraCounter = new Vector3(-3f, 0f, 8f);
        public static readonly Vector3 CounterUnlock = Pad(ExtraCounter);
        public static readonly Vector3 ExtraCounterTop = new Vector3(ExtraCounter.x, 1.05f, ExtraCounter.z);
        public static readonly Vector3 ExtraServingCircle = new Vector3(0.6f, 0.02f, 8f);

        // Dining: −X, 2×2 grid. Extra table completes the southwest cell. Trash on the west edge.
        public static readonly Vector3[] Tables =
        {
            new Vector3(-8f, 0f, 7f),
            new Vector3(-8f, 0f, 3f),
            new Vector3(-12f, 0f, 7f)
        };
        public static readonly Vector3 ExtraTable = new Vector3(-12f, 0f, 3f);
        public static readonly Vector3 TableUnlock = Pad(ExtraTable);
        public static readonly Vector3 TrashBin = new Vector3(-13f, 0f, 0f);

        // Boxing / drive-thru: south wall. BOX → PACK → WINDOW on x = −9. Lane west; Boost door east.
        public static readonly Vector3 BoxingTable = new Vector3(-9f, 0f, -9f);
        public static readonly Vector3 BoxingUnlock = Pad(BoxingTable);
        public static readonly Vector3 BoxingCircle = new Vector3(-8f, 0.02f, -7.2f);
        public static readonly Vector3 PackageCounter = new Vector3(-9f, 0f, -12f);
        public static readonly Vector3 PackageCounterTop = new Vector3(PackageCounter.x, 1.05f, PackageCounter.z);
        public static readonly Vector3 PackageDrop = new Vector3(-10.8f, 0.02f, -12f);
        public static readonly Vector3 DriveThruWindow = new Vector3(-9f, 0f, -14.35f);
        public static readonly Vector3 DriveThruCircle = new Vector3(-9f, 0.02f, -13.05f);
        public static readonly Vector3 DriveThruUnlock = new Vector3(-6f, 0.02f, -12f);
        public static readonly Vector3 DriveThruCash = new Vector3(-7.2f, 0.04f, -14.5f);
        public static readonly Vector3 DriveThruRoad = new Vector3(-6f, 0f, -17.6f);
        public static readonly Vector3[] DriveThruQueue =
        {
            new Vector3(-9f, 0.35f, -17.55f),
            new Vector3(-5f, 0.35f, -17.55f),
            new Vector3(-1f, 0.35f, -17.55f)
        };
        public static readonly Vector3 DriveThruSpawn = new Vector3(3f, 0.35f, -17.55f);
        public static readonly Vector3 DriveThruExit = new Vector3(-15.2f, 0.35f, -17.55f);

        // HR: east room. Door on the main z = 0 aisle. Desk deep in the room.
        public const float HrDoorHalf = 1.3f;
        public const float HrDoorZ = 0f;
        public const float HrRoomWidth = 8.2f;
        public const float HrRoomDepth = 8.2f;
        public static readonly Vector3 HrDoor = new Vector3(WallHalf, 0f, HrDoorZ);
        public static readonly Vector3 HrRoomCenter = new Vector3(WallHalf + HrRoomDepth * 0.5f, 0f, HrDoorZ);
        public static readonly Vector3 HrDesk = new Vector3(21.5f, 0f, 0f);
        public static readonly Vector3 HrChair = new Vector3(20.2f, 0f, 0.95f);
        public static readonly Vector3 HrHirePoint = new Vector3(20.3f, 0.02f, 0f);

        // Boost: south room. Door shifted east so it is not in the middle of the drive-thru.
        public const float BoostDoorHalf = 1.3f;
        public const float BoostDoorX = 11f;
        public const float BoostRoomWidth = 8.2f;
        public const float BoostRoomDepth = 8.2f;
        public static readonly Vector3 BoostDoor = new Vector3(BoostDoorX, 0f, -WallHalf);
        public static readonly Vector3 BoostRoomCenter = new Vector3(BoostDoorX, 0f, -WallHalf - BoostRoomDepth * 0.5f);
        public static readonly Vector3 BoostStation = new Vector3(BoostDoorX, 0f, -20.5f);
        public static readonly Vector3 BoostPoint = new Vector3(BoostDoorX, 0.02f, -19.35f);

        // Circulation. HiringSpot is a hall point only (hire happens at the HR desk).
        public static readonly Vector3 Aisle = new Vector3(2f, 0f, 0f);
        public static readonly Vector3 HiringSpot = new Vector3(2f, 0.02f, 0f);
        public static readonly Vector3 PlayerSpawn = new Vector3(Aisle.x, 1.05f, Aisle.z);
        public static readonly Vector3 Entrance = new Vector3(-12f, 0f, -6f);
        public static readonly Vector3 QueueEntry = new Vector3(-3f, 0f, -6f);
        public static readonly Vector3[] QueueSlots =
        {
            new Vector3(-3f, 0f, 1.5f),
            new Vector3(-3f, 0f, -1.5f),
            new Vector3(-3f, 0f, -4.5f)
        };
        public static readonly Vector3[] Exit =
        {
            Entrance,
            ClampInside(new Vector3(-14f, 0f, -8f))
        };

        public static Vector3 Scaled(float x, float y, float z) => new Vector3(x * Scale, y, z * Scale);

        public static Vector3 Pad(Vector3 facility) => new Vector3(facility.x, 0.02f, facility.z);

        public static float Horizontal(Vector3 a, Vector3 b)
        {
            a.y = b.y = 0f;
            return Vector3.Distance(a, b);
        }

        public static Vector3 StaffEntry(int slot)
        {
            float z = HrDoorZ + (Mathf.Clamp(slot, 0, 2) - 1) * 0.85f;
            return new Vector3(WallHalf - 1.5f, 0f, z);
        }

        public static bool ContainsPlayable(Vector3 point) =>
            ContainsHall(point) || ContainsHrOffice(point) || ContainsBoostRoom(point) || (BagLine.Current!=null && BagLine.Current.Expanded && point.x>=-26.5f && point.x<=-14.5f && Mathf.Abs(point.z)<=8.5f);

        public static bool ContainsHall(Vector3 point)
        {
            float limit = WallHalf + 0.2f;
            return Mathf.Abs(point.x) <= limit && Mathf.Abs(point.z) <= limit;
        }

        public static Vector3 ClampPlayable(Vector3 point)
        {
            if (ContainsPlayable(point)) return point;
            if (point.x > WallHalf && Mathf.Abs(point.z - HrDoorZ) <= HrRoomWidth * 0.5f)
            {
                point.x = Mathf.Clamp(point.x, WallHalf + 0.2f, WallHalf + HrRoomDepth - 0.2f);
                point.z = Mathf.Clamp(point.z, HrDoorZ - HrRoomWidth * 0.5f + 0.2f,
                    HrDoorZ + HrRoomWidth * 0.5f - 0.2f);
                return point;
            }
            if (point.z < -WallHalf && Mathf.Abs(point.x - BoostDoorX) <= BoostRoomWidth * 0.5f)
            {
                point.x = Mathf.Clamp(point.x, BoostDoorX - BoostRoomWidth * 0.5f + 0.2f,
                    BoostDoorX + BoostRoomWidth * 0.5f - 0.2f);
                point.z = Mathf.Clamp(point.z, -WallHalf - BoostRoomDepth + 0.2f, -WallHalf - 0.2f);
                return point;
            }
            float hall = WallHalf - 0.6f;
            point.x = Mathf.Clamp(point.x, -hall, hall);
            point.z = Mathf.Clamp(point.z, -hall, hall);
            return point;
        }

        public static GameObject CreateFloor(Transform parent, Material material)
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(parent, false);
            floor.transform.position = new Vector3(0f, -0.1f, 0f);
            floor.transform.localScale = new Vector3(FloorSize, 0.2f, FloorSize);
            Apply(floor, material);
            return floor;
        }

        public static void CreateWalls(Transform parent, Material material)
        {
            const float height = 1.2f;
            CreateWall(parent, "Wall+Z", new Vector3(0f, height * 0.5f, WallHalf), new Vector3(FloorSize, height, 0.4f), material);
            CreateWall(parent, "Wall-X", new Vector3(-WallHalf, height * 0.5f, 0f), new Vector3(0.4f, height, FloorSize), material);
            SplitEastWall(parent, material, height);
            SplitSouthWall(parent, material, height);
        }

        public static bool ContainsHrOffice(Vector3 point)
        {
            float minX = WallHalf + 0.1f;
            float maxX = WallHalf + HrRoomDepth - 0.1f;
            float minZ = HrDoorZ - HrRoomWidth * 0.5f + 0.1f;
            float maxZ = HrDoorZ + HrRoomWidth * 0.5f - 0.1f;
            return point.x >= minX && point.x <= maxX && point.z >= minZ && point.z <= maxZ;
        }

        public static bool ContainsHrDoorway(Vector3 point) =>
            Mathf.Abs(point.x - WallHalf) <= 0.55f && Mathf.Abs(point.z - HrDoorZ) <= HrDoorHalf;

        public static bool ContainsHrUpgradeRange(Vector3 point) =>
            ContainsHrOffice(point) || ContainsHrDoorway(point);

        public static bool ContainsBoostRoom(Vector3 point)
        {
            float minX = BoostDoorX - BoostRoomWidth * 0.5f + 0.1f;
            float maxX = BoostDoorX + BoostRoomWidth * 0.5f - 0.1f;
            float maxZ = -WallHalf - 0.1f;
            float minZ = -WallHalf - BoostRoomDepth + 0.1f;
            return point.x >= minX && point.x <= maxX && point.z >= minZ && point.z <= maxZ;
        }

        public static bool ContainsBoostDoorway(Vector3 point) =>
            Mathf.Abs(point.z + WallHalf) <= 0.55f && Mathf.Abs(point.x - BoostDoorX) <= BoostDoorHalf;

        public static bool ContainsBoostUpgradeRange(Vector3 point) =>
            ContainsBoostRoom(point) || ContainsBoostDoorway(point);

        static void SplitEastWall(Transform parent, Material material, float height)
        {
            float doorMin = HrDoorZ - HrDoorHalf;
            float doorMax = HrDoorZ + HrDoorHalf;
            float southLength = doorMin - (-WallHalf);
            float northLength = WallHalf - doorMax;
            float southCenterZ = (-WallHalf + doorMin) * 0.5f;
            float northCenterZ = (doorMax + WallHalf) * 0.5f;
            CreateWall(parent, "Wall+X_S", new Vector3(WallHalf, height * 0.5f, southCenterZ),
                new Vector3(0.4f, height, southLength), material);
            CreateWall(parent, "Wall+X_N", new Vector3(WallHalf, height * 0.5f, northCenterZ),
                new Vector3(0.4f, height, northLength), material);
        }

        static void SplitSouthWall(Transform parent, Material material, float height)
        {
            float doorMin = BoostDoorX - BoostDoorHalf;
            float doorMax = BoostDoorX + BoostDoorHalf;
            float westLength = doorMin - (-WallHalf);
            float eastLength = WallHalf - doorMax;
            float westCenterX = (-WallHalf + doorMin) * 0.5f;
            float eastCenterX = (doorMax + WallHalf) * 0.5f;
            CreateWall(parent, "Wall-Z_W", new Vector3(westCenterX, height * 0.5f, -WallHalf),
                new Vector3(westLength, height, 0.4f), material);
            CreateWall(parent, "Wall-Z_E", new Vector3(eastCenterX, height * 0.5f, -WallHalf),
                new Vector3(eastLength, height, 0.4f), material);
        }

        static Vector3 ClampInside(Vector3 point)
        {
            float limit = WallHalf - 0.6f;
            point.x = Mathf.Clamp(point.x, -limit, limit);
            point.z = Mathf.Clamp(point.z, -limit, limit);
            return point;
        }

        static void CreateWall(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent, false);
            wall.transform.position = position;
            wall.transform.localScale = scale;
            Apply(wall, material);
        }

        static void Apply(GameObject instance, Material material)
        {
            Renderer renderer = instance.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;
        }
    }
}
