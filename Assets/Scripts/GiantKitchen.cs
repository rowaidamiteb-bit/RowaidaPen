using System.Collections.Generic;
using UnityEngine;

// الجزء الثالث — فصل المطبخ العملاق: رويدا صغيرة بمطبخ كبير.
// ملعقة مقلاع، مغسلة تتعبى وإسفنجة، أي شكل مسكر يصير كوكيز بنفس شكله، فار سارق القلم، فشار ينط،
// صودا تفور وترفع رويدا، خلاط يشفط الخطوط، وكيكة عيد ميلاد بالنهاية
public static class GiantKitchen
{
    public static void Build(Transform root, LevelData data, GameManager gm, System.Random rng)
    {
        foreach (Vector4 s in data.spoons) SpoonCatapult.Create(root, s, gm);
        foreach (Vector3 s in data.sinks) FaucetSink.Create(root, s, gm);
        foreach (Vector4 s in data.cookieZones) CookieZone.Create(root, s, gm);
        foreach (Vector3 s in data.popcornPans) PopcornPan.Create(root, s, gm);
        foreach (Vector3 s in data.sodas) SodaGeyser.Create(root, s, gm);
        foreach (Vector4 s in data.blenders) BlenderVortex.Create(root, s, gm);
        if (data.mouseChase.HasValue) MouseThief.Create(root, data.mouseChase.Value, gm);
        if (data.birthdayCake) BirthdayCake.Create(root, data.door, gm);
        if (data.giantKitchen) GiantKitchenScenery.Create(root, data, gm, rng);
    }

    public static bool Touched(Bounds b)
    {
        foreach (InkMark m in Object.FindObjectsByType<InkMark>(FindObjectsSortMode.None))
        {
            if (m.beingEaten) continue;
            var r = m.GetComponent<MeshRenderer>();
            if (r != null && r.bounds.Intersects(b)) { Object.Destroy(m.gameObject); return true; }
        }
        return false;
    }

    public static InkMark ClosedIn(float x0, float x1, float y0, float y1)
    {
        foreach (InkMark m in Object.FindObjectsByType<InkMark>(FindObjectsSortMode.None))
        {
            if (!m.closed || m.beingEaten) continue;
            var r = m.GetComponent<MeshRenderer>();
            if (r == null) continue;
            Vector3 c = r.bounds.center;
            if (c.x > x0 && c.x < x1 && c.y > y0 && c.y < y1) return m;
        }
        return null;
    }

    // رويدا تطير لنقطة معينة بقوس (تستخدمها الملعقة)
    public static void Launch(RowaidaController r, Vector3 target, float time)
    {
        var rb = r.GetComponent<Rigidbody>();
        Vector3 p = rb.position;
        float g = Mathf.Abs(Physics.gravity.y);
        float vx = (target.x - p.x) / time;
        float vy = (target.y - p.y + 0.5f * g * time * time) / time;
        r.Held = true;
        r.CarryX = vx;
        rb.linearVelocity = new Vector3(vx, vy, 0f);
    }
}

// ---------- ملعقة المقلاع ----------

// ملعقة عملاقة على مكعب سكر: رويدا توقف بطرفها (الصحن). ارسمي حبة (دايرة) فوق طرف اليد: تطيح وتقلب الملعقة، ورويدا تنقذف لفوق
public class SpoonCatapult : MonoBehaviour
{
    float bowlX, top, flipT = -1f, flyT = -1f;
    Vector3 target;
    bool holding, prompted;
    Transform spoon;

    public static void Create(Transform root, Vector4 spec, GameManager gm)
    {
        var go = new GameObject("Spoon Catapult");
        go.transform.SetParent(root, false);
        var s = go.AddComponent<SpoonCatapult>();
        s.bowlX = spec.x; s.top = spec.y; s.target = new Vector3(spec.z, spec.w, 0f);
        Material steel = gm.Mat("Spoon Steel", new Color(0.82f, 0.84f, 0.88f), 0.95f, 0.85f);
        Material sugar = gm.Mat("Sugar Cube", new Color(1f, 1f, 0.98f), 0.3f);
        Material grain = gm.Mat("Sugar Grain", new Color(0.92f, 0.94f, 0.98f), 0.6f);
        // مكعب السكر (المحور)
        LevelBuilder.Visual("Sugar Cube", PrimitiveType.Cube, go.transform, new Vector3(s.bowlX + 1.7f, s.top + 0.4f, 0.9f), Vector3.one * 0.8f, sugar);
        for (int i = 0; i < 6; i++)
            LevelBuilder.Visual("Grain", PrimitiveType.Cube, go.transform, new Vector3(s.bowlX + 1.4f + i * 0.12f, s.top + 0.82f, 0.6f + (i % 2) * 0.2f), Vector3.one * 0.08f, grain, false);
        s.spoon = new GameObject("Spoon").transform;
        s.spoon.SetParent(go.transform, false);
        s.spoon.position = new Vector3(s.bowlX + 1.7f, s.top + 0.85f, 0.9f);
        LevelBuilder.Visual("Bowl", PrimitiveType.Sphere, s.spoon, new Vector3(-1.7f, 0f, -0.9f), new Vector3(1.6f, 0.35f, 1.2f), steel);
        LevelBuilder.Visual("Neck", PrimitiveType.Cube, s.spoon, new Vector3(-0.6f, 0.05f, -0.9f), new Vector3(1.4f, 0.12f, 0.3f), steel);
        LevelBuilder.Visual("Handle", PrimitiveType.Cube, s.spoon, new Vector3(1.4f, 0.08f, -0.9f), new Vector3(3f, 0.14f, 0.45f), steel);
        LevelBuilder.Visual("Handle End", PrimitiveType.Sphere, s.spoon, new Vector3(2.9f, 0.08f, -0.9f), new Vector3(0.6f, 0.15f, 0.6f), steel);
        s.spoon.localRotation = Quaternion.Euler(0f, 0f, 14f);
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time, dt = Time.deltaTime;
        RowaidaController r = gm.Rowaida;
        Vector3 handleEnd = spoon.position + spoon.rotation * new Vector3(2.9f, 0.08f, 0f);
        bool inBowl = r != null && r.transform.position.x > bowlX - 0.5f && r.transform.position.x < bowlX + 0.6f && r.transform.position.y - top < 1.4f;

        if (flipT < 0f)
        {
            spoon.localRotation = Quaternion.Euler(0f, 0f, 14f + Mathf.Sin(t * 2f) * 0.8f);
            // حبة طاحت على طرف اليد؟
            InkMark m = GiantKitchen.ClosedIn(bowlX + 2.4f, bowlX + 6f, top - 1f, top + 8f);
            if (m != null && m.GetComponent<MeshRenderer>().bounds.min.y < handleEnd.y + 0.6f)
            {
                Fx.Sparkle(m.GetComponent<MeshRenderer>().bounds.center, new Color(1f, 0.9f, 0.6f), 12);
                Destroy(m.gameObject);
                flipT = 0f;
                LevelSfx.Play("clank", 0.9f, 0.8f);
                if (inBowl)
                {
                    GiantKitchen.Launch(r, target, 1.3f);
                    flyT = 0f;
                    holding = false;
                    LevelSfx.Play("boing", 0.9f, 0.8f);
                    gm.Sound.PlaySqueak(1.3f);
                }
                else gm.UI.ShowMessage("", 2.2f, "رويدا لازم تكون بصحن الملعقة أول!");
            }
        }
        else
        {
            flipT += dt;
            float k = Mathf.Clamp01(flipT / 0.18f);
            float back = Mathf.Clamp01((flipT - 1.5f) / 0.8f);
            spoon.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(14f, -22f, k) + (22f + 14f) * back);
            if (back >= 1f && flyT < 0f) flipT = -1f;   // ما طارت: ترجع الملعقة
        }

        // وهي طايرة: لين توصل
        if (flyT >= 0f)
        {
            flyT += dt;
            var rb = r.GetComponent<Rigidbody>();
            if (Random.value < 0.3f) Fx.Sparkle(r.transform.position, new Color(1f, 0.95f, 0.7f), 2);
            if (flyT > 0.5f && rb.linearVelocity.y > -0.05f && rb.linearVelocity.y < 0.3f)
            {
                r.Held = false;
                r.CarryX = 0f;
                flyT = -2f;
                Fx.Dust(r.transform.position + Vector3.down * 0.7f, 0.5f);
            }
            if (flyT > 3f) { r.Held = false; r.CarryX = 0f; flyT = -2f; }
            return;
        }

        bool wait = flyT == -1f && inBowl && gm.IsPlaying;
        if (wait && !prompted) { prompted = true; gm.UI.ShowMessage("", 3.4f, "رويدا بصحن الملعقة! ارسمي حبة (دايرة) فوق طرف اليد الثاني"); }
        HoldHelper.Hold(ref holding, wait);
    }
}

