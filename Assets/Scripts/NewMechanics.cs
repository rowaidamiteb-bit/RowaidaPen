using System.Collections.Generic;
using UnityEngine;

// أفكار المراحل الجديدة: بالونات، نار، بوابات، جسور تنكسر، وظلام بفانوس
public static class NewFeatures
{
    public static void Build(Transform root, LevelData data, GameManager gm, System.Random rng)
    {
        foreach (Vector4 f in data.firePits) FirePit.Create(root, f, gm);
        foreach (Vector3 p in data.pins) PinRow.Create(root, p, gm);
        foreach (Vector3 f in data.fans) FanZone.Create(root, f, gm);
        foreach (Vector4 p in data.portals) Portal.Create(root, new Vector3(p.x, p.y, 0f), new Vector3(p.z, p.w, 0f));
        foreach (Vector3 peg in data.pegs)
            LevelBuilder.CreatePaperBlock(root, new Block(new Vector3(peg.x, peg.y - 0.3f, 0f), new Vector3(peg.z, 0.6f, 2.2f)), gm, rng, data, false);
        if (data.darkness) BuildDarkness(root, data, gm);
    }

    // ---------- الرسم ----------

    // خط جديد: في الوادي الخط الطويل يصير هش، وفي الظلام يضوي
    public static void OnLineMade(GameObject stroke, float length, LevelData level)
    {
        if (level == null) return;
        if (level.maxSpan > 0f && length > level.maxSpan) stroke.AddComponent<FragileInk>();
        if (level.darkness) AddGlow(stroke, length);
    }

    // شكل مغلق جديد: في صفحة البالونات يصير بالونة
    public static void OnShapeMade(GameObject stroke, LevelData level)
    {
        if (level == null) return;
        if (level.balloonInk) stroke.AddComponent<BalloonLift>();
        if (level.darkness) AddGlow(stroke, 3f);
    }

    static Material glowInk;

    // حبر يضوي في الظلام
    public static Material GlowInk()
    {
        if (glowInk == null)
        {
            glowInk = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "Glow Ink" };
            glowInk.SetColor("_BaseColor", new Color(1.7f, 1.55f, 0.75f));
        }
        return glowInk;
    }

    static void AddGlow(GameObject stroke, float length)
    {
        var r = stroke.GetComponent<MeshRenderer>();
        if (r == null) return;
        var light = new GameObject("Ink Light").AddComponent<Light>();
        light.transform.SetParent(stroke.transform, false);
        light.transform.position = r.bounds.center + new Vector3(0f, 0.3f, -0.6f);
        light.type = LightType.Point;
        light.color = new Color(1f, 0.9f, 0.55f);
        light.range = Mathf.Clamp(length * 0.7f, 2.5f, 5.5f);
        light.intensity = 1.6f;
    }

    // ---------- الظلام ----------

    static void BuildDarkness(Transform root, LevelData data, GameManager gm)
    {
        // فانوس مع رويدا: ضو دافي يمشي معها
        var rowaida = root.GetComponentInChildren<RowaidaController>();
        if (rowaida != null)
        {
            var lantern = new GameObject("Lantern").AddComponent<Light>();
            lantern.transform.SetParent(rowaida.transform, false);
            lantern.transform.localPosition = new Vector3(0.4f, 0.5f, -0.7f);
            lantern.type = LightType.Point;
            lantern.color = new Color(1f, 0.82f, 0.5f);
            lantern.range = 7f;
            lantern.intensity = 3.2f;
            var glow = gm.Mat("Lantern Glow", new Color(2.6f, 2f, 0.9f), 0.9f);
            LevelBuilder.Visual("Lantern", PrimitiveType.Sphere, rowaida.transform, new Vector3(0.38f, 0.05f, -0.32f), Vector3.one * 0.16f, glow, false);
            lantern.gameObject.AddComponent<Flicker>().baseIntensity = 3.2f;
        }

        // يراعات عند أطراف الحفر (تعلّم وين تنتهي الأرض)
        var blocks = data.blocks;
        for (int i = 0; i < blocks.Count - 1; i++)
        {
            Block a = blocks[i], b = blocks[i + 1];
            float x0 = a.center.x + a.size.x * 0.5f, x1 = b.center.x - b.size.x * 0.5f;
            if (x1 - x0 < 0.5f) continue;
            FireflyCluster.Create(root, new Vector3(x0 - 0.2f, a.center.y + a.size.y * 0.5f + 0.5f, -0.3f), gm);
            FireflyCluster.Create(root, new Vector3(x1 + 0.2f, b.center.y + b.size.y * 0.5f + 0.5f, -0.3f), gm);
        }
    }

    // عشان رويدا ما تمشي لين ما يخلص الحبس (تستخدمها النار)
    public static bool RowaidaOn(Bounds b, Vector3 p) => p.x > b.min.x - 0.15f && p.x < b.max.x + 0.15f && p.y > b.max.y + 0.2f && p.y < b.max.y + 1.4f;
}

