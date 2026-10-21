using System.Collections.Generic;
using UnityEngine;

// تفاصيل زيادة لمراحل الجزء الثاني: أغراض صغيرة تتحرك ورا الطريق حسب الفصل، وأشياء طايرة بالسما.
// البحر: صدف ونجوم بحر ونوارس. المدرسة: أوراق لاصقة ودبابيس وطيارات ورق.
// الوقت: تروس صغيرة تلف وساعات جيب ورمل وصبار. الموسيقى: نوتات تطلع وسماعات تنبض وطبول صغيرة
public class Part2Details : MonoBehaviour
{
    enum Chapter { None, Sea, Underwater, School, Time, Desert, Music }

    readonly List<Transform> spinners = new List<Transform>();
    readonly List<float> spinSpeed = new List<float>();
    readonly List<Transform> pulsers = new List<Transform>();
    readonly List<Transform> flyers = new List<Transform>();
    readonly List<Vector3> flyerData = new List<Vector3>();   // سرعة، ارتفاع، طور
    readonly List<Transform> risers = new List<Transform>();
    readonly List<Vector3> riserBase = new List<Vector3>();
    float minX, maxX;
    Chapter chapter;

    static Chapter Of(LevelData data)
    {
        switch (data.ambience)
        {
            case "sea": case "night_sea": case "storm": return Chapter.Sea;
            case "underwater": return Chapter.Underwater;
            case "classroom": return Chapter.School;
            case "clock": case "factory": case "day": case "night": return Chapter.Time;
            case "desert": return Chapter.Desert;
            case "hall": case "crowd": return Chapter.Music;
        }
        return Chapter.None;
    }

    public static void Build(Transform root, LevelData data, GameManager gm, System.Random rng)
    {
        Chapter ch = Of(data);
        if (ch == Chapter.None) return;
        var go = new GameObject("Part 2 Details");
        go.transform.SetParent(root, false);
        var d = go.AddComponent<Part2Details>();
        d.chapter = ch;
        float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        d.minX = float.MaxValue; d.maxX = float.MinValue;

        foreach (Block b in data.blocks)
        {
            float top = b.center.y + b.size.y * 0.5f, x0 = b.center.x - b.size.x * 0.5f, x1 = b.center.x + b.size.x * 0.5f;
            d.minX = Mathf.Min(d.minX, x0); d.maxX = Mathf.Max(d.maxX, x1);
            if (x1 - x0 < 2.5f) continue;
            for (float x = x0 + 0.9f; x < x1 - 0.7f; x += R(2.2f, 3.6f))
            {
                if (Mathf.Abs(x - data.door.x) < 1.8f || Mathf.Abs(x - data.spawn.x) < 1.2f) continue;
                Vector3 p = new Vector3(x, top, R(1.1f, 1.8f));
                d.Prop(go.transform, p, rng.Next(4), gm, rng);
            }
        }

        // أشياء طايرة بالسما (ورا)
        int count = ch == Chapter.Underwater ? 0 : 7;
        for (int i = 0; i < count; i++)
        {
            Vector3 p = new Vector3(R(d.minX, d.maxX), R(5f, 9f), R(5f, 9f));
            Transform f = d.Flyer(go.transform, p, gm, rng);
            if (f == null) continue;
            d.flyers.Add(f);
            d.flyerData.Add(new Vector3(R(0.8f, 1.8f) * (rng.Next(2) == 0 ? 1f : -1f), p.y, R(0f, 6f)));
        }
    }

