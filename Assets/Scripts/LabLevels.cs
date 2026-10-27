using System.Collections.Generic;
using UnityEngine;

// الجزء الثالث — فصل المختبر: أنابيب ألوان ترسمينها (السائل يمشي على رسمتك)، سلايم صديق ياكل ويكبر ويصير جسر،
// سلك كهربا يشغل مصعد، بركة حمض ورغوة تتمدد، ليزر ومرايا ترسمينها، وبرج رغوة لازم توقفين على الدرجة الصح
public static class LabFeatures
{
    public static void Build(Transform root, LevelData data, GameManager gm, System.Random rng)
    {
        if (data.labTaps.Count > 0) LiquidNetwork.Create(root, data, gm);
        foreach (Vector4 s in data.slimeFriends) SlimeFriend.Create(root, s, gm);
        foreach (Vector4 s in data.circuitLifts) CircuitLift.Create(root, s, gm);
        foreach (Vector3 s in data.acidPools) AcidPool.Create(root, s, gm);
        foreach (LaserSpec s in data.lasers) LaserMirror.Create(root, s, gm);
        foreach (Vector4 s in data.foamTowers) FoamTower.Create(root, s, gm);
        if (data.slimeChase.y > data.slimeChase.x) SlimeChase.Create(root, data.slimeChase.x, data.slimeChase.y, gm);
        if (data.lab > 0) LabScenery.Create(root, data, gm, rng);
    }

    // ألوان السوائل: 1 أحمر، 2 أزرق، 4 أصفر، وخلطاتها
    public static Color MixColor(int bits)
    {
        switch (bits)
        {
            case 1: return new Color(1f, 0.22f, 0.25f);
            case 2: return new Color(0.2f, 0.45f, 1f);
            case 4: return new Color(1f, 0.85f, 0.15f);
            case 3: return new Color(0.65f, 0.25f, 0.9f);
            case 6: return new Color(0.25f, 0.85f, 0.35f);
            case 5: return new Color(1f, 0.55f, 0.12f);
            case 7: return new Color(0.45f, 0.32f, 0.2f);
            default: return new Color(0.8f, 0.9f, 1f);
        }
    }

    public static string ColorName(int bits)
    {
        switch (bits)
        {
            case 3: return "بنفسجي";
            case 6: return "أخضر";
            case 5: return "برتقالي";
            case 1: return "أحمر";
            case 2: return "أزرق";
            case 4: return "أصفر";
        }
        return "";
    }

    public static Material Glass(float alpha = 0.32f)
    {
        var tex = new Texture2D(2, 2);
        tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
        tex.Apply();
        return NightSky.Transparent("Lab Glass", tex, new Color(0.85f, 0.95f, 1f, alpha));
    }

    public static Material Glow(GameManager gm, Color c, float boost = 1.6f) => gm.Mat("Glow " + ColorUtility.ToHtmlStringRGB(c) + boost, c * boost, 0.6f);

    // نقطة على مسار (بالمسافة من أوله)
    public static Vector3 PointAt(Vector3[] path, float[] cum, float d)
    {
        if (d <= 0f) return path[0];
        for (int i = 1; i < path.Length; i++)
            if (cum[i] >= d)
            {
                float k = (d - cum[i - 1]) / Mathf.Max(0.0001f, cum[i] - cum[i - 1]);
                return Vector3.Lerp(path[i - 1], path[i], k);
            }
        return path[path.Length - 1];
    }

    public static float[] Cumulative(Vector3[] path)
    {
        var cum = new float[path.Length];
        for (int i = 1; i < path.Length; i++) cum[i] = cum[i - 1] + Vector3.Distance(path[i - 1], path[i]);
        return cum;
    }

    // خط عادي يبدأ (أو ينتهي) قريب من نقطة: نرجع مساره من جهة النقطة
    public static Vector3[] LineFrom(Vector3 p, float radius, out InkMark mark)
    {
        mark = null;
        foreach (InkMark m in Object.FindObjectsByType<InkMark>(FindObjectsSortMode.None))
        {
            if (m.closed || m.path == null || m.path.Length < 2 || m.beingEaten) continue;
            Vector3 a = m.path[0], b = m.path[m.path.Length - 1];
            if (Vector2.Distance(a, p) < radius) { mark = m; return m.path; }
            if (Vector2.Distance(b, p) < radius)
            {
                mark = m;
                var rev = (Vector3[])m.path.Clone();
                System.Array.Reverse(rev);
                return rev;
            }
        }
        return null;
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
}

// ---------- الأنابيب والألوان ----------

// خزانات ألوان معلقة فوق (حنفيات)، ودوارق على الأرض كل واحد يبي لون معين، وكل دورق يفتح بوابة زجاج.
// ارسمي خط من بوز الخزان للدورق: السائل يمشي على رسمتك ويصب فيه. خلطي الألوان الصح (أحمر+أزرق = بنفسجي...)
public class LiquidNetwork : MonoBehaviour
{
    class Tap { public Vector3 nozzle; public int bit; public Transform valve; public float nextDrop; public InkMark mark; public Vector3[] path; public float[] cum; }
    class Flask { public Vector3 mouth; public int need, bits; public float level, mudT = -1f; public Transform liquid, gate; public float gateX, openT = -1f; public BoxCollider gateCol; public Renderer liquidR, lamp; public bool prompted, holding; public float lastPour; }
    class Drop { public Transform t; public Tap tap; public float d; public Vector3[] path; public float[] cum; }

    readonly List<Tap> taps = new List<Tap>();
    readonly List<Flask> flasks = new List<Flask>();
    readonly List<Drop> drops = new List<Drop>();
    GameManager gm;
    float nextPourSound;