// يرف الضو شوي مثل نار الفانوس
public class Flicker : MonoBehaviour
{
    public float baseIntensity = 1f;
    Light lightRef;
    void Start() { lightRef = GetComponent<Light>(); }
    void Update()
    {
        if (lightRef != null) lightRef.intensity = baseIntensity * (0.9f + Mathf.PerlinNoise(Time.time * 3f, 0.4f) * 0.2f);
    }
}

// مجموعة يراعات تلف حول نقطة مع ضو أخضر خفيف
public class FireflyCluster : MonoBehaviour
{
    readonly List<Transform> bugs = new List<Transform>();
    float seed;

    public static void Create(Transform root, Vector3 pos, GameManager gm)
    {
        var go = new GameObject("Fireflies");
        go.transform.SetParent(root, false);
        go.transform.position = pos;
        var c = go.AddComponent<FireflyCluster>();
        c.seed = pos.x * 1.7f;
        var mat = gm.Mat("Firefly Glow", new Color(2.2f, 2.8f, 0.9f), 0.9f);
        for (int i = 0; i < 4; i++)
            c.bugs.Add(LevelBuilder.Visual("Firefly", PrimitiveType.Sphere, go.transform, Vector3.zero, Vector3.one * 0.07f, mat, false).transform);
        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(0.75f, 1f, 0.45f);
        light.range = 2.6f;
        light.intensity = 1.3f;
    }

    void Update()
    {
        float t = Time.time + seed;
        for (int i = 0; i < bugs.Count; i++)
        {
            float a = t * (0.8f + i * 0.25f) + i * 1.6f;
            bugs[i].localPosition = new Vector3(Mathf.Cos(a) * 0.45f, Mathf.Sin(a * 1.3f) * 0.3f, Mathf.Sin(a) * 0.3f);
            bugs[i].localScale = Vector3.one * (0.05f + 0.03f * Mathf.Abs(Mathf.Sin(t * 4f + i)));
        }
    }
}

// ---------- البالونات ----------

// الشكل المغلق يصير سلة، وفوقها بالونة حقيقية (لمعة وعقدة وخيطين).
// لما توقف رويدا في السلة تطلع لين الأرض العالية اللي جنبها. لو صدمت دبابيس تنفجر وتطيح السلة،
// ولو كانت عند مروحة تدفها المروحة لليمين فوق الحفرة الطويلة
public class BalloonLift : MonoBehaviour
{
    const float RiseSpeed = 1.7f, FerrySpeed = 2.3f;
    static readonly Color[] Colors = { new Color(1f, 0.4f, 0.45f), new Color(1f, 0.75f, 0.25f), new Color(0.4f, 0.7f, 1f), new Color(0.55f, 0.85f, 0.45f), new Color(0.8f, 0.55f, 1f) };
    static int next;

    Rigidbody rb;
    MeshRenderer rend;
    Transform balloon;
    Color color;
    float targetTop, doneTime, ferryStart, lastX;
    bool rising, ferrying, done, popped;
    FanZone fan;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rend = GetComponent<MeshRenderer>();
        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezePositionX;
        GameManager gm = GameManager.Instance;

        color = Colors[next++ % Colors.Length];
        Bounds b = rend.bounds;

