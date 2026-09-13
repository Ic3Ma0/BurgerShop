using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BurgerShop.UI
{
    public sealed class StatUpgradePopup : MonoBehaviour
    {
        public static readonly Color ReadyLeft = HudChrome.Tomato;
        public static readonly Color ReadyRight = HudChrome.Tomato;
        public static readonly Color Disabled = HudChrome.TrackNavy;

        Image[] firstSteps, secondSteps;

        public CanvasGroup Group { get; private set; }
        public RectTransform Panel { get; private set; }
        public Text TitleLabel { get; private set; }
        public Button FirstButton { get; private set; }
        public Button SecondButton { get; private set; }
        public Button CloseButton { get; private set; }
        public Text FirstLabel { get; private set; }
        public Text SecondLabel { get; private set; }
        public Text CloseLabel { get; private set; }
        public bool IsDismissed { get; private set; }
        public bool IsVisible => Group != null && Group.alpha > 0.5f;

        public static bool IsControl(Graphic graphic) =>
            graphic != null && (graphic.gameObject.name == "PlayerUpgradePopup" || graphic.gameObject.name == "StaffUpgradePopup" || graphic.gameObject.name == "SpeedButton"
                || graphic.gameObject.name == "CarryButton"
                || graphic.gameObject.name == "CloseButton");

        public static StatUpgradePopup Build(Transform parent, string objectName, string title,
            string firstName, string secondName)
        {
            Image plate = HudChrome.Panel(parent, objectName, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 144f), new Vector2(816f, 488f), HudChrome.Cream, 1f);
            plate.raycastTarget = true;
            var popup = plate.gameObject.AddComponent<StatUpgradePopup>();
            popup.Panel = plate.rectTransform;
            popup.Group = plate.gameObject.AddComponent<CanvasGroup>();
            popup.TitleLabel = HudChrome.Label(plate.transform, "Title", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(-48f, -40f), new Vector2(600f, 56f), 44,
                HudChrome.Ink, TextAnchor.UpperCenter, true, true);
            popup.TitleLabel.text = title;
            popup.FirstButton = MakeButton(plate.transform, firstName + "Button", new Vector2(-192f, -56f));
            popup.SecondButton = MakeButton(plate.transform, secondName + "Button", new Vector2(192f, -56f));
            popup.FirstLabel = LabelOn(popup.FirstButton.transform, firstName + "Label");
            popup.SecondLabel = LabelOn(popup.SecondButton.transform, secondName + "Label");
            popup.CloseButton = MakeCloseButton(plate.transform);
            popup.CloseLabel = LabelOn(popup.CloseButton.transform, "CloseLabel");
            popup.CloseLabel.text = "Close";
            popup.CloseLabel.rectTransform.offsetMax = new Vector2(-8,-8);
            popup.CloseLabel.fontSize = 44;
            popup.CloseLabel.color = HudChrome.Ink;
            FoodIcons.Add(popup.FirstButton.transform, FoodIcon.Speed, new Vector2(0, 88), 48);
            FoodIcons.Add(popup.SecondButton.transform, FoodIcon.Carry, new Vector2(0, 88), 48);
            popup.firstSteps = MakeSteps(popup.FirstButton.transform);
            popup.secondSteps = MakeSteps(popup.SecondButton.transform);
            popup.SetVisible(false);
            return popup;
        }

        public void Bind(UnityAction first, UnityAction second, UnityAction close = null)
        {
            FirstButton.onClick.RemoveAllListeners();
            SecondButton.onClick.RemoveAllListeners();
            CloseButton.onClick.RemoveAllListeners();
            if (first != null) FirstButton.onClick.AddListener(first);
            if (second != null) SecondButton.onClick.AddListener(second);
            CloseButton.onClick.AddListener(close ?? Dismiss);
        }

        public void SetVisible(bool visible)
        {
            if (Group == null) return;
            Group.alpha = visible ? 1f : 0f;
            Group.interactable = visible;
            Group.blocksRaycasts = visible;
        }

        public void SetOption(bool first, string text, bool interactable, Color color)
        {
            Button button = first ? FirstButton : SecondButton;
            Text label = first ? FirstLabel : SecondLabel;
            if (label != null) { label.text = text; label.color = interactable ? Color.white : HudChrome.Ink; }
            if (button == null) return;
            button.interactable = interactable;
            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = color;
                image.raycastTarget = true;
            }
        }

        public void PaintOption(bool first, string name, bool max, int cost, bool afford, Color ready)
        {
            string text = max ? $"{name}\nMAX" : $"{name}\n{cost}";
            SetOption(first, text, afford, afford ? ready : Disabled);
        }

        public void PaintStat(bool first, string name, int tier, string current, string next, bool max, int cost, long coins)
        {
            var steps = first ? firstSteps : secondSteps;
            for (int i = 0; i < steps.Length; i++)
                steps[i].color = i < tier ? HudChrome.Gold : HudChrome.TrackNavy;
            bool afford = !max && coins >= cost;
            string values = max ? current : current + " → " + next;
            string price = max ? "MAX" : afford ? cost.ToString("N0") + " coins" : "Need " + (cost-coins).ToString("N0") + " more";
            SetOption(first, name + " · " + tier + "/" + steps.Length + "\n" + values + "\n" + price, afford, afford ? HudChrome.Tomato : Disabled);
        }

        public void Dismiss()
        {
            IsDismissed = true;
            SetVisible(false);
        }

        public void ResetDismissed() => IsDismissed = false;

        public void ClickFirst() { if (FirstButton != null && FirstButton.interactable) FirstButton.onClick.Invoke(); }

        public void ClickSecond() { if (SecondButton != null && SecondButton.interactable) SecondButton.onClick.Invoke(); }

        public void ClickClose() => CloseButton?.onClick.Invoke();

        static Image[] MakeSteps(Transform parent)
        {
            var steps = new Image[Player.PlayerBoost.MaxLevel];
            float stride = 300f / steps.Length;
            for (int i = 0; i < steps.Length; i++)
            {
                steps[i] = HudChrome.Panel(parent, "LevelStep" + (i + 1),
                    new Vector2(.5f, 0), new Vector2(.5f, .5f),
                    new Vector2(-150f + stride * (i + .5f), 16f),
                    new Vector2(stride - 3f, 10f), HudChrome.TrackNavy);
                steps[i].raycastTarget = false;
            }
            return steps;
        }

        static Button MakeButton(Transform parent, string name, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(352f, 272f);
            Image image = go.GetComponent<Image>();
            image.sprite = HudChrome.Rounded();
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 0.85f;
            image.color = Disabled;
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
            text.rectTransform.offsetMin = new Vector2(16f, 8f);
            text.rectTransform.offsetMax = new Vector2(-16f, -64f);
            text.raycastTarget = false;
            return text;
        }
    }
}
