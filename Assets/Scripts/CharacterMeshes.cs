using System.Collections.Generic;
using UnityEngine;

// مجسمات شخصية رويدا: راس بوجه مرسوم، جسم ناعم بالدوران، وشعر من خصل
public static class CharacterMeshes
{
    // كرة بإحداثيات صورة (u = 0.5 قدام الوجه). لو faceHole = true نشيل منطقة الوجه (للشعر)
    public static Mesh Sphere(int lonSegments, int latSegments, bool faceHole)
    {
        var verts = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tris = new List<int>();

        for (int i = 0; i <= latSegments; i++)
        {
            float v = i / (float)latSegments;
            float lat = (v - 0.5f) * Mathf.PI;
            for (int j = 0; j <= lonSegments; j++)
            {
                float u = j / (float)lonSegments;
                verts.Add(Dir(u, lat));
                uvs.Add(new Vector2(u, v));
            }
        }

        int row = lonSegments + 1;
        for (int i = 0; i < latSegments; i++)
            for (int j = 0; j < lonSegments; j++)
            {
                if (faceHole)
                {
                    Vector3 c = Dir((j + 0.5f) / lonSegments, ((i + 0.5f) / latSegments - 0.5f) * Mathf.PI);
                    if (c.z > 0.15f && c.y < 0.42f && c.y > -0.95f) continue;
                }
                int a = i * row + j, b = a + 1, cc = a + row, d = cc + 1;
                tris.Add(a); tris.Add(b); tris.Add(cc);
                tris.Add(b); tris.Add(d); tris.Add(cc);
            }

        var mesh = new Mesh { name = faceHole ? "Hair Cap" : "Head" };
        mesh.SetVertices(verts);
        mesh.SetNormals(verts);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    static Vector3 Dir(float u, float lat)
    {
        float lon = (u - 0.5f) * Mathf.PI * 2f;
        return new Vector3(Mathf.Cos(lat) * Mathf.Sin(lon), Mathf.Sin(lat), Mathf.Cos(lat) * Mathf.Cos(lon));
    }

    // مجسم بالدوران حول المحور Y من خط جانبي (نصف القطر، الارتفاع) من تحت لفوق
    public static Mesh Lathe(IList<Vector2> profile, int segments = 20, float depthScale = 1f)
    {
        var verts = new List<Vector3>();
        var tris = new List<int>();
        for (int i = 0; i < profile.Count; i++)
            for (int j = 0; j < segments; j++)
            {
                float a = j * Mathf.PI * 2f / segments;
                verts.Add(new Vector3(Mathf.Sin(a) * profile[i].x, profile[i].y, Mathf.Cos(a) * profile[i].x * depthScale));
            }

        for (int i = 0; i < profile.Count - 1; i++)
            for (int j = 0; j < segments; j++)
            {
                int j2 = (j + 1) % segments;
                int a = i * segments + j, b = i * segments + j2, c = (i + 1) * segments + j, d = (i + 1) * segments + j2;
                tris.Add(a); tris.Add(b); tris.Add(c);
                tris.Add(b); tris.Add(d); tris.Add(c);
            }

        var mesh = new Mesh { name = "Lathe" };
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    // ---------- الوجه ----------

    // يرسم الوجه على صورة الراس: عيون كبيرة لامعة، رموش، حواجب، خدود وردية، وفم صغير
    public static Texture2D FaceTexture(Color skin, bool closed = false)
    {
        const int w = 1024, h = 512;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, true) { name = "Rowaida Face" };
        var px = new Color[w * h];
        float aa = 0.006f;

        Color lash = new Color(0.08f, 0.05f, 0.05f);
        Color irisTop = new Color(0.1f, 0.06f, 0.05f);
        Color irisBottom = new Color(0.5f, 0.31f, 0.2f);
        Color brow = new Color(0.22f, 0.13f, 0.11f);
        Color blush = new Color(1f, 0.58f, 0.6f);
        Color lips = new Color(0.82f, 0.45f, 0.45f);
        Color noseShade = new Color(skin.r * 0.92f, skin.g * 0.82f, skin.b * 0.8f);

        for (int y = 0; y < h; y++)
        {
            float lat = ((y + 0.5f) / h - 0.5f) * Mathf.PI;
            for (int x = 0; x < w; x++)
            {
                Vector3 d = Dir((x + 0.5f) / w, lat);
                Color c = skin;
                if (d.z > 0f)
                {
                    float fx = d.x, fy = d.y;

                    // خدود
                    for (int s = -1; s <= 1; s += 2)
                    {
                        float e = Ellipse(fx, fy, s * 0.5f, -0.3f, 0.17f, 0.09f);
                        c = Color.Lerp(c, blush, 0.5f * Mathf.Clamp01(1f - e) * Mathf.Clamp01(1f - e));
                    }

                    // خشم وفم
                    c = Color.Lerp(c, noseShade, Fill(Ellipse(fx, fy, 0f, -0.22f, 0.03f, 0.022f), 0.3f));
                    float mx = fx / 0.07f;
                    if (Mathf.Abs(mx) < 1f)
                    {
                        float my = -0.43f + 0.025f * mx * mx;
                        c = Color.Lerp(c, lips, Band(Mathf.Abs(fy - my), 0.011f, aa));
                    }

                    for (int s = -1; s <= 1; s += 2)
                    {
                        float cx = s * 0.34f, cy = -0.04f;

                        // حواجب
                        float bx = (fx - cx) / 0.15f;
                        if (Mathf.Abs(bx) < 1f)
                        {
                            float by = 0.27f + 0.045f * (1f - bx * bx) - s * bx * 0.012f;
                            float thick = 0.016f * (1f - 0.5f * Mathf.Abs(bx));
                            c = Color.Lerp(c, brow, 0.9f * Band(Mathf.Abs(fy - by), thick, aa));
                        }

                        // عين مسكرة (نايمة): خط مقوس لتحت مع رموش صغيرة
                        if (closed)
                        {
                            float ex = (fx - cx) / 0.14f;
                            if (Mathf.Abs(ex) < 1.1f)
                            {
                                float ey = cy - 0.035f * (1f - ex * ex);
                                c = Color.Lerp(c, lash, Band(Mathf.Abs(fy - ey), 0.013f * (1.2f - 0.5f * Mathf.Abs(ex)), aa));
                            }
                            for (int k = 0; k < 3; k++)
                            {
                                float lx = cx + s * (0.06f + k * 0.035f);
                                float lb = cy - 0.035f * (1f - Mathf.Pow((lx - cx) / 0.14f, 2f));
                                if (Mathf.Abs(fx - lx) < 0.006f && fy < lb && fy > lb - 0.03f)
                                    c = Color.Lerp(c, lash, 0.85f);
                            }
                            continue;
                        }

                        // العين
                        float eye = Ellipse(fx, fy, cx, cy, 0.15f, 0.19f);
                        if (eye < 1.25f)
                        {
                            float inside = Fill(eye, 0.02f / 0.17f);
                            c = Color.Lerp(c, new Color(0.98f, 0.97f, 0.97f), inside);

                            float iris = Ellipse(fx, fy, cx, cy - 0.015f, 0.115f, 0.165f);
                            float t = Mathf.InverseLerp(cy + 0.15f, cy - 0.16f, fy);
                            Color irisColor = Color.Lerp(irisTop, irisBottom, t * t);
                            c = Color.Lerp(c, irisColor, inside * Fill(iris, 0.05f));
                            c = Color.Lerp(c, new Color(0.04f, 0.02f, 0.02f), inside * Fill(Ellipse(fx, fy, cx, cy - 0.01f, 0.055f, 0.08f), 0.08f));

                            // لمعة العين
                            c = Color.Lerp(c, Color.white, inside * Fill(Ellipse(fx, fy, cx - s * 0.035f, cy + 0.065f, 0.042f, 0.048f), 0.12f));
                            c = Color.Lerp(c, Color.white, inside * 0.9f * Fill(Ellipse(fx, fy, cx + s * 0.04f, cy - 0.075f, 0.02f, 0.02f), 0.2f));

                            // الرموش العلوية (خط سميك) والسفلية (خفيفة)
                            float rel = fy - cy;
                            if (rel > -0.03f)
                            {
                                float thickness = 0.13f + 0.12f * Mathf.Clamp01((s * (fx - cx)) / 0.15f);
                                c = Color.Lerp(c, lash, Band(eye - 1f, thickness, 0.05f) * Mathf.Clamp01((rel + 0.03f) / 0.04f));
                            }
                            else if (s * (fx - cx) > 0f && rel < -0.08f)
                            {
                                c = Color.Lerp(c, lash, 0.55f * Band(eye - 1f, 0.05f, 0.04f));
                            }
                        }

                        // طرف الرمش للخارج
                        float wingX = s * (fx - cx);
                        if (wingX > 0.12f && wingX < 0.2f)
                        {
                            float wingY = cy + 0.1f + (wingX - 0.12f) * 0.6f;
                            c = Color.Lerp(c, lash, Band(Mathf.Abs(fy - wingY), 0.012f * (1f - (wingX - 0.12f) / 0.08f), aa));
                        }
                    }
                }
                px[y * w + x] = c;
            }
        }

        tex.SetPixels(px);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Trilinear;
        tex.anisoLevel = 4;
        tex.Apply();
        return tex;
    }

    static float Ellipse(float x, float y, float cx, float cy, float rx, float ry)
    {
        float dx = (x - cx) / rx, dy = (y - cy) / ry;
        return Mathf.Sqrt(dx * dx + dy * dy);
    }

    // 1 داخل الشكل و 0 برا مع حافة ناعمة
    static float Fill(float normalizedDistance, float soft)
    {
        return Mathf.Clamp01((1f - normalizedDistance) / soft + 0.5f);
    }

    // 1 على الخط و 0 بعيد عنه
    static float Band(float distance, float halfWidth, float soft)
    {
        return Mathf.Clamp01((halfWidth - Mathf.Abs(distance)) / soft + 0.5f);
    }

    // ---------- الشعر ----------

    // خصل شعر طويلة مدموجة في مجسم واحد
    public static Mesh Hair(Vector3 headCenter, Vector3 headRadius)
    {
        var parts = new List<CombineInstance>();
        var rng = new System.Random(5);
        float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);

        Vector3 Scalp(float lonDeg, float latDeg, float lift)
        {
            float lon = lonDeg * Mathf.Deg2Rad, lat = latDeg * Mathf.Deg2Rad;
            Vector3 d = new Vector3(Mathf.Cos(lat) * Mathf.Sin(lon), Mathf.Sin(lat), Mathf.Cos(lat) * Mathf.Cos(lon));
            return headCenter + Vector3.Scale(d, headRadius) * lift;
        }

        // الشعر الطويل ورا (لين الخصر)
        for (int i = 0; i < 56; i++)
        {
            float lon = Mathf.Lerp(-105f, 105f, i / 55f) + 180f + R(-3f, 3f);
            float lat = R(15f, 45f);
            Vector3 start = Scalp(lon, lat, 1.02f);
            Vector3 outward = new Vector3(start.x - headCenter.x, 0f, start.z - headCenter.z).normalized;
            float length = R(0.6f, 0.75f);
            float spread = R(0.2f, 0.235f);
            float phase = R(0f, 6f);

            var pts = new List<Vector3>();
            var radii = new List<float>();
            const int n = 9;
            for (int k = 0; k < n; k++)
            {
                float t = k / (n - 1f);
                float y = Mathf.Lerp(start.y, start.y - length, t);
                float r = Mathf.Lerp(Vector3.Distance(new Vector3(start.x, 0f, start.z), new Vector3(headCenter.x, 0f, headCenter.z)), spread, Mathf.Sqrt(t));
                Vector3 p = new Vector3(headCenter.x, y, headCenter.z) + outward * r;
                p += Vector3.Cross(outward, Vector3.up) * Mathf.Sin(t * 5f + phase) * 0.015f * t;
                pts.Add(p);
                radii.Add(Mathf.Lerp(0.036f, 0.01f, t * t));
            }
            parts.Add(Strand(pts, radii));
        }

        // خصل جانبية تنزل قدام الكتف
        for (int s = -1; s <= 1; s += 2)
            for (int i = 0; i < 6; i++)
            {
                float lon = s * R(68f, 95f);
                Vector3 start = Scalp(lon, R(10f, 35f), 1.03f);
                Vector3 side = new Vector3(s, 0f, 0f);
                var pts = new List<Vector3>();
                var radii = new List<float>();
                float length = R(0.42f, 0.55f);
                for (int k = 0; k < 7; k++)
                {
                    float t = k / 6f;
                    Vector3 p = start + Vector3.down * (length * t) + side * (0.03f * Mathf.Sin(t * 3f)) + Vector3.forward * (0.03f * t);
                    pts.Add(p);
                    radii.Add(Mathf.Lerp(0.032f, 0.008f, t));
                }
                parts.Add(Strand(pts, radii));
            }

        // الغرة على الجبهة
        for (int i = 0; i < 17; i++)
        {
            float lon = Mathf.Lerp(-60f, 60f, i / 16f) + R(-2f, 2f);
            Vector3 root = Scalp(lon * 0.5f, 72f, 1.0f);
            Vector3 tip = Scalp(lon, R(18f, 26f), 1.06f);
            Vector3 control = Scalp(lon * 0.85f, 55f, 1.22f);
            var pts = new List<Vector3>();
            var radii = new List<float>();
            for (int k = 0; k < 7; k++)
            {
                float t = k / 6f;
                Vector3 p = (1 - t) * (1 - t) * root + 2 * (1 - t) * t * control + t * t * tip;
                pts.Add(p);
                radii.Add(Mathf.Lerp(0.036f, 0.008f, t * t));
            }
            parts.Add(Strand(pts, radii));
        }

        var mesh = new Mesh { name = "Hair Strands", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mesh.CombineMeshes(parts.ToArray(), true, false);
        foreach (CombineInstance ci in parts) Object.Destroy(ci.mesh);
        mesh.RecalculateBounds();
        return mesh;
    }

    static CombineInstance Strand(List<Vector3> pts, List<float> radii)
    {
        var m = new Mesh();
        TubeMesh.Build(m, pts, radii[0], false, 7, radii);
        return new CombineInstance { mesh = m, transform = Matrix4x4.identity };
    }
}
