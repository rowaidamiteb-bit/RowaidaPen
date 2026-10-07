using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// السما الليلية ورا الدفتر: تدرج كحلي، نجوم صغيرة تلمع، غيوم ناعمة، وهلال مضيء
// السما تمشي مع الكاميرا ببطء (كل طبقة بسرعة مختلفة) عشان تعطي إحساس عمق
public class NightSky : MonoBehaviour
{
    Transform farLayer, cloudLayer;
    readonly List<Transform> stars = new List<Transform>();
    readonly List<Vector3> starScale = new List<Vector3>();
    readonly List<float> starPhase = new List<float>();
    readonly List<float> starSpeed = new List<float>();
    readonly List<Transform> clouds = new List<Transform>();
    readonly List<float> cloudDrift = new List<float>();
    readonly List<Material> cloudMats = new List<Material>();
    readonly List<Color> cloudColors = new List<Color>();
    Transform moonT, haloT;
    Material skyMat;
    Vector3 farBase, cloudBase;

    const float FarFollow = 0.92f;    // الطبقة البعيدة (النجوم والهلال) تمشي تقريبًا مع الكاميرا
    const float CloudFollow = 0.8f;   // الغيوم أقرب شوي فتتحرك أكثر

    public void Build()
    {
        var rng = new System.Random(21);
        float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);

        farLayer = new GameObject("Far Sky").transform;
        farLayer.SetParent(transform, false);
        cloudLayer = new GameObject("Clouds").transform;
        cloudLayer.SetParent(transform, false);

        // التدرج
        Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
        skyMat = new Material(unlit) { name = "Dream Sky" };
        skyMat.SetTexture("_BaseMap", PaperMeshes.SkyGradient());
        skyMat.SetColor("_BaseColor", Color.white);
        Quad("Sky Gradient", farLayer, new Vector3(0f, 8f, 95f), new Vector2(420f, 190f), skyMat);

        // نجوم: لمعات بأربع أطراف ونقاط صغيرة
        Material sparkle = Transparent("Star Sparkle", SparkleTexture(), new Color(2.2f, 2.1f, 1.9f, 1f));
        Material dot = Transparent("Star Dot", DotTexture(), new Color(1.8f, 1.8f, 2f, 1f));
        Material warmDot = Transparent("Star Warm", DotTexture(), new Color(2f, 1.7f, 1.3f, 1f));
        for (int i = 0; i < 260; i++)
        {
            bool big = rng.Next(6) == 0;
            float size = big ? R(0.9f, 1.6f) : R(0.18f, 0.45f);
            Material m = big ? sparkle : (rng.Next(4) == 0 ? warmDot : dot);
            Vector3 p = new Vector3(R(-75f, 75f), R(-22f, 38f), R(80f, 92f));
            var star = Quad("Star", farLayer, p, new Vector2(size, size), m);
            star.localRotation = Quaternion.Euler(0f, 0f, big ? R(-15f, 15f) : 0f);
            stars.Add(star);
            starScale.Add(star.localScale);
            starPhase.Add(R(0f, 10f));
            starSpeed.Add(R(0.8f, 2.6f));
        }

        // الهلال مع هالة ضوء
        Material moon = Transparent("Moon", CrescentTexture(), new Color(2.4f, 2.2f, 1.75f, 1f));
        Material halo = Transparent("Moon Halo", DotTexture(), new Color(0.75f, 0.7f, 0.95f, 0.35f));
        haloT = Quad("Moon Halo", farLayer, new Vector3(17f, 10f, 87f), new Vector2(26f, 26f), halo);
        moonT = Quad("Moon", farLayer, new Vector3(17f, 10f, 85f), new Vector2(9f, 9f), moon);
        moonT.localRotation = Quaternion.Euler(0f, 0f, -20f);

