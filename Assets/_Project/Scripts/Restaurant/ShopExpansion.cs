using System;
using BurgerShop.Economy;
using BurgerShop.Player;
using BurgerShop.UI;
using UnityEngine;

namespace BurgerShop.Restaurant
{
    public sealed class ShopExpansion : MonoBehaviour
    {
        public const int TableCost = 150;
        public const int GrillCost = 200;
        public const int CounterCost = 250;
        public const int BoxingCost = 150;
        public const int DriveThruCost = 250;
        public const int ExtraGrillLv2Cost = 80;
        public const int ExtraGrillLv3Cost = 160;

        DiningArea dining;
        BurgerServingZone serving;
        WorkerHiringZone hiring;
        BurgerInventory player;
        RestaurantWallet wallet;
        CashFloor cash;
        UpgradeHud upgradeHud;
        DiningTable extraTable;
        ExpandableGrill extraGrill;
        CounterStock extraStock;
        CounterDropZone extraDrop;
        Transform extraCircle;
        BoxingStation boxing;
        DriveThruLane driveThru;

        public event Action PurchaseCompleted;

        public FacilityUnlockZone TablePad { get; private set; }
        public FacilityUnlockZone GrillPad { get; private set; }
        public FacilityUnlockZone CounterPad { get; private set; }
        public FacilityUnlockZone BoxingPad { get; private set; }
        public FacilityUnlockZone DriveThruPad { get; private set; }
        public bool HasExtraTable => extraTable != null;
        public bool HasExtraGrill => extraGrill != null;
        public bool HasExtraCounter => extraStock != null;
        public bool HasBoxing => boxing != null;
        public bool HasDriveThru => driveThru != null;
        public BoxingStation Boxing => boxing;
        public DriveThruLane DriveThru => driveThru;
        public int ExtraGrillLevel => extraGrill != null && extraGrill.Upgrade != null ? extraGrill.Upgrade.Level : 0;
        public DiningTable ExtraTable => extraTable;
        public ExpandableGrill ExtraGrill => extraGrill;
        public CounterStock ExtraStock => extraStock;
        public CounterDropZone ExtraDrop => extraDrop;
        public GrillUpgradeZone ExtraGrillUpgrade => extraGrill != null ? extraGrill.Upgrade : null;
        public string NextInstallHint
        {
            get
            {
                if (wallet == null) return null;
                FacilityUnlockZone[] pads = { TablePad, BoxingPad, GrillPad, CounterPad, DriveThruPad };
                string[] names = { "a table", "a boxing table", "a grill", "a counter", "a drive-thru" };
                for (int i = 0; i < pads.Length; i++)
                    if (pads[i] != null && pads[i].RankVisible && !pads[i].IsPurchased
                        && (wallet.Coins > 0 || pads[i].Invested > 0))
                        return $"Install {names[i]} - Remaining {pads[i].Remaining}";
                return null;
            }
        }

        public void Configure(DiningArea hall, BurgerServingZone cashier, WorkerHiringZone staff,
            BurgerInventory carrier, RestaurantWallet earnings, CashFloor cashFloor = null)
        {
            dining = hall;
            serving = cashier;
            hiring = staff;
            player = carrier;
            wallet = earnings;
            cash = cashFloor;
            if (TablePad == null)
                TablePad = MakePad("TableUnlockPad", ShopLayout.TableUnlock, TableCost, "TABLE", UnlockTable);
            if (GrillPad == null)
                GrillPad = MakePad("GrillUnlockPad", ShopLayout.GrillUnlock, GrillCost, "GRILL", UnlockGrill);
            if (CounterPad == null)
                CounterPad = MakePad("CounterUnlockPad", ShopLayout.CounterUnlock, CounterCost, "COUNTER", UnlockCounter);
            if (BoxingPad == null)
                BoxingPad = MakePad("BoxingUnlockPad", ShopLayout.BoxingUnlock, BoxingCost, "BOX", UnlockBoxing);
            if (DriveThruPad == null)
                DriveThruPad = MakePad("DriveThruUnlockPad", ShopLayout.DriveThruUnlock, DriveThruCost, "LANE",
                    UnlockDriveThru);
        }

        public void BindUpgradeHud(UpgradeHud hud)
        {
            upgradeHud = hud;
            if (hud != null && extraGrill != null && extraGrill.Upgrade != null)
                hud.AddZone(extraGrill.Upgrade);
        }

