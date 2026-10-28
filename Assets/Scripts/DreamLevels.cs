using System.Collections.Generic;
using UnityEngine;

// الجزء الثالث — فصل الأحلام (57 لين 59): بين الغيوم (خطك يصير غيمة تنططها، ريح نعسانة تبي مخدة، خرفان تعدينها وتصير جسر)،
// سماء النجوم (توصلين النجوم وتطلع كوكبة جسر، قمر نايم تصحينه يشيل الظلام، فانوس طاير يرفعها)،
// والكابوس والنور (ظل يلحقها ويخاف من اللمبات، وحش بالظلام ترسمين له ابتسامة، ومنبه يصحيها من الحلم)
public static class DreamFeatures
{
    public static void Build(Transform root, LevelData data, GameManager gm, System.Random rng)
    {
        foreach (Vector4 s in data.cloudBounces) CloudBounce.Create(root, s, gm);
        foreach (Vector4 s in data.sleepyWinds) SleepyWind.Create(root, s, gm);
        foreach (Vector3 s in data.sheepBridges) SheepBridge.Create(root, s, gm);
        foreach (Vector4 s in data.constellations) Constellation.Create(root, s, gm);
        foreach (Vector4 s in data.moonFogs) MoonFog.Create(root, s, gm);
        foreach (Vector3 s in data.lanternLifts) LanternLift.Create(root, s, gm);
        if (data.shadowChase.z > 0f) ShadowChase.Create(root, data.shadowChase, gm);
        foreach (Vector2 s in data.scaryEyes) ScaryEyes.Create(root, s, gm);
        foreach (Vector2 s in data.alarmWakes) AlarmWake.Create(root, s, gm);
        if (data.dreamStyle > 0) DreamScenery.Create(root, data, gm, rng);
    }

    static Material hidden;

    // نخفي شكل الحبر (ونبني بداله شكل ثاني)
    public static void HideInk(InkMark m)
    {
        if (hidden == null) hidden = LabFeatures2.Tint("Hidden Ink", new Color(1f, 1f, 1f, 0f));
        var mr = m.GetComponent<MeshRenderer>();
        if (mr != null) mr.sharedMaterial = hidden;
        foreach (Renderer r in m.GetComponentsInChildren<Renderer>()) if (r != mr) r.enabled = false;
    }

    public static void Freeze(InkMark m, bool colliders)
    {
        var rb = m.GetComponent<Rigidbody>();
        if (rb != null) { rb.isKinematic = true; rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
        if (!colliders) foreach (Collider c in m.GetComponentsInChildren<Collider>()) c.enabled = false;
    }

    // نفخة غيمة (كرات بيضا ملزوقة)
    public static Transform Puff(Transform parent, Vector3 localPos, float size, GameManager gm)
    {
        var t = new GameObject("Puff").transform;
        t.SetParent(parent, false);
        t.localPosition = localPos;
        Material m = gm.Mat("Dream Cloud", new Color(1f, 0.97f, 1f), 0.2f);
        LevelBuilder.Visual("Puff", PrimitiveType.Sphere, t, Vector3.zero, Vector3.one * size, m, false);
        LevelBuilder.Visual("Puff", PrimitiveType.Sphere, t, new Vector3(size * 0.35f, size * 0.15f, 0.05f), Vector3.one * size * 0.75f, m, false);
        LevelBuilder.Visual("Puff", PrimitiveType.Sphere, t, new Vector3(-size * 0.35f, size * 0.1f, -0.05f), Vector3.one * size * 0.7f, m, false);
        return t;
    }

    // وجه نعسان أو صاحي (عيون مسكرة خط، أو مفتوحة)
    public static void Face(Transform parent, Vector3 at, float s, bool awake, GameManager gm)
    {
        Material dark = gm.Mat("Monster Pupil", new Color(0.05f, 0.04f, 0.06f), 0.9f);
        Material pink = gm.Mat("Blush", new Color(1f, 0.6f, 0.7f), 0.3f);
        for (int e = -1; e <= 1; e += 2)
        {
            if (awake) LevelBuilder.Visual("Eye", PrimitiveType.Sphere, parent, at + new Vector3(e * 0.18f * s, 0.08f * s, 0f), new Vector3(0.12f, 0.16f, 0.05f) * s, dark, false);
            else LevelBuilder.Visual("Eye", PrimitiveType.Cube, parent, at + new Vector3(e * 0.18f * s, 0.06f * s, 0f), new Vector3(0.16f, 0.035f, 0.04f) * s, dark, false).transform.localRotation = Quaternion.Euler(0f, 0f, e * 12f);
            LevelBuilder.Visual("Blush", PrimitiveType.Sphere, parent, at + new Vector3(e * 0.3f * s, -0.05f * s, 0f), new Vector3(0.12f, 0.07f, 0.03f) * s, pink, false);
        }
        LevelBuilder.Visual("Mouth", PrimitiveType.Sphere, parent, at + new Vector3(0f, -0.1f * s, 0f), new Vector3(0.12f, 0.06f, 0.03f) * s, dark, false);
    }
}

// ---------- خط على الأرض يصير غيمة ناعمة تنطط رويدا فوق الجدار ----------
public class CloudBounce : MonoBehaviour
{
    float x0, x1, top, wallX;
    InkMark cloud;
    Vector3[] path;
    Transform puffs;
    float squish;
    readonly HashSet<InkMark> seen = new HashSet<InkMark>();
    bool holding, prompted, told;

    public static void Create(Transform root, Vector4 s, GameManager gm)
    {
        var go = new GameObject("Cloud Bounce");
        go.transform.SetParent(root, false);
        var c = go.AddComponent<CloudBounce>();
        c.x0 = s.x; c.x1 = s.y; c.top = s.z; c.wallX = s.w;
        // علامة: سهم لفوق مرسوم على الجدار
        Material soft = gm.Mat("Dream Arrow", new Color(1f, 0.7f, 0.9f), 0.3f);
        for (int k = 0; k < 3; k++) LevelBuilder.Visual("Up Arrow", PrimitiveType.Cube, go.transform, new Vector3(c.wallX - 0.05f, c.top + 1.4f + k * 0.8f, -2.05f), new Vector3(0.12f, 0.5f, 0.02f), soft, false);
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float dt = Time.deltaTime;
        RowaidaController r = gm.Rowaida;
        if (cloud == null)
        {
            puffs = null;
            foreach (InkMark m in GameFeatures.Fresh(seen))
            {
                if (m.closed || m.path == null) continue;
                Vector3 c = GameFeatures.Center(m);
                if (c.x < x0 - 0.5f || c.x > x1 + 0.5f || c.y > top + 2.5f) continue;
                cloud = m; path = m.path;
                DreamFeatures.HideInk(m);
                puffs = new GameObject("Cloud Puffs").transform;
                puffs.SetParent(m.transform, false);
                float len = 0f;
                for (int i = 1; i < path.Length; i++)
                {
                    float seg = Vector3.Distance(path[i - 1], path[i]);
                    for (float d = 0f; d < seg; d += 0.35f) { if ((len + d) % 0.35f < 0.36f) DreamFeatures.Puff(puffs, m.transform.InverseTransformPoint(Vector3.Lerp(path[i - 1], path[i], d / seg)) + new Vector3(0f, 0.05f, 0f), Random.Range(0.45f, 0.65f), gm); }
                    len += seg;
                }
                Vector3 mid = m.transform.InverseTransformPoint(path[path.Length / 2]);
                DreamFeatures.Face(puffs, mid + new Vector3(0f, 0.12f, -0.4f), 1.2f, false, gm);
                LevelSfx.Play("boing", 0.5f, 1.4f);
                Fx.Sparkle(c, new Color(1f, 0.85f, 1f), 14);
                break;
            }
        }
        else if (r != null && path != null)
        {
            // رويدا لمست الغيمة؟ تنط عالي
            Vector3 p = r.transform.position;
            for (int i = 1; i < path.Length; i++)
            {
                Vector3 a = path[i - 1], b = path[i];
                if (p.x < Mathf.Min(a.x, b.x) - 0.2f || p.x > Mathf.Max(a.x, b.x) + 0.2f) continue;
                float y = Mathf.Lerp(a.y, b.y, Mathf.InverseLerp(a.x, b.x, p.x));
                if (p.y - y > -0.2f && p.y - y < 1.1f * r.transform.localScale.y && r.Bounce(10.2f))
                {
                    squish = 1f;
                    LevelSfx.Play("boing", 0.8f);
                    Fx.Sparkle(new Vector3(p.x, y, 0f), new Color(1f, 0.85f, 1f), 10);
                    if (!told) { told = true; gm.UI.ShowMessage("", 1.8f, "بوينغ!"); }
                }
                break;
            }
        }
        if (puffs != null)
        {
            squish = Mathf.MoveTowards(squish, 0f, dt * 3f);
            puffs.localScale = new Vector3(1f + squish * 0.1f, 1f - Mathf.Sin(squish * Mathf.PI) * 0.35f, 1f);
        }
        bool wait = cloud == null && HoldHelper.Before(x0);
        if (wait && !prompted) { prompted = true; gm.UI.ShowMessage("", 3.6f, "جدار عالٍ! لنرسم خطًّا على الأرض أمامه... يصبح غيمة تقفز برويدا إلى الأعلى"); }
        HoldHelper.Hold(ref holding, wait);
        if (cloud == null) LabFeatures.Show(x0 - 5f, x1, new Vector3(x0 - 2f, top - 0.5f, 0f), new Vector3(wallX + 1f, top + 5f, 0f));
    }
}

// ---------- ريح نعسانة تنفخ رويدا لورا: ارسمي مخدة (شكل مسكر) جنب راسها وتنام ----------
public class SleepyWind : MonoBehaviour
{
    float x0, x1, top, nextZ;
    Vector3 head;
    Transform face, cheeks;
    readonly List<Transform> eyesOpen = new List<Transform>(), eyesShut = new List<Transform>();
    bool asleep, pushing, prompted;
    readonly HashSet<InkMark> seen = new HashSet<InkMark>();
    AudioSource blow;