    void Prop(Transform parent, Vector3 p, int kind, GameManager gm, System.Random rng)
    {
        float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        var t = new GameObject("Detail").transform;
        t.SetParent(parent, false);
        t.position = p;
        t.localRotation = Quaternion.Euler(0f, R(-30f, 30f), 0f);
        switch (chapter)
        {
            case Chapter.Sea:
            case Chapter.Underwater:
                if (kind < 2)
                {
                    // نجمة بحر
                    Material m = gm.Mat("Starfish " + kind, kind == 0 ? new Color(1f, 0.5f, 0.35f) : new Color(1f, 0.75f, 0.3f), 0.4f);
                    for (int i = 0; i < 5; i++)
                    {
                        var arm = LevelBuilder.Visual("Arm", PrimitiveType.Capsule, t, Vector3.zero, new Vector3(0.12f, 0.2f, 0.06f), m, false).transform;
                        arm.localRotation = Quaternion.Euler(90f, i * 72f, 0f);
                        arm.localPosition = Quaternion.Euler(0f, i * 72f, 0f) * new Vector3(0f, 0.04f, 0.17f);
                    }
                }
                else if (kind == 2)
                {
                    // صدفة
                    Material m = gm.Mat("Shell Pink", new Color(1f, 0.82f, 0.8f), 0.6f);
                    LevelBuilder.Visual("Shell", PrimitiveType.Sphere, t, new Vector3(0f, 0.08f, 0f), new Vector3(0.4f, 0.16f, 0.32f), m, false);
                    for (int i = -1; i <= 1; i++)
                        LevelBuilder.Visual("Rib", PrimitiveType.Cube, t, new Vector3(i * 0.09f, 0.14f, 0f), new Vector3(0.02f, 0.04f, 0.3f), gm.Mat("Shell Rib", new Color(0.9f, 0.6f, 0.6f), 0.4f), false).transform.localRotation = Quaternion.Euler(0f, i * 25f, 0f);
                }
                else
                {
                    // سطل ورمل صغير (ولو تحت المويه: فقاعة تطلع)
                    Material bucket = gm.Mat("Toy Bucket", new Color(0.3f, 0.7f, 1f), 0.4f);
                    LevelBuilder.Visual("Bucket", PrimitiveType.Cylinder, t, new Vector3(0f, 0.22f, 0f), new Vector3(0.4f, 0.22f, 0.4f), bucket);
                    LevelBuilder.Visual("Sand", PrimitiveType.Sphere, t, new Vector3(0.45f, 0.05f, 0f), new Vector3(0.5f, 0.2f, 0.45f), gm.Mat("Sand Pile", new Color(0.95f, 0.85f, 0.6f), 0.2f), false);
                    if (chapter == Chapter.Underwater) AddRiser(t.position + Vector3.up * 0.3f, gm.Mat("Air Bubble", new Color(0.8f, 0.95f, 1f), 0.98f), PrimitiveType.Sphere, 0.15f, parent);
                }
                break;
            case Chapter.School:
                if (kind == 0)
                {
                    // ورقة لاصقة واقفة ورا
                    Color[] cs = { new Color(1f, 0.92f, 0.4f), new Color(1f, 0.6f, 0.75f), new Color(0.6f, 0.9f, 0.6f) };
                    Material m = gm.Mat("Sticky " + rng.Next(3), cs[rng.Next(3)], 0.2f);
                    var note = LevelBuilder.Visual("Sticky Note", PrimitiveType.Cube, t, new Vector3(0f, 0.45f, 0f), new Vector3(0.8f, 0.8f, 0.02f), m).transform;
                    note.localRotation = Quaternion.Euler(-10f, 0f, R(-12f, 12f));
                    for (int i = 0; i < 3; i++)
                        LevelBuilder.Visual("Scribble", PrimitiveType.Cube, note, new Vector3(0f, 0.2f - i * 0.2f, -0.6f), new Vector3(0.7f - i * 0.15f, 0.04f, 0.2f), gm.Mat("Ink Scribble", new Color(0.2f, 0.25f, 0.45f), 0.2f), false);
                    pulsers.Add(note);
                }
                else if (kind == 1)
                {
                    // دبوس
                    Material m = gm.Mat("Pin Head", new Color(0.95f, 0.3f, 0.3f), 0.6f);
                    LevelBuilder.Visual("Pin", PrimitiveType.Cylinder, t, new Vector3(0f, 0.15f, 0f), new Vector3(0.04f, 0.15f, 0.04f), gm.Mat("Sharpener Steel", new Color(0.8f, 0.82f, 0.86f), 0.9f, 0.8f), false);
                    LevelBuilder.Visual("Head", PrimitiveType.Sphere, t, new Vector3(0f, 0.32f, 0f), Vector3.one * 0.18f, m);
                }
                else if (kind == 2)
                {
                    // مكعبات حروف
                    for (int i = 0; i < 2; i++)
                    {
                        Material m = gm.Mat("Letter Block " + i, MusicFeatures.KeyColors[(i * 2 + rng.Next(3)) % 5], 0.3f);
                        var c = LevelBuilder.Visual("Letter Block", PrimitiveType.Cube, t, new Vector3(i * 0.35f, 0.17f + i * 0.0f, i * 0.1f), Vector3.one * 0.34f, m).transform;
                        c.localRotation = Quaternion.Euler(0f, R(-20f, 20f), 0f);
                    }
                }
                else
                {
                    // براية صغيرة ونشارة
                    LevelBuilder.Visual("Mini Sharpener", PrimitiveType.Cube, t, new Vector3(0f, 0.12f, 0f), new Vector3(0.3f, 0.24f, 0.22f), gm.Mat("Sharpener Body", new Color(0.3f, 0.55f, 0.9f), 0.6f, 0.3f));
                    for (int i = 0; i < 3; i++)
                        LevelBuilder.Visual("Shaving", PrimitiveType.Cube, t, new Vector3(0.3f + i * 0.12f, 0.02f, R(-0.15f, 0.15f)), new Vector3(0.16f, 0.02f, 0.1f), gm.Mat("Shaving Wood", new Color(0.85f, 0.65f, 0.4f), 0.2f), false).transform.localRotation = Quaternion.Euler(0f, R(0f, 180f), 0f);
                }
                break;
            case Chapter.Time:
            case Chapter.Desert:
                if (chapter == Chapter.Desert && kind < 2)
                {
                    // صبار
                    Material m = gm.Mat("Cactus", new Color(0.35f, 0.65f, 0.35f), 0.3f);
                    LevelBuilder.Visual("Trunk", PrimitiveType.Capsule, t, new Vector3(0f, 0.55f, 0f), new Vector3(0.3f, 0.55f, 0.3f), m);
                    LevelBuilder.Visual("Arm", PrimitiveType.Capsule, t, new Vector3(0.25f, 0.7f, 0f), new Vector3(0.18f, 0.25f, 0.18f), m);
                    LevelBuilder.Visual("Arm", PrimitiveType.Capsule, t, new Vector3(-0.22f, 0.5f, 0f), new Vector3(0.16f, 0.2f, 0.16f), m);
                    LevelBuilder.Visual("Flower", PrimitiveType.Sphere, t, new Vector3(0f, 1.12f, 0f), Vector3.one * 0.14f, gm.Mat("Cactus Flower", new Color(1f, 0.5f, 0.7f), 0.4f), false);
                }
                else if (kind < 2)
                {
                    // ترس صغير يلف
                    Material m = gm.Mat("Gear Brass", new Color(0.85f, 0.65f, 0.3f), 0.7f, 0.5f);
                    var g = new GameObject("Mini Gear").transform;
                    g.SetParent(t, false);
                    g.localPosition = new Vector3(0f, 0.55f, 0f);
                    LevelBuilder.Visual("Disc", PrimitiveType.Cylinder, g, Vector3.zero, new Vector3(0.8f, 0.05f, 0.8f), m).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    for (int i = 0; i < 8; i++)
                    {
                        var tooth = LevelBuilder.Visual("Tooth", PrimitiveType.Cube, g, Quaternion.Euler(0f, 0f, i * 45f) * new Vector3(0f, 0.45f, 0f), new Vector3(0.14f, 0.14f, 0.09f), m, false).transform;
                        tooth.localRotation = Quaternion.Euler(0f, 0f, i * 45f);
                    }
                    spinners.Add(g); spinSpeed.Add(R(40f, 90f) * (rng.Next(2) == 0 ? 1f : -1f));
                }
                else if (kind == 2)
                {
                    // ساعة جيب عقربها يلف
                    Material gold = gm.Mat("Gear Brass", new Color(0.85f, 0.65f, 0.3f), 0.7f, 0.5f);
                    Material face = gm.Mat("Clock Face", new Color(0.98f, 0.96f, 0.9f), 0.3f);
                    var w = new GameObject("Pocket Watch").transform;
                    w.SetParent(t, false);
                    w.localPosition = new Vector3(0f, 0.38f, 0f);
                    w.localRotation = Quaternion.Euler(-15f, 0f, 0f);
                    LevelBuilder.Visual("Case", PrimitiveType.Cylinder, w, Vector3.zero, new Vector3(0.7f, 0.05f, 0.7f), gold).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    LevelBuilder.Visual("Face", PrimitiveType.Cylinder, w, new Vector3(0f, 0f, -0.05f), new Vector3(0.58f, 0.02f, 0.58f), face, false).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    LevelBuilder.Visual("Ring", PrimitiveType.Sphere, w, new Vector3(0f, 0.4f, 0f), new Vector3(0.14f, 0.14f, 0.05f), gold, false);
                    var hand = new GameObject("Hand").transform;
                    hand.SetParent(w, false);
                    hand.localPosition = new Vector3(0f, 0f, -0.08f);
                    LevelBuilder.Visual("Needle", PrimitiveType.Cube, hand, new Vector3(0f, 0.12f, 0f), new Vector3(0.03f, 0.24f, 0.01f), gm.Mat("Note Black", new Color(0.12f, 0.1f, 0.15f), 0.6f), false);
                    spinners.Add(hand); spinSpeed.Add(-120f);
                }
                else
                {
                    // كومة رمل صغيرة وحبات
                    Material sand = gm.Mat("Sand Pile", new Color(0.95f, 0.85f, 0.6f), 0.2f);
                    LevelBuilder.Visual("Sand", PrimitiveType.Sphere, t, new Vector3(0f, 0.05f, 0f), new Vector3(0.8f, 0.3f, 0.6f), sand, false);
                    LevelBuilder.Visual("Sand", PrimitiveType.Sphere, t, new Vector3(0.4f, 0.03f, 0.1f), new Vector3(0.4f, 0.15f, 0.35f), sand, false);
                }
                break;
            case Chapter.Music:
                if (kind == 0)
                {
                    // سماعة تنبض مع الموسيقى
                    Material box = gm.Mat("Piano Wood", new Color(0.15f, 0.12f, 0.14f), 0.7f);
                    Material cone = gm.Mat("Speaker Cone", new Color(0.35f, 0.35f, 0.4f), 0.4f);
                    LevelBuilder.Visual("Speaker", PrimitiveType.Cube, t, new Vector3(0f, 0.5f, 0f), new Vector3(0.6f, 1f, 0.5f), box);
                    var c1 = LevelBuilder.Visual("Cone", PrimitiveType.Sphere, t, new Vector3(0f, 0.35f, -0.25f), new Vector3(0.42f, 0.42f, 0.1f), cone, false).transform;
                    LevelBuilder.Visual("Tweeter", PrimitiveType.Sphere, t, new Vector3(0f, 0.8f, -0.25f), new Vector3(0.18f, 0.18f, 0.06f), cone, false);
                    pulsers.Add(c1);
                }
                else if (kind == 1)
                {
                    // طبلة صغيرة
                    Material drum = gm.Mat("Drum Red", new Color(0.9f, 0.25f, 0.3f), 0.5f);
                    Material skin = gm.Mat("Drum Skin", new Color(0.97f, 0.95f, 0.88f), 0.3f);
                    LevelBuilder.Visual("Drum", PrimitiveType.Cylinder, t, new Vector3(0f, 0.22f, 0f), new Vector3(0.6f, 0.22f, 0.6f), drum);
                    var s = LevelBuilder.Visual("Skin", PrimitiveType.Cylinder, t, new Vector3(0f, 0.45f, 0f), new Vector3(0.58f, 0.01f, 0.58f), skin, false).transform;
                    pulsers.Add(s);
                }
                else
                {
                    // نوتات صغيرة تطلع وتختفي
                    Material ink = gm.Mat("Note Color " + kind, MusicFeatures.KeyColors[rng.Next(5)], 0.5f);
                    AddRiser(t.position + Vector3.up * 0.4f, ink, PrimitiveType.Sphere, 0.22f, parent);
                }
                break;
        }
    }

