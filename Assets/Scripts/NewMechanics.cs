using System.Collections.Generic;
using UnityEngine;

// أفكار المراحل الجديدة: بالونات، نار، بوابات، جسور تنكسر، وظلام بفانوس
public static class NewFeatures
{
    public static void Build(Transform root, LevelData data, GameManager gm, System.Random rng)
    {
        foreach (Vector4 f in data.firePits) FirePit.Create(root, f, gm);
        foreach (Vector4 d in data.dragons) Dragon.Create(root, d, gm);
        foreach (Vector3 p in data.pins) PinRow.Create(root, p, gm);
        foreach (Vector3 f in data.fans) FanZone.Create(root, f, gm);
        for (int i = 0; i < data.portals.Count; i++)
        {
            Vector4 p = data.portals[i];
            var portal = Portal.Create(root, new Vector3(p.x, p.y, 0f), new Vector3(p.z, p.w, 0f));
            foreach (Vector3 m in data.portalMoves)
                if ((int)m.x == i) portal.MoveExit(m.y, m.z, gm);
        }
        foreach (Vector3 g in data.gates) PortalGate.Create(root, g, gm, rng, data);
        foreach (Vector3 peg in data.pegs) StonePillar.Create(root, peg, gm);
        foreach (Vector4 b in data.birds) PaperBird.Create(root, b, gm);
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
    Transform swirlIn, swirlOut, exitRoot;
    Vector3 exitBase;
    float moveRange, movePeriod;

    // البوابة البرتقالية تمشي يمين ويسار على سكة (range متر، كل period ثانية)
    public void MoveExit(float range, float period, GameManager gm)
    {
        moveRange = range;
        movePeriod = period;
        Material rail = gm.Mat("Portal Rail", new Color(1f, 0.75f, 0.45f), 0.5f);
        for (float x = 0f; x <= range + 0.01f; x += 0.45f)
            LevelBuilder.Visual("Rail Dot", PrimitiveType.Sphere, transform.parent, exitBase + new Vector3(x, 1.25f, 0.2f), Vector3.one * 0.12f, rail, false);
    }
    readonly Dictionary<int, float> cooldown = new Dictionary<int, float>();

    public static Portal Create(Transform root, Vector3 entry, Vector3 exit)
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
        p.exitRoot = outGo.transform;
        p.exitBase = p.exit;
        return p;
    }

    static Transform Ring(Transform parent, Vector3 local, Color c)
    {
        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "Portal Ring" };
        mat.SetColor("_BaseColor", c * 1.25f);
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
        if (movePeriod > 0f)
        {
            exit = exitBase + Vector3.right * (0.5f - 0.5f * Mathf.Cos(Time.time * Mathf.PI * 2f / movePeriod)) * moveRange;
            exitRoot.position = exit;
        }
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
        if (rowaida == null)
        {
            // الشكل يطلع ويطيح على طول (عشان ينزل على الزر)
            target += new Vector3(0.6f, 0f, 0f);
            body.linearVelocity = new Vector3(0f, Mathf.Min(body.linearVelocity.y, 0f), 0f);
            if (body.GetComponent<InkMark>() != null && body.GetComponent<PortalBall>() == null) body.gameObject.AddComponent<PortalBall>();
        }
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

// ---------- التنين ----------

// تنين شخبطة على صخرة طايرة: يتنفس، يرجع راسه، وينفخ نار مايلة على الطريق.
// الخط اللي ترسمينه فوق راس رويدا يصير درع يصد النار (ويتفحم شوي شوي لين يحترق)
public class Dragon : MonoBehaviour
{
    const float Period = 4.2f, WarnTime = 1.1f, BreathTime = 1.4f, BurnHeat = 4.5f, Speed = 16f;
    float phase, start, nextFlame, nextSmoke;
    bool wasWarn, wasBreath;
    Vector3 target;
    Transform head, jaw, wings, tail;
    Light mouthLight;
    Material fireMat, fireMat2, smokeMat;
    static readonly Dictionary<InkMark, float> heat = new Dictionary<InkMark, float>();

    public static void Create(Transform root, Vector4 spec, GameManager gm)
    {
        var go = new GameObject("Dragon");
        go.transform.SetParent(root, false);
        go.transform.position = new Vector3(spec.x, spec.y, 0f);
        var d = go.AddComponent<Dragon>();
        d.target = new Vector3(spec.z, 0f, 0f);
        d.phase = spec.w;
        d.start = Time.time;
        heat.Clear();
        d.Build(gm);
    }

    Transform Part(string name, PrimitiveType type, Transform parent, Vector3 pos, Vector3 scale, Material mat, float rotZ = 0f)
    {
        var t = LevelBuilder.Visual(name, type, parent, pos, scale, mat).transform;
        t.localRotation = Quaternion.Euler(0f, 0f, rotZ);
        return t;
    }

