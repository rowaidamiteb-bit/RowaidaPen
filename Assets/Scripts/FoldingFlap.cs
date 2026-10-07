using UnityEngine;

// صفحة تنطوي وتنفتح مثل الأوريغامي: تلف حول مفصل.
// لتحت: الجسر يختفي وتصير حفرة. لفوق: توقف مثل الجدار ورويدا تنتظر لين تنزل
public class FoldingFlap : MonoBehaviour
{
    FoldSpec spec;
    Rigidbody rb;
    float clock;
    float foldedAngle;
    bool wasMoving;

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
        // الزاوية لما تكون مطوية: لتحت أو لفوق، حسب جهة المفصل
        float dir = spec.foldUp ? 1f : -1f;
        flap.foldedAngle = (spec.foldUp ? 95f : 100f) * dir * side;
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
        rb.MoveRotation(Quaternion.Euler(0f, 0f, angle));

        // صوت ورق لما تبدأ تتحرك
        bool moving = Mathf.Abs(angle - AngleAt(clock - 0.05f)) > 0.01f;
        if (moving && !wasMoving)
        {
            Camera cam = Camera.main;
            if (cam != null && Mathf.Abs(cam.transform.position.x - transform.position.x) < 16f)
                GameManager.Instance.Sound.Play(SoundClips.Flop, 0.45f);
        }
        wasMoving = moving;
    }
}