        // غيوم ناعمة بنفسجية على أعماق مختلفة
        Texture2D[] cloudTex = { CloudTexture(1), CloudTexture(2), CloudTexture(3) };
        for (int i = 0; i < 16; i++)
        {
            float depth = R(55f, 80f);
            float brightness = Mathf.Lerp(0.55f, 0.35f, Mathf.InverseLerp(55f, 80f, depth));
            var mat = Transparent("Cloud", cloudTex[i % 3], new Color(brightness, brightness * 0.92f, brightness * 1.45f, R(0.35f, 0.6f)));
            float w = R(22f, 42f);
            float y = rng.Next(3) == 0 ? R(-20f, -6f) : R(4f, 26f);
            var cloud = Quad("Cloud", cloudLayer, new Vector3(R(-70f, 70f), y, depth), new Vector2(w, w * 0.42f), mat);
            if (rng.Next(2) == 0) cloud.localScale = new Vector3(-cloud.localScale.x, cloud.localScale.y, 1f);
            clouds.Add(cloud);
            cloudMats.Add(mat);
            cloudColors.Add(mat.GetColor("_BaseColor"));
            cloudDrift.Add(R(0.15f, 0.45f));
        }

        farBase = farLayer.localPosition;
        cloudBase = cloudLayer.localPosition;
    }

    // جو ممطر: الهلال والنجوم يختفون ورا الغيوم، والغيوم أغمق وأكثف، والسما أغمق
    public void SetStormy(bool stormy)
    {
        if (moonT == null) return;
        moonT.gameObject.SetActive(!stormy);
        haloT.gameObject.SetActive(!stormy);
        for (int i = 0; i < stars.Count; i++)
            stars[i].gameObject.SetActive(!stormy || i % 7 == 0);
        for (int i = 0; i < cloudMats.Count; i++)
        {
            Color c = cloudColors[i];
            cloudMats[i].SetColor("_BaseColor", stormy ? new Color(c.r * 0.55f, c.g * 0.6f, c.b * 0.6f, Mathf.Min(1f, c.a + 0.35f)) : c);
        }
        skyMat.SetColor("_BaseColor", stormy ? new Color(0.55f, 0.6f, 0.7f) : Color.white);
    }

    void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null || farLayer == null) return;

        Vector3 c = cam.transform.position;
        farLayer.localPosition = farBase + new Vector3(c.x * FarFollow, c.y * FarFollow, 0f);
        cloudLayer.localPosition = cloudBase + new Vector3(c.x * CloudFollow, c.y * CloudFollow, 0f);

        // النجوم تلمع وتطفي بهدوء
        float t = Time.time;
        for (int i = 0; i < stars.Count; i++)
        {
            float k = 0.65f + 0.35f * Mathf.Sin(t * starSpeed[i] + starPhase[i]);
            stars[i].localScale = starScale[i] * k;
        }

        // الغيوم تمشي ببطء شديد
        for (int i = 0; i < clouds.Count; i++)
        {
            Vector3 p = clouds[i].localPosition;
            p.x += cloudDrift[i] * Time.deltaTime;
            if (p.x > 80f) p.x = -80f;
            clouds[i].localPosition = p;
        }
    }

    static Transform Quad(string name, Transform parent, Vector3 pos, Vector2 size, Material mat)
    {
        var go = LevelBuilder.Visual(name, PrimitiveType.Quad, parent, pos, new Vector3(size.x, size.y, 1f), mat, false);
        go.GetComponent<MeshRenderer>().receiveShadows = false;
        return go.transform;
    }

    // مادة شفافة بدون إضاءة (اللون فوق 1 يعطي توهج مع الـ Bloom)
    static Material Transparent(string name, Texture2D tex, Color color)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = name };
        m.SetTexture("_BaseMap", tex);
        m.SetColor("_BaseColor", color);
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", 0f);
        m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0f);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.SetOverrideTag("RenderType", "Transparent");
        m.renderQueue = (int)RenderQueue.Transparent;
        return m;
    }

    // ---------- الصور ----------

    static Texture2D NewTex(int w, int h, string name)
    {
        return new Texture2D(w, h, TextureFormat.RGBA32, true) { name = name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear };
    }

    static Texture2D DotTexture()
    {
        const int s = 64;
        var tex = NewTex(s, s, "Star Dot");
        var px = new Color[s * s];
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float dx = (x + 0.5f) / s * 2f - 1f, dy = (y + 0.5f) / s * 2f - 1f;
                float r2 = dx * dx + dy * dy;
                float a = Mathf.Exp(-r2 * 9f) + 0.5f * Mathf.Exp(-r2 * 40f);
                px[y * s + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(a) * Mathf.Clamp01(1f - r2));
            }
        tex.SetPixels(px);
        tex.Apply();
        return tex;
    }

    static Texture2D SparkleTexture()
    {
        const int s = 128;
        var tex = NewTex(s, s, "Star Sparkle");
        var px = new Color[s * s];
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float dx = (x + 0.5f) / s * 2f - 1f, dy = (y + 0.5f) / s * 2f - 1f;
                float r2 = dx * dx + dy * dy;
                float core = Mathf.Exp(-r2 * 60f);
                float glow = 0.35f * Mathf.Exp(-r2 * 8f);
                float rays = Mathf.Exp(-Mathf.Abs(dx) * 40f) * Mathf.Exp(-dy * dy * 5f) + Mathf.Exp(-Mathf.Abs(dy) * 40f) * Mathf.Exp(-dx * dx * 5f);
                float a = Mathf.Clamp01(core + glow + rays * 0.8f) * Mathf.Clamp01(1f - r2);
                px[y * s + x] = new Color(1f, 1f, 1f, a);
            }
        tex.SetPixels(px);
        tex.Apply();
        return tex;
    }

    // هلال: دائرة ناقص دائرة مزاحة
    static Texture2D CrescentTexture()
    {
        const int s = 256;
        var tex = NewTex(s, s, "Crescent");
        var px = new Color[s * s];
        float aa = 2f / s;
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float u = (x + 0.5f) / s - 0.5f, v = (y + 0.5f) / s - 0.5f;
                float outer = Mathf.Clamp01((0.42f - Mathf.Sqrt(u * u + v * v)) / aa);
                float cu = u - 0.16f, cv = v - 0.07f;
                float inner = Mathf.Clamp01((0.37f - Mathf.Sqrt(cu * cu + cv * cv)) / aa);
                float a = outer * (1f - inner);
                float shade = 0.88f + 0.12f * Mathf.PerlinNoise(x * 0.06f, y * 0.06f);
                px[y * s + x] = new Color(shade, shade, shade * 0.97f, a);
            }
        tex.SetPixels(px);
        tex.Apply();
        return tex;
    }

    // غيمة ناعمة: كور منفوخة فوق قاعدة مسطحة، بحواف ضبابية
    static Texture2D CloudTexture(int seed)
    {
        const int w = 256, h = 112;
        var tex = NewTex(w, h, "Cloud");
        var px = new Color[w * h];
        var rng = new System.Random(seed * 17);
        float aspect = (float)w / h;

        // كور كبيرة متداخلة (بإحداثيات فيها الدوائر دوائر فعلًا)
        var puffs = new List<Vector3>();
        int count = 4 + rng.Next(2);
        for (int i = 0; i < count; i++)
        {
            float t = (i + 0.5f) / count;
            float r = 0.2f + 0.16f * Mathf.Sin(t * Mathf.PI) + (float)rng.NextDouble() * 0.06f;
            puffs.Add(new Vector3((0.2f + t * 0.6f) * aspect, 0.3f + r * 0.55f, r));
        }

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float u = (x + 0.5f) / w, v = (y + 0.5f) / h;
                Vector2 q = new Vector2(u * aspect, v);

                // قاعدة بيضاوية عريضة + الكور فوقها
                float bx = (u - 0.5f) / 0.42f, by = (v - 0.32f) / 0.16f;
                float d = 1f - Mathf.Sqrt(bx * bx + by * by);
                foreach (Vector3 p in puffs)
                    d = Mathf.Max(d, 1f - Vector2.Distance(q, new Vector2(p.x, p.y)) / p.z);

                float noise = Mathf.PerlinNoise(u * 7f + seed * 3.1f, v * 7f) * 0.5f + Mathf.PerlinNoise(u * 19f, v * 19f + seed) * 0.25f;
                float a = Mathf.Clamp01((d - 0.05f + (noise - 0.35f) * 0.4f) * 3f);

                // تلاشي عند أطراف الصورة عشان ما تبان حواف مستطيلة
                float edge = Mathf.Clamp01(Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v)) * 12f);
                float light = 0.78f + 0.22f * Mathf.Clamp01(v * 1.5f);
                px[y * w + x] = new Color(light, light, light, a * edge);
            }
        tex.SetPixels(px);
        tex.Apply();
        return tex;
    }
}