    void Build(GameManager gm)
    {
        Material rock = gm.Mat("Dragon Rock", new Color(0.36f, 0.27f, 0.25f), 0.15f);
        Material rockDark = gm.Mat("Dragon Rock Dark", new Color(0.24f, 0.17f, 0.16f), 0.1f);
        Material skin = gm.Mat("Dragon Skin", new Color(0.93f, 0.3f, 0.22f), 0.35f);
        Material belly = gm.Mat("Dragon Belly", new Color(1f, 0.82f, 0.4f), 0.3f);
        Material spike = gm.Mat("Dragon Spike", new Color(0.55f, 0.12f, 0.15f), 0.3f);
        Material wing = gm.Mat("Dragon Wing", new Color(0.75f, 0.2f, 0.32f), 0.25f);
        Material horn = gm.Mat("Dragon Horn", new Color(0.98f, 0.92f, 0.75f), 0.5f);
        Material white = gm.Mat("Monster Eye", Color.white, 0.7f);
        Material black = gm.Mat("Monster Pupil", new Color(0.05f, 0.04f, 0.06f), 0.9f);
        Material mouthIn = gm.Mat("Dragon Mouth", new Color(0.35f, 0.05f, 0.08f), 0.4f);

        // الصخرة الطايرة (مع حصى تحتها)
        Part("Rock", PrimitiveType.Sphere, transform, new Vector3(0.3f, -0.45f, 0.3f), new Vector3(3.4f, 1.1f, 2f), rock);
        Part("Rock Bottom", PrimitiveType.Sphere, transform, new Vector3(0.4f, -0.95f, 0.3f), new Vector3(2f, 1f, 1.5f), rockDark);
        Part("Rock Tip", PrimitiveType.Sphere, transform, new Vector3(0.5f, -1.45f, 0.3f), new Vector3(0.8f, 0.7f, 0.7f), rockDark);
        for (int i = 0; i < 3; i++)
            Part("Pebble", PrimitiveType.Sphere, transform, new Vector3(-0.5f + i * 0.75f, -1.55f - i * 0.15f, 0.3f), Vector3.one * (0.3f - i * 0.06f), rock).gameObject.AddComponent<Bob>();

        // الجسم والذيل والرجول
        var body = new GameObject("Body").transform;
        body.SetParent(transform, false);
        Part("Torso", PrimitiveType.Sphere, body, new Vector3(0.45f, 0.6f, 0f), new Vector3(1.6f, 1.1f, 1f), skin);
        Part("Belly", PrimitiveType.Sphere, body, new Vector3(0.25f, 0.45f, -0.3f), new Vector3(1.05f, 0.75f, 0.5f), belly);
        for (int s = 0; s < 2; s++)
            Part("Leg", PrimitiveType.Capsule, body, new Vector3(-0.05f + s * 0.85f, 0.15f, -0.25f + s * 0.1f), new Vector3(0.32f, 0.3f, 0.32f), skin);
        for (int i = 0; i < 4; i++)
            Part("Back Spike", PrimitiveType.Cube, body, new Vector3(0.05f + i * 0.33f, 1.15f - Mathf.Abs(i - 1.2f) * 0.07f, 0f), Vector3.one * 0.2f, spike, 45f);
        tail = new GameObject("Tail").transform;
        tail.SetParent(body, false);
        tail.localPosition = new Vector3(1.15f, 0.5f, 0f);
        Part("Tail 1", PrimitiveType.Capsule, tail, new Vector3(0.35f, -0.05f, 0f), new Vector3(0.3f, 0.45f, 0.3f), skin, 70f);
        Part("Tail 2", PrimitiveType.Capsule, tail, new Vector3(0.8f, 0.12f, 0f), new Vector3(0.2f, 0.35f, 0.2f), skin, 120f);
        Part("Tail Tip", PrimitiveType.Cube, tail, new Vector3(1.07f, 0.35f, 0f), new Vector3(0.28f, 0.28f, 0.08f), spike, 45f);

        // الجناحات تحت الجسم ورا (ترفرف)
        wings = new GameObject("Wings").transform;
        wings.SetParent(body, false);
        wings.localPosition = new Vector3(0.55f, 0.95f, 0.35f);
        Part("Wing", PrimitiveType.Sphere, wings, new Vector3(0.2f, 0.55f, 0f), new Vector3(0.7f, 1.3f, 0.08f), wing, -25f);
        Part("Wing Bone", PrimitiveType.Capsule, wings, new Vector3(0.05f, 0.65f, -0.05f), new Vector3(0.09f, 0.6f, 0.09f), spike, -25f);

        // الرقبة والراس
        Part("Neck", PrimitiveType.Capsule, body, new Vector3(-0.35f, 1.05f, 0f), new Vector3(0.48f, 0.6f, 0.48f), skin, 35f);
        head = new GameObject("Head").transform;
        head.SetParent(transform, false);
        head.localPosition = new Vector3(-0.7f, 1.55f, 0f);
        Part("Skull", PrimitiveType.Sphere, head, new Vector3(0f, 0.05f, 0f), new Vector3(0.85f, 0.7f, 0.7f), skin);
        Part("Snout", PrimitiveType.Capsule, head, new Vector3(-0.5f, 0.08f, 0f), new Vector3(0.42f, 0.38f, 0.45f), skin, 90f);
        for (int s = -1; s <= 1; s += 2)
            Part("Nostril", PrimitiveType.Sphere, head, new Vector3(-0.82f, 0.18f, s * 0.12f - 0.08f), Vector3.one * 0.08f, black);
        Part("Eye", PrimitiveType.Sphere, head, new Vector3(-0.15f, 0.22f, -0.3f), new Vector3(0.28f, 0.3f, 0.12f), white);
        Part("Pupil", PrimitiveType.Sphere, head, new Vector3(-0.2f, 0.2f, -0.36f), new Vector3(0.12f, 0.17f, 0.05f), black);
        Part("Brow", PrimitiveType.Cube, head, new Vector3(-0.15f, 0.4f, -0.3f), new Vector3(0.32f, 0.07f, 0.06f), spike, -15f);
        for (int s = 0; s < 2; s++)
            Part("Horn", PrimitiveType.Capsule, head, new Vector3(0.2f + s * 0.15f, 0.45f, 0.1f - s * 0.2f), new Vector3(0.12f, 0.25f, 0.12f), horn, -40f);
        jaw = new GameObject("Jaw").transform;
        jaw.SetParent(head, false);
        jaw.localPosition = new Vector3(-0.05f, -0.12f, 0f);
        Part("Jaw Inside", PrimitiveType.Sphere, jaw, new Vector3(-0.4f, 0.02f, -0.05f), new Vector3(0.6f, 0.15f, 0.35f), mouthIn);
        Part("Jaw Skin", PrimitiveType.Capsule, jaw, new Vector3(-0.42f, -0.08f, 0f), new Vector3(0.24f, 0.36f, 0.36f), belly, 90f);
        for (int t = 0; t < 3; t++)
            Part("Tooth", PrimitiveType.Cube, head, new Vector3(-0.72f + t * 0.17f, -0.1f, -0.15f), Vector3.one * 0.07f, white, 45f);

        // ضو بالفم
        mouthLight = new GameObject("Mouth Light").AddComponent<Light>();
        mouthLight.transform.SetParent(head, false);
        mouthLight.transform.localPosition = new Vector3(-0.9f, 0f, -0.6f);
        mouthLight.type = LightType.Point;
        mouthLight.color = new Color(1f, 0.55f, 0.18f);
        mouthLight.range = 6f;
        mouthLight.intensity = 0f;

        var soft = NightSky.DotTexture();
        fireMat = NightSky.Transparent("Dragon Flame", soft, new Color(2.4f, 0.45f, 0.06f, 0.9f));
        fireMat2 = NightSky.Transparent("Dragon Flame Core", soft, new Color(2.6f, 1.6f, 0.4f, 0.95f));
        smokeMat = NightSky.Transparent("Dragon Smoke", soft, new Color(0.35f, 0.32f, 0.33f, 0.55f));
    }

