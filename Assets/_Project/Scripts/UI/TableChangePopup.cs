using BurgerShop.Restaurant;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BurgerShop.UI
{
    public sealed class TableChangePopup : MonoBehaviour
    {
        public CanvasGroup Group { get; private set; }
        public RectTransform Panel { get; private set; }
        public Text TitleLabel { get; private set; }
        public Text LevelLabel { get; private set; }
        public Button[] SelectButtons { get; private set; }
        public Text[] NameLabels { get; private set; }
        public Text[] TierLabels { get; private set; }
        public Text[] PayLabels { get; private set; }
        public Text[] SpeedLabels { get; private set; }
        public Text[] SelectLabels { get; private set; }
        public Image[] Swatches { get; private set; }
        public Button CloseButton { get; private set; }
        public bool IsDismissed { get; private set; }
        public bool IsVisible => Group != null && Group.alpha > 0.5f;

        public static bool IsControl(Graphic graphic)
        {
            if (graphic == null) return false;
            string name = graphic.gameObject.name;
            return name == "TableChangePopup" || name == "CloseButton"
                || name == "Select0Button" || name == "Select1Button" || name == "Select2Button";
        }

        public static TableChangePopup Build(Transform parent)
        {
            Image plate = HudChrome.Panel(parent, "TableChangePopup", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 168f), new Vector2(1000f, 560f), HudChrome.Cream, 1f);
            plate.raycastTarget = true;
            var popup = plate.gameObject.AddComponent<TableChangePopup>();
            popup.Panel = plate.rectTransform;
            popup.Group = plate.gameObject.AddComponent<CanvasGroup>();
            popup.TitleLabel = HudChrome.Label(plate.transform, "Title", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(720f, 52f), 44,
                HudChrome.Ink, TextAnchor.UpperCenter, true, true);
            popup.TitleLabel.text = "TABLE CHANGE";
            popup.LevelLabel = HudChrome.Label(plate.transform, "Level", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(400f, 40f), 32,
                HudChrome.Ink, TextAnchor.UpperCenter, true, true);
            popup.LevelLabel.text = "Level 2 · " + ShopRanks.StarRewardCopy;
            popup.SelectButtons = new Button[3];
            popup.NameLabels = new Text[3];
            popup.TierLabels = new Text[3];
            popup.PayLabels = new Text[3];
            popup.SpeedLabels = new Text[3];
            popup.SelectLabels = new Text[3];
            popup.Swatches = new Image[3];
            float[] xs = { -320f, 0f, 320f };
            for (int i = 0; i < 3; i++)
            {
                Image card = HudChrome.Panel(plate.transform, "Card" + i, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(xs[i], -20f), new Vector2(288f, 390f), Color.white, 1f);
                popup.Swatches[i] = HudChrome.Panel(card.transform, "Swatch" + i, new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(160f, 52f), HudChrome.Gold, 1f);
                popup.NameLabels[i] = HudChrome.Label(card.transform, "Name" + i, new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -76f), new Vector2(260f, 32f), 30,
                    HudChrome.Ink, TextAnchor.UpperCenter, true, true);
                popup.TierLabels[i] = HudChrome.Label(card.transform, "Tier" + i, new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -108f), new Vector2(260f, 26f), 24,
                    HudChrome.Ink, TextAnchor.UpperCenter, true, true);
                popup.PayLabels[i] = HudChrome.Label(card.transform, "Pay" + i, new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -138f), new Vector2(260f, 26f), 24,
                    HudChrome.Ink, TextAnchor.UpperCenter, true, true);
                popup.SpeedLabels[i] = HudChrome.Label(card.transform, "Speed" + i, new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -166f), new Vector2(260f, 26f), 24,
                    HudChrome.Ink, TextAnchor.UpperCenter, true, true);
                popup.SelectButtons[i] = MakeSelect(card.transform, "Select" + i + "Button");
                popup.SelectLabels[i] = LabelOn(popup.SelectButtons[i].transform, "Select" + i + "Label");
                popup.SelectLabels[i].fontSize = 26;
                popup.SelectLabels[i].text = "SELECT";
            }
            popup.CloseButton = MakeCloseButton(plate.transform);
            HudChrome.Label(popup.CloseButton.transform, "CloseLabel", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero, 32, HudChrome.Ink, TextAnchor.MiddleCenter, true, true).text = "Close";
            popup.PaintChoices();
            popup.SetVisible(false);
            return popup;
        }

        public void PaintChoices(TableUpgradeZone zone = null)
        {
            long coins = zone != null && zone.Wallet != null ? zone.Wallet.Coins : long.MaxValue;
            int invested = zone != null ? zone.Invested : 0;
            for (int i = 0; i < TableSetCatalog.ChoiceCount; i++)
            {
                var set = TableSetCatalog.Get(TableSetCatalog.Choices[i]);
                int due = TableSetCatalog.Due(set.Id, invested);
                bool afford = due == 0 || coins >= due;
                if (NameLabels[i] != null) NameLabels[i].text = set.Name;
                if (TierLabels[i] != null) TierLabels[i].text = set.TierLabel + " · " + set.Cost.ToString("N0");
                if (PayLabels[i] != null) PayLabels[i].text = set.MealPay + " / meal · " + set.PayLabel;
                if (SpeedLabels[i] != null) SpeedLabels[i].text = set.EatSeconds.ToString("0.0") + "s · " + set.SpeedLabel;
                if (Swatches[i] != null) Swatches[i].color = set.TableColor;
                if (SelectLabels[i] != null)
                    SelectLabels[i].text = due == 0 ? "SELECT · Paid · " + ShopRanks.StarRewardCopy
                        : afford ? "SELECT · " + due.ToString("N0") + " · " + ShopRanks.StarRewardCopy
                        : "Need " + (due - coins).ToString("N0");
                if (SelectButtons[i] != null)
                {
                    SelectButtons[i].interactable = afford;
                    var image = SelectButtons[i].GetComponent<Image>();
                    if (image != null) image.color = afford ? HudChrome.Green : HudChrome.TrackNavy;
                }
            }
        }

        public void Bind(UnityAction first, UnityAction second, UnityAction third, UnityAction close = null)
        {
            for (int i = 0; i < SelectButtons.Length; i++)
                SelectButtons[i].onClick.RemoveAllListeners();
            CloseButton.onClick.RemoveAllListeners();
            if (first != null) SelectButtons[0].onClick.AddListener(first);
            if (second != null) SelectButtons[1].onClick.AddListener(second);
            if (third != null) SelectButtons[2].onClick.AddListener(third);
            CloseButton.onClick.AddListener(close ?? Dismiss);
        }

        public void SetVisible(bool visible)
        {
            if (Group == null) return;
            Group.alpha = visible ? 1f : 0f;
            Group.interactable = visible;
            Group.blocksRaycasts = visible;
        }

        public void Dismiss()
        {
            IsDismissed = true;
            SetVisible(false);
        }

        public void ResetDismissed() => IsDismissed = false;

        public void ClickSelect(int index)
        {
            if (index < 0 || index >= SelectButtons.Length) return;
            Button button = SelectButtons[index];
            if (button != null && button.interactable) button.onClick.Invoke();
        }

        public void ClickClose() => CloseButton?.onClick.Invoke();

        public string CombinedCopy()
        {
            var parts = new System.Text.StringBuilder();
            if (TitleLabel != null) parts.Append(TitleLabel.text).Append('\n');
            if (LevelLabel != null) parts.Append(LevelLabel.text).Append('\n');
            for (int i = 0; i < 3; i++)
            {
                if (NameLabels[i] != null) parts.Append(NameLabels[i].text).Append('\n');
                if (TierLabels[i] != null) parts.Append(TierLabels[i].text).Append('\n');
                if (PayLabels[i] != null) parts.Append(PayLabels[i].text).Append('\n');
                if (SpeedLabels[i] != null) parts.Append(SpeedLabels[i].text).Append('\n');
                if (SelectLabels[i] != null) parts.Append(SelectLabels[i].text).Append('\n');
            }
            return parts.ToString();
        }

        static Button MakeSelect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 16f);
            rect.sizeDelta = new Vector2(220f, 72f);
            Image image = go.GetComponent<Image>();
            image.sprite = HudChrome.Rounded();
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 0.85f;
            image.color = HudChrome.Green;
            image.raycastTarget = true;
            go.AddComponent<UiPressPulse>();
            return go.GetComponent<Button>();
        }

        static Button MakeCloseButton(Transform parent)
        {
            var go = new GameObject("CloseButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-16f, -8f);
            rect.sizeDelta = new Vector2(132f, 132f);
            Image image = go.GetComponent<Image>();
            image.sprite = HudChrome.Rounded();
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1.1f;
            image.color = HudChrome.Cream;
            image.raycastTarget = true;
            go.AddComponent<UiPressPulse>();
            return go.GetComponent<Button>();
        }

        static Text LabelOn(Transform parent, string name)
        {
            Text text = HudChrome.Label(parent, name, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero, 32, Color.white, TextAnchor.MiddleCenter, true, true);
            text.raycastTarget = false;
            return text;
        }
    }
}
