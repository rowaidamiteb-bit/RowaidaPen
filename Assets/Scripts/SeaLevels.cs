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
        foreach (Vector3 t in data.tentacles) Tentacle.Create(root, t, gm);
        foreach (Vector3 w in data.whirlpools) Whirlpool.Create(root, w, gm);
        foreach (Vector3 s in data.sharks) Shark.Create(root, s, gm);
        if (data.lighthouse.HasValue) Lighthouse.Create(root, data.lighthouse.Value, gm);
        if (data.waitForBridge) GapWait.Create(root, data);
        if (data.tideBars.Count > 0)
        {
            TideWait.Create(root, data);
            TideRuler.Create(root, data.tideBars[0].x + 1.5f, data.tideBars[0].z + 0.4f, gm);
        }
        if (data.boatRide) new GameObject("Boat Dock").AddComponent<BoatDock>().transform.SetParent(root, false);
        if (data.tentacles.Count > 0) GiantOctopus.Create(root, new Vector3(data.door.x * 0.5f, 0.7f, 12f), gm);
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
    Transform body, foam, edge;
    readonly List<Transform> waves = new List<Transform>();
    readonly List<Vector3> waveBase = new List<Vector3>();
    GameManager gm;
    const float Depth = 40f;

    // مستوى المويه الحين (يبدا من الجزر)
    public float Level => LevelAt(Time.time);
    public float LevelAt(float time) => baseLevel - amp * Mathf.Cos((time - start) * Mathf.PI * 2f / Mathf.Max(0.1f, period));

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

        // البحر ورقة زرقا من الدفتر: سطور وتظليل بالألوان الخشبية، وحافة أوراق قدام
        Color sea = data.theme.seaColor;
        var water = new Material(manager.paperMaterial) { name = "Sea Paper" };
        water.SetTexture("_BaseMap", SeaPaperTexture(sea));
        water.SetColor("_BaseColor", Color.Lerp(sea, Color.white, 0.25f) * 1.15f);
        water.SetFloat("_Smoothness", 0.15f);
        water.mainTextureScale = new Vector2(60f, 4f);
        s.body = LevelBuilder.Visual("Water", PrimitiveType.Cube, go.transform, Vector3.zero, new Vector3(200f, Depth, 16f), water, false).transform;
        s.edge = LevelBuilder.Visual("Paper Edge", PrimitiveType.Cube, go.transform, Vector3.zero, new Vector3(200f, 0.9f, 0.05f), manager.pageEdgeMaterial, false).transform;
        // موج مرسوم بالقلم: خطوط ~ على السطح، وحافة مموجة بيضا قدام
        Material pencilBlue = manager.Mat("Sea Pencil " + ColorUtility.ToHtmlStringRGB(sea), Color.Lerp(sea, Color.black, 0.35f), 0.2f);
        Material foamMat = manager.Mat("Sea Foam", new Color(0.97f, 0.98f, 1f), 0.3f);
        var foamPts = new List<Vector3>();
        for (float x = -40f; x <= 120f; x += 0.1f)
            foamPts.Add(new Vector3(x, Mathf.Abs(Mathf.Sin(x * Mathf.PI / 0.8f)) * 0.12f, 0f));
        s.foam = DoodleLine("Foam Edge", go.transform, foamPts, 0.05f, foamMat).transform;
        var rng = new System.Random(5);
        for (int i = 0; i < 48; i++)
        {
            var pts = new List<Vector3>();
            float len = 1.6f + (float)rng.NextDouble() * 1.6f;
            for (float x = 0f; x <= len; x += 0.08f)
                pts.Add(new Vector3(x, Mathf.Sin(x * Mathf.PI * 2f / 0.8f) * 0.07f, 0f));
            var w = DoodleLine("Wave Doodle", go.transform, pts, 0.035f, pencilBlue).transform;
            s.waves.Add(w);
            s.waveBase.Add(new Vector3(-35f + (float)rng.NextDouble() * 150f, 0f, -1.4f + (float)rng.NextDouble() * 9f));
        }
        manager.Respawned += s.OnRespawned;
        s.Place();
        return s;
    }


    // خط مرسوم (أنبوب رفيع) من نقاط
    public static GameObject DoodleLine(string name, Transform parent, List<Vector3> pts, float radius, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var mesh = new Mesh { name = name };
        TubeMesh.Build(mesh, pts, radius, false, 6);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return go;
    }

    // نسيج ورقة البحر: سطور دفتر خفيفة وتظليل مايل مثل الألوان الخشبية
    static Texture2D SeaPaperTexture(Color sea)
    {
        const int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat };
        var rng = new System.Random(11);
        var px = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float v = 0.93f + (float)rng.NextDouble() * 0.05f;
                // تظليل مايل
                if ((x + y * 2) % 9 < 2) v -= 0.07f;
                // سطر الدفتر
                if (y % 32 == 0) v -= 0.12f;
                px[y * size + x] = new Color(v, v, v, 1f);
            }
        tex.SetPixels(px);
        tex.Apply();
        return tex;
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
        edge.position = new Vector3(40f, level - 0.45f, -2.03f);
        foam.position = new Vector3(Mathf.Sin(t * 0.7f) * 0.15f, level, -2.06f);
        for (int i = 0; i < waves.Count; i++)
            waves[i].position = waveBase[i] + new Vector3(Mathf.Sin(t * 0.5f + i) * 0.4f, level + 0.03f + Mathf.Sin(t * 1.5f + i) * 0.03f, 0f);
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
            nextPinch = t + 2.5f;
            dir = Mathf.Sign(p.x - r.transform.position.x);   // يقرص مرة ويهرب
            r.Knockback(-1f);
            gm.Sound.PlaySqueak(1.8f);
            Fx.Sparkle(r.transform.position, new Color(1f, 0.5f, 0.3f), 10);
        }
    }
}

