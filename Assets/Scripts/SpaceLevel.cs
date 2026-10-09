using UnityEngine;

// مرحلة الفضاء: كواكب كبيرة ورا، جاذبية خفيفة، وفضائيين يطلقون ليزر
public static class SpaceFeatures
{
    public static void Build(Transform root, LevelData data, GameManager gm)
    {
        if (!data.planets) return;
        // كواكب ضخمة بعيدة: تمشي مع الكاميرا تقريبًا (مثل السما) وتبقى على أطراف الشاشة
        var far = new GameObject("Planets").transform;
        far.SetParent(root, false);
        far.gameObject.AddComponent<ParallaxFollow>().factor = 0.9f;
        Planet(far, new Vector3(-34f, 14f, 72f), 14f, new Color(0.95f, 0.55f, 0.35f), true, gm);
        Planet(far, new Vector3(30f, -40f, 76f), 24f, new Color(0.35f, 0.55f, 0.95f), false, gm);
        Planet(far, new Vector3(38f, 11f, 66f), 7f, new Color(0.75f, 0.45f, 0.9f), true, gm);
        Planet(far, new Vector3(-22f, -36f, 70f), 11f, new Color(0.45f, 0.85f, 0.65f), false, gm);
    }

    static void Planet(Transform root, Vector3 pos, float size, Color color, bool ring, GameManager gm)
    {
        Material body = gm.Mat("Planet " + ColorUtility.ToHtmlStringRGB(color), color, 0.3f);
        Material band = gm.Mat("Planet Band " + ColorUtility.ToHtmlStringRGB(color), color * 0.75f, 0.3f);
        var p = LevelBuilder.Visual("Planet", PrimitiveType.Sphere, root, pos, Vector3.one * size, body, false);
        // خطوط على الكوكب
        for (int i = -1; i <= 1; i++)
            LevelBuilder.Visual("Band", PrimitiveType.Sphere, p.transform, new Vector3(0f, i * 0.22f, 0f), new Vector3(1.005f, 0.08f, 1.005f), band, false);
        if (!ring) return;
        Material ringMat = gm.Mat("Planet Ring", new Color(0.95f, 0.88f, 0.7f), 0.4f);
        var r = new GameObject("Ring");
        r.transform.SetParent(p.transform, false);
        r.transform.localRotation = Quaternion.Euler(70f, 0f, 18f);
        r.transform.localScale = Vector3.one * 0.95f;
        r.AddComponent<MeshFilter>().sharedMesh = PaperMeshes.Ring(0.85f, 0.06f, 48, 6);
        r.AddComponent<MeshRenderer>().sharedMaterial = ringMat;
    }
}

// طلقة ليزر: تمشي بخط مستقيم، إذا صابت رويدا تدفها، وإذا صابت رسمة (درع) أو صفحة تنطفي بشرارات
public class LaserBolt : MonoBehaviour
{
    Vector3 velocity;
    float life = 3f;

    static Material boltMat;

    public static void Fire(Vector3 from, Vector3 velocity)
    {
        if (boltMat == null)
        {
            boltMat = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "Laser" };
            boltMat.SetColor("_BaseColor", new Color(3f, 0.6f, 2.4f));
        }
        var go = LevelBuilder.Visual("Laser", PrimitiveType.Capsule, null, from, new Vector3(0.09f, 0.28f, 0.09f), boltMat, false);
        go.transform.rotation = Quaternion.FromToRotation(Vector3.up, velocity);
        go.AddComponent<LaserBolt>().velocity = velocity;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        life -= dt;
        if (life <= 0f) { Destroy(gameObject); return; }

        Vector3 step = velocity * dt;
        if (Physics.SphereCast(transform.position, 0.08f, velocity.normalized, out RaycastHit hit, step.magnitude, ~0, QueryTriggerInteraction.Ignore))
        {
            GameManager gm = GameManager.Instance;
            var rowaida = hit.collider.GetComponentInParent<RowaidaController>();
            if (rowaida != null && gm.IsPlaying)
            {
                rowaida.Knockback(velocity.x < 0f ? -1f : 1f);
                gm.Sound.PlayBoing();
                var cam = Camera.main.GetComponent<CameraFollow>();
                if (cam != null) cam.Shake(0.1f, 0.2f);
            }
            gm.Sound.PlayPoof();
            Fx.Sparkle(hit.point, new Color(1f, 0.4f, 0.9f), 10);
            Destroy(gameObject);
            return;
        }
        transform.position += step;
    }
}

// شي بعيد يتحرك مع الكاميرا (factor = 1 يعني ثابت بالشاشة)
public class ParallaxFollow : MonoBehaviour
{
    public float factor = 0.9f;

    void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        Vector3 c = cam.transform.position;
        transform.position = new Vector3(c.x * factor, c.y * factor, 0f);
    }
}