        // البالونة فوق السلة (أعلى من راس رويدا) مع لمعة وعقدة
        Material skin = gm.Mat("Balloon " + ColorUtility.ToHtmlStringRGB(color), color, 0.85f);
        Material shine = gm.Mat("Balloon Shine", new Color(1.6f, 1.6f, 1.6f), 0.9f);
        Material stringMat = gm.Mat("Balloon String", new Color(0.95f, 0.95f, 0.95f), 0.2f);
        balloon = new GameObject("Balloon").transform;
        balloon.SetParent(transform, false);
        balloon.position = new Vector3(b.center.x, b.max.y + 2.35f, 0f);
        float size = Mathf.Clamp(b.size.x * 0.75f, 1.3f, 2f);
        LevelBuilder.Visual("Balloon Skin", PrimitiveType.Sphere, balloon, Vector3.zero, new Vector3(size, size * 1.15f, size), skin);
        LevelBuilder.Visual("Shine", PrimitiveType.Sphere, balloon, new Vector3(-size * 0.22f, size * 0.25f, -size * 0.4f), new Vector3(0.22f, 0.32f, 0.1f) * size, shine, false);
        LevelBuilder.Visual("Knot", PrimitiveType.Sphere, balloon, new Vector3(0f, -size * 0.6f, 0f), new Vector3(0.18f, 0.14f, 0.18f), skin, false);

        // خيطين من طرفي السلة للعقدة
        Vector3 knot = balloon.position + Vector3.down * size * 0.62f;
        for (int s = -1; s <= 1; s += 2)
        {
            Vector3 foot = new Vector3(b.center.x + s * b.extents.x * 0.8f, b.max.y, 0f);
            Vector3 mid = (foot + knot) * 0.5f, dir = knot - foot;
            var str = LevelBuilder.Visual("String", PrimitiveType.Cylinder, transform, Vector3.zero, new Vector3(0.025f, dir.magnitude * 0.5f, 0.025f), stringMat, false);
            str.transform.position = mid;
            str.transform.rotation = Quaternion.FromToRotation(Vector3.up, dir);
        }
        // السلة بلون خشبي فاتح
        rend.material.SetColor("_BaseColor", new Color(0.72f, 0.5f, 0.3f));

