using System.Collections.Generic;
using UnityEngine;

// الجزء الثاني — فصل أدوات المدرسة: مسطرة ميزان، صندوق الألوان، آلة حاسبة، دباسة، ومقلمة
public static class SchoolFeatures
{
    public static void Build(Transform root, LevelData data, GameManager gm, System.Random rng)
    {
        foreach (Vector4 r in data.rulers) RulerSeesaw.Create(root, r, gm);
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
            gm.Sound.PlayBoing();
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
