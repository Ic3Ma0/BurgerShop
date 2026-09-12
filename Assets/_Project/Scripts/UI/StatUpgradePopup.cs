using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BurgerShop.UI
{
    public sealed class StatUpgradePopup : MonoBehaviour
    {
        public static readonly Color ReadyLeft = new Color(0.20f, 0.62f, 0.78f, 1f);
        public static readonly Color ReadyRight = new Color(0.86f, 0.56f, 0.22f, 1f);
        public static readonly Color Disabled = new Color(0.52f, 0.54f, 0.58f, 1f);

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
            graphic != null && (graphic.gameObject.name == "SpeedButton"
                || graphic.gameObject.name == "CarryButton"
                || graphic.gameObject.name == "CloseButton");

        public static StatUpgradePopup Build(Transform parent, string objectName, string title,
            string firstName, string secondName)
        {
            Image plate = HudChrome.Panel(parent, objectName, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 280f), new Vector2(560f, 268f), new Color(0.10f, 0.16f, 0.22f, 0.94f), 0.8f);
            var popup = plate.gameObject.AddComponent<StatUpgradePopup>();
            popup.Panel = plate.rectTransform;
            popup.Group = plate.gameObject.AddComponent<CanvasGroup>();
            popup.TitleLabel = HudChrome.Label(plate.transform, "Title", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(520f, 44f), 32,
                Color.white, TextAnchor.UpperCenter, true, true);
            popup.TitleLabel.text = title;
            popup.FirstButton = MakeButton(plate.transform, firstName + "Button", new Vector2(-130f, -28f));
            popup.SecondButton = MakeButton(plate.transform, secondName + "Button", new Vector2(130f, -28f));
            popup.FirstLabel = LabelOn(popup.FirstButton.transform, firstName + "Label");
            popup.SecondLabel = LabelOn(popup.SecondButton.transform, secondName + "Label");
            popup.CloseButton = MakeCloseButton(plate.transform);
            popup.CloseLabel = LabelOn(popup.CloseButton.transform, "CloseLabel");
            popup.CloseLabel.text = "X";
            popup.CloseLabel.fontSize = 30;
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
            if (label != null) label.text = text;
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

        public void Dismiss()
        {
            IsDismissed = true;
            SetVisible(false);
        }

        public void ResetDismissed() => IsDismissed = false;

        public void ClickFirst() => FirstButton?.onClick.Invoke();

        public void ClickSecond() => SecondButton?.onClick.Invoke();

        public void ClickClose() => CloseButton?.onClick.Invoke();

        static Button MakeButton(Transform parent, string name, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(220f, 128f);
            Image image = go.GetComponent<Image>();
            image.sprite = HudChrome.Rounded();
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 0.85f;
            image.color = Disabled;
            image.raycastTarget = true;
            return go.GetComponent<Button>();
        }

        static Button MakeCloseButton(Transform parent)
        {
            var go = new GameObject("CloseButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-10f, -10f);
            rect.sizeDelta = new Vector2(56f, 56f);
            Image image = go.GetComponent<Image>();
            image.sprite = HudChrome.Rounded();
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1.1f;
            image.color = new Color(0.70f, 0.28f, 0.30f, 1f);
            image.raycastTarget = true;
            return go.GetComponent<Button>();
        }

        static Text LabelOn(Transform parent, string name)
        {
            Text text = HudChrome.Label(parent, name, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero, 28, Color.white, TextAnchor.MiddleCenter, true, true);
            text.rectTransform.offsetMin = new Vector2(10f, 8f);
            text.rectTransform.offsetMax = new Vector2(-10f, -8f);
            text.raycastTarget = false;
            return text;
        }
    }
}
