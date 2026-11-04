using System.Collections.Generic;
using UnityEngine;

// رسم بكسل حقيقي لعالم البكسل: كل مكعب يتقسم مربعات (16×16 بكسل) بحافة فاتحة فوق ويسار وغامقة تحت ويمين
// زي بلوكات ألعاب زمان، ومربعات خاصة: "؟" والطوب والأنابيب والورق والعملات
public static class PixelArt
{
    public const float Tile = 0.6f;   // حجم المربع الواحد بالعالم

    static readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
    static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
    static readonly Dictionary<Vector3Int, Mesh> meshes = new Dictionary<Vector3Int, Mesh>();

    // بلوك بكسل: pattern = bevel (عادي) / leaf / brick / pipe / question / coin
    public static GameObject Block(string name, Transform parent, Vector3 pos, Vector3 scale, Color c, bool shadows, string pattern = "bevel", float tile = Tile)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.AddComponent<MeshFilter>().sharedMesh = Box(scale, tile);
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = Mat(pattern, c);
        r.shadowCastingMode = shadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
        return go;
    }

    public static Material Mat(string pattern, Color c)
    {
        string key = pattern + " " + ColorUtility.ToHtmlStringRGBA(c);
        if (materials.TryGetValue(key, out Material m) && m != null) return m;
        Texture2D tex = Texture(pattern);
        // الأنماط الرمادية تتلون باللون (ونعوض أن وسطها أغمق شوي من الأبيض)
        bool colored = pattern == "question" || pattern == "coin";
        m = GameManager.MakeMaterial(colored ? Color.white : c * 1.22f, 0.12f);
        m.name = "Pixel " + key;
        if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
        if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", colored ? Color.white : c * 1.22f);
        materials[key] = m;
        return m;
    }

    // مكعب بإحداثيات صورة تتكرر حسب المقاس (عشان المربعات ما تتمطط)
    static Mesh Box(Vector3 scale, float tile)
    {
        var n = new Vector3Int(Count(scale.x, tile), Count(scale.y, tile), Count(scale.z, tile));
        if (meshes.TryGetValue(n, out Mesh cached) && cached != null) return cached;
        var verts = new List<Vector3>();
        var norms = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tris = new List<int>();
        void Face(Vector3 normal, Vector3 u, Vector3 v, float nu, float nv)
        {
            int s = verts.Count;
            Vector3 c = normal * 0.5f;
            verts.Add(c - u * 0.5f - v * 0.5f); verts.Add(c + u * 0.5f - v * 0.5f); verts.Add(c + u * 0.5f + v * 0.5f); verts.Add(c - u * 0.5f + v * 0.5f);
            for (int k = 0; k < 4; k++) norms.Add(normal);
            uvs.Add(new Vector2(0f, 0f)); uvs.Add(new Vector2(nu, 0f)); uvs.Add(new Vector2(nu, nv)); uvs.Add(new Vector2(0f, nv));
            // نتأكد إن الوجه يطالع برا
            if (Vector3.Dot(Vector3.Cross(u, v), normal) > 0f) { tris.Add(s); tris.Add(s + 1); tris.Add(s + 2); tris.Add(s); tris.Add(s + 2); tris.Add(s + 3); }
            else { tris.Add(s); tris.Add(s + 2); tris.Add(s + 1); tris.Add(s); tris.Add(s + 3); tris.Add(s + 2); }
        }
        Face(Vector3.back, Vector3.right, Vector3.up, n.x, n.y);
        Face(Vector3.forward, Vector3.left, Vector3.up, n.x, n.y);
        Face(Vector3.left, Vector3.back, Vector3.up, n.z, n.y);
        Face(Vector3.right, Vector3.forward, Vector3.up, n.z, n.y);
        Face(Vector3.up, Vector3.right, Vector3.forward, n.x, n.z);
        Face(Vector3.down, Vector3.right, Vector3.back, n.x, n.z);
        var mesh = new Mesh { name = "Pixel Box " + n };
        mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        meshes[n] = mesh;
        return mesh;
    }

    static int Count(float size, float tile) => Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(size) / tile));

    static Texture2D Texture(string pattern)
    {
        if (textures.TryGetValue(pattern, out Texture2D t) && t != null) return t;
        bool colored = pattern == "question" || pattern == "coin";
        t = new Texture2D(16, 16, TextureFormat.RGBA32, true, !colored) { name = "Pixel " + pattern, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
        var px = new Color[256];
        var rng = new System.Random(pattern.GetHashCode());
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                float v;
                switch (pattern)
                {
                    case "leaf": v = Leaf(x, y, rng); break;
                    case "brick": v = Brick(x, y); break;
                    case "pipe": v = Pipe(x); break;
                    case "flat": v = y == 15 ? 1f : y == 0 ? 0.7f : 0.82f; break;
                    case "question": px[y * 16 + x] = Question(x, y); continue;
                    case "coin": px[y * 16 + x] = Coin(x, y); continue;
                    default: v = Bevel(x, y, rng); break;
                }
                px[y * 16 + x] = new Color(v, v, v, 1f);
            }
        t.SetPixels(px);
        t.Apply();
        textures[pattern] = t;
        return t;
    }

    // y = 0 تحت. الوسط 0.82 تقريبا (نعوضه باللون ×1.22)
    static float Bevel(int x, int y, System.Random rng)
    {
        if (y == 15 || x == 0) return x == 15 || y == 0 ? 0.7f : 1f;
        if (y == 0 || x == 15) return 0.5f;
        if (y == 14 || x == 1) return 0.92f;
        if (y == 1 || x == 14) return 0.66f;
        // نقاط صغيرة زي رسم البكسل
        int h = (x * 7 + y * 13) % 11;
        return h == 0 ? 0.74f : h == 5 ? 0.88f : 0.82f;
    }

    static float Leaf(int x, int y, System.Random rng)
    {
        if (y == 15) return 1f;
        if (y == 14) return 0.94f;
        if (y == 0) return 0.6f;
        int h = (x * 5 + y * 11 + x * y) % 9;
        if (h == 0) return 0.66f;
        if (h == 4 && y > 7) return 0.95f;
        return (x + y) % 2 == 0 ? 0.83f : 0.8f;
    }

    // طوب: صفين طوب بكل مربع، وبينهم مونة غامقة، وكل طوبة لها حافة فاتحة
    static float Brick(int x, int y)
    {
        if (y == 7 || y == 15) return 0.42f;
        int off = y < 7 ? 0 : 8;
        int bx = (x + off) % 16;
        if (bx == 0 || bx == 8) return 0.42f;
        if (y == 6 || y == 14) return 0.98f;
        if (y == 0 || y == 8) return 0.66f;
        if (bx == 1 || bx == 9) return 0.92f;
        return 0.82f;
    }

    // أنبوب: خطوط طول (لمعة يسار وظل يمين)
    static float Pipe(int x)
    {
        float[] cols = { 0.35f, 0.95f, 1.15f, 1f, 0.92f, 0.86f, 0.84f, 0.82f, 0.8f, 0.76f, 0.72f, 0.66f, 0.6f, 0.54f, 0.48f, 0.32f };
        return cols[x];
    }

    static readonly string[] questionArt =
    {
        "OOOOOOOOOOOOOOOO",
        "OHHHHHHHHHHHHHSO",
        "OHrYYYYYYYYYYrSO",
        "OHYYYYqqqqqYYYSO",
        "OHYYYqqdddqqYYSO",
        "OHYYYqqdYYqqdYSO",
        "OHYYYYddYYqqdYSO",
        "OHYYYYYYYqqddYSO",
        "OHYYYYYYqqddYYSO",
        "OHYYYYYqqddYYYSO",
        "OHYYYYYqqdYYYYSO",
        "OHYYYYYYddYYYYSO",
        "OHYYYYYqqYYYYYSO",
        "OHrYYYYqqdYYYrSO",
        "OSSSSSSSSSSSSSSO",
        "OOOOOOOOOOOOOOOO",
    };

    static Color Question(int x, int y)
    {
        char ch = questionArt[15 - y][x];
        switch (ch)
        {
            case 'O': return new Color(0.45f, 0.2f, 0.05f);
            case 'H': return new Color(1f, 0.92f, 0.55f);
            case 'S': return new Color(0.8f, 0.45f, 0.08f);
            case 'r': return new Color(0.55f, 0.28f, 0.08f);
            case 'q': return new Color(0.98f, 0.98f, 0.95f);
            case 'd': return new Color(0.5f, 0.25f, 0.05f);
            default: return new Color(1f, 0.72f, 0.12f);
        }
    }

    static Color Coin(int x, int y)
    {
        float dx = (x - 7.5f) / 6.5f, dy = (y - 7.5f) / 7.5f, r = dx * dx + dy * dy;
        if (r > 1f) return new Color(0.7f, 0.42f, 0.05f);
        if (r > 0.72f) return x < 8 && y > 7 ? new Color(1f, 0.95f, 0.6f) : new Color(0.85f, 0.55f, 0.08f);
        if (Mathf.Abs(x - 7.5f) < 1.1f && Mathf.Abs(dy) < 0.6f) return new Color(0.8f, 0.5f, 0.06f);
        return new Color(1f, 0.8f, 0.18f);
    }
}
