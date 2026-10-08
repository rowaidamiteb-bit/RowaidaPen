using System.Collections.Generic;
using UnityEngine;

// مجسمات وصور تنبني بالكود: صفحة ورق سميكة، حلقة سلك الدفتر، وملمس الورق
public static class PaperMeshes
{
    // صندوق: الوجه العلوي (submesh 0) ورق، والجوانب (submesh 1) حواف صفحات مكدسة
    public static Mesh PaperSlab(Vector3 size)
    {
        Vector3 h = size * 0.5f;
        var verts = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();
        var top = new List<int>();
        var sides = new List<int>();

        void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 n, List<int> tris, System.Func<Vector3, Vector2> uv)
        {
            int i = verts.Count;
            foreach (Vector3 p in new[] { a, b, c, d })
            {
                verts.Add(p);
                normals.Add(n);
                uvs.Add(uv(p));
            }
            tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
            tris.Add(i); tris.Add(i + 2); tris.Add(i + 3);
        }

        float x = h.x, y = h.y, z = h.z;
        // السطح العلوي: الصورة تغطي الصفحة كلها مرة وحدة (عشان الرسومات)
        System.Func<Vector3, Vector2> topUV = p => new Vector2((p.x + x) / size.x, (p.z + z) / size.z);
        System.Func<Vector3, Vector2> frontUV = p => new Vector2(p.x / 4f, p.y);
        System.Func<Vector3, Vector2> sideUV = p => new Vector2(p.z / 4f, p.y);

        Quad(new Vector3(-x, y, z), new Vector3(x, y, z), new Vector3(x, y, -z), new Vector3(-x, y, -z), Vector3.up, top, topUV);
        Quad(new Vector3(-x, y, -z), new Vector3(x, y, -z), new Vector3(x, -y, -z), new Vector3(-x, -y, -z), Vector3.back, sides, frontUV);
        Quad(new Vector3(x, y, z), new Vector3(-x, y, z), new Vector3(-x, -y, z), new Vector3(x, -y, z), Vector3.forward, sides, frontUV);
        Quad(new Vector3(x, y, -z), new Vector3(x, y, z), new Vector3(x, -y, z), new Vector3(x, -y, -z), Vector3.right, sides, sideUV);
        Quad(new Vector3(-x, y, z), new Vector3(-x, y, -z), new Vector3(-x, -y, -z), new Vector3(-x, -y, z), Vector3.left, sides, sideUV);
        Quad(new Vector3(x, -y, z), new Vector3(-x, -y, z), new Vector3(-x, -y, -z), new Vector3(x, -y, -z), Vector3.down, sides, topUV);

        var mesh = new Mesh { name = "Paper Slab" };
        mesh.SetVertices(verts);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.subMeshCount = 2;
        mesh.SetTriangles(top, 0);
        mesh.SetTriangles(sides, 1);
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        return mesh;
    }

    // حلقة (طارة) في مستوى YZ، مثل سلك الدفتر الحلزوني
    public static Mesh Ring(float majorRadius, float minorRadius, int majorSegments = 24, int minorSegments = 8)
    {
        var verts = new List<Vector3>();
        var normals = new List<Vector3>();
        var tris = new List<int>();

        for (int i = 0; i < majorSegments; i++)
        {
            float u = i * Mathf.PI * 2f / majorSegments;
            Vector3 c = new Vector3(0f, Mathf.Cos(u), Mathf.Sin(u));
            for (int j = 0; j < minorSegments; j++)
            {
                float v = j * Mathf.PI * 2f / minorSegments;
                Vector3 n = c * Mathf.Cos(v) + Vector3.right * Mathf.Sin(v);
                verts.Add(c * majorRadius + n * minorRadius);
                normals.Add(n);
            }
        }

        for (int i = 0; i < majorSegments; i++)
        {
            int i2 = (i + 1) % majorSegments;
            for (int j = 0; j < minorSegments; j++)
            {
                int j2 = (j + 1) % minorSegments;
                int a = i * minorSegments + j, b = i * minorSegments + j2;
                int c = i2 * minorSegments + j, d = i2 * minorSegments + j2;
                tris.Add(a); tris.Add(c); tris.Add(b);
                tris.Add(b); tris.Add(c); tris.Add(d);
            }
        }

        var mesh = new Mesh { name = "Binding Ring" };
        mesh.SetVertices(verts);
        mesh.SetNormals(normals);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    // ملمس ورق ناعم (حبيبات خفيفة)
    public static Texture2D PaperGrain()
    {
        const int size = 256;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, true) { name = "Paper Grain" };
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float fibers = Mathf.PerlinNoise(x * 0.08f, y * 0.02f) * 0.5f + Mathf.PerlinNoise(x * 0.3f + 50f, y * 0.3f) * 0.5f;
                float grain = Random.value * 0.03f;
                float v = 0.94f + fibers * 0.05f - grain;
                pixels[y * size + x] = new Color(v, v * 0.99f, v * 0.965f);
            }
        return Finish(tex, pixels);
    }

    // حواف صفحات مكدسة فوق بعض (خطوط أفقية رفيعة)
    public static Texture2D PageEdges()
    {
        const int size = 256;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, true) { name = "Page Edges" };
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            bool gap = (y % 8) == 0;
            float shade = 0.86f + Mathf.PerlinNoise(0.5f, y * 0.37f) * 0.08f;
            for (int x = 0; x < size; x++)
            {
                float v = gap ? 0.62f + Mathf.PerlinNoise(x * 0.05f, y) * 0.12f : shade;
                pixels[y * size + x] = new Color(v, v * 0.985f, v * 0.96f);
            }
        }
        return Finish(tex, pixels);
    }

    // تدرّج للخلفية: كحلي تحت وبنفسجي فوق
    public static Texture2D SkyGradient()
    {
        return SkyGradient(new Color(0.05f, 0.05f, 0.11f), new Color(0.16f, 0.15f, 0.30f), new Color(0.30f, 0.24f, 0.42f));
    }

    public static Texture2D SkyGradient(Color bottom, Color middle, Color top)
    {
        const int size = 256;
        var tex = new Texture2D(4, size, TextureFormat.RGBA32, false) { name = "Dream Sky" };
        var pixels = new Color[4 * size];
        for (int y = 0; y < size; y++)
        {
            float t = y / (size - 1f);
            Color c = t < 0.55f ? Color.Lerp(bottom, middle, t / 0.55f) : Color.Lerp(middle, top, (t - 0.55f) / 0.45f);
            for (int x = 0; x < 4; x++) pixels[y * 4 + x] = c;
        }
        tex.SetPixels(pixels);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.Apply();
        return tex;
    }

    static Texture2D Finish(Texture2D tex, Color[] pixels)
    {
        tex.SetPixels(pixels);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Trilinear;
        tex.anisoLevel = 8;
        tex.Apply();
        return tex;
    }
}
