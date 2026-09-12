using UnityEngine;

namespace BurgerShop.Restaurant
{
    public sealed class StationUpgradeFeedback : MonoBehaviour
    {
        public const float PunchDuration = 0.32f;
        public const float PopupDuration = 0.8f;
        public const int DefaultMaxLevel = 3;

        Transform station;
        Transform[] extras;
        TextMesh popup;
        TextMesh maxBadge;
        Vector3 restScale = Vector3.one;
        Vector3 restPosition;
        Vector3 popupRest;
        Color popupColor = new Color(1f, 0.9f, 0.28f, 1f);
        int maxLevel = DefaultMaxLevel;
        bool hidePersistentMax;
        float punchElapsed = -1f;
        float popupElapsed = -1f;

        public int VisualLevel { get; private set; } = 1;
        public bool IsPunching => punchElapsed >= 0f && punchElapsed < PunchDuration;
        public bool IsPopupPlaying => popupElapsed >= 0f && popupElapsed < PopupDuration;
        public float PunchScale => !IsPunching ? 1f : 1f + 0.16f * Mathf.Sin(Mathf.Clamp01(punchElapsed / PunchDuration) * Mathf.PI);
        public string PopupText => popup != null ? popup.text : "";
        public bool HidePersistentMax
        {
            get => hidePersistentMax;
            set
            {
                hidePersistentMax = value;
                RefreshMaxBadge();
            }
        }

        public bool ShowsMax => !hidePersistentMax && maxBadge != null && maxBadge.gameObject.activeSelf;
        public int ActivePartCount
        {
            get
            {
                int count = 0;
                if (extras == null) return 0;
                for (int i = 0; i < extras.Length; i++)
                    if (extras[i] != null && extras[i].gameObject.activeSelf) count++;
                return count;
            }
        }

        public static StationUpgradeFeedback Attach(Transform visualRoot, Transform[] extraParts, int levels = DefaultMaxLevel)
        {
            StationUpgradeFeedback feedback = visualRoot.GetComponent<StationUpgradeFeedback>()
                ?? visualRoot.gameObject.AddComponent<StationUpgradeFeedback>();
            TextMesh pop = FindOrCreateLabel(visualRoot, "UpgradeLevelPop", new Vector3(-1.2f, 3.15f, 0.2f),
                new Color(1f, 0.9f, 0.28f, 1f), 64, 0.13f);
            TextMesh max = FindOrCreateLabel(visualRoot, "UpgradeMaxBadge", new Vector3(-1.4f, 2.55f, 0.2f),
                new Color(1f, 0.78f, 0.18f, 1f), 42, 0.1f);
            max.text = "MAX";
            pop.gameObject.SetActive(false);
            max.gameObject.SetActive(false);
            feedback.Configure(visualRoot, extraParts, pop, max, levels);
            return feedback;
        }

        public void Configure(Transform visualRoot, Transform[] extraParts, TextMesh levelPopup, TextMesh maxLabel,
            int levels = DefaultMaxLevel)
        {
            station = visualRoot;
            extras = extraParts;
            popup = levelPopup;
            maxBadge = maxLabel;
            maxLevel = Mathf.Max(1, levels);
            restScale = station != null ? station.localScale : Vector3.one;
            restPosition = station != null ? station.localPosition : Vector3.zero;
            popupRest = popup != null ? popup.transform.localPosition : Vector3.zero;
            if (popup != null) popupColor = popup.color;
            ShowLevel(VisualLevel);
        }

        public void ShowLevel(int level)
        {
            VisualLevel = Mathf.Clamp(level, 1, maxLevel);
            punchElapsed = -1f;
            popupElapsed = -1f;
            ApplyParts();
            ResetPunch();
            HidePopup();
            RefreshMaxBadge();
        }

