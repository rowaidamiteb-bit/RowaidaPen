using System.Collections.Generic;
using UnityEngine;

// قلم شفاف يرسم الحل قدام اللاعبة: لما رويدا توقف عند عقبة شوي، يطلع قلم يرسم نقاط تضوي وين ترسمين ووش ترسمين
// ويعيد الرسم كل شوي لين تحلينها (والحل يتحدث: لو رسمتي جزء يختفي من التلميح)
public class HintGhost : MonoBehaviour
{
    const float Speed = 4.5f, Pause = 1.1f, Spacing = 0.26f, Delay = 2.4f;

    System.Func<List<Vector3[]>> build;
    readonly List<Transform> dots = new List<Transform>();
    readonly List<float> dotAt = new List<float>();
    readonly List<int> strokeEnds = new List<int>();
    Transform pencil;
    TextMesh label;
    System.Func<string> labelText;
    float total, cycleT;
    float minX, maxX, maxY;
    Material dotMat, rimMat;

    // تستدعيها العقبة كل فريم: waiting = رويدا واقفة تستنى عندها
    public static void Tick(ref HintGhost h, ref float heldT, bool waiting, System.Func<List<Vector3[]>> build, System.Func<string> label = null)
    {
        heldT = waiting ? heldT + Time.deltaTime : 0f;
        bool want = waiting && heldT > Delay;
        if (want && h == null) h = Create(build, label);
        else if (!want && h != null) { Destroy(h.gameObject); h = null; }
    }

    static HintGhost Create(System.Func<List<Vector3[]>> build, System.Func<string> label)
    {
        GameManager gm = GameManager.Instance;
        var go = new GameObject("Hint Ghost");
        var h = go.AddComponent<HintGhost>();
        h.build = build;
        h.labelText = label;
        h.dotMat = gm.Mat("Hint Glow", new Color(1.6f, 1.25f, 0.3f), 0.3f);
        h.rimMat = gm.Mat("Hint Rim", new Color(0.55f, 0.32f, 0.12f), 0.2f);
        // القلم: جسم أصفر، رأس خشب، سن رصاص، ممحاة وردية
        h.pencil = new GameObject("Hint Pencil").transform;
        h.pencil.SetParent(go.transform, false);
        var inner = new GameObject("Tilt").transform;
        inner.SetParent(h.pencil, false);
        inner.localRotation = Quaternion.Euler(0f, 0f, -32f);
        Material yellow = gm.Mat("Hint Pencil", new Color(1f, 0.8f, 0.2f), 0.4f);
        Material wood = gm.Mat("Hint Wood", new Color(0.95f, 0.78f, 0.55f), 0.2f);
        Material lead = gm.Mat("Hint Lead", new Color(0.15f, 0.15f, 0.18f), 0.5f);
        Material pink = gm.Mat("Hint Eraser", new Color(1f, 0.55f, 0.65f), 0.3f);
        Material steel = gm.Mat("Spoon Steel", new Color(0.82f, 0.84f, 0.88f), 0.95f, 0.85f);
        LevelBuilder.Visual("Lead", PrimitiveType.Sphere, inner, new Vector3(0f, 0.06f, -0.6f), new Vector3(0.07f, 0.12f, 0.07f), lead, false);
        LevelBuilder.Visual("Wood", PrimitiveType.Sphere, inner, new Vector3(0f, 0.2f, -0.6f), new Vector3(0.17f, 0.3f, 0.17f), wood, false);
        LevelBuilder.Visual("Body", PrimitiveType.Cylinder, inner, new Vector3(0f, 0.85f, -0.6f), new Vector3(0.2f, 0.6f, 0.2f), yellow, false);
        LevelBuilder.Visual("Band", PrimitiveType.Cylinder, inner, new Vector3(0f, 1.48f, -0.6f), new Vector3(0.21f, 0.05f, 0.21f), steel, false);
        LevelBuilder.Visual("Eraser", PrimitiveType.Cylinder, inner, new Vector3(0f, 1.6f, -0.6f), new Vector3(0.2f, 0.08f, 0.2f), pink, false);
        if (label != null)
        {
            var t = new GameObject("Hint Label");
            t.transform.SetParent(go.transform, false);
            h.label = t.AddComponent<TextMesh>();
            h.label.fontSize = 64;
            h.label.characterSize = 0.09f;
            h.label.anchor = TextAnchor.MiddleCenter;
            h.label.fontStyle = FontStyle.Bold;
            h.label.color = new Color(0.95f, 0.4f, 0.1f);
        }
        h.Rebuild();
        LevelSfx.Play("chime", 0.35f, 1.5f);
        gm.UI.ShowMessage("", 3f, "شوفي القلم الأصفر... اضغطي واسحبي وارسمي نفس اللي يرسمه!");
        return h;
    }

