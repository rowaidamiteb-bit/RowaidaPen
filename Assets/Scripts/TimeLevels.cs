using System.Collections.Generic;
using UnityEngine;

// الجزء الثاني — فصل الوقت: ساعة، ساعة رملية، تروس، بندول، وليل ونهار
public static class TimeFeatures
{
    public static void Build(Transform root, LevelData data, GameManager gm, System.Random rng)
    {
        foreach (Vector3 c in data.clocks) ClockBridge.Create(root, c, gm);
        foreach (Vector4 h in data.hourglasses) Hourglass.Create(root, h, gm);
        foreach (GearSpec g in data.gearBridges) GearBridge.Create(root, g, gm);
        foreach (Vector4 a in data.alarms) AlarmSleeper.Create(root, a, gm, rng, data);
        if (data.dayPads.Count > 0) DayNight.Create(root, data, gm);
    }

    // نص بالأرقام (TextMesh) يواجه الكاميرا
    public static TextMesh Label(Transform parent, string text, Vector3 pos, float size, Color color)
    {
        var go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        var t = go.AddComponent<TextMesh>();
        t.text = text;
        t.font = ArabicText.Font;
        t.fontSize = 80;
        t.characterSize = size * 0.1f;
        t.anchor = TextAnchor.MiddleCenter;
        t.alignment = TextAlignment.Center;
        t.color = color;
        go.GetComponent<MeshRenderer>().sharedMaterial = ArabicText.Font.material;
        return t;
    }
}

// ساعة كبيرة على حفرة: عقرب الدقايق هو الجسر بس يلف بدون ما يوقف.
// ارسمي خط صغير تحت المكان اللي يصير فيه العقرب مستوي: العقرب يوقف عليه ورويدا تمشي فوقه
public class ClockBridge : MonoBehaviour
{
    float from, to, speed, angle;   // الزاوية مع عقارب الساعة من الساعة 12
    bool jammed, holding;
    int prompted = -1;
    Transform hand;
    BoxCollider handCol;
    float Len => to - from + 0.2f;

    public static void Create(Transform root, Vector3 spec, GameManager gm)
    {
        var go = new GameObject("Clock Bridge");
        go.transform.SetParent(root, false);
        var c = go.AddComponent<ClockBridge>();
        c.from = spec.x; c.to = spec.y; c.speed = spec.z;
        c.angle = 200f;
        Vector3 pivot = new Vector3(c.from - 0.2f, -0.15f, 0f);
        float r = Mathf.Max(2.4f, c.Len * 0.55f);   // وجه الساعة أصغر من العقرب (شكل كرتوني)

        // وجه الساعة ورا (دايرة كريمية، شرطات، وأرقام)
        Material face = gm.Mat("Clock Face", new Color(1f, 0.97f, 0.88f), 0.3f);
        Material rim = gm.Mat("Clock Rim", new Color(0.55f, 0.35f, 0.25f), 0.4f);
        Material ink = gm.Mat("Clock Ink", new Color(0.15f, 0.13f, 0.12f), 0.3f);
        var faceT = LevelBuilder.Visual("Face", PrimitiveType.Cylinder, go.transform, pivot + new Vector3(0f, 0f, 2f), new Vector3(r * 2f, 0.05f, r * 2f), face, false).transform;
        faceT.rotation = Quaternion.Euler(90f, 0f, 0f);
        var rimT = LevelBuilder.Visual("Rim", PrimitiveType.Cylinder, go.transform, pivot + new Vector3(0f, 0f, 2.05f), new Vector3(r * 2f + 0.4f, 0.04f, r * 2f + 0.4f), rim, false).transform;
        rimT.rotation = Quaternion.Euler(90f, 0f, 0f);
        for (int i = 0; i < 12; i++)
        {
            float a = i * 30f * Mathf.Deg2Rad;
            var tick = LevelBuilder.Visual("Tick", PrimitiveType.Cube, go.transform, pivot + new Vector3(Mathf.Sin(a), Mathf.Cos(a), 0f) * (r - 0.35f) + new Vector3(0f, 0f, 1.9f), new Vector3(0.12f, i % 3 == 0 ? 0.5f : 0.3f, 0.05f), ink, false).transform;
            tick.rotation = Quaternion.Euler(0f, 0f, -i * 30f);
            if (i % 3 == 0)
                TimeFeatures.Label(go.transform, i == 0 ? "12" : (i).ToString(), pivot + new Vector3(Mathf.Sin(a), Mathf.Cos(a), 0f) * (r - 0.85f) + new Vector3(0f, 0f, 1.85f), 0.8f, ink.color);
        }
        // عقرب الساعات (زينة) يأشر على 3
        var hour = LevelBuilder.Visual("Hour Hand", PrimitiveType.Cube, go.transform, pivot + new Vector3(r * 0.25f, 0f, 1.7f), new Vector3(r * 0.5f, 0.35f, 0.08f), ink, false).transform;
        hour.rotation = Quaternion.Euler(0f, 0f, 60f);
        hour.position = pivot + hour.rotation * new Vector3(r * 0.25f, 0f, 0f) + new Vector3(0f, 0f, 1.7f);

        // عقرب الدقايق: هو الجسر (له جسم يصطدم بس لما يوقف)
        c.hand = new GameObject("Minute Hand").transform;
        c.hand.SetParent(go.transform, false);
        c.hand.position = pivot;
        Material handMat = gm.Mat("Clock Hand", new Color(0.2f, 0.18f, 0.25f), 0.4f);
        LevelBuilder.Visual("Hand", PrimitiveType.Cube, c.hand, new Vector3(c.Len * 0.5f, 0f, 0.6f), new Vector3(c.Len, 0.3f, 0.5f), handMat);
        var tip = LevelBuilder.Visual("Tip", PrimitiveType.Cube, c.hand, new Vector3(c.Len - 0.1f, 0f, 0.6f), new Vector3(0.6f, 0.6f, 0.5f), handMat).transform;
        tip.localRotation = Quaternion.Euler(0f, 0f, 45f);
        LevelBuilder.Visual("Center", PrimitiveType.Cylinder, c.hand, new Vector3(0f, 0f, 0.3f), new Vector3(0.6f, 0.1f, 0.6f), rim, false).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        c.handCol = c.hand.gameObject.AddComponent<BoxCollider>();
        c.handCol.center = new Vector3(c.Len * 0.5f, 0f, 0f);
        c.handCol.size = new Vector3(c.Len, 0.3f, 2f);
        c.handCol.enabled = false;
        c.Apply();
    }