    float Local => Mathf.Repeat(Time.time - start + phase, Period);
    // بداية الدورة: ينفخ. آخر الدورة: يحذر (يرجع راسه ويطلع دخان)
    bool Breathing => Local < BreathTime;
    bool Warning => Local > Period - WarnTime;

    Vector3 Mouth => head.TransformPoint(new Vector3(-0.95f, 0f, 0f));
    // وين يطالع: رويدا (بين حدود)، ووقت النفخ يلحقها بسرعة قريبة من مشيها
    Vector3 aimPoint;
    Vector3 Aim => (aimPoint - head.position).normalized;

    void Track(GameManager gm, bool breathing)
    {
        float lo = target.x - 3f, hi = transform.position.x - 2.5f;
        float want = gm.Rowaida != null ? Mathf.Clamp(gm.Rowaida.transform.position.x + 0.3f, lo, hi) : target.x;
        if (aimPoint == Vector3.zero) aimPoint = new Vector3(lo, 0f, 0f);
        aimPoint.x = breathing ? Mathf.MoveTowards(aimPoint.x, want, 3.1f * Time.deltaTime) : Mathf.Lerp(aimPoint.x, want, Time.deltaTime * 4f);
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        bool breathing = Breathing, warning = Warning;
        Track(gm, breathing);
        if (gm.Rowaida != null)
        {
            float dx = gm.Rowaida.transform.position.x - transform.position.x;
            if (dx > -14f && dx < 3f) CameraFollow.PullRequest = Mathf.Max(CameraFollow.PullRequest, 1f);
        }
        float t = Time.time;

        // الحركة: يتنفس، الجناح يرفرف، الذيل يتمايل
        wings.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * (warning ? 9f : 2.5f)) * 18f);
        tail.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.7f) * 12f);

        float aimAngle = Mathf.Atan2(Aim.y, Aim.x) * Mathf.Rad2Deg - 180f;
        float headAngle, jawOpen;
        if (breathing) { headAngle = aimAngle + Mathf.Sin(t * 20f) * 2f; jawOpen = 32f; }
        else if (warning)
        {
            float k = Mathf.InverseLerp(Period - WarnTime, Period, Local);
            headAngle = Mathf.Lerp(-10f, -38f, k);   // يرجع راسه لورا ويشهق
            jawOpen = 6f + k * 10f;
        }
        else { headAngle = -8f + Mathf.Sin(t * 1.3f) * 4f; jawOpen = 0f; }
        head.localRotation = Quaternion.Slerp(head.localRotation, Quaternion.Euler(0f, 0f, headAngle), Time.deltaTime * (breathing ? 18f : 6f));
        jaw.localRotation = Quaternion.Slerp(jaw.localRotation, Quaternion.Euler(0f, 0f, jawOpen), Time.deltaTime * 12f);
        mouthLight.intensity = breathing ? 6f + Mathf.PerlinNoise(t * 9f, 1f) * 3f : warning ? 1.5f + Mathf.Sin(t * 25f) : 0f;

        if (warning && !wasWarn) gm.Sound.PlayRumble(0.45f);
        if (breathing && !wasBreath) { gm.Sound.PlayPoof(); gm.Sound.PlayRumble(0.6f); }
        wasWarn = warning; wasBreath = breathing;

        // دخان من الخشم وهو يحذر
        if (warning && t >= nextSmoke)
        {
            nextSmoke = t + 0.09f;
            var s = LevelBuilder.Visual("Smoke", PrimitiveType.Quad, null, head.TransformPoint(new Vector3(-0.85f, 0.2f, -0.1f)), Vector3.one * Random.Range(0.3f, 0.5f), smokeMat, false);
            var fx = s.AddComponent<FxParticle>();
            fx.velocity = new Vector3(Random.Range(-0.6f, 0.2f), Random.Range(0.8f, 1.4f), 0f);
            fx.grow = 1.8f; fx.drag = 1f; fx.life = 0.8f;
        }

        if (!breathing) return;

        // النفخة: توقف عند أول درع (خط حبر) أو الأرض
        Vector3 from = Mouth, dir = (aimPoint - from).normalized;
        float reach = 14f;
        InkMark shield = null;
        foreach (RaycastHit h in Physics.SphereCastAll(from, 0.3f, dir, reach, ~0, QueryTriggerInteraction.Ignore))
        {
            var mark = h.collider.GetComponentInParent<InkMark>();
            if (h.collider.GetComponentInParent<RowaidaController>() != null) continue;
            if (mark == null && h.collider.attachedRigidbody != null) continue;   // أشياء تتحرك (بالونات، وحوش) ما توقفها
            if (h.distance <= 0f || h.distance >= reach) continue;
            reach = h.distance;
            shield = mark;
        }
        Vector3 end = from + dir * reach;

        if (t >= nextFlame)
        {
            nextFlame = t + 0.01f;
            for (int i = 0; i < 3; i++)
            {
                bool core = Random.value < 0.35f;
                var p = LevelBuilder.Visual("Dragon Flame", PrimitiveType.Quad, null, from + (Vector3)Random.insideUnitCircle * 0.1f + new Vector3(0f, 0f, -0.3f),
                    Vector3.one * (core ? 0.75f : 1.05f), core ? fireMat2 : fireMat, false);
                var fx = p.AddComponent<FxParticle>();
                float speed = Speed * Random.Range(0.85f, 1.1f);
                fx.velocity = (dir + (Vector3)Random.insideUnitCircle * 0.08f) * speed;
                fx.grow = core ? 1.4f : 2.2f;
                fx.life = Mathf.Max(0.05f, reach / speed * 1.1f);
            }
            // شرار وين ما توقف
            // وين ما توقف: لهب يتطاير لفوق
            var splash = LevelBuilder.Visual("Flame Splash", PrimitiveType.Quad, null, end + new Vector3(Random.Range(-0.4f, 0.4f), 0.1f, -0.4f), Vector3.one * Random.Range(0.6f, 1f), fireMat, false);
            var sfx = splash.AddComponent<FxParticle>();
            sfx.velocity = new Vector3(Random.Range(-1.5f, 1.5f), Random.Range(1.5f, 3f), 0f);
            sfx.grow = 1f; sfx.drag = 2f; sfx.life = 0.35f;
            if (Random.value < 0.4f) Fx.Sparkle(end, new Color(1f, 0.55f, 0.15f), 2);
        }

        // الدرع يسخن ويتفحم، وبعدين يحترق
        if (shield != null)
        {
            heat.TryGetValue(shield, out float h0);
            float h1 = h0 + Time.deltaTime;
            heat[shield] = h1;
            var rend = shield.GetComponent<MeshRenderer>();
            if (rend != null)
            {
                rend.material.color = Color.Lerp(rend.material.color, new Color(0.12f, 0.06f, 0.05f), Time.deltaTime * 0.6f);
            }
            if (h1 >= BurnHeat)
            {
                Fx.Sparkle(end, new Color(1f, 0.45f, 0.15f), 20);
                gm.Sound.PlayPoof();
                heat.Remove(shield);
                Destroy(shield.gameObject);
            }
        }

        // رويدا بطريق النار؟
        RowaidaController rowaida = gm.Rowaida;
        if (rowaida == null || !gm.IsPlaying || Local < 0.12f) return;
        Vector3 r = rowaida.transform.position + Vector3.up * 0.3f;
        float along = Mathf.Clamp(Vector3.Dot(r - from, dir), 0f, reach);
        if ((from + dir * along - r).magnitude < 0.7f)
        {
            Fx.Sparkle(r, new Color(1f, 0.5f, 0.2f), 16);
            gm.KillRowaida();
        }
    }
}