    // نجيب الحل من جديد (يمكن جزء منه انحل)
    void Rebuild()
    {
        foreach (Transform d in dots) Destroy(d.gameObject);
        dots.Clear(); dotAt.Clear(); strokeEnds.Clear();
        total = 0f;
        List<Vector3[]> strokes = build();
        Vector3 labelPos = Vector3.zero;
        bool any = false;
        minX = float.MaxValue; maxX = float.MinValue; maxY = float.MinValue;
        foreach (Vector3[] s in strokes)
        {
            if (s == null || s.Length < 2) continue;
            if (!any) { labelPos = s[0]; any = true; }
            float carry = 0f;
            for (int i = 1; i < s.Length; i++)
            {
                float seg = Vector3.Distance(s[i - 1], s[i]);
                for (float d = carry; d < seg; d += Spacing)
                {
                    Vector3 p = Vector3.Lerp(s[i - 1], s[i], d / seg);
                    p.z = -0.6f;
                    minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x); maxY = Mathf.Max(maxY, p.y);
                    var dot = LevelBuilder.Visual("Hint Dot", PrimitiveType.Sphere, transform, p, Vector3.zero, dotMat, false).transform;
                    // حد غامق ورا النقطة عشان تبان فوق أي لون
                    LevelBuilder.Visual("Hint Rim", PrimitiveType.Sphere, dot, new Vector3(0f, 0f, 0.25f), Vector3.one * 1.22f, rimMat, false);
                    dots.Add(dot);
                    dotAt.Add(total + d);
                    carry = d + Spacing - seg;
                }
                total += seg;
            }
            strokeEnds.Add(dots.Count);
            total += 0.6f;   // القلم يرفع بين الخطوط
        }
        if (label != null)
        {
            string txt = labelText();
            label.text = txt;
            label.gameObject.SetActive(any && !string.IsNullOrEmpty(txt));
            label.transform.position = labelPos + new Vector3(0f, 1.1f, -0.7f);
        }
        pencil.gameObject.SetActive(any);
    }

    void Update()
    {
        // الكاميرا ترجع لورا وتتحرك عشان التلميح كله يبان مع رويدا
        RowaidaController r = GameManager.Instance != null ? GameManager.Instance.Rowaida : null;
        if (r != null && dots.Count > 0)
        {
            float rx = r.transform.position.x;
            float lo = Mathf.Min(minX, rx), hi = Mathf.Max(maxX, rx);
            float shift = Mathf.Clamp((lo + hi) * 0.5f - rx, -7f, 7f);
            if (Mathf.Abs(shift) > Mathf.Abs(CameraFollow.ShiftRequest)) CameraFollow.ShiftRequest = shift;
            float span = Mathf.Max(hi - lo, (maxY - r.transform.position.y) * 1.2f);
            CameraFollow.PullRequest = Mathf.Max(CameraFollow.PullRequest, Mathf.Clamp((span - 2f) / 5f, 0.25f, 1.4f));
        }
        float cycle = total / Speed + Pause;
        cycleT += Time.deltaTime;
        if (cycleT > cycle) { cycleT = 0f; Rebuild(); cycle = total / Speed + Pause; }
        float p = cycleT * Speed;
        float pulse = 0.25f + Mathf.Sin(Time.time * 6f) * 0.04f;
        Vector3 tip = pencil.position;
        for (int i = 0; i < dots.Count; i++)
        {
            bool on = dotAt[i] <= p;
            float pop = Mathf.Clamp01((p - dotAt[i]) * 3f);
            dots[i].localScale = on ? Vector3.one * pulse * (0.6f + pop * 0.4f) : Vector3.zero;
            if (on) tip = dots[i].position;
        }
        pencil.position = new Vector3(tip.x, tip.y, 0f);
        bool drawing = p < total;
        pencil.localScale = Vector3.one * (drawing ? 1f : Mathf.MoveTowards(pencil.localScale.x, 0.85f, Time.deltaTime));
        if (label != null) label.transform.localScale = Vector3.one * (1f + Mathf.Sin(Time.time * 4f) * 0.06f);
    }

    // دايرة صغيرة (عشان التلميح يوري "ارسمي دايرة")
    public static Vector3[] Circle(Vector3 c, float r = 0.65f)
    {
        var pts = new Vector3[21];
        for (int i = 0; i <= 20; i++)
        {
            float a = Mathf.PI * 0.5f + i / 20f * Mathf.PI * 2f;
            pts[i] = c + new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f);
        }
        return pts;
    }
}
