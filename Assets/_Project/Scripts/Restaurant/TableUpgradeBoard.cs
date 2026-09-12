using System;
using BurgerShop.Economy;
using BurgerShop.Player;
using UnityEngine;

namespace BurgerShop.Restaurant
{
    public sealed class TableUpgradeBoard : MonoBehaviour
    {
        public const int StarterTableCount = 3;
        public const int ExtraPairIndex = 3;
        public const int FourSeatIndex = 4;
        public const int SquareIndex = 5;
        DiningArea dining;
        ShopExpansion expansion;
        RestaurantWallet wallet;
        BurgerInventory player;
        readonly TableUpgradeZone[] zones = new TableUpgradeZone[SquareIndex + 1];

        public event Action Changed;

        public int ZoneCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < zones.Length; i++)
                    if (zones[i] != null) count++;
                return count;
            }
        }

        public TableUpgradeZone ZoneAt(int index) =>
            index >= 0 && index < zones.Length ? zones[index] : null;

        public int SetAt(int index) => (int)(ZoneAt(index)?.SetId ?? TableSetId.Starter);

        public int InvestedAt(int index) => ZoneAt(index)?.Invested ?? 0;

        public TableUpgradeZone PendingZoneInRange
        {
            get
            {
                TableUpgradeZone best = null;
                float distance = float.MaxValue;
                for (int i = 0; i < zones.Length; i++)
                {
                    TableUpgradeZone zone = zones[i];
                    if (zone == null || !zone.IsChoiceRange) continue;
                    float next = zone.DistanceSquared;
                    if (next < distance)
                    {
                        best = zone;
                        distance = next;
                    }
                }
                return best;
            }
        }

        public void Configure(DiningArea hall, ShopExpansion shop, RestaurantWallet earnings,
            BurgerInventory carrier)
        {
            if (expansion != null) expansion.PurchaseCompleted -= OnFacilityBought;
            dining = hall;
            expansion = shop;
            wallet = earnings;
            player = carrier;
            BindStarterTables();
            EnsurePurchasedTables();
            if (expansion != null) expansion.PurchaseCompleted += OnFacilityBought;
        }

        public void Restore(int table0Set, int table1Set, int table2Set, int extraSet,
            int table0Invest, int table1Invest, int table2Invest, int extraInvest,
            int fourSeatSet = 0, int squareSet = 0, int fourSeatInvest = 0, int squareInvest = 0)
        {
            BindStarterTables();
            EnsurePurchasedTables();
            ZoneAt(0)?.Restore(table0Set, table0Invest);
            ZoneAt(1)?.Restore(table1Set, table1Invest);
            ZoneAt(2)?.Restore(table2Set, table2Invest);
            if (ZoneAt(ExtraPairIndex) != null) ZoneAt(ExtraPairIndex).Restore(extraSet, extraInvest);
            if (ZoneAt(FourSeatIndex) != null) ZoneAt(FourSeatIndex).Restore(fourSeatSet, fourSeatInvest);
            if (ZoneAt(SquareIndex) != null) ZoneAt(SquareIndex).Restore(squareSet, squareInvest);
        }

        void OnDestroy()
        {
            if (expansion != null) expansion.PurchaseCompleted -= OnFacilityBought;
        }

        void OnFacilityBought()
        {
            EnsurePurchasedTables();
            Changed?.Invoke();
        }

        void BindStarterTables()
        {
            if (dining == null) return;
            int count = Math.Min(StarterTableCount, dining.TableCount);
            for (int i = 0; i < count; i++)
                EnsureZone(i, dining.Tables[i]);
        }

        void EnsurePurchasedTables()
        {
            if (expansion == null) return;
            if (expansion.ExtraTable != null) EnsureZone(ExtraPairIndex, expansion.ExtraTable);
            if (expansion.FourSeatTable != null) EnsureZone(FourSeatIndex, expansion.FourSeatTable);
            if (expansion.SquareTable != null) EnsureZone(SquareIndex, expansion.SquareTable);
        }

        void EnsureZone(int index, DiningTable table)
        {
            if (table == null || index < 0 || index >= zones.Length) return;
            if (zones[index] != null)
            {
                if (zones[index].Table == table) return;
                InvestZoneRegistry.Unregister(zones[index]);
            }
            Vector3 padPos = ShopLayout.TableUpgradePad(table.Center);
            Transform pad = BuildPad("Chair" + index + "UnlockPad", padPos);
            TextMesh label = NewLabel("Chair" + index + "UnlockPadLabel", padPos + Vector3.up * 1.35f);
            TableUpgradeZone zone = pad.gameObject.AddComponent<TableUpgradeZone>();
            zone.Configure(table, index, wallet, player, pad, label, () => Changed?.Invoke());
            zones[index] = zone;
        }

        Transform BuildPad(string name, Vector3 position)
        {
            GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pad.name = name;
            pad.transform.SetParent(transform, false);
            pad.transform.position = position;
            pad.transform.localScale = new Vector3(2f, 0.02f, 2f);
            SolidOccupancy.Apply(pad.GetComponent<Collider>(), false);
            pad.GetComponent<Renderer>().sharedMaterial =
                BurgerShop.Core.RuntimeMaterials.Create(new Color(0.60f, 0.36f, 0.90f));
            return pad.transform;
        }

        TextMesh NewLabel(string name, Vector3 position)
        {
            TextMesh label = new GameObject(name).AddComponent<TextMesh>();
            label.transform.SetParent(transform, false);
            label.transform.position = position;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontSize = 32;
            label.characterSize = 0.06f;
            label.color = new Color(0.94f, 0.85f, 1f);
            return label;
        }
    }
}
