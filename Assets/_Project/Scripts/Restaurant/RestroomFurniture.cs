using System.Globalization;
using BurgerShop.Core;
using BurgerShop.UI;
using UnityEngine;
using UnityEngine.UI;

namespace BurgerShop.Restaurant
{
    // Runtime primitives for the built restroom. Occupancy stays on RestroomExpansion.
    public static class RestroomFurniture
    {
        public const int StallCount = 3;
        public const string TitleCopy = "CLEAN BOT";
        public const string IdleTimerCopy = "00m 00s";
        public const string TrashCopy = "TRASH";
        public static readonly Color Wood = new Color(.72f, .47f, .24f);
        public static readonly Color WoodDoor = new Color(.80f, .56f, .30f);
        public static readonly Color LockerYellow = new Color(.95f, .78f, .16f);
        public static readonly Vector3 CleanerPad = new Vector3(-1.72f, .02f, -15.82f);
        public static readonly Vector3 TrashPoint = new Vector3(-3.38f, 0f, -15.56f);
        public static readonly float[] DoorAjarYaw = { 64f, 78f, 20f };

        public static Vector3 StallSeat(int index) => new Vector3(-3.68f + index * 2.05f, 0f, -20.05f);

        public static string FormatTimer(float seconds)
        {
            int total = Mathf.Max(0, (int)seconds);
            return (total / 60).ToString("00", CultureInfo.InvariantCulture) + "m "
                + (total % 60).ToString("00", CultureInfo.InvariantCulture) + "s";
        }

        public static Text Build(Transform area, Material wood, Material woodDoor, Material cream, Material white,
            Material dark, Material yellow, Material red, Material steel, Material ink, Material green, Transform[] doors)
        {
            for (int i = 0; i < StallCount; i++)
                BuildStall(area, i, wood, woodDoor, white, dark, ink, doors);
            BuildSinks(area, cream, white, steel, dark);
            BuildLockers(area, yellow, dark);
            BuildTrash(area, red, steel, white);
            BuildScrubber(area, cream, red, dark, white, green);
            ShopFixtures.CreateActionCircle(area, "CleanerCircle", CleanerPad, new Color(.96f, .97f, 1f));
            return BuildChip(area);
        }

        static void BuildStall(Transform area, int index, Material wood, Material woodDoor, Material white, Material dark,
            Material ink, Transform[] doors)
        {
            Vector3 seat = StallSeat(index);
            var root = new GameObject("Stall_" + index).transform;
            root.SetParent(area, false);
            root.position = seat;
            Part(root, "ToiletPedestal", PrimitiveType.Cube, new Vector3(0f, .28f, -.42f), new Vector3(.42f, .56f, .55f), white, false);
            Part(root, "ToiletBowl", PrimitiveType.Sphere, new Vector3(0f, .62f, -.18f), new Vector3(.72f, .22f, .78f), white, false);
            Part(root, "ToiletSeat", PrimitiveType.Sphere, new Vector3(0f, .72f, -.16f), new Vector3(.58f, .04f, .62f), dark, false);
            Part(root, "ToiletTank", PrimitiveType.Cube, new Vector3(0f, .92f, -.72f), new Vector3(.72f, .72f, .22f), white, false);
            Part(root, "StallPartitionL", PrimitiveType.Cube, new Vector3(-1.00f, .78f, -.15f), new Vector3(.08f, 1.56f, 1.85f), wood, true);
            if (index == StallCount - 1)
                Part(root, "StallPartitionR", PrimitiveType.Cube, new Vector3(1.00f, .78f, -.15f), new Vector3(.08f, 1.56f, 1.85f), wood, true);
            foreach (float x in new[] { -.96f, .96f })
            {
                Part(root, "StallLeg", PrimitiveType.Cylinder, new Vector3(x, .18f, .72f), new Vector3(.12f, .18f, .12f), dark, false);
                Part(root, "StallLegRear", PrimitiveType.Cylinder, new Vector3(x, .18f, -.92f), new Vector3(.12f, .18f, .12f), dark, false);
            }
            var hinge = new GameObject("CubicleDoorHinge").transform;
            hinge.SetParent(root, false);
            hinge.localPosition = new Vector3(-.92f, 0f, .78f);
            hinge.localRotation = Quaternion.Euler(0f, DoorAjarYaw[index], 0f);
            doors[index] = hinge;
            var door = Part(hinge, "CubicleDoor", PrimitiveType.Cube, new Vector3(.92f, .72f, 0f), new Vector3(1.84f, 1.22f, .07f), woodDoor, false);
            Part(door, "DoorHandle", PrimitiveType.Sphere, new Vector3(.72f, .02f, -.08f), new Vector3(.10f, .08f, .08f), ink, false);
            Part(root, "StallDirtyMark", PrimitiveType.Cube, new Vector3(0f, 1.42f, .82f), new Vector3(.18f, .10f, .04f), ink, false).gameObject.SetActive(false);
        }