        public void Restore(bool table, bool grill, bool counter, int grillLevel,
            bool boxingStation = false, bool lane = false)
        {
            if (table && !HasExtraTable)
            {
                UnlockTable();
                TablePad?.RestorePurchased();
            }
            if (grill && !HasExtraGrill)
            {
                UnlockGrill();
                GrillPad?.RestorePurchased();
                if (grillLevel >= 1 && extraGrill?.Upgrade != null)
                    extraGrill.Upgrade.RestoreLevel(Mathf.Clamp(grillLevel, 1, 3));
            }
            if (counter && !HasExtraCounter)
            {
                UnlockCounter();
                CounterPad?.RestorePurchased();
            }
            if (boxingStation && !HasBoxing)
            {
                UnlockBoxing();
                BoxingPad?.RestorePurchased();
            }
            if (lane && !HasDriveThru)
            {
                UnlockDriveThru();
                DriveThruPad?.RestorePurchased();
            }
        }

        public void RestoreInvestments(int table, int grill, int counter, int box, int lane)
        {
            TablePad?.RestoreInvestment(table);
            GrillPad?.RestoreInvestment(grill);
            CounterPad?.RestoreInvestment(counter);
            BoxingPad?.RestoreInvestment(box);
            DriveThruPad?.RestoreInvestment(lane);
        }

        public void ApplyRank(int rank)
        {
            SetPad(TablePad, ShopRanks.PadUnlocked(rank, "TABLE"));
            SetPad(BoxingPad, ShopRanks.PadUnlocked(rank, "BOX"));
            SetPad(GrillPad, ShopRanks.PadUnlocked(rank, "GRILL"));
            SetPad(CounterPad, ShopRanks.PadUnlocked(rank, "COUNTER"));
            SetPad(DriveThruPad, ShopRanks.PadUnlocked(rank, "LANE"));
        }

        static void SetPad(FacilityUnlockZone pad, bool unlocked)
        {
            pad?.SetRankVisible(unlocked);
        }

        void UnlockTable()
        {
            if (HasExtraTable || dining == null) return;
            extraTable = dining.AddTable(ShopLayout.ExtraTable);
            extraTable.gameObject.name = "ExtraDiningTable";
        }

        void UnlockGrill()
        {
            if (HasExtraGrill || player == null || wallet == null) return;
            extraGrill = ExpandableGrill.Create(transform, ShopLayout.ExtraGrill, player, wallet);
            extraGrill.UpgradeSpot?.gameObject.SetActive(true);
            if (hiring != null && extraGrill.Station != null && extraGrill.Pickup != null)
                hiring.RegisterKitchen(extraGrill.Station, extraGrill.Pickup.PickupPoint);
            if (upgradeHud != null && extraGrill.Upgrade != null)
                upgradeHud.AddZone(extraGrill.Upgrade);
        }

        void UnlockCounter()
        {
            if (HasExtraCounter || serving == null) return;
            Transform desk = new GameObject("ExtraOrderCounter").transform;
            desk.SetParent(transform, false);
            Vector3 counter = ShopLayout.ExtraCounter;
            Material body = BurgerShop.Core.RuntimeMaterials.Create(new Color(0.38f, 0.49f, 0.58f));
            Material top = BurgerShop.Core.RuntimeMaterials.Create(new Color(0.90f, 0.88f, 0.78f));
            Part(desk, "OrderCounter", counter + Vector3.up * 0.5f, new Vector3(3.2f, 1f, 1.4f), body);
            Part(desk, "OrderCounterTop", counter + Vector3.up * 1.05f, new Vector3(3.35f, 0.12f, 1.55f), top);
            extraCircle = ShopFixtures.CreateCashierCircle(desk, ShopLayout.ExtraServingCircle);
            extraCircle.name = "ExtraCashierCircle";
            extraStock = ShopFixtures.CreateCounterStock(desk, ShopLayout.ExtraCounterTop);
            extraDrop = desk.gameObject.AddComponent<CounterDropZone>();
            extraDrop.Configure(extraStock, extraCircle);
            serving.RegisterCounter(extraStock, extraDrop, extraCircle);
            hiring?.RegisterDrop(extraDrop);
        }

        void UnlockBoxing()
        {
            if (HasBoxing) return;
            boxing = BoxingStation.Create(transform);
            boxing.BindPlayer(player);
            hiring?.RegisterBoxing(boxing);
            driveThru?.BindBoxing(boxing);
        }

        void UnlockDriveThru()
        {
            if (HasDriveThru) return;
            driveThru = DriveThruLane.Create(transform, boxing, wallet, cash, player);
            hiring?.RegisterDriveThru(driveThru);
        }