// ---------- المغسلة ----------

// مغسلة عميقة (حفرة) وحنفية: المسي يد الحنفية بخط تنزل مويه وتتعبى. بعدين ارسمي شكل مسكر: يصير إسفنجة تطفو وتشيل رويدا للجهة الثانية
public class FaucetSink : MonoBehaviour
{
    const float Depth = 3.2f;
    float from, to, top, fill = -1f;
    bool holding, prompted, prompted2, riding, crossed;
    Transform water, stream, handle, sponge;
    readonly Dictionary<InkMark, float> seen = new Dictionary<InkMark, float>();
    AudioSource tap;
    Rigidbody spongeRb;

    float WaterTop => Mathf.Lerp(top - Depth + 0.2f, top - 0.55f, Mathf.Clamp01(fill));

    public static void Create(Transform root, Vector3 spec, GameManager gm)
    {
        var go = new GameObject("Faucet Sink");
        go.transform.SetParent(root, false);
        var s = go.AddComponent<FaucetSink>();
        s.from = spec.x; s.to = spec.y; s.top = spec.z;
        float w = s.to - s.from, cx = (s.from + s.to) * 0.5f;
        Material steel = gm.Mat("Spoon Steel", new Color(0.82f, 0.84f, 0.88f), 0.95f, 0.85f);
        Material dark = gm.Mat("Pot Dark", new Color(0.35f, 0.36f, 0.4f), 0.6f, 0.5f);
        var tex = new Texture2D(2, 2);
        tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
        tex.Apply();
        Material waterM = NightSky.Transparent("Sink Water", tex, new Color(0.55f, 0.82f, 1f, 0.55f));
        // جوانب المغسلة وقاعها
        LevelBuilder.Visual("Sink Floor", PrimitiveType.Cube, go.transform, new Vector3(cx, s.top - Depth, 0f), new Vector3(w, 0.15f, 2.4f), steel);
        LevelBuilder.Visual("Sink Back", PrimitiveType.Cube, go.transform, new Vector3(cx, s.top - Depth * 0.5f, 1.25f), new Vector3(w, Depth, 0.1f), steel);
        LevelBuilder.Visual("Drain", PrimitiveType.Cylinder, go.transform, new Vector3(cx, s.top - Depth + 0.09f, 0f), new Vector3(0.6f, 0.02f, 0.6f), dark, false);
        s.water = LevelBuilder.Visual("Water", PrimitiveType.Cube, go.transform, Vector3.zero, Vector3.one, waterM, false).transform;
        // الحنفية: عمود ورا يمين وبوز فوق المغسلة
        Vector3 basePos = new Vector3(s.to + 0.9f, s.top, 1.2f);
        LevelBuilder.Visual("Faucet Pipe", PrimitiveType.Cylinder, go.transform, basePos + new Vector3(0f, 2.2f, 0f), new Vector3(0.35f, 2.2f, 0.35f), steel);
        var arm = LevelBuilder.Visual("Faucet Arm", PrimitiveType.Cylinder, go.transform, basePos + new Vector3(-1.4f, 4.3f, -0.6f), new Vector3(0.3f, 1.5f, 0.3f), steel).transform;
        arm.localRotation = Quaternion.Euler(-20f, 0f, 80f);
        LevelBuilder.Visual("Spout", PrimitiveType.Cylinder, go.transform, new Vector3(cx + 0.5f, s.top + 4f, 0.2f), new Vector3(0.4f, 0.2f, 0.4f), steel);
        s.handle = new GameObject("Faucet Handle").transform;
        s.handle.SetParent(go.transform, false);
        s.handle.position = basePos + new Vector3(0f, 2.6f, -0.45f);
        LevelBuilder.Visual("Bar A", PrimitiveType.Cube, s.handle, Vector3.zero, new Vector3(0.9f, 0.14f, 0.14f), steel);
        LevelBuilder.Visual("Bar B", PrimitiveType.Cube, s.handle, Vector3.zero, new Vector3(0.14f, 0.9f, 0.14f), steel);
        LevelBuilder.Visual("Cap", PrimitiveType.Sphere, s.handle, new Vector3(0f, 0f, -0.1f), Vector3.one * 0.25f, gm.Mat("Lamp Blue", new Color(0.3f, 0.55f, 1.4f), 0.5f), false);
        s.stream = LevelBuilder.Visual("Stream", PrimitiveType.Cylinder, go.transform, Vector3.zero, Vector3.one, waterM, false).transform;
        s.stream.gameObject.SetActive(false);
        s.tap = LevelSfx.LoopSource(go, "faucet", 0f);
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time, dt = Time.deltaTime, cx = (from + to) * 0.5f, w = to - from;
        RowaidaController r = gm.Rowaida;

        // البالوعة تسحب أي خط (ما ينفع جسر عادي)
        ForestFeatures2.SwallowLines(from, to, top, seen, 0.9f);
        if (fill < 0f)
        {
            if (GiantKitchen.Touched(new Bounds(handle.position, new Vector3(1.3f, 1.3f, 4f))))
            {
                fill = 0f;
                LevelSfx.Play("clank", 0.6f, 1.4f);
            }
        }
        else if (fill < 1f)
        {
            fill = Mathf.Min(1f, fill + dt / 3.4f);
            handle.localRotation = Quaternion.Euler(0f, 0f, fill * 90f);
            if (fill >= 1f) LevelSfx.Play("plop", 0.6f, 0.8f);
        }
        float wt = WaterTop;
        float h = Mathf.Max(0.05f, wt - (top - Depth));
        water.position = new Vector3(cx, top - Depth + h * 0.5f, 0f);
        water.localScale = new Vector3(w - 0.05f, h, 2.3f);
        bool running = fill >= 0f && fill < 1f;
        stream.gameObject.SetActive(running);
        if (running)
        {
            float y0 = top + 3.9f, y1 = wt;
            stream.position = new Vector3(cx + 0.5f, (y0 + y1) * 0.5f, 0.2f);
            stream.localScale = new Vector3(0.28f + Mathf.Sin(t * 30f) * 0.02f, (y0 - y1) * 0.5f, 0.28f);
            if (Random.value < 0.5f)
            {
                var b = LevelBuilder.Visual("Splash", PrimitiveType.Sphere, null, new Vector3(cx + 0.5f + Random.Range(-0.3f, 0.3f), wt, Random.Range(-0.3f, 0.5f)), Vector3.one * 0.12f, gm.Mat("Air Bubble", new Color(0.8f, 0.95f, 1f), 0.98f), false);
                var fx = b.AddComponent<FxParticle>();
                fx.velocity = new Vector3(Random.Range(-1.5f, 1.5f), Random.Range(1f, 2.5f), 0f);
                fx.gravity = 8f; fx.life = 0.5f;
            }
        }
        if (tap != null) tap.volume = running && r != null ? Mathf.Clamp01(1f - Mathf.Abs(r.transform.position.x - cx) / 14f) * 0.6f : 0f;

        // إسفنجة: أي شكل مسكر فوق المويه (بعد ما تتعبى)
        if (fill >= 1f && sponge == null)
        {
            InkMark m = GiantKitchen.ClosedIn(from - 0.5f, to + 0.5f, top - Depth, top + 6f);
            if (m != null)
            {
                Destroy(m.gameObject);
                sponge = MakeSponge(gm);
                LevelSfx.Play("pop", 0.8f, 0.7f);
                LevelSfx.Play("splash", 0.6f);
            }
        }
        if (fill >= 1f && !prompted2 && sponge == null) { prompted2 = true; gm.UI.ShowMessage("", 3f, "تعبت! الحين ارسمي شكل مسكر يصير إسفنجة تطفو"); }

        // الإسفنجة تشيل رويدا لليمين
        if (sponge != null)
        {
            Vector3 p = spongeRb.position;
            p.y = Mathf.Lerp(p.y, top - 0.03f + Mathf.Sin(t * 2f) * 0.03f, dt * 4f);
            bool onSponge = r != null && Mathf.Abs(r.transform.position.x - p.x) < 1.1f && r.transform.position.y > p.y && r.transform.position.y - p.y < 1.4f;
            if (onSponge && !crossed)
            {
                riding = true;
                p.x = Mathf.MoveTowards(p.x, to - 1.05f, 1.7f * dt);
                r.Held = true;
                r.CarryX = p.x < to - 1.06f ? 1.7f : 0f;
                if (p.x >= to - 1.06f) { crossed = true; r.Held = false; r.CarryX = 0f; riding = false; }
            }
            spongeRb.MovePosition(p);
        }

        if (riding) return;
        bool ready = sponge != null && !crossed && spongeRb.position.x < from + 1.2f;
        bool wait = HoldHelper.Before(from) && !ready && !crossed;
        if (wait && !prompted) { prompted = true; gm.UI.ShowMessage("", 3.2f, "مغسلة! المسي يد الحنفية بخط عشان تتعبى مويه"); }
        HoldHelper.Hold(ref holding, wait);
    }

