using BurgerShop.Restaurant;
using UnityEngine;
using UnityEngine.UI;

namespace BurgerShop.UI
{
    // Pulses the existing purple grill pad, then the rank HUD. Does not teach earning.
    // After DirectInteraction LateUpdate so the 058 pad hide cannot win the same frame.
    [DefaultExecutionOrder(100)]
    public sealed class UpgradeGuide : MonoBehaviour
    {
        static readonly Color RankPulse = new Color(0.60f, 0.36f, 0.90f, 1f);
        SessionGoalTracker goals;
        GrillUpgradeZone grill;
        StarProgressHud stars;
        Image starPanel;
        Color starIdle;
        Vector3 padIdle;
        bool padIdleSet;
        public bool HighlightsGrill { get; private set; }
        public bool HighlightsRankHud { get; private set; }

        public static UpgradeGuide Build(Transform hudParent, SessionGoalTracker tracker, GrillUpgradeZone zone,
            StarProgressHud starHud = null)
        {
            _ = hudParent;
            var host = zone != null ? zone.transform : tracker != null ? tracker.transform : null;
            var root = new GameObject("UpgradeGuide").transform;
            if (host != null) root.SetParent(host, false);
            var guide = root.gameObject.AddComponent<UpgradeGuide>();
            guide.goals = tracker;
            guide.grill = zone;
            guide.stars = starHud;
            if (starHud != null) guide.starPanel = starHud.GetComponent<Image>();
            if (guide.starPanel != null) guide.starIdle = guide.starPanel.color;
            guide.Refresh();
            return guide;
        }

        void LateUpdate() => Refresh();

        public void Refresh()
        {
            var step = goals != null && goals.Opening != null ? goals.Opening.Current : OpeningGuide.Step.Done;
            HighlightsGrill = step == OpeningGuide.Step.Upgrade;
            HighlightsRankHud = step == OpeningGuide.Step.RankUp;
            grill?.SetOpeningHighlight(HighlightsGrill);
            var pad = grill != null ? grill.Pad : null;
            if (pad != null)
            {
                if (!padIdleSet) { padIdle = pad.localScale; padIdleSet = true; }
                if (HighlightsGrill)
                {
                    float pulse = 1.04f + 0.08f * Mathf.Sin(Time.unscaledTime * 4f);
                    pad.localScale = new Vector3(padIdle.x * pulse, padIdle.y, padIdle.z * pulse);
                }
                else pad.localScale = padIdle;
            }
            if (starPanel != null)
            {
                if (HighlightsRankHud)
                {
                    float blend = 0.42f + 0.18f * Mathf.Sin(Time.unscaledTime * 4f);
                    starPanel.color = Color.Lerp(starIdle, RankPulse, blend);
                }
                else starPanel.color = starIdle;
            }
        }
    }
}
