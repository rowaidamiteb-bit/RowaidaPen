using System.Collections.Generic;
using UnityEngine;

// ---------- بوابة الرقم: ارسمي بالمربع نفس عدد الخطوط اللي على البوابة ----------
public class TallyGate : MonoBehaviour
{
    float gateX, top, openT = -1f;
    int n, count;
    Rect box;
    readonly List<InkMark> marks = new List<InkMark>();
    readonly HashSet<InkMark> seen = new HashSet<InkMark>();
    Transform gate;
    BoxCollider col;
    TextMesh countT;
    bool holding, prompted;

    public static void Create(Transform root, Vector4 s, GameManager gm)
    {
        var go = new GameObject("Tally Gate");
        go.transform.SetParent(root, false);
        var g = go.AddComponent<TallyGate>();
        g.gateX = s.x; g.top = s.y; g.n = (int)s.z;
        Vector3 bc = new Vector3(s.w, g.top + 3.7f, 0.4f);
        g.box = new Rect(bc.x - 1.7f, bc.y - 0.9f, 3.4f, 1.8f);
        Color frame = new Color(0.95f, 0.45f, 0.5f);
        // المربع اللي ترسمين فيه
        GameFeatures.Px("Box Top", go.transform, bc + new Vector3(0f, 0.95f, 0f), new Vector3(3.5f, 0.1f, 0.1f), frame);
        GameFeatures.Px("Box Bottom", go.transform, bc + new Vector3(0f, -0.95f, 0f), new Vector3(3.5f, 0.1f, 0.1f), frame);
        GameFeatures.Px("Box Left", go.transform, bc + new Vector3(-1.75f, 0f, 0f), new Vector3(0.1f, 2f, 0.1f), frame);
        GameFeatures.Px("Box Right", go.transform, bc + new Vector3(1.75f, 0f, 0f), new Vector3(0.1f, 2f, 0.1f), frame);
        LevelBuilder.Visual("Box Paper", PrimitiveType.Cube, go.transform, bc + new Vector3(0f, 0f, 0.15f), new Vector3(3.4f, 1.8f, 0.05f), gm.Mat("Badge White", Color.white, 0.4f), false);
        GameFeatures.Text(go.transform, bc + new Vector3(0f, 1.35f, -0.2f), "لنرسم " + g.n + " خطوط هنا", 0.055f, new Color(0.85f, 0.3f, 0.4f));
        g.countT = GameFeatures.Text(go.transform, bc + new Vector3(2.3f, 0f, -0.2f), "0", 0.1f, new Color(0.3f, 0.45f, 0.85f));
        // البوابة ورقمها الكبير
        g.gate = new GameObject("Number Gate").transform;
        g.gate.SetParent(go.transform, false);
        g.gate.position = new Vector3(g.gateX, g.top, 0f);
        GameFeatures.Px("Gate", g.gate, new Vector3(0f, 1.7f, 0f), new Vector3(0.6f, 3.4f, 2f), new Color(0.35f, 0.55f, 0.95f), true);
        GameFeatures.Px("Gate Plate", g.gate, new Vector3(0f, 2.2f, -1.02f), new Vector3(1.1f, 1.3f, 0.05f), new Color(1f, 0.95f, 0.7f));
        GameFeatures.Text(g.gate, g.gate.position + new Vector3(0f, 2.2f, -1.08f), g.n.ToString(), 0.2f, new Color(0.9f, 0.3f, 0.35f));
        g.col = g.gate.gameObject.AddComponent<BoxCollider>();
        g.col.center = new Vector3(0f, 1.7f, 0f);
        g.col.size = new Vector3(0.6f, 3.4f, 2f);
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float dt = Time.deltaTime;
        if (openT < 0f)
        {
            foreach (InkMark m in GameFeatures.Fresh(seen))
            {
                if (m.closed) continue;
                Vector3 c = GameFeatures.Center(m);
                if (!box.Contains(new Vector2(c.x, c.y))) continue;
                foreach (Collider cc in m.GetComponentsInChildren<Collider>()) cc.enabled = false;
                marks.Add(m);
                count++;
                GameFeatures.SetText(countT, count.ToString());
                LevelSfx.Play("count", 0.6f, 0.8f + count * 0.12f);
                if (count == n)
                {
                    openT = 0f;
                    LevelSfx.Play("powerup", 0.7f);
                    gm.UI.ShowMessage("", 2f, "صحيح! " + n + " بالضبط");
                }
                else if (count > n)
                {
                    LevelSfx.Play("pxbuzz", 0.7f);
                    foreach (InkMark k in marks) if (k != null) { Fx.Sparkle(GameFeatures.Center(k), new Color(1f, 0.5f, 0.5f), 6); Destroy(k.gameObject); }
                    marks.Clear();
                    count = 0;
                    GameFeatures.SetText(countT, "0");
                    gm.UI.ShowMessage("", 2.4f, "أكثر من اللازم! المطلوب " + n + " بالضبط، لنحاول من جديد");
                }
            }
        }
        else if (openT < 1f)
        {
            openT = Mathf.Min(1f, openT + dt / 0.8f);
            gate.position = new Vector3(gateX, top - openT * 3.6f, 0f);
            if (openT >= 1f) col.enabled = false;
        }
        bool wait = openT < 1f && HoldHelper.Before(gateX - 0.4f);
        if (wait && !prompted) { prompted = true; gm.UI.ShowMessage("", 3.6f, "بوابة الرقم " + n + "! لنرسم داخل المربع " + n + " خطوط بالضبط"); }
        HoldHelper.Hold(ref holding, wait);
        if (openT < 0f) LabFeatures.Show(box.xMin - 4f, gateX, new Vector3(box.xMin - 0.5f, top - 0.5f, 0f), new Vector3(gateX + 1f, box.yMax + 1f, 0f));
    }
}

// ---------- ميزان الأرقام: الكفة اليسار فيها رقم، حطي بالكفة اليمين نفس العدد دواير، وينزل الجسر ----------
public class NumberScale : MonoBehaviour
{
    float sx, top, gapFrom, gapTo, baseY, offset, stableT, bridgeT = -1f;
    int need, count;
    Transform beam, panL, panR, pivot, chainL, chainR;
    Rigidbody panRb;
    BoxCollider bridgeCol;
    TextMesh countT;
    bool holding, prompted;