    Transform MakeSponge(GameManager gm)
    {
        var go = new GameObject("Sponge");
        go.transform.SetParent(transform, false);
        go.transform.position = new Vector3(from + 1.05f, top - 0.6f, 0f);
        Material yellow = gm.Mat("Sponge Yellow", new Color(1f, 0.88f, 0.3f), 0.15f);
        Material green = gm.Mat("Sponge Green", new Color(0.35f, 0.75f, 0.4f), 0.15f);
        Material hole = gm.Mat("Sponge Hole", new Color(0.85f, 0.7f, 0.2f), 0.1f);
        LevelBuilder.Visual("Sponge Body", PrimitiveType.Cube, go.transform, new Vector3(0f, -0.35f, 0f), new Vector3(2f, 0.5f, 1.4f), yellow);
        LevelBuilder.Visual("Scrub", PrimitiveType.Cube, go.transform, new Vector3(0f, -0.05f, 0f), new Vector3(2f, 0.14f, 1.4f), green);
        for (int i = 0; i < 6; i++)
            LevelBuilder.Visual("Hole", PrimitiveType.Sphere, go.transform, new Vector3(-0.7f + i * 0.28f, -0.4f, -0.71f), new Vector3(0.14f, 0.1f, 0.02f), hole, false);
        var col = go.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, -0.28f, 0f);
        col.size = new Vector3(2f, 0.64f, 1.4f);
        spongeRb = go.AddComponent<Rigidbody>();
        spongeRb.isKinematic = true;
        spongeRb.interpolation = RigidbodyInterpolation.Interpolate;
        Fx.Sparkle(go.transform.position, new Color(1f, 0.95f, 0.5f), 14);
        return go.transform;
    }
}

// ---------- الكوكيز ----------

// منطقة عجين (مربع منقط): أي شكل مسكر ترسمينه فيها ينخبز كوكيز صلب بنفس شكله بالضبط ويثبت مكانه.
// الخطوط العادية فيها تذوب (العجين طري). kind = 0: حفرة لازم كوكيز يغطيها، 1: جدار (ارسمي شكل درج)
public class CookieZone : MonoBehaviour
{
    float x0, x1, top, kind;
    bool holding, prompted, warned;
    readonly List<Transform> cookies = new List<Transform>();
    readonly Dictionary<InkMark, float> seen = new Dictionary<InkMark, float>();
    Material dough, golden, chip;

    public static void Create(Transform root, Vector4 spec, GameManager gm)
    {
        var go = new GameObject("Cookie Zone");
        go.transform.SetParent(root, false);
        var c = go.AddComponent<CookieZone>();
        c.x0 = spec.x; c.x1 = spec.y; c.top = spec.z; c.kind = spec.w;
        c.dough = gm.Mat("Cookie Dough", new Color(1f, 0.9f, 0.7f), 0.3f);
        c.golden = gm.Mat("Cookie Golden", new Color(0.88f, 0.62f, 0.3f), 0.35f);
        c.chip = gm.Mat("Choco Chip", new Color(0.3f, 0.17f, 0.1f), 0.5f);
        // إطار منقط (قطاعة كوكيز) يبين المنطقة
        Material dash = gm.Mat("Cutter Dash", new Color(0.95f, 0.55f, 0.4f), 0.3f);
        float y0 = c.top - (c.kind < 0.5f ? 0.3f : 0f), y1 = c.top + (c.kind < 0.5f ? 1.8f : 3.4f);
        for (float x = c.x0; x < c.x1; x += 0.5f)
        {
            LevelBuilder.Visual("Dash", PrimitiveType.Cube, go.transform, new Vector3(x + 0.12f, y0, -0.1f), new Vector3(0.25f, 0.06f, 0.06f), dash, false);
            LevelBuilder.Visual("Dash", PrimitiveType.Cube, go.transform, new Vector3(x + 0.12f, y1, -0.1f), new Vector3(0.25f, 0.06f, 0.06f), dash, false);
        }
        for (float y = y0; y < y1; y += 0.5f)
        {
            LevelBuilder.Visual("Dash", PrimitiveType.Cube, go.transform, new Vector3(c.x0, y + 0.12f, -0.1f), new Vector3(0.06f, 0.25f, 0.06f), dash, false);
            LevelBuilder.Visual("Dash", PrimitiveType.Cube, go.transform, new Vector3(c.x1, y + 0.12f, -0.1f), new Vector3(0.06f, 0.25f, 0.06f), dash, false);
        }
        // كومة طحين ونشابة صغيرة جنب المنطقة
        Material flour = gm.Mat("Flour", new Color(0.98f, 0.97f, 0.94f), 0.15f);
        LevelBuilder.Visual("Flour Pile", PrimitiveType.Sphere, go.transform, new Vector3(c.x0 - 0.8f, c.top, 1.2f), new Vector3(1.2f, 0.4f, 1f), flour, false);
    }

    public bool Covers(float from, float to, float y)
    {
        foreach (Transform ck in cookies)
        {
            if (ck == null) continue;
            var col = ck.GetComponent<Collider>();
            if (col == null) continue;
            Bounds b = col.bounds;
            if (b.min.x <= from + 0.35f && b.max.x >= to - 0.35f && b.max.y > y - 0.6f && b.max.y < y + 2.2f) return true;
        }
        return false;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time;
        float yMin = top - 3.5f, yMax = top + 4f;
        foreach (InkMark m in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
        {
            if (m.beingEaten) continue;
            var r = m.GetComponent<MeshRenderer>();
            if (r == null) continue;
            Vector3 c = r.bounds.center;
            if (c.x < x0 || c.x > x1 || c.y < yMin || c.y > yMax) continue;
            if (m.closed && m.outline != null && m.outline.Length > 2)
            {
                cookies.Add(Bake(m.transform.position, m.outline, gm));
                Destroy(m.gameObject);
                continue;
            }
            if (m.closed) continue;
            // خط عادي: يذوب بالعجين
            if (!seen.ContainsKey(m)) { seen[m] = t; continue; }
            if (t - seen[m] < 0.5f) continue;
            m.beingEaten = true;
            for (int k = 0; k < 4; k++) SmokeCloud.Puff(Vector3.Lerp(r.bounds.min, r.bounds.max, k / 3f), 0.8f);
            Destroy(m.gameObject);
            if (!warned) { warned = true; gm.UI.ShowMessage("", 2.6f, "العجين طري! الخط يذوب... ارسمي شكل مسكر يصير كوكيز"); }
        }
        for (int i = 0; i < cookies.Count; i++)
            if (cookies[i] != null) cookies[i].localScale = Vector3.Lerp(cookies[i].localScale, Vector3.one, Time.deltaTime * 6f);

        if (kind < 0.5f)
        {
            bool wait = HoldHelper.Before(x0) && !Covers(x0, x1, top) && !RowaidaController.BridgeCovers(x0, x1, top);
            if (wait && !prompted) { prompted = true; gm.UI.ShowMessage("", 3.4f, "قطاعة كوكيز! ارسمي شكل مسكر طويل بالمربع المنقط... ينخبز ويصير جسر"); }
            HoldHelper.Hold(ref holding, wait);
        }
        else if (!prompted && gm.Rowaida != null && gm.Rowaida.transform.position.x > x0 - 2f)
        {
            prompted = true;
            gm.UI.ShowMessage("", 3.6f, "جدار عالي! ارسمي شكل مثل الدرج (مثلث أو نجمة) بالمربع المنقط... ينخبز وتطلعين عليه");
        }
    }

    // كوكيز بنفس شكل الرسمة: وجه قدام ووجه ورا وجوانب، وحبات شوكولاتة
    Transform Bake(Vector3 center, Vector3[] outline, GameManager gm)
    {
        var go = new GameObject("Cookie");
        go.transform.SetParent(transform, false);
        go.transform.position = new Vector3(center.x, center.y, 0f);
        int n = outline.Length;
        const float half = 0.7f;
        var verts = new List<Vector3>();
        var tris = new List<int>();
        // وجه قدام (مروحة من المركز)
        verts.Add(new Vector3(0f, 0f, -half));
        for (int i = 0; i < n; i++) verts.Add(new Vector3(outline[i].x, outline[i].y, -half));
        // نعرف اتجاه الرسم (مع أو ضد عقارب الساعة)
        float area = 0f;
        for (int i = 0; i < n; i++) { Vector3 a = outline[i], b = outline[(i + 1) % n]; area += a.x * b.y - b.x * a.y; }
        bool ccw = area > 0f;
        for (int i = 0; i < n; i++)
        {
            int a = 1 + i, b = 1 + (i + 1) % n;
            if (ccw) { tris.Add(0); tris.Add(b); tris.Add(a); } else { tris.Add(0); tris.Add(a); tris.Add(b); }
        }
        // وجه ورا
        int back = verts.Count;
        verts.Add(new Vector3(0f, 0f, half));
        for (int i = 0; i < n; i++) verts.Add(new Vector3(outline[i].x, outline[i].y, half));
        for (int i = 0; i < n; i++)
        {
            int a = back + 1 + i, b = back + 1 + (i + 1) % n;
            if (ccw) { tris.Add(back); tris.Add(a); tris.Add(b); } else { tris.Add(back); tris.Add(b); tris.Add(a); }
        }
        // الجوانب
        for (int i = 0; i < n; i++)
        {
            Vector3 p0 = outline[i], p1 = outline[(i + 1) % n];
            int s = verts.Count;
            verts.Add(new Vector3(p0.x, p0.y, -half)); verts.Add(new Vector3(p1.x, p1.y, -half));
            verts.Add(new Vector3(p1.x, p1.y, half)); verts.Add(new Vector3(p0.x, p0.y, half));
            if (ccw) { tris.Add(s); tris.Add(s + 1); tris.Add(s + 2); tris.Add(s); tris.Add(s + 2); tris.Add(s + 3); }
            else { tris.Add(s); tris.Add(s + 2); tris.Add(s + 1); tris.Add(s); tris.Add(s + 3); tris.Add(s + 2); }
        }
        var mesh = new Mesh { name = "Cookie" };
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = golden;
        go.AddComponent<MeshCollider>().sharedMesh = mesh;
        // حبات شوكولاتة على الوجه
        for (int i = 0; i < Mathf.Min(10, n / 3); i++)
        {
            Vector3 p = Vector3.Lerp(Vector3.zero, outline[(i * 7) % n], Random.Range(0.25f, 0.75f));
            LevelBuilder.Visual("Chip", PrimitiveType.Sphere, go.transform, new Vector3(p.x, p.y, -half - 0.02f), new Vector3(0.18f, 0.18f, 0.08f), chip, false);
        }
        go.transform.localScale = Vector3.one * 0.85f;
        LevelSfx.Play("bake", 0.8f);
        Fx.Sparkle(go.transform.position, new Color(1f, 0.8f, 0.45f), 18);
        SmokeCloud.Puff(go.transform.position + Vector3.up * 0.5f, 1.2f);
        return go.transform;
    }
}

// ---------- الفشار ----------

// مقلاة حارة بالحفرة: كل شوي تنفجر حبات فشار كبيرة وتطير فوق الحفرة وتوقف صف (منصات) شوي، وبعدين تطيح
public class PopcornPan : MonoBehaviour
{
    const float Period = 4.4f, Hover = 2.9f;
    float from, to, top, start;
    bool holding, prompted;
    readonly List<Transform> corns = new List<Transform>();
    readonly List<BoxCollider> cols = new List<BoxCollider>();
    bool popped;

