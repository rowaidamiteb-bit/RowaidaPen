using UnityEngine;

// مخاطر الفضاء: مطر النيازك والثقب الأسود

// قطعة صفحة ينكسر فيها نيزك: تختفي وتطيح قطع ورق (وترجع لما رويدا ترجع)
public class BreakableTile : MonoBehaviour
{
    bool broken;
    Collider[] colliders;
    Renderer[] renderers;

    void Start()
    {
        colliders = GetComponentsInChildren<Collider>();
        renderers = GetComponentsInChildren<Renderer>();
        GameManager.Instance.Respawned += OnRespawned;
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.Respawned -= OnRespawned;
    }

    void OnRespawned(Vector3 pos)
    {
        if (!broken) return;
        broken = false;
        foreach (Collider c in colliders) c.enabled = true;
        foreach (Renderer r in renderers) r.enabled = true;
    }

    public void Break()
    {
        if (broken) return;
        broken = true;
        Bounds b = GetComponent<Renderer>().bounds;
        foreach (Collider c in colliders) c.enabled = false;
        foreach (Renderer r in renderers) r.enabled = false;
        GameManager gm = GameManager.Instance;
        Material paper = gm.Mat("Space Paper Bit", new Color(0.2f, 0.21f, 0.38f), 0.2f);
        Material ember = gm.Mat("Ember", new Color(2.5f, 0.9f, 0.3f), 0.5f);
        for (int i = 0; i < 16; i++)
        {
            var bit = LevelBuilder.Visual("Paper Bit", PrimitiveType.Cube, null,
                new Vector3(Random.Range(b.min.x, b.max.x), b.max.y - Random.Range(0f, 0.6f), Random.Range(-1.5f, 1.5f)),
                new Vector3(Random.Range(0.15f, 0.4f), 0.06f, Random.Range(0.15f, 0.4f)), i % 4 == 0 ? ember : paper, false);
            bit.transform.rotation = Random.rotation;
            var p = bit.AddComponent<FxParticle>();
            p.velocity = new Vector3(Random.Range(-2f, 2f), Random.Range(1f, 4f), Random.Range(-1f, 1f));
            p.gravity = 8f;
            p.spin = Random.insideUnitSphere * 400f;
            p.life = 1.3f;
        }
    }
}

// مطر النيازك: كل شوي يطيح نيزك مشتعل، ودايرة حمرا توري وين بيطيح.
// أغلبها تطيح قدام رويدا وتكسر الصفحة، وبعضها عليها مباشرة (ارسمي مظلة فوقها)
public class MeteorShower : MonoBehaviour
{
    const float Every = 1.35f, FlightTime = 1.4f;
    LevelData data;
    float next = 2f;

    public static void Create(Transform root, LevelData data)
    {
        var go = new GameObject("Meteor Shower");
        go.transform.SetParent(root, false);
        var m = go.AddComponent<MeteorShower>();
        m.data = data;
        m.next = Time.time + 2f;
        GameManager.Instance.Respawned += m.OnRespawned;
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.Respawned -= OnRespawned;
    }

    void OnRespawned(Vector3 pos) { next = Time.time + 2.5f; }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || !gm.IsPlaying || gm.Rowaida == null) return;
        Vector3 r = gm.Rowaida.transform.position;
        if (r.x < data.meteorZone.x - 4f || r.x > data.meteorZone.y) return;
        if (Time.time < next) return;
        next = Time.time + Every * Random.Range(0.8f, 1.2f);

        // وين يطيح: قدامها على الصفحة، أو عليها هي
        float x = Random.value < 0.3f ? r.x + gm.Rowaida.walkSpeed * FlightTime : r.x + Random.Range(2.5f, 6.5f);
        x = Mathf.Clamp(x, data.meteorZone.x + 0.3f, data.meteorZone.y - 0.3f);
        float top = 0f;
        foreach (Block b in data.blocks)
            if (x >= b.center.x - b.size.x * 0.5f && x <= b.center.x + b.size.x * 0.5f) top = b.center.y + b.size.y * 0.5f;
        Meteor.Launch(new Vector3(x, top, 0f), FlightTime);
    }
}

// نيزك: صخرة مشتعلة تطيح بخط مايل. تكسر قطعة الصفحة اللي تطيح عليها، وإذا صابت رويدا تدفها
public class Meteor : MonoBehaviour
{
    Vector3 velocity;
    Transform marker;
    float nextTrail;

    static Material rockMat, fireMat, markerMat;

