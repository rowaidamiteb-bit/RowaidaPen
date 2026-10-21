using System.Collections.Generic;
using UnityEngine;

// الجزء الثاني — فصل أدوات المدرسة: مسطرة ميزان، صندوق الألوان، آلة حاسبة، دباسة، ومقلمة
public static class SchoolFeatures
{
    public static void Build(Transform root, LevelData data, GameManager gm, System.Random rng)
    {
        foreach (Vector4 r in data.rulers) RulerSeesaw.Create(root, r, gm);
        foreach (Vector3 g in data.colorGates) ColorGate.Create(root, g, gm);
        foreach (CalcSpec c in data.calcs) CalcPuzzle.Create(root, c, gm);
        foreach (Vector3 s in data.scissors) GiantScissors.Create(root, s, gm);
        foreach (Vector3 z in data.zippers) Zipper.Create(root, z, gm);
        foreach (Vector3 s in data.sharpeners) Sharpener.Create(root, s, gm);
        foreach (Vector3 g in data.glues) GluePuddle.Create(root, g, gm);
    }
}

// مسطرة فوق حفرة، موزونة على قلم واقف. على طرفها اليسار ممحاة ثقيلة فهي مايلة لتحت.
// ارمي كورة على طرفها الثاني: الكورة تلزق، وإذا صارت أثقل (أبعد = أقوى) تتعدل المسطرة وتصير جسر
public class RulerSeesaw : MonoBehaviour
{
    const float MaxTilt = 28f;
    float from, to, pivotX, eraserMass, angle = MaxTilt;
    Rigidbody rb;
    BoxCollider plank;
    bool holding;
    int prompted;
    readonly List<Transform> weights = new List<Transform>();
    readonly List<float> weightMass = new List<float>();

    float ArmLeft => pivotX - from;
    float ArmRight => to - pivotX;

    public static void Create(Transform root, Vector4 spec, GameManager gm)
    {
        var go = new GameObject("Ruler Seesaw");
        go.transform.SetParent(root, false);
        go.transform.position = new Vector3(spec.z, 0f, 0f);
        var s = go.AddComponent<RulerSeesaw>();
        s.from = spec.x; s.to = spec.y; s.pivotX = spec.z; s.eraserMass = spec.w;
        s.rb = go.AddComponent<Rigidbody>();
        s.rb.isKinematic = true;
        s.rb.interpolation = RigidbodyInterpolation.Interpolate;
        float len = s.to - s.from, mid = (s.from + s.to) * 0.5f - s.pivotX;
        s.plank = go.AddComponent<BoxCollider>();
        s.plank.center = new Vector3(mid, -0.12f, 0f);
        s.plank.size = new Vector3(len, 0.24f, 2f);

        // المسطرة: خشب أصفر وعليها شرطات ونص سنتي
        Material wood = gm.Mat("Ruler Wood", new Color(1f, 0.85f, 0.35f), 0.3f);
        Material tick = gm.Mat("Ruler Tick", new Color(0.15f, 0.12f, 0.1f), 0.3f);
        LevelBuilder.Visual("Ruler", PrimitiveType.Cube, go.transform, new Vector3(mid, -0.12f, 0f), new Vector3(len, 0.24f, 1.6f), wood);
        int n = Mathf.FloorToInt(len / 0.25f);
        for (int i = 1; i < n; i++)
        {
            float x = s.from - s.pivotX + i * 0.25f;
            float l = i % 4 == 0 ? 0.5f : i % 2 == 0 ? 0.32f : 0.2f;
            LevelBuilder.Visual("Tick", PrimitiveType.Cube, go.transform, new Vector3(x, 0.005f, -0.8f + l * 0.5f), new Vector3(0.03f, 0.02f, l), tick, false);
        }
        // ممحاة ثقيلة على الطرف اليسار
        Material pink = gm.Mat("Eraser Rubber", new Color(1f, 0.6f, 0.66f), 0.25f);
        Material blue = gm.Mat("Eraser Sleeve", new Color(0.28f, 0.48f, 0.9f), 0.35f);
        float ex = s.from - s.pivotX + 0.55f, size = 0.55f + s.eraserMass * 0.15f;
        // معلقة تحت الطرف بخيط (عشان ما تسد طريق رويدا)
        Material thread = gm.Mat("Thread", new Color(0.3f, 0.25f, 0.2f), 0.2f);
        LevelBuilder.Visual("Thread", PrimitiveType.Cylinder, go.transform, new Vector3(ex, -0.6f, 0f), new Vector3(0.04f, 0.4f, 0.04f), thread, false);
        LevelBuilder.Visual("Eraser", PrimitiveType.Cube, go.transform, new Vector3(ex, -1f - size * 0.5f, 0f), new Vector3(size * 1.4f, size, 1f), pink);
        LevelBuilder.Visual("Eraser Sleeve", PrimitiveType.Cube, go.transform, new Vector3(ex + size * 0.3f, -1f - size * 0.5f, 0f), new Vector3(size * 0.8f, size * 1.04f, 1.04f), blue);

        // القلم اللي واقفة عليه (من قاع الحفرة) ورأسه المبري تحت المسطرة
        Material pencil = gm.Mat("Big Pencil", new Color(1f, 0.75f, 0.2f), 0.35f);
        Material lead = gm.Mat("Pencil Lead", new Color(0.2f, 0.2f, 0.22f), 0.5f);
        Material cone = gm.Mat("Pencil Wood", new Color(0.95f, 0.8f, 0.6f), 0.2f);
        LevelBuilder.Visual("Post", PrimitiveType.Cylinder, root, new Vector3(spec.z, -4.6f, 0.3f), new Vector3(0.6f, 3.6f, 0.6f), pencil);
        var tip = LevelBuilder.Visual("Tip", PrimitiveType.Cube, root, new Vector3(spec.z, -0.75f, 0.3f), new Vector3(0.5f, 0.5f, 0.5f), cone).transform;
        tip.rotation = Quaternion.Euler(0f, 0f, 45f);
        LevelBuilder.Visual("Lead", PrimitiveType.Sphere, root, new Vector3(spec.z, -0.35f, 0.3f), Vector3.one * 0.2f, lead, false);

        go.transform.rotation = Quaternion.Euler(0f, 0f, MaxTilt);
    }