    float Local => Mathf.Repeat(Time.time - start, Period);
    // 0..0.5 طلوع، 0.5..0.5+Hover واقفة، بعدها تطيح
    float Up01 => Local < 0.5f ? Local / 0.5f : Local < 0.5f + Hover ? 1f : Mathf.Max(0f, 1f - (Local - 0.5f - Hover) / 0.5f);
    float StandLeft => Local >= 0.5f && Local < 0.5f + Hover ? 0.5f + Hover - Local : 0f;

    public static void Create(Transform root, Vector3 spec, GameManager gm)
    {
        var go = new GameObject("Popcorn Pan");
        go.transform.SetParent(root, false);
        var p = go.AddComponent<PopcornPan>();
        p.from = spec.x; p.to = spec.y; p.top = spec.z;
        p.start = Time.time;
        float w = p.to - p.from, cx = (p.from + p.to) * 0.5f;
        Material iron = gm.Mat("Pan Iron", new Color(0.22f, 0.22f, 0.25f), 0.6f, 0.4f);
        Material kernel = gm.Mat("Kernel", new Color(1f, 0.8f, 0.25f), 0.6f);
        Material pop = gm.Mat("Popcorn", new Color(1f, 0.98f, 0.9f), 0.25f);
        Material butter = gm.Mat("Butter", new Color(1f, 0.92f, 0.5f), 0.6f);
        Material flame = gm.Mat("Stove Flame", new Color(0.4f, 0.6f, 1.6f), 0.5f);
        LevelBuilder.Visual("Pan", PrimitiveType.Cylinder, go.transform, new Vector3(cx, p.top - 2.6f, 0f), new Vector3(w * 0.95f, 0.2f, 2.4f), iron);
        for (int i = 0; i < 14; i++)
            LevelBuilder.Visual("Kernel", PrimitiveType.Sphere, go.transform, new Vector3(p.from + 0.4f + Random.Range(0f, w - 0.8f), p.top - 2.38f, Random.Range(-0.8f, 0.8f)), Vector3.one * 0.18f, kernel, false);
        for (int i = 0; i < 8; i++)
        {
            float a = i / 8f * Mathf.PI * 2f;
            LevelBuilder.Visual("Flame", PrimitiveType.Capsule, go.transform, new Vector3(cx + Mathf.Cos(a) * w * 0.3f, p.top - 3f, Mathf.Sin(a) * 0.8f), new Vector3(0.2f, 0.25f, 0.2f), flame, false);
        }
        int n = Mathf.Max(2, Mathf.RoundToInt(w / 1.4f));
        float sw = w / n;
        for (int i = 0; i < n; i++)
        {
            var c = new GameObject("Popcorn");
            c.transform.SetParent(go.transform, false);
            // فشارة: كم كورة بيضا منفوخة مع لمسة زبدة
            for (int k = 0; k < 5; k++)
                LevelBuilder.Visual("Puff", PrimitiveType.Sphere, c.transform, new Vector3(Mathf.Cos(k * 1.3f) * 0.3f, Mathf.Sin(k * 1.9f) * 0.12f - 0.15f, Mathf.Sin(k * 1.3f) * 0.35f), Vector3.one * (0.75f + (k % 2) * 0.15f) * sw * 0.55f, pop);
            LevelBuilder.Visual("Butter", PrimitiveType.Sphere, c.transform, new Vector3(0.1f, 0.15f, -0.2f), Vector3.one * 0.25f, butter, false);
            var col = c.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, -0.25f, 0f);
            col.size = new Vector3(sw + 0.08f, 0.5f, 1.4f);
            var rb = c.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            p.corns.Add(c.transform);
            p.cols.Add(col);
        }
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time, w = to - from;
        float up = Up01;
        int n = corns.Count;
        float sw = w / n;
        for (int i = 0; i < n; i++)
        {
            float k = Mathf.Clamp01(up * 1.15f - i * 0.03f);
            Vector3 low = new Vector3(from + (i + 0.5f) * sw, top - 2.4f, 0f), high = new Vector3(from + (i + 0.5f) * sw, top, 0f);
            Vector3 p = Vector3.Lerp(low, high, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.6f * (Local < 0.5f ? 1f : 0f);
            corns[i].GetComponent<Rigidbody>().MovePosition(p);
            corns[i].position = p;
            corns[i].localScale = Vector3.one * Mathf.Lerp(0.3f, 1f, k);
            corns[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 3f + i) * 4f);
            cols[i].enabled = k > 0.95f;
        }
        if (Local < 0.1f && !popped)
        {
            popped = true;
            RowaidaController r0 = gm.Rowaida;
            if (r0 != null && Mathf.Abs(r0.transform.position.x - (from + to) * 0.5f) < 14f) LevelSfx.Play("popcorn", 0.8f);
        }
        if (Local > 0.2f) popped = false;

        RowaidaController r = gm.Rowaida;
        bool rowaidaOn = r != null && r.transform.position.x > from - 0.2f && r.transform.position.x < to + 0.2f;
        float cross = (w + 1.6f) / (r != null ? r.walkSpeed : 2.8f);
        bool ready = up > 0.99f && (StandLeft > cross || rowaidaOn);
        bool wait = HoldHelper.Before(from) && !ready;
        if (wait && !prompted) { prompted = true; gm.UI.ShowMessage("", 3f, "فشار! استني لين ينفجر ويصير صف... وعدي عليه بسرعة"); }
        HoldHelper.Hold(ref holding, wait);
    }
}

// ---------- الفار السارق ----------

