using BurgerShop.Core;
using BurgerShop.Building;
using UnityEngine;
using UnityEngine.UI;

namespace BurgerShop.UI
{
    public sealed class TaskCapsuleHud : MonoBehaviour
    {
        const int SparkCount = 6;
        SessionGoalTracker tracker;
        Text title;
        Text detail;
        GameObject otherInvestments;
        Text progress;
        Image background;
        Image iconBadge;
        Image checkIcon;
        Image glyphIcon;
        RectTransform fillRect;
        RectTransform self;
        Spark[] sparks;
        string shownTitle;
        int shownProgress = int.MinValue;
        bool shownCelebrate;
        float punch;
        float displayedFill, fillFrom, fillTarget, fillAge=.6f;
        readonly NumberPunch progressPunch = new NumberPunch();

        public bool IsProgressPunching => progressPunch.IsActive;
        public float ProgressPunchScale => progressPunch.Scale;

        public static readonly Vector2 LayoutAnchor = new Vector2(0f, 1f);
        public static readonly Vector2 LayoutPosition = new Vector2(32f, -106f);
        public static readonly Vector2 LayoutSize = new Vector2(350f, 180f);
        public const float ProgressHairline = 3f;

        struct Spark
        {
            public RectTransform Rect;
            public CanvasGroup Group;
            public Vector2 Velocity;
            public float Spin;
            public float Life;
            public float MaxLife;
        }

        public static TaskCapsuleHud Build(Transform parent, SessionGoalTracker goals)
        {
            Image back = HudChrome.Panel(parent, "TaskCapsule", LayoutAnchor, LayoutAnchor,
                LayoutPosition, LayoutSize, HudChrome.CapsuleIdle, 0.85f);
            Image badge = HudChrome.Icon(back.transform, "TaskBadge", HudChrome.Circle(), new Vector2(0f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(16f, 2f), new Vector2(22f, 22f), new Color(0.20f, 0.78f, 0.48f, 1f));
            Image check = HudChrome.Icon(badge.transform, "TaskCheck", HudChrome.Check(), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(12f, 12f), Color.white);
            Image glyph = HudChrome.Icon(badge.transform, "TaskGlyph", HudChrome.Star(), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(12f, 12f), Color.white);

            Text title = HudChrome.Label(back.transform, "TaskTitle", new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero, 20, HudChrome.TitleIdle, TextAnchor.MiddleLeft, true, false);
            title.fontSize = 20;
            title.rectTransform.offsetMin = new Vector2(32f, 7f);
            title.rectTransform.offsetMax = new Vector2(-50f, -3f);
            title.horizontalOverflow = HorizontalWrapMode.Overflow;
            title.verticalOverflow = VerticalWrapMode.Truncate;
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 16;
            title.resizeTextMaxSize = 20;

            Image barBack = Hairline(back.transform, "TaskBarBack", new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(32f, 4f), new Vector2(244f, ProgressHairline), new Color(0.82f, 0.90f, 0.86f, 0.9f));
            Image fill = Hairline(barBack.transform, "TaskBarFill", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                Vector2.zero, new Vector2(40f, ProgressHairline), HudChrome.FillGreen);

            Text progress = HudChrome.Label(back.transform, "TaskProgress", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f), new Vector2(-6f, 2f), new Vector2(48f, 24f), 18,
                new Color(0.18f, 0.42f, 0.28f, 1f), TextAnchor.MiddleRight, true, false);
            progress.fontSize = 18;

            var hud = back.gameObject.AddComponent<TaskCapsuleHud>();
            back.rectTransform.sizeDelta=LayoutSize;
            badge.rectTransform.anchorMin=badge.rectTransform.anchorMax=new Vector2(0,1);
            badge.rectTransform.anchoredPosition=new Vector2(16,-24);
            progress.rectTransform.anchorMin=progress.rectTransform.anchorMax=new Vector2(1,0);
            progress.rectTransform.anchoredPosition=new Vector2(-10,15);
            title.rectTransform.offsetMin=new Vector2(32,139);
            title.rectTransform.offsetMax=new Vector2(-12,-4);
            title.horizontalOverflow=HorizontalWrapMode.Wrap;
            hud.detail=HudChrome.Label(back.transform,"InvestmentDetail",Vector2.zero,Vector2.one,new Vector2(0,.5f),Vector2.zero,Vector2.zero,17,HudChrome.Ink,TextAnchor.UpperLeft,false,false);
            hud.detail.rectTransform.offsetMin=new Vector2(16,38);hud.detail.rectTransform.offsetMax=new Vector2(-16,-44);
            OpeningHudCopy.Style(title,20,true);OpeningHudCopy.Style(hud.detail,19,true);
            back.gameObject.AddComponent<RectMask2D>();
            back.raycastTarget=true;
            var click=back.gameObject.AddComponent<Button>();click.targetGraphic=back;
            click.onClick.AddListener(()=>{
                if(goals.CanUpgrade)return;
                if(goals.TeachingPrerequisite!=null){if(goals.Rank==3&&BurgerShop.Restaurant.MainHallExpansion.HasAccess)return;var shop=Object.FindFirstObjectByType<FacilityShopHud>();if(shop!=null){if(!shop.IsOpen)shop.Open();if(!BurgerShop.Restaurant.MainHallExpansion.HasAccess)shop.ShowExpansions();}return;}
                if(!goals.ShowsInvestment)return;
                var offer=goals.Investments?.Current;
                if(offer?.Facility!=null)FacilityDetailsHud.Current?.Open(offer.Facility);
                else if(offer?.ShopPurchase==true)Object.FindFirstObjectByType<FacilityShopHud>()?.Open();
                else if(offer?.Id=="main-hall"){var shop=Object.FindFirstObjectByType<FacilityShopHud>();if(shop!=null){shop.Open();shop.ShowExpansions();}}
            });
            var other=HudChrome.Panel(back.transform,"OtherInvestments",new Vector2(1,0),new Vector2(1,0),new Vector2(-72,7),new Vector2(130,24),HudChrome.Cream);
            hud.otherInvestments=other.gameObject;
            other.raycastTarget=true;var otherButton=other.gameObject.AddComponent<Button>();otherButton.targetGraphic=other;
            var otherLabel=HudChrome.Label(other.transform,"Text",Vector2.zero,Vector2.one,new Vector2(.5f,.5f),Vector2.zero,Vector2.zero,16,HudChrome.Ink,TextAnchor.MiddleCenter,false,false);otherLabel.text="换个投资";OpeningHudCopy.Style(otherLabel,16);
            otherButton.onClick.AddListener(()=>goals.Investments?.Next());
            hud.BuildSparks(back.transform);
            hud.Configure(goals, title, progress, fill, back, null, badge, check, glyph);
            return hud;
        }