    public static void Create(Transform root, Vector4 s, GameManager gm)
    {
        var go = new GameObject("Number Scale");
        go.transform.SetParent(root, false);
        var n = go.AddComponent<NumberScale>();
        n.sx = s.x; n.top = s.y; n.need = (int)s.z; n.gapFrom = s.w; n.gapTo = n.gapFrom + 4f;
        n.baseY = n.top + 2.7f;
        Color brass = new Color(0.9f, 0.7f, 0.3f), wood = new Color(0.65f, 0.45f, 0.3f);
        GameFeatures.Px("Pole", go.transform, new Vector3(n.sx, n.top + 2.7f, 1.2f), new Vector3(0.25f, 5.4f, 0.25f), brass, true);
        GameFeatures.Px("Base", go.transform, new Vector3(n.sx, n.top + 0.1f, 1.2f), new Vector3(1.2f, 0.2f, 0.8f), wood, true);
        n.beam = GameFeatures.Px("Beam", go.transform, new Vector3(n.sx, n.top + 5.3f, 0.6f), new Vector3(3.4f, 0.16f, 0.16f), brass, true).transform;
        GameFeatures.Px("Pointer", go.transform, new Vector3(n.sx, n.top + 5.7f, 0.5f), new Vector3(0.08f, 0.6f, 0.08f), new Color(0.9f, 0.3f, 0.35f));
        n.chainL = GameFeatures.Px("Chain", go.transform, Vector3.zero, Vector3.one, new Color(0.5f, 0.5f, 0.55f)).transform;
        n.chainR = GameFeatures.Px("Chain", go.transform, Vector3.zero, Vector3.one, new Color(0.5f, 0.5f, 0.55f)).transform;
        // الكفة اليسار: فيها صندوق عليه الرقم
        n.panL = new GameObject("Pan Left").transform;
        n.panL.SetParent(go.transform, false);
        GameFeatures.Px("Pan", n.panL, Vector3.zero, new Vector3(1.8f, 0.12f, 1.4f), brass, true);
        GameFeatures.Px("Weight", n.panL, new Vector3(0f, 0.5f, 0f), new Vector3(0.9f, 0.9f, 0.9f), new Color(0.35f, 0.4f, 0.55f), true);
        var wl = GameFeatures.Text(n.panL, Vector3.zero, n.need.ToString(), 0.12f, Color.white);
        wl.transform.localPosition = new Vector3(0f, 0.5f, -0.47f);
        // الكفة اليمين: سطل تطيح فيه الدواير
        var pr = new GameObject("Pan Right");
        pr.transform.SetParent(go.transform, false);
        var parts = new[] { (new Vector3(0f, 0f, 0f), new Vector3(1.9f, 0.12f, 1.4f)), (new Vector3(-0.92f, 0.6f, 0f), new Vector3(0.08f, 1.2f, 1.4f)), (new Vector3(0.92f, 0.6f, 0f), new Vector3(0.08f, 1.2f, 1.4f)) };
        foreach (var p in parts) GameFeatures.Px("Pan Part", pr.transform, p.Item1, p.Item2, brass, true).AddComponent<BoxCollider>();
        n.panRb = pr.AddComponent<Rigidbody>();
        n.panRb.isKinematic = true;
        n.panRb.interpolation = RigidbodyInterpolation.Interpolate;
        n.panR = pr.transform;
        n.countT = GameFeatures.Text(go.transform, Vector3.zero, "0", 0.09f, new Color(0.3f, 0.45f, 0.85f));
        // الجسر المتحرك (واقف لين يتوازن الميزان)
        n.pivot = new GameObject("Drawbridge").transform;
        n.pivot.SetParent(go.transform, false);
        n.pivot.position = new Vector3(n.gapFrom, n.top - 0.15f, 0f);
        var plank = GameFeatures.Px("Plank", n.pivot, new Vector3(2.05f, 0f, 0f), new Vector3(4.1f, 0.3f, 1.8f), wood, true);
        for (int k = 0; k < 5; k++) GameFeatures.Px("Plank Line", n.pivot, new Vector3(0.4f + k * 0.8f, 0.16f, 0f), new Vector3(0.05f, 0.02f, 1.8f), wood * 0.7f);
        n.bridgeCol = plank.AddComponent<BoxCollider>();
        n.bridgeCol.enabled = false;
        n.pivot.localRotation = Quaternion.Euler(0f, 0f, 80f);
        n.Place();
    }

    void Place()
    {
        float ang = Mathf.Atan2(offset, 1.55f) * Mathf.Rad2Deg;
        beam.localRotation = Quaternion.Euler(0f, 0f, -ang);
        float ly = baseY - offset, ry = baseY + offset;
        panL.position = new Vector3(sx - 1.55f, ly, 0f);
        panRb.MovePosition(new Vector3(sx + 1.55f, ry, 0f));
        Vector3 bl = beam.position + beam.right * -1.55f, brr = beam.position + beam.right * 1.55f;
        LabFeatures2.Stretch(chainL, bl, new Vector3(sx - 1.55f, ly + 0.06f, 0.3f), 0.05f);
        LabFeatures2.Stretch(chainR, brr, new Vector3(sx + 1.55f, ry + 0.06f, 0.3f), 0.05f);
        countT.transform.position = new Vector3(sx + 1.55f, ry - 0.45f, -0.75f);
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float dt = Time.deltaTime;
        // كم دايرة بالكفة اليمين
        var inPan = new List<InkMark>();
        foreach (InkMark m in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
        {
            if (!m.closed || m.beingEaten) continue;
            Vector3 c = GameFeatures.Center(m);
            if (Mathf.Abs(c.x - panR.position.x) < 1f && c.y > panR.position.y - 0.2f && c.y < panR.position.y + 2.4f) inPan.Add(m);
        }
        if (bridgeT < 0f && inPan.Count > need)
        {
            // زيادة: الأعلى يطير برا
            InkMark extra = inPan[0];
            foreach (InkMark m in inPan) if (GameFeatures.Center(m).y > GameFeatures.Center(extra).y) extra = m;
            Fx.Sparkle(GameFeatures.Center(extra), new Color(1f, 0.5f, 0.5f), 10);
            Destroy(extra.gameObject);
            inPan.Remove(extra);
            LevelSfx.Play("pxbuzz", 0.6f);
            gm.UI.ShowMessage("", 2.2f, "أكثر من اللازم! المطلوب " + need + " بالضبط");
        }
        if (inPan.Count != count)
        {
            count = inPan.Count;
            GameFeatures.SetText(countT, count.ToString());
            LevelSfx.Play("count", 0.6f, 0.8f + count * 0.15f);
        }
        float want = Mathf.Clamp01((need - count) / (float)need) * 0.7f;
        offset = Mathf.MoveTowards(offset, want, dt * 1.2f);
        Place();
        if (bridgeT < 0f)
        {
            stableT = count == need && offset < 0.02f ? stableT + dt : 0f;
            if (stableT > 0.5f) { bridgeT = 0f; LevelSfx.Play("powerup", 0.7f); LevelSfx.Play("rope", 0.6f); gm.UI.ShowMessage("", 2f, "توازن الميزان! نزل الجسر"); }
        }
        else if (bridgeT < 1f)
        {
            bridgeT = Mathf.Min(1f, bridgeT + dt / 1.2f);
            pivot.localRotation = Quaternion.Euler(0f, 0f, 80f * (1f - Mathf.SmoothStep(0f, 1f, bridgeT)));
            if (bridgeT >= 1f) { bridgeCol.enabled = true; LevelSfx.Play("thud", 0.7f); }
        }
        bool wait = bridgeT < 1f && HoldHelper.Before(gapFrom);
        if (wait && !prompted) { prompted = true; gm.UI.ShowMessage("", 3.8f, "ميزان الأرقام! لنضع في الكفة اليمنى " + need + " دوائر ليتوازن وينزل الجسر"); }
        HoldHelper.Hold(ref holding, wait);
        if (bridgeT < 1f) LabFeatures.Show(sx - 5f, gapFrom, new Vector3(sx - 2.8f, top - 0.5f, 0f), new Vector3(gapTo + 0.8f, top + 6.2f, 0f));
    }
}

// ---------- آلة حاسبة: حوطي الجواب الصح بدايرة، وتطبع ورقة طويلة تصير درج ----------
public class CalcRamp : MonoBehaviour
{
    float wallX, top, printT = -1f, nextTick;
    int ans;
    string expr;
    readonly List<Vector3> keys = new List<Vector3>();
    readonly List<int> vals = new List<int>();
    Transform ramp;
    BoxCollider rampCol;
    TextMesh screen;
    readonly HashSet<InkMark> seen = new HashSet<InkMark>();
    bool holding, prompted;
    float rampFrom => wallX - 4.5f;

