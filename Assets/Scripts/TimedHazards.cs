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
    float Active => spec.kind == "pencil" || spec.kind == "rollinglog" ? (spec.width + 6f) / 4f : 1.2f;

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
            case "tesla":
            {
                // كويلين على جنبين الطريق، وصاعقة بينهم
                Material copper = gm.Mat("Battery Copper", new Color(0.95f, 0.6f, 0.25f), 0.8f, 0.6f);
                Material steel = gm.Mat("Spoon Steel", new Color(0.82f, 0.84f, 0.88f), 0.95f, 0.85f);
                for (int s = -1; s <= 1; s += 2)
                {
                    Vector3 cp = new Vector3(spec.x + s * (spec.width * 0.5f + 0.6f), 0f, 0.6f);
                    LevelBuilder.Visual("Coil Base", PrimitiveType.Cylinder, transform, cp + Vector3.up * 0.2f, new Vector3(0.8f, 0.2f, 0.8f), steel);
                    for (int r = 0; r < 6; r++) LevelBuilder.Visual("Coil Ring", PrimitiveType.Cylinder, transform, cp + Vector3.up * (0.5f + r * 0.28f), new Vector3(0.55f, 0.06f, 0.55f), copper, false);
                    LevelBuilder.Visual("Coil Core", PrimitiveType.Cylinder, transform, cp + Vector3.up * 1.2f, new Vector3(0.3f, 0.9f, 0.3f), steel);
                    LevelBuilder.Visual("Coil Ball", PrimitiveType.Sphere, transform, cp + Vector3.up * 2.4f, Vector3.one * 0.6f, steel);
                }
                Material bolt = gm.Mat("Bolt", new Color(1.8f, 2f, 3f), 0.5f);
                for (int i = 0; i < 8; i++) Part(PrimitiveType.Cube, Vector3.zero, new Vector3(0.1f, 0.1f, 0.1f), bolt);
                break;
            }
            case "drip":
            {
                // قطارة زجاج كبيرة معلقة فوق الطريق تنقط حمض أخضر
                Material glass = LabFeatures.Glass(0.35f);
                Material acidM = gm.Mat("Acid Glow", new Color(0.6f, 1.6f, 0.4f), 0.6f);
                Material bulb = gm.Mat("Pipette Bulb", new Color(0.95f, 0.35f, 0.4f), 0.5f);
                LevelBuilder.Visual("Pipette Tube", PrimitiveType.Cylinder, transform, new Vector3(spec.x, 5.2f, 0f), new Vector3(0.35f, 1.4f, 0.35f), glass, false);
                LevelBuilder.Visual("Pipette Acid", PrimitiveType.Cylinder, transform, new Vector3(spec.x, 4.7f, 0f), new Vector3(0.25f, 0.8f, 0.25f), acidM, false);
                LevelBuilder.Visual("Pipette Bulb", PrimitiveType.Sphere, transform, new Vector3(spec.x, 6.9f, 0f), new Vector3(0.7f, 0.9f, 0.7f), bulb);
                LevelBuilder.Visual("Puddle", PrimitiveType.Cylinder, transform, new Vector3(spec.x, 0.02f, 0f), new Vector3(spec.width + 0.4f, 0.02f, 1.4f), acidM, false);
                Part(PrimitiveType.Sphere, Vector3.zero, new Vector3(0.35f, 0.5f, 0.35f), acidM);
                break;
            }
            case "bubbleflask":
            {
                // دورق كبير يغلي جنب الطريق: فقاعة تكبر فوق الطريق وتنفجر
                Material glass = LabFeatures.Glass(0.3f);
                Material potion = gm.Mat("Potion Pink", new Color(1.3f, 0.45f, 0.9f), 0.7f);
                Vector3 fp = new Vector3(spec.x, 0f, 1.4f);
                LevelBuilder.Visual("Big Flask Liquid", PrimitiveType.Sphere, transform, fp + Vector3.up * 0.8f, new Vector3(1.8f, 1.3f, 1.8f), potion, false);
                LevelBuilder.Visual("Big Flask", PrimitiveType.Sphere, transform, fp + Vector3.up * 1f, Vector3.one * 2.2f, glass, false);
                LevelBuilder.Visual("Big Neck", PrimitiveType.Cylinder, transform, fp + Vector3.up * 2.5f, new Vector3(0.7f, 0.6f, 0.7f), glass, false);
                var bub = LevelBuilder.Visual("Bubble", PrimitiveType.Sphere, model, Vector3.zero, Vector3.one, LabFeatures.Glass(0.45f), false).transform;
                parts.Add(bub);
                break;
            }
            case "flour":
            {
                // كيس طحين عملاق ورا الطريق: ينهز وينفض غيمة طحين كبيرة
                Material sack = gm.Mat("Flour Sack", new Color(0.9f, 0.82f, 0.65f), 0.2f);
                Material printM = gm.Mat("Tower Red", new Color(0.85f, 0.2f, 0.22f), 0.35f);
                Material flourM = gm.Mat("Flour", new Color(0.98f, 0.97f, 0.94f), 0.15f);
                var bag = Part(PrimitiveType.Cube, new Vector3(0f, 1.6f, 0f), new Vector3(2.2f, 3.2f, 1.4f), sack);
                LevelBuilder.Visual("Label", PrimitiveType.Cube, bag, new Vector3(0f, 0.05f, -0.51f), new Vector3(0.6f, 0.25f, 0.02f), printM, false);
                Part(PrimitiveType.Sphere, new Vector3(0f, 3.25f, 0f), new Vector3(1.6f, 0.6f, 1.2f), flourM);
                LevelBuilder.Visual("Flour Pile", PrimitiveType.Sphere, transform, new Vector3(spec.x, 0.05f, 0f), new Vector3(spec.width + 0.6f, 0.25f, 1.8f), flourM, false);
                break;
            }
            case "rollingpin":
            {
                // نشابة عملاقة تتدحرج على الطريق
                Material wood = gm.Mat("Tree Wood", new Color(0.62f, 0.45f, 0.28f), 0.25f);
                Material handle = gm.Mat("Pin Handle", new Color(0.85f, 0.35f, 0.3f), 0.4f);
                var body = Part(PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.9f, 1f, 0.9f), wood);
                body.localRotation = Quaternion.Euler(90f, 0f, 0f);
                for (int s = -1; s <= 1; s += 2)
                {
                    var h = Part(PrimitiveType.Cylinder, new Vector3(0f, 0f, s * 1.35f), new Vector3(0.28f, 0.35f, 0.28f), handle);
                    h.localRotation = Quaternion.Euler(90f, 0f, 0f);
                }
                break;
            }
            case "mousetrap":
            {
                // مصيدة فيران خشب على الطريق بقطعة جبن
                Material wood = gm.Mat("Trap Wood", new Color(0.85f, 0.68f, 0.45f), 0.3f);
                Material steel = gm.Mat("Spoon Steel", new Color(0.82f, 0.84f, 0.88f), 0.95f, 0.85f);
                Material cheese = gm.Mat("Cheese", new Color(1f, 0.82f, 0.25f), 0.4f);
                LevelBuilder.Visual("Trap Base", PrimitiveType.Cube, transform, new Vector3(spec.x, 0.08f, 0f), new Vector3(spec.width + 0.6f, 0.16f, 1.4f), wood);
                LevelBuilder.Visual("Cheese", PrimitiveType.Cube, transform, new Vector3(spec.x + 0.3f, 0.3f, 0.3f), new Vector3(0.4f, 0.25f, 0.35f), cheese, false).transform.localRotation = Quaternion.Euler(0f, 30f, 0f);
                // البار (يتحرك): مفصله بالنص
                Part(PrimitiveType.Cube, new Vector3(-(spec.width + 0.4f) * 0.25f, 0f, 0f), new Vector3((spec.width + 0.4f) * 0.5f, 0.06f, 1.3f), steel);
                break;
            }
            case "pepper":
            {
                // علبة فلفل فوق الطريق تنهز وتنثر فلفل
                Material glass = gm.Mat("Shaker Glass", new Color(0.92f, 0.95f, 0.97f), 0.9f);
                Material capM = gm.Mat("Sharpener Steel", new Color(0.8f, 0.82f, 0.86f), 0.9f, 0.8f);
                Material pep = gm.Mat("Pepper Dark", new Color(0.2f, 0.18f, 0.16f), 0.3f);
                Part(PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.7f, 0.55f, 0.7f), glass);
                Part(PrimitiveType.Cylinder, new Vector3(0f, -0.25f, 0f), new Vector3(0.6f, 0.3f, 0.6f), pep);
                Part(PrimitiveType.Sphere, new Vector3(0f, -0.65f, 0f), new Vector3(0.72f, 0.3f, 0.72f), capM);
                break;
            }
            case "toast":
            {
                // محمصة ورا الطريق، والتوست ينط منها ويطير فوق الطريق
                Material body = gm.Mat("Toaster", new Color(0.85f, 0.88f, 0.92f), 0.9f, 0.7f);
                Material bread = gm.Mat("Toast", new Color(0.95f, 0.75f, 0.45f), 0.3f);
                Material crust = gm.Mat("Toast Crust", new Color(0.7f, 0.45f, 0.2f), 0.3f);
                Vector3 tp = new Vector3(spec.x + spec.width * 0.5f + 1.1f, 0.6f, 1.3f);
                LevelBuilder.Visual("Toaster", PrimitiveType.Cube, transform, tp, new Vector3(1.4f, 1.2f, 0.9f), body);
                LevelBuilder.Visual("Slot", PrimitiveType.Cube, transform, tp + new Vector3(0f, 0.6f, 0f), new Vector3(1f, 0.04f, 0.2f), black, false);
                LevelBuilder.Visual("Lever", PrimitiveType.Cube, transform, tp + new Vector3(0.72f, 0.2f, 0f), new Vector3(0.12f, 0.12f, 0.25f), gm.Mat("Tower Red", new Color(0.85f, 0.2f, 0.22f), 0.35f), false);
                Part(PrimitiveType.Cube, Vector3.zero, new Vector3(0.8f, 0.8f, 0.12f), crust);
                Part(PrimitiveType.Cube, new Vector3(0f, 0f, -0.01f), new Vector3(0.68f, 0.68f, 0.13f), bread);
                break;
            }
            case "flame":
            {
                // عين بوتجاز على الطريق، ونار تطلع منها
                Material ring = gm.Mat("Stove Black", new Color(0.15f, 0.14f, 0.16f), 0.6f, 0.3f);
                LevelBuilder.Visual("Burner", PrimitiveType.Cylinder, transform, new Vector3(spec.x, 0.03f, 0f), new Vector3(spec.width + 0.3f, 0.04f, 1.6f), ring, false);
                LevelBuilder.Visual("Burner Cap", PrimitiveType.Cylinder, transform, new Vector3(spec.x, 0.06f, 0f), new Vector3(0.6f, 0.04f, 0.6f), gm.Mat("Sharpener Steel", new Color(0.8f, 0.82f, 0.86f), 0.9f, 0.8f), false);
                Material fo = gm.Mat("Flame Orange", new Color(2f, 0.8f, 0.2f), 0.5f);
                Material fy = gm.Mat("Flame Yellow", new Color(2.2f, 1.8f, 0.5f), 0.5f);
                for (int i = 0; i < 6; i++)
                    Part(PrimitiveType.Capsule, new Vector3(-spec.width * 0.4f + i * spec.width * 0.16f, 0f, (i % 2) * 0.3f - 0.15f), new Vector3(0.28f, 0.5f, 0.28f), i % 2 == 0 ? fo : fy);
                break;
            }
            case "chocofall":
            {
                // صنبور شوكولاتة فوق، وشلال شوكولاتة ينزل على الطريق
                Material choc = gm.Mat("Choco Drip", new Color(0.4f, 0.22f, 0.13f), 0.7f);
                Material gold = gm.Mat("Gear Brass", new Color(0.85f, 0.65f, 0.3f), 0.7f, 0.5f);
                LevelBuilder.Visual("Spout", PrimitiveType.Cylinder, transform, new Vector3(spec.x, 5.3f, 0f), new Vector3(spec.width + 0.4f, 0.25f, 1.2f), gold);
                LevelBuilder.Visual("Pipe", PrimitiveType.Cylinder, transform, new Vector3(spec.x, 6.2f, 0.4f), new Vector3(0.4f, 0.8f, 0.4f), gold);
                LevelBuilder.Visual("Puddle", PrimitiveType.Cylinder, transform, new Vector3(spec.x, 0.02f, 0f), new Vector3(spec.width + 0.6f, 0.02f, 1.6f), choc, false);
                Part(PrimitiveType.Cube, new Vector3(0f, 2.6f, 0f), new Vector3(spec.width, 5.2f, 0.9f), choc);
                break;
            }
            case "sprinkles":
            {
                // غيمة غزل بنات فوق الطريق تمطر حبيبات ملونة
                Material cotton = gm.Mat("Cotton Candy", new Color(1f, 0.8f, 0.9f), 0.2f);
                for (int i = 0; i < 4; i++)
                    LevelBuilder.Visual("Cloud Puff", PrimitiveType.Sphere, transform, new Vector3(spec.x + (i - 1.5f) * 0.6f, 4.6f + Mathf.Sin(i * 2f) * 0.2f, 0.2f), Vector3.one * (1f + (i % 2) * 0.3f), cotton, false);
                break;
            }
            case "kettle":
            {
                // إبريق على رف يمين الطريق، وبخار يطلع من بوزه بقوة على الطريق
                Material kettle = gm.Mat("Kettle", new Color(0.3f, 0.75f, 0.85f), 0.8f, 0.3f);
                Material shelf = gm.Mat("Shelf Wood", new Color(0.75f, 0.55f, 0.35f), 0.3f);
                Vector3 kp = new Vector3(spec.x + spec.width * 0.5f + 1.3f, 1.4f, 0.4f);
                LevelBuilder.Visual("Shelf", PrimitiveType.Cube, transform, kp + new Vector3(0f, -0.6f, 0.3f), new Vector3(1.6f, 0.15f, 1.4f), shelf);
                LevelBuilder.Visual("Shelf Leg", PrimitiveType.Cube, transform, kp + new Vector3(0f, -1.1f, 0.6f), new Vector3(0.15f, 1f, 0.15f), shelf);
                LevelBuilder.Visual("Kettle", PrimitiveType.Sphere, transform, kp, new Vector3(1f, 0.9f, 0.9f), kettle);
                var sp = LevelBuilder.Visual("Spout", PrimitiveType.Cylinder, transform, kp + new Vector3(-0.6f, 0.15f, 0f), new Vector3(0.16f, 0.35f, 0.16f), kettle).transform;
                sp.localRotation = Quaternion.Euler(0f, 0f, 60f);
                LevelBuilder.Visual("Knob", PrimitiveType.Sphere, transform, kp + new Vector3(0f, 0.5f, 0f), Vector3.one * 0.18f, gm.Mat("Tower Red", new Color(0.85f, 0.2f, 0.22f), 0.35f), false);
                break;
            }
            case "oil":
            {
                // مقلاة زيت ورا الطريق تطرطش قطرات حارة فوقه
                Material iron = gm.Mat("Pan Iron", new Color(0.22f, 0.22f, 0.25f), 0.6f, 0.4f);
                Material oilM = gm.Mat("Hot Oil", new Color(1f, 0.85f, 0.3f), 0.95f);
                LevelBuilder.Visual("Oil Pan", PrimitiveType.Cylinder, transform, new Vector3(spec.x, 0.15f, 1.3f), new Vector3(1.6f, 0.1f, 1.2f), iron);
                LevelBuilder.Visual("Oil", PrimitiveType.Cylinder, transform, new Vector3(spec.x, 0.24f, 1.3f), new Vector3(1.4f, 0.02f, 1f), oilM, false);
                for (int i = 0; i < 3; i++) Part(PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.2f, oilM);
                break;
            }
            case "mole":
            {
                // كومة تراب وحفرة على الطريق، والخلد يطلع منها فجأة
                Material dirt = gm.Mat("Forest Soil", new Color(0.45f, 0.3f, 0.2f), 0.15f);
                Material holeM = gm.Mat("Monster Pupil", new Color(0.05f, 0.04f, 0.06f), 0.9f);
                Material fur = gm.Mat("Mole Fur", new Color(0.32f, 0.27f, 0.3f), 0.35f);
                Material pink = gm.Mat("Bunny Pink", new Color(1f, 0.7f, 0.75f), 0.3f);
                LevelBuilder.Visual("Mound", PrimitiveType.Sphere, transform, new Vector3(spec.x, 0.02f, 0f), new Vector3(spec.width + 0.4f, 0.3f, 1.6f), dirt, false);
                LevelBuilder.Visual("Hole", PrimitiveType.Cylinder, transform, new Vector3(spec.x, 0.15f, -0.1f), new Vector3(0.75f, 0.01f, 0.75f), holeM, false);
                Part(PrimitiveType.Sphere, Vector3.zero, new Vector3(0.7f, 0.85f, 0.6f), fur);
                Part(PrimitiveType.Sphere, new Vector3(0f, 0.12f, -0.3f), Vector3.one * 0.16f, pink);
                Part(PrimitiveType.Sphere, new Vector3(-0.12f, 0.28f, -0.25f), Vector3.one * 0.06f, black);
                Part(PrimitiveType.Sphere, new Vector3(0.12f, 0.28f, -0.25f), Vector3.one * 0.06f, black);
                Part(PrimitiveType.Sphere, new Vector3(-0.3f, -0.15f, -0.2f), new Vector3(0.22f, 0.1f, 0.15f), pink);
                Part(PrimitiveType.Sphere, new Vector3(0.3f, -0.15f, -0.2f), new Vector3(0.22f, 0.1f, 0.15f), pink);
                break;
            }
            case "rollinglog":
            {
                Material bark = gm.Mat("Tree Bark", new Color(0.5f, 0.33f, 0.2f), 0.2f);
                Material ring = gm.Mat("Log Rings", new Color(0.9f, 0.75f, 0.5f), 0.25f);
                var body = Part(PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.7f, 0.85f, 0.7f), bark);
                body.localRotation = Quaternion.Euler(90f, 0f, 0f);
                for (int s = -1; s <= 1; s += 2)
                {
                    var end = Part(PrimitiveType.Cylinder, new Vector3(0f, 0f, s * 0.86f), new Vector3(0.62f, 0.02f, 0.62f), ring);
                    end.localRotation = Quaternion.Euler(90f, 0f, 0f);
                }
                Part(PrimitiveType.Cube, new Vector3(0f, 0.36f, 0.2f), new Vector3(0.08f, 0.12f, 0.08f), bark);
                break;
            }
            case "pinecone":
            {
                // غصن صنوبر فوق الطريق، ومنه تطيح أكواز
                Material bark = gm.Mat("Tree Bark", new Color(0.5f, 0.33f, 0.2f), 0.2f);
                Material pine = gm.Mat("Pine Needles", new Color(0.2f, 0.5f, 0.32f), 0.3f);
                Material cone = gm.Mat("Pine Cone", new Color(0.58f, 0.38f, 0.2f), 0.25f);
                Material coneDark = gm.Mat("Pine Cone Dark", new Color(0.42f, 0.26f, 0.14f), 0.2f);
                LevelBuilder.Visual("Pine Trunk", PrimitiveType.Cylinder, transform, new Vector3(spec.x + 2.4f, 3.2f, 2.3f), new Vector3(0.7f, 4f, 0.7f), bark);
                var br = LevelBuilder.Visual("Pine Branch", PrimitiveType.Cylinder, transform, new Vector3(spec.x + 1f, 5.4f, 0.8f), new Vector3(0.2f, 1.6f, 0.2f), bark).transform;
                br.localRotation = Quaternion.Euler(25f, 0f, 80f);
                for (int i = 0; i < 5; i++)
                {
                    var n = LevelBuilder.Visual("Needles", PrimitiveType.Sphere, transform, new Vector3(spec.x - 0.6f + i * 0.75f, 5.6f + (i % 2) * 0.25f, 0.6f + (i % 3) * 0.3f), new Vector3(1.4f, 0.7f, 1.2f), pine).transform;
                    n.localRotation = Quaternion.Euler(0f, 0f, i * 13f);
                }
                for (int i = 0; i < 4; i++)
                    Part(PrimitiveType.Sphere, new Vector3(0f, 0.22f - i * 0.15f, 0f), new Vector3(0.42f - i * 0.07f, 0.2f, 0.42f - i * 0.07f), i % 2 == 0 ? cone : coneDark);
                break;
            }
            case "acorn":
            {
                // سنجاب على غصن يرمي بندق على الطريق
                Material bark = gm.Mat("Tree Bark", new Color(0.5f, 0.33f, 0.2f), 0.2f);
                Material fur = gm.Mat("Squirrel Fur", new Color(0.82f, 0.45f, 0.2f), 0.35f);
                Material belly = gm.Mat("Squirrel Belly", new Color(1f, 0.88f, 0.72f), 0.3f);
                Material nut = gm.Mat("Acorn", new Color(0.72f, 0.48f, 0.22f), 0.45f);
                Material capM = gm.Mat("Acorn Cap", new Color(0.45f, 0.3f, 0.15f), 0.3f);
                Vector3 sq = new Vector3(spec.x + spec.width * 0.5f + 1.6f, 3.3f, 0.6f);
                var limb = LevelBuilder.Visual("Branch", PrimitiveType.Cylinder, transform, sq + new Vector3(0.6f, -0.35f, 0.3f), new Vector3(0.25f, 1.4f, 0.25f), bark).transform;
                limb.localRotation = Quaternion.Euler(0f, 0f, 90f);
                LevelBuilder.Visual("Tree Trunk", PrimitiveType.Cylinder, transform, sq + new Vector3(2f, -1.2f, 0.8f), new Vector3(0.8f, 2.5f, 0.8f), bark);
                var body = new GameObject("Squirrel").transform;
                body.SetParent(transform, false);
                body.position = sq;
                LevelBuilder.Visual("Body", PrimitiveType.Sphere, body, Vector3.zero, new Vector3(0.45f, 0.55f, 0.4f), fur);
                LevelBuilder.Visual("Belly", PrimitiveType.Sphere, body, new Vector3(-0.1f, -0.02f, -0.1f), new Vector3(0.3f, 0.4f, 0.25f), belly, false);
                LevelBuilder.Visual("Head", PrimitiveType.Sphere, body, new Vector3(-0.12f, 0.38f, 0f), Vector3.one * 0.32f, fur);
                LevelBuilder.Visual("Eye", PrimitiveType.Sphere, body, new Vector3(-0.24f, 0.45f, -0.12f), Vector3.one * 0.07f, black, false);
                for (int s = -1; s <= 1; s += 2)
                    LevelBuilder.Visual("Ear", PrimitiveType.Sphere, body, new Vector3(-0.08f, 0.56f, s * 0.1f), new Vector3(0.08f, 0.14f, 0.06f), fur, false);
                for (int i = 0; i < 4; i++)
                    LevelBuilder.Visual("Tail", PrimitiveType.Sphere, body, new Vector3(0.3f + Mathf.Sin(i * 0.8f) * 0.15f, -0.1f + i * 0.22f, 0.05f), Vector3.one * (0.34f - i * 0.03f), fur, false);
                Part(PrimitiveType.Sphere, Vector3.zero, new Vector3(0.32f, 0.38f, 0.32f), nut);
                Part(PrimitiveType.Sphere, new Vector3(0f, 0.15f, 0f), new Vector3(0.36f, 0.16f, 0.36f), capM);
                break;
            }
            case "speaker":
            {
                // سماعة ضخمة يمين المنطقة، تطلق موجات صوت على الطريق
                Material box = gm.Mat("Piano Wood", new Color(0.15f, 0.12f, 0.14f), 0.7f);
                Material cone = gm.Mat("Speaker Cone", new Color(0.35f, 0.35f, 0.4f), 0.4f);
                Material ring = gm.Mat("Sound Ring", new Color(0.7f, 0.85f, 1.4f), 0.5f);
                var sp = LevelBuilder.Visual("Big Speaker", PrimitiveType.Cube, transform, new Vector3(spec.x + spec.width * 0.5f + 1.3f, 1.6f, 0.9f), new Vector3(1.8f, 3.2f, 1.4f), box).transform;
                LevelBuilder.Visual("Woofer", PrimitiveType.Sphere, sp, new Vector3(-0.5f, -0.18f, 0f), new Vector3(0.1f, 0.42f, 0.7f), cone, false);
                LevelBuilder.Visual("Tweeter", PrimitiveType.Sphere, sp, new Vector3(-0.5f, 0.28f, 0f), new Vector3(0.1f, 0.18f, 0.3f), cone, false);
                // ثلاث حلقات موجة
                for (int i = 0; i < 3; i++)
                {
                    var rp = Part(PrimitiveType.Cylinder, Vector3.zero, new Vector3(1.6f, 0.04f, 1.6f), ring);
                    rp.localRotation = Quaternion.Euler(0f, 0f, 90f);
                }
                break;
            }
            case "paperball":
            {
                Material paper = gm.Mat("Bird Paper", new Color(0.97f, 0.95f, 0.9f), 0.2f);
                Material line = gm.Mat("Ink Scribble", new Color(0.2f, 0.25f, 0.45f), 0.2f);
                Part(PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.7f, paper);
                Part(PrimitiveType.Cube, new Vector3(0.1f, 0.1f, -0.3f), new Vector3(0.4f, 0.04f, 0.1f), line);
                Part(PrimitiveType.Cube, new Vector3(-0.12f, -0.08f, -0.3f), new Vector3(0.3f, 0.04f, 0.1f), line);
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
            case "tesla":
            {
                // صاعقة متعرجة بين الكويلين
                model.position = Vector3.zero;
                model.gameObject.SetActive(on);
                Vector3 a = new Vector3(cx - w * 0.5f - 0.6f, 2.4f, 0.6f), b = new Vector3(cx + w * 0.5f + 0.6f, 2.4f, 0.6f);
                Vector3 prev = a;
                for (int i = 0; i < parts.Count; i++)
                {
                    float u = (i + 1f) / parts.Count;
                    Vector3 next = Vector3.Lerp(a, b, u) + (i < parts.Count - 1 ? new Vector3(0f, Random.Range(-1.6f, 0.4f), Random.Range(-0.6f, 0.2f)) : Vector3.zero);
                    parts[i].localPosition = (prev + next) * 0.5f;
                    parts[i].localScale = new Vector3((next - prev).magnitude, 0.09f, 0.09f);
                    parts[i].localRotation = Quaternion.FromToRotation(Vector3.right, next - prev);
                    prev = next;
                }
                if (on && Random.value < 0.3f) Fx.Sparkle(new Vector3(cx + Random.Range(-w * 0.5f, w * 0.5f), 1.5f + spec.y, 0f), new Color(0.6f, 0.8f, 1f), 3);
                break;
            }
            case "drip":
            {
                // قطرة كبيرة تطيح وتحرق الخطوط
                model.gameObject.SetActive(on);
                model.position = new Vector3(cx, 4f - k * k * 3.9f, 0f);
                if (on && k > 0.9f && Random.value < 0.5f)
                {
                    Bounds burn = new Bounds(new Vector3(cx, 1f + spec.y, 0f), new Vector3(w + 0.4f, 2f, 4f));
                    foreach (InkMark m in Object.FindObjectsByType<InkMark>(FindObjectsSortMode.None))
                    {
                        var mr = m.GetComponent<MeshRenderer>();
                        if (mr == null || !mr.bounds.Intersects(burn)) continue;
                        SmokeCloud.Puff(mr.bounds.center, 0.8f);
                        Object.Destroy(m.gameObject);
                    }
                    SmokeCloud.Puff(new Vector3(cx, 0.4f + spec.y, 0f), 0.9f);
                }
                break;
            }
            case "bubbleflask":
            {
                // فقاعة تطلع من الدورق وتكبر فوق الطريق وتنفجر
                model.position = new Vector3(cx, 0f, 0f);
                model.gameObject.SetActive(on);
                float grow = Mathf.Clamp01(k / 0.75f);
                parts[0].localPosition = Vector3.Lerp(new Vector3(0f, 3f, 1.4f), new Vector3(0f, 1.3f, 0f), grow);
                parts[0].localScale = Vector3.one * (0.4f + grow * 2.2f);
                if (on && k > 0.75f && k < 0.8f)
                    for (int i = 0; i < 10; i++)
                    {
                        var d = LevelBuilder.Visual("Splash", PrimitiveType.Sphere, null, new Vector3(cx, 1.3f + spec.y, 0f) + Random.insideUnitSphere * 0.8f, Vector3.one * 0.18f, GameManager.Instance.Mat("Potion Pink", new Color(1.3f, 0.45f, 0.9f), 0.7f), false);
                        var fx = d.AddComponent<FxParticle>();
                        fx.velocity = Random.insideUnitSphere * 4f + Vector3.up * 2f; fx.gravity = 10f; fx.life = 0.7f;
                    }
                if (k >= 0.8f) parts[0].localScale = Vector3.zero;
                break;
            }
            case "flour":
            {
                model.position = new Vector3(cx + w * 0.5f + 1.4f, 0f, 1.6f);
                model.localRotation = Quaternion.Euler(0f, 0f, on ? Mathf.Sin(k * Mathf.PI) * 35f : Mathf.Sin(t * 1.5f) * 2f);
                model.gameObject.SetActive(true);
                if (on && k < 0.3f && Random.value < 0.6f)
                    SmokeCloud.Puff(new Vector3(cx + Random.Range(-1.5f, 2f), Random.Range(0.3f, 3f) + spec.y, Random.Range(-2.5f, 0.5f)), Random.Range(2f, 3.5f));
                break;
            }
            case "rollingpin":
                model.position = new Vector3(cx + w * 0.5f + 3f - k * (w + 6f), 0.45f, 0f);
                model.localRotation = Quaternion.Euler(0f, 0f, k * (w + 6f) / 0.45f * Mathf.Rad2Deg);
                if (on && Random.value < 0.3f) SmokeCloud.Puff(model.position + new Vector3(0.4f, -0.3f, Random.Range(-0.8f, 0.8f)), 0.6f);
                break;
            case "mousetrap":
            {
                // البار مفتوح لورا، وينطبق بقوة وقت العقبة
                float snapA = on ? (k < 0.08f ? k / 0.08f : k > 0.8f ? 1f - (k - 0.8f) / 0.2f : 1f) : 0f;
                model.position = new Vector3(cx, 0.2f, 0f);
                model.localRotation = Quaternion.Euler(0f, 0f, -170f * (1f - snapA));
                model.gameObject.SetActive(true);
                break;
            }
            case "pepper":
            {
                model.position = new Vector3(cx, 4.4f, 0f);
                model.localRotation = Quaternion.Euler(0f, 0f, 180f + (on ? Mathf.Sin(t * 30f) * 25f : Mathf.Sin(t * 2f) * 5f));
                model.gameObject.SetActive(true);
                if (on && Random.value < 0.9f)
                {
                    var dot = LevelBuilder.Visual("Pepper", PrimitiveType.Cube, null, new Vector3(cx + Random.Range(-0.3f, 0.3f), 3.8f + spec.y, Random.Range(-0.3f, 0.3f)), Vector3.one * Random.Range(0.05f, 0.1f), GameManager.Instance.Mat("Pepper Dark", new Color(0.2f, 0.18f, 0.16f), 0.3f), false);
                    var fx = dot.AddComponent<FxParticle>();
                    fx.velocity = new Vector3(Random.Range(-0.8f, 0.8f), -Random.Range(2f, 4f), Random.Range(-0.5f, 0.5f));
                    fx.flutter = 0.6f; fx.life = 1.2f;
                }
                break;
            }
            case "toast":
            {
                Vector3 a = new Vector3(cx + w * 0.5f + 1.1f, 1.3f, 1.3f), b = new Vector3(cx - w * 0.5f - 1.2f, 0.4f, 0f);
                model.position = Vector3.Lerp(a, b, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 2.2f;
                model.localRotation = Quaternion.Euler(0f, 0f, k * 540f);
                break;
            }
            case "flame":
            {
                model.position = new Vector3(cx, 0.15f, 0f);
                float h = on ? (k < 0.15f ? k / 0.15f : k > 0.85f ? (1f - k) / 0.15f : 1f) : 0f;
                for (int i = 0; i < parts.Count; i++)
                {
                    float f = h * (0.9f + Mathf.Sin(t * 20f + i * 1.7f) * 0.25f);
                    parts[i].localScale = new Vector3(0.28f, 0.1f + f * 1.1f, 0.28f);
                    parts[i].localPosition = new Vector3(parts[i].localPosition.x, f * 1.1f, parts[i].localPosition.z);
                }
                model.gameObject.SetActive(true);
                // النار تحرق الخطوط اللي فوقها
                if (on && h > 0.5f)
                {
                    Bounds burn = new Bounds(new Vector3(cx, 1.2f + spec.y, 0f), new Vector3(w + 0.4f, 2.4f, 4f));
                    foreach (InkMark m in Object.FindObjectsByType<InkMark>(FindObjectsSortMode.None))
                    {
                        var mr = m.GetComponent<MeshRenderer>();
                        if (mr == null || !mr.bounds.Intersects(burn)) continue;
                        SmokeCloud.Puff(mr.bounds.center, 1f);
                        LevelSfx.Play("sizzle", 0.6f);
                        Object.Destroy(m.gameObject);
                    }
                }
                break;
            }
            case "chocofall":
            {
                float h = on ? (k < 0.2f ? k / 0.2f : k > 0.85f ? (1f - k) / 0.15f : 1f) : 0f;
                model.position = new Vector3(cx, 5.2f * (1f - h), 0f);
                model.localScale = new Vector3(1f, Mathf.Max(0.01f, h), 1f);
                model.gameObject.SetActive(h > 0.01f);
                if (on && Random.value < 0.4f)
                {
                    var sp = LevelBuilder.Visual("Choco Splash", PrimitiveType.Sphere, null, new Vector3(cx + Random.Range(-0.5f, 0.5f), 0.1f + spec.y, Random.Range(-0.6f, 0.6f)), Vector3.one * Random.Range(0.12f, 0.22f), GameManager.Instance.Mat("Choco Drip", new Color(0.4f, 0.22f, 0.13f), 0.7f), false);
                    var fx = sp.AddComponent<FxParticle>();
                    fx.velocity = new Vector3(Random.Range(-2f, 2f), Random.Range(1.5f, 3f), Random.Range(-1f, 1f));
                    fx.gravity = 10f; fx.life = 0.5f;
                }
                break;
            }
            case "sprinkles":
                model.gameObject.SetActive(false);
                if (on)
                    for (int s = 0; s < 3; s++)
                    {
                        var sp = LevelBuilder.Visual("Sprinkle", PrimitiveType.Capsule, null, new Vector3(cx + Random.Range(-1f, 1f), 4.1f + spec.y, Random.Range(-0.5f, 0.5f)), new Vector3(0.06f, 0.12f, 0.06f), GameManager.Instance.Mat("Crayon Fall " + Random.Range(0, 4), MusicFeatures.KeyColors[Random.Range(0, 4)], 0.4f), false);
                        sp.transform.rotation = Random.rotation;
                        var fx = sp.AddComponent<FxParticle>();
                        fx.velocity = new Vector3(Random.Range(-0.5f, 0.5f), -Random.Range(4f, 6f), 0f);
                        fx.spin = Random.insideUnitSphere * 400f; fx.life = 0.9f;
                    }
                break;
            case "kettle":
                model.gameObject.SetActive(false);
                if (on && Random.value < 0.8f)
                {
                    Vector3 from = new Vector3(cx + w * 0.5f + 0.6f, 1.75f + spec.y, 0.4f);
                    var puff = LevelBuilder.Visual("Steam Jet", PrimitiveType.Sphere, null, from, Vector3.one * Random.Range(0.3f, 0.5f), GameManager.Instance.Mat("Steam Puff", new Color(0.95f, 0.95f, 1f), 0.2f), false);
                    var fx = puff.AddComponent<FxParticle>();
                    fx.velocity = new Vector3(-Random.Range(5f, 7f), Random.Range(-0.6f, 0.2f), -0.3f);
                    fx.grow = 2f; fx.life = 0.6f;
                }
                break;
            case "oil":
            {
                model.position = Vector3.zero;
                for (int i = 0; i < parts.Count; i++)
                {
                    float kk = Mathf.Repeat(k * 1.5f + i * 0.3f, 1f);
                    Vector3 a = new Vector3(cx + (i - 1) * 0.3f, 0.3f, 1.3f), b = new Vector3(cx + (i - 1) * 0.8f - 0.3f, 0.2f, -0.6f);
                    parts[i].localPosition = Vector3.Lerp(a, b, kk) + Vector3.up * Mathf.Sin(kk * Mathf.PI) * 2.2f;
                }
                if (on && Random.value < 0.05f) LevelSfx.Play("sizzle", 0.3f);
                break;
            }
            case "mole":
            {
                // يطلع بسرعة، يتلفت، ويرجع ينزل
                float up = k < 0.15f ? k / 0.15f : k > 0.85f ? (1f - k) / 0.15f : 1f;
                model.position = new Vector3(cx, -0.55f + up * 0.9f, 0f);
                model.localRotation = Quaternion.Euler(0f, Mathf.Sin(k * 20f) * 25f, 0f);
                model.gameObject.SetActive(on);
                if (on && k < 0.2f && Random.value < 0.5f)
                {
                    var dirt = LevelBuilder.Visual("Dirt", PrimitiveType.Sphere, null, new Vector3(cx, 0.3f + spec.y, 0f), Vector3.one * Random.Range(0.1f, 0.2f), GameManager.Instance.Mat("Forest Soil", new Color(0.45f, 0.3f, 0.2f), 0.15f), false);
                    var fx = dirt.AddComponent<FxParticle>();
                    fx.velocity = new Vector3(Random.Range(-2f, 2f), Random.Range(2f, 4f), Random.Range(-1f, 1f));
                    fx.gravity = 10f; fx.life = 0.6f;
                }
                break;
            }
            case "rollinglog":
                model.position = new Vector3(cx + w * 0.5f + 3.5f - k * (w + 6f), 0.36f, 0f);
                model.localRotation = Quaternion.Euler(0f, 0f, k * (w + 6f) / 0.35f * Mathf.Rad2Deg);
                break;
            case "pinecone":
            {
                // يطيح من الغصن، ينط نطة صغيرة، ويتدحرج شوي
                if (k < 0.45f) { float a = k / 0.45f; model.position = new Vector3(cx, 5.2f - a * a * 4.95f, 0f); model.localRotation = Quaternion.Euler(0f, 0f, a * 200f); }
                else { float a = (k - 0.45f) / 0.55f; model.position = new Vector3(cx - a * 1.2f, 0.25f + Mathf.Sin(Mathf.Min(1f, a * 2.5f) * Mathf.PI) * 0.5f, 0f); model.localRotation = Quaternion.Euler(0f, 0f, 200f + a * 360f); }
                break;
            }
            case "acorn":
            {
                float land = 0.4f;
                Vector3 from = new Vector3(cx + w * 0.5f + 1.4f, 3.3f, 0.3f), hitP = new Vector3(cx + w * 0.5f, 0.2f, 0f);
                if (k < land) { float a = k / land; model.position = Vector3.Lerp(from, hitP, a) + Vector3.up * Mathf.Sin(a * Mathf.PI) * 1.2f; }
                else model.position = hitP + Vector3.left * ((k - land) / (1f - land)) * (w + 1.2f);
                model.localRotation = Quaternion.Euler(0f, 0f, k * 720f);
                break;
            }
            case "speaker":
            {
                // موجات تطلع من السماعة وتمشي لليسار وهي تكبر
                model.position = Vector3.zero;
                for (int i = 0; i < 3; i++)
                {
                    float kk = Mathf.Clamp01(k * 1.4f - i * 0.2f);
                    parts[i].localPosition = new Vector3(cx + w * 0.5f + 0.4f - kk * (w + 1.6f), 1.1f, 0f);
                    parts[i].localScale = new Vector3(0.6f + kk * 2.2f, 0.04f, 0.6f + kk * 2.2f);
                    parts[i].gameObject.SetActive(on && kk > 0f && kk < 1f);
                }
                if (on && k < 0.1f && Random.value < 0.4f)
                {
                    var cam = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
                    if (cam != null) cam.Shake(0.06f, 0.15f);
                }
                break;
            }
            case "paperball":
            {
                // واحد من الجمهور يرميها: تطلع من قدام تحت، تنزل على الطريق، وتتدحرج لليسار
                float land = 0.45f;
                Vector3 from = new Vector3(cx + w * 0.5f + 1f, -1.2f, -3f), hitP = new Vector3(cx + w * 0.5f, 0.35f, 0f);
                if (k < land)
                {
                    float a = k / land;
                    model.position = Vector3.Lerp(from, hitP, a) + Vector3.up * Mathf.Sin(a * Mathf.PI) * 3f;
                }
                else model.position = hitP + Vector3.left * ((k - land) / (1f - land)) * (w + 1.5f);
                model.localRotation = Quaternion.Euler(0f, 0f, k * 900f);
                break;
            }
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
            case "speaker": return "drum";
            case "pinecone": return "crack";
            case "mole": return "dig";
            case "pepper": return "sneeze";
            case "flour": return "whoosh";
            case "tesla": return "tesla";
            case "drip": return "sizzle";
            case "bubbleflask": return "pop";
            case "rollingpin": return "rumble";
            case "mousetrap": return "snap";
            case "toast": return "toaster";
            case "flame": return "sizzle";
            case "chocofall": return "slurp";
            case "sprinkles": return "chime";
            case "kettle": return "whistle";
            case "oil": return "sizzle";
            case "rollinglog": return "rumble";
            case "acorn": return "pop";
            case "paperball": return "whoosh";
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
        if (spec.kind == "pencil" || spec.kind == "rollinglog")
        {
            float gap = model.position.x - p.x;
            // تنط لحالها، إلا بالمراحل اللي فيها نط (هناك اللاعبة تنط بنفسها)
            bool manual = gm.CurrentLevel != null && gm.CurrentLevel.allowJump;
            if (!manual && on && gm.IsPlaying && gap > 1.1f && gap < 2.4f && Mathf.Abs(p.y - spec.y - 0.7f) < 0.4f) rw.Jump(true);
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
