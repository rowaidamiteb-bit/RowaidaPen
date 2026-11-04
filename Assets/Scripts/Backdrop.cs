using System.Collections.Generic;
using UnityEngine;

// خلفية فصل الدفتر (المراحل اللي ما لها منظر خاص): جزر دفاتر صغيرة طايرة بعيد عليها أغراض رسم،
// ووراها أغراض رسم عملاقة واقفة بالضباب (أقلام، ألوان شمع، كتب، مساطر) كأنها مدينة.
// الأشياء البعيدة تاخذ لون السما شوي عشان تبان بعيدة وما تزحم اللعب
public class Backdrop : MonoBehaviour
{
    readonly List<Transform> bob = new List<Transform>();
    readonly List<Vector3> home = new List<Vector3>();
    readonly List<float> phase = new List<float>();
    static Mesh ringMesh;

    // نوع الأغراض العملاقة: 0 أدوات رسم، 1 موسيقى، 2 ساعات وتروس، 3 صحرا
    static int Style(LevelData data)
    {
        switch (data.ambience)
        {
            case "hall": case "crowd": return 1;
            case "clock": case "factory": case "day": case "night": return 2;
            case "desert": return 3;
            case "classroom": return 0;
        }
        return string.IsNullOrEmpty(data.ambience) ? 0 : -1;
    }

    public static bool Wants(LevelData data) =>
        Style(data) >= 0 && !data.sea && !data.underwater && !data.forest && data.kitchenScenery == 0 &&
        data.lab == 0 && data.gameStyle == 0 && data.dreamStyle == 0 && !data.finale && data.shadowChase.z <= 0f;

    public static void Create(Transform root, LevelData data, GameManager gm, System.Random rng)
    {
        if (!Wants(data)) return;
        var go = new GameObject("Backdrop");
        go.transform.SetParent(root, false);
        var b = go.AddComponent<Backdrop>();
        float R(float a, float c) => a + (float)rng.NextDouble() * (c - a);
        float minX = float.MaxValue, maxX = float.MinValue;
        foreach (Block bl in data.blocks) { minX = Mathf.Min(minX, bl.center.x - bl.size.x * 0.5f); maxX = Mathf.Max(maxX, bl.center.x + bl.size.x * 0.5f); }
        if (minX > maxX) { minX = -10f; maxX = 40f; }
        LevelTheme theme = data.theme;
        Color haze = Color.Lerp(theme.fog, theme.skyMid, 0.6f);
        bool flipped = data.flipped;

        // جزر دفاتر طايرة (قريبة شوي)
        for (float x = minX - 18f; x < maxX + 22f; x += R(9f, 15f))
        {
            var island = new GameObject("Floating Notebook").transform;
            island.SetParent(go.transform, false);
            float y = flipped ? R(0f, 8f) : R(-7f, 2f);
            island.position = new Vector3(x, y, R(18f, 32f));
            island.rotation = Quaternion.Euler(R(-4f, 4f), R(-25f, 25f), R(-4f, 4f));
            island.localScale = Vector3.one * R(0.55f, 0.8f);
            b.Island(island, theme, gm, rng, R);
            Hazify(island.gameObject, haze, 0.42f);
            b.bob.Add(island); b.home.Add(island.position); b.phase.Add(R(0f, 6.28f));
        }

        // أغراض عملاقة واقفة بعيد
        for (float x = minX - 50f; x < maxX + 55f; x += R(13f, 22f))
        {
            var giant = new GameObject("Giant").transform;
            giant.SetParent(go.transform, false);
            giant.position = new Vector3(x, flipped ? 44f : -44f, R(95f, 125f));
            giant.localScale = Vector3.one * R(8.5f, 12f);
            int style = Style(data);
            if (style == 0) b.Giant(giant, theme, gm, rng, R);
            else b.ThemedGiant(giant, style, gm, rng, R);
            if (flipped) giant.localRotation = Quaternion.Euler(180f, 0f, 0f) * giant.localRotation;
            Hazify(giant.gameObject, haze, 0.72f);
        }
    }

