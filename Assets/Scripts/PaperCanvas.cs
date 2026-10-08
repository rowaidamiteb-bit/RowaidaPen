using System.Collections.Generic;
using UnityEngine;

// ورقة نرسم عليها بالرصاص: رسومات صغيرة، تظليل، لطخات رصاص، وآثار مساحة
// الإحداثيات بالمتر على سطح الصفحة: x (طول الصفحة) و z (عمقها)، والمركز في النص
public class PaperCanvas
{
    readonly float PixelsPerUnit;
    static readonly Color Graphite = new Color(0.3f, 0.3f, 0.34f);

    readonly int w, h;
    readonly Vector2 size;
    readonly Color[] px;
    readonly System.Random rng;

    readonly LevelTheme theme;
    Color ink = Graphite;   // لون القلم الحالي (رصاص، أو شمع ملون حسب شكل المرحلة)

    public PaperCanvas(Vector2 worldSize, int seed, float pixelsPerUnit = 80f, LevelTheme levelTheme = null, int pageIndex = 0)
    {
        size = worldSize;
        PixelsPerUnit = pixelsPerUnit;
        w = Mathf.Clamp(Mathf.RoundToInt(worldSize.x * PixelsPerUnit), 16, 2048);
        h = Mathf.Clamp(Mathf.RoundToInt(worldSize.y * PixelsPerUnit), 16, 2048);
        px = new Color[w * h];
        rng = new System.Random(seed);
        theme = levelTheme;
        Color paper = theme != null ? theme.PaperFor(pageIndex) : new Color(1f, 0.99f, 0.965f);
        if (theme != null) ink = theme.doodleColor;

        // ورق بحبيبات خفيفة
        float ox = (float)rng.NextDouble() * 100f;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float fibers = Mathf.PerlinNoise(ox + x * 0.08f, y * 0.02f) * 0.5f + Mathf.PerlinNoise(ox + x * 0.3f, y * 0.3f + 50f) * 0.5f;
                float v = 0.945f + fibers * 0.045f - (float)rng.NextDouble() * 0.025f;
                px[y * w + x] = paper * v;
            }