    public static void Create(Transform root, Vector4 s, GameManager gm)
    {
        var go = new GameObject("Sleepy Wind");
        go.transform.SetParent(root, false);
        var w = go.AddComponent<SleepyWind>();
        w.x0 = s.x; w.x1 = s.y; w.top = s.z;
        w.head = new Vector3(s.w, w.top + 3.2f, 0f);
        w.face = new GameObject("Wind Face").transform;
        w.face.SetParent(go.transform, false);
        w.face.position = w.head;
        DreamFeatures.Puff(w.face, Vector3.zero, 2.2f, gm);
        DreamFeatures.Puff(w.face, new Vector3(0.9f, 0.4f, 0.4f), 1.6f, gm);
        DreamFeatures.Puff(w.face, new Vector3(0.6f, -0.6f, 0.3f), 1.4f, gm);
        Material dark = gm.Mat("Monster Pupil", new Color(0.05f, 0.04f, 0.06f), 0.9f);
        for (int e = 0; e < 2; e++)
        {
            w.eyesOpen.Add(LevelBuilder.Visual("Eye", PrimitiveType.Sphere, w.face, new Vector3(-0.55f, 0.35f, -0.75f - e * 0.0f) + new Vector3(0f, 0f, 0f) + new Vector3(e * 0.55f - 0.2f, 0f, 0f), new Vector3(0.22f, 0.3f, 0.08f), dark, false).transform);
            var shut = LevelBuilder.Visual("Eye Shut", PrimitiveType.Cube, w.face, new Vector3(-0.75f + e * 0.55f, 0.32f, -0.78f), new Vector3(0.3f, 0.05f, 0.05f), dark, false).transform;
            shut.gameObject.SetActive(false);
            w.eyesShut.Add(shut);
        }
        // خدود منفوخة وفم ينفخ
        w.cheeks = LevelBuilder.Visual("Blow Mouth", PrimitiveType.Sphere, w.face, new Vector3(-1f, -0.2f, -0.6f), new Vector3(0.35f, 0.3f, 0.2f), dark, false).transform;
        w.blow = LevelSfx.LoopSource(go, "swirl", 0f);
    }

    void OnDisable() { Release(); }

    void Release()
    {
        if (!pushing) return;
        pushing = false;
        var gm = GameManager.Instance;
        if (gm != null && gm.Rowaida != null) gm.Rowaida.WalkBonus = 0f;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time;
        RowaidaController r = gm.Rowaida;
        face.position = head + new Vector3(0f, Mathf.Sin(t * (asleep ? 1f : 3f)) * 0.1f, 0f);
        if (!asleep)
        {
            face.localScale = Vector3.one * (1f + Mathf.Sin(t * 6f) * 0.04f);
            // خطوط ريح تطير لليسار
            if (Random.value < 0.5f)
            {
                var d = LevelBuilder.Visual("Wind Streak", PrimitiveType.Cube, null, new Vector3(head.x - 1.4f, top + Random.Range(0.3f, 3.5f), Random.Range(-1f, 0.5f)), new Vector3(Random.Range(0.6f, 1.4f), 0.05f, 0.05f), gm.Mat("Wind Streak", new Color(1f, 1f, 1f), 0.2f), false);
                var fx = d.AddComponent<FxParticle>();
                fx.velocity = Vector3.left * Random.Range(7f, 11f); fx.life = 0.8f;
            }
            bool inZone = r != null && gm.IsPlaying && r.transform.position.x > x0 && r.transform.position.x < x1 && r.transform.position.y > top - 0.5f && r.transform.position.y < top + 4f;
            if (inZone) { r.WalkBonus = -4.3f; pushing = true; }
            else Release();
            if (blow != null) blow.volume = r != null ? Mathf.Clamp01(1f - Mathf.Abs(r.transform.position.x - head.x) / 12f) * 0.4f : 0f;
            if (inZone && !prompted) { prompted = true; gm.UI.ShowMessage("", 3.8f, "ريح نعسانة تدفع رويدا إلى الخلف! لنرسم لها وسادة (شكلًا مغلقًا) بجانب رأسها لتنام"); }
            // مخدة جنب الراس؟
            foreach (InkMark m in GameFeatures.Fresh(seen))
            {
                if (!m.closed) continue;
                if (Vector2.Distance(GameFeatures.Center(m), head) > 2.6f) continue;
                asleep = true;
                m.beingEaten = true;
                Release();
                DreamFeatures.Freeze(m, false);
                DreamFeatures.HideInk(m);
                m.transform.position = head + new Vector3(0.4f, -1.25f, 0.3f);
                m.transform.rotation = Quaternion.identity;
                Material pillow = gm.Mat("Pillow", new Color(0.85f, 0.9f, 1f), 0.3f);
                LevelBuilder.Visual("Pillow", PrimitiveType.Sphere, m.transform, Vector3.zero, new Vector3(2.4f, 0.8f, 1.4f), pillow, false);
                LevelBuilder.Visual("Pillow Lace", PrimitiveType.Sphere, m.transform, new Vector3(0f, 0f, 0.05f), new Vector3(2.6f, 0.55f, 1.2f), gm.Mat("Badge White", Color.white, 0.4f), false);
                foreach (Transform e in eyesOpen) e.gameObject.SetActive(false);
                foreach (Transform e in eyesShut) e.gameObject.SetActive(true);
                cheeks.localScale = new Vector3(0.18f, 0.08f, 0.1f);
                face.localRotation = Quaternion.Euler(0f, 0f, -12f);
                if (blow != null) blow.volume = 0f;
                LevelSfx.Play("snore", 0.7f);
                gm.UI.ShowMessage("", 2.2f, "نامت الريح... ششش");
                break;
            }
            if (!asleep) LabFeatures.Show(x0 - 3f, x1, new Vector3(x0 - 1f, top - 0.5f, 0f), new Vector3(head.x + 1.8f, head.y + 1.8f, 0f));
        }
        else if (t > nextZ)
        {
            // Zzz تطلع
            nextZ = t + 0.9f;
            var z = GameFeatures.Text(null, head + new Vector3(-0.8f, 1f, -0.5f), "Z", Random.Range(0.08f, 0.13f), new Color(0.6f, 0.6f, 0.95f));
            var fx = z.gameObject.AddComponent<FxParticle>();
            fx.velocity = new Vector3(-0.4f, 1f, 0f); fx.life = 1.8f; fx.grow = 0.6f;
        }
    }
}

// ---------- خرفان الحلم: كل دايرة ترسمينها تصير خروف يطير ويصف بالحفرة، 3 خرفان = جسر ----------
public class SheepBridge : MonoBehaviour
{
    class Sheep { public InkMark m; public Vector3 from, to; public float t; }
    float gapFrom, gapTo, top;
    int slots;
    readonly List<Sheep> sheep = new List<Sheep>();
    readonly HashSet<InkMark> seen = new HashSet<InkMark>();
    bool holding, prompted;

