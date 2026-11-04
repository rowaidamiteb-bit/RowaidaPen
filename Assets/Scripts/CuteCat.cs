using System.Collections.Generic;
using UnityEngine;

// قطوة كيوت ملفوفة على نفسها: جسم ناعم بخطوط، راس بوجه مرسوم (عيون، خشم، فم، خدود، شوارب)،
// أذان مخروطية، كفوف، وذيل ملفوف قدامها. تستخدمها كل المراحل اللي فيها قطط
public class CuteCat
{
    public Transform root, body, head, tail;
    Renderer faceRenderer;
    Material sleepFace, awakeFace;

    static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
    static Mesh sphere, ear;

    // pos: مكان القطوة على الأرض، size: 1 = بطول تقريبا 2.5 متر
    public static CuteCat Build(Transform parent, Vector3 pos, float size, Color fur, Color stripe, bool sleeping)
    {
        GameManager gm = GameManager.Instance;
        if (sphere == null) sphere = CharacterMeshes.Sphere(36, 22, false);
        if (ear == null) ear = CharacterMeshes.Lathe(new[] { new Vector2(0.2f, 0f), new Vector2(0.17f, 0.12f), new Vector2(0.08f, 0.27f), new Vector2(0.005f, 0.36f) }, 16, 0.45f);
        Color cream = Color.Lerp(fur, new Color(1f, 0.96f, 0.88f), 0.7f);
        Material furMat = gm.Mat("Cute Cat Fur " + ColorUtility.ToHtmlStringRGB(fur), fur, 0.25f);
        Material stripeMat = gm.Mat("Cute Cat Stripe " + ColorUtility.ToHtmlStringRGB(stripe), stripe, 0.25f);
        Material pink = gm.Mat("Cute Cat Pink", new Color(1f, 0.62f, 0.7f), 0.3f);

        var c = new CuteCat();
        c.root = new GameObject("Cute Cat").transform;
        c.root.SetParent(parent, false);
        c.root.position = pos;
        c.root.localScale = Vector3.one * size;

        // الجسم (نتنفس فيه): كورة عريضة ملفوفة، بطنها فاتح وظهرها مخطط
        c.body = new GameObject("Body").transform;
        c.body.SetParent(c.root, false);
        Part(c.body, "Body", sphere, new Vector3(0.15f, 0.72f, 0f), new Vector3(1.12f, 0.74f, 0.8f), BodyMat(fur, stripe, cream));

        // كفوف قدام تحت الراس
        // رجول قدام ممدودة تحت الراس (تبدأ من تحت الجسم عشان ما تطلع منفصلة)
        Material pawMat = gm.Mat("Cute Cat Paw " + ColorUtility.ToHtmlStringRGB(fur), Color.Lerp(fur, cream, 0.3f), 0.25f);
        for (int k = 0; k < 2; k++)
            Part(c.body, "Paw", sphere, new Vector3(-0.6f - k * 0.48f, 0.12f, -0.68f + k * 0.04f), new Vector3(0.17f, 0.12f, 0.36f), pawMat);

        // الذيل ملفوف من ورا لين قدام الكفوف، وطرفه غامق
        c.tail = new GameObject("Tail").transform;
        c.tail.SetParent(c.root, false);
        c.tail.localPosition = new Vector3(1.15f, 0.28f, 0.2f);
        var path = new List<Vector3> { Vector3.zero, new Vector3(0.18f, -0.06f, -0.55f), new Vector3(-0.15f, -0.1f, -1.05f), new Vector3(-0.8f, -0.12f, -1.22f), new Vector3(-1.3f, -0.08f, -1.12f) };
        var tailGo = new GameObject("Tail Tube");
        tailGo.transform.SetParent(c.tail, false);
        tailGo.AddComponent<MeshFilter>().sharedMesh = Tube(path, 0.2f, 0.14f);
        tailGo.AddComponent<MeshRenderer>().sharedMaterial = furMat;
        Part(c.tail, "Tail Tip", sphere, path[path.Count - 1], Vector3.one * 0.15f, stripeMat);

        // الراس: وجه مرسوم ناحية الكاميرا
        c.head = new GameObject("Head").transform;
        c.head.SetParent(c.root, false);
        c.head.localPosition = new Vector3(-0.85f, 0.78f, -0.5f);
        c.head.localRotation = Quaternion.Euler(0f, 0f, 7f);
        c.sleepFace = FaceMat(fur, stripe, cream, true);
        c.awakeFace = FaceMat(fur, stripe, cream, false);
        c.faceRenderer = Part(c.head, "Face", sphere, Vector3.zero, new Vector3(0.6f, 0.52f, 0.55f), sleeping ? c.sleepFace : c.awakeFace).GetComponent<Renderer>();
        c.faceRenderer.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        for (int s = -1; s <= 1; s += 2)
        {
            var e = Part(c.head, "Ear", ear, new Vector3(s * 0.3f, 0.33f, 0.05f), Vector3.one, furMat).transform;
            e.localRotation = Quaternion.Euler(-8f, 0f, -s * 24f);
            var inner = Part(e, "Inner Ear", ear, new Vector3(0f, 0.03f, -0.05f), new Vector3(0.62f, 0.7f, 0.5f), pink).transform;
            inner.localRotation = Quaternion.identity;
        }
        return c;
    }

