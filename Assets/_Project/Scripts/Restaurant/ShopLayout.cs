using UnityEngine;

namespace BurgerShop.Restaurant
{
    /// <summary>
    /// Hall is 30×30 (walls ±15). Zones, 3-unit pitch, aisles ≥ 2.5:
    ///   +Z north
    ///   -X dining (2×2 tables) | center counters + queue | +X kitchen (two grills)
    ///   z=0 east–west aisle into HR door (x=+15)
    ///   south wall: BOX → PACK → WINDOW, road west, Boost door east
    /// Side wing (BS-SPEC-033) enters south of HR then east behind it; locked until purchased.
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

        public static bool CompactStart {get;set;}
        public static bool SmallFootprint {get;set;}
        public static bool WingUnlocked { get; private set; }

        public static void ResetWingLock() => WingUnlocked = false;

        // Kitchen: +X, north. Trays / green pickups face the south aisle. Purple upgrades sit north of each grill.
        public static Vector3 Grill => SmallFootprint ? new Vector3(1,0,-3.5f) : CompactStart ? new Vector3(4,0,1.5f) : new Vector3(5f,0f,9f);
        public static Vector3 UpgradeSpot => Grill + new Vector3(0f, 0.02f, GrillUpgradeNorth);
        public static readonly Vector3 ExtraGrill = new Vector3(12f, 0f, 9f);
        public static readonly Vector3 GrillUnlock = Pad(ExtraGrill);
        public static readonly Vector3 ExtraGrillUpgradeOffset = new Vector3(0f, 0.02f, GrillUpgradeNorth);
        public static readonly Vector3 ExtraGrillUpgrade = ExtraGrill + ExtraGrillUpgradeOffset;
        public static Vector3 GrillPickup => Grill + GrillPickupLocal;
        public static Vector3 ExtraGrillPickup => ExtraGrill + GrillPickupLocal;

        // Cola: back wing east of HR (not the old kitchen slot).
        public static readonly Vector3 Cola = new Vector3(34f, 0f, 7f);
        public static readonly Vector3 ColaUpgrade = Cola + new Vector3(0f, 0.02f, GrillUpgradeNorth);
        public static Vector3 ColaPickup => Cola + GrillPickupLocal;
        public static readonly Vector3 ColaCounter = new Vector3(28f, 0f, 5f);
        public static readonly Vector3 ColaCounterTop = new Vector3(ColaCounter.x, 1.05f, ColaCounter.z);
        public static readonly Vector3 ColaServingCircle = ColaCounter + StaffCircleOffset;
        public static readonly Vector3 ColaCash = ColaServingCircle + CashFloor.CounterOffsetFromServing;
        public static readonly Vector3 ColaQueueEntry = new Vector3(28f, 0f, -4f);
        public static readonly Vector3[] ColaQueueSlots =
        {
            new Vector3(28f, 0f, 2.5f),
            new Vector3(28f, 0f, 0f),
            new Vector3(28f, 0f, -2.5f)
        };

        // Dine-in counters on the center axis, facing customers to the south. White circles on the kitchen (+X) staff side.
        // Second counter stacks north so the kitchen pickup lane stays clear.
        public static Vector3 Counter => SmallFootprint ? new Vector3(-5,0,-3) : new Vector3(-3,0,CompactStart?0:4);
        public static Vector3 CounterTop => new Vector3(Counter.x, 1.05f, Counter.z);
        public static Vector3 ServingCircle => Counter + StaffCircleOffset;
        public static Vector3 CounterCash => ServingCircle + CashFloor.CounterOffsetFromServing;
        public static readonly Vector3 ExtraCounter = new Vector3(-3f, 0f, 8f);
        public static readonly Vector3 CounterUnlock = Pad(ExtraCounter);
        public static readonly Vector3 ExtraCounterTop = new Vector3(ExtraCounter.x, 1.05f, ExtraCounter.z);
        public static readonly Vector3 ExtraServingCircle = new Vector3(0.6f, 0.02f, 8f);

