using System.Collections.Generic;
using UnityEngine;

// أفكار المراحل الجديدة: بالونات، نار، بوابات، جسور تنكسر، وظلام بفانوس
public static class NewFeatures
{
    public static void Build(Transform root, LevelData data, GameManager gm, System.Random rng)
    {
        foreach (Vector4 f in data.firePits) FirePit.Create(root, f, gm);
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

// شكل مغلق يصير بالونة: يطفو مكانه، ولما توقف عليه رويدا يطلع فيها لين يوصل للأرض العالية اللي جنبه
public class BalloonLift : MonoBehaviour
{
    const float RiseSpeed = 1.7f;
    static readonly Color[] Colors = { new Color(1f, 0.4f, 0.45f), new Color(1f, 0.75f, 0.25f), new Color(0.4f, 0.7f, 1f), new Color(0.55f, 0.85f, 0.45f), new Color(0.8f, 0.55f, 1f) };
    static int next;

    Rigidbody rb;
    MeshRenderer rend;
    float targetTop, doneTime;
    bool rising, done;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rend = GetComponent<MeshRenderer>();
        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezePositionX;

        // لون بالونة وخيط نازل منها
        Color c = Colors[next++ % Colors.Length];
        rend.material.SetColor("_BaseColor", c);
        rend.material.SetFloat("_Smoothness", 0.75f);
        Bounds b = rend.bounds;
        // جسم البالونة: نعبي الشكل بكورة منفوخة بنفس اللون (أفتح شوي)
        var fill = LevelBuilder.Visual("Balloon Body", PrimitiveType.Sphere, transform, Vector3.zero,
            new Vector3(b.size.x * 0.92f, b.size.y * 0.92f, 0.5f), GameManager.Instance.Mat("Balloon Fill " + ColorUtility.ToHtmlStringRGB(c), Color.Lerp(c, Color.white, 0.25f), 0.7f), false);
        fill.transform.position = b.center + new Vector3(0f, 0f, 0.05f);
        var str = LevelBuilder.Visual("Balloon String", PrimitiveType.Cylinder, transform, Vector3.zero, new Vector3(0.025f, 0.45f, 0.025f), GameManager.Instance.Mat("Balloon String", new Color(0.92f, 0.92f, 0.92f), 0.2f), false);
        str.transform.position = new Vector3(b.center.x, b.min.y - 0.45f, b.center.z);
        Fx.Sparkle(b.center, c, 10);
        GameManager.Instance.Sound.PlayBoing();
    }

    void FixedUpdate()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || rb == null) return;
        Bounds b = rend.bounds;
        RowaidaController rowaida = gm.Rowaida;
        // لازم تكون فعلًا فوقها (مو واقفة على طرف الصفحة جنبها)
        bool riding = rowaida != null && NewFeatures.RowaidaOn(b, rowaida.transform.position) && rowaida.transform.position.x > b.min.x + 0.5f;

        if (!rising && !done && riding && gm.IsPlaying)
        {
            rising = true;
            targetTop = FindTarget(b, gm.CurrentLevel);
            rowaida.Held = true;
            gm.Sound.PlaySqueak(1.5f);
        }

        Vector3 v = rb.linearVelocity;
        if (rising)
        {
            v.y = RiseSpeed;
            // رويدا تطلع معها (ما نخليها تتأخر عن البالونة)
            if (riding)
            {
                var body = rowaida.GetComponent<Rigidbody>();
                Vector3 rv = body.linearVelocity;
                if (rv.y < RiseSpeed) { rv.y = RiseSpeed; body.linearVelocity = rv; }
            }
            if (b.max.y >= targetTop)
            {
                v.y = 0f;
                rising = false;
                done = true;
                doneTime = Time.time;
                if (rowaida != null) rowaida.Held = false;
            }
        }
        else if (done)
        {
            // بعد ما تنزل منها تطير بعيد وتنفجر
            float since = Time.time - doneTime;
            v.y = since > 2.2f ? 2.4f : 0f;
            if (since > 4.2f)
            {
                Fx.Sparkle(b.center, rend.material.GetColor("_BaseColor"), 14);
                gm.Sound.PlayPoof();
                Destroy(gameObject);
                return;
            }
        }
        else v.y = Mathf.Sin(Time.time * 2f) * 0.12f;   // تطفو مكانها
        rb.linearVelocity = v;
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
        if (rising && gm != null && gm.Rowaida != null) gm.Rowaida.Held = false;
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