    public static void Create(Transform root, LevelData data, GameManager gm)
    {
        var go = new GameObject("Liquid Network");
        go.transform.SetParent(root, false);
        var n = go.AddComponent<LiquidNetwork>();
        n.gm = gm;
        Material steel = gm.Mat("Spoon Steel", new Color(0.82f, 0.84f, 0.88f), 0.95f, 0.85f);
        Material glass = LabFeatures.Glass();
        foreach (Vector4 s in data.labTaps)
        {
            var t = new Tap { bit = (int)s.z, nozzle = new Vector3(s.x, s.y - 1.1f, 0f) };
            Color c = LabFeatures.MixColor(t.bit);
            // خزان زجاج كروي فيه سائل يضوي، ومعلق بأنبوب من فوق
            LevelBuilder.Visual("Tank Pipe", PrimitiveType.Cylinder, go.transform, new Vector3(s.x, s.y + 3f, 0.3f), new Vector3(0.18f, 2.4f, 0.18f), steel, false);
            LevelBuilder.Visual("Tank Liquid", PrimitiveType.Sphere, go.transform, new Vector3(s.x, s.y + 0.2f, 0.3f), new Vector3(1.05f, 0.9f, 1.05f), LabFeatures.Glow(gm, c, 1.3f), false);
            LevelBuilder.Visual("Tank Glass", PrimitiveType.Sphere, go.transform, new Vector3(s.x, s.y + 0.3f, 0.3f), Vector3.one * 1.35f, glass, false);
            LevelBuilder.Visual("Tank Cap", PrimitiveType.Cylinder, go.transform, new Vector3(s.x, s.y + 0.98f, 0.3f), new Vector3(0.5f, 0.1f, 0.5f), steel, false);
            LevelBuilder.Visual("Nozzle", PrimitiveType.Cylinder, go.transform, new Vector3(s.x, s.y - 0.7f, 0.3f), new Vector3(0.22f, 0.3f, 0.22f), steel, false);
            t.valve = new GameObject("Valve").transform;
            t.valve.SetParent(go.transform, false);
            t.valve.position = new Vector3(s.x, s.y - 0.55f, -0.05f);
            for (int k = 0; k < 2; k++) LevelBuilder.Visual("Valve Bar", PrimitiveType.Cube, t.valve, Vector3.zero, new Vector3(0.5f, 0.06f, 0.06f), LabFeatures.Glow(gm, c, 1f), false).transform.localRotation = Quaternion.Euler(0f, 0f, k * 90f);
            // نقطة منقطة تحت البوز: من هنا ابدئي الرسم
            for (int k = 0; k < 8; k++)
            {
                float a = k / 8f * Mathf.PI * 2f;
                LevelBuilder.Visual("Start Dot", PrimitiveType.Sphere, go.transform, t.nozzle + new Vector3(Mathf.Cos(a) * 0.35f, Mathf.Sin(a) * 0.35f, 0f), Vector3.one * 0.07f, LabFeatures.Glow(gm, c, 1f), false);
            }
            n.taps.Add(t);
        }
        foreach (Vector4 s in data.labFlasks)
        {
            var f = new Flask { need = (int)s.z, gateX = s.w };
            Vector3 b = new Vector3(s.x, s.y, 0.25f);
            f.mouth = new Vector3(s.x, s.y + 2.3f, 0f);
            Color need = LabFeatures.MixColor(f.need);
            // دورق مخروطي: قاع عريض ورقبة
            LevelBuilder.Visual("Flask Body", PrimitiveType.Sphere, go.transform, b + new Vector3(0f, 0.75f, 0f), new Vector3(1.7f, 1.5f, 1.7f), glass, false);
            LevelBuilder.Visual("Flask Neck", PrimitiveType.Cylinder, go.transform, b + new Vector3(0f, 1.8f, 0f), new Vector3(0.6f, 0.47f, 0.6f), LabFeatures.Glass(0.75f), false);
            LevelBuilder.Visual("Flask Rim", PrimitiveType.Cylinder, go.transform, b + new Vector3(0f, 2.27f, 0f), new Vector3(0.75f, 0.07f, 0.75f), LabFeatures.Glow(gm, need, 1.1f), false);
            f.liquid = LevelBuilder.Visual("Flask Liquid", PrimitiveType.Sphere, go.transform, b + new Vector3(0f, 0.4f, 0f), new Vector3(1.4f, 0.01f, 1.4f), LabFeatures.Glow(gm, Color.white, 1f), false).transform;
            f.liquidR = f.liquid.GetComponent<Renderer>();
            // شارة اللون المطلوب قدام الدورق
            LevelBuilder.Visual("Need Badge", PrimitiveType.Cylinder, go.transform, b + new Vector3(0f, 0.7f, -0.9f), new Vector3(0.55f, 0.03f, 0.55f), gm.Mat("Badge White", Color.white, 0.4f), false).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            LevelBuilder.Visual("Need Color", PrimitiveType.Cylinder, go.transform, b + new Vector3(0f, 0.7f, -0.93f), new Vector3(0.42f, 0.03f, 0.42f), LabFeatures.Glow(gm, need, 1.2f), false).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            for (int k = 0; k < 10; k++)
            {
                float a = k / 10f * Mathf.PI * 2f;
                LevelBuilder.Visual("End Dot", PrimitiveType.Sphere, go.transform, f.mouth + new Vector3(Mathf.Cos(a) * 0.5f, 0.15f + Mathf.Sin(a) * 0.3f, -0.5f), Vector3.one * 0.07f, LabFeatures.Glow(gm, need, 1f), false);
            }
            // البوابة الزجاج
            f.gate = new GameObject("Glass Gate").transform;
            f.gate.SetParent(go.transform, false);
            f.gate.position = new Vector3(f.gateX, s.y, 0f);
            LevelBuilder.Visual("Gate Glass", PrimitiveType.Cube, f.gate, new Vector3(0f, 1.6f, 0f), new Vector3(0.2f, 3.2f, 1.8f), LabFeatures.Glass(0.45f), false);
            LevelBuilder.Visual("Gate Tint", PrimitiveType.Cube, f.gate, new Vector3(0f, 1.6f, -0.92f), new Vector3(0.22f, 3.2f, 0.04f), LabFeatures.Glow(gm, need, 0.9f), false);
            f.gateCol = f.gate.gameObject.AddComponent<BoxCollider>();
            f.gateCol.center = new Vector3(0f, 1.6f, 0f);
            f.gateCol.size = new Vector3(0.3f, 3.2f, 2f);
            Material frame = gm.Mat("Lab Frame", new Color(0.55f, 0.6f, 0.68f), 0.7f, 0.5f);
            LevelBuilder.Visual("Gate Frame", PrimitiveType.Cube, go.transform, new Vector3(f.gateX, s.y + 3.4f, 0f), new Vector3(0.6f, 0.3f, 2.2f), frame);
            for (int sd = -1; sd <= 1; sd += 2) LevelBuilder.Visual("Gate Post", PrimitiveType.Cube, go.transform, new Vector3(f.gateX, s.y + 1.7f, sd * 1.05f), new Vector3(0.3f, 3.4f, 0.15f), frame);
            f.lamp = LevelBuilder.Visual("Gate Lamp", PrimitiveType.Sphere, go.transform, new Vector3(f.gateX, s.y + 3.7f, -0.3f), Vector3.one * 0.35f, gm.Mat("Lamp Red", new Color(1.6f, 0.2f, 0.2f), 0.5f), false).GetComponent<Renderer>();
            n.flasks.Add(f);
        }
    }