    void FixedUpdate()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        // كورة طاحت على المسطرة؟ تلزق (وكل ما كانت أكبر كانت أثقل)
        Bounds pb = plank.bounds;
        pb.Expand(0.25f);
        foreach (InkMark mark in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
        {
            if (!mark.closed || weights.Contains(mark.transform)) continue;
            var body = mark.GetComponent<Rigidbody>();
            var rend = mark.GetComponent<MeshRenderer>();
            if (body == null || rend == null || !rend.bounds.Intersects(pb)) continue;
            // لازم تكون لامسة سطح المسطرة نفسه (بإحداثيات المسطرة)
            Vector3 lp0 = transform.InverseTransformPoint(rend.bounds.center);
            if (lp0.x < -ArmLeft || lp0.x > ArmRight || lp0.y < -0.3f || lp0.y > rend.bounds.extents.y + 0.25f) continue;
            body.isKinematic = true;
            mark.transform.SetParent(transform, true);
            // صارت ثقل: ما تسد الطريق، وتنزاح ورا رويدا شوي
            foreach (Collider c in mark.GetComponentsInChildren<Collider>()) c.enabled = false;
            mark.transform.position += new Vector3(0f, 0f, 0.65f);
            weights.Add(mark.transform);
            weightMass.Add(Mathf.Clamp(rend.bounds.size.x, 0.8f, 2.5f));
            LevelSfx.Play("clack");
            Fx.Sparkle(rend.bounds.center, new Color(1f, 0.85f, 0.4f), 10);
        }

        // العزم: الممحاة يسار، الكور، ورويدا لو واقفة عليها
        float torque = -eraserMass * (ArmLeft - 0.55f);
        for (int i = 0; i < weights.Count; i++)
            if (weights[i] != null) torque += weightMass[i] * transform.InverseTransformPoint(weights[i].position).x;
        RowaidaController r = gm.Rowaida;
        if (r != null)
        {
            Vector3 lp = transform.InverseTransformPoint(r.transform.position);
            if (lp.x > -ArmLeft && lp.x < ArmRight && lp.y > 0f && lp.y < 1.6f) torque += lp.x;
        }
        // اليمين أثقل: تنعدل وترتاح على الصفحة اللي بعدها. اليسار أثقل: تميل لتحت
        float target = Mathf.Clamp(-torque * 9f, 0f, MaxTilt);
        angle = Mathf.MoveTowards(angle, target, 60f * Time.fixedDeltaTime);
        rb.MoveRotation(Quaternion.Euler(0f, 0f, angle));

        // رويدا تستنى عند الطرف لين تتعدل المسطرة
        if (r == null) return;
        Vector3 p = r.transform.position;
        bool atEdge = p.x > from - 1.6f && p.x < from - 0.4f && p.y > 0.3f;
        bool wait = atEdge && angle > 2f && gm.IsPlaying;
        if (wait && prompted == 0)
        {
            prompted = 1;
            gm.UI.ShowMessage("", 2.5f, "ارمي كورة على طرف المسطرة الثاني... كل ما كانت أبعد كانت أقوى");
        }
        if (wait) { r.Held = true; r.CarryX = 0f; holding = true; }
        else if (holding) { r.Held = false; holding = false; }
    }
}

// باب ملوّن (صندوق الألوان): ما ينفتح إلا إذا رسمتي عليه بلونه. الباب البنفسجي يبي أحمر وأزرق
public class ColorGate : MonoBehaviour
{
    float x, openT = -1f, shake;
    InkColor colorA, colorB;
    bool needB, gotA, gotB, holding;
    int prompted;
    Transform door;
    BoxCollider col;
    Renderer halfA, halfB;
    Material dull;

    public static void Create(Transform root, Vector3 spec, GameManager gm)
    {
        var go = new GameObject("Color Gate");
        go.transform.SetParent(root, false);
        go.transform.position = new Vector3(spec.x, 0f, 0f);
        var g = go.AddComponent<ColorGate>();
        g.x = spec.x;
        g.colorA = (InkColor)(int)spec.y;
        g.needB = spec.z >= 0f;
        g.colorB = g.needB ? (InkColor)(int)spec.z : g.colorA;

        Material frame = gm.Mat("Gate Frame", new Color(0.97f, 0.95f, 0.9f), 0.3f);
        g.dull = gm.Mat("Gate Dull", new Color(0.82f, 0.8f, 0.78f), 0.3f);
        g.door = new GameObject("Door").transform;
        g.door.SetParent(go.transform, false);
        g.col = go.AddComponent<BoxCollider>();
        g.col.center = new Vector3(0f, 1.7f, 0f);
        g.col.size = new Vector3(0.6f, 3.4f, 2.4f);
        // الباب: نصين، كل نص بلون (لو باب بلون واحد النصين نفس اللون)
        Material ma = gm.Mat("Gate " + g.colorA, InkColorEffect.ColorOf(g.colorA), 0.4f);
        Material mb = gm.Mat("Gate " + g.colorB, InkColorEffect.ColorOf(g.colorB), 0.4f);
        g.halfA = LevelBuilder.Visual("Top", PrimitiveType.Cube, g.door, new Vector3(0f, 2.55f, 0f), new Vector3(0.55f, 1.7f, 2.3f), ma).GetComponent<Renderer>();
        g.halfB = LevelBuilder.Visual("Bottom", PrimitiveType.Cube, g.door, new Vector3(0f, 0.85f, 0f), new Vector3(0.55f, 1.7f, 2.3f), mb).GetComponent<Renderer>();
        // شخابيط بيضا على الباب (مثل ورق ملون)
        for (int i = 0; i < 4; i++)
            LevelBuilder.Visual("Stripe", PrimitiveType.Cube, g.door, new Vector3(-0.29f, 0.4f + i * 0.85f, 0f), new Vector3(0.02f, 0.06f, 2f), frame, false);
        // إطار وفوقه أقلام شمع بالألوان المطلوبة
        LevelBuilder.Visual("Frame Top", PrimitiveType.Cube, go.transform, new Vector3(0f, 3.55f, 0f), new Vector3(0.8f, 0.3f, 2.6f), frame);
        AddCrayon(go.transform, g.colorA, gm, g.needB ? -0.45f : 0f);
        if (g.needB) AddCrayon(go.transform, g.colorB, gm, 0.45f);
    }

    static void AddCrayon(Transform parent, InkColor c, GameManager gm, float z)
    {
        Material m = gm.Mat("Crayon " + c, InkColorEffect.ColorOf(c), 0.35f);
        var body = LevelBuilder.Visual("Crayon", PrimitiveType.Cylinder, parent, new Vector3(0f, 4.1f, z - 0.3f), new Vector3(0.28f, 0.45f, 0.28f), m).transform;
        var tip = LevelBuilder.Visual("Crayon Tip", PrimitiveType.Cylinder, parent, new Vector3(0f, 4.65f, z - 0.3f), new Vector3(0.18f, 0.12f, 0.18f), m).transform;
        body.localRotation = tip.localRotation = Quaternion.identity;
    }

