using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// وحش شخابيط: رسمة قديمة ما كملت وصحت. يتمشى على صفحته، ياكل الجسور القريبة،
// ويسكر الطريق على رويدا. اللاعب يشخبط عليه عشان يمسحه: كل ما شخبط أكثر يبهت ويصغر لين يختفي.
public class ScribbleMonster : MonoBehaviour
{
    public enum Kind { PurpleBlob, FuzzyBall, GreenEye, SnowChaser, SnowGuard, SnowThrower }
    enum State { Wander, EatInk }

    const float Radius = 0.45f;
    const float WanderSpeed = 1.1f, ChaseSpeed = 1.8f, HuntSpeed = 2.1f;
    const float SightRange = 7f;            // من وين يشوف رويدا ويركض لها
    const float SnowChaseSpeed = 2.4f;      // رجل الثلج أبطأ من رويدا: يمسكها بس إذا وقفت أو انصدت
    bool hunting;
    float chaseFrom;

    // ---------- رجل الثلج اللي يرمي ----------

    const float ThrowRange = 10f, ThrowEvery = 1.05f, FlightTime = 0.9f;
    float nextThrow, throwAnim;

    // يرمي كرة ثلج على المكان اللي بتوصله رويدا (ترسمين درع فوقها يصد الكور)
    void UpdateThrowing()
    {
        if (throwAnim > 0f)
        {
            throwAnim -= Time.deltaTime * 3f;
            visual.localRotation = Quaternion.Euler(0f, 180f, Mathf.Sin(Mathf.Clamp01(throwAnim) * Mathf.PI) * -25f);
        }
        GameManager gm = GameManager.Instance;
        if (gm == null || !gm.IsPlaying || gm.Rowaida == null || Stunned) return;
        Vector3 r = gm.Rowaida.transform.position;
        float dx = transform.position.x - r.x;
        if (dx < 1.2f || dx > ThrowRange) { nextThrow = Mathf.Max(nextThrow, Time.time + 0.5f); return; }
        if (Time.time < nextThrow) return;
        nextThrow = Time.time + ThrowEvery;

        Vector3 from = transform.position + new Vector3(-0.55f, 1.1f, 0f);
        float vx = gm.Rowaida.GetComponent<Rigidbody>().linearVelocity.x;
        Vector3 target = r + new Vector3(vx * FlightTime, 0.25f, 0f);
        Vector3 v = (target - from - 0.5f * Physics.gravity * FlightTime * FlightTime) / FlightTime;
        v.z = 0f;
        Snowball.Throw(from, v, GetComponent<Collider>());
        gm.Sound.PlaySqueak(Random.Range(1.6f, 1.9f));
        throwAnim = 1f;
    }

    // ---------- رجل الثلج اللي يلحق ----------