    public static void Launch(Vector3 target, float flightTime)
    {
        if (rockMat == null)
        {
            rockMat = GameManager.MakeMaterial(new Color(0.35f, 0.22f, 0.18f), 0.2f);
            fireMat = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "Meteor Fire" };
            fireMat.SetColor("_BaseColor", new Color(3f, 1.2f, 0.3f));
            markerMat = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "Meteor Marker" };
            markerMat.SetColor("_BaseColor", new Color(2f, 0.2f, 0.25f));
        }
        Vector3 start = target + new Vector3(5f, 13f, 0f);
        var go = new GameObject("Meteor");
        go.transform.position = start;
        LevelBuilder.Visual("Rock", PrimitiveType.Sphere, go.transform, Vector3.zero, new Vector3(0.7f, 0.62f, 0.66f), rockMat);
        LevelBuilder.Visual("Glow", PrimitiveType.Sphere, go.transform, new Vector3(0.12f, 0.15f, 0f), Vector3.one * 0.82f, fireMat, false);
        var m = go.AddComponent<Meteor>();
        m.velocity = (target - start) / flightTime;
        // دايرة حمرا على الأرض توري وين بيطيح
        m.marker = LevelBuilder.Visual("Meteor Marker", PrimitiveType.Cylinder, null, target + Vector3.up * 0.03f, new Vector3(1.3f, 0.01f, 1.3f), markerMat, false).transform;
        Object.Destroy(go, flightTime + 2f);
        Object.Destroy(m.marker.gameObject, flightTime + 0.3f);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (marker != null) marker.localScale = new Vector3(1.3f, 0.01f, 1.3f) * (1f + Mathf.Sin(Time.time * 18f) * 0.12f);

        // ذيل نار
        if (Time.time >= nextTrail)
        {
            nextTrail = Time.time + 0.025f;
            var f = LevelBuilder.Visual("Fire", PrimitiveType.Sphere, null, transform.position + Random.insideUnitSphere * 0.2f, Vector3.one * Random.Range(0.25f, 0.45f), fireMat, false);
            var p = f.AddComponent<FxParticle>();
            p.velocity = -velocity * 0.08f + Random.insideUnitSphere * 0.4f;
            p.life = 0.35f;
        }

        Vector3 step = velocity * dt;
        if (Physics.SphereCast(transform.position, 0.3f, velocity.normalized, out RaycastHit hit, step.magnitude, ~0, QueryTriggerInteraction.Ignore))
        {
            Explode(hit);
            return;
        }
        transform.position += step;
    }

    void Explode(RaycastHit hit)
    {
        GameManager gm = GameManager.Instance;
        Vector3 at = hit.point;
        var tile = hit.collider.GetComponentInParent<BreakableTile>();
        if (tile != null) tile.Break();

        if (gm.IsPlaying && gm.Rowaida != null)
        {
            bool direct = hit.collider.GetComponentInParent<RowaidaController>() != null;
            // صابها: تندف بقوة (وممكن تطيح في حفرة)
            if (direct || Vector3.Distance(gm.Rowaida.transform.position, at) < 1f) gm.Rowaida.Knockback(-1f);
        }

        gm.Sound.PlayPoof();
        gm.Sound.PlayRumble(0.35f);
        var cam = Camera.main.GetComponent<CameraFollow>();
        if (cam != null) cam.Shake(0.18f, 0.3f);
        Fx.Sparkle(at, new Color(1f, 0.55f, 0.2f), 18);
        Fx.Dust(at, 1f);
        if (marker != null) Destroy(marker.gameObject);
        Destroy(gameObject);
    }
}

// ثقب أسود: يلف ويسحب كل شي له. يرفع رويدا من الأرض لين يبلعها إذا قربت،
// إلا إذا رسمتي سقف (حاجز) فوقها يمسكها. ويبلع الأشكال المغلقة اللي تقرب منه
public class BlackHole : MonoBehaviour
{
    const float Range = 9f, Strength = 30f, SwallowRadius = 1.75f;
    Transform disk, disk2;
    float nextRumble, nextSwirl;
    Material swirlMat;

    public static void Create(Transform root, Vector3 pos, GameManager gm)
    {
        var go = new GameObject("Black Hole");
        go.transform.SetParent(root, false);
        go.transform.position = pos;
        var h = go.AddComponent<BlackHole>();

        Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
        var black = new Material(unlit) { name = "Event Horizon" };
        black.SetColor("_BaseColor", Color.black);
        LevelBuilder.Visual("Core", PrimitiveType.Sphere, go.transform, Vector3.zero, Vector3.one * 1.8f, black, false);
        var glowA = new Material(unlit) { name = "Accretion" };
        glowA.SetColor("_BaseColor", new Color(3f, 1.3f, 0.4f));
        var glowB = new Material(unlit) { name = "Accretion Purple" };
        glowB.SetColor("_BaseColor", new Color(1.6f, 0.4f, 2.6f));
        // الحلقة محورها x: نلفها عشان تصير أفقية ومايلة شوي (قرص حول الثقب)
        h.disk = Ring(go.transform, 1.45f, 0.13f, glowA, Quaternion.Euler(-14f, 0f, 90f));
        h.disk2 = Ring(go.transform, 1.95f, 0.07f, glowB, Quaternion.Euler(-22f, 0f, 80f));
        Ring(go.transform, 0.98f, 0.045f, glowA, Quaternion.Euler(0f, 90f, 0f));   // هالة تواجه الكاميرا
        h.swirlMat = glowB;
    }