    static string Name(InkColor c) => c == InkColor.Red ? "الأحمر (2)" : c == InkColor.Blue ? "الأزرق (3)" : c == InkColor.Green ? "الأخضر (4)" : "الأسود (1)";

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        if (openT >= 0f)
        {
            // ينفتح: يطلع لفوق
            openT += Time.deltaTime;
            door.localPosition = Vector3.up * Mathf.SmoothStep(0f, 5f, Mathf.Clamp01(openT / 0.8f));
        }
        else
        {
            if (shake > 0f) shake -= Time.deltaTime;
            door.localPosition = new Vector3(shake > 0f ? Mathf.Sin(Time.time * 60f) * 0.06f : 0f, 0f, 0f);
            // خط لامس الباب؟
            foreach (InkMark mark in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
            {
                var r = mark.GetComponent<MeshRenderer>();
                if (r == null) continue;
                Bounds b = r.bounds;
                if (b.max.x < x - 0.5f || b.min.x > x + 0.5f || b.min.y > 3.6f || b.max.y < 0f) continue;
                var fx = mark.GetComponent<InkColorEffect>();
                InkColor c = fx != null ? fx.color : InkColor.Black;
                bool used = false;
                if (c == colorA && !gotA) { gotA = true; used = true; }
                else if (needB && c == colorB && !gotB) { gotB = true; used = true; }
                Fx.Sparkle(b.center, InkColorEffect.ColorOf(c), used ? 22 : 8);
                if (used) LevelSfx.Play("crayon");
                else
                {
                    shake = 0.4f;
                    LevelSfx.Play("buzz");
                    gm.UI.ShowMessage("", 2f, "لون غلط! الباب يبي " + Wanted());
                }
                Destroy(mark.gameObject);
            }
            // النص اللي انصبغ يلمع، واللي باقي باهت شوي
            halfA.transform.localScale = new Vector3(gotA ? 0.6f : 0.55f, 1.7f, 2.3f);
            halfB.transform.localScale = new Vector3(gotB || (!needB && gotA) ? 0.6f : 0.55f, 1.7f, 2.3f);
            if (gotA && (!needB || gotB))
            {
                openT = 0f;
                col.enabled = false;
                LevelSfx.Play("success");
                Fx.Confetti(new Vector3(x, 2.5f, -0.5f), 30);
            }
        }

        // رويدا تستنى عند الباب المقفل
        RowaidaController r0 = gm.Rowaida;
        if (r0 == null) return;
        float px = r0.transform.position.x;
        bool wait = openT < 0f && px > x - 1.6f && px < x - 0.3f && gm.IsPlaying;
        if (wait && prompted == 0)
        {
            prompted = 1;
            gm.UI.ShowMessage("", 2.8f, "الباب يبي " + Wanted() + "... اختاري اللون وارسمي عليه");
        }
        if (wait) { r0.Held = true; r0.CarryX = 0f; holding = true; CameraFollow.PullRequest = Mathf.Max(CameraFollow.PullRequest, 1f); }
        else if (holding) { r0.Held = false; holding = false; }
    }

    string Wanted()
    {
        if (!needB) return Name(colorA);
        if (gotA) return Name(colorB);
        if (gotB) return Name(colorA);
        return Name(colorA) + " و" + Name(colorB);
    }
}

// آلة حاسبة: لوحة أزرار أرقام طايرة فوق الطريق، وشاشة فيها رقم. ارمي كور على الأزرار:
// إذا صار مجموعها نفس الرقم ينفتح الباب، وإذا زاد ترجع الأزرار من جديد
public class CalcSpec
{
    public float gateX, panelX, panelY = 3f;
    public int target;
    public int[] keys;
    public float move;   // اللوحة تتحرك يمين ويسار (كم متر)
    public CalcSpec(float gateX, float panelX, int target, params int[] keys) { this.gateX = gateX; this.panelX = panelX; this.target = target; this.keys = keys; }
}

public class CalcPuzzle : MonoBehaviour
{
    const float Spacing = 1.45f;
    CalcSpec spec;
    readonly List<Transform> keyCaps = new List<Transform>();
    readonly List<GameObject> keyBall = new List<GameObject>();
    TextMesh screen;
    Transform rig;
    Transform door;
    BoxCollider doorCol;
    float openT = -1f, resetAt = -1f;
    bool holding;
    int prompted;

    public static void Create(Transform root, CalcSpec spec, GameManager gm)
    {
        var go = new GameObject("Calculator");
        go.transform.SetParent(root, false);
        var c = go.AddComponent<CalcPuzzle>();
        c.spec = spec;
        Material body = gm.Mat("Calc Body", new Color(0.3f, 0.32f, 0.38f), 0.4f);
        Material key = gm.Mat("Calc Key", new Color(0.93f, 0.93f, 0.9f), 0.5f);
        Material lcd = gm.Mat("Calc Screen", new Color(0.72f, 0.85f, 0.6f), 0.6f);
        Material orange = gm.Mat("Calc Orange", new Color(1f, 0.6f, 0.2f), 0.5f);
        // كل شي (اللوحة، الأزرار، الشاشة) داخل حامل يقدر يتحرك
        c.rig = new GameObject("Rig").transform;
        c.rig.SetParent(go.transform, false);

        // لوحة الأزرار
        float w = spec.keys.Length * Spacing + 0.4f;
        float cx = spec.panelX + (spec.keys.Length - 1) * Spacing * 0.5f;
        var panel = LevelBuilder.Visual("Panel", PrimitiveType.Cube, c.rig, new Vector3(cx, spec.panelY - 0.25f, 0f), new Vector3(w, 0.5f, 2f), body);
        panel.AddComponent<BoxCollider>();
        for (int i = 0; i < spec.keys.Length; i++)
        {
            float x = spec.panelX + i * Spacing;
            var cap = LevelBuilder.Visual("Key", PrimitiveType.Cube, c.rig, new Vector3(x, spec.panelY + 0.12f, 0f), new Vector3(1.15f, 0.25f, 1.3f), key).transform;
            cap.gameObject.AddComponent<BoxCollider>();
            c.keyCaps.Add(cap);
            c.keyBall.Add(null);
            Label(c.rig, spec.keys[i].ToString(), new Vector3(x, spec.panelY + 0.05f, -0.72f), 0.55f, new Color(0.15f, 0.15f, 0.2f));
        }
        // الشاشة فوق اللوحة
        LevelBuilder.Visual("Screen Frame", PrimitiveType.Cube, c.rig, new Vector3(cx, spec.panelY + 1.55f, 0.4f), new Vector3(Mathf.Max(4f, w * 0.7f), 1.3f, 0.3f), body);
        LevelBuilder.Visual("Screen", PrimitiveType.Cube, c.rig, new Vector3(cx, spec.panelY + 1.55f, 0.22f), new Vector3(Mathf.Max(3.6f, w * 0.7f - 0.4f), 0.95f, 0.05f), lcd, false);
        c.screen = Label(c.rig, "", new Vector3(cx, spec.panelY + 1.55f, 0.15f), 0.9f, new Color(0.1f, 0.15f, 0.1f));

        // الباب (شكل زر "=" كبير)
        c.door = new GameObject("Calc Door").transform;
        c.door.SetParent(go.transform, false);
        c.door.position = new Vector3(spec.gateX, 0f, 0f);
        LevelBuilder.Visual("Door", PrimitiveType.Cube, c.door, new Vector3(0f, 1.6f, 0f), new Vector3(0.6f, 3.2f, 2.3f), orange);
        for (int s = 0; s < 2; s++)
            LevelBuilder.Visual("Equals", PrimitiveType.Cube, c.door, new Vector3(-0.32f, 1.4f + s * 0.45f, 0f), new Vector3(0.04f, 0.15f, 1.2f), key, false);
        c.doorCol = c.door.gameObject.AddComponent<BoxCollider>();
        c.doorCol.center = new Vector3(0f, 1.6f, 0f);
        c.doorCol.size = new Vector3(0.6f, 3.2f, 2.3f);
        c.UpdateScreen();
    }