    // يمسح (يجمد ويكسر) الحبر اللي عدّاه وصار وراه
    void FreezeInkBehind()
    {
        Vector3 p = transform.position;
        foreach (InkMark mark in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
        {
            var r = mark.GetComponent<MeshRenderer>();
            if (r == null) continue;
            Bounds b = r.bounds;
            bool behind = dir > 0 ? b.max.x < p.x - 0.6f : b.min.x > p.x + 0.6f;
            // بس الحبر اللي مشى عليه أو قريب منه (مو كل رسمة في المرحلة)
            if (behind && Mathf.Abs(b.center.y - p.y) < 2.5f && Mathf.Abs(b.center.x - p.x) < 4f) Shatter(mark);
        }
    }

    void Shatter(InkMark mark)
    {
        Bounds b = mark.GetComponent<MeshRenderer>().bounds;
        Material ice = GameManager.Instance.Mat("Ice Shard", new Color(0.75f, 0.9f, 1f), 0.9f);
        int n = Mathf.Clamp(Mathf.RoundToInt(b.size.x * 4f), 4, 14);
        for (int i = 0; i < n; i++)
        {
            Vector3 q = new Vector3(Random.Range(b.min.x, b.max.x), Random.Range(b.min.y, b.max.y), 0f);
            var shard = LevelBuilder.Visual("Ice Shard", PrimitiveType.Cube, null, q, Vector3.one * Random.Range(0.06f, 0.12f), ice, false);
            shard.transform.rotation = Random.rotation;
            shard.AddComponent<PoofPuff>().shrinkOnly = true;
        }
        GameManager.Instance.Sound.PlayPoof();
        Destroy(mark.gameObject);
    }

    static void SnowPuff(Vector3 pos)
    {
        Material snow = GameManager.Instance.Mat("Snow Puff", new Color(0.97f, 0.98f, 1f), 0.2f);
        var puff = LevelBuilder.Visual("Snow Puff", PrimitiveType.Sphere, null, pos, Vector3.one * Random.Range(0.15f, 0.3f), snow, false);
        puff.AddComponent<PoofPuff>();
    }

    // رويدا خسرت قلب: يرجع وراها شوي ويصحى بعد ثانيتين
    void OnRespawned(Vector3 pos)
    {
        if (dead) return;
        Vector3 back = pos + new Vector3(-1.6f, 0f, 0f);
        rb.position = back;
        transform.position = back;
        rb.linearVelocity = Vector3.zero;
        chaseFrom = Time.time + 2.2f;
        hunting = false;
    }

    void OnDestroy()
    {
        if (kind == Kind.SnowChaser && GameManager.Instance != null) GameManager.Instance.Respawned -= OnRespawned;
    }
    const float FoodSight = 3.5f;
    const float EraseNeeded = 3.2f;         // كم متر شخبطة لازم عشان ينمسح
    public const float EraseReach = 0.75f;  // الشخبطة لازم تكون قريبة من الوحش بهالمسافة

    Kind kind;
    float patrolMin, patrolMax;
    int dir = 1;
    State state = State.Wander;
    Rigidbody rb;
    Transform visual, mouth, hint;
    readonly List<Transform> pupils = new List<Transform>();
    float nextThink, stateTime, nextSqueak, nextChomp;
    InkMark target;
    float phase;
    bool dead;

    // المسح بالشخبطة
    float erased;          // من 0 لـ 1
    float stunnedUntil;
    readonly List<Renderer> renderers = new List<Renderer>();
    readonly List<Color> baseColors = new List<Color>();
    MaterialPropertyBlock block;

    static PhysicsMaterial slippery;

    // ---------- البناء ----------

    public static ScribbleMonster Spawn(Transform parent, Kind kind, Vector3 groundPos, float patrolMin, float patrolMax, GameManager gm, bool showHint = false)
    {
        var go = new GameObject("Scribble Monster (" + kind + ")");
        go.transform.SetParent(parent, false);
        // في الصفحة المقلوبة الوحش يمشي على السقف: تحت السطح ومقلوب
        bool flipped = Physics.gravity.y > 0f;
        go.transform.position = groundPos + (flipped ? Vector3.down : Vector3.up) * Radius;
        if (flipped) go.transform.rotation = Quaternion.Euler(0f, 0f, 180f);

        if (slippery == null)
            slippery = new PhysicsMaterial("Monster") { dynamicFriction = 0f, staticFriction = 0f, frictionCombine = PhysicsMaterialCombine.Minimum };
        var col = go.AddComponent<SphereCollider>();
        col.radius = Radius;
        col.sharedMaterial = slippery;
        var rb = go.AddComponent<Rigidbody>();
        rb.mass = 2f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;

        var m = go.AddComponent<ScribbleMonster>();
        m.kind = kind;
        m.rb = rb;
        m.patrolMin = patrolMin;
        m.patrolMax = patrolMax;
        m.dir = Random.value < 0.5f ? -1 : 1;
        m.phase = Random.value * 10f;
        m.BuildVisual(gm);
        if (kind == Kind.SnowChaser)
        {
            m.chaseFrom = Time.time + 2.5f;   // يعطيها فرصة تمشي شوي أول
            gm.Respawned += m.OnRespawned;
        }
        m.CollectRenderers();
        if (showHint) m.BuildHint(gm);
        return m;
    }

    void BuildVisual(GameManager gm)
    {
        visual = new GameObject("Visual").transform;
        visual.SetParent(transform, false);
        Material white = gm.Mat("Monster Eye", Color.white, 0.7f);
        Material black = gm.Mat("Monster Pupil", new Color(0.05f, 0.04f, 0.06f), 0.9f);
        Material mouthMat = gm.Mat("Monster Mouth", new Color(0.35f, 0.08f, 0.15f), 0.4f);

        switch (kind)
        {
            case Kind.SnowChaser:
            case Kind.SnowGuard:
            case Kind.SnowThrower:
                BuildSnowman(gm, white, black);
                break;
            case Kind.PurpleBlob:
            {
                Material body = gm.Mat("Monster Purple", new Color(0.62f, 0.45f, 0.92f), 0.35f);
                Material belly = gm.Mat("Monster Purple Belly", new Color(0.8f, 0.7f, 1f), 0.3f);
                Part(PrimitiveType.Sphere, new Vector3(0f, 0f, 0f), new Vector3(1f, 0.9f, 0.9f), body, true);
                Part(PrimitiveType.Sphere, new Vector3(0f, -0.12f, 0.3f), new Vector3(0.55f, 0.45f, 0.3f), belly);
                Eyes(white, black, 0.17f, 0.17f, 0.36f, 0.27f);
                for (int s = -1; s <= 1; s += 2)
                {
                    var stalk = Part(PrimitiveType.Cylinder, new Vector3(s * 0.18f, 0.5f, 0f), new Vector3(0.04f, 0.12f, 0.04f), body);
                    stalk.localRotation = Quaternion.Euler(0f, 0f, -s * 18f);
                    Part(PrimitiveType.Sphere, new Vector3(s * 0.23f, 0.63f, 0f), Vector3.one * 0.11f, belly);
                    Part(PrimitiveType.Sphere, new Vector3(s * 0.22f, -0.42f, 0.05f), new Vector3(0.22f, 0.12f, 0.26f), body);
                }
                mouth = Part(PrimitiveType.Sphere, new Vector3(0f, -0.1f, 0.42f), new Vector3(0.3f, 0.09f, 0.06f), mouthMat);
                for (int s = -1; s <= 1; s += 2)
                    Part(PrimitiveType.Cube, new Vector3(s * 0.06f, -0.07f, 0.45f), new Vector3(0.05f, 0.05f, 0.02f), white);
                break;
            }
            case Kind.FuzzyBall:
            {
                Material body = gm.Mat("Monster Fuzz", new Color(0.13f, 0.12f, 0.16f), 0.2f);
                Material horn = gm.Mat("Monster Horn", new Color(0.95f, 0.85f, 0.6f), 0.4f);
                Part(PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.86f, body, true);
                var rng = new System.Random(3);
                for (int i = 0; i < 46; i++)
                {
                    Vector3 d = new Vector3((float)rng.NextDouble() * 2f - 1f, (float)rng.NextDouble() * 2f - 1f, (float)rng.NextDouble() * 2f - 1f).normalized;
                    if (d.z > 0.55f && d.y > -0.4f) continue; // نخلي الوجه نظيف
                    Part(PrimitiveType.Sphere, d * 0.42f, Vector3.one * (0.12f + (float)rng.NextDouble() * 0.08f), body);
                }
                Eyes(white, black, 0.16f, 0.12f, 0.36f, 0.3f);
                for (int s = -1; s <= 1; s += 2)
                {
                    var h = Part(PrimitiveType.Capsule, new Vector3(s * 0.22f, 0.43f, 0f), new Vector3(0.07f, 0.1f, 0.07f), horn);
                    h.localRotation = Quaternion.Euler(0f, 0f, -s * 25f);
                }
                mouth = Part(PrimitiveType.Sphere, new Vector3(0f, -0.14f, 0.38f), new Vector3(0.36f, 0.12f, 0.06f), mouthMat);
                for (int t = -2; t <= 2; t++)
                {
                    var tooth = Part(PrimitiveType.Cube, new Vector3(t * 0.06f, -0.09f, 0.41f), new Vector3(0.045f, 0.045f, 0.02f), white);
                    tooth.localRotation = Quaternion.Euler(0f, 0f, 45f);
                }
                break;
            }
            default:
            {
                Material body = gm.Mat("Monster Green", new Color(0.45f, 0.85f, 0.42f), 0.35f);
                Material spots = gm.Mat("Monster Green Spots", new Color(0.3f, 0.65f, 0.3f), 0.3f);
                Part(PrimitiveType.Capsule, new Vector3(0f, 0.02f, 0f), new Vector3(0.7f, 0.46f, 0.65f), body, true);
                Part(PrimitiveType.Sphere, new Vector3(-0.22f, 0.25f, -0.15f), Vector3.one * 0.12f, spots);
                Part(PrimitiveType.Sphere, new Vector3(0.25f, -0.1f, -0.2f), Vector3.one * 0.09f, spots);
                Part(PrimitiveType.Sphere, new Vector3(0f, 0.16f, 0.27f), Vector3.one * 0.34f, white);
                pupils.Add(Part(PrimitiveType.Sphere, new Vector3(0f, 0.16f, 0.43f), Vector3.one * 0.15f, black));
                for (int s = -1; s <= 1; s += 2)
                {
                    var arm = Part(PrimitiveType.Capsule, new Vector3(s * 0.38f, -0.05f, 0.05f), new Vector3(0.09f, 0.16f, 0.09f), body);
                    arm.localRotation = Quaternion.Euler(0f, 0f, s * 35f);
                    var antenna = Part(PrimitiveType.Cylinder, new Vector3(s * 0.12f, 0.5f, 0f), new Vector3(0.03f, 0.1f, 0.03f), spots);
                    antenna.localRotation = Quaternion.Euler(0f, 0f, -s * 20f);
                }
                mouth = Part(PrimitiveType.Sphere, new Vector3(0f, -0.16f, 0.31f), new Vector3(0.22f, 0.07f, 0.05f), mouthMat);
                break;
            }
        }
    }

    // رجل ثلج: ثلاث كور ثلج، عيون فحم، أنف جزر، يدين أعواد، وشال.
    // اللي يلحق: قبعة سودا وحواجب غاضبة. الحارس: طاقية حمرا
    void BuildSnowman(GameManager gm, Material white, Material black)
    {
        bool chaser = kind == Kind.SnowChaser;
        Material snow = gm.Mat("Snow Body", new Color(0.96f, 0.98f, 1f), 0.35f);
        Material coal = gm.Mat("Coal", new Color(0.08f, 0.08f, 0.1f), 0.6f);
        Material carrot = gm.Mat("Carrot", new Color(1f, 0.5f, 0.15f), 0.4f);
        Material stick = gm.Mat("Stick", new Color(0.4f, 0.26f, 0.14f), 0.2f);
        Material scarf = chaser ? gm.Mat("Scarf Blue", new Color(0.25f, 0.45f, 0.85f), 0.3f) : gm.Mat("Scarf Green", new Color(0.3f, 0.7f, 0.4f), 0.3f);

        Part(PrimitiveType.Sphere, new Vector3(0f, 0.03f, 0f), new Vector3(1f, 0.92f, 0.95f), snow, true);
        Part(PrimitiveType.Sphere, new Vector3(0f, 0.62f, 0f), Vector3.one * 0.72f, snow, true);
        Part(PrimitiveType.Sphere, new Vector3(0f, 1.1f, 0f), Vector3.one * 0.52f, snow, true);

        // عيون فحم (تتحرك وتطالع) وأنف جزر وابتسامة نقاط
        for (int s = -1; s <= 1; s += 2)
            pupils.Add(Part(PrimitiveType.Sphere, new Vector3(s * 0.1f, 1.17f, 0.22f), Vector3.one * 0.08f, coal));
        var nose = Part(PrimitiveType.Capsule, new Vector3(0f, 1.09f, 0.33f), new Vector3(0.07f, 0.14f, 0.07f), carrot);
        nose.localRotation = Quaternion.Euler(90f, 0f, 0f);
        for (int i = -2; i <= 2; i++)
            Part(PrimitiveType.Sphere, new Vector3(i * 0.055f, 0.99f - (2 - Mathf.Abs(i)) * 0.02f, 0.23f), Vector3.one * 0.04f, coal);
        mouth = Part(PrimitiveType.Sphere, new Vector3(0f, 0.97f, 0.2f), new Vector3(0.12f, 0.02f, 0.02f), coal);

        // أزرار فحم
        for (int i = 0; i < 3; i++) Part(PrimitiveType.Sphere, new Vector3(0f, 0.75f - i * 0.13f, 0.34f - Mathf.Abs(i - 1) * 0.02f), Vector3.one * 0.07f, coal);

        // يدين أعواد
        for (int s = -1; s <= 1; s += 2)
        {
            var arm = Part(PrimitiveType.Cylinder, new Vector3(s * 0.5f, 0.75f, 0f), new Vector3(0.035f, 0.26f, 0.035f), stick);
            arm.localRotation = Quaternion.Euler(0f, 0f, s * (chaser ? 110f : 60f));
            var twig = Part(PrimitiveType.Cylinder, new Vector3(s * 0.68f, chaser ? 0.8f : 0.92f, 0f), new Vector3(0.025f, 0.08f, 0.025f), stick);
            twig.localRotation = Quaternion.Euler(0f, 0f, s * 20f);
        }

        // شال حول الرقبة وطرفه نازل
        Part(PrimitiveType.Cylinder, new Vector3(0f, 0.88f, 0f), new Vector3(0.52f, 0.05f, 0.52f), scarf);
        var tail = Part(PrimitiveType.Cube, new Vector3(0.14f, 0.74f, 0.27f), new Vector3(0.1f, 0.26f, 0.04f), scarf);
        tail.localRotation = Quaternion.Euler(0f, 0f, -12f);

        if (chaser)
        {
            Material hat = gm.Mat("Top Hat", new Color(0.1f, 0.1f, 0.12f), 0.5f);
            Part(PrimitiveType.Cylinder, new Vector3(0f, 1.34f, 0f), new Vector3(0.5f, 0.02f, 0.5f), hat);
            Part(PrimitiveType.Cylinder, new Vector3(0f, 1.47f, 0f), new Vector3(0.32f, 0.13f, 0.32f), hat);
            Part(PrimitiveType.Cylinder, new Vector3(0f, 1.38f, 0f), new Vector3(0.33f, 0.03f, 0.33f), scarf);
            for (int s = -1; s <= 1; s += 2)
            {
                var brow = Part(PrimitiveType.Cube, new Vector3(s * 0.1f, 1.25f, 0.22f), new Vector3(0.12f, 0.025f, 0.02f), coal);
                brow.localRotation = Quaternion.Euler(0f, 0f, -s * 22f);
            }
        }
        else
        {
            Material beanie = kind == Kind.SnowThrower ? gm.Mat("Beanie Blue", new Color(0.3f, 0.5f, 0.95f), 0.3f) : gm.Mat("Beanie", new Color(0.9f, 0.25f, 0.3f), 0.3f);
            var cap = Part(PrimitiveType.Sphere, new Vector3(0f, 1.26f, 0f), new Vector3(0.5f, 0.3f, 0.5f), beanie);
            Part(PrimitiveType.Sphere, new Vector3(0f, 1.43f, 0f), Vector3.one * 0.13f, white);
            Part(PrimitiveType.Cylinder, new Vector3(0f, 1.2f, 0f), new Vector3(0.53f, 0.04f, 0.53f), white);
        }
    }

    public bool IsSnowman => kind == Kind.SnowChaser || kind == Kind.SnowGuard || kind == Kind.SnowThrower;

    // وين تنحسب الشخبطة (رجل الثلج أطول فنحسب من نصه)
    public Vector3 EraseCenter => transform.position + transform.up * (IsSnowman ? 0.5f : 0f);
    public float EraseRadius => IsSnowman ? 1f : EraseReach;

    void Eyes(Material white, Material black, float x, float y, float z, float size)
    {
        for (int s = -1; s <= 1; s += 2)
        {
            Part(PrimitiveType.Sphere, new Vector3(s * x, y, z), Vector3.one * size, white);
            pupils.Add(Part(PrimitiveType.Sphere, new Vector3(s * x, y, z + size * 0.42f), Vector3.one * size * 0.45f, black));
        }
    }

    // جزء من الشكل. لو outline = true نضيف له حدود سودا كرتونية (نسخة أكبر شوي من ورا)
    Transform Part(PrimitiveType type, Vector3 pos, Vector3 scale, Material mat, bool outline = false)
    {
        var go = LevelBuilder.Visual("Part", type, visual, pos, scale, mat);
        if (outline)
        {
            var o = LevelBuilder.Visual("Outline", type, go.transform, Vector3.zero, Vector3.one * 1.07f, OutlineMat(), false);
            o.GetComponent<MeshRenderer>().receiveShadows = false;
        }
        return go.transform;
    }

    static Material outlineMat;
    static Material OutlineMat()
    {
        if (outlineMat == null)
        {
            outlineMat = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "Scribble Outline" };
            outlineMat.SetColor("_BaseColor", new Color(0.07f, 0.05f, 0.1f));
            outlineMat.SetFloat("_Cull", (float)CullMode.Front);
        }
        return outlineMat;
    }

