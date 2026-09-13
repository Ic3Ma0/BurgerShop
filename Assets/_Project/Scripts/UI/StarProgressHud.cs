using BurgerShop.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BurgerShop.UI
{
    public sealed class StarProgressHud : MonoBehaviour
    {
        SessionGoalTracker tracker;
        Text label;
        Text upgradeLabel;
        RectTransform fillRect;
        readonly NumberPunch punch = new NumberPunch();
        int shown = int.MinValue;

        public bool IsPunching => punch.IsActive;
        public float PunchScale => punch.Scale;

        public static StarProgressHud Build(Transform parent, SessionGoalTracker goals)
        {
            Image root = HudChrome.Panel(parent, "StarProgress", new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(32f, -24f), new Vector2(320f, 96f), new Color(0.08f, 0.18f, 0.32f, 0.22f), 0.85f);
            root.color = HudChrome.Cream;

            HudChrome.Icon(root.transform, "StarIcon", HudChrome.Star(), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(40f, 0f), new Vector2(48f, 48f), new Color(1f, 0.84f, 0.16f, 1f));

            Image track = HudChrome.Panel(root.transform, "StarBarBack", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(76f, 0f), new Vector2(220f, 40f), HudChrome.TrackNavy, 0.55f);

            Image fill = HudChrome.Panel(track.transform, "StarBarFill", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                Vector2.zero, new Vector2(80f, 28f), HudChrome.FillGreen, 0.55f);
            fill.rectTransform.anchorMin = new Vector2(0f, 0f);
            fill.rectTransform.anchorMax = new Vector2(0f, 1f);
            fill.rectTransform.pivot = new Vector2(0f, 0.5f);

            Text value = HudChrome.Label(track.transform, "StarValue", Vector2.zero, Vector2.one, new Vector2(1f, 0.5f),
                Vector2.zero, Vector2.zero, 28, HudChrome.Ink, TextAnchor.MiddleRight, true, true);
            var valueRect = value.rectTransform;
            valueRect.offsetMin = new Vector2(8f, 0f);
            valueRect.offsetMax = new Vector2(-10f, 0f);

            var hud = root.gameObject.AddComponent<StarProgressHud>();
            hud.upgradeLabel=HudChrome.Label(root.transform,"RankAction",new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,2),new Vector2(260,26),24,HudChrome.Tomato,TextAnchor.MiddleCenter,true,false);
            hud.Configure(goals, value, fill);
            return hud;
        }

        public void Configure(SessionGoalTracker goals, Text text, Image bar)
        {
            tracker = goals;
            label = text;
            fillRect = bar != null ? bar.rectTransform : null;
            HudChrome.Mute(bar);
            if (label != null) label.raycastTarget = false;
            shown = int.MinValue;
            Advance(0f);
        }

        public void RefreshNow() => Advance(0f);

        void LateUpdate() => Advance(Time.deltaTime);

        public void Advance(float deltaTime)
        {
            if (tracker == null || label == null) return;
            if (shown == int.MinValue)
                shown = tracker.Stars;
            else if (shown != tracker.Stars)
            {
                if (tracker.Stars > shown) punch.Play();
                shown = tracker.Stars;
            }
            label.text = tracker.StarLabel;
            if(upgradeLabel!=null)upgradeLabel.text=tracker.CanUpgrade?"Upgrade":"";
            punch.Advance(deltaTime);
            label.rectTransform.localScale = new Vector3(punch.Scale, punch.Scale, 1f);
            if (fillRect != null)
            {
                // Completed requirements switch to an actionable status; never fake 99%.
                fillRect.gameObject.SetActive(tracker.ProgressFraction<1f);
                HudChrome.SetHorizontalFill(fillRect, tracker.ProgressFraction);
            }
        }
    }
}