    void AddRiser(Vector3 at, Material m, PrimitiveType type, float size, Transform parent)
    {
        var r = new GameObject("Riser").transform;
        r.SetParent(parent, false);
        r.position = at;
        LevelBuilder.Visual("Head", type, r, Vector3.zero, new Vector3(size * 1.3f, size, size * 0.6f), m, false);
        if (chapter == Chapter.Music)
            LevelBuilder.Visual("Stem", PrimitiveType.Cube, r, new Vector3(size * 0.55f, size * 1.6f, 0f), new Vector3(size * 0.2f, size * 3f, size * 0.2f), m, false);
        risers.Add(r); riserBase.Add(at);
    }

    Transform Flyer(Transform parent, Vector3 p, GameManager gm, System.Random rng)
    {
        var f = new GameObject("Flyer").transform;
        f.SetParent(parent, false);
        f.position = p;
        switch (chapter)
        {
            case Chapter.Sea:
            {
                // نورس (جناحين على شكل V)
                Material white = gm.Mat("Gull", new Color(0.97f, 0.97f, 0.97f), 0.3f);
                LevelBuilder.Visual("Body", PrimitiveType.Sphere, f, Vector3.zero, new Vector3(0.45f, 0.15f, 0.15f), white, false);
                for (int s = -1; s <= 1; s += 2)
                    LevelBuilder.Visual("Wing", PrimitiveType.Cube, f, new Vector3(0f, 0.08f, s * 0.3f), new Vector3(0.2f, 0.03f, 0.55f), white, false).transform.localRotation = Quaternion.Euler(s * 25f, 0f, 0f);
                f.localRotation = Quaternion.Euler(0f, 0f, 0f);
                break;
            }
            case Chapter.School:
            {
                Material paper = gm.Mat("Bird Paper", new Color(0.97f, 0.95f, 0.9f), 0.2f);
                LevelBuilder.Visual("Plane", PrimitiveType.Cube, f, Vector3.zero, new Vector3(0.8f, 0.04f, 0.5f), paper, false);
                LevelBuilder.Visual("Keel", PrimitiveType.Cube, f, new Vector3(0f, -0.08f, 0f), new Vector3(0.7f, 0.15f, 0.03f), paper, false);
                break;
            }
            case Chapter.Time:
            case Chapter.Desert:
            {
                // ريشة طايرة / ورقة تقويم
                Material m = gm.Mat("Calendar Page", new Color(0.98f, 0.97f, 0.93f), 0.2f);
                LevelBuilder.Visual("Page", PrimitiveType.Cube, f, Vector3.zero, new Vector3(0.5f, 0.6f, 0.02f), m, false);
                LevelBuilder.Visual("Header", PrimitiveType.Cube, f, new Vector3(0f, 0.24f, -0.015f), new Vector3(0.5f, 0.12f, 0.01f), gm.Mat("Tower Red", new Color(0.85f, 0.2f, 0.22f), 0.35f), false);
                break;
            }
            case Chapter.Music:
            {
                Material m = gm.Mat("Note Color Sky " + rng.Next(5), MusicFeatures.KeyColors[rng.Next(5)], 0.5f);
                LevelBuilder.Visual("Head", PrimitiveType.Sphere, f, Vector3.zero, new Vector3(0.45f, 0.34f, 0.15f), m, false);
                LevelBuilder.Visual("Stem", PrimitiveType.Cube, f, new Vector3(0.2f, 0.45f, 0f), new Vector3(0.06f, 0.9f, 0.06f), m, false);
                LevelBuilder.Visual("Flag", PrimitiveType.Cube, f, new Vector3(0.35f, 0.75f, 0f), new Vector3(0.3f, 0.08f, 0.06f), m, false).transform.localRotation = Quaternion.Euler(0f, 0f, -30f);
                break;
            }
            default:
                Destroy(f.gameObject);
                return null;
        }
        return f;
    }