    public static void Create(Transform root, Vector3 s, GameManager gm)
    {
        var go = new GameObject("Sheep Bridge");
        go.transform.SetParent(root, false);
        var b = go.AddComponent<SheepBridge>();
        b.gapFrom = s.x; b.gapTo = s.y; b.top = s.z;
        b.slots = Mathf.CeilToInt((b.gapTo - b.gapFrom) / 1.4f);
        // سياج صغير وعداد خرفان
        Material wood = gm.Mat("Fence Wood", new Color(0.9f, 0.85f, 0.95f), 0.3f);
        for (int k = 0; k < 3; k++) LevelBuilder.Visual("Fence", PrimitiveType.Cube, go.transform, new Vector3(b.gapFrom - 1.6f + k * 0.4f, b.top + 0.4f, 1.3f), new Vector3(0.1f, 0.8f, 0.1f), wood);
        LevelBuilder.Visual("Fence Bar", PrimitiveType.Cube, go.transform, new Vector3(b.gapFrom - 1.2f, b.top + 0.6f, 1.3f), new Vector3(1f, 0.08f, 0.08f), wood);
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time, dt = Time.deltaTime;
        foreach (InkMark m in GameFeatures.Fresh(seen))
        {
            if (!m.closed || sheep.Count >= slots) continue;
            Vector3 c = GameFeatures.Center(m);
            if (c.x < gapFrom - 2.5f || c.x > gapTo + 2f) continue;
            m.beingEaten = true;
            DreamFeatures.Freeze(m, false);
            DreamFeatures.HideInk(m);
            var s = new Sheep { m = m, from = m.transform.position, to = new Vector3(gapFrom + (gapTo - gapFrom) * (sheep.Count + 0.5f) / slots, top - 0.35f, 0f), t = 0f };
            // شكل الخروف
            Transform body = new GameObject("Sheep").transform;
            body.SetParent(m.transform, false);
            for (int k = 0; k < 6; k++) DreamFeatures.Puff(body, new Vector3(-0.45f + (k % 3) * 0.45f, (k / 3) * 0.3f, 0f), 0.6f, gm);
            Material face = gm.Mat("Sheep Face", new Color(0.3f, 0.28f, 0.32f), 0.4f);
            LevelBuilder.Visual("Sheep Head", PrimitiveType.Sphere, body, new Vector3(0.85f, 0.25f, -0.1f), new Vector3(0.45f, 0.5f, 0.45f), face, false);
            for (int e = -1; e <= 1; e += 2) LevelBuilder.Visual("Sheep Ear", PrimitiveType.Sphere, body, new Vector3(0.8f, 0.45f, e * 0.25f), new Vector3(0.3f, 0.1f, 0.15f), face, false);
            for (int e = -1; e <= 1; e += 2) LevelBuilder.Visual("Sheep Eye", PrimitiveType.Sphere, body, new Vector3(0.98f, 0.32f, e * 0.12f - 0.18f), Vector3.one * 0.08f, gm.Mat("Badge White", Color.white, 0.4f), false);
            for (int k = 0; k < 4; k++) LevelBuilder.Visual("Sheep Leg", PrimitiveType.Cube, body, new Vector3(-0.4f + k * 0.27f, -0.35f, (k % 2) * 0.3f - 0.15f), new Vector3(0.1f, 0.3f, 0.1f), face, false);
            var col = m.gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.15f, 0f);
            col.size = new Vector3(1.45f, 0.5f, 1.6f);
            sheep.Add(s);
            LevelSfx.Play("baa", 0.8f, Random.Range(0.9f, 1.2f));
            var num = GameFeatures.Text(null, c + new Vector3(0f, 1.2f, -0.4f), sheep.Count.ToString(), 0.12f, new Color(0.8f, 0.5f, 0.9f));
            var fx = num.gameObject.AddComponent<FxParticle>();
            fx.velocity = Vector3.up; fx.life = 1.5f;
        }
        foreach (Sheep s in sheep)
        {
            if (s.m == null) continue;
            s.t = Mathf.Min(1f, s.t + dt / 0.8f);
            Vector3 p = Vector3.Lerp(s.from, s.to, Mathf.SmoothStep(0f, 1f, s.t)) + Vector3.up * Mathf.Sin(s.t * Mathf.PI) * 1.2f;
            if (s.t >= 1f) p = s.to + Vector3.up * Mathf.Sin(t * 2f + s.to.x) * 0.04f;
            s.m.transform.position = p;
            s.m.transform.rotation = Quaternion.identity;
        }
        bool done = sheep.Count >= slots && sheep.TrueForAll(s => s.t >= 1f);
        bool wait = !done && HoldHelper.Before(gapFrom);
        if (wait && !prompted) { prompted = true; gm.UI.ShowMessage("", 3.6f, "لنعدّ الخراف! نرسم " + slots + " دوائر... كل واحدة تصبح خروفًا يصطف في الحفرة"); }
        HoldHelper.Hold(ref holding, wait);
        if (!done) LabFeatures.Show(gapFrom - 5f, gapFrom, new Vector3(gapFrom - 3f, top - 1f, 0f), new Vector3(gapTo + 1f, top + 3.5f, 0f));
    }
}

// ---------- كوكبة نجوم: خط يمر على كل النجوم يوصلها، وتطلع جسر من نور ----------
public class Constellation : MonoBehaviour
{
    float gapFrom, gapTo, top, bridgeT = -1f;
    readonly List<Transform> stars = new List<Transform>();
    readonly List<Vector3> pos = new List<Vector3>();
    bool[] lit;
    Transform bridge;
    BoxCollider bridgeCol;
    bool holding, prompted;
    Material litMat, dimMat;

