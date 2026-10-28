using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// الصفحة الأخيرة: ذكريات رويدا تطفو فوق الطريق. كل ذكرى صورة فورية (بولارويد) قاعدة على غيمة ناعمة،
// مكتوب تحتها اسم المرحلة، ووراها هالة دافية. أول ما تقرب رويدا تطلع الذكرى بلمعات ورنة، كأنها تتذكرها
public static class MemoryClouds
{
    struct Memory { public Texture picture; public string caption; }

    public static void Build(Transform root, LevelData data, GameManager gm)
    {
        if (!data.memoryClouds) return;
        BuildRainbow(root);

        // الذكريات: صورة مرحلة، بعدين رسمة من رسومات اللاعبة... يتناوبون
        var thumbs = new List<Memory>();
        for (int i = 0; i < Levels.All.Count - 1; i++)
        {
            var thumb = Resources.Load<Texture2D>("LevelThumbs/level_" + i);
            if (thumb != null) thumbs.Add(new Memory { picture = thumb, caption = Levels.All[i].title });
        }
        var drawings = new List<Memory>();
        int seed = 300;
        foreach (int level in DrawingMemory.Levels())
            if (level < Levels.All.Count - 1)
                drawings.Add(new Memory { picture = DrawingMemory.PageFor(level, seed++, 900f), caption = "من رسوماتي" });
        var memories = new List<Memory>();
        for (int i = 0; i < Mathf.Max(thumbs.Count, drawings.Count); i++)
        {
            if (i < thumbs.Count) memories.Add(thumbs[i]);
            if (i < drawings.Count) memories.Add(drawings[i]);
        }

        float startX = data.spawn.x + 4f, endX = data.door.x + 1f;
        int count = Mathf.Min(memories.Count, 8);
        for (int i = 0; i < count; i++)
        {
            float x = Mathf.Lerp(startX, endX, count == 1 ? 0f : i / (count - 1f));
            float y = 2.7f + (i % 2) * 0.45f;
            float tilt = (i % 2 == 0 ? -1f : 1f) * (3f + (i * 7 % 5));
            MemoryCard.Create(root, new Vector3(x, y, 4.8f + (i % 3) * 0.5f), memories[i].picture, memories[i].caption, tilt, i, gm);
        }
    }

    // قوس قزح باستيل كبير في السما (يمشي مع الكاميرا مثل الأشياء البعيدة)
    static void BuildRainbow(Transform root)
    {
        var sky = new GameObject("Rainbow").transform;
        sky.SetParent(root, false);
        sky.gameObject.AddComponent<ParallaxFollow>().factor = 0.92f;
        Color[] bands =
        {
            new Color(1f, 0.6f, 0.65f), new Color(1f, 0.78f, 0.55f), new Color(1f, 0.95f, 0.6f),
            new Color(0.65f, 0.92f, 0.7f), new Color(0.6f, 0.8f, 1f), new Color(0.78f, 0.65f, 1f),
        };
        for (int b = 0; b < bands.Length; b++)
        {
            var band = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "Rainbow Band" };
            band.SetColor("_BaseColor", bands[b] * 1.05f);
            var ring = new GameObject("Band");
            ring.transform.SetParent(sky, false);
            ring.transform.localPosition = new Vector3(10f, -14f, 70f);
            ring.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);   // محور الحلقة x: نلفها تواجه الكاميرا
            ring.AddComponent<MeshFilter>().sharedMesh = PaperMeshes.Ring(30f - b * 1.6f, 0.8f, 64, 4);
            ring.AddComponent<MeshRenderer>().sharedMaterial = band;
        }
    }
}

// ذكرى وحدة: صورة فورية على غيمة
public class MemoryCard : MonoBehaviour
{
    static Texture2D[] cloudTex;
    static Texture2D glowTex;

    Transform card;
    Vector3 home;
    float phase, tilt, appear = -1f;
    bool shown;
    int index;

