using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// مرحلة زيزو: انهيار ثلجي يلحق رويدا، جليد يتكسر تحتها، رجال ثلج يرمون كور، وزلاجة ترسمينها وتنزلين فيها المنحدر
public static class SnowFeatures
{
    public static void Build(Transform root, LevelData data, GameManager gm, System.Random rng)
    {
        foreach (Vector3 plank in data.icePlanks)
            CrackingIce.Create(root, plank.x, plank.y, plank.z, gm);
        if (data.sled != null) SledStation.Create(root, data, gm, rng);
        if (data.avalanche)
            Avalanche.Create(root, data.spawn.x - 9f, data.sled != null ? data.sled.spotX + 0.4f : data.door.x - 2f, gm);
    }

    // صفحة مايلة (للمنحدر والقفزة): نلف أب فاضي ونبني الصفحة داخله
    public static void SlopedPage(Transform root, Vector2 from, Vector2 to, LevelData data, GameManager gm, System.Random rng)
    {
        const float thickness = 1.2f;
        Vector2 d = to - from;
        float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        Quaternion rot = Quaternion.Euler(0f, 0f, angle);
        Vector3 surfaceMid = new Vector3((from.x + to.x) * 0.5f, (from.y + to.y) * 0.5f, 0f);
        var pivot = new GameObject("Slope");
        pivot.transform.SetParent(root, false);
        pivot.transform.position = surfaceMid - rot * Vector3.up * (thickness * 0.5f);
        pivot.transform.rotation = rot;
        // الحواف تتداخل شوي مع اللي جنبها عشان ما يصير فراغ
        LevelBuilder.CreatePaperBlock(pivot.transform, new Block(pivot.transform.position, new Vector3(d.magnitude + 0.3f, thickness, 4f)), gm, rng, data, false);
    }
}

// جليد يتشقق: لما توقف عليه رويدا تطلع شقوق وبعد شوي ينكسر ويطيح (ويرجع لما ترجع رويدا)
public class CrackingIce : MonoBehaviour
{
    const float BreakTime = 0.9f;
    const float Thickness = 0.45f;

    float from, to, top, timer = -1f;
    bool broken;
    Collider col;
    Renderer[] renderers;
    readonly List<Transform> cracks = new List<Transform>();