    // عقارب الساعة: 0 = فوق، 90 = يمين (مستوي = جسر)
    void Apply() => hand.localRotation = Quaternion.Euler(0f, 0f, 90f - angle);

    // فيه خط تحت مكان العقرب لما يصير مستوي؟
    bool Stopper()
    {
        foreach (InkMark mark in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
        {
            var r = mark.GetComponent<MeshRenderer>();
            if (r == null) continue;
            Bounds b = r.bounds;
            if (b.max.x > from + 0.8f && b.min.x < to && b.max.y < 0.1f && b.max.y > -1.6f) return true;
        }
        return false;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        if (!jammed)
        {
            float next = angle + speed * Time.deltaTime;
            // وصل للمستوي وفيه خط يوقفه؟
            if (Mathf.Repeat(angle, 360f) < 90f && Mathf.Repeat(next, 360f) >= 90f && Stopper())
            {
                angle = 90f;
                jammed = true;
                handCol.enabled = true;
                gm.Sound.PlayBoing();
                Fx.Sparkle(new Vector3(to - 0.5f, 0f, 0f), new Color(1f, 0.85f, 0.4f), 18);
                gm.UI.ShowMessage("", 1.8f, "وقف العقرب!");
            }
            else angle = Mathf.Repeat(next, 360f);
            Apply();
        }

        RowaidaController rw = gm.Rowaida;
        if (rw == null) return;
        Vector3 rp = rw.transform.position;
        bool atEdge = rp.x > from - 1.6f && rp.x < from - 0.4f && rp.y > 0.3f;
        bool wait = atEdge && !jammed && gm.IsPlaying;
        if (wait && prompted != 0)
        {
            prompted = 0;
            gm.UI.ShowMessage("", 2.8f, "العقرب يلف! ارسمي خط تحت مكانه لما يصير مستوي يوقفه");
        }
        if (wait) { rw.Held = true; rw.CarryX = 0f; holding = true; CameraFollow.PullRequest = Mathf.Max(CameraFollow.PullRequest, 1f); }
        else if (holding) { rw.Held = false; holding = false; }
    }
}

// حفرة تنملي رمل: أرضية رمل تطلع لين سطح الصفحة (رويدا تستنى لين تنملي)
public class SandPit : MonoBehaviour
{
    public const float Bottom = -5f;
    public float from, to, level = Bottom, target = Bottom;
    Transform sand, mound;
    BoxCollider col;
    bool holding;
    int prompted = -1;
    public readonly List<Hourglass> glasses = new List<Hourglass>();

    public static SandPit Get(Transform root, float from, float to, GameManager gm)
    {
        foreach (SandPit p in root.GetComponentsInChildren<SandPit>())
            if (Mathf.Approximately(p.from, from)) return p;
        var go = new GameObject("Sand Pit");
        go.transform.SetParent(root, false);
        var s = go.AddComponent<SandPit>();
        s.from = from; s.to = to;
        Material sandMat = gm.Mat("Sand", new Color(0.95f, 0.8f, 0.5f), 0.15f);
        s.sand = LevelBuilder.Visual("Sand", PrimitiveType.Cube, go.transform, Vector3.zero, Vector3.one, sandMat).transform;
        s.mound = LevelBuilder.Visual("Mound", PrimitiveType.Sphere, go.transform, Vector3.zero, Vector3.one, sandMat).transform;
        s.col = go.AddComponent<BoxCollider>();
        s.Place();
        return s;
    }

    void Place()
    {
        float h = Mathf.Max(0.05f, level - Bottom), w = to - from, cx = (from + to) * 0.5f;
        sand.position = new Vector3(cx, Bottom + h * 0.5f, 0f);
        sand.localScale = new Vector3(w, h, 2.2f);
        mound.position = new Vector3(cx, level, 0f);
        mound.localScale = new Vector3(w * 0.7f, 0.35f, 1.8f);
        mound.gameObject.SetActive(level > Bottom + 0.2f && level < -0.05f);
        col.center = transform.InverseTransformPoint(new Vector3(cx, level - 0.5f, 0f));
        col.size = new Vector3(w, 1f, 2.2f);
        col.enabled = level > Bottom + 0.3f;
    }