    void Update()
    {
        float t = Time.time, dt = Time.deltaTime;
        for (int i = 0; i < spinners.Count; i++) if (spinners[i] != null) spinners[i].Rotate(0f, 0f, spinSpeed[i] * dt, Space.Self);
        for (int i = 0; i < pulsers.Count; i++)
        {
            if (pulsers[i] == null) continue;
            float beat = chapter == Chapter.Music ? Mathf.Pow(Mathf.Abs(Mathf.Sin(t * Mathf.PI * 2f + i)), 6f) : 0f;
            if (chapter == Chapter.School) pulsers[i].localRotation = Quaternion.Euler(-10f, 0f, Mathf.Sin(t * 1.5f + i) * 6f);
            else pulsers[i].localScale = BaseScale(pulsers[i]) * (1f + beat * 0.15f);
        }
        for (int i = 0; i < risers.Count; i++)
        {
            float k = Mathf.Repeat(t * 0.35f + i * 0.31f, 1f);
            risers[i].position = riserBase[i] + new Vector3(Mathf.Sin(k * 9f + i) * 0.2f, k * 2.4f, 0f);
            risers[i].localScale = Vector3.one * Mathf.Sin(k * Mathf.PI);
        }
        for (int i = 0; i < flyers.Count; i++)
        {
            Vector3 d = flyerData[i];
            Vector3 p = flyers[i].position;
            p.x += d.x * dt;
            if (p.x > maxX + 8f) p.x = minX - 8f;
            if (p.x < minX - 8f) p.x = maxX + 8f;
            p.y = d.y + Mathf.Sin(t * 0.8f + d.z) * 0.6f;
            flyers[i].position = p;
            float face = d.x > 0f ? 0f : 180f;
            if (chapter == Chapter.Sea)
            {
                flyers[i].localRotation = Quaternion.Euler(0f, face, 0f);
                foreach (Transform w in flyers[i])
                    if (w.name == "Wing") w.localRotation = Quaternion.Euler(Mathf.Sign(w.localPosition.z) * (25f + Mathf.Sin(t * 8f + d.z) * 25f), 0f, 0f);
            }
            else if (chapter == Chapter.Time || chapter == Chapter.Desert) flyers[i].localRotation = Quaternion.Euler(Mathf.Sin(t * 2f + d.z) * 40f, t * 60f + d.z * 30f, Mathf.Sin(t * 1.3f + d.z) * 30f);
            else flyers[i].localRotation = Quaternion.Euler(0f, face, Mathf.Sin(t * 1.5f + d.z) * 10f);
        }
    }

    readonly Dictionary<Transform, Vector3> baseScales = new Dictionary<Transform, Vector3>();
    Vector3 BaseScale(Transform t)
    {
        if (!baseScales.TryGetValue(t, out Vector3 s)) { s = t.localScale; baseScales[t] = s; }
        return s;
    }
}