    static TextMesh Label(Transform parent, string text, Vector3 pos, float size, Color color)
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

    int Sum()
    {
        int s = 0;
        for (int i = 0; i < keyBall.Count; i++) if (keyBall[i] != null) s += spec.keys[i];
        return s;
    }

    void UpdateScreen()
    {
        var parts = new List<string>();
        for (int i = 0; i < keyBall.Count; i++) if (keyBall[i] != null) parts.Add(spec.keys[i].ToString());
        int sum = Sum();
        // الأرقام من اليسار لليمين مثل الآلة الحاسبة
        if (sum < spec.target) parts.Add("?");
        screen.text = string.Join(" + ", parts) + "  =  " + spec.target;
        screen.color = sum > spec.target ? new Color(0.8f, 0.1f, 0.1f) : new Color(0.1f, 0.15f, 0.1f);
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        // لوحة تتحرك: الأزرار والكور اللي عليها يمشون معها
        if (spec.move > 0f)
        {
            rig.localPosition = new Vector3(Mathf.Sin(Time.time * 0.7f) * spec.move, 0f, 0f);
            for (int i = 0; i < keyBall.Count; i++)
                if (keyBall[i] != null) keyBall[i].transform.position = new Vector3(keyCaps[i].position.x, keyBall[i].transform.position.y, keyBall[i].transform.position.z);
        }

        if (openT >= 0f)
        {
            openT += Time.deltaTime;
            door.position = new Vector3(spec.gateX, -Mathf.SmoothStep(0f, 3.6f, Mathf.Clamp01(openT / 0.8f)), 0f);
        }
        else
        {
            // كورة نزلت على زر؟
            for (int i = 0; i < keyCaps.Count; i++)
            {
                if (keyBall[i] != null) continue;
                Vector3 k = keyCaps[i].position;
                foreach (InkMark mark in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
                {
                    if (!mark.closed || keyBall.Contains(mark.gameObject)) continue;
                    Vector3 c = mark.transform.position;
                    if (Mathf.Abs(c.x - k.x) > 0.7f || c.y < k.y || c.y > k.y + 1.4f) continue;
                    var body = mark.GetComponent<Rigidbody>();
                    if (body != null) { body.isKinematic = true; }
                    // تنزل وتقعد على الزر
                    var rr = mark.GetComponent<MeshRenderer>();
                    float half = rr != null ? rr.bounds.extents.y : 0.45f;
                    mark.transform.position = new Vector3(k.x, k.y + 0.12f + half, c.z);
                    keyBall[i] = mark.gameObject;
                    keyCaps[i].localPosition += Vector3.down * 0.12f;
                    LevelSfx.Play("beep", 1f, 0.8f + spec.keys[i] * 0.08f);
                    UpdateScreen();
                    break;
                }
            }
            int sum = Sum();
            if (sum == spec.target)
            {
                openT = 0f;
                doorCol.enabled = false;
                LevelSfx.Play("success");
                Fx.Confetti(new Vector3(spec.gateX, 2f, -0.5f), 30);
                gm.UI.ShowMessage("", 1.8f, "صح! " + spec.target);
            }
            else if (sum > spec.target && resetAt < 0f)
            {
                resetAt = Time.time + 1.2f;
                LevelSfx.Play("error");
                gm.UI.ShowMessage("", 2f, "أكثر من " + spec.target + "! جربي مرة ثانية");
            }
            if (resetAt > 0f && Time.time >= resetAt)
            {
                resetAt = -1f;
                for (int i = 0; i < keyBall.Count; i++)
                {
                    if (keyBall[i] == null) continue;
                    Fx.Sparkle(keyBall[i].transform.position, new Color(1f, 0.6f, 0.2f), 10);
                    Destroy(keyBall[i]);
                    keyBall[i] = null;
                    keyCaps[i].localPosition += Vector3.up * 0.12f;
                }
                UpdateScreen();
            }
        }

        // رويدا تستنى عند الباب
        RowaidaController r = gm.Rowaida;
        if (r == null) return;
        float px = r.transform.position.x;
        bool wait = openT < 0f && px > spec.gateX - 1.6f && px < spec.gateX - 0.3f && gm.IsPlaying;
        if (wait && prompted == 0)
        {
            prompted = 1;
            gm.UI.ShowMessage("", 2.8f, "ارمي كور على الأرقام... لازم مجموعها يصير " + spec.target);
        }
        if (wait) { r.Held = true; r.CarryX = 0f; holding = true; CameraFollow.PullRequest = Mathf.Max(CameraFollow.PullRequest, 1f); }
        else if (holding) { r.Held = false; holding = false; }
    }
}

// مقص كبير نايم على الحفرة: يفتح ويسكر بسرعة ويقص أي جسر بين شفراته.
// ارمي كورة بين الشفرات: تعلق ويظل المقص مفتوح، وبعدين ارسمي الجسر
public class GiantScissors : MonoBehaviour
{
    const float OpenAngle = 30f, Period = 2.4f;
    float from, to, angle = OpenAngle, jamAngle = -1f, nextSnip;
    Transform upper, lower, pivot;
    GameObject jamBall;
    bool holding;
    int prompted = -1;
    float Len => to - from + 0.8f;

