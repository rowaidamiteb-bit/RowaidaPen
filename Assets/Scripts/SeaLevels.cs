using System.Collections.Generic;
using UnityEngine;

// الجزء الثاني — فصل البحر: موج ومد وجزر، سرطانات، فقاعات هوا، أخطبوط، دوامة، ومنارة
public static class SeaFeatures
{
    public static void Build(Transform root, LevelData data, GameManager gm, System.Random rng)
    {
        foreach (Vector4 s in data.slopes)
            SnowFeatures.SlopedPage(root, new Vector2(s.x, s.y), new Vector2(s.z, s.w), data, gm, rng);
        if (data.sea) SeaWater.Create(root, data, gm);
        foreach (Vector3 c in data.crabs) Crab.Create(root, c, gm);
        foreach (Vector3 v in data.vents) BubbleVent.Create(root, v, gm);
        foreach (Vector3 j in data.jellies) Jellyfish.Create(root, j, gm);
        if (data.underwater)
        {
            SeaDecor.Create(root, data, gm, rng);
            var rowaida = root.GetComponentInChildren<RowaidaController>();
            if (rowaida != null) AirTank.Create(rowaida, gm);
        }
    }
}

// البحر: سطحه يطلع وينزل (مد وجزر) مع موج صغير. لو غطى نص رويدا تغرق.
// الأشكال المغلقة تطفو عليه
public class SeaWater : MonoBehaviour
{
    public static SeaWater Instance;
    float baseLevel, amp, period, start, touch;
    Transform body, foam;
    readonly List<Transform> waves = new List<Transform>();
    GameManager gm;
    const float Depth = 40f;

    // مستوى الماي الحين (يبدا من الجزر)
    public float Level => baseLevel - amp * Mathf.Cos((Time.time - start) * Mathf.PI * 2f / Mathf.Max(0.1f, period));

    public static SeaWater Create(Transform root, LevelData data, GameManager manager)
    {
        var go = new GameObject("Sea");
        go.transform.SetParent(root, false);
        var s = go.AddComponent<SeaWater>();
        Instance = s;
        s.gm = manager;
        s.baseLevel = data.seaLevel;
        s.amp = data.tideAmp;
        s.period = data.tidePeriod;
        s.start = Time.time;

        Material water = manager.Mat("Sea Water", data.theme.seaColor, 0.55f);
        Material crest = manager.Mat("Sea Crest", Color.Lerp(data.theme.seaColor, Color.white, 0.35f), 0.6f);
        Material foamMat = manager.Mat("Sea Foam", new Color(0.92f, 0.97f, 1f), 0.6f);
        s.body = LevelBuilder.Visual("Water", PrimitiveType.Cube, go.transform, Vector3.zero, new Vector3(200f, Depth, 16f), water, false).transform;
        s.foam = LevelBuilder.Visual("Foam", PrimitiveType.Cube, go.transform, Vector3.zero, new Vector3(200f, 0.08f, 0.35f), foamMat, false).transform;
        // موجات صغيرة على السطح
        for (int i = 0; i < 90; i++)
            s.waves.Add(LevelBuilder.Visual("Wave", PrimitiveType.Sphere, go.transform, Vector3.zero, new Vector3(1.3f, 0.22f, 0.6f), i % 2 == 0 ? crest : water, false).transform);
        manager.Respawned += s.OnRespawned;
        s.Place();
        return s;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (gm != null) gm.Respawned -= OnRespawned;
    }

    // ترجع بعد ما غرقت: نبدا الدورة من الجزر عشان تلحق
    void OnRespawned(Vector3 pos) => start = Time.time;

    void Place()
    {
        float level = Level, t = Time.time;
        body.position = new Vector3(40f, level - Depth * 0.5f, 6f);
        foam.position = new Vector3(40f, level + 0.02f, -2.1f);
        for (int i = 0; i < waves.Count; i++)
        {
            float x = -30f + i * 1.4f;
            float z = -1.8f + (i % 3) * 2.5f;
            waves[i].position = new Vector3(x + Mathf.Sin(t * 0.8f + i) * 0.3f, level + Mathf.Sin(t * 2.2f + i * 0.9f) * 0.08f, z);
        }
    }

    void Update()
    {
        Place();
        if (!gm.IsPlaying) return;
        var r = gm.Rowaida;
        if (r == null) return;
        float waist = r.transform.position.y - 0.2f;
        touch = waist < Level ? touch + Time.deltaTime : 0f;
        if (touch > 0.3f)
        {
            touch = 0f;
            Fx.Sparkle(r.transform.position, new Color(0.6f, 0.85f, 1f), 16);
            gm.Sound.PlayPoof();
            gm.KillRowaida();
        }
    }

