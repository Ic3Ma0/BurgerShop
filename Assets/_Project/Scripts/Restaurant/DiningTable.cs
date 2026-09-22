using System.Collections.Generic;
using BurgerShop.Customer;
using BurgerShop.Core;
using BurgerShop.Player;
using UnityEngine;

namespace BurgerShop.Restaurant
{
    public enum DiningTableKind
    {
        Pair = 0,
        FourSeat = 1,
        Square = 2
    }

    public sealed class DiningTable : MonoBehaviour
    {
        public const int TrashPerGuest = 2;

        readonly List<Material> appearanceMaterials = new List<Material>();
        Vector3[] seats;
        CustomerAgent[] occupants;
        int[] trashOnSeat;
        int[] outstanding;
        List<Transform>[] piles;
        readonly List<TrashMotion> pickups = new List<TrashMotion>();
        Vector3 waitPosition;
        TrashInventory collector;
        CashFloor cash;
        float pickupRadius = 1.35f;
        float pickupInterval = 0.25f;
        float pickupCooldown;

        public int SeatCount => seats?.Length ?? 0;
        public int OccupiedSeats
        {
            get
            {
                int count = 0;
                if (occupants == null) return 0;
                for (int i = 0; i < occupants.Length; i++)
                    if (occupants[i] != null) count++;
                return count;
            }
        }
        public int TrashCount
        {
            get
            {
                int count = 0;
                if (trashOnSeat == null) return 0;
                for (int i = 0; i < trashOnSeat.Length; i++) count += trashOnSeat[i];
                return count;
            }
        }
        public int OutstandingTrash
        {
            get
            {
                int count = 0;
                if (outstanding == null) return 0;
                for (int i = 0; i < outstanding.Length; i++) count += outstanding[i];
                return count;
            }
        }
        public bool IsDirty => TrashCount > 0;
        public bool HasAvailableSeat
        {
            get
            {
                if (seats == null || IsDirty) return false;
                for (int i = 0; i < seats.Length; i++)
                    if (SeatIsOpen(i, null)) return true;
                return false;
            }
        }
        public float EatSeconds { get; private set; } = TableSetCatalog.StarterEatSeconds;
        public int MealPay { get; private set; } = TableSetCatalog.StarterPay;
        public TableSetId SetId { get; private set; } = TableSetId.Starter;
        public DiningTableKind Kind { get; private set; } = DiningTableKind.Pair;
        int legacyFurnitureLevel=1;
        public int FurnitureLevel => Mathf.Max(legacyFurnitureLevel,TableSetCatalog.Get(SetId).FurnitureLevel);
        public int MealTip => Mathf.Max(MealPay,10+(legacyFurnitureLevel-1)*5);
        public void SetFurnitureLevel(int value) { legacyFurnitureLevel=Mathf.Clamp(value,1,4); if(legacyFurnitureLevel>1&&SetId==TableSetId.Starter)FurnitureVisual.Apply(this); }
        public Vector3 WaitPosition => transform.TransformPoint(waitPosition);
        public Vector3 SeatPosition(int index) => transform.TransformPoint(seats[index]);
        public Vector3 Center => transform.position;

        public void Configure(Vector3[] sitPositions, Vector3 wait, float eatSeconds = 3f)
        {
            seats = System.Array.ConvertAll(sitPositions, transform.InverseTransformPoint);
            occupants = new CustomerAgent[seats.Length];
            trashOnSeat = new int[seats.Length];
            outstanding = new int[seats.Length];
            piles = new List<Transform>[seats.Length];
            for (int i = 0; i < piles.Length; i++)
                piles[i] = new List<Transform>();
            waitPosition = transform.InverseTransformPoint(wait);
            EatSeconds = Mathf.Max(0.1f, eatSeconds);
            pickupCooldown = 0f;
        }

        public void ApplySet(TableSetId id)
        {
            TableSet set = TableSetCatalog.Get(id);
            SetId = set.Id;
            MealPay = set.MealPay;
            EatSeconds = set.EatSeconds;
            Recolor(set);
        }

        public void BindCollector(TrashInventory bag, float radius = 1.35f, float interval = 0.25f)
        {
            collector = bag;
            pickupRadius = Mathf.Max(0.1f, radius);
            pickupInterval = Mathf.Max(0.05f, interval);
            pickupCooldown = 0f;
        }