        Fx.Sparkle(balloon.position, color, 12);
        gm.Sound.PlayBoing();
    }

    void FixedUpdate()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || rb == null || popped) return;
        Bounds b = rend.bounds;
        RowaidaController rowaida = gm.Rowaida;
        // لازم تكون فعلًا فوقها (مو واقفة على طرف الصفحة جنبها)
        bool riding = rowaida != null && NewFeatures.RowaidaOn(b, rowaida.transform.position) && rowaida.transform.position.x > b.min.x + 0.5f;

        if (!rising && !ferrying && !done && riding && gm.IsPlaying)
        {
            fan = FanZone.At(b.center.x);
            targetTop = fan != null ? fan.Height + 0.12f : FindTarget(b, gm.CurrentLevel);
            rising = true;
            rowaida.Held = true;
            rowaida.CarryX = 0f;
            gm.Sound.PlaySqueak(1.5f);
        }

        // الدبابيس تفجرها
        if ((rising || ferrying) && PinRow.Hits(balloon.position, 0.8f)) { Pop(gm, rowaida); return; }

        Vector3 v = rb.linearVelocity;
        if (rising)
        {
            v.y = Mathf.Clamp((targetTop - b.max.y) * 3f, -RiseSpeed, RiseSpeed);
            Carry(rowaida, riding, 0f, v.y);
            if (Mathf.Abs(targetTop - b.max.y) < 0.04f)
            {
                v.y = 0f;
                rising = false;
                if (fan != null)
                {
                    // المروحة تدفها لليمين فوق الحفرة
                    ferrying = true;
                    ferryStart = Time.time;
                    lastX = float.MinValue;
                    rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezePositionY;
                }
                else Finish(rowaida);
            }
        }
        else if (ferrying)
        {
            v = new Vector3(FerrySpeed, 0f, 0f);
            Carry(rowaida, true, FerrySpeed, 0f);
            // توقف لما توصل طرف الصفحة اللي بعد الحفرة، أو لو علقت، أو لو رويدا نزلت منها
            bool stuck = Mathf.Abs(b.center.x - lastX) < 0.001f && Time.time - ferryStart > 0.5f;
            lastX = b.center.x;
            Vector3 rp = rowaida != null ? rowaida.transform.position : Vector3.zero;
            bool lost = rowaida == null || Mathf.Abs(rp.x - b.center.x) > b.extents.x + 1f || rp.y < b.max.y - 0.3f;
            if (b.max.x >= fan.To - 0.05f || stuck || lost)
            {
                v = Vector3.zero;
                ferrying = false;
                rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezePositionX;
                Finish(rowaida);
            }
        }
        else if (done)
        {
            // بعد ما تنزل منها تطير بعيد وتختفي
            float since = Time.time - doneTime;
            v.y = since > 2.2f ? 2.4f : 0f;
            if (since > 4.2f)
            {
                Fx.Sparkle(balloon.position, color, 14);
                Destroy(gameObject);
                return;
            }
        }
        else v.y = Mathf.Sin(Time.time * 2f) * 0.12f;   // تطفو مكانها
        rb.linearVelocity = v;

        // البالونة تتمايل شوي
        balloon.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 1.6f) * 4f + (ferrying ? -8f : 0f));
    }

    // رويدا تمشي مع السلة
    static void Carry(RowaidaController rowaida, bool riding, float vx, float vy)
    {
        if (!riding) return;
        rowaida.CarryX = vx;
        var body = rowaida.GetComponent<Rigidbody>();
        Vector3 rv = body.linearVelocity;
        if (vy > 0f && rv.y < vy) rv.y = vy;
        rv.x = vx;
        body.linearVelocity = rv;
    }

    void Finish(RowaidaController rowaida)
    {
        done = true;
        doneTime = Time.time;
        if (rowaida != null) { rowaida.Held = false; rowaida.CarryX = 0f; }
    }

    // انفجرت: قصاصات ملونة، والسلة تطيح
    void Pop(GameManager gm, RowaidaController rowaida)
    {
        popped = true;
        gm.Sound.PlayPoof();
        gm.Sound.PlayBoing();
        Material bit = gm.Mat("Balloon " + ColorUtility.ToHtmlStringRGB(color), color, 0.85f);
        for (int i = 0; i < 18; i++)
        {
            var p = LevelBuilder.Visual("Balloon Bit", PrimitiveType.Cube, null, balloon.position + Random.insideUnitSphere * 0.5f, new Vector3(0.18f, 0.12f, 0.02f), bit, false);
            p.transform.rotation = Random.rotation;
            var fx = p.AddComponent<FxParticle>();
            fx.velocity = Random.insideUnitSphere * 5f;
            fx.gravity = 6f;
            fx.spin = Random.insideUnitSphere * 600f;
            fx.life = 1.2f;
        }
        var cam = Camera.main.GetComponent<CameraFollow>();
        if (cam != null) cam.Shake(0.12f, 0.25f);
        Destroy(balloon.gameObject);
        foreach (Transform t in transform) if (t.name == "String") Destroy(t.gameObject);
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;
        if (rowaida != null) { rowaida.Held = false; rowaida.CarryX = 0f; }
        Destroy(gameObject, 4f);
    }

    // سطح الأرض العالية اللي جنب البالونة (على يمينها)
    static float FindTarget(Bounds b, LevelData level)
    {
        float best = float.MaxValue;
        foreach (Block blk in level.blocks)
        {
            float left = blk.center.x - blk.size.x * 0.5f, top = blk.center.y + blk.size.y * 0.5f;
            if (left > b.max.x - 1f && left < b.max.x + 3f && top > b.max.y + 0.3f) best = Mathf.Min(best, top);
        }
        return best < float.MaxValue ? best + 0.12f : b.max.y + 3f;
    }

    void OnDestroy()
    {
        var gm = GameManager.Instance;
        if ((rising || ferrying) && gm != null && gm.Rowaida != null) { gm.Rowaida.Held = false; gm.Rowaida.CarryX = 0f; }
    }
}

// صف دبابيس معلقة من خشبة: البالونة اللي تطلع تحتها تنفجر
public class PinRow : MonoBehaviour
{
    static readonly List<PinRow> all = new List<PinRow>();
    float from, to, y;