    void Island(Transform t, LevelTheme theme, GameManager gm, System.Random rng, System.Func<float, float, float> R)
    {
        Vector3 size = new Vector3(R(5f, 8f), R(0.9f, 1.4f), R(2.6f, 3.6f));
        var slab = new GameObject("Page");
        slab.transform.SetParent(t, false);
        slab.AddComponent<MeshFilter>().sharedMesh = PaperMeshes.PaperSlab(size);
        slab.AddComponent<MeshRenderer>().sharedMaterials = new[] { gm.paperMaterial, gm.pageEdgeMaterial };
        LevelBuilder.OutlineBox(slab.transform, size, gm, rng);
        if (theme.rings)
        {
            if (ringMesh == null) ringMesh = PaperMeshes.Ring(0.26f, 0.03f);
            for (float x = -size.x * 0.5f + 0.5f; x <= size.x * 0.5f - 0.4f; x += 0.55f)
            {
                var ring = new GameObject("Ring");
                ring.transform.SetParent(slab.transform, false);
                ring.transform.localPosition = new Vector3(x, size.y * 0.5f - 0.05f, size.z * 0.5f + 0.02f);
                ring.transform.localRotation = Quaternion.Euler(0f, 0f, 8f);
                ring.AddComponent<MeshFilter>().sharedMesh = ringMesh;
                ring.AddComponent<MeshRenderer>().sharedMaterial = gm.metalMaterial;
            }
        }
        // أغراض فوق الجزيرة
        float top = size.y * 0.5f;
        Color RandomColor() => theme.floatColors[rng.Next(theme.floatColors.Length)];
        int n = 1 + rng.Next(3);
        for (int i = 0; i < n; i++)
        {
            Vector3 p = new Vector3(R(-size.x * 0.4f, size.x * 0.3f), top, R(-0.6f, 0.8f));
            switch (theme.props)
            {
                case PropSet.Snow: Props.PineTree(slab.transform, p, R(0.8f, 1.5f), gm); break;
                case PropSet.Space: if (i == 0) Props.Rocket(slab.transform, p, R(0.8f, 1.2f), gm); else Props.MiniPlanet(slab.transform, p, R(0.4f, 0.6f), RandomColor(), gm); break;
                case PropSet.Ink: Props.InkBottle(slab.transform, p, R(0f, 360f), gm); break;
                case PropSet.Origami: Props.OrigamiCrane(slab.transform, p, R(0f, 360f), RandomColor(), gm, R(1f, 1.6f)); break;
                case PropSet.Crayons: Props.Crayon(slab.transform, p, R(0f, 360f), RandomColor(), gm); break;
                case PropSet.Paint: Props.PaintTube(slab.transform, p, R(0f, 360f), RandomColor(), gm); break;
                case PropSet.Erasers: Props.Eraser(slab.transform, p, R(-40f, 40f), gm, rng); break;
                default:
                    if (i == 0 && rng.Next(2) == 0) Props.GiantPencil(slab.transform, p + new Vector3(-1f, 0f, 0f), R(-10f, 10f), gm);
                    else Props.PaperBall(slab.transform, p, R(0.4f, 0.6f), gm, rng.Next(1000));
                    break;
            }
        }
        // ورق صغير معلق تحت الجزيرة (كأنها صفحات مقطوعة)
        for (int i = 0; i < 3; i++)
        {
            var strip = LevelBuilder.Visual("Torn Page", PrimitiveType.Cube, slab.transform, new Vector3(R(-size.x * 0.35f, size.x * 0.35f), -size.y * 0.5f - R(0.3f, 0.8f), R(-0.5f, 0.5f)), new Vector3(R(0.5f, 1f), R(0.6f, 1.3f), 0.03f), gm.paperMaterial, false);
            strip.transform.localRotation = Quaternion.Euler(0f, R(-30f, 30f), R(-12f, 12f));
        }
    }