// حصى تطفو تحت الصخرة
public class Bob : MonoBehaviour
{
    Vector3 home;
    float seed;
    void Start() { home = transform.localPosition; seed = Random.value * 10f; }
    void Update() => transform.localPosition = home + Vector3.up * Mathf.Sin(Time.time * 1.6f + seed) * 0.12f;
}

// ---------- البوابة المقفلة والزر ----------

// باب عالي يسد الطريق، وزر فوق رف وراه. أي شكل يطيح على الزر (من البوابة) يفتح الباب
public class PortalGate : MonoBehaviour
{
    float gateX, buttonX, buttonTop, openT = -1f;
    Transform door, cap;
    bool holding;

    public bool Open => openT >= 0f;

    public static void Create(Transform root, Vector3 spec, GameManager gm, System.Random rng, LevelData data)
    {
        var go = new GameObject("Portal Gate");
        go.transform.SetParent(root, false);
        var g = go.AddComponent<PortalGate>();
        g.gateX = spec.x; g.buttonX = spec.y; g.buttonTop = spec.z;

        Material wood = gm.Mat("Gate Wood", new Color(0.55f, 0.36f, 0.62f), 0.3f);
        Material band = gm.Mat("Gate Band", new Color(0.98f, 0.55f, 0.72f), 0.4f);
        Material metal = gm.Mat("Gate Lock", new Color(0.95f, 0.8f, 0.35f), 0.8f, 0.6f);
        Material red = gm.Mat("Button Cap", new Color(1f, 0.35f, 0.55f), 0.6f);
        Material baseMat = gm.Mat("Button Base", new Color(0.3f, 0.25f, 0.35f), 0.4f);

        // الباب: له جسم يصطدم، وينزل تحت الأرض لما ينفتح
        g.door = new GameObject("Gate Door").transform;
        g.door.SetParent(go.transform, false);
        g.door.position = new Vector3(g.gateX, 2.6f, 0f);
        var col = g.door.gameObject.AddComponent<BoxCollider>();
        col.size = new Vector3(0.6f, 5.2f, 2.4f);
        LevelBuilder.Visual("Door", PrimitiveType.Cube, g.door, Vector3.zero, new Vector3(0.6f, 5.2f, 2.4f), wood);
        for (int i = 0; i < 3; i++)
            LevelBuilder.Visual("Band", PrimitiveType.Cube, g.door, new Vector3(0f, -1.7f + i * 1.7f, -0.05f), new Vector3(0.66f, 0.22f, 2.45f), band);
        var lockT = LevelBuilder.Visual("Lock", PrimitiveType.Cylinder, g.door, new Vector3(0f, 0.6f, -1.25f), new Vector3(0.55f, 0.06f, 0.55f), metal).transform;
        lockT.localRotation = Quaternion.Euler(90f, 0f, 0f);
        LevelBuilder.Visual("Keyhole", PrimitiveType.Cube, g.door, new Vector3(0f, 0.52f, -1.32f), new Vector3(0.08f, 0.22f, 0.02f), baseMat, false);

        // الرف والزر فوقه
        LevelBuilder.CreatePaperBlock(root, new Block(new Vector3(g.buttonX, g.buttonTop - 0.3f, 0f), new Vector3(2.4f, 0.6f, 2.2f)), gm, rng, data, false);
        var bt = LevelBuilder.Visual("Button Base", PrimitiveType.Cylinder, go.transform, new Vector3(g.buttonX, g.buttonTop + 0.06f, 0f), new Vector3(1.1f, 0.06f, 1.1f), baseMat);
        g.cap = LevelBuilder.Visual("Button Cap", PrimitiveType.Cylinder, go.transform, new Vector3(g.buttonX, g.buttonTop + 0.2f, 0f), new Vector3(0.8f, 0.1f, 0.8f), red).transform;
        // سهم صغير يأشر على الزر
        var arrow = LevelBuilder.Visual("Arrow", PrimitiveType.Cube, go.transform, new Vector3(g.buttonX, g.buttonTop + 1.2f, -0.3f), new Vector3(0.35f, 0.35f, 0.05f), red, false);
        arrow.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        arrow.AddComponent<Bob>();
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        if (!Open)
        {
            // شي من الحبر (مو رويدا) نزل على الزر؟
            Vector3 c = new Vector3(buttonX, buttonTop + 0.4f, 0f);
            foreach (Collider hit in Physics.OverlapBox(c, new Vector3(0.6f, 0.35f, 1f)))
            {
                if (hit.GetComponentInParent<InkMark>() == null) continue;
                openT = 0f;
                cap.localPosition += Vector3.down * 0.1f;
                gm.Sound.PlayBoing();
                gm.Sound.PlayRumble(0.4f);
                Fx.Sparkle(c, new Color(1f, 0.5f, 0.75f), 18);
                var keep = hit.GetComponentInParent<PortalBall>();
                if (keep != null) keep.Keep();
                break;
            }
        }
        else if (openT < 1f)
        {
            openT += Time.deltaTime / 0.9f;
            door.position = new Vector3(gateX, Mathf.Lerp(2.6f, -3.2f, Mathf.SmoothStep(0f, 1f, openT)), 0f);
            if (Random.value < 0.4f) Fx.Dust(new Vector3(gateX, 0.1f, 0f), 0.3f);
        }

        // رويدا تستنى عند الباب المقفل
        RowaidaController rowaida = gm.Rowaida;
        if (rowaida == null) return;
        float x = rowaida.transform.position.x;
        bool wait = !Open && x > gateX - 1.2f && x < gateX - 0.2f && gm.IsPlaying;
        if (!Open && x > gateX - 14f && x < gateX) CameraFollow.PullRequest = Mathf.Max(CameraFollow.PullRequest, 1f);
        if (wait) { rowaida.Held = true; rowaida.CarryX = 0f; holding = true; }
        else if (holding) { rowaida.Held = false; holding = false; }
    }
}