        if (theme != null) DrawPattern(theme.pattern, theme.patternColor);
    }

    // نقشة الورق حسب شكل المرحلة
    void DrawPattern(PaperPattern pattern, Color c)
    {
        float spacing = 0.45f * PixelsPerUnit;
        switch (pattern)
        {
            case PaperPattern.Ruled:
                // خطوط دفتر بالطول، وخط هامش أحمر
                for (float y = spacing * 0.6f; y < h; y += spacing) BandRow((int)y, c, 0.55f);
                for (int y = 0; y < h; y++) for (int dx = 0; dx < 2; dx++) BlendPx((int)(w * 0.06f) + dx, y, new Color(0.95f, 0.45f, 0.45f), 0.6f);
                break;
            case PaperPattern.Grid:
                for (float y = 0; y < h; y += spacing * 0.75f) BandRow((int)y, c, 0.45f);
                for (float x = 0; x < w; x += spacing * 0.75f) for (int y = 0; y < h; y++) BlendPx((int)x, y, c, 0.45f);
                break;
            case PaperPattern.Dots:
                for (float y = spacing * 0.5f; y < h; y += spacing * 0.7f)
                    for (float x = spacing * 0.5f; x < w; x += spacing * 0.7f)
                        for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++) BlendPx((int)x + dx, (int)y + dy, c, 0.6f);
                break;
            case PaperPattern.Kraft:
                // ألياف غامقة متفرقة
                for (int i = 0; i < w * h / 60; i++)
                {
                    int x = rng.Next(w), y = rng.Next(h), len = 2 + rng.Next(6);
                    for (int k = 0; k < len; k++) BlendPx(x + k, y, c, 0.35f);
                }
                break;
            case PaperPattern.Watercolor:
                for (int i = 0; i < Mathf.Max(3, (int)(size.x * 1.2f)); i++)
                {
                    Color blot = Color.Lerp(c, new Color(0.45f, 0.75f, 0.5f), (float)rng.NextDouble());
                    Blot(new Vector2(R(-size.x * 0.5f, size.x * 0.5f), R(-size.y * 0.5f, size.y * 0.5f)), R(0.4f, 1.1f), blot, 0.35f);
                }
                break;
            case PaperPattern.Splats:
                Color[] paints = { new Color(0.95f, 0.4f, 0.45f), new Color(0.4f, 0.65f, 1f), new Color(1f, 0.8f, 0.3f), new Color(0.45f, 0.8f, 0.5f), new Color(0.7f, 0.5f, 0.95f) };
                for (int i = 0; i < Mathf.Max(3, (int)(size.x * 0.9f)); i++)
                {
                    Vector2 p = new Vector2(R(-size.x * 0.5f, size.x * 0.5f), R(-size.y * 0.5f, size.y * 0.5f));
                    Color paint = paints[rng.Next(paints.Length)];
                    Blot(p, R(0.15f, 0.4f), paint, 0.75f);
                    for (int d = 0; d < 5; d++) Blot(p + new Vector2(R(-0.5f, 0.5f), R(-0.4f, 0.4f)), R(0.03f, 0.08f), paint, 0.8f);
                }
                break;
            case PaperPattern.Parchment:
                // بقع قديمة وحواف أغمق
                for (int i = 0; i < Mathf.Max(2, (int)(size.x * 0.6f)); i++)
                    Blot(new Vector2(R(-size.x * 0.5f, size.x * 0.5f), R(-size.y * 0.5f, size.y * 0.5f)), R(0.3f, 0.9f), c, 0.2f);
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        float ex = Mathf.Min(x, w - 1 - x) / (0.3f * PixelsPerUnit), ey = Mathf.Min(y, h - 1 - y) / (0.3f * PixelsPerUnit);
                        float edge = 1f - Mathf.Clamp01(Mathf.Min(ex, ey));
                        BlendPx(x, y, c, edge * 0.35f);
                    }
                break;
            case PaperPattern.Origami:
                // خطوط طي خفيفة بالقطر
                Color crease = new Color(1f, 1f, 1f);
                for (int y = 0; y < h; y++)
                {
                    int x1 = (int)(y * (float)w / h), x2 = w - 1 - x1;
                    for (int d = 0; d < 2; d++) { BlendPx(x1 + d, y, crease, 0.25f); BlendPx(x2 + d, y, crease, 0.25f); }
                }
                break;
        }
    }

    void BandRow(int y, Color c, float a)
    {
        for (int x = 0; x < w; x++) BlendPx(x, y, c, a);
    }

    void BlendPx(int x, int y, Color c, float a)
    {
        if (x < 0 || y < 0 || x >= w || y >= h) return;
        int i = y * w + x;
        px[i] = Color.Lerp(px[i], c, Mathf.Clamp01(a));
    }

    // بقعة لون ناعمة الأطراف
    void Blot(Vector2 center, float radius, Color c, float strength)
    {
        Vector2 cp = ToPixels(center);
        float r = radius * PixelsPerUnit;
        for (int y = Mathf.Max(0, (int)(cp.y - r)); y < Mathf.Min(h, (int)(cp.y + r)); y++)
            for (int x = Mathf.Max(0, (int)(cp.x - r)); x < Mathf.Min(w, (int)(cp.x + r)); x++)
            {
                float n = Mathf.PerlinNoise(x * 0.06f + center.x, y * 0.06f + center.y) * 0.4f;
                float d = Vector2.Distance(new Vector2(x, y), cp) / r + n - 0.2f;
                if (d >= 1f) continue;
                BlendPx(x, y, c, strength * Mathf.Clamp01((1f - d) * 3f));
            }
    }

    public Texture2D ToTexture(string name)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, true) { name = name };
        tex.SetPixels(px);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Trilinear;
        tex.anisoLevel = 8;
        tex.Apply();
        return tex;
    }

    Vector2 ToPixels(Vector2 p)
    {
        return new Vector2((p.x / size.x + 0.5f) * w, (p.y / size.y + 0.5f) * h);
    }

    float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);

    // ---------- أدوات الرسم ----------

    // خط رصاص (عرضه بالبكسل وقوته من 0 لـ 1) مع ملمس رصاص متقطع
    public void Line(Vector2 a, Vector2 b, float width = 1.4f, float strength = 0.75f)
    {
        Vector2 pa = ToPixels(a), pb = ToPixels(b);
        int x0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(pa.x, pb.x) - width - 2));
        int x1 = Mathf.Min(w - 1, Mathf.CeilToInt(Mathf.Max(pa.x, pb.x) + width + 2));
        int y0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(pa.y, pb.y) - width - 2));
        int y1 = Mathf.Min(h - 1, Mathf.CeilToInt(Mathf.Max(pa.y, pb.y) + width + 2));
        Vector2 ab = pb - pa;
        float len2 = Mathf.Max(ab.sqrMagnitude, 1e-4f);

        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                float t = Mathf.Clamp01(Vector2.Dot(p - pa, ab) / len2);
                float d = Vector2.Distance(p, pa + ab * t);
                float a2 = Mathf.Clamp01(width - d + 0.5f);
                if (a2 <= 0f) continue;
                float grain = 0.55f + 0.45f * Mathf.PerlinNoise(x * 0.9f, y * 0.9f);
                StrokeBlend(x, y, a2 * strength * grain);
            }
    }

    public void Polyline(IList<Vector2> pts, float width = 1.4f, float strength = 0.75f)
    {
        currentStroke++;
        for (int i = 0; i < pts.Count - 1; i++) Line(pts[i], pts[i + 1], width, strength);
        currentStroke++;
    }

    // داخل نفس الخط، المكان اللي ينرسم مرتين ما يغمق أكثر (عشان ما تطلع نقاط عند الوصلات)
    int currentStroke = 1;
    int[] strokeStamp;
    float[] strokeAlpha;

    void StrokeBlend(int x, int y, float a)
    {
        if (strokeStamp == null)
        {
            strokeStamp = new int[w * h];
            strokeAlpha = new float[w * h];
        }
        int i = y * w + x;
        if (strokeStamp[i] == currentStroke)
        {
            float prev = strokeAlpha[i];
            if (a <= prev) return;
            Blend(x, y, ink, (a - prev) / Mathf.Max(1e-3f, 1f - prev));
            strokeAlpha[i] = a;
        }
        else
        {
            strokeStamp[i] = currentStroke;
            strokeAlpha[i] = a;
            Blend(x, y, ink, a);
        }
    }

    // لطخة رصاص ناعمة
    public void Smudge(Vector2 center, float radius, float strength)
    {
        Vector2 c = ToPixels(center);
        float r = radius * PixelsPerUnit;
        for (int y = Mathf.Max(0, (int)(c.y - r)); y < Mathf.Min(h, (int)(c.y + r)); y++)
            for (int x = Mathf.Max(0, (int)(c.x - r)); x < Mathf.Min(w, (int)(c.x + r)); x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c) / r;
                if (d >= 1f) continue;
                float n = Mathf.PerlinNoise(x * 0.05f + center.x, y * 0.05f);
                Blend(x, y, ink, (1f - d) * (1f - d) * strength * (0.5f + n));
            }
    }

    // أثر مساحة: خط عريض فاتح
    public void Erased(Vector2 a, Vector2 b, float width)
    {
        Vector2 pa = ToPixels(a), pb = ToPixels(b);
        float wpx = width * PixelsPerUnit;
        int x0 = Mathf.Max(0, (int)(Mathf.Min(pa.x, pb.x) - wpx)), x1 = Mathf.Min(w - 1, (int)(Mathf.Max(pa.x, pb.x) + wpx));
        int y0 = Mathf.Max(0, (int)(Mathf.Min(pa.y, pb.y) - wpx)), y1 = Mathf.Min(h - 1, (int)(Mathf.Max(pa.y, pb.y) + wpx));
        Vector2 ab = pb - pa;
        float len2 = Mathf.Max(ab.sqrMagnitude, 1e-4f);
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                Vector2 p = new Vector2(x, y);
                float t = Mathf.Clamp01(Vector2.Dot(p - pa, ab) / len2);
                float d = Vector2.Distance(p, pa + ab * t) / wpx;
                if (d >= 1f) continue;
                Blend(x, y, new Color(0.99f, 0.985f, 0.97f), 0.5f * (1f - d));
                if (rng.NextDouble() < 0.004) Blend(x, y, ink, 0.5f);
            }
    }

    // تظليل بخطوط مائلة داخل مستطيل، يخف كل ما بعدنا عن الحافة
    public void Hatch(Vector2 min, Vector2 max, float spacing, float angleDeg, bool fadeTowardMinX)
    {
        Vector2 dir = new Vector2(Mathf.Cos(angleDeg * Mathf.Deg2Rad), Mathf.Sin(angleDeg * Mathf.Deg2Rad));
        float diag = (max - min).magnitude;
        Vector2 center = (min + max) * 0.5f;
        Vector2 normal = new Vector2(-dir.y, dir.x);
        for (float o = -diag; o <= diag; o += spacing * R(0.8f, 1.2f))
        {
            Vector2 p0 = center + normal * o - dir * diag, p1 = center + normal * o + dir * diag;
            if (!ClipToRect(ref p0, ref p1, min, max)) continue;
            float mid = (p0.x + p1.x) * 0.5f;
            float k = Mathf.InverseLerp(min.x, max.x, mid);
            float strength = (fadeTowardMinX ? k : 1f - k) * 0.55f;
            Line(p0, p1, 1.1f, strength);
        }
    }

    static bool ClipToRect(ref Vector2 a, ref Vector2 b, Vector2 min, Vector2 max)
    {
        float t0 = 0f, t1 = 1f;
        Vector2 d = b - a;
        float[] p = { -d.x, d.x, -d.y, d.y };
        float[] q = { a.x - min.x, max.x - a.x, a.y - min.y, max.y - a.y };
        for (int i = 0; i < 4; i++)
        {
            if (Mathf.Abs(p[i]) < 1e-6f) { if (q[i] < 0f) return false; continue; }
            float t = q[i] / p[i];
            if (p[i] < 0f) t0 = Mathf.Max(t0, t); else t1 = Mathf.Min(t1, t);
            if (t0 > t1) return false;
        }
        Vector2 a0 = a;
        a = a0 + d * t0;
        b = a0 + d * t1;
        return true;
    }

    void Blend(int x, int y, Color c, float a)
    {
        int i = y * w + x;
        px[i] = Color.Lerp(px[i], c, Mathf.Clamp01(a));
    }

    // ---------- رسومات صغيرة ----------

    // يرسم رسمة عشوائية من رسومات رويدا في مكان معين
    public void RandomDoodle(Vector2 center, float scale, bool scribbly = false)
    {
        // في صفحات الشخابيط: أغلبها وحوش ما كملت وشخابيط
        int[] scribbleKinds = { Doodles.MonsterKind, Doodles.MonsterKind, 10, 10, 8 };
        int kind = scribbly && rng.Next(4) != 0 ? scribbleKinds[rng.Next(scribbleKinds.Length)] : rng.Next(12);
        float angle = R(-25f, 25f);
        var shape = Doodles.Get(kind, rng);
        DrawShape(shape, center, scale, angle);
    }

    public void Arrow(Vector2 center, float scale)
    {
        DrawShape(Doodles.Get(Doodles.ArrowKind, rng), center, scale, R(-6f, 6f));
    }

    void DrawShape(List<List<Vector2>> shape, Vector2 center, float scale, float angleDeg)
    {
        float c = Mathf.Cos(angleDeg * Mathf.Deg2Rad), s = Mathf.Sin(angleDeg * Mathf.Deg2Rad);
        float strength = R(0.7f, 0.95f);
        // كل رسمة بلون (رصاص أو شمع ملون حسب شكل المرحلة)
        Color previous = ink;
        if (theme != null) ink = theme.RandomDoodleColor(rng);
        foreach (var stroke in shape)
        {
            var pts = new List<Vector2>(stroke.Count);
            foreach (Vector2 p in stroke)
            {
                Vector2 q = new Vector2(p.x * c - p.y * s, p.x * s + p.y * c) * scale;
                q += new Vector2(R(-0.01f, 0.01f), R(-0.01f, 0.01f));
                pts.Add(center + q);
            }
            // الخط أعرض شوي في الأوراق الصغيرة اللي دقتها عالية
            Polyline(pts, R(1.6f, 2.2f) * Mathf.Max(1f, PixelsPerUnit / 200f), strength);
        }
        ink = previous;
    }
}

