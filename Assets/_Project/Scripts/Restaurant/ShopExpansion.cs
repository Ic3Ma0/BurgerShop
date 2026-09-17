using System;
using BurgerShop.Customer;
using BurgerShop.Economy;
using BurgerShop.Player;
using BurgerShop.UI;
using UnityEngine;

namespace BurgerShop.Restaurant
{
    public sealed class ShopExpansion : MonoBehaviour
    {
        public const int TableCost = 150;
        public const int FourSeatCost = 200;
        public const int SquareTableCost = 150;
        public const int GrillCost = 200;
        public const int CounterCost = 250;
        public const int BoxingCost = 150;
        public const int DriveThruCost = 250;
        public const int WingCost = 300;
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
        DiningTable fourSeatTable;
        DiningTable squareTable;
        ExpandableGrill extraGrill;
        CounterStock extraStock;
        CounterDropZone extraDrop;
        Transform extraCircle;
        BoxingStation boxing;
        DriveThruLane driveThru;
        ExpandableGrill colaMachine;
        BurgerServingZone colaServing;
        Material wingFloor;
        Material wingWall;
        int pendingTableInvestment;
        int pendingFourSeatInvestment;
        int pendingSquareInvestment;
        int pendingColaInvestment;
        int pendingColaBarInvestment;

        public event Action PurchaseCompleted;

        public FacilityUnlockZone TablePad { get; private set; }
        public FacilityUnlockZone FourSeatPad { get; private set; }
        public FacilityUnlockZone SquarePad { get; private set; }
        public FacilityUnlockZone GrillPad { get; private set; }
        public FacilityUnlockZone CounterPad { get; private set; }
        public FacilityUnlockZone BoxingPad { get; private set; }
        public FacilityUnlockZone DriveThruPad { get; private set; }
        public FacilityUnlockZone WingPad { get; private set; }
        public FacilityUnlockZone ColaPad { get; private set; }
        public FacilityUnlockZone ColaBarPad { get; private set; }
        public bool HasExtraTable => extraTable != null;
        public bool HasFourSeatTable => fourSeatTable != null;
        public bool HasSquareTable => squareTable != null;
        public bool HasExtraGrill => extraGrill != null;
        public bool HasExtraCounter => extraStock != null;
        public bool HasBoxing => boxing != null;
        public bool HasDriveThru => driveThru != null;
        public bool HasWing => ShopLayout.WingUnlocked;
        public bool HasColaMachine => colaMachine != null;
        public bool HasColaBar => colaServing != null;
        public BoxingStation Boxing => boxing;
        public DriveThruLane DriveThru => driveThru;
        public ExpandableGrill ColaMachine => colaMachine;
        public BurgerServingZone ColaServing => colaServing;
        public ProductionStation ColaStation => colaMachine != null ? colaMachine.Station : null;
        public GrillUpgradeZone ColaUpgrade => colaMachine != null ? colaMachine.Upgrade : null;
        public int ExtraGrillLevel => extraGrill != null && extraGrill.Upgrade != null ? extraGrill.Upgrade.Level : 0;
        public int ColaLevel => colaMachine != null && colaMachine.Upgrade != null ? colaMachine.Upgrade.Level : 1;
        public DiningTable ExtraTable => extraTable;
        public DiningTable FourSeatTable => fourSeatTable;
        public DiningTable SquareTable => squareTable;
        public ExpandableGrill ExtraGrill => extraGrill;
        public CounterStock ExtraStock => extraStock;
        public CounterDropZone ExtraDrop => extraDrop;
        public GrillUpgradeZone ExtraGrillUpgrade => extraGrill != null ? extraGrill.Upgrade : null;
        public int TableInvested => TablePad != null ? TablePad.Invested : pendingTableInvestment;
        public int FourSeatInvested => FourSeatPad != null ? FourSeatPad.Invested : pendingFourSeatInvestment;
        public int SquareInvested => SquarePad != null ? SquarePad.Invested : pendingSquareInvestment;
        public int ColaInvested => ColaPad != null ? ColaPad.Invested : pendingColaInvestment;
        public int ColaBarInvested => ColaBarPad != null ? ColaBarPad.Invested : pendingColaBarInvestment;
        public FacilityUnlockZone NextWingPad
        {
            get { var pad=!HasWing?WingPad:!HasColaMachine?ColaPad:!HasColaBar?ColaBarPad:!HasExtraTable?TablePad:null;
                return pad!=null&&pad.RankVisible?pad:null; }
        }
        public string NextWingHint => !HasWing ? "Install a wing" : !HasColaMachine ? "Install a cola" : !HasColaBar ? "Install a cola bar" : "Install a table";
        TextMesh wingGuide;
        void LateUpdate()
        {
            if (wingGuide == null)
            {
                wingGuide = new GameObject("WingBuildGuide").AddComponent<TextMesh>();
                wingGuide.transform.SetParent(transform, false);
                wingGuide.anchor = TextAnchor.MiddleCenter;
                wingGuide.fontSize = 48; wingGuide.characterSize = .14f;
                wingGuide.color = new Color(.16f,.22f,.24f);
            }
            var next = NextWingPad;
            wingGuide.gameObject.SetActive(next != null);
            if (next == null) return;
            bool atDoor = HasWing && player != null && player.transform.position.x < ShopLayout.WallHalf;
            wingGuide.transform.position = (atDoor ? ShopLayout.SideDoor : next.transform.position) + Vector3.up * 2.2f;
            wingGuide.text = atDoor ? "ENTER >" : (NextWingHint.Replace("Install a ", "").ToUpperInvariant() + "  " + next.Remaining + "\nv");
            if (Camera.main != null) wingGuide.transform.rotation = Camera.main.transform.rotation;
        }

