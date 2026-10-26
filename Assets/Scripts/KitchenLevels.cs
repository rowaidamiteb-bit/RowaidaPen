using System.Collections.Generic;
using UnityEngine;

// الجزء الثالث — فصل المطبخ: وصفة بالقدر، ثلاجة بريحها الباردة، نهر شوكولاتة ومارشملو، مكينة علك،
// بوابة عصا حلاوة، مقلاة بان كيك، فرن بابه جسر، سير متحرك، دببة جيلي، وكيكة بشموع
public static class KitchenFeatures
{
    public static void Build(Transform root, LevelData data, GameManager gm, System.Random rng)
    {
        foreach (Vector3 s in data.recipePots) RecipePot.Create(root, s, gm);
        foreach (Vector3 s in data.fridges) FridgeWind.Create(root, s, gm);
        foreach (Vector3 s in data.chocoRivers) ChocoRiver.Create(root, s, gm);
        foreach (Vector3 s in data.gumballPits) GumballPit.Create(root, s, gm);
        foreach (Vector2 s in data.candyGates) CandyGate.Create(root, s, gm);
        foreach (Vector3 s in data.pancakePans) PancakeFlip.Create(root, s, gm);
        foreach (Vector3 s in data.ovenDoors) OvenDoor.Create(root, s, gm);
        foreach (Vector3 s in data.conveyors) Conveyor.Create(root, s, gm);
        foreach (Vector4 s in data.jellyBears) JellyBears.Create(root, s, gm);
        foreach (Vector3 s in data.cakes) CakeTower.Create(root, s, gm);
        if (data.kitchenScenery > 0) KitchenScenery.Create(root, data, gm, rng);
    }

    public static bool Touched(Bounds b, bool closedOnly = false, bool openOnly = false)
    {
        foreach (InkMark m in Object.FindObjectsByType<InkMark>(FindObjectsSortMode.None))
        {
            if (m.beingEaten || (closedOnly && !m.closed) || (openOnly && m.closed)) continue;
            var r = m.GetComponent<MeshRenderer>();
            if (r != null && r.bounds.Intersects(b)) { Object.Destroy(m.gameObject); return true; }
        }
        return false;
    }

    public static InkMark ClosedNear(float x0, float x1)
    {
        foreach (InkMark m in Object.FindObjectsByType<InkMark>(FindObjectsSortMode.None))
        {
            if (!m.closed || m.beingEaten) continue;
            var r = m.GetComponent<MeshRenderer>();
            if (r != null && r.bounds.center.x > x0 && r.bounds.center.x < x1) return m;
        }
        return null;
    }
}

// ---------- قدر الطبخ ----------

// قدر بالحفرة وفوقه ثلاث مكونات معلقة، وكرت الوصفة يقول الترتيب. المسي المكونات بخط بنفس الترتيب:
// تطيح بالقدر، والشوربة تغلي، ومغرفة عملاقة تنزل وتصير جسر. لو الترتيب غلط القدر يكح وترجع المكونات
public class RecipePot : MonoBehaviour
{
    static readonly int[] Order = { 1, 0, 2 };   // جزر، طماط، بصل
    float from, to, top, ladleT = -1f;
    int step;
    bool holding, prompted;
    readonly Transform[] items = new Transform[3];
    readonly Vector3[] hooks = new Vector3[3];
    readonly float[] dropT = { -1f, -1f, -1f };
    readonly Transform[] strings = new Transform[3];
    Transform soup, ladle, arrow, face, mouth;
    float happyUntil, coughUntil;
    readonly Transform[] cardIcons = new Transform[3];
    BoxCollider bridge;
    Material soupMat, soupDone;

    public static void Create(Transform root, Vector3 spec, GameManager gm)
    {
        var go = new GameObject("Recipe Pot");
        go.transform.SetParent(root, false);
        var p = go.AddComponent<RecipePot>();
        p.from = spec.x; p.to = spec.y; p.top = spec.z;
        float cx = (p.from + p.to) * 0.5f, w = p.to - p.from;
        Material metal = gm.Mat("Pot Metal", new Color(0.7f, 0.72f, 0.76f), 0.85f, 0.7f);
        Material dark = gm.Mat("Pot Dark", new Color(0.35f, 0.36f, 0.4f), 0.6f, 0.5f);
        p.soupMat = gm.Mat("Soup", new Color(0.95f, 0.45f, 0.25f), 0.7f);
        p.soupDone = gm.Mat("Soup Done", new Color(1.2f, 0.6f, 0.3f), 0.8f);
        Material flame = gm.Mat("Stove Flame", new Color(0.4f, 0.6f, 1.6f), 0.5f);
        // القدر
        LevelBuilder.Visual("Pot", PrimitiveType.Cylinder, go.transform, new Vector3(cx, p.top - 2.3f, 0f), new Vector3(w * 0.8f, 1.3f, 2.4f), metal);
        LevelBuilder.Visual("Rim", PrimitiveType.Cylinder, go.transform, new Vector3(cx, p.top - 1.0f, 0f), new Vector3(w * 0.84f, 0.06f, 2.55f), dark);
        // وجه القدر: عيون وفم (يبتسم إذا المكون صح، ويكح إذا غلط)
        Material eyeW = gm.Mat("Monster Eye", Color.white, 0.7f);
        Material eyeB = gm.Mat("Monster Pupil", new Color(0.05f, 0.04f, 0.06f), 0.9f);
        p.face = new GameObject("Pot Face").transform;
        p.face.SetParent(go.transform, false);
        p.face.position = new Vector3(cx, p.top - 2.1f, -1.22f);
        for (int s = -1; s <= 1; s += 2)
        {
            LevelBuilder.Visual("Eye", PrimitiveType.Sphere, p.face, new Vector3(s * 0.55f, 0.35f, 0f), new Vector3(0.45f, 0.5f, 0.1f), eyeW, false);
            LevelBuilder.Visual("Pupil", PrimitiveType.Sphere, p.face, new Vector3(s * 0.55f, 0.32f, -0.06f), new Vector3(0.22f, 0.26f, 0.06f), eyeB, false);
            LevelBuilder.Visual("Cheek", PrimitiveType.Sphere, p.face, new Vector3(s * 0.95f, -0.05f, 0f), new Vector3(0.35f, 0.2f, 0.05f), gm.Mat("Bunny Pink", new Color(1f, 0.7f, 0.75f), 0.3f), false);
        }
        p.mouth = LevelBuilder.Visual("Mouth", PrimitiveType.Sphere, p.face, new Vector3(0f, -0.25f, 0f), new Vector3(0.6f, 0.2f, 0.08f), eyeB, false).transform;
        for (int s = -1; s <= 1; s += 2)
            LevelBuilder.Visual("Handle", PrimitiveType.Cube, go.transform, new Vector3(cx + s * (w * 0.42f + 0.3f), p.top - 1.3f, 0f), new Vector3(0.6f, 0.15f, 0.4f), dark);
        p.soup = LevelBuilder.Visual("Soup", PrimitiveType.Cylinder, go.transform, new Vector3(cx, p.top - 1.15f, 0f), new Vector3(w * 0.76f, 0.03f, 2.3f), p.soupMat, false).transform;
        for (int i = 0; i < 8; i++)
        {
            float a = i / 8f * Mathf.PI * 2f;
            LevelBuilder.Visual("Flame", PrimitiveType.Capsule, go.transform, new Vector3(cx + Mathf.Cos(a) * w * 0.3f, p.top - 3.75f, Mathf.Sin(a) * 0.8f), new Vector3(0.18f, 0.25f, 0.18f), flame, false);
        }
        // كرت الوصفة على الصفحة اليسار
        Material card = gm.Mat("Recipe Card", new Color(1f, 0.98f, 0.92f), 0.2f);
        Material ink = gm.Mat("Note Black", new Color(0.12f, 0.1f, 0.15f), 0.6f);
        var c = LevelBuilder.Visual("Recipe Card", PrimitiveType.Cube, go.transform, new Vector3(p.from - 2.6f, p.top + 1.7f, 1.5f), new Vector3(2.4f, 1.6f, 0.05f), card).transform;
        c.localRotation = Quaternion.Euler(0f, 0f, 4f);
        LevelBuilder.Visual("Card Pin", PrimitiveType.Sphere, c, new Vector3(0f, 0.42f, -0.6f), new Vector3(0.08f, 0.1f, 1f), gm.Mat("Tower Red", new Color(0.85f, 0.2f, 0.22f), 0.35f), false);
        LevelBuilder.Visual("Card Stand", PrimitiveType.Cylinder, go.transform, new Vector3(p.from - 2.6f, p.top + 0.45f, 1.55f), new Vector3(0.08f, 0.45f, 0.08f), ink);
        for (int k = 0; k < 3; k++)
        {
            p.cardIcons[k] = MakeIngredient(Order[k], c, new Vector3(0.32f - k * 0.32f, -0.05f, -0.6f), 0.18f, gm);
            if (k < 2) LevelBuilder.Visual("Arrow", PrimitiveType.Cube, c, new Vector3(0.16f - k * 0.32f, -0.05f, -0.6f), new Vector3(0.06f, 0.02f, 1f), ink, false);
        }
        p.arrow = LevelBuilder.Visual("Step", PrimitiveType.Cube, c, Vector3.zero, new Vector3(0.12f, 0.03f, 1f), gm.Mat("Tower Red", new Color(0.85f, 0.2f, 0.22f), 0.35f), false).transform;
        // المكونات معلقة فوق القدر
        Material thread = gm.Mat("Thread", new Color(0.8f, 0.8f, 0.85f), 0.2f);
        for (int i = 0; i < 3; i++)
        {
            p.hooks[i] = new Vector3(p.from + w * (0.25f + i * 0.25f), p.top + 3f, 0f);
            p.items[i] = MakeIngredient(i, go.transform, p.hooks[i], 1f, gm);
            p.strings[i] = LevelBuilder.Visual("String", PrimitiveType.Cube, go.transform, p.hooks[i] + Vector3.up * 1.6f, new Vector3(0.03f, 3f, 0.03f), thread, false).transform;
        }
        // المغرفة العملاقة (فوق، تنتظر)
        p.ladle = new GameObject("Ladle").transform;
        p.ladle.SetParent(go.transform, false);
        LevelBuilder.Visual("Handle", PrimitiveType.Cube, p.ladle, Vector3.zero, new Vector3(w + 1.2f, 0.3f, 1.4f), metal);
        LevelBuilder.Visual("Bowl", PrimitiveType.Sphere, p.ladle, new Vector3(-(w + 1.2f) * 0.5f, -0.2f, 0f), new Vector3(1.6f, 0.9f, 1.6f), metal);
        LevelBuilder.Visual("Grip", PrimitiveType.Cube, p.ladle, new Vector3((w + 1.2f) * 0.42f, 0.02f, 0f), new Vector3(1f, 0.34f, 1.42f), gm.Mat("Tower Red", new Color(0.85f, 0.2f, 0.22f), 0.35f));
        p.ladle.position = new Vector3(p.to + 2f, p.top + 7f, 0.5f);
        p.ladle.localRotation = Quaternion.Euler(0f, 0f, -70f);
        p.bridge = p.ladle.gameObject.AddComponent<BoxCollider>();
        p.bridge.size = new Vector3(w + 1.2f, 0.3f, 1.4f);
        p.bridge.enabled = false;
    }

