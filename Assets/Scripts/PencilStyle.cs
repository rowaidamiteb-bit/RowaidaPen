using System.Collections.Generic;
using UnityEngine;

// شكل ولون خط القلم اللي تختاره اللاعبة (للزينة بس، الخط يشتغل نفس الشي)
public static class PencilStyle
{
    public enum Style { Normal, Dotted, Glitter, Neon, Rainbow }

    public static readonly Color[] Colors =
    {
        new Color(0.21f, 0.21f, 0.23f),   // رصاصي
        new Color(0.95f, 0.42f, 0.6f),    // وردي
        new Color(1f, 0.55f, 0.2f),       // برتقالي
        new Color(1f, 0.8f, 0.15f),       // ذهبي
        new Color(0.3f, 0.75f, 0.4f),     // أخضر
        new Color(0.25f, 0.55f, 0.95f),   // أزرق
        new Color(0.6f, 0.4f, 0.9f),      // بنفسجي
        new Color(0.45f, 0.3f, 0.2f),     // بني
    };

    public static readonly string[] ColorNames = { "رصاصي", "وردي", "برتقالي", "ذهبي", "أخضر", "أزرق", "بنفسجي", "بني" };
    public static readonly string[] StyleNames = { "عادي", "منقط", "لامع", "نيون", "قوس قزح" };

    public static int ColorIndex
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt("pencilColor", 0), 0, Colors.Length - 1);
        set { PlayerPrefs.SetInt("pencilColor", value); PlayerPrefs.Save(); }
    }

    public static Style Current
    {
        get => (Style)Mathf.Clamp(PlayerPrefs.GetInt("pencilStyle", 0), 0, StyleNames.Length - 1);
        set { PlayerPrefs.SetInt("pencilStyle", (int)value); PlayerPrefs.Save(); }
    }

    public static Color CurrentColor => Colors[ColorIndex];

    static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
    static Texture2D dashTex, rainbowTex;

    // مادة الخط حسب اللون والشكل المختارين
    public static Material Material(GameManager gm)
    {
        Style style = Current;
        Color color = CurrentColor;
        string key = style + "_" + ColorIndex;
        if (cache.TryGetValue(key, out Material m) && m != null) return m;

        switch (style)
        {
            case Style.Neon:
            {
                // يضوي (فوق 1 = توهج مع الـ Bloom)
                m = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "Ink Neon" };
                Color glow = color * 1.7f;   // توهج بلونه (لو زاد يبيض)
                glow.a = 1f;
                m.SetColor("_BaseColor", glow);
                break;
            }
            case Style.Rainbow:
                m = new Material(gm.inkMaterial) { name = "Ink Rainbow" };
                m.SetTexture("_BaseMap", RainbowTexture());
                m.SetTextureScale("_BaseMap", new Vector2(0.35f, 1f));
                m.SetColor("_BaseColor", Color.white);
                m.SetFloat("_Smoothness", 0.5f);
                break;
            case Style.Glitter:
                m = new Material(gm.inkMaterial) { name = "Ink Glitter" };
                m.SetColor("_BaseColor", Color.Lerp(color, Color.white, 0.15f));
                m.SetFloat("_Smoothness", 0.95f);
                m.SetFloat("_Metallic", 0.75f);
                break;
            case Style.Dotted:
                // شرطات: نص الخط شفاف (Alpha Clip) والتصادم يظل كامل
                m = new Material(gm.inkMaterial) { name = "Ink Dotted" };
                m.SetColor("_BaseColor", color);
                m.SetTexture("_BaseMap", DashTexture());
                m.SetTextureScale("_BaseMap", new Vector2(2.6f, 1f));
                m.SetFloat("_AlphaClip", 1f);
                m.SetFloat("_Cutoff", 0.5f);
                m.EnableKeyword("_ALPHATEST_ON");
                m.SetFloat("_Cull", 0f);
                break;
            default:
                m = new Material(gm.inkMaterial) { name = "Ink" };
                m.SetColor("_BaseColor", color);
                break;
        }
        cache[key] = m;
        return m;
    }

    // زينة زيادة بعد ما يخلص الخط (اللامع: لمعات تطلع منه)
    public static void Decorate(GameObject stroke)
    {
        if (Current == Style.Glitter) stroke.AddComponent<GlitterTwinkle>();
    }

    static Texture2D DashTexture()
    {
        if (dashTex != null) return dashTex;
        dashTex = new Texture2D(16, 2, TextureFormat.RGBA32, false) { name = "Dash", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Point };
        var px = new Color[32];
        for (int i = 0; i < 32; i++) px[i] = new Color(1f, 1f, 1f, (i % 16) < 10 ? 1f : 0f);
        dashTex.SetPixels(px);
        dashTex.Apply();
        return dashTex;
    }

    public static Texture2D RainbowTexture()
    {
        if (rainbowTex != null) return rainbowTex;
        rainbowTex = new Texture2D(64, 2, TextureFormat.RGBA32, false) { name = "Rainbow", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
        var px = new Color[128];
        for (int i = 0; i < 128; i++) px[i] = Color.HSVToRGB((i % 64) / 64f, 0.6f, 1f);
        rainbowTex.SetPixels(px);
        rainbowTex.Apply();
        return rainbowTex;
    }
}

// لمعات صغيرة تطلع من خط القلم اللامع بين فترة وفترة
public class GlitterTwinkle : MonoBehaviour
{
    float next;
    Renderer rend;

    void Start()
    {
        rend = GetComponent<Renderer>();
    }

    void Update()
    {
        if (rend == null || Time.time < next) return;
        next = Time.time + Random.Range(0.15f, 0.4f);
        Bounds b = rend.bounds;
        Vector3 p = new Vector3(Random.Range(b.min.x, b.max.x), Random.Range(b.min.y, b.max.y), -0.15f);
        var mesh = GetComponent<MeshFilter>().sharedMesh;
        if (mesh != null && mesh.vertexCount > 0)
            p = transform.TransformPoint(mesh.vertices[Random.Range(0, mesh.vertexCount)]);
        Fx.Sparkle(p, Color.Lerp(PencilStyle.CurrentColor, Color.white, 0.6f), 4);
    }
}