        // Dining: −X, two Rank-2 starter pair tables (BS-SPEC-069). Extra / 031 pads stay in the back wing.
        public static Vector3[] Tables => SmallFootprint ? new[]{new Vector3(-11,0,-3),new Vector3(-11,0,-7.5f)} : CompactStart ? new[]{new Vector3(-8,0,2),new Vector3(-8,0,-2)} : new[]{new Vector3(-8,0,7),new Vector3(-8,0,3)};
        public static readonly Vector3 ExtraTable = new Vector3(26f, 0f, -6f);
        public static readonly Vector3 TableUnlock = Pad(ExtraTable);
        public static readonly Vector3 FourSeatTable = new Vector3(30.5f, 0f, -6f);
        public static readonly Vector3 FourSeatUnlock = Pad(FourSeatTable);
        public static readonly Vector3 SquareTable = new Vector3(35f, 0f, -6f);
        public static readonly Vector3 SquareUnlock = Pad(SquareTable);
        public static readonly Vector3 TableUpgradeOffset = new Vector3(1.55f, 0.02f, 0f);
        public static Vector3 TableUpgradePad(Vector3 table) => table + TableUpgradeOffset;
        public static Vector3 TrashBin => SmallFootprint ? new Vector3(-13,0,-10) : new Vector3(-13f, 0f, 0f);

        // Boxing / drive-thru: south wall. BOX → PACK → WINDOW. Cars run on
        // StreetEnvironment.SouthStreet (z = −30, 8 m wide), just outside the window.
        public static readonly Vector3 BoxingTable = new Vector3(11f, 0f, -19f);
        public static readonly Vector3 BoxingUnlock = Pad(BoxingTable);
        public static readonly Vector3 BoxingCircle = new Vector3(11f, 0.02f, -17f);
        public static readonly Vector3 PackageCounter = new Vector3(11f, 0f, -26.3f);
        public static readonly Vector3 PackageCounterTop = new Vector3(PackageCounter.x, 1.05f, PackageCounter.z);
        public static readonly Vector3 PackageDrop = new Vector3(9f, 0.02f, -25f);
        public static readonly Vector3 DriveThruWindow = new Vector3(11f, 0f, -26.3f);
        public static readonly Vector3 DriveThruCircle = new Vector3(11f, 0.02f, -25f);
        public static readonly Vector3 DriveThruUnlock = new Vector3(13f, 0.02f, -24f);
        public static readonly Vector3 DriveThruCash = new Vector3(13f, 0.04f, -26f);
        public static readonly Vector3 DriveThruRoad = new Vector3(14f, 0f, -29f);
        public static readonly Vector3[] DriveThruQueue =
        {
            new Vector3(11f, 0.35f, -29f),
            new Vector3(15f, 0.35f, -29f),
            new Vector3(19f, 0.35f, -29f)
        };
        public static readonly Vector3 DriveThruSpawn = new Vector3(27f, 0.35f, -29f);
        public static readonly Vector3 DriveThruExit = new Vector3(-35f, 0.35f, -29f);

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

        // Side wing: entrance south of HR, corridor south of HR, hall behind HR.
        public const float SideDoorHalf = 1.5f;
        public const float SideDoorZ = -6.5f;
        public static readonly Vector3 SideDoor = new Vector3(WallHalf, 0f, SideDoorZ);
        public static readonly Vector3 WingUnlock = new Vector3(12f, 0.02f, SideDoorZ);
        public const float WingCorridorMinX = 15.2f;
        public const float WingCorridorMaxX = 23.4f;
        public const float WingCorridorMinZ = -8f;
        public const float WingCorridorMaxZ = -4.3f;
        public const float WingBackMinX = 23.4f;
        public const float WingBackMaxX = 38f;
        public const float WingBackMinZ = -8f;
        public const float WingBackMaxZ = 11.5f;