    void Update()
    {
        level = Mathf.MoveTowards(level, Mathf.Min(target, 0f), 1.4f * Time.deltaTime);
        Place();

        GameManager gm = GameManager.Instance;
        if (gm == null || gm.Rowaida == null) return;
        RowaidaController r = gm.Rowaida;
        Vector3 p = r.transform.position;
        bool atEdge = p.x > from - 1.6f && p.x < from - 0.4f && p.y > 0.3f;
        bool wait = atEdge && level < -0.08f && gm.IsPlaying;
        if (wait && prompted != 0)
        {
            prompted = 0;
            bool any = false;
            foreach (Hourglass g in glasses) if (g.Flipped) any = true;
            gm.UI.ShowMessage("", 2.8f, any ? "باقي ساعة رملية... اقلبيها هي بعد" : "المسي الساعة الرملية بخط عشان تنقلب وتملي الحفرة");
            if (any) prompted = 1;
        }
        if (wait) { r.Held = true; r.CarryX = 0f; holding = true; CameraFollow.PullRequest = Mathf.Max(CameraFollow.PullRequest, 1f); }
        else if (holding) { r.Held = false; holding = false; }
    }
}

// ساعة رملية مقلوبة معلقة فوق حفرة: لو لمسها خط تنقلب، والرمل ينزل منها ويملي الحفرة
public class Hourglass : MonoBehaviour
{
    SandPit pit;
    float capacity, flipT = -1f, poured;
    Transform body, sandTop, sandBottom;
    Material grainMat;
    float nextGrain;
    public bool Flipped => flipT >= 0f;

    public static void Create(Transform root, Vector4 spec, GameManager gm)
    {
        var go = new GameObject("Hourglass");
        go.transform.SetParent(root, false);
        go.transform.position = new Vector3(spec.z, spec.w, 0f);
        var h = go.AddComponent<Hourglass>();
        h.pit = SandPit.Get(root, spec.x, spec.y, gm);
        h.pit.glasses.Add(h);
        h.capacity = (0f - SandPit.Bottom) / h.pit.glasses.Count + 0.2f;
        foreach (Hourglass other in h.pit.glasses) other.capacity = h.capacity;

        Material wood = gm.Mat("Hourglass Wood", new Color(0.6f, 0.38f, 0.22f), 0.35f);
        // زجاج شفاف عشان يبان الرمل اللي داخله
        Material glass = NightSky.Transparent("Hourglass Glass", Texture2D.whiteTexture, new Color(0.85f, 0.95f, 1f, 0.28f));
        Material sandMat = gm.Mat("Sand", new Color(0.95f, 0.8f, 0.5f), 0.15f);
        Material rope = gm.Mat("Thread", new Color(0.3f, 0.25f, 0.2f), 0.2f);
        // حبل معلق فيه من فوق
        LevelBuilder.Visual("Rope", PrimitiveType.Cylinder, go.transform, new Vector3(0f, 3f, 0.3f), new Vector3(0.05f, 1.8f, 0.05f), rope, false);
        h.body = new GameObject("Body").transform;
        h.body.SetParent(go.transform, false);
        for (int s = -1; s <= 1; s += 2)
        {
            LevelBuilder.Visual("Cap", PrimitiveType.Cylinder, h.body, new Vector3(0f, s * 1.15f, 0f), new Vector3(1.3f, 0.12f, 1.3f), wood);
            LevelBuilder.Visual("Post", PrimitiveType.Cylinder, h.body, new Vector3(s * 0.55f, 0f, -0.2f), new Vector3(0.1f, 1.15f, 0.1f), wood);
            LevelBuilder.Visual("Bulb", PrimitiveType.Sphere, h.body, new Vector3(0f, s * 0.55f, 0f), new Vector3(0.95f, 1.05f, 0.95f), glass, false);
        }
        // الرمل: مقلوبة يعني الرمل كله تحت
        h.sandBottom = LevelBuilder.Visual("Sand Low", PrimitiveType.Sphere, h.body, new Vector3(0f, -0.7f, -0.05f), new Vector3(0.75f, 0.6f, 0.75f), sandMat, false).transform;
        h.sandTop = LevelBuilder.Visual("Sand High", PrimitiveType.Sphere, h.body, new Vector3(0f, 0.7f, -0.05f), Vector3.zero, sandMat, false).transform;
        h.grainMat = sandMat;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        if (flipT < 0f)
        {
            // يتمايل شوي، وأي خط يلمسه يقلبه
            body.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 1.5f + transform.position.x) * 4f);
            Bounds g = new Bounds(transform.position, new Vector3(1.6f, 2.6f, 4f));
            foreach (InkMark mark in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
            {
                var r = mark.GetComponent<MeshRenderer>();
                if (r == null || !r.bounds.Intersects(g)) continue;
                flipT = 0f;
                gm.Sound.PlayBoing();
                Fx.Sparkle(transform.position, new Color(1f, 0.9f, 0.5f), 16);
                Destroy(mark.gameObject);
                break;
            }
            return;
        }