    public void SetAwake(bool awake)
    {
        if (faceRenderer != null) faceRenderer.sharedMaterial = awake ? awakeFace : sleepFace;
    }

    static GameObject Part(Transform parent, string name, Mesh mesh, Vector3 pos, Vector3 scale, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    // أنبوب ناعم على مسار (ينحف لآخره)
    static Mesh Tube(List<Vector3> pts, float r0, float r1)
    {
        // نعّم المسار (كاتمل رووم)
        var p = new List<Vector3>();
        for (int i = 0; i < pts.Count - 1; i++)
            for (int k = 0; k < 6; k++)
            {
                float t = k / 6f;
                Vector3 a = pts[Mathf.Max(0, i - 1)], b = pts[i], c = pts[i + 1], d = pts[Mathf.Min(pts.Count - 1, i + 2)];
                p.Add(0.5f * (2f * b + (-a + c) * t + (2f * a - 5f * b + 4f * c - d) * t * t + (-a + 3f * b - 3f * c + d) * t * t * t));
            }
        p.Add(pts[pts.Count - 1]);
        const int seg = 12;
        var verts = new List<Vector3>();
        var tris = new List<int>();
        Vector3 up = Vector3.up;
        for (int i = 0; i < p.Count; i++)
        {
            Vector3 fwd = (p[Mathf.Min(p.Count - 1, i + 1)] - p[Mathf.Max(0, i - 1)]).normalized;
            Vector3 side = Vector3.Cross(fwd, up).normalized, n2 = Vector3.Cross(side, fwd);
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
        var mesh = new Mesh { name = "Cat Tail" };
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    static Material Textured(string key, Texture2D tex)
    {
        var m = GameManager.MakeMaterial(Color.white, 0.25f);
        m.name = key;
        if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
        if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
        cache[key] = m;
        return m;
    }

    // ملمس الجسم: خطوط عرضية على الظهر وبطن فاتح
    static Material BodyMat(Color fur, Color stripe, Color cream)
    {
        string key = "Cat Body " + ColorUtility.ToHtmlStringRGB(fur) + ColorUtility.ToHtmlStringRGB(stripe);
        if (cache.TryGetValue(key, out Material m) && m != null) return m;
        const int w = 256, h = 128;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, true) { name = key };
        var px = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            float lat = ((y + 0.5f) / h - 0.5f) * Mathf.PI;
            for (int x = 0; x < w; x++)
            {
                float lon = ((x + 0.5f) / w - 0.5f) * Mathf.PI * 2f;
                // اتجاه النقطة على الكورة (الكاميرا تشوف ناحية -z)
                Vector3 d = new Vector3(Mathf.Cos(lat) * Mathf.Sin(lon), Mathf.Sin(lat), Mathf.Cos(lat) * Mathf.Cos(lon));
                Color c = fur;
                float band = Mathf.Sin(d.x * 11f + Mathf.Sin(d.z * 6f) * 0.6f);
                float stripeAmt = Mathf.Clamp01((band - 0.55f) / 0.12f) * Mathf.Clamp01((d.y + 0.05f) / 0.25f);
                if (d.x > 0.85f) stripeAmt *= 0.4f;
                c = Color.Lerp(c, stripe, stripeAmt);
                float belly = Mathf.Clamp01((-d.y - 0.15f) / 0.2f) * Mathf.Clamp01((-d.z + 0.1f) / 0.3f);
                c = Color.Lerp(c, cream, belly);
                px[y * w + x] = c;
            }
        }
        tex.SetPixels(px);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.Apply();
        return Textured(key, tex);
    }

    // الوجه: عيون (مسكرة ومقوسة لو نايمة)، خشم وردي، فم ω، خدود، شوارب، وخطوط على الجبهة
    static Material FaceMat(Color fur, Color stripe, Color cream, bool sleeping)
    {
        string key = "Cat Face " + ColorUtility.ToHtmlStringRGB(fur) + (sleeping ? " Sleep" : " Awake");
        if (cache.TryGetValue(key, out Material m) && m != null) return m;
        const int w = 512, h = 256;
        const float aa = 0.006f;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, true) { name = key };
        var px = new Color[w * h];
        Color line = new Color(0.22f, 0.12f, 0.1f), nose = new Color(1f, 0.5f, 0.6f), blush = new Color(1f, 0.55f, 0.6f);
        for (int y = 0; y < h; y++)
        {
            float lat = ((y + 0.5f) / h - 0.5f) * Mathf.PI;
            for (int x = 0; x < w; x++)
            {
                float lon = ((x + 0.5f) / w - 0.5f) * Mathf.PI * 2f;
                Vector3 d = new Vector3(Mathf.Cos(lat) * Mathf.Sin(lon), Mathf.Sin(lat), Mathf.Cos(lat) * Mathf.Cos(lon));
                Color c = fur;
                float fx = d.x, fy = d.y;
                // خطوط الجبهة
                if (fy > 0.45f)
                    for (int k = -1; k <= 1; k++)
                    {
                        float sx = k * 0.2f * (1.1f - fy * 0.4f);
                        c = Color.Lerp(c, stripe, Band(fx - sx, 0.045f * (fy - 0.4f) / 0.6f + 0.012f, aa));
                    }
                if (d.z > 0f)
                {
                    // بوز فاتح
                    c = Color.Lerp(c, cream, Fill(Ellipse(fx, fy, 0f, -0.33f, 0.4f, 0.27f), 0.25f));
                    // خدود
                    for (int s = -1; s <= 1; s += 2)
                    {
                        float e = Ellipse(fx, fy, s * 0.47f, -0.2f, 0.15f, 0.09f);
                        c = Color.Lerp(c, blush, 0.6f * Mathf.Clamp01(1f - e) * Mathf.Clamp01(1f - e));
                    }
                    // خشم
                    float ny = (fy + 0.16f) / 0.05f;
                    if (Mathf.Abs(ny) < 1.2f)
                    {
                        float half = 0.075f * Mathf.Clamp01((ny + 1f) / 2f) + 0.012f;
                        c = Color.Lerp(c, nose, Band(fx, half, aa) * Band(ny / 1.1f, 1f, 0.15f));
                    }
                    // فم ω
                    for (int s = -1; s <= 1; s += 2)
                    {
                        float mx = fx - s * 0.055f, my = fy + 0.235f;
                        float r = Mathf.Sqrt(mx * mx + my * my);
                        if (my < 0.01f) c = Color.Lerp(c, line, Band(r - 0.055f, 0.01f, aa));
                    }
                    c = Color.Lerp(c, line, Band(fx, 0.008f, aa) * (fy < -0.2f && fy > -0.245f ? 1f : 0f));
                    // شوارب
                    for (int s = -1; s <= 1; s += 2)
                        for (int k = -1; k <= 1; k++)
                        {
                            float ax = s * fx;
                            if (ax < 0.33f || ax > 0.75f) continue;
                            float wy = -0.27f + k * 0.06f + (ax - 0.33f) * k * 0.18f;
                            c = Color.Lerp(c, line, 0.75f * Band(fy - wy, 0.006f, aa));
                        }
                    // العيون
                    for (int s = -1; s <= 1; s += 2)
                    {
                        float cx = s * 0.3f, cy = 0.02f;
                        if (sleeping)
                        {
                            float ex = (fx - cx) / 0.13f;
                            if (Mathf.Abs(ex) < 1f)
                            {
                                float ey = cy - 0.06f * (1f - ex * ex);
                                c = Color.Lerp(c, line, Band(fy - ey, 0.016f * (1.15f - 0.4f * Mathf.Abs(ex)), aa));
                            }
                        }
                        else
                        {
                            float e = Ellipse(fx, fy, cx, cy, 0.1f, 0.13f);
                            c = Color.Lerp(c, new Color(0.1f, 0.07f, 0.08f), Fill(e, 0.08f));
                            c = Color.Lerp(c, new Color(0.35f, 0.75f, 0.45f), Fill(Ellipse(fx, fy, cx, cy - 0.04f, 0.075f, 0.06f), 0.15f) * 0.6f);
                            c = Color.Lerp(c, Color.white, Fill(Ellipse(fx, fy, cx - s * 0.03f, cy + 0.05f, 0.035f, 0.04f), 0.15f));
                            c = Color.Lerp(c, Color.white, 0.9f * Fill(Ellipse(fx, fy, cx + s * 0.035f, cy - 0.05f, 0.017f, 0.017f), 0.25f));
                        }
                    }
                }
                px[y * w + x] = c;
            }
        }
        tex.SetPixels(px);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.anisoLevel = 4;
        tex.Apply();
        return Textured(key, tex);
    }