        // Boost: south room. Door shifted east so it is not in the middle of the drive-thru.
        public const float BoostDoorHalf = 1.3f;
        public const float BoostDoorX = 11f;
        public const float BoostRoomWidth = 8.2f;
        public const float BoostRoomDepth = 13f;
        public static readonly Vector3 BoostDoor = new Vector3(BoostDoorX, 0f, -WallHalf);
        public static readonly Vector3 BoostRoomCenter = new Vector3(BoostDoorX, 0f, -WallHalf - BoostRoomDepth * 0.5f);
        public static Vector3 BoostStation => SmallFootprint?new Vector3(2,0,-11):CompactStart?new Vector3(5,0,-9):new Vector3(13,0,-11);
        public static Vector3 BoostPoint => BoostStation+Vector3.up*.02f;

        // Circulation. HiringSpot is a hall point only (hire happens at the HR desk).
        public static Vector3 Aisle => SmallFootprint ? new Vector3(-.5f,0,-8) : new Vector3(2f, 0f, CompactStart ? -3.5f : 0f);
        public static readonly Vector3 HiringSpot = new Vector3(2f, 0.02f, 0f);
        public static Vector3 PlayerSpawn => new Vector3(Aisle.x, 1.05f, Aisle.z);
        public static Vector3 Entrance => new Vector3(-12f, 0f, SmallFootprint ? -12f : -6f);
        public static Vector3 QueueEntry => SmallFootprint ? new Vector3(-5,0,-12f) : new Vector3(-3,0,CompactStart?-10.5f:-6);
        public static Vector3[] QueueSlots => new[]{new Vector3(Counter.x,0,Counter.z-2.5f),new Vector3(Counter.x,0,Counter.z-5.5f),new Vector3(Counter.x,0,Counter.z-8.5f)};
        public static Vector3[] Exit => new[]
        {
            Entrance,
            BurgerShop.Core.RestaurantEntrance.Door,
            BurgerShop.Core.RestaurantEntrance.Corner,
            BurgerShop.Core.RestaurantEntrance.Outside
        };

        public const float HrPlugHeight = 1.2f;
        public static readonly Vector3 HrDoorPlugSize = new Vector3(0.4f, HrPlugHeight, HrDoorHalf * 2f);
        public static readonly Vector3 BoostDoorPlugSize = new Vector3(BoostDoorHalf * 2f, HrPlugHeight, 0.4f);

        // Route cross-wing traffic through the south HR corridor, never through the office.
        public static Vector3[] WingRoute(Vector3 from, Vector3 to)
        {
            var points = new System.Collections.Generic.List<Vector3>();
            bool source = from.x > WallHalf, destination = to.x > WallHalf;
            if (source != destination && WingUnlocked)
            {
                Vector3[] crossing = { new Vector3(2,0,0), new Vector3(14,0,0),
                    new Vector3(14,0,SideDoorZ), new Vector3(24.3f,0,SideDoorZ), new Vector3(24.3f,0,0) };
                if (source) System.Array.Reverse(crossing);
                points.AddRange(crossing);
            }
            points.Add(to);
            return points.ToArray();

        }

        public static bool IsSouthOfShop(Vector3 point) => point.z < -WallHalf + 0.4f;

        // One indoor router for customers: always a walkable path, never null-as-freeze.
        public static Vector3[] Walk(Vector3 from, Vector3 to)
        {
            var layout = BurgerShop.Building.FacilityLayout.Current;
            if (layout != null)
                return layout.Route(from,to) ?? System.Array.Empty<Vector3>();
            var authored=IndoorRoute(from,to);
            var cursor=from;
            foreach(var p in authored){if(!ActorObstacles.Clear(cursor,p))return ActorObstacles.Route(from,to)??System.Array.Empty<Vector3>();cursor=p;}
            return authored;
        }