    void Update()
    {
        float t = Time.time, dt = Time.deltaTime;
        RowaidaController r = gm.Rowaida;

        // كل خزان: يدور على خط يبدأ عند بوزه
        foreach (Tap tap in taps)
        {
            if (tap.mark == null)
            {
                var p = LabFeatures.LineFrom(tap.nozzle, 0.9f, out InkMark m);
                if (p != null) { tap.mark = m; tap.path = p; tap.cum = LabFeatures.Cumulative(p); LevelSfx.Play("pour", 0.6f); }
            }
            bool flowing = tap.mark != null;
            tap.valve.localRotation = Quaternion.Euler(0f, 0f, flowing ? t * 200f : 0f);
            if (flowing && t >= tap.nextDrop)
            {
                tap.nextDrop = t + 0.05f;
                var d = LevelBuilder.Visual("Drop", PrimitiveType.Sphere, transform, tap.nozzle, Vector3.one * 0.2f, LabFeatures.Glow(gm, LabFeatures.MixColor(tap.bit), 1.4f), false).transform;
                drops.Add(new Drop { t = d, tap = tap, d = 0f, path = tap.path, cum = tap.cum });
            }
            if (!flowing && Random.value < 0.02f)
            {
                var d = LevelBuilder.Visual("Drip", PrimitiveType.Sphere, null, tap.nozzle, Vector3.one * 0.12f, LabFeatures.Glow(gm, LabFeatures.MixColor(tap.bit), 1.4f), false);
                var fx = d.AddComponent<FxParticle>();
                fx.velocity = Vector3.down; fx.gravity = 9f; fx.life = 0.6f;
            }
        }

        // القطرات تمشي على الأنابيب المرسومة
        for (int i = drops.Count - 1; i >= 0; i--)
        {
            Drop d = drops[i];
            float total = d.cum[d.cum.Length - 1];
            d.d += 5f * dt;
            if (d.tap.mark == null || d.d >= total)
            {
                Vector3 end = d.path[d.path.Length - 1];
                Flask hit = null;
                if (d.tap.mark != null) foreach (Flask f in flasks) if (Vector2.Distance(end, f.mouth) < 1.2f) hit = f;
                if (hit != null) Receive(hit, d.tap.bit);
                else
                {
                    var fx = d.t.gameObject.AddComponent<FxParticle>();
                    fx.velocity = new Vector3(Random.Range(-0.5f, 0.5f), -1f, 0f); fx.gravity = 12f; fx.life = 0.6f;
                    drops.RemoveAt(i);
                    continue;
                }
                Destroy(d.t.gameObject);
                drops.RemoveAt(i);
                continue;
            }
            d.t.position = LabFeatures.PointAt(d.path, d.cum, d.d) + new Vector3(0f, 0.05f, -0.15f) + Random.insideUnitSphere * 0.04f;
        }

        // الدوارق والبوابات
        foreach (Flask f in flasks)
        {
            if (f.mudT >= 0f)
            {
                f.mudT += dt;
                if (Random.value < 0.3f) SmokeCloud.Puff(f.mouth + Vector3.up * 0.3f, 0.7f);
                if (f.mudT > 2.2f) { f.mudT = -1f; f.bits = 0; f.level = 0f; }
            }
            else if (f.openT < 0f && f.bits == f.need && f.level >= 1f)
            {
                f.openT = 0f;
                LevelSfx.Play("chime", 0.9f, 1.3f);
                LevelSfx.Play("bake", 0.5f);
                Fx.Sparkle(f.mouth, LabFeatures.MixColor(f.need), 24);
                f.lamp.sharedMaterial = gm.Mat("Lamp Green", new Color(0.3f, 1.6f, 0.4f), 0.5f);
                gm.UI.ShowMessage("", 2.2f, "طلع " + LabFeatures.ColorName(f.need) + "! البوابة انفتحت");
            }
            if (t - f.lastPour > 0.3f && f.level > 0f && f.level < 1f && f.bits != f.need) { }
            float lv = Mathf.Clamp01(f.level);
            f.liquid.localScale = new Vector3(1.4f, Mathf.Max(0.01f, lv * 1.1f), 1.4f);
            f.liquid.position = new Vector3(f.mouth.x, f.mouth.y - 2.3f + 0.15f + lv * 0.55f, 0.25f);
            f.liquidR.sharedMaterial = f.mudT >= 0f ? gm.Mat("Mud", new Color(0.4f, 0.3f, 0.2f), 0.3f) : LabFeatures.Glow(gm, LabFeatures.MixColor(f.bits), 1.3f);
            if (f.level > 0.3f && Random.value < 0.15f)
            {
                var b = LevelBuilder.Visual("Bubble", PrimitiveType.Sphere, null, f.liquid.position + new Vector3(Random.Range(-0.4f, 0.4f), 0.2f, -0.2f), Vector3.one * Random.Range(0.08f, 0.16f), LabFeatures.Glow(gm, LabFeatures.MixColor(f.bits), 1.6f), false);
                var fx = b.AddComponent<FxParticle>();
                fx.velocity = Vector3.up * Random.Range(0.6f, 1.2f); fx.life = 0.8f;
            }
            if (f.openT >= 0f && f.openT < 1f)
            {
                f.openT = Mathf.Min(1f, f.openT + dt / 0.8f);
                f.gate.localPosition = new Vector3(f.gate.localPosition.x, Mathf.SmoothStep(0f, 3.4f, f.openT), 0f);
                if (f.openT >= 1f) f.gateCol.enabled = false;
            }
            bool wait = f.openT < 0f && HoldHelper.Before(f.gateX - 0.3f);
            if (wait && !f.prompted) { f.prompted = true; gm.UI.ShowMessage("", 3.6f, "البوابة تبي لون " + LabFeatures.ColorName(f.need) + "! ارسمي أنابيب من الخزانات للدورق وخلطي الألوان"); }
            HoldHelper.Hold(ref f.holding, wait);
            // الكاميرا ترجع شوي قرب الدورق عشان الخزانات اللي فوقه تبان
            if (r != null && f.openT < 0f && r.transform.position.x > f.mouth.x - 7f && r.transform.position.x < f.gateX + 0.5f)
                CameraFollow.PullRequest = Mathf.Max(CameraFollow.PullRequest, 1f);
        }
        if (drops.Count > 0 && t > nextPourSound && r != null && Mathf.Abs(r.transform.position.x - drops[0].t.position.x) < 14f) { nextPourSound = t + 1.4f; LevelSfx.Play("pour", 0.35f, Random.Range(0.9f, 1.1f)); }
    }

    void Receive(Flask f, int bit)
    {
        if (f.openT >= 0f || f.mudT >= 0f) return;
        f.lastPour = Time.time;
        int before = f.bits;
        f.bits |= bit;
        f.level = Mathf.Min(1.2f, f.level + 0.02f);
        if (f.bits != before) LevelSfx.Play("plop", 0.6f, 0.8f + f.bits * 0.05f);
        // لون مو بالوصفة: يخرب ويصير طين ويفضى
        if ((f.bits & ~f.need) != 0)
        {
            f.mudT = 0f;
            LevelSfx.Play("error", 0.8f);
            gm.UI.ShowMessage("", 2.6f, "اوه! صار لون طين... هذا اللون مو بالخلطة. شيلي أنبوبه وجربي ثاني");
        }
    }
}

// ---------- صديق السلايم ----------

// سلايم أخضر صغير جنب حفرة: ارسمي دواير قريب منه، ياكلها ويكبر. لما يكبر ثلاث مرات ينط بالحفرة ويتمدد جسر جيلي
public class SlimeFriend : MonoBehaviour
{
    float from, to, top, homeX, jumpT = -1f, chewT;
    int size;
    bool holding, prompted;
    Transform body, eyeL, eyeR, mouth;
    BoxCollider bridge;
    Material slime, core;

    public static void Create(Transform root, Vector4 spec, GameManager gm)
    {
        var go = new GameObject("Slime Friend");
        go.transform.SetParent(root, false);
        var s = go.AddComponent<SlimeFriend>();
        s.from = spec.x; s.to = spec.y; s.top = spec.z; s.homeX = spec.w;
        var tex = new Texture2D(2, 2);
        tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
        tex.Apply();
        s.slime = NightSky.Transparent("Slime", tex, new Color(0.35f, 1.2f, 0.45f, 0.78f));
        s.core = gm.Mat("Slime Core", new Color(0.25f, 0.75f, 0.3f), 0.8f);
        Material white = gm.Mat("Monster Eye", Color.white, 0.7f);
        Material black = gm.Mat("Monster Pupil", new Color(0.05f, 0.04f, 0.06f), 0.9f);
        s.body = new GameObject("Body").transform;
        s.body.SetParent(go.transform, false);
        LevelBuilder.Visual("Core", PrimitiveType.Sphere, s.body, new Vector3(0f, 0.4f, 0.05f), new Vector3(0.6f, 0.5f, 0.5f), s.core, false);
        LevelBuilder.Visual("Jelly", PrimitiveType.Sphere, s.body, new Vector3(0f, 0.45f, 0f), new Vector3(1.1f, 0.9f, 1f), s.slime, false);
        for (int i = 0; i < 4; i++) LevelBuilder.Visual("Bubble", PrimitiveType.Sphere, s.body, new Vector3(-0.25f + i * 0.17f, 0.3f + (i % 2) * 0.25f, -0.2f), Vector3.one * 0.08f, white, false);
        s.eyeL = MakeEye(s.body, new Vector3(-0.2f, 0.75f, -0.42f), white, black);
        s.eyeR = MakeEye(s.body, new Vector3(0.2f, 0.78f, -0.42f), white, black);
        s.mouth = LevelBuilder.Visual("Mouth", PrimitiveType.Sphere, s.body, new Vector3(0f, 0.45f, -0.46f), new Vector3(0.3f, 0.08f, 0.05f), black, false).transform;
        go.transform.position = new Vector3(s.homeX, s.top, 0.4f);
        var col = new GameObject("Slime Bridge");
        col.transform.SetParent(go.transform.parent, false);
        col.transform.position = new Vector3((s.from + s.to) * 0.5f, s.top - 0.2f, 0f);
        s.bridge = col.AddComponent<BoxCollider>();
        s.bridge.size = new Vector3(s.to - s.from + 0.4f, 0.4f, 1.6f);
        s.bridge.enabled = false;
        s.body.localScale = Vector3.one * 0.7f;
    }