        public void BindCash(CashFloor floor) => cash = floor;

        public void LeaveMealCash() => LeaveMealCash(MealTip);
        public void LeaveMealCash(int lockedTip) => cash?.DropAtTable(this,lockedTip);

        public bool IsSeatBlocked(int seatIndex)
        {
            return seats != null && seatIndex >= 0 && seatIndex < seats.Length && IsDirty;
        }

        public bool HasPendingPickups => pickups.Count > 0;
        public bool IsCollectorInRange => IsActorInRange(collector != null ? collector.transform : null);

        public bool IsActorInRange(Transform actor)
        {
            if (actor == null) return false;
            Vector3 offset = actor.position - Center;
            offset.y = 0f;
            return offset.sqrMagnitude <= pickupRadius * pickupRadius || ShopLayout.Horizontal(actor.position,WaitPosition)<=.45f;
        }

        public Vector3 SeatApproach(int index)
        {
            var seat=SeatPosition(index);
            if(ActorObstacles.Clear(seat,seat))return seat;
            var outward=seat-Center;outward.y=0;
            // Mount the chair from its side, never walk through the tabletop.
            return seat+Vector3.Cross(Vector3.up,outward.normalized)*.8f;
        }

        public bool TryAssignSeat(CustomerAgent guest, out Vector3 sitPosition, out int seatIndex)
        {
            sitPosition = WaitPosition;
            seatIndex = -1;
            if (guest == null || seats == null) return false;
            for (int i = 0; i < occupants.Length; i++)
            {
                if (!SeatIsOpen(i, guest)) continue;
                if (occupants[i] != guest) guest.LockMealTip(MealTip);
                occupants[i] = guest;
                sitPosition = SeatPosition(i);
                seatIndex = i;
                return true;
            }
            return false;
        }

        bool SeatIsOpen(int seatIndex, CustomerAgent guest)
        {
            if (IsDirty) return false;
            if (occupants[seatIndex] != null)
                return occupants[seatIndex] == guest;
            return true;
        }

        public void Release(CustomerAgent guest)
        {
            if (guest == null || occupants == null) return;
            for (int i = 0; i < occupants.Length; i++)
                if (occupants[i] == guest) occupants[i] = null;
        }

        public void LeaveMealTrash(int seatIndex)
        {
            if (seats == null || seatIndex < 0 || seatIndex >= seats.Length) return;
            for (int i = 0; i < TrashPerGuest; i++)
            {
                Transform visual = SpawnTrash(seatIndex, trashOnSeat[seatIndex]);
                piles[seatIndex].Add(visual);
                trashOnSeat[seatIndex]++;
                outstanding[seatIndex]++;
            }
        }

        public void NotifyTrashDisposed(int seatIndex)
        {
            if (outstanding == null || seatIndex < 0 || seatIndex >= outstanding.Length) return;
            if (outstanding[seatIndex] > 0)
            {
                outstanding[seatIndex]--;
                if(OutstandingTrash==0 && HasAvailableSeat)
                {
                    UI.FeedbackDirector.Current?.World(Center,"",.35f);
                    UI.TableCleanFlash.Play(transform);
                }
            }
        }

        public bool TryPickupTrash(TrashInventory bag)
        {
            if (bag != collector) collector = bag;
            return TryBeginPickup(bag, out TrashMotion started) && CompletePickupNow(started);
        }

        public bool TryBeginPickup(TrashInventory bag, out TrashMotion motion) => TryStartPickup(bag, out motion);

        bool CompletePickupNow(TrashMotion started)
        {
            if (started == null) return false;
            started.Advance(TrashMotion.Duration);
            return true;
        }

        bool TryStartPickup(TrashInventory bag, out TrashMotion motion)
        {
            motion = null;
            if (bag == null || trashOnSeat == null) return false;
            for (int seat = 0; seat < trashOnSeat.Length; seat++)
            {
                if (trashOnSeat[seat] <= 0 || piles[seat].Count == 0) continue;
                Transform visual = piles[seat][piles[seat].Count - 1];
                piles[seat].RemoveAt(piles[seat].Count - 1);
                motion = visual.gameObject.AddComponent<TrashMotion>();
                int capturedSeat = seat;
                TrashMotion launched = motion;
                launched.Launch(bag.transform, new Vector3(0f, 1.55f, 0.15f), Vector3.up * 0.55f, Vector3.one,
                    () => FinishPickup(capturedSeat, visual, launched, bag));
                pickups.Add(launched);
                return true;
            }
            return false;
        }