    // طماط (0)، جزر (1)، بصل (2)
    public static Transform MakeIngredient(int kind, Transform parent, Vector3 pos, float s, GameManager gm)
    {
        var t = new GameObject("Ingredient " + kind).transform;
        t.SetParent(parent, false);
        t.localPosition = pos;
        t.localScale = Vector3.one * s;
        Material leaf = gm.Mat("Leaf Green", new Color(0.35f, 0.72f, 0.3f), 0.35f);
        switch (kind)
        {
            case 0:
                LevelBuilder.Visual("Tomato", PrimitiveType.Sphere, t, Vector3.zero, new Vector3(0.9f, 0.8f, 0.9f), gm.Mat("Tomato", new Color(0.95f, 0.2f, 0.18f), 0.75f), false);
                for (int i = 0; i < 4; i++) LevelBuilder.Visual("Leaf", PrimitiveType.Cube, t, new Vector3(0f, 0.4f, 0f), new Vector3(0.4f, 0.04f, 0.1f), leaf, false).transform.localRotation = Quaternion.Euler(0f, i * 45f, 0f);
                break;
            case 1:
                var car = LevelBuilder.Visual("Carrot", PrimitiveType.Capsule, t, new Vector3(0f, -0.1f, 0f), new Vector3(0.4f, 0.6f, 0.4f), gm.Mat("Carrot", new Color(1f, 0.55f, 0.15f), 0.5f), false).transform;
                car.localScale = new Vector3(0.38f, 0.6f, 0.38f);
                for (int i = 0; i < 3; i++) LevelBuilder.Visual("Leaves", PrimitiveType.Capsule, t, new Vector3((i - 1) * 0.08f, 0.6f, 0f), new Vector3(0.06f, 0.2f, 0.06f), leaf, false).transform.localRotation = Quaternion.Euler(0f, 0f, (i - 1) * 25f);
                break;
            default:
                LevelBuilder.Visual("Onion", PrimitiveType.Sphere, t, Vector3.zero, new Vector3(0.8f, 0.85f, 0.8f), gm.Mat("Onion", new Color(0.7f, 0.35f, 0.65f), 0.6f), false);
                LevelBuilder.Visual("Sprout", PrimitiveType.Capsule, t, new Vector3(0f, 0.5f, 0f), new Vector3(0.08f, 0.2f, 0.08f), leaf, false);
                break;
        }
        return t;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time, dt = Time.deltaTime, cx = (from + to) * 0.5f;
        arrow.localPosition = new Vector3(0.32f - Mathf.Min(step, 2) * 0.32f, -0.3f + Mathf.Sin(t * 5f) * 0.02f, -0.6f);
        arrow.gameObject.SetActive(step < 3);

        for (int i = 0; i < 3; i++)
        {
            if (dropT[i] < 0f)
            {
                items[i].localPosition = hooks[i] + new Vector3(Mathf.Sin(t * 1.4f + i) * 0.08f, 0f, 0f);
                items[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.4f + i) * 6f);
                strings[i].gameObject.SetActive(true);
                if (ladleT < 0f && KitchenFeatures.Touched(new Bounds(items[i].position, new Vector3(1.3f, 1.3f, 4f))))
                {
                    dropT[i] = 0f;
                    bool right = Order[step] == i;
                    if (right)
                    {
                        step++;
                        LevelSfx.Play("plop", 0.8f, 0.9f + step * 0.1f);
                        happyUntil = t + 1.2f;
                    }
                    else
                    {
                        // غلط: القدر يكح، وكل المكونات ترجع لمكانها
                        LevelSfx.Play("error", 0.8f);
                        coughUntil = t + 1.4f;
                        for (int k = 0; k < 8; k++) SmokeCloud.Puff(new Vector3(cx + Random.Range(-1f, 1f), top - 0.5f, Random.Range(-0.5f, 0.5f)), Random.Range(0.8f, 1.4f));
                        gm.UI.ShowMessage("", 2.4f, "مو هذا! شوفي كرت الوصفة واتبعي الترتيب");
                        step = 0;
                        for (int k = 0; k < 3; k++) dropT[k] = -1f;
                    }
                }
            }
            else
            {
                // تطيح بالقدر وتغطس
                dropT[i] += dt;
                strings[i].gameObject.SetActive(false);
                float k = Mathf.Clamp01(dropT[i] / 0.6f);
                items[i].localPosition = Vector3.Lerp(hooks[i], new Vector3(cx + (i - 1) * 0.6f, top - 1.3f, 0f), k * k);
                items[i].localRotation = Quaternion.Euler(0f, 0f, k * 360f);
                items[i].gameObject.SetActive(k < 1f || step == 3);
                if (k >= 1f && dropT[i] - dt < 0.6f) Fx.Sparkle(new Vector3(cx, top - 1f, 0f), new Color(1f, 0.6f, 0.3f), 10);
            }
        }

        // كل المكونات بالترتيب: تغلي والمغرفة تنزل
        if (step >= 3 && ladleT < 0f) { ladleT = 0f; LevelSfx.Play("boil", 0.9f); soup.GetComponent<MeshRenderer>().sharedMaterial = soupDone; gm.UI.ShowMessage("", 2f, "يم! الشوربة جاهزة"); }
        if (ladleT >= 0f && ladleT < 1f)
        {
            ladleT = Mathf.Min(1f, ladleT + dt / 1.4f);
            float k = Mathf.SmoothStep(0f, 1f, ladleT);
            ladle.position = Vector3.Lerp(new Vector3(to + 2f, top + 7f, 0.5f), new Vector3(cx + 0.6f, top - 0.15f, 0f), k);
            ladle.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-70f, 0f, k));
            if (ladleT >= 1f) { bridge.enabled = true; LevelSfx.Play("clank", 0.8f); Fx.Sparkle(new Vector3(cx, top + 0.3f, 0f), new Color(1f, 0.9f, 0.6f), 16); }
        }
        // وجه القدر
        bool happy = t < happyUntil || step >= 3, cough = t < coughUntil;
        mouth.localScale = cough ? new Vector3(0.35f, 0.35f + Mathf.Abs(Mathf.Sin(t * 20f)) * 0.15f, 0.08f) : happy ? new Vector3(0.75f, 0.32f, 0.08f) : new Vector3(0.5f, 0.12f, 0.08f);
        face.localRotation = Quaternion.Euler(0f, 0f, cough ? Mathf.Sin(t * 30f) * 6f : happy ? Mathf.Sin(t * 8f) * 4f : Mathf.Sin(t * 1.5f) * 1.5f);
        // الشوربة تفور (فقاعات)
        soup.localScale = new Vector3((to - from) * 0.76f, 0.03f + (step >= 3 ? Mathf.Abs(Mathf.Sin(t * 8f)) * 0.04f : 0f), 2.3f);
        if (Random.value < (step >= 3 ? 0.4f : 0.08f))
        {
            var bub = LevelBuilder.Visual("Bubble", PrimitiveType.Sphere, null, new Vector3(cx + Random.Range(-1.5f, 1.5f), top - 1.1f, Random.Range(-0.8f, 0.8f)), Vector3.one * Random.Range(0.1f, 0.25f), soupMat, false);
            var fx = bub.AddComponent<FxParticle>();
            fx.velocity = Vector3.up * Random.Range(0.5f, 1.2f); fx.life = 0.5f; fx.grow = 0.5f;
        }
        if (step >= 3 && Random.value < 0.15f) SmokeCloud.Puff(new Vector3(cx + Random.Range(-1f, 1f), top - 0.3f, 1f), Random.Range(0.7f, 1.2f));

        bool wait = HoldHelper.Before(from) && !bridge.enabled && !RowaidaController.BridgeCovers(from, to, top);
        if (wait && !prompted) { prompted = true; gm.UI.ShowMessage("", 3.4f, "اتبعي الوصفة! المسي المكونات بخط بنفس ترتيب الكرت"); }
        HoldHelper.Hold(ref holding, wait);
    }
}

// ---------- الثلاجة ----------

// ثلاجة بابها يفتح ويسكر: لما يفتح تطلع ريح باردة تدف رويدا لورا. استني لين يتسكر، أو ارسمي دايرة على الباب تصير مغناطيس يسكره
public class FridgeWind : MonoBehaviour
{
    const float Period = 5.6f, Closed = 2.8f;
    float x, top, range, start, nextFlake;
    bool holding, prompted, magnet, pushing;
    Transform door, magnetT;
    Light inside;

    float Local => Mathf.Repeat(Time.time - start, Period);
    float Open01 => magnet ? 0f : Local < Closed ? 0f : Local < Closed + 0.3f ? (Local - Closed) / 0.3f : Local < Period - 0.3f ? 1f : (Period - Local) / 0.3f;
    bool Blowing => Open01 > 0.5f;
    float UntilOpen => Local < Closed ? Closed - Local : 0f;

    public static void Create(Transform root, Vector3 spec, GameManager gm)
    {
        var go = new GameObject("Fridge");
        go.transform.SetParent(root, false);
        var f = go.AddComponent<FridgeWind>();
        f.x = spec.x; f.top = spec.y; f.range = spec.z;
        f.start = Time.time;
        Material white = gm.Mat("Fridge White", new Color(0.92f, 0.96f, 0.98f), 0.7f);
        Material steel = gm.Mat("Sharpener Steel", new Color(0.8f, 0.82f, 0.86f), 0.9f, 0.8f);
        Material insideM = gm.Mat("Fridge Inside", new Color(0.85f, 0.95f, 1f), 0.5f);
        Material milk = gm.Mat("Milk", Color.white, 0.4f);
        Material juice = gm.Mat("Juice", new Color(1f, 0.6f, 0.15f), 0.5f);
        Vector3 body = new Vector3(f.x + 1.3f, f.top, 1.7f);
        LevelBuilder.Visual("Fridge Body", PrimitiveType.Cube, go.transform, body + new Vector3(0.4f, 1.9f, 0.4f), new Vector3(1.8f, 3.8f, 2f), white);
        LevelBuilder.Visual("Fridge Inside", PrimitiveType.Cube, go.transform, body + new Vector3(-0.45f, 1.9f, 0.4f), new Vector3(0.1f, 3.5f, 1.7f), insideM, false);
        for (int i = 0; i < 3; i++)
            LevelBuilder.Visual("Shelf", PrimitiveType.Cube, go.transform, body + new Vector3(-0.3f, 0.9f + i * 1f, 0.4f), new Vector3(0.4f, 0.04f, 1.6f), steel, false);
        LevelBuilder.Visual("Milk", PrimitiveType.Cube, go.transform, body + new Vector3(-0.3f, 1.2f, 0.6f), new Vector3(0.3f, 0.55f, 0.3f), milk, false);
        LevelBuilder.Visual("Juice", PrimitiveType.Cylinder, go.transform, body + new Vector3(-0.3f, 2.2f, 0.1f), new Vector3(0.3f, 0.3f, 0.3f), juice, false);
        LevelBuilder.Visual("Jelly", PrimitiveType.Sphere, go.transform, body + new Vector3(-0.3f, 3.1f, 0.6f), new Vector3(0.4f, 0.3f, 0.4f), gm.Mat("Jelly Red", new Color(1f, 0.3f, 0.4f), 0.9f), false);
        // الباب على الوجه اليسار، مفصله ورا
        var hinge = new GameObject("Door Hinge").transform;
        hinge.SetParent(go.transform, false);
        hinge.position = body + new Vector3(-0.55f, 0f, 1.35f);
        f.door = hinge;
        LevelBuilder.Visual("Door", PrimitiveType.Cube, hinge, new Vector3(0f, 1.9f, -0.95f), new Vector3(0.14f, 3.75f, 1.9f), white);
        LevelBuilder.Visual("Handle", PrimitiveType.Cube, hinge, new Vector3(-0.12f, 1.9f, -1.7f), new Vector3(0.08f, 1f, 0.1f), steel, false);
        f.magnetT = new GameObject("Magnet").transform;
        f.magnetT.SetParent(hinge, false);
        f.magnetT.localPosition = new Vector3(-0.1f, 2.6f, -0.9f);
        Material red = gm.Mat("Tower Red", new Color(0.85f, 0.2f, 0.22f), 0.35f);
        for (int s = -1; s <= 1; s += 2) LevelBuilder.Visual("Arm", PrimitiveType.Cube, f.magnetT, new Vector3(0f, 0f, s * 0.15f), new Vector3(0.08f, 0.4f, 0.1f), red, false);
        LevelBuilder.Visual("Bottom", PrimitiveType.Cube, f.magnetT, new Vector3(0f, -0.2f, 0f), new Vector3(0.08f, 0.1f, 0.4f), red, false);
        f.magnetT.gameObject.SetActive(false);
        f.inside = new GameObject("Fridge Light").AddComponent<Light>();
        f.inside.transform.SetParent(go.transform, false);
        f.inside.transform.position = body + new Vector3(-1f, 2f, -0.3f);
        f.inside.type = LightType.Point;
        f.inside.color = new Color(0.7f, 0.9f, 1f);
        f.inside.range = 3f;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time;
        RowaidaController r = gm.Rowaida;

        if (!magnet && KitchenFeatures.ClosedNear(x + 0.3f, x + 2.5f) is InkMark m)
        {
            Destroy(m.gameObject);
            magnet = true;
            magnetT.gameObject.SetActive(true);
            LevelSfx.Play("clank", 0.7f, 1.4f);
            gm.UI.ShowMessage("", 2.2f, "مغناطيس! الثلاجة تسكرت");
        }
        float open = Open01;
        door.localRotation = Quaternion.Euler(0f, open * 75f, 0f);
        inside.intensity = open * 2.5f;
        if (open > 0.1f && open < 0.2f && Local > Closed && Local < Closed + 0.1f && r != null && Mathf.Abs(r.transform.position.x - x) < 12f) LevelSfx.Play("cold", 0.7f);

        // الريح الباردة
        bool blowing = Blowing;
        if (blowing && t >= nextFlake)
        {
            nextFlake = t + 0.04f;
            var flake = LevelBuilder.Visual("Snowflake", PrimitiveType.Cube, null, new Vector3(x + 0.6f, top + Random.Range(0.2f, 3f), Random.Range(-1f, 1f)), Vector3.one * Random.Range(0.06f, 0.12f), gm.Mat("Snow Flake", new Color(0.9f, 0.97f, 1f), 0.6f), false);
            var fx = flake.AddComponent<FxParticle>();
            fx.velocity = new Vector3(-Random.Range(5f, 8f), Random.Range(-0.5f, 0.5f), 0f);
            fx.spin = Random.insideUnitSphere * 300f; fx.life = 1f;
        }
        bool inZone = r != null && r.transform.position.x > x - range && r.transform.position.x < x + 0.5f;
        if (blowing && inZone) { r.WalkBonus = -4.5f; pushing = true; }
        else if (pushing && r != null) { r.WalkBonus = 0f; pushing = false; }

        float cross = (range + 1.6f) / (r != null ? r.walkSpeed : 2.8f);
        bool wait = !magnet && HoldHelper.Before(x - range) && (blowing || UntilOpen < cross);
        if (wait && !prompted) { prompted = true; gm.UI.ShowMessage("", 3.4f, "الثلاجة تطلع ريح باردة! استني لين تتسكر... أو ارسمي دايرة على بابها"); }
        HoldHelper.Hold(ref holding, wait);
    }