// فار شقي سرق قلم رويدا وهرب فيه: يركض قدامها طول المرحلة ويلتفت يضحك. الحبر ينقص شوي شوي لين تمسكينه عند جحره
public class MouseThief : MonoBehaviour
{
    float startX, endX, top, nextSqueak;
    bool caught, introduced;
    Transform body, tail, pencil;
    readonly List<Transform> legs = new List<Transform>();
    float hop;

    public static void Create(Transform root, Vector3 spec, GameManager gm)
    {
        var go = new GameObject("Mouse Thief");
        go.transform.SetParent(root, false);
        var m = go.AddComponent<MouseThief>();
        m.startX = spec.x; m.endX = spec.y; m.top = spec.z;
        go.transform.position = new Vector3(m.startX, m.top, 0.3f);
        Material fur = gm.Mat("Mouse Fur", new Color(0.62f, 0.6f, 0.65f), 0.3f);
        Material pink = gm.Mat("Bunny Pink", new Color(1f, 0.7f, 0.75f), 0.3f);
        Material black = gm.Mat("Monster Pupil", new Color(0.05f, 0.04f, 0.06f), 0.9f);
        Material white = gm.Mat("Monster Eye", Color.white, 0.7f);
        Material yellow = gm.Mat("Big Pencil", new Color(1f, 0.75f, 0.2f), 0.35f);
        Material wood = gm.Mat("Pencil Wood", new Color(0.96f, 0.76f, 0.55f), 0.3f);
        m.body = new GameObject("Body").transform;
        m.body.SetParent(go.transform, false);
        LevelBuilder.Visual("Torso", PrimitiveType.Sphere, m.body, new Vector3(0f, 0.45f, 0f), new Vector3(0.9f, 0.65f, 0.6f), fur);
        LevelBuilder.Visual("Head", PrimitiveType.Sphere, m.body, new Vector3(0.5f, 0.6f, 0f), new Vector3(0.5f, 0.45f, 0.45f), fur);
        LevelBuilder.Visual("Snout", PrimitiveType.Sphere, m.body, new Vector3(0.78f, 0.55f, 0f), new Vector3(0.25f, 0.2f, 0.22f), fur, false);
        LevelBuilder.Visual("Nose", PrimitiveType.Sphere, m.body, new Vector3(0.9f, 0.57f, 0f), Vector3.one * 0.09f, pink, false);
        for (int s = -1; s <= 1; s += 2)
        {
            LevelBuilder.Visual("Ear", PrimitiveType.Sphere, m.body, new Vector3(0.4f, 0.9f, s * 0.2f), new Vector3(0.3f, 0.32f, 0.08f), fur, false);
            LevelBuilder.Visual("Ear In", PrimitiveType.Sphere, m.body, new Vector3(0.38f, 0.9f, s * 0.2f - s * 0.02f), new Vector3(0.2f, 0.22f, 0.06f), pink, false);
            LevelBuilder.Visual("Eye", PrimitiveType.Sphere, m.body, new Vector3(0.66f, 0.7f, s * 0.14f), Vector3.one * 0.13f, white, false);
            LevelBuilder.Visual("Pupil", PrimitiveType.Sphere, m.body, new Vector3(0.71f, 0.71f, s * 0.15f), Vector3.one * 0.07f, black, false);
            LevelBuilder.Visual("Whisker", PrimitiveType.Cube, m.body, new Vector3(0.88f, 0.52f, s * 0.15f), new Vector3(0.02f, 0.02f, 0.35f), black, false);
        }
        for (int i = 0; i < 4; i++)
            m.legs.Add(LevelBuilder.Visual("Leg", PrimitiveType.Capsule, m.body, new Vector3(i < 2 ? 0.25f : -0.25f, 0.12f, i % 2 == 0 ? -0.18f : 0.18f), new Vector3(0.1f, 0.12f, 0.1f), pink, false).transform);
        m.tail = new GameObject("Tail").transform;
        m.tail.SetParent(m.body, false);
        m.tail.localPosition = new Vector3(-0.45f, 0.4f, 0f);
        for (int i = 0; i < 6; i++)
            LevelBuilder.Visual("Tail Seg", PrimitiveType.Sphere, m.tail, new Vector3(-i * 0.13f, Mathf.Sin(i * 0.7f) * 0.1f + i * 0.04f, 0f), Vector3.one * (0.09f - i * 0.008f), pink, false);
        // القلم المسروق على ظهره
        m.pencil = new GameObject("Stolen Pencil").transform;
        m.pencil.SetParent(m.body, false);
        m.pencil.localPosition = new Vector3(-0.05f, 0.88f, 0f);
        m.pencil.localRotation = Quaternion.Euler(0f, 0f, 12f);
        var p = LevelBuilder.Visual("Pencil Body", PrimitiveType.Cylinder, m.pencil, Vector3.zero, new Vector3(0.18f, 0.6f, 0.18f), yellow).transform;
        p.localRotation = Quaternion.Euler(0f, 0f, 90f);
        LevelBuilder.Visual("Tip", PrimitiveType.Capsule, m.pencil, new Vector3(0.7f, 0f, 0f), new Vector3(0.14f, 0.12f, 0.14f), wood, false).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
    }

    float GroundAt(float x)
    {
        if (Physics.Raycast(new Vector3(x, top + 6f, 0f), Vector3.down, out RaycastHit hit, 12f, ~0, QueryTriggerInteraction.Ignore) && hit.collider.GetComponentInParent<RowaidaController>() == null)
            return hit.point.y;
        return float.NaN;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time, dt = Time.deltaTime;
        RowaidaController r = gm.Rowaida;
        if (r == null) return;
        if (!introduced && gm.IsPlaying) { introduced = true; gm.UI.ShowMessage("", 3.4f, "فار شقي سرق قلمك! الحبر بينقص... لحقيه لين جحره"); LevelSfx.Play("mouse", 0.8f); }

        Vector3 p = transform.position;
        if (!caught)
        {
            // يركض قدامها، ويوقف ينتظر لو بعدت عنه
            float goal = Mathf.Clamp(r.transform.position.x + 5.5f, startX, endX);
            float speed = goal > p.x ? 4.2f : 0f;
            p.x = Mathf.MoveTowards(p.x, goal, speed * dt);
            float g = GroundAt(p.x);
            bool overGap = float.IsNaN(g);
            hop = overGap ? hop + dt * 6f : 0f;
            p.y = overGap ? top + 1.2f + Mathf.Sin(Mathf.Min(hop, Mathf.PI)) * 0.8f : Mathf.Lerp(p.y, g, dt * 12f);
            transform.position = p;
            bool running = speed > 0f && Mathf.Abs(goal - p.x) > 0.05f;
            body.localRotation = Quaternion.Euler(0f, running ? 0f : 180f * Mathf.Clamp01(Mathf.Sin(t * 1.5f) * 3f), 0f);
            for (int i = 0; i < legs.Count; i++) legs[i].localRotation = Quaternion.Euler(0f, 0f, running ? Mathf.Sin(t * 25f + i * 1.6f) * 40f : 0f);
            tail.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 8f) * 20f);
            body.localPosition = new Vector3(0f, running ? Mathf.Abs(Mathf.Sin(t * 20f)) * 0.08f : 0f, 0f);
            if (t > nextSqueak && Mathf.Abs(r.transform.position.x - p.x) < 9f) { nextSqueak = t + Random.Range(2.5f, 5f); LevelSfx.Play("mouse", 0.5f, Random.Range(0.9f, 1.2f)); }
            // الحبر ينقص شوي شوي
            if (gm.IsPlaying) gm.UseInk(0.12f * dt);
            // مسكتيه عند الجحر
            if (p.x >= endX - 0.05f && Mathf.Abs(r.transform.position.x - p.x) < 1.6f)
            {
                caught = true;
                pencil.SetParent(null, true);
                var fx = pencil.gameObject.AddComponent<FxParticle>();
                fx.velocity = new Vector3(-1.5f, 3f, 0f);
                fx.gravity = 8f; fx.spin = new Vector3(0f, 0f, 300f); fx.life = 1.2f;
                gm.RefundInk(6f);
                LevelSfx.Play("sticker", 0.8f);
                LevelSfx.Play("mouse", 0.9f, 1.4f);
                Fx.Sparkle(p + Vector3.up, new Color(1f, 0.9f, 0.4f), 24);
                gm.UI.ShowMessage("", 2.6f, "رجعتي القلم! والفار هرب لجحره");
            }
        }
        else
        {
            // يدخل الجحر ويختفي
            p.z = Mathf.MoveTowards(p.z, 2.5f, dt * 2.5f);
            transform.position = p;
            transform.localScale = Vector3.one * Mathf.MoveTowards(transform.localScale.x, 0f, dt * 0.6f);
        }
    }
}

// ---------- الصودا ----------