    void Giant(Transform t, LevelTheme theme, GameManager gm, System.Random rng, System.Func<float, float, float> R)
    {
        int kind = rng.Next(5);
        if (kind == 3) kind = rng.Next(3);   // الكتب تطلع بلوكات كبيرة: نبدلها
        Color RandomColor() => theme.floatColors[rng.Next(theme.floatColors.Length)];
        Color[] crayons = { new Color(0.95f, 0.35f, 0.4f), new Color(0.35f, 0.6f, 1f), new Color(1f, 0.8f, 0.25f), new Color(0.45f, 0.8f, 0.45f), new Color(0.7f, 0.45f, 0.95f) };
        switch (kind)
        {
            case 0:
            case 1:
            {
                // قلم رصاص واقف
                var p = Props.GiantPencil(t, Vector3.zero, 0f, gm);
                p.transform.localPosition = Vector3.zero;
                p.transform.localRotation = Quaternion.Euler(0f, R(0f, 360f), R(-4f, 4f));
                break;
            }
            case 2:
            {
                // قلم شمع واقف
                var c = new GameObject("Crayon Tower").transform;
                c.SetParent(t, false);
                c.localRotation = Quaternion.Euler(0f, 0f, R(-3f, 3f));
                Color col = crayons[rng.Next(crayons.Length)];
                Material wax = gm.Mat("Crayon " + ColorUtility.ToHtmlStringRGB(col), col, 0.45f);
                Material wrap = gm.Mat("Crayon Wrap " + ColorUtility.ToHtmlStringRGB(col), Color.Lerp(col, Color.white, 0.35f), 0.2f);
                LevelBuilder.Visual("Wax", PrimitiveType.Cylinder, c, new Vector3(0f, 1.6f, 0f), new Vector3(0.45f, 1.6f, 0.45f), wax, false);
                LevelBuilder.Visual("Wrap", PrimitiveType.Cylinder, c, new Vector3(0f, 1.3f, 0f), new Vector3(0.46f, 1.05f, 0.46f), wrap, false);
                for (int k = 0; k < 2; k++) LevelBuilder.Visual("Band", PrimitiveType.Cylinder, c, new Vector3(0f, 0.55f + k * 1.5f, 0f), new Vector3(0.47f, 0.03f, 0.47f), wax, false);
                var tip = CharacterMeshes.Lathe(new[] { new Vector2(0.225f, 0f), new Vector2(0.2f, 0.1f), new Vector2(0.07f, 0.42f), new Vector2(0f, 0.46f) }, 16);
                var tipGo = new GameObject("Tip");
                tipGo.transform.SetParent(c, false);
                tipGo.transform.localPosition = new Vector3(0f, 3.2f, 0f);
                tipGo.AddComponent<MeshFilter>().sharedMesh = tip;
                tipGo.AddComponent<MeshRenderer>().sharedMaterial = wax;
                break;
            }
            case 3:
            {
                // كتب فوق بعض
                var s = new GameObject("Book Stack").transform;
                s.SetParent(t, false);
                float y = 0f;
                int books = 7 + rng.Next(4);
                for (int k = 0; k < books; k++)
                {
                    float h = R(0.3f, 0.5f), w = R(1.6f, 2.1f), d = R(1.2f, 1.5f);
                    Color col = crayons[rng.Next(crayons.Length)] * 0.85f;
                    var book = new GameObject("Book").transform;
                    book.SetParent(s, false);
                    book.localPosition = new Vector3(R(-0.15f, 0.15f), y + h * 0.5f, 0f);
                    book.localRotation = Quaternion.Euler(0f, R(-15f, 15f), 0f);
                    LevelBuilder.Visual("Cover", PrimitiveType.Cube, book, Vector3.zero, new Vector3(w, h, d), gm.Mat("Book " + ColorUtility.ToHtmlStringRGB(col), col, 0.35f), false);
                    LevelBuilder.Visual("Pages", PrimitiveType.Cube, book, new Vector3(0.05f, 0f, 0f), new Vector3(w - 0.05f, h * 0.8f, d * 0.94f), gm.paperMaterial, false);
                    y += h;
                }
                break;
            }
            default:
            {
                // مسطرة واقفة
                var r = new GameObject("Ruler Tower").transform;
                r.SetParent(t, false);
                r.localRotation = Quaternion.Euler(0f, R(-30f, 30f), R(-5f, 5f));
                Material body = gm.Mat("Giant Ruler", new Color(1f, 0.85f, 0.35f), 0.4f);
                Material ink = gm.Mat("Ruler Ink", new Color(0.25f, 0.2f, 0.15f), 0.3f);
                LevelBuilder.Visual("Ruler", PrimitiveType.Cube, r, new Vector3(0f, 2f, 0f), new Vector3(0.6f, 4f, 0.08f), body, false);
                for (int k = 0; k < 20; k++)
                    LevelBuilder.Visual("Tick", PrimitiveType.Cube, r, new Vector3(-0.3f + (k % 5 == 0 ? 0.12f : 0.07f), 0.15f + k * 0.19f, -0.045f), new Vector3(k % 5 == 0 ? 0.24f : 0.14f, 0.025f, 0.01f), ink, false);
                break;
            }
        }
    }