        public string NextInstallHint
        {
            get
            {
                if (wallet == null) return null;
                FacilityUnlockZone[] pads =
                    { WingPad, ColaPad, ColaBarPad, TablePad, FourSeatPad, SquarePad, BoxingPad, GrillPad, CounterPad, DriveThruPad };
                string[] names =
                    { "a wing", "a cola", "a cola bar", "a table", "a 4-seat table", "a square table", "a boxing table", "a grill", "a counter", "a drive-thru" };
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
            if (WingPad == null)
                ShopLayout.ResetWingLock();
            if (WingPad == null)
                WingPad = MakePad("WingUnlockPad", ShopLayout.WingUnlock, WingCost, "WING", UnlockWing);
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
            if (hud != null && colaMachine != null && colaMachine.Upgrade != null)
                hud.AddZone(colaMachine.Upgrade);
        }

        public void Restore(bool table, bool grill, bool counter, int grillLevel,
            bool boxingStation = false, bool lane = false, bool fourSeat = false, bool square = false,
            bool wing = false, bool colaMachineBought = false, bool colaBar = false, int colaLevel = 1)
        {
            if ((wing || table || fourSeat || square || colaMachineBought || colaBar) && !HasWing)
                UnlockWing();
            if (table && !HasExtraTable)
            {
                UnlockTable();
                TablePad?.RestorePurchased();
            }
            if (fourSeat && !HasFourSeatTable)
            {
                UnlockFourSeat();
                FourSeatPad?.RestorePurchased();
            }
            if (square && !HasSquareTable)
            {
                UnlockSquare();
                SquarePad?.RestorePurchased();
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
            if (colaMachineBought && !HasColaMachine)
            {
                UnlockColaMachine();
                ColaPad?.RestorePurchased();
                if (colaLevel >= 1 && colaMachine?.Upgrade != null)
                    colaMachine.Upgrade.RestoreLevel(Mathf.Clamp(colaLevel, 1, 3));
            }
            if (colaBar && !HasColaBar)
            {
                UnlockColaBar();
                ColaBarPad?.RestorePurchased();
            }
        }

        public void RestoreInvestments(int table, int grill, int counter, int box, int lane,
            int fourSeat = 0, int square = 0, int wing = 0, int cola = 0, int colaBar = 0)
        {
            WingPad?.RestoreInvestment(wing);
            GrillPad?.RestoreInvestment(grill);
            CounterPad?.RestoreInvestment(counter);
            BoxingPad?.RestoreInvestment(box);
            DriveThruPad?.RestoreInvestment(lane);
            if (!HasWing)
            {
                pendingTableInvestment = table;
                pendingFourSeatInvestment = fourSeat;
                pendingSquareInvestment = square;
                pendingColaInvestment = cola;
                pendingColaBarInvestment = colaBar;
                return;
            }
            ApplyWingPadInvestments(table, fourSeat, square, cola, colaBar);
        }

        public bool TryPurchaseWing()
        {
            var goals = GetComponentInParent<SessionGoalTracker>();
            if (HasWing || WingPad == null || wallet == null || goals == null || !goals.Allows(ShopRanks.ColaWingRank)) return false;
            if (!wallet.TrySpend(WingPad.Remaining, () => WingPad.RestoreInvestment(WingCost))) return false;
            GetComponentInParent<SessionGoalTracker>()?.AddUpgradeStars();
            BurgerShop.Building.FacilityLayout.Current?.RefreshNavigation();
            GetComponentInParent<BurgerShop.Persistence.RestaurantPersistence>()?.Flush();
            return HasWing;
        }

        void UnlockWing()
        {
            if (wingFloor == null)
                wingFloor = BurgerShop.Core.RuntimeMaterials.Create(new Color(0.76f, 0.72f, 0.58f));
            if (wingWall == null)
                wingWall = BurgerShop.Core.RuntimeMaterials.Create(new Color(0.40f, 0.29f, 0.17f));
            Transform world = transform.parent != null ? transform.parent : transform;
            ShopLayout.OpenWing(world, wingFloor, wingWall);
            WingPad?.RestorePurchased();
            if (TablePad == null)
                TablePad = MakePad("TableUnlockPad", ShopLayout.TableUnlock, TableCost, "TABLE", UnlockTable);
            if (FourSeatPad == null)
                FourSeatPad = MakePad("FourSeatUnlockPad", ShopLayout.FourSeatUnlock, FourSeatCost, "4-SEAT",
                    UnlockFourSeat);
            if (SquarePad == null)
                SquarePad = MakePad("SquareUnlockPad", ShopLayout.SquareUnlock, SquareTableCost, "SQUARE",
                    UnlockSquare);
            if (ColaPad == null)
                ColaPad = MakePad("ColaUnlockPad", ShopLayout.Pad(ShopLayout.Cola), GrillCost, "COLA", UnlockColaMachine);
            if (ColaBarPad == null)
                ColaBarPad = MakePad("ColaBarUnlockPad", ShopLayout.Pad(ShopLayout.ColaCounter), CounterCost, "BAR",
                    UnlockColaBar);
            ApplyWingPadInvestments(pendingTableInvestment, pendingFourSeatInvestment, pendingSquareInvestment,
                pendingColaInvestment, pendingColaBarInvestment);
        }

        void ApplyWingPadInvestments(int table, int fourSeat, int square, int cola, int colaBar)
        {
            TablePad?.RestoreInvestment(table);
            FourSeatPad?.RestoreInvestment(fourSeat);
            SquarePad?.RestoreInvestment(square);
            ColaPad?.RestoreInvestment(cola);
            ColaBarPad?.RestoreInvestment(colaBar);
            pendingTableInvestment = 0;
            pendingFourSeatInvestment = 0;
            pendingSquareInvestment = 0;
            pendingColaInvestment = 0;
            pendingColaBarInvestment = 0;
        }

        void UnlockColaMachine()
        {
            if (HasColaMachine || player == null || wallet == null) return;
            if (!HasWing) UnlockWing();
            colaMachine = ExpandableGrill.CreateColaStarter(transform, player, wallet);
            colaServing?.GetComponentInChildren<CounterTierVisual>()?.Follow(colaMachine.Upgrade);
            if (hiring != null && colaMachine.Station != null && colaMachine.Pickup != null)
                hiring.RegisterCola(colaMachine.Station, colaMachine.Pickup.PickupPoint, colaServing,
                    colaServing != null ? colaServing.DropZone : null);
            if (upgradeHud != null && colaMachine.Upgrade != null)
                upgradeHud.AddZone(colaMachine.Upgrade);
        }

        void UnlockColaBar()
        {
            if (HasColaBar) return;
            if (!HasWing) UnlockWing();
            Transform area = new GameObject("ColaCustomerArea").transform;
            area.SetParent(transform, false);
            Vector3 counter = ShopLayout.ColaCounter;
            Material body = BurgerShop.Core.RuntimeMaterials.Create(new Color(0.22f, 0.42f, 0.62f));
            Material top = BurgerShop.Core.RuntimeMaterials.Create(new Color(0.86f, 0.90f, 0.94f));
            Part(area, "ColaCounter", counter + Vector3.up * 0.5f, new Vector3(3.2f, 1f, 1.4f), body, true);
            Part(area, "ColaCounterTop", counter + Vector3.up * 1.05f, new Vector3(3.35f, 0.12f, 1.55f), top, true);
            ShopFixtures.CreateStationLabel(area, "ColaCounterLabel", counter + Vector3.up * 1.85f, "COLA");

            CustomerQueue queue = area.gameObject.AddComponent<CustomerQueue>();
            queue.Product = KitchenProduct.Cola;
            queue.Configure(ShopLayout.Entrance, ShopLayout.ColaQueueEntry, ShopLayout.ColaQueueSlots, counter);

            CounterStock stock = ShopFixtures.CreateCounterStock(area, ShopLayout.ColaCounterTop, false,
                KitchenProduct.Cola);
            Transform circle = ShopFixtures.CreateCashierCircle(area, ShopLayout.ColaServingCircle);
            circle.name = "ColaCashierCircle";
            CounterDropZone drop = area.gameObject.AddComponent<CounterDropZone>();
            drop.Configure(stock, circle, 1.05f, 0.25f, false, KitchenProduct.Cola);
            colaServing = area.gameObject.AddComponent<BurgerServingZone>();
            colaServing.Configure(queue, player, wallet, circle, ShopLayout.Exit, stock, dining, drop, 10, cash);
            var appearance = CounterTierVisual.Create(area, "ColaCounterAppearance", counter, 3.2f, 1.4f, 1.05f, FoodIcon.Cola);
            if (colaMachine != null) appearance.Follow(colaMachine.Upgrade);
            hiring?.RegisterCola(colaMachine != null ? colaMachine.Station : null,
                colaMachine != null ? colaMachine.Pickup.PickupPoint : null, colaServing, drop);
        }

        public void ApplyRank(int rank)
        {
            var tracker=GetComponentInParent<UI.SessionGoalTracker>();
            if(tracker!=null&&tracker.LegacyAccess)rank=int.MaxValue;
            SetPad(WingPad,ShopRanks.PadUnlocked(rank,"WING"));
            SetPad(ColaPad,ShopRanks.PadUnlocked(rank,"COLA"));
            SetPad(ColaBarPad,ShopRanks.PadUnlocked(rank,"BAR"));
            SetPad(FourSeatPad,ShopRanks.PadUnlocked(rank,"FOUR"));SetPad(SquarePad,ShopRanks.PadUnlocked(rank,"SQUARE"));
            SetPad(TablePad, ShopRanks.PadUnlocked(rank, "TABLE"));
            SetPad(BoxingPad, ShopRanks.PadUnlocked(rank, "BOX"));
            SetPad(GrillPad, ShopRanks.PadUnlocked(rank, "GRILL"));
            SetPad(CounterPad, ShopRanks.PadUnlocked(rank, "COUNTER"));
            SetPad(DriveThruPad, ShopRanks.PadUnlocked(rank, "LANE"));
            if (rank >= ShopRanks.DriveThruRank)
                EnsureDriveThruOpen();
            else
                driveThru?.SetOpen(false);
        }

        public void EnsureDriveThruOpen()
        {
            var tracker = GetComponentInParent<SessionGoalTracker>();
            bool rankAllows = tracker == null || tracker.Allows(ShopRanks.DriveThruRank);
            if (!rankAllows)
            {
                driveThru?.SetOpen(false);
                return;
            }
            if (tracker != null && !CarWindowReady(tracker))
                return;
            if (!HasDriveThru)
                driveThru = DriveThruLane.Create(transform, boxing, wallet, cash, player);
            AdoptDriveThru(driveThru);
            DriveThruPad?.RestorePurchased();
        }

        bool CarWindowReady(SessionGoalTracker tracker)
        {
            if (tracker.Allows(ShopRanks.BoxingRank) || HasBoxing || HasDriveThru)
                return true;
            Transform world = transform.parent != null ? transform.parent : transform;
            Transform floor = world.Find("BoostRoom/CarServiceFloor");
            if (floor == null)
            {
                BoostRoom bay = world.GetComponentInChildren<BoostRoom>(true);
                floor = bay != null ? bay.transform.Find("CarServiceFloor") : null;
            }
            return floor != null && floor.gameObject.activeInHierarchy;
        }

        public void AdoptDriveThru(DriveThruLane lane)
        {
            if (lane == null) return;
            if (driveThru == null) driveThru = lane;
            driveThru.BindBoxing(boxing);
            hiring?.RegisterDriveThru(driveThru);
            var tracker = GetComponentInParent<SessionGoalTracker>();
            driveThru.SetOpen(tracker == null || tracker.Allows(ShopRanks.DriveThruRank));
        }

        static void SetPad(FacilityUnlockZone pad, bool unlocked)
        {
            pad?.SetRankVisible(unlocked || pad.Invested>0);
        }

        void UnlockTable()
        {
            if (HasExtraTable || dining == null) return;
            extraTable = dining.AddTable(ShopLayout.ExtraTable);
            extraTable.gameObject.name = "ExtraDiningTable";
        }

        void UnlockFourSeat()
        {
            if (HasFourSeatTable || dining == null) return;
            fourSeatTable = dining.AddTable(ShopLayout.FourSeatTable, DiningTableKind.FourSeat);
            fourSeatTable.gameObject.name = "FourSeatDiningTable";
        }

        void UnlockSquare()
        {
            if (HasSquareTable || dining == null) return;
            squareTable = dining.AddTable(ShopLayout.SquareTable, DiningTableKind.Square);
            squareTable.gameObject.name = "SquareDiningTable";
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
            Part(desk, "OrderCounter", counter + Vector3.up * 0.5f, new Vector3(3.2f, 1f, 1.4f), body, true);
            Part(desk, "OrderCounterTop", counter + Vector3.up * 1.05f, new Vector3(3.35f, 0.12f, 1.55f), top, true);
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
            EnsureDriveThruOpen();
            GetComponentInParent<SessionGoalTracker>()?.ApplyUnlocks();
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
                Transform look = title == "TABLE" ? extraTable?.transform
                    : title == "4-SEAT" ? fourSeatTable?.transform
                    : title == "SQUARE" ? squareTable?.transform
                    : title == "GRILL" ? extraGrill?.transform
                    : title == "COUNTER" ? extraStock?.transform
                    : title == "COLA" ? colaMachine?.transform
                    : title == "BAR" ? colaServing?.transform
                    : title == "WING" ? transform.Find("WingBackFloor") ?? transform.parent?.Find("WingBackFloor")
                    : title == "BOX" ? boxing?.transform
                    : driveThru?.transform;
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
            else if (title == "WING")
            {
                Part(icon, "Floor", center, new Vector3(1.0f, 0.08f, 0.7f), light);
                Part(icon, "Door", center + new Vector3(0.4f, 0.22f, 0f), new Vector3(0.12f, 0.45f, 0.35f), light);
            }
            else if (title == "COLA")
            {
                Part(icon, "Cup", center, new Vector3(0.35f, 0.4f, 0.35f), light);
                Part(icon, "Straw", center + new Vector3(0.08f, 0.32f, 0f), new Vector3(0.06f, 0.28f, 0.06f), light);
            }
            else if (title == "BAR")
            {
                Part(icon, "Desk", center, new Vector3(0.9f, 0.22f, 0.45f), light);
                Part(icon, "Top", center + Vector3.up * 0.18f, new Vector3(0.95f, 0.08f, 0.5f), light);
            }
            else if (title == "BOX")
            {
                Part(icon, "Box", center, new Vector3(0.65f, 0.35f, 0.65f), light);
                Part(icon, "Lid", center + Vector3.up * 0.22f, new Vector3(0.72f, 0.07f, 0.72f), light);
            }
            else if (title == "4-SEAT")
            {
                Part(icon, "Top", center, new Vector3(0.55f, 0.12f, 1.05f), light);
                Part(icon, "Stem", center - Vector3.up * 0.25f, new Vector3(0.13f, 0.4f, 0.13f), light);
            }
            else if (title == "SQUARE")
            {
                Part(icon, "Top", center, new Vector3(0.55f, 0.12f, 0.55f), light);
                Part(icon, "Stem", center - Vector3.up * 0.25f, new Vector3(0.12f, 0.4f, 0.12f), light);
                Part(icon, "BackA", center + new Vector3(0f, 0.22f, -0.28f), new Vector3(0.4f, 0.35f, 0.08f), light);
                Part(icon, "BackB", center + new Vector3(0f, 0.22f, 0.28f), new Vector3(0.4f, 0.35f, 0.08f), light);
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
            SolidOccupancy.Apply(pad.GetComponent<Collider>(), false);
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
                SolidOccupancy.Apply(coin.GetComponent<Collider>(), false);
            }
            return pad.transform;
        }

        static void Part(Transform parent, string name, Vector3 position, Vector3 scale, Material material,
            bool solid = false)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.position = position;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            SolidOccupancy.Apply(part.GetComponent<Collider>(), solid);
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

        void OnDestroy() => ShopLayout.ResetWingLock();
    }
}