        flipT += Time.deltaTime;
        // تنقلب نص لفة
        body.localRotation = Quaternion.Euler(0f, 0f, Mathf.SmoothStep(0f, 180f, Mathf.Clamp01(flipT / 0.6f)));
        if (flipT < 0.6f) return;

        // الرمل ينزل: يخف من فوق ويملي الحفرة
        if (poured < capacity)
        {
            float step = Mathf.Min(capacity - poured, capacity / 3.2f * Time.deltaTime);
            poured += step;
            pit.target += step;
            float k = 1f - poured / capacity;
            // بعد القلبة: Sand Low صار فوق
            sandBottom.localScale = new Vector3(0.75f, 0.6f, 0.75f) * Mathf.Max(0.05f, k);
            float top = transform.position.y - 1.2f, bottom = pit.level;

            // حبات رمل تطيح
            if (Time.time >= nextGrain)
            {
                nextGrain = Time.time + 0.025f;
                var grain = LevelBuilder.Visual("Grain", PrimitiveType.Sphere, null, new Vector3(transform.position.x + Random.Range(-0.08f, 0.08f), top, -0.1f), Vector3.one * Random.Range(0.08f, 0.14f), grainMat, false);
                var fx = grain.AddComponent<FxParticle>();
                fx.velocity = Vector3.down * 3f;
                fx.gravity = 9f;
                fx.life = Mathf.Sqrt(Mathf.Max(0.1f, top - bottom) / 6f) + 0.1f;
            }
        }

    }
}

// تروس وجسر متحرك: الترس الأول (موتور) يلف، وآخر ترس جنب جسر مرفوع.
// كل خط يلمس ترسين ورا بعض يصير سير يوصلهم. لما توصل الحركة لآخر ترس، الجسر ينزل
public class GearSpec
{
    public float from, to;
    public Vector3[] gears;   // (x، y، نص القطر) — الأول موتور، والأخير ترس الجسر
    public GearSpec(float from, float to, params Vector3[] gears) { this.from = from; this.to = to; this.gears = gears; }
}

public class GearBridge : MonoBehaviour
{
    GearSpec spec;
    readonly List<Transform> gears = new List<Transform>();
    bool[] linked;
    float lowerT = -1f;
    Transform plank;
    BoxCollider plankCol;
    bool holding;
    int prompted = -1;

    public static void Create(Transform root, GearSpec spec, GameManager gm)
    {
        var go = new GameObject("Gear Bridge");
        go.transform.SetParent(root, false);
        var g = go.AddComponent<GearBridge>();
        g.spec = spec;
        g.linked = new bool[spec.gears.Length - 1];
        Material brass = gm.Mat("Gear Brass", new Color(0.85f, 0.65f, 0.3f), 0.7f, 0.5f);
        Material motor = gm.Mat("Gear Motor", new Color(0.35f, 0.6f, 0.85f), 0.6f, 0.4f);
        Material dark = gm.Mat("Gear Axle", new Color(0.25f, 0.22f, 0.2f), 0.5f);
        for (int i = 0; i < spec.gears.Length; i++)
        {
            Vector3 s = spec.gears[i];
            var gear = new GameObject("Gear").transform;
            gear.SetParent(go.transform, false);
            gear.position = new Vector3(s.x, s.y, 0.5f);
            Material m = i == 0 ? motor : brass;
            var disc = LevelBuilder.Visual("Disc", PrimitiveType.Cylinder, gear, Vector3.zero, new Vector3(s.z * 2f, 0.2f, s.z * 2f), m).transform;
            disc.localRotation = Quaternion.Euler(90f, 0f, 0f);
            int teeth = Mathf.RoundToInt(s.z * 10f);
            for (int k = 0; k < teeth; k++)
            {
                float a = k * Mathf.PI * 2f / teeth;
                var tooth = LevelBuilder.Visual("Tooth", PrimitiveType.Cube, gear, new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * (s.z + 0.12f), new Vector3(0.28f, 0.28f, 0.2f), m).transform;
                tooth.localRotation = Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg);
            }
            var axle = LevelBuilder.Visual("Axle", PrimitiveType.Cylinder, gear, new Vector3(0f, 0f, -0.12f), new Vector3(0.35f, 0.05f, 0.35f), dark, false).transform;
            axle.localRotation = Quaternion.Euler(90f, 0f, 0f);
            g.gears.Add(gear);
        }