    public static void Create(Transform root, Vector4 s, GameManager gm)
    {
        var go = new GameObject("Constellation");
        go.transform.SetParent(root, false);
        var c = go.AddComponent<Constellation>();
        c.gapFrom = s.x; c.gapTo = s.y; c.top = s.z;
        float[] dy = { 0f, 0.7f, 0.25f, 1.1f, 0.5f };
        c.lit = new bool[dy.Length];
        c.dimMat = gm.Mat("Star Dim", new Color(0.7f, 0.75f, 1.1f), 0.5f);
        c.litMat = gm.Mat("Star Lit", new Color(2.6f, 2.3f, 1.2f), 0.5f);
        for (int i = 0; i < dy.Length; i++)
        {
            Vector3 p = new Vector3(c.gapFrom + 0.4f + i * (c.gapTo - c.gapFrom - 0.8f) / (dy.Length - 1), s.w + dy[i], 0f);
            var st = new GameObject("Star").transform;
            st.SetParent(go.transform, false);
            st.position = p;
            for (int k = 0; k < 2; k++) LevelBuilder.Visual("Star Arm", PrimitiveType.Cube, st, Vector3.zero, new Vector3(0.55f, 0.14f, 0.1f), c.dimMat, false).transform.localRotation = Quaternion.Euler(0f, 0f, k * 90f);
            for (int k = 0; k < 2; k++) LevelBuilder.Visual("Star Arm", PrimitiveType.Cube, st, Vector3.zero, new Vector3(0.35f, 0.12f, 0.1f), c.dimMat, false).transform.localRotation = Quaternion.Euler(0f, 0f, 45f + k * 90f);
            c.stars.Add(st); c.pos.Add(p);
        }
        // جسر النور (يطلع لما تكتمل)
        c.bridge = new GameObject("Light Bridge").transform;
        c.bridge.SetParent(go.transform, false);
        c.bridge.position = new Vector3((c.gapFrom + c.gapTo) * 0.5f, c.top - 0.15f, 0f);
        var plank = LevelBuilder.Visual("Light Plank", PrimitiveType.Cube, c.bridge, Vector3.zero, new Vector3(c.gapTo - c.gapFrom + 0.3f, 0.3f, 1.6f), gm.Mat("Light Bridge", new Color(1.6f, 1.5f, 0.9f), 0.5f), false);
        c.bridgeCol = plank.AddComponent<BoxCollider>();
        c.bridgeCol.enabled = false;
        c.bridge.localScale = new Vector3(0.001f, 1f, 1f);
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time, dt = Time.deltaTime;
        for (int i = 0; i < stars.Count; i++)
        {
            stars[i].localRotation = Quaternion.Euler(0f, 0f, t * (lit[i] ? 60f : 15f) + i * 20f);
            stars[i].localScale = Vector3.one * (lit[i] ? 1.3f + Mathf.Sin(t * 5f + i) * 0.1f : 0.9f + Mathf.Sin(t * 2f + i) * 0.1f);
        }
        if (bridgeT < 0f)
        {
            foreach (InkMark m in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
            {
                if (m.closed || m.path == null) continue;
                for (int i = 0; i < pos.Count; i++)
                {
                    if (lit[i] || GameFeatures.PathDistance(m.path, pos[i]) > 0.6f) continue;
                    lit[i] = true;
                    foreach (Renderer rr in stars[i].GetComponentsInChildren<Renderer>()) rr.sharedMaterial = litMat;
                    LevelSfx.Play("twinkle", 0.6f, 0.9f + i * 0.08f);
                }
            }
            if (System.Array.TrueForAll(lit, l => l))
            {
                bridgeT = 0f;
                // خيوط نور بين النجوم، والخطوط المرسومة تختفي
                for (int i = 1; i < pos.Count; i++)
                {
                    var beam = LevelBuilder.Visual("Star Line", PrimitiveType.Cylinder, transform, Vector3.zero, Vector3.one, litMat, false).transform;
                    LabFeatures2.Stretch(beam, pos[i - 1], pos[i], 0.06f);
                }
                foreach (InkMark m in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
                    if (!m.closed && m.path != null && GameFeatures.PathDistance(m.path, pos[0]) < 1f) { Fx.Sparkle(GameFeatures.Center(m), new Color(1f, 0.9f, 0.5f), 8); Destroy(m.gameObject); }
                LevelSfx.Play("chime", 0.8f, 1.2f);
                gm.UI.ShowMessage("", 2f, "كوكبة! صارت النجوم جسرًا من نور");
            }
        }
        else if (bridgeT < 1f)
        {
            bridgeT = Mathf.Min(1f, bridgeT + dt / 1.2f);
            bridge.localScale = new Vector3(Mathf.Max(0.001f, bridgeT), 1f, 1f);
            if (Random.value < 0.6f) Fx.Sparkle(new Vector3(Mathf.Lerp(gapFrom, gapTo, bridgeT), top, -0.3f), new Color(1f, 0.9f, 0.5f), 3);
            if (bridgeT >= 1f) bridgeCol.enabled = true;
        }
        bool wait = bridgeT < 1f && HoldHelper.Before(gapFrom);
        if (wait && !prompted) { prompted = true; gm.UI.ShowMessage("", 3.6f, "نجوم فوق الحفرة! لنرسم خطًّا يمر على كل النجوم ويصلها"); }
        HoldHelper.Hold(ref holding, wait);
        if (bridgeT < 0f) LabFeatures.Show(gapFrom - 5f, gapFrom, new Vector3(gapFrom - 2.5f, top - 1f, 0f), new Vector3(gapTo + 1f, pos[3].y + 1.2f, 0f));
    }
}

// ---------- ظلام سادّ الطريق ورويدا خايفة: خط يلمس القمر النايم يصحيه، ويضوي ويشيل الظلام ----------
public class MoonFog : MonoBehaviour
{
    float x0, x1, top, wakeT = -1f, nextSnore;
    Vector3 moon;
    Transform moonT, cap;
    readonly List<Transform> fog = new List<Transform>();
    readonly List<Vector3> fogHome = new List<Vector3>();
    GameObject eyesShut, eyesOpen;
    Renderer moonR;
    bool holding, prompted;

    public static void Create(Transform root, Vector4 s, GameManager gm)
    {
        var go = new GameObject("Moon Fog");
        go.transform.SetParent(root, false);
        var f = go.AddComponent<MoonFog>();
        f.x0 = s.x; f.x1 = s.y; f.top = s.z;
        f.moon = new Vector3(s.w, f.top + 6.2f, 0f);
        Material dark = gm.Mat("Dream Dark", new Color(0.04f, 0.03f, 0.08f), 0.2f);
        for (int k = 0; k < 40; k++)
        {
            Vector3 p = new Vector3(Random.Range(f.x0, f.x1), f.top + Random.Range(0f, 4.5f), Random.Range(-1.6f, 1.2f));
            var b = LevelBuilder.Visual("Darkness", PrimitiveType.Sphere, go.transform, p, Vector3.one * Random.Range(1.4f, 2.4f), dark, false).transform;
            f.fog.Add(b); f.fogHome.Add(p);
        }
        // عيون صغيرة تلمع بالظلام (تخوف)
        for (int k = 0; k < 4; k++)
            for (int e = 0; e < 2; e++)
                f.fog.Add(LevelBuilder.Visual("Dark Eye", PrimitiveType.Sphere, go.transform, new Vector3(f.x0 + 1f + k * 1.4f + e * 0.25f, f.top + 1.5f + (k % 2) * 1.2f, -1.9f), Vector3.one * 0.12f, gm.Mat("Dark Eye", new Color(1.6f, 1.4f, 0.3f), 0.5f), false).transform);
        for (int k = f.fogHome.Count; k < f.fog.Count; k++) f.fogHome.Add(f.fog[k].position);
        // القمر النايم بطاقية
        f.moonT = new GameObject("Sleepy Moon").transform;
        f.moonT.SetParent(go.transform, false);
        f.moonT.position = f.moon;
        f.moonR = LevelBuilder.Visual("Moon", PrimitiveType.Sphere, f.moonT, Vector3.zero, Vector3.one * 1.8f, gm.Mat("Moon Sleepy", new Color(1f, 0.92f, 0.6f), 0.4f), false).GetComponent<Renderer>();
        f.eyesShut = new GameObject("Shut"); f.eyesShut.transform.SetParent(f.moonT, false);
        f.eyesOpen = new GameObject("Open"); f.eyesOpen.transform.SetParent(f.moonT, false);
        DreamFeatures.Face(f.eyesShut.transform, new Vector3(0f, 0f, -0.88f), 2f, false, gm);
        DreamFeatures.Face(f.eyesOpen.transform, new Vector3(0f, 0f, -0.88f), 2f, true, gm);
        f.eyesOpen.SetActive(false);
        f.cap = new GameObject("Night Cap").transform;
        f.cap.SetParent(f.moonT, false);
        f.cap.localPosition = new Vector3(0.2f, 0.75f, 0f);
        f.cap.localRotation = Quaternion.Euler(0f, 0f, -30f);
        LevelBuilder.Visual("Cap", PrimitiveType.Sphere, f.cap, new Vector3(0f, 0.3f, 0f), new Vector3(1.2f, 1f, 1.2f), gm.Mat("Night Cap", new Color(0.4f, 0.5f, 1f), 0.3f), false);
        LevelBuilder.Visual("Cap Ball", PrimitiveType.Sphere, f.cap, new Vector3(0.3f, 0.95f, 0f), Vector3.one * 0.35f, gm.Mat("Badge White", Color.white, 0.4f), false);
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time, dt = Time.deltaTime;
        moonT.position = moon + new Vector3(0f, Mathf.Sin(t * 1.2f) * 0.12f, 0f);
        if (wakeT < 0f)
        {
            moonT.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 0.8f) * 8f);
            for (int i = 0; i < fog.Count; i++) fog[i].position = fogHome[i] + new Vector3(Mathf.Sin(t * 0.7f + i) * 0.2f, Mathf.Sin(t * 0.9f + i * 1.3f) * 0.15f, 0f);
            if (t > nextSnore) { nextSnore = t + 3f; LevelSfx.Play("snore", 0.35f, 1.2f); }
            foreach (InkMark m in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
            {
                if (m.closed || m.path == null || GameFeatures.PathDistance(m.path, moon) > 1.2f) continue;
                wakeT = 0f;
                Destroy(m.gameObject);
                eyesShut.SetActive(false);
                eyesOpen.SetActive(true);
                moonR.sharedMaterial = gm.Mat("Moon Awake", new Color(2.6f, 2.4f, 1.4f), 0.4f);
                LevelSfx.Play("chime", 0.8f, 1.1f);
                LevelSfx.Play("twinkle", 0.7f);
                gm.UI.ShowMessage("", 2.2f, "استيقظ القمر وأضاء الطريق!");
                break;
            }
        }
        else if (wakeT < 1f)
        {
            wakeT = Mathf.Min(1f, wakeT + dt / 1.5f);
            moonT.localScale = Vector3.one * (1f + Mathf.Sin(wakeT * Mathf.PI) * 0.3f);
            for (int i = 0; i < fog.Count; i++)
            {
                fog[i].position = fogHome[i] + (fogHome[i] - moon).normalized * wakeT * 4f;
                fog[i].localScale = Vector3.one * Mathf.Max(0f, 1f - wakeT) * (i < 40 ? 1.9f : 0.12f);
            }
            if (Random.value < 0.6f) Fx.Sparkle(new Vector3(Random.Range(x0, x1), top + Random.Range(0f, 4f), -0.5f), new Color(1f, 0.95f, 0.6f), 2);
            if (wakeT >= 1f) foreach (Transform fo in fog) fo.gameObject.SetActive(false);
        }
        bool wait = wakeT < 1f && HoldHelper.Before(x0);
        if (wait && !prompted) { prompted = true; gm.UI.ShowMessage("", 3.6f, "ظلام! رويدا خائفة... لنرسم خطًّا يلمس القمر النائم ويوقظه"); }
        HoldHelper.Hold(ref holding, wait);
        if (wakeT < 0f) LabFeatures.Show(x0 - 5f, x0, new Vector3(x0 - 2.5f, top - 0.5f, 0f), new Vector3(Mathf.Max(x1, moon.x + 1.5f), moon.y + 1.6f, 0f));
    }
}

// ---------- فانوس طاير: ارسمي دايرة فوق السلة، تصير فانوس يضوي وينفخ ويرفع رويدا ----------
public class LanternLift : MonoBehaviour
{
    float platX, top, lift, riseT = -1f;
    bool lit, holding, prompted, delivered;
    Transform basket, lantern, flame;
    Rigidbody rb;
    Transform[] ropes = new Transform[2];
    readonly HashSet<InkMark> seen = new HashSet<InkMark>();

