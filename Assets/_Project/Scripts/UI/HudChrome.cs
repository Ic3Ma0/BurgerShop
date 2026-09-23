using UnityEngine;
using UnityEngine.UI;

namespace BurgerShop.UI
{
    public static class HudChrome
    {
        public const float JoystickPad = 24f;
        public static readonly Vector2 JoystickPosition = new Vector2(220f, 240f);
        public static readonly Vector2 JoystickSize = new Vector2(280f, 280f);

        static Font font;
        static Font chineseFont;
        public static Font ChineseFont()
        {
            if(chineseFont==null)chineseFont=Resources.Load<Font>("Fonts/NotoSansCJKsc-Regular");
            return chineseFont!=null?chineseFont:Font();
        }
        static Sprite circle;
        static Sprite rounded;
        static Sprite star;
        static Sprite bill;
        static Sprite check;
        static Sprite gear;

        public static readonly Color Cream = new Color32(255,245,230,255);
        public static readonly Color Ink = new Color32(62,43,37,255);
        public static readonly Color Tomato = new Color32(217,75,61,255);
        public static readonly Color Gold = new Color32(255,200,87,255);
        public static readonly Color Green = new Color32(38,132,91,255);
        public static readonly Color HeaderBlue = Cream;
        public static readonly Color TrackNavy = new Color32(232,216,195,255);
        public static readonly Color FillGreen = Green;
        public static readonly Color CapsuleIdle = Cream;
        public static readonly Color CapsuleDone = Green;
        public static readonly Color TitleIdle = Ink;
        public static readonly Color CoinGreen = BurgerShop.Core.BanknoteLook.Green;

        public static Font Font()
        {
            if (font == null) font = Resources.Load<Font>("Fonts/NotoSansCJKsc-Regular") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font;
        }

        public static Sprite Circle()
        {
            if (circle == null) circle = MakeCircle(64);
            return circle;
        }

        public static Sprite Rounded()
        {
            if (rounded == null) rounded = MakeRounded(64, 24f);
            return rounded;
        }

        public static Sprite Star()
        {
            if (star == null) star = MakeStar(64);
            return star;
        }

        public static Sprite Bill()
        {
            if (bill == null) bill = MakeBill(64, 40);
            return bill;
        }

        public static Sprite Check()
        {
            if (check == null) check = MakeCheck(64);
            return check;
        }

        public static Sprite Gear()
        {
            if (gear == null) gear = MakeGear(64);
            return gear;
        }

        public static Rect JoystickKeepout()
        {
            float halfW = JoystickSize.x * 0.5f;
            float halfH = JoystickSize.y * 0.5f;
            return Rect.MinMaxRect(
                JoystickPosition.x - halfW - JoystickPad,
                JoystickPosition.y - halfH - JoystickPad,
                JoystickPosition.x + halfW + JoystickPad,
                JoystickPosition.y + halfH + JoystickPad);
        }

        public static Rect LocalRect(RectTransform target, RectTransform space)
        {
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);
            var spaceCorners = new Vector3[4];
            space.GetWorldCorners(spaceCorners);
            Vector3 origin = spaceCorners[0];
            Vector3 xAxis = spaceCorners[3] - origin;
            Vector3 yAxis = spaceCorners[1] - origin;
            float width = Mathf.Max(xAxis.magnitude, 0.0001f);
            float height = Mathf.Max(yAxis.magnitude, 0.0001f);
            Vector3 xN = xAxis / width;
            Vector3 yN = yAxis / height;
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;
            for (int i = 0; i < 4; i++)
            {
                Vector3 delta = corners[i] - origin;
                float x = Vector3.Dot(delta, xN);
                float y = Vector3.Dot(delta, yN);
                minX = Mathf.Min(minX, x);
                minY = Mathf.Min(minY, y);
                maxX = Mathf.Max(maxX, x);
                maxY = Mathf.Max(maxY, y);
            }
            return Rect.MinMaxRect(minX, minY, maxX, maxY);
        }