        // الجسر: لوح خشب معلق على طرف الصفحة اليمين، مرفوع لفوق
        Material wood = gm.Mat("Bridge Wood", new Color(0.65f, 0.45f, 0.28f), 0.3f);
        Material plankLine = gm.Mat("Bridge Line", new Color(0.45f, 0.3f, 0.18f), 0.3f);
        float len = spec.to - spec.from + 0.3f;
        g.plank = new GameObject("Drawbridge").transform;
        g.plank.SetParent(go.transform, false);
        g.plank.position = new Vector3(spec.to, 0f, 0f);
        LevelBuilder.Visual("Plank", PrimitiveType.Cube, g.plank, new Vector3(-len * 0.5f, -0.15f, 0f), new Vector3(len, 0.3f, 1.8f), wood);
        for (int k = 1; k < 6; k++)
            LevelBuilder.Visual("Board", PrimitiveType.Cube, g.plank, new Vector3(-len * k / 6f, 0.005f, 0f), new Vector3(0.04f, 0.02f, 1.7f), plankLine, false);
        g.plankCol = g.plank.gameObject.AddComponent<BoxCollider>();
        g.plankCol.center = new Vector3(-len * 0.5f, -0.15f, 0f);
        g.plankCol.size = new Vector3(len, 0.3f, 2f);
        g.plankCol.enabled = false;
        g.plank.localRotation = Quaternion.Euler(0f, 0f, -80f);
    }

    bool Touches(Bounds b, int i)
    {
        Vector3 s = spec.gears[i];
        Vector3 c = new Vector3(s.x, s.y, b.center.z);
        return b.SqrDistance(c) < (s.z + 0.35f) * (s.z + 0.35f);
    }

    int Turning()
    {
        int n = 1;
        while (n < spec.gears.Length && linked[n - 1]) n++;
        return n;   // كم ترس يلف من البداية
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        // خط يلمس ترسين ورا بعض = سير
        foreach (InkMark mark in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
        {
            var r = mark.GetComponent<MeshRenderer>();
            if (r == null || mark.closed) continue;
            for (int i = 0; i < linked.Length; i++)
                if (!linked[i] && Touches(r.bounds, i) && Touches(r.bounds, i + 1))
                {
                    linked[i] = true;
                    gm.Sound.PlayBoing();
                    Fx.Sparkle(r.bounds.center, new Color(1f, 0.8f, 0.4f), 16);
                }
        }
        int turning = Turning();
        for (int i = 0; i < gears.Count; i++)
            if (i < turning) gears[i].Rotate(0f, 0f, (i % 2 == 0 ? -1f : 1f) * 120f / spec.gears[i].z * Time.deltaTime, Space.Self);

        if (turning == gears.Count && lowerT < 0f)
        {
            lowerT = 0f;
            gm.Sound.PlayRumble(0.4f);
        }
        if (lowerT >= 0f && lowerT < 1f)
        {
            lowerT += Time.deltaTime / 1.6f;
            plank.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-80f, 0f, Mathf.SmoothStep(0f, 1f, lowerT)));
            if (lowerT >= 1f)
            {
                plankCol.enabled = true;
                Fx.Dust(new Vector3(spec.from, 0f, 0f), 0.6f);
                gm.UI.ShowMessage("", 1.6f, "نزل الجسر!");
            }
        }

        RowaidaController rw = gm.Rowaida;
        if (rw == null) return;
        Vector3 rp = rw.transform.position;
        bool atEdge = rp.x > spec.from - 1.6f && rp.x < spec.from - 0.4f && rp.y > 0.3f;
        bool wait = atEdge && lowerT < 1f && gm.IsPlaying;
        if (wait && prompted != turning)
        {
            prompted = turning;
            gm.UI.ShowMessage("", 2.8f, turning == 1 ? "ارسمي خط يلمس الترسين... يصير سير ويدورهم" : "باقي ترس! وصليه بخط بعد");
        }
        if (wait) { rw.Held = true; rw.CarryX = 0f; holding = true; CameraFollow.PullRequest = Mathf.Max(CameraFollow.PullRequest, 1f); }
        else if (holding) { rw.Held = false; holding = false; }
    }
}

// منبه على رف، وقطوة نايمة تسد الطريق. لو طاحت كورة على زر المنبه يرن، والقطوة تصحى وتروح من الطريق
public class AlarmSleeper : MonoBehaviour
{
    float sleeperX, ringT = -1f, nextZ;
    Vector3 alarmPos;
    Transform cat, alarm, button, catBody;
    BoxCollider catCol;
    Material zMat;
    bool holding;
    int prompted;