    public static void Create(Transform root, Vector3 spec, GameManager gm)
    {
        var go = new GameObject("Pins");
        go.transform.SetParent(root, false);
        var p = go.AddComponent<PinRow>();
        p.from = spec.x; p.to = spec.y; p.y = spec.z;
        Material wood = gm.Mat("Pin Beam", new Color(0.55f, 0.38f, 0.25f), 0.3f);
        Material steel = gm.Mat("Pin Steel", new Color(0.8f, 0.82f, 0.86f), 0.9f, 0.9f);
        Material[] heads = { gm.Mat("Pin Red", new Color(0.95f, 0.25f, 0.3f), 0.6f), gm.Mat("Pin Blue", new Color(0.25f, 0.5f, 0.95f), 0.6f), gm.Mat("Pin Yellow", new Color(1f, 0.8f, 0.2f), 0.6f) };
        LevelBuilder.Visual("Beam", PrimitiveType.Cube, go.transform, new Vector3((p.from + p.to) * 0.5f, p.y + 0.45f, 0f), new Vector3(p.to - p.from + 0.3f, 0.18f, 1.6f), wood);
        int i = 0;
        for (float x = p.from + 0.15f; x <= p.to - 0.1f; x += 0.32f, i++)
        {
            LevelBuilder.Visual("Pin Head", PrimitiveType.Sphere, go.transform, new Vector3(x, p.y + 0.3f, -0.1f), new Vector3(0.2f, 0.12f, 0.2f), heads[i % 3]);
            LevelBuilder.Visual("Needle", PrimitiveType.Cylinder, go.transform, new Vector3(x, p.y + 0.1f, -0.1f), new Vector3(0.025f, 0.17f, 0.025f), steel, false);
        }
        all.Add(p);
    }

    void OnDestroy() { all.Remove(this); }

    public static bool Hits(Vector3 balloonCenter, float radius)
    {
        foreach (PinRow p in all)
            if (p != null && balloonCenter.x + radius > p.from && balloonCenter.x - radius < p.to && balloonCenter.y + radius * 1.15f > p.y)
                return true;
        return false;
    }
}

// مروحة ورق كبيرة تدف البالونات لليمين فوق حفرة طويلة
public class FanZone : MonoBehaviour
{
    static readonly List<FanZone> all = new List<FanZone>();
    public float From, To, Height;
    Transform blades;
    float nextStreak;

    public static void Create(Transform root, Vector3 spec, GameManager gm)
    {
        var go = new GameObject("Fan");
        go.transform.SetParent(root, false);
        var f = go.AddComponent<FanZone>();
        f.From = spec.x; f.To = spec.y; f.Height = spec.z;
        Material stick = gm.Mat("Fan Stick", new Color(0.55f, 0.38f, 0.25f), 0.3f);
        Material[] paper = { gm.Mat("Fan Pink", new Color(1f, 0.6f, 0.7f), 0.3f), gm.Mat("Fan Yellow", new Color(1f, 0.85f, 0.4f), 0.3f), gm.Mat("Fan Blue", new Color(0.5f, 0.75f, 1f), 0.3f) };
        // المروحة واقفة على طرف الصفحة اللي قبل الحفرة
        Vector3 hub = new Vector3(f.From - 0.6f, f.Height + 2.2f, 0.4f);
        LevelBuilder.Visual("Fan Stick", PrimitiveType.Cylinder, go.transform, new Vector3(hub.x, f.Height + 1.1f, 0.4f), new Vector3(0.09f, 1.1f, 0.09f), stick);
        f.blades = new GameObject("Blades").transform;
        f.blades.SetParent(go.transform, false);
        f.blades.position = hub;
        f.blades.rotation = Quaternion.Euler(0f, 90f, 0f);   // تواجه الحفرة (تنفخ لليمين)
        for (int i = 0; i < 4; i++)
        {
            var blade = LevelBuilder.Visual("Blade", PrimitiveType.Cube, f.blades, Vector3.zero, new Vector3(0.05f, 0.9f, 0.55f), paper[i % 3]);
            blade.transform.localRotation = Quaternion.Euler(i * 90f, 0f, 0f);
            blade.transform.localPosition = blade.transform.localRotation * new Vector3(0f, 0.45f, 0f);
        }
        all.Add(f);
    }