// ---------- تحت المويه ----------

// هوا رويدا: ينقص مع الوقت (فقاعات صغيرة فوق راسها تختفي وحدة وحدة). إذا خلص تخسر قلب
public class AirTank : MonoBehaviour
{
    public const float Drain = 1f / 15f;
    public static AirTank Instance;
    public float air = 1f;
    readonly List<Transform> dots = new List<Transform>();
    GameManager gm;
    float warnAt;
    bool warned;

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
        if (gm != null && gm.UI != null) gm.UI.SetAir(false, 0f);
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
        gm.UI.SetAir(true, air);
        if (air < 0.3f && !warned && gm.IsPlaying) { warned = true; gm.UI.ShowMessage("", 2.2f, "الهوا قرب يخلص! خذي فقاعة"); }
        if (air > 0.5f) warned = false;
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
        var go = LevelBuilder.Visual("Air Bubble", PrimitiveType.Sphere, null, pos, Vector3.one * 0.75f, mat, false);
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
        // قريبة من رويدا؟ تنجذب لها (مثل المغناطيس) عشان تاخذها بسهولة
        if (gm != null && gm.Rowaida != null && AirTank.Instance != null)
        {
            Vector3 toHer = gm.Rowaida.transform.position + Vector3.up * 0.4f - p;
            if (toHer.sqrMagnitude < 3.5f * 3.5f) move = toHer.normalized * 5f;
        }
        transform.position = p + move * Time.deltaTime;
        transform.localScale = Vector3.one * (0.75f + Mathf.Sin(age * 6f) * 0.04f);