// شكل طلع من بوابة: إذا ما نزل على زر، يختفي بعد شوي (عشان ما يسد الطريق)
public class PortalBall : MonoBehaviour
{
    float dieAt;
    bool kept;

    void Start() => dieAt = Time.time + 2.2f;
    public void Keep() => kept = true;

    void Update()
    {
        if (kept || Time.time < dieAt) return;
        Fx.Sparkle(transform.position, new Color(0.7f, 0.6f, 1f), 14);
        if (GameManager.Instance != null) GameManager.Instance.Sound.PlayPoof();
        Destroy(gameObject);
    }
}

// ---------- الوادي: أعمدة حجر وطير ورق ----------

// عمود حجر طالع من قاع الوادي (حجارة فوق بعض، وفوقه عشب)
public static class StonePillar
{
    public static void Create(Transform root, Vector3 spec, GameManager gm)
    {
        var go = new GameObject("Stone Pillar");
        go.transform.SetParent(root, false);
        float x = spec.x, top = spec.y, w = spec.z, bottom = top - 14f;
        go.transform.position = new Vector3(x, (top + bottom) * 0.5f, 0f);
        var col = go.AddComponent<BoxCollider>();
        col.size = new Vector3(w, top - bottom, 2f);

        Material[] stone =
        {
            gm.Mat("Canyon Stone A", new Color(0.78f, 0.52f, 0.36f), 0.15f),
            gm.Mat("Canyon Stone B", new Color(0.66f, 0.42f, 0.3f), 0.12f),
            gm.Mat("Canyon Stone C", new Color(0.86f, 0.62f, 0.44f), 0.15f),
        };
        Material moss = gm.Mat("Canyon Moss", new Color(0.45f, 0.68f, 0.32f), 0.25f);
        Material grass = gm.Mat("Canyon Grass", new Color(0.35f, 0.58f, 0.25f), 0.2f);
        var rng = new System.Random((int)(x * 10f));
        float y = top - 0.4f;
        int k = 0;
        while (y > bottom)
        {
            float h = 0.7f + (float)rng.NextDouble() * 0.4f;
            float ww = w * (1.02f + (float)rng.NextDouble() * 0.18f) * (1f + k * 0.03f);
            // أول حجر مسطح (عشان السطح)، والباقي حجارة مدورة
            bool slab = k == 0;
            var s = LevelBuilder.Visual("Stone", slab ? PrimitiveType.Cube : PrimitiveType.Sphere, go.transform, Vector3.zero,
                slab ? new Vector3(ww, h, 1.9f) : new Vector3(ww * 1.3f, h * 1.35f, 2.2f + (float)rng.NextDouble() * 0.3f), stone[k % 3]);
            s.transform.position = new Vector3(x + ((float)rng.NextDouble() - 0.5f) * 0.12f, y, 0f);
            s.transform.rotation = Quaternion.Euler(0f, ((float)rng.NextDouble() - 0.5f) * 14f, ((float)rng.NextDouble() - 0.5f) * 6f);
            y -= h * 0.92f;
            k++;
        }
        // غطا عشب فوق
        var cap = LevelBuilder.Visual("Moss", PrimitiveType.Sphere, go.transform, Vector3.zero, new Vector3(w * 1.15f, 0.22f, 2.1f), moss);
        cap.transform.position = new Vector3(x, top - 0.02f, 0f);
        for (int i = 0; i < 4; i++)
        {
            var blade = LevelBuilder.Visual("Grass", PrimitiveType.Cube, go.transform, Vector3.zero, new Vector3(0.05f, 0.28f, 0.05f), grass, false);
            blade.transform.position = new Vector3(x - w * 0.4f + i * w * 0.27f, top + 0.15f, -0.85f);
            blade.transform.rotation = Quaternion.Euler(0f, 0f, (i % 2 == 0 ? 1 : -1) * 15f);
        }
    }
}