        void FinishPickup(int seat, Transform visual, TrashMotion motion, TrashInventory bag)
        {
            pickups.Remove(motion);
            if (trashOnSeat != null && seat >= 0 && seat < trashOnSeat.Length && trashOnSeat[seat] > 0)
                trashOnSeat[seat]--;
            if (bag != null && bag.TryCollect(this, seat, visual))
            {
                if (TrashCount == 0) RecordCleanTable();
                return;
            }
            if (piles == null || seat < 0 || seat >= piles.Length || visual == null) return;
            piles[seat].Add(visual);
            if (trashOnSeat != null) trashOnSeat[seat]++;
        }

        void RecordCleanTable()
        {
            var goals = GetComponentInParent<UI.SessionGoalTracker>()
                ?? UnityEngine.Object.FindFirstObjectByType<UI.SessionGoalTracker>();
            goals?.NotifyCleanTable();
        }

        void TickPickups(float deltaTime)
        {
            for (int i = pickups.Count - 1; i >= 0; i--)
                pickups[i]?.Advance(deltaTime);
        }

        void Update()
        {
            if (Application.isPlaying) Advance(Time.deltaTime);
        }

        public void Advance(float deltaTime)
        {
            if (deltaTime <= 0f) return;
            int flying = pickups.Count;
            TickPickups(deltaTime);
            bool completed = flying > 0 && pickups.Count < flying;
            if (collector == null || !collector.isActiveAndEnabled) return;
            if (!IsCollectorInRange)
            {
                pickupCooldown = 0f;
                return;
            }
            if (completed)
            {
                pickupCooldown = pickupInterval;
                return;
            }
            pickupCooldown = Mathf.Max(0f, pickupCooldown - deltaTime);
            if (pickupCooldown <= 0f && TryStartPickup(collector, out TrashMotion started))
            {
                pickupCooldown = pickupInterval;
                started.Advance(deltaTime);
            }
        }

        Transform SpawnTrash(int seatIndex, int pileIndex)
        {
            Transform visual = TrashVisual.Create(transform, seatIndex * 10 + pileIndex);
            Vector3 seat = SeatPosition(seatIndex);
            Vector3 toward = Center - seat;
            toward.y = 0f;
            if (toward.sqrMagnitude < 0.0001f) toward = Vector3.right;
            toward.Normalize();
            Vector3 side = Vector3.Cross(Vector3.up, toward);
            float spread = pileIndex % 2 == 0 ? -0.18f : 0.18f;
            visual.position = seat + toward * 0.40f + side * spread + Vector3.up * 1.02f;
            visual.rotation = Quaternion.Euler(0f, pileIndex * 40f, 0f);
            return visual;
        }

