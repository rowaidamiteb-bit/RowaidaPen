using System.Collections.Generic;
using UnityEngine;

// الجزء الثالث — فصل الغابة: بذور تطلع شجر، فطر نايم يصحى وينطط، نهر بجذوع تنمسك بحبل، ونحل يحرس الطريق
public static class ForestFeatures
{
    public static void Build(Transform root, LevelData data, GameManager gm, System.Random rng)
    {
        foreach (Vector3 s in data.seedPits) SeedPit.Create(root, s, gm);
        foreach (Vector3 m in data.mushrooms) Mushroom.Create(root, m, gm);
        foreach (Vector3 r in data.rivers) RiverLogs.Create(root, r, gm);
        foreach (Vector3 h in data.hives) BeeHive.Create(root, h, gm);
        foreach (Vector3 h in data.honeys) GluePuddle.Create(root, h, gm, true);
        if (data.forest) ForestScenery.Create(root, data, gm, rng);
        if (data.sticker.HasValue) Sticker.Create(root, data.sticker.Value, data, gm);
    }
}

// ---------- البذرة ----------

// حفرة تحتها تراب: ارمي بذرة (دايرة) تطيح بالتراب، تطلع شجرة وغصنها العريض يصير جسر
public class SeedPit : MonoBehaviour
{
    const float Depth = 3.4f, GrowTime = 2.4f;
    float from, to, top, growT = -1f;
    bool holding, prompted, birdLanded;
    Transform trunk, platform, canopy, sprout, bird;
    readonly List<Transform> leaves = new List<Transform>();
    BoxCollider bridge;

    public bool Grown => growT >= 1f;

    public static void Create(Transform root, Vector3 spec, GameManager gm)
    {
        var go = new GameObject("Seed Pit");
        go.transform.SetParent(root, false);
        var s = go.AddComponent<SeedPit>();
        s.from = spec.x; s.to = spec.y; s.top = spec.z;
        float cx = (s.from + s.to) * 0.5f, w = s.to - s.from, soilY = s.top - Depth;
        Material soil = gm.Mat("Forest Soil", new Color(0.45f, 0.3f, 0.2f), 0.15f);
        Material soilDark = gm.Mat("Forest Soil Dark", new Color(0.32f, 0.2f, 0.13f), 0.1f);
        Material bark = gm.Mat("Tree Bark", new Color(0.5f, 0.33f, 0.2f), 0.2f);
        Material wood = gm.Mat("Tree Wood", new Color(0.62f, 0.45f, 0.28f), 0.25f);
        Material leaf = gm.Mat("Leaf Green", new Color(0.35f, 0.72f, 0.3f), 0.35f);
        Material leaf2 = gm.Mat("Leaf Light", new Color(0.55f, 0.85f, 0.35f), 0.35f);
        Material stone = gm.Mat("Pebble", new Color(0.7f, 0.68f, 0.64f), 0.3f);

        // كومة تراب تحت مع حصى وأوراق يابسة
        LevelBuilder.Visual("Soil", PrimitiveType.Sphere, go.transform, new Vector3(cx, soilY, 0f), new Vector3(w * 0.9f, 1.1f, 2.6f), soil);
        LevelBuilder.Visual("Soil Top", PrimitiveType.Sphere, go.transform, new Vector3(cx, soilY + 0.25f, -0.2f), new Vector3(w * 0.5f, 0.5f, 1.4f), soilDark);
        for (int i = 0; i < 5; i++)
            LevelBuilder.Visual("Pebble", PrimitiveType.Sphere, go.transform, new Vector3(s.from + w * (0.15f + i * 0.18f), soilY + 0.45f, -0.9f + (i % 2) * 0.4f), new Vector3(0.3f, 0.18f, 0.25f), stone, false);
        // نبتة صغيرة نايمة (تنتظر البذرة)
        s.sprout = new GameObject("Sprout").transform;
        s.sprout.SetParent(go.transform, false);
        s.sprout.position = new Vector3(cx, soilY + 0.5f, -0.3f);
        LevelBuilder.Visual("Stem", PrimitiveType.Cylinder, s.sprout, new Vector3(0f, 0.2f, 0f), new Vector3(0.06f, 0.2f, 0.06f), leaf, false);
        LevelBuilder.Visual("Leaf", PrimitiveType.Sphere, s.sprout, new Vector3(0.12f, 0.42f, 0f), new Vector3(0.25f, 0.08f, 0.14f), leaf2, false);
        LevelBuilder.Visual("Leaf", PrimitiveType.Sphere, s.sprout, new Vector3(-0.12f, 0.38f, 0f), new Vector3(0.22f, 0.08f, 0.13f), leaf2, false);

        // الشجرة (تكبر بعدين)
        s.trunk = LevelBuilder.Visual("Trunk", PrimitiveType.Cylinder, go.transform, new Vector3(cx, soilY, 0.3f), new Vector3(0.7f, 0.01f, 0.7f), bark).transform;
        s.platform = new GameObject("Branch Bridge").transform;
        s.platform.SetParent(go.transform, false);
        s.platform.position = new Vector3(cx, s.top - 0.17f, 0f);
        LevelBuilder.Visual("Branch", PrimitiveType.Cube, s.platform, Vector3.zero, new Vector3(w + 0.5f, 0.34f, 1.6f), wood);
        for (int i = 0; i < 6; i++)
            LevelBuilder.Visual("Bark Line", PrimitiveType.Cube, s.platform, new Vector3(-w * 0.4f + i * w * 0.16f, 0.175f, -0.3f + (i % 3) * 0.3f), new Vector3(0.6f, 0.02f, 0.05f), bark, false);
        // أوراق على أطراف الغصن
        for (int i = 0; i < 8; i++)
        {
            float lx = -w * 0.5f + (i / 7f) * w;
            var l = LevelBuilder.Visual("Leaf", PrimitiveType.Sphere, s.platform, new Vector3(lx, -0.05f, i % 2 == 0 ? -0.95f : 0.95f), new Vector3(0.6f, 0.3f, 0.45f), i % 3 == 0 ? leaf2 : leaf, false).transform;
            s.leaves.Add(l);
        }
        s.platform.localScale = new Vector3(0.01f, 1f, 1f);
        s.canopy = new GameObject("Canopy").transform;
        s.canopy.SetParent(go.transform, false);
        s.canopy.position = new Vector3(cx, s.top + 1.6f, 1.6f);
        for (int i = 0; i < 6; i++)
            LevelBuilder.Visual("Canopy Puff", PrimitiveType.Sphere, s.canopy, new Vector3((i - 2.5f) * w * 0.17f, Mathf.Sin(i * 1.7f) * 0.5f + 0.4f, (i % 2) * 0.4f), Vector3.one * (1.6f + (i % 3) * 0.4f), i % 2 == 0 ? leaf : leaf2);
        // فواكه حمرا صغيرة
        Material apple = gm.Mat("Apple", new Color(0.95f, 0.25f, 0.25f), 0.6f);
        for (int i = 0; i < 4; i++)
            LevelBuilder.Visual("Apple", PrimitiveType.Sphere, s.canopy, new Vector3((i - 1.5f) * w * 0.22f, -0.3f + (i % 2) * 0.5f, -0.75f), Vector3.one * 0.28f, apple, false);
        s.canopy.localScale = Vector3.one * 0.01f;
        s.canopy.gameObject.SetActive(false);

        s.bridge = s.platform.gameObject.AddComponent<BoxCollider>();
        s.bridge.size = new Vector3(w + 0.5f, 0.34f, 1.6f);
        s.bridge.enabled = false;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time, soilY = top - Depth, cx = (from + to) * 0.5f;

        // بذرة طاحت بالتراب؟
        if (growT < 0f)
        {
            sprout.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.5f) * 8f);
            foreach (InkMark m in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
            {
                if (!m.closed || m.beingEaten) continue;
                var r = m.GetComponent<MeshRenderer>();
                if (r == null) continue;
                Bounds b = r.bounds;
                if (b.center.x > from + 0.2f && b.center.x < to - 0.2f && b.min.y < soilY + 0.8f)
                {
                    Destroy(m.gameObject);
                    growT = 0f;
                    LevelSfx.Play("pop", 0.7f, 0.8f);
                    LevelSfx.Play("grow", 0.9f);
                    Fx.Sparkle(new Vector3(cx, soilY + 0.6f, 0f), new Color(0.6f, 1f, 0.4f), 18);
                    Fx.Dust(new Vector3(cx, soilY + 0.5f, 0f), 0.6f);
                    break;
                }
            }
        }
        else if (growT < 1f)
        {
            growT = Mathf.Min(1f, growT + Time.deltaTime / GrowTime);
            float k = Mathf.SmoothStep(0f, 1f, growT);
            float h = (top - 0.2f - soilY) * Mathf.Clamp01(k / 0.7f);
            trunk.localScale = new Vector3(0.7f, Mathf.Max(0.01f, h * 0.5f), 0.7f);
            trunk.position = new Vector3(cx + Mathf.Sin(t * 20f) * 0.03f * (1f - k), soilY + h * 0.5f, 0.3f);
            sprout.localScale = Vector3.one * Mathf.Max(0f, 1f - k * 3f);
            float b = Mathf.Clamp01((k - 0.55f) / 0.45f);
            platform.localScale = new Vector3(Mathf.Max(0.01f, b), 1f, 1f);
            canopy.gameObject.SetActive(b > 0f);
            canopy.localScale = Vector3.one * Mathf.Max(0.01f, Mathf.SmoothStep(0f, 1f, b) * (1f + Mathf.Sin(b * Mathf.PI) * 0.15f));
            if (Random.value < 0.3f) Fx.Sparkle(new Vector3(cx + Random.Range(-1f, 1f), soilY + h, -0.3f), new Color(0.6f, 1f, 0.45f), 2);
            if (growT >= 1f)
            {
                bridge.enabled = true;
                Fx.Sparkle(new Vector3(cx, top + 0.5f, 0f), new Color(1f, 0.95f, 0.5f), 20);
                LevelSfx.Play("chirp", 0.5f, 1.1f);
            }
        }
        else
        {
            // الأوراق تتمايل، وعصفور ينزل على الشجرة
            canopy.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 0.9f + cx) * 2f);
            for (int i = 0; i < leaves.Count; i++) leaves[i].localRotation = Quaternion.Euler(Mathf.Sin(t * 2f + i) * 10f, 0f, 0f);
            if (!birdLanded) { birdLanded = true; bird = ForestScenery.MakeBird(canopy, new Vector3(0.6f, 1.4f, -0.6f), GameManager.Instance); }
            if (bird != null) bird.localRotation = Quaternion.Euler(0f, Mathf.Sin(t * 0.7f) > 0.3f ? 180f : 0f, Mathf.Max(0f, Mathf.Sin(t * 5f)) * 15f);
        }

        bool wait = HoldHelper.Before(from) && !Grown && !RowaidaController.BridgeCovers(from, to, top);
        if (wait && !prompted) { prompted = true; gm.UI.ShowMessage("", 3.2f, "ارسمي دايرة (بذرة) وخليها تطيح بالتراب... وشوفي وش يطلع!"); }
        HoldHelper.Hold(ref holding, wait);
    }
}

