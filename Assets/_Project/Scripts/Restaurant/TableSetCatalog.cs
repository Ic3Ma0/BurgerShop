using UnityEngine;

namespace BurgerShop.Restaurant
{
    public enum TableSetId
    {
        Starter = 0,
        Bistro = 1,
        Diner = 2,
        Patio = 3
    }

    public readonly struct TableSet
    {
        public TableSet(TableSetId id, string name, string tierLabel, int cost, int mealPay, float eatSeconds,
            string payLabel, string speedLabel, Color tableColor, Color chairColor)
        {
            Id = id;
            Name = name;
            TierLabel = tierLabel;
            Cost = cost;
            MealPay = mealPay;
            EatSeconds = eatSeconds;
            PayLabel = payLabel;
            SpeedLabel = speedLabel;
            TableColor = tableColor;
            ChairColor = chairColor;
        }

        public TableSetId Id { get; }
        public string Name { get; }
        public string TierLabel { get; }
        public int Cost { get; }
        public int MealPay { get; }
        public float EatSeconds { get; }
        public string PayLabel { get; }
        public string SpeedLabel { get; }
        public Color TableColor { get; }
        public Color ChairColor { get; }
        public int PayBonus => MealPay - TableSetCatalog.StarterPay;
        public int FurnitureLevel => Id == TableSetId.Starter ? 1 : 2;
    }

    public static class TableSetCatalog
    {
        public const int UpgradeCost = 80;
        public const int MinCost = 50;
        public const int MaxCost = 120;
        public const int LegacyPaidCost = 80;
        public const int StarterPay = 10;
        public const float StarterEatSeconds = 3f;
        public const int MaxSetId = 3;
        public const int ChoiceCount = 3;
        public static readonly TableSetId[] Choices = { TableSetId.Bistro, TableSetId.Diner, TableSetId.Patio };

        public static TableSet Get(TableSetId id)
        {
            switch (id)
            {
                case TableSetId.Bistro:
                    return new TableSet(id, "Bistro", "Value", 50, 12, 3.0f, "PAY +2", "SAME SPEED",
                        new Color(0.86f, 0.52f, 0.18f), new Color(0.82f, 0.62f, 0.22f));
                case TableSetId.Diner:
                    return new TableSet(id, "Diner", "Turnover", 80, 12, 2.4f, "PAY +2", "SPEED +20%",
                        new Color(0.22f, 0.62f, 0.64f), new Color(0.93f, 0.88f, 0.78f));
                case TableSetId.Patio:
                    return new TableSet(id, "Patio", "Premium", 120, 16, 3.6f, "PAY +6", "SLOWER",
                        new Color(0.18f, 0.42f, 0.78f), new Color(0.82f, 0.22f, 0.20f));
                default:
                    return new TableSet(TableSetId.Starter, "Starter", "Starter", 0, StarterPay, StarterEatSeconds,
                        "PAY +0", "—", new Color(0.86f, 0.22f, 0.18f), new Color(0.18f, 0.42f, 0.72f));
            }
        }

        public static TableSet Get(int id) => Get((TableSetId)id);

        public static bool IsChoice(TableSetId id) =>
            id == TableSetId.Bistro || id == TableSetId.Diner || id == TableSetId.Patio;

        public static bool IsValidId(int id) => id >= 0 && id <= MaxSetId;

        public static int CostFor(TableSetId id) => Get(id).Cost;

        public static int Due(TableSetId id, int invested) =>
            Mathf.Max(0, CostFor(id) - Mathf.Max(0, invested));

        public static bool IsValidInvestment(int amount) => amount >= 0 && amount <= MaxCost;

        public static bool IsPaidInFull(int setId, int investment)
        {
            if (!IsChoice((TableSetId)setId) || !IsValidInvestment(investment)) return false;
            return investment >= CostFor((TableSetId)setId) || investment == LegacyPaidCost;
        }

        public static bool IsConsistent(int setId, int investment) =>
            IsValidId(setId) && IsValidInvestment(investment) && (setId == 0 || IsPaidInFull(setId, investment));
    }
}
