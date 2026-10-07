using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// وحش شخابيط: رسمة قديمة ما كملت وصحت. يتمشى على صفحته، ياكل الجسور القريبة،
// يسكر الطريق على رويدا، ويركض لأي كورة (شكل مغلق) ويلعب فيها لين يمل.
// إذا طاح في حفرة يختفي بسحابة.
public class ScribbleMonster : MonoBehaviour
{
    public enum Kind { PurpleBlob, FuzzyBall, GreenEye }
    enum State { Wander, ChaseToy, PlayToy, EatInk }

    const float Radius = 0.45f;
    const float WanderSpeed = 1.1f, ChaseSpeed = 2.6f;
    const float ToySight = 9f, FoodSight = 3.5f;

    Kind kind;
    float patrolMin, patrolMax;
    int dir = 1;
    State state = State.Wander;
    Rigidbody rb;
    Transform visual, mouth;
    readonly List<Transform> pupils = new List<Transform>();
    float nextThink, stateTime, nextBump, nextSqueak, nextChomp;
    InkMark target;
    float lostTargetTime;
    float phase;
    bool dead;

    static PhysicsMaterial slippery;

    // ---------- البناء ----------

    public static ScribbleMonster Spawn(Transform parent, Kind kind, Vector3 groundPos, float patrolMin, float patrolMax, GameManager gm)
    {
        var go = new GameObject("Scribble Monster (" + kind + ")");
        go.transform.SetParent(parent, false);
        go.transform.position = groundPos + Vector3.up * Radius;

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
                var eye = Part(PrimitiveType.Sphere, new Vector3(0f, 0.16f, 0.27f), Vector3.one * 0.34f, white);
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
        if (state == State.PlayToy) UpdatePlaying();
        Animate();

        // طاح في حفرة
        if (transform.position.y < -5f) Poof();
    }