// ---------- الفطر ----------

// فطرة نايمة قبل جدار أو حفرة: المسيها بخط تصحى (تكبر وتتلون وتفتح عيونها)، وبعدها رويدا تنط عليها
public class Mushroom : MonoBehaviour
{
    float x, top, power, wakeT = -1f, squash;
    bool holding, prompted;
    Transform cap, body, eyes, lids;
    Renderer capRend;
    Material sleepMat, awakeMat;
    readonly List<Transform> zzz = new List<Transform>();
    readonly List<Renderer> dots = new List<Renderer>();

    bool Awake => wakeT >= 0f;

    public static void Create(Transform root, Vector3 spec, GameManager gm)
    {
        var go = new GameObject("Mushroom");
        go.transform.SetParent(root, false);
        go.transform.position = new Vector3(spec.x, spec.y, 0f);
        var m = go.AddComponent<Mushroom>();
        m.x = spec.x; m.top = spec.y; m.power = spec.z;
        Material stem = gm.Mat("Mushroom Stem", new Color(0.98f, 0.93f, 0.82f), 0.3f);
        m.sleepMat = gm.Mat("Mushroom Sleepy", new Color(0.65f, 0.55f, 0.6f), 0.3f);
        m.awakeMat = gm.Mat("Mushroom Red", new Color(0.95f, 0.25f, 0.3f), 0.55f);
        Material dot = gm.Mat("Mushroom Dot", new Color(1f, 0.97f, 0.9f), 0.4f);
        Material white = gm.Mat("Monster Eye", Color.white, 0.7f);
        Material black = gm.Mat("Monster Pupil", new Color(0.05f, 0.04f, 0.06f), 0.9f);
        m.body = new GameObject("Body").transform;
        m.body.SetParent(go.transform, false);
        LevelBuilder.Visual("Stem", PrimitiveType.Capsule, m.body, new Vector3(0f, 0.45f, 0f), new Vector3(0.55f, 0.45f, 0.55f), stem);
        m.cap = new GameObject("Cap").transform;
        m.cap.SetParent(m.body, false);
        m.cap.localPosition = new Vector3(0f, 0.9f, 0f);
        m.capRend = LevelBuilder.Visual("Cap", PrimitiveType.Sphere, m.cap, Vector3.zero, new Vector3(1.5f, 0.75f, 1.4f), m.sleepMat).GetComponent<Renderer>();
        for (int i = 0; i < 6; i++)
        {
            float a = i / 6f * Mathf.PI * 2f;
            var d = LevelBuilder.Visual("Dot", PrimitiveType.Sphere, m.cap, new Vector3(Mathf.Cos(a) * 0.45f, 0.25f + (i % 2) * 0.05f, Mathf.Sin(a) * 0.42f), new Vector3(0.22f, 0.1f, 0.22f), dot, false);
            m.dots.Add(d.GetComponent<Renderer>());
            d.SetActive(false);
        }
        // وجه على الساق: عيون مسكرة وهي نايمة
        m.eyes = new GameObject("Eyes").transform;
        m.eyes.SetParent(m.body, false);
        m.eyes.localPosition = new Vector3(0f, 0.5f, -0.27f);
        for (int s = -1; s <= 1; s += 2)
        {
            LevelBuilder.Visual("Eye", PrimitiveType.Sphere, m.eyes, new Vector3(s * 0.12f, 0f, 0f), Vector3.one * 0.14f, white, false);
            LevelBuilder.Visual("Pupil", PrimitiveType.Sphere, m.eyes, new Vector3(s * 0.12f, 0f, -0.05f), Vector3.one * 0.07f, black, false);
        }
        m.lids = new GameObject("Lids").transform;
        m.lids.SetParent(m.body, false);
        m.lids.localPosition = new Vector3(0f, 0.5f, -0.28f);
        for (int s = -1; s <= 1; s += 2)
            LevelBuilder.Visual("Lid", PrimitiveType.Cube, m.lids, new Vector3(s * 0.12f, 0f, 0f), new Vector3(0.16f, 0.03f, 0.02f), black, false);
        m.eyes.gameObject.SetActive(false);
        // Zzz
        Material z = gm.Mat("Zzz", new Color(0.55f, 0.65f, 1f), 0.4f);
        for (int i = 0; i < 3; i++)
        {
            var zt = LevelBuilder.Visual("Z", PrimitiveType.Cube, go.transform, Vector3.zero, new Vector3(0.18f, 0.04f, 0.02f), z, false).transform;
            m.zzz.Add(zt);
        }
        m.body.localScale = new Vector3(1f, 0.7f, 1f);
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time;
        RowaidaController r = gm.Rowaida;

        if (!Awake)
        {
            // يتنفس وهو نايم، وZzz تطلع
            body.localScale = new Vector3(1f + Mathf.Sin(t * 1.6f) * 0.03f, 0.7f - Mathf.Sin(t * 1.6f) * 0.03f, 1f);
            for (int i = 0; i < zzz.Count; i++)
            {
                float k = Mathf.Repeat(t * 0.4f + i / 3f, 1f);
                zzz[i].position = new Vector3(x + 0.5f + k * 0.6f, top + 1.2f + k * 1.2f, -0.2f);
                zzz[i].localScale = new Vector3(0.18f, 0.04f, 0.02f) * (0.5f + k);
                zzz[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 3f + i) * 20f);
                zzz[i].gameObject.SetActive(k < 0.9f);
            }
            // خط لمسها؟ تصحى (والخط يختفي)
            Bounds me = new Bounds(new Vector3(x, top + 0.7f, 0f), new Vector3(1.8f, 1.8f, 4f));
            foreach (InkMark mark in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
            {
                var mr = mark.GetComponent<MeshRenderer>();
                if (mr == null || !mr.bounds.Intersects(me)) continue;
                Destroy(mark.gameObject);
                wakeT = 0f;
                capRend.sharedMaterial = awakeMat;
                foreach (Renderer d in dots) d.gameObject.SetActive(true);
                eyes.gameObject.SetActive(true);
                lids.gameObject.SetActive(false);
                foreach (Transform z in zzz) z.gameObject.SetActive(false);
                LevelSfx.Play("pop", 0.9f, 1.2f);
                LevelSfx.Play("boing", 0.6f, 1.4f);
                Fx.Sparkle(new Vector3(x, top + 1.2f, -0.3f), new Color(1f, 0.5f, 0.55f), 18);
                break;
            }
        }
        else
        {
            wakeT += Time.deltaTime;
            // يكبر (يصحى بقوة) وبعدين يتمايل
            float grow = Mathf.Min(1f, wakeT / 0.4f);
            float wob = Mathf.Sin(wakeT * 14f) * Mathf.Exp(-wakeT * 3f) * 0.2f;
            squash = Mathf.Max(0f, squash - Time.deltaTime * 4f);
            float sq = Mathf.Sin(squash * Mathf.PI) * 0.35f;
            body.localScale = new Vector3(1f + wob + sq, Mathf.Lerp(0.7f, 1.15f, grow) - wob - sq, 1f + wob + sq);
            cap.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 2f + x) * 4f);
            eyes.localPosition = new Vector3(0f, 0.5f, -0.27f) + (r != null ? new Vector3(Mathf.Clamp(r.transform.position.x - x, -1f, 1f) * 0.05f, 0f, 0f) : Vector3.zero);