// طير ورق (أوريغامي) كبير: يصيح، وبعدين ينقض على الحفرة ويخطف أي خط بطريقه.
// رويدا تستنى عند الطرف لين يعدي، وأنتِ ترسمين الجسور وراه
public class PaperBird : MonoBehaviour
{
    const float Period = 8f, Warn = 1.5f, Swoop = 1.9f;
    float from, to, low, phase, start, nextCry;
    Transform model, wingL, wingR, alert;
    readonly List<GameObject> carried = new List<GameObject>();
    bool holding, wasSwoop;

    public static void Create(Transform root, Vector4 spec, GameManager gm)
    {
        var go = new GameObject("Paper Bird");
        go.transform.SetParent(root, false);
        var b = go.AddComponent<PaperBird>();
        b.from = spec.x; b.to = spec.y; b.low = spec.z; b.phase = spec.w;
        b.start = Time.time;
        b.Build(gm);
        go.transform.position = b.Park;
    }

    void Build(GameManager gm)
    {
        Material paper = gm.Mat("Bird Paper", new Color(0.97f, 0.95f, 0.9f), 0.2f);
        Material fold = gm.Mat("Bird Fold", new Color(0.83f, 0.8f, 0.76f), 0.2f);
        Material beak = gm.Mat("Bird Beak", new Color(1f, 0.62f, 0.25f), 0.3f);
        Material black = gm.Mat("Monster Pupil", new Color(0.05f, 0.04f, 0.06f), 0.9f);
        Material red = gm.Mat("Bird Alert", new Color(1f, 0.25f, 0.25f), 0.4f);

        model = new GameObject("Model").transform;
        model.SetParent(transform, false);
        // الجسم: معيّن ورق
        var body = LevelBuilder.Visual("Body", PrimitiveType.Cube, model, Vector3.zero, new Vector3(1.1f, 1.1f, 0.5f), paper).transform;
        body.localRotation = Quaternion.Euler(0f, 0f, 45f);
        body.localScale = new Vector3(1.3f, 0.7f, 0.5f);
        LevelBuilder.Visual("Fold", PrimitiveType.Cube, model, new Vector3(0f, -0.05f, -0.26f), new Vector3(1.2f, 0.04f, 0.02f), fold, false);
        // الرقبة والراس (لقدام فوق) والذيل (لورا فوق)
        var neck = LevelBuilder.Visual("Neck", PrimitiveType.Cube, model, new Vector3(0.85f, 0.45f, 0f), new Vector3(1.1f, 0.18f, 0.2f), paper).transform;
        neck.localRotation = Quaternion.Euler(0f, 0f, 40f);
        var head = LevelBuilder.Visual("Head", PrimitiveType.Cube, model, new Vector3(1.38f, 0.82f, 0f), new Vector3(0.45f, 0.22f, 0.24f), paper).transform;
        head.localRotation = Quaternion.Euler(0f, 0f, -20f);
        var bk = LevelBuilder.Visual("Beak", PrimitiveType.Cube, model, new Vector3(1.7f, 0.7f, 0f), new Vector3(0.38f, 0.1f, 0.12f), beak).transform;
        bk.localRotation = Quaternion.Euler(0f, 0f, -25f);
        LevelBuilder.Visual("Eye", PrimitiveType.Sphere, model, new Vector3(1.4f, 0.86f, -0.13f), Vector3.one * 0.09f, black, false);
        var tail = LevelBuilder.Visual("Tail", PrimitiveType.Cube, model, new Vector3(-0.95f, 0.4f, 0f), new Vector3(1.1f, 0.16f, 0.2f), paper).transform;
        tail.localRotation = Quaternion.Euler(0f, 0f, -35f);
        // الجناحات: مثلثات ورق ترفرف (واحد قدام، وواحد ورا)
        wingL = Wing(model, paper, fold, -0.35f);
        wingR = Wing(model, fold, paper, 0.35f);
        // مخالب
        for (int i = 0; i < 2; i++)
        {
            var claw = LevelBuilder.Visual("Claw", PrimitiveType.Cube, model, new Vector3(-0.05f + i * 0.25f, -0.55f, 0f), new Vector3(0.06f, 0.4f, 0.06f), beak, false).transform;
            claw.localRotation = Quaternion.Euler(0f, 0f, 15f);
        }
        model.localScale = Vector3.one * 1.25f;

        // علامة تعجب حمرا فوق الحفرة قبل لا ينقض
        alert = new GameObject("Bird Alert").transform;
        alert.SetParent(transform.parent, false);
        alert.position = new Vector3((from + to) * 0.5f, 3.4f, -0.5f);
        LevelBuilder.Visual("Bar", PrimitiveType.Cube, alert, new Vector3(0f, 0.25f, 0f), new Vector3(0.18f, 0.6f, 0.1f), red, false);
        LevelBuilder.Visual("Dot", PrimitiveType.Sphere, alert, new Vector3(0f, -0.25f, 0f), Vector3.one * 0.2f, red, false);
        alert.gameObject.SetActive(false);
    }