        // Customers never walk a straight line through the south wall: they use the 045/046 door.
        public static Vector3[] IndoorRoute(Vector3 from, Vector3 to)
        {
            var points = new System.Collections.Generic.List<Vector3>();
            Vector3 cursor = from;
            bool fromStreet = IsSouthOfShop(from);
            bool toStreet = IsSouthOfShop(to);
            if (fromStreet && !toStreet)
            {
                if (from.x < BurgerShop.Core.RestaurantEntrance.Door.x - 2f)
                    Append(points, ref cursor, BurgerShop.Core.RestaurantEntrance.Corner);
                Append(points, ref cursor, BurgerShop.Core.RestaurantEntrance.Door);
                Append(points, ref cursor, Entrance);
            }
            else if (!fromStreet && toStreet)
            {
                Append(points, ref cursor, Entrance);
                Append(points, ref cursor, BurgerShop.Core.RestaurantEntrance.Door);
                Append(points, ref cursor, BurgerShop.Core.RestaurantEntrance.Corner);
            }
            // After the door, wrap the counter on z=0. Do this even when the walk
            // started on the street — the remaining indoor leg must not clip the desk.
            if (!IsSouthOfShop(cursor) && !toStreet && CrossesCounter(cursor, to))
            {
                float aisleZ = Aisle.z;
                Append(points, ref cursor, new Vector3(cursor.x, cursor.y, aisleZ));
                Append(points, ref cursor, new Vector3(to.x, cursor.y, aisleZ));
            }
            foreach (var p in WingRoute(cursor, to))
                Append(points, ref cursor, p);
            StripLongRetrace(points);
            if (points.Count == 0) points.Add(to);
            return points.ToArray();
        }

        // Arrival (outside→door→Entrance) plus an indoor Walk that also started
        // on the street would retrace the door corridor. Skip that overlapping prefix.
        public static void ConcatWalk(System.Collections.Generic.List<Vector3> points, Vector3[] extra)
        {
            if (points == null || extra == null) return;
            for (int i = 0; i < extra.Length; i++)
            {
                if (points.Count == 0)
                {
                    points.Add(extra[i]);
                    continue;
                }
                Vector3 p = extra[i];
                if (IsEntryApproach(p) && ContainsNear(points, p, 0.35f)) continue;
                Vector3 cursor = points[points.Count - 1];
                Append(points, ref cursor, p);
            }
        }

        static bool IsEntryApproach(Vector3 p) =>
            IsSouthOfShop(p)
            || Horizontal(p, BurgerShop.Core.RestaurantEntrance.Outside) < 0.4f
            || Horizontal(p, BurgerShop.Core.RestaurantEntrance.Corner) < 0.4f
            || Horizontal(p, BurgerShop.Core.RestaurantEntrance.Door) < 0.4f
            || Horizontal(p, Entrance) < 0.4f;

        static bool ContainsNear(System.Collections.Generic.List<Vector3> points, Vector3 p, float radius)
        {
            for (int i = 0; i < points.Count; i++)
                if (Horizontal(points[i], p) <= radius) return true;
            return false;
        }

        static Vector3[] CardinalizeFrom(Vector3 from, Vector3[] route)
        {
            if (route == null || route.Length == 0) return route;
            var points = new System.Collections.Generic.List<Vector3>();
            Vector3 cursor = from;
            for (int i = 0; i < route.Length; i++)
                Append(points, ref cursor, route[i]);
            StripLongRetrace(points);
            if (points.Count == 0)
                points.Add(new Vector3(route[route.Length - 1].x, from.y, route[route.Length - 1].z));
            return points.ToArray();
        }

