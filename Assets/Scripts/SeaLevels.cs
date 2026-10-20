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
