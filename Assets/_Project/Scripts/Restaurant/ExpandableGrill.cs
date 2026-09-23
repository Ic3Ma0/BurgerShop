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
            new Vector3(1.04f, 1.04f, 1.04f),
            new Vector3(1.08f, 1.08f, 1.08f)
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
            bool changed=shown!=Mathf.Clamp(level,1,3);
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
            if(changed)BurgerShop.Building.FacilityLayout.Current?.RefreshNavigation();
            _ = animate;
        }

        public void SnapOutputToTray()
        {
            Transform tray = ActiveLook != null ? ActiveLook.Find("OutputTray") : null;
            if (output == null || tray == null) return;
            output.position = tray.position + Vector3.up * (tray.GetComponent<Renderer>().bounds.extents.y + .01f);
        }

        public static ExpandableGrill CreateStarter(Transform parent, BurgerInventory player, RestaurantWallet wallet)
        {
            return Create(parent, ShopLayout.Grill, player, wallet, "BurgerGrill", "PickupSpot",
                ShopLayout.UpgradeSpot, "GrillUpgradeSpot", new[] { 30, 60 }, new[] { 3f, 2f, 1.5f });
        }

        public static ExpandableGrill CreateColaStarter(Transform parent, BurgerInventory player, RestaurantWallet wallet)
        {
            return Create(parent, ShopLayout.Cola, player, wallet, "ColaMachine", "ColaPickupSpot",
                ShopLayout.ColaUpgrade, "ColaUpgradeSpot", new[] { 30, 60 }, new[] { 3f, 2f, 1.5f },
                KitchenProduct.Cola);
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
            string upgradeSpotName, int[] costs, float[] cookSeconds, KitchenProduct product = KitchenProduct.Burger)
        {
            Transform root = new GameObject(rootName).transform;
            root.SetParent(parent, false);
            root.position = position;

            bool cola = product == KitchenProduct.Cola;
            Transform[] kits = { BuildLook(root,1,cola),BuildLook(root,2,cola),BuildLook(root,3,cola) };

            Transform output = new GameObject(cola ? "ColaOutput" : "BurgerOutput").transform;
            output.SetParent(root, false);
            TextMesh label = NewLabel(root, cola ? "ColaStatus" : "GrillStatus", StatusLocal(1),
                cola ? new Color(0.82f, 0.92f, 1f) : new Color(1f, 0.92f, 0.72f), 36, 0.09f);
            ProductionStation station = root.gameObject.AddComponent<ProductionStation>();
            station.Configure(output, kits[0].Find("ProgressFill"), label, cookSeconds[0],
                ProductionStation.CapacityForLevel(1, product), product);

            GameObject spot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            spot.name = pickupName;
            spot.transform.SetParent(root, false);
            spot.transform.localPosition = ShopLayout.GrillPickupLocal;
            spot.transform.localScale = new Vector3(2f, 0.015f, 2f);
            SolidOccupancy.Apply(spot.GetComponent<Collider>(), false);
            spot.GetComponent<Renderer>().sharedMaterial = BurgerShop.Core.RuntimeMaterials.Create(new Color(0.24f, 0.77f, 0.46f));
            BurgerPickupZone pickup = root.gameObject.AddComponent<BurgerPickupZone>();
            pickup.Configure(station, player, spot.transform);

            Vector3 upgradeLocal = upgradeWorld - position;
            GameObject upgradeSpot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            upgradeSpot.name = upgradeSpotName;
            upgradeSpot.transform.SetParent(root, false);
            upgradeSpot.transform.localPosition = upgradeLocal;
            upgradeSpot.transform.localScale = new Vector3(2f, 0.02f, 2f);
            SolidOccupancy.Apply(upgradeSpot.GetComponent<Collider>(), false);
            upgradeSpot.GetComponent<Renderer>().sharedMaterial =
                BurgerShop.Core.RuntimeMaterials.Create(new Color(0.60f, 0.36f, 0.90f));
            TextMesh upgradeLabel = NewLabel(root, "UpgradeMarker",
                upgradeLocal + Vector3.up * 1.35f, new Color(0.94f, 0.85f, 1f), 32, 0.06f);
            StationUpgradeFeedback visuals = StationUpgradeFeedback.Attach(root, Array.Empty<Transform>());
            visuals.HidePersistentMax = true;
            GrillUpgradeZone upgrade = root.gameObject.AddComponent<GrillUpgradeZone>();
            upgrade.Configure(station, wallet, player, upgradeSpot.transform, upgradeLabel, null, visuals,
                costs, cookSeconds, product);

            ExpandableGrill visual = root.gameObject.AddComponent<ExpandableGrill>();
            visual.UpgradeSpot = upgradeSpot.transform;
            visual.Bind(station, pickup, upgrade, visuals, kits, output, label);
            return visual;
        }

        static Transform BuildLook(Transform parent,int level,bool cola)
        {
            var look=new GameObject("Look_Lv"+level).transform;look.SetParent(parent,false);
            look.localScale=LookScales[level-1];look.gameObject.SetActive(level==1);
            var cream=BurgerShop.Core.RuntimeMaterials.Create(BurgerShop.Core.RestaurantStyle.Cream);
            var red=BurgerShop.Core.RuntimeMaterials.Create(BurgerShop.Core.RestaurantStyle.Red);
            var metal=BurgerShop.Core.RuntimeMaterials.Create(BurgerShop.Core.RestaurantStyle.Steel);
            var dark=BurgerShop.Core.RuntimeMaterials.Create(BurgerShop.Core.RestaurantStyle.Ink);
            var blue=BurgerShop.Core.RuntimeMaterials.Create(BurgerShop.Core.RestaurantStyle.Blue);
            var light=BurgerShop.Core.RuntimeMaterials.Create(new Color(.46f,.84f,.83f),true);
            var green=BurgerShop.Core.RuntimeMaterials.Create(new Color(.3f,.76f,.33f));
            look.gameObject.AddComponent<BurgerVisual>().OwnMaterials(cream,red,metal,dark,blue,light,green);
            BodyMetrics(level,out float width,out float height,out float depth,out float top);
            void Block(string name,Vector3 pos,Vector3 size,Material mat,bool solid=false)=>BurgerShop.Core.RestaurantStyle.Block(look,name,pos,size,mat,solid);
            Block("Body",new Vector3(0,height*.5f+.10f,0),new Vector3(width,height,depth),cola?cream:metal,true);
            Block("BasePlinth",new Vector3(0,.1f,0),new Vector3(width-.1f,.16f,depth-.1f),dark);
            Block("CounterLip",new Vector3(0,top,0),new Vector3(width+.10f,.12f,depth+.08f),metal);
            Block("FrontFascia",new Vector3(0,top-.26f,-depth*.5f-.025f),new Vector3(width-.1f,.32f,.08f),cola?blue:red);
            for(int i=0;i<level+1;i++)
            {
                float x=-width*.36f+i*width*.72f/level;
                Block("CabinetHandle",new Vector3(x,.50f,-depth*.5f-.06f),new Vector3(.32f,.055f,.08f),dark);
            }
            Block("OutputTray",new Vector3(width*.39f,top+.1f,0),new Vector3(.90f,.10f,depth*.84f),metal,true);
            var cycle=look.gameObject.AddComponent<EquipmentCycleVisual>();
            if(cola)
            {
                Block("DispenserBack",new Vector3(-.40f,top+.70f,.24f),new Vector3(width*.58f,1.4f,.64f),blue);
                Block("ColaHeader",new Vector3(-.40f,top+1.30f,-.02f),new Vector3(width*.63f,.44f,1.05f),red);
                Block("SelectionPanel",new Vector3(-.40f,top+.96f,-.30f),new Vector3(width*.53f,.28f,.10f),dark);
                for(int i=0;i<level;i++)
                {
                    float x=-.4f+(i-(level-1)*.5f)*.36f;
                    Block("FlavorButton_"+i,new Vector3(x,top+.97f,-.36f),new Vector3(.24f,.18f,.035f),i%2==0?light:cream);
                    Block("Nozzle_"+i,new Vector3(x,top+.66f,-.32f),new Vector3(.11f,.28f,.19f),metal);
                    Block("DispenserLever_"+i,new Vector3(x,top+.40f,-.30f),new Vector3(.05f,.30f,.06f),dark);
                }
                Block("DripTray",new Vector3(-.4f,top+.08f,-.2f),new Vector3(width*.55f,.10f,.80f),dark);
                for(int i=0;i<6;i++)Block("DripGrate",new Vector3(-width*.36f+i*.20f,top+.14f,-.2f),new Vector3(.04f,.025f,.65f),metal);
                var cup=ColaVisualFactory.Create(look,0);cup.name="FillingCup";cup.localPosition=new Vector3(-.4f,top+.16f,-.25f);cup.localScale=Vector3.one*.7f;
                cycle.Stream=BurgerShop.Core.RestaurantStyle.Block(look,"ColaStream",new Vector3(-.4f,top+.44f,-.36f),new Vector3(.045f,.35f,.045f),dark);
            }
            else
            {
                var lids=new System.Collections.Generic.List<Transform>();
                float surface=width*.62f;
                for(int i=0;i<level;i++)
                {
                    float x=-width*.47f+surface*(i+.5f)/level;
                    float plate=surface/level-.06f;
                    Block(i==0?"GrillTop":"GrillDeck_"+(i+1),new Vector3(x,top+.08f,0),new Vector3(plate,.10f,depth*.74f),dark);
                    var hinge=new GameObject("GrillLid_"+(i+1)).transform;hinge.SetParent(look,false);hinge.localPosition=new Vector3(x,top+.18f,depth*.31f);hinge.localRotation=Quaternion.Euler(42,0,0);lids.Add(hinge);
                    BurgerShop.Core.RestaurantStyle.Block(hinge,"PressPlate",new Vector3(0,0,-depth*.31f),new Vector3(plate,.13f,depth*.66f),metal);
                    BurgerShop.Core.RestaurantStyle.Block(hinge,"PressHandle",new Vector3(0,.10f,-depth*.61f),new Vector3(plate*.65f,.10f,.14f),dark);
                    BagVisualFactory.Part(look,"CookingPatty",PrimitiveType.Cylinder,new Vector3(x,top+.16f,-.05f),new Vector3(plate*.64f,.045f,.43f),red);
                    Block("Lamp_"+(i+1),new Vector3(x,top-.24f,-depth*.5f-.073f),new Vector3(.12f,.07f,.018f),light);
                }
                cycle.Lids=lids.ToArray();
                if(level>=2)Block("Splashback",new Vector3(-.35f,top+.38f,depth*.46f),new Vector3(width*.70f,.70f,.07f),metal);
                if(level==3)
                {
                    Block("VentHood",new Vector3(-.35f,top+1.05f,depth*.30f),new Vector3(width*.76f,.22f,.7f),metal);
                    Block("HoodSupport",new Vector3(-.35f,top+.60f,depth*.47f),new Vector3(.16f,.8f,.10f),dark);
                }
            }
            Part(look,"ProgressBack",PrimitiveType.Cube,new Vector3(0,top-.10f,-depth*.5f-.072f),new Vector3(1.5f,.10f,.035f),dark);
            Part(look,"ProgressFill",PrimitiveType.Cube,new Vector3(-.75f,top-.10f,-depth*.5f-.095f),new Vector3(0,.06f,.03f),green);
            return look;
        }

        public static void BodyMetrics(int level, out float width, out float height, out float depth, out float bodyTop)
        {
            int tier = Mathf.Clamp(level, 1, 3);
            width = 2.4f + (tier - 1) * 0.2f;
            height = .90f + (tier - 1) * .05f;
            depth = 1.6f + (tier - 1) * .10f;
            bodyTop = height + .10f;
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
            SolidOccupancy.Apply(part.GetComponent<Collider>(), true);
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