    void FixedUpdate()
    {
        float level = Level;
        foreach (InkMark mark in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
        {
            if (!mark.closed) continue;
            var rb = mark.GetComponent<Rigidbody>();
            var rend = mark.GetComponent<MeshRenderer>();
            if (rb == null || rend == null || rb.isKinematic) continue;
            Bounds b = rend.bounds;
            float submerged = Mathf.Clamp01((level - b.min.y) / Mathf.Max(0.1f, b.size.y));
            if (submerged <= 0f) continue;
            rb.AddForce(Vector3.up * submerged * 3f * 9.81f, ForceMode.Acceleration);
            rb.linearVelocity *= 1f - 2.5f * submerged * Time.fixedDeltaTime;
            rb.angularVelocity *= 1f - 4f * submerged * Time.fixedDeltaTime;
        }
    }
}

// سرطان يمشي جنب على الرمل رايح جاي. لو لمس رويدا يقرصها ويرجعها لورا
public class Crab : MonoBehaviour
{
    float from, to, dir = 1f, nextPinch;
    Transform body, clawL, clawR;
    readonly List<Transform> legs = new List<Transform>();

    public static void Create(Transform root, Vector3 spec, GameManager gm)
    {
        var go = new GameObject("Crab");
        go.transform.SetParent(root, false);
        go.transform.position = new Vector3((spec.x + spec.y) * 0.5f, spec.z + 0.28f, -0.2f);
        var c = go.AddComponent<Crab>();
        c.from = spec.x; c.to = spec.y;
        Material shell = gm.Mat("Crab Shell", new Color(1f, 0.42f, 0.25f), 0.45f);
        Material dark = gm.Mat("Crab Dark", new Color(0.8f, 0.25f, 0.15f), 0.4f);
        Material white = gm.Mat("Monster Eye", Color.white, 0.7f);
        Material black = gm.Mat("Monster Pupil", new Color(0.05f, 0.04f, 0.06f), 0.9f);
        c.body = new GameObject("Body").transform;
        c.body.SetParent(go.transform, false);
        LevelBuilder.Visual("Shell", PrimitiveType.Sphere, c.body, Vector3.zero, new Vector3(0.75f, 0.4f, 0.55f), shell);
        for (int s = -1; s <= 1; s += 2)
        {
            LevelBuilder.Visual("Stalk", PrimitiveType.Cylinder, c.body, new Vector3(s * 0.12f, 0.25f, -0.15f), new Vector3(0.04f, 0.1f, 0.04f), dark, false);
            LevelBuilder.Visual("Eye", PrimitiveType.Sphere, c.body, new Vector3(s * 0.12f, 0.37f, -0.17f), Vector3.one * 0.13f, white, false);
            LevelBuilder.Visual("Pupil", PrimitiveType.Sphere, c.body, new Vector3(s * 0.12f, 0.37f, -0.23f), Vector3.one * 0.06f, black, false);
            var claw = new GameObject("Claw").transform;
            claw.SetParent(c.body, false);
            claw.localPosition = new Vector3(s * 0.45f, 0.08f, -0.2f);
            LevelBuilder.Visual("Pincer A", PrimitiveType.Sphere, claw, new Vector3(0f, 0.06f, 0f), new Vector3(0.22f, 0.12f, 0.14f), shell);
            LevelBuilder.Visual("Pincer B", PrimitiveType.Sphere, claw, new Vector3(0f, -0.06f, 0f), new Vector3(0.2f, 0.1f, 0.13f), dark);
            if (s < 0) c.clawL = claw; else c.clawR = claw;
            for (int k = 0; k < 3; k++)
            {
                var leg = LevelBuilder.Visual("Leg", PrimitiveType.Cube, c.body, new Vector3(s * (0.3f + k * 0.04f), -0.15f, 0.05f + k * 0.08f), new Vector3(0.22f, 0.04f, 0.04f), dark, false).transform;
                leg.localRotation = Quaternion.Euler(0f, 0f, s * -35f);
                c.legs.Add(leg);
            }
        }
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time;
        Vector3 p = transform.position;
        p.x += dir * 1.2f * Time.deltaTime;
        if (p.x > to - 0.4f) dir = -1f;
        if (p.x < from + 0.4f) dir = 1f;
        transform.position = p;
        // رجول تتحرك، ومقصات تفتح وتسكر
        for (int i = 0; i < legs.Count; i++) legs[i].localPosition += new Vector3(0f, Mathf.Sin(t * 18f + i * 2f) * 0.002f, 0f);
        float snap = Mathf.Abs(Mathf.Sin(t * 5f)) * 25f;
        clawL.localRotation = Quaternion.Euler(0f, 0f, snap);
        clawR.localRotation = Quaternion.Euler(0f, 0f, -snap);
        body.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(t * 9f)) * 0.04f, 0f);