// قارورة صودا عملاقة جنب رف عالي: رويدا توقف عندها. ارسمي حبة نعناع (دايرة) فوق القارورة: تفور نافورة رغوة ترفع رويدا للرف
public class SodaGeyser : MonoBehaviour
{
    float x, top, height, eruptT = -1f, foamX;
    bool riding;
    bool holding, prompted, delivered;
    Transform foam, spray;
    Rigidbody foamRb;
    readonly List<Transform> bubbles = new List<Transform>();

    public static void Create(Transform root, Vector3 spec, GameManager gm)
    {
        var go = new GameObject("Soda Geyser");
        go.transform.SetParent(root, false);
        var s = go.AddComponent<SodaGeyser>();
        s.x = spec.x; s.top = spec.y; s.height = spec.z;
        var tex = new Texture2D(2, 2);
        tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
        tex.Apply();
        Material bottle = NightSky.Transparent("Soda Bottle", tex, new Color(0.35f, 0.85f, 0.45f, 0.55f));
        Material soda = gm.Mat("Soda Drink", new Color(0.45f, 0.3f, 0.15f), 0.8f);
        Material label = gm.Mat("Soda Label", new Color(0.95f, 0.25f, 0.3f), 0.4f);
        Material foamM = gm.Mat("Foam", new Color(1f, 0.97f, 0.9f), 0.2f);
        Vector3 b = new Vector3(s.x + 0.2f, s.top, 1.3f);
        LevelBuilder.Visual("Soda", PrimitiveType.Cylinder, go.transform, b + new Vector3(0f, 1.3f, 0f), new Vector3(1.05f, 1.2f, 1.05f), soda, false);
        LevelBuilder.Visual("Bottle", PrimitiveType.Cylinder, go.transform, b + new Vector3(0f, 1.6f, 0f), new Vector3(1.2f, 1.6f, 1.2f), bottle, false);
        LevelBuilder.Visual("Neck", PrimitiveType.Cylinder, go.transform, b + new Vector3(0f, 3.6f, 0f), new Vector3(0.5f, 0.5f, 0.5f), bottle, false);
        LevelBuilder.Visual("Label", PrimitiveType.Cylinder, go.transform, b + new Vector3(0f, 1.6f, 0f), new Vector3(1.22f, 0.4f, 1.22f), label, false);
        s.foam = new GameObject("Foam Lift").transform;
        s.foam.SetParent(go.transform, false);
        s.foam.position = new Vector3(s.x, s.top - 0.3f, 0f);
        for (int i = 0; i < 6; i++)
            LevelBuilder.Visual("Foam Puff", PrimitiveType.Sphere, s.foam, new Vector3(-1f + i * 0.4f, -0.1f + Mathf.Sin(i * 2f) * 0.1f, Mathf.Sin(i * 1.1f) * 0.4f), Vector3.one * 0.9f, foamM);
        var col = s.foam.gameObject.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, -0.1f, 0f);
        col.size = new Vector3(2.6f, 0.4f, 1.4f);
        s.foamRb = s.foam.gameObject.AddComponent<Rigidbody>();
        s.foamRb.isKinematic = true;
        s.foamRb.interpolation = RigidbodyInterpolation.Interpolate;
        s.spray = new GameObject("Spray").transform;
        s.spray.SetParent(go.transform, false);
        for (int i = 0; i < 8; i++) s.bubbles.Add(LevelBuilder.Visual("Column", PrimitiveType.Sphere, s.spray, Vector3.zero, Vector3.one * 0.9f, foamM, false).transform);
        s.spray.gameObject.SetActive(false);
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time, dt = Time.deltaTime;
        RowaidaController r = gm.Rowaida;
        bool atBottle = r != null && r.transform.position.x > x + 0.4f && r.transform.position.x < x + 1.8f && r.transform.position.y - top < 1.4f;

        if (eruptT < 0f)
        {
            InkMark m = GiantKitchen.ClosedIn(x - 1.5f, x + 1.8f, top + 1f, top + 9f);
            if (m != null)
            {
                Destroy(m.gameObject);
                eruptT = 0f;
                riding = atBottle;
                foamX = atBottle ? r.transform.position.x + 0.3f : x + 1f;
                LevelSfx.Play("fizz", 1f);
                LevelSfx.Play("pop", 0.7f, 1.4f);
                Fx.Sparkle(new Vector3(x, top + 3.6f, 1f), new Color(0.7f, 1f, 0.7f), 16);
            }
        }
        else
        {
            eruptT += dt;
            // الرغوة تطلع بثلاث ثواني، توقف فوق شوي، وبعدين تنزل
            float rise = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(eruptT / 2.6f));
            float down = Mathf.Clamp01((eruptT - 5.5f) / 1.5f);
            float h = height * rise * (1f - down);
            Vector3 fp = new Vector3(foamX, top - 0.3f + h + 0.3f, 0f);
            foamRb.MovePosition(fp);
            spray.gameObject.SetActive(h > 0.1f);
            for (int i = 0; i < bubbles.Count; i++)
            {
                float k = (i + 0.5f) / bubbles.Count;
                bubbles[i].position = new Vector3(x + Mathf.Sin(t * 8f + i) * 0.15f, top + h * k - 0.3f, 0f);
                bubbles[i].localScale = Vector3.one * (0.9f + Mathf.Sin(t * 10f + i * 2f) * 0.15f);
            }
            if (Random.value < 0.5f && h > 0.2f)
            {
                var d = LevelBuilder.Visual("Drop", PrimitiveType.Sphere, null, fp + new Vector3(Random.Range(-0.5f, 0.5f), 0.2f, Random.Range(-0.5f, 0.5f)), Vector3.one * Random.Range(0.12f, 0.25f), GameManager.Instance.Mat("Foam", new Color(1f, 0.97f, 0.9f), 0.2f), false);
                var fx = d.AddComponent<FxParticle>();
                fx.velocity = new Vector3(Random.Range(-2f, 2f), Random.Range(1f, 3f), Random.Range(-1f, 1f));
                fx.gravity = 8f; fx.life = 0.7f;
            }
            if (rise >= 1f && !delivered && r != null && r.transform.position.y > top + height * 0.8f) { delivered = true; riding = false; holding = false; r.Held = false; }
            if (eruptT > 7.5f) { eruptT = -1f; delivered = false; spray.gameObject.SetActive(false); foamRb.MovePosition(new Vector3(x, top - 0.3f, 0f)); riding = false; }
        }

        bool wait = ((atBottle && eruptT < 0f) || riding) && !delivered && gm.IsPlaying;
        if (wait && !prompted) { prompted = true; gm.UI.ShowMessage("", 3.4f, "قارورة صودا! ارسمي حبة نعناع (دايرة) فوقها... وتفور!"); }
        HoldHelper.Hold(ref holding, wait);
    }
}

// ---------- الخلاط ----------

// خلاط عملاق ورا حفرة: يشتغل ويطفي، ولما يشتغل يشفط أي خط قريب ويقطعه. ارسمي الجسر وهو طافي، أو ارسمي دايرة فوق الخلاط تصير غطاه ويطفي على طول
public class BlenderVortex : MonoBehaviour
{
    const float Period = 6f, OnTime = 3.4f;
    float from, to, top, x, start;
    bool holding, prompted, lidded;
    Transform blades, lid;
    AudioSource motor;
    readonly Dictionary<InkMark, float> grab = new Dictionary<InkMark, float>();

    float Local => Mathf.Repeat(Time.time - start, Period);
    bool On => !lidded && Local < OnTime;
    float OffLeft => On ? 0f : lidded ? 99f : Period - Local;