        public static bool OverlapsJoystickKeepout(RectTransform target, RectTransform space)
        {
            Rect keepout = JoystickKeepout();
            Rect local = LocalRect(target, space);
            return local.xMin < keepout.xMax && local.xMax > keepout.xMin &&
                   local.yMin < keepout.yMax && local.yMax > keepout.yMin;
        }

        public static void Mute(Graphic graphic)
        {
            if (graphic != null) graphic.raycastTarget = false;
        }

        public static void HideFromDefaultScreen(GameObject hud)
        {
            if (hud == null) return;
            CanvasGroup group = hud.GetComponent<CanvasGroup>();
            if (group == null) group = hud.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
        }

        public static void Style(Text text, int size, Color color, TextAnchor align, bool bold, bool darkOutline)
        {
            text.font = Font();
            text.fontSize = Mathf.Clamp(size,14,44);
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Min(16,text.fontSize);
            text.resizeTextMaxSize = text.fontSize;
            text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            text.alignment = align;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            Outline outline = text.GetComponent<Outline>();
            if (outline != null) outline.enabled = false;
            Shadow shadow = text.GetComponent<Shadow>();
            if (shadow != null) shadow.enabled = false;
        }

        public static Image Panel(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, Color color, float round = 1f)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            Image image = go.GetComponent<Image>();
            image.sprite = Rounded();
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1f;
            if(size.y>=56f && color.a>0f)
            {
                var shadow=go.AddComponent<Shadow>();
                shadow.effectColor=new Color(Ink.r,Ink.g,Ink.b,.16f);
                shadow.effectDistance=new Vector2(0,-4);
                shadow.useGraphicAlpha=true;
            }
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static Image Icon(Transform parent, string name, Sprite sprite, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            Image image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        public static Text Label(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size,
            int fontSize, Color color, TextAnchor align, bool bold, bool darkOutline)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(LocalizedText));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            Text text = go.GetComponent<Text>();
            Style(text, fontSize, color, align, bold, darkOutline);
            return text;
        }

        public static void SetHorizontalFill(RectTransform fill, float amount)
        {
            if (fill == null || fill.parent == null) return;
            float width = ((RectTransform)fill.parent).rect.width * Mathf.Clamp01(amount);
            fill.anchorMin = new Vector2(0f, 0f);
            fill.anchorMax = new Vector2(0f, 1f);
            fill.pivot = new Vector2(0f, 0.5f);
            fill.sizeDelta = new Vector2(Mathf.Max(0f, width), 0f);
            fill.anchoredPosition = Vector2.zero;
        }

        public static Image BuildTopBand(Transform parent)
        {
            var go = new GameObject("TopChrome", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, 92f);
            Image image = go.GetComponent<Image>();
            image.color = Color.clear;
            image.raycastTarget = false;
            return image;
        }

        static Sprite MakeCircle(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            float center = (size - 1) * 0.5f;
            float radius = center - 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) - radius;
                    float alpha = Mathf.Clamp01(0.5f - d);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 64f, 0, SpriteMeshType.FullRect);
        }

        static Sprite MakeRounded(int size, float radius)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            float center = (size - 1) * 0.5f;
            float hw = center;
            float hh = center;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x - center;
                    float py = y - center;
                    float qx = Mathf.Abs(px) - hw + radius;
                    float qy = Mathf.Abs(py) - hh + radius;
                    float outside = Mathf.Min(Mathf.Max(qx, qy), 0f) +
                                    Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f)) - radius;
                    float alpha = Mathf.Clamp01(0.5f - outside);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            texture.Apply(false, false);
            float border = radius;
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        }

        static Sprite MakeStar(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            var points = new Vector2[10];
            float cx = (size - 1) * 0.5f;
            float cy = cx;
            float outer = cx - 2f;
            float inner = outer * 0.42f;
            for (int i = 0; i < points.Length; i++)
            {
                float radius = (i & 1) == 0 ? outer : inner;
                float angle = Mathf.PI * 0.5f + i * Mathf.PI / 5f;
                points[i] = new Vector2(cx + Mathf.Cos(angle) * radius, cy + Mathf.Sin(angle) * radius);
            }
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float alpha = PointInPolygon(new Vector2(x + 0.5f, y + 0.5f), points) ? 1f : 0f;
                    if (alpha < 1f)
                    {
                        float edge = 0f;
                        for (int i = 0; i < points.Length; i++)
                        {
                            Vector2 a = points[i];
                            Vector2 b = points[(i + 1) % points.Length];
                            edge = Mathf.Max(edge, 1f - DistanceToSegment(new Vector2(x + 0.5f, y + 0.5f), a, b));
                        }
                        alpha = Mathf.Clamp01(edge);
                    }
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 64f, 0, SpriteMeshType.FullRect);
        }

        static Sprite MakeBill(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            float cx = (width - 1) * 0.5f;
            float cy = (height - 1) * 0.5f;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float nx = (Mathf.Abs(x - cx) - (cx - 6f)) / 6f;
                    float ny = (Mathf.Abs(y - cy) - (cy - 6f)) / 6f;
                    float round = Mathf.Max(nx, ny);
                    if (round > 1f)
                    {
                        texture.SetPixel(x, y, Color.clear);
                        continue;
                    }
                    Color color = new Color(0.38f, 0.86f, 0.48f, 1f);
                    if (x > 8 && x < width - 8 && y > 6 && y < height - 6)
                        color = new Color(0.55f, 0.94f, 0.62f, 1f);
                    float dx = x - cx;
                    float dy = y - cy;
                    if (dx * dx + dy * dy < 36f) color = Color.white;
                    if (dx * dx + dy * dy < 16f) color = new Color(0.22f, 0.72f, 0.38f, 1f);
                    texture.SetPixel(x, y, color);
                }
            }
            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 64f, 0, SpriteMeshType.FullRect);
        }

        static Sprite MakeGear(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            float cx = (size - 1) * 0.5f;
            float cy = cx;
            const int teeth = 8;
            float hole = size * 0.14f;
            float hub = size * 0.28f;
            float rim = size * 0.34f;
            float outer = size * 0.46f;
            float step = Mathf.PI * 2f / teeth;
            float halfTooth = step * 0.28f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float angle = Mathf.Atan2(dy, dx);
                    if (angle < 0f) angle += Mathf.PI * 2f;
                    float sector = angle % step;
                    float toCenter = Mathf.Min(sector, step - sector);
                    bool inTooth = r <= outer && r >= rim && toCenter <= halfTooth;
                    bool inRim = r <= rim && r >= hole;
                    bool inHub = r <= hub && r >= hole;
                    float alpha = (inTooth || inRim || inHub) ? Mathf.Clamp01(outer + 0.6f - r) : 0f;
                    if (r < hole - 0.5f) alpha = 0f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 64f, 0, SpriteMeshType.FullRect);
        }

        static Sprite MakeCheck(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            Vector2 a = new Vector2(size * 0.22f, size * 0.52f);
            Vector2 b = new Vector2(size * 0.42f, size * 0.28f);
            Vector2 c = new Vector2(size * 0.78f, size * 0.72f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    float d = Mathf.Min(DistanceToSegment(p, a, b), DistanceToSegment(p, b, c));
                    float alpha = Mathf.Clamp01(5.2f - d);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 64f, 0, SpriteMeshType.FullRect);
        }

        static bool PointInPolygon(Vector2 point, Vector2[] polygon)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                if ((polygon[i].y > point.y) != (polygon[j].y > point.y) &&
                    point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x)
                    inside = !inside;
            }
            return inside;
        }

        static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float denom = Vector2.Dot(ab, ab);
            float t = denom <= 0.0001f ? 0f : Mathf.Clamp01(Vector2.Dot(point - a, ab) / denom);
            return Vector2.Distance(point, a + ab * t);
        }
    }
}