    public static void Create(Transform root, float from, float to, float top, GameManager gm)
    {
        var go = new GameObject("Ice");
        go.transform.SetParent(root, false);
        go.transform.position = new Vector3((from + to) * 0.5f, top - Thickness * 0.5f, 0f);
        var ice = go.AddComponent<CrackingIce>();
        ice.top = top;
        ice.from = from;
        ice.to = to;
        Vector3 size = new Vector3(to - from - 0.04f, Thickness, 3.6f);
        ice.col = go.AddComponent<BoxCollider>();
        ((BoxCollider)ice.col).size = size;

        Material iceMat = gm.Mat("Ice", new Color(0.68f, 0.86f, 0.98f), 0.96f, 0.1f);
        Material frost = gm.Mat("Frost", new Color(0.9f, 0.96f, 1f), 0.5f);
        LevelBuilder.Visual("Ice Slab", PrimitiveType.Cube, go.transform, Vector3.zero, size, iceMat);
        // رقاقات جليد معلقة تحت اللوح
        for (float ix = -size.x * 0.5f + 0.25f; ix < size.x * 0.5f - 0.1f; ix += 0.45f)
        {
            float len = 0.25f + Mathf.PerlinNoise(from * 3f, ix * 2f) * 0.35f;
            LevelBuilder.Visual("Icicle", PrimitiveType.Capsule, go.transform, new Vector3(ix, -Thickness * 0.5f - len * 0.5f, -1.2f + Mathf.PerlinNoise(ix, from) * 2.4f), new Vector3(0.09f, len * 0.5f, 0.09f), iceMat, false);
        }
        // لمعات وخطوط ثلج خفيفة فوق الجليد
        var rng = new System.Random((int)(from * 100f));
        for (int i = 0; i < 4; i++)
        {
            var streak = LevelBuilder.Visual("Frost", PrimitiveType.Cube, go.transform,
                new Vector3(((float)rng.NextDouble() - 0.5f) * size.x * 0.8f, Thickness * 0.5f + 0.003f, ((float)rng.NextDouble() - 0.5f) * 2.6f),
                new Vector3(0.5f + (float)rng.NextDouble() * 0.6f, 0.005f, 0.05f), frost, false);
            streak.transform.localRotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 60f - 30f, 0f);
        }
        // الشقوق مخفية لين توقف عليه
        Material crackMat = gm.Mat("Ice Crack", new Color(0.2f, 0.35f, 0.55f), 0.6f);
        for (int i = 0; i < 6; i++)
        {
            var crack = LevelBuilder.Visual("Crack", PrimitiveType.Cube, go.transform, new Vector3(0f, Thickness * 0.5f + 0.006f, 0f), Vector3.zero, crackMat, false);
            crack.transform.localRotation = Quaternion.Euler(0f, i * 30f + (float)rng.NextDouble() * 20f, 0f);
            ice.cracks.Add(crack.transform);
        }
        ice.renderers = go.GetComponentsInChildren<Renderer>();
        gm.Respawned += ice.OnRespawned;
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.Respawned -= OnRespawned;
    }

    void OnRespawned(Vector3 pos)
    {
        broken = false;
        timer = -1f;
        col.enabled = true;
        foreach (Renderer r in renderers) r.enabled = true;
        foreach (Transform c in cracks) c.localScale = Vector3.zero;
    }

    void Update()
    {
        if (broken) return;
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.Rowaida == null) return;

        if (timer < 0f)
        {
            Vector3 p = gm.Rowaida.transform.position;
            bool onTop = p.x > from - 0.1f && p.x < to + 0.1f && p.y > top + 0.2f && p.y < top + 1.3f;
            if (!onTop) return;
            timer = 0f;
            gm.Sound.PlaySqueak(Random.Range(0.4f, 0.5f));
        }

        // الشقوق تكبر شوي شوي
        timer += Time.deltaTime;
        float k = Mathf.Clamp01(timer / BreakTime);
        float width = (to - from);
        for (int i = 0; i < cracks.Count; i++)
            cracks[i].localScale = new Vector3(width * 0.7f * Mathf.Clamp01(k * 1.6f - i * 0.12f), 0.006f, 0.03f);
        if (timer >= BreakTime) Break(gm);
    }

    void Break(GameManager gm)
    {
        broken = true;
        col.enabled = false;
        foreach (Renderer r in renderers) r.enabled = false;
        gm.Sound.PlayPoof();
        Material iceMat = gm.Mat("Ice", new Color(0.68f, 0.86f, 0.98f), 0.96f, 0.1f);
        for (int i = 0; i < 14; i++)
        {
            var shard = LevelBuilder.Visual("Ice Shard", PrimitiveType.Cube, null,
                transform.position + new Vector3(Random.Range(from - transform.position.x, to - transform.position.x), 0f, Random.Range(-1.4f, 1.4f)),
                new Vector3(Random.Range(0.2f, 0.5f), 0.12f, Random.Range(0.2f, 0.5f)), iceMat, false);
            shard.transform.rotation = Random.rotation;
            var p = shard.AddComponent<FxParticle>();
            p.velocity = new Vector3(Random.Range(-1f, 1f), Random.Range(-0.5f, 1.5f), Random.Range(-0.5f, 0.5f));
            p.gravity = 12f;
            p.spin = Random.insideUnitSphere * 300f;
            p.life = 1.4f;
        }
    }
}

// انهيار ثلجي: جدار ثلج ضخم يتدحرج ورا رويدا. أبطأ منها شوي، بس إذا وقفت أو انصدت يمسكها
public class Avalanche : MonoBehaviour
{
    const float Speed = 2.7f;   // تقريبًا بسرعة رويدا: أي وقفة أو دفة تقربه منها

    float endX, delay = 2.2f, nextRumble, nextPuff, nextShake;
    bool stopped;
    readonly List<Transform> balls = new List<Transform>();
    readonly List<Vector3> ballBase = new List<Vector3>();

    float Front => transform.position.x + 1.2f;