        FacilityUnlockZone MakePad(string name, Vector3 position, int cost, string title, Action unlocked)
        {
            Transform pad = BuildMoneyPad(name, position);
            BuildFacilityIcon(pad, position, title);
            TextMesh label = new GameObject(name + "Label").AddComponent<TextMesh>();
            label.transform.SetParent(transform, false);
            label.transform.position = position + Vector3.up * 2.7f;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontSize = 36;
            label.characterSize = 0.07f;
            label.color = new Color(0.95f, 0.98f, 0.72f);
            FacilityUnlockZone zone = pad.gameObject.AddComponent<FacilityUnlockZone>();
            zone.Configure(wallet, player, pad, cost, title, () =>
            {
                unlocked();
                Transform look = title=="TABLE"?extraTable?.transform:title=="GRILL"?extraGrill?.transform:title=="COUNTER"?extraStock?.transform:title=="BOX"?boxing?.transform:driveThru?.transform;
                UI.VisualMeshPulse.Play(look);
                PurchaseCompleted?.Invoke();
            }, label);
            return zone;
        }

        void BuildFacilityIcon(Transform pad, Vector3 position, string title)
        {
            Transform icon = new GameObject("FacilityIcon_" + title).transform;
            icon.position = position + Vector3.up * 4.1f;
            icon.SetParent(pad, true); // Preserve the icon proportions despite the thin pad's scale.
            Material light = Core.RuntimeMaterials.Create(new Color(0.9f, 0.98f, 0.82f));
            icon.gameObject.AddComponent<BurgerVisual>().OwnMaterials(light);
            Vector3 center = icon.position;
            if (title == "LANE")
            {
                Part(icon, "CarBody", center, new Vector3(0.9f, 0.24f, 0.5f), light);
                Part(icon, "CarRoof", center + Vector3.up * 0.2f, new Vector3(0.4f, 0.22f, 0.45f), light);
            }
            else if (title == "BOX")
            {
                Part(icon, "Box", center, new Vector3(0.65f, 0.35f, 0.65f), light);
                Part(icon, "Lid", center + Vector3.up * 0.22f, new Vector3(0.72f, 0.07f, 0.72f), light);
            }
            else
            {
                Part(icon, "Top", center, new Vector3(0.9f, 0.12f, 0.6f), light);
                if (title == "TABLE")
                    Part(icon, "Stem", center - Vector3.up * 0.25f, new Vector3(0.13f, 0.4f, 0.13f), light);
                else
                    Part(icon, "Base", center - Vector3.up * 0.25f, new Vector3(0.8f, 0.4f, 0.5f), light);
                if (title == "GRILL")
                    Part(icon, "Chimney", center + new Vector3(0.3f, 0.28f, 0.15f), new Vector3(0.15f, 0.45f, 0.15f), light);
            }
        }

        Transform BuildMoneyPad(string name, Vector3 position)
        {
            GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pad.name = name;
            pad.transform.SetParent(transform, false);
            pad.transform.position = position;
            pad.transform.localScale = new Vector3(2f, 0.02f, 2f);
            Collider collider = pad.GetComponent<Collider>();
            collider.enabled = false;
            BurgerVisual.Release(collider);
            pad.GetComponent<Renderer>().sharedMaterial =
                BurgerShop.Core.RuntimeMaterials.Create(new Color(0.18f, 0.72f, 0.32f));
            Material gold = BurgerShop.Core.RuntimeMaterials.Create(new Color(0.95f, 0.75f, 0.18f));
            for (int i = 0; i < 4; i++)
            {
                GameObject coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                coin.name = "Coin_" + i;
                coin.transform.SetParent(pad.transform, false);
                coin.transform.localPosition = new Vector3((i % 2) * 0.12f - 0.04f, 6f + i * 4.2f, (i / 2) * 0.10f - 0.04f);
                coin.transform.localScale = new Vector3(0.22f, 2.2f, 0.22f);
                coin.GetComponent<Renderer>().sharedMaterial = gold;
                Collider coinCollider = coin.GetComponent<Collider>();
                coinCollider.enabled = false;
                BurgerVisual.Release(coinCollider);
            }
            return pad.transform;
        }

        static void Part(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.position = position;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            Collider collider = part.GetComponent<Collider>();
            collider.enabled = false;
            BurgerVisual.Release(collider);
        }

        public static ShopExpansion Create(Transform parent, DiningArea hall, BurgerServingZone cashier,
            WorkerHiringZone staff, BurgerInventory carrier, RestaurantWallet earnings, CashFloor cashFloor = null)
        {
            GameObject root = new GameObject("ShopExpansion");
            root.transform.SetParent(parent, false);
            ShopExpansion expansion = root.AddComponent<ShopExpansion>();
            expansion.Configure(hall, cashier, staff, carrier, earnings, cashFloor);
            return expansion;
        }
    }
}