    void OnDisable()
    {
        GameManager gm = GameManager.Instance;
        if (pushing && gm != null && gm.Rowaida != null) gm.Rowaida.WalkBonus = 0f;
    }
}

// ---------- نهر الشوكولاتة ----------

// نهر شوكولاتة: الخطوط تغرق فيه. ارسمي دواير: كل وحدة تصير مارشملو يطفو بمكانه، ولما يكتملون رويدا تمشي عليهم
public class ChocoRiver : MonoBehaviour
{
    const float Melt = 16f;
    float from, to, top, slotW;
    bool holding, prompted;
    Transform[] slots;
    float[] placed;
    readonly Dictionary<InkMark, float> seen = new Dictionary<InkMark, float>();
    Material choco, mallowA, mallowB;

    public static void Create(Transform root, Vector3 spec, GameManager gm)
    {
        var go = new GameObject("Choco River");
        go.transform.SetParent(root, false);
        var c = go.AddComponent<ChocoRiver>();
        c.from = spec.x; c.to = spec.y; c.top = spec.z;
        int n = Mathf.Max(2, Mathf.RoundToInt((c.to - c.from) / 1.5f));
        c.slotW = (c.to - c.from) / n;
        c.slots = new Transform[n];
        c.placed = new float[n];
        var tex = new Texture2D(8, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat };
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 8; x++)
                tex.SetPixel(x, y, Color.Lerp(new Color(0.36f, 0.2f, 0.12f), new Color(0.55f, 0.33f, 0.2f), Mathf.Clamp01(Mathf.Sin(y / 64f * Mathf.PI * 4f + x * 0.3f) * 1.5f - 0.5f)));
        tex.Apply();
        c.choco = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "Choco" };
        c.choco.SetTexture("_BaseMap", tex);
        c.choco.SetColor("_BaseColor", Color.white);
        c.choco.SetTextureScale("_BaseMap", new Vector2(1f, 3f));
        var q = LevelBuilder.Visual("Chocolate", PrimitiveType.Quad, go.transform, new Vector3((c.from + c.to) * 0.5f, c.top - 0.55f, 0.5f), new Vector3(c.to - c.from + 0.2f, 13f, 1f), c.choco, false).transform;
        q.localRotation = Quaternion.Euler(90f, 0f, 0f);
        // قطرات شوكولاتة على أطراف الصفحات
        Material drip = gm.Mat("Choco Drip", new Color(0.4f, 0.22f, 0.13f), 0.7f);
        for (int s = 0; s < 2; s++)
            for (int i = 0; i < 4; i++)
                LevelBuilder.Visual("Drip", PrimitiveType.Capsule, go.transform, new Vector3(s == 0 ? c.from + 0.05f : c.to - 0.05f, c.top - 0.25f - (i % 2) * 0.15f, -1.2f + i * 0.8f), new Vector3(0.18f, 0.25f + (i % 3) * 0.08f, 0.18f), drip, false);
        c.mallowA = gm.Mat("Marshmallow", new Color(1f, 0.97f, 0.95f), 0.3f);
        c.mallowB = gm.Mat("Marshmallow Pink", new Color(1f, 0.78f, 0.85f), 0.3f);
        // مارشملو منقط (وين ترسمين)
        Material hint = gm.Mat("Staff Hint", new Color(0.55f, 0.6f, 0.95f), 0.3f);
        for (int i = 0; i < n; i++)
            for (int k = 0; k < 8; k++)
            {
                float a = k / 8f * Mathf.PI * 2f;
                LevelBuilder.Visual("Hint Dot", PrimitiveType.Sphere, go.transform, new Vector3(c.from + (i + 0.5f) * c.slotW + Mathf.Cos(a) * 0.4f, c.top + 0.6f + Mathf.Sin(a) * 0.35f, 0f), Vector3.one * 0.06f, hint, false);
            }
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time;
        choco.SetTextureOffset("_BaseMap", new Vector2(0f, t * 0.15f));
        ForestFeatures2.SwallowLines(from, to, top, seen, 1f);
        RowaidaController r = gm.Rowaida;
        bool rowaidaOn = r != null && r.transform.position.x > from - 0.3f && r.transform.position.x < to + 0.3f;

        // دايرة؟ تصير مارشملو بأقرب مكان فاضي
        InkMark m = KitchenFeatures.ClosedNear(from - 0.5f, to + 0.5f);
        if (m != null)
        {
            float cx = m.GetComponent<MeshRenderer>().bounds.center.x;
            Destroy(m.gameObject);
            int best = -1; float bd = 99f;
            for (int i = 0; i < slots.Length; i++)
                if (slots[i] == null && Mathf.Abs(from + (i + 0.5f) * slotW - cx) < bd) { bd = Mathf.Abs(from + (i + 0.5f) * slotW - cx); best = i; }
            if (best >= 0)
            {
                slots[best] = MakeMallow(best, gm);
                placed[best] = t;
                LevelSfx.Play("pop", 0.8f, 0.8f);
                LevelSfx.Play("slurp", 0.4f, 1.3f);
            }
        }
        bool all = true;
        float minLeft = 99f;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) { all = false; continue; }
            float age = t - placed[i];
            float left = Melt - age;
            if (rowaidaOn) placed[i] = Mathf.Max(placed[i], t - Melt + 2f);   // ما يذوب ورويدا فوقه
            minLeft = Mathf.Min(minLeft, left);
            Transform s = slots[i];
            float melt = Mathf.Clamp01((age - (Melt - 2f)) / 2f);
            s.position = new Vector3(s.position.x, top - 0.32f + Mathf.Sin(t * 2f + i) * 0.03f - melt * 0.6f, 0f);
            bool stepped = r != null && Mathf.Abs(r.transform.position.x - s.position.x) < slotW * 0.5f && r.transform.position.y - s.position.y < 1.6f;
            float squish = stepped ? 0.8f + Mathf.Sin(t * 6f) * 0.03f : 1f;
            s.localScale = Vector3.Lerp(s.localScale, new Vector3(stepped ? 1.06f : 1f, (1f - melt * 0.5f) * squish, stepped ? 1.06f : 1f), Time.deltaTime * 10f);
            if (age > Melt) { Destroy(s.gameObject); slots[i] = null; LevelSfx.Play("slurp", 0.5f); }
        }
        float cross = (to - from + 1.6f) / (r != null ? r.walkSpeed : 2.8f);
        bool ready = all && (minLeft > cross || rowaidaOn);
        bool wait = HoldHelper.Before(from) && !ready;
        if (wait && !prompted) { prompted = true; gm.UI.ShowMessage("", 3.4f, "نهر شوكولاتة! الخطوط تغرق... ارسمي دواير بالأماكن المنقطة تصير مارشملو"); }
        HoldHelper.Hold(ref holding, wait);
    }

    Transform MakeMallow(int i, GameManager gm)
    {
        var go = new GameObject("Marshmallow");
        go.transform.SetParent(transform, false);
        go.transform.position = new Vector3(from + (i + 0.5f) * slotW, top - 0.32f, 0f);
        LevelBuilder.Visual("Puff", PrimitiveType.Cylinder, go.transform, Vector3.zero, new Vector3(slotW + 0.05f, 0.32f, 1.5f), i % 2 == 0 ? mallowA : mallowB);
        LevelBuilder.Visual("Top", PrimitiveType.Sphere, go.transform, new Vector3(0f, 0.3f, 0f), new Vector3(slotW, 0.12f, 1.4f), i % 2 == 0 ? mallowA : mallowB, false);
        var col = go.AddComponent<BoxCollider>();
        col.size = new Vector3(slotW + 0.05f, 0.64f, 1.5f);
        var rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        Fx.Sparkle(go.transform.position + Vector3.up * 0.4f, new Color(1f, 0.85f, 0.9f), 12);
        return go.transform;
    }
}

// ---------- مكينة العلك ----------

// مكينة علك على طرف حفرة: المسي يدها الحمرا بخط، تلف ويطيح منها علك ملون كثير يعبي الحفرة ويصير أرض
public class GumballPit : MonoBehaviour
{
    float from, to, top, fillT = -1f;
    bool holding, prompted;
    Transform crank, pile;
    BoxCollider fillCol;
    readonly List<Transform> balls = new List<Transform>();
    Material[] colors;