    public static void Create(Transform root, Vector3 spec, GameManager gm)
    {
        var go = new GameObject("Giant Scissors");
        go.transform.SetParent(root, false);
        var s = go.AddComponent<GiantScissors>();
        s.from = spec.x; s.to = spec.y;
        s.pivot = new GameObject("Pivot").transform;
        s.pivot.SetParent(go.transform, false);
        s.pivot.position = new Vector3(s.from - 0.3f, 0.15f, 0.9f);   // ورا طريق رويدا شوي
        Material steel = gm.Mat("Scissor Steel", new Color(0.78f, 0.8f, 0.85f), 0.85f, 0.7f);
        Material handle = gm.Mat("Scissor Handle", new Color(0.95f, 0.3f, 0.45f), 0.5f);
        Material screw = gm.Mat("Scissor Screw", new Color(0.4f, 0.4f, 0.45f), 0.8f, 0.6f);
        s.upper = Blade(s.pivot, s.Len, steel, handle, 1f);
        s.lower = Blade(s.pivot, s.Len, steel, handle, -1f);
        LevelBuilder.Visual("Screw", PrimitiveType.Cylinder, s.pivot, new Vector3(0f, 0f, -0.15f), new Vector3(0.35f, 0.05f, 0.35f), screw, false).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
    }

    // شفرة (مثلث طويل) وورا المسمار مقبض حلقة
    static Transform Blade(Transform pivot, float len, Material steel, Material handle, float side)
    {
        var b = new GameObject("Blade").transform;
        b.SetParent(pivot, false);
        b.localPosition = new Vector3(0f, 0f, side * 0.06f);
        var blade = LevelBuilder.Visual("Steel", PrimitiveType.Cube, b, new Vector3(len * 0.5f, side * 0.12f, 0f), new Vector3(len, 0.28f, 0.05f), steel, false).transform;
        LevelBuilder.Visual("Edge", PrimitiveType.Cube, b, new Vector3(len * 0.5f, side * 0.02f, -0.03f), new Vector3(len, 0.04f, 0.02f), steel, false);
        var tip = LevelBuilder.Visual("Tip", PrimitiveType.Cube, b, new Vector3(len, side * 0.05f, 0f), new Vector3(0.3f, 0.3f, 0.05f), steel, false).transform;
        tip.localRotation = Quaternion.Euler(0f, 0f, 45f);
        var ring = new GameObject("Handle").transform;
        ring.SetParent(b, false);
        ring.localPosition = new Vector3(-1.1f, side * 0.35f, 0f);
        ring.localRotation = Quaternion.Euler(0f, 90f, 0f);
        ring.gameObject.AddComponent<MeshFilter>().sharedMesh = PaperMeshes.Ring(0.45f, 0.1f, 24, 6);
        ring.gameObject.AddComponent<MeshRenderer>().sharedMaterial = handle;
        return b;
    }

    // زاوية الشفرة اللي فوق لو كانت واقفة على الكورة
    float AngleFor(Bounds ball)
    {
        Vector3 p = pivot.position;
        float dx = Mathf.Max(0.5f, ball.center.x - p.x);
        return Mathf.Clamp(Mathf.Atan2(ball.max.y - p.y, dx) * Mathf.Rad2Deg, 8f, OpenAngle);
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time;

        if (jamAngle < 0f)
        {
            // كورة بين الشفرات؟ تعلق
            foreach (InkMark mark in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
            {
                if (!mark.closed) continue;
                var r = mark.GetComponent<MeshRenderer>();
                if (r == null) continue;
                Bounds b = r.bounds;
                if (b.center.x < from + 0.4f || b.center.x > to || b.center.y < -0.4f || b.center.y > 3f) continue;
                jamBall = mark.gameObject;
                var body = jamBall.GetComponent<Rigidbody>();
                if (body != null) body.isKinematic = true;
                foreach (Collider c in jamBall.GetComponentsInChildren<Collider>()) c.enabled = false;
                jamBall.transform.position = new Vector3(b.center.x, Mathf.Max(b.center.y, 0.3f + b.extents.y), 0.9f);
                jamAngle = AngleFor(jamBall.GetComponent<MeshRenderer>().bounds);
                LevelSfx.Play("clank");
                Fx.Sparkle(b.center, new Color(1f, 0.9f, 0.5f), 18);
                gm.UI.ShowMessage("", 2f, "علق المقص! ارسمي الجسر");
                break;
            }
        }

        // يفتح ويسكر (إلا لو معلق)
        float target;
        if (jamAngle >= 0f) target = jamAngle;
        else
        {
            float local = Mathf.Repeat(t, Period);
            target = local < 1.4f ? OpenAngle : local < 1.55f ? Mathf.Lerp(OpenAngle, 0f, (local - 1.4f) / 0.15f) : local < 1.95f ? 0f : Mathf.Lerp(0f, OpenAngle, (local - 1.95f) / 0.45f);
        }
        angle = jamAngle >= 0f ? Mathf.MoveTowards(angle, target, 120f * Time.deltaTime) : target;
        upper.localRotation = Quaternion.Euler(0f, 0f, angle);
        lower.localRotation = Quaternion.Euler(0f, 0f, -angle);

        // سكر: يقص أي خط بين الشفرات
        if (jamAngle < 0f && angle < 2f && t >= nextSnip)
        {
            nextSnip = t + 0.5f;
            LevelSfx.Play("snip");
            foreach (InkMark mark in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
            {
                if (mark.closed) continue;
                var r = mark.GetComponent<MeshRenderer>();
                if (r == null) continue;
                Bounds b = r.bounds;
                if (b.max.x < from + 0.2f || b.min.x > to - 0.2f || b.min.y > 1.2f || b.max.y < -0.8f) continue;
                Fx.Sparkle(new Vector3((from + to) * 0.5f, b.center.y, 0f), new Color(0.8f, 0.8f, 0.85f), 16);
                Destroy(mark.gameObject);
            }
        }

        // رويدا تستنى لين يعلق المقص وينرسم جسر
        RowaidaController rw = gm.Rowaida;
        if (rw == null) return;
        Vector3 rp = rw.transform.position;
        bool atEdge = rp.x > from - 1.6f && rp.x < from - 0.5f && rp.y > 0.3f;
        bool bridge = RowaidaController.BridgeCovers(from, to, 0f);
        bool wait = atEdge && gm.IsPlaying && (jamAngle < 0f || !bridge);
        int stage = jamAngle < 0f ? 0 : 1;
        if (wait && prompted != stage)
        {
            prompted = stage;
            gm.UI.ShowMessage("", 2.6f, stage == 0 ? "المقص يقص الجسور! ارمي كورة بين شفراته" : "ارسمي الجسر");
        }
        if (wait) { rw.Held = true; rw.CarryX = 0f; holding = true; }
        else if (holding) { rw.Held = false; holding = false; }
    }
}

// ---------- المقلمة ----------

// رويدا تستنى قبل حفرة لين شرط معين يصير
static class HoldHelper
{
    public static void Hold(ref bool holding, bool wait)
    {
        RowaidaController r = GameManager.Instance != null ? GameManager.Instance.Rowaida : null;
        if (r == null) return;
        if (wait) { r.Held = true; r.CarryX = 0f; holding = true; }
        else if (holding) { r.Held = false; holding = false; }
    }

    public static bool Before(float x0)
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.Rowaida == null || !gm.IsPlaying) return false;
        float x = gm.Rowaida.transform.position.x;
        return x > x0 - 1.6f && x < x0 - 0.4f;
    }
}

// سحاب المقلمة مفتوح فوق حفرة: ارسمي خط من المسكة لقدام، المسكة تمشي عليه وتسكر السحاب ويصير جسر
public class Zipper : MonoBehaviour
{
    float from, to, top, slider, target;
    bool holding, prompted;
    readonly List<Transform> upper = new List<Transform>(), lower = new List<Transform>();
    Transform pull, bridgeT;
    BoxCollider bridge;
    const float Tooth = 0.26f;