        if (gm != null && gm.Rowaida != null && AirTank.Instance != null)
        {
            Vector3 r = gm.Rowaida.transform.position + Vector3.up * 0.3f;
            if ((r - transform.position).sqrMagnitude < 1.3f * 1.3f)
            {
                AirTank.Instance.Refill(0.6f);
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

// ---------- الأخطبوط ----------

// ذراع أخطبوط جوعانة واقفة بالحفرة: أي جسر ترسمينه فوقها تسحبه تحت المويه.
// ارمي لها كورة (شكل مغلق): تاكلها وتنام تحت المويه، وبعدها ارسمي الجسر وعدّي
public class Tentacle : MonoBehaviour
{
    enum State { Hungry, Grabbing, Eating, Sleeping }
    const float Height = 3.6f;
    float from, to, stateT, nextZ, nextScan;
    State state = State.Hungry;
    bool holding;
    int prompted = -1;
    readonly List<Transform> segs = new List<Transform>();
    GameObject prey;
    Vector3 preyTarget;
    Material bubbleMat, zMat;

    float Cx => (from + to) * 0.5f;
    float Water => SeaWater.Instance != null ? SeaWater.Instance.Level : -0.7f;

    public static void Create(Transform root, Vector3 spec, GameManager gm)
    {
        var go = new GameObject("Tentacle");
        go.transform.SetParent(root, false);
        var t = go.AddComponent<Tentacle>();
        t.from = spec.x; t.to = spec.y;
        Material skin = gm.Mat("Octopus Skin", new Color(0.75f, 0.3f, 0.6f), 0.5f);
        Material sucker = gm.Mat("Octopus Sucker", new Color(1f, 0.75f, 0.8f), 0.4f);
        Material white = gm.Mat("Monster Eye", Color.white, 0.7f);
        Material black = gm.Mat("Monster Pupil", new Color(0.05f, 0.04f, 0.06f), 0.9f);
        t.bubbleMat = gm.Mat("Air Bubble", new Color(0.8f, 0.95f, 1f), 0.98f);
        t.zMat = gm.Mat("Sleep Z", new Color(0.95f, 0.95f, 1f), 0.3f);
        for (int i = 0; i < 16; i++)
        {
            float k = 1f - i / 16f;
            var s = LevelBuilder.Visual("Segment", PrimitiveType.Sphere, go.transform, Vector3.zero, Vector3.one * (0.3f + 0.55f * k), skin).transform;
            LevelBuilder.Visual("Sucker", PrimitiveType.Sphere, s, new Vector3(-0.35f, 0f, -0.25f), new Vector3(0.35f, 0.35f, 0.2f), sucker, false);
            t.segs.Add(s);
        }
        // عيون صغيرة على طرف الذراع (عشان تبان إنها تطالع وتنتظر)
        Transform tip = t.segs[t.segs.Count - 1];
        for (int e = -1; e <= 1; e += 2)
        {
            LevelBuilder.Visual("Eye", PrimitiveType.Sphere, tip, new Vector3(e * 0.35f, 0.2f, -0.45f), Vector3.one * 0.55f, white, false);
            LevelBuilder.Visual("Pupil", PrimitiveType.Sphere, tip, new Vector3(e * 0.35f, 0.18f, -0.7f), Vector3.one * 0.28f, black, false);
        }
    }

    // كورة (شكل مغلق) قريبة من الحفرة
    InkMark FindBall()
    {
        foreach (InkMark mark in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
        {
            if (!mark.closed || mark.gameObject == prey) continue;
            Vector3 c = mark.transform.position;
            if (c.x > from - 2.5f && c.x < to + 2.5f && c.y < Water + 4f) return mark;
        }
        return null;
    }

    // جسر فوق الحفرة
    InkMark FindBridge()
    {
        foreach (InkMark mark in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
        {
            if (mark.closed || mark.gameObject == prey) continue;
            var r = mark.GetComponent<MeshRenderer>();
            if (r == null) continue;
            Bounds b = r.bounds;
            if (b.max.x > from + 0.3f && b.min.x < to - 0.3f && b.min.y < Water + 4f) return mark;
        }
        return null;
    }

    void Grab(InkMark mark, State next)
    {
        prey = mark.gameObject;
        foreach (Collider c in prey.GetComponentsInChildren<Collider>()) c.enabled = false;
        var body = prey.GetComponent<Rigidbody>();
        if (body != null) body.isKinematic = true;
        preyTarget = mark.closed ? mark.transform.position : mark.GetComponent<MeshRenderer>().bounds.center;
        state = next;
        stateT = 0f;
        GameManager.Instance.Sound.PlayChomp();
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time, water = Water;
        stateT += Time.deltaTime;

        // يدور على أكل أو جسر
        if (state == State.Hungry && t >= nextScan)
        {
            nextScan = t + 0.1f;
            InkMark ball = FindBall();
            if (ball != null) Grab(ball, State.Eating);
            else
            {
                InkMark found = FindBridge();
                if (found != null) Grab(found, State.Grabbing);
            }
        }

        // كم طالعة الذراع، ووين طرفها
        float up;
        Vector3 basePos = new Vector3(Cx, water - 1.2f, -0.3f);
        Vector3 tipAim = basePos + Vector3.up * Height;
        switch (state)
        {
            case State.Hungry:
                up = 1f;
                break;
            case State.Grabbing:
            case State.Eating:
                // تمد طرفها للشي (0.4 ث)، وبعدين تسحبه تحت (0.8 ث)
                tipAim = preyTarget;
                up = stateT < 0.4f ? 1f : Mathf.Clamp01(1f - (stateT - 0.4f) / 0.8f);
                if (stateT > 1.2f)
                {
                    if (prey != null)
                    {
                        Fx.Sparkle(new Vector3(Cx, water, 0f), new Color(0.6f, 0.85f, 1f), 14);
                        Destroy(prey);
                    }
                    gm.Sound.PlayPoof();
                    if (state == State.Eating)
                    {
                        state = State.Sleeping;
                        Fx.Sparkle(new Vector3(Cx, water + 0.5f, -0.5f), new Color(1f, 0.6f, 0.8f), 20);
                        gm.UI.ShowMessage("", 2f, "شبع ونام! ارسمي الجسر");
                    }
                    else state = State.Hungry;
                    stateT = 0f;
                }
                break;
            default:
                up = 0f;
                break;
        }
        // ترجع تطلع بعد ما تسحب الجسر
        if (state == State.Hungry && stateT < 0.6f) up = stateT / 0.6f;

        Vector3 tip = basePos;
        for (int i = 0; i < segs.Count; i++)
        {
            float k = i / (float)(segs.Count - 1);
            float sway = Mathf.Sin(t * 2.2f + k * 3.5f) * 0.5f * k;
            Vector3 straight = basePos + new Vector3(sway + k * k * 0.8f, k * Height, 0f);
            Vector3 reach = Vector3.Lerp(basePos, tipAim, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 1.2f;
            Vector3 p = (state == State.Grabbing || state == State.Eating) ? reach : straight;
            p = Vector3.Lerp(basePos + Vector3.up * k * 0.5f, p, up);
            segs[i].position = p;
            segs[i].gameObject.SetActive(p.y > water - 0.5f);
            tip = p;
        }
        if (prey != null) prey.transform.position += (tip - (prey.GetComponent<InkMark>().closed ? prey.transform.position : prey.GetComponent<MeshRenderer>().bounds.center)) * Mathf.Min(1f, Time.deltaTime * 10f);

        // نايمة: Z تطلع من المويه
        if (state == State.Sleeping && t >= nextZ)
        {
            nextZ = t + 0.7f;
            var z = LevelBuilder.Visual("Z", PrimitiveType.Cube, null, new Vector3(Cx + Random.Range(-0.4f, 0.4f), water + 0.2f, -0.6f), new Vector3(0.28f, 0.06f, 0.02f), zMat, false);
            var fx = z.AddComponent<FxParticle>();
            fx.velocity = new Vector3(0.3f, 0.8f, 0f);
            fx.grow = 0.8f;
            fx.life = 1.4f;
        }

        // رويدا تستنى عند الطرف لين تنام الذراع وينرسم جسر
        RowaidaController rowaida = gm.Rowaida;
        if (rowaida == null) return;
        Vector3 rp = rowaida.transform.position;
        bool atEdge = rp.x > from - 1.6f && rp.x < from - 0.5f && rp.y > water + 0.5f;
        bool bridge = RowaidaController.BridgeCovers(from, to, 0f);
        bool wait = atEdge && gm.IsPlaying && (state != State.Sleeping || !bridge);
        if (wait && prompted != (int)state * 10 + (bridge ? 1 : 0))
        {
            prompted = (int)state * 10 + (bridge ? 1 : 0);
            gm.UI.ShowMessage("", 2.5f, state != State.Sleeping ? "الأخطبوط جوعان! ارمي له كورة بالمويه" : "ارسمي الجسر");
        }
        if (wait) { rowaida.Held = true; rowaida.CarryX = 0f; holding = true; }
        else if (holding) { rowaida.Held = false; holding = false; }
    }
}
// راس أخطبوط كبير يطالع من بعيد (زينة)
public class GiantOctopus : MonoBehaviour
{
    Transform head;
    readonly List<Transform> pupils = new List<Transform>();

    public static void Create(Transform root, Vector3 pos, GameManager gm)
    {
        var go = new GameObject("Giant Octopus");
        go.transform.SetParent(root, false);
        go.transform.position = pos;
        var o = go.AddComponent<GiantOctopus>();
        Material skin = gm.Mat("Octopus Skin", new Color(0.75f, 0.3f, 0.6f), 0.5f);
        Material spot = gm.Mat("Octopus Spot", new Color(0.6f, 0.2f, 0.5f), 0.4f);
        Material white = gm.Mat("Monster Eye", Color.white, 0.7f);
        Material black = gm.Mat("Monster Pupil", new Color(0.05f, 0.04f, 0.06f), 0.9f);
        o.head = LevelBuilder.Visual("Head", PrimitiveType.Sphere, go.transform, Vector3.zero, new Vector3(7f, 6f, 5f), skin, false).transform;
        for (int i = 0; i < 6; i++)
            LevelBuilder.Visual("Spot", PrimitiveType.Sphere, o.head, new Vector3(-0.25f + i * 0.1f, 0.3f + (i % 2) * 0.08f, -0.3f).normalized * 0.49f, Vector3.one * 0.08f, spot, false);   // على سطح الراس
        for (int s = -1; s <= 1; s += 2)
        {
            LevelBuilder.Visual("Eye", PrimitiveType.Sphere, o.head, new Vector3(s * 0.18f, -0.05f, -0.42f), new Vector3(0.22f, 0.26f, 0.12f), white, false);
            o.pupils.Add(LevelBuilder.Visual("Pupil", PrimitiveType.Sphere, o.head, new Vector3(s * 0.18f, -0.07f, -0.48f), new Vector3(0.1f, 0.14f, 0.06f), black, false).transform);
        }
    }

    void Update()
    {
        float t = Time.time;
        head.localPosition = new Vector3(0f, Mathf.Sin(t * 0.6f) * 0.3f, 0f);
        // العيون تطالع رويدا
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.Rowaida == null) return;
        float dx = Mathf.Clamp((gm.Rowaida.transform.position.x - transform.position.x) * 0.004f, -0.05f, 0.05f);
        for (int i = 0; i < pupils.Count; i++)
            pupils[i].localPosition = new Vector3((i == 0 ? -0.18f : 0.18f) + dx, -0.07f, -0.48f);
    }
}

// ---------- الدوامة ----------

// دوامة بالبحر: أي خط يقرب منها تسحبه وتلفه وتبلعه. الجسر لازم يكون مقوّس فوقها بعيد عنها
public class Whirlpool : MonoBehaviour
{
    float radius, nextScan, nextFoam;
    readonly List<Transform> rings = new List<Transform>();
    readonly List<Transform> debris = new List<Transform>();
    Material foamMat;
    Transform eye;

    Vector3 Center => new Vector3(transform.position.x, SeaWater.Instance != null ? SeaWater.Instance.Level : -0.7f, 0f);

    public static void Create(Transform root, Vector3 spec, GameManager gm)
    {
        var go = new GameObject("Whirlpool");
        go.transform.SetParent(root, false);
        go.transform.position = new Vector3(spec.x, 0f, 0f);
        var w = go.AddComponent<Whirlpool>();
        w.radius = spec.y;
        Material dark = gm.Mat("Whirl Dark", new Color(0.08f, 0.2f, 0.35f), 0.2f);
        Material mid = gm.Mat("Whirl Mid", new Color(0.15f, 0.4f, 0.6f), 0.7f);
        w.foamMat = gm.Mat("Sea Foam", new Color(0.92f, 0.97f, 1f), 0.6f);
        // دوامة مرسومة بالقلم: خطين حلزون (غامق وأبيض) يلفون
        for (int i = 0; i < 2; i++)
        {
            var holder = new GameObject("Spiral Holder").transform;   // يسطّحها شوي بالعمق
            holder.SetParent(go.transform, false);
            holder.localScale = new Vector3(1f, 1f, 0.55f);
            var pts = new List<Vector3>();
            for (float k = 0f; k <= 1f; k += 0.005f)
            {
                float r = w.radius * (1f - k * 0.92f), a = k * Mathf.PI * 2f * 3f + i * Mathf.PI;
                pts.Add(new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r));
            }
            var spiral = SeaWater.DoodleLine("Spiral", holder, pts, i == 0 ? 0.06f : 0.045f, i == 0 ? dark : w.foamMat).transform;
            w.rings.Add(spiral);
        }
        w.eye = LevelBuilder.Visual("Eye", PrimitiveType.Sphere, go.transform, Vector3.zero, new Vector3(w.radius * 0.55f, 0.05f, w.radius * 0.3f), dark, false).transform;
        // خشب وبراميل تلف حولها
        Material wood = gm.Mat("Debris Wood", new Color(0.6f, 0.42f, 0.25f), 0.3f);
        for (int i = 0; i < 4; i++)
        {
            var d = LevelBuilder.Visual(i % 2 == 0 ? "Barrel" : "Plank", i % 2 == 0 ? PrimitiveType.Cylinder : PrimitiveType.Cube, go.transform, Vector3.zero,
                i % 2 == 0 ? new Vector3(0.35f, 0.25f, 0.35f) : new Vector3(0.9f, 0.1f, 0.25f), wood, false).transform;
            w.debris.Add(d);
        }
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time;
        Vector3 c = Center;
        for (int i = 0; i < rings.Count; i++)
        {
            rings[i].parent.position = c + Vector3.up * (0.05f + i * 0.01f);
            rings[i].localRotation = Quaternion.Euler(0f, t * (110f + i * 30f), 0f);   // تلف على سطح المويه
        }
        eye.position = c + Vector3.up * 0.04f;
        for (int i = 0; i < debris.Count; i++)
        {
            float a = t * (1.2f + i * 0.3f) + i * 1.6f;
            float r = radius * (0.85f - 0.15f * Mathf.Repeat(t * 0.2f + i * 0.25f, 1f));
            debris[i].position = c + new Vector3(Mathf.Cos(a) * r, 0.05f, Mathf.Sin(a) * r * 0.45f);
            debris[i].rotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f);
        }
        if (t >= nextFoam)
        {
            nextFoam = t + 0.1f;
            float a = Random.value * Mathf.PI * 2f;
            var f = LevelBuilder.Visual("Foam", PrimitiveType.Sphere, null, c + new Vector3(Mathf.Cos(a) * radius, 0.05f, Mathf.Sin(a) * radius * 0.45f), new Vector3(0.3f, 0.06f, 0.2f), foamMat, false);
            var fx = f.AddComponent<FxParticle>();
            fx.velocity = new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a) * 0.45f) * 2f - new Vector3(Mathf.Cos(a), 0f, 0f) * 0.8f;
            fx.life = 0.8f;
        }

        if (t < nextScan) return;
        nextScan = t + 0.05f;
        // أي خط (أو شكل) قريب: تبلعه
        foreach (InkMark mark in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
        {
            if (mark.GetComponent<SuckedInk>() != null) continue;
            foreach (Collider col in mark.GetComponentsInChildren<Collider>())
            {
                if (!col.enabled) continue;
                Vector3 p = col.ClosestPoint(c);
                p.z = 0f;
                if ((p - c).sqrMagnitude > radius * radius) continue;
                mark.gameObject.AddComponent<SuckedInk>().center = c;
                gm.Sound.PlayRumble(0.3f);
                break;
            }
        }
    }
}

// خط انسحب للدوامة: يلف حولها وينزل تحت المويه لين يختفي
public class SuckedInk : MonoBehaviour
{
    public Vector3 center;
    float age;

    void Start()
    {
        foreach (Collider c in GetComponentsInChildren<Collider>()) c.enabled = false;
        var body = GetComponent<Rigidbody>();
        if (body != null) body.isKinematic = true;
        Fx.Sparkle(center, new Color(0.7f, 0.9f, 1f), 12);
    }

    void Update()
    {
        age += Time.deltaTime;
        // يلف حول مركز الدوامة وينزل
        transform.RotateAround(center, Vector3.up, 360f * Time.deltaTime);
        transform.position += Vector3.down * 2.2f * Time.deltaTime;
        if (age > 1.6f) Destroy(gameObject);
    }
}

// ---------- المنارة: قوارب، قرش، وضو المنارة ----------

// الشكل المغلق بالبحر يصير قارب بشراع. لما تركب رويدا يمشي لليمين لين يصدم صخرة أو جزيرة
public class SeaBoat : MonoBehaviour
{
    public const float Speed = 2.2f;
    public static readonly List<SeaBoat> All = new List<SeaBoat>();
    Rigidbody rb;
    MeshRenderer rend;
    bool floating, sailing, done, sinking;
    float sinkT;
    Transform rig;

    public Bounds Box => rend.bounds;
    public bool Idle => floating && !sailing && !done && !sinking;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rend = GetComponent<MeshRenderer>();
        All.Add(this);
    }

    void OnDestroy() => All.Remove(this);

    float Water => SeaWater.Instance != null ? SeaWater.Instance.Level : -0.7f;

    void FixedUpdate()
    {
        if (rb == null || rend == null || GameManager.Instance == null) return;
        Bounds b = Box;
        if (sinking)
        {
            sinkT += Time.fixedDeltaTime;
            rb.MovePosition(rb.position + Vector3.down * 1.5f * Time.fixedDeltaTime);
            if (sinkT > 1.5f) Destroy(gameObject);
            return;
        }
        if (!floating)
        {
            // أول ما يلمس المويه يصير قارب يطفو
            if (b.min.y > Water + 0.05f) return;
            floating = true;
            rb.isKinematic = true;
            rb.rotation = Quaternion.identity;
            BuildRig(b);
            GameManager.Instance.Sound.PlayPoof();
            Fx.Sparkle(new Vector3(b.center.x, Water, 0f), new Color(0.7f, 0.9f, 1f), 10);
        }
        // يطفو: قاعه فوق المويه بشوي، ويتمايل مع الموج
        // يطفو: نصه بمستوى سطح الصخور (عشان رويدا تدخل وتطلع منه بسهولة)، ويتمايل مع الموج
        float targetY = rb.position.y + (Water + 0.25f + Mathf.Sin(Time.time * 2f) * 0.03f - b.center.y);
        Vector3 p = rb.position;
        p.y = Mathf.Lerp(p.y, targetY, 0.3f);

        GameManager gm = GameManager.Instance;
        RowaidaController rowaida = gm.Rowaida;
        bool riding = rowaida != null && !done && Riding(rowaida, b);
        if (riding && !sailing) { sailing = true; gm.Sound.PlaySqueak(1.3f); }

        if (sailing && !done)
        {
            // يصدم صخرة أو جزيرة قدامه؟ يوقف وتنزل رويدا
            bool blocked = false;
            foreach (RaycastHit h in Physics.RaycastAll(new Vector3(b.max.x - 0.1f, Water + 0.15f, 0f), Vector3.right, 0.35f, ~0, QueryTriggerInteraction.Ignore))
                if (h.collider.GetComponentInParent<InkMark>() == null && h.collider.GetComponentInParent<RowaidaController>() == null) blocked = true;
            if (blocked || (rowaida != null && !riding && Mathf.Abs(rowaida.transform.position.x - b.center.x) > b.extents.x + 1f))
            {
                done = true;
                if (rowaida != null) { rowaida.Held = false; rowaida.CarryX = 0f; }
            }
            else
            {
                p.x += Speed * Time.fixedDeltaTime;
                if (rowaida != null) { rowaida.Held = true; rowaida.CarryX = Speed; }
            }
        }
        rb.MovePosition(p);
    }

    bool Riding(RowaidaController r, Bounds b)
    {
        Vector3 rp = r.transform.position;
        return rp.x > b.min.x + 0.45f && rp.x < b.max.x + 0.2f && rp.y > b.min.y && rp.y < b.max.y + 1.6f;
    }

    // صاري وشراع فوق القارب
    void BuildRig(Bounds b)
    {
        GameManager gm = GameManager.Instance;
        rig = new GameObject("Rig").transform;
        rig.SetParent(transform, true);
        rig.position = new Vector3(b.center.x - b.extents.x * 0.35f, b.center.y, 0.35f);
        Material wood = gm.Mat("Debris Wood", new Color(0.6f, 0.42f, 0.25f), 0.3f);
        Material sail = gm.Mat("Sail", new Color(0.97f, 0.95f, 0.88f), 0.3f);
        Material flag = gm.Mat("Flag", new Color(1f, 0.4f, 0.45f), 0.4f);
        LevelBuilder.Visual("Mast", PrimitiveType.Cylinder, rig, new Vector3(0f, 1.3f, 0f), new Vector3(0.07f, 1.3f, 0.07f), wood);
        var s = LevelBuilder.Visual("Sail", PrimitiveType.Cube, rig, new Vector3(0.45f, 1.55f, 0f), new Vector3(0.8f, 1.5f, 0.03f), sail);
        s.transform.localRotation = Quaternion.Euler(0f, 0f, -8f);
        LevelBuilder.Visual("Flag", PrimitiveType.Cube, rig, new Vector3(0.18f, 2.65f, 0f), new Vector3(0.3f, 0.16f, 0.02f), flag, false);
    }

    // القرش عضه: يغرق
    public void Sink()
    {
        if (sinking) return;
        sinking = true;
        var rowaida = GameManager.Instance.Rowaida;
        if (rowaida != null && sailing && !done) { rowaida.Held = false; rowaida.CarryX = 0f; }
        foreach (Collider c in GetComponentsInChildren<Collider>()) c.enabled = false;
        GameManager.Instance.Sound.PlayChomp();
        Fx.Sparkle(Box.center, new Color(0.7f, 0.9f, 1f), 18);
    }

    public bool Sailing => sailing && !done && !sinking;
}

// رويدا تستنى على طرف الصخرة/الجزيرة لين يصير فيه قارب جنبها
public class BoatDock : MonoBehaviour
{
    bool holding;

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.Rowaida == null) return;
        RowaidaController r = gm.Rowaida;
        Vector3 p = r.transform.position;
        bool wait = false;
        if (gm.IsPlaying && !SeaBoatCarrying())
        {
            // واقفة على أرض (مو حبر) والأرض تخلص قدامها؟
            bool onGround = Physics.Raycast(p, Vector3.down, out RaycastHit hit, 1.2f, ~0, QueryTriggerInteraction.Ignore) && hit.collider.GetComponentInParent<InkMark>() == null && hit.collider.GetComponentInParent<RowaidaController>() == null;
            bool groundAhead = Physics.Raycast(p + Vector3.right * 1.1f, Vector3.down, 1.6f, ~0, QueryTriggerInteraction.Ignore);
            if (onGround && !groundAhead)
            {
                wait = true;
                foreach (SeaBoat boat in SeaBoat.All)
                    if (boat != null && boat.Idle && boat.Box.min.x < p.x + 2.2f && boat.Box.max.x > p.x + 1f) wait = false;
            }
        }
        if (wait) { r.Held = true; r.CarryX = 0f; holding = true; }
        else if (holding) { r.Held = false; holding = false; }
    }

    static bool SeaBoatCarrying()
    {
        foreach (SeaBoat b in SeaBoat.All) if (b != null && b.Sailing) return true;
        return false;
    }
}