    public static void Create(Transform root, Vector4 spec, GameManager gm)
    {
        var go = new GameObject("Blender");
        go.transform.SetParent(root, false);
        var b = go.AddComponent<BlenderVortex>();
        b.from = spec.x; b.to = spec.y; b.top = spec.z; b.x = spec.w;
        b.start = Time.time;
        var tex = new Texture2D(2, 2);
        tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
        tex.Apply();
        Material glass = NightSky.Transparent("Blender Glass", tex, new Color(0.8f, 0.92f, 1f, 0.35f));
        Material baseM = gm.Mat("Blender Base", new Color(0.95f, 0.45f, 0.5f), 0.6f, 0.2f);
        Material steel = gm.Mat("Spoon Steel", new Color(0.82f, 0.84f, 0.88f), 0.95f, 0.85f);
        Material smoothie = gm.Mat("Smoothie", new Color(1f, 0.55f, 0.65f), 0.6f);
        Vector3 bp = new Vector3(b.x, b.top - 3f, 2f);
        LevelBuilder.Visual("Base", PrimitiveType.Cube, go.transform, bp + new Vector3(0f, 0.9f, 0f), new Vector3(2.4f, 1.8f, 2.2f), baseM);
        LevelBuilder.Visual("Button", PrimitiveType.Cylinder, go.transform, bp + new Vector3(0f, 0.9f, -1.12f), new Vector3(0.45f, 0.05f, 0.45f), steel, false).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        LevelBuilder.Visual("Smoothie", PrimitiveType.Cylinder, go.transform, bp + new Vector3(0f, 2.6f, 0f), new Vector3(1.8f, 0.8f, 1.8f), smoothie, false);
        LevelBuilder.Visual("Jar", PrimitiveType.Cylinder, go.transform, bp + new Vector3(0f, 3.9f, 0f), new Vector3(2.1f, 2.1f, 2.1f), glass, false);
        b.blades = new GameObject("Blades").transform;
        b.blades.SetParent(go.transform, false);
        b.blades.position = bp + new Vector3(0f, 2f, 0f);
        for (int i = 0; i < 4; i++) LevelBuilder.Visual("Blade", PrimitiveType.Cube, b.blades, Vector3.zero, new Vector3(1.4f, 0.05f, 0.25f), steel, false).transform.localRotation = Quaternion.Euler(i * 10f, i * 45f, 0f);
        b.lid = LevelBuilder.Visual("Lid", PrimitiveType.Cylinder, go.transform, bp + new Vector3(0f, 6.1f, 0f), new Vector3(2.2f, 0.18f, 2.2f), baseM).transform;
        b.lid.gameObject.SetActive(false);
        b.motor = LevelSfx.LoopSource(go, "blender", 0f);
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time, dt = Time.deltaTime;
        RowaidaController r = gm.Rowaida;
        if (!lidded && GiantKitchen.ClosedIn(x - 2f, x + 2f, top + 1f, top + 8f) is InkMark m)
        {
            Destroy(m.gameObject);
            lidded = true;
            lid.gameObject.SetActive(true);
            LevelSfx.Play("clank", 0.7f, 1.2f);
            gm.UI.ShowMessage("", 2.2f, "سكرتي الخلاط! طفى");
        }
        bool on = On;
        blades.Rotate(0f, (on ? 1400f : 30f) * dt, 0f, Space.World);
        if (motor != null) { motor.volume = on && r != null ? Mathf.Clamp01(1f - Mathf.Abs(r.transform.position.x - x) / 14f) * 0.5f : 0f; motor.pitch = 1f; }
        // يشفط الخطوط القريبة
        if (on)
            foreach (InkMark mk in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
            {
                if (mk.beingEaten) continue;
                var mr = mk.GetComponent<MeshRenderer>();
                if (mr == null || mr.bounds.max.x < from - 1f || mr.bounds.min.x > to + 1f || mr.bounds.center.y > top + 3f) continue;
                if (r != null && r.transform.position.x > from - 0.3f && r.transform.position.x < to + 0.3f) continue;
                if (!grab.ContainsKey(mk)) grab[mk] = t;
                float age = t - grab[mk];
                mk.transform.position = Vector3.Lerp(mk.transform.position, new Vector3(x, top - 1f, 2f), dt * 2.5f);
                mk.transform.Rotate(0f, 0f, 400f * dt);
                if (age > 0.8f) { Fx.Sparkle(mk.transform.position, new Color(1f, 0.6f, 0.7f), 10); LevelSfx.Play("snip", 0.5f); Destroy(mk.gameObject); }
            }
        // تموج سحب (أوراق صغيرة تدور)
        if (on && Random.value < 0.3f)
        {
            var bit = LevelBuilder.Visual("Swirl", PrimitiveType.Cube, null, new Vector3(Random.Range(from, to), top + Random.Range(-1f, 1f), Random.Range(-0.5f, 0.5f)), Vector3.one * 0.08f, gm.Mat("Flour", new Color(0.98f, 0.97f, 0.94f), 0.15f), false);
            var fx = bit.AddComponent<FxParticle>();
            fx.velocity = (new Vector3(x, top - 1f, 2f) - bit.transform.position) * 1.5f;
            fx.life = 0.6f;
        }

        bool rowaidaOn = r != null && r.transform.position.x > from - 0.3f && r.transform.position.x < to + 0.3f;
        float cross = (to - from + 1.6f) / (r != null ? r.walkSpeed : 2.8f);
        bool safe = RowaidaController.BridgeCovers(from, to, top) && (OffLeft > cross || rowaidaOn);
        bool wait = HoldHelper.Before(from) && !safe;
        if (wait && !prompted) { prompted = true; gm.UI.ShowMessage("", 3.6f, "خلاط يشفط الخطوط! ارسمي الجسر وهو طافي... أو دايرة فوقه تصير غطاه"); }
        HoldHelper.Hold(ref holding, wait);
    }
}

// ---------- كيكة عيد الميلاد ----------

// كيكة عيد ميلاد كبيرة ورا الباب بشموع تولع، وأعلام فوقها
public class BirthdayCake : MonoBehaviour
{
    readonly List<Transform> flames = new List<Transform>();
    Vector3 door;
    bool cheered;

    public static void Create(Transform root, Vector3 door, GameManager gm)
    {
        var go = new GameObject("Birthday Cake");
        go.transform.SetParent(root, false);
        var c = go.AddComponent<BirthdayCake>();
        c.door = door;
        Material sponge = gm.Mat("Cake Sponge", new Color(1f, 0.85f, 0.65f), 0.3f);
        Material pink = gm.Mat("Cake Pink", new Color(1f, 0.7f, 0.82f), 0.5f);
        Material cream = gm.Mat("Cake Cream", new Color(1f, 0.95f, 0.97f), 0.5f);
        Material candle = gm.Mat("Candle", new Color(0.6f, 0.85f, 1f), 0.4f);
        Material flame = gm.Mat("Candle Flame", new Color(2f, 1.3f, 0.3f), 0.5f);
        Vector3 b = new Vector3(door.x + 2.6f, door.y, 3f);
        float[] r = { 3.2f, 2.4f, 1.6f };
        float y = 0f;
        for (int i = 0; i < 3; i++)
        {
            float h = 1.2f - i * 0.15f;
            LevelBuilder.Visual("Tier", PrimitiveType.Cylinder, go.transform, b + new Vector3(0f, y + h * 0.5f, 0f), new Vector3(r[i], h * 0.5f, r[i]), i % 2 == 0 ? pink : sponge);
            LevelBuilder.Visual("Frosting", PrimitiveType.Cylinder, go.transform, b + new Vector3(0f, y + h, 0f), new Vector3(r[i] + 0.1f, 0.06f, r[i] + 0.1f), cream, false);
            y += h;
        }
        for (int i = 0; i < 5; i++)
        {
            float a = i / 5f * Mathf.PI * 2f;
            Vector3 cp = b + new Vector3(Mathf.Cos(a) * 0.5f, y + 0.4f, Mathf.Sin(a) * 0.5f);
            LevelBuilder.Visual("Candle", PrimitiveType.Cylinder, go.transform, cp, new Vector3(0.14f, 0.4f, 0.14f), candle, false);
            c.flames.Add(LevelBuilder.Visual("Flame", PrimitiveType.Sphere, go.transform, cp + Vector3.up * 0.55f, new Vector3(0.14f, 0.26f, 0.14f), flame, false).transform);
        }
    }

    void Update()
    {
        float t = Time.time;
        for (int i = 0; i < flames.Count; i++) flames[i].localScale = new Vector3(0.14f, 0.26f + Mathf.Sin(t * 12f + i) * 0.05f, 0.14f);
        GameManager gm = GameManager.Instance;
        if (!cheered && gm != null && gm.Rowaida != null && Mathf.Abs(gm.Rowaida.transform.position.x - door.x) < 3f)
        {
            cheered = true;
            LevelSfx.Play("applause", 0.6f);
            for (int i = 0; i < 30; i++)
            {
                var bit = LevelBuilder.Visual("Confetti", PrimitiveType.Cube, null, door + new Vector3(Random.Range(-1f, 4f), Random.Range(3f, 5f), Random.Range(-0.5f, 2f)), Vector3.one * 0.14f, gm.Mat("Crayon Fall " + i % 4, MusicFeatures.KeyColors[i % 4], 0.4f), false);
                bit.transform.rotation = Random.rotation;
                var fx = bit.AddComponent<FxParticle>();
                fx.velocity = new Vector3(Random.Range(-2f, 2f), Random.Range(1f, 4f), 0f);
                fx.gravity = 4f; fx.drag = 0.8f; fx.flutter = 1f; fx.spin = Random.insideUnitSphere * 400f; fx.life = 2f;
            }
        }
    }
}

// ---------- منظر المطبخ العملاق ----------

// رويدا صغيرة: ورا الطريق أدوات وأكل عملاق (صحن، كوب، شوكة، تفاحة، خبزة، ملاحة)، فتات سكر على الصفحات، وضو شباك
public class GiantKitchenScenery : MonoBehaviour
{
    readonly List<Transform> steam = new List<Transform>();