    public static void Create(Transform root, Vector3 spec, GameManager gm)
    {
        var go = new GameObject("Zipper");
        go.transform.SetParent(root, false);
        var z = go.AddComponent<Zipper>();
        z.from = spec.x; z.to = spec.y; z.top = spec.z;
        z.slider = z.target = z.from;
        Material cloth = gm.Mat("Zip Cloth", new Color(0.35f, 0.45f, 0.75f), 0.2f);
        Material teeth = gm.Mat("Zip Teeth", new Color(0.85f, 0.85f, 0.88f), 0.8f, 0.6f);
        Material metal = gm.Mat("Zip Pull", new Color(0.95f, 0.75f, 0.3f), 0.8f, 0.7f);
        int n = Mathf.CeilToInt((z.to - z.from) / Tooth);
        for (int i = 0; i < n; i++)
        {
            var a = LevelBuilder.Visual("Tooth", PrimitiveType.Cube, go.transform, Vector3.zero, new Vector3(Tooth * 0.75f, 0.12f, 0.35f), teeth, false).transform;
            LevelBuilder.Visual("Cloth", PrimitiveType.Cube, a, new Vector3(0f, 0f, -1.3f), new Vector3(1.4f, 0.5f, 2f), cloth, false);
            z.upper.Add(a);
            var b = LevelBuilder.Visual("Tooth", PrimitiveType.Cube, go.transform, Vector3.zero, new Vector3(Tooth * 0.75f, 0.12f, 0.35f), teeth, false).transform;
            LevelBuilder.Visual("Cloth", PrimitiveType.Cube, b, new Vector3(0f, 0f, 1.3f), new Vector3(1.4f, 0.5f, 2f), cloth, false);
            z.lower.Add(b);
        }
        z.pull = new GameObject("Pull").transform;
        z.pull.SetParent(go.transform, false);
        LevelBuilder.Visual("Slider", PrimitiveType.Cube, z.pull, new Vector3(0f, 0.05f, 0f), new Vector3(0.5f, 0.28f, 0.9f), metal);
        var tab = LevelBuilder.Visual("Tab", PrimitiveType.Cube, z.pull, new Vector3(0.35f, 0.25f, -0.2f), new Vector3(0.55f, 0.1f, 0.3f), metal).transform;
        tab.localRotation = Quaternion.Euler(0f, 0f, 25f);
        z.bridgeT = new GameObject("Zip Bridge").transform;
        z.bridgeT.SetParent(go.transform, false);
        z.bridge = z.bridgeT.gameObject.AddComponent<BoxCollider>();
        z.bridge.enabled = false;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time;

        // خط يبدأ عند المسكة؟ المسكة تمشي لين آخره
        if (target < to - 0.05f)
            foreach (InkMark m in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
            {
                if (m.closed) continue;
                var r = m.GetComponent<MeshRenderer>();
                if (r == null) continue;
                Bounds b = r.bounds;
                if (b.min.x < slider + 1.3f && b.max.x > slider + 0.8f && Mathf.Abs(b.center.y - top) < 1.6f)
                {
                    target = Mathf.Clamp(b.max.x, slider, to);
                    if (to - target < 0.6f) target = to;
                    Destroy(m.gameObject);
                    LevelSfx.Play("zip", 0.9f, Random.Range(0.95f, 1.1f));
                }
            }
        if (slider < target)
        {
            slider = Mathf.MoveTowards(slider, target, 7f * Time.deltaTime);
            if (slider >= to - 0.01f) Fx.Sparkle(new Vector3(to, top, 0f), new Color(1f, 0.85f, 0.4f), 14);
        }

        // الجزء المسكر مستوي، والباقي مفتوح ومتدلي
        float open = to - slider;
        for (int i = 0; i < upper.Count; i++)
        {
            float x = from + (i + 0.5f) * Tooth;
            if (x <= slider)
            {
                upper[i].position = new Vector3(x, top - 0.08f, -0.12f);
                lower[i].position = new Vector3(x + Tooth * 0.5f, top - 0.08f, 0.12f);
                upper[i].localRotation = lower[i].localRotation = Quaternion.identity;
            }
            else
            {
                float k = Mathf.Sin(Mathf.PI * Mathf.Clamp01((x - slider) / Mathf.Max(0.01f, open)));
                float sway = Mathf.Sin(t * 2f + i * 0.4f) * 0.05f;
                upper[i].position = new Vector3(x, top - 0.08f - k * 0.9f + sway, -0.12f - k * 1.1f);
                lower[i].position = new Vector3(x + Tooth * 0.5f, top - 0.08f - k * 1.5f - sway, 0.12f + k * 1.1f);
                upper[i].localRotation = Quaternion.Euler(-k * 35f, 0f, 0f);
                lower[i].localRotation = Quaternion.Euler(k * 35f, 0f, 0f);
            }
        }
        pull.position = new Vector3(slider, top, 0f);
        pull.localRotation = Quaternion.Euler(0f, 0f, slider < target ? Mathf.Sin(t * 30f) * 8f : 0f);

        // الجسر: من بداية الحفرة لين المسكة
        float w = slider - from;
        bridge.enabled = w > 0.2f;
        bridgeT.position = new Vector3(from + w * 0.5f, top - 0.15f, 0f);
        bridge.size = new Vector3(Mathf.Max(0.01f, w + 0.2f), 0.3f, 2f);

        bool wait = HoldHelper.Before(from) && slider < to - 0.01f;
        if (wait && !prompted) { prompted = true; gm.UI.ShowMessage("", 3f, "السحاب مفتوح! ارسمي خط من المسكة لين آخر الحفرة عشان يتسكر"); }
        HoldHelper.Hold(ref holding, wait);
    }
}

// براية عملاقة جوعانة جنب حفرة: أي خط فوق الحفرة تسحبه وتبريه. وهي تبري (مشغولة) ارسمي الجسر بسرعة
public class Sharpener : MonoBehaviour
{
    float from, to, top, busyUntil;
    bool holding, prompted, promptedEat;
    Transform blade, crank, box;
    InkMark eating;
    float eatT;
    Vector3 eatStart;
    Material shaving;
    const float Busy = 5f;