        static void Append(System.Collections.Generic.List<Vector3> points, ref Vector3 cursor, Vector3 next)
        {
            next.y = cursor.y;
            const float eps = 0.05f;
            for (int guard = 0; guard < 4; guard++)
            {
                Vector3 delta = next - cursor;
                delta.y = 0f;
                if (delta.sqrMagnitude < 0.0025f) return;
                bool xMove = Mathf.Abs(delta.x) >= eps;
                bool zMove = Mathf.Abs(delta.z) >= eps;
                if (!xMove || !zMove)
                {
                    points.Add(next);
                    cursor = next;
                    return;
                }
                Vector3 elbow = Elbow(cursor, next);
                if ((elbow - cursor).sqrMagnitude < 0.0025f || Horizontal(elbow, next) < eps)
                    elbow = new Vector3(next.x, cursor.y, cursor.z);
                if ((elbow - cursor).sqrMagnitude < 0.0025f)
                    elbow = new Vector3(cursor.x, cursor.y, next.z);
                points.Add(elbow);
                cursor = elbow;
            }
            points.Add(next);
            cursor = next;
        }

        // Prefer the door axis then a 90° turn into the aisle — never a hypotenuse.
        static Vector3 Elbow(Vector3 from, Vector3 to)
        {
            Vector3 xFirst = new Vector3(to.x, from.y, from.z);
            Vector3 zFirst = new Vector3(from.x, from.y, to.z);
            bool xHit = CrossesCounter(from, xFirst) || CrossesCounter(xFirst, to);
            bool zHit = CrossesCounter(from, zFirst) || CrossesCounter(zFirst, to);
            if (xHit && !zHit) return zFirst;
            if (zHit && !xHit) return xFirst;
            if (xHit && zHit) return new Vector3(from.x, from.y, Aisle.z);
            float doorX = BurgerShop.Core.RestaurantEntrance.Door.x;
            if (Mathf.Abs(from.x - doorX) <= 2.5f && Mathf.Abs(to.x - doorX) > 2.5f)
                return zFirst;
            if (Mathf.Abs(to.x - doorX) <= 2.5f && Mathf.Abs(from.x - doorX) > 2.5f)
                return xFirst;
            if (Mathf.Abs(from.z - Aisle.z) <= 0.2f) return xFirst;
            return xFirst;
        }

        static void StripLongRetrace(System.Collections.Generic.List<Vector3> points)
        {
            const float eps = 0.08f;
            const float minReverse = 0.75f;
            bool changed = true;
            while (changed && points.Count >= 3)
            {
                changed = false;
                for (int i = 0; i < points.Count - 2; i++)
                {
                    Vector3 a = points[i], b = points[i + 1], c = points[i + 2];
                    bool sameZ = Mathf.Abs(a.z - b.z) < eps && Mathf.Abs(b.z - c.z) < eps;
                    bool sameX = Mathf.Abs(a.x - b.x) < eps && Mathf.Abs(b.x - c.x) < eps;
                    float ab = sameZ ? b.x - a.x : sameX ? b.z - a.z : 0f;
                    float bc = sameZ ? c.x - b.x : sameX ? c.z - b.z : 0f;
                    if ((sameX || sameZ) && ab * bc < 0f && Mathf.Abs(ab) > minReverse && Mathf.Abs(bc) > minReverse)
                    {
                        points.RemoveAt(i + 1);
                        changed = true;
                        break;
                    }
                }
            }
        }

        static bool CrossesCounter(Vector3 from, Vector3 to)
        {
            return SegmentHitsRect(from, to, Counter.x-1.8f, Counter.x+1.8f, Counter.z-.8f, Counter.z+.8f)
                || SegmentHitsRect(from, to, -4.8f, -1.2f, 7.2f, 8.8f);
        }

        static bool SegmentHitsRect(Vector3 a, Vector3 b, float x0, float x1, float z0, float z1)
        {
            for (int i = 1; i <= 8; i++)
            {
                float t = i / 9f;
                float x = a.x + (b.x - a.x) * t;
                float z = a.z + (b.z - a.z) * t;
                if (x > x0 && x < x1 && z > z0 && z < z1) return true;
            }
            return false;
        }