    public static void Create(Transform root, float startX, float endX, GameManager gm)
    {
        var go = new GameObject("Avalanche");
        go.transform.SetParent(root, false);
        go.transform.position = new Vector3(startX, 0f, 0f);
        var a = go.AddComponent<Avalanche>();
        a.endX = endX;
        Material snow = gm.Mat("Snow Body", new Color(0.96f, 0.98f, 1f), 0.35f);
        Material shade = gm.Mat("Snow Shade", new Color(0.82f, 0.88f, 0.97f), 0.3f);
        var rng = new System.Random(8);
        for (int i = 0; i < 22; i++)
        {
            float y = -2f + (float)rng.NextDouble() * 6.5f;
            // الجدار أعلى ورا وأقرب للأرض قدام، مثل موجة
            float x = -3.5f + (float)rng.NextDouble() * 3.5f - Mathf.Max(0f, y - 1f) * 0.35f;
            Vector3 pos = new Vector3(x, y, -1.8f + (float)rng.NextDouble() * 3.6f);
            float s = 1.4f + (float)rng.NextDouble() * 2.2f;
            var ball = LevelBuilder.Visual("Snow Ball", PrimitiveType.Sphere, go.transform, pos, Vector3.one * s, i % 3 == 0 ? shade : snow, true);
            a.balls.Add(ball.transform);
            a.ballBase.Add(pos);
        }
        gm.Respawned += a.OnRespawned;
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.Respawned -= OnRespawned;
    }

    // رويدا خسرت قلب: يرجع بعيد وراها
    void OnRespawned(Vector3 pos)
    {
        if (pos.x > endX) return;
        transform.position = new Vector3(pos.x - 9f, 0f, 0f);
        delay = 1.8f;
        stopped = false;
        gameObject.SetActive(true);
    }

    public void Dissolve()
    {
        if (stopped) return;
        stopped = true;
        GameManager.Instance.Sound.PlayPoof();
        for (int i = 0; i < 25; i++) Puff(transform.position + new Vector3(Random.Range(-3f, 1f), Random.Range(-1f, 4f), Random.Range(-1.5f, 1.5f)), 1f);
        gameObject.SetActive(false);
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time;

        // الكور تتدحرج وتتمايل
        for (int i = 0; i < balls.Count; i++)
        {
            balls[i].localPosition = ballBase[i] + new Vector3(Mathf.Sin(t * 3f + i) * 0.15f, Mathf.Abs(Mathf.Sin(t * 4f + i * 1.7f)) * 0.25f, 0f);
            balls[i].Rotate(0f, 0f, -200f * Time.deltaTime, Space.Self);
        }

        if (!gm.IsPlaying || stopped) return;
        if (delay > 0f) { delay -= Time.deltaTime; return; }

        // يوقف عند محطة الزلاجة (ما ينزل المنحدر)
        if (transform.position.x + 1.2f < endX)
            transform.position += new Vector3(Speed * Time.deltaTime, 0f, 0f);

        // غبار ثلج يطير من قدامه
        if (t >= nextPuff)
        {
            nextPuff = t + 0.05f;
            Puff(new Vector3(Front, Random.Range(-0.5f, 3.5f), Random.Range(-1.5f, 1.5f)), 0.5f);
        }

        RowaidaController rowaida = gm.Rowaida;
        if (rowaida == null) return;
        float gap = rowaida.transform.position.x - Front;

        // هدير وهزة كاميرا كل ما قرب
        if (t >= nextRumble && gap < 14f)
        {
            nextRumble = t + 1.1f;
            gm.Sound.PlayRumble(Mathf.Lerp(0.6f, 0.15f, gap / 14f));
        }
        if (t >= nextShake && gap < 6f)
        {
            nextShake = t + 0.25f;
            var cam = Camera.main.GetComponent<CameraFollow>();
            if (cam != null) cam.Shake(Mathf.Lerp(0.1f, 0.02f, gap / 6f), 0.3f);
        }

        // يمسح الحبر اللي يغطيه
        foreach (InkMark mark in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
        {
            var r = mark.GetComponent<MeshRenderer>();
            if (r != null && r.bounds.max.x < Front - 0.3f)
            {
                Puff(r.bounds.center, 0.6f);
                Destroy(mark.gameObject);
            }
        }

        // مسكها
        if (gap < 0.2f && rowaida.transform.position.y > -3f)
        {
            for (int i = 0; i < 10; i++) Puff(rowaida.transform.position + Random.insideUnitSphere, 0.8f);
            gm.KillRowaida();
        }
    }

    static void Puff(Vector3 pos, float size)
    {
        Material snow = GameManager.Instance.Mat("Snow Puff", new Color(0.97f, 0.98f, 1f), 0.2f);
        var go = LevelBuilder.Visual("Snow Puff", PrimitiveType.Sphere, null, pos, Vector3.one * Random.Range(0.25f, 0.5f) * size, snow, false);
        var p = go.AddComponent<FxParticle>();
        p.velocity = new Vector3(Random.Range(1f, 3f), Random.Range(0f, 1.5f), Random.Range(-0.5f, 0.5f));
        p.drag = 2f;
        p.grow = 1.5f;
        p.life = Random.Range(0.5f, 0.9f);
    }
}

// كرة ثلج يرميها رجل الثلج: إذا صابت رويدا تدفها لورا، وإذا صابت أي شي ثاني (مثل درع مرسوم) تتفتت
public class Snowball : MonoBehaviour
{
    bool done;