    public static void Create(Transform root, Vector3 s, GameManager gm)
    {
        var go = new GameObject("Lantern Lift");
        go.transform.SetParent(root, false);
        var l = go.AddComponent<LanternLift>();
        l.platX = s.x; l.top = s.y; l.lift = s.z;
        Material wicker = gm.Mat("Wicker", new Color(0.75f, 0.55f, 0.35f), 0.3f);
        var b = new GameObject("Basket");
        b.transform.SetParent(go.transform, false);
        b.transform.position = new Vector3(l.platX, l.top - 0.15f, 0f);
        LevelBuilder.Visual("Basket Floor", PrimitiveType.Cube, b.transform, Vector3.zero, new Vector3(1.6f, 0.3f, 1.8f), wicker);
        LevelBuilder.Visual("Basket Back", PrimitiveType.Cube, b.transform, new Vector3(0f, 0.35f, 0.85f), new Vector3(1.6f, 0.5f, 0.1f), wicker, false);
        for (int k = 0; k < 4; k++) LevelBuilder.Visual("Basket Weave", PrimitiveType.Cube, b.transform, new Vector3(-0.6f + k * 0.4f, 0.35f, 0.8f), new Vector3(0.05f, 0.5f, 0.12f), gm.Mat("Wicker Dark", new Color(0.55f, 0.38f, 0.22f), 0.3f), false);
        b.AddComponent<BoxCollider>().size = new Vector3(1.6f, 0.3f, 1.8f);
        l.rb = b.AddComponent<Rigidbody>();
        l.rb.isKinematic = true;
        l.rb.interpolation = RigidbodyInterpolation.Interpolate;
        l.basket = b.transform;
        // الفانوس (فاضي ومكرمش لين ترسمين)
        l.lantern = new GameObject("Lantern").transform;
        l.lantern.SetParent(go.transform, false);
        LevelBuilder.Visual("Lantern Paper", PrimitiveType.Sphere, l.lantern, Vector3.zero, new Vector3(1.8f, 2.2f, 1.8f), gm.Mat("Lantern Paper", new Color(2f, 0.9f, 0.4f), 0.3f), false);
        LevelBuilder.Visual("Lantern Ring", PrimitiveType.Cylinder, l.lantern, new Vector3(0f, -1.05f, 0f), new Vector3(1f, 0.05f, 1f), gm.Mat("Lantern Rim", new Color(0.8f, 0.3f, 0.2f), 0.4f), false);
        l.flame = LevelBuilder.Visual("Lantern Flame", PrimitiveType.Sphere, l.lantern, new Vector3(0f, -0.8f, 0f), new Vector3(0.3f, 0.45f, 0.3f), gm.Mat("Flame Orange", new Color(2.4f, 0.9f, 0.2f), 0.5f), false).transform;
        l.lantern.localScale = new Vector3(0.5f, 0.12f, 0.5f);
        for (int k = 0; k < 2; k++) l.ropes[k] = LevelBuilder.Visual("Lantern Rope", PrimitiveType.Cylinder, go.transform, Vector3.zero, Vector3.one, gm.Mat("Rope", new Color(0.8f, 0.65f, 0.4f), 0.2f), false).transform;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time, dt = Time.deltaTime;
        RowaidaController r = gm.Rowaida;
        if (!lit)
        {
            foreach (InkMark m in GameFeatures.Fresh(seen))
            {
                if (!m.closed) continue;
                Vector3 c = GameFeatures.Center(m);
                if (Mathf.Abs(c.x - platX) > 1.8f || c.y < top + 2f) continue;
                lit = true;
                m.beingEaten = true;
                Fx.Sparkle(c, new Color(1f, 0.8f, 0.4f), 16);
                Destroy(m.gameObject);
                LevelSfx.Play("whoosh", 0.7f);
                LevelSfx.Play("chime", 0.6f, 1.2f);
                gm.UI.ShowMessage("", 2f, "انتفخ الفانوس وأضاء!");
                break;
            }
        }
        bool onPlate = r != null && Mathf.Abs(r.transform.position.x - platX) < 0.75f && r.transform.position.y > basket.position.y && r.transform.position.y - basket.position.y < 1.5f;
        if (lit && riseT < 0f && onPlate) { riseT = 0f; LevelSfx.Play("whoosh", 0.6f, 0.8f); }
        if (riseT >= 0f && !delivered)
        {
            riseT = Mathf.Min(1f, riseT + dt / 3f);
            if (riseT >= 1f) { delivered = true; LevelSfx.Play("twinkle", 0.6f); }
        }
        float k = riseT < 0f ? 0f : Mathf.SmoothStep(0f, 1f, riseT);
        rb.MovePosition(new Vector3(platX, top - 0.15f + k * lift, 0f));
        float inflate = lit ? 1f : 0.3f;
        lantern.localScale = Vector3.Lerp(lantern.localScale, lit ? Vector3.one : new Vector3(0.5f, 0.12f, 0.5f), dt * 3f);
        lantern.position = new Vector3(platX + Mathf.Sin(t * 1.3f) * 0.08f, basket.position.y + 2.6f + (lit ? 1.1f : 0f) * inflate + Mathf.Sin(t * 2f) * 0.05f, 0.2f);
        flame.localScale = lit ? new Vector3(0.3f, 0.45f + Mathf.Sin(t * 17f) * 0.06f, 0.3f) : Vector3.zero;
        for (int i = 0; i < 2; i++)
        {
            float sx = i == 0 ? -0.75f : 0.75f;
            LabFeatures2.Stretch(ropes[i], basket.position + new Vector3(sx, 0.15f, 0f), lantern.position + new Vector3(sx * 0.4f, -1f * lantern.localScale.y, 0f), 0.04f);
        }
        bool wait = r != null && gm.IsPlaying && !delivered && r.transform.position.x > platX - 0.2f && r.transform.position.x < platX + 0.6f && r.transform.position.y - top < lift + 1.5f;
        if (wait && !prompted) { prompted = true; gm.UI.ShowMessage("", 3.6f, "سلة فانوس! لنرسم دائرة فوقها... تصبح فانوسًا يطير ويرفع رويدا"); }
        HoldHelper.Hold(ref holding, wait);
        if (!delivered) LabFeatures.Show(platX - 6f, platX + 0.6f, new Vector3(platX - 3f, top - 0.5f, 0f), new Vector3(platX + 2f, top + lift + 4.5f, 0f));
    }
}

// ---------- ظل الكابوس: يلحق رويدا ويبلعها، بس يخاف من النور: الأشكال المسكرة تصير لمبات تطرده ----------
public class ShadowChase : MonoBehaviour
{
    public static ShadowChase Instance;
    float endX, speed, delay = 3f, nextGrowl, goneT = -1f, gulpT;
    readonly List<Transform> smoke = new List<Transform>();
    readonly List<Transform> lamps = new List<Transform>();
    readonly List<float> lampT = new List<float>();
    readonly HashSet<InkMark> seen = new HashSet<InkMark>();
    Transform eyes;
    bool told;
    float Front => transform.position.x + 2.2f;

    public static void Create(Transform root, Vector4 s, GameManager gm)
    {
        var go = new GameObject("Nightmare Shadow");
        go.transform.SetParent(root, false);
        go.transform.position = new Vector3(s.x, 2.2f, 0f);
        var sh = go.AddComponent<ShadowChase>();
        Instance = sh;
        sh.endX = s.y; sh.speed = s.z;
        Material dark = gm.Mat("Shadow Smoke", new Color(0.06f, 0.03f, 0.1f), 0.1f);
        for (int k = 0; k < 26; k++) sh.smoke.Add(LevelBuilder.Visual("Smoke", PrimitiveType.Sphere, go.transform, Random.insideUnitSphere * 1.6f, Vector3.one * Random.Range(1.2f, 2.2f), dark, false).transform);
        // أيادي طويلة
        for (int k = 0; k < 3; k++) sh.smoke.Add(LevelBuilder.Visual("Claw", PrimitiveType.Sphere, go.transform, new Vector3(2f, -0.8f + k * 0.9f, -0.6f), new Vector3(1.6f, 0.25f, 0.25f), dark, false).transform);
        sh.eyes = new GameObject("Eyes").transform;
        sh.eyes.SetParent(go.transform, false);
        sh.eyes.localPosition = new Vector3(1.2f, 0.6f, -1.8f);
        Material glow = gm.Mat("Shadow Eye", new Color(2.4f, 0.4f, 2.4f), 0.5f);
        for (int e = -1; e <= 1; e += 2) LevelBuilder.Visual("Eye", PrimitiveType.Sphere, sh.eyes, new Vector3(e * 0.4f, 0f, 0f), new Vector3(0.45f, 0.22f, 0.1f), glow, false).transform.localRotation = Quaternion.Euler(0f, 0f, e * -15f);
        gm.Respawned += sh.OnRespawned;
    }

    void OnDestroy() { if (GameManager.Instance != null) GameManager.Instance.Respawned -= OnRespawned; if (Instance == this) Instance = null; }

    void OnRespawned(Vector3 pos)
    {
        if (goneT >= 0f) return;
        transform.position = new Vector3(pos.x - 12f, 2.2f, 0f);
        delay = 2.5f;
    }

    public void Vanish()
    {
        if (goneT >= 0f) return;
        goneT = 0f;
        LevelSfx.Play("whoosh", 0.8f, 0.6f);
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time, dt = Time.deltaTime;
        gulpT = Mathf.Max(0f, gulpT - dt);
        for (int i = 0; i < smoke.Count; i++)
        {
            Transform sm = smoke[i];
            if (i < 26) sm.localPosition = Quaternion.Euler(0f, 0f, t * 40f + i * 13f) * new Vector3(Mathf.Sin(i * 1.7f) * 1.5f, Mathf.Cos(i * 2.3f) * 1.5f, Mathf.Sin(i) * 0.8f);
            else sm.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 3f + i) * 25f);
        }
        transform.localScale = Vector3.one * (1f + Mathf.Sin(gulpT * Mathf.PI * 2f) * 0.2f);
        // اللمبات اللي رسمتيها (تضوي شوي وتطفي)
        foreach (InkMark m in GameFeatures.Fresh(seen))
        {
            if (!m.closed) continue;
            DreamFeatures.Freeze(m, false);
            DreamFeatures.HideInk(m);
            m.transform.rotation = Quaternion.identity;
            LevelBuilder.Visual("Lamp Glow", PrimitiveType.Sphere, m.transform, Vector3.zero, Vector3.one * 0.9f, gm.Mat("Dream Lamp", new Color(2.6f, 2.2f, 1f), 0.5f), false);
            LevelBuilder.Visual("Lamp Halo", PrimitiveType.Sphere, m.transform, Vector3.zero, Vector3.one * 2.2f, LabFeatures2.Tint("Lamp Halo", new Color(1f, 0.9f, 0.5f, 0.25f)), false);
            var light = m.gameObject.AddComponent<Light>();
            light.type = LightType.Point; light.color = new Color(1f, 0.9f, 0.6f); light.range = 6f; light.intensity = 2.5f;
            lamps.Add(m.transform); lampT.Add(t);
            LevelSfx.Play("twinkle", 0.6f);
            if (!told) { told = true; gm.UI.ShowMessage("", 2.2f, "مصباح! الظل يخاف من النور"); }
        }
        for (int i = lamps.Count - 1; i >= 0; i--)
        {
            if (lamps[i] == null || t - lampT[i] > 7f) { if (lamps[i] != null) Destroy(lamps[i].gameObject); lamps.RemoveAt(i); lampT.RemoveAt(i); continue; }
            lamps[i].localScale = Vector3.one * (t - lampT[i] > 6f ? 7f - (t - lampT[i]) : 1f);
        }
        if (goneT >= 0f)
        {
            goneT += dt;
            transform.localScale = Vector3.one * Mathf.Max(0f, 1f - goneT);
            transform.position += Vector3.left * dt * 4f;
            if (goneT > 1f) gameObject.SetActive(false);
            return;
        }
        if (!gm.IsPlaying) return;
        if (delay > 0f) { delay -= dt; return; }
        RowaidaController r = gm.Rowaida;
        float gap = r != null ? r.transform.position.x - Front : 99f;
        // نور قريب؟ يرجع لورا
        bool scared = false;
        foreach (Transform l in lamps) if (l != null && l.position.x > transform.position.x - 1f && l.position.x - Front < 4f) scared = true;
        float s = speed;
        if (scared) s = -2.2f;
        else if (r != null && r.Held) s = gap < 6f ? 2f : 4.5f;
        else if (gap > 8f) s = 4.5f;
        if (Front < endX || s < 0f) transform.position += new Vector3(s * dt, 0f, 0f);
        eyes.localScale = scared ? new Vector3(1f, 0.4f, 1f) : Vector3.one;
        if (r == null) return;
        if (gap < 9f) CameraFollow.ShiftRequest = -Mathf.Lerp(3.5f, 1f, gap / 9f);
        if (t > nextGrowl && gap < 8f) { nextGrowl = t + 2f; LevelSfx.Play("rumble", Mathf.Lerp(0.6f, 0.2f, gap / 8f), 0.6f); LevelSfx.Play("heartbeat", Mathf.Lerp(0.8f, 0.3f, gap / 8f)); }
        if (gap < 0.2f && r.transform.position.y > -3f)
        {
            gulpT = 0.5f;
            LevelSfx.Play("gloop", 1f, 0.5f);
            gm.KillRowaida();
        }
    }
}

