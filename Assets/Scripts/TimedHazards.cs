using System.Collections.Generic;
using UnityEngine;

// عقبات تجي كل شوي (سمك ينط، محارة تسكر، أقلام تتدحرج، طيارات ورق، وقواق، بخار، خفافيش، صنوج...).
// كل وحدة لها منطقة على الطريق: رويدا تستنى قبلها إذا العقبة جاية، ولو صدمتها ترجعها لورا
public class HazardSpec
{
    public string kind;
    public float x, width = 2f, period = 4f, phase, y;   // y: ارتفاع الأرض تحت العقبة
    public HazardSpec(string kind, float x, float width = 2f, float period = 4f, float phase = 0f)
    { this.kind = kind; this.x = x; this.width = width; this.period = period; this.phase = phase; }
}

public class TimedHazard : MonoBehaviour
{
    HazardSpec spec;
    float start;
    bool holding, played;
    Transform model;
    readonly List<Transform> parts = new List<Transform>();
    const float Active = 1.2f;   // كم ثانية تكون العقبة على الطريق

    float Local => Mathf.Repeat(Time.time - start + spec.phase, spec.period);
    // العقبة على الطريق من 0 لين Active
    bool On => Local < Active && Enabled;
    float UntilOn => Local < Active ? 0f : spec.period - Local;
    float K => Mathf.Clamp01(Local / Active);   // تقدم الحركة وهي شغالة
    bool Enabled => spec.kind != "bat" || DayNight.Night;

    public static void Create(Transform root, HazardSpec spec, GameManager gm)
    {
        var go = new GameObject("Hazard " + spec.kind);
        go.transform.SetParent(root, false);
        var h = go.AddComponent<TimedHazard>();
        h.spec = spec;
        h.start = Time.time;
        h.model = new GameObject("Model").transform;
        h.model.SetParent(go.transform, false);
        go.transform.position = new Vector3(0f, spec.y, 0f);   // كل شي يرتفع مع الأرض
        h.Build(gm);
    }

    Transform Part(PrimitiveType t, Vector3 pos, Vector3 scale, Material m, float rotZ = 0f)
    {
        var p = LevelBuilder.Visual("Part", t, model, pos, scale, m).transform;
        p.localRotation = Quaternion.Euler(0f, 0f, rotZ);
        parts.Add(p);
        return p;
    }