    public static void Create(Transform root, Vector3 pos, Texture picture, string caption, float tilt, int index, GameManager gm)
    {
        if (cloudTex == null) cloudTex = new[] { NightSky.CloudTexture(4), NightSky.CloudTexture(5), NightSky.CloudTexture(6) };
        if (glowTex == null) glowTex = NightSky.DotTexture();

        var go = new GameObject("Memory");
        go.transform.SetParent(root, false);
        go.transform.position = pos;
        var m = go.AddComponent<MemoryCard>();
        m.home = pos;
        m.phase = index * 1.37f;
        m.tilt = tilt;
        m.index = index;

        // هالة دافية وردية ورا الذكرى
        var glow = NightSky.Transparent("Memory Glow", glowTex, new Color(1f, 0.82f, 0.9f, 0.55f));
        Quad(go.transform, "Glow", new Vector3(0f, 0.1f, 0.6f), new Vector2(5.2f, 4.4f), glow);

        // الغيمة: طبقات ناعمة (ورا الصورة، وقدامها من تحت عشان تبان قاعدة فيها)
        Color[] tints = { new Color(1f, 0.97f, 0.99f, 0.95f), new Color(1f, 0.88f, 0.94f, 0.9f), new Color(1f, 1f, 1f, 1f) };
        Quad(go.transform, "Cloud Back", new Vector3(0.2f, -0.55f, 0.35f), new Vector2(5f, 2.1f), NightSky.Transparent("Memory Cloud", cloudTex[index % 3], tints[1]));
        Quad(go.transform, "Cloud Mid", new Vector3(-0.5f, -0.95f, 0.15f), new Vector2(4.2f, 1.75f), NightSky.Transparent("Memory Cloud", cloudTex[(index + 1) % 3], tints[0]));

        // الصورة الفورية (مايلة شوي)
        m.card = new GameObject("Polaroid").transform;
        m.card.SetParent(go.transform, false);
        m.card.localRotation = Quaternion.Euler(0f, 0f, tilt);

        float aspect = (float)picture.width / picture.height;
        Vector2 size = aspect > 1f ? new Vector2(2.3f, 2.3f / aspect) : new Vector2(1.55f * aspect, 1.55f);
        const float side = 0.13f, bottom = 0.5f;
        Material paper = gm.Mat("Polaroid Paper", new Color(1f, 0.99f, 0.97f), 0.35f);
        LevelBuilder.Visual("Frame", PrimitiveType.Cube, m.card, new Vector3(0f, -bottom * 0.5f + side * 0.5f, 0.03f),
            new Vector3(size.x + side * 2f, size.y + side + bottom, 0.035f), paper, true);
        var pic = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "Memory Picture" };
        pic.SetTexture("_BaseMap", picture);
        pic.SetColor("_BaseColor", new Color(1f, 0.97f, 0.97f));
        Quad(m.card, "Picture", new Vector3(0f, 0f, 0f), size, pic);

        // شريط لاصق وردي شفاف شوي فوق
        var tape = NightSky.Transparent("Washi Tape", null, new Color(1f, 0.62f, 0.78f, 0.75f));
        var t = Quad(m.card, "Tape", new Vector3(0f, size.y * 0.5f + 0.05f, -0.03f), new Vector2(0.75f, 0.24f), tape);
        t.localRotation = Quaternion.Euler(0f, 0f, -tilt * 1.5f + 4f);

        // الكلام تحت الصورة (اسم المرحلة)
        Caption(m.card, ArabicText.Fix(caption), new Vector3(0f, -size.y * 0.5f - bottom * 0.45f, -0.02f), size.x + side);

        // الغيمة القدامية تغطي أسفل الصورة شوي
        Quad(go.transform, "Cloud Front", new Vector3(0.6f, -1.45f, -0.2f), new Vector2(4.6f, 1.6f), NightSky.Transparent("Memory Cloud", cloudTex[(index + 2) % 3], tints[2]));

