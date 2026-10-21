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
    // كم ثانية تكون العقبة على الطريق (القلم أطول لأنه يتدحرج من بعيد على مهله)
    float Active => spec.kind == "pencil" ? (spec.width + 6f) / 4f : 1.2f;

    float Local => Mathf.Repeat(Time.time - start + spec.phase, spec.period);
    // العقبة على الطريق من 0 لين Active
    bool On => Local < Active && Enabled;
    float UntilOn => Local < Active ? 0f : spec.period - Local;
    float K => Mathf.Clamp01(Local / Active);   // تقدم الحركة وهي شغالة
    bool Enabled => spec.kind != "bat" || DayNight.Night;

    public static void Create(Transform root, HazardSpec spec, GameManager gm)
    {
        // الخفافيش تطير لحالها بشكل عشوائي (مو بوقت ثابت)
        if (spec.kind == "bat")
        {
            for (int i = 0; i < 3; i++) WanderBat.Create(root, spec, i, gm);
            return;
        }
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
                var body = Part(PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.6f, 0.9f, 0.6f), yellow);
                body.localRotation = Quaternion.Euler(90f, 0f, 0f);
                var er = Part(PrimitiveType.Cylinder, new Vector3(0f, 0f, 0.98f), new Vector3(0.6f, 0.12f, 0.6f), pink);
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
            case "stapler":
            {
                // دباسة عملاقة: القاعدة على الأرض، والذراع يتحرك من مفصل ورا
                Material red = gm.Mat("Stapler Red", new Color(0.9f, 0.22f, 0.25f), 0.6f, 0.2f);
                Material steel = gm.Mat("Sharpener Steel", new Color(0.8f, 0.82f, 0.86f), 0.9f, 0.8f);
                LevelBuilder.Visual("Stapler Base", PrimitiveType.Cube, transform, new Vector3(spec.x, 0.04f, 0.2f), new Vector3(1.5f, 0.08f, 3.6f), steel, false);
                LevelBuilder.Visual("Stapler Anvil", PrimitiveType.Cube, transform, new Vector3(spec.x, 0.09f, -1.3f), new Vector3(0.8f, 0.04f, 0.6f), red, false);
                LevelBuilder.Visual("Hinge", PrimitiveType.Cube, transform, new Vector3(spec.x, 0.5f, 1.8f), new Vector3(1.3f, 0.95f, 0.5f), red);
                // الذراع (تحت المفصل لليسار)
                Part(PrimitiveType.Cube, new Vector3(0f, 0.05f, -1.75f), new Vector3(1.3f, 0.45f, 3.6f), red);
                Part(PrimitiveType.Cube, new Vector3(0f, -0.25f, -1.75f), new Vector3(1f, 0.15f, 3.4f), steel);
                Part(PrimitiveType.Cube, new Vector3(0f, 0.12f, -3.4f), new Vector3(1.35f, 0.5f, 0.4f), red);
                break;
            }
            case "metronome":
            {
                // مترونوم كبير معلق: إطار خشب فوق الطريق، وذراع بثقل يتأرجح
                Material wood = gm.Mat("Guitar Wood", new Color(0.55f, 0.3f, 0.15f), 0.5f);
                Material brass = gm.Mat("Gear Brass", new Color(0.85f, 0.65f, 0.3f), 0.7f, 0.5f);
                Material dark = gm.Mat("Piano Wood", new Color(0.15f, 0.12f, 0.14f), 0.7f);
                for (int s = -1; s <= 1; s += 2)
                {
                    var leg = LevelBuilder.Visual("Frame Leg", PrimitiveType.Cube, transform, new Vector3(spec.x + s * 2.6f, 3.5f, 1.4f), new Vector3(0.35f, 7f, 0.35f), wood).transform;
                    leg.localRotation = Quaternion.Euler(0f, 0f, s * 6f);
                }
                LevelBuilder.Visual("Frame Top", PrimitiveType.Cube, transform, new Vector3(spec.x, 6.9f, 1f), new Vector3(5.2f, 0.5f, 1.2f), wood);
                LevelBuilder.Visual("Pivot", PrimitiveType.Cylinder, transform, new Vector3(spec.x, 6.6f, 0.3f), new Vector3(0.4f, 0.5f, 0.4f), brass).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                // الذراع والثقل (تحت المحور)
                Part(PrimitiveType.Cube, new Vector3(0f, -2.7f, 0f), new Vector3(0.14f, 5.4f, 0.14f), brass);
                Part(PrimitiveType.Cube, new Vector3(0f, -4.7f, 0f), new Vector3(1f, 0.8f, 0.6f), dark);
                Part(PrimitiveType.Cube, new Vector3(0f, -4.7f, -0.31f), new Vector3(0.7f, 0.12f, 0.02f), brass);
                Part(PrimitiveType.Sphere, new Vector3(0f, -5.45f, 0f), Vector3.one * 0.4f, brass);
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
                model.position = new Vector3(cx + w * 0.5f + 3.5f - k * (w + 6f), 0.3f, 0f);
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
            case "stapler":
            {
                // تنفتح فوق، وتنطبق بقوة على الطريق، وترجع ترتفع
                float open = on ? (k < 0.12f ? 1f - k / 0.12f : k > 0.85f ? (k - 0.85f) / 0.15f : 0f) : 1f;
                float shake = !on && UntilOn < 0.6f ? Mathf.Sin(t * 50f) * 3f : 0f;
                model.position = new Vector3(cx, 0.95f, 1.8f);
                model.localRotation = Quaternion.Euler(open * 38f + shake, 0f, 0f);
                model.gameObject.SetActive(true);
                if (on && k > 0.12f && k < 0.2f && Random.value < 0.5f)
                {
                    var bit = LevelBuilder.Visual("Staple", PrimitiveType.Cube, null, new Vector3(cx + Random.Range(-1f, 1f), 0.15f + spec.y, Random.Range(-0.6f, 0.6f)), new Vector3(0.3f, 0.05f, 0.05f), GameManager.Instance.Mat("Sharpener Steel", new Color(0.8f, 0.82f, 0.86f), 0.9f, 0.8f), false);
                    var fx = bit.AddComponent<FxParticle>();
                    fx.velocity = new Vector3(Random.Range(-3f, 3f), Random.Range(2f, 4f), Random.Range(-1f, 1f));
                    fx.gravity = 12f; fx.spin = Random.insideUnitSphere * 600f; fx.life = 0.6f;
                }
                break;
            }
            case "metronome":
            {
                // يتأرجح يمين ويسار، ويمر من الطريق مرة كل دورة
                float x = Mathf.Repeat(Time.time - start + spec.phase - Active * 0.5f, spec.period);
                int cycle = Mathf.FloorToInt((Time.time - start + spec.phase - Active * 0.5f) / spec.period);
                float side = cycle % 2 == 0 ? 1f : -1f;
                model.gameObject.SetActive(true);
                model.position = new Vector3(cx, 6.6f, 0f);
                model.localRotation = Quaternion.Euler(0f, 0f, side * 58f * Mathf.Sin(Mathf.PI * x / spec.period));
                break;
            }
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
            case "metronome": return "tock";
            case "stapler": return "clank";
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
            // البخار: دخان كثيف يغطي الرؤية شوي
            if (spec.kind == "steam" && near && Mathf.Abs(r0.transform.position.x - spec.x) < 9f)
                for (int i = 0; i < 7; i++)
                    SmokeCloud.Puff(new Vector3(spec.x + Random.Range(-4f, 6f), spec.y + Random.Range(0.3f, 3.8f), Random.Range(-3f, -0.8f)), Random.Range(2.8f, 5f));
        }
        if (!on) played = false;

        RowaidaController rw = gm.Rowaida;
        if (rw == null) return;
        Vector3 p = rw.transform.position;
        float x0 = spec.x - spec.width * 0.5f, x1 = spec.x + spec.width * 0.5f;
        // القلم: يتدحرج لها ورويدا تنط من فوقه (ولو لمسها يرجعها لورا)
        if (spec.kind == "pencil")
        {
            float gap = model.position.x - p.x;
            if (on && gm.IsPlaying && gap > 1.1f && gap < 2.4f && Mathf.Abs(p.y - spec.y - 0.7f) < 0.4f) rw.Jump(true);
            if (on && Mathf.Abs(gap) < 0.45f && p.y - spec.y < 1.05f && p.y - spec.y > -0.5f && !rw.IsKnocked)
            {
                rw.Knockback(-1.2f);
                LevelSfx.Play("thud", 0.7f);
                Fx.Sparkle(p, new Color(1f, 0.8f, 0.4f), 10);
            }
            return;
        }
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