    // spec: (x القطوة، x المنبه، y المنبه، سقف فوق المنبه؟ 1/0)
    public static void Create(Transform root, Vector4 spec, GameManager gm, System.Random rng, LevelData data)
    {
        var go = new GameObject("Alarm Sleeper");
        go.transform.SetParent(root, false);
        var a = go.AddComponent<AlarmSleeper>();
        a.sleeperX = spec.x;
        a.alarmPos = new Vector3(spec.y, spec.z, 0f);
        a.zMat = gm.Mat("Sleep Z", new Color(0.95f, 0.95f, 1f), 0.3f);

        // القطوة النايمة (كورة كبيرة دايرة على نفسها)
        Material fur = gm.Mat("Cat Fur", new Color(1f, 0.7f, 0.35f), 0.3f);
        Material stripe = gm.Mat("Cat Stripe", new Color(0.85f, 0.5f, 0.2f), 0.3f);
        Material pink = gm.Mat("Cat Pink", new Color(1f, 0.65f, 0.7f), 0.3f);
        Material black = gm.Mat("Monster Pupil", new Color(0.05f, 0.04f, 0.06f), 0.9f);
        a.cat = new GameObject("Cat").transform;
        a.cat.SetParent(go.transform, false);
        a.cat.position = new Vector3(spec.x, 0f, 0f);
        a.catBody = new GameObject("Body").transform;
        a.catBody.SetParent(a.cat, false);
        LevelBuilder.Visual("Body", PrimitiveType.Sphere, a.catBody, new Vector3(0f, 0.95f, 0f), new Vector3(2.4f, 1.9f, 1.8f), fur);
        for (int i = 0; i < 3; i++)
            LevelBuilder.Visual("Stripe", PrimitiveType.Cube, a.catBody, new Vector3(-0.4f + i * 0.4f, 1.85f, -0.1f), new Vector3(0.15f, 0.12f, 1.2f), stripe, false);
        LevelBuilder.Visual("Head", PrimitiveType.Sphere, a.catBody, new Vector3(-1f, 0.75f, -0.35f), new Vector3(1.1f, 1f, 1f), fur);
        for (int s = -1; s <= 1; s += 2)
        {
            var ear = LevelBuilder.Visual("Ear", PrimitiveType.Cube, a.catBody, new Vector3(-1f + s * 0.3f, 1.25f, -0.35f), new Vector3(0.3f, 0.3f, 0.2f), fur).transform;
            ear.localRotation = Quaternion.Euler(0f, 0f, 45f);
            // عيون مسكرة (خط)
            LevelBuilder.Visual("Closed Eye", PrimitiveType.Cube, a.catBody, new Vector3(-1f + s * 0.22f, 0.82f, -0.85f), new Vector3(0.2f, 0.04f, 0.02f), black, false);
        }
        LevelBuilder.Visual("Nose", PrimitiveType.Sphere, a.catBody, new Vector3(-1f, 0.66f, -0.86f), Vector3.one * 0.1f, pink, false);
        var tail = LevelBuilder.Visual("Tail", PrimitiveType.Capsule, a.catBody, new Vector3(1.1f, 0.4f, -0.4f), new Vector3(0.25f, 0.7f, 0.25f), stripe).transform;
        tail.localRotation = Quaternion.Euler(0f, 0f, 70f);
        a.catCol = a.cat.gameObject.AddComponent<BoxCollider>();
        a.catCol.center = new Vector3(0f, 1f, 0f);
        a.catCol.size = new Vector3(2.4f, 2f, 2f);

        // المنبه على رف (وأحيانا سقف فوقه: لازم الكورة تتدحرج له من الجنب)
        LevelBuilder.CreatePaperBlock(root, new Block(new Vector3(spec.y, spec.z - 0.3f, 0f), new Vector3(2.4f, 0.6f, 2.2f)), gm, rng, data, false);
        if (spec.w > 0.5f)
            LevelBuilder.CreatePaperBlock(root, new Block(new Vector3(spec.y - 0.7f, spec.z + 2.5f, 0f), new Vector3(2.8f, 0.4f, 2.2f)), gm, rng, data, false);
        Material red = gm.Mat("Alarm Red", new Color(0.95f, 0.25f, 0.25f), 0.6f);
        Material bell = gm.Mat("Alarm Bell", new Color(0.95f, 0.8f, 0.3f), 0.8f, 0.5f);
        Material face = gm.Mat("Clock Face", new Color(1f, 0.97f, 0.88f), 0.3f);
        a.alarm = new GameObject("Alarm").transform;
        a.alarm.SetParent(go.transform, false);
        a.alarm.position = a.alarmPos;
        var bodyT = LevelBuilder.Visual("Clock", PrimitiveType.Cylinder, a.alarm, new Vector3(0f, 0.55f, 0f), new Vector3(1f, 0.3f, 1f), red).transform;
        bodyT.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var faceT = LevelBuilder.Visual("Face", PrimitiveType.Cylinder, a.alarm, new Vector3(0f, 0.55f, -0.31f), new Vector3(0.8f, 0.02f, 0.8f), face, false).transform;
        faceT.localRotation = Quaternion.Euler(90f, 0f, 0f);
        for (int s = -1; s <= 1; s += 2)
        {
            LevelBuilder.Visual("Bell", PrimitiveType.Sphere, a.alarm, new Vector3(s * 0.38f, 1.1f, 0f), new Vector3(0.45f, 0.35f, 0.45f), bell);
            LevelBuilder.Visual("Leg", PrimitiveType.Cube, a.alarm, new Vector3(s * 0.32f, 0.08f, 0f), new Vector3(0.1f, 0.18f, 0.1f), bell, false);
        }
        a.button = LevelBuilder.Visual("Button", PrimitiveType.Cylinder, a.alarm, new Vector3(0f, 1.15f, 0f), new Vector3(0.3f, 0.08f, 0.3f), bell).transform;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time;

        if (ringT < 0f)
        {
            // Zzz من القطوة، وتتنفس
            catBody.localScale = new Vector3(1f, 1f + Mathf.Sin(t * 1.5f) * 0.03f, 1f);
            if (t >= nextZ)
            {
                nextZ = t + 0.9f;
                var z = LevelBuilder.Visual("Z", PrimitiveType.Cube, null, cat.position + new Vector3(-1.3f, 1.6f, -0.6f), new Vector3(0.3f, 0.06f, 0.02f), zMat, false);
                var fx = z.AddComponent<FxParticle>();
                fx.velocity = new Vector3(-0.2f, 0.7f, 0f);
                fx.grow = 0.8f;
                fx.life = 1.6f;
            }
            // كورة على المنبه؟
            foreach (InkMark mark in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
            {
                if (!mark.closed) continue;
                Vector3 c = mark.transform.position;
                if (Mathf.Abs(c.x - alarmPos.x) > 1.5f || c.y < alarmPos.y || c.y > alarmPos.y + 2.2f) continue;
                ringT = 0f;
                gm.Sound.PlayLaser();
                gm.UI.ShowMessage("", 1.8f, "رررن! صحت القطوة");
                break;
            }
        }
        else
        {
            ringT += Time.deltaTime;
            // المنبه يرن (يهتز)
            alarm.localRotation = ringT < 1.6f ? Quaternion.Euler(0f, 0f, Mathf.Sin(ringT * 60f) * 8f) : Quaternion.identity;
            if (ringT < 1.6f && Random.value < 0.3f) Fx.Sparkle(alarmPos + Vector3.up * 1.1f, new Color(1f, 0.85f, 0.3f), 2);
            // القطوة تنط وتروح ورا (تفضّي الطريق)
            if (ringT > 0.4f)
            {
                float k = Mathf.Clamp01((ringT - 0.4f) / 1.2f);
                cat.position = new Vector3(sleeperX + k * 1.5f, Mathf.Sin(k * Mathf.PI) * 1.6f, k * 4f);
                catCol.enabled = false;
            }
        }

        RowaidaController rw = gm.Rowaida;
        if (rw == null) return;
        float px = rw.transform.position.x;
        bool wait = ringT < 0.5f && px > sleeperX - 3f && px < sleeperX - 1.4f && gm.IsPlaying;
        if (wait && prompted == 0)
        {
            prompted = 1;
            gm.UI.ShowMessage("", 2.8f, "القطوة نايمة بالطريق! ارمي كورة على زر المنبه");
        }
        if (wait) { rw.Held = true; rw.CarryX = 0f; holding = true; CameraFollow.PullRequest = Mathf.Max(CameraFollow.PullRequest, 1f); }
        else if (holding) { rw.Held = false; holding = false; }
    }
}

// الليل والنهار: قرص شمس/قمر فوق، المسيه بخط عشان يتغير الوقت.
// الصفحات الصفرا تطلع بالنهار بس، والزرقا بالليل بس. والخطوط اللي ترسمينها تختفي لما يتغير الوقت
public class DayNight : MonoBehaviour
{
    public static bool Night;
    LevelData data;
    Transform dial, sunSide, moonSide;
    Renderer overlay;
    Material overlayMat;
    float dialAngle, flipCooldown, dark;
    bool holding;
    int prompted = -1;
    readonly List<(Collider col, Renderer rend, bool night)> pads = new List<(Collider, Renderer, bool)>();
    Material dayMat, nightMat, ghostDay, ghostNight;