    bool IsBusy => Time.time < busyUntil;

    public static void Create(Transform root, Vector3 spec, GameManager gm)
    {
        var go = new GameObject("Sharpener");
        go.transform.SetParent(root, false);
        var s = go.AddComponent<Sharpener>();
        s.from = spec.x; s.to = spec.y; s.top = spec.z;
        Material body = gm.Mat("Sharpener Body", new Color(0.3f, 0.55f, 0.9f), 0.6f, 0.3f);
        Material steel = gm.Mat("Sharpener Steel", new Color(0.8f, 0.82f, 0.86f), 0.9f, 0.8f);
        Material hole = gm.Mat("Monster Pupil", new Color(0.05f, 0.04f, 0.06f), 0.9f);
        Material white = gm.Mat("Monster Eye", Color.white, 0.7f);
        s.shaving = gm.Mat("Shaving Wood", new Color(0.85f, 0.65f, 0.4f), 0.2f);
        s.box = new GameObject("Box").transform;
        s.box.SetParent(go.transform, false);
        s.box.position = new Vector3(s.to + 1.3f, s.top, 1f);
        LevelBuilder.Visual("Body", PrimitiveType.Cube, s.box, new Vector3(0f, 1.1f, 0f), new Vector3(2.2f, 2.2f, 1.8f), body);
        var h = LevelBuilder.Visual("Hole", PrimitiveType.Cylinder, s.box, new Vector3(-1.12f, 1f, 0f), new Vector3(0.9f, 0.05f, 0.9f), hole, false).transform;
        h.localRotation = Quaternion.Euler(0f, 0f, 90f);
        s.blade = LevelBuilder.Visual("Blade", PrimitiveType.Cube, s.box, new Vector3(-1.14f, 1f, -0.05f), new Vector3(0.05f, 0.8f, 0.12f), steel, false).transform;
        // عيون فوق الفتحة (عشان تبان إنها جوعانة)
        for (int i = -1; i <= 1; i += 2)
        {
            LevelBuilder.Visual("Eye", PrimitiveType.Sphere, s.box, new Vector3(-1.12f, 1.75f, i * 0.35f), Vector3.one * 0.32f, white, false);
            LevelBuilder.Visual("Pupil", PrimitiveType.Sphere, s.box, new Vector3(-1.25f, 1.75f, i * 0.35f), Vector3.one * 0.14f, hole, false);
        }
        s.crank = new GameObject("Crank").transform;
        s.crank.SetParent(s.box, false);
        s.crank.localPosition = new Vector3(0.4f, 1.1f, -0.95f);
        LevelBuilder.Visual("Arm", PrimitiveType.Cube, s.crank, new Vector3(0f, 0.4f, 0f), new Vector3(0.12f, 0.8f, 0.08f), steel);
        LevelBuilder.Visual("Knob", PrimitiveType.Sphere, s.crank, new Vector3(0f, 0.8f, -0.12f), Vector3.one * 0.22f, gm.Mat("Tower Red", new Color(0.85f, 0.2f, 0.22f), 0.35f));
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time;
        RowaidaController r = gm.Rowaida;
        bool rowaidaOn = r != null && r.transform.position.x > from - 0.3f && r.transform.position.x < to + 0.3f;

        // تسحب الخط اللي فوق الحفرة (إلا لو رويدا عليه)
        if (eating == null && !IsBusy && !rowaidaOn)
            foreach (InkMark m in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
            {
                var mr = m.GetComponent<MeshRenderer>();
                if (mr == null) continue;
                Bounds b = mr.bounds;
                if (b.max.x > from + 0.3f && b.min.x < to - 0.3f && b.center.y > top - 2.5f && b.center.y < top + 2.5f)
                {
                    eating = m; eatT = 0f; eatStart = m.transform.position;
                    foreach (Collider c in m.GetComponentsInChildren<Collider>()) c.enabled = false;
                    var rb = m.GetComponent<Rigidbody>();
                    if (rb != null) rb.isKinematic = true;
                    LevelSfx.Play("grind", 0.9f);
                    if (!promptedEat) { promptedEat = true; gm.UI.ShowMessage("", 3.2f, "البراية أكلت خطك! وهي تبري ارسمي الجسر بسرعة"); }
                    break;
                }
            }
        if (eating != null)
        {
            eatT += Time.deltaTime;
            Vector3 mouth = box.position + new Vector3(-1.2f, 1f, -1f);
            Vector3 c0 = eating.GetComponent<MeshRenderer>().bounds.center - eating.transform.position;
            eating.transform.position = Vector3.Lerp(eatStart, mouth - c0 * 0.05f, Mathf.SmoothStep(0f, 1f, eatT / 0.7f));
            eating.transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.05f, eatT / 0.7f);
            if (eatT >= 0.7f)
            {
                Destroy(eating.gameObject);
                eating = null;
                busyUntil = t + Busy;
                LevelSfx.Play("grind", 0.8f, 0.9f);
            }
        }

        // وهي مشغولة: الذراع يلف، والبراية تهتز، ونشارة تطلع
        bool working = IsBusy || eating != null;
        crank.localRotation = Quaternion.Euler(0f, 0f, working ? -t * 400f : Mathf.Sin(t * 2f) * 10f);
        blade.localPosition = new Vector3(-1.14f, 1f + (working ? Mathf.Sin(t * 40f) * 0.15f : 0f), -0.05f);
        box.localScale = Vector3.one * (working ? 1f + Mathf.Sin(t * 30f) * 0.02f : 1f + Mathf.Sin(t * 3f) * 0.03f);
        if (working && Random.value < 0.4f)
        {
            var sh = LevelBuilder.Visual("Shaving", PrimitiveType.Cube, null, box.position + new Vector3(0.3f, 2.3f, 0f), new Vector3(0.25f, 0.04f, 0.18f), shaving, false);
            sh.transform.rotation = Random.rotation;
            var fx = sh.AddComponent<FxParticle>();
            fx.velocity = new Vector3(Random.Range(-1.5f, 1.5f), Random.Range(3f, 5f), Random.Range(-1f, 0f));
            fx.gravity = 9f; fx.spin = Random.insideUnitSphere * 400f; fx.life = 1f;
        }
        if (IsBusy && Mathf.Repeat(t, 1.2f) < Time.deltaTime) LevelSfx.Play("grind", 0.5f);

        // رويدا تستنى إلا لو فيه جسر والبراية مشغولة (عندها وقت تعدي)
        float cross = (to - from + 1.6f) / (r != null ? r.walkSpeed : 2.8f);
        bool safe = RowaidaController.BridgeCovers(from, to, top) && (busyUntil - t > cross || rowaidaOn);
        bool wait = HoldHelper.Before(from) && !safe;
        if (wait && !prompted && !IsBusy) { prompted = true; gm.UI.ShowMessage("", 3.2f, "البراية جوعانة! ارسمي خط تاكله، وهي تبري ارسمي الجسر"); }
        HoldHelper.Hold(ref holding, wait);
    }
}

// صمغ على الطريق: يلزق رويدا. ارسمي خط فوقه تمشي عليه. الصمغ يبلع الخط بعد شوي
public class GluePuddle : MonoBehaviour
{
    float from, to, top;
    bool holding, prompted;
    readonly List<Transform> bubbles = new List<Transform>();
    readonly Dictionary<InkMark, float> sinking = new Dictionary<InkMark, float>();
    const float Swallow = 6f;