            // رويدا عليه؟ تنط
            if (r != null && gm.IsPlaying)
            {
                Vector3 p = r.transform.position;
                if (Mathf.Abs(p.x - x) < 0.45f && p.y - top < 1.3f && p.y - top > 0.2f && r.Bounce(power))
                {
                    squash = 1f;
                    LevelSfx.Play("boing", 0.9f, Random.Range(0.9f, 1.1f));
                    Fx.Sparkle(new Vector3(x, top + 1.3f, -0.2f), new Color(1f, 0.85f, 0.4f), 10);
                }
            }
        }

        bool wait = !Awake && r != null && gm.IsPlaying && r.transform.position.x > x - 1.9f && r.transform.position.x < x - 0.7f;
        if (wait && !prompted) { prompted = true; gm.UI.ShowMessage("", 3f, "الفطرة نايمة! المسيها بخط عشان تصحى وتنطط رويدا"); }
        HoldHelper.Hold(ref holding, wait);
    }
}

// ---------- النهر ----------

// نهر يجري بالحفرة (من ورا لقدام) ويجيب جذوع خشب. ارسمي خط على النهر: يصير حبل يصيد أول جذع ويوقفه جسر
public class RiverLogs : MonoBehaviour
{
    const float FarZ = 9f, NearZ = -8f, Speed = 2.4f;
    float from, to, top, period, nextLog, waterY;
    bool holding, prompted, ropeReady;
    readonly List<Transform> logs = new List<Transform>();
    Transform hooked, rope, stake;
    Material waterMat, barkMat, ringMat, mossMat;
    readonly List<Transform> pads = new List<Transform>();
    Material ropeMat;
    bool stopped;