    public static void Throw(Vector3 from, Vector3 velocity, Collider thrower)
    {
        GameManager gm = GameManager.Instance;
        Material snow = gm.Mat("Snow Body", new Color(0.96f, 0.98f, 1f), 0.35f);
        var go = LevelBuilder.Visual("Snowball", PrimitiveType.Sphere, null, from, Vector3.one * 0.32f, snow, true);
        var col = go.AddComponent<SphereCollider>();
        col.radius = 0.5f;
        // تعدي من رجال الثلج الثانيين (ما تصيب بعضها)
        foreach (ScribbleMonster m in Object.FindObjectsByType<ScribbleMonster>(FindObjectsSortMode.None))
            if (m.IsSnowman) Physics.IgnoreCollision(col, m.GetComponent<Collider>());
        var rb = go.AddComponent<Rigidbody>();
        rb.mass = 0.3f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.constraints = RigidbodyConstraints.FreezePositionZ;
        rb.linearVelocity = velocity;
        go.AddComponent<Snowball>();
        Object.Destroy(go, 6f);
    }

    void OnCollisionEnter(Collision c)
    {
        if (done) return;
        done = true;
        GameManager gm = GameManager.Instance;
        var rowaida = c.collider.GetComponentInParent<RowaidaController>();
        if (rowaida != null && gm.IsPlaying)
        {
            rowaida.Knockback(GetComponent<Rigidbody>().linearVelocity.x < 0f ? -1f : 1f);
            gm.Sound.PlayBoing();
        }
        gm.Sound.PlayPoof();
        Material snow = gm.Mat("Snow Puff", new Color(0.97f, 0.98f, 1f), 0.2f);
        for (int i = 0; i < 8; i++)
        {
            var go = LevelBuilder.Visual("Snow Bit", PrimitiveType.Sphere, null, transform.position + Random.insideUnitSphere * 0.15f, Vector3.one * Random.Range(0.06f, 0.12f), snow, false);
            var p = go.AddComponent<FxParticle>();
            p.velocity = Random.insideUnitSphere * 2.5f + Vector3.up * 1.5f;
            p.gravity = 9f;
            p.life = 0.6f;
        }
        Destroy(gameObject);
    }
}

// محطة الزلاجة: رويدا توقف فوق التلة لين ترسمين زلاجة على الخط المنقط، بعدين تركبها وتنزل المنحدر وتطير فوق الحفرة
public class SledStation : MonoBehaviour
{
    SledSpec spec;
    bool riding, done;
    InkMark sled;

    float HintFrom => spec.spotX + 0.7f;
    float HintTo => spec.spotX + 2.9f;

    public static void Create(Transform root, LevelData data, GameManager gm, System.Random rng)
    {
        SledSpec s = data.sled;
        // المنحدر والقفزة
        SnowFeatures.SlopedPage(root, new Vector2(s.hillStart, 0f), new Vector2(s.hillEnd, -s.drop), data, gm, rng);
        SnowFeatures.SlopedPage(root, new Vector2(s.hillEnd, -s.drop), new Vector2(s.rampEnd, -s.drop + s.rampRise), data, gm, rng);
        var go = new GameObject("Sled Station");
        go.transform.SetParent(root, false);
        var st = go.AddComponent<SledStation>();
        st.spec = s;
        gm.Respawned += st.OnRespawned;
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.Respawned -= OnRespawned;
    }