        RowaidaController r = gm.Rowaida;
        if (r == null || !gm.IsPlaying || t < nextPinch) return;
        Vector3 d = r.transform.position - p;
        if (Mathf.Abs(d.x) < 0.6f && d.y > -0.2f && d.y < 1.1f)
        {
            nextPinch = t + 1f;
            r.Knockback(-1f);
            gm.Sound.PlaySqueak(1.8f);
            Fx.Sparkle(r.transform.position, new Color(1f, 0.5f, 0.3f), 10);
        }
    }
}

// ---------- تحت الماي ----------

// هوا رويدا: ينقص مع الوقت (فقاعات صغيرة فوق راسها تختفي وحدة وحدة). إذا خلص تخسر قلب
public class AirTank : MonoBehaviour
{
    public const float Drain = 1f / 11f;
    public static AirTank Instance;
    public float air = 1f;
    readonly List<Transform> dots = new List<Transform>();
    GameManager gm;
    float warnAt;

    public static void Create(RowaidaController rowaida, GameManager gm)
    {
        var a = rowaida.gameObject.AddComponent<AirTank>();
        Instance = a;
        a.gm = gm;
        Material bubble = gm.Mat("Air Dot", new Color(0.85f, 0.97f, 1f), 0.95f);
        for (int i = 0; i < 5; i++)
            a.dots.Add(LevelBuilder.Visual("Air Dot", PrimitiveType.Sphere, null, Vector3.zero, Vector3.one * 0.18f, bubble, false).transform);
        gm.Respawned += a.OnRespawned;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (gm != null) gm.Respawned -= OnRespawned;
        foreach (Transform d in dots) if (d != null) Destroy(d.gameObject);
    }

    void OnRespawned(Vector3 pos) => air = 1f;

    public void Refill(float amount)
    {
        air = Mathf.Min(1f, air + amount);
        gm.Sound.PlaySqueak(1.5f);
        Fx.Sparkle(transform.position + Vector3.up * 0.8f, new Color(0.8f, 0.95f, 1f), 12);
    }

    void Update()
    {
        if (gm.IsPlaying) air = Mathf.Max(0f, air - Drain * Time.deltaTime);
        // خمس فقاعات فوق الراس على شكل قوس
        Vector3 head = transform.position + Vector3.up * 1.35f;
        for (int i = 0; i < dots.Count; i++)
        {
            float k = Mathf.Clamp01(air * dots.Count - i);
            dots[i].position = head + new Vector3((i - 2f) * 0.26f, -Mathf.Abs(i - 2f) * 0.06f + Mathf.Sin(Time.time * 3f + i) * 0.03f, -0.4f);
            dots[i].localScale = Vector3.one * 0.18f * k;
        }
        if (air < 0.25f && Time.time >= warnAt && gm.IsPlaying)
        {
            warnAt = Time.time + 0.6f;
            gm.Sound.PlaySqueak(0.7f);
        }
        if (air <= 0f && gm.IsPlaying)
        {
            Fx.Sparkle(transform.position, new Color(0.7f, 0.9f, 1f), 16);
            gm.KillRowaida();
        }
    }
}

// فتحة بقاع البحر تطلع فقاعات هوا كبيرة كل شوي
public class BubbleVent : MonoBehaviour
{
    float next, period;
    Material bubbleMat;

    public static void Create(Transform root, Vector3 spec, GameManager gm)
    {
        var go = new GameObject("Bubble Vent");
        go.transform.SetParent(root, false);
        go.transform.position = new Vector3(spec.x, spec.y, 0f);
        var v = go.AddComponent<BubbleVent>();
        v.period = spec.z;
        v.next = Time.time + 0.5f;
        v.bubbleMat = gm.Mat("Air Bubble", new Color(0.8f, 0.95f, 1f), 0.98f);
        Material rock = gm.Mat("Vent Rock", new Color(0.35f, 0.4f, 0.5f), 0.2f);
        for (int i = 0; i < 5; i++)
            LevelBuilder.Visual("Vent Rock", PrimitiveType.Sphere, go.transform, new Vector3(-0.5f + i * 0.25f, 0.05f + (i % 2) * 0.12f, (i % 3 - 1) * 0.3f), Vector3.one * (0.35f + (i % 2) * 0.15f), rock);
    }