    public static void Create(Transform root, Vector3 spec, GameManager gm)
    {
        var go = new GameObject("River");
        go.transform.SetParent(root, false);
        var rv = go.AddComponent<RiverLogs>();
        rv.from = spec.x; rv.to = spec.y; rv.period = spec.z;
        rv.top = 0f;
        rv.waterY = rv.top - 1.1f;
        float cx = (rv.from + rv.to) * 0.5f, w = rv.to - rv.from;

        // المويه: سطح بخطوط تتحرك (يجري لقدام)
        var tex = new Texture2D(8, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat };
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 8; x++)
            {
                float k = Mathf.Sin(y / 64f * Mathf.PI * 6f + x * 0.4f);
                tex.SetPixel(x, y, Color.Lerp(new Color(0.3f, 0.62f, 0.85f), new Color(0.75f, 0.92f, 1f), Mathf.Clamp01(k * 2.5f - 1.6f)));
            }
        tex.Apply();
        rv.waterMat = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "River Water" };
        rv.waterMat.SetTexture("_BaseMap", tex);
        rv.waterMat.SetColor("_BaseColor", Color.white);
        rv.waterMat.SetTextureScale("_BaseMap", new Vector2(1f, 4f));
        var water = LevelBuilder.Visual("Water", PrimitiveType.Quad, go.transform, new Vector3(cx, rv.waterY, (FarZ + NearZ) * 0.5f), new Vector3(w + 0.2f, FarZ - NearZ, 1f), rv.waterMat, false).transform;
        water.localRotation = Quaternion.Euler(90f, 0f, 0f);
        // ضفاف: حصى وقصب
        Material rock = gm.Mat("River Rock", new Color(0.6f, 0.6f, 0.58f), 0.35f);
        Material reed = gm.Mat("Reed", new Color(0.4f, 0.65f, 0.3f), 0.3f);
        Material cat = gm.Mat("Cattail", new Color(0.45f, 0.3f, 0.18f), 0.3f);
        for (int side = 0; side < 2; side++)
        {
            float ex = side == 0 ? rv.from + 0.15f : rv.to - 0.15f;
            for (int i = 0; i < 6; i++)
                LevelBuilder.Visual("Bank Rock", PrimitiveType.Sphere, go.transform, new Vector3(ex, rv.waterY + 0.05f, -3f + i * 1.6f), new Vector3(0.6f, 0.35f, 0.7f), rock, false);
            for (int i = 0; i < 3; i++)
            {
                var rd = LevelBuilder.Visual("Reed", PrimitiveType.Cylinder, go.transform, new Vector3(ex + (side == 0 ? 0.3f : -0.3f), rv.waterY + 0.6f, 2f + i * 0.5f), new Vector3(0.05f, 0.65f, 0.05f), reed, false).transform;
                LevelBuilder.Visual("Cattail", PrimitiveType.Capsule, rd, new Vector3(0f, 0.85f, 0f), new Vector3(1.6f, 0.2f, 1.6f), cat, false);
            }
        }
        // أوراق زنبق تطفو
        Material lily = gm.Mat("Lily Pad", new Color(0.35f, 0.7f, 0.35f), 0.4f);
        Material lilyFlower = gm.Mat("Lily Flower", new Color(1f, 0.7f, 0.85f), 0.4f);
        for (int i = 0; i < 3; i++)
        {
            var pad = LevelBuilder.Visual("Lily Pad", PrimitiveType.Cylinder, go.transform, new Vector3(rv.from + w * (0.25f + i * 0.25f), rv.waterY + 0.02f, FarZ - i * 5f), new Vector3(0.9f, 0.02f, 0.9f), lily, false).transform;
            if (i == 1) LevelBuilder.Visual("Flower", PrimitiveType.Sphere, pad, new Vector3(0.1f, 2f, 0f), new Vector3(0.35f, 6f, 0.35f), lilyFlower, false);
            rv.pads.Add(pad);
        }
        // وتد الحبل على الضفة اليسار
        Material post = gm.Mat("Tree Wood", new Color(0.62f, 0.45f, 0.28f), 0.25f);
        rv.stake = LevelBuilder.Visual("Rope Post", PrimitiveType.Cylinder, go.transform, new Vector3(rv.from - 0.5f, rv.top + 0.45f, 1f), new Vector3(0.22f, 0.45f, 0.22f), post).transform;
        rv.ropeMat = gm.Mat("Rope", new Color(0.85f, 0.72f, 0.45f), 0.2f);
        rv.barkMat = gm.Mat("Tree Bark", new Color(0.5f, 0.33f, 0.2f), 0.2f);
        rv.ringMat = gm.Mat("Log Rings", new Color(0.9f, 0.75f, 0.5f), 0.25f);
        rv.mossMat = gm.Mat("Moss", new Color(0.4f, 0.65f, 0.3f), 0.2f);
        rv.nextLog = Time.time + 0.5f;
    }

    Transform SpawnLog()
    {
        float w = to - from, cx = (from + to) * 0.5f;
        var log = new GameObject("Log");
        log.transform.SetParent(transform, false);
        log.transform.position = new Vector3(cx, top - 0.42f, FarZ);
        var body = LevelBuilder.Visual("Bark", PrimitiveType.Cylinder, log.transform, Vector3.zero, new Vector3(0.84f, (w + 0.7f) * 0.5f, 0.84f), barkMat).transform;
        body.localRotation = Quaternion.Euler(0f, 0f, 90f);
        for (int s = -1; s <= 1; s += 2)
        {
            var ring = LevelBuilder.Visual("End", PrimitiveType.Cylinder, log.transform, new Vector3(s * (w + 0.7f) * 0.5f, 0f, 0f), new Vector3(0.78f, 0.02f, 0.78f), ringMat, false).transform;
            ring.localRotation = Quaternion.Euler(0f, 0f, 90f);
        }
        LevelBuilder.Visual("Moss", PrimitiveType.Sphere, log.transform, new Vector3(w * 0.2f, 0.36f, 0.1f), new Vector3(0.9f, 0.2f, 0.5f), mossMat, false);
        var stub = LevelBuilder.Visual("Stub", PrimitiveType.Cylinder, log.transform, new Vector3(-w * 0.25f, 0.4f, 0f), new Vector3(0.14f, 0.25f, 0.14f), barkMat, false).transform;
        stub.localRotation = Quaternion.Euler(0f, 0f, -30f);
        var rb = log.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        var col = log.AddComponent<BoxCollider>();
        col.size = new Vector3(w + 0.7f, 0.84f, 0.84f);
        logs.Add(log.transform);
        return log.transform;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time, dt = Time.deltaTime;
        waterMat.SetTextureOffset("_BaseMap", new Vector2(0f, t * 0.6f));

        // جذوع جديدة (لين ينمسك واحد)
        if (!stopped && t >= nextLog)
        {
            nextLog = t + period;
            SpawnLog();
            if (Mathf.Abs((from + to) * 0.5f - (gm.Rowaida != null ? gm.Rowaida.transform.position.x : 0f)) < 14f) LevelSfx.Play("splash", 0.35f, 0.8f);
        }

        // خط على النهر؟ يصير حبل
        if (hooked == null)
            foreach (InkMark m in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
            {
                if (m.closed || m.beingEaten) continue;
                var mr = m.GetComponent<MeshRenderer>();
                if (mr == null) continue;
                Bounds b = mr.bounds;
                if (b.center.x > from - 0.3f && b.center.x < to + 0.3f && b.center.y < top + 2.5f && b.center.y > waterY - 1f)
                {
                    Destroy(m.gameObject);
                    ropeReady = true;
                    LevelSfx.Play("rope", 0.9f);
                    if (!prompted) { prompted = true; }
                    break;
                }
            }
        // الحبل يصيد أقرب جذع جاي
        if (ropeReady && hooked == null)
        {
            Transform best = null;
            foreach (Transform l in logs) if (l != null && l.position.z > -0.4f && (best == null || l.position.z < best.position.z)) best = l;
            if (best != null)
            {
                hooked = best;
                ropeReady = false;
                rope = LevelBuilder.Visual("Rope", PrimitiveType.Cylinder, transform, Vector3.zero, Vector3.one, ropeMat, false).transform;
                LevelSfx.Play("rope", 0.7f, 1.2f);
            }
        }

        // الجذوع تمشي مع النهر وتتمايل
        for (int i = logs.Count - 1; i >= 0; i--)
        {
            Transform l = logs[i];
            if (l == null) { logs.RemoveAt(i); continue; }
            Vector3 p = l.position;
            if (l == hooked)
            {
                p.z = Mathf.MoveTowards(p.z, 0f, (p.z > 0f ? Speed : Speed * 1.5f) * dt);
                if (Mathf.Abs(p.z) < 0.01f && !stopped) { stopped = true; Fx.Sparkle(new Vector3(p.x, top + 0.3f, 0f), new Color(1f, 0.9f, 0.5f), 14); LevelSfx.Play("thud", 0.6f); }
            }
            else
            {
                p.z -= Speed * dt;
                // لو فيه جذع ممسوك بالنص، الباقي يغطس تحته
                if (stopped && Mathf.Abs(p.z) < 2.5f) p.y = Mathf.Lerp(p.y, waterY - 1.2f, dt * 3f);
            }
            if (!stopped || l == hooked) p.y = top - 0.42f + Mathf.Sin(t * 2f + i) * 0.04f;
            l.position = p;
            l.GetComponent<Rigidbody>().MovePosition(p);
            l.localRotation = Quaternion.Euler(Mathf.Sin(t * 1.5f + i) * 3f, 0f, 0f);
            if (p.z < NearZ) { Destroy(l.gameObject); logs.RemoveAt(i); }
        }
        // ورق الزنبق يجري ويرجع ورا
        for (int i = 0; i < pads.Count; i++)
        {
            Vector3 p = pads[i].position;
            p.z -= Speed * 0.6f * dt;
            if (p.z < NearZ) p.z = FarZ;
            pads[i].position = p;
            pads[i].localRotation = Quaternion.Euler(0f, t * 20f + i * 50f, 0f);
        }
        // الحبل: من الوتد للجذع
        if (rope != null && hooked != null)
        {
            Vector3 a = stake.position + Vector3.up * 0.35f;
            Vector3 b = hooked.position + new Vector3(-(to - from) * 0.45f, 0.3f, 0f);
            rope.position = (a + b) * 0.5f;
            rope.up = (b - a).normalized;
            rope.localScale = new Vector3(0.07f, (b - a).magnitude * 0.5f, 0.07f);
        }

        bool ready = stopped && hooked != null;
        bool wait = HoldHelper.Before(from) && !ready;
        if (wait && !prompted)
        {
            prompted = true;
            gm.UI.ShowMessage("", 3.2f, "النهر يجري! ارسمي خط على النهر يصير حبل يصيد جذع");
        }
        HoldHelper.Hold(ref holding, wait);
    }
}

// ---------- النحل ----------

// خلية معلقة على غصن، ونحل يحرس الطريق تحتها: إذا قربت رويدا يلحقها ويقرصها.
// ارسمي دايرة: تصير وردة ملونة، والنحل يروح لها ويلهى فيها شوي
public class BeeHive : MonoBehaviour
{
    class Bee { public Transform t; public Transform wl, wr; public float phase; public Vector3 vel; }

    const float LureTime = 8f;
    float x, top, range, lureUntil, nextSting;
    bool holding, prompted;
    Vector3 hivePos;
    readonly List<Bee> bees = new List<Bee>();
    Transform flower, hive;
    AudioSource buzz;

    bool Lured => flower != null && Time.time < lureUntil;