    public static void Create(Transform root, Vector3 spec, GameManager gm)
    {
        var go = new GameObject("Gumball Pit");
        go.transform.SetParent(root, false);
        var g = go.AddComponent<GumballPit>();
        g.from = spec.x; g.to = spec.y; g.top = spec.z;
        Color[] cols = { new Color(1f, 0.3f, 0.35f), new Color(0.3f, 0.6f, 1f), new Color(1f, 0.85f, 0.2f), new Color(0.4f, 0.85f, 0.4f), new Color(0.75f, 0.45f, 0.95f), new Color(1f, 0.6f, 0.25f) };
        g.colors = new Material[cols.Length];
        for (int i = 0; i < cols.Length; i++) g.colors[i] = gm.Mat("Gumball " + i, cols[i], 0.85f);
        Material red = gm.Mat("Gumball Machine Red", new Color(0.9f, 0.2f, 0.25f), 0.7f, 0.2f);
        Material steel = gm.Mat("Sharpener Steel", new Color(0.8f, 0.82f, 0.86f), 0.9f, 0.8f);
        var tex = new Texture2D(2, 2);
        tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
        tex.Apply();
        Material glass = NightSky.Transparent("Gumball Glass", tex, new Color(0.85f, 0.95f, 1f, 0.3f));
        Vector3 b = new Vector3(g.from - 1.4f, g.top, 1.1f);
        LevelBuilder.Visual("Base", PrimitiveType.Cube, go.transform, b + new Vector3(0f, 0.8f, 0f), new Vector3(1.4f, 1.6f, 1.2f), red);
        LevelBuilder.Visual("Cap", PrimitiveType.Cylinder, go.transform, b + new Vector3(0f, 3.75f, 0f), new Vector3(0.6f, 0.15f, 0.6f), red);
        // كور جوا الزجاج
        for (int i = 0; i < 26; i++)
            LevelBuilder.Visual("Inside Ball", PrimitiveType.Sphere, go.transform, b + new Vector3(0f, 2.6f, 0f) + Random.insideUnitSphere * 0.75f, Vector3.one * 0.3f, g.colors[i % cols.Length], false);
        LevelBuilder.Visual("Glass", PrimitiveType.Sphere, go.transform, b + new Vector3(0f, 2.6f, 0f), Vector3.one * 2.1f, glass, false);
        LevelBuilder.Visual("Chute", PrimitiveType.Cube, go.transform, b + new Vector3(0.85f, 0.45f, -0.3f), new Vector3(0.6f, 0.2f, 0.5f), steel, false).transform.localRotation = Quaternion.Euler(0f, 0f, -25f);
        g.crank = new GameObject("Crank").transform;
        g.crank.SetParent(go.transform, false);
        g.crank.position = b + new Vector3(0f, 1f, -0.62f);
        LevelBuilder.Visual("Disc", PrimitiveType.Cylinder, g.crank, Vector3.zero, new Vector3(0.6f, 0.05f, 0.6f), steel).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        LevelBuilder.Visual("Arm", PrimitiveType.Cube, g.crank, new Vector3(0f, 0.25f, -0.08f), new Vector3(0.1f, 0.5f, 0.06f), steel, false);
        LevelBuilder.Visual("Knob", PrimitiveType.Sphere, g.crank, new Vector3(0f, 0.5f, -0.15f), Vector3.one * 0.22f, red, false);
        g.pile = new GameObject("Gumball Pile").transform;
        g.pile.SetParent(go.transform, false);
        g.pile.position = new Vector3((g.from + g.to) * 0.5f, g.top - 3f, 0f);
        g.fillCol = g.pile.gameObject.AddComponent<BoxCollider>();
        g.fillCol.size = new Vector3(g.to - g.from + 0.1f, 0.4f, 2f);
        g.fillCol.enabled = false;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time, dt = Time.deltaTime;
        if (fillT < 0f)
        {
            crank.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 2f) * 10f);
            if (KitchenFeatures.Touched(new Bounds(crank.position, new Vector3(1.4f, 1.6f, 4f))))
            {
                fillT = 0f;
                LevelSfx.Play("rattle", 0.9f);
                LevelSfx.Play("clank", 0.5f, 1.3f);
            }
        }
        else if (fillT < 1f)
        {
            fillT = Mathf.Min(1f, fillT + dt / 2.6f);
            crank.Rotate(0f, 0f, -540f * dt, Space.Self);
            // كور تطيح من المكينة للحفرة
            for (int k = 0; k < 3; k++)
            {
                Vector3 from3 = new Vector3(from - 0.3f, top + 0.4f, 0.6f);
                float tx = Random.Range(from + 0.3f, to - 0.3f);
                var ball = LevelBuilder.Visual("Gumball", PrimitiveType.Sphere, transform, from3, Vector3.one * Random.Range(0.4f, 0.55f), colors[Random.Range(0, colors.Length)], false).transform;
                var fl = ball.gameObject.AddComponent<GumballFall>();
                fl.Init(from3, new Vector3(tx, top - 3f + fillT * 2.9f + Random.Range(-0.2f, 0.2f), Random.Range(-0.9f, 0.9f)));
                balls.Add(ball);
            }
            pile.position = new Vector3((from + to) * 0.5f, top - 3f + fillT * 2.8f, 0f);
            if (fillT >= 1f)
            {
                pile.position = new Vector3((from + to) * 0.5f, top - 0.2f, 0f);
                fillCol.enabled = true;
                Fx.Sparkle(new Vector3((from + to) * 0.5f, top + 0.3f, 0f), new Color(1f, 0.8f, 0.9f), 20);
            }
        }

        bool wait = HoldHelper.Before(from) && !fillCol.enabled && !RowaidaController.BridgeCovers(from, to, top);
        if (wait && !prompted) { prompted = true; gm.UI.ShowMessage("", 3.2f, "مكينة علك! المسي يدها الحمرا بخط عشان تعبي الحفرة"); }
        HoldHelper.Hold(ref holding, wait);
    }
}

// كورة علك تطيح بقوس وتستقر
public class GumballFall : MonoBehaviour
{
    Vector3 a, b;
    float t;
    public void Init(Vector3 from, Vector3 to) { a = from; b = to; transform.position = from; }
    void Update()
    {
        if (t >= 1f) return;
        t = Mathf.Min(1f, t + Time.deltaTime / 0.7f);
        transform.position = Vector3.Lerp(a, b, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 1.2f;
    }
}

// ---------- بوابة عصا الحلاوة ----------

// عصا حلاوة مخططة تسد الطريق مثل حاجز. المسيها بخط ترتفع
public class CandyGate : MonoBehaviour
{
    float x, top, liftT = -1f;
    bool holding, prompted;
    Transform boom;

    public static void Create(Transform root, Vector2 spec, GameManager gm)
    {
        var go = new GameObject("Candy Gate");
        go.transform.SetParent(root, false);
        var g = go.AddComponent<CandyGate>();
        g.x = spec.x; g.top = spec.y;
        Material red = gm.Mat("Cane Red", new Color(0.95f, 0.2f, 0.28f), 0.7f);
        Material white = gm.Mat("Cane White", new Color(1f, 0.98f, 0.97f), 0.7f);
        // عمود مخطط على اليمين (المفصل)
        for (int i = 0; i < 6; i++)
            LevelBuilder.Visual("Post", PrimitiveType.Cylinder, go.transform, new Vector3(g.x + 0.9f, g.top + 0.2f + i * 0.3f, 0.9f), new Vector3(0.32f, 0.15f, 0.32f), i % 2 == 0 ? red : white);
        LevelBuilder.Visual("Post Top", PrimitiveType.Sphere, go.transform, new Vector3(g.x + 0.9f, g.top + 1.95f, 0.9f), Vector3.one * 0.4f, red);
        g.boom = new GameObject("Boom").transform;
        g.boom.SetParent(go.transform, false);
        g.boom.position = new Vector3(g.x + 0.9f, g.top + 1.2f, 0.9f);
        // العصا: قطع مخططة من المفصل لليسار، وطرفها معقوف
        for (int i = 0; i < 9; i++)
        {
            var seg = LevelBuilder.Visual("Cane", PrimitiveType.Cylinder, g.boom, new Vector3(-0.2f - i * 0.28f, 0f, -0.9f), new Vector3(0.22f, 0.14f, 0.22f), i % 2 == 0 ? red : white).transform;
            seg.localRotation = Quaternion.Euler(0f, 0f, 90f);
        }
        for (int i = 0; i < 5; i++)
        {
            float a = i / 4f * Mathf.PI;
            LevelBuilder.Visual("Hook", PrimitiveType.Sphere, g.boom, new Vector3(-2.7f - Mathf.Sin(a) * 0.3f, -0.3f + Mathf.Cos(a) * 0.3f, -0.9f), Vector3.one * 0.22f, i % 2 == 0 ? red : white, false);
        }
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        if (liftT < 0f)
        {
            boom.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 2f) * 1.5f);
            if (KitchenFeatures.Touched(new Bounds(new Vector3(x - 0.5f, top + 1.2f, 0f), new Vector3(3.2f, 1f, 4f))))
            {
                liftT = 0f;
                LevelSfx.Play("chime", 0.8f, 1.2f);
                LevelSfx.Play("whoosh", 0.5f);
            }
        }
        else if (liftT < 1f)
        {
            liftT = Mathf.Min(1f, liftT + Time.deltaTime / 0.6f);
            float k = 1f - Mathf.Pow(1f - liftT, 3f);
            boom.localRotation = Quaternion.Euler(0f, 0f, -k * 80f + Mathf.Sin(liftT * 20f) * (1f - liftT) * 6f);
            if (liftT >= 1f) Fx.Sparkle(new Vector3(x, top + 2f, 0f), new Color(1f, 0.6f, 0.7f), 14);
        }
        bool wait = liftT < 0.9f && HoldHelper.Before(x - 1.6f);
        if (wait && !prompted) { prompted = true; gm.UI.ShowMessage("", 3f, "بوابة عصا الحلاوة! المسيها بخط عشان ترتفع"); }
        HoldHelper.Hold(ref holding, wait);
    }
}

// ---------- مقلاة البان كيك ----------

// مقلاة على طرف الحفرة فيها بان كيك. المسي المقلاة بخط: تقلب البان كيك يطير ويكبر وينزل جسر فوق الحفرة
public class PancakeFlip : MonoBehaviour
{
    float from, to, top, flipT = -1f;
    bool holding, prompted;
    Transform pan, cake;
    BoxCollider bridge;

    public static void Create(Transform root, Vector3 spec, GameManager gm)
    {
        var go = new GameObject("Pancake Pan");
        go.transform.SetParent(root, false);
        var p = go.AddComponent<PancakeFlip>();
        p.from = spec.x; p.to = spec.y; p.top = spec.z;
        Material iron = gm.Mat("Pan Iron", new Color(0.22f, 0.22f, 0.25f), 0.6f, 0.4f);
        Material wood = gm.Mat("Tree Wood", new Color(0.62f, 0.45f, 0.28f), 0.25f);
        Material cakeM = gm.Mat("Pancake", new Color(0.92f, 0.65f, 0.32f), 0.4f);
        Material cakeTop = gm.Mat("Pancake Top", new Color(0.8f, 0.5f, 0.22f), 0.4f);
        Material butter = gm.Mat("Butter", new Color(1f, 0.92f, 0.5f), 0.6f);
        Material syrup = gm.Mat("Syrup", new Color(0.65f, 0.3f, 0.08f), 0.95f);
        Material stove = gm.Mat("Stove Black", new Color(0.15f, 0.14f, 0.16f), 0.6f, 0.3f);
        LevelBuilder.Visual("Mini Stove", PrimitiveType.Cube, go.transform, new Vector3(p.from - 1.4f, p.top + 0.25f, 1.1f), new Vector3(1.8f, 0.5f, 1.4f), stove);
        p.pan = new GameObject("Pan").transform;
        p.pan.SetParent(go.transform, false);
        p.pan.position = new Vector3(p.from - 1.4f, p.top + 0.6f, 1.1f);
        LevelBuilder.Visual("Pan", PrimitiveType.Cylinder, p.pan, Vector3.zero, new Vector3(1.4f, 0.1f, 1.2f), iron);
        LevelBuilder.Visual("Handle", PrimitiveType.Cube, p.pan, new Vector3(-1.1f, 0.05f, 0f), new Vector3(1f, 0.1f, 0.15f), wood);
        p.cake = new GameObject("Pancake").transform;
        p.cake.SetParent(go.transform, false);
        LevelBuilder.Visual("Cake", PrimitiveType.Cylinder, p.cake, Vector3.zero, new Vector3(1f, 0.12f, 1f), cakeM);
        LevelBuilder.Visual("Cake Top", PrimitiveType.Cylinder, p.cake, new Vector3(0f, 0.1f, 0f), new Vector3(0.95f, 0.02f, 0.95f), cakeTop, false);
        LevelBuilder.Visual("Butter", PrimitiveType.Cube, p.cake, new Vector3(0f, 0.18f, 0f), new Vector3(0.22f, 0.1f, 0.22f), butter, false);
        LevelBuilder.Visual("Syrup", PrimitiveType.Sphere, p.cake, new Vector3(0.15f, 0.12f, 0.1f), new Vector3(0.5f, 0.04f, 0.4f), syrup, false);
        p.cake.position = p.pan.position + Vector3.up * 0.15f;
        p.bridge = p.cake.gameObject.AddComponent<BoxCollider>();
        p.bridge.size = new Vector3(1f, 0.24f, 1f);
        p.bridge.enabled = false;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time, dt = Time.deltaTime, cx = (from + to) * 0.5f, w = to - from;
        if (flipT < 0f)
        {
            cake.localScale = Vector3.one * (1f + Mathf.Sin(t * 6f) * 0.02f);
            if (Random.value < 0.01f) LevelSfx.Play("sizzle", 0.2f);
            if (KitchenFeatures.Touched(new Bounds(pan.position + new Vector3(-0.5f, 0f, 0f), new Vector3(2.8f, 1.6f, 4f))))
            {
                flipT = 0f;
                LevelSfx.Play("whoosh", 0.8f);
                LevelSfx.Play("sizzle", 0.6f);
            }
        }
        else if (flipT < 1f)
        {
            flipT = Mathf.Min(1f, flipT + dt / 1.3f);
            float k = flipT;
            pan.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Mathf.Min(1f, k * 4f) * Mathf.PI) * 30f);
            Vector3 a = pan.position + Vector3.up * 0.15f, b = new Vector3(cx, top - 0.12f, 0f);
            cake.position = Vector3.Lerp(a, b, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 4f;
            cake.localRotation = Quaternion.Euler(0f, 0f, k * 540f);
            float grow = Mathf.Lerp(1f, (w + 0.6f), Mathf.SmoothStep(0f, 1f, k));
            cake.localScale = new Vector3(grow, 1f + Mathf.Sin(k * Mathf.PI) * 0.3f, Mathf.Lerp(1f, 1.6f, k));
            if (flipT >= 1f)
            {
                cake.localRotation = Quaternion.identity;
                bridge.enabled = true;
                LevelSfx.Play("squish", 0.8f);
                Fx.Dust(new Vector3(cx, top, 0f), 0.6f);
                Fx.Sparkle(new Vector3(cx, top + 0.3f, 0f), new Color(1f, 0.85f, 0.5f), 16);
            }
        }
        bool wait = HoldHelper.Before(from) && !bridge.enabled && !RowaidaController.BridgeCovers(from, to, top);
        if (wait && !prompted) { prompted = true; gm.UI.ShowMessage("", 3.2f, "المسي المقلاة بخط تقلب البان كيك... ويصير جسر!"); }
        HoldHelper.Hold(ref holding, wait);
    }
}