    static float Ellipse(float x, float y, float cx, float cy, float rx, float ry)
    {
        float dx = (x - cx) / rx, dy = (y - cy) / ry;
        return Mathf.Sqrt(dx * dx + dy * dy);
    }

    static float Fill(float nd, float soft) => Mathf.Clamp01((1f - nd) / soft + 0.5f);

    static float Band(float dist, float half, float soft) => Mathf.Clamp01((half - Mathf.Abs(dist)) / soft + 0.5f);

    // حرف Z طاير (نوم): ثلاث قطع
    public static GameObject SleepZ(Vector3 at, Material mat)
    {
        var z = new GameObject("Z");
        z.transform.position = at;
        z.transform.localScale = Vector3.one * 0.32f;
        LevelBuilder.Visual("Z Top", PrimitiveType.Cube, z.transform, new Vector3(0f, 0.45f, 0f), new Vector3(0.9f, 0.16f, 0.1f), mat, false);
        LevelBuilder.Visual("Z Bottom", PrimitiveType.Cube, z.transform, new Vector3(0f, -0.45f, 0f), new Vector3(0.9f, 0.16f, 0.1f), mat, false);
        LevelBuilder.Visual("Z Mid", PrimitiveType.Cube, z.transform, Vector3.zero, new Vector3(1.15f, 0.16f, 0.1f), mat, false).transform.localRotation = Quaternion.Euler(0f, 0f, 38f);
        return z;
    }
}