    public static void Create(Transform root, Vector4 s, GameManager gm)
    {
        var go = new GameObject("Calculator");
        go.transform.SetParent(root, false);
        var c = go.AddComponent<CalcRamp>();
        c.wallX = s.x; c.top = s.y;
        int a = (int)s.z, b = (int)s.w;
        c.ans = a + b;
        c.expr = a + " + " + b + " = ";
        Vector3 body = new Vector3(c.wallX - 6.4f, c.top + 4.4f, 1.1f);
        GameFeatures.Px("Calc Body", go.transform, body, new Vector3(4.8f, 4.2f, 0.6f), new Color(0.3f, 0.32f, 0.4f), true);
        LevelBuilder.Visual("Calc Screen", PrimitiveType.Cube, go.transform, body + new Vector3(0f, 1.3f, -0.32f), new Vector3(4f, 1f, 0.05f), gm.Mat("Calc LCD", new Color(0.6f, 0.75f, 0.55f), 0.6f), false);
        c.screen = GameFeatures.Text(go.transform, body + new Vector3(0f, 1.3f, -0.38f), a + " + " + b + " = ?", 0.09f, new Color(0.15f, 0.2f, 0.12f));
        // أزرار صغيرة للزينة
        for (int row = 0; row < 2; row++)
            for (int k = 0; k < 5; k++)
                GameFeatures.Px("Small Key", go.transform, body + new Vector3(-1.8f + k * 0.9f, 0.35f - row * 0.5f, -0.33f), new Vector3(0.7f, 0.35f, 0.08f), new Color(0.75f, 0.77f, 0.85f));
        // أزرار الأجوبة الكبيرة (تحوطينها بدايرة)
        int[] opts = { c.ans + 1, c.ans, c.ans - 1 };
        Color[] kc = { new Color(1f, 0.55f, 0.35f), new Color(0.45f, 0.75f, 1f), new Color(0.6f, 0.85f, 0.4f) };
        for (int k = 0; k < 3; k++)
        {
            Vector3 kp = new Vector3(body.x - 1.4f + k * 1.4f, c.top + 3.1f, 0.3f);
            GameFeatures.Px("Answer Key", go.transform, kp, new Vector3(1f, 0.85f, 0.4f), kc[k], true);
            GameFeatures.Text(go.transform, kp + new Vector3(0f, 0f, -0.24f), opts[k].ToString(), 0.1f, Color.white);
            c.keys.Add(new Vector3(kp.x, kp.y, 0f));
            c.vals.Add(opts[k]);
        }
        GameFeatures.Px("Printer Slot", go.transform, new Vector3(c.wallX - 4.3f, c.top + 2.35f, 0.9f), new Vector3(0.8f, 0.15f, 1.4f), new Color(0.15f, 0.15f, 0.18f));
        // ورقة الطباعة (تصير درج)
        c.ramp = new GameObject("Receipt Ramp").transform;
        c.ramp.SetParent(go.transform, false);
        Vector3 r0 = new Vector3(c.rampFrom, c.top - 0.04f, 0f), r1 = new Vector3(c.wallX + 0.05f, c.top + 3f, 0f);
        c.ramp.position = r0;
        c.ramp.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(r1.y - r0.y, r1.x - r0.x) * Mathf.Rad2Deg);
        float len = Vector3.Distance(r0, r1);
        var paper = LevelBuilder.Visual("Receipt", PrimitiveType.Cube, c.ramp, new Vector3(len * 0.5f, 0f, 0f), new Vector3(len, 0.1f, 1.6f), gm.Mat("Receipt", new Color(0.98f, 0.97f, 0.92f), 0.2f));
        for (int k = 1; k < 8; k++) LevelBuilder.Visual("Receipt Line", PrimitiveType.Cube, c.ramp, new Vector3(len * k / 8f, 0.06f, 0f), new Vector3(0.03f, 0.01f, 1.2f), gm.Mat("Receipt Ink", new Color(0.4f, 0.4f, 0.5f), 0.2f), false);
        c.rampCol = paper.AddComponent<BoxCollider>();
        c.rampCol.enabled = false;
        c.ramp.localScale = new Vector3(0.001f, 1f, 1f);
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time, dt = Time.deltaTime;
        if (printT < 0f)
        {
            foreach (InkMark m in GameFeatures.Fresh(seen))
            {
                if (!m.closed) continue;
                Vector3 c = GameFeatures.Center(m);
                int best = -1;
                for (int k = 0; k < keys.Count; k++) if (Vector2.Distance(c, keys[k]) < 1.1f && (best < 0 || Vector2.Distance(c, keys[k]) < Vector2.Distance(c, keys[best]))) best = k;
                if (best < 0) continue;
                Fx.Sparkle(c, Color.white, 10);
                Destroy(m.gameObject);
                if (vals[best] == ans)
                {
                    printT = 0f;
                    GameFeatures.SetText(screen, expr + ans);
                    LevelSfx.Play("powerup", 0.7f);
                    gm.UI.ShowMessage("", 2f, "صحيح! الآلة تطبع الآن");
                }
                else
                {
                    LevelSfx.Play("pxbuzz", 0.8f);
                    gm.UI.ShowMessage("", 2.2f, "خطأ! لنجرب جوابًا آخر");
                }
                break;
            }
        }
        else if (printT < 1f)
        {
            printT = Mathf.Min(1f, printT + dt / 1.6f);
            ramp.localScale = new Vector3(Mathf.Max(0.001f, printT), 1f, 1f);
            if (t > nextTick) { nextTick = t + 0.12f; LevelSfx.Play("type", 0.4f, 1.3f); }
            if (printT >= 1f) { rampCol.enabled = true; LevelSfx.Play("chime", 0.6f, 1.3f); }
        }
        bool wait = printT < 1f && HoldHelper.Before(rampFrom);
        if (wait && !prompted) { prompted = true; gm.UI.ShowMessage("", 3.8f, "آلة حاسبة! لنحِط الجواب الصحيح بدائرة فتطبع درجًا"); }
        HoldHelper.Hold(ref holding, wait);
        if (printT < 1f) LabFeatures.Show(wallX - 11f, rampFrom, new Vector3(wallX - 9.2f, top - 0.5f, 0f), new Vector3(wallX + 1f, top + 6.8f, 0f));
    }
}

// ---------- الزعيم الأخير: فيروس عملاق يلحق رويدا ويخرب الخطوط. الأشكال المسكرة جدران حماية توقفه ----------
public class VirusBoss : MonoBehaviour
{
    const float Radius = 2.2f;
    float endX, speed, delay = 3f, stuckT, nextGlitch, deadT = -1f, gulpT;
    bool told;
    InkMark blocking;
    Transform body, eyeL, eyeR;
    readonly List<Transform> spikes = new List<Transform>();
    readonly List<Transform> orbit = new List<Transform>();
    float Front => transform.position.x + Radius;