    public static void Create(Transform root, LevelData data, GameManager gm)
    {
        Night = false;
        var go = new GameObject("Day Night");
        go.transform.SetParent(root, false);
        var d = go.AddComponent<DayNight>();
        d.data = data;
        d.dayMat = gm.Mat("Day Pad", new Color(1f, 0.85f, 0.35f), 0.3f);
        d.nightMat = gm.Mat("Night Pad", new Color(0.3f, 0.35f, 0.75f), 0.3f);
        d.ghostDay = NightSky.Transparent("Day Ghost", Texture2D.whiteTexture, new Color(1f, 0.85f, 0.35f, 0.32f));
        d.ghostNight = NightSky.Transparent("Night Ghost", Texture2D.whiteTexture, new Color(0.4f, 0.45f, 0.9f, 0.32f));
        Material sunIcon = gm.Mat("Sun Icon", new Color(1f, 0.6f, 0.1f), 0.4f);
        Material moonIcon = gm.Mat("Moon Icon", new Color(0.95f, 0.95f, 1f), 0.4f);
        foreach (Vector4 p in data.dayPads)
        {
            bool night = p.w > 0.5f;
            var pad = LevelBuilder.Visual(night ? "Night Pad" : "Day Pad", PrimitiveType.Cube, go.transform, new Vector3((p.x + p.y) * 0.5f, p.z - 0.25f, 0f), new Vector3(p.y - p.x, 0.5f, 2.2f), night ? d.nightMat : d.dayMat);
            var col = pad.AddComponent<BoxCollider>();
            // رسمة شمس أو قمر على الصفحة
            var icon = LevelBuilder.Visual("Icon", PrimitiveType.Sphere, pad.transform, new Vector3(0f, 0f, -0.52f), new Vector3(0.35f / (p.y - p.x), 0.7f, 0.05f), night ? moonIcon : sunIcon, false);
            d.pads.Add((col, pad.GetComponent<Renderer>(), night));
        }

        // القرص: نص شمس ونص قمر، يلف لما يتغير الوقت
        d.dial = new GameObject("Sun Moon Dial").transform;
        d.dial.SetParent(go.transform, false);
        Material sky = gm.Mat("Dial Rim", new Color(0.45f, 0.35f, 0.6f), 0.4f);
        var rim = LevelBuilder.Visual("Rim", PrimitiveType.Cylinder, d.dial, Vector3.zero, new Vector3(2f, 0.05f, 2f), sky, false).transform;
        rim.localRotation = Quaternion.Euler(90f, 0f, 0f);
        d.sunSide = LevelBuilder.Visual("Sun", PrimitiveType.Sphere, d.dial, new Vector3(0f, 0f, -0.1f), Vector3.one * 0.75f, gm.Mat("Sun Glow", new Color(1.6f, 1.1f, 0.3f), 0.5f), false).transform;
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI / 4f;
            var ray = LevelBuilder.Visual("Ray", PrimitiveType.Cube, d.sunSide, new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * 0.75f, new Vector3(0.35f, 0.12f, 0.1f), sunIcon, false).transform;
            ray.localRotation = Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg);
        }
        d.moonSide = LevelBuilder.Visual("Moon", PrimitiveType.Sphere, d.dial, new Vector3(0f, 0f, -0.1f), Vector3.one * 0.85f, moonIcon, false).transform;
        LevelBuilder.Visual("Moon Bite", PrimitiveType.Sphere, d.moonSide, new Vector3(0.3f, 0.2f, -0.2f), Vector3.one * 0.8f, sky, false);