        static void BuildSinks(Transform area, Material cream, Material white, Material steel, Material dark)
        {
            for (int i = 0; i < 2; i++)
            {
                var root = new GameObject("Sink_" + i).transform;
                root.SetParent(area, false);
                root.position = new Vector3(3.48f, 0f, -18.45f + i * 1.75f);
                Part(root, "SinkCabinet", PrimitiveType.Cube, new Vector3(0f, .42f, 0f), new Vector3(.52f, .84f, .88f), cream, true);
                Part(root, "SinkBasin", PrimitiveType.Cube, new Vector3(-.12f, .90f, 0f), new Vector3(.58f, .12f, .82f), white, false);
                Part(root, "SinkTap", PrimitiveType.Cube, new Vector3(.12f, 1.18f, 0f), new Vector3(.08f, .28f, .08f), steel, false);
                Part(root, "SinkSpout", PrimitiveType.Cube, new Vector3(-.06f, 1.28f, 0f), new Vector3(.28f, .06f, .06f), steel, false);
                Part(root, "SinkHandle", PrimitiveType.Cube, new Vector3(.16f, 1.08f, .16f), new Vector3(.04f, .04f, .14f), dark, false);
            }
        }

        static void BuildLockers(Transform area, Material yellow, Material dark)
        {
            var root = new GameObject("LockerCabinet").transform;
            root.SetParent(area, false);
            root.position = new Vector3(-4.42f, 0f, -17.15f);
            Part(root, "LockerBody", PrimitiveType.Cube, new Vector3(0f, .82f, 0f), new Vector3(.46f, 1.64f, 1.42f), yellow, true);
            for (int row = 0; row < 4; row++)
                for (int col = 0; col < 3; col++)
                {
                    float y = .28f + row * .38f;
                    float z = -.42f + col * .42f;
                    Part(root, "LockerDoor", PrimitiveType.Cube, new Vector3(.24f, y, z), new Vector3(.04f, .32f, .36f), yellow, false);
                    Part(root, "LockerSlot", PrimitiveType.Cube, new Vector3(.27f, y, z), new Vector3(.02f, .04f, .04f), dark, false);
                }
        }

        static void BuildTrash(Transform area, Material red, Material steel, Material white)
        {
            var root = new GameObject("RestroomTrash").transform;
            root.SetParent(area, false);
            root.position = TrashPoint;
            Part(root, "TrashBase", PrimitiveType.Cube, new Vector3(0f, .22f, 0f), new Vector3(.72f, .44f, .62f), steel, true);
            Part(root, "TrashBody", PrimitiveType.Cube, new Vector3(0f, .72f, 0f), new Vector3(.78f, .62f, .66f), red, true);
            Part(root, "TrashLid", PrimitiveType.Cube, new Vector3(0f, 1.08f, -.04f), new Vector3(.82f, .08f, .70f), red, false);
            Part(root, "TrashIcon", PrimitiveType.Cube, new Vector3(0f, .70f, .34f), new Vector3(.16f, .22f, .02f), white, false);
            var label = new GameObject("RestroomTrashLabel").AddComponent<TextMesh>();
            label.transform.SetParent(root, false);
            label.transform.localPosition = new Vector3(0f, 1.00f, .36f);
            label.text = TrashCopy;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontSize = 42;
            label.characterSize = .028f;
            label.color = Color.white;
            label.fontStyle = FontStyle.Bold;
        }