    public static void Create(Transform root, Vector3 spec, GameManager gm)
    {
        var go = new GameObject("Glue");
        go.transform.SetParent(root, false);
        var g = go.AddComponent<GluePuddle>();
        g.from = spec.x; g.to = spec.y; g.top = spec.z;
        Material glue = gm.Mat("Glue", new Color(0.92f, 0.95f, 1f), 0.95f);
        Material bottle = gm.Mat("Glue Bottle", new Color(0.95f, 0.95f, 0.92f), 0.5f);
        Material cap = gm.Mat("Glue Cap", new Color(1f, 0.5f, 0.2f), 0.5f);
        float w = g.to - g.from, cx = (g.from + g.to) * 0.5f;
        LevelBuilder.Visual("Puddle", PrimitiveType.Cylinder, go.transform, new Vector3(cx, g.top + 0.02f, 0f), new Vector3(w, 0.04f, 2.6f), glue, false);
        for (int i = 0; i < 5; i++)
            LevelBuilder.Visual("Blob", PrimitiveType.Sphere, go.transform, new Vector3(g.from + w * (0.15f + i * 0.17f), g.top + 0.05f, -0.8f + i * 0.4f), new Vector3(0.6f, 0.12f, 0.5f), glue, false);
        for (int i = 0; i < 4; i++)
            g.bubbles.Add(LevelBuilder.Visual("Bubble", PrimitiveType.Sphere, go.transform, new Vector3(g.from + w * (0.2f + i * 0.2f), g.top, -0.4f + i * 0.25f), Vector3.one * 0.1f, glue, false).transform);
        // علبة صمغ مقلوبة ورا (هي اللي كبت)
        var b = LevelBuilder.Visual("Glue Bottle", PrimitiveType.Cylinder, go.transform, new Vector3(g.to + 0.6f, g.top + 0.5f, 1.3f), new Vector3(0.9f, 0.6f, 0.9f), bottle).transform;
        b.localRotation = Quaternion.Euler(0f, 0f, 70f);
        LevelBuilder.Visual("Cap", PrimitiveType.Cylinder, b, new Vector3(0f, -1.15f, 0f), new Vector3(0.5f, 0.3f, 0.5f), cap);
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time;
        for (int i = 0; i < bubbles.Count; i++)
        {
            float k = Mathf.Repeat(t * 0.6f + i * 0.37f, 1f);
            bubbles[i].localScale = Vector3.one * (0.05f + k * 0.3f);
            bubbles[i].gameObject.SetActive(k < 0.95f);
        }

        // الخطوط اللي على الصمغ تغرق بعد شوي
        foreach (InkMark m in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
        {
            if (m.closed || sinking.ContainsKey(m)) continue;
            var r = m.GetComponent<MeshRenderer>();
            if (r == null) continue;
            Bounds b = r.bounds;
            if (b.max.x > from && b.min.x < to && b.min.y < top + 0.6f) sinking[m] = t;
        }
        var gone = new List<InkMark>();
        RowaidaController rw = gm.Rowaida;
        bool onIt = rw != null && rw.transform.position.x > from - 0.5f && rw.transform.position.x < to + 0.5f;
        foreach (var kv in sinking)
        {
            if (kv.Key == null) { gone.Add(kv.Key); continue; }
            if (t - kv.Value > Swallow && !onIt)
            {
                LevelSfx.Play("glue", 0.6f, 0.8f);
                Destroy(kv.Key.gameObject);
                gone.Add(kv.Key);
            }
        }
        foreach (var k in gone) sinking.Remove(k);

        bool covered = RowaidaController.BridgeCovers(from, to, top);
        bool wait = HoldHelper.Before(from) && !covered;
        if (wait && !prompted)
        {
            prompted = true;
            LevelSfx.Play("glue", 0.7f);
            gm.UI.ShowMessage("", 3f, "صمغ! بتلزقين فيه... ارسمي خط فوقه تمشين عليه");
        }
        HoldHelper.Hold(ref holding, wait);
    }
}
