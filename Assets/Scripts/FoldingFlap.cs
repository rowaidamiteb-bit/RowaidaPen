using UnityEngine;

// صفحة تنطوي وتنفتح مثل الأوريغامي: تلف حول مفصل.
// تنطوي فوق الصفحة اللي جنبها (مثل قلب ورقة) أو توقف مثل الجدار، وتمسح أي رسمة تعترضها
public class FoldingFlap : MonoBehaviour
{
    FoldSpec spec;
    Rigidbody rb;
    float clock;
    float foldedAngle;
    bool wasMoving;
    BoxCollider slabCollider;

    // كل الصفحات اللي تنطوي في المرحلة (رويدا تسأل عنها قبل ما تمشي عليها)
    public static readonly System.Collections.Generic.List<FoldingFlap> All = new System.Collections.Generic.List<FoldingFlap>();
    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    // حدود الصفحة على محور x
    public float MinX => spec.hingeOnRight ? spec.hingeX - spec.length : spec.hingeX;
    public float MaxX => spec.hingeOnRight ? spec.hingeX : spec.hingeX + spec.length;
    public float Top => spec.top;

    // الصفحة مفتوحة وبتظل مفتوحة هالكم ثانية على الأقل؟
    public bool StaysFlatFor(float seconds)
    {
        float p = Mathf.Repeat(clock, spec.period) / spec.period;
        return p < 0.45f && (0.45f - p) * spec.period > seconds;
    }

    public static FoldingFlap Spawn(Transform parent, FoldSpec spec, GameManager gm)
    {
        var pivot = new GameObject(spec.foldUp ? "Folding Wall" : "Folding Bridge");
        pivot.transform.SetParent(parent, false);
        pivot.transform.position = new Vector3(spec.hingeX, spec.top, 0f);

        const float thickness = 0.35f;
        float side = spec.hingeOnRight ? -1f : 1f;
        var slab = new GameObject("Flap Page");
        slab.transform.SetParent(pivot.transform, false);
        slab.transform.localPosition = new Vector3(side * spec.length * 0.5f, -thickness * 0.5f, 0f);
        Vector3 size = new Vector3(spec.length, thickness, 4f);
        slab.AddComponent<MeshFilter>().sharedMesh = PaperMeshes.PaperSlab(size);
        slab.AddComponent<MeshRenderer>().sharedMaterials = new[] { gm.paperMaterial, gm.pageEdgeMaterial };
        slab.AddComponent<BoxCollider>().size = size;
        var rng = new System.Random((int)(spec.hingeX * 10f));
        LevelBuilder.OutlineBox(slab.transform, size, gm, rng);

        var rb = pivot.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        var flap = pivot.AddComponent<FoldingFlap>();
        flap.spec = spec;
        flap.rb = rb;
        flap.clock = spec.phase;
        // الزاوية لما تكون مطوية: تنطوي فوق الصفحة اللي جنبها، أو توقف جدار، أو تتدلى لتحت
        float dir = spec.foldUp || spec.foldOver ? 1f : -1f;
        flap.foldedAngle = (spec.foldOver ? 178f : spec.foldUp ? 95f : 100f) * dir * side;
        flap.slabCollider = slab.GetComponent<BoxCollider>();

        // خط الطيّة عند المفصل
        LevelBuilder.Visual("Fold Crease", PrimitiveType.Cube, pivot.transform, new Vector3(0f, 0.005f, 0f), new Vector3(0.03f, 0.01f, 4.05f), gm.pencilMaterial, false);
        return flap;
    }

    // دورة كاملة: مفتوحة، تنطوي، مطوية، تنفتح
    float AngleAt(float t)
    {
        float p = Mathf.Repeat(t, spec.period) / spec.period;
        float k;
        if (p < 0.45f) k = 0f;
        else if (p < 0.55f) k = Mathf.SmoothStep(0f, 1f, (p - 0.45f) / 0.1f);
        else if (p < 0.9f) k = 1f;
        else k = Mathf.SmoothStep(1f, 0f, (p - 0.9f) / 0.1f);
        return foldedAngle * k;
    }

    void FixedUpdate()
    {
        clock += Time.fixedDeltaTime;
        float angle = AngleAt(clock);
        // تحذير: الصفحة ترتجف شوي قبل ما تنطوي
        float p = Mathf.Repeat(clock, spec.period) / spec.period;
        float warn = p > 0.45f - 0.4f / spec.period && p < 0.45f ? Mathf.Sin(clock * 60f) * 0.7f : 0f;
        rb.MoveRotation(Quaternion.Euler(0f, 0f, angle + warn));

        // صوت ورق لما تبدأ تتحرك
        bool moving = Mathf.Abs(angle - AngleAt(clock - 0.05f)) > 0.01f;
        if (moving && !wasMoving)
        {
            Camera cam = Camera.main;
            if (cam != null && Mathf.Abs(cam.transform.position.x - transform.position.x) < 16f)
                GameManager.Instance.Sound.Play(SoundClips.Flop, 0.45f);
        }
        wasMoving = moving;

        // الطيّة تمسح أي رسمة تعترضها وهي تنطوي (مو وهي ترجع تنفتح)
        // وهي تتحرك ما تصدم شي (بس لما توقف تصير أرض أو جدار)
        slabCollider.enabled = !moving;
        bool folding = moving && Mathf.Abs(angle) > Mathf.Abs(AngleAt(clock - 0.05f));
        if (folding) CrushInk();
    }

    void CrushInk()
    {
        Transform t = slabCollider.transform;
        Vector3 half = Vector3.Scale(slabCollider.size * 0.5f, t.lossyScale) + Vector3.one * 0.05f;
        foreach (Collider c in Physics.OverlapBox(t.TransformPoint(slabCollider.center), half, t.rotation))
        {
            var mark = c.GetComponentInParent<InkMark>();
            if (mark == null) continue;
            GameManager.Instance.Sound.PlayPoof();
            Destroy(mark.gameObject);
        }
    }
}