        public void PlayUpgrade(int level)
        {
            VisualLevel = Mathf.Clamp(level, 1, maxLevel);
            ApplyParts();
            RefreshMaxBadge();
            if (VisualLevel <= 1) return;
            punchElapsed = 0f;
            popupElapsed = 0f;
            if (popup != null)
            {
                popup.text = "LV" + VisualLevel;
                popup.color = popupColor;
                popup.transform.localPosition = popupRest;
                popup.transform.localScale = Vector3.one * 0.35f;
                popup.gameObject.SetActive(true);
            }
            ApplyPunch();
            ApplyPopup();
        }

        public void Advance(float deltaTime)
        {
            if (deltaTime < 0f) return;
            if (IsPunching)
            {
                punchElapsed += deltaTime;
                ApplyPunch();
                if (!IsPunching)
                {
                    punchElapsed = -1f;
                    ResetPunch();
                }
            }
            if (IsPopupPlaying)
            {
                popupElapsed += deltaTime;
                ApplyPopup();
                if (!IsPopupPlaying)
                {
                    popupElapsed = -1f;
                    HidePopup();
                }
            }
            FaceCamera();
        }

        void RefreshMaxBadge()
        {
            if (maxBadge == null) return;
            maxBadge.gameObject.SetActive(!hidePersistentMax && VisualLevel >= maxLevel);
        }

        void ApplyParts()
        {
            if (extras == null) return;
            for (int i = 0; i < extras.Length; i++)
                if (extras[i] != null) extras[i].gameObject.SetActive(i < VisualLevel - 1);
        }

        void ApplyPunch()
        {
            if (station == null) return;
            float t = Mathf.Clamp01(punchElapsed / PunchDuration);
            float wave = Mathf.Sin(t * Mathf.PI);
            station.localScale = restScale * (1f + 0.16f * wave);
            float shake = (1f - t) * 0.045f * Mathf.Sin(punchElapsed * 52f);
            station.localPosition = restPosition + new Vector3(shake, 0f, -shake * 0.4f);
        }

        void ApplyPopup()
        {
            if (popup == null) return;
            float t = Mathf.Clamp01(popupElapsed / PopupDuration);
            float rise = t < 0.35f
                ? Mathf.Sin(t / 0.35f * Mathf.PI * 0.5f)
                : 1f - ((t - 0.35f) / 0.65f);
            float scale = t < 0.18f
                ? Mathf.Lerp(0.35f, 1.28f, t / 0.18f)
                : Mathf.Lerp(1.28f, 1f, Mathf.Clamp01((t - 0.18f) / 0.22f));
            popup.transform.localPosition = popupRest + Vector3.up * (0.55f * Mathf.Max(0f, rise));
            popup.transform.localScale = Vector3.one * scale;
            Color color = popupColor;
            color.a = t > 0.72f ? Mathf.Clamp01(1f - (t - 0.72f) / 0.28f) : 1f;
            popup.color = color;
        }

        void ResetPunch()
        {
            if (station == null) return;
            station.localScale = restScale;
            station.localPosition = restPosition;
        }

        void HidePopup()
        {
            if (popup == null) return;
            popup.gameObject.SetActive(false);
            popup.transform.localPosition = popupRest;
            popup.transform.localScale = Vector3.one;
            popup.color = popupColor;
        }

        void FaceCamera()
        {
            if (Camera.main == null) return;
            if (popup != null && popup.gameObject.activeSelf)
                popup.transform.rotation = Camera.main.transform.rotation;
            if (maxBadge != null && maxBadge.gameObject.activeSelf)
                maxBadge.transform.rotation = Camera.main.transform.rotation;
        }

        static TextMesh FindOrCreateLabel(Transform parent, string name, Vector3 localPosition, Color color,
            int fontSize, float characterSize)
        {
            Transform existing = parent.Find(name);
            TextMesh label = existing != null ? existing.GetComponent<TextMesh>() : null;
            if (label == null)
            {
                GameObject go = new GameObject(name);
                go.transform.SetParent(parent, false);
                label = go.AddComponent<TextMesh>();
            }
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