// ---------- عيون تخوف بالظلام: يطلع وحش يسد الطريق، ارسمي له ابتسامة ويطلع قطو لطيف ----------
public class ScaryEyes : MonoBehaviour
{
    float x, top, riseK, friendT = -1f, warnTold;
    bool risen, holding, prompted;
    Transform monster, teeth, eyesScary, eyesCute;
    BoxCollider col;
    Renderer bodyR;
    readonly HashSet<InkMark> seen = new HashSet<InkMark>();

    public static void Create(Transform root, Vector2 s, GameManager gm)
    {
        var go = new GameObject("Scary Eyes");
        go.transform.SetParent(root, false);
        var e = go.AddComponent<ScaryEyes>();
        e.x = s.x; e.top = s.y;
        // شجيرة سودا وعيون حمر تلمع
        Material bush = gm.Mat("Dark Bush", new Color(0.08f, 0.05f, 0.12f), 0.2f);
        for (int k = 0; k < 5; k++) LevelBuilder.Visual("Bush", PrimitiveType.Sphere, go.transform, new Vector3(e.x - 0.8f + k * 0.4f, e.top + 0.4f + (k % 2) * 0.2f, 1.6f), Vector3.one * 1f, bush, false);
        e.monster = new GameObject("Monster").transform;
        e.monster.SetParent(go.transform, false);
        e.monster.position = new Vector3(e.x, e.top - 2.6f, 0f);
        e.bodyR = LevelBuilder.Visual("Monster Body", PrimitiveType.Sphere, e.monster, new Vector3(0f, 1.3f, 0f), new Vector3(2f, 2.6f, 1.8f), gm.Mat("Nightmare Fur", new Color(0.12f, 0.08f, 0.16f), 0.2f), true).GetComponent<Renderer>();
        for (int k = 0; k < 6; k++) LevelBuilder.Visual("Fur Spike", PrimitiveType.Sphere, e.monster, new Vector3(-0.8f + k * 0.32f, 2.5f + Mathf.Sin(k) * 0.1f, 0f), new Vector3(0.25f, 0.7f, 0.25f), gm.Mat("Nightmare Fur", new Color(0.12f, 0.08f, 0.16f), 0.2f), false);
        e.eyesScary = new GameObject("Scary").transform; e.eyesScary.SetParent(e.monster, false);
        e.eyesCute = new GameObject("Cute").transform; e.eyesCute.SetParent(e.monster, false);
        Material red = gm.Mat("Scary Eye", new Color(2.4f, 0.3f, 0.2f), 0.5f);
        for (int s2 = -1; s2 <= 1; s2 += 2) LevelBuilder.Visual("Eye", PrimitiveType.Sphere, e.eyesScary, new Vector3(s2 * 0.4f, 1.8f, -0.85f), new Vector3(0.4f, 0.2f, 0.1f), red, false).transform.localRotation = Quaternion.Euler(0f, 0f, s2 * -20f);
        e.teeth = new GameObject("Teeth").transform; e.teeth.SetParent(e.monster, false);
        for (int k = 0; k < 5; k++) LevelBuilder.Visual("Tooth", PrimitiveType.Cube, e.teeth, new Vector3(-0.4f + k * 0.2f, 1f, -0.85f), new Vector3(0.12f, 0.25f, 0.05f), gm.Mat("Badge White", Color.white, 0.4f), false).transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        for (int s2 = -1; s2 <= 1; s2 += 2)
        {
            LevelBuilder.Visual("Cute Eye", PrimitiveType.Sphere, e.eyesCute, new Vector3(s2 * 0.4f, 1.75f, -0.85f), new Vector3(0.5f, 0.55f, 0.1f), gm.Mat("Badge White", Color.white, 0.4f), false);
            LevelBuilder.Visual("Cute Pupil", PrimitiveType.Sphere, e.eyesCute, new Vector3(s2 * 0.4f + 0.05f, 1.72f, -0.9f), new Vector3(0.28f, 0.32f, 0.05f), gm.Mat("Monster Pupil", new Color(0.05f, 0.04f, 0.06f), 0.9f), false);
            LevelBuilder.Visual("Cute Blush", PrimitiveType.Sphere, e.eyesCute, new Vector3(s2 * 0.75f, 1.3f, -0.8f), new Vector3(0.3f, 0.15f, 0.05f), gm.Mat("Blush", new Color(1f, 0.6f, 0.7f), 0.3f), false);
            LevelBuilder.Visual("Cat Ear", PrimitiveType.Cube, e.eyesCute, new Vector3(s2 * 0.6f, 2.6f, 0f), new Vector3(0.4f, 0.4f, 0.3f), gm.Mat("Cute Fur", new Color(0.75f, 0.6f, 0.95f), 0.3f), false).transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        }
        e.eyesCute.gameObject.SetActive(false);
        e.col = e.monster.gameObject.AddComponent<BoxCollider>();
        e.col.center = new Vector3(0f, 1.3f, 0f);
        e.col.size = new Vector3(1.8f, 2.6f, 2f);
        e.col.enabled = false;
        // عيون تلمع بالشجيرة قبل ما يطلع
        for (int s2 = -1; s2 <= 1; s2 += 2) LevelBuilder.Visual("Bush Eye", PrimitiveType.Sphere, go.transform, new Vector3(e.x + s2 * 0.2f, e.top + 0.6f, 1.0f), new Vector3(0.18f, 0.1f, 0.05f), red, false);
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time, dt = Time.deltaTime;
        RowaidaController r = gm.Rowaida;
        if (!risen && r != null && gm.IsPlaying && r.transform.position.x > x - 5f && r.transform.position.x < x)
        {
            risen = true;
            col.enabled = true;
            LevelSfx.Play("rumble", 0.8f, 0.7f);
            LevelSfx.Play("heartbeat", 0.8f);
        }
        if (risen) riseK = Mathf.MoveTowards(riseK, 1f, dt * 3f);
        if (friendT < 0f)
        {
            monster.position = new Vector3(x + (risen ? Mathf.Sin(t * 20f) * 0.03f : 0f), top - 2.6f + riseK * 2.6f, 0f);
            teeth.localScale = new Vector3(1f, 1f + Mathf.Abs(Mathf.Sin(t * 3f)) * 0.4f, 1f);
            if (risen)
            {
                foreach (InkMark m in GameFeatures.Fresh(seen))
                {
                    if (m.closed || m.path == null || m.path.Length < 3) continue;
                    Vector3 c = GameFeatures.Center(m);
                    if (Mathf.Abs(c.x - x) > 1.6f || c.y < top + 0.3f || c.y > top + 3f) continue;
                    // ابتسامة = الوسط أوطى من الطرفين
                    Vector3 a = m.path[0], b = m.path[m.path.Length - 1], mid = m.path[m.path.Length / 2];
                    float sag = (a.y + b.y) * 0.5f - mid.y;
                    if (sag > 0.12f && Mathf.Abs(b.x - a.x) > 0.5f)
                    {
                        friendT = 0f;
                        Fx.Sparkle(c, new Color(1f, 0.7f, 0.9f), 12);
                        Destroy(m.gameObject);
                        // الابتسامة تنرسم على وجهه
                        for (int k = 0; k < 7; k++)
                        {
                            float u = k / 6f - 0.5f;
                            LevelBuilder.Visual("Smile", PrimitiveType.Cube, eyesCute, new Vector3(u * 0.9f, 1.15f + u * u * 0.9f, -0.9f), new Vector3(0.16f, 0.07f, 0.04f), gm.Mat("Monster Pupil", new Color(0.05f, 0.04f, 0.06f), 0.9f), false).transform.localRotation = Quaternion.Euler(0f, 0f, u * 70f);
                        }
                        eyesScary.gameObject.SetActive(false);
                        teeth.gameObject.SetActive(false);
                        eyesCute.gameObject.SetActive(true);
                        bodyR.sharedMaterial = gm.Mat("Cute Fur", new Color(0.75f, 0.6f, 0.95f), 0.3f);
                        foreach (Renderer rr in monster.GetComponentsInChildren<Renderer>()) if (rr.name == "Fur Spike") rr.sharedMaterial = bodyR.sharedMaterial;
                        LevelSfx.Play("meow", 0.9f);
                        gm.UI.ShowMessage("", 2.4f, "ابتسم! وصار قطًّا لطيفًا لا يخيف");
                    }
                    else if (t - warnTold > 3f) { warnTold = t; gm.UI.ShowMessage("", 2f, "لنرسم له ابتسامة (خطًّا مقوسًا إلى الأسفل)"); }
                    break;
                }
            }
        }
        else
        {
            // يصير صغير ولطيف ويقعد على جنب
            friendT = Mathf.Min(1f, friendT + dt / 1f);
            monster.position = Vector3.Lerp(new Vector3(x, top, 0f), new Vector3(x + 0.3f, top, 1.8f), friendT);
            monster.localScale = Vector3.one * Mathf.Lerp(1f, 0.55f, friendT);
            if (friendT >= 1f) col.enabled = false;
        }
        bool wait = risen && friendT < 1f && HoldHelper.Before(x - 1f);
        if (wait && !prompted) { prompted = true; gm.UI.ShowMessage("", 3.6f, "وحش في الظلام! لنرسم ابتسامة على وجهه كي لا يخيف"); }
        HoldHelper.Hold(ref holding, wait);
        if (risen && friendT < 0f) LabFeatures.Show(x - 5f, x, new Vector3(x - 3f, top - 0.5f, 0f), new Vector3(x + 1.5f, top + 3.5f, 0f));
    }
}