        // طبقة غامقة قدام الكاميرا (الليل)
        d.overlayMat = NightSky.Transparent("Night Overlay", Texture2D.whiteTexture, new Color(0.05f, 0.05f, 0.2f, 0f));
        var cam = Camera.main;
        var ov = LevelBuilder.Visual("Night Overlay", PrimitiveType.Quad, cam.transform, new Vector3(0f, 0f, 0.6f), new Vector3(3f, 2f, 1f), d.overlayMat, false);
        d.overlay = ov.GetComponent<Renderer>();
        d.Apply();
    }

    void OnDestroy()
    {
        if (overlay != null) Destroy(overlay.gameObject);
        Night = false;
    }

    void Apply()
    {
        foreach (var p in pads)
        {
            bool on = p.night == Night;
            p.col.enabled = on;
            p.rend.sharedMaterial = on ? (p.night ? nightMat : dayMat) : (p.night ? ghostNight : ghostDay);
        }
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        Camera cam = Camera.main;
        // القرص يمشي مع الكاميرا فوق
        dial.position = new Vector3(cam.transform.position.x + 4f, 3.7f, -0.5f);
        dialAngle = Mathf.MoveTowards(dialAngle, Night ? 180f : 0f, 360f * Time.deltaTime);
        // يلف لفة وهو يتغير، ويبان بس الوقت الحالي (شمس أو قمر)
        dial.localRotation = Quaternion.Euler(0f, 0f, dialAngle);
        sunSide.gameObject.SetActive(!Night);
        moonSide.gameObject.SetActive(Night);
        sunSide.localRotation = moonSide.localRotation = Quaternion.Euler(0f, 0f, -dialAngle);
        dark = Mathf.MoveTowards(dark, Night ? 0.75f : 0f, Time.deltaTime * 1.5f);
        overlayMat.SetColor("_BaseColor", new Color(0.02f, 0.03f, 0.15f, dark));
        flipCooldown -= Time.deltaTime;

        // خطوط: كل خط يتبع وقت رسمته، ولو تغير الوقت يختفي. والخط اللي يلمس القرص يغير الوقت
        Bounds db = new Bounds(dial.position, new Vector3(2.2f, 2.2f, 6f));
        foreach (InkMark mark in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
        {
            var tag = mark.GetComponent<TimeOfInk>();
            var r = mark.GetComponent<MeshRenderer>();
            if (tag == null)
            {
                if (r != null && r.bounds.Intersects(db))
                {
                    if (flipCooldown <= 0f)
                    {
                        Night = !Night;
                        flipCooldown = 0.6f;
                        Apply();
                        gm.Sound.PlayBoing();
                        Fx.Sparkle(dial.position, Night ? new Color(0.7f, 0.75f, 1f) : new Color(1f, 0.8f, 0.3f), 20);
                    }
                    Destroy(mark.gameObject);
                    continue;
                }
                mark.gameObject.AddComponent<TimeOfInk>().night = Night;
            }
        }

        // رويدا تستنى إذا الأرض قدامها مو موجودة بهالوقت
        RowaidaController rw = gm.Rowaida;
        if (rw == null) return;
        Vector3 rp = rw.transform.position;
        bool grounded = Physics.Raycast(rp, Vector3.down, 1.2f, ~0, QueryTriggerInteraction.Ignore);

        int need = -1;
        foreach (Vector4 p in data.dayPads)
            if (p.x > rp.x && p.x < rp.x + 1.6f) need = p.w > 0.5f ? 1 : 0;
        // الصفحة اللي قدامها مو بوقتها؟ تستنى
        bool wait = gm.IsPlaying && grounded && need >= 0 && (need == 1) != Night;
        if (wait && prompted != need)
        {
            prompted = need;
            gm.UI.ShowMessage("", 2.6f, need == 1 ? "الصفحة الزرقا تطلع بالليل... المسي القمر بخط" : "الصفحة الصفرا تطلع بالنهار... المسي الشمس بخط");
        }
        if (wait) { rw.Held = true; rw.CarryX = 0f; holding = true; }
        else if (holding) { rw.Held = false; holding = false; }
    }
}

// الخط يتبع وقت رسمته (نهار أو ليل): إذا تغير الوقت يختفي
public class TimeOfInk : MonoBehaviour
{
    public bool night;
    bool shown = true;

    void Update()
    {
        bool on = night == DayNight.Night;
        if (on == shown) return;
        shown = on;
        foreach (Collider c in GetComponentsInChildren<Collider>()) c.enabled = on;
        foreach (Renderer r in GetComponentsInChildren<Renderer>()) r.enabled = on;
    }
}