// المنارة: برج مخطط وفوقه ضو يلف ويمسح البحر
public class Lighthouse : MonoBehaviour
{
    public static Lighthouse Instance;
    const float Period = 7f;
    float minX, maxX;
    Transform beam, lamp;
    Light spot;
    Vector3 top;

    // وين ضو المنارة يلمس البحر الحين
    public float BeamX => Mathf.Lerp(minX, maxX, 0.5f + 0.5f * Mathf.Sin(Time.time * Mathf.PI * 2f / Period));

    public static void Create(Transform root, Vector3 spec, GameManager gm)
    {
        var go = new GameObject("Lighthouse");
        go.transform.SetParent(root, false);
        go.transform.position = new Vector3(spec.x, spec.y, 0.8f);
        var l = go.AddComponent<Lighthouse>();
        Instance = l;
        l.minX = spec.z - 22f; l.maxX = spec.z;
        Material white = gm.Mat("Tower White", new Color(0.95f, 0.94f, 0.9f), 0.3f);
        Material red = gm.Mat("Tower Red", new Color(0.85f, 0.2f, 0.22f), 0.35f);
        Material dark = gm.Mat("Tower Dark", new Color(0.15f, 0.15f, 0.18f), 0.5f);
        var glass = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "Lamp" };
        glass.SetColor("_BaseColor", new Color(3f, 2.7f, 1.5f));
        for (int i = 0; i < 6; i++)
            LevelBuilder.Visual("Stripe", PrimitiveType.Cylinder, go.transform, new Vector3(0f, 0.6f + i * 1.1f, 0f), new Vector3(1.7f - i * 0.1f, 0.55f, 1.7f - i * 0.1f), i % 2 == 0 ? white : red);
        LevelBuilder.Visual("Balcony", PrimitiveType.Cylinder, go.transform, new Vector3(0f, 7.1f, 0f), new Vector3(1.6f, 0.1f, 1.6f), dark);
        l.lamp = LevelBuilder.Visual("Lamp", PrimitiveType.Sphere, go.transform, new Vector3(0f, 7.7f, 0f), Vector3.one * 0.9f, glass, false).transform;
        LevelBuilder.Visual("Roof", PrimitiveType.Cylinder, go.transform, new Vector3(0f, 8.35f, 0f), new Vector3(1.2f, 0.25f, 1.2f), red);
        LevelBuilder.Visual("Door", PrimitiveType.Cube, go.transform, new Vector3(0f, 0.55f, -0.8f), new Vector3(0.6f, 1.1f, 0.1f), dark, false);
        l.top = go.transform.position + Vector3.up * 7.7f;
        // شعاع: مربع شفاف طويل من اللمبة للبحر
        // نسيج للشعاع: يخف من الجنبين بس (مو من الأطراف)
        var tex = new Texture2D(4, 32, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < 32; y++)
        {
            float a = Mathf.SmoothStep(0f, 1f, 1f - Mathf.Abs(y / 31f - 0.5f) * 2f);
            for (int x = 0; x < 4; x++) tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();
        var beamMat = NightSky.Transparent("Beam", tex, new Color(1.6f, 1.45f, 0.8f, 0.3f));
        l.beam = LevelBuilder.Visual("Beam", PrimitiveType.Quad, root, Vector3.zero, Vector3.one, beamMat, false).transform;
        l.spot = new GameObject("Beam Light").AddComponent<Light>();
        l.spot.transform.SetParent(root, false);
        l.spot.type = LightType.Point;
        l.spot.color = new Color(1f, 0.92f, 0.6f);
        l.spot.range = 7f;
        l.spot.intensity = 3f;
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void Update()
    {
        float water = SeaWater.Instance != null ? SeaWater.Instance.Level : -0.7f;
        Vector3 hit = new Vector3(BeamX, water, 0f);
        Vector3 d = hit - top;
        beam.position = (top + hit) * 0.5f + new Vector3(0f, 0f, -0.2f);
        beam.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        beam.localScale = new Vector3(d.magnitude * 1.1f, 3.2f, 1f);
        spot.transform.position = hit + Vector3.up * 1.5f;
        lamp.localScale = Vector3.one * (0.9f + Mathf.Sin(Time.time * 6f) * 0.05f);
    }
}

// قرش (زعنفة بس تبان): يسبح رايح جاي، ويعض أي قارب يمر جنبه. يخاف من ضو المنارة ويغطس
public class Shark : MonoBehaviour
{
    float from, to, dir = 1f, down;
    Transform fin;