        go.transform.localScale = Vector3.zero;   // تطلع لما تقرب رويدا
    }

    static Transform Quad(Transform parent, string name, Vector3 pos, Vector2 size, Material mat)
    {
        var q = LevelBuilder.Visual(name, PrimitiveType.Quad, parent, pos, new Vector3(size.x, size.y, 1f), mat, false);
        q.GetComponent<MeshRenderer>().receiveShadows = false;
        return q.transform;
    }

    // نص بخط واضح على الصورة الفورية (كانفس صغير داخل المشهد)
    static void Caption(Transform parent, string text, Vector3 pos, float width)
    {
        var go = new GameObject("Caption", typeof(RectTransform), typeof(Canvas));
        go.transform.SetParent(parent, false);
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width * 100f, 40f);
        rt.localPosition = pos;
        rt.localScale = Vector3.one * 0.01f;

        var t = new GameObject("Text", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
        t.transform.SetParent(go.transform, false);
        t.rectTransform.anchorMin = Vector2.zero;
        t.rectTransform.anchorMax = Vector2.one;
        t.rectTransform.offsetMin = t.rectTransform.offsetMax = Vector2.zero;
        t.font = ArabicText.Font;
        t.fontSize = ArabicText.Size(26);
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = new Color(0.78f, 0.33f, 0.55f);
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.text = text;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (!shown && gm != null && gm.Rowaida != null && gm.Rowaida.transform.position.x > home.x - 9f)
        {
            // تتذكرها: تطلع بلمعات ورنة
            shown = true;
            appear = 0f;
            gm.Sound.Play(SynthAudio.Ding(0.75f + (index % 4) * 0.12f), 0.35f);
            for (int i = 0; i < 3; i++) Fx.Sparkle(home + Random.insideUnitSphere * 0.8f + Vector3.back * 0.6f, new Color(1f, 0.85f, 0.92f), 10);
        }

        float t = Time.time + phase;
        float s = 1f;
        if (!shown) s = 0f;
        else if (appear < 1f)
        {
            appear = Mathf.Min(1f, appear + Time.deltaTime / 0.7f);
            s = 1f + Mathf.Sin(appear * Mathf.PI) * 0.12f - (1f - appear) * (1f - appear);
        }
        transform.localScale = Vector3.one * Mathf.Max(0f, s);
        float rise = shown ? (1f - Mathf.Clamp01(appear)) * -1.2f : -1.2f;
        transform.position = home + new Vector3(Mathf.Sin(t * 0.45f) * 0.12f, Mathf.Sin(t * 0.8f) * 0.15f + rise, 0f);
        if (card != null) card.localRotation = Quaternion.Euler(0f, 0f, tilt + Mathf.Sin(t * 0.9f) * 2.5f);
    }
}

// قليتر: لمعات صغيرة تطلع من سطح الصفحة (بس لما تكون قريبة من الكاميرا)
public class PageGlitter : MonoBehaviour
{
    static readonly Color[] Colors = { new Color(1f, 0.7f, 0.85f), new Color(1f, 0.9f, 0.5f), Color.white, new Color(0.85f, 0.7f, 1f) };
    Bounds bounds;
    float next;

    void Start()
    {
        var r = GetComponent<Renderer>();
        if (r != null) bounds = r.bounds;
    }

    void Update()
    {
        if (Time.time < next) return;
        next = Time.time + Random.Range(0.025f, 0.07f);
        Camera cam = Camera.main;
        if (cam == null || Mathf.Abs(cam.transform.position.x - bounds.center.x) > bounds.extents.x + 12f) return;
        Vector3 p = new Vector3(Random.Range(bounds.min.x, bounds.max.x), bounds.max.y + 0.03f, Random.Range(bounds.min.z, bounds.max.z));
        var c = Colors[Random.Range(0, Colors.Length)];
        Material m = GameManager.Instance.Mat("Glitter " + ColorUtility.ToHtmlStringRGB(c), c * 2.2f, 0.95f);
        var g = LevelBuilder.Visual("Glitter", PrimitiveType.Cube, null, p, Vector3.one * Random.Range(0.05f, 0.09f), m, false);
        g.transform.rotation = Quaternion.Euler(0f, 45f, 45f);
        var fx = g.AddComponent<FxParticle>();
        fx.velocity = new Vector3(0f, Random.Range(0.1f, 0.4f), 0f);
        fx.spin = new Vector3(0f, 200f, 0f);
        fx.life = Random.Range(0.6f, 1.1f);
    }
}