        public static DiningTable Create(Transform parent, Vector3 position,
            DiningTableKind kind = DiningTableKind.Pair)
        {
            GameObject root = new GameObject("DiningTable");
            root.transform.SetParent(parent, false);
            root.transform.position = position;
            DiningTable table = root.AddComponent<DiningTable>();
            table.Kind = kind;
            Material red = table.AppearanceMaterial(RestaurantStyle.Red);
            Material wood = table.AppearanceMaterial(RestaurantStyle.Ink);
            Material seat = table.AppearanceMaterial(RestaurantStyle.Blue);
            Material steel = table.AppearanceMaterial(RestaurantStyle.Ink);
            Material cream = table.AppearanceMaterial(RestaurantStyle.Cream);
            Material metal = table.AppearanceMaterial(RestaurantStyle.Steel);
            Vector3[] sit;
            Vector3 wait;
            if (kind == DiningTableKind.FourSeat)
            {
                Part(root.transform, "Top", PrimitiveType.Cube, new Vector3(0f, 0.72f, 0f),
                    new Vector3(1.20f, 0.12f, 2.10f), red);
                Part(root.transform, "LegN", PrimitiveType.Cube, new Vector3(0f, 0.34f, 0.65f),
                    new Vector3(0.16f, 0.68f, 0.16f), wood);
                Part(root.transform, "LegS", PrimitiveType.Cube, new Vector3(0f, 0.34f, -0.65f),
                    new Vector3(0.16f, 0.68f, 0.16f), wood);
                Vector3[] chairs =
                {
                    new Vector3(-0.90f, 0f, -0.62f),
                    new Vector3(-0.90f, 0f, 0.62f),
                    new Vector3(0.90f, 0f, -0.62f),
                    new Vector3(0.90f, 0f, 0.62f)
                };
                string[] names = { "ChairA", "ChairB", "ChairC", "ChairD" };
                sit = new Vector3[chairs.Length];
                for (int i = 0; i < chairs.Length; i++)
                {
                    Chair(root.transform, names[i], chairs[i], seat, steel, false);
                    sit[i] = position + chairs[i];
                }
                wait = position + new Vector3(0f, 0f, -1.50f);
            }
            else if (kind == DiningTableKind.Square)
            {
                Part(root.transform, "Top", PrimitiveType.Cube, new Vector3(0f, 0.72f, 0f),
                    new Vector3(1.05f, 0.12f, 1.05f), red);
                Part(root.transform, "LegNW", PrimitiveType.Cube, new Vector3(-0.38f, 0.34f, 0.38f),
                    new Vector3(0.10f, 0.68f, 0.10f), wood);
                Part(root.transform, "LegNE", PrimitiveType.Cube, new Vector3(0.38f, 0.34f, 0.38f),
                    new Vector3(0.10f, 0.68f, 0.10f), wood);
                Part(root.transform, "LegSW", PrimitiveType.Cube, new Vector3(-0.38f, 0.34f, -0.38f),
                    new Vector3(0.10f, 0.68f, 0.10f), wood);
                Part(root.transform, "LegSE", PrimitiveType.Cube, new Vector3(0.38f, 0.34f, -0.38f),
                    new Vector3(0.10f, 0.68f, 0.10f), wood);
                Chair(root.transform, "ChairA", new Vector3(0f, 0f, 0.82f), seat, steel, true);
                Chair(root.transform, "ChairB", new Vector3(0f, 0f, -0.82f), seat, steel, true);
                sit = new[]
                {
                    position + new Vector3(0f, 0f, 0.82f),
                    position + new Vector3(0f, 0f, -0.82f)
                };
                wait = position + new Vector3(-1.05f, 0f, 0f);
            }
            else
            {
                Part(root.transform, "Top", PrimitiveType.Cube, new Vector3(0f, 0.72f, 0f),
                    new Vector3(1.25f, 0.12f, 1.25f), red);
                Part(root.transform, "Leg", PrimitiveType.Cube, new Vector3(0f, 0.34f, 0f),
                    new Vector3(0.18f, 0.68f, 0.18f), wood);
                Chair(root.transform, "ChairA", new Vector3(0f, 0f, 0.95f), seat, steel, false);
                Chair(root.transform, "ChairB", new Vector3(0f, 0f, -0.95f), seat, steel, false);
                sit = new[]
                {
                    position + new Vector3(0f, 0f, 0.95f),
                    position + new Vector3(0f, 0f, -0.95f)
                };
                wait = position + new Vector3(-1.15f, 0f, 0f);
            }

            // All trim stays inside the existing furniture footprint and adds no obstacles.
            Vector3 topSize = root.transform.Find("Top").localScale;
            Detail(root.transform, "TableRim", new Vector3(0,.652f,0),
                new Vector3(topSize.x,.055f,topSize.z), cream);
            if (kind == DiningTableKind.Pair)
                Detail(root.transform, "PedestalFoot", new Vector3(0,.04f,0), new Vector3(.63f,.08f,.63f), wood);
            if (kind == DiningTableKind.FourSeat)
                foreach (float z in new[]{-.65f,.65f})
                    Detail(root.transform, "PedestalFoot", new Vector3(0,.04f,z), new Vector3(.70f,.08f,.40f), wood);
            Detail(root.transform, "NapkinHolder", new Vector3(-.06f,.86f,0), new Vector3(.16f,.16f,.20f), metal);
            Detail(root.transform, "Napkins", new Vector3(-.06f,.950f,0), new Vector3(.12f,.025f,.15f), cream);
            Detail(root.transform, "Ketchup", new Vector3(.14f,.88f,0), new Vector3(.075f,.20f,.075f), table.AppearanceMaterial(RestaurantStyle.Red));
            Detail(root.transform, "KetchupCap", new Vector3(.14f,.99f,0), new Vector3(.035f,.035f,.035f), cream);
            table.Configure(sit, wait, TableSetCatalog.StarterEatSeconds);
            table.ApplySet(TableSetId.Starter);
            return table;
        }