    void CollectRenderers()
    {
        block = new MaterialPropertyBlock();
        foreach (Renderer r in visual.GetComponentsInChildren<Renderer>())
        {
            if (r.sharedMaterial == OutlineMat()) continue;
            renderers.Add(r);
            baseColors.Add(r.sharedMaterial.GetColor("_BaseColor"));
        }
    }

    // تلميح: شخبطة منقطة فوق أول وحش توري اللاعب وش يسوي
    void BuildHint(GameManager gm)
    {
        hint = new GameObject("Scribble Hint").transform;
        hint.SetParent(transform, false);
        Material mat = gm.Mat("Hint", new Color(0.55f, 0.55f, 0.62f), 0.2f);
        var pts = new List<Vector3>();
        for (int i = 0; i <= 6; i++)
            pts.Add(new Vector3(-0.45f + i * 0.15f, i % 2 == 0 ? 0.28f : -0.28f, 0f));
        // متقطعة: كل جزء لحاله
        for (int i = 0; i < pts.Count - 1; i++)
        {
            var go = new GameObject("Hint Dash");
            go.transform.SetParent(hint, false);
            var mesh = new Mesh();
            Vector3 a = pts[i], b = pts[i + 1];
            TubeMesh.Build(mesh, new List<Vector3> { Vector3.Lerp(a, b, 0.1f), Vector3.Lerp(a, b, 0.75f) }, 0.035f, false, 6);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        }
    }

