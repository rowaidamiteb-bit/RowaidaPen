using System.Collections.Generic;
using UnityEngine;

// أشكال ناعمة مشتركة لتجميل الشخصيات والأغراض: مخاريط (قرون، أشواك، أسنان)، أنابيب على مسار (ذيول، أرجل)،
// صخور وكور مكرمشة، جناح بغشاء، ووجوه مرسومة على الكور (عيون وفم وخدود)
public static class Shapes
{
    static readonly Dictionary<string, Mesh> cache = new Dictionary<string, Mesh>();
    static readonly Dictionary<string, Material> faces = new Dictionary<string, Material>();

    public static GameObject Add(Transform parent, string name, Mesh mesh, Vector3 pos, Vector3 scale, Material mat, Vector3 euler = default, bool shadows = true)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(euler);
        go.transform.localScale = scale;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = shadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
        return go;
    }

    // مخروط ناعم (قاعدته عند 0 وراسه فوق عند 1)، bend يقوسه لقدام (للقرون)
    public static Mesh Cone(float bend = 0f, int seg = 14)
    {
        string key = "cone" + bend.ToString("0.00") + seg;
        if (cache.TryGetValue(key, out Mesh m) && m != null) return m;
        var path = new List<Vector3>();
        for (int i = 0; i <= 8; i++)
        {
            float t = i / 8f;
            path.Add(new Vector3(bend * t * t, t, 0f));
        }
        m = Tube(path, 0.5f, 0.02f, seg, true);
        cache[key] = m;
        return m;
    }

    // أنبوب على مسار ينحف من r0 إلى r1 (cap = سد الطرفين)
    public static Mesh Tube(List<Vector3> pts, float r0, float r1, int seg = 12, bool cap = true, bool smooth = true)
    {
        var p = new List<Vector3>();
        if (smooth && pts.Count > 2)
        {
            for (int i = 0; i < pts.Count - 1; i++)
                for (int k = 0; k < 5; k++)
                {
                    float t = k / 5f;
                    Vector3 a = pts[Mathf.Max(0, i - 1)], b = pts[i], c = pts[i + 1], d = pts[Mathf.Min(pts.Count - 1, i + 2)];
                    p.Add(0.5f * (2f * b + (-a + c) * t + (2f * a - 5f * b + 4f * c - d) * t * t + (-a + 3f * b - 3f * c + d) * t * t * t));
                }
            p.Add(pts[pts.Count - 1]);
        }
        else p.AddRange(pts);
        var verts = new List<Vector3>();
        var tris = new List<int>();
        Vector3 prevSide = Vector3.zero;
        for (int i = 0; i < p.Count; i++)
        {
            Vector3 fwd = (p[Mathf.Min(p.Count - 1, i + 1)] - p[Mathf.Max(0, i - 1)]).normalized;
            Vector3 up = Mathf.Abs(Vector3.Dot(fwd, Vector3.forward)) > 0.9f ? Vector3.up : Vector3.forward;
            Vector3 side = i == 0 ? Vector3.Cross(fwd, up).normalized : Vector3.ProjectOnPlane(prevSide, fwd).normalized;
            if (side.sqrMagnitude < 1e-4f) side = Vector3.Cross(fwd, up).normalized;
            prevSide = side;
            Vector3 n2 = Vector3.Cross(side, fwd);
            float r = Mathf.Lerp(r0, r1, i / (p.Count - 1f));
            for (int j = 0; j < seg; j++)
            {
                float a = j * Mathf.PI * 2f / seg;
                verts.Add(p[i] + (side * Mathf.Cos(a) + n2 * Mathf.Sin(a)) * r);
            }
        }
        for (int i = 0; i < p.Count - 1; i++)
            for (int j = 0; j < seg; j++)
            {
                int a = i * seg + j, b = i * seg + (j + 1) % seg, c = a + seg, d = b + seg;
                tris.Add(a); tris.Add(c); tris.Add(b);
                tris.Add(b); tris.Add(c); tris.Add(d);
            }
        if (cap)
        {
            int c0 = verts.Count; verts.Add(p[0]);
            int c1 = verts.Count; verts.Add(p[p.Count - 1]);
            int last = (p.Count - 1) * seg;
            for (int j = 0; j < seg; j++)
            {
                int j2 = (j + 1) % seg;
                tris.Add(c0); tris.Add(j2); tris.Add(j);
                tris.Add(c1); tris.Add(last + j); tris.Add(last + j2);
            }
        }
        var mesh = new Mesh { name = "Tube" };
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    // كورة مشوهة (صخرة، غيمة، كورة ورق) — rough: قد إيش متعرجة
    public static Mesh Blob(int seed, float rough = 0.25f, float flatBottom = 0f)
    {
        string key = "blob" + seed + "_" + rough.ToString("0.00") + "_" + flatBottom.ToString("0.0");
        if (cache.TryGetValue(key, out Mesh m) && m != null) return m;
        m = CharacterMeshes.Sphere(24, 16, false);
        var v = m.vertices;
        float o = seed * 3.17f;
        for (int i = 0; i < v.Length; i++)
        {
            Vector3 d = v[i];
            float n = Mathf.PerlinNoise(d.x * 1.6f + o, d.y * 1.6f + d.z * 1.3f) * 0.7f + Mathf.PerlinNoise(d.z * 3.5f + o, d.x * 3.5f + d.y) * 0.3f;
            Vector3 p = d * (1f - rough * 0.5f + n * rough);
            if (flatBottom > 0f && p.y < -flatBottom) p.y = -flatBottom + (p.y + flatBottom) * 0.15f;
            v[i] = p;
        }
        m.vertices = v;
        m.RecalculateNormals();
        m.RecalculateBounds();
        cache[key] = m;
        return m;
    }

    // جناح بغشاء: عظمة فوق، وأصابع تنزل، والغشاء بينها بحافة مقوسة (بمستوى XY)
    public static Mesh WingMesh(int fingers = 3)
    {
        string key = "wing" + fingers;
        if (cache.TryGetValue(key, out Mesh m) && m != null) return m;
        var verts = new List<Vector3> { Vector3.zero };
        // نقاط الحافة من الكتف للطرف ثم للأسفل بأقواس
        var tips = new List<Vector3>();
        for (int k = 0; k <= fingers; k++)
        {
            float a = Mathf.Lerp(20f, -70f, k / (float)fingers) * Mathf.Deg2Rad;
            float len = Mathf.Lerp(1f, 0.55f, k / (float)fingers);
            tips.Add(new Vector3(Mathf.Cos(a) * len, Mathf.Sin(a) * len + 0.25f, 0f));
        }
        var edge = new List<Vector3>();
        for (int k = 0; k < tips.Count - 1; k++)
        {
            edge.Add(tips[k]);
            Vector3 mid = (tips[k] + tips[k + 1]) * 0.5f;
            edge.Add(Vector3.Lerp(mid, Vector3.zero, 0.25f));
        }
        edge.Add(tips[tips.Count - 1]);
        edge.Add(new Vector3(0.15f, -0.25f, 0f));
        var tris = new List<int>();
        foreach (Vector3 e in edge) verts.Add(e);
        for (int i = 1; i < verts.Count - 1; i++) { tris.Add(0); tris.Add(i); tris.Add(i + 1); }
        // الوجه الثاني (من ورا)
        int n = verts.Count;
        for (int i = 0; i < n; i++) verts.Add(verts[i] + Vector3.forward * 0.01f);
        for (int i = 1; i < n - 1; i++) { tris.Add(n); tris.Add(n + i + 1); tris.Add(n + i); }
        m = new Mesh { name = "Wing" };
        m.SetVertices(verts);
        m.SetTriangles(tris, 0);
        m.RecalculateNormals();
        m.RecalculateBounds();
        cache[key] = m;
        return m;
    }

    // نقاط أطراف أصابع الجناح (عشان نحط عظام عليها)
    public static Vector3 WingTip(int k, int fingers = 3)
    {
        float a = Mathf.Lerp(20f, -70f, k / (float)fingers) * Mathf.Deg2Rad;
        float len = Mathf.Lerp(1f, 0.55f, k / (float)fingers);
        return new Vector3(Mathf.Cos(a) * len, Mathf.Sin(a) * len + 0.25f, 0f);
    }

    // مجسم بوجوه مسطحة حادة (ورق مطوي، كريستال)، يبان من الجهتين
    public static Mesh Facets(Vector3[] v, int[] t)
    {
        var verts = new List<Vector3>();
        var tris = new List<int>();
        for (int i = 0; i < t.Length; i += 3)
        {
            int s = verts.Count;
            verts.Add(v[t[i]]); verts.Add(v[t[i + 1]]); verts.Add(v[t[i + 2]]);
            tris.Add(s); tris.Add(s + 1); tris.Add(s + 2);
            s = verts.Count;
            verts.Add(v[t[i]]); verts.Add(v[t[i + 2]]); verts.Add(v[t[i + 1]]);
            tris.Add(s); tris.Add(s + 1); tris.Add(s + 2);
        }
        var m = new Mesh { name = "Facets" };
        m.SetVertices(verts);
        m.SetTriangles(tris, 0);
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }

    public static Mesh Sphere()
    {
        if (cache.TryGetValue("sphere", out Mesh m) && m != null) return m;
        m = CharacterMeshes.Sphere(36, 22, false);
        cache["sphere"] = m;
        return m;
    }

    // ---------- أشكال واقعية ----------

    const float ShellSpread = 1.08f;   // نص زاوية مروحة الصدفة

    // ارتفاع سطح الصدفة: u من جنب لجنب (0..1)، v من المفصل للحافة (0..1). inner = السطح اللي جوا (ناعم بدون ضلوع)
    public static float ShellY(float u, float v, float height, int ribs, bool inner)
    {
        float side = Mathf.Pow(Mathf.Max(0f, Mathf.Cos((u * 2f - 1f) * Mathf.PI * 0.5f)), 0.45f);   // صفر على الجوانب
        float dome = height * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Mathf.PI * Mathf.Pow(v, 0.75f))), 0.6f) * side;
        float rib = Mathf.Pow(0.5f + 0.5f * Mathf.Cos(u * ribs * Mathf.PI * 2f), 1.5f) * height * 0.3f * Mathf.Pow(v, 0.8f) * side;
        return inner ? dome * 0.78f + rib * Mathf.Pow(v, 8f) : dome + rib;
    }

    // مكان نقطة على الصدفة (المفصل عند 0 والحافة لقدام ناحية -z)
    public static Vector3 ShellPoint(float u, float v, float length, float height, int ribs, bool inner)
    {
        float a = (u * 2f - 1f) * ShellSpread;
        float r = length * v * (1f - 0.16f * Mathf.Pow(u * 2f - 1f, 4f));
        return new Vector3(Mathf.Sin(a) * r, ShellY(u, v, height, ribs, inner), -Mathf.Cos(a) * r);
    }

    // نص صدفة محار (مثل الأسقلوب): مروحة بضلوع وحافة متموجة. قسمين: 0 برا (ضلوع)، 1 جوا (ناعم لامع)
    public static Mesh Shell(int ribs = 16, float length = 1.25f, float height = 0.28f)
    {
        string key = "shell" + ribs + "_" + length.ToString("0.00") + "_" + height.ToString("0.00");
        if (cache.TryGetValue(key, out Mesh m) && m != null) return m;
        const int nu = 96, nv = 22;
        var verts = new List<Vector3>();
        var uvs = new List<Vector2>();
        var outer = new List<int>();
        var inner = new List<int>();
        for (int s = 0; s < 2; s++)
        {
            int b = verts.Count;
            for (int j = 0; j <= nv; j++)
                for (int i = 0; i <= nu; i++)
                {
                    float u = i / (float)nu, v = j / (float)nv;
                    verts.Add(ShellPoint(u, v, length, height, ribs, s == 1));
                    uvs.Add(new Vector2(u, v));
                }
            var tris = s == 0 ? outer : inner;
            for (int j = 0; j < nv; j++)
                for (int i = 0; i < nu; i++)
                {
                    int a = b + j * (nu + 1) + i, c = a + 1, d = a + nu + 1, e = d + 1;
                    if (s == 0) { tris.Add(a); tris.Add(c); tris.Add(d); tris.Add(c); tris.Add(e); tris.Add(d); }
                    else { tris.Add(a); tris.Add(d); tris.Add(c); tris.Add(c); tris.Add(d); tris.Add(e); }
                }
        }
        m = new Mesh { name = "Shell" };
        m.SetVertices(verts);
        m.SetUVs(0, uvs);
        m.subMeshCount = 2;
        m.SetTriangles(outer, 0);
        m.SetTriangles(inner, 1);
        m.RecalculateNormals();
        m.RecalculateBounds();
        cache[key] = m;
        return m;
    }

    // لون الصدفة من برا: فاتح عند المفصل ويغمق للحافة، خطوط لون بين الضلوع، وخطوط نمو دايرية
    public static Material ShellPattern(string key, Color light, Color dark, int ribs)
    {
        key = "shell " + key;
        if (faces.TryGetValue(key, out Material cached) && cached != null) return cached;
        const int w = 384, h = 128;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, true) { name = key, wrapMode = TextureWrapMode.Clamp };
        var px = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float u = x / (float)(w - 1), v = y / (float)(h - 1);
                Color c = Color.Lerp(light, dark, Mathf.SmoothStep(0.05f, 0.55f, v));
                // أحزمة لون مثل الصدف الحقيقي
                if (Mathf.PerlinNoise(v * 7f, u * 1.5f + 3f) > 0.58f) c = Color.Lerp(c, dark * 0.8f, 0.35f);
                // بين الضلوع أغمق شوي
                float rib = 0.5f + 0.5f * Mathf.Cos(u * ribs * Mathf.PI * 2f);
                c *= Mathf.Lerp(0.78f, 1.06f, Mathf.Pow(rib, 0.7f));
                // خطوط النمو
                if (Mathf.Repeat(v * 26f + Mathf.PerlinNoise(u * 6f, v * 3f) * 0.8f, 1f) < 0.1f) c *= 0.86f;
                c.a = 1f;
                px[y * w + x] = c;
            }
        tex.SetPixels(px);
        tex.Apply();
        var mat = GameManager.MakeMaterial(Color.white, 0.45f);
        mat.name = key;
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
        faces[key] = mat;
        return mat;
    }

    // صنج نحاس: قبة صغيرة بالنص، وبعدها ينزل رقيق للحافة، وعليه حلقات خراطة تلمع. الحافة عند y = 0 والقبة لفوق
    public static Mesh Cymbal(float radius = 0.8f)
    {
        string key = "cymbal" + radius.ToString("0.00");
        if (cache.TryGetValue(key, out Mesh m) && m != null) return m;
        float Top(float r)
        {
            float bell = 0.2f * radius, y;
            if (r < bell) y = 0.11f * radius + 0.1f * radius * Mathf.Pow(Mathf.Cos(r / bell * Mathf.PI * 0.5f), 1.2f);
            else y = 0.11f * radius * Mathf.Pow(1f - (r - bell) / (radius - bell), 1.15f);
            if (r > bell * 1.1f && r < radius * 0.97f) y += 0.0035f * Mathf.Sin(r / radius * 110f);
            return y;
        }
        var profile = new List<Vector2>();
        const int n = 72;
        // من تحت (من النص للحافة)، وبعدين من فوق (من الحافة للنص)
        for (int i = 0; i <= n; i++) { float r = radius * i / n; profile.Add(new Vector2(r, Top(r) - Mathf.Lerp(0.02f, 0.008f, i / (float)n))); }
        for (int i = n; i >= 0; i--) { float r = radius * i / n; profile.Add(new Vector2(r, Top(r))); }
        m = CharacterMeshes.Lathe(profile, 48);
        m.name = "Cymbal";
        cache[key] = m;
        return m;
    }

    // نجمة خماسية مجسمة بوجوه حادة (مثل نجوم الزينة): الرؤوس بالمستوى XY والنص بارز لقدام ولورا
    public static Mesh Star(float inner = 0.42f, float depth = 0.22f)
    {
        string key = "star" + inner.ToString("0.00") + depth.ToString("0.00");
        if (cache.TryGetValue(key, out Mesh m) && m != null) return m;
        var v = new List<Vector3> { new Vector3(0f, 0f, -depth), new Vector3(0f, 0f, depth) };
        for (int k = 0; k < 10; k++)
        {
            float a = Mathf.PI * 0.5f + k * Mathf.PI / 5f;
            float r = k % 2 == 0 ? 1f : inner;
            v.Add(new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f));
        }
        var t = new List<int>();
        for (int k = 0; k < 10; k++)
        {
            int a = 2 + k, b = 2 + (k + 1) % 10;
            t.Add(0); t.Add(a); t.Add(b);
            t.Add(1); t.Add(b); t.Add(a);
        }
        m = Facets(v.ToArray(), t.ToArray());
        m.name = "Star";
        cache[key] = m;
        return m;
    }

    // ---------- وجوه مرسومة ----------

    public enum Eyes { Cute, Sleepy, Angry, Happy, Scared, Dots, None }
    public enum Mouth { Smile, Cat, Fangs, Open, Flat, Frown, None }

    // كورة (رادياس 1) عليها وجه ناحية -z (الكاميرا)، ولونها base، وتحت الوجه بوز فاتح لو muzzle
    public static Material Face(Color skin, Eyes eyes, Mouth mouth, Color? muzzle = null, Color? iris = null, bool blush = true, float eyeSize = 1f, float eyeGap = 1f, float faceY = 0f)
    {
        string key = ColorUtility.ToHtmlStringRGB(skin) + eyes + mouth + (muzzle.HasValue ? ColorUtility.ToHtmlStringRGB(muzzle.Value) : "-") +
                     (iris.HasValue ? ColorUtility.ToHtmlStringRGB(iris.Value) : "-") + blush + eyeSize.ToString("0.00") + eyeGap.ToString("0.00") + faceY.ToString("0.00");
        if (faces.TryGetValue(key, out Material cached) && cached != null) return cached;
        const int w = 512, h = 256;
        const float aa = 0.006f;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, true) { name = "Face " + key, wrapMode = TextureWrapMode.Clamp, anisoLevel = 4 };
        var px = new Color[w * h];
        Color line = new Color(0.16f, 0.09f, 0.1f), pink = new Color(1f, 0.55f, 0.62f), mouthIn = new Color(0.45f, 0.1f, 0.15f);
        Color irisC = iris ?? new Color(0.35f, 0.22f, 0.15f);
        for (int y = 0; y < h; y++)
        {
            float lat = ((y + 0.5f) / h - 0.5f) * Mathf.PI;
            for (int x = 0; x < w; x++)
            {
                float lon = ((x + 0.5f) / w - 0.5f) * Mathf.PI * 2f;
                // الكورة تنحط مدورة 180: u = 0.5 يطالع الكاميرا
                Vector3 d = new Vector3(Mathf.Cos(lat) * Mathf.Sin(lon), Mathf.Sin(lat), Mathf.Cos(lat) * Mathf.Cos(lon));
                Color c = skin;
                float fx = d.x, fy = d.y - faceY;
                if (d.z > 0.05f)
                {
                    if (muzzle.HasValue) c = Color.Lerp(c, muzzle.Value, Fill(Ellipse(fx, fy, 0f, -0.3f, 0.38f, 0.26f), 0.25f));
                    if (blush)
                        for (int s = -1; s <= 1; s += 2)
                        {
                            float e = Ellipse(fx, fy, s * 0.5f * eyeGap, -0.2f, 0.15f, 0.085f);
                            c = Color.Lerp(c, pink, 0.55f * Mathf.Clamp01(1f - e) * Mathf.Clamp01(1f - e));
                        }
                    // الفم
                    switch (mouth)
                    {
                        case Mouth.Smile:
                        {
                            float mx = fx / 0.12f;
                            if (Mathf.Abs(mx) < 1f) c = Color.Lerp(c, line, Band(fy - (-0.27f + 0.05f * (1f - mx * mx) * -1f), 0.013f, aa));
                            break;
                        }
                        case Mouth.Frown:
                        {
                            float mx = fx / 0.11f;
                            if (Mathf.Abs(mx) < 1f) c = Color.Lerp(c, line, Band(fy - (-0.3f + 0.045f * (1f - mx * mx)), 0.013f, aa));
                            break;
                        }
                        case Mouth.Flat:
                            if (Mathf.Abs(fx) < 0.1f) c = Color.Lerp(c, line, Band(fy + 0.28f, 0.013f, aa));
                            break;
                        case Mouth.Cat:
                            for (int s = -1; s <= 1; s += 2)
                            {
                                float mx = fx - s * 0.055f, my = fy + 0.25f;
                                if (my < 0.01f) c = Color.Lerp(c, line, Band(Mathf.Sqrt(mx * mx + my * my) - 0.055f, 0.01f, aa));
                            }
                            break;
                        case Mouth.Open:
                        {
                            float e = Ellipse(fx, fy, 0f, -0.28f, 0.1f, 0.075f);
                            c = Color.Lerp(c, mouthIn, Fill(e, 0.12f));
                            c = Color.Lerp(c, pink, Fill(Ellipse(fx, fy, 0f, -0.33f, 0.06f, 0.035f), 0.2f) * Fill(e, 0.12f));
                            c = Color.Lerp(c, line, Band(e - 1f, 0.09f, 0.06f));
                            break;
                        }
                        case Mouth.Fangs:
                        {
                            float mx = fx / 0.2f;
                            if (Mathf.Abs(mx) < 1f)
                            {
                                float top = -0.24f - 0.03f * mx * mx, bottom = -0.24f - 0.11f * (1f - mx * mx) - 0.02f;
                                if (fy < top && fy > bottom) c = mouthIn;
                                c = Color.Lerp(c, line, Band(fy - top, 0.011f, aa));
                                // أنياب
                                for (int s = -1; s <= 1; s += 2)
                                {
                                    float tx = fx - s * 0.1f;
                                    float th = Mathf.Clamp01(1f - Mathf.Abs(tx) / 0.035f) * 0.07f;
                                    if (fy < top && fy > top - th) c = Color.white;
                                }
                            }
                            break;
                        }
                    }
                    // العيون
                    for (int s = -1; s <= 1; s += 2)
                    {
                        float cx = s * 0.3f * eyeGap, cy = 0.02f, rx = 0.11f * eyeSize, ry = 0.14f * eyeSize;
                        switch (eyes)
                        {
                            case Eyes.None:
                                break;
                            case Eyes.Sleepy:
                            {
                                float ex = (fx - cx) / (0.13f * eyeSize);
                                if (Mathf.Abs(ex) < 1f) c = Color.Lerp(c, line, Band(fy - (cy - 0.06f * (1f - ex * ex)), 0.016f, aa));
                                break;
                            }
                            case Eyes.Happy:
                            {
                                float ex = (fx - cx) / (0.12f * eyeSize);
                                if (Mathf.Abs(ex) < 1f) c = Color.Lerp(c, line, Band(fy - (cy - 0.03f + 0.06f * (1f - ex * ex)), 0.016f, aa));
                                break;
                            }
                            case Eyes.Dots:
                                c = Color.Lerp(c, line, Fill(Ellipse(fx, fy, cx, cy, 0.05f * eyeSize, 0.065f * eyeSize), 0.15f));
                                c = Color.Lerp(c, Color.white, Fill(Ellipse(fx, fy, cx - s * 0.015f, cy + 0.025f, 0.015f, 0.018f), 0.3f));
                                break;
                            default:
                            {
                                bool angry = eyes == Eyes.Angry, scared = eyes == Eyes.Scared;
                                float e = Ellipse(fx, fy, cx, cy, rx, ry);
                                // جفن الغضب يقص العين من فوق
                                float lid = angry ? cy + ry * 0.45f - s * (fx - cx) * -0.9f : 99f;
                                if (fy > lid) e = 9f;
                                c = Color.Lerp(c, Color.white, Fill(e, 0.08f));
                                float pr = scared ? 0.35f : 0.68f;
                                float pupil = Ellipse(fx, fy, cx + (scared ? 0f : s * -0.015f), cy - 0.02f, rx * pr, ry * pr);
                                if (fy <= lid)
                                {
                                    c = Color.Lerp(c, irisC, Fill(pupil, 0.1f));
                                    c = Color.Lerp(c, line, Fill(Ellipse(fx, fy, cx + (scared ? 0f : s * -0.015f), cy - 0.025f, rx * pr * 0.55f, ry * pr * 0.55f), 0.15f));
                                    c = Color.Lerp(c, Color.white, Fill(Ellipse(fx, fy, cx - s * 0.03f, cy + ry * 0.35f, rx * 0.3f, ry * 0.28f), 0.2f));
                                }
                                // خط حول العين
                                if (fy <= lid + 0.01f) c = Color.Lerp(c, line, Band(e - 1f, angry ? 0.12f : 0.09f, 0.06f));
                                if (angry && Mathf.Abs(fx - cx) < rx * 1.3f)
                                    c = Color.Lerp(c, line, Band(fy - (lid + 0.02f), 0.02f, aa));
                                break;
                            }
                        }
                    }
                }
                px[y * w + x] = c;
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        var mat = GameManager.MakeMaterial(Color.white, 0.3f);
        mat.name = "Face " + key;
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
        if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
        faces[key] = mat;
        return mat;
    }

    // راس كورة بوجه مرسوم ناحية الكاميرا (نفس فكرة القطوة)
    public static GameObject FaceBall(Transform parent, string name, Vector3 pos, Vector3 radii, Material face, Vector3 euler = default)
    {
        var holder = new GameObject(name);
        holder.transform.SetParent(parent, false);
        holder.transform.localPosition = pos;
        holder.transform.localRotation = Quaternion.Euler(euler);
        Add(holder.transform, "Face", Sphere(), Vector3.zero, radii, face, new Vector3(0f, 180f, 0f));
        return holder;
    }

    // ملمس خطوط (بطن تنين، صدفة) على كورة: أشرطة عرضية فاتحة وغامقة
    public static Material Banded(string key, Color a, Color b, int bands, float smooth = 0.3f)
    {
        key = "band " + key;
        if (faces.TryGetValue(key, out Material cached) && cached != null) return cached;
        var tex = new Texture2D(4, 64, TextureFormat.RGBA32, true) { name = key, wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < 64; y++)
        {
            float v = Mathf.Sin(y / 64f * Mathf.PI * bands);
            Color c = Color.Lerp(a, b, Mathf.Clamp01(Mathf.Abs(v) < 0.18f ? 1f : 0f));
            for (int x = 0; x < 4; x++) tex.SetPixel(x, y, c);
        }
        tex.Apply();
        var mat = GameManager.MakeMaterial(Color.white, smooth);
        mat.name = key;
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
        faces[key] = mat;
        return mat;
    }

    static float Ellipse(float x, float y, float cx, float cy, float rx, float ry)
    {
        float dx = (x - cx) / rx, dy = (y - cy) / ry;
        return Mathf.Sqrt(dx * dx + dy * dy);
    }

    static float Fill(float nd, float soft) => Mathf.Clamp01((1f - nd) / soft + 0.5f);

    static float Band(float dist, float half, float soft) => Mathf.Clamp01((half - Mathf.Abs(dist)) / soft + 0.5f);

    // مادة تضوي (حمم، نار، ضو)
    public static Material Glow(string key, Color c)
    {
        key = "glow " + key;
        if (faces.TryGetValue(key, out Material cached) && cached != null) return cached;
        var m = GameManager.MakeMaterial(c, 0.5f);
        m.name = key;
        if (m.HasProperty("_EmissionColor")) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c * 1.6f); }
        faces[key] = m;
        return m;
    }
}