        static void BuildScrubber(Transform area, Material cream, Material red, Material dark, Material white, Material green)
        {
            var root = new GameObject("FloorScrubber").transform;
            root.SetParent(area, false);
            root.position = new Vector3(CleanerPad.x, 0f, CleanerPad.z);
            Part(root, "ScrubberBody", PrimitiveType.Cylinder, new Vector3(0f, .38f, 0f), new Vector3(.72f, .38f, .72f), cream, true);
            Part(root, "ScrubberCollar", PrimitiveType.Cylinder, new Vector3(0f, .12f, 0f), new Vector3(.80f, .08f, .80f), dark, false);
            Part(root, "ScrubberTank", PrimitiveType.Cylinder, new Vector3(0f, .82f, 0f), new Vector3(.58f, .22f, .58f), red, true);
            Part(root, "ScrubberCap", PrimitiveType.Cylinder, new Vector3(0f, 1.02f, 0f), new Vector3(.22f, .08f, .22f), dark, false);
            Part(root, "ScrubberMop", PrimitiveType.Sphere, new Vector3(.42f, .12f, .18f), new Vector3(.32f, .16f, .32f), dark, false);
            Part(root, "ScrubberBolt", PrimitiveType.Cube, new Vector3(.38f, .55f, 0f), new Vector3(.04f, .22f, .10f), green, false);
            Part(root, "ScrubberScreen", PrimitiveType.Cube, new Vector3(0f, .70f, .34f), new Vector3(.22f, .16f, .04f), white, false);
        }

        static Text BuildChip(Transform area)
        {
            var root = new GameObject("CleanBotChip", typeof(RectTransform), typeof(Canvas));
            root.transform.SetParent(area, false);
            root.transform.position = new Vector3(-3.58f, 2.12f, -17.05f);
            root.transform.localScale = Vector3.one * .01f;
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 8;
            ((RectTransform)root.transform).sizeDelta = new Vector2(236f, 92f);
            root.AddComponent<StationBillboard>();
            var card = HudChrome.Panel(root.transform, "Card", Vector2.one * .5f, Vector2.one * .5f, Vector2.zero,
                new Vector2(236f, 92f), new Color(1f, .58f, .12f));
            HudChrome.Icon(card.transform, "BotIcon", FoodIcons.Get(FoodIcon.Bot), new Vector2(0f, .5f), new Vector2(0f, .5f),
                new Vector2(40f, 0f), new Vector2(52f, 52f), Color.white);
            HudChrome.Label(card.transform, "CleanBotTitle", new Vector2(.28f, .48f), new Vector2(.96f, .92f), new Vector2(0f, 1f),
                Vector2.zero, Vector2.zero, 22, new Color(.95f, .22f, .05f), TextAnchor.MiddleLeft, true, false).text = TitleCopy;
            var timer = HudChrome.Label(card.transform, "CleanBotTimer", new Vector2(.28f, .08f), new Vector2(.96f, .48f), new Vector2(0f, 0f),
                Vector2.zero, Vector2.zero, 22, Color.white, TextAnchor.MiddleLeft, true, false);
            timer.text = IdleTimerCopy;
            return timer;
        }

        static Transform Part(Transform parent, string name, PrimitiveType type, Vector3 localPosition, Vector3 localScale,
            Material material, bool solid)
        {
            var part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            SolidOccupancy.Apply(part.GetComponent<Collider>(), solid);
            return part.transform;
        }
    }
}