    // أغراض عملاقة حسب الفصل: نوتات وبيانو، ساعات وتروس، صبار
    void ThemedGiant(Transform t, int style, GameManager gm, System.Random rng, System.Func<float, float, float> R)
    {
        Color[] cols = { new Color(0.95f, 0.35f, 0.45f), new Color(0.35f, 0.55f, 1f), new Color(1f, 0.75f, 0.25f), new Color(0.45f, 0.8f, 0.5f), new Color(0.7f, 0.45f, 0.95f) };
        Color col = cols[rng.Next(cols.Length)];
        Material m = gm.Mat("Giant " + ColorUtility.ToHtmlStringRGB(col), col, 0.4f);
        t.localPosition += Vector3.up * 2.4f * t.localScale.y;
        if (style == 1)
        {
            if (rng.Next(3) > 0)
            {
                // نوتة موسيقية عملاقة
                Shapes.Add(t, "Note Head", Shapes.Sphere(), new Vector3(0f, 0.3f, 0f), new Vector3(0.42f, 0.3f, 0.3f), m, new Vector3(0f, 0f, 25f), false);
                LevelBuilder.Visual("Stem", PrimitiveType.Cube, t, new Vector3(0.36f, 1.5f, 0f), new Vector3(0.08f, 2.4f, 0.08f), m, false);
                Shapes.Add(t, "Flag", Shapes.Tube(new List<Vector3> { new Vector3(0.36f, 2.7f, 0f), new Vector3(0.7f, 2.3f, 0f), new Vector3(0.75f, 1.8f, 0f) }, 0.08f, 0.04f, 8), Vector3.zero, Vector3.one, m, default, false);
            }
            else
            {
                // مفاتيح بيانو واقفة
                Material white = gm.Mat("Giant Key White", new Color(0.97f, 0.96f, 0.93f), 0.5f), black = gm.Mat("Giant Key Black", new Color(0.12f, 0.11f, 0.15f), 0.5f);
                for (int k = 0; k < 5; k++)
                {
                    LevelBuilder.Visual("Key", PrimitiveType.Cube, t, new Vector3(k * 0.42f - 0.84f, R(1.3f, 1.8f), 0f), new Vector3(0.4f, 3.4f, 0.3f), white, false);
                    if (k < 4 && k != 2) LevelBuilder.Visual("Black Key", PrimitiveType.Cube, t, new Vector3(k * 0.42f - 0.63f, 2.3f, -0.12f), new Vector3(0.24f, 2.2f, 0.3f), black, false);
                }
            }
        }
        else if (style == 2)
        {
            if (rng.Next(2) == 0)
            {
                // ساعة حائط عملاقة على عمود
                Material face = gm.Mat("Giant Clock Face", new Color(1f, 0.97f, 0.9f), 0.4f), ink = gm.Mat("Ruler Ink", new Color(0.25f, 0.2f, 0.15f), 0.3f);
                LevelBuilder.Visual("Pole", PrimitiveType.Cylinder, t, new Vector3(0f, 0.8f, 0f), new Vector3(0.2f, 1.6f, 0.2f), m, false);
                var rim = LevelBuilder.Visual("Rim", PrimitiveType.Cylinder, t, new Vector3(0f, 2.6f, 0f), new Vector3(1.6f, 0.12f, 1.6f), m, false).transform;
                rim.localRotation = Quaternion.Euler(90f, 0f, 0f);
                var fc = LevelBuilder.Visual("Face", PrimitiveType.Cylinder, t, new Vector3(0f, 2.6f, -0.08f), new Vector3(1.4f, 0.06f, 1.4f), face, false).transform;
                fc.localRotation = Quaternion.Euler(90f, 0f, 0f);
                LevelBuilder.Visual("Hand", PrimitiveType.Cube, t, new Vector3(0f, 2.85f, -0.16f), new Vector3(0.06f, 0.5f, 0.03f), ink, false);
                LevelBuilder.Visual("Hand", PrimitiveType.Cube, t, new Vector3(0.15f, 2.62f, -0.16f), new Vector3(0.36f, 0.06f, 0.03f), ink, false);
            }
            else
            {
                // ترس عملاق
                var g = new GameObject("Gear").transform;
                g.SetParent(t, false);
                g.localPosition = new Vector3(0f, 2.2f, 0f);
                var disc = LevelBuilder.Visual("Disc", PrimitiveType.Cylinder, g, Vector3.zero, new Vector3(2.2f, 0.15f, 2.2f), m, false).transform;
                disc.localRotation = Quaternion.Euler(90f, 0f, 0f);
                for (int k = 0; k < 12; k++)
                {
                    float a = k * 30f;
                    var tooth = LevelBuilder.Visual("Tooth", PrimitiveType.Cube, g, Quaternion.Euler(0f, 0f, a) * new Vector3(0f, 1.2f, 0f), new Vector3(0.35f, 0.4f, 0.28f), m, false).transform;
                    tooth.localRotation = Quaternion.Euler(0f, 0f, a);
                }
                spin.Add(g);
            }
        }
        else
        {
            // صبار صحرا
            Material cactus = gm.Mat("Giant Cactus", new Color(0.35f, 0.7f, 0.4f), 0.35f);
            Shapes.Add(t, "Trunk", Shapes.Tube(new List<Vector3> { Vector3.zero, new Vector3(0f, 3.2f, 0f) }, 0.4f, 0.36f, 12), Vector3.zero, Vector3.one, cactus, default, false);
            for (int sd = -1; sd <= 1; sd += 2)
                Shapes.Add(t, "Arm", Shapes.Tube(new List<Vector3> { new Vector3(0f, 1.4f + sd * 0.3f, 0f), new Vector3(sd * 0.8f, 1.5f + sd * 0.3f, 0f), new Vector3(sd * 0.85f, 2.4f + sd * 0.3f, 0f) }, 0.24f, 0.22f, 10), Vector3.zero, Vector3.one, cactus, default, false);
        }
    }