// دخان كثيف يطلع من فتحات البخار ويغطي الشاشة شوي (بين الكاميرا ورويدا)، ويخف مع الوقت
public class SmokeCloud : MonoBehaviour
{
    static Texture2D tex;
    Material mat;
    Vector3 drift;
    float age, life, maxAlpha, baseSize;

    static Texture2D Tex()
    {
        if (tex != null) return tex;
        tex = new Texture2D(64, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                float dx = (x - 31.5f) / 31.5f, dy = (y - 31.5f) / 31.5f;
                // دايرة ناعمة مع شوية تكتلات عشان تبان مثل الدخان
                float n = Mathf.PerlinNoise(x * 0.12f, y * 0.12f) * 0.35f;
                float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy) - n * 0.5f + 0.15f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
            }
        tex.Apply();
        return tex;
    }

    public static void Puff(Vector3 pos, float size)
    {
        var go = LevelBuilder.Visual("Smoke", PrimitiveType.Quad, null, pos, Vector3.one * size, null, false);
        var s = go.AddComponent<SmokeCloud>();
        s.mat = NightSky.Transparent("Smoke", Tex(), new Color(0.93f, 0.93f, 0.95f, 0f));
        go.GetComponent<MeshRenderer>().sharedMaterial = s.mat;
        s.drift = new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(0.2f, 0.6f), 0f);
        s.life = Random.Range(3.5f, 5f);
        s.maxAlpha = Random.Range(0.75f, 0.95f);
        s.baseSize = size;
        go.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
    }

    void Update()
    {
        age += Time.deltaTime;
        float k = age / life;
        float a = k < 0.15f ? k / 0.15f : 1f - Mathf.Clamp01((k - 0.5f) / 0.5f);
        mat.SetColor("_BaseColor", new Color(0.93f, 0.93f, 0.95f, a * maxAlpha));
        transform.position += drift * Time.deltaTime;
        transform.localScale = Vector3.one * baseSize * (1f + k * 0.5f);
        transform.Rotate(0f, 0f, 8f * Time.deltaTime);
        if (age >= life) { Destroy(mat); Destroy(gameObject); }
    }
}