    static Transform MakeEye(Transform parent, Vector3 pos, Material white, Material black)
    {
        var e = new GameObject("Eye").transform;
        e.SetParent(parent, false);
        e.localPosition = pos;
        LevelBuilder.Visual("White", PrimitiveType.Sphere, e, Vector3.zero, Vector3.one * 0.26f, white, false);
        LevelBuilder.Visual("Pupil", PrimitiveType.Sphere, e, new Vector3(0f, 0f, -0.1f), Vector3.one * 0.12f, black, false);
        return e;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time, dt = Time.deltaTime, w = to - from, cx = (from + to) * 0.5f;
        float baseScale = 0.7f + size * 0.35f;
        chewT = Mathf.Max(0f, chewT - dt);

        if (jumpT < 0f)
        {
            // دايرة قريبة؟ ياكلها
            InkMark m = LabFeatures.ClosedIn(homeX - 4f, homeX + 3f, top - 1f, top + 7f);
            if (m != null && size < 3)
            {
                Vector3 c = m.GetComponent<MeshRenderer>().bounds.center;
                Destroy(m.gameObject);
                size++;
                chewT = 0.8f;
                LevelSfx.Play("gloop", 0.9f, 1.3f - size * 0.15f);
                Fx.Sparkle(transform.position + Vector3.up, new Color(0.5f, 1f, 0.5f), 14);
                if (size >= 3) { jumpT = 0f; gm.UI.ShowMessage("", 2.2f, "شبع وكبر! شوفي وش بيسوي"); }
            }
            float wob = Mathf.Sin(t * 4f) * 0.05f + (chewT > 0f ? Mathf.Sin(t * 30f) * 0.08f : 0f);
            body.localScale = new Vector3(baseScale * (1f + wob), baseScale * (1f - wob), baseScale);
            transform.position = new Vector3(homeX, top + Mathf.Abs(Mathf.Sin(t * 2f)) * 0.08f, 0.4f);
            mouth.localScale = chewT > 0f ? new Vector3(0.3f, 0.2f + Mathf.Abs(Mathf.Sin(t * 25f)) * 0.15f, 0.05f) : new Vector3(0.3f, 0.08f, 0.05f);
        }
        else if (jumpT < 1f)
        {
            // ينط بالحفرة ويتمدد عرضها
            jumpT = Mathf.Min(1f, jumpT + dt / 1.1f);
            float k = jumpT;
            Vector3 a = new Vector3(homeX, top, 0.4f), b = new Vector3(cx, top - 0.35f, 0f);
            transform.position = Vector3.Lerp(a, b, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 2.5f;
            float stretch = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((k - 0.6f) / 0.4f));
            body.localScale = new Vector3(Mathf.Lerp(baseScale, (w + 0.6f) / 1.1f, stretch), Mathf.Lerp(baseScale, 0.55f, stretch), Mathf.Lerp(baseScale, 1.7f, stretch));
            if (jumpT >= 1f)
            {
                bridge.enabled = true;
                LevelSfx.Play("squish", 0.9f);
                LevelSfx.Play("wobble", 0.7f);
                Fx.Sparkle(b + Vector3.up * 0.4f, new Color(0.5f, 1f, 0.5f), 20);
            }
        }
        else
        {
            // جسر جيلي يتهز، وينضغط تحت رويدا
            RowaidaController r = gm.Rowaida;
            bool on = r != null && r.transform.position.x > from && r.transform.position.x < to;
            float wob = Mathf.Sin(t * 6f) * 0.04f;
            body.localScale = new Vector3((w + 0.6f) / 1.1f, 0.55f * (on ? 0.85f : 1f) * (1f + wob), 1.7f);
            if (on) body.localPosition = new Vector3(Mathf.Clamp(r.transform.position.x - cx, -w * 0.3f, w * 0.3f) * 0.05f, 0f, 0f);
        }
        // العيون تتبع رويدا
        RowaidaController rw = gm.Rowaida;
        if (rw != null)
        {
            Vector3 look = Vector3.ClampMagnitude((rw.transform.position - eyeL.position) * 0.02f, 0.05f);
            eyeL.GetChild(1).localPosition = new Vector3(look.x, look.y, -0.1f);
            eyeR.GetChild(1).localPosition = new Vector3(look.x, look.y, -0.1f);
        }

        bool wait = HoldHelper.Before(from) && !bridge.enabled && !RowaidaController.BridgeCovers(from, to, top);
        if (wait && !prompted) { prompted = true; gm.UI.ShowMessage("", 3.4f, "سلايم جوعان! ارسمي له دواير ياكلها... يكبر ويساعدك"); }
        HoldHelper.Hold(ref holding, wait);
    }
}

// ---------- دائرة كهربا ----------

// بطارية عملاقة ومصعد: ارسمي سلك من طرف البطارية (+) لمحرك المصعد فوق. الكهربا تمشي بالسلك (شرار)، والمصعد يرفع رويدا
public class CircuitLift : MonoBehaviour
{
    float bx, top, liftX, height, riseT = -1f;
    bool holding, prompted, delivered;
    Vector3 termA, termB;
    InkMark wire;
    Vector3[] path;
    float[] cum;
    Transform lift, gear, bulb;
    Rigidbody liftRb;
    readonly List<Transform> sparks = new List<Transform>();
    AudioSource crackle;
    Material sparkMat;

