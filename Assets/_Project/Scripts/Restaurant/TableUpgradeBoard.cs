using System;
using BurgerShop.Economy;
using BurgerShop.Player;
using UnityEngine;

namespace BurgerShop.Restaurant
{
    public sealed class TableUpgradeBoard : MonoBehaviour
    {
        public const int StarterTableCount = 3;
        DiningArea dining;
        ShopExpansion expansion;
        RestaurantWallet wallet;
        BurgerInventory player;
        readonly TableUpgradeZone[] zones = new TableUpgradeZone[StarterTableCount + 1];

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
            EnsureExtraTable();
            if (expansion != null) expansion.PurchaseCompleted += OnFacilityBought;
        }

        public void Restore(int table0Set, int table1Set, int table2Set, int extraSet,
            int table0Invest, int table1Invest, int table2Invest, int extraInvest)
        {
            BindStarterTables();
            EnsureExtraTable();
            ZoneAt(0)?.Restore(table0Set, table0Invest);
            ZoneAt(1)?.Restore(table1Set, table1Invest);
            ZoneAt(2)?.Restore(table2Set, table2Invest);
            if (ZoneAt(3) != null) ZoneAt(3).Restore(extraSet, extraInvest);
        }

        void OnDestroy()
        {
            if (expansion != null) expansion.PurchaseCompleted -= OnFacilityBought;
        }

        void OnFacilityBought()
        {
            EnsureExtraTable();
            Changed?.Invoke();
        }

        void BindStarterTables()
        {
            if (dining == null) return;
            int count = Math.Min(StarterTableCount, dining.TableCount);
            for (int i = 0; i < count; i++)
                EnsureZone(i, dining.Tables[i]);
        }

        void EnsureExtraTable()
        {
            DiningTable extra = expansion != null ? expansion.ExtraTable : null;
            if (extra == null && dining != null && dining.TableCount > StarterTableCount)
                extra = dining.Tables[StarterTableCount];
            if (extra != null) EnsureZone(StarterTableCount, extra);
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
            Collider collider = pad.GetComponent<Collider>();
            collider.enabled = false;
            BurgerVisual.Release(collider);
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