    public static void Create(Transform root, Vector3 spec, GameManager gm)
    {
        var go = new GameObject("Shark");
        go.transform.SetParent(root, false);
        go.transform.position = new Vector3((spec.x + spec.y) * 0.5f, 0f, -0.3f);
        var s = go.AddComponent<Shark>();
        s.from = spec.x; s.to = spec.y;
        Material grey = gm.Mat("Shark", new Color(0.6f, 0.66f, 0.75f), 0.6f);
        s.fin = new GameObject("Fin").transform;
        s.fin.SetParent(go.transform, false);
        var f = LevelBuilder.Visual("Fin", PrimitiveType.Cube, s.fin, new Vector3(0f, 0.5f, 0f), new Vector3(0.8f, 0.8f, 0.1f), grey).transform;
        f.localRotation = Quaternion.Euler(0f, 0f, 45f);
        LevelBuilder.Visual("Back", PrimitiveType.Sphere, s.fin, new Vector3(0f, -0.05f, 0f), new Vector3(1.6f, 0.35f, 0.5f), grey);
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        float water = SeaWater.Instance != null ? SeaWater.Instance.Level : -0.7f;
        Vector3 p = transform.position;
        p.x += dir * 2f * Time.deltaTime;
        if (p.x > to) dir = -1f;
        if (p.x < from) dir = 1f;
        fin.localRotation = Quaternion.Euler(0f, dir > 0 ? 0f : 180f, 0f);

        // ضو المنارة عليه؟ يغطس شوي
        bool lit = Lighthouse.Instance != null && Mathf.Abs(Lighthouse.Instance.BeamX - p.x) < 2.5f;
        down = lit ? 1.6f : Mathf.Max(0f, down - Time.deltaTime);
        float depth = down > 0f ? -1.2f : 0f;
        p.y = Mathf.Lerp(p.y, water + depth, Time.deltaTime * 5f);
        transform.position = p;

        // قارب ماشي جنبه وهو طالع؟ يعضه
        if (down > 0f || gm == null) return;
        foreach (SeaBoat boat in SeaBoat.All)
        {
            if (boat == null || !boat.Sailing) continue;
            Bounds b = boat.Box;
            if (p.x > b.min.x - 0.6f && p.x < b.max.x + 0.6f) boat.Sink();
        }
    }
}

// ---------- عشان اللعب يكون واضح ----------

// رويدا تستنى عند كل حفرة لين ترسمين جسر (وتطلع جملة تقول وش المطلوب)
public class GapWait : MonoBehaviour
{
    LevelData data;
    bool holding;
    int lastPrompt = -1;