    void OnDestroy() { all.Remove(this); }

    public static FanZone At(float x)
    {
        foreach (FanZone f in all) if (f != null && x > f.From - 1.5f && x < f.To) return f;
        return null;
    }

    void Update()
    {
        blades.Rotate(400f * Time.deltaTime, 0f, 0f, Space.Self);
        // خطوط ريح بيضا تمشي فوق الحفرة
        if (Time.time >= nextStreak)
        {
            nextStreak = Time.time + 0.08f;
            Material m = GameManager.Instance.Mat("Wind Streak", new Color(1.4f, 1.4f, 1.5f), 0.2f);
            var s = LevelBuilder.Visual("Wind", PrimitiveType.Cube, null, new Vector3(From, Height + Random.Range(0.6f, 3.6f), Random.Range(-1f, 1f)), new Vector3(0.8f, 0.03f, 0.03f), m, false);
            var fx = s.AddComponent<FxParticle>();
            fx.velocity = new Vector3(Random.Range(5f, 8f), 0f, 0f);
            fx.life = (To - From) / 6.5f;
        }
    }
}

// ---------- النار ----------

// حفرة نار: تشتعل وتنطفي. وهي شغالة تحرق أي رسمة فوقها وتحرق رويدا.
// رويدا توقف عند طرفها وتستنى لين يكون عندها وقت تعدي، وأنتِ ترسمين الجسر أول ما تنطفي
public class FirePit : MonoBehaviour
{
    const float On = 1.4f, Warn = 0.6f, FlameTop = 3.2f;
    float from, to, period, phase, start, nextFlame, nextBurn;
    bool wasActive, holding;
    Light glow;
    Material fireMat, fireMat2;

    public static void Create(Transform root, Vector4 spec, GameManager gm)
    {
        var go = new GameObject("Fire Pit");
        go.transform.SetParent(root, false);
        var f = go.AddComponent<FirePit>();
        f.from = spec.x; f.to = spec.y; f.period = spec.z; f.phase = spec.w;
        f.start = Time.time;

        Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
        // لهب ناعم: بقع ضو شفافة (برتقالي، وقلبها أصفر)
        var soft = NightSky.DotTexture();
        f.fireMat = NightSky.Transparent("Flame", soft, new Color(2.3f, 0.42f, 0.05f, 0.9f));
        f.fireMat2 = NightSky.Transparent("Flame Core", soft, new Color(2.5f, 0.72f, 0.08f, 0.9f));
        var lava = new Material(unlit) { name = "Lava" };
        lava.SetColor("_BaseColor", new Color(1.4f, 0.35f, 0.06f));
        float cx = (f.from + f.to) * 0.5f;
        LevelBuilder.Visual("Lava", PrimitiveType.Cube, go.transform, new Vector3(cx, -1.6f, 0f), new Vector3(f.to - f.from, 0.25f, 3.2f), lava, false);

        f.glow = new GameObject("Fire Light").AddComponent<Light>();
        f.glow.transform.SetParent(go.transform, false);
        f.glow.transform.position = new Vector3(cx, 0.5f, -1f);
        f.glow.type = LightType.Point;
        f.glow.color = new Color(1f, 0.55f, 0.2f);
        f.glow.range = 7f;
    }

    float Local => Mathf.Repeat(Time.time - start + phase, period);
    bool Active => Local < On;
    float UntilActive => Active ? 0f : period - Local;

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        bool active = Active, warning = !active && UntilActive < Warn;