        void Recolor(TableSet set)
        {
            Paint(transform.Find("Top"), set.TableColor);
            foreach (Transform child in transform)
                if (child.name.StartsWith("Chair"))
                    PaintChair(child, set);
        }

        static void PaintChair(Transform chair, TableSet set)
        {
            if (chair == null) return;
            Paint(chair.Find("Seat"), set.ChairColor);
            Paint(chair.Find("BackCushion"), set.ChairColor);
        }

        static void Paint(Transform part, Color color)
        {
            if (part == null) return;
            Renderer renderer = part.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial.color = color;
        }

        static void Chair(Transform parent, string name, Vector3 local, Material cushion, Material frame,
            bool tallBack)
        {
            Transform chair = new GameObject(name).transform;
            chair.SetParent(parent, false);
            chair.localPosition = local;
            Vector3 towardTable = Vector3.zero - local;
            towardTable.y = 0f;
            if (towardTable.sqrMagnitude > 0.0001f)
                chair.localRotation = Quaternion.LookRotation(towardTable);
            Part(chair, "Seat", PrimitiveType.Cube, new Vector3(0f, 0.38f, 0f), new Vector3(0.62f, 0.12f, 0.58f), cushion);
            float backHeight = tallBack ? 0.88f : 0.55f;
            float backY = tallBack ? 0.90f : 0.72f;
            Part(chair, "Back", PrimitiveType.Cube, new Vector3(0f, backY, -0.26f),
                new Vector3(0.62f, backHeight, tallBack ? 0.12f : 0.1f), frame);
            Part(chair, "BackCushion", PrimitiveType.Cube, new Vector3(0f, backY, -0.18f),
                    new Vector3(0.50f, backHeight * 0.72f, 0.08f), cushion);
            Detail(chair, "SeatFrame", new Vector3(0,.302f,0), new Vector3(.58f,.045f,.55f), frame);
            Detail(chair, "BackPostL", new Vector3(-.22f,.20f,-.20f), new Vector3(.075f,.40f,.075f), frame);
            Detail(chair, "BackPostR", new Vector3(.22f,.20f,-.20f), new Vector3(.075f,.40f,.075f), frame);
            Part(chair, "PostL", PrimitiveType.Cube, new Vector3(-0.22f, 0.18f, 0.18f), new Vector3(0.08f, 0.36f, 0.08f), frame);
            Part(chair, "PostR", PrimitiveType.Cube, new Vector3(0.22f, 0.18f, 0.18f), new Vector3(0.08f, 0.36f, 0.08f), frame);
        }

        Material AppearanceMaterial(Color color)
        {
            var material = RuntimeMaterials.Create(color);
            appearanceMaterials.Add(material);
            return material;
        }

        void OnDestroy()
        {
            foreach (var material in appearanceMaterials)
                if (material != null) BurgerVisual.Release(material);
        }

        static void Detail(Transform parent, string name, Vector3 position, Vector3 size, Material material)
            => RestaurantStyle.Block(parent, name, position, size, material);

        static void Part(Transform parent, string name, PrimitiveType type, Vector3 localPosition, Vector3 localScale, Material material)
        {
            bool back = name == "Back" || name == "BackCushion";
            Vector3 meshSize = back ? new Vector3(localScale.x, localScale.z, localScale.y) : localScale;
            GameObject part = RestaurantStyle.Block(parent, name, localPosition, meshSize, material);
            Mesh mesh = part.GetComponent<MeshFilter>().sharedMesh;
            Vector3[] vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 v = back ? Quaternion.Euler(90, 0, 0) * vertices[i] : vertices[i];
                vertices[i] = new Vector3(v.x / localScale.x, v.y / localScale.y, v.z / localScale.z);
            }
            mesh.vertices = vertices; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            // Existing placement, seat tests and saves use these local dimensions.
            part.transform.localScale = localScale;
            var collider = part.AddComponent<BoxCollider>();
            collider.center = Vector3.zero;
            collider.size = Vector3.one;
            SolidOccupancy.Apply(collider, true);
        }
    }
}