    public static void Create(Transform root, Vector3 spec, GameManager gm)
    {
        var go = new GameObject("Bee Hive");
        go.transform.SetParent(root, false);
        var h = go.AddComponent<BeeHive>();
        h.x = spec.x; h.top = spec.y; h.range = spec.z;
        h.hivePos = new Vector3(h.x, h.top + 3.6f, 0.5f);
        Material bark = gm.Mat("Tree Bark", new Color(0.5f, 0.33f, 0.2f), 0.2f);
        Material hiveMat = gm.Mat("Hive", new Color(0.98f, 0.75f, 0.3f), 0.45f);
        Material hiveDark = gm.Mat("Hive Dark", new Color(0.85f, 0.58f, 0.2f), 0.4f);
        Material hole = gm.Mat("Monster Pupil", new Color(0.05f, 0.04f, 0.06f), 0.9f);
        Material leaf = gm.Mat("Leaf Green", new Color(0.35f, 0.72f, 0.3f), 0.35f);
        // غصن من شجرة ورا
        LevelBuilder.Visual("Tree Trunk", PrimitiveType.Cylinder, go.transform, new Vector3(h.x + 2.6f, h.top + 2.5f, 2.2f), new Vector3(0.9f, 3.5f, 0.9f), bark);
        var branch = LevelBuilder.Visual("Branch", PrimitiveType.Cylinder, go.transform, new Vector3(h.x + 1.2f, h.top + 4.6f, 1.2f), new Vector3(0.28f, 1.7f, 0.28f), bark).transform;
        branch.localRotation = Quaternion.Euler(30f, 0f, 75f);
        for (int i = 0; i < 4; i++)
            LevelBuilder.Visual("Leaves", PrimitiveType.Sphere, go.transform, new Vector3(h.x + 1.5f + i * 0.7f, h.top + 5.3f + (i % 2) * 0.5f, 2f), Vector3.one * 1.6f, leaf);
        // الخلية: حلقات فوق بعض
        h.hive = new GameObject("Hive").transform;
        h.hive.SetParent(go.transform, false);
        h.hive.position = h.hivePos;
        float[] sizes = { 0.6f, 0.95f, 1.15f, 1.05f, 0.75f };
        for (int i = 0; i < sizes.Length; i++)
            LevelBuilder.Visual("Ring", PrimitiveType.Sphere, h.hive, new Vector3(0f, 0.55f - i * 0.32f, 0f), new Vector3(sizes[i], 0.42f, sizes[i]), i % 2 == 0 ? hiveMat : hiveDark);
        LevelBuilder.Visual("Hole", PrimitiveType.Sphere, h.hive, new Vector3(0f, -0.2f, -0.52f), new Vector3(0.3f, 0.26f, 0.1f), hole, false);
        LevelBuilder.Visual("Drip", PrimitiveType.Capsule, h.hive, new Vector3(0.25f, -0.85f, -0.2f), new Vector3(0.12f, 0.2f, 0.12f), gm.Mat("Honey", new Color(1f, 0.68f, 0.12f), 0.95f), false);
        // النحل
        Material yellow = gm.Mat("Bee Yellow", new Color(1f, 0.82f, 0.2f), 0.5f);
        Material stripe = gm.Mat("Bee Stripe", new Color(0.12f, 0.1f, 0.1f), 0.4f);
        Material wing = gm.Mat("Bee Wing", new Color(0.92f, 0.97f, 1f), 0.9f);
        for (int i = 0; i < 6; i++)
        {
            var b = new GameObject("Bee").transform;
            b.SetParent(go.transform, false);
            b.position = h.hivePos + Random.insideUnitSphere;
            LevelBuilder.Visual("Body", PrimitiveType.Sphere, b, Vector3.zero, new Vector3(0.36f, 0.26f, 0.26f), yellow, false);
            for (int s = 0; s < 2; s++)
                LevelBuilder.Visual("Stripe", PrimitiveType.Cylinder, b, new Vector3(-0.04f + s * 0.1f, 0f, 0f), new Vector3(0.27f, 0.02f, 0.27f), stripe, false).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            LevelBuilder.Visual("Eye", PrimitiveType.Sphere, b, new Vector3(0.15f, 0.04f, -0.08f), Vector3.one * 0.07f, stripe, false);
            var wl = LevelBuilder.Visual("Wing", PrimitiveType.Sphere, b, new Vector3(0f, 0.15f, 0.08f), new Vector3(0.2f, 0.04f, 0.14f), wing, false).transform;
            var wr = LevelBuilder.Visual("Wing", PrimitiveType.Sphere, b, new Vector3(0f, 0.15f, -0.08f), new Vector3(0.2f, 0.04f, 0.14f), wing, false).transform;
            h.bees.Add(new Bee { t = b, wl = wl, wr = wr, phase = i * 1.05f });
        }
        h.buzz = LevelSfx.LoopSource(go, "bee", 0f);
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time, dt = Time.deltaTime;
        RowaidaController r = gm.Rowaida;
        Vector3 rp = r != null ? r.transform.position + Vector3.up * 0.6f : hivePos;
        bool inZone = r != null && rp.x > x - range && rp.x < x + range && rp.y - top < 3.5f;

        // دايرة جديدة قريب؟ تصير وردة
        foreach (InkMark m in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
        {
            if (!m.closed || m.beingEaten) continue;
            var mr = m.GetComponent<MeshRenderer>();
            if (mr == null || mr.bounds.center.x < x - range - 5f || mr.bounds.center.x > x + range + 1f) continue;
            Vector3 c = mr.bounds.center;
            Destroy(m.gameObject);
            if (flower != null) Destroy(flower.gameObject);
            flower = MakeFlower(c, gm);
            lureUntil = t + LureTime;
            LevelSfx.Play("pop", 0.8f, 1.3f);
            LevelSfx.Play("chime", 0.5f, 1.4f);
            break;
        }
        if (flower != null && t > lureUntil)
        {
            // الوردة تذبل وتطيح أوراقها
            Fx.Sparkle(flower.position + Vector3.up * 0.8f, new Color(1f, 0.6f, 0.8f), 10);
            Destroy(flower.gameObject);
            flower = null;
        }

        // النحل: يحرس الخلية، أو يلحق رويدا، أو يلف حول الوردة
        float closest = 99f;
        foreach (Bee b in bees)
        {
            Vector3 target;
            if (Lured) target = flower.position + Vector3.up * 0.9f + new Vector3(Mathf.Cos(t * 3f + b.phase), Mathf.Sin(t * 4f + b.phase) * 0.4f, Mathf.Sin(t * 3f + b.phase)) * 0.7f;
            else if (inZone && gm.IsPlaying) target = rp + new Vector3(Mathf.Cos(t * 6f + b.phase), Mathf.Sin(t * 7f + b.phase), 0f) * 0.4f;
            else target = hivePos + new Vector3(Mathf.Cos(t * 1.8f + b.phase) * 1.6f, Mathf.Sin(t * 2.3f + b.phase) * 0.8f - 0.6f, Mathf.Sin(t * 1.8f + b.phase) * 1f);
            Vector3 p = b.t.position;
            float spd = inZone && !Lured ? 7f : 3.5f;
            b.vel = Vector3.Lerp(b.vel, (target - p) * 3f, dt * 4f);
            if (b.vel.magnitude > spd) b.vel = b.vel.normalized * spd;
            p += b.vel * dt + new Vector3(0f, Mathf.Sin(t * 20f + b.phase) * 0.01f, 0f);
            b.t.position = p;
            b.t.localRotation = Quaternion.Euler(0f, b.vel.x >= 0f ? 0f : 180f, 0f);
            float flap = Mathf.Sin(t * 60f + b.phase) * 50f;
            b.wl.localRotation = Quaternion.Euler(flap, 0f, 0f);
            b.wr.localRotation = Quaternion.Euler(-flap, 0f, 0f);
            if (r != null) closest = Mathf.Min(closest, (p - rp).magnitude);
            // تقرص
            if (r != null && !Lured && gm.IsPlaying && t >= nextSting && (p - rp).sqrMagnitude < 0.5f * 0.5f && !r.IsKnocked)
            {
                nextSting = t + 1.2f;
                r.Knockback(-1.3f);
                LevelSfx.Play("zap", 0.5f, 1.6f);
                gm.Sound.PlaySqueak(1.5f);
                Fx.Sparkle(rp, new Color(1f, 0.85f, 0.2f), 12);
                gm.UI.ShowMessage("", 1.6f, "آي! النحلة قرصتك");
            }
        }
        // صوت الطنين يعلى كل ما قربت رويدا
        if (buzz != null)
        {
            float d = r != null ? Mathf.Abs(rp.x - x) : 99f;
            buzz.volume = Mathf.Lerp(buzz.volume, Mathf.Clamp01(1f - d / 12f) * (inZone && !Lured ? 0.6f : 0.25f), dt * 3f);
            buzz.pitch = inZone && !Lured ? 1.25f : 1f;
        }
        hive.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.2f) * 3f);
        if (flower != null) flower.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 2f) * 5f);

        bool wait = HoldHelper.Before(x - range) && !Lured;
        if (wait && !prompted) { prompted = true; gm.UI.ShowMessage("", 3.2f, "النحل يحرس الطريق! ارسمي دايرة تصير وردة تلهيه"); }
        HoldHelper.Hold(ref holding, wait);
    }

    // وردة على الأرض تحت مكان الدايرة
    Transform MakeFlower(Vector3 at, GameManager gm)
    {
        float groundY = top;
        if (Physics.Raycast(at + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 12f, ~0, QueryTriggerInteraction.Ignore)) groundY = hit.point.y;
        var f = new GameObject("Flower").transform;
        f.SetParent(transform, false);
        f.position = new Vector3(at.x, groundY, 0.6f);
        Color[] petals = { new Color(1f, 0.45f, 0.6f), new Color(0.65f, 0.5f, 1f), new Color(1f, 0.6f, 0.3f), new Color(0.4f, 0.7f, 1f) };
        Material petal = gm.Mat("Flower Petal " + Random.Range(0, 4), petals[Random.Range(0, petals.Length)], 0.45f);
        Material stem = gm.Mat("Leaf Green", new Color(0.35f, 0.72f, 0.3f), 0.35f);
        Material center = gm.Mat("Flower Center", new Color(1f, 0.85f, 0.25f), 0.5f);
        LevelBuilder.Visual("Stem", PrimitiveType.Cylinder, f, new Vector3(0f, 0.45f, 0f), new Vector3(0.08f, 0.45f, 0.08f), stem, false);
        LevelBuilder.Visual("Leaf", PrimitiveType.Sphere, f, new Vector3(0.18f, 0.35f, 0f), new Vector3(0.35f, 0.08f, 0.15f), stem, false);
        var head = new GameObject("Head").transform;
        head.SetParent(f, false);
        head.localPosition = new Vector3(0f, 0.95f, -0.05f);
        for (int i = 0; i < 6; i++)
        {
            float a = i / 6f * Mathf.PI * 2f;
            LevelBuilder.Visual("Petal", PrimitiveType.Sphere, head, new Vector3(Mathf.Cos(a) * 0.22f, Mathf.Sin(a) * 0.22f, 0f), new Vector3(0.26f, 0.26f, 0.08f), petal, false);
        }
        LevelBuilder.Visual("Center", PrimitiveType.Sphere, head, new Vector3(0f, 0f, -0.04f), Vector3.one * 0.2f, center, false);
        Fx.Sparkle(f.position + Vector3.up, new Color(1f, 0.7f, 0.85f), 16);
        return f;
    }
}