    void OnRespawned(Vector3 pos)
    {
        if (riding || done) return;
        sled = null;
        if (GameManager.Instance.Rowaida != null) GameManager.Instance.Rowaida.Held = false;
    }

    void Update()
    {
        if (riding || done) return;
        GameManager gm = GameManager.Instance;
        if (gm == null || !gm.IsPlaying || gm.Rowaida == null) return;
        RowaidaController rowaida = gm.Rowaida;
        float x = rowaida.transform.position.x;

        if (sled == null) sled = FindSled();
        if (sled == null)
        {
            // توصل للمحطة وتستنى الزلاجة
            if (x >= spec.spotX && x < spec.hillStart) rowaida.Held = true;
            return;
        }

        rowaida.Held = false;
        // تمشي لين توقف فوق الزلاجة، بعدين تنطلق
        Bounds b = sled.GetComponent<MeshRenderer>().bounds;
        if (x >= b.center.x - 0.2f) StartCoroutine(Ride(rowaida, sled.gameObject));
    }

    // أي رسمة على الخط المنقط (عرضها متر وأكثر) تصير زلاجة
    InkMark FindSled()
    {
        foreach (InkMark mark in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
        {
            var r = mark.GetComponent<MeshRenderer>();
            if (r == null) continue;
            Bounds b = r.bounds;
            if (b.center.x > HintFrom - 0.8f && b.center.x < HintTo + 0.8f && b.center.y > -0.4f && b.center.y < 1.6f && b.size.x >= 1f)
                return mark;
        }
        return null;
    }

    // مسار الزلاجة: ارتفاع السطح عند x
    float SurfaceY(float x)
    {
        if (x < spec.hillStart) return 0f;
        if (x < spec.hillEnd) return -spec.drop * (x - spec.hillStart) / (spec.hillEnd - spec.hillStart);
        return -spec.drop + spec.rampRise * Mathf.Clamp01((x - spec.hillEnd) / (spec.rampEnd - spec.hillEnd));
    }

    IEnumerator Ride(RowaidaController rowaida, GameObject sledGo)
    {
        riding = true;
        GameManager gm = GameManager.Instance;
        Transform originalParent = rowaida.transform.parent;
        var body = rowaida.GetComponent<Rigidbody>();
        rowaida.enabled = false;
        body.isKinematic = true;
        body.linearVelocity = Vector3.zero;

        // الزلاجة ما تصطدم بشي وهي ماشية
        foreach (Collider c in sledGo.GetComponentsInChildren<Collider>()) c.enabled = false;
        var sledBody = sledGo.GetComponent<Rigidbody>();
        if (sledBody != null) sledBody.isKinematic = true;
        Destroy(sledGo.GetComponent<InkMark>());

        Bounds b = sledGo.GetComponent<MeshRenderer>().bounds;
        var rig = new GameObject("Sled Ride").transform;
        rig.SetParent(originalParent, true);
        rig.position = new Vector3(rowaida.transform.position.x, b.max.y, 0f);
        sledGo.transform.SetParent(rig, true);
        // رويدا ما ننقلها بالأب (الفيزياء ترجعها)، نحطها فوق الزلاجة كل فريم
        body.interpolation = RigidbodyInterpolation.None;
        Vector3 seat = new Vector3(0f, 0.72f, 0f);
        Seat(rowaida, body, rig, seat);

        gm.Sound.PlayBoing();
        Fx.Sparkle(rig.position + Vector3.up * 0.5f, new Color(0.8f, 0.95f, 1f), 12);
        float lift = rig.position.y - SurfaceY(rig.position.x);   // الزلاجة فوق السطح بكم
        float speed = 2.5f;
        float nextSpray = 0f;

        // 1) على الأرض: تمشي، تنزل المنحدر وتسرع، وتطلع القفزة
        while (rig.position.x < spec.rampEnd)
        {
            float x = rig.position.x;
            bool downhill = x >= spec.hillStart && x < spec.hillEnd;
            speed = Mathf.Min(10f, speed + (downhill ? 6f : 0.5f) * Time.deltaTime);
            float slope = (SurfaceY(x + 0.05f) - SurfaceY(x - 0.05f)) / 0.1f;
            float dx = speed * Time.deltaTime / Mathf.Sqrt(1f + slope * slope);
            x += dx;
            rig.position = new Vector3(x, SurfaceY(x) + lift, 0f);
            rig.rotation = Quaternion.Slerp(rig.rotation, Quaternion.Euler(0f, 0f, Mathf.Atan(slope) * Mathf.Rad2Deg), Time.deltaTime * 10f);
            gm.Sound.SetWind(Mathf.Clamp01(speed / 10f) * 0.8f);
            if (Time.time >= nextSpray) { nextSpray = Time.time + 0.03f; Spray(rig.position, speed); }
            Seat(rowaida, body, rig, seat);
            yield return null;
        }

        // 2) طيران فوق الحفرة
        speed = Mathf.Max(speed, 9.5f);
        float rampAngle = Mathf.Atan2(spec.rampRise, spec.rampEnd - spec.hillEnd);
        Vector3 vel = new Vector3(Mathf.Cos(rampAngle), Mathf.Sin(rampAngle), 0f) * speed;
        const float gravity = 7f;
        gm.Sound.PlaySqueak(1.4f);
        while (true)
        {
            vel.y -= gravity * Time.deltaTime;
            Vector3 next = rig.position + vel * Time.deltaTime;
            if (vel.y < 0f && next.y <= spec.landingTop + lift) { rig.position = new Vector3(next.x, spec.landingTop + lift, 0f); break; }
            rig.position = next;
            rig.rotation = Quaternion.Slerp(rig.rotation, Quaternion.Euler(0f, 0f, Mathf.Atan2(vel.y, vel.x) * Mathf.Rad2Deg * 0.6f), Time.deltaTime * 6f);
            Seat(rowaida, body, rig, seat);
            yield return null;
        }

        // 3) نزلت: هزة وغبار، وتتزحلق شوي لين توقف
        var cam = Camera.main.GetComponent<CameraFollow>();
        if (cam != null) cam.Shake(0.25f, 0.35f);
        Fx.Dust(rig.position, 1f);
        gm.Sound.PlayBoing();
        speed = vel.x;
        while (speed > 0.3f)
        {
            speed = Mathf.Max(0f, speed - 7f * Time.deltaTime);
            rig.position += new Vector3(speed * Time.deltaTime, 0f, 0f);
            rig.rotation = Quaternion.Slerp(rig.rotation, Quaternion.identity, Time.deltaTime * 12f);
            gm.Sound.SetWind(Mathf.Clamp01(speed / 10f) * 0.8f);
            if (Time.time >= nextSpray) { nextSpray = Time.time + 0.04f; Spray(rig.position, speed); }
            Seat(rowaida, body, rig, seat);
            yield return null;
        }
        gm.Sound.SetWind(0f);
        rig.rotation = Quaternion.identity;

        // تنزل من الزلاجة وتكمل مشي
        body.interpolation = RigidbodyInterpolation.Interpolate;
        rowaida.transform.rotation = Quaternion.identity;
        body.isKinematic = false;
        body.linearVelocity = Vector3.zero;
        rowaida.enabled = true;
        riding = false;
        done = true;
        var avalanche = FindAnyObjectByType<Avalanche>();
        if (avalanche != null) avalanche.Dissolve();
    }

    // نحط رويدا فوق الزلاجة (مايلة معها)
    static void Seat(RowaidaController rowaida, Rigidbody body, Transform rig, Vector3 seat)
    {
        Vector3 pos = rig.position + rig.rotation * seat;
        body.position = pos;
        body.rotation = rig.rotation;
        rowaida.transform.SetPositionAndRotation(pos, rig.rotation);
    }

    static void Spray(Vector3 pos, float speed)
    {
        Material snow = GameManager.Instance.Mat("Snow Puff", new Color(0.97f, 0.98f, 1f), 0.2f);
        var go = LevelBuilder.Visual("Spray", PrimitiveType.Sphere, null, pos + new Vector3(-0.6f, 0f, Random.Range(-0.3f, 0.3f)), Vector3.one * Random.Range(0.1f, 0.2f), snow, false);
        var p = go.AddComponent<FxParticle>();
        p.velocity = new Vector3(-speed * 0.3f, Random.Range(1f, 2.5f), Random.Range(-1f, 1f));
        p.gravity = 6f;
        p.grow = 0.8f;
        p.life = 0.5f;
    }
}