        static Image Hairline(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            Image image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public void Configure(SessionGoalTracker goals, Text titleText, Text progressText, Image bar, Image back, CanvasGroup celebration)
        {
            Configure(goals, titleText, progressText, bar, back, celebration, null, null, null);
        }

        public void Configure(SessionGoalTracker goals, Text titleText, Text progressText, Image bar, Image back,
            CanvasGroup celebration, Image badge, Image check, Image glyph)
        {
            tracker = goals;
            title = titleText;
            progress = progressText;
            background = back;
            iconBadge = badge;
            checkIcon = check;
            glyphIcon = glyph;
            fillRect = bar != null ? bar.rectTransform : null;
            self = (RectTransform)transform;
            HudChrome.Mute(bar);
            // The capsule opens the existing facility detail sheet.
            shownTitle = null;
            shownProgress = int.MinValue;
            Refresh(true);
            _ = celebration;
        }

        public void RefreshNow() => Advance(0f);

        void LateUpdate() => Advance(Time.deltaTime);

        public void Advance(float deltaTime)
        {
            Refresh(false);
            float dt=Mathf.Max(0f,deltaTime);
            fillAge+=dt;displayedFill=Mathf.Lerp(fillFrom,fillTarget,Mathf.Clamp01(fillAge/.6f));
            if(fillRect!=null)HudChrome.SetHorizontalFill(fillRect,displayedFill);
            TickSparks(dt);
        }

        void Refresh(bool force)
        {
            if (tracker == null || title == null) return;
            if(otherInvestments!=null)otherInvestments.SetActive(tracker.ShowsInvestment);
            if(detail!=null)detail.text=OpeningHudCopy.Detail(tracker);
            string copy = OpeningHudCopy.Title(tracker);
            int prog = tracker.CapsuleProgress;
            int need = tracker.CapsuleRequired;
            bool celebrating = tracker.IsCelebrating && (tracker.Opening == null || !tracker.Opening.IsActive);
            bool changed = force || shownTitle != copy || shownProgress != prog || shownCelebrate != celebrating;
            if (!changed)
            {
                return;
            }

            bool justFinished = !force && celebrating && !shownCelebrate;
            bool progressUp = !force && prog > shownProgress && shownProgress != int.MinValue;
            shownTitle = copy;
            shownProgress = prog;
            shownCelebrate = celebrating;
            if (progressUp) progressPunch.Play();
            title.text = copy;
            if (progress != null) progress.text = $"{prog}/{need}";
            fillTarget=need<=0?0f:(float)prog/need;
            fillFrom=displayedFill;fillAge=justFinished?0f:.6f;
            if(!justFinished)displayedFill=fillTarget;
            if(fillRect!=null)HudChrome.SetHorizontalFill(fillRect,displayedFill);

            bool done = celebrating;
            if(detail!=null)detail.color=done?Color.white:HudChrome.Ink;
            if (background != null) background.color = done ? HudChrome.CapsuleDone : HudChrome.CapsuleIdle;
            if (title != null)
            {
                title.color = done ? Color.white : HudChrome.TitleIdle;
                Outline outline = title.GetComponent<Outline>();
                if (outline != null)
                    outline.effectColor = done ? new Color(0.05f, 0.22f, 0.12f, 0.7f) : new Color(1f, 1f, 1f, 0.28f);
            }
            if (progress != null) progress.color = done ? Color.white : new Color(0.18f, 0.42f, 0.28f, 1f);
            if (iconBadge != null)
                iconBadge.color = done ? new Color(0.12f, 0.62f, 0.36f, 1f) : IconColor(copy);
            if (checkIcon != null) checkIcon.enabled = done;
            if (glyphIcon != null) glyphIcon.enabled = !done;
            if (justFinished && (FeedbackDirector.Current==null||FeedbackDirector.Current.DecorationsEnabled))
            {
                FeedbackDirector.Current?.RequestSound(FeedbackSound.Task);
                punch = 1f;
                Burst();
            }
        }

        void TickSparks(float dt)
        {
            progressPunch.Advance(dt);
            if (progress != null)
                progress.rectTransform.localScale = new Vector3(progressPunch.Scale, progressPunch.Scale, 1f);
            if (self != null)
            {
                punch = Mathf.MoveTowards(punch, 0f, dt * 2.4f);
                float scale = 1f + 0.04f * punch;
                self.localScale = new Vector3(scale, scale, 1f);
            }
            if (sparks == null) return;
            bool celebrating = tracker != null && tracker.IsCelebrating && (tracker.Opening == null || !tracker.Opening.IsActive);
            for (int i = 0; i < sparks.Length; i++)
            {
                Spark spark = sparks[i];
                if (spark.Rect == null) continue;
                spark.Life -= dt;
                if (!celebrating || spark.Life <= 0f)
                {
                    if (spark.Group != null) spark.Group.alpha = 0f;
                    sparks[i] = spark;
                    continue;
                }
                spark.Rect.anchoredPosition += spark.Velocity * dt;
                spark.Rect.Rotate(0f, 0f, spark.Spin * dt);
                if (spark.Group != null) spark.Group.alpha = Mathf.Clamp01(spark.Life / spark.MaxLife);
                sparks[i] = spark;
            }
        }

        void Burst()
        {
            if (sparks == null) return;
            for (int i = 0; i < sparks.Length; i++)
            {
                Spark spark = sparks[i];
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float speed = Random.Range(90f, 240f);
                spark.Velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
                spark.Spin = Random.Range(-220f, 220f);
                spark.MaxLife = spark.Life = 0.6f;
                spark.Rect.anchoredPosition = new Vector2(Random.Range(-24f, 24f), Random.Range(-8f, 10f));
                spark.Rect.localScale = Vector3.one * Random.Range(0.45f, 0.9f);
                if (spark.Group != null) spark.Group.alpha = 1f;
                sparks[i] = spark;
            }
        }

        void BuildSparks(Transform parent)
        {
            sparks = new Spark[SparkCount];
            Color[] colors =
            {
                Color.white,
                new Color(1f, 0.92f, 0.35f),
                new Color(0.45f, 1f, 0.62f),
                new Color(0.55f, 0.9f, 1f),
                new Color(1f, 0.55f, 0.82f),
                new Color(0.7f, 1f, 0.45f)
            };
            for (int i = 0; i < SparkCount; i++)
            {
                Image image = HudChrome.Icon(parent, "TaskSpark_" + i, HudChrome.Star(), new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10f, 10f), colors[i % colors.Length]);
                var group = image.gameObject.AddComponent<CanvasGroup>();
                group.alpha = 0f;
                group.blocksRaycasts = false;
                group.interactable = false;
                sparks[i] = new Spark { Rect = image.rectTransform, Group = group };
            }
        }