        // الشكل: لهب يطلع وهي شغالة، ورفرفة صغيرة قبلها
        glow.intensity = active ? 5f + Mathf.PerlinNoise(Time.time * 8f, 0f) * 2f : warning ? 2f + Mathf.Sin(Time.time * 30f) : 0.8f;
        if (Time.time >= nextFlame && (active || warning))
        {
            nextFlame = Time.time + (active ? 0.006f : 0.05f);
            float x = Random.Range(from + 0.2f, to - 0.2f);
            var p = LevelBuilder.Visual("Flame", PrimitiveType.Quad, null, new Vector3(x, -1.4f, Random.Range(-1f, 0.5f)),
                new Vector3(1.3f, 1.9f, 1f) * Random.Range(0.6f, 1.2f), Random.value < 0.35f ? fireMat2 : fireMat, false);
            var fx = p.AddComponent<FxParticle>();
            fx.velocity = new Vector3(Random.Range(-0.3f, 0.3f), active ? Random.Range(4f, 6.5f) : Random.Range(1.2f, 2.4f), 0f);
            fx.drag = 1.2f;
            fx.life = active ? Random.Range(0.45f, 0.65f) : 0.35f;
        }
        if (active && !wasActive)
        {
            gm.Sound.PlayRumble(0.35f);
            gm.Sound.PlayPoof();
        }
        wasActive = active;

        if (active) BurnInk(gm);

        RowaidaController rowaida = gm.Rowaida;
        if (rowaida == null) return;
        Vector3 r = rowaida.transform.position;

        // تحترق إذا كانت فوق النار وهي شغالة
        if (active && gm.IsPlaying && r.x > from - 0.1f && r.x < to + 0.1f && r.y < FlameTop)
        {
            Fx.Sparkle(r, new Color(1f, 0.5f, 0.2f), 16);
            gm.KillRowaida();
            return;
        }

        // توقف عند الطرف وتستنى لين يكون عندها وقت كافي تعدي
        float cross = (to - from + 1.4f) / Mathf.Max(0.1f, rowaida.walkSpeed);
        bool atEdge = r.x > from - 1.1f && r.x < from - 0.1f;
        bool unsafeNow = active || UntilActive < cross;
        if (atEdge && unsafeNow && gm.IsPlaying)
        {
            rowaida.Held = true;
            holding = true;
        }
        else if (holding)
        {
            rowaida.Held = false;
            holding = false;
        }
    }

    void BurnInk(GameManager gm)
    {
        if (Time.time < nextBurn) return;
        nextBurn = Time.time + 0.05f;
        foreach (InkMark mark in Object.FindObjectsByType<InkMark>(FindObjectsSortMode.None))
        {
            var rend = mark.GetComponent<MeshRenderer>();
            if (rend == null) continue;
            Bounds b = rend.bounds;
            if (b.max.x > from + 0.1f && b.min.x < to - 0.1f && b.min.y < FlameTop)
            {
                Fx.Sparkle(b.center, new Color(1f, 0.45f, 0.15f), 12);
                gm.Sound.PlayPoof();
                Destroy(mark.gameObject);
            }
        }
    }
}

// ---------- البوابات ----------

// بوابة زرقا تدخل منها رويدا (أو أي شكل) وتطلع من البوابة البرتقالية
public class Portal : MonoBehaviour
{
    static Texture2D swirlTex;
    Vector3 exit;
    Transform swirlIn, swirlOut;
    readonly Dictionary<int, float> cooldown = new Dictionary<int, float>();

    public static void Create(Transform root, Vector3 entry, Vector3 exit)
    {
        if (swirlTex == null) swirlTex = NightSky.DotTexture();
        var go = new GameObject("Portal");
        go.transform.SetParent(root, false);
        go.transform.position = entry + Vector3.up * 0.95f;
        var p = go.AddComponent<Portal>();
        p.exit = exit + Vector3.up * 0.95f;
        var box = go.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(0.5f, 1.7f, 2f);

        p.swirlIn = Ring(go.transform, Vector3.zero, new Color(0.4f, 0.8f, 1f));
        var outGo = new GameObject("Portal Exit");
        outGo.transform.SetParent(root, false);
        outGo.transform.position = p.exit;
        p.swirlOut = Ring(outGo.transform, Vector3.zero, new Color(1f, 0.6f, 0.25f));
    }