    public static void Create(Transform root, Vector4 spec, GameManager gm)
    {
        var go = new GameObject("Circuit Lift");
        go.transform.SetParent(root, false);
        var c = go.AddComponent<CircuitLift>();
        c.bx = spec.x; c.top = spec.y; c.liftX = spec.z; c.height = spec.w;
        Material shell = gm.Mat("Battery Shell", new Color(0.15f, 0.15f, 0.18f), 0.7f, 0.3f);
        Material copper = gm.Mat("Battery Copper", new Color(0.95f, 0.6f, 0.25f), 0.8f, 0.6f);
        Material red = gm.Mat("Tower Red", new Color(0.85f, 0.2f, 0.22f), 0.35f);
        Material steel = gm.Mat("Spoon Steel", new Color(0.82f, 0.84f, 0.88f), 0.95f, 0.85f);
        Material yellow = gm.Mat("Hazard Yellow", new Color(1f, 0.82f, 0.15f), 0.4f);
        c.sparkMat = gm.Mat("Spark", new Color(2.5f, 2.2f, 0.8f), 0.5f);
        // البطارية نايمة ورا الطريق
        var bat = LevelBuilder.Visual("Battery", PrimitiveType.Cylinder, go.transform, new Vector3(c.bx, c.top + 2.6f, 1.1f), new Vector3(1.1f, 1.3f, 1.1f), shell).transform;
        bat.localRotation = Quaternion.Euler(0f, 0f, 90f);
        LevelBuilder.Visual("Copper Band", PrimitiveType.Cylinder, go.transform, new Vector3(c.bx + 0.7f, c.top + 2.6f, 1.1f), new Vector3(1.12f, 0.6f, 1.12f), copper, false).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        LevelBuilder.Visual("Plus", PrimitiveType.Cube, go.transform, new Vector3(c.bx - 0.4f, c.top + 2.6f, 0.53f), new Vector3(0.4f, 0.1f, 0.02f), gm.Mat("Badge White", Color.white, 0.4f), false);
        LevelBuilder.Visual("Plus V", PrimitiveType.Cube, go.transform, new Vector3(c.bx - 0.4f, c.top + 2.6f, 0.53f), new Vector3(0.1f, 0.4f, 0.02f), gm.Mat("Badge White", Color.white, 0.4f), false);
        LevelBuilder.Visual("Terminal", PrimitiveType.Cylinder, go.transform, new Vector3(c.bx + 1.45f, c.top + 2.6f, 1.1f), new Vector3(0.4f, 0.15f, 0.4f), steel, false).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        c.termA = new Vector3(c.bx + 1.7f, c.top + 2.6f, 0f);
        LevelBuilder.Visual("Battery Stand", PrimitiveType.Cube, go.transform, new Vector3(c.bx, c.top + 1f, 1.1f), new Vector3(2.4f, 2f, 1.2f), gm.Mat("Lab Frame", new Color(0.55f, 0.6f, 0.68f), 0.7f, 0.5f));
        // عمود المصعد ومحرك فوق
        for (int s = -1; s <= 1; s += 2)
            LevelBuilder.Visual("Rail", PrimitiveType.Cube, go.transform, new Vector3(c.liftX + s * 0.75f, c.top + (c.height + 2f) * 0.5f, 1f), new Vector3(0.12f, c.height + 2f, 0.12f), steel);
        Vector3 motor = new Vector3(c.liftX, c.top + c.height + 2.2f, 0.6f);
        LevelBuilder.Visual("Motor", PrimitiveType.Cube, go.transform, motor, new Vector3(1.6f, 0.9f, 1f), yellow);
        c.gear = LevelBuilder.Visual("Motor Gear", PrimitiveType.Cylinder, go.transform, motor + new Vector3(0f, 0f, -0.55f), new Vector3(0.6f, 0.05f, 0.6f), steel, false).transform;
        c.gear.localRotation = Quaternion.Euler(90f, 0f, 0f);
        LevelBuilder.Visual("Motor Terminal", PrimitiveType.Sphere, go.transform, motor + new Vector3(-0.9f, 0f, -0.3f), Vector3.one * 0.3f, copper, false);
        c.termB = new Vector3(c.liftX - 0.95f, motor.y, 0f);
        c.bulb = LevelBuilder.Visual("Bulb", PrimitiveType.Sphere, go.transform, motor + new Vector3(0.5f, 0.6f, -0.2f), Vector3.one * 0.35f, gm.Mat("Bulb Off", new Color(0.6f, 0.6f, 0.55f), 0.8f), false).transform;
        // نقاط منقطة عند الطرفين (من هنا لهنا)
        foreach (Vector3 tp in new[] { c.termA, c.termB })
            for (int k = 0; k < 8; k++)
            {
                float a = k / 8f * Mathf.PI * 2f;
                LevelBuilder.Visual("Term Dot", PrimitiveType.Sphere, go.transform, tp + new Vector3(Mathf.Cos(a) * 0.4f, Mathf.Sin(a) * 0.4f, 0f), Vector3.one * 0.07f, c.sparkMat, false);
            }
        // منصة المصعد
        var l = new GameObject("Lift Platform");
        l.transform.SetParent(go.transform, false);
        l.transform.position = new Vector3(c.liftX, c.top - 0.15f, 0f);
        LevelBuilder.Visual("Plate", PrimitiveType.Cube, l.transform, Vector3.zero, new Vector3(1.6f, 0.3f, 1.8f), steel);
        LevelBuilder.Visual("Stripe", PrimitiveType.Cube, l.transform, new Vector3(0f, 0.16f, -0.85f), new Vector3(1.6f, 0.04f, 0.1f), yellow, false);
        var col = l.AddComponent<BoxCollider>();
        col.size = new Vector3(1.6f, 0.3f, 1.8f);
        c.liftRb = l.AddComponent<Rigidbody>();
        c.liftRb.isKinematic = true;
        c.liftRb.interpolation = RigidbodyInterpolation.Interpolate;
        c.lift = l.transform;
        c.crackle = LevelSfx.LoopSource(go, "crackle", 0f);
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time, dt = Time.deltaTime;
        RowaidaController r = gm.Rowaida;

        if (wire == null)
        {
            // سلك يوصل الطرفين؟
            var p = LabFeatures.LineFrom(termA, 1f, out InkMark m);
            if (p != null && Vector2.Distance(p[p.Length - 1], termB) < 1.1f)
            {
                wire = m; path = p; cum = LabFeatures.Cumulative(p);
                LevelSfx.Play("tesla", 0.6f, 1.4f);
                LevelSfx.Play("chime", 0.5f, 1.6f);
                bulb.GetComponent<Renderer>().sharedMaterial = gm.Mat("Bulb Glow", new Color(2.5f, 2f, 1.2f), 0.5f);
                gm.UI.ShowMessage("", 2f, "الكهربا وصلت!");
            }
        }
        bool powered = wire != null;
        if (powered)
        {
            gear.Rotate(0f, 300f * dt, 0f, Space.Self);
            // شرار يمشي بالسلك
            if (sparks.Count < 10 && Random.value < 0.5f) sparks.Add(LevelBuilder.Visual("Spark", PrimitiveType.Cube, transform, termA, Vector3.one * 0.14f, sparkMat, false).transform);
            float total = cum[cum.Length - 1];
            for (int i = sparks.Count - 1; i >= 0; i--)
            {
                Transform s = sparks[i];
                float d = Mathf.Repeat(t * 6f + i * total / 10f, total);
                s.position = LabFeatures.PointAt(path, cum, d) + Random.insideUnitSphere * 0.08f + new Vector3(0f, 0f, -0.15f);
                s.rotation = Random.rotation;
            }
            if (Random.value < 0.08f) Fx.Sparkle(LabFeatures.PointAt(path, cum, Random.Range(0f, total)), new Color(1f, 0.9f, 0.4f), 3);
        }
        if (crackle != null) crackle.volume = powered && r != null ? Mathf.Clamp01(1f - Mathf.Abs(r.transform.position.x - liftX) / 12f) * 0.4f : 0f;

        bool onLift = r != null && Mathf.Abs(r.transform.position.x - liftX) < 0.7f && r.transform.position.y - lift.position.y < 1.5f && r.transform.position.y > lift.position.y;
        if (powered && onLift && riseT < 0f) { riseT = 0f; LevelSfx.Play("motor", 0.5f); }
        if (riseT >= 0f && riseT < 1f)
        {
            riseT = Mathf.Min(1f, riseT + dt / 2.4f);
            liftRb.MovePosition(new Vector3(liftX, top - 0.15f + Mathf.SmoothStep(0f, 1f, riseT) * height, 0f));
            if (riseT >= 1f) { delivered = true; LevelSfx.Play("chime", 0.6f, 1.2f); }
        }
        // رويدا توقف على المصعد لين يوصل فوق
        bool wait = r != null && gm.IsPlaying && !delivered && r.transform.position.x > liftX - 0.7f && r.transform.position.x < liftX + 0.3f && r.transform.position.y - top < height + 1.5f;
        if (wait && !prompted) { prompted = true; gm.UI.ShowMessage("", 3.6f, "المصعد طافي! ارسمي سلك من طرف البطارية للمحرك فوق"); }
        HoldHelper.Hold(ref holding, wait);
    }
}

// ---------- بركة الحمض ----------

// بركة حمض أخضر يضوي: الخطوط تذوب فيه. ارسمي دواير صودا: تطيح وتفور رغوة بيضا تتمدد لين تلمس اللي جنبها... وتصير أرض
public class AcidPool : MonoBehaviour
{
    class Island { public Transform t; public BoxCollider col; public float left, right, center; public float born; }
    float from, to, top;
    bool holding, prompted;
    readonly List<Island> islands = new List<Island>();
    readonly Dictionary<InkMark, float> seen = new Dictionary<InkMark, float>();
    Material acid, foam;