// ---------- الفرن ----------

// فرن على يمين الحفرة: بابه ينفتح وينسكر لحاله، ولما ينفتح ينبطح فوق الحفرة جسر. المسي زر الفرن بخط يظل مفتوح
public class OvenDoor : MonoBehaviour
{
    float from, to, top, period, start;
    bool holding, prompted, stuckOpen;
    Transform hinge, knob;
    BoxCollider bridge;
    Light glow;

    float Local => Mathf.Repeat(Time.time - start, period * 2f);
    // 0 = مسكر (واقف)، 1 = مفتوح (منبطح)
    float Open01 => stuckOpen ? 1f : Local < period ? 0f : Local < period + 0.6f ? (Local - period) / 0.6f : Local < period * 2f - 0.6f ? 1f : (period * 2f - Local) / 0.6f;
    float OpenLeft => stuckOpen ? 99f : Local >= period + 0.6f && Local < period * 2f - 0.6f ? period * 2f - 0.6f - Local : 0f;

    public static void Create(Transform root, Vector3 spec, GameManager gm)
    {
        var go = new GameObject("Oven");
        go.transform.SetParent(root, false);
        var o = go.AddComponent<OvenDoor>();
        o.from = spec.x; o.to = spec.y; o.top = 0f; o.period = spec.z;
        o.start = Time.time;
        float w = o.to - o.from;
        Material body = gm.Mat("Oven Body", new Color(0.92f, 0.45f, 0.38f), 0.5f, 0.2f);
        Material black = gm.Mat("Stove Black", new Color(0.15f, 0.14f, 0.16f), 0.6f, 0.3f);
        Material steel = gm.Mat("Sharpener Steel", new Color(0.8f, 0.82f, 0.86f), 0.9f, 0.8f);
        Material heat = gm.Mat("Oven Heat", new Color(1.8f, 0.7f, 0.2f), 0.5f);
        Material cookie = gm.Mat("Cookie", new Color(0.85f, 0.6f, 0.3f), 0.3f);
        Vector3 b = new Vector3(o.to + 1.5f, o.top, 1.5f);
        LevelBuilder.Visual("Oven Body", PrimitiveType.Cube, go.transform, b + new Vector3(0f, 1.6f, 0f), new Vector3(3f, 3.2f, 1.8f), body);
        LevelBuilder.Visual("Oven Mouth", PrimitiveType.Cube, go.transform, b + new Vector3(-1.2f, 1.1f, 0f), new Vector3(0.7f, 1.6f, 1.5f), heat, false);
        LevelBuilder.Visual("Tray", PrimitiveType.Cube, go.transform, b + new Vector3(-0.9f, 0.8f, 0f), new Vector3(0.9f, 0.05f, 1.3f), steel, false);
        for (int i = 0; i < 3; i++) LevelBuilder.Visual("Cookie", PrimitiveType.Cylinder, go.transform, b + new Vector3(-0.9f, 0.86f, -0.4f + i * 0.4f), new Vector3(0.3f, 0.04f, 0.3f), cookie, false);
        LevelBuilder.Visual("Top Panel", PrimitiveType.Cube, go.transform, b + new Vector3(0f, 3.35f, 0f), new Vector3(3.1f, 0.3f, 1.9f), black);
        o.knob = LevelBuilder.Visual("Knob", PrimitiveType.Cylinder, go.transform, b + new Vector3(-0.6f, 3.35f, -0.98f), new Vector3(0.35f, 0.08f, 0.35f), steel).transform;
        o.knob.localRotation = Quaternion.Euler(90f, 0f, 0f);
        LevelBuilder.Visual("Knob 2", PrimitiveType.Cylinder, go.transform, b + new Vector3(0.4f, 3.35f, -0.98f), new Vector3(0.3f, 0.06f, 0.3f), steel, false).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        // الباب: مفصله تحت على يسار الفرن عند طرف الحفرة
        o.hinge = new GameObject("Door Hinge").transform;
        o.hinge.SetParent(go.transform, false);
        o.hinge.position = new Vector3(o.to, o.top - 0.15f, 0f);
        LevelBuilder.Visual("Door", PrimitiveType.Cube, o.hinge, new Vector3(-(w + 0.3f) * 0.5f, 0f, 0f), new Vector3(w + 0.3f, 0.3f, 1.6f), body);
        LevelBuilder.Visual("Window", PrimitiveType.Cube, o.hinge, new Vector3(-(w + 0.3f) * 0.5f, 0.16f, 0f), new Vector3(w * 0.6f, 0.02f, 1f), gm.Mat("Oven Window", new Color(0.35f, 0.2f, 0.15f), 0.9f), false);
        LevelBuilder.Visual("Door Handle", PrimitiveType.Cube, o.hinge, new Vector3(-w - 0.05f, 0.2f, 0f), new Vector3(0.12f, 0.12f, 1.3f), steel, false);
        var c = new GameObject("Door Bridge");
        c.transform.SetParent(o.hinge, false);
        c.transform.localPosition = new Vector3(-(w + 0.3f) * 0.5f, 0f, 0f);
        o.bridge = c.AddComponent<BoxCollider>();
        o.bridge.size = new Vector3(w + 0.3f, 0.3f, 1.6f);
        o.bridge.enabled = false;
        o.glow = new GameObject("Oven Glow").AddComponent<Light>();
        o.glow.transform.SetParent(go.transform, false);
        o.glow.transform.position = b + new Vector3(-1.8f, 1.2f, -0.6f);
        o.glow.type = LightType.Point;
        o.glow.color = new Color(1f, 0.6f, 0.25f);
        o.glow.range = 4f;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        RowaidaController r = gm.Rowaida;
        bool rowaidaOn = r != null && r.transform.position.x > from - 0.2f && r.transform.position.x < to + 0.2f;
        // رويدا على الباب؟ ما يتسكر
        if (rowaidaOn && Local > period * 2f - 0.8f) start += Time.deltaTime;

        if (!stuckOpen && KitchenFeatures.Touched(new Bounds(knob.position, new Vector3(1.2f, 1.2f, 4f))))
        {
            stuckOpen = true;
            LevelSfx.Play("clank", 0.6f, 1.5f);
            gm.UI.ShowMessage("", 2.2f, "الفرن صار مفتوح على طول");
        }
        float open = Open01;
        hinge.localRotation = Quaternion.Euler(0f, 0f, -90f * (1f - open));
        bridge.enabled = open > 0.98f;
        if (stuckOpen) knob.localRotation = Quaternion.Euler(90f, 0f, 0f) * Quaternion.Euler(0f, 90f, 0f);
        glow.intensity = 0.5f + open * 2.5f;
        if (open > 0.5f && Random.value < 0.05f) SmokeCloud.Puff(new Vector3(to + 0.3f, top + 1.2f, 0.8f), Random.Range(0.5f, 0.9f));
        if (open > 0.05f && open < 0.08f && r != null && Mathf.Abs(r.transform.position.x - to) < 12f) LevelSfx.Play("clank", 0.35f, 0.7f);

        float cross = (to - from + 1.6f) / (r != null ? r.walkSpeed : 2.8f);
        bool ready = bridge.enabled && (OpenLeft > cross || rowaidaOn);
        bool wait = HoldHelper.Before(from) && !ready && !RowaidaController.BridgeCovers(from, to, top);
        if (wait && !prompted) { prompted = true; gm.UI.ShowMessage("", 3.4f, "باب الفرن يصير جسر لما ينفتح! استني... أو المسي زر الفرن بخط"); }
        HoldHelper.Hold(ref holding, wait);
    }
}

// ---------- السير المتحرك ----------

// سير متحرك على الطريق يرجع رويدا لورا، وعليه كب كيك. المسي المفتاح (يمين السير) بخط: ينعكس ويوديها لقدام
public class Conveyor : MonoBehaviour
{
    float from, to, top, dir = -1f;
    bool holding, prompted, pushing;
    Material belt;
    Transform lever, lamp;
    readonly List<Transform> cupcakes = new List<Transform>();
    readonly List<Transform> rollers = new List<Transform>();
    Material red, green;
    AudioSource motor;

