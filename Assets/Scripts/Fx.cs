using UnityEngine;

// تأثيرات صغيرة تعطي اللعبة إحساس حي: قصاصات ورق ملونة، غبار، لمعات، وفتات رصاص
public static class Fx
{
    static readonly Color[] ConfettiColors =
    {
        new Color(1f, 0.45f, 0.55f), new Color(1f, 0.8f, 0.3f), new Color(0.45f, 0.8f, 1f),
        new Color(0.55f, 0.9f, 0.5f), new Color(0.8f, 0.6f, 1f), Color.white,
    };

    // قصاصات ورق ملونة تطير وتنزل وهي تتمايل (لما تفوز)
    public static void Confetti(Vector3 pos, int count)
    {
        GameManager gm = GameManager.Instance;
        for (int i = 0; i < count; i++)
        {
            Color c = ConfettiColors[Random.Range(0, ConfettiColors.Length)];
            Material m = gm.Mat("Confetti " + ColorUtility.ToHtmlStringRGB(c), c, 0.3f);
            var go = LevelBuilder.Visual("Confetti", PrimitiveType.Cube, null, pos, new Vector3(0.14f, 0.09f, 0.01f) * Random.Range(0.8f, 1.3f), m, false);
            go.transform.rotation = Random.rotation;
            var p = go.AddComponent<FxParticle>();
            p.velocity = new Vector3(Random.Range(-3.5f, 3.5f), Random.Range(4f, 9f), Random.Range(-2f, 1.5f));
            p.gravity = 6f;
            p.drag = 1.6f;
            p.spin = Random.insideUnitSphere * 720f;
            p.flutter = 1.2f;
            p.life = Random.Range(2.2f, 3.2f);
        }
    }

    // غبار ورق تحت رجول رويدا لما تنزل من طيحة
    public static void Dust(Vector3 pos, float strength)
    {
        GameManager gm = GameManager.Instance;
        Material m = gm.Mat("Dust", new Color(0.92f, 0.9f, 0.88f), 0.1f);
        int count = Mathf.RoundToInt(Mathf.Lerp(4f, 10f, strength));
        for (int i = 0; i < count; i++)
        {
            float side = i % 2 == 0 ? 1f : -1f;
            var go = LevelBuilder.Visual("Dust", PrimitiveType.Sphere, null, pos + new Vector3(side * 0.2f, 0.05f, Random.Range(-0.3f, 0.3f)),
                Vector3.one * Random.Range(0.1f, 0.18f), m, false);
            var p = go.AddComponent<FxParticle>();
            p.velocity = new Vector3(side * Random.Range(0.8f, 2.2f), Random.Range(0.3f, 1f), Random.Range(-0.5f, 0.5f));
            p.drag = 4f;
            p.grow = 1.2f;
            p.life = Random.Range(0.4f, 0.7f);
        }
    }

    // لمعات تطلع على شكل دائرة (لما ترجع رويدا بعد ما تخسر قلب)
    public static void Sparkle(Vector3 pos, Color color, int count)
    {
        GameManager gm = GameManager.Instance;
        Material m = gm.Mat("Sparkle " + ColorUtility.ToHtmlStringRGB(color), color * 2.2f, 0.9f);
        for (int i = 0; i < count; i++)
        {
            float a = i / (float)count * Mathf.PI * 2f;
            var go = LevelBuilder.Visual("Sparkle", PrimitiveType.Sphere, null, pos, Vector3.one * 0.09f, m, false);
            var p = go.AddComponent<FxParticle>();
            p.velocity = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * Random.Range(2.2f, 3f);
            p.drag = 3f;
            p.life = 0.6f;
        }
    }

    // فتات رصاص صغير عند رأس القلم وهو يرسم
    public static void Graphite(Vector3 pos)
    {
        GameManager gm = GameManager.Instance;
        Material m = gm.Mat("Graphite Dust", new Color(0.25f, 0.25f, 0.28f), 0.4f);
        var go = LevelBuilder.Visual("Graphite", PrimitiveType.Cube, null, pos + Random.insideUnitSphere * 0.08f, Vector3.one * Random.Range(0.03f, 0.055f), m, false);
        go.transform.rotation = Random.rotation;
        var p = go.AddComponent<FxParticle>();
        p.velocity = new Vector3(Random.Range(-0.6f, 0.6f), Random.Range(0.2f, 1.2f), Random.Range(-0.4f, 0.1f));
        p.gravity = 5f;
        p.spin = Random.insideUnitSphere * 400f;
        p.life = Random.Range(0.35f, 0.6f);
    }
}

// جزء صغير يتحرك بسرعة وجاذبية ويصغر لين يختفي
public class FxParticle : MonoBehaviour
{
    public Vector3 velocity, spin;
    public float gravity, drag, grow, flutter, life = 1f;
    float age;
    Vector3 baseScale;

    void Start()
    {
        baseScale = transform.localScale;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        age += dt;
        velocity.y -= gravity * dt;
        velocity *= Mathf.Max(0f, 1f - drag * dt);
        if (flutter > 0f) velocity.x += Mathf.Sin(age * 7f + GetInstanceID()) * flutter * dt * 4f;
        transform.position += velocity * dt;
        if (spin != Vector3.zero) transform.Rotate(spin * dt, Space.Self);
        float k = age / life;
        float shrink = k > 0.7f ? 1f - (k - 0.7f) / 0.3f : 1f;
        transform.localScale = baseScale * (1f + grow * k) * Mathf.Max(0f, shrink);
        if (age >= life) Destroy(gameObject);
    }
}