    public static void Create(Transform root, Vector3 spec, GameManager gm)
    {
        var go = new GameObject("Acid Pool");
        go.transform.SetParent(root, false);
        var a = go.AddComponent<AcidPool>();
        a.from = spec.x; a.to = spec.y; a.top = spec.z;
        var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat };
        for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
            {
                float k = Mathf.Sin(x / 32f * Mathf.PI * 4f + Mathf.Sin(y / 32f * Mathf.PI * 2f) * 2f) * Mathf.Sin(y / 32f * Mathf.PI * 4f);
                tex.SetPixel(x, y, Color.Lerp(new Color(0.35f, 1.1f, 0.25f), new Color(0.75f, 1.6f, 0.4f), Mathf.Clamp01(k * 1.5f)));
            }
        tex.Apply();
        a.acid = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "Acid" };
        a.acid.SetTexture("_BaseMap", tex);
        a.acid.SetColor("_BaseColor", Color.white);
        a.acid.SetTextureScale("_BaseMap", new Vector2((a.to - a.from) / 2f, 4f));
        var q = LevelBuilder.Visual("Acid", PrimitiveType.Quad, go.transform, new Vector3((a.from + a.to) * 0.5f, a.top - 0.7f, 0.5f), new Vector3(a.to - a.from + 0.2f, 11f, 1f), a.acid, false).transform;
        q.localRotation = Quaternion.Euler(90f, 0f, 0f);
        a.foam = gm.Mat("Soda Foam", new Color(0.98f, 0.98f, 0.95f), 0.25f);
        // علامة خطر
        Material yellow = gm.Mat("Hazard Yellow", new Color(1f, 0.82f, 0.15f), 0.4f);
        Material black = gm.Mat("Monster Pupil", new Color(0.05f, 0.04f, 0.06f), 0.9f);
        var sign = LevelBuilder.Visual("Warning", PrimitiveType.Cube, go.transform, new Vector3(a.from - 1f, a.top + 1.2f, 1.4f), new Vector3(0.9f, 0.9f, 0.05f), yellow, false).transform;
        sign.localRotation = Quaternion.Euler(0f, 0f, 45f);
        LevelBuilder.Visual("Mark", PrimitiveType.Cube, go.transform, new Vector3(a.from - 1f, a.top + 1.25f, 1.36f), new Vector3(0.1f, 0.4f, 0.02f), black, false);
        LevelBuilder.Visual("Sign Pole", PrimitiveType.Cylinder, go.transform, new Vector3(a.from - 1f, a.top + 0.4f, 1.45f), new Vector3(0.06f, 0.4f, 0.06f), black, false);
    }

    bool Covered()
    {
        float reach = from;
        var sorted = new List<Island>(islands);
        sorted.Sort((p, q) => p.left.CompareTo(q.left));
        foreach (Island i in sorted)
        {
            if (i.left > reach + 0.25f) return false;
            reach = Mathf.Max(reach, i.right);
        }
        return reach >= to - 0.25f;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time, dt = Time.deltaTime;
        acid.SetTextureOffset("_BaseMap", new Vector2(Mathf.Sin(t * 0.4f) * 0.1f, t * 0.05f));
        if (Random.value < 0.25f)
        {
            var b = LevelBuilder.Visual("Acid Bubble", PrimitiveType.Sphere, null, new Vector3(Random.Range(from, to), top - 0.7f, Random.Range(-1.5f, 3f)), Vector3.one * Random.Range(0.1f, 0.25f), gm.Mat("Acid Glow", new Color(0.6f, 1.6f, 0.4f), 0.6f), false);
            var fx = b.AddComponent<FxParticle>();
            fx.velocity = Vector3.up * Random.Range(0.3f, 0.8f); fx.grow = 0.6f; fx.life = 0.7f;
        }
        // الخطوط تذوب
        foreach (InkMark m in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
        {
            if (m.closed || m.beingEaten) continue;
            var r = m.GetComponent<MeshRenderer>();
            if (r == null || r.bounds.center.x < from || r.bounds.center.x > to || r.bounds.center.y > top + 1.5f) continue;
            if (!seen.ContainsKey(m)) { seen[m] = t; continue; }
            if (t - seen[m] < 0.8f) { if (Random.value < 0.3f) SmokeCloud.Puff(r.bounds.center, 0.5f); continue; }
            m.beingEaten = true;
            LevelSfx.Play("sizzle", 0.6f);
            Fx.Sparkle(r.bounds.center, new Color(0.6f, 1f, 0.4f), 12);
            Destroy(m.gameObject);
            if (!prompted) { prompted = true; gm.UI.ShowMessage("", 3.2f, "الحمض ذوب الخط! ارسمي دواير صودا... تفور وتصير رغوة"); }
        }
        // دايرة: صودا تطيح وتفور جزيرة رغوة
        InkMark c = LabFeatures.ClosedIn(from - 0.3f, to + 0.3f, top - 3f, top + 7f);
        if (c != null && c.GetComponent<MeshRenderer>().bounds.min.y < top + 0.45f)   // لمست الحمض أو الرغوة
        {
            float x = Mathf.Clamp(c.GetComponent<MeshRenderer>().bounds.center.x, from + 0.3f, to - 0.3f);
            Destroy(c.gameObject);
            islands.Add(MakeIsland(x));
            LevelSfx.Play("fizz", 0.8f);
            for (int k = 0; k < 6; k++) SmokeCloud.Puff(new Vector3(x + Random.Range(-0.5f, 0.5f), top - 0.3f, 0f), Random.Range(0.6f, 1f));
        }
        // الجزر تتمدد لين تلمس جيرانها أو طرف البركة
        foreach (Island i in islands)
        {
            float leftLimit = from, rightLimit = to;
            foreach (Island o in islands)
            {
                if (o == i) continue;
                if (o.center < i.center) leftLimit = Mathf.Max(leftLimit, o.right);
                else rightLimit = Mathf.Min(rightLimit, o.left);
            }
            float grow = 1.4f * dt;
            i.left = Mathf.Max(Mathf.Max(leftLimit, i.center - 2.2f), i.left - grow);
            i.right = Mathf.Min(Mathf.Min(rightLimit, i.center + 2.2f), i.right + grow);
            float w = i.right - i.left, mid = (i.left + i.right) * 0.5f;
            i.t.position = new Vector3(mid, top - 0.25f, 0f);
            i.t.localScale = new Vector3(w, 1f + Mathf.Sin(t * 3f + i.center) * 0.03f, 1f);
            i.col.size = new Vector3(1f, 0.5f, 1.6f);
        }
        bool wait = HoldHelper.Before(from) && !Covered();
        if (wait && !prompted) { prompted = true; gm.UI.ShowMessage("", 3.4f, "بركة حمض! الخطوط تذوب... ارسمي دواير صودا تفور وتصير رغوة"); }
        HoldHelper.Hold(ref holding, wait);
    }

    Island MakeIsland(float x)
    {
        var go = new GameObject("Foam Island");
        go.transform.SetParent(transform, false);
        go.transform.position = new Vector3(x, top - 0.25f, 0f);
        for (int k = 0; k < 7; k++)
            LevelBuilder.Visual("Foam", PrimitiveType.Sphere, go.transform, new Vector3(-0.42f + k * 0.14f, Mathf.Sin(k * 1.7f) * 0.06f, Mathf.Cos(k * 1.3f) * 0.3f), new Vector3(0.3f, 0.6f, 1.2f), foam, false);
        var col = go.AddComponent<BoxCollider>();
        col.size = new Vector3(1f, 0.5f, 1.6f);
        var rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        return new Island { t = go.transform, col = col, left = x - 0.3f, right = x + 0.3f, center = x, born = Time.time };
    }
}

// ---------- الليزر والمرايا ----------

public class LaserSpec
{
    public Vector3 emitter;   // (x، y، زاوية الشعاع بالدرجات)
    public Vector2 sensor;    // مكان الحساس (يستقبل من فوق بس)
    public float gateX, gateY;
    public LaserSpec(Vector3 emitter, Vector2 sensor, float gateX, float gateY = 0f) { this.emitter = emitter; this.sensor = sensor; this.gateX = gateX; this.gateY = gateY; }
}

// جهاز ليزر يطلق شعاع أحمر. ارسمي خطوط مستقيمة: تصير مرايا يرتد عليها الشعاع. وصلي الشعاع للحساس (من فوق) ينفتح الباب
public class LaserMirror : MonoBehaviour
{
    LaserSpec spec;
    readonly List<Transform> beams = new List<Transform>();
    Transform hitGlow, sensorCup, gate;
    BoxCollider gateCol;
    float powerT, openT = -1f;
    bool holding, prompted;
    Renderer sensorLamp;
    AudioSource hum;
    Material beamMat;