// أشكال الرسومات (كل شكل مجموعة خطوط، حجمه تقريبًا من -0.5 إلى 0.5)
public static class Doodles
{
    public const int ArrowKind = 100;
    public const int MonsterKind = 12;

    public static List<List<Vector2>> Get(int kind, System.Random rng)
    {
        var s = new List<List<Vector2>>();
        switch (kind)
        {
            case 0: // نجمة
                var star = new List<Vector2>();
                for (int i = 0; i <= 5; i++)
                {
                    float a = Mathf.PI / 2f + i * 4f * Mathf.PI / 5f;
                    star.Add(new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 0.45f);
                }
                s.Add(star);
                break;
            case 1: // حلزون
                var spiral = new List<Vector2>();
                for (int i = 0; i <= 60; i++)
                {
                    float t = i / 60f;
                    float a = t * Mathf.PI * 6f;
                    spiral.Add(new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (0.05f + t * 0.42f));
                }
                s.Add(spiral);
                break;
            case 2: // بيت
                s.Add(Pts(-0.35f, -0.45f, -0.35f, 0.1f, 0.35f, 0.1f, 0.35f, -0.45f, -0.35f, -0.45f));
                s.Add(Pts(-0.45f, 0.08f, 0f, 0.45f, 0.45f, 0.08f));
                s.Add(Pts(-0.08f, -0.45f, -0.08f, -0.15f, 0.08f, -0.15f, 0.08f, -0.45f));
                s.Add(Pts(0.16f, -0.05f, 0.28f, -0.05f, 0.28f, 0.05f, 0.16f, 0.05f, 0.16f, -0.05f));
                break;
            case 3: // وردة
                s.Add(Circle(0f, 0.15f, 0.08f, 16));
                for (int i = 0; i < 6; i++)
                {
                    float a = i * Mathf.PI / 3f;
                    Vector2 c = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 0.17f + new Vector2(0f, 0.15f);
                    s.Add(Circle(c.x, c.y, 0.09f, 12));
                }
                s.Add(Pts(0f, 0.05f, 0.02f, -0.2f, 0f, -0.48f));
                s.Add(Pts(0.01f, -0.28f, 0.15f, -0.18f, 0.02f, -0.22f));
                break;
            case 4: // شمس
                s.Add(Circle(0f, 0f, 0.2f, 20));
                for (int i = 0; i < 8; i++)
                {
                    float a = i * Mathf.PI / 4f;
                    Vector2 d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    s.Add(new List<Vector2> { d * 0.27f, d * 0.45f });
                }
                break;
            case 5: // قلب
                var heart = new List<Vector2>();
                for (int i = 0; i <= 40; i++)
                {
                    float t = i / 40f * Mathf.PI * 2f;
                    float x = 16f * Mathf.Pow(Mathf.Sin(t), 3f);
                    float y = 13f * Mathf.Cos(t) - 5f * Mathf.Cos(2f * t) - 2f * Mathf.Cos(3f * t) - Mathf.Cos(4f * t);
                    heart.Add(new Vector2(x, y) * 0.026f);
                }
                s.Add(heart);
                break;
            case 6: // غيمة
                var cloud = new List<Vector2>();
                float[] bumps = { 0.16f, 0.22f, 0.18f, 0.14f };
                float startX = -0.4f;
                foreach (float r in bumps)
                {
                    for (int i = 0; i <= 10; i++)
                    {
                        float a = Mathf.PI - i / 10f * Mathf.PI;
                        cloud.Add(new Vector2(startX + r + Mathf.Cos(a) * r, Mathf.Sin(a) * r));
                    }
                    startX += r * 1.5f;
                }
                cloud.Add(new Vector2(-0.4f, 0f));
                s.Add(cloud);
                break;
            case 7: // شجرة
                s.Add(Pts(-0.05f, -0.45f, -0.04f, -0.05f));
                s.Add(Pts(0.05f, -0.45f, 0.04f, -0.05f));
                var crown = new List<Vector2>();
                for (int i = 0; i <= 36; i++)
                {
                    float a = -Mathf.PI / 2f + i / 36f * Mathf.PI * 2f;
                    float r = 0.24f + 0.04f * Mathf.Sin(a * 7f);
                    crown.Add(new Vector2(Mathf.Cos(a) * r, 0.17f + Mathf.Sin(a) * r));
                }
                s.Add(crown);
                break;
            case 8: // أرنب (مثل اللي في شنطة رويدا)
                s.Add(Circle(0f, -0.15f, 0.2f, 20));
                s.Add(Ellipse(-0.09f, 0.22f, 0.06f, 0.2f, 14));
                s.Add(Ellipse(0.09f, 0.22f, 0.06f, 0.2f, 14));
                s.Add(Circle(-0.07f, -0.12f, 0.015f, 6));
                s.Add(Circle(0.07f, -0.12f, 0.015f, 6));
                s.Add(Pts(-0.04f, -0.22f, 0f, -0.25f, 0.04f, -0.22f));
                break;
            case 9: // وجه مبتسم
                s.Add(Circle(0f, 0f, 0.4f, 28));
                s.Add(Circle(-0.13f, 0.1f, 0.03f, 6));
                s.Add(Circle(0.13f, 0.1f, 0.03f, 6));
                var smile = new List<Vector2>();
                for (int i = 0; i <= 12; i++)
                {
                    float a = Mathf.PI * 1.15f + i / 12f * Mathf.PI * 0.7f;
                    smile.Add(new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 0.24f);
                }
                s.Add(smile);
                break;
            case 10: // شخابيط
                var scribble = new List<Vector2>();
                Vector2 p = Vector2.zero;
                for (int i = 0; i < 40; i++)
                {
                    float a = i * 0.9f + (float)rng.NextDouble();
                    p = new Vector2(Mathf.Cos(a) * 0.25f + (i / 40f - 0.5f) * 0.5f, Mathf.Sin(a * 1.3f) * 0.2f);
                    scribble.Add(p);
                }
                s.Add(scribble);
                break;
            case 11: // شبكة إكس-أو صغيرة
                s.Add(Pts(-0.15f, -0.45f, -0.15f, 0.45f));
                s.Add(Pts(0.15f, -0.45f, 0.15f, 0.45f));
                s.Add(Pts(-0.45f, -0.15f, 0.45f, -0.15f));
                s.Add(Pts(-0.45f, 0.15f, 0.45f, 0.15f));
                s.Add(Circle(0f, 0f, 0.09f, 12));
                s.Add(Pts(-0.38f, 0.22f, -0.22f, 0.38f));
                s.Add(Pts(-0.38f, 0.38f, -0.22f, 0.22f));
                s.Add(Circle(0.3f, -0.3f, 0.09f, 12));
                break;
            case MonsterKind: // وحش ما كمل: جسم مدور، عيون، وأسنان
                var body = new List<Vector2>();
                for (int i = 0; i <= 30; i++)
                {
                    float a = i / 30f * Mathf.PI * 2f;
                    float r = 0.36f + 0.04f * Mathf.Sin(a * 5f);
                    body.Add(new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r * 0.9f));
                }
                s.Add(body);
                s.Add(Circle(-0.12f, 0.1f, 0.09f, 10));
                s.Add(Circle(0.12f, 0.1f, 0.09f, 10));
                s.Add(Circle(-0.11f, 0.09f, 0.025f, 6));
                s.Add(Circle(0.13f, 0.09f, 0.025f, 6));
                s.Add(Pts(-0.18f, -0.12f, -0.12f, -0.2f, -0.06f, -0.12f, 0f, -0.2f, 0.06f, -0.12f, 0.12f, -0.2f, 0.18f, -0.12f));
                s.Add(Pts(-0.1f, 0.3f, -0.18f, 0.48f));
                s.Add(Pts(0.1f, 0.3f, 0.18f, 0.48f));
                break;
            default: // سهم لقدام
                s.Add(Pts(-0.45f, 0f, 0.42f, 0.02f));
                s.Add(Pts(0.22f, 0.16f, 0.43f, 0.02f, 0.22f, -0.13f));
                break;
        }
        return s;
    }

    static List<Vector2> Pts(params float[] xy)
    {
        var l = new List<Vector2>();
        for (int i = 0; i < xy.Length; i += 2) l.Add(new Vector2(xy[i], xy[i + 1]));
        return l;
    }

    static List<Vector2> Circle(float cx, float cy, float r, int n)
    {
        return Ellipse(cx, cy, r, r, n);
    }

    static List<Vector2> Ellipse(float cx, float cy, float rx, float ry, int n)
    {
        var l = new List<Vector2>();
        for (int i = 0; i <= n; i++)
        {
            float a = i / (float)n * Mathf.PI * 2f;
            l.Add(new Vector2(cx + Mathf.Cos(a) * rx, cy + Mathf.Sin(a) * ry));
        }
        return l;
    }
}