    void Update()
    {
        if (Time.time < next) return;
        next = Time.time + period;
        AirBubble.Spawn(transform.position + Vector3.up * 0.4f, bubbleMat);
    }
}

// فقاعة هوا تطلع لفوق. إذا صدمت خط من تحت تتزحلق عليه، ولو لمستها رويدا تعبي هواها
public class AirBubble : MonoBehaviour
{
    float age;

    public static void Spawn(Vector3 pos, Material mat)
    {
        var go = LevelBuilder.Visual("Air Bubble", PrimitiveType.Sphere, null, pos, Vector3.one * 0.55f, mat, false);
        go.AddComponent<AirBubble>();
        LevelBuilder.Visual("Shine", PrimitiveType.Sphere, go.transform, new Vector3(-0.18f, 0.18f, -0.3f), Vector3.one * 0.25f, GameManager.Instance.Mat("Bubble Shine", Color.white, 1f), false);
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        age += Time.deltaTime;
        Vector3 p = transform.position;
        Vector3 move = Vector3.up * 1.4f;
        // خط فوقها؟ تتزحلق على طوله (لو مسطح تروح لقدام)
        if (Physics.SphereCast(p, 0.25f, Vector3.up, out RaycastHit hit, 0.3f, ~0, QueryTriggerInteraction.Ignore)
            && hit.collider.GetComponentInParent<InkMark>() != null)
        {
            Vector3 along = Vector3.ProjectOnPlane(Vector3.up, hit.normal);
            along.z = 0f;
            if (along.magnitude < 0.25f) along = Vector3.right;
            move = along.normalized * 1.8f;
        }
        transform.position = p + move * Time.deltaTime;
        transform.localScale = Vector3.one * (0.55f + Mathf.Sin(age * 6f) * 0.03f);

        if (gm != null && gm.Rowaida != null && AirTank.Instance != null)
        {
            Vector3 r = gm.Rowaida.transform.position + Vector3.up * 0.3f;
            if ((r - transform.position).sqrMagnitude < 1.3f * 1.3f)
            {
                AirTank.Instance.Refill(0.45f);
                Destroy(gameObject);
                return;
            }
        }
        if (age > 10f) Destroy(gameObject);
    }
}

// قنديل بحر يطلع وينزل ببطء. لو لمس رويدا يلسعها ويرجعها لورا
public class Jellyfish : MonoBehaviour
{
    Vector3 home;
    float range, seed, nextSting, lift, nextScan;
    bool scared;
    Transform bell;
    readonly List<Transform> tentacles = new List<Transform>();

    public static void Create(Transform root, Vector3 spec, GameManager gm)
    {
        var go = new GameObject("Jellyfish");
        go.transform.SetParent(root, false);
        go.transform.position = new Vector3(spec.x, spec.y, 0f);
        var j = go.AddComponent<Jellyfish>();
        j.home = go.transform.position;
        j.range = spec.z;
        j.seed = spec.x;
        Material jelly = gm.Mat("Jelly", new Color(1f, 0.55f, 0.85f), 0.9f);
        Material glow = gm.Mat("Jelly Glow", new Color(1.6f, 0.9f, 1.6f), 0.9f);
        j.bell = LevelBuilder.Visual("Bell", PrimitiveType.Sphere, go.transform, Vector3.zero, new Vector3(0.9f, 0.6f, 0.9f), jelly).transform;
        LevelBuilder.Visual("Dot", PrimitiveType.Sphere, j.bell, new Vector3(0f, 0.1f, -0.42f), Vector3.one * 0.2f, glow, false);
        for (int i = 0; i < 5; i++)
            j.tentacles.Add(LevelBuilder.Visual("Tentacle", PrimitiveType.Capsule, go.transform, new Vector3(-0.3f + i * 0.15f, -0.55f, 0f), new Vector3(0.06f, 0.35f, 0.06f), jelly, false).transform);
    }