    public static void Create(Transform root, LaserSpec spec, GameManager gm)
    {
        var go = new GameObject("Laser");
        go.transform.SetParent(root, false);
        var l = go.AddComponent<LaserMirror>();
        l.spec = spec;
        Material dark = gm.Mat("Stove Black", new Color(0.15f, 0.14f, 0.16f), 0.6f, 0.3f);
        Material steel = gm.Mat("Spoon Steel", new Color(0.82f, 0.84f, 0.88f), 0.95f, 0.85f);
        l.beamMat = gm.Mat("Laser Beam", new Color(3f, 0.25f, 0.3f), 0.5f);
        // الجهاز
        var em = new GameObject("Emitter").transform;
        em.SetParent(go.transform, false);
        em.position = new Vector3(spec.emitter.x, spec.emitter.y, 0f);
        em.localRotation = Quaternion.Euler(0f, 0f, spec.emitter.z);
        LevelBuilder.Visual("Body", PrimitiveType.Cube, em, new Vector3(-0.5f, 0f, 0f), new Vector3(1.2f, 0.6f, 0.6f), dark);
        LevelBuilder.Visual("Lens", PrimitiveType.Cylinder, em, new Vector3(0.15f, 0f, 0f), new Vector3(0.35f, 0.12f, 0.35f), l.beamMat, false).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        LevelBuilder.Visual("Stand", PrimitiveType.Cylinder, go.transform, new Vector3(spec.emitter.x - 0.3f, spec.emitter.y - 0.6f, 0.4f), new Vector3(0.12f, 0.5f, 0.12f), steel, false);
        // الحساس: كوب يستقبل من فوق
        l.sensorCup = new GameObject("Sensor").transform;
        l.sensorCup.SetParent(go.transform, false);
        l.sensorCup.position = new Vector3(spec.sensor.x, spec.sensor.y, 0f);
        LevelBuilder.Visual("Cup", PrimitiveType.Cylinder, l.sensorCup, new Vector3(0f, -0.2f, 0f), new Vector3(0.8f, 0.2f, 0.8f), steel);
        l.sensorLamp = LevelBuilder.Visual("Eye", PrimitiveType.Sphere, l.sensorCup, new Vector3(0f, 0.02f, 0f), new Vector3(0.6f, 0.2f, 0.6f), gm.Mat("Lamp Red", new Color(1.6f, 0.2f, 0.2f), 0.5f), false).GetComponent<Renderer>();
        for (int k = 0; k < 10; k++)
        {
            float a = k / 10f * Mathf.PI * 2f;
            LevelBuilder.Visual("Sensor Dot", PrimitiveType.Sphere, l.sensorCup, new Vector3(Mathf.Cos(a) * 0.6f, 0.5f + Mathf.Sin(a) * 0.15f, -0.1f), Vector3.one * 0.07f, l.beamMat, false);
        }
        // الباب المعدني
        l.gate = new GameObject("Laser Gate").transform;
        l.gate.SetParent(go.transform, false);
        l.gate.position = new Vector3(spec.gateX, spec.gateY, 0f);
        LevelBuilder.Visual("Door", PrimitiveType.Cube, l.gate, new Vector3(0f, 1.7f, 0f), new Vector3(0.35f, 3.4f, 1.9f), gm.Mat("Lab Frame", new Color(0.55f, 0.6f, 0.68f), 0.7f, 0.5f));
        LevelBuilder.Visual("Door Stripe", PrimitiveType.Cube, l.gate, new Vector3(0f, 1.7f, -0.96f), new Vector3(0.37f, 0.3f, 0.02f), gm.Mat("Hazard Yellow", new Color(1f, 0.82f, 0.15f), 0.4f), false);
        l.gateCol = l.gate.gameObject.AddComponent<BoxCollider>();
        l.gateCol.center = new Vector3(0f, 1.7f, 0f);
        l.gateCol.size = new Vector3(0.4f, 3.4f, 2f);
        l.hitGlow = LevelBuilder.Visual("Hit Glow", PrimitiveType.Sphere, go.transform, Vector3.zero, Vector3.one * 0.25f, l.beamMat, false).transform;
        l.hum = LevelSfx.LoopSource(go, "laserhum", 0f);
    }

    Transform Beam(int i)
    {
        while (beams.Count <= i) beams.Add(LevelBuilder.Visual("Beam", PrimitiveType.Cube, transform, Vector3.zero, Vector3.one, beamMat, false).transform);
        return beams[i];
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time, dt = Time.deltaTime;
        // نجمع قطع المرايا (كل الخطوط العادية)
        var segs = new List<Vector4>();
        foreach (InkMark m in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
        {
            if (m.closed || m.path == null || m.beingEaten) continue;
            for (int i = 1; i < m.path.Length; i++) segs.Add(new Vector4(m.path[i - 1].x, m.path[i - 1].y, m.path[i].x, m.path[i].y));
        }
        Vector2 pos = new Vector2(spec.emitter.x, spec.emitter.y) + (Vector2)(Quaternion.Euler(0f, 0f, spec.emitter.z) * Vector3.right) * 0.3f;
        Vector2 dir = Quaternion.Euler(0f, 0f, spec.emitter.z) * Vector3.right;
        bool hitSensor = false;
        int used = 0;
        for (int bounce = 0; bounce < 8; bounce++)
        {
            float best = 40f;
            int bestSeg = -1;
            for (int i = 0; i < segs.Count; i++)
            {
                Vector2 a = new Vector2(segs[i].x, segs[i].y), b = new Vector2(segs[i].z, segs[i].w);
                if (SegHit(pos, dir, a, b, out float d) && d > 0.05f && d < best) { best = d; bestSeg = i; }
            }
            // جدران وصفحات (غير الخطوط ورويدا)
            foreach (RaycastHit h in Physics.RaycastAll(new Vector3(pos.x, pos.y, 0f), new Vector3(dir.x, dir.y, 0f), best, ~0, QueryTriggerInteraction.Ignore))
            {
                if (h.collider.GetComponentInParent<InkMark>() != null || h.collider.GetComponentInParent<RowaidaController>() != null) continue;
                if (h.distance < best) { best = h.distance; bestSeg = -2; }
            }
            // الحساس (من فوق بس)
            Vector2 sc = spec.sensor + Vector2.up * 0.1f;
            Vector2 toS = sc - pos;
            float along = Vector2.Dot(toS, dir);
            if (along > 0f && along < best && (toS - dir * along).magnitude < 0.45f && dir.y < -0.5f) { best = along; bestSeg = -3; }

            Vector2 end = pos + dir * best;
            Transform bm = Beam(used++);
            bm.gameObject.SetActive(true);
            bm.position = new Vector3((pos.x + end.x) * 0.5f, (pos.y + end.y) * 0.5f, -0.1f);
            bm.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            bm.localScale = new Vector3(best, 0.08f + Mathf.Sin(t * 40f) * 0.015f, 0.08f);
            if (bestSeg == -3) { hitSensor = true; hitGlow.position = new Vector3(end.x, end.y, -0.1f); break; }
            if (bestSeg < 0) { hitGlow.position = new Vector3(end.x, end.y, -0.1f); break; }
            // ترتد على المراية
            Vector2 sa = new Vector2(segs[bestSeg].x, segs[bestSeg].y), sb = new Vector2(segs[bestSeg].z, segs[bestSeg].w);
            Vector2 s = (sb - sa).normalized, n = new Vector2(-s.y, s.x);
            dir = (dir - 2f * Vector2.Dot(dir, n) * n).normalized;
            pos = end;
            if (Random.value < 0.15f) Fx.Sparkle(new Vector3(end.x, end.y, -0.1f), new Color(1f, 0.4f, 0.4f), 2);
        }
        for (int i = used; i < beams.Count; i++) beams[i].gameObject.SetActive(false);
        hitGlow.localScale = Vector3.one * (0.25f + Mathf.Sin(t * 30f) * 0.05f);

        RowaidaController r = gm.Rowaida;
        if (hum != null) hum.volume = r != null ? Mathf.Clamp01(1f - Mathf.Abs(r.transform.position.x - spec.emitter.x) / 14f) * 0.25f : 0f;
        powerT = hitSensor ? powerT + dt : 0f;
        sensorLamp.sharedMaterial = hitSensor ? gm.Mat("Lamp Green", new Color(0.3f, 1.6f, 0.4f), 0.5f) : gm.Mat("Lamp Red", new Color(1.6f, 0.2f, 0.2f), 0.5f);
        if (powerT > 0.5f && openT < 0f) { openT = 0f; LevelSfx.Play("chime", 0.9f, 1.4f); LevelSfx.Play("whoosh", 0.6f); Fx.Sparkle(new Vector3(spec.sensor.x, spec.sensor.y, 0f), new Color(1f, 0.5f, 0.5f), 20); }
        if (openT >= 0f && openT < 1f)
        {
            openT = Mathf.Min(1f, openT + dt / 0.9f);
            gate.localPosition = new Vector3(gate.localPosition.x, spec.gateY + Mathf.SmoothStep(0f, 3.6f, openT), 0f);
            if (openT >= 1f) gateCol.enabled = false;
        }
        bool wait = openT < 0f && HoldHelper.Before(spec.gateX - 0.3f);
        if (wait && !prompted) { prompted = true; gm.UI.ShowMessage("", 3.6f, "ليزر! ارسمي خطوط مستقيمة تصير مرايا... وصلي الشعاع للحساس من فوق"); }
        HoldHelper.Hold(ref holding, wait);
    }

    // شعاع يصطدم بقطعة؟ (المسافة d)
    static bool SegHit(Vector2 p, Vector2 d, Vector2 a, Vector2 b, out float dist)
    {
        dist = 0f;
        Vector2 e = b - a;
        float den = d.x * e.y - d.y * e.x;
        if (Mathf.Abs(den) < 1e-5f) return false;
        Vector2 ap = a - p;
        float tt = (ap.x * e.y - ap.y * e.x) / den;
        float u = (ap.x * d.y - ap.y * d.x) / den;
        if (tt < 0f || u < 0f || u > 1f) return false;
        dist = tt;
        return true;
    }
}

// ---------- برج الرغوة ----------

// دورق كبير جنب جدار فيه شباك عالي. رويدا توقف على المنصة فوق الدورق. كل قطرة (دايرة) ترسمينها فوق الدورق
// ترفع الرغوة درجة. لازم توقف عند الدرجة الصح (مقابل الشباك)... لو زادت، تصدم رويدا السقف وتستنى الرغوة تنزل
public class FoamTower : MonoBehaviour
{
    float x, top, step, target;
    int level;
    float shown, nextSettle;
    bool holding, prompted, done, warnedHigh;
    Transform foam, plate;
    Rigidbody plateRb;
    readonly List<Renderer> marks = new List<Renderer>();
    Material markOn, markOff, markTarget;