        static bool UsableWalk(Vector3[] route, Vector3 from, Vector3 to)
        {
            if (route == null || route.Length == 0) return false;
            Vector3 last = route[route.Length - 1];
            last.y = from.y;
            Vector3 dest = to;
            dest.y = from.y;
            if ((last - dest).sqrMagnitude > 0.36f) return false;
            bool indoor = !IsSouthOfShop(from) && !IsSouthOfShop(to);
            for (int i = 0; i < route.Length; i++)
            {
                Vector3 p = route[i];
                if (indoor && IsSouthOfShop(p) && Horizontal(p, BurgerShop.Core.RestaurantEntrance.Door) > 1.2f)
                    return false;
                if (Horizontal(p, BurgerShop.Core.RestaurantEntrance.Door) < 1.2f) continue;
                if (OccupiesWall(p)) return false;
            }
            return !HasLongRetrace(from, route);
        }

        static bool HasLongRetrace(Vector3 from, Vector3[] route)
        {
            if (route == null || route.Length == 0) return false;
            var points = new System.Collections.Generic.List<Vector3>(route.Length + 1) { from };
            points.AddRange(route);
            const float eps = 0.08f;
            const float minReverse = 0.75f;
            for (int i = 0; i < points.Count - 2; i++)
            {
                Vector3 a = points[i], b = points[i + 1], c = points[i + 2];
                bool sameZ = Mathf.Abs(a.z - b.z) < eps && Mathf.Abs(b.z - c.z) < eps;
                bool sameX = Mathf.Abs(a.x - b.x) < eps && Mathf.Abs(b.x - c.x) < eps;
                float ab = sameZ ? b.x - a.x : sameX ? b.z - a.z : 0f;
                float bc = sameZ ? c.x - b.x : sameX ? c.z - b.z : 0f;
                if ((sameX || sameZ) && ab * bc < 0f && Mathf.Abs(ab) > minReverse && Mathf.Abs(bc) > minReverse)
                    return true;
            }
            return false;
        }

        public static bool OccupiesWall(Vector3 point, float radius = 0.32f)
        {
            Physics.SyncTransforms();
            Collider[] hits = Physics.OverlapBox(point + Vector3.up * 0.6f, new Vector3(radius, 0.45f, radius));
            for (int i = 0; i < hits.Length; i++)
            {
                Collider collider = hits[i];
                if (!SolidOccupancy.BlocksPlayer(collider)) continue;
                string name = collider.name;
                if (name.StartsWith("Wall") || name.StartsWith("WingWall") || name.StartsWith("HrWall")
                    || name.Contains("DoorPlug") || name == "EntranceWallLeft")
                    return true;
            }
            return false;
        }

        public static void SealDoor(Transform parent, string name, Vector3 door, Vector3 scale, bool sealedShut)
        {
            if (parent == null || string.IsNullOrEmpty(name)) return;
            Transform plug = parent.Find(name);
            if (sealedShut)
            {
                if (plug != null) return;
                Transform sample = parent.Find("Wall+Z") ?? parent.Find("Wall-X") ?? parent.Find("Wall-Z_W");
                Material material = sample != null
                    ? sample.GetComponent<Renderer>().sharedMaterial
                    : BurgerShop.Core.RuntimeMaterials.Create(new Color(0.40f, 0.29f, 0.17f));
                CreateWall(parent, name, door + Vector3.up * (scale.y * 0.5f), scale, material);
                if(!MainHallExpansion.HasAccess)parent.Find(name).gameObject.SetActive(false);
                return;
            }
            if (plug == null) return;
            // Play-mode Destroy is deferred: Create() opens then bootstrap closes in the same
            // frame, so the queued destroy would leave a permanent hole. Always tear down now.
            Object.DestroyImmediate(plug.gameObject);
        }

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
            (MainHallExpansion.HasAccess && CourierLine.Current!=null && Mathf.Abs(point.x)<=14.5f && point.z>=14.5f && point.z<=25.5f) || ContainsHall(point) || (MainHallExpansion.HasAccess && (ContainsHrOffice(point) || ContainsBoostRoom(point))) || (BagLine.Current!=null && BagLine.Current.Expanded && point.x>=-26.5f && point.x<=-14.5f && Mathf.Abs(point.z)<=8.5f)
            || (WingUnlocked && (ContainsWing(point) || ContainsSideDoorway(point)));