// ---------- منظر الغابة ----------

// شجر بعيد ورا (يتحرك أبطأ من الطريق لأنه بعيد)، شجر قريب ورا الصفحات، عشب وسراخس قدام،
// فراشات حول الطريق، أرانب، طيور بالسما، وأشعة شمس بين الشجر
public class ForestScenery : MonoBehaviour
{
    readonly List<Transform> butterflies = new List<Transform>();
    readonly List<Vector3> butterflyHome = new List<Vector3>();
    readonly List<Transform> birds = new List<Transform>();
    readonly List<Transform> sway = new List<Transform>();
    readonly List<Transform> bunnies = new List<Transform>();
    readonly List<Vector3> bunnyHome = new List<Vector3>();
    readonly List<Transform> glows = new List<Transform>();
    float minX, maxX;
    bool magic;

    public static void Create(Transform root, LevelData data, GameManager gm, System.Random rng)
    {
        var go = new GameObject("Forest Scenery");
        go.transform.SetParent(root, false);
        var s = go.AddComponent<ForestScenery>();
        s.magic = data.ambience == "magic";
        float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        s.minX = float.MaxValue; s.maxX = float.MinValue;
        foreach (Block b in data.blocks) { s.minX = Mathf.Min(s.minX, b.center.x - b.size.x * 0.5f); s.maxX = Mathf.Max(s.maxX, b.center.x + b.size.x * 0.5f); }

        Material bark = gm.Mat("Tree Bark", new Color(0.5f, 0.33f, 0.2f), 0.2f);
        Color[] leafCols = s.magic
            ? new[] { new Color(0.45f, 0.35f, 0.75f), new Color(0.3f, 0.5f, 0.7f), new Color(0.6f, 0.4f, 0.8f) }
            : new[] { new Color(0.3f, 0.62f, 0.32f), new Color(0.42f, 0.7f, 0.36f), new Color(0.26f, 0.52f, 0.32f), new Color(0.55f, 0.72f, 0.3f) };

        // شجر بعيد كبير (طبقتين)
        for (int layer = 0; layer < 2; layer++)
        {
            float z = layer == 0 ? 20f : 11f;
            float scale = layer == 0 ? 1.7f : 1.15f;
            for (float x = s.minX - 14f; x < s.maxX + 14f; x += R(5f, 9f) * scale)
            {
                Vector3 tp = new Vector3(x, -6f, z + R(-1.5f, 1.5f));
                var tree = s.magic && layer == 1 && rng.Next(3) > 0
                    ? MakeGiantShroom(go.transform, tp, scale * R(0.8f, 1.2f), gm, rng)
                    : MakeTree(go.transform, tp, scale * R(0.8f, 1.2f), bark, gm.Mat("Forest Leaf " + layer + rng.Next(3), leafCols[rng.Next(leafCols.Length)] * (layer == 0 ? 0.85f : 1f), 0.3f), rng, layer == 0);
                s.sway.Add(tree);
            }
        }
        // شجيرات وسراخس ورا الصفحات مباشرة
        Material fern = gm.Mat("Fern", s.magic ? new Color(0.4f, 0.45f, 0.75f) : new Color(0.3f, 0.6f, 0.3f), 0.3f);
        foreach (Block b in data.blocks)
        {
            float top = b.center.y + b.size.y * 0.5f, x0 = b.center.x - b.size.x * 0.5f, x1 = b.center.x + b.size.x * 0.5f;
            for (float x = x0 + 0.6f; x < x1 - 0.4f; x += R(1.2f, 2.4f))
            {
                var bush = new GameObject("Fern").transform;
                bush.SetParent(go.transform, false);
                bush.position = new Vector3(x, top, R(1.6f, 1.95f));
                for (int k = 0; k < 5; k++)
                {
                    var leafT = LevelBuilder.Visual("Frond", PrimitiveType.Capsule, bush, Vector3.zero, new Vector3(0.14f, 0.45f, 0.05f), fern, false).transform;
                    leafT.localRotation = Quaternion.Euler(0f, 0f, -60f + k * 30f);
                    leafT.localPosition = leafT.localRotation * new Vector3(0f, 0.4f, 0f);
                }
                s.sway.Add(bush);
                // فطر صغير أو زهرة بين الشجيرات
                if (rng.Next(3) == 0)
                {
                    bool glowShroom = s.magic || rng.Next(2) == 0;
                    Color c = glowShroom ? (s.magic ? new Color(0.5f, 1.4f, 1.6f) : new Color(1f, 0.35f, 0.35f)) : new Color(1f, 0.85f, 0.3f);
                    var m = new GameObject("Tiny Mushroom").transform;
                    m.SetParent(go.transform, false);
                    m.position = new Vector3(x + 0.5f, top, R(1.1f, 1.5f));
                    LevelBuilder.Visual("Stem", PrimitiveType.Cylinder, m, new Vector3(0f, 0.12f, 0f), new Vector3(0.1f, 0.12f, 0.1f), gm.Mat("Mushroom Stem", new Color(0.98f, 0.93f, 0.82f), 0.3f), false);
                    LevelBuilder.Visual("Cap", PrimitiveType.Sphere, m, new Vector3(0f, 0.26f, 0f), new Vector3(0.32f, 0.16f, 0.32f), gm.Mat("Tiny Cap " + (s.magic ? "Glow" : glowShroom ? "Red" : "Yellow"), c, 0.5f), false);
                    if (s.magic) s.glows.Add(m);
                }
            }
            // عشب قدام على حافة الصفحة
            Material grass = gm.Mat("Grass Blade", s.magic ? new Color(0.35f, 0.45f, 0.65f) : new Color(0.4f, 0.75f, 0.35f), 0.3f);
            for (float x = x0 + 0.3f; x < x1 - 0.2f; x += R(0.5f, 1.1f))
            {
                var tuft = new GameObject("Grass").transform;
                tuft.SetParent(go.transform, false);
                tuft.position = new Vector3(x, top, R(-1.95f, -1.75f));
                for (int k = 0; k < 3; k++)
                {
                    var blade = LevelBuilder.Visual("Blade", PrimitiveType.Cube, tuft, Vector3.zero, new Vector3(0.05f, R(0.25f, 0.45f), 0.03f), grass, false).transform;
                    blade.localRotation = Quaternion.Euler(0f, 0f, -20f + k * 20f);
                    blade.localPosition = blade.localRotation * new Vector3(0f, blade.localScale.y * 0.5f, 0f);
                }
                s.sway.Add(tuft);
            }
            // أرنب يلعب ورا (بعض الصفحات)
            if (b.size.x > 7f && rng.Next(2) == 0)
            {
                Vector3 home = new Vector3(b.center.x + R(-b.size.x * 0.3f, b.size.x * 0.3f), top, 1.4f);
                s.bunnies.Add(MakeBunny(go.transform, home, gm));
                s.bunnyHome.Add(home);
            }
        }
        // فراشات
        Color[] wingCols = { new Color(1f, 0.6f, 0.2f), new Color(0.5f, 0.7f, 1f), new Color(1f, 0.5f, 0.75f), new Color(1f, 0.9f, 0.3f) };
        for (int i = 0; i < 8; i++)
        {
            Vector3 home = new Vector3(R(s.minX, s.maxX), R(1.5f, 3.5f), R(-1f, 1.5f));
            var bf = new GameObject("Butterfly").transform;
            bf.SetParent(go.transform, false);
            bf.position = home;
            Material w = gm.Mat("Butterfly " + i % 4, s.magic ? wingCols[i % 4] * 1.6f : wingCols[i % 4], 0.4f);
            for (int side = -1; side <= 1; side += 2)
            {
                var wing = new GameObject("Wing").transform;
                wing.SetParent(bf, false);
                LevelBuilder.Visual("Upper", PrimitiveType.Sphere, wing, new Vector3(0.05f, 0f, side * 0.13f), new Vector3(0.2f, 0.02f, 0.22f), w, false);
                LevelBuilder.Visual("Lower", PrimitiveType.Sphere, wing, new Vector3(-0.08f, 0f, side * 0.1f), new Vector3(0.13f, 0.02f, 0.15f), w, false);
            }
            LevelBuilder.Visual("Body", PrimitiveType.Capsule, bf, Vector3.zero, new Vector3(0.04f, 0.1f, 0.04f), bark, false).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            s.butterflies.Add(bf);
            s.butterflyHome.Add(home);
        }
        // طيور بالسما
        if (!s.magic)
            for (int i = 0; i < 5; i++)
            {
                var bird = MakeBird(go.transform, new Vector3(R(s.minX, s.maxX), R(6f, 9f), R(6f, 10f)), gm);
                s.birds.Add(bird);
            }
        // أشعة شمس بين الشجر
        if (!s.magic)
        {
            var tex = new Texture2D(4, 32, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 4; x++)
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Sin(y / 31f * Mathf.PI) * 0.5f));
            tex.Apply();
            var rayMat = NightSky.Transparent("Sun Ray", tex, new Color(1.3f, 1.2f, 0.8f, 0.18f));
            for (float x = s.minX; x < s.maxX; x += R(9f, 14f))
            {
                var ray = LevelBuilder.Visual("Sun Ray", PrimitiveType.Quad, go.transform, new Vector3(x, 4f, 4f), new Vector3(14f, R(1.2f, 2.2f), 1f), rayMat, false).transform;
                ray.localRotation = Quaternion.Euler(0f, 0f, -60f);
            }
        }
    }

    static Transform MakeTree(Transform parent, Vector3 basePos, float scale, Material bark, Material leaf, System.Random rng, bool far)
    {
        var t = new GameObject("Tree").transform;
        t.SetParent(parent, false);
        t.position = basePos;
        float h = (far ? 9f : 11f) * scale;
        LevelBuilder.Visual("Trunk", PrimitiveType.Cylinder, t, new Vector3(0f, h * 0.5f, 0f), new Vector3(0.5f * scale, h * 0.5f, 0.5f * scale), bark, !far);
        int puffs = 3 + rng.Next(3);
        for (int i = 0; i < puffs; i++)
        {
            float a = i / (float)puffs * Mathf.PI * 2f;
            LevelBuilder.Visual("Leaves", PrimitiveType.Sphere, t, new Vector3(Mathf.Cos(a) * scale, h + Mathf.Sin(a) * 0.6f * scale, Mathf.Sin(a) * 0.4f * scale), Vector3.one * (2.2f + (float)rng.NextDouble()) * scale, leaf, !far);
        }
        LevelBuilder.Visual("Top", PrimitiveType.Sphere, t, new Vector3(0f, h + 1.2f * scale, 0f), Vector3.one * 2.4f * scale, leaf, !far);
        return t;
    }

    // فطرة عملاقة تضوي (لبستان الفطر)
    static Transform MakeGiantShroom(Transform parent, Vector3 basePos, float scale, GameManager gm, System.Random rng)
    {
        Color[] caps = { new Color(1.2f, 0.45f, 0.7f), new Color(0.5f, 0.8f, 1.4f), new Color(0.8f, 0.55f, 1.3f), new Color(1.3f, 0.75f, 0.35f) };
        var t = new GameObject("Tree").transform;
        t.SetParent(parent, false);
        t.position = basePos;
        float h = 10f * scale;
        Material stem = gm.Mat("Giant Shroom Stem", new Color(0.95f, 0.9f, 0.85f), 0.3f);
        Material cap = gm.Mat("Giant Shroom Cap " + rng.Next(4), caps[rng.Next(caps.Length)], 0.5f);
        Material dot = gm.Mat("Giant Shroom Dot", new Color(1.3f, 1.25f, 1.1f), 0.4f);
        LevelBuilder.Visual("Stem", PrimitiveType.Cylinder, t, new Vector3(0f, h * 0.5f, 0f), new Vector3(0.9f * scale, h * 0.5f, 0.9f * scale), stem, false);
        LevelBuilder.Visual("Cap", PrimitiveType.Sphere, t, new Vector3(0f, h, 0f), new Vector3(5f, 2.2f, 5f) * scale, cap, false);
        for (int i = 0; i < 6; i++)
        {
            float a = i / 6f * Mathf.PI * 2f;
            LevelBuilder.Visual("Dot", PrimitiveType.Sphere, t, new Vector3(Mathf.Cos(a) * 1.5f * scale, h + 0.7f * scale, Mathf.Sin(a) * 1.5f * scale - 0.4f * scale), new Vector3(0.7f, 0.3f, 0.7f) * scale, dot, false);
        }
        return t;
    }

    public static Transform MakeBird(Transform parent, Vector3 pos, GameManager gm)
    {
        Color[] cols = { new Color(0.35f, 0.6f, 1f), new Color(1f, 0.45f, 0.35f), new Color(1f, 0.8f, 0.25f) };
        Material body = gm.Mat("Bird " + Random.Range(0, 3), cols[Random.Range(0, cols.Length)], 0.4f);
        Material beak = gm.Mat("Bird Beak", new Color(1f, 0.62f, 0.25f), 0.3f);
        Material black = gm.Mat("Monster Pupil", new Color(0.05f, 0.04f, 0.06f), 0.9f);
        var b = new GameObject("Bird").transform;
        b.SetParent(parent, false);
        b.localPosition = pos;
        LevelBuilder.Visual("Body", PrimitiveType.Sphere, b, Vector3.zero, new Vector3(0.45f, 0.35f, 0.32f), body, false);
        LevelBuilder.Visual("Head", PrimitiveType.Sphere, b, new Vector3(0.2f, 0.17f, 0f), Vector3.one * 0.26f, body, false);
        LevelBuilder.Visual("Beak", PrimitiveType.Cube, b, new Vector3(0.36f, 0.15f, 0f), new Vector3(0.12f, 0.05f, 0.05f), beak, false);
        LevelBuilder.Visual("Eye", PrimitiveType.Sphere, b, new Vector3(0.27f, 0.22f, -0.1f), Vector3.one * 0.05f, black, false);
        for (int s = -1; s <= 1; s += 2)
        {
            var w = LevelBuilder.Visual("Wing", PrimitiveType.Sphere, b, new Vector3(-0.02f, 0.05f, s * 0.16f), new Vector3(0.3f, 0.06f, 0.2f), body, false).transform;
            w.name = "Wing";
        }
        LevelBuilder.Visual("Tail", PrimitiveType.Cube, b, new Vector3(-0.27f, 0.06f, 0f), new Vector3(0.18f, 0.04f, 0.14f), body, false);
        return b;
    }

    static Transform MakeBunny(Transform parent, Vector3 pos, GameManager gm)
    {
        Material fur = gm.Mat("Bunny Fur", new Color(0.97f, 0.95f, 0.92f), 0.3f);
        Material pink = gm.Mat("Bunny Pink", new Color(1f, 0.7f, 0.75f), 0.3f);
        Material black = gm.Mat("Monster Pupil", new Color(0.05f, 0.04f, 0.06f), 0.9f);
        var b = new GameObject("Bunny").transform;
        b.SetParent(parent, false);
        b.position = pos;
        LevelBuilder.Visual("Body", PrimitiveType.Sphere, b, new Vector3(0f, 0.28f, 0f), new Vector3(0.6f, 0.5f, 0.45f), fur);
        LevelBuilder.Visual("Head", PrimitiveType.Sphere, b, new Vector3(0.28f, 0.55f, 0f), Vector3.one * 0.36f, fur);
        for (int s = -1; s <= 1; s += 2)
        {
            var ear = LevelBuilder.Visual("Ear", PrimitiveType.Capsule, b, new Vector3(0.24f, 0.88f, s * 0.08f), new Vector3(0.1f, 0.2f, 0.06f), fur, false).transform;
            ear.localRotation = Quaternion.Euler(s * 10f, 0f, 15f);
            LevelBuilder.Visual("Inner", PrimitiveType.Capsule, ear, new Vector3(0f, 0f, -0.3f), new Vector3(0.5f, 0.8f, 0.3f), pink, false);
        }
        LevelBuilder.Visual("Eye", PrimitiveType.Sphere, b, new Vector3(0.4f, 0.6f, -0.12f), Vector3.one * 0.06f, black, false);
        LevelBuilder.Visual("Nose", PrimitiveType.Sphere, b, new Vector3(0.46f, 0.52f, -0.02f), Vector3.one * 0.05f, pink, false);
        LevelBuilder.Visual("Tail", PrimitiveType.Sphere, b, new Vector3(-0.32f, 0.35f, 0f), Vector3.one * 0.16f, fur, false);
        return b;
    }

    void Update()
    {
        float t = Time.time, dt = Time.deltaTime;
        for (int i = 0; i < sway.Count; i++) sway[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 0.9f + i * 0.7f) * (sway[i].name == "Tree" ? 1f : 6f));
        for (int i = 0; i < butterflies.Count; i++)
        {
            Vector3 h = butterflyHome[i];
            butterflies[i].position = h + new Vector3(Mathf.Sin(t * 0.5f + i) * 2.5f, Mathf.Sin(t * 1.3f + i * 2f) * 0.6f, Mathf.Cos(t * 0.4f + i) * 0.8f);
            float flap = Mathf.Abs(Mathf.Sin(t * 14f + i)) * 70f;
            butterflies[i].GetChild(0).localRotation = Quaternion.Euler(flap, 0f, 0f);
            butterflies[i].GetChild(1).localRotation = Quaternion.Euler(-flap, 0f, 0f);
            butterflies[i].localRotation = Quaternion.Euler(0f, Mathf.Cos(t * 0.5f + i) > 0f ? 0f : 180f, 0f);
        }
        for (int i = 0; i < birds.Count; i++)
        {
            Vector3 p = birds[i].localPosition;
            p.x += (1.5f + i * 0.3f) * dt;
            if (p.x > maxX + 10f) p.x = minX - 10f;
            p.y += Mathf.Sin(t * 1.2f + i) * 0.005f;
            birds[i].localPosition = p;
            foreach (Transform c in birds[i])
                if (c.name == "Wing") c.localRotation = Quaternion.Euler(Mathf.Sign(c.localPosition.z) * Mathf.Sin(t * 10f + i) * 45f, 0f, 0f);
        }
        // الأرانب: نطات صغيرة رايح جاي
        for (int i = 0; i < bunnies.Count; i++)
        {
            float k = t * 0.8f + i * 3f;
            float hop = Mathf.Abs(Mathf.Sin(k * Mathf.PI * 1.5f));
            float dx = Mathf.Sin(k * 0.4f) * 1.5f;
            bunnies[i].position = bunnyHome[i] + new Vector3(dx, hop * 0.35f, 0f);
            bunnies[i].localRotation = Quaternion.Euler(0f, Mathf.Cos(k * 0.4f) > 0f ? 0f : 180f, 0f);
        }
        for (int i = 0; i < glows.Count; i++) glows[i].localScale = Vector3.one * (1f + Mathf.Sin(t * 2f + i) * 0.1f);
    }
}