    public static void Create(Transform root, Vector4 spec, GameManager gm)
    {
        var go = new GameObject("Foam Tower");
        go.transform.SetParent(root, false);
        var f = go.AddComponent<FoamTower>();
        f.x = spec.x; f.top = spec.y; f.step = spec.z; f.target = spec.w;
        Material glass = LabFeatures.Glass(0.28f);
        Material foamM = gm.Mat("Soda Foam", new Color(0.98f, 0.98f, 0.95f), 0.25f);
        Material pink = gm.Mat("Potion Pink", new Color(1.3f, 0.45f, 0.9f), 0.7f);
        f.markOn = gm.Mat("Lamp Green", new Color(0.3f, 1.6f, 0.4f), 0.5f);
        f.markOff = gm.Mat("Mark Off", new Color(0.6f, 0.65f, 0.7f), 0.5f);
        f.markTarget = gm.Mat("Hazard Yellow", new Color(1f, 0.82f, 0.15f), 0.4f);
        Vector3 b = new Vector3(f.x, f.top, 1.3f);
        // أنبوب زجاج طويل مدرّج
        float tubeH = f.step * 5f + 1f;
        LevelBuilder.Visual("Tube", PrimitiveType.Cylinder, go.transform, b + new Vector3(0f, tubeH * 0.5f, 0f), new Vector3(1.6f, tubeH * 0.5f, 1.6f), glass, false);
        LevelBuilder.Visual("Potion", PrimitiveType.Cylinder, go.transform, b + new Vector3(0f, 0.4f, 0f), new Vector3(1.5f, 0.4f, 1.5f), pink, false);
        for (int i = 1; i <= 5; i++)
        {
            var m = LevelBuilder.Visual("Mark", PrimitiveType.Cube, go.transform, b + new Vector3(-0.85f, i * f.step, -0.5f), new Vector3(0.5f, 0.08f, 0.05f), i == (int)f.target ? f.markTarget : f.markOff, false).GetComponent<Renderer>();
            f.marks.Add(m);
        }
        f.foam = LevelBuilder.Visual("Foam Column", PrimitiveType.Cylinder, go.transform, b, new Vector3(1.4f, 0.01f, 1.4f), foamM, false).transform;
        // منصة المكبس على الطريق
        var p = new GameObject("Piston Plate");
        p.transform.SetParent(go.transform, false);
        p.transform.position = new Vector3(f.x, f.top - 0.15f, 0f);
        LevelBuilder.Visual("Plate", PrimitiveType.Cube, p.transform, Vector3.zero, new Vector3(1.6f, 0.3f, 1.8f), gm.Mat("Lab Frame", new Color(0.55f, 0.6f, 0.68f), 0.7f, 0.5f));
        for (int k = 0; k < 6; k++) LevelBuilder.Visual("Foam Under", PrimitiveType.Sphere, p.transform, new Vector3(-0.6f + k * 0.24f, -0.25f, Random.Range(-0.5f, 0.5f)), Vector3.one * 0.5f, foamM, false);
        var col = p.AddComponent<BoxCollider>();
        col.size = new Vector3(1.6f, 0.3f, 1.8f);
        f.plateRb = p.AddComponent<Rigidbody>();
        f.plateRb.isKinematic = true;
        f.plateRb.interpolation = RigidbodyInterpolation.Interpolate;
        f.plate = p.transform;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time, dt = Time.deltaTime;
        RowaidaController r = gm.Rowaida;

        // قطرة (دايرة) فوق الدورق
        if (!done)
        {
            InkMark m = LabFeatures.ClosedIn(x - 1.6f, x + 1.6f, top + 1f, top + 12f);
            if (m != null && level < 5)
            {
                Destroy(m.gameObject);
                level++;
                nextSettle = t + 5f;
                LevelSfx.Play("fizz", 0.7f, 0.9f + level * 0.08f);
                LevelSfx.Play("plop", 0.6f, 0.8f + level * 0.1f);
            }
        }
        // الرغوة تنزل درجة كل شوي إذا زادت عن المطلوب
        if (level > target && t > nextSettle) { level--; nextSettle = t + 5f; LevelSfx.Play("slurp", 0.5f); }
        // الرغوة ما ترتفع إلا لما رويدا واقفة على المنصة
        bool onPlate = r != null && Mathf.Abs(r.transform.position.x - x) < 0.7f && r.transform.position.y > plate.position.y && r.transform.position.y - plate.position.y < 1.5f;
        if (onPlate || shown > 0.01f || level == 0) shown = Mathf.MoveTowards(shown, level, dt * 1.6f);
        float h = shown * step;
        foam.position = new Vector3(x, top + h * 0.5f, 1.3f);
        foam.localScale = new Vector3(1.4f + Mathf.Sin(t * 6f) * 0.03f, Mathf.Max(0.01f, h * 0.5f), 1.4f + Mathf.Cos(t * 5f) * 0.03f);
        plateRb.MovePosition(new Vector3(x, top - 0.15f + h, 0f));
        for (int i = 0; i < marks.Count; i++) marks[i].sharedMaterial = i + 1 == (int)target ? markTarget : i < level ? markOn : markOff;
        if (Random.value < 0.2f && h > 0.1f) SmokeCloud.Puff(new Vector3(x + Random.Range(-0.6f, 0.6f), top + h + 0.2f, 1.3f), 0.6f);

        bool atTarget = level == (int)target && Mathf.Abs(shown - level) < 0.02f;
        if (atTarget && onPlate && !done) { done = true; LevelSfx.Play("chime", 0.8f, 1.3f); Fx.Sparkle(plate.position + Vector3.up, new Color(1f, 0.9f, 0.5f), 18); }
        if (level > target && onPlate && !warnedHigh) { warnedHigh = true; gm.UI.ShowMessage("", 2.8f, "عالي زيادة! استني الرغوة تنزل درجة..."); }
        bool wait = r != null && gm.IsPlaying && !done && r.transform.position.x > x - 0.05f && r.transform.position.x < x + 0.6f && r.transform.position.y - top < 8f;
        if (wait && !prompted) { prompted = true; gm.UI.ShowMessage("", 3.8f, "برج الرغوة! كل قطرة (دايرة) فوقه ترفعه درجة... وقفي عند العلامة الصفرا بالضبط"); }
        HoldHelper.Hold(ref holding, wait);
    }
}