// خفاش يطير بشكل عشوائي بالليل حول مكانه، وأحيانًا ينقض على رويدا. بالنهار نايم معلق بالمقلوب
public class WanderBat : MonoBehaviour
{
    Vector3 home, target, vel;
    Transform wingL, wingR, body;
    float nextPick, nextHit, seed;
    bool diving;

    public static void Create(Transform root, HazardSpec spec, int index, GameManager gm)
    {
        var go = new GameObject("Bat");
        go.transform.SetParent(root, false);
        var b = go.AddComponent<WanderBat>();
        b.seed = index * 1.7f + spec.x;
        b.home = new Vector3(spec.x - 2f + index * 2f, spec.y + 5.2f + (index % 2) * 0.4f, 0.4f);
        go.transform.position = b.home;
        b.target = b.home;
        Material dark = gm.Mat("Bat", new Color(0.18f, 0.12f, 0.25f), 0.3f);
        Material eye = gm.Mat("Bat Eye", new Color(1.5f, 1.3f, 0.3f), 0.5f);
        b.body = new GameObject("Body").transform;
        b.body.SetParent(go.transform, false);
        LevelBuilder.Visual("Torso", PrimitiveType.Sphere, b.body, Vector3.zero, new Vector3(0.45f, 0.42f, 0.38f), dark);
        for (int s = -1; s <= 1; s += 2)
        {
            LevelBuilder.Visual("Ear", PrimitiveType.Cube, b.body, new Vector3(s * 0.12f, 0.24f, 0f), new Vector3(0.08f, 0.16f, 0.05f), dark, false).transform.localRotation = Quaternion.Euler(0f, 0f, s * -15f);
            LevelBuilder.Visual("Eye", PrimitiveType.Sphere, b.body, new Vector3(s * 0.09f, 0.06f, -0.18f), Vector3.one * 0.09f, eye, false);
            var wing = new GameObject("Wing").transform;
            wing.SetParent(b.body, false);
            wing.localPosition = new Vector3(s * 0.15f, 0.05f, 0f);
            LevelBuilder.Visual("Membrane", PrimitiveType.Cube, wing, new Vector3(s * 0.35f, 0f, 0f), new Vector3(0.65f, 0.05f, 0.38f), dark, false);
            LevelBuilder.Visual("Tip", PrimitiveType.Cube, wing, new Vector3(s * 0.62f, -0.08f, 0f), new Vector3(0.18f, 0.05f, 0.3f), dark, false).transform.localRotation = Quaternion.Euler(0f, 0f, s * 30f);
            if (s < 0) b.wingL = wing; else b.wingR = wing;
        }
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time, dt = Time.deltaTime;
        Vector3 p = transform.position;

        if (!DayNight.Night)
        {
            // بالنهار: يرجع لمكانه وينام معلق
            diving = false;
            p = Vector3.Lerp(p, home, dt * 3f);
            transform.position = p;
            body.localRotation = Quaternion.Slerp(body.localRotation, Quaternion.Euler(0f, 0f, 180f), dt * 6f);
            wingL.localRotation = Quaternion.Euler(0f, 0f, -70f);
            wingR.localRotation = Quaternion.Euler(0f, 0f, 70f);
            body.localScale = Vector3.one * (1f + Mathf.Sin(t * 2f + seed) * 0.03f);
            return;
        }

        RowaidaController r = gm.Rowaida;
        Vector3 rp = r != null ? r.transform.position + Vector3.up * 0.6f : home;
        bool close = r != null && Mathf.Abs(rp.x - home.x) < 6f;
        if (t >= nextPick)
        {
            // يختار مكان عشوائي جديد، وأحيانًا ينقض عليها
            diving = close && Random.value < 0.35f;
            target = diving ? rp + new Vector3(Random.Range(-0.3f, 0.3f), 0f, 0f)
                : home + new Vector3(Random.Range(-4.5f, 4.5f), Random.Range(-4.2f, -0.5f), Random.Range(-0.6f, 0.6f));
            nextPick = t + (diving ? 1.1f : Random.Range(0.4f, 1.3f));
            if (diving && Random.value < 0.6f) gm.Sound.PlaySqueak(2.2f);
        }
        if (diving && r != null) target = Vector3.Lerp(target, rp, dt * 2f);
        Vector3 want = (target - p);
        float speed = diving ? 6f : 3.5f;
        vel = Vector3.Lerp(vel, want.normalized * Mathf.Min(speed, want.magnitude * 3f), dt * 4f);
        vel += new Vector3(Mathf.PerlinNoise(t * 1.5f, seed) - 0.5f, Mathf.PerlinNoise(seed, t * 1.5f) - 0.5f, 0f) * dt * 20f;
        p += vel * dt;
        p.y = Mathf.Max(p.y, home.y - 5f);
        transform.position = p;

        body.localRotation = Quaternion.Slerp(body.localRotation, Quaternion.Euler(0f, 0f, Mathf.Clamp(-vel.x * 6f, -30f, 30f)), dt * 8f);
        body.localScale = Vector3.one;
        float flap = Mathf.Sin(t * (diving ? 40f : 26f) + seed) * 45f;
        wingL.localRotation = Quaternion.Euler(0f, 0f, flap);
        wingR.localRotation = Quaternion.Euler(0f, 0f, -flap);

        // لمسها؟ ترجع لورا
        if (r != null && gm.IsPlaying && t >= nextHit && (rp - p).sqrMagnitude < 0.55f * 0.55f && !r.IsKnocked)
        {
            nextHit = t + 1.5f;
            r.Knockback(-1f);
            gm.Sound.PlaySqueak(2.5f);
            Fx.Sparkle(p, new Color(0.6f, 0.4f, 0.8f), 10);
            diving = false;
            nextPick = t + 0.2f;
        }
    }
}