    public static void Create(Transform root, LevelData data)
    {
        var g = new GameObject("Gap Wait").AddComponent<GapWait>();
        g.transform.SetParent(root, false);
        g.data = data;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.Rowaida == null) return;
        RowaidaController r = gm.Rowaida;
        Vector3 p = r.transform.position;
        bool wait = false;
        var blocks = data.blocks;
        for (int i = 0; i < blocks.Count - 1 && gm.IsPlaying; i++)
        {
            Block a = blocks[i], b = blocks[i + 1];
            float x0 = a.center.x + a.size.x * 0.5f, x1 = b.center.x - b.size.x * 0.5f, top = a.center.y + a.size.y * 0.5f;
            if (x1 - x0 < 1f) continue;
            if (p.x < x0 - 1.6f || p.x > x0 - 0.5f || p.y < top) continue;
            if (RowaidaController.BridgeCovers(x0, x1, top)) continue;
            wait = true;
            if (lastPrompt != i) { lastPrompt = i; gm.UI.ShowMessage("", 2.2f, "ارسمي جسر عشان تعدي"); }
        }
        if (wait) { r.Held = true; r.CarryX = 0f; holding = true; }
        else if (holding) { r.Held = false; holding = false; }
    }
}

// المد والجزر: رويدا تستنى على الأرض العالية إذا المويه بيغطي الرمل قبل ما تعدي،
// إلا لو فيه ممشى مرسوم فوق الرمل
public class TideWait : MonoBehaviour
{
    LevelData data;
    bool holding;
    int lastPrompt = -1;