        public static bool ContainsHall(Vector3 point)
        {
            float limit = WallHalf + 0.2f;
            return MainHallExpansion.Current!=null?MainHallExpansion.Current.Bounds.Contains(new Vector2(point.x,point.z)):Mathf.Abs(point.x) <= limit && Mathf.Abs(point.z) <= limit;
        }

        public static bool ContainsWing(Vector3 point)
        {
            bool corridor = point.x >= WingCorridorMinX - 0.05f && point.x <= WingCorridorMaxX + 0.05f
                && point.z >= WingCorridorMinZ - 0.05f && point.z <= WingCorridorMaxZ + 0.05f;
            bool back = point.x >= WingBackMinX - 0.05f && point.x <= WingBackMaxX + 0.05f
                && point.z >= WingBackMinZ - 0.05f && point.z <= WingBackMaxZ + 0.05f;
            return corridor || back;
        }

        public static bool ContainsSideDoorway(Vector3 point) =>
            Mathf.Abs(point.x - WallHalf) <= 0.55f && Mathf.Abs(point.z - SideDoorZ) <= SideDoorHalf;

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
            if (WingUnlocked && point.x > WallHalf)
            {
                point.x = Mathf.Clamp(point.x, WallHalf + 0.2f, WingBackMaxX - 0.2f);
                point.z = Mathf.Clamp(point.z, WingBackMinZ + 0.2f, Mathf.Max(WingBackMaxZ, WingCorridorMaxZ) - 0.2f);
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
            SolidOccupancy.Apply(floor.GetComponent<Collider>(), true);
            return floor;
        }

