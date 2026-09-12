using BurgerShop.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BurgerShop.UI
{
    public sealed class TaskCapsuleHud : MonoBehaviour
    {
        const int SparkCount = 14;
        SessionGoalTracker tracker;
        Text title;
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
        readonly NumberPunch progressPunch = new NumberPunch();

        public bool IsProgressPunching => progressPunch.IsActive;
        public float ProgressPunchScale => progressPunch.Scale;

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
            Image back = HudChrome.Panel(parent, "TaskCapsule", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -108f), new Vector2(620f, 104f), HudChrome.CapsuleIdle, 0.62f);
            var shadow = back.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.05f, 0.18f, 0.14f, 0.28f);
            shadow.effectDistance = new Vector2(0f, -6f);

            Image badge = HudChrome.Icon(back.transform, "TaskBadge", HudChrome.Circle(), new Vector2(0f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(52f, 4f), new Vector2(72f, 72f), new Color(0.20f, 0.78f, 0.48f, 1f));
            Image check = HudChrome.Icon(badge.transform, "TaskCheck", HudChrome.Check(), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40f, 40f), Color.white);
            Image glyph = HudChrome.Icon(badge.transform, "TaskGlyph", HudChrome.Star(), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(34f, 34f), Color.white);

            Text title = HudChrome.Label(back.transform, "TaskTitle", new Vector2(0f, 0.42f), new Vector2(1f, 1f),
                new Vector2(0f, 1f), Vector2.zero, Vector2.zero, 28, HudChrome.TitleIdle, TextAnchor.MiddleLeft, true, false);
            title.rectTransform.offsetMin = new Vector2(100f, 0f);
            title.rectTransform.offsetMax = new Vector2(-18f, -8f);
            title.horizontalOverflow = HorizontalWrapMode.Wrap;

            Image barBack = HudChrome.Panel(back.transform, "TaskBarBack", new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(100f, 16f), new Vector2(390f, 16f), new Color(0.82f, 0.90f, 0.86f, 1f), 0.45f);
            Image fill = HudChrome.Panel(barBack.transform, "TaskBarFill", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                Vector2.zero, new Vector2(80f, 16f), HudChrome.FillGreen, 0.45f);

            Text progress = HudChrome.Label(back.transform, "TaskProgress", new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(1f, 0f), new Vector2(-18f, 12f), new Vector2(88f, 28f), 22,
                new Color(0.18f, 0.42f, 0.28f, 1f), TextAnchor.MiddleRight, true, false);

            var hud = back.gameObject.AddComponent<TaskCapsuleHud>();
            hud.BuildSparks(back.transform);
            hud.Configure(goals, title, progress, fill, back, null, badge, check, glyph);
            return hud;
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
            HudChrome.Mute(back);
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
            TickSparks(Mathf.Max(0f, deltaTime));
        }

        void Refresh(bool force)
        {
            if (tracker == null || title == null) return;
            bool changed = force || shownTitle != tracker.Title || shownProgress != tracker.Progress || shownCelebrate != tracker.IsCelebrating;
            if (!changed)
            {
                if (fillRect != null)
                    HudChrome.SetHorizontalFill(fillRect, tracker.Required <= 0 ? 0f : (float)tracker.Progress / tracker.Required);
                return;
            }

            bool justFinished = tracker.IsCelebrating && !shownCelebrate;
            bool progressUp = !force && tracker.Progress > shownProgress && shownProgress != int.MinValue;
            shownTitle = tracker.Title;
            shownProgress = tracker.Progress;
            shownCelebrate = tracker.IsCelebrating;
            if (progressUp) progressPunch.Play();
            title.text = tracker.Title;
            if (progress != null) progress.text = $"{tracker.Progress}/{tracker.Required}";
            if (fillRect != null)
                HudChrome.SetHorizontalFill(fillRect, tracker.Required <= 0 ? 0f : (float)tracker.Progress / tracker.Required);

            bool done = tracker.IsCelebrating;
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
                iconBadge.color = done ? new Color(0.12f, 0.62f, 0.36f, 1f) : IconColor(tracker.Title);
            if (checkIcon != null) checkIcon.enabled = done;
            if (glyphIcon != null) glyphIcon.enabled = !done;
            if (justFinished)
            {
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
                float scale = 1f + 0.08f * punch;
                self.localScale = new Vector3(scale, scale, 1f);
            }
            if (sparks == null) return;
            bool celebrating = tracker != null && tracker.IsCelebrating;
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
                spark.MaxLife = spark.Life = Random.Range(0.7f, 1.25f);
                spark.Rect.anchoredPosition = new Vector2(Random.Range(-40f, 40f), Random.Range(-10f, 24f));
                spark.Rect.localScale = Vector3.one * Random.Range(0.55f, 1.15f);
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
                    new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(22f, 22f), colors[i % colors.Length]);
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
            if (taskTitle != null && taskTitle.StartsWith("Clear")) return new Color(0.86f, 0.55f, 0.28f, 1f);
            if (taskTitle != null && taskTitle.StartsWith("Take trash")) return new Color(0.55f, 0.48f, 0.42f, 1f);
            if (taskTitle != null && taskTitle.StartsWith("Hire")) return new Color(0.18f, 0.78f, 0.88f, 1f);
            return new Color(0.45f, 0.55f, 0.62f, 1f);
        }
    }
}