    public static void Create(Transform root, LevelData data, GameManager gm, System.Random rng)
    {
        var go = new GameObject("Giant Kitchen");
        go.transform.SetParent(root, false);
        var s = go.AddComponent<GiantKitchenScenery>();
        float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        float minX = float.MaxValue, maxX = float.MinValue;
        foreach (Block b in data.blocks) { minX = Mathf.Min(minX, b.center.x - b.size.x * 0.5f); maxX = Mathf.Max(maxX, b.center.x + b.size.x * 0.5f); }
        Transform p = go.transform;

        // جدار بلاط كبير جدًا (عشان رويدا صغيرة)
        var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat };
        for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
                tex.SetPixel(x, y, x % 32 < 1 || y % 32 < 1 ? new Color(0.8f, 0.82f, 0.85f) : new Color(0.97f, 0.96f, 0.93f));
        tex.Apply();
        var wall = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "Giant Tile" };
        wall.SetTexture("_BaseMap", tex);
        wall.SetColor("_BaseColor", Color.white);
        float len = maxX - minX + 60f;
        wall.SetTextureScale("_BaseMap", new Vector2(len / 9f, 30f / 9f));
        LevelBuilder.Visual("Giant Wall", PrimitiveType.Quad, p, new Vector3((minX + maxX) * 0.5f, 6f, 26f), new Vector3(len, 30f, 1f), wall, false);
        // سطح طاولة خشب تحت الصفحات
        Material counter = gm.Mat("Counter Wood", new Color(0.78f, 0.6f, 0.42f), 0.35f);
        LevelBuilder.Visual("Counter", PrimitiveType.Cube, p, new Vector3((minX + maxX) * 0.5f, -4.5f, 6f), new Vector3(len, 1f, 30f), counter, false);

        Material plate = gm.Mat("Giant Plate", new Color(0.97f, 0.97f, 0.98f), 0.8f);
        Material plateRim = gm.Mat("Plate Rim", new Color(0.4f, 0.65f, 0.9f), 0.7f);
        Material cup = gm.Mat("Giant Cup", new Color(1f, 0.6f, 0.5f), 0.7f);
        Material steel = gm.Mat("Spoon Steel", new Color(0.82f, 0.84f, 0.88f), 0.95f, 0.85f);
        Material apple = gm.Mat("Apple", new Color(0.95f, 0.25f, 0.25f), 0.6f);
        Material bread = gm.Mat("Toast", new Color(0.95f, 0.75f, 0.45f), 0.3f);
        Material crust = gm.Mat("Toast Crust", new Color(0.7f, 0.45f, 0.2f), 0.3f);
        Material leaf = gm.Mat("Leaf Green", new Color(0.35f, 0.72f, 0.3f), 0.35f);
        int k = 0;
        for (float x = minX - 10f; x < maxX + 10f; x += R(8f, 12f), k++)
        {
            float z = R(9f, 14f);
            switch (k % 5)
            {
                case 0: // صحن واقف
                    var pl = LevelBuilder.Visual("Giant Plate", PrimitiveType.Cylinder, p, new Vector3(x, 2.5f, z + 4f), new Vector3(9f, 0.15f, 9f), plate, false).transform;
                    pl.localRotation = Quaternion.Euler(80f, 0f, 0f);
                    LevelBuilder.Visual("Rim", PrimitiveType.Cylinder, pl, new Vector3(0f, -0.6f, 0f), new Vector3(1.02f, 0.5f, 1.02f), plateRim, false);
                    break;
                case 1: // كوب بخاره يطلع
                    LevelBuilder.Visual("Giant Cup", PrimitiveType.Cylinder, p, new Vector3(x, -1.2f, z), new Vector3(4f, 3f, 4f), cup, false);
                    LevelBuilder.Visual("Cup Handle", PrimitiveType.Capsule, p, new Vector3(x + 2.3f, -0.8f, z), new Vector3(0.6f, 1.4f, 0.6f), cup, false);
                    LevelBuilder.Visual("Tea", PrimitiveType.Cylinder, p, new Vector3(x, 1.75f, z), new Vector3(3.6f, 0.05f, 3.6f), gm.Mat("Tea", new Color(0.6f, 0.35f, 0.15f), 0.9f), false);
                    var st = new GameObject("Steam").transform;
                    st.SetParent(p, false);
                    st.position = new Vector3(x, 2f, z);
                    s.steam.Add(st);
                    break;
                case 2: // شوكة واقفة
                    var fork = new GameObject("Giant Fork").transform;
                    fork.SetParent(p, false);
                    fork.position = new Vector3(x, -3f, z);
                    fork.localRotation = Quaternion.Euler(0f, 0f, R(-8f, 8f));
                    LevelBuilder.Visual("Handle", PrimitiveType.Cube, fork, new Vector3(0f, 4f, 0f), new Vector3(0.6f, 8f, 0.25f), steel, false);
                    for (int i = 0; i < 4; i++) LevelBuilder.Visual("Tine", PrimitiveType.Cube, fork, new Vector3(-0.75f + i * 0.5f, 9.2f, 0f), new Vector3(0.18f, 2.4f, 0.2f), steel, false);
                    LevelBuilder.Visual("Bridge", PrimitiveType.Cube, fork, new Vector3(0f, 8f, 0f), new Vector3(1.8f, 0.4f, 0.25f), steel, false);
                    break;
                case 3: // تفاحة
                    LevelBuilder.Visual("Giant Apple", PrimitiveType.Sphere, p, new Vector3(x, -1f, z), new Vector3(6f, 5.5f, 6f), apple, false);
                    LevelBuilder.Visual("Stem", PrimitiveType.Cylinder, p, new Vector3(x, 1.9f, z), new Vector3(0.3f, 0.6f, 0.3f), crust, false);
                    LevelBuilder.Visual("Leaf", PrimitiveType.Sphere, p, new Vector3(x + 0.8f, 2.3f, z), new Vector3(1.4f, 0.2f, 0.7f), leaf, false);
                    break;
                default: // خبزة
                    LevelBuilder.Visual("Giant Bread", PrimitiveType.Cube, p, new Vector3(x, -1.3f, z), new Vector3(6f, 3f, 2.5f), bread, false);
                    LevelBuilder.Visual("Bread Top", PrimitiveType.Capsule, p, new Vector3(x, 0.2f, z), new Vector3(3f, 3.1f, 2.5f), crust, false).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                    break;
            }
        }
        // فتات سكر وطحين على الصفحات
        Material sugar = gm.Mat("Sugar Cube", new Color(1f, 1f, 0.98f), 0.3f);
        foreach (Block b in data.blocks)
        {
            float top = b.center.y + b.size.y * 0.5f, x0 = b.center.x - b.size.x * 0.5f, x1 = b.center.x + b.size.x * 0.5f;
            for (float x = x0 + 0.7f; x < x1 - 0.5f; x += R(1.2f, 2.6f))
            {
                if (rng.Next(3) == 0) LevelBuilder.Visual("Sugar Cube", PrimitiveType.Cube, p, new Vector3(x, top + 0.25f, R(1.2f, 1.8f)), Vector3.one * 0.5f, sugar);
                else LevelBuilder.Visual("Crumb", PrimitiveType.Sphere, p, new Vector3(x, top + 0.08f, R(-1.8f, 1.8f)), new Vector3(R(0.15f, 0.3f), 0.12f, R(0.15f, 0.3f)), crust, false);
            }
        }
        // ضو شباك قوي (أشعة)
        var rt = new Texture2D(4, 32, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < 32; y++) for (int x = 0; x < 4; x++) rt.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Sin(y / 31f * Mathf.PI) * 0.5f));
        rt.Apply();
        var rayMat = NightSky.Transparent("Window Ray", rt, new Color(1.3f, 1.15f, 0.8f, 0.16f));
        for (float x = minX; x < maxX; x += R(10f, 16f))
        {
            var ray = LevelBuilder.Visual("Window Ray", PrimitiveType.Quad, p, new Vector3(x, 5f, 5f), new Vector3(20f, R(2f, 3.5f), 1f), rayMat, false).transform;
            ray.localRotation = Quaternion.Euler(0f, 0f, -55f);
        }
    }

    void Update()
    {
        // بخار الكوب
        foreach (Transform st in steam)
            if (Random.value < 0.08f) SmokeCloud.Puff(st.position + new Vector3(Random.Range(-1f, 1f), 0f, 0f), Random.Range(1.5f, 2.5f));
    }
}