        public static void CreateWalls(Transform parent, Material material)
        {
            WingUnlocked = false;
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
            Horizontal(point,BoostPoint)<=1.5f;

        public static void OpenWing(Transform parent, Material floorMaterial, Material wallMaterial)
        {
            if (WingUnlocked) return;
            WingUnlocked = true;
            Transform plug = parent != null ? parent.Find("WingDoorPlug") : null;
            if (plug != null) Object.DestroyImmediate(plug.gameObject);

            const float height = 1.2f;
            float corridorX = (WingCorridorMinX + WingCorridorMaxX) * 0.5f;
            float corridorZ = (WingCorridorMinZ + WingCorridorMaxZ) * 0.5f;
            CreateFloorSlab(parent, "WingCorridorFloor",
                new Vector3(corridorX, -0.1f, corridorZ),
                new Vector3(WingCorridorMaxX - WingCorridorMinX, 0.2f, WingCorridorMaxZ - WingCorridorMinZ),
                floorMaterial);
            float backX = (WingBackMinX + WingBackMaxX) * 0.5f;
            float backZ = (WingBackMinZ + WingBackMaxZ) * 0.5f;
            CreateFloorSlab(parent, "WingBackFloor",
                new Vector3(backX, -0.1f, backZ),
                new Vector3(WingBackMaxX - WingBackMinX, 0.2f, WingBackMaxZ - WingBackMinZ),
                floorMaterial);

            float northZ = WingBackMaxZ + 0.2f;
            float southZ = WingBackMinZ - 0.2f;
            float eastX = WingBackMaxX + 0.2f;
            float westX = WingBackMinX;
            CreateWall(parent, "WingWall+Z", new Vector3((westX + eastX) * 0.5f, height * 0.5f, northZ),
                new Vector3(eastX - westX, height, 0.4f), wallMaterial);
            CreateWall(parent, "WingWall+X", new Vector3(eastX, height * 0.5f, (southZ + northZ) * 0.5f),
                new Vector3(0.4f, height, northZ - southZ), wallMaterial);
            CreateWall(parent, "WingWall-Z", new Vector3((WallHalf + eastX) * 0.5f, height * 0.5f, southZ),
                new Vector3(eastX - WallHalf, height, 0.4f), wallMaterial);
            float hrNorth = HrDoorZ + HrRoomWidth * 0.5f;
            CreateWall(parent, "WingWall-X_N", new Vector3(westX, height * 0.5f, (northZ + hrNorth) * 0.5f),
                new Vector3(0.4f, height, northZ - hrNorth), wallMaterial);
        }

        static void SplitEastWall(Transform parent, Material material, float height)
        {
            float hrMin = HrDoorZ - HrDoorHalf;
            float hrMax = HrDoorZ + HrDoorHalf;
            float sideMin = SideDoorZ - SideDoorHalf;
            float sideMax = SideDoorZ + SideDoorHalf;
            float southLength = sideMin - (-WallHalf);
            float southCenterZ = (-WallHalf + sideMin) * 0.5f;
            CreateWall(parent, "Wall+X_S", new Vector3(WallHalf, height * 0.5f, southCenterZ),
                new Vector3(0.4f, height, southLength), material);

            float midLength = hrMin - sideMax;
            float midCenterZ = (sideMax + hrMin) * 0.5f;
            CreateWall(parent, "Wall+X_Mid", new Vector3(WallHalf, height * 0.5f, midCenterZ),
                new Vector3(0.4f, height, midLength), material);

            CreateWall(parent, "WingDoorPlug", new Vector3(WallHalf, height * 0.5f, SideDoorZ),
                new Vector3(0.4f, height, SideDoorHalf * 2f), material);
            CreateWall(parent, "HrDoorPlug", new Vector3(WallHalf, height * 0.5f, HrDoorZ),
                new Vector3(0.4f, height, HrDoorHalf * 2f), material);

            float northLength = WallHalf - hrMax;
            float northCenterZ = (hrMax + WallHalf) * 0.5f;
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
            CreateWall(parent,"EntranceWallLeft",new Vector3(-14.5f,height*.5f,-WallHalf),new Vector3(1,height,.4f),material);
            CreateWall(parent,"Wall-Z_W",new Vector3((-10+doorMin)*.5f,height*.5f,-WallHalf),new Vector3(doorMin+10,height,.4f),material);
            BurgerShop.Core.RestaurantEntrance.Build(parent);
            CreateWall(parent, "Wall-Z_E", new Vector3(eastCenterX, height * 0.5f, -WallHalf),
                new Vector3(eastLength, height, 0.4f), material);
            CreateWall(parent, "BoostDoorPlug", new Vector3(BoostDoorX, height * 0.5f, -WallHalf),
                new Vector3(BoostDoorHalf * 2f, height, 0.4f), material);
        }

        static Vector3 ClampInside(Vector3 point)
        {
            float limit = WallHalf - 0.6f;
            point.x = Mathf.Clamp(point.x, -limit, limit);
            point.z = Mathf.Clamp(point.z, -limit, limit);
            return point;
        }

        static void CreateFloorSlab(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = name;
            floor.transform.SetParent(parent, false);
            floor.transform.position = position;
            floor.transform.localScale = scale;
            Apply(floor, material);
            SolidOccupancy.Apply(floor.GetComponent<Collider>(), true);
        }

        static void CreateWall(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent, false);
            wall.transform.position = position;
            wall.transform.localScale = scale;
            Apply(wall, material);
            SolidOccupancy.Apply(wall.GetComponent<Collider>(), true);
        }

        static void Apply(GameObject instance, Material material)
        {
            Renderer renderer = instance.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;
        }
    }
}