    readonly List<Transform> spin = new List<Transform>();

    // نخلي الشي ياخذ لون الجو (كأنه بعيد بالضباب): نسخة من مواده بلون مخلوط
    static readonly Dictionary<string, Material> hazeCache = new Dictionary<string, Material>();
    static void Hazify(GameObject go, Color haze, float k)
    {
        foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                Material m = mats[i];
                if (m == null) continue;
                string key = m.GetInstanceID() + "|" + ColorUtility.ToHtmlStringRGB(haze) + "|" + k;
                if (!hazeCache.TryGetValue(key, out Material h) || h == null)
                {
                    h = new Material(m) { name = m.name + " (far)" };
                    Color c = h.HasProperty("_BaseColor") ? h.GetColor("_BaseColor") : Color.white;
                    Color mixed = Color.Lerp(c, haze, k);
                    mixed.a = c.a;
                    if (h.HasProperty("_BaseColor")) h.SetColor("_BaseColor", mixed);
                    if (h.HasProperty("_Smoothness")) h.SetFloat("_Smoothness", Mathf.Min(h.GetFloat("_Smoothness"), 0.3f));
                    if (h.HasProperty("_EmissionColor")) { h.SetColor("_EmissionColor", Color.black); h.DisableKeyword("_EMISSION"); }
                    hazeCache[key] = h;
                }
                mats[i] = h;
            }
            r.sharedMaterials = mats;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }
    }

    void Update()
    {
        float t = Time.time;
        foreach (Transform g in spin) if (g != null) g.localRotation = Quaternion.Euler(0f, 0f, t * 8f);
        for (int i = 0; i < bob.Count; i++)
            if (bob[i] != null) bob[i].position = home[i] + new Vector3(0f, Mathf.Sin(t * 0.45f + phase[i]) * 0.35f, 0f);
    }
}