    void Build(GameManager gm)
    {
        Material white = gm.Mat("Monster Eye", Color.white, 0.7f);
        Material black = gm.Mat("Monster Pupil", new Color(0.05f, 0.04f, 0.06f), 0.9f);
        switch (spec.kind)
        {
            case "fish":
            {
                Material orange = gm.Mat("Jump Fish", new Color(1f, 0.55f, 0.2f), 0.6f);
                Part(PrimitiveType.Sphere, Vector3.zero, new Vector3(1.1f, 0.6f, 0.4f), orange);
                Part(PrimitiveType.Cube, new Vector3(-0.65f, 0f, 0f), new Vector3(0.4f, 0.4f, 0.08f), orange, 45f);
                Part(PrimitiveType.Sphere, new Vector3(0.3f, 0.1f, -0.2f), Vector3.one * 0.18f, white);
                Part(PrimitiveType.Sphere, new Vector3(0.34f, 0.1f, -0.27f), Vector3.one * 0.08f, black);
                break;
            }
            case "clam":
            {
                Material shell = gm.Mat("Clam Shell", new Color(0.75f, 0.6f, 0.85f), 0.5f);
                Material pearl = gm.Mat("Pearl", new Color(1f, 0.97f, 0.95f), 0.95f);
                Part(PrimitiveType.Sphere, new Vector3(0f, 0.1f, 0f), new Vector3(2.2f, 0.4f, 1.6f), shell);          // تحت
                Part(PrimitiveType.Sphere, new Vector3(0f, 0.3f, 0f), new Vector3(2.2f, 0.4f, 1.6f), shell);          // فوق (يتحرك)
                Part(PrimitiveType.Sphere, new Vector3(0f, 0.35f, -0.2f), Vector3.one * 0.35f, pearl);
                break;
            }
            case "pencil":
            {
                Material yellow = gm.Mat("Big Pencil", new Color(1f, 0.75f, 0.2f), 0.35f);
                Material pink = gm.Mat("Eraser Rubber", new Color(1f, 0.6f, 0.66f), 0.25f);
                var body = Part(PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.45f, 1f, 0.45f), yellow);
                body.localRotation = Quaternion.Euler(90f, 0f, 0f);
                var er = Part(PrimitiveType.Cylinder, new Vector3(0f, 0f, 1.05f), new Vector3(0.45f, 0.1f, 0.45f), pink);
                er.localRotation = Quaternion.Euler(90f, 0f, 0f);
                break;
            }
            case "crayons":
            {
                for (int i = 0; i < 4; i++)
                {
                    Material m = gm.Mat("Crayon Fall " + i, MusicFeatures.KeyColors[i], 0.4f);
                    Part(PrimitiveType.Cylinder, new Vector3(-0.8f + i * 0.55f, 0f, 0f), new Vector3(0.22f, 0.45f, 0.22f), m);
                }
                // صندوق الألوان فوق
                Material box = gm.Mat("Crayon Box", new Color(0.95f, 0.75f, 0.2f), 0.4f);
                var b = LevelBuilder.Visual("Crayon Box", PrimitiveType.Cube, transform, new Vector3(spec.x, 4.6f, 0.3f), new Vector3(2.6f, 0.9f, 1.4f), box).transform;
                LevelBuilder.Visual("Box Stripe", PrimitiveType.Cube, b, new Vector3(0f, 0f, -0.51f), new Vector3(1.02f, 0.25f, 0.02f), gm.Mat("Crayon Box Stripe", new Color(0.2f, 0.55f, 0.3f), 0.4f), false);
                break;
            }
            case "plane":
            {
                Material paper = gm.Mat("Bird Paper", new Color(0.97f, 0.95f, 0.9f), 0.2f);
                Part(PrimitiveType.Cube, new Vector3(0f, 0f, 0f), new Vector3(1.4f, 0.06f, 0.9f), paper, 0f).localRotation = Quaternion.Euler(0f, 0f, -8f);
                Part(PrimitiveType.Cube, new Vector3(-0.1f, -0.15f, 0f), new Vector3(1.2f, 0.25f, 0.05f), paper, -8f);
                break;
            }
            case "cuckoo":
            {
                Material wood = gm.Mat("Cuckoo Wood", new Color(0.5f, 0.32f, 0.2f), 0.3f);
                Material bird = gm.Mat("Cuckoo Bird", new Color(0.95f, 0.85f, 0.3f), 0.5f);
                Material beak = gm.Mat("Bird Beak", new Color(1f, 0.62f, 0.25f), 0.3f);
                // بيت القواق ثابت فوق
                var house = LevelBuilder.Visual("Cuckoo House", PrimitiveType.Cube, transform, new Vector3(spec.x + spec.width * 0.5f + 0.9f, 2.2f, 0.6f), new Vector3(1.6f, 1.8f, 1.2f), wood).transform;
                var roof = LevelBuilder.Visual("Roof", PrimitiveType.Cube, transform, new Vector3(spec.x + spec.width * 0.5f + 0.9f, 3.25f, 0.6f), new Vector3(1.4f, 1.4f, 1.3f), wood).transform;
                roof.localRotation = Quaternion.Euler(0f, 0f, 45f);
                Part(PrimitiveType.Sphere, Vector3.zero, new Vector3(0.8f, 0.6f, 0.6f), bird);
                Part(PrimitiveType.Sphere, new Vector3(-0.35f, 0.2f, 0f), Vector3.one * 0.45f, bird);
                Part(PrimitiveType.Cube, new Vector3(-0.65f, 0.2f, 0f), new Vector3(0.3f, 0.12f, 0.12f), beak);
                Part(PrimitiveType.Sphere, new Vector3(-0.42f, 0.3f, -0.2f), Vector3.one * 0.1f, black);
                // عمود يدفه (نابض)
                Part(PrimitiveType.Cube, new Vector3(0.9f, 0f, 0f), new Vector3(1.6f, 0.08f, 0.08f), wood);
                break;
            }
            case "steam":
            {
                Material pipe = gm.Mat("Steam Pipe", new Color(0.45f, 0.45f, 0.5f), 0.7f, 0.5f);
                LevelBuilder.Visual("Vent", PrimitiveType.Cylinder, transform, new Vector3(spec.x, 0.05f, 0.6f), new Vector3(0.9f, 0.08f, 0.9f), pipe);
                break;
            }
            case "bat":
            {
                Material dark = gm.Mat("Bat", new Color(0.18f, 0.12f, 0.25f), 0.3f);
                Part(PrimitiveType.Sphere, Vector3.zero, new Vector3(0.5f, 0.45f, 0.4f), dark);
                Part(PrimitiveType.Cube, new Vector3(-0.5f, 0.1f, 0f), new Vector3(0.7f, 0.06f, 0.4f), dark, 20f);
                Part(PrimitiveType.Cube, new Vector3(0.5f, 0.1f, 0f), new Vector3(0.7f, 0.06f, 0.4f), dark, -20f);
                Part(PrimitiveType.Sphere, new Vector3(-0.1f, 0.08f, -0.2f), Vector3.one * 0.1f, gm.Mat("Bat Eye", new Color(1.5f, 1.3f, 0.3f), 0.5f));
                Part(PrimitiveType.Sphere, new Vector3(0.1f, 0.08f, -0.2f), Vector3.one * 0.1f, gm.Mat("Bat Eye", new Color(1.5f, 1.3f, 0.3f), 0.5f));
                break;
            }
            case "cymbals":
            {
                Material brass = gm.Mat("Gear Brass", new Color(0.85f, 0.65f, 0.3f), 0.7f, 0.5f);
                for (int s = 0; s < 2; s++)
                {
                    var c = Part(PrimitiveType.Cylinder, Vector3.zero, new Vector3(1.5f, 0.05f, 1.5f), brass);
                    c.localRotation = Quaternion.Euler(0f, 0f, 90f);
                }
                break;
            }
            case "bow":
            {
                Material wood = gm.Mat("Guitar Wood", new Color(0.55f, 0.3f, 0.15f), 0.5f);
                Material hair = gm.Mat("Bow Hair", new Color(0.97f, 0.95f, 0.88f), 0.4f);
                Part(PrimitiveType.Cube, new Vector3(0f, 0.15f, 0f), new Vector3(3.2f, 0.09f, 0.09f), wood);
                Part(PrimitiveType.Cube, new Vector3(0f, -0.05f, 0f), new Vector3(3f, 0.04f, 0.04f), hair);
                Part(PrimitiveType.Cube, new Vector3(1.55f, 0.05f, 0f), new Vector3(0.2f, 0.35f, 0.15f), wood);
                break;
            }
            case "cannon":
            {
                Material metal = gm.Mat("Cannon", new Color(0.85f, 0.25f, 0.4f), 0.6f, 0.3f);
                var barrel = LevelBuilder.Visual("Cannon", PrimitiveType.Cylinder, transform, new Vector3(spec.x + spec.width * 0.5f + 1.2f, 0.6f, 0.5f), new Vector3(0.8f, 0.7f, 0.8f), metal).transform;
                barrel.localRotation = Quaternion.Euler(0f, 0f, 70f);
                break;
            }
        }
    }

    // وين العقبة الحين (حسب نوعها)
    void Animate(float t)
    {
        float k = K, cx = spec.x, w = spec.width;
        bool on = On;
        model.gameObject.SetActive(on || spec.kind == "clam" || spec.kind == "cuckoo");
        switch (spec.kind)
        {
            case "fish":
                // ينط من المويه قدام ويطيح ورا (قوس فوق الطريق)
                model.position = new Vector3(cx + (k - 0.5f) * w, -0.6f + Mathf.Sin(k * Mathf.PI) * 2.6f, Mathf.Lerp(-2.5f, 2.5f, k));
                model.localRotation = Quaternion.Euler(0f, 90f, Mathf.Lerp(50f, -50f, k));
                break;
            case "clam":
            {
                model.position = new Vector3(cx, 0f, 0f);
                // مفتوحة وهي مو شغالة، وتسكر بقوة وقت العقبة
                float open = on ? (k < 0.15f ? 1f - k / 0.15f : 0f) : Mathf.Clamp01((Local - Active) / 0.4f);
                parts[1].localPosition = new Vector3(0f, 0.3f + open * 0.5f, 0.4f * open);
                parts[1].localRotation = Quaternion.Euler(-open * 40f, 0f, 0f);
                break;
            }
            case "pencil":
                // يتدحرج من اليمين لليسار على الطريق
                model.position = new Vector3(cx + w * 0.5f + 2f - k * (w + 4f), 0.25f, -0.5f);
                model.localRotation = Quaternion.Euler(0f, 0f, k * 720f);
                break;
            case "crayons":
                model.position = new Vector3(cx, 4.2f - k * k * 5f, 0f);
                break;
            case "plane":
                model.position = new Vector3(cx + w * 0.5f + 3f - k * (w + 6f), 1.1f + Mathf.Sin(k * 6f) * 0.25f, -0.3f);
                model.localRotation = Quaternion.Euler(0f, 180f, Mathf.Sin(k * 6f) * 10f);
                break;
            case "cuckoo":
            {
                // يطلع من البيت ويدف الطريق
                float push = on ? Mathf.Sin(k * Mathf.PI) : 0f;
                model.position = new Vector3(cx + w * 0.5f + 0.4f - push * (w + 0.6f), 1.1f, 0f);
                break;
            }
            case "steam":
                if (on && Random.value < 0.6f)
                {
                    var puff = LevelBuilder.Visual("Steam", PrimitiveType.Sphere, null, new Vector3(cx + Random.Range(-0.3f, 0.3f), 0.2f + spec.y, 0.3f), Vector3.one * Random.Range(0.4f, 0.7f), GameManager.Instance.Mat("Steam Puff", new Color(0.95f, 0.95f, 1f), 0.2f), false);
                    var fx = puff.AddComponent<FxParticle>();
                    fx.velocity = new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(3.5f, 5f), 0f);
                    fx.grow = 1.5f;
                    fx.life = 0.7f;
                }
                break;
            case "bat":
                model.position = new Vector3(cx + w * 0.5f + 3f - k * (w + 6f), 1.2f + Mathf.Sin(k * 12f) * 0.4f, -0.3f);
                parts[1].localRotation = Quaternion.Euler(0f, 0f, 20f + Mathf.Sin(t * 30f) * 30f);
                parts[2].localRotation = Quaternion.Euler(0f, 0f, -20f - Mathf.Sin(t * 30f) * 30f);
                break;
            case "cymbals":
            {
                float gap = on ? Mathf.Abs(Mathf.Cos(k * Mathf.PI)) * 1.6f : 1.6f;
                model.position = new Vector3(cx, 1f, 0f);
                parts[0].localPosition = new Vector3(-gap * 0.5f - 0.05f, 0f, 0f);
                parts[1].localPosition = new Vector3(gap * 0.5f + 0.05f, 0f, 0f);
                model.gameObject.SetActive(true);
                break;
            }
            case "bow":
                model.position = new Vector3(cx + w * 0.5f + 2f - k * (w + 4f), 1f, -0.2f);
                break;
            case "cannon":
                if (on && Random.value < 0.5f)
                {
                    var bit = LevelBuilder.Visual("Confetti", PrimitiveType.Cube, null, new Vector3(cx + w * 0.5f + 1f, 1f + spec.y, -0.3f), Vector3.one * 0.15f, GameManager.Instance.Mat("Crayon Fall " + Random.Range(0, 4), MusicFeatures.KeyColors[Random.Range(0, 4)], 0.4f), false);
                    bit.transform.rotation = Random.rotation;
                    var fx = bit.AddComponent<FxParticle>();
                    fx.velocity = new Vector3(Random.Range(-9f, -5f), Random.Range(0f, 3f), Random.Range(-1f, 1f));
                    fx.gravity = 4f;
                    fx.spin = Random.insideUnitSphere * 500f;
                    fx.life = 0.8f;
                }
                break;
        }
        model.position += Vector3.up * spec.y;
    }

    string StartSound()
    {
        switch (spec.kind)
        {
            case "fish": case "clam": return "splash";
            case "pencil": return "clack";
            case "crayons": return "crayon";
            case "plane": return "whoosh";
            case "cuckoo": return "cuckoo";
            case "steam": return "steam";
            case "bat": return "squeak";
            case "cymbals": return "cymbal";
            case "bow": return "string_buzz";
            case "cannon": return "applause";
        }
        return null;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        Animate(Time.time);
        bool on = On;
        if (on && !played)
        {
            played = true;
            string snd = StartSound();
            RowaidaController r0 = gm.Rowaida;
            bool near = r0 != null && Mathf.Abs(r0.transform.position.x - spec.x) < 14f;
            if (snd == "squeak") { if (near) gm.Sound.PlaySqueak(2f); }
            else if (snd != null && near) LevelSfx.Play(snd, 0.6f);
        }
        if (!on) played = false;

        RowaidaController rw = gm.Rowaida;
        if (rw == null) return;
        Vector3 p = rw.transform.position;
        float x0 = spec.x - spec.width * 0.5f, x1 = spec.x + spec.width * 0.5f;
        // صدمتها؟ ترجعها لورا
        if (on && K > 0.1f && K < 0.9f && p.x > x0 - 0.3f && p.x < x1 + 0.3f && p.y - spec.y < 2.4f && p.y - spec.y > -1f && !rw.IsKnocked)
        {
            rw.Knockback(-1.2f);
            Fx.Sparkle(p, new Color(1f, 0.8f, 0.4f), 10);
        }
        // تستنى قبل المنطقة إذا العقبة شغالة أو جاية قبل ما تعدي
        float cross = (spec.width + 1.6f) / rw.walkSpeed;
        bool before = p.x > x0 - 1.6f && p.x < x0 - 0.4f;
        bool wait = before && Enabled && (on || UntilOn < cross) && gm.IsPlaying;
        if (wait) { rw.Held = true; rw.CarryX = 0f; holding = true; }
        else if (holding) { rw.Held = false; holding = false; }
    }
}