// ---------- منبه عملاق بالآخر: خط يضرب زره، يرن ورويدا تصحى من الحلم والظل يختفي ----------
public class AlarmWake : MonoBehaviour
{
    float x, top, ringT = -1f;
    Vector3 button;
    Transform clock, hammer;
    BoxCollider col;
    Transform flash;
    bool holding, prompted;

    public static void Create(Transform root, Vector2 s, GameManager gm)
    {
        var go = new GameObject("Alarm Wake");
        go.transform.SetParent(root, false);
        var a = go.AddComponent<AlarmWake>();
        a.x = s.x; a.top = s.y;
        a.button = new Vector3(a.x, a.top + 4.1f, 0f);
        a.clock = new GameObject("Big Alarm").transform;
        a.clock.SetParent(go.transform, false);
        a.clock.position = new Vector3(a.x, a.top, 0f);
        Material red = gm.Mat("Alarm Red", new Color(0.95f, 0.3f, 0.35f), 0.6f);
        Material bell = gm.Mat("Alarm Bell", new Color(1f, 0.8f, 0.3f), 0.8f, 0.6f);
        var face = LevelBuilder.Visual("Clock Body", PrimitiveType.Cylinder, a.clock, new Vector3(0f, 1.8f, 0f), new Vector3(3f, 0.6f, 3f), red, true).transform;
        face.localRotation = Quaternion.Euler(90f, 0f, 0f);
        LevelBuilder.Visual("Clock Face", PrimitiveType.Cylinder, a.clock, new Vector3(0f, 1.8f, -0.62f), new Vector3(2.5f, 0.02f, 2.5f), gm.Mat("Badge White", Color.white, 0.4f), false).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        for (int k = 0; k < 12; k++)
        {
            float ang = k / 12f * Mathf.PI * 2f;
            LevelBuilder.Visual("Tick", PrimitiveType.Cube, a.clock, new Vector3(Mathf.Sin(ang) * 1.05f, 1.8f + Mathf.Cos(ang) * 1.05f, -0.65f), new Vector3(0.08f, 0.2f, 0.02f), gm.Mat("Monster Pupil", new Color(0.05f, 0.04f, 0.06f), 0.9f), false).transform.localRotation = Quaternion.Euler(0f, 0f, -ang * Mathf.Rad2Deg);
        }
        LevelBuilder.Visual("Hand", PrimitiveType.Cube, a.clock, new Vector3(0f, 2.15f, -0.67f), new Vector3(0.08f, 0.7f, 0.02f), gm.Mat("Monster Pupil", new Color(0.05f, 0.04f, 0.06f), 0.9f), false);
        LevelBuilder.Visual("Hand", PrimitiveType.Cube, a.clock, new Vector3(0.25f, 1.8f, -0.67f), new Vector3(0.5f, 0.08f, 0.02f), gm.Mat("Monster Pupil", new Color(0.05f, 0.04f, 0.06f), 0.9f), false);
        for (int k = -1; k <= 1; k += 2)
        {
            LevelBuilder.Visual("Bell", PrimitiveType.Sphere, a.clock, new Vector3(k * 0.95f, 3.45f, 0f), new Vector3(1f, 0.8f, 1f), bell, true);
            LevelBuilder.Visual("Leg", PrimitiveType.Cube, a.clock, new Vector3(k * 0.9f, 0.15f, 0f), new Vector3(0.25f, 0.4f, 0.25f), bell, true).transform.localRotation = Quaternion.Euler(0f, 0f, k * 25f);
        }
        a.hammer = LevelBuilder.Visual("Hammer", PrimitiveType.Cube, a.clock, new Vector3(0f, 3.6f, 0f), new Vector3(0.12f, 0.6f, 0.12f), bell, false).transform;
        LevelBuilder.Visual("Button", PrimitiveType.Cylinder, a.clock, new Vector3(0f, 4.1f, 0f), new Vector3(0.5f, 0.12f, 0.5f), red, true);
        a.col = a.clock.gameObject.AddComponent<BoxCollider>();
        a.col.center = new Vector3(0f, 1.8f, 0f);
        a.col.size = new Vector3(3f, 3.6f, 2f);
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time, dt = Time.deltaTime;
        if (ringT < 0f)
        {
            hammer.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 2f) * 5f);
            foreach (InkMark m in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
            {
                if (m.closed || m.path == null || GameFeatures.PathDistance(m.path, button) > 0.6f) continue;
                ringT = 0f;
                Destroy(m.gameObject);
                LevelSfx.Play("alarm", 1f);
                if (ShadowChase.Instance != null) ShadowChase.Instance.Vanish();
                gm.UI.ShowMessage("", 3f, "رنّ المنبه! استيقظت رويدا من الحلم");
                // ومضة بيضا قدام الكاميرا
                var cam = Camera.main;
                if (cam != null)
                {
                    flash = LevelBuilder.Visual("Wake Flash", PrimitiveType.Quad, cam.transform, new Vector3(0f, 0f, cam.nearClipPlane + 0.05f), new Vector3(4f, 3f, 1f), LabFeatures2.Tint("Wake Flash", new Color(1f, 0.97f, 0.9f, 0f)), false).transform;
                    flash.localRotation = Quaternion.identity;
                }
                break;
            }
        }
        else
        {
            ringT += dt;
            hammer.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 70f) * 35f);
            clock.position = new Vector3(x + Mathf.Sin(t * 60f) * 0.06f * Mathf.Clamp01(2f - ringT), top - Mathf.Clamp01((ringT - 1.6f) / 1f) * 4f, 0f);
            if (ringT > 2.6f) col.enabled = false;
            if (flash != null)
            {
                float a = ringT < 0.5f ? ringT / 0.5f : Mathf.Clamp01(1f - (ringT - 0.8f) / 1.2f);
                flash.GetComponent<Renderer>().material.color = new Color(1f, 0.97f, 0.9f, a);
                if (ringT > 2.2f) { Destroy(flash.gameObject); flash = null; }
            }
        }
        bool wait = ringT < 2.6f && HoldHelper.Before(x - 1.5f);
        if (wait && !prompted) { prompted = true; gm.UI.ShowMessage("", 3.6f, "منبه عملاق! لنرسم خطًّا يضرب زره الأحمر في الأعلى ويوقظ رويدا"); }
        HoldHelper.Hold(ref holding, wait);
        if (ringT < 0f) LabFeatures.Show(x - 7f, x, new Vector3(x - 4f, top - 0.5f, 0f), new Vector3(x + 2f, top + 5f, 0f));
    }
}

// ---------- منظر الأحلام: 1 غيوم وقوس قزح، 2 نجوم وكواكب، 3 كابوس بشجر يابس وعيون ----------
public class DreamScenery : MonoBehaviour
{
    int style;
    float minX, maxX;
    readonly List<Transform> bob = new List<Transform>();
    readonly List<Vector3> bobHome = new List<Vector3>();
    readonly List<Transform> spin = new List<Transform>();
    readonly List<Renderer> twinkle = new List<Renderer>();
    readonly List<Transform> blink = new List<Transform>();
    Transform shooting;
    float nextShoot;

    public static void Create(Transform root, LevelData data, GameManager gm, System.Random rng)
    {
        var go = new GameObject("Dream Scenery");
        go.transform.SetParent(root, false);
        var s = go.AddComponent<DreamScenery>();
        s.style = data.dreamStyle;
        float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        s.minX = float.MaxValue; s.maxX = float.MinValue;
        foreach (Block b in data.blocks) { s.minX = Mathf.Min(s.minX, b.center.x - b.size.x * 0.5f); s.maxX = Mathf.Max(s.maxX, b.center.x + b.size.x * 0.5f); }
        if (s.style == 1) s.Clouds(go.transform, gm, R);
        if (s.style == 2) s.Stars(go.transform, gm, R);
        if (s.style == 3) s.Nightmare(go.transform, gm, R);
    }

    void Bob(Transform t) { bob.Add(t); bobHome.Add(t.position); }