    public static void Create(Transform root, Vector3 spec, GameManager gm)
    {
        var go = new GameObject("Conveyor");
        go.transform.SetParent(root, false);
        var c = go.AddComponent<Conveyor>();
        c.from = spec.x; c.to = spec.y; c.top = spec.z;
        float w = c.to - c.from, cx = (c.from + c.to) * 0.5f;
        var tex = new Texture2D(16, 4, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat };
        for (int x = 0; x < 16; x++)
            for (int y = 0; y < 4; y++)
                tex.SetPixel(x, y, x % 8 < 2 ? new Color(0.5f, 0.5f, 0.55f) : new Color(0.25f, 0.25f, 0.3f));
        tex.Apply();
        c.belt = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "Belt" };
        c.belt.SetTexture("_BaseMap", tex);
        c.belt.SetColor("_BaseColor", Color.white);
        c.belt.SetTextureScale("_BaseMap", new Vector2(w / 1f, 1f));
        var surf = LevelBuilder.Visual("Belt", PrimitiveType.Quad, go.transform, new Vector3(cx, c.top + 0.06f, 0f), new Vector3(w, 1.6f, 1f), c.belt, false).transform;
        surf.localRotation = Quaternion.Euler(90f, 0f, 0f);
        Material steel = gm.Mat("Sharpener Steel", new Color(0.8f, 0.82f, 0.86f), 0.9f, 0.8f);
        Material rail = gm.Mat("Belt Rail", new Color(0.95f, 0.75f, 0.3f), 0.6f, 0.3f);
        for (int s = -1; s <= 1; s += 2)
            LevelBuilder.Visual("Rail", PrimitiveType.Cube, go.transform, new Vector3(cx, c.top + 0.12f, s * 0.85f), new Vector3(w + 0.3f, 0.2f, 0.1f), rail);
        for (int i = 0; i <= Mathf.RoundToInt(w); i++)
        {
            var rl = LevelBuilder.Visual("Roller", PrimitiveType.Cylinder, go.transform, new Vector3(c.from + i * w / Mathf.Round(w), c.top - 0.05f, -0.86f), new Vector3(0.18f, 0.02f, 0.18f), steel, false).transform;
            rl.localRotation = Quaternion.Euler(90f, 0f, 0f);
            c.rollers.Add(rl);
        }
        // كب كيك راكبين على السير (ورا شوي)
        Material cup = gm.Mat("Cupcake Cup", new Color(0.4f, 0.75f, 1f), 0.4f);
        Material[] icing = { gm.Mat("Icing Pink", new Color(1f, 0.65f, 0.8f), 0.5f), gm.Mat("Icing Mint", new Color(0.6f, 0.95f, 0.8f), 0.5f), gm.Mat("Icing Choco", new Color(0.5f, 0.3f, 0.2f), 0.5f) };
        Material cherry = gm.Mat("Apple", new Color(0.95f, 0.25f, 0.25f), 0.6f);
        for (int i = 0; i < 4; i++)
        {
            var ck = new GameObject("Cupcake").transform;
            ck.SetParent(go.transform, false);
            ck.position = new Vector3(c.from + (i + 0.5f) * w / 4f, c.top + 0.06f, 0.55f);
            LevelBuilder.Visual("Cup", PrimitiveType.Cylinder, ck, new Vector3(0f, 0.18f, 0f), new Vector3(0.4f, 0.18f, 0.4f), cup, false);
            LevelBuilder.Visual("Icing", PrimitiveType.Sphere, ck, new Vector3(0f, 0.42f, 0f), new Vector3(0.48f, 0.35f, 0.48f), icing[i % 3], false);
            LevelBuilder.Visual("Cherry", PrimitiveType.Sphere, ck, new Vector3(0f, 0.64f, 0f), Vector3.one * 0.12f, cherry, false);
            c.cupcakes.Add(ck);
        }
        // المفتاح
        c.red = gm.Mat("Lamp Red", new Color(1.6f, 0.2f, 0.2f), 0.5f);
        c.green = gm.Mat("Lamp Green", new Color(0.3f, 1.6f, 0.4f), 0.5f);
        Material box = gm.Mat("Switch Box", new Color(0.95f, 0.8f, 0.3f), 0.5f);
        LevelBuilder.Visual("Switch Box", PrimitiveType.Cube, go.transform, new Vector3(c.to + 0.9f, c.top + 0.7f, 1f), new Vector3(0.7f, 1.4f, 0.5f), box);
        c.lever = new GameObject("Lever").transform;
        c.lever.SetParent(go.transform, false);
        c.lever.position = new Vector3(c.to + 0.9f, c.top + 1.1f, 0.7f);
        LevelBuilder.Visual("Stick", PrimitiveType.Cylinder, c.lever, new Vector3(0f, 0.35f, 0f), new Vector3(0.07f, 0.35f, 0.07f), steel, false);
        LevelBuilder.Visual("Ball", PrimitiveType.Sphere, c.lever, new Vector3(0f, 0.72f, 0f), Vector3.one * 0.2f, gm.Mat("Tower Red", new Color(0.85f, 0.2f, 0.22f), 0.35f), false);
        c.lever.localRotation = Quaternion.Euler(0f, 0f, 35f);
        c.lamp = LevelBuilder.Visual("Lamp", PrimitiveType.Sphere, go.transform, new Vector3(c.to + 0.9f, c.top + 0.4f, 0.72f), Vector3.one * 0.18f, c.red, false).transform;
        c.motor = LevelSfx.LoopSource(go, "motor", 0f);
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time, dt = Time.deltaTime;
        belt.SetTextureOffset("_BaseMap", new Vector2(-dir * t * 1.2f, 0f));
        foreach (Transform rl in rollers) rl.Rotate(0f, dir * 400f * dt, 0f, Space.Self);
        for (int i = 0; i < cupcakes.Count; i++)
        {
            Vector3 p = cupcakes[i].position;
            p.x += dir * 1.2f * dt;
            if (p.x < from + 0.2f) p.x = to - 0.2f;
            if (p.x > to - 0.2f) p.x = from + 0.2f;
            cupcakes[i].position = p;
            cupcakes[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 8f + i) * 3f);
        }
        if (dir < 0f && KitchenFeatures.Touched(new Bounds(lever.position + Vector3.up * 0.4f, new Vector3(1.4f, 1.8f, 4f))))
        {
            dir = 1f;
            lever.localRotation = Quaternion.Euler(0f, 0f, -35f);
            lamp.GetComponent<MeshRenderer>().sharedMaterial = green;
            LevelSfx.Play("clank", 0.7f, 1.3f);
            Fx.Sparkle(lever.position, new Color(0.4f, 1f, 0.5f), 10);
        }
        RowaidaController r = gm.Rowaida;
        bool on = r != null && r.transform.position.x > from && r.transform.position.x < to && r.transform.position.y - top < 1.3f;
        if (on) { r.WalkBonus = dir < 0f ? -4.2f : 2f; pushing = true; }
        else if (pushing && r != null) { r.WalkBonus = 0f; pushing = false; }
        if (motor != null) motor.volume = r != null ? Mathf.Clamp01(1f - Mathf.Abs(r.transform.position.x - (from + to) * 0.5f) / 10f) * 0.35f : 0f;

        bool wait = dir < 0f && HoldHelper.Before(from);
        if (wait && !prompted) { prompted = true; gm.UI.ShowMessage("", 3.2f, "السير يرجعك لورا! المسي المفتاح اللي بعده بخط عشان ينعكس"); }
        HoldHelper.Hold(ref holding, wait);
    }

    void OnDisable()
    {
        GameManager gm = GameManager.Instance;
        if (pushing && gm != null && gm.Rowaida != null) gm.Rowaida.WalkBonus = 0f;
    }
}

// ---------- دببة الجيلي ----------

// دببة جيلي تحرس حفرة: أي خط ينرسم قريب منها تروح له وتاكله. ارسمي دايرة: تصير حلاوة ملفوفة، والدببة تروح تاكلها شوي... وقتها ارسمي الجسر
public class JellyBears : MonoBehaviour
{
    class Bear { public Transform t; public Vector3 home; public float phase; }
    const float BusyTime = 9f;
    float from, to, top, range, busyUntil;
    bool holding, prompted;
    readonly List<Bear> bears = new List<Bear>();
    readonly Dictionary<InkMark, float> spotted = new Dictionary<InkMark, float>();
    Transform candy;
    InkMark target;

    bool Busy => candy != null && Time.time < busyUntil;

    public static void Create(Transform root, Vector4 spec, GameManager gm)
    {
        var go = new GameObject("Jelly Bears");
        go.transform.SetParent(root, false);
        var j = go.AddComponent<JellyBears>();
        j.from = spec.x; j.to = spec.y; j.top = spec.z; j.range = spec.w;
        Color[] cols = { new Color(1f, 0.25f, 0.3f), new Color(0.35f, 0.85f, 0.35f), new Color(1f, 0.75f, 0.15f) };
        Material black = gm.Mat("Monster Pupil", new Color(0.05f, 0.04f, 0.06f), 0.9f);
        for (int i = 0; i < 3; i++)
        {
            var tex = new Texture2D(2, 2);
            tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            tex.Apply();
            Material m = NightSky.Transparent("Jelly Bear " + i, tex, new Color(cols[i].r * 1.2f, cols[i].g * 1.2f, cols[i].b * 1.2f, 0.82f));
            var b = new GameObject("Jelly Bear").transform;
            b.SetParent(go.transform, false);
            Vector3 home = new Vector3(i == 1 ? j.to + 1f : j.from - 1.2f - i * 0.8f, j.top, i == 1 ? 1.2f : 1f + i * 0.2f);
            b.position = home;
            LevelBuilder.Visual("Body", PrimitiveType.Sphere, b, new Vector3(0f, 0.5f, 0f), new Vector3(0.75f, 0.85f, 0.6f), m, false);
            LevelBuilder.Visual("Head", PrimitiveType.Sphere, b, new Vector3(0f, 1.12f, 0f), new Vector3(0.6f, 0.55f, 0.5f), m, false);
            for (int s = -1; s <= 1; s += 2)
            {
                LevelBuilder.Visual("Ear", PrimitiveType.Sphere, b, new Vector3(s * 0.22f, 1.4f, 0f), Vector3.one * 0.2f, m, false);
                LevelBuilder.Visual("Arm", PrimitiveType.Sphere, b, new Vector3(s * 0.38f, 0.65f, -0.05f), new Vector3(0.2f, 0.3f, 0.2f), m, false);
                LevelBuilder.Visual("Leg", PrimitiveType.Sphere, b, new Vector3(s * 0.2f, 0.12f, -0.05f), new Vector3(0.25f, 0.22f, 0.25f), m, false);
                LevelBuilder.Visual("Eye", PrimitiveType.Sphere, b, new Vector3(s * 0.12f, 1.17f, -0.24f), Vector3.one * 0.07f, black, false);
            }
            LevelBuilder.Visual("Nose", PrimitiveType.Sphere, b, new Vector3(0f, 1.05f, -0.26f), new Vector3(0.12f, 0.08f, 0.06f), black, false);
            Material blush = gm.Mat("Bunny Pink", new Color(1f, 0.7f, 0.75f), 0.3f);
            for (int s = -1; s <= 1; s += 2) LevelBuilder.Visual("Blush", PrimitiveType.Sphere, b, new Vector3(s * 0.2f, 1.05f, -0.22f), new Vector3(0.12f, 0.07f, 0.04f), blush, false);
            var smile = LevelBuilder.Visual("Smile", PrimitiveType.Capsule, b, new Vector3(0f, 0.97f, -0.25f), new Vector3(0.04f, 0.08f, 0.03f), black, false).transform;
            smile.localRotation = Quaternion.Euler(0f, 0f, 90f);
            LevelBuilder.Visual("Belly", PrimitiveType.Sphere, b, new Vector3(0f, 0.45f, -0.22f), new Vector3(0.45f, 0.5f, 0.15f), m, false);
            j.bears.Add(new Bear { t = b, home = home, phase = i * 1.3f });
        }
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time, dt = Time.deltaTime, cx = (from + to) * 0.5f;
        RowaidaController r = gm.Rowaida;
        bool rowaidaOn = r != null && r.transform.position.x > from - 0.3f && r.transform.position.x < to + 0.3f;

        // دايرة؟ تصير حلاوة ملفوفة على الأرض
        InkMark circle = KitchenFeatures.ClosedNear(cx - range - 5f, cx + range + 5f);
        if (circle != null)
        {
            Vector3 c = circle.GetComponent<MeshRenderer>().bounds.center;
            Destroy(circle.gameObject);
            if (candy != null) Destroy(candy.gameObject);
            candy = MakeCandy(c.x < from ? Mathf.Min(c.x, from - 1.5f) : Mathf.Max(c.x, to + 1.5f), gm);
            busyUntil = t + BusyTime;
            LevelSfx.Play("pop", 0.8f, 1.2f);
            gm.UI.ShowMessage("", 2.2f, "الدببة راحت للحلاوة! بسرعة ارسمي الجسر");
        }
        if (candy != null && t > busyUntil && !rowaidaOn) { Destroy(candy.gameObject); candy = null; }

        // خط قريب؟ (إذا مو مشغولة) دب يروح ياكله
        if (!Busy)
        {
            foreach (InkMark m in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
            {
                if (m.closed || m.beingEaten) continue;
                var mr = m.GetComponent<MeshRenderer>();
                if (mr == null || mr.bounds.max.x < cx - range || mr.bounds.min.x > cx + range) continue;
                if (rowaidaOn && mr.bounds.min.x < from + 0.5f && mr.bounds.max.x > to - 0.5f) continue;   // رويدا عليه
                if (!spotted.ContainsKey(m)) spotted[m] = t;
                if (t - spotted[m] > 0.9f && target == null) target = m;
            }
        }
        if (target != null && (Busy || rowaidaOn)) target = null;

        for (int i = 0; i < bears.Count; i++)
        {
            Bear b = bears[i];
            Vector3 goal = b.home;
            if (Busy) goal = candy.position + new Vector3(Mathf.Cos(i * 2.1f) * 0.9f, 0f, Mathf.Sin(i * 2.1f) * 0.7f + 0.5f);
            else if (target != null && i == 0)
            {
                Bounds tb = target.GetComponent<MeshRenderer>().bounds;
                goal = new Vector3(Mathf.Clamp(tb.center.x, from - 1f, to + 1f), top, 0.6f);
            }
            Vector3 p = b.t.position;
            p = Vector3.MoveTowards(p, goal, 3f * dt);
            b.t.position = p;
            bool moving = (goal - p).sqrMagnitude > 0.01f;
            float wob = Mathf.Sin(t * (moving ? 12f : 3f) + b.phase);
            b.t.localScale = new Vector3(1f + wob * 0.05f, 1f - wob * 0.05f, 1f);
            b.t.localRotation = Quaternion.Euler(0f, 0f, moving ? wob * 8f : 0f);
            if (Busy && Random.value < 0.01f) LevelSfx.Play("munch", 0.3f, Random.Range(1.1f, 1.4f));
        }
        // الدب وصل للخط: ياكله
        if (target != null && !Busy && (bears[0].t.position - new Vector3(Mathf.Clamp(target.GetComponent<MeshRenderer>().bounds.center.x, from - 1f, to + 1f), top, 0.6f)).sqrMagnitude < 0.05f)
        {
            Fx.Sparkle(target.GetComponent<MeshRenderer>().bounds.center, new Color(1f, 0.5f, 0.6f), 14);
            LevelSfx.Play("munch", 0.8f);
            LevelSfx.Play("wobble", 0.5f);
            spotted.Remove(target);
            Destroy(target.gameObject);
            target = null;
            gm.UI.ShowMessage("", 2.4f, "الدب أكل خطك! ارسمي دايرة حلاوة تلهيهم أول");
        }

        bool wait = HoldHelper.Before(from) && !(RowaidaController.BridgeCovers(from, to, top) && (Busy || rowaidaOn));
        if (wait && !prompted) { prompted = true; gm.UI.ShowMessage("", 3.4f, "دببة الجيلي تاكل الخطوط! ارسمي دايرة تصير حلاوة تلهيهم... وبعدين الجسر"); }
        HoldHelper.Hold(ref holding, wait);
    }

    Transform MakeCandy(float x, GameManager gm)
    {
        var c = new GameObject("Wrapped Candy").transform;
        c.SetParent(transform, false);
        c.position = new Vector3(x, top + 0.3f, 0.6f);
        Material wrap = gm.Mat("Candy Wrap", new Color(1f, 0.5f, 0.75f), 0.8f);
        Material stripe = gm.Mat("Cane White", new Color(1f, 0.98f, 0.97f), 0.7f);
        LevelBuilder.Visual("Candy", PrimitiveType.Sphere, c, Vector3.zero, new Vector3(0.6f, 0.45f, 0.45f), wrap, false);
        LevelBuilder.Visual("Stripe", PrimitiveType.Cube, c, new Vector3(0f, 0f, -0.2f), new Vector3(0.1f, 0.42f, 0.05f), stripe, false);
        for (int s = -1; s <= 1; s += 2)
        {
            var end = LevelBuilder.Visual("Twist", PrimitiveType.Cube, c, new Vector3(s * 0.42f, 0f, 0f), new Vector3(0.25f, 0.35f, 0.3f), wrap, false).transform;
            end.localRotation = Quaternion.Euler(45f, 0f, s * 15f);
        }
        Fx.Sparkle(c.position, new Color(1f, 0.6f, 0.8f), 14);
        return c;
    }
}

// ---------- الكيكة ----------

// كيكة طوابق (جدار عالي) فوقها شموع تسد الطريق. ارسمي درج كريمة تطلعين عليه، وطفي كل شمعة بخط عليها
public class CakeTower : MonoBehaviour
{
    float x0, x1, top;
    bool holding, prompted;
    readonly List<Transform> flames = new List<Transform>();
    readonly List<float> candleX = new List<float>();
    readonly List<bool> lit = new List<bool>();
    Light glow;