    public static void Create(Transform root, Vector4 s, GameManager gm)
    {
        var go = new GameObject("Virus Boss");
        go.transform.SetParent(root, false);
        go.transform.position = new Vector3(s.x, Radius, 0f);
        var v = go.AddComponent<VirusBoss>();
        v.endX = s.y; v.speed = s.z;
        Material skin = gm.Mat("Virus Skin", new Color(0.75f, 0.15f, 0.55f), 0.5f);
        Material glow = gm.Mat("Virus Glow", new Color(2.2f, 0.3f, 1.2f), 0.5f);
        v.body = LevelBuilder.Visual("Virus", PrimitiveType.Sphere, go.transform, Vector3.zero, Vector3.one * Radius * 2f, skin, true).transform;
        for (int k = 0; k < 16; k++)
        {
            float a = k / 16f * Mathf.PI * 2f;
            Vector3 d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), (k % 3 - 1) * 0.4f).normalized;
            var sp = new GameObject("Spike").transform;
            sp.SetParent(go.transform, false);
            sp.localPosition = d * Radius;
            sp.up = d;
            LevelBuilder.Visual("Spike Rod", PrimitiveType.Cylinder, sp, new Vector3(0f, 0.3f, 0f), new Vector3(0.18f, 0.3f, 0.18f), skin, false);
            LevelBuilder.Visual("Spike Ball", PrimitiveType.Sphere, sp, new Vector3(0f, 0.65f, 0f), Vector3.one * 0.38f, glow, false);
            v.spikes.Add(sp);
        }
        // وجه غاضب
        for (int e = -1; e <= 1; e += 2)
        {
            LevelBuilder.Visual("Eye White", PrimitiveType.Sphere, go.transform, new Vector3(0.75f, 0.5f, e * 0.55f - 1.6f), new Vector3(0.7f, 0.8f, 0.3f), gm.Mat("Badge White", Color.white, 0.4f), false);
            var pupil = LevelBuilder.Visual("Pupil", PrimitiveType.Sphere, go.transform, new Vector3(0.85f, 0.45f, e * 0.55f - 1.75f), new Vector3(0.3f, 0.35f, 0.15f), gm.Mat("Lamp Red", new Color(1.6f, 0.2f, 0.2f), 0.5f), false).transform;
            if (e < 0) v.eyeL = pupil; else v.eyeR = pupil;
            LevelBuilder.Visual("Brow", PrimitiveType.Cube, go.transform, new Vector3(0.8f, 1f, e * 0.55f - 1.65f), new Vector3(0.7f, 0.12f, 0.1f), gm.Mat("Monster Pupil", new Color(0.05f, 0.04f, 0.06f), 0.9f), false).transform.localRotation = Quaternion.Euler(0f, 0f, e * 20f);
        }
        for (int k = 0; k < 5; k++) LevelBuilder.Visual("Tooth", PrimitiveType.Cube, go.transform, new Vector3(0.9f - k * 0.05f, -0.5f, -1.3f - k * 0.18f + 0.35f), new Vector3(0.18f, k % 2 == 0 ? 0.3f : 0.2f, 0.12f), gm.Mat("Badge White", Color.white, 0.4f), false).transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        for (int k = 0; k < 10; k++) v.orbit.Add(GameFeatures.Px("Glitch Bit", go.transform, Vector3.zero, Vector3.one * 0.3f, k % 2 == 0 ? new Color(1f, 0.2f, 0.5f) : new Color(0.3f, 0.9f, 1f)).transform);
        gm.Respawned += v.OnRespawned;
    }

    void OnDestroy() { if (GameManager.Instance != null) GameManager.Instance.Respawned -= OnRespawned; }

    void OnRespawned(Vector3 pos)
    {
        if (deadT >= 0f) return;
        transform.position = new Vector3(pos.x - 13f, Radius, 0f);
        delay = 2.5f;
        blocking = null;
    }

    public void Defeat()
    {
        if (deadT >= 0f) return;
        deadT = 0f;
        LevelSfx.Play("glitch", 1f, 0.7f);
        LevelSfx.Play("rumble", 0.8f);
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time, dt = Time.deltaTime;
        // يتنفس ويلف
        gulpT = Mathf.Max(0f, gulpT - dt);
        body.localScale = Vector3.one * Radius * 2f * (1f + Mathf.Sin(t * 5f) * 0.04f + Mathf.Sin(gulpT * Mathf.PI * 2f) * 0.15f);
        for (int i = 0; i < spikes.Count; i++) spikes[i].localScale = Vector3.one * (1f + Mathf.Sin(t * 6f + i) * 0.15f);
        for (int i = 0; i < orbit.Count; i++)
        {
            float a = t * 2f + i * 0.63f;
            orbit[i].localPosition = new Vector3(Mathf.Cos(a) * 3f, Mathf.Sin(a * 1.3f) * 2.6f, Mathf.Sin(a) * 1.5f);
            if (Random.value < 0.05f) orbit[i].localPosition += Random.insideUnitSphere * 0.6f;
        }
        if (deadT >= 0f)
        {
            // ينمسح: يتفتت بكسل
            deadT += dt;
            transform.localScale = Vector3.one * Mathf.Max(0f, 1f - deadT * 0.8f);
            if (Random.value < 0.6f) GameFeatures.Burst(transform.position + Random.insideUnitSphere * Radius, Random.value < 0.5f ? new Color(1f, 0.2f, 0.5f) : new Color(0.3f, 0.9f, 1f), 3, 0.25f, 6f);
            if (deadT > 1.3f) gameObject.SetActive(false);
            return;
        }
        if (!gm.IsPlaying) return;
        if (delay > 0f) { delay -= dt; return; }
        RowaidaController r = gm.Rowaida;
        float gap = r != null ? r.transform.position.x - Front : 99f;
        if (r != null)
        {
            eyeL.localPosition = new Vector3(0.85f, 0.45f + Mathf.Clamp(r.transform.position.y - transform.position.y, -1f, 1f) * 0.1f, -2.3f);
            eyeR.localPosition = new Vector3(0.85f, eyeL.localPosition.y, -1.2f);
        }
        // جدار حماية (شكل مسكر) قدامه؟ يوقف لين يحترق
        if (blocking == null && stuckT <= 0f)
        {
            foreach (InkMark m in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
            {
                if (!m.closed || m.beingEaten) continue;
                var mr = m.GetComponent<MeshRenderer>();
                if (mr == null || mr.bounds.min.x > Front + 0.1f || mr.bounds.max.x < Front - 1.5f || mr.bounds.min.y > transform.position.y + Radius) continue;
                blocking = m;
                m.beingEaten = true;
                stuckT = 2.6f;
                LevelSfx.Play("sizzle", 0.8f);
                if (!told) { told = true; gm.UI.ShowMessage("", 2.4f, "جدار حماية! توقف الفيروس"); }
                break;
            }
        }
        if (blocking != null)
        {
            stuckT -= dt;
            Vector3 bc = GameFeatures.Center(blocking);
            if (Random.value < 0.5f) Fx.Sparkle(bc + Random.insideUnitSphere * 0.4f, new Color(1f, 0.5f, 0.2f), 2);
            transform.position += new Vector3(Mathf.Sin(t * 40f) * 0.01f, 0f, 0f);
            if (stuckT <= 0f)
            {
                SmokeCloud.Puff(bc, 0.9f);
                Destroy(blocking.gameObject);
                blocking = null;
                LevelSfx.Play("glitch", 0.6f);
            }
        }
        else
        {
            float s = speed;
            // لو رويدا واقفة تستنى، الفيروس يقرب منها شوي شوي... ولو تأخرتي يبلعها
            if (r != null && r.Held) s = gap < 6f ? 2f : 4.5f;
            else if (gap > 8f) s = 4.5f;
            if (Front < endX) transform.position += new Vector3(s * dt, 0f, 0f);
        }
        // يخرب الخطوط اللي يلمسها
        foreach (InkMark m in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
        {
            if (m.closed || m == blocking) continue;
            var mr = m.GetComponent<MeshRenderer>();
            if (mr != null && mr.bounds.min.x < Front - 0.4f) { GameFeatures.Burst(mr.bounds.center, new Color(1f, 0.2f, 0.5f), 6); Destroy(m.gameObject); }
        }
        if (r == null) return;
        if (gap < 9f) CameraFollow.ShiftRequest = -Mathf.Lerp(3.5f, 1f, gap / 9f);
        if (t > nextGlitch && gap < 10f) { nextGlitch = t + 1.4f; LevelSfx.Play("glitch", Mathf.Lerp(0.6f, 0.15f, gap / 10f)); }
        if (gap < 0.2f && r.transform.position.y > -3f)
        {
            // يبلعها: ينتفخ ويسوي قلب
            GameFeatures.Burst(r.transform.position, new Color(1f, 0.2f, 0.5f), 12);
            LevelSfx.Play("gloop", 1f, 0.6f);
            gulpT = 0.5f;
            gm.KillRowaida();
        }
    }
}

// ---------- إعلان مزعج ينط قدام رويدا ويسد الطريق: خط على زر X يسكره ----------
public class PopupAd : MonoBehaviour
{
    float x, top, showK;
    Vector3 xBtn;
    Transform win;
    BoxCollider col;
    bool shown, closed, holding, prompted;

    public static void Create(Transform root, Vector2 s, GameManager gm)
    {
        var go = new GameObject("Popup Ad");
        go.transform.SetParent(root, false);
        var p = go.AddComponent<PopupAd>();
        p.x = s.x; p.top = s.y;
        p.xBtn = new Vector3(p.x + 1.05f, p.top + 3.05f, 0f);
        p.win = new GameObject("Window").transform;
        p.win.SetParent(go.transform, false);
        p.win.position = new Vector3(p.x, p.top, 0f);
        GameFeatures.Px("Window", p.win, new Vector3(0f, 1.6f, 0f), new Vector3(2.8f, 3.3f, 0.3f), new Color(0.97f, 0.97f, 1f), true);
        GameFeatures.Px("Title Bar", p.win, new Vector3(0f, 3.05f, -0.17f), new Vector3(2.8f, 0.4f, 0.05f), new Color(0.25f, 0.45f, 0.95f));
        GameFeatures.Px("X Button", p.win, new Vector3(1.05f, 3.05f, -0.21f), new Vector3(0.45f, 0.36f, 0.05f), new Color(0.95f, 0.25f, 0.3f));
        GameFeatures.Text(p.win, p.win.position + new Vector3(1.05f, 3.05f, -0.26f), "X", 0.06f, Color.white);
        GameFeatures.Text(p.win, p.win.position + new Vector3(-0.3f, 3.05f, -0.22f), "!!!", 0.05f, Color.white);
        GameFeatures.Text(p.win, p.win.position + new Vector3(0f, 2.2f, -0.2f), "مبروك!!", 0.085f, new Color(0.9f, 0.25f, 0.35f));
        GameFeatures.Text(p.win, p.win.position + new Vector3(0f, 1.55f, -0.2f), "جائزة كبرى", 0.06f, new Color(0.2f, 0.25f, 0.4f));
        GameFeatures.Px("Gift", p.win, new Vector3(0f, 0.8f, -0.2f), new Vector3(0.6f, 0.5f, 0.1f), new Color(1f, 0.75f, 0.2f));
        GameFeatures.Px("Gift Ribbon", p.win, new Vector3(0f, 0.8f, -0.26f), new Vector3(0.1f, 0.5f, 0.02f), new Color(0.9f, 0.25f, 0.35f));
        GameFeatures.Px("OK", p.win, new Vector3(0f, 0.25f, -0.2f), new Vector3(1f, 0.3f, 0.05f), new Color(0.35f, 0.8f, 0.4f));
        p.col = go.AddComponent<BoxCollider>();
        p.col.center = new Vector3(p.x, p.top + 1.6f, 0f);
        p.col.size = new Vector3(2.8f, 3.3f, 2f);
        p.col.enabled = false;
        p.win.localScale = Vector3.zero;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float dt = Time.deltaTime;
        RowaidaController r = gm.Rowaida;
        if (!shown && r != null && gm.IsPlaying && r.transform.position.x > x - 5f && r.transform.position.x < x)
        {
            shown = true;
            col.enabled = true;
            LevelSfx.Play("popup", 0.8f);
        }
        if (shown && !closed)
        {
            showK = Mathf.MoveTowards(showK, 1f, dt * 5f);
            win.localScale = Vector3.one * (showK < 1f ? Mathf.Sin(showK * Mathf.PI * 0.75f) * 1.15f : 1f);
            foreach (InkMark m in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
            {
                if (m.closed || m.path == null || GameFeatures.PathDistance(m.path, xBtn) > 0.45f) continue;
                closed = true;
                col.enabled = false;
                Destroy(m.gameObject);
                LevelSfx.Play("bugzap", 0.6f, 1.4f);
                gm.UI.ShowMessage("", 1.6f, "أُغلق!");
                break;
            }
        }
        if (closed)
        {
            showK = Mathf.MoveTowards(showK, 0f, dt * 4f);
            win.localScale = Vector3.one * showK;
        }
        bool wait = shown && !closed && HoldHelper.Before(x - 1.4f);
        if (wait && !prompted) { prompted = true; gm.UI.ShowMessage("", 3.6f, "إعلان مزعج سدّ الطريق! لنرسم خطًّا على زر X الأحمر لإغلاقه"); }
        HoldHelper.Hold(ref holding, wait);
        if (shown && !closed) LabFeatures.Show(x - 6f, x, new Vector3(x - 3.5f, top - 0.5f, 0f), new Vector3(x + 1.8f, top + 3.9f, 0f));
    }
}

// ---------- زر الحفظ: حوطي الديسك بدايرة، تنحفظ اللعبة والفيروس ينمسح ----------
public class SaveButton : MonoBehaviour
{
    float x, top, savedT = -1f, missTold;
    Vector3 disk;
    Transform fill, diskT;
    BoxCollider barrier;
    readonly List<Transform> barrierBits = new List<Transform>();
    readonly HashSet<InkMark> seen = new HashSet<InkMark>();
    TextMesh label;
    bool holding, prompted;

    public static void Create(Transform root, Vector2 s, GameManager gm)
    {
        var go = new GameObject("Save Button");
        go.transform.SetParent(root, false);
        var b = go.AddComponent<SaveButton>();
        b.x = s.x; b.top = s.y;
        b.disk = new Vector3(b.x, b.top + 3.3f, 0f);
        GameFeatures.Px("Pedestal", go.transform, new Vector3(b.x, b.top + 1.35f, 1.3f), new Vector3(0.6f, 2.7f, 0.6f), new Color(0.3f, 0.3f, 0.38f), true);
        // ديسك (فلوبي)
        b.diskT = new GameObject("Disk").transform;
        b.diskT.SetParent(go.transform, false);
        b.diskT.position = b.disk;
        GameFeatures.Px("Disk Body", b.diskT, Vector3.zero, new Vector3(1.1f, 1.1f, 0.15f), new Color(0.2f, 0.35f, 0.85f), true);
        GameFeatures.Px("Disk Shutter", b.diskT, new Vector3(0.05f, 0.33f, -0.09f), new Vector3(0.6f, 0.38f, 0.03f), new Color(0.8f, 0.82f, 0.88f));
        GameFeatures.Px("Disk Hole", b.diskT, new Vector3(0.18f, 0.33f, -0.11f), new Vector3(0.12f, 0.24f, 0.02f), new Color(0.2f, 0.35f, 0.85f));
        GameFeatures.Px("Disk Label", b.diskT, new Vector3(0f, -0.2f, -0.09f), new Vector3(0.8f, 0.5f, 0.03f), Color.white);
        b.label = GameFeatures.Text(go.transform, b.disk + new Vector3(0f, 1.2f, -0.2f), "حفظ", 0.07f, new Color(0.5f, 0.9f, 1f));
        // شريط التقدم
        GameFeatures.Px("Bar Frame", go.transform, b.disk + new Vector3(0f, -0.95f, -0.1f), new Vector3(1.6f, 0.24f, 0.05f), new Color(0.2f, 0.2f, 0.25f));
        b.fill = GameFeatures.Px("Bar Fill", go.transform, b.disk + new Vector3(-0.75f, -0.95f, -0.14f), new Vector3(0.001f, 0.16f, 0.05f), new Color(0.3f, 1f, 0.5f)).transform;
        // حاجز تشويش قدام الباب لين تحفظين
        var w = new GameObject("Glitch Barrier");
        w.transform.SetParent(go.transform, false);
        w.transform.position = new Vector3(b.x + 1.8f, b.top, 0f);
        for (int row = 0; row < 16; row++)
            for (int k = 0; k < 3; k++)
                b.barrierBits.Add(GameFeatures.Px("Barrier", w.transform, new Vector3(b.x + 1.6f + k * 0.2f, b.top + 0.1f + row * 0.2f, 0f), new Vector3(0.2f, 0.2f, 1.8f), (row + k) % 3 == 0 ? new Color(1f, 0.2f, 0.45f) : new Color(0.15f, 0.05f, 0.1f)).transform);
        b.barrier = w.AddComponent<BoxCollider>();
        b.barrier.center = new Vector3(0f, 1.6f, 0f);
        b.barrier.size = new Vector3(0.6f, 3.2f, 2f);
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time, dt = Time.deltaTime;
        diskT.localRotation = Quaternion.Euler(0f, Mathf.Sin(t * 2f) * 15f, 0f);
        if (savedT < 0f)
        {
            foreach (Transform bit in barrierBits) if (Random.value < 0.02f) bit.localScale = Random.value < 0.5f ? new Vector3(0.2f, 0.2f, 1.8f) : new Vector3(0.26f, 0.1f, 1.8f);
            foreach (InkMark m in GameFeatures.Fresh(seen))
            {
                if (!m.closed) continue;
                if (GameFeatures.Inside(m, disk))
                {
                    savedT = 0f;
                    Destroy(m.gameObject);
                    LevelSfx.Play("save", 0.9f);
                    var boss = FindFirstObjectByType<VirusBoss>();
                    if (boss != null) boss.Defeat();
                    gm.UI.ShowMessage("", 2.6f, "حُفظت اللعبة! ومُسح الفيروس");
                    break;
                }
                if (t - missTold > 4f && Vector3.Distance(GameFeatures.Center(m), disk) < 3f) { missTold = t; gm.UI.ShowMessage("", 2f, "لنحط القرص كله بالدائرة"); }
            }
        }
        else
        {
            savedT += dt;
            float k = Mathf.Clamp01(savedT / 1f);
            fill.localScale = new Vector3(1.5f * k, 0.16f, 0.05f);
            fill.position = disk + new Vector3(-0.75f + 0.75f * k, -0.95f, -0.14f);
            if (k >= 1f && barrier.enabled)
            {
                barrier.enabled = false;
                GameFeatures.SetText(label, "تم الحفظ");
                label.color = new Color(0.4f, 1f, 0.5f);
                foreach (Transform bit in barrierBits)
                {
                    bit.SetParent(null, true);
                    var fx = bit.gameObject.AddComponent<FxParticle>();
                    fx.velocity = new Vector3(Random.Range(-2f, 4f), Random.Range(2f, 6f), Random.Range(-2f, 2f));
                    fx.gravity = 8f; fx.life = 1f; fx.spin = Random.insideUnitSphere * 300f;
                }
                barrierBits.Clear();
                for (int i = 0; i < 30; i++) GameFeatures.Burst(disk + Random.insideUnitSphere * 2f, Color.HSVToRGB(Random.value, 0.7f, 1f), 1, 0.14f, 5f);
            }
        }
        bool wait = savedT < 1f && HoldHelper.Before(x + 1.2f);
        if (wait && !prompted) { prompted = true; gm.UI.ShowMessage("", 3.6f, "زر الحفظ! لنحط القرص بدائرة لحفظ اللعبة"); }
        HoldHelper.Hold(ref holding, wait);
        if (savedT < 0f) LabFeatures.Show(x - 6f, x + 1.6f, new Vector3(x - 2.5f, top - 0.5f, 0f), new Vector3(x + 3f, top + 5.2f, 0f));
    }
}

// ---------- منظر فصل الألعاب: 1 بكسل، 2 كود، 3 أرقام، 4 الزعيم ----------
public class GameScenery : MonoBehaviour
{
    int style;
    readonly List<Transform> bob = new List<Transform>();
    readonly List<Vector3> bobHome = new List<Vector3>();
    readonly List<TextMesh> rain = new List<TextMesh>();
    readonly List<Transform> spin = new List<Transform>();
    readonly List<Renderer> flicker = new List<Renderer>();
    float minX, maxX;

    public static void Create(Transform root, LevelData data, GameManager gm, System.Random rng)
    {
        var go = new GameObject("Game Scenery");
        go.transform.SetParent(root, false);
        var s = go.AddComponent<GameScenery>();
        s.style = data.gameStyle;
        float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        s.minX = float.MaxValue; s.maxX = float.MinValue;
        foreach (Block b in data.blocks) { s.minX = Mathf.Min(s.minX, b.center.x - b.size.x * 0.5f); s.maxX = Mathf.Max(s.maxX, b.center.x + b.size.x * 0.5f); }
        Transform p = go.transform;
        switch (s.style)
        {
            case 1: s.Pixel(p, data, gm, R); break;
            case 2: s.Code(p, gm, R); break;
            case 3: s.Numbers(p, gm, R); break;
            case 4: s.Boss(p, gm, R); break;
        }
    }

    void Bob(Transform t) { bob.Add(t); bobHome.Add(t.position); }

    // عالم البكسل: غيوم وتلال وأنابيب خضرا ومربعات "؟" وشجر وقلعة
    void Pixel(Transform p, LevelData data, GameManager gm, System.Func<float, float, float> R)
    {
        Color white = new Color(0.97f, 0.97f, 1f), green = new Color(0.3f, 0.75f, 0.3f), dgreen = new Color(0.15f, 0.5f, 0.2f), pipe = new Color(0.2f, 0.75f, 0.3f);
        for (float x = minX - 10f; x < maxX + 12f; x += R(6f, 9f))
        {
            // غيمة بكسل
            Vector3 c = new Vector3(x, R(7f, 10f), R(9f, 13f));
            var cloud = new GameObject("Pixel Cloud").transform;
            cloud.SetParent(p, false);
            cloud.position = c;
            int[] w = { 2, 4, 5 };
            for (int row = 0; row < 3; row++)
                for (int k = 0; k < w[row]; k++)
                    GameFeatures.Px("Cloud", cloud, new Vector3((k - (w[row] - 1) * 0.5f) * 0.6f, -row * 0.6f + 1.2f, 0f), new Vector3(0.6f, 0.6f, 0.6f), white);
            Bob(cloud);
            // تلة بكسل
            float hx = x + R(1f, 4f), hz = R(9f, 12f);
            int steps = Random.Range(3, 6);
            for (int k = 0; k < steps; k++)
                GameFeatures.Px("Hill", p, new Vector3(hx, -1.5f + k * 0.6f + 0.3f, hz), new Vector3((steps - k) * 1.2f, 0.6f, 1.5f), k % 2 == 0 ? green : dgreen);
            GameFeatures.Px("Hill Eye", p, new Vector3(hx - 0.3f, -1.5f + steps * 0.6f - 0.4f, hz - 0.78f), new Vector3(0.12f, 0.35f, 0.05f), Color.black);
        }
        for (float x = minX - 4f; x < maxX + 4f; x += R(7f, 11f))
        {
            // أنبوب أخضر
            float h = R(1f, 2.4f);
            Vector3 b = new Vector3(x, h * 0.5f - 0.2f, R(3f, 5f));
            GameFeatures.Px("Pipe", p, b, new Vector3(1.1f, h, 1.1f), pipe, true);
            GameFeatures.Px("Pipe Rim", p, b + new Vector3(0f, h * 0.5f + 0.15f, 0f), new Vector3(1.4f, 0.35f, 1.4f), pipe * 1.1f, true);
            GameFeatures.Px("Pipe Shine", p, b + new Vector3(-0.3f, 0f, -0.56f), new Vector3(0.15f, h, 0.02f), new Color(0.6f, 1f, 0.6f));
            // شجرة بكسل
            Vector3 tr = new Vector3(x + R(2f, 4f), 0f, R(4f, 6f));
            GameFeatures.Px("Trunk", p, tr + new Vector3(0f, 0.6f, 0f), new Vector3(0.4f, 1.2f, 0.4f), new Color(0.55f, 0.35f, 0.2f), true);
            for (int k = 0; k < 3; k++) GameFeatures.Px("Leaves", p, tr + new Vector3(0f, 1.5f + k * 0.45f, 0f), new Vector3(1.6f - k * 0.45f, 0.45f, 1.2f), k % 2 == 0 ? green : dgreen, true);
            // مربع "؟" طاير
            var q = GameFeatures.Px("Question", p, new Vector3(x + R(-1f, 1f), R(5.5f, 7f), R(1.5f, 3f)), Vector3.one * 0.8f, new Color(1f, 0.75f, 0.15f), true).transform;
            GameFeatures.Text(q, q.position + new Vector3(0f, 0f, -0.42f), "?", 0.1f, new Color(0.6f, 0.3f, 0.05f));
            Bob(q);
        }
        // شمس بكسل
        var sun = new GameObject("Pixel Sun").transform;
        sun.SetParent(p, false);
        sun.position = new Vector3((minX + maxX) * 0.5f + 6f, 12f, 14f);
        for (int y = -2; y <= 2; y++)
            for (int x = -2; x <= 2; x++)
                if (Mathf.Abs(x) + Mathf.Abs(y) < 4) GameFeatures.Px("Sun", sun, new Vector3(x * 0.7f, y * 0.7f, 0f), Vector3.one * 0.7f, (x + y) % 2 == 0 ? new Color(1.6f, 1.3f, 0.3f) : new Color(1.5f, 1f, 0.2f));
        spin.Add(sun);
        // قلعة عند الباب
        Vector3 cs = new Vector3(data.door.x + 4f, data.door.y, 4f);
        Color stone = new Color(0.7f, 0.68f, 0.72f), stoneD = new Color(0.5f, 0.48f, 0.52f);
        for (int row = 0; row < 6; row++)
            for (int k = 0; k < 6; k++)
                GameFeatures.Px("Castle", p, cs + new Vector3(-1.5f + k * 0.6f, 0.3f + row * 0.6f, 0f), Vector3.one * 0.6f, (row + k) % 2 == 0 ? stone : stoneD, true);
        for (int k = 0; k < 6; k += 2) GameFeatures.Px("Battlement", p, cs + new Vector3(-1.5f + k * 0.6f, 3.9f, 0f), Vector3.one * 0.6f, stone, true);
        GameFeatures.Px("Castle Door", p, cs + new Vector3(0f, 0.9f, -0.31f), new Vector3(1f, 1.8f, 0.05f), new Color(0.15f, 0.1f, 0.1f));
        GameFeatures.Px("Flag Pole", p, cs + new Vector3(1.2f, 5f, 0f), new Vector3(0.1f, 2.2f, 0.1f), new Color(0.3f, 0.3f, 0.3f));
        var flag = GameFeatures.Px("Flag", p, cs + new Vector3(1.65f, 5.7f, 0f), new Vector3(0.8f, 0.5f, 0.05f), new Color(0.95f, 0.25f, 0.3f)).transform;
        Bob(flag);
        GameFeatures.Text(p, new Vector3((minX + maxX) * 0.5f, 11f, 10f), "SCORE 000500     WORLD 3-1     TIME 300", 0.12f, Color.white);
    }

    // الكود: لوحة دوائر كهربا تضوي، كود مكتوب، شاشات، ومطر أرقام 0 و 1
    void Code(Transform p, GameManager gm, System.Func<float, float, float> R)
    {
        float len = maxX - minX + 40f, cx = (minX + maxX) * 0.5f;
        LevelBuilder.Visual("Board", PrimitiveType.Quad, p, new Vector3(cx, 5f, 11f), new Vector3(len, 18f, 1f), gm.Mat("Circuit Board", new Color(0.04f, 0.12f, 0.12f), 0.6f), false);
        Color[] trace = { new Color(0.2f, 1.4f, 0.9f), new Color(0.3f, 0.9f, 1.6f), new Color(1.4f, 1f, 0.3f) };
        for (int k = 0; k < 70; k++)
        {
            Vector3 a = new Vector3(R(minX - 10f, maxX + 10f), R(-1f, 11f), 10.9f);
            Color c = trace[k % 3] * 0.6f;
            float w = R(1.5f, 5f);
            var h = LevelBuilder.Visual("Trace", PrimitiveType.Cube, p, a + new Vector3(w * 0.5f, 0f, 0f), new Vector3(w, 0.06f, 0.02f), gm.Mat("Trace " + k % 3, c, 0.5f), false);
            float v = R(0.8f, 3f) * (Random.value < 0.5f ? 1f : -1f);
            LevelBuilder.Visual("Trace", PrimitiveType.Cube, p, a + new Vector3(w, v * 0.5f, 0f), new Vector3(0.06f, Mathf.Abs(v), 0.02f), gm.Mat("Trace " + k % 3, c, 0.5f), false);
            var node = LevelBuilder.Visual("Node", PrimitiveType.Sphere, p, a + new Vector3(w, v, 0f), Vector3.one * 0.18f, gm.Mat("Node " + k % 3, trace[k % 3], 0.5f), false);
            flicker.Add(node.GetComponent<Renderer>());
        }
        string[] code = { "if (rowaida.happy) {", "draw(line);", "for (i = 0; i < 10; i++)", "return true;", "pen.ink += 1;", "}", "function jump() {", "while (playing) {", "score++;", "// TODO: fix bug", "let page = 54;", "print(\"hi\");" };
        for (int k = 0; k < 26; k++)
            GameFeatures.Text(p, new Vector3(R(minX - 8f, maxX + 8f), R(5.5f, 11f), R(8.5f, 10.5f)), code[k % code.Length], R(0.05f, 0.09f), k % 3 == 0 ? new Color(0.4f, 1f, 0.6f) : k % 3 == 1 ? new Color(0.5f, 0.85f, 1f) : new Color(1f, 0.8f, 0.4f), TextAnchor.MiddleLeft);
        for (float x = minX - 6f; x < maxX + 6f; x += R(8f, 12f))
        {
            // شاشة
            Vector3 m = new Vector3(x, R(6.5f, 8.5f), R(6f, 8f));
            GameFeatures.Px("Monitor", p, m, new Vector3(3f, 2f, 0.3f), new Color(0.15f, 0.16f, 0.2f), true);
            var scr = LevelBuilder.Visual("Monitor Screen", PrimitiveType.Cube, p, m + new Vector3(0f, 0f, -0.16f), new Vector3(2.7f, 1.7f, 0.02f), gm.Mat("Monitor Glow", new Color(0.1f, 0.35f, 0.5f), 0.8f), false);
            flicker.Add(scr.GetComponent<Renderer>());
            GameFeatures.Text(p, m + new Vector3(-1.2f, 0.6f, -0.2f), "> run game\n> loading...\n> OK", 0.04f, new Color(0.4f, 1f, 0.6f), TextAnchor.UpperLeft).alignment = TextAlignment.Left;
            // حرف كيبورد طاير
            var key = GameFeatures.Px("Key", p, new Vector3(x + R(2f, 5f), R(5f, 7f), R(2f, 4f)), new Vector3(0.8f, 0.8f, 0.35f), new Color(0.85f, 0.87f, 0.95f), true).transform;
            GameFeatures.Text(key, key.position + new Vector3(0f, 0f, -0.19f), "ABCDEFGHJKLMNPQRSTUVWXYZ"[Random.Range(0, 24)].ToString(), 0.08f, new Color(0.2f, 0.25f, 0.4f));
            Bob(key);
        }
        for (int k = 0; k < 24; k++)
        {
            var tm = GameFeatures.Text(p, new Vector3(R(minX - 8f, maxX + 8f), R(0f, 14f), R(7f, 10f)), "", 0.06f, new Color(0.3f, 1f, 0.5f, 0.7f));
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < 8; i++) sb.Append(Random.value < 0.5f ? "0\n" : "1\n");
            tm.text = sb.ToString();
            rain.Add(tm);
        }
    }

    // الأرقام: أرقام كبيرة ملونة طايرة، رموز رياضيات، عداد خرز، ونرد
    void Numbers(Transform p, GameManager gm, System.Func<float, float, float> R)
    {
        Color[] cols = { new Color(1f, 0.4f, 0.45f), new Color(0.35f, 0.6f, 1f), new Color(1f, 0.75f, 0.2f), new Color(0.4f, 0.8f, 0.45f), new Color(0.75f, 0.45f, 0.95f) };
        string[] syms = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "+", "-", "×", "÷", "=" };
        for (int k = 0; k < 34; k++)
        {
            var tm = GameFeatures.Text(p, new Vector3(R(minX - 8f, maxX + 8f), R(5f, 11f), R(6f, 11f)), syms[k % syms.Length], R(0.15f, 0.35f), cols[k % cols.Length]);
            spin.Add(tm.transform);
            Bob(tm.transform);
        }
        for (float x = minX - 4f; x < maxX + 4f; x += R(9f, 13f))
        {
            // عداد خرز
            Vector3 a = new Vector3(x, 0f, R(2.5f, 4f));
            Color wood = new Color(0.65f, 0.45f, 0.3f);
            GameFeatures.Px("Abacus Frame", p, a + new Vector3(-1.2f, 1.2f, 0f), new Vector3(0.15f, 2.4f, 0.15f), wood, true);
            GameFeatures.Px("Abacus Frame", p, a + new Vector3(1.2f, 1.2f, 0f), new Vector3(0.15f, 2.4f, 0.15f), wood, true);
            GameFeatures.Px("Abacus Top", p, a + new Vector3(0f, 2.4f, 0f), new Vector3(2.55f, 0.15f, 0.15f), wood, true);
            for (int row = 0; row < 4; row++)
            {
                float y = 0.5f + row * 0.5f;
                GameFeatures.Px("Rod", p, a + new Vector3(0f, y, 0f), new Vector3(2.4f, 0.04f, 0.04f), new Color(0.7f, 0.7f, 0.7f));
                int beads = Random.Range(3, 7);
                for (int k = 0; k < beads; k++) LevelBuilder.Visual("Bead", PrimitiveType.Sphere, p, a + new Vector3(-0.95f + k * 0.32f + (k > 2 ? 0.4f : 0f), y, 0f), new Vector3(0.28f, 0.28f, 0.28f), gm.Mat("Bead " + row, cols[row], 0.5f), true);
            }
            // نرد
            Vector3 d = new Vector3(x + R(3f, 6f), 0.45f, R(1.8f, 3f));
            var die = GameFeatures.Px("Die", p, d, Vector3.one * 0.9f, Color.white, true).transform;
            die.localRotation = Quaternion.Euler(0f, R(0f, 60f), 0f);
            for (int k = 0; k < 5; k++) GameFeatures.Px("Pip", die, Vector3.zero, Vector3.one * 0.16f, Color.black).transform.localPosition = new Vector3((k % 2 == 0 ? -0.25f : 0.25f) * (k == 4 ? 0f : 1f), (k < 2 ? 0.25f : k < 4 ? -0.25f : 0f), -0.51f);
        }
    }

    // الزعيم: مربعات تشويش حمرا تومض، شاشات مكسورة، وعلامات خطر
    void Boss(Transform p, GameManager gm, System.Func<float, float, float> R)
    {
        float len = maxX - minX + 40f, cx = (minX + maxX) * 0.5f;
        LevelBuilder.Visual("Dark Wall", PrimitiveType.Quad, p, new Vector3(cx, 5f, 11f), new Vector3(len, 18f, 1f), gm.Mat("Boss Wall", new Color(0.08f, 0.02f, 0.05f), 0.4f), false);
        for (int k = 0; k < 90; k++)
        {
            var g = GameFeatures.Px("Glitch", p, new Vector3(R(minX - 10f, maxX + 10f), R(0f, 12f), R(8f, 10.8f)), new Vector3(R(0.3f, 2f), R(0.1f, 0.6f), 0.05f), k % 4 == 0 ? new Color(0.3f, 0.9f, 1f) : new Color(1f, 0.15f, 0.35f));
            flicker.Add(g.GetComponent<Renderer>());
        }
        for (float x = minX - 4f; x < maxX + 4f; x += R(7f, 10f))
        {
            Vector3 m = new Vector3(x, R(5.5f, 8f), R(5f, 7f));
            var mon = GameFeatures.Px("Broken Monitor", p, m, new Vector3(2.6f, 1.8f, 0.3f), new Color(0.12f, 0.1f, 0.12f), true).transform;
            mon.localRotation = Quaternion.Euler(0f, 0f, R(-12f, 12f));
            GameFeatures.Text(mon, m + new Vector3(0f, 0.05f, -0.2f), "ERROR", 0.07f, new Color(1f, 0.25f, 0.3f));
            for (int k = -1; k <= 1; k += 2) GameFeatures.Px("Crack", mon, Vector3.zero, new Vector3(1.6f, 0.05f, 0.02f), Color.white).transform.localPosition = new Vector3(k * 0.2f, k * 0.1f, -0.17f);
            // علامة خطر
            var tri = GameFeatures.Px("Warning", p, new Vector3(x + R(2f, 5f), R(4f, 6f), R(2f, 4f)), new Vector3(0.9f, 0.9f, 0.05f), new Color(1f, 0.8f, 0.15f)).transform;
            tri.localRotation = Quaternion.Euler(0f, 0f, 45f);
            GameFeatures.Text(p, tri.position + new Vector3(0f, -0.05f, -0.05f), "!", 0.09f, Color.black);
            Bob(tri);
        }
        for (int k = 0; k < 20; k++)
        {
            var tm = GameFeatures.Text(p, new Vector3(R(minX - 8f, maxX + 8f), R(0f, 14f), R(7f, 10f)), "", 0.06f, new Color(1f, 0.3f, 0.4f, 0.7f));
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < 8; i++) sb.Append(Random.value < 0.5f ? "0\n" : "1\n");
            tm.text = sb.ToString();
            rain.Add(tm);
        }
    }

    void Update()
    {
        float t = Time.time, dt = Time.deltaTime;
        for (int i = 0; i < bob.Count; i++) if (bob[i] != null) bob[i].position = bobHome[i] + new Vector3(style == 1 && i % 2 == 0 ? Mathf.Sin(t * 0.2f + i) * 1.5f : 0f, Mathf.Sin(t * 1.5f + i) * 0.15f, 0f);
        foreach (Transform s in spin) if (s != null) s.localRotation = Quaternion.Euler(0f, Mathf.Sin(t * 0.8f + s.position.x) * 30f, style == 1 ? t * 20f : Mathf.Sin(t + s.position.y) * 10f);
        foreach (TextMesh r in rain)
        {
            Vector3 p = r.transform.position;
            p.y -= dt * 1.5f;
            if (p.y < -2f) p.y = 14f;
            r.transform.position = p;
        }
        foreach (Renderer r in flicker) if (r != null && Random.value < (style == 4 ? 0.03f : 0.01f)) r.enabled = !r.enabled || Random.value < 0.6f;
    }
}