    // فيه ممشى فوق الرمل؟ (ولو كان مرسوم بأكثر من خط، المهم يغطي الرمل كله فوق المويه)
    static bool WalkwayCovers(float x0, float x1, float minY)
    {
        var parts = new List<Vector2>();
        foreach (InkMark m in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
        {
            if (m.closed) continue;
            var r = m.GetComponent<MeshRenderer>();
            if (r == null) continue;
            Bounds b = r.bounds;
            if (b.center.y < minY || b.center.y > minY + 3f) continue;
            parts.Add(new Vector2(b.min.x, b.max.x));
        }
        parts.Sort((a, b) => a.x.CompareTo(b.x));
        float reach = x0 + 0.8f;   // لازم يبدا قريب من الطرف
        foreach (Vector2 p in parts)
        {
            if (p.x > reach + 0.5f) break;   // فيه فراغ
            reach = Mathf.Max(reach, p.y);
        }
        return reach >= x1 - 0.8f;
    }

    public static void Create(Transform root, LevelData data)
    {
        var t = new GameObject("Tide Wait").AddComponent<TideWait>();
        t.transform.SetParent(root, false);
        t.data = data;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.Rowaida == null || SeaWater.Instance == null) return;
        RowaidaController r = gm.Rowaida;
        Vector3 p = r.transform.position;
        bool wait = false;
        for (int i = 0; i < data.tideBars.Count && gm.IsPlaying; i++)
        {
            Vector3 bar = data.tideBars[i];
            if (p.x < bar.x - 1.4f || p.x > bar.x - 0.4f || p.y < bar.z + 1f) continue;
            if (WalkwayCovers(bar.x, bar.y, bar.z + 0.4f)) continue;
            // وهي تستنى: الكاميرا ترجع لورا عشان يبان الرمل كله
            CameraFollow.PullRequest = Mathf.Max(CameraFollow.PullRequest, 1f);
            // تلحق تعدي قبل ما يطلع المويه؟
            float cross = (bar.y - bar.x + 0.8f) / r.walkSpeed;
            foreach (Vector3 c in data.crabs) if (c.x < bar.y && c.y > bar.x) cross += 1.5f;   // السرطان يأخرها شوي
            bool safe = true;
            for (float t = 0f; t <= cross; t += 0.1f)
                if (SeaWater.Instance.LevelAt(Time.time + t) > bar.z + 0.4f) { safe = false; break; }
            if (safe) continue;
            wait = true;
            if (lastPrompt != i)
            {
                lastPrompt = i;
                gm.UI.ShowMessage("", 2.5f, cross > 5f ? "الرمل طويل! ارسمي ممشى فوق المويه" : "استني الجزر... أو ارسمي ممشى");
            }
        }
        if (wait) { r.Held = true; r.CarryX = 0f; holding = true; }
        else if (holding) { r.Held = false; holding = false; }
    }
}