    static Transform Wing(Transform parent, Material a, Material b, float z)
    {
        var pivot = new GameObject("Wing").transform;
        pivot.SetParent(parent, false);
        pivot.localPosition = new Vector3(0f, 0.2f, z);
        var w = LevelBuilder.Visual("Wing Sheet", PrimitiveType.Cube, pivot, new Vector3(0f, 0f, 0f), new Vector3(0.9f, 0.05f, 1.6f), a).transform;
        w.localPosition = new Vector3(0f, 0f, Mathf.Sign(z) * 0.8f);
        w.localRotation = Quaternion.Euler(0f, 35f, 0f);
        LevelBuilder.Visual("Wing Fold", PrimitiveType.Cube, w, new Vector3(0f, 0.6f, 0f), new Vector3(0.05f, 1f, 0.9f), b, false);
        return pivot;
    }

    Vector3 Park => new Vector3(from - 3f, 9.5f, 2f);
    float Local => Mathf.Repeat(Time.time - start + phase, Period);
    bool Swooping => Local < Swoop;
    float UntilSwoop => Swooping ? 0f : Period - Local;

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        bool swooping = Swooping;
        float until = UntilSwoop;

        // رفرفة
        float flap = Mathf.Sin(Time.time * (swooping ? 7f : 4f)) * (swooping ? 25f : 40f);
        wingL.localRotation = Quaternion.Euler(-flap, 0f, 0f);
        wingR.localRotation = Quaternion.Euler(flap, 0f, 0f);