    static Transform Ring(Transform parent, Vector3 local, Color c)
    {
        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "Portal Ring" };
        mat.SetColor("_BaseColor", c * 2.4f);
        var ring = new GameObject("Ring");
        ring.transform.SetParent(parent, false);
        ring.transform.localPosition = local;
        ring.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);   // محور الحلقة x: نلفها تواجه الكاميرا
        ring.transform.localScale = new Vector3(1f, 1.15f, 0.75f);
        ring.AddComponent<MeshFilter>().sharedMesh = PaperMeshes.Ring(0.85f, 0.09f, 40, 6);
        ring.AddComponent<MeshRenderer>().sharedMaterial = mat;
        // دوامة داخل الحلقة
        var swirl = NightSky.Transparent("Portal Swirl", swirlTex, new Color(c.r, c.g, c.b, 0.85f));
        var q = LevelBuilder.Visual("Swirl", PrimitiveType.Quad, parent, local + new Vector3(0f, 0f, 0.02f), new Vector3(1.5f, 1.9f, 1f), swirl, false);
        var light = q.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = c;
        light.range = 3.5f;
        light.intensity = 1.6f;
        return q.transform;
    }

    void Update()
    {
        float s = 1f + Mathf.Sin(Time.time * 4f) * 0.06f;
        swirlIn.localScale = new Vector3(1.5f * s, 1.9f * s, 1f);
        swirlOut.localScale = new Vector3(1.5f / s, 1.9f / s, 1f);
        swirlIn.Rotate(0f, 0f, 120f * Time.deltaTime);
        swirlOut.Rotate(0f, 0f, -120f * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        Rigidbody body = other.attachedRigidbody;
        if (body == null) return;
        int id = body.GetInstanceID();
        if (cooldown.TryGetValue(id, out float until) && Time.time < until) return;
        cooldown[id] = Time.time + 0.8f;

        GameManager gm = GameManager.Instance;
        var rowaida = body.GetComponent<RowaidaController>();
        Vector3 target = exit;
        if (rowaida == null) target += new Vector3(0.6f, 0f, 0f);
        Fx.Sparkle(body.position, new Color(0.5f, 0.85f, 1f), 14);
        body.position = target;
        body.transform.position = target;
        Fx.Sparkle(target, new Color(1f, 0.65f, 0.3f), 14);
        gm.Sound.PlayLaser();
        if (rowaida != null)
        {
            var cam = Camera.main.GetComponent<CameraFollow>();
            if (cam != null) cam.SetTarget(rowaida.transform, true);
        }
    }
}

// ---------- الجسور اللي تنكسر ----------

// خط أطول من المسموح: يصير لونه أحمر، ولما توقف عليه رويدا يتكسر
public class FragileInk : MonoBehaviour
{
    MeshRenderer rend;
    float crack = -1f;

    void Start()
    {
        rend = GetComponent<MeshRenderer>();
        Color c = rend.material.GetColor("_BaseColor");
        rend.material.SetColor("_BaseColor", Color.Lerp(c, new Color(0.85f, 0.25f, 0.2f), 0.6f));
        GameManager.Instance.Sound.PlaySqueak(0.6f);
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.Rowaida == null || rend == null) return;
        Bounds b = rend.bounds;
        if (crack < 0f)
        {
            if (NewFeatures.RowaidaOn(b, gm.Rowaida.transform.position)) crack = 0f;
            return;
        }
        crack += Time.deltaTime;
        transform.position += new Vector3(0f, Mathf.Sin(crack * 80f) * 0.004f, 0f);
        if (crack < 0.25f) return;

        Material m = gm.Mat("Broken Ink", new Color(0.3f, 0.22f, 0.2f), 0.4f);
        for (int i = 0; i < 12; i++)
        {
            var bit = LevelBuilder.Visual("Ink Bit", PrimitiveType.Cube, null, new Vector3(Random.Range(b.min.x, b.max.x), b.center.y, 0f), Vector3.one * Random.Range(0.08f, 0.18f), m, false);
            bit.transform.rotation = Random.rotation;
            var fx = bit.AddComponent<FxParticle>();
            fx.velocity = new Vector3(Random.Range(-1f, 1f), Random.Range(0f, 2f), 0f);
            fx.gravity = 10f;
            fx.spin = Random.insideUnitSphere * 300f;
            fx.life = 1.2f;
        }
        gm.Sound.PlayPoof();
        Destroy(gameObject);
    }
}