// ---------- الملصق المخفي ----------

// بكل مرحلة من الجزء الثالث ملصق لامع مخبى بمكان صعب شوي: المسيه بخط (أو تمر عليه رويدا) وينحفظ
public class Sticker : MonoBehaviour
{
    Vector3 home;
    int levelIndex;
    bool taken;
    Transform badge;

    public static void Create(Transform root, Vector3 pos, LevelData data, GameManager gm)
    {
        int index = Levels.All.IndexOf(data);
        if (index < 0) index = gm.LevelIndex;
        var go = new GameObject("Sticker");
        go.transform.SetParent(root, false);
        go.transform.position = pos;
        var s = go.AddComponent<Sticker>();
        s.home = pos;
        s.levelIndex = index;
        bool had = index >= 0 && index < Levels.All.Count && SaveData.HasSticker(index);
        Material rim = gm.Mat("Sticker Rim", Color.white, 0.6f);
        Material face = gm.Mat("Sticker Face", had ? new Color(0.8f, 0.8f, 0.85f) : new Color(1f, 0.45f, 0.65f), 0.6f);
        Material star = gm.Mat("Sticker Star", had ? new Color(0.95f, 0.95f, 0.95f) : new Color(1.4f, 1.15f, 0.3f), 0.6f);
        s.badge = new GameObject("Badge").transform;
        s.badge.SetParent(go.transform, false);
        var r = LevelBuilder.Visual("Rim", PrimitiveType.Cylinder, s.badge, Vector3.zero, new Vector3(0.75f, 0.03f, 0.75f), rim, false).transform;
        r.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var f = LevelBuilder.Visual("Face", PrimitiveType.Cylinder, s.badge, new Vector3(0f, 0f, -0.02f), new Vector3(0.62f, 0.03f, 0.62f), face, false).transform;
        f.localRotation = Quaternion.Euler(90f, 0f, 0f);
        for (int i = 0; i < 2; i++)
        {
            var st = LevelBuilder.Visual("Star", PrimitiveType.Cube, s.badge, new Vector3(0f, 0f, -0.05f), new Vector3(0.28f, 0.28f, 0.02f), star, false).transform;
            st.localRotation = Quaternion.Euler(0f, 0f, i * 45f);
        }
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || taken) return;
        float t = Time.time;
        transform.position = home + Vector3.up * Mathf.Sin(t * 2f) * 0.15f;
        badge.localRotation = Quaternion.Euler(0f, Mathf.Sin(t * 1.5f) * 35f, Mathf.Sin(t * 0.9f) * 8f);
        if (Random.value < 0.08f) Fx.Sparkle(transform.position + Random.insideUnitSphere * 0.4f, new Color(1f, 0.9f, 0.5f), 1);

        bool touched = gm.Rowaida != null && (gm.Rowaida.transform.position + Vector3.up * 0.5f - transform.position).sqrMagnitude < 0.8f * 0.8f;
        if (!touched)
        {
            Bounds me = new Bounds(transform.position, new Vector3(1.2f, 1.2f, 4f));
            foreach (InkMark m in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
            {
                var mr = m.GetComponent<MeshRenderer>();
                if (mr != null && mr.bounds.Intersects(me)) { touched = true; break; }
            }
        }
        if (!touched) return;
        taken = true;
        bool first = levelIndex >= 0 && levelIndex < Levels.All.Count && !SaveData.HasSticker(levelIndex);
        if (levelIndex >= 0 && levelIndex < Levels.All.Count) SaveData.SetSticker(levelIndex);
        LevelSfx.Play("sticker", 0.9f);
        Fx.Sparkle(transform.position, new Color(1f, 0.6f, 0.8f), 30);
        Fx.Sparkle(transform.position, new Color(1f, 0.95f, 0.5f), 20);
        gm.UI.ShowMessage("", 2.4f, first ? "لقيتي الملصق المخفي!" : "هذا الملصق عندك من قبل");
        Destroy(gameObject, 0.05f);
    }
}