    void Clouds(Transform p, GameManager gm, System.Func<float, float, float> R)
    {
        for (float x = minX - 10f; x < maxX + 10f; x += R(4f, 7f))
        {
            var c = new GameObject("Dream Cloud").transform;
            c.SetParent(p, false);
            c.position = new Vector3(x, R(-3f, 9f), R(5f, 12f));
            int n = Random.Range(3, 6);
            for (int k = 0; k < n; k++) DreamFeatures.Puff(c, new Vector3(k * 0.9f - n * 0.45f, Mathf.Sin(k) * 0.3f, 0f), R(1.2f, 2f), gm);
            if (Random.value < 0.3f) DreamFeatures.Face(c, new Vector3(0f, 0.1f, -0.9f), 2f, false, gm);
            Bob(c);
        }
        // قوس قزح
        Color[] rb = { new Color(1f, 0.5f, 0.55f), new Color(1f, 0.75f, 0.4f), new Color(1f, 0.95f, 0.5f), new Color(0.6f, 0.9f, 0.6f), new Color(0.55f, 0.75f, 1f), new Color(0.75f, 0.6f, 1f) };
        for (int band = 0; band < rb.Length; band++)
        {
            float rr = 9f - band * 0.45f;
            Material m = gm.Mat("Rainbow " + band, rb[band], 0.3f);
            for (int k = 0; k <= 24; k++)
            {
                float a = k / 24f * Mathf.PI;
                LevelBuilder.Visual("Rainbow", PrimitiveType.Cube, p, new Vector3((minX + maxX) * 0.5f + Mathf.Cos(a) * rr, -1f + Mathf.Sin(a) * rr, 13f), new Vector3(1.3f, 0.45f, 0.1f), m, false).transform.localRotation = Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg + 90f);
            }
        }
        // مخدات وبلوكات حروف طايرة وZzz
        for (float x = minX - 4f; x < maxX + 4f; x += R(6f, 9f))
        {
            var pil = LevelBuilder.Visual("Floating Pillow", PrimitiveType.Sphere, p, new Vector3(x, R(4.5f, 7f), R(2.5f, 4f)), new Vector3(1.6f, 0.55f, 1f), gm.Mat("Pillow", new Color(0.85f, 0.9f, 1f), 0.3f), false).transform;
            Bob(pil);
            var block = GameFeatures.Px("Toy Block", p, new Vector3(x + R(2f, 4f), R(5f, 8f), R(3f, 5f)), Vector3.one * 0.8f, Color.HSVToRGB(Random.value, 0.4f, 1f), true).transform;
            GameFeatures.Text(block, block.position + new Vector3(0f, 0f, -0.42f), "أبجد"[Random.Range(0, 4)].ToString(), 0.1f, Color.white);
            spin.Add(block); Bob(block);
            var z = GameFeatures.Text(p, new Vector3(x + R(0f, 3f), R(6f, 9f), R(4f, 8f)), "Z", R(0.12f, 0.25f), new Color(0.7f, 0.65f, 1f));
            Bob(z.transform);
        }
    }

    void Stars(Transform p, GameManager gm, System.Func<float, float, float> R)
    {
        Material[] starM = { gm.Mat("BG Star", new Color(2.2f, 2.2f, 1.8f), 0.5f), gm.Mat("BG Star Blue", new Color(1.4f, 1.8f, 2.6f), 0.5f) };
        for (int k = 0; k < 140; k++)
        {
            var st = LevelBuilder.Visual("Star", PrimitiveType.Sphere, p, new Vector3(R(minX - 15f, maxX + 15f), R(3f, 16f), R(9f, 14f)), Vector3.one * R(0.06f, 0.2f), starM[k % 2], false);
            twinkle.Add(st.GetComponent<Renderer>());
        }
        // كواكب بحلقات
        Color[] pc = { new Color(1f, 0.6f, 0.4f), new Color(0.5f, 0.7f, 1f), new Color(0.85f, 0.6f, 1f) };
        for (int k = 0; k < 4; k++)
        {
            var pl = new GameObject("Planet").transform;
            pl.SetParent(p, false);
            pl.position = new Vector3(Mathf.Lerp(minX, maxX, (k + 0.5f) / 4f) + R(-3f, 3f), R(8f, 12f), R(10f, 13f));
            LevelBuilder.Visual("Planet Body", PrimitiveType.Sphere, pl, Vector3.zero, Vector3.one * R(1.5f, 3f), gm.Mat("Planet " + k % 3, pc[k % 3], 0.4f), false);
            var ring = LevelBuilder.Visual("Planet Ring", PrimitiveType.Cylinder, pl, Vector3.zero, new Vector3(4f, 0.02f, 4f), LabFeatures2.Tint("Planet Ring", new Color(1f, 0.9f, 0.7f, 0.5f)), false).transform;
            ring.localRotation = Quaternion.Euler(70f, 0f, 20f);
            spin.Add(pl); Bob(pl);
        }
        // كوكبات بعيدة (خطوط خفيفة)
        for (int k = 0; k < 6; k++)
        {
            Vector3 a = new Vector3(R(minX - 5f, maxX + 5f), R(9f, 14f), 12.5f);
            for (int i = 0; i < 4; i++)
            {
                Vector3 b = a + new Vector3(R(0.8f, 1.8f), R(-0.8f, 0.8f), 0f);
                LevelBuilder.Visual("Const Star", PrimitiveType.Sphere, p, b, Vector3.one * 0.18f, starM[0], false);
                var line = LevelBuilder.Visual("Const Line", PrimitiveType.Cylinder, p, Vector3.zero, Vector3.one, LabFeatures2.Tint("Const Line", new Color(0.8f, 0.85f, 1f, 0.35f)), false).transform;
                LabFeatures2.Stretch(line, a, b, 0.03f);
                a = b;
            }
        }
        shooting = LevelBuilder.Visual("Shooting Star", PrimitiveType.Cube, p, new Vector3(0f, -50f, 12f), new Vector3(1.6f, 0.06f, 0.06f), starM[0], false).transform;
    }

    void Nightmare(Transform p, GameManager gm, System.Func<float, float, float> R)
    {
        Material bark = gm.Mat("Dead Tree", new Color(0.08f, 0.05f, 0.1f), 0.2f);
        for (float x = minX - 6f; x < maxX + 6f; x += R(4f, 7f))
        {
            // شجرة يابسة بفروع
            Vector3 b = new Vector3(x, 0f, R(4f, 9f));
            float h = R(4f, 7f);
            LevelBuilder.Visual("Trunk", PrimitiveType.Cylinder, p, b + new Vector3(0f, h * 0.5f - 1f, 0f), new Vector3(0.35f, h * 0.5f, 0.35f), bark, false);
            for (int k = 0; k < 4; k++)
            {
                var br = LevelBuilder.Visual("Branch", PrimitiveType.Cylinder, p, b + new Vector3(0f, h * (0.5f + k * 0.12f) - 1f, 0f), new Vector3(0.12f, R(0.8f, 1.6f), 0.12f), bark, false).transform;
                br.localRotation = Quaternion.Euler(0f, 0f, (k % 2 == 0 ? 1f : -1f) * R(30f, 60f));
                br.position += br.up * br.localScale.y;
            }
            // عيون ترمش بالظلام
            var eyes = new GameObject("Dark Eyes").transform;
            eyes.SetParent(p, false);
            eyes.position = new Vector3(x + R(1f, 3f), R(1f, 6f), R(5f, 9f));
            for (int e = -1; e <= 1; e += 2) LevelBuilder.Visual("Eye", PrimitiveType.Sphere, eyes, new Vector3(e * 0.2f, 0f, 0f), new Vector3(0.18f, 0.1f, 0.05f), gm.Mat("Dark Eye " + (Random.value < 0.5f ? "Y" : "P"), Random.value < 0.5f ? new Color(1.6f, 1.4f, 0.3f) : new Color(1.6f, 0.5f, 1.8f), 0.5f), false);
            blink.Add(eyes);
            // ساعة مكسورة طايرة
            var clock = LevelBuilder.Visual("Floating Clock", PrimitiveType.Cylinder, p, new Vector3(x + R(-1f, 1f), R(6f, 10f), R(6f, 10f)), new Vector3(1.2f, 0.1f, 1.2f), gm.Mat("Clock Ghost", new Color(0.5f, 0.45f, 0.6f), 0.3f), false).transform;
            clock.localRotation = Quaternion.Euler(90f, 0f, 0f);
            spin.Add(clock); Bob(clock);
        }
        // ضباب بنفسجي
        for (int k = 0; k < 30; k++)
        {
            var f = LevelBuilder.Visual("Purple Fog", PrimitiveType.Sphere, p, new Vector3(R(minX - 8f, maxX + 8f), R(-1.5f, 1f), R(2.5f, 8f)), new Vector3(R(3f, 6f), R(1f, 2f), R(2f, 3f)), LabFeatures2.Tint("Purple Fog", new Color(0.5f, 0.3f, 0.7f, 0.25f)), false).transform;
            Bob(f);
        }
        // نور الفجر عند الباب
        LevelBuilder.Visual("Dawn", PrimitiveType.Sphere, p, new Vector3(maxX - 2f, 3f, 12f), Vector3.one * 9f, gm.Mat("Dawn Glow", new Color(2.2f, 1.4f, 0.8f), 0.4f), false);
    }

    void Update()
    {
        float t = Time.time;
        for (int i = 0; i < bob.Count; i++) if (bob[i] != null) bob[i].position = bobHome[i] + new Vector3(Mathf.Sin(t * 0.3f + i) * 0.4f, Mathf.Sin(t * 1.1f + i * 1.7f) * 0.2f, 0f);
        foreach (Transform s in spin) if (s != null) s.localRotation = Quaternion.Euler(style == 3 ? 90f : 0f, t * 15f, style == 3 ? t * 30f : Mathf.Sin(t + s.position.x) * 10f);
        foreach (Renderer r in twinkle) if (Random.value < 0.01f) r.enabled = !r.enabled || Random.value < 0.7f;
        foreach (Transform b in blink) b.localScale = Mathf.Repeat(t + b.position.x, 4f) < 0.15f ? new Vector3(1f, 0.1f, 1f) : Vector3.one;
        if (shooting != null)
        {
            if (t > nextShoot)
            {
                nextShoot = t + Random.Range(3f, 6f);
                shooting.position = new Vector3(Random.Range(minX, maxX), Random.Range(10f, 15f), 12f);
                shooting.localRotation = Quaternion.Euler(0f, 0f, -25f);
            }
            shooting.position += new Vector3(-14f, -6.5f, 0f) * Time.deltaTime;
        }
    }
}