    static Transform Ring(Transform parent, float radius, float thickness, Material mat, Quaternion rot)
    {
        var r = new GameObject("Disk");
        r.transform.SetParent(parent, false);
        r.transform.localRotation = rot;
        r.AddComponent<MeshFilter>().sharedMesh = PaperMeshes.Ring(radius, thickness, 48, 6);
        r.AddComponent<MeshRenderer>().sharedMaterial = mat;
        return r.transform;
    }

    void OnDestroy()
    {
        GameManager gm = GameManager.Instance;
        if (gm != null && gm.Rowaida != null) gm.Rowaida.WalkBonus = 0f;
    }

    void Update()
    {
        disk.Rotate(160f * Time.deltaTime, 0f, 0f, Space.Self);
        disk2.Rotate(-90f * Time.deltaTime, 0f, 0f, Space.Self);
        transform.localScale = Vector3.one * (1f + Mathf.Sin(Time.time * 3f) * 0.04f);

        // ذرات تلف وتنسحب لداخل
        if (Time.time >= nextSwirl)
        {
            nextSwirl = Time.time + 0.05f;
            float a = Random.value * Mathf.PI * 2f;
            Vector3 from = transform.position + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * Random.Range(3f, 5f);
            var s = LevelBuilder.Visual("Swirl", PrimitiveType.Sphere, null, from, Vector3.one * 0.1f, swirlMat, false);
            s.AddComponent<Swirl>().center = transform.position;
        }

        GameManager gm = GameManager.Instance;
        if (gm == null || gm.Rowaida == null) return;
        float dist = Vector3.Distance(gm.Rowaida.transform.position, transform.position);
        if (Time.time >= nextRumble && dist < Range + 3f)
        {
            nextRumble = Time.time + 1.2f;
            gm.Sound.PlayRumble(Mathf.Lerp(0.5f, 0.1f, dist / (Range + 3f)));
        }
    }

    void FixedUpdate()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.Rowaida == null) return;
        RowaidaController rowaida = gm.Rowaida;
        Vector3 to = transform.position - rowaida.transform.position;
        to.z = 0f;
        float dist = to.magnitude;
        if (dist > Range || !gm.IsPlaying)
        {
            rowaida.WalkBonus = 0f;
            return;
        }

        // تسحبها لفوق كقوة، وتسرّعها لما تقرب وتبطّيها بعد ما تعدي
        float accel = Strength / Mathf.Max(dist, 1f);
        var body = rowaida.GetComponent<Rigidbody>();
        if (!body.isKinematic) body.AddForce(new Vector3(0f, to.normalized.y * accel, 0f), ForceMode.Acceleration);
        rowaida.WalkBonus = Mathf.Clamp(to.normalized.x * accel * 0.25f, -2f, 2f);

        // الأشكال المغلقة (الكور) تنسحب بعد
        foreach (InkMark mark in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
        {
            if (!mark.closed) continue;
            var rb = mark.GetComponent<Rigidbody>();
            if (rb == null) continue;
            Vector3 d = transform.position - rb.worldCenterOfMass;
            if (d.magnitude < SwallowRadius)
            {
                Fx.Sparkle(rb.worldCenterOfMass, new Color(0.8f, 0.4f, 1f), 10);
                Destroy(mark.gameObject);
                continue;
            }
            if (d.magnitude < Range) rb.AddForce(d.normalized * Strength / Mathf.Max(d.magnitude, 1f), ForceMode.Acceleration);
        }

        // بلعها
        if (dist < SwallowRadius)
        {
            rowaida.WalkBonus = 0f;
            Fx.Sparkle(rowaida.transform.position, new Color(0.8f, 0.4f, 1f), 20);
            gm.KillRowaida();
        }
    }
}

// ذرة صغيرة تلف حول الثقب الأسود وتنسحب لين تختفي
public class Swirl : MonoBehaviour
{
    public Vector3 center;
    float angle, radius;

    void Start()
    {
        Vector3 d = transform.position - center;
        radius = d.magnitude;
        angle = Mathf.Atan2(d.y, d.x);
    }

    void Update()
    {
        angle += Time.deltaTime * (6f / Mathf.Max(radius, 0.6f));
        radius -= Time.deltaTime * 2.2f;
        if (radius < 1.05f) { Destroy(gameObject); return; }
        transform.position = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
    }
}