    void Update()
    {
        float t = Time.time + seed;
        // ينقبض ويطلع، وبعدين ينزل شوي شوي
        float pulse = Mathf.Sin(t * 2.2f);
        // يخاف من الحبر: إذا فيه خط قريب منه يطلع فوق بعيد، وبعدين يرجع شوي شوي
        if (Time.time >= nextScan)
        {
            nextScan = Time.time + 0.15f;
            scared = false;
            foreach (InkMark mark in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
            {
                var rend = mark.GetComponent<MeshRenderer>();
                if (rend != null && rend.bounds.SqrDistance(transform.position) < 1.8f * 1.8f) { scared = true; break; }
            }
        }
        lift = scared ? Mathf.MoveTowards(lift, 3.2f, 3f * Time.deltaTime) : Mathf.MoveTowards(lift, 0f, 0.5f * Time.deltaTime);
        transform.position = home + Vector3.up * (Mathf.Sin(t * 0.7f) * range + lift);
        bell.localScale = new Vector3(0.9f - pulse * 0.08f, 0.6f + pulse * 0.08f, 0.9f - pulse * 0.08f);
        for (int i = 0; i < tentacles.Count; i++)
            tentacles[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 3f + i) * 15f);

        GameManager gm = GameManager.Instance;
        if (gm == null || gm.Rowaida == null || !gm.IsPlaying || Time.time < nextSting) return;
        Vector3 d = gm.Rowaida.transform.position + Vector3.up * 0.2f - transform.position;
        if (Mathf.Abs(d.x) < 0.7f && Mathf.Abs(d.y) < 1.1f)
        {
            nextSting = Time.time + 1.2f;
            gm.Rowaida.Knockback(-1f);
            gm.Sound.PlayLaser();
            Fx.Sparkle(gm.Rowaida.transform.position, new Color(1f, 0.6f, 0.9f), 12);
        }
    }
}

// زينة: عشب بحر يتمايل، وسمك صغير يسبح ورا
public class SeaDecor : MonoBehaviour
{
    readonly List<Transform> weeds = new List<Transform>();
    readonly List<Transform> fish = new List<Transform>();
    readonly List<float> fishSpeed = new List<float>();

    public static void Create(Transform root, LevelData data, GameManager gm, System.Random rng)
    {
        var go = new GameObject("Sea Decor");
        go.transform.SetParent(root, false);
        var d = go.AddComponent<SeaDecor>();
        Material weed = gm.Mat("Seaweed", new Color(0.25f, 0.65f, 0.4f), 0.4f);
        Material weed2 = gm.Mat("Seaweed Light", new Color(0.4f, 0.8f, 0.45f), 0.4f);
        foreach (Block b in data.blocks)
        {
            float top = b.center.y + b.size.y * 0.5f, x0 = b.center.x - b.size.x * 0.5f, x1 = b.center.x + b.size.x * 0.5f;
            for (float x = x0 + 0.8f; x < x1 - 0.5f; x += 1.6f + (float)rng.NextDouble() * 1.8f)
            {
                var pivot = new GameObject("Weed").transform;
                pivot.SetParent(go.transform, false);
                pivot.position = new Vector3(x, top, 0.9f + (float)rng.NextDouble() * 0.8f);
                float h = 0.6f + (float)rng.NextDouble() * 0.9f;
                for (int k = 0; k < 3; k++)
                    LevelBuilder.Visual("Leaf", PrimitiveType.Capsule, pivot, new Vector3(Mathf.Sin(k) * 0.06f, h * (k + 0.5f) * 0.5f, 0f), new Vector3(0.14f, h * 0.3f, 0.14f), k % 2 == 0 ? weed : weed2, false);
                d.weeds.Add(pivot);
            }
        }
        Color[] fishColors = { new Color(1f, 0.6f, 0.2f), new Color(1f, 0.85f, 0.3f), new Color(0.4f, 0.8f, 1f) };
        for (int i = 0; i < 14; i++)
        {
            Material m = gm.Mat("Fish " + i % 3, fishColors[i % 3], 0.5f);
            var f = new GameObject("Fish").transform;
            f.SetParent(go.transform, false);
            f.position = new Vector3(-10f + (float)rng.NextDouble() * 70f, 1.5f + (float)rng.NextDouble() * 5f, 4f + (float)rng.NextDouble() * 5f);
            LevelBuilder.Visual("Body", PrimitiveType.Sphere, f, Vector3.zero, new Vector3(0.6f, 0.35f, 0.2f), m, false);
            var tail = LevelBuilder.Visual("Tail", PrimitiveType.Cube, f, new Vector3(-0.35f, 0f, 0f), new Vector3(0.2f, 0.25f, 0.04f), m, false);
            tail.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            d.fish.Add(f);
            d.fishSpeed.Add(0.6f + (float)rng.NextDouble() * 0.8f);
        }
    }

    void Update()
    {
        float t = Time.time;
        for (int i = 0; i < weeds.Count; i++) weeds[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.3f + i) * 12f);
        for (int i = 0; i < fish.Count; i++)
        {
            Vector3 p = fish[i].position;
            p.x += fishSpeed[i] * Time.deltaTime;
            p.y += Mathf.Sin(t * 1.5f + i) * 0.003f;
            if (p.x > 70f) p.x = -15f;
            fish[i].position = p;
        }
    }
}