    void Think()
    {
        if (state == State.ChaseToy || state == State.PlayToy)
        {
            // الكورة راحت أو طاحت: نظل نلاحقها لحظة (عشان الوحش يطيح وراها في الحفرة) وبعدين نرجع نتمشى
            bool lost = target == null || target.transform.position.y < -1.5f;
            if (lost)
            {
                if (lostTargetTime <= 0f) lostTargetTime = Time.time;
                else if (Time.time - lostTargetTime > 0.7f)
                {
                    target = null;
                    lostTargetTime = 0f;
                    SetState(State.Wander);
                }
                return;
            }
            lostTargetTime = 0f;
            if (state == State.PlayToy) return;
        }
        if (state == State.EatInk) return;

        InkMark toy = null, food = null;
        float bestToy = ToySight, bestFood = FoodSight;
        Vector3 p = transform.position;
        foreach (InkMark mark in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
        {
            Vector3 q = ClosestPoint(mark, p);
            float dx = Mathf.Abs(q.x - p.x), dy = Mathf.Abs(q.y - p.y);
            if (dy > 2.5f) continue;
            if (mark.closed) { if (dx < bestToy && q.y > -1.5f) { bestToy = dx; toy = mark; } }
            else if (!mark.beingEaten && dx < bestFood) { bestFood = dx; food = mark; }
        }

        if (toy != null)
        {
            target = toy;
            if (state != State.ChaseToy) { SetState(State.ChaseToy); Squeak(1.4f); }
        }
        else if (food != null)
        {
            // جسر قريب: نمشي له (في حالة التمشي)، ولما نوصله ناكله
            target = food;
            if (state == State.ChaseToy) SetState(State.Wander);
            if (Mathf.Abs(ClosestPoint(food, p).x - p.x) < 0.7f)
            {
                SetState(State.EatInk);
                food.beingEaten = true;
            }
        }
        else
        {
            target = null;
            if (state == State.ChaseToy) SetState(State.Wander);
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

    void SetState(State s)
    {
        state = s;
        stateTime = 0f;

        // لما يكون ملتهي بكورة ما يسكر الطريق: يعدي جنب رويدا بدون ما يصدمها
        bool distracted = s == State.ChaseToy || s == State.PlayToy;
        var rowaida = GameManager.Instance != null ? GameManager.Instance.Rowaida : null;
        if (rowaida != null)
            Physics.IgnoreCollision(GetComponent<Collider>(), rowaida.GetComponent<Collider>(), distracted);
    }

    void FixedUpdate()
    {
        if (dead) return;
        Vector3 v = rb.linearVelocity;
        float speed = 0f;
        Vector3 p = transform.position;

        switch (state)
        {
            case State.Wander:
                if (p.x < patrolMin) dir = 1;
                else if (p.x > patrolMax) dir = -1;
                speed = WanderSpeed * dir;
                // أكل قريب؟ نمشي له
                if (target != null && !target.closed && target.gameObject != null)
                {
                    float tx = ClosestPoint(target, p).x;
                    if (Mathf.Abs(tx - p.x) < FoodSight) { dir = tx > p.x ? 1 : -1; speed = ChaseSpeed * 0.7f * dir; }
                }
                break;
            case State.ChaseToy:
                float targetX = target != null ? target.transform.position.x : p.x + dir;
                if (Mathf.Abs(targetX - p.x) > 0.3f) dir = targetX > p.x ? 1 : -1;
                speed = ChaseSpeed * dir;
                if (target != null && Vector3.Distance(ClosestPoint(target, p), p) < Radius + 0.35f) SetState(State.PlayToy);
                break;
            case State.PlayToy:
            case State.EatInk:
                speed = 0f;
                break;
        }
        v.x = speed;
        rb.linearVelocity = v;
    }

    void UpdatePlaying()
    {
        if (target == null) { SetState(State.Wander); return; }
        var toyRb = target.GetComponent<Rigidbody>();
        Vector3 p = transform.position;
        if (Vector3.Distance(ClosestPoint(target, p), p) > Radius + 1f) { SetState(State.ChaseToy); return; }
        if (Time.time >= nextBump && toyRb != null && !toyRb.isKinematic)
        {
            // يدف الكورة ويلعب فيها
            nextBump = Time.time + Random.Range(0.7f, 1.2f);
            float side = Random.value < 0.5f ? -1f : 1f;
            toyRb.AddForce(new Vector3(side * Random.Range(1.5f, 3f), Random.Range(2.5f, 4f), 0f), ForceMode.VelocityChange);
            Squeak(Random.Range(1.2f, 1.7f));
        }
        // بعد شوي يمل وتختفي الكورة
        if (stateTime > 7f)
        {
            Destroy(target.gameObject);
            target = null;
            SetState(State.Wander);
        }
    }

    void UpdateEating()
    {
        if (target == null) { SetState(State.Wander); return; }
        if (Time.time >= nextChomp)
        {
            nextChomp = Time.time + 0.3f;
            GameManager.Instance.Sound.PlayChomp();
        }
        // بعد ثانية وشوي يخلص الجسر
        if (stateTime > 1.3f)
        {
            Destroy(target.gameObject);
            target = null;
            SetState(State.Wander);
        }
    }

    // ---------- رويدا ----------

    void OnCollisionEnter(Collision c)
    {
        var rowaida = c.collider.GetComponentInParent<RowaidaController>();
        if (rowaida == null || dead) return;
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

        // ينط ويتمطط وهو يمشي
        float hop = Mathf.Abs(Mathf.Sin(phase)) * 0.08f * Mathf.Clamp01(speed);
        float squash = 1f + Mathf.Sin(phase * 2f) * 0.05f;
        visual.localPosition = new Vector3(0f, hop, 0f);
        visual.localScale = new Vector3(1f / squash, squash, 1f / squash);

        // الوجه يطالع الكاميرا (لفة 180) مع ميلة صغيرة لجهة مشيه
        float face = state == State.PlayToy || state == State.EatInk ? 0f : dir * 30f;
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

        if (Time.time > nextSqueak && Random.value < Time.deltaTime * 0.15f) Squeak(Random.Range(0.9f, 1.3f));
    }

    readonly Dictionary<Transform, Vector2> pupilBase = new Dictionary<Transform, Vector2>();
    float mouthBase = -1f;

    Vector2 PupilBase(Transform t)
    {
        if (!pupilBase.TryGetValue(t, out Vector2 b)) pupilBase[t] = b = t.localPosition;
        return b;
    }

    float MouthBase() { if (mouthBase < 0f) mouthBase = mouth.localScale.y; return mouthBase; }

    void Squeak(float pitch)
    {
        nextSqueak = Time.time + 2.5f;
        Camera cam = Camera.main;
        if (cam != null && Mathf.Abs(cam.transform.position.x - transform.position.x) < 14f)
            GameManager.Instance.Sound.PlaySqueak(pitch);
    }

    void Poof()
    {
        dead = true;
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

// سحابة صغيرة تكبر وتختفي (لما يطيح الوحش)
public class PoofPuff : MonoBehaviour
{
    float age;
    Vector3 vel;

    void Start()
    {
        vel = Random.insideUnitSphere * 1.2f + Vector3.up * 1.5f;
        vel.z = 0f;
    }

    void Update()
    {
        age += Time.deltaTime;
        transform.position += vel * Time.deltaTime;
        transform.localScale *= 1f + Time.deltaTime * 1.5f;
        if (age > 0.6f) transform.localScale *= 1f - Time.deltaTime * 6f;
        if (age > 1f) Destroy(gameObject);
    }
}