    // ---------- التفكير والحركة ----------

    void Update()
    {
        if (dead) return;
        stateTime += Time.deltaTime;
        if (Time.time >= nextThink)
        {
            nextThink = Time.time + 0.25f;
            Think();
        }

        if (state == State.EatInk) UpdateEating();
        if (kind == Kind.SnowChaser && hunting) FreezeInkBehind();
        if (kind == Kind.SnowThrower) UpdateThrowing();

        // لو وقفت الشخبطة يرجع لونه شوي شوي
        if (Time.time > stunnedUntil + 0.6f) erased = Mathf.Max(0f, erased - Time.deltaTime * 0.12f);

        Animate();

        // طاح في حفرة
        if (Physics.gravity.y > 0f ? transform.position.y > 7f : transform.position.y < -5f) Poof();
    }

    void Think()
    {
        if (state == State.EatInk || Stunned) return;
        if (IsSnowman) { target = null; return; }   // رجال الثلج ما ياكلون الجسور

        // يدور على جسر قريب ياكله
        InkMark food = null;
        float best = FoodSight;
        Vector3 p = transform.position;
        foreach (InkMark mark in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
        {
            if (mark.closed || mark.beingEaten) continue;
            Vector3 q = ClosestPoint(mark, p);
            float dx = Mathf.Abs(q.x - p.x);
            if (Mathf.Abs(q.y - p.y) < 2.5f && dx < best) { best = dx; food = mark; }
        }
        target = food;
        if (food != null && Mathf.Abs(ClosestPoint(food, p).x - p.x) < 0.7f)
        {
            state = State.EatInk;
            stateTime = 0f;
            food.beingEaten = true;
        }
    }

    static Vector3 ClosestPoint(InkMark mark, Vector3 from)
    {
        Vector3 best = mark.transform.position;
        float bestD = float.MaxValue;
        foreach (Collider c in mark.GetComponentsInChildren<Collider>())
        {
            if (!c.enabled) continue;
            Vector3 q = c.ClosestPoint(from);
            float d = (q - from).sqrMagnitude;
            if (d < bestD) { bestD = d; best = q; }
        }
        return best;
    }

    bool Stunned => Time.time < stunnedUntil;

    bool SeesRowaida(Vector3 p, out float dx)
    {
        dx = 0f;
        GameManager gm = GameManager.Instance;
        if (gm == null || !gm.IsPlaying || gm.Rowaida == null) return false;
        Vector3 rp = gm.Rowaida.transform.position;
        dx = rp.x - p.x;
        return Mathf.Abs(dx) < SightRange && Mathf.Abs(dx) > 0.2f && Mathf.Abs(rp.y - p.y) < 2.2f;
    }

    // فيه أرض (صفحة أو جسر) قدامه؟
    bool GroundAhead(Vector3 p, int direction)
    {
        Vector3 down = Physics.gravity.y > 0f ? Vector3.up : Vector3.down;
        Vector3 from = p + new Vector3(direction * (Radius + 0.2f), 0f, 0f);
        return Physics.Raycast(from, down, Radius + 0.9f, ~0, QueryTriggerInteraction.Ignore);
    }

    void FixedUpdate()
    {
        if (dead) return;
        Vector3 v = rb.linearVelocity;
        Vector3 p = transform.position;
        float speed = 0f;

        bool sawHer = false;
        if (!Stunned && state == State.Wander)
        {
            if (p.x < patrolMin) dir = 1;
            else if (p.x > patrolMax) dir = -1;
            speed = kind == Kind.SnowGuard || kind == Kind.SnowThrower ? 0f : WanderSpeed * dir;   // الحارس والرامي واقفين مكانهم
            // جسر قريب؟ نمشي له
            if (target != null)
            {
                dir = ClosestPoint(target, p).x > p.x ? 1 : -1;
                speed = ChaseSpeed * dir;
            }
            // رجل الثلج اللي يلحق: يمشي ورا رويدا على طول (بعد ما يصحى)
            else if (kind == Kind.SnowChaser)
            {
                speed = 0f;
                GameManager gm = GameManager.Instance;
                if (Time.time > chaseFrom && gm.IsPlaying && gm.Rowaida != null)
                {
                    float dx = gm.Rowaida.transform.position.x - p.x;
                    if (Mathf.Abs(dx) > 0.2f)
                    {
                        dir = dx > 0f ? 1 : -1;
                        speed = SnowChaseSpeed * dir;
                        sawHer = true;
                    }
                }
            }
            // شاف رويدا قريبة على نفس المستوى؟ يركض لها
            else if (kind != Kind.SnowThrower && SeesRowaida(p, out float dx))
            {
                sawHer = true;
                dir = dx > 0f ? 1 : -1;
                speed = HuntSpeed * dir;
            }
            // ما يطيح من طرف الصفحة: يوقف ويستنى
            if (speed != 0f && !GroundAhead(p, dir)) speed = 0f;
        }
        if (sawHer && !hunting)
        {
            // أول ما يشوفها: يصرخ وينط نطة صغيرة
            GameManager.Instance.Sound.PlaySqueak(Random.Range(1.1f, 1.3f));
            v.y = -Mathf.Sign(Physics.gravity.y) * 3f;
        }
        hunting = sawHer;
        v.x = speed;
        rb.linearVelocity = v;
    }

    void UpdateEating()
    {
        if (target == null) { state = State.Wander; return; }
        if (Stunned)
        {
            // الشخبطة وقفته عن الأكل
            target.beingEaten = false;
            target = null;
            state = State.Wander;
            return;
        }
        if (Time.time >= nextChomp)
        {
            nextChomp = Time.time + 0.3f;
            GameManager.Instance.Sound.PlayChomp();
        }
        if (stateTime > 1.3f)
        {
            Destroy(target.gameObject);
            target = null;
            state = State.Wander;
        }
    }

    // ---------- المسح بالشخبطة ----------

    // اللاعب يشخبط على الوحش: amount = طول الشخبطة الجديدة
    public void Erase(float amount)
    {
        if (dead) return;
        erased += amount / EraseNeeded;
        stunnedUntil = Time.time + 0.35f;
        if (hint != null) Destroy(hint.gameObject);
        if (Time.time > nextSqueak)
        {
            nextSqueak = Time.time + 0.4f;
            GameManager.Instance.Sound.PlaySqueak(Random.Range(1.6f, 2.1f));
        }
        if (Random.value < 0.35f) Crumb();
        if (erased >= 1f) Poof();
    }

    // فتات مساحة يطيح من الوحش
    void Crumb()
    {
        var mat = GameManager.Instance.Mat("Eraser Pink", new Color(0.95f, 0.62f, 0.66f), 0.15f);
        var c = LevelBuilder.Visual("Eraser Crumb", PrimitiveType.Sphere, null, transform.position + Random.insideUnitSphere * 0.35f, new Vector3(0.07f, 0.04f, 0.04f), mat, false);
        c.AddComponent<PoofPuff>().shrinkOnly = true;
    }

    // ---------- رويدا ----------

    void OnCollisionEnter(Collision c)
    {
        if (dead) return;
        // رجل الثلج اللي يلحق: أي حبر يصدمه من قدام يتجمد ويتكسر
        if (kind == Kind.SnowChaser)
        {
            var mark = c.collider.GetComponentInParent<InkMark>();
            if (mark != null && c.contactCount > 0 && Mathf.Abs(c.GetContact(0).normal.x) > 0.6f) { Shatter(mark); return; }
        }
        var rowaida = c.collider.GetComponentInParent<RowaidaController>();
        if (rowaida == null) return;
        if (kind == Kind.SnowChaser)
        {
            // لحقها: تخسر قلب
            for (int i = 0; i < 8; i++) SnowPuff(rowaida.transform.position + Random.insideUnitSphere * 0.5f);
            GameManager.Instance.KillRowaida();
            return;
        }
        float push = rowaida.transform.position.x < transform.position.x ? -1f : 1f;
        rowaida.Knockback(push);
        rb.AddForce(new Vector3(-push * 2f, 1.5f, 0f), ForceMode.VelocityChange);
        GameManager.Instance.Sound.PlayBoing();
    }

    // ---------- الشكل والحركة ----------

    void Animate()
    {
        float speed = Mathf.Abs(rb.linearVelocity.x);
        phase += Time.deltaTime * (2f + speed * 4f);

        // ينط ويتمطط وهو يمشي، ويرتجف ويصغر وهو ينمسح
        float hop = Mathf.Abs(Mathf.Sin(phase)) * 0.08f * Mathf.Clamp01(speed);
        float squash = 1f + Mathf.Sin(phase * 2f) * 0.05f;
        float shrink = 1f - erased * 0.4f;
        Vector3 shake = Stunned ? Random.insideUnitSphere * 0.04f : Vector3.zero;
        visual.localPosition = new Vector3(0f, hop, 0f) + shake;
        visual.localScale = new Vector3(1f / squash, squash, 1f / squash) * shrink;

        // الوجه يطالع الكاميرا (لفة 180) مع ميلة صغيرة لجهة مشيه
        float face = state == State.EatInk || Stunned ? 0f : dir * 30f;
        visual.localRotation = Quaternion.Slerp(visual.localRotation, Quaternion.Euler(0f, 180f - face, 0f), Time.deltaTime * 6f);

        // العيون تطالع الهدف (محور x المحلي معكوس لأن الوجه ملفوف 180)
        Vector3 look = target != null ? (target.transform.position - transform.position) : new Vector3(dir, 0f, 0f);
        look.z = 0f;
        Vector3 off = look.sqrMagnitude > 0.01f ? look.normalized * 0.035f : Vector3.zero;
        foreach (Transform pupil in pupils)
        {
            Vector2 b = PupilBase(pupil);
            pupil.localPosition = new Vector3(b.x - off.x, b.y + off.y, pupil.localPosition.z);
        }

        // الفم يفتح ويسكر وهو ياكل
        if (mouth != null)
        {
            float open = state == State.EatInk ? 1f + Mathf.Abs(Mathf.Sin(Time.time * 18f)) * 1.6f : 1f;
            Vector3 ms = mouth.localScale;
            mouth.localScale = new Vector3(ms.x, MouthBase() * open, ms.z);
        }

        // يبهت لونه لين يصير مثل الورق
        Color paper = new Color(0.93f, 0.92f, 0.95f);
        for (int i = 0; i < renderers.Count; i++)
        {
            renderers[i].GetPropertyBlock(block);
            block.SetColor("_BaseColor", Color.Lerp(baseColors[i], paper, erased * 0.85f));
            renderers[i].SetPropertyBlock(block);
        }

        // التلميح يتحرك فوق الوحش
        if (hint != null)
        {
            hint.localPosition = new Vector3(Mathf.Sin(Time.time * 4f) * 0.12f, 1.05f + Mathf.Sin(Time.time * 2f) * 0.05f, -0.6f);
            hint.localScale = Vector3.one * (1f + Mathf.Sin(Time.time * 6f) * 0.06f);
        }

        if (Time.time > nextSqueak && Random.value < Time.deltaTime * 0.15f) Squeak(Random.Range(0.9f, 1.3f));
    }

    readonly Dictionary<Transform, Vector2> pupilBase = new Dictionary<Transform, Vector2>();
    float mouthBase = -1f;

    Vector2 PupilBase(Transform t)
    {
        if (!pupilBase.TryGetValue(t, out Vector2 b)) pupilBase[t] = b = t.localPosition;
        return b;
    }

    float MouthBase()
    {
        if (mouthBase < 0f) mouthBase = mouth.localScale.y;
        return mouthBase;
    }

    void Squeak(float pitch)
    {
        nextSqueak = Time.time + 2.5f;
        Camera cam = Camera.main;
        if (cam != null && Mathf.Abs(cam.transform.position.x - transform.position.x) < 14f)
            GameManager.Instance.Sound.PlaySqueak(pitch);
    }

    void Poof()
    {
        if (dead) return;
        dead = true;
        if (target != null) target.beingEaten = false;
        GameManager.Instance.Sound.PlayPoof();
        var mat = GameManager.Instance.Mat("Poof Cloud", new Color(0.95f, 0.93f, 1f), 0.1f);
        for (int i = 0; i < 10; i++)
        {
            var puff = LevelBuilder.Visual("Poof", PrimitiveType.Sphere, null, transform.position + Random.insideUnitSphere * 0.5f, Vector3.one * Random.Range(0.2f, 0.45f), mat, false);
            puff.AddComponent<PoofPuff>();
        }
        Destroy(gameObject);
    }
}

// سحابة صغيرة تكبر وتختفي (لما ينمسح الوحش)، أو فتات يطيح ويصغر
public class PoofPuff : MonoBehaviour
{
    public bool shrinkOnly;
    float age;
    Vector3 vel;

    void Start()
    {
        vel = shrinkOnly ? new Vector3(Random.Range(-1f, 1f), Random.Range(0.5f, 1.5f), 0f) : Random.insideUnitSphere * 1.2f + Vector3.up * 1.5f;
        vel.z = 0f;
    }

    void Update()
    {
        age += Time.deltaTime;
        if (shrinkOnly) vel.y -= 9f * Time.deltaTime;
        transform.position += vel * Time.deltaTime;
        if (!shrinkOnly) transform.localScale *= 1f + Time.deltaTime * 1.5f;
        if (age > 0.6f) transform.localScale *= 1f - Time.deltaTime * 6f;
        if (age > 1f) Destroy(gameObject);
    }
}