// مسطرة مدرسية مغروسة بالبحر: تبين وين وصل المويه، والخط الأحمر = الرمل يغرق
public class TideRuler : MonoBehaviour
{
    public static void Create(Transform root, float x, float dangerY, GameManager gm)
    {
        var go = new GameObject("Tide Ruler");
        go.transform.SetParent(root, false);
        go.transform.position = new Vector3(x, 0f, 1.2f);
        Material wood = gm.Mat("Ruler Wood", new Color(1f, 0.85f, 0.35f), 0.3f);
        Material tick = gm.Mat("Ruler Tick", new Color(0.15f, 0.12f, 0.1f), 0.3f);
        Material red = gm.Mat("Ruler Red", new Color(0.9f, 0.15f, 0.2f), 0.3f);
        LevelBuilder.Visual("Ruler", PrimitiveType.Cube, go.transform, new Vector3(0f, 0f, 0f), new Vector3(0.55f, 5f, 0.08f), wood);
        for (int i = 0; i < 20; i++)
        {
            float y = -2.4f + i * 0.25f;
            LevelBuilder.Visual("Tick", PrimitiveType.Cube, go.transform, new Vector3(-0.17f + (i % 2 == 0 ? 0.05f : 0f), y, -0.05f), new Vector3(i % 2 == 0 ? 0.22f : 0.12f, 0.03f, 0.02f), tick, false);
        }
        LevelBuilder.Visual("Danger", PrimitiveType.Cube, go.transform, new Vector3(0f, dangerY, -0.06f), new Vector3(0.6f, 0.07f, 0.02f), red, false);
    }
}