    public static void Create(Transform root, Vector3 spec, GameManager gm)
    {
        var go = new GameObject("Cake Tower");
        go.transform.SetParent(root, false);
        var c = go.AddComponent<CakeTower>();
        c.x0 = spec.x; c.x1 = spec.y; c.top = spec.z;
        float w = c.x1 - c.x0, cx = (c.x0 + c.x1) * 0.5f;
        Material sponge = gm.Mat("Cake Sponge", new Color(1f, 0.85f, 0.65f), 0.3f);
        Material cream = gm.Mat("Cake Cream", new Color(1f, 0.95f, 0.97f), 0.5f);
        Material pinkC = gm.Mat("Cake Pink", new Color(1f, 0.7f, 0.82f), 0.5f);
        Material candleM = gm.Mat("Candle", new Color(0.6f, 0.85f, 1f), 0.4f);
        Material flame = gm.Mat("Candle Flame", new Color(2f, 1.3f, 0.3f), 0.5f);
        Material cherry = gm.Mat("Apple", new Color(0.95f, 0.25f, 0.25f), 0.6f);
        // طبقات كريمة على وجه الكيكة، وقطرات
        for (int i = 0; i < 3; i++)
            LevelBuilder.Visual("Cream Band", PrimitiveType.Cube, go.transform, new Vector3(cx, c.top * (0.3f + i * 0.33f), -2.02f), new Vector3(w, 0.18f, 0.05f), i % 2 == 0 ? cream : pinkC, false);
        LevelBuilder.Visual("Frosting", PrimitiveType.Cube, go.transform, new Vector3(cx, c.top + 0.05f, 0f), new Vector3(w + 0.1f, 0.12f, 4.05f), pinkC, false);
        for (int i = 0; i < Mathf.RoundToInt(w * 1.5f); i++)
            LevelBuilder.Visual("Drip", PrimitiveType.Capsule, go.transform, new Vector3(c.x0 + 0.3f + i * 0.66f, c.top - 0.25f - (i % 3) * 0.1f, -2.03f), new Vector3(0.22f, 0.25f + (i % 2) * 0.1f, 0.05f), pinkC, false);
        for (int i = 0; i < 6; i++)
            LevelBuilder.Visual("Cherry", PrimitiveType.Sphere, go.transform, new Vector3(c.x0 + 0.5f + i * w / 6f, c.top + 0.2f, 1.6f), Vector3.one * 0.32f, cherry, false);
        // الشموع على الطريق فوق الكيكة
        for (float x = c.x0 + 1.6f; x < c.x1 - 0.8f; x += 2.2f)
        {
            LevelBuilder.Visual("Candle", PrimitiveType.Cylinder, go.transform, new Vector3(x, c.top + 0.65f, 0f), new Vector3(0.28f, 0.6f, 0.28f), candleM);
            for (int k = 0; k < 3; k++)
                LevelBuilder.Visual("Stripe", PrimitiveType.Cylinder, go.transform, new Vector3(x, c.top + 0.3f + k * 0.35f, 0f), new Vector3(0.3f, 0.04f, 0.3f), pinkC, false);
            c.flames.Add(LevelBuilder.Visual("Flame", PrimitiveType.Sphere, go.transform, new Vector3(x, c.top + 1.45f, 0f), new Vector3(0.22f, 0.4f, 0.22f), flame, false).transform);
            c.candleX.Add(x);
            c.lit.Add(true);
        }
        c.glow = new GameObject("Candle Glow").AddComponent<Light>();
        c.glow.transform.SetParent(go.transform, false);
        c.glow.transform.position = new Vector3(cx, c.top + 1.8f, -0.8f);
        c.glow.type = LightType.Point;
        c.glow.color = new Color(1f, 0.75f, 0.4f);
        c.glow.range = 5f;
        c.glow.intensity = 1.6f;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time;
        int litCount = 0;
        bool wait = false;
        RowaidaController r = gm.Rowaida;
        for (int i = 0; i < flames.Count; i++)
        {
            if (!lit[i]) continue;
            litCount++;
            Transform f = flames[i];
            f.localScale = new Vector3(0.22f, 0.4f + Mathf.Sin(t * 13f + i) * 0.06f, 0.22f);
            f.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 7f + i) * 8f);
            // خط على الشمعة؟ تنطفي
            if (KitchenFeatures.Touched(new Bounds(f.position, new Vector3(1f, 1.4f, 4f))))
            {
                lit[i] = false;
                f.gameObject.SetActive(false);
                LevelSfx.Play("whoosh", 0.5f, 1.6f);
                for (int k = 0; k < 3; k++) SmokeCloud.Puff(f.position + Vector3.up * (0.3f + k * 0.3f), 0.4f + k * 0.15f);
                continue;
            }
            if (r != null && gm.IsPlaying && r.transform.position.y > top && r.transform.position.x > candleX[i] - 1.5f && r.transform.position.x < candleX[i] - 0.5f) wait = true;
        }
        glow.intensity = litCount * 0.8f;
        if (wait && !prompted) { prompted = true; gm.UI.ShowMessage("", 3f, "شمعة بالطريق! طفيها بخط عليها"); }
        HoldHelper.Hold(ref holding, wait);
    }
}

// ---------- منظر المطبخ وأرض الحلويات ----------

// 1 = مطبخ (جدار بلاط، رفوف ومرطبانات، أدوات معلقة، شباك وستارة، زرع)، 2 = أرض الحلويات (تلال جيلي، شجر مصاص، عصي حلاوة، غيوم غزل بنات)
public class KitchenScenery : MonoBehaviour
{
    readonly List<Transform> sway = new List<Transform>();
    readonly List<Transform> spin = new List<Transform>();
    readonly List<Transform> floaters = new List<Transform>();
    readonly List<Vector3> floatHome = new List<Vector3>();

    public static void Create(Transform root, LevelData data, GameManager gm, System.Random rng)
    {
        var go = new GameObject("Kitchen Scenery");
        go.transform.SetParent(root, false);
        var s = go.AddComponent<KitchenScenery>();
        float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        float minX = float.MaxValue, maxX = float.MinValue;
        foreach (Block b in data.blocks) { minX = Mathf.Min(minX, b.center.x - b.size.x * 0.5f); maxX = Mathf.Max(maxX, b.center.x + b.size.x * 0.5f); }
        if (data.kitchenScenery == 1) s.BuildKitchen(go.transform, minX, maxX, gm, rng, R);
        else s.BuildCandy(go.transform, minX, maxX, data, gm, rng, R);
        KitchenExtras.Build(go.transform, data, minX, maxX, gm, rng);
    }

