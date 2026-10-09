using System.Collections.Generic;
using UnityEngine;

// الصفحة الأخيرة: غيوم منفوخة فوق الطريق، وفي كل غيمة إطار صورة فيه ذكرى من رحلة رويدا
// (صور المراحل اللي عدّتها، والرسومات اللي رسمتيها)
public static class MemoryClouds
{
    public static void Build(Transform root, LevelData data, GameManager gm)
    {
        if (!data.memoryClouds) return;

        // الذكريات: رسومات اللاعبة أول (إذا فيه)، وبعدها صور المراحل
        var drawings = new List<Texture>();
        int seed = 300;
        foreach (int level in DrawingMemory.Levels())
            if (level < Levels.All.Count - 1) drawings.Add(DrawingMemory.PageFor(level, seed++, 900f));
        var thumbs = new List<Texture>();
        for (int i = 0; i < Levels.All.Count - 1; i++)
        {
            var thumb = Resources.Load<Texture2D>("LevelThumbs/level_" + i);
            if (thumb != null) thumbs.Add(thumb);
        }
        // صورة مرحلة، بعدين رسمة، بعدين صورة... (يتناوبون)
        var mixed = new List<Texture>();
        for (int i = 0; i < Mathf.Max(drawings.Count, thumbs.Count); i++)
        {
            if (i < thumbs.Count) mixed.Add(thumbs[i]);
            if (i < drawings.Count) mixed.Add(drawings[i]);
        }

        // قوس قزح باستيل كبير في السما (يمشي مع الكاميرا مثل الأشياء البعيدة)
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

        Material cloud = gm.Mat("Memory Cloud", new Color(1f, 0.96f, 0.98f), 0.25f);
        Material cloudPink = gm.Mat("Memory Cloud Pink", new Color(1f, 0.86f, 0.92f), 0.25f);
        Material frame = gm.Mat("Memory Frame", new Color(1f, 0.99f, 0.97f), 0.4f);
        Material tape = gm.Mat("Memory Tape", new Color(1f, 0.75f, 0.85f), 0.2f);
        Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");

        float startX = data.spawn.x + 3f, endX = data.door.x + 2f;
        int count = Mathf.Min(mixed.Count, 9);
        var rng = new System.Random(77);
        for (int i = 0; i < count; i++)
        {
            float x = Mathf.Lerp(startX, endX, count == 1 ? 0f : i / (count - 1f));
            float y = 3.1f + (i % 2) * 0.55f;
            var go = new GameObject("Memory Cloud");
            go.transform.SetParent(root, false);
            go.transform.position = new Vector3(x, y, 5f + (i % 2) * 0.8f);
            go.AddComponent<FloatBob>().phase = i * 1.3f;

            // الغيمة: كور منفوخة حول الصورة
            for (int k = 0; k < 11; k++)
            {
                float a = k / 11f * Mathf.PI * 2f;
                Vector3 p = new Vector3(Mathf.Cos(a) * 1.8f, Mathf.Sin(a) * 1.2f - 0.1f, 0.35f);
                float s = 0.9f + (float)rng.NextDouble() * 0.6f;
                LevelBuilder.Visual("Puff", PrimitiveType.Sphere, go.transform, p, new Vector3(s, s * 0.85f, s * 0.8f), k % 3 == 0 ? cloudPink : cloud, false);
            }

            // إطار صورة (مثل صورة فورية) وشريط لاصق
            Texture tex = mixed[i];
            float aspect = (float)tex.width / tex.height;
            Vector2 size = aspect > 1f ? new Vector2(2.5f, 2.5f / aspect) : new Vector2(1.6f * aspect, 1.6f);
            LevelBuilder.Visual("Frame", PrimitiveType.Cube, go.transform, new Vector3(0f, -0.05f, 0.02f), new Vector3(size.x + 0.22f, size.y + 0.34f, 0.04f), frame, false);
            var pic = new Material(unlit) { name = "Memory Picture" };
            pic.SetTexture("_BaseMap", tex);
            pic.SetColor("_BaseColor", Color.white);
            var quad = LevelBuilder.Visual("Picture", PrimitiveType.Quad, go.transform, new Vector3(0f, 0.02f, -0.01f), new Vector3(size.x, size.y, 1f), pic, false);
            var t2 = LevelBuilder.Visual("Tape", PrimitiveType.Cube, go.transform, new Vector3(0f, size.y * 0.5f + 0.12f, -0.03f), new Vector3(0.5f, 0.14f, 0.01f), tape, false);
            t2.transform.localRotation = Quaternion.Euler(0f, 0f, ((float)rng.NextDouble() - 0.5f) * 20f);
            go.transform.localRotation = Quaternion.Euler(0f, 0f, ((float)rng.NextDouble() - 0.5f) * 8f);
        }
    }
}

// تطفو فوق وتحت بهدوء وتلمع حولها لمعات
public class FloatBob : MonoBehaviour
{
    public float phase;
    Vector3 home;
    float nextSparkle;

    void Start() { home = transform.position; }

    void Update()
    {
        float t = Time.time + phase;
        transform.position = home + new Vector3(Mathf.Sin(t * 0.5f) * 0.15f, Mathf.Sin(t * 0.9f) * 0.18f, 0f);
        if (Time.time >= nextSparkle)
        {
            nextSparkle = Time.time + Random.Range(0.3f, 0.7f);
            Fx.Sparkle(transform.position + new Vector3(Random.Range(-1.7f, 1.7f), Random.Range(-1.1f, 1.1f), -0.4f), new Color(1f, 0.8f, 0.9f), 3);
        }
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