        static Color IconColor(string taskTitle)
        {
            if (taskTitle != null && taskTitle.StartsWith("Install")) return new Color(0.22f, 0.78f, 0.48f, 1f);
            if (taskTitle != null && taskTitle.StartsWith("Pick")) return new Color(0.96f, 0.62f, 0.22f, 1f);
            if (taskTitle != null && taskTitle.StartsWith("Move")) return new Color(0.28f, 0.62f, 0.94f, 1f);
            if (taskTitle != null && taskTitle.StartsWith("Serve")) return new Color(0.94f, 0.42f, 0.38f, 1f);
            if (taskTitle != null && taskTitle.StartsWith("Upgrade")) return new Color(0.60f, 0.36f, 0.90f, 1f);
            if (taskTitle != null && taskTitle.StartsWith("Tap")) return new Color(1f, 0.84f, 0.16f, 1f);
            if (taskTitle != null && taskTitle.StartsWith("Clear")) return new Color(0.86f, 0.55f, 0.28f, 1f);
            if (taskTitle != null && taskTitle.StartsWith("Take trash")) return new Color(0.55f, 0.48f, 0.42f, 1f);
            if (taskTitle != null && taskTitle.StartsWith("Hire")) return new Color(0.18f, 0.78f, 0.88f, 1f);
            return new Color(0.45f, 0.55f, 0.62f, 1f);
        }
    }
}