    void BuildKitchen(Transform p, float minX, float maxX, GameManager gm, System.Random rng, System.Func<float, float, float> R)
    {
        // جدار بلاط ورا
        var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat };
        for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
                tex.SetPixel(x, y, x % 16 == 0 || y % 16 == 0 ? new Color(0.75f, 0.85f, 0.85f) : ((x / 16 + y / 16) % 2 == 0 ? new Color(0.97f, 0.98f, 0.97f) : new Color(0.75f, 0.9f, 0.9f)));
        tex.Apply();
        var wall = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "Tile Wall" };
        wall.SetTexture("_BaseMap", tex);
        wall.SetColor("_BaseColor", Color.white);
        float len = maxX - minX + 40f;
        wall.SetTextureScale("_BaseMap", new Vector2(len / 2.4f, 14f / 2.4f));
        LevelBuilder.Visual("Tile Wall", PrimitiveType.Quad, p, new Vector3((minX + maxX) * 0.5f, 3f, 9f), new Vector3(len, 14f, 1f), wall, false);
        Material wood = gm.Mat("Shelf Wood", new Color(0.75f, 0.55f, 0.35f), 0.3f);
        Color[] jarCols = { new Color(1f, 0.5f, 0.4f), new Color(0.5f, 0.8f, 0.5f), new Color(1f, 0.85f, 0.3f), new Color(0.6f, 0.5f, 0.9f), new Color(0.95f, 0.95f, 0.9f) };
        Material steel = gm.Mat("Sharpener Steel", new Color(0.8f, 0.82f, 0.86f), 0.9f, 0.8f);
        for (float x = minX - 10f; x < maxX + 10f; x += R(9f, 13f))
        {
            // رف ومرطبانات
            float sy = R(4.2f, 5.2f);
            LevelBuilder.Visual("Shelf", PrimitiveType.Cube, p, new Vector3(x, sy, 7.5f), new Vector3(4f, 0.18f, 1.2f), wood, false);
            for (int k = 0; k < 4; k++)
            {
                Material jar = gm.Mat("Jar " + rng.Next(5), jarCols[rng.Next(jarCols.Length)], 0.7f);
                float h = R(0.5f, 0.9f);
                var j = LevelBuilder.Visual("Jar", PrimitiveType.Cylinder, p, new Vector3(x - 1.4f + k * 0.95f, sy + 0.09f + h * 0.5f, 7.4f), new Vector3(0.6f, h * 0.5f, 0.6f), jar, false).transform;
                LevelBuilder.Visual("Lid", PrimitiveType.Cylinder, p, j.position + Vector3.up * (h * 0.5f + 0.05f), new Vector3(0.65f, 0.05f, 0.65f), gm.Mat("Tower Red", new Color(0.85f, 0.2f, 0.22f), 0.35f), false);
            }
            // أدوات معلقة على عمود
            float rx = x + R(4f, 6f);
            LevelBuilder.Visual("Rail", PrimitiveType.Cylinder, p, new Vector3(rx, 7f, 7.6f), new Vector3(0.08f, 1.6f, 0.08f), steel, false).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            for (int k = 0; k < 3; k++)
            {
                var tool = new GameObject("Utensil").transform;
                tool.SetParent(p, false);
                tool.position = new Vector3(rx - 1f + k, 7f, 7.5f);
                LevelBuilder.Visual("Handle", PrimitiveType.Cube, tool, new Vector3(0f, -0.6f, 0f), new Vector3(0.08f, 1.2f, 0.05f), k == 1 ? wood : steel, false);
                if (k == 0) LevelBuilder.Visual("Ladle", PrimitiveType.Sphere, tool, new Vector3(0f, -1.3f, 0f), new Vector3(0.4f, 0.25f, 0.4f), steel, false);
                else if (k == 1) LevelBuilder.Visual("Spatula", PrimitiveType.Cube, tool, new Vector3(0f, -1.35f, 0f), new Vector3(0.35f, 0.45f, 0.04f), gm.Mat("Spatula", new Color(1f, 0.5f, 0.4f), 0.4f), false);
                else for (int w = 0; w < 4; w++) LevelBuilder.Visual("Whisk", PrimitiveType.Capsule, tool, new Vector3(0f, -1.35f, 0f), new Vector3(0.3f, 0.35f, 0.03f), steel, false).transform.localRotation = Quaternion.Euler(0f, w * 45f, 0f);
                sway.Add(tool);
            }
            // شباك فيه سما وستارة
            if (rng.Next(2) == 0)
            {
                float wx = x + R(-3f, -1f);
                Material sky = gm.Mat("Window Sky", new Color(0.7f, 0.9f, 1.2f), 0.2f);
                Material frame = gm.Mat("Window Frame", Color.white, 0.4f);
                Material curtain = gm.Mat("Curtain", new Color(1f, 0.6f, 0.6f), 0.3f);
                LevelBuilder.Visual("Window", PrimitiveType.Cube, p, new Vector3(wx, 2.4f, 8.8f), new Vector3(2.6f, 2.2f, 0.1f), sky, false);
                LevelBuilder.Visual("Frame H", PrimitiveType.Cube, p, new Vector3(wx, 2.4f, 8.7f), new Vector3(2.7f, 0.1f, 0.12f), frame, false);
                LevelBuilder.Visual("Frame V", PrimitiveType.Cube, p, new Vector3(wx, 2.4f, 8.7f), new Vector3(0.1f, 2.3f, 0.12f), frame, false);
                for (int sd = -1; sd <= 1; sd += 2)
                {
                    var c = LevelBuilder.Visual("Curtain", PrimitiveType.Capsule, p, new Vector3(wx + sd * 1.45f, 2.6f, 8.5f), new Vector3(0.6f, 1.3f, 0.2f), curtain, false).transform;
                    sway.Add(c);
                }
                // زرع على الشباك
                LevelBuilder.Visual("Pot", PrimitiveType.Cylinder, p, new Vector3(wx + 0.6f, 1.45f, 8.4f), new Vector3(0.4f, 0.2f, 0.4f), gm.Mat("Clay Pot", new Color(0.85f, 0.45f, 0.3f), 0.3f), false);
                var herb = LevelBuilder.Visual("Herb", PrimitiveType.Sphere, p, new Vector3(wx + 0.6f, 1.85f, 8.4f), new Vector3(0.6f, 0.5f, 0.5f), gm.Mat("Leaf Green", new Color(0.35f, 0.72f, 0.3f), 0.35f), false).transform;
                sway.Add(herb);
            }
        }
        // ساعة مطبخ
        var clock = new GameObject("Kitchen Clock").transform;
        clock.SetParent(p, false);
        clock.position = new Vector3((minX + maxX) * 0.5f, 6.4f, 8.7f);
        LevelBuilder.Visual("Face", PrimitiveType.Cylinder, clock, Vector3.zero, new Vector3(1.4f, 0.05f, 1.4f), gm.Mat("Clock Face", new Color(0.98f, 0.96f, 0.9f), 0.3f), false).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var hand = new GameObject("Hand").transform;
        hand.SetParent(clock, false);
        hand.localPosition = new Vector3(0f, 0f, -0.08f);
        LevelBuilder.Visual("Needle", PrimitiveType.Cube, hand, new Vector3(0f, 0.3f, 0f), new Vector3(0.06f, 0.6f, 0.02f), gm.Mat("Note Black", new Color(0.12f, 0.1f, 0.15f), 0.6f), false);
        spin.Add(hand);
    }

    void BuildCandy(Transform p, float minX, float maxX, LevelData data, GameManager gm, System.Random rng, System.Func<float, float, float> R)
    {
        Color[] cols = { new Color(1f, 0.55f, 0.7f), new Color(0.55f, 0.8f, 1f), new Color(1f, 0.85f, 0.4f), new Color(0.6f, 0.9f, 0.6f), new Color(0.75f, 0.6f, 1f) };
        // تلال جيلي (بعيد)
        for (float x = minX - 15f; x < maxX + 15f; x += R(5f, 9f))
        {
            Material m = gm.Mat("Gumdrop " + rng.Next(5), cols[rng.Next(cols.Length)] * 0.95f, 0.75f);
            float sz = R(4f, 8f);
            LevelBuilder.Visual("Gumdrop Hill", PrimitiveType.Sphere, p, new Vector3(x, -3f, R(14f, 20f)), new Vector3(sz, sz * 1.2f, sz), m, false);
        }
        // شجر مصاص وعصي حلاوة
        Material stick = gm.Mat("Lolly Stick", new Color(1f, 0.98f, 0.95f), 0.4f);
        for (float x = minX - 8f; x < maxX + 8f; x += R(4f, 7f))
        {
            var lol = new GameObject("Lollipop").transform;
            lol.SetParent(p, false);
            float h = R(3f, 6f);
            lol.position = new Vector3(x, -2f, R(6f, 10f));
            LevelBuilder.Visual("Stick", PrimitiveType.Cylinder, lol, new Vector3(0f, h * 0.5f, 0f), new Vector3(0.18f, h * 0.5f, 0.18f), stick, false);
            var swirl = new GameObject("Swirl").transform;
            swirl.SetParent(lol, false);
            swirl.localPosition = new Vector3(0f, h + 0.9f, 0f);
            Material a = gm.Mat("Lolly " + rng.Next(5), cols[rng.Next(cols.Length)], 0.8f);
            Material b2 = gm.Mat("Cane White", new Color(1f, 0.98f, 0.97f), 0.7f);
            for (int k = 0; k < 4; k++)
                LevelBuilder.Visual("Ring", PrimitiveType.Cylinder, swirl, new Vector3(0f, 0f, -k * 0.01f), new Vector3(1.9f - k * 0.45f, 0.06f, 1.9f - k * 0.45f), k % 2 == 0 ? a : b2, false).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            spin.Add(swirl);
        }
        // غيوم غزل بنات
        Material cotton = gm.Mat("Cotton Candy", new Color(1f, 0.8f, 0.9f), 0.2f);
        Material cotton2 = gm.Mat("Cotton Candy Blue", new Color(0.8f, 0.9f, 1f), 0.2f);
        for (int i = 0; i < 6; i++)
        {
            var c = new GameObject("Cotton Cloud").transform;
            c.SetParent(p, false);
            Vector3 home = new Vector3(R(minX, maxX), R(6f, 8.5f), R(8f, 12f));
            c.position = home;
            for (int k = 0; k < 4; k++)
                LevelBuilder.Visual("Puff", PrimitiveType.Sphere, c, new Vector3((k - 1.5f) * 0.9f, Mathf.Sin(k * 2f) * 0.3f, 0f), Vector3.one * R(1.2f, 1.8f), i % 2 == 0 ? cotton : cotton2, false);
            floaters.Add(c);
            floatHome.Add(home);
        }
        // حلاوة صغيرة ودببة جيلي على أطراف الصفحات (ورا)
        foreach (Block b in data.blocks)
        {
            float top = b.center.y + b.size.y * 0.5f, x0 = b.center.x - b.size.x * 0.5f, x1 = b.center.x + b.size.x * 0.5f;
            for (float x = x0 + 0.8f; x < x1 - 0.6f; x += R(1.6f, 3f))
            {
                Material m = gm.Mat("Sweet " + rng.Next(5), cols[rng.Next(cols.Length)], 0.8f);
                int kind = rng.Next(3);
                Vector3 pos = new Vector3(x, top, R(1.2f, 1.8f));
                if (kind == 0)
                {
                    LevelBuilder.Visual("Gumdrop", PrimitiveType.Sphere, p, pos + Vector3.up * 0.15f, new Vector3(0.4f, 0.4f, 0.4f), m, false);
                }
                else if (kind == 1)
                {
                    var cane = new GameObject("Mini Cane").transform;
                    cane.SetParent(p, false);
                    cane.position = pos;
                    Material red = gm.Mat("Cane Red", new Color(0.95f, 0.2f, 0.28f), 0.7f);
                    Material white = gm.Mat("Cane White", new Color(1f, 0.98f, 0.97f), 0.7f);
                    for (int k = 0; k < 5; k++) LevelBuilder.Visual("Seg", PrimitiveType.Cylinder, cane, new Vector3(0f, 0.1f + k * 0.18f, 0f), new Vector3(0.12f, 0.09f, 0.12f), k % 2 == 0 ? red : white, false);
                    LevelBuilder.Visual("Hook", PrimitiveType.Sphere, cane, new Vector3(0.1f, 1f, 0f), Vector3.one * 0.14f, red, false);
                }
                else
                {
                    LevelBuilder.Visual("Wrapped", PrimitiveType.Sphere, p, pos + Vector3.up * 0.14f, new Vector3(0.36f, 0.26f, 0.26f), m, false);
                }
            }
        }
    }

    void Update()
    {
        float t = Time.time;
        for (int i = 0; i < sway.Count; i++) sway[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.1f + i) * 4f);
        for (int i = 0; i < spin.Count; i++) spin[i].Rotate(0f, 0f, -20f * Time.deltaTime, Space.Self);
        for (int i = 0; i < floaters.Count; i++) floaters[i].position = floatHome[i] + new Vector3(Mathf.Sin(t * 0.2f + i) * 3f, Mathf.Sin(t * 0.5f + i) * 0.3f, 0f);
    }
}
