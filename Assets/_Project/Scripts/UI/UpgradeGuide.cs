using BurgerShop.Restaurant;
using UnityEngine;
using UnityEngine.UI;

namespace BurgerShop.UI
{
    // Shows one physical next-action marker or pulses the ready rank HUD.
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
        Transform pointer;
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
            var offer=goals?.Investments?.Current;
            HighlightsGrill = goals!=null&&goals.ShowsInvestment&&offer!=null&&grill!=null&&Vector3.Distance(offer.Target,grill.UpgradePosition)<.2f;
            if(pointer==null){var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);go.name="NextActionMarker";go.GetComponent<Collider>().enabled=false;BurgerVisual.Release(go.GetComponent<Collider>());pointer=go.transform;pointer.SetParent(transform,false);pointer.localScale=new Vector3(1.6f,.012f,1.6f);var material=Core.RuntimeMaterials.Create(new Color(.75f,.6f,.15f));go.GetComponent<Renderer>().sharedMaterial=material;go.AddComponent<BurgerVisual>().OwnMaterials(material);}
            bool show=goals!=null&&!goals.CanUpgrade&&(goals.Rank==1||goals.ShowsInvestment||(goals.TeachingPrerequisite!=null&&goals.Rank==3));
            pointer.gameObject.SetActive(show);
            if(show){Vector3 point=goals.Rank==1?goals.Opening.Target:goals.TeachingPrerequisite!=null?(MainHallExpansion.HasAccess?ShopLayout.HrHirePoint:MainHallExpansion.PurchasePoint):offer!=null?offer.Target:ShopLayout.BoostPoint;point.y=.03f;pointer.position=point;}

            HighlightsRankHud = goals!=null&&goals.CanUpgrade;
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