        // تحذير: صيحة وعلامة تعجب
        bool warn = !swooping && until < Warn;
        alert.gameObject.SetActive(warn && Mathf.Repeat(Time.time * 4f, 1f) < 0.65f);
        if (warn && Time.time >= nextCry)
        {
            nextCry = Time.time + 0.45f;
            gm.Sound.PlaySqueak(Random.Range(1.9f, 2.3f));
        }

        if (swooping)
        {
            // ينزل من فوق اليسار، يمر تحت مستوى الجسور، ويطلع فوق اليمين
            float s = Local / Swoop;
            float x = Mathf.Lerp(from - 2.5f, to + 2.5f, s);
            float d = 2f * s - 1f;
            float y = low + (9f - low) * d * d;
            float slope = (9f - low) * 2f * d * 2f / (to - from + 5f);
            transform.position = new Vector3(x, y, -1.4f);   // قدام الأعمدة عشان يبان وهو ينقض
            model.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan(slope) * Mathf.Rad2Deg);
            if (!wasSwoop) gm.Sound.PlayLaser();
            Snatch(gm);
        }
        else
        {
            if (wasSwoop) DropCarried();
            // يحوم بعيد فوق
            transform.position = Vector3.Lerp(transform.position, Park + new Vector3(Mathf.Sin(Time.time * 0.6f) * 2f, Mathf.Sin(Time.time * 1.1f) * 0.5f, 0f), Time.deltaTime * 1.5f);
            model.localRotation = Quaternion.Slerp(model.localRotation, Quaternion.identity, Time.deltaTime * 3f);
        }
        wasSwoop = swooping;

        // رويدا تستنى عند الطرف لين يكون عندها وقت تعدي قبل الطير الجاي
        RowaidaController rowaida = gm.Rowaida;
        if (rowaida == null) return;
        Vector3 r = rowaida.transform.position;
        float cross = (to - from + 1.4f) / Mathf.Max(0.1f, rowaida.walkSpeed);
        bool atEdge = r.x > from - 1.1f && r.x < from - 0.1f && r.y > -0.5f;
        if (atEdge && until < cross && gm.IsPlaying) { rowaida.Held = true; rowaida.CarryX = 0f; holding = true; }
        else if (holding) { rowaida.Held = false; holding = false; }
    }

    // يخطف الخطوط اللي قريبة منه (فوق الحفرة)
    void Snatch(GameManager gm)
    {
        Vector3 p = transform.position;
        p.z = 0f;
        foreach (InkMark mark in Object.FindObjectsByType<InkMark>(FindObjectsSortMode.None))
        {
            if (carried.Contains(mark.gameObject)) continue;
            var rend = mark.GetComponent<MeshRenderer>();
            if (rend == null) continue;
            Bounds b = rend.bounds;
            if (b.center.x < from - 0.5f || b.center.x > to + 0.5f) continue;
            if (b.SqrDistance(p) > 1.6f * 1.6f) continue;
            foreach (Collider c in mark.GetComponentsInChildren<Collider>()) c.enabled = false;
            var body = mark.GetComponent<Rigidbody>();
            if (body != null) body.isKinematic = true;
            mark.transform.SetParent(transform, true);
            carried.Add(mark.gameObject);
            Fx.Sparkle(b.center, new Color(1f, 1f, 0.9f), 10);
            gm.Sound.PlayChomp();
        }
    }

    void DropCarried()
    {
        foreach (GameObject g in carried)
            if (g != null)
            {
                Fx.Sparkle(g.transform.position, new Color(1f, 1f, 0.9f), 8);
                Destroy(g);
            }
        carried.Clear();
    }
}
