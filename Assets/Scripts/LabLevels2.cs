using System.Collections.Generic;
using UnityEngine;

// الجزء الثالث — المختبر (المراحل 50 لين 52): التجميد والذوبان (مسدس تجميد، جدار ثلج يذوب بخط من النار، سقف يطيح ثلج)،
// التكبير والتصغير (زر يشغل شعاع التصغير، بيت فار، شعاع يكبر رسمتك، مصعد بحبل وسطل)، والبركان المجنون (رغوة طالعة، روبوت يقلد رسمتك، دوارق تنفجر)
public static class LabFeatures2
{
    public static void Build(Transform root, LevelData data, GameManager gm)
    {
        foreach (Vector4 s in data.freezePools) FreezePool.Create(root, s, gm);
        foreach (Vector4 s in data.iceWalls) IceWall.Create(root, s, gm);
        foreach (Vector3 s in data.icicles) IcicleCeiling.Create(root, s, gm);
        SizeRay.Reset();
        foreach (Vector4 s in data.sizeRays) SizeRay.Create(root, s, gm);
        foreach (Vector3 s in data.mouseHoles) MouseHole(root, s, gm);
        foreach (Vector4 s in data.growBeams) GrowBeam.Create(root, s, gm);
        foreach (Vector4 s in data.pulleyLifts) PulleyLift.Create(root, s, gm);
        if (data.volcano.w > 0f) VolcanoFoam.Create(root, data.volcano, gm);
        foreach (Vector4 s in data.robotHelpers) RobotHelper.Create(root, s, gm);
        foreach (Vector3 s in data.blastFlasks) BlastFlask.Create(root, s, gm);
    }

    // مادة شفافة بلون
    public static Material Tint(string name, Color c)
    {
        var tex = new Texture2D(2, 2);
        tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
        tex.Apply();
        return NightSky.Transparent(name, tex, c);
    }

    // قطعة اسطوانة بين نقطتين (حبال وأسلاك)
    public static void Stretch(Transform t, Vector3 a, Vector3 b, float thick)
    {
        t.position = (a + b) * 0.5f;
        t.up = (b - a).normalized;
        t.localScale = new Vector3(thick, (b - a).magnitude * 0.5f, thick);
    }

    // بيت فار تحت الجدار المعلق: قوس خشب، فار صغير يطل، وقطعة جبن
    static void MouseHole(Transform root, Vector3 s, GameManager gm)
    {
        Material wood = gm.Mat("Hole Wood", new Color(0.6f, 0.4f, 0.25f), 0.3f);
        Material dark = gm.Mat("Hole Dark", new Color(0.12f, 0.08f, 0.08f), 0.2f);
        Material grey = gm.Mat("Mouse Grey", new Color(0.62f, 0.6f, 0.62f), 0.3f);
        Material pink = gm.Mat("Mouse Pink", new Color(1f, 0.65f, 0.7f), 0.3f);
        Material cheese = gm.Mat("Cheese", new Color(1f, 0.8f, 0.25f), 0.3f);
        float x0 = s.x, x1 = s.y, h = s.z;
        for (int k = 0; k < 2; k++)
        {
            float px = k == 0 ? x0 : x1;
            LevelBuilder.Visual("Hole Post", PrimitiveType.Cube, root, new Vector3(px, h * 0.5f, -1.95f), new Vector3(0.18f, h, 0.1f), wood);
        }
        LevelBuilder.Visual("Hole Beam", PrimitiveType.Cube, root, new Vector3((x0 + x1) * 0.5f, h - 0.08f, -1.95f), new Vector3(x1 - x0 + 0.2f, 0.16f, 0.12f), wood);
        LevelBuilder.Visual("Hole Back", PrimitiveType.Cube, root, new Vector3((x0 + x1) * 0.5f, h * 0.5f, 2f), new Vector3(x1 - x0, h, 0.1f), dark, false);
        // فار يطل من فوق البوابة
        Vector3 m = new Vector3(x0 - 0.5f, 0.25f, 1.2f);
        LevelBuilder.Visual("Mouse", PrimitiveType.Sphere, root, m, new Vector3(0.6f, 0.4f, 0.4f), grey);
        LevelBuilder.Visual("Mouse Head", PrimitiveType.Sphere, root, m + new Vector3(0.35f, 0.08f, 0f), Vector3.one * 0.3f, grey);
        for (int e = -1; e <= 1; e += 2) LevelBuilder.Visual("Mouse Ear", PrimitiveType.Sphere, root, m + new Vector3(0.3f, 0.28f, e * 0.12f), new Vector3(0.16f, 0.16f, 0.05f), pink, false);
        LevelBuilder.Visual("Mouse Nose", PrimitiveType.Sphere, root, m + new Vector3(0.52f, 0.06f, 0f), Vector3.one * 0.07f, pink, false);
        LevelBuilder.Visual("Cheese", PrimitiveType.Cube, root, new Vector3(x1 + 0.6f, 0.15f, 1.3f), new Vector3(0.5f, 0.3f, 0.4f), cheese).transform.localRotation = Quaternion.Euler(0f, 30f, 0f);
    }
}

// ---------- مسدس التجميد: خط منه للمويه يجمدها جسر ثلج، والدفاية تذوبه بعد شوي ----------
public class FreezePool : MonoBehaviour
{
    const float Last = 7f;   // كم ثانية يعيش الثلج
    float from, to, top, gunX;
    Vector3 nozzle;
    InkMark line;
    Transform ice, surface;
    BoxCollider iceCol;
    float iceT = -1f, grow, melt;
    bool holding, prompted, frozeOnce;
    Material lineIce;
    Renderer heater;

    public static void Create(Transform root, Vector4 s, GameManager gm)
    {
        var go = new GameObject("Freeze Pool");
        go.transform.SetParent(root, false);
        var f = go.AddComponent<FreezePool>();
        f.from = s.x; f.to = s.y; f.top = s.z; f.gunX = s.w;
        f.nozzle = new Vector3(f.gunX + 1.1f, f.top + 1.9f, 0f);
        Material water = LabFeatures2.Tint("Pool Water", new Color(0.3f, 0.6f, 1f, 0.65f));
        Material steel = gm.Mat("Spoon Steel", new Color(0.82f, 0.84f, 0.88f), 0.95f, 0.85f);
        Material coldGlow = gm.Mat("Cold Glow", new Color(0.5f, 1.4f, 2.2f), 0.6f);
        Material white = gm.Mat("Badge White", Color.white, 0.4f);
        f.lineIce = gm.Mat("Ice Line", new Color(0.6f, 1.2f, 1.8f), 0.8f);
        float w = f.to - f.from, mid = (f.from + f.to) * 0.5f;
        // المويه
        LevelBuilder.Visual("Water", PrimitiveType.Cube, go.transform, new Vector3(mid, f.top - 2.3f, 0.2f), new Vector3(w, 3.8f, 3.6f), water, false);
        f.surface = LevelBuilder.Visual("Water Top", PrimitiveType.Cube, go.transform, new Vector3(mid, f.top - 0.42f, 0.2f), new Vector3(w, 0.04f, 3.6f), LabFeatures2.Tint("Water Shine", new Color(0.75f, 0.9f, 1f, 0.7f)), false).transform;
        // الثلج (يمتد من اليسار لليمين)
        f.ice = new GameObject("Ice").transform;
        f.ice.SetParent(go.transform, false);
        f.ice.position = new Vector3(f.from, f.top - 0.25f, 0f);
        var slab = LevelBuilder.Visual("Ice Slab", PrimitiveType.Cube, f.ice, new Vector3(0.5f, 0f, 0f), new Vector3(1f, 0.5f, 3.4f), LabFeatures2.Tint("Ice", new Color(0.8f, 0.95f, 1f, 0.85f)));
        f.iceCol = slab.AddComponent<BoxCollider>();
        f.iceCol.enabled = false;
        for (int k = 0; k < 6; k++) LevelBuilder.Visual("Ice Crack", PrimitiveType.Cube, f.ice, new Vector3(0.1f + k * 0.16f, 0.26f, -1.2f + (k % 3) * 0.9f), new Vector3(0.12f, 0.01f, 0.5f), coldGlow, false).transform.localRotation = Quaternion.Euler(0f, k * 47f, 0f);
        f.ice.localScale = new Vector3(0.001f, 1f, 1f);
        // مسدس التجميد على ستاند
        Vector3 g = new Vector3(f.gunX, f.top, 0.4f);
        LevelBuilder.Visual("Gun Base", PrimitiveType.Cylinder, go.transform, g + new Vector3(0f, 0.1f, 0f), new Vector3(0.9f, 0.1f, 0.9f), steel);
        LevelBuilder.Visual("Gun Pole", PrimitiveType.Cylinder, go.transform, g + new Vector3(0f, 1f, 0f), new Vector3(0.15f, 0.9f, 0.15f), steel);
        var body = LevelBuilder.Visual("Gun Body", PrimitiveType.Cylinder, go.transform, g + new Vector3(0.45f, 2f, -0.4f), new Vector3(0.55f, 0.6f, 0.55f), white).transform;
        body.localRotation = Quaternion.Euler(0f, 0f, 75f);
        for (int k = 0; k < 3; k++) LevelBuilder.Visual("Gun Coil", PrimitiveType.Cylinder, body, new Vector3(0f, -0.5f + k * 0.45f, 0f), new Vector3(1.12f, 0.08f, 1.12f), coldGlow, false);
        LevelBuilder.Visual("Gun Tank", PrimitiveType.Sphere, go.transform, g + new Vector3(-0.1f, 2.55f, -0.3f), Vector3.one * 0.55f, LabFeatures2.Tint("Gun Tank", new Color(0.5f, 0.85f, 1f, 0.7f)), false);
        // نقاط ثلج حول البوز: من هنا ابدئي الرسم
        for (int k = 0; k < 8; k++)
        {
            float a = k / 8f * Mathf.PI * 2f;
            LevelBuilder.Visual("Start Dot", PrimitiveType.Sphere, go.transform, f.nozzle + new Vector3(Mathf.Cos(a) * 0.4f, Mathf.Sin(a) * 0.4f, -0.3f), Vector3.one * 0.09f, coldGlow, false);
        }
        // الدفاية الحارة جنب المويه
        Vector3 hp = new Vector3(f.to + 0.2f, f.top - 0.5f, 1.9f);
        for (int k = 0; k < 5; k++) LevelBuilder.Visual("Heater Fin", PrimitiveType.Cube, go.transform, hp + new Vector3(-0.8f + k * 0.4f, 1f, 0f), new Vector3(0.22f, 1.6f, 0.3f), gm.Mat("Heater", new Color(0.85f, 0.85f, 0.88f), 0.6f, 0.3f));
        f.heater = LevelBuilder.Visual("Heater Glow", PrimitiveType.Cube, go.transform, hp + new Vector3(0f, 0.35f, -0.2f), new Vector3(2f, 0.14f, 0.06f), gm.Mat("Heater Hot", new Color(2.2f, 0.6f, 0.2f), 0.5f), false).GetComponent<Renderer>();
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time, dt = Time.deltaTime;
        surface.localScale = new Vector3(to - from, 0.04f, 3.6f);
        surface.position = new Vector3((from + to) * 0.5f, top - 0.42f + Mathf.Sin(t * 2f) * 0.03f, 0.2f);
        if (Random.value < 0.05f) Fx.Sparkle(nozzle + new Vector3(0f, 0f, -0.3f), new Color(0.7f, 0.9f, 1f), 2);

        // خط من المسدس للمويه؟ تتجمد
        if (iceT < 0f && melt <= 0f && line == null)
        {
            var p = LabFeatures.LineFrom(nozzle, 1f, out InkMark m);
            if (p != null)
            {
                Vector3 end = p[p.Length - 1];
                if (end.x > from - 0.6f && end.x < to + 0.6f && end.y < top + 3f)
                {
                    line = m;
                    iceT = t; grow = 0f;
                    var lr = m.GetComponent<MeshRenderer>();
                    if (lr != null) lr.sharedMaterial = lineIce;
                    // الخط صار شعاع تجميد: رويدا تعدي من تحته عادي
                    foreach (Collider c in m.GetComponentsInChildren<Collider>()) c.enabled = false;
                    LevelSfx.Play("cold", 0.8f);
                    LevelSfx.Play("crack", 0.5f, 1.3f);
                    if (!frozeOnce) { frozeOnce = true; gm.UI.ShowMessage("", 2.4f, "تجمدت! عدي بسرعة قبل ما تذوبها الدفاية"); }
                }
            }
        }
        if (iceT >= 0f)
        {
            grow = Mathf.Min(1f, grow + dt / 0.7f);
            if (Random.value < 0.3f && grow < 1f) SmokeCloud.Puff(new Vector3(from + (to - from) * grow, top, 0f), 0.6f);
            float age = t - iceT;
            if (age > Last - 2f && Random.value < 0.2f) Fx.Sparkle(new Vector3(Random.Range(from, to), top, -0.5f), new Color(0.6f, 0.9f, 1f), 2);
            if (age > Last)
            {
                // ذاب الثلج: والخط ينكسر
                iceT = -1f; melt = 1f;
                LevelSfx.Play("crack", 0.7f, 0.8f);
                LevelSfx.Play("plop", 0.6f);
                if (line != null) { Fx.Sparkle(line.GetComponent<MeshRenderer>().bounds.center, new Color(0.7f, 0.9f, 1f), 14); Destroy(line.gameObject); }
                line = null;
            }
        }
        if (melt > 0f) melt = Mathf.Max(0f, melt - dt / 0.7f);
        float shake = iceT >= 0f && t - iceT > Last - 1.5f ? Mathf.Sin(t * 50f) * 0.03f : 0f;
        float thick = iceT >= 0f ? 1f : melt;
        ice.localScale = new Vector3(Mathf.Max(0.001f, (to - from) * (iceT >= 0f ? grow : melt)), thick, 1f);
        ice.position = new Vector3(from + shake, top - 0.25f, 0f);
        iceCol.enabled = iceT >= 0f && grow >= 0.98f;
        heater.sharedMaterial.color = new Color(2.2f, 0.6f, 0.2f) * (0.7f + Mathf.Sin(t * 4f) * 0.3f);

        // رويدا تستنى لين يتجمد، ولين يكون باقي وقت يكفيها تعدي
        float cross = (to - from + 1.6f) / 2.8f;
        bool safe = iceT >= 0f && grow >= 0.98f && Last - (t - iceT) > cross + 0.6f;
        bool wait = HoldHelper.Before(from) && !safe;
        if (wait && !prompted) { prompted = true; gm.UI.ShowMessage("", 3.6f, "مويه! ارسمي خط من مسدس التجميد للمويه... تتجمد وتصير جسر"); }
        HoldHelper.Hold(ref holding, wait);
        if (!safe || t - iceT < 1f) LabFeatures.Show(gunX - 5f, to, new Vector3(gunX - 1f, top - 1f, 0f), new Vector3(to + 1.5f, top + 3.2f, 0f));
    }
}

// ---------- جدار ثلج: خط من نار الموقد للجدار يحمى ويذوبه ----------
public class IceWall : MonoBehaviour
{
    float x, top, height, burnerX;
    Vector3 flame;
    Transform block, flameOuter, flameInner;
    BoxCollider col;
    InkMark line;
    Vector3[] path;
    float[] cum;
    float heatT = -1f, meltT = -1f;
    bool holding, prompted;
    Material hot;

    public static void Create(Transform root, Vector4 s, GameManager gm)
    {
        var go = new GameObject("Ice Wall");
        go.transform.SetParent(root, false);
        var w = go.AddComponent<IceWall>();
        w.x = s.x; w.top = s.y; w.height = s.z; w.burnerX = s.w;
        w.flame = new Vector3(w.burnerX, w.top + 1.55f, 0f);
        w.hot = new Material(gm.Mat("Hot Line", new Color(2.4f, 0.7f, 0.2f), 0.5f));
        Material iceM = LabFeatures2.Tint("Ice Block", new Color(0.75f, 0.92f, 1f, 0.8f));
        Material coldGlow = gm.Mat("Cold Glow", new Color(0.5f, 1.4f, 2.2f), 0.6f);
        Material steel = gm.Mat("Spoon Steel", new Color(0.82f, 0.84f, 0.88f), 0.95f, 0.85f);
        // الجدار
        w.block = new GameObject("Ice Block").transform;
        w.block.SetParent(go.transform, false);
        w.block.position = new Vector3(w.x, w.top, 0f);
        var cube = LevelBuilder.Visual("Ice", PrimitiveType.Cube, w.block, new Vector3(0f, 0.5f, 0f), new Vector3(1.4f, 1f, 2.8f), iceM);
        w.col = cube.AddComponent<BoxCollider>();
        w.block.localScale = new Vector3(1f, w.height, 1f);
        // أشياء مجمدة داخله: دورق صغير وفقاعات
        LevelBuilder.Visual("Frozen Flask", PrimitiveType.Sphere, w.block, new Vector3(0.1f, 0.35f, 0.2f), new Vector3(0.5f, 0.15f, 0.5f), gm.Mat("Potion Pink", new Color(1.3f, 0.45f, 0.9f), 0.7f), false);
        for (int k = 0; k < 6; k++) LevelBuilder.Visual("Frozen Bubble", PrimitiveType.Sphere, w.block, new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(0.1f, 0.9f), Random.Range(-0.8f, 0.8f)), new Vector3(0.12f, 0.12f / w.height, 0.12f), coldGlow, false);
        // شوك ثلج فوق
        for (int k = 0; k < 4; k++) LevelBuilder.Visual("Ice Spike", PrimitiveType.Sphere, go.transform, new Vector3(w.x - 0.5f + k * 0.33f, w.top + w.height + 0.15f, -0.4f + (k % 2) * 0.6f), new Vector3(0.22f, 0.5f, 0.22f), iceM, false);
        // موقد بنسن
        Vector3 b = new Vector3(w.burnerX, w.top, 0.6f);
        LevelBuilder.Visual("Burner Base", PrimitiveType.Cylinder, go.transform, b + new Vector3(0f, 0.08f, 0f), new Vector3(0.7f, 0.08f, 0.7f), steel);
        LevelBuilder.Visual("Burner Tube", PrimitiveType.Cylinder, go.transform, b + new Vector3(0f, 0.6f, 0f), new Vector3(0.22f, 0.5f, 0.22f), steel);
        LevelBuilder.Visual("Burner Hose", PrimitiveType.Cylinder, go.transform, b + new Vector3(-0.5f, 0.15f, 0.3f), new Vector3(0.1f, 0.4f, 0.1f), gm.Mat("Hose Orange", new Color(1f, 0.5f, 0.2f), 0.4f)).transform.localRotation = Quaternion.Euler(0f, 0f, 80f);
        w.flameOuter = LevelBuilder.Visual("Flame", PrimitiveType.Sphere, go.transform, b + new Vector3(0f, 1.35f, 0f), new Vector3(0.35f, 0.7f, 0.35f), gm.Mat("Flame Orange", new Color(2.4f, 0.9f, 0.2f), 0.5f), false).transform;
        w.flameInner = LevelBuilder.Visual("Flame Core", PrimitiveType.Sphere, go.transform, b + new Vector3(0f, 1.25f, -0.05f), new Vector3(0.2f, 0.45f, 0.2f), gm.Mat("Flame Blue", new Color(0.4f, 0.8f, 2.4f), 0.5f), false).transform;
        for (int k = 0; k < 8; k++)
        {
            float a = k / 8f * Mathf.PI * 2f;
            LevelBuilder.Visual("Start Dot", PrimitiveType.Sphere, go.transform, w.flame + new Vector3(Mathf.Cos(a) * 0.45f, Mathf.Sin(a) * 0.45f, -0.3f), Vector3.one * 0.09f, gm.Mat("Flame Orange", new Color(2.4f, 0.9f, 0.2f), 0.5f), false);
        }
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time, dt = Time.deltaTime;
        flameOuter.localScale = new Vector3(0.35f, 0.7f + Mathf.Sin(t * 17f) * 0.08f, 0.35f);
        flameInner.localScale = new Vector3(0.2f, 0.45f + Mathf.Sin(t * 23f) * 0.05f, 0.2f);

        if (line == null && heatT < 0f && meltT < 0f)
        {
            var p = LabFeatures.LineFrom(flame, 1f, out InkMark m);
            if (p != null)
            {
                Vector3 end = p[p.Length - 1];
                if (end.x > x - 2.2f && end.x < x + 0.8f && end.y > top - 0.3f && end.y < top + height + 0.6f)
                {
                    line = m; path = p; cum = LabFeatures.Cumulative(p);
                    heatT = 0f;
                    foreach (Collider c in m.GetComponentsInChildren<Collider>()) c.enabled = false;
                    LevelSfx.Play("sizzle", 0.7f);
                }
            }
        }
        if (heatT >= 0f && meltT < 0f)
        {
            // الحرارة تمشي على الخط من النار للجدار
            heatT += dt;
            float k = Mathf.Clamp01(heatT / 1.2f);
            if (line != null)
            {
                var lr = line.GetComponent<MeshRenderer>();
                hot.color = Color.Lerp(new Color(0.3f, 0.1f, 0.05f), new Color(2.4f, 0.7f, 0.2f), k);
                if (lr != null) lr.sharedMaterial = hot;
                Vector3 at = LabFeatures.PointAt(path, cum, cum[cum.Length - 1] * k);
                if (Random.value < 0.6f) Fx.Sparkle(at + new Vector3(0f, 0f, -0.2f), new Color(1f, 0.6f, 0.2f), 2);
            }
            if (heatT > 1.2f)
            {
                meltT = 0f;
                LevelSfx.Play("sizzle", 0.9f, 0.8f);
                LevelSfx.Play("steam", 0.6f);
                if (line != null) { SmokeCloud.Puff(line.GetComponent<MeshRenderer>().bounds.center, 0.8f); Destroy(line.gameObject); }
                line = null;
                gm.UI.ShowMessage("", 2f, "الثلج قاعد يذوب!");
            }
        }
        if (meltT >= 0f && meltT < 1f)
        {
            meltT = Mathf.Min(1f, meltT + dt / 1.8f);
            block.localScale = new Vector3(1f + meltT * 0.3f, Mathf.Max(0.02f, height * (1f - meltT)), 1f);
            if (Random.value < 0.4f) SmokeCloud.Puff(new Vector3(x + Random.Range(-0.6f, 0.6f), top + height * (1f - meltT), 0f), 0.7f);
            if (Random.value < 0.5f)
            {
                var d = LevelBuilder.Visual("Drip", PrimitiveType.Sphere, null, new Vector3(x + Random.Range(-0.7f, 0.7f), top + height * (1f - meltT), -0.6f), Vector3.one * 0.1f, gm.Mat("Cold Glow", new Color(0.5f, 1.4f, 2.2f), 0.6f), false);
                var fx = d.AddComponent<FxParticle>();
                fx.velocity = Vector3.down; fx.gravity = 9f; fx.life = 0.5f;
            }
            if (meltT >= 1f) { col.enabled = false; block.gameObject.SetActive(false); LevelSfx.Play("chime", 0.6f, 1.3f); }
        }
        bool wait = meltT < 1f && HoldHelper.Before(x - 0.7f);
        if (wait && !prompted) { prompted = true; gm.UI.ShowMessage("", 3.6f, "جدار ثلج! ارسمي خط من نار الموقد للجدار... الخط يحمى ويذوب الثلج"); }
        HoldHelper.Hold(ref holding, wait);
        if (meltT < 1f) LabFeatures.Show(burnerX - 4f, x, new Vector3(burnerX - 1.5f, top - 0.5f, 0f), new Vector3(x + 1.2f, top + height + 0.8f, 0f));
    }
}

// ---------- سقف ثلج: يطيح شوك ثلج على رويدا، والخط فوق راسها مظلة تحميها ----------
public class IcicleCeiling : MonoBehaviour
{
    class Icicle { public Transform t; public Vector3 home; public int state; public float timer, vy; }
    float from, to, ceil, next;
    bool prompted, warned;
    readonly List<Icicle> list = new List<Icicle>();

    public static void Create(Transform root, Vector3 s, GameManager gm)
    {
        var go = new GameObject("Icicle Ceiling");
        go.transform.SetParent(root, false);
        var c = go.AddComponent<IcicleCeiling>();
        c.from = s.x; c.to = s.y; c.ceil = s.z;
        Material iceM = gm.Mat("Icicle", new Color(0.55f, 0.85f, 1.15f), 0.9f);
        Material snow = gm.Mat("Snow", new Color(0.97f, 0.98f, 1f), 0.2f);
        Material pipe = gm.Mat("Spoon Steel", new Color(0.82f, 0.84f, 0.88f), 0.95f, 0.85f);
        float w = c.to - c.from + 1f, mid = (c.from + c.to) * 0.5f;
        // أنبوب تبريد كبير متجمد
        LevelBuilder.Visual("Cold Pipe", PrimitiveType.Cylinder, go.transform, new Vector3(mid, c.ceil + 0.5f, 0f), new Vector3(0.9f, w * 0.5f, 0.9f), pipe).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        LevelBuilder.Visual("Pipe Frost", PrimitiveType.Cube, go.transform, new Vector3(mid, c.ceil + 0.05f, 0f), new Vector3(w, 0.3f, 1.2f), snow, false);
        for (float px = c.from; px <= c.to + 0.01f; px += 3f)
            LevelBuilder.Visual("Pipe Clamp", PrimitiveType.Cylinder, go.transform, new Vector3(px, c.ceil + 0.5f, 0f), new Vector3(1.05f, 0.08f, 1.05f), gm.Mat("Stove Black", new Color(0.15f, 0.14f, 0.16f), 0.6f, 0.3f)).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        for (float px = c.from + 0.3f; px < c.to; px += 0.75f)
        {
            var t = new GameObject("Icicle").transform;
            t.SetParent(go.transform, false);
            t.position = new Vector3(px, c.ceil - 0.1f, 0f);
            float len = Random.Range(0.6f, 0.95f);
            LevelBuilder.Visual("Spike", PrimitiveType.Sphere, t, new Vector3(0f, -len * 0.5f, 0f), new Vector3(0.28f, len, 0.28f), iceM, false);
            LevelBuilder.Visual("Spike Tip", PrimitiveType.Sphere, t, new Vector3(0f, -len * 0.85f, 0f), new Vector3(0.12f, len * 0.4f, 0.12f), iceM, false);
            c.list.Add(new Icicle { t = t, home = t.position });
        }
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time, dt = Time.deltaTime;
        RowaidaController r = gm.Rowaida;
        bool inZone = r != null && gm.IsPlaying && r.transform.position.x > from - 2f && r.transform.position.x < to;
        if (inZone && !prompted) { prompted = true; gm.UI.ShowMessage("", 3.6f, "سقف ثلج! ارسمي خط فوق راسها... مظلة تحميها من الثلج اللي يطيح"); }
        if (inZone) LabFeatures.Show(from - 4f, to, new Vector3(from - 1f, ceil - 4.5f, 0f), new Vector3(to + 1f, ceil + 1f, 0f));
        if (inZone && t > next)
        {
            // نختار شوكة قدامها (بمكانها لما توصل الشوكة تحت)
            float target = r.transform.position.x + (r.Held ? 0f : 2.4f);
            Icicle best = null;
            foreach (Icicle i in list)
                if (i.state == 0 && Mathf.Abs(i.home.x - target) < 0.6f && (best == null || Mathf.Abs(i.home.x - target) < Mathf.Abs(best.home.x - target))) best = i;
            if (best != null) { best.state = 1; best.timer = 0.35f; LevelSfx.Play("crack", 0.35f, 1.6f); }
            next = t + 0.75f;
        }
        foreach (Icicle i in list)
        {
            switch (i.state)
            {
                case 1:
                    i.timer -= dt;
                    i.t.position = i.home + new Vector3(Mathf.Sin(t * 60f) * 0.05f, 0f, 0f);
                    if (i.timer <= 0f) { i.state = 2; i.vy = 0f; i.t.position = i.home; }
                    break;
                case 2:
                {
                    i.vy += 20f * dt;
                    float step = i.vy * dt;
                    Vector3 tip = i.t.position + Vector3.down * 0.85f;
                    bool hit = false;
                    // رويدا؟
                    if (r != null)
                    {
                        Vector3 rp = r.transform.position;
                        float s = r.transform.localScale.y;
                        if (Mathf.Abs(tip.x - rp.x) < 0.45f && tip.y - step < rp.y + 0.9f * s && tip.y > rp.y - 0.5f * s)
                        {
                            hit = true;
                            if (!r.IsKnocked) r.Knockback(-1f);
                            LevelSfx.Play("crack", 0.8f, 1.1f);
                            if (!warned) { warned = true; gm.UI.ShowMessage("", 2.4f, "آي! ارسمي خط فوقها يحميها"); }
                        }
                    }
                    if (!hit)
                        foreach (RaycastHit h in Physics.RaycastAll(tip, Vector3.down, step + 0.05f, ~0, QueryTriggerInteraction.Ignore))
                        {
                            if (h.collider.GetComponentInParent<RowaidaController>() != null) continue;
                            hit = true;
                            LevelSfx.Play("crack", 0.55f, 1.4f);
                            break;
                        }
                    if (hit)
                    {
                        Fx.Sparkle(tip, new Color(0.7f, 0.9f, 1f), 12);
                        for (int k = 0; k < 6; k++)
                        {
                            var d = LevelBuilder.Visual("Ice Bit", PrimitiveType.Cube, null, tip, Vector3.one * 0.12f, LabFeatures2.Tint("Ice Block", new Color(0.75f, 0.92f, 1f, 0.8f)), false);
                            var fx = d.AddComponent<FxParticle>();
                            fx.velocity = new Vector3(Random.Range(-2f, 2f), Random.Range(1f, 3f), Random.Range(-1f, 1f)); fx.gravity = 12f; fx.life = 0.5f;
                        }
                        i.state = 3; i.timer = 2.5f;
                        i.t.localScale = Vector3.zero;
                    }
                    else i.t.position += Vector3.down * step;
                    break;
                }
                case 3:
                    i.timer -= dt;
                    if (i.timer <= 0f)
                    {
                        // ترجع تنبت
                        i.t.position = i.home;
                        i.t.localScale = Vector3.MoveTowards(i.t.localScale, Vector3.one, dt * 2f);
                        if (i.t.localScale.x >= 1f) i.state = 0;
                    }
                    break;
            }
        }
    }
}

// ---------- شعاع التصغير/التكبير: قوس فيه شعاع، رويدا تمر تحته ويتغير حجمها. بعضها يبي زر (دايرة تطيح على الرف) ----------
public class SizeRay : MonoBehaviour
{
    public static readonly List<SizeRay> All = new List<SizeRay>();
    static float target = 1f;
    float buttonX, rayX, top, scale;
    bool on, holding, prompted, told;
    float lastX = -999f;
    Transform beam, cap;
    Renderer lamp, beamR;
    AudioSource hum;

    public static void Reset() { All.Clear(); target = 1f; }

    public static void Create(Transform root, Vector4 s, GameManager gm)
    {
        var go = new GameObject("Size Ray");
        go.transform.SetParent(root, false);
        var r = go.AddComponent<SizeRay>();
        r.buttonX = s.x; r.rayX = s.y; r.top = s.z; r.scale = s.w;
        r.on = r.buttonX < -900f;
        All.Add(r);
        All.Sort((a, b) => a.rayX.CompareTo(b.rayX));
        bool shrink = r.scale < 1f;
        Color c = shrink ? new Color(0.4f, 1.2f, 2.2f) : new Color(1.6f, 0.5f, 2.2f);
        Material frame = gm.Mat("Lab Frame", new Color(0.55f, 0.6f, 0.68f), 0.7f, 0.5f);
        Material glow = gm.Mat("Ray Glow " + shrink, c, 0.5f);
        // القوس
        for (int k = -1; k <= 1; k += 2) LevelBuilder.Visual("Arch Post", PrimitiveType.Cube, go.transform, new Vector3(r.rayX + k * 1f, r.top + 1.7f, 1.2f), new Vector3(0.3f, 3.4f, 0.3f), frame);
        LevelBuilder.Visual("Arch Top", PrimitiveType.Cube, go.transform, new Vector3(r.rayX, r.top + 3.5f, 0.6f), new Vector3(2.4f, 0.4f, 1.8f), frame);
        LevelBuilder.Visual("Dish", PrimitiveType.Sphere, go.transform, new Vector3(r.rayX, r.top + 3.25f, 0f), new Vector3(1.1f, 0.35f, 1.1f), glow, false);
        // علامة: سهم صغير لتحت (تصغير) أو لفوق (تكبير)
        for (int k = 0; k < 3; k++)
            LevelBuilder.Visual("Sign", PrimitiveType.Cube, go.transform, new Vector3(r.rayX - 0.18f + k * 0.18f, r.top + 3.5f + (shrink ? -k : k) * 0.0f + (k == 1 ? (shrink ? -0.1f : 0.1f) : 0f), -0.35f), new Vector3(0.12f, 0.12f, 0.02f), glow, false);
        r.beam = LevelBuilder.Visual("Beam", PrimitiveType.Cylinder, go.transform, new Vector3(r.rayX, r.top + 1.6f, 0f), new Vector3(1.1f, 1.6f, 1.1f), LabFeatures2.Tint("Ray Beam " + shrink, new Color(c.r * 0.4f, c.g * 0.4f, c.b * 0.4f, 0.35f)), false).transform;
        r.beamR = r.beam.GetComponent<Renderer>();
        r.hum = LevelSfx.LoopSource(go, "laserhum", 0f);
        if (!r.on)
        {
            // الرف والزر الأحمر فوق الطريق
            Vector3 b = new Vector3(r.buttonX, r.top + 2.6f, 0f);
            var shelf = LevelBuilder.Visual("Button Shelf", PrimitiveType.Cube, go.transform, b, new Vector3(1.8f, 0.2f, 1.8f), gm.Mat("Shelf Wood", new Color(0.7f, 0.5f, 0.3f), 0.3f));
            shelf.AddComponent<BoxCollider>();
            LevelBuilder.Visual("Shelf Arm", PrimitiveType.Cube, go.transform, b + new Vector3(0f, 0.6f, 1.2f), new Vector3(0.15f, 1.4f, 0.15f), frame);
            LevelBuilder.Visual("Button Base", PrimitiveType.Cylinder, go.transform, b + new Vector3(0f, 0.15f, 0f), new Vector3(1.1f, 0.06f, 1.1f), gm.Mat("Stove Black", new Color(0.15f, 0.14f, 0.16f), 0.6f, 0.3f), false);
            r.cap = LevelBuilder.Visual("Button Cap", PrimitiveType.Cylinder, go.transform, b + new Vector3(0f, 0.27f, 0f), new Vector3(0.85f, 0.08f, 0.85f), gm.Mat("Target Red", new Color(1f, 0.25f, 0.3f), 0.4f), false).transform;
            // سلك من الزر للقوس
            Vector3 w0 = b + new Vector3(0.9f, 0f, 1f), w1 = new Vector3(r.rayX - 1f, r.top + 3.3f, 1.2f);
            var wire = LevelBuilder.Visual("Wire", PrimitiveType.Cylinder, go.transform, Vector3.zero, Vector3.one, gm.Mat("Wire Red", new Color(0.9f, 0.2f, 0.2f), 0.4f), false).transform;
            LabFeatures2.Stretch(wire, w0, w1, 0.07f);
        }
        r.lamp = LevelBuilder.Visual("Lamp", PrimitiveType.Sphere, go.transform, new Vector3(r.rayX, r.top + 3.85f, 0.3f), Vector3.one * 0.35f, gm.Mat(r.on ? "Lamp Green" : "Lamp Red", r.on ? new Color(0.3f, 1.6f, 0.4f) : new Color(1.6f, 0.2f, 0.2f), 0.5f), false).GetComponent<Renderer>();
        gm.Respawned += r.OnRespawned;
    }

    void OnDestroy()
    {
        All.Remove(this);
        if (GameManager.Instance != null) GameManager.Instance.Respawned -= OnRespawned;
        if (All.Count == 0)
        {
            target = 1f;
            var gm = GameManager.Instance;
            if (gm != null && gm.Rowaida != null) gm.Rowaida.transform.localScale = Vector3.one;
        }
    }

    // رجعت من نقطة الرجوع: حجمها حسب آخر شعاع عدته
    void OnRespawned(Vector3 pos)
    {
        float s = 1f;
        foreach (SizeRay r in All) if (r.rayX < pos.x) s = r.scale;
        target = s;
        var gm = GameManager.Instance;
        if (gm != null && gm.Rowaida != null) gm.Rowaida.transform.localScale = Vector3.one * s;
        lastX = pos.x;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time, dt = Time.deltaTime;
        RowaidaController r = gm.Rowaida;
        if (!on)
        {
            // دايرة طاحت على الزر؟
            InkMark m = LabFeatures.ClosedIn(buttonX - 0.9f, buttonX + 0.9f, top + 2.6f, top + 4.2f);
            if (m != null)
            {
                on = true;
                cap.localScale = new Vector3(0.85f, 0.03f, 0.85f);
                lamp.sharedMaterial = gm.Mat("Lamp Green", new Color(0.3f, 1.6f, 0.4f), 0.5f);
                LevelSfx.Play("snap", 0.7f);
                LevelSfx.Play("chime", 0.7f, 1.4f);
                Fx.Sparkle(new Vector3(rayX, top + 3.2f, 0f), new Color(0.6f, 0.9f, 1f), 20);
            }
        }
        beam.gameObject.SetActive(on);
        if (on)
        {
            beam.localScale = new Vector3(1.1f + Mathf.Sin(t * 8f) * 0.08f, 1.6f, 1.1f + Mathf.Cos(t * 7f) * 0.08f);
            if (Random.value < 0.4f) Fx.Sparkle(new Vector3(rayX + Random.Range(-0.5f, 0.5f), top + Random.Range(0f, 3f), -0.3f), scale < 1f ? new Color(0.6f, 0.9f, 1f) : new Color(1f, 0.6f, 1f), 1);
        }
        if (hum != null) hum.volume = on && r != null ? Mathf.Clamp01(1f - Mathf.Abs(r.transform.position.x - rayX) / 8f) * 0.2f : 0f;

        if (r != null)
        {
            float x = r.transform.position.x;
            // عدت تحت الشعاع
            if (on && lastX > -900f && lastX < rayX && x >= rayX)
            {
                target = scale;
                LevelSfx.Play(scale < 1f ? "wobble" : "grow", 0.8f, scale < 1f ? 1.5f : 0.9f);
                Fx.Sparkle(r.transform.position, scale < 1f ? new Color(0.6f, 0.9f, 1f) : new Color(1f, 0.6f, 1f), 24);
                if (!told) { told = true; gm.UI.ShowMessage("", 2.4f, scale < 1f ? "صغرت! الحين تقدر تدخل بيت الفار" : "رجعت لحجمها!"); }
            }
            lastX = x;
            // أول شعاع يتحكم بحجمها
            if (All.Count > 0 && All[0] == this)
            {
                float cur = r.transform.localScale.x;
                if (!Mathf.Approximately(cur, target)) r.transform.localScale = Vector3.one * Mathf.MoveTowards(cur, target, dt * 1.6f);
            }
        }
        bool wait = !on && HoldHelper.Before(rayX);
        if (wait && !prompted) { prompted = true; gm.UI.ShowMessage("", 3.6f, "شعاع التصغير طافي! ارسمي دايرة تطيح على الزر الأحمر اللي فوق الرف"); }
        HoldHelper.Hold(ref holding, wait);
        if (!on) LabFeatures.Show(buttonX - 4f, rayX, new Vector3(buttonX - 1.5f, top - 0.5f, 0f), new Vector3(rayX + 1.5f, top + 4.4f, 0f));
    }
}

// ---------- شعاع التكبير فوق حفرة: أي رسمة داخل الشعاع تكبر (خط صغير يصير جسر طويل) ----------
public class GrowBeam : MonoBehaviour
{
    class Growing { public InkMark m; public Vector3 c; public float t0; public bool line; }
    float x, top, height, factor, from, to;
    bool holding, prompted;
    readonly HashSet<InkMark> done = new HashSet<InkMark>();
    readonly List<Growing> growing = new List<Growing>();
    Transform beam;

    public static void Create(Transform root, Vector4 s, GameManager gm)
    {
        var go = new GameObject("Grow Beam");
        go.transform.SetParent(root, false);
        var g = go.AddComponent<GrowBeam>();
        g.x = s.x; g.top = s.y; g.height = s.z; g.factor = s.w;
        g.from = g.x - 2f; g.to = g.x + 2f;
        Material frame = gm.Mat("Lab Frame", new Color(0.55f, 0.6f, 0.68f), 0.7f, 0.5f);
        Material glow = gm.Mat("Grow Glow", new Color(0.4f, 2.2f, 0.6f), 0.5f);
        Vector3 e = new Vector3(g.x, g.top + g.height + 0.4f, 0.3f);
        LevelBuilder.Visual("Emitter Arm", PrimitiveType.Cube, go.transform, e + new Vector3(-1.3f, 0.35f, 0.6f), new Vector3(2.8f, 0.25f, 0.25f), frame);
        LevelBuilder.Visual("Emitter Pole", PrimitiveType.Cube, go.transform, new Vector3(g.x - 2.6f, (g.top + e.y) * 0.5f, 0.9f), new Vector3(0.25f, e.y - g.top + 0.8f, 0.25f), frame);
        LevelBuilder.Visual("Emitter", PrimitiveType.Cylinder, go.transform, e + new Vector3(0f, 0.2f, 0f), new Vector3(1.2f, 0.25f, 1.2f), frame);
        LevelBuilder.Visual("Emitter Lens", PrimitiveType.Sphere, go.transform, e, new Vector3(1f, 0.3f, 1f), glow, false);
        g.beam = LevelBuilder.Visual("Grow Beam", PrimitiveType.Cylinder, go.transform, new Vector3(g.x, g.top + g.height * 0.5f - 0.5f, 0f), new Vector3(2f, (g.height + 1f) * 0.5f, 2f), LabFeatures2.Tint("Grow Beam", new Color(0.3f, 1f, 0.4f, 0.25f)), false).transform;
        // أسهم تكبير على الأطراف
        for (int k = -1; k <= 1; k += 2)
            for (int a = 0; a < 2; a++)
                LevelBuilder.Visual("Arrow", PrimitiveType.Cube, go.transform, new Vector3(g.x + k * 1.25f, g.top + 2.2f + a * 0.25f, -0.4f), new Vector3(0.35f, 0.08f, 0.02f), glow, false).transform.localRotation = Quaternion.Euler(0f, 0f, k * (a == 0 ? 35f : -35f));
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time;
        beam.localScale = new Vector3(2f + Mathf.Sin(t * 5f) * 0.1f, (height + 1f) * 0.5f, 2f);
        if (Random.value < 0.5f)
        {
            var d = LevelBuilder.Visual("Grow Spark", PrimitiveType.Sphere, null, new Vector3(x + Random.Range(-0.9f, 0.9f), top - 0.8f, Random.Range(-0.8f, 0.8f)), Vector3.one * 0.08f, gm.Mat("Grow Glow", new Color(0.4f, 2.2f, 0.6f), 0.5f), false);
            var fx = d.AddComponent<FxParticle>();
            fx.velocity = Vector3.up * Random.Range(2f, 4f); fx.life = 1f; fx.grow = 1.5f;
        }
        // رسمة جديدة داخل الشعاع؟
        foreach (InkMark m in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
        {
            if (m.beingEaten || done.Contains(m)) continue;
            var mr = m.GetComponent<MeshRenderer>();
            if (mr == null) continue;
            Vector3 c = mr.bounds.center;
            if (Mathf.Abs(c.x - x) > 1.1f || c.y < top - 1.5f || c.y > top + height) continue;
            done.Add(m);
            growing.Add(new Growing { m = m, c = c, t0 = t, line = !m.closed });
            LevelSfx.Play("grow", 0.8f);
            Fx.Sparkle(c, new Color(0.6f, 1f, 0.6f), 20);
        }
        for (int i = growing.Count - 1; i >= 0; i--)
        {
            Growing g = growing[i];
            if (g.m == null) { growing.RemoveAt(i); continue; }
            float k = Mathf.Clamp01((t - g.t0) / 0.6f);
            float s = 1f + (factor - 1f) * (1f - (1f - k) * (1f - k));
            if (g.line)
            {
                // الخط يكبر حول وسطه، وينزل لمستوى الطريق
                Vector3 c2 = new Vector3(g.c.x, Mathf.Lerp(g.c.y, top + 0.05f, k), 0f);
                g.m.transform.localScale = Vector3.one * s;
                g.m.transform.position = c2 - g.c * s;
            }
            else
            {
                g.m.transform.localScale = Vector3.one * s;
                var rb = g.m.GetComponent<Rigidbody>();
                if (rb != null && k >= 1f) rb.mass *= factor;
            }
            if (k >= 1f) growing.RemoveAt(i);
        }
        bool covered = RowaidaController.BridgeCovers(from, to, top);
        bool wait = HoldHelper.Before(from) && (!covered || growing.Count > 0);
        if (wait && !prompted) { prompted = true; gm.UI.ShowMessage("", 3.6f, "حفرة كبيرة! ارسمي خط صغير داخل الشعاع الأخضر... يكبر ويصير جسر"); }
        HoldHelper.Hold(ref holding, wait);
        if (!covered) LabFeatures.Show(from - 4f, to, new Vector3(from - 1.5f, top - 1f, 0f), new Vector3(to + 1.5f, top + height + 1f, 0f));
    }
}

// ---------- مصعد بحبل: دايرة تطيح في السطل، السطل يثقل وينزل، والمنصة ترفع رويدا ----------
public class PulleyLift : MonoBehaviour
{
    float platX, top, lift, bucketX, y0, riseT = -1f;
    bool holding, prompted, delivered, heavy;
    Transform plat, bucket, wheelA, wheelB, ropeA, ropeB, ropeC;
    Rigidbody platRb, bucketRb;

    public static void Create(Transform root, Vector4 s, GameManager gm)
    {
        var go = new GameObject("Pulley Lift");
        go.transform.SetParent(root, false);
        var p = go.AddComponent<PulleyLift>();
        p.platX = s.x; p.top = s.y; p.lift = s.z; p.bucketX = s.w;
        p.y0 = p.top + 2.7f + p.lift;
        Material frame = gm.Mat("Lab Frame", new Color(0.55f, 0.6f, 0.68f), 0.7f, 0.5f);
        Material rope = gm.Mat("Rope", new Color(0.8f, 0.65f, 0.4f), 0.2f);
        Material bucketM = gm.Mat("Bucket Blue", new Color(0.3f, 0.55f, 0.95f), 0.5f, 0.2f);
        Material wood = gm.Mat("Shelf Wood", new Color(0.7f, 0.5f, 0.3f), 0.3f);
        float wy = p.top + p.lift + 4.6f;
        // عمود وعارضة البكرات
        LevelBuilder.Visual("Pulley Beam", PrimitiveType.Cube, go.transform, new Vector3((p.platX + p.bucketX) * 0.5f, wy + 0.5f, 0.6f), new Vector3(Mathf.Abs(p.platX - p.bucketX) + 1.6f, 0.3f, 0.3f), frame);
        LevelBuilder.Visual("Pulley Post", PrimitiveType.Cube, go.transform, new Vector3(p.bucketX - 1.1f, (p.top + wy) * 0.5f + 0.25f, 1.1f), new Vector3(0.3f, wy - p.top + 0.5f, 0.3f), frame);
        p.wheelA = LevelBuilder.Visual("Wheel", PrimitiveType.Cylinder, go.transform, new Vector3(p.platX, wy, 0.3f), new Vector3(0.8f, 0.1f, 0.8f), frame).transform;
        p.wheelA.localRotation = Quaternion.Euler(90f, 0f, 0f);
        p.wheelB = LevelBuilder.Visual("Wheel", PrimitiveType.Cylinder, go.transform, new Vector3(p.bucketX, wy, 0.3f), new Vector3(0.8f, 0.1f, 0.8f), frame).transform;
        p.wheelB.localRotation = Quaternion.Euler(90f, 0f, 0f);
        for (int k = 0; k < 2; k++)
            LevelBuilder.Visual("Spoke", PrimitiveType.Cube, k == 0 ? p.wheelA : p.wheelB, Vector3.zero, new Vector3(0.9f, 1.2f, 0.12f), gm.Mat("Stove Black", new Color(0.15f, 0.14f, 0.16f), 0.6f, 0.3f), false);
        p.ropeA = LevelBuilder.Visual("Rope", PrimitiveType.Cylinder, go.transform, Vector3.zero, Vector3.one, rope, false).transform;
        p.ropeB = LevelBuilder.Visual("Rope", PrimitiveType.Cylinder, go.transform, Vector3.zero, Vector3.one, rope, false).transform;
        p.ropeC = LevelBuilder.Visual("Rope", PrimitiveType.Cylinder, go.transform, Vector3.zero, Vector3.one, rope, false).transform;
        // المنصة
        var pl = new GameObject("Lift Plate");
        pl.transform.SetParent(go.transform, false);
        pl.transform.position = new Vector3(p.platX, p.top - 0.15f, 0f);
        LevelBuilder.Visual("Plate", PrimitiveType.Cube, pl.transform, Vector3.zero, new Vector3(1.6f, 0.3f, 1.8f), wood);
        pl.AddComponent<BoxCollider>().size = new Vector3(1.6f, 0.3f, 1.8f);
        p.platRb = pl.AddComponent<Rigidbody>();
        p.platRb.isKinematic = true;
        p.platRb.interpolation = RigidbodyInterpolation.Interpolate;
        p.plat = pl.transform;
        // السطل
        var bk = new GameObject("Bucket");
        bk.transform.SetParent(go.transform, false);
        bk.transform.position = new Vector3(p.bucketX, p.y0, 0f);
        var parts = new[] { (new Vector3(0f, 0f, 0f), new Vector3(1.6f, 0.15f, 1.6f)), (new Vector3(-0.78f, 0.55f, 0f), new Vector3(0.12f, 1.1f, 1.6f)), (new Vector3(0.78f, 0.55f, 0f), new Vector3(0.12f, 1.1f, 1.6f)) };
        foreach (var part in parts)
        {
            var v = LevelBuilder.Visual("Bucket Side", PrimitiveType.Cube, bk.transform, part.Item1, part.Item2, bucketM);
            v.AddComponent<BoxCollider>();
        }
        LevelBuilder.Visual("Bucket Back", PrimitiveType.Cube, bk.transform, new Vector3(0f, 0.55f, 0.75f), new Vector3(1.6f, 1.1f, 0.1f), bucketM, false);
        LevelBuilder.Visual("Bucket Handle", PrimitiveType.Cube, bk.transform, new Vector3(0f, 1.25f, 0f), new Vector3(1.5f, 0.06f, 0.06f), frame, false);
        p.bucketRb = bk.AddComponent<Rigidbody>();
        p.bucketRb.isKinematic = true;
        p.bucketRb.interpolation = RigidbodyInterpolation.Interpolate;
        p.bucket = bk.transform;
        // نقاط حول السطل: هنا تطيح الدايرة
        for (int k = 0; k < 10; k++)
        {
            float a = k / 10f * Mathf.PI * 2f;
            LevelBuilder.Visual("Drop Dot", PrimitiveType.Sphere, bk.transform, new Vector3(Mathf.Cos(a) * 0.55f, 2f + Mathf.Sin(a) * 0.55f, -0.5f), Vector3.one * 0.09f, gm.Mat("Hazard Yellow", new Color(1f, 0.82f, 0.15f), 0.4f), false);
        }
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float dt = Time.deltaTime;
        RowaidaController r = gm.Rowaida;
        bool onPlate = r != null && Mathf.Abs(r.transform.position.x - platX) < 0.75f && r.transform.position.y > plat.position.y && r.transform.position.y - plat.position.y < 1.5f;
        if (!heavy)
        {
            InkMark m = LabFeatures.ClosedIn(bucketX - 0.9f, bucketX + 0.9f, y0 - 0.3f, y0 + 1.6f);
            if (m != null)
            {
                var rb = m.GetComponent<Rigidbody>();
                if (rb == null || rb.linearVelocity.magnitude < 1.5f)
                {
                    heavy = true;
                    LevelSfx.Play("plop", 0.8f);
                    gm.UI.ShowMessage("", 2f, "السطل ثقل!");
                }
            }
        }
        // المصعد يتحرك بس لما رويدا واقفة عليه
        if (heavy && riseT < 0f && onPlate) { riseT = 0f; LevelSfx.Play("rope", 0.7f); }
        if (riseT >= 0f && !delivered)
        {
            riseT = Mathf.Min(1f, riseT + dt / 2.6f);
            if (riseT >= 1f) { delivered = true; LevelSfx.Play("chime", 0.6f, 1.2f); }
        }
        float k = riseT < 0f ? 0f : Mathf.SmoothStep(0f, 1f, riseT);
        platRb.MovePosition(new Vector3(platX, top - 0.15f + k * lift, 0f));
        bucketRb.MovePosition(new Vector3(bucketX, y0 - k * lift, 0f));
        wheelA.localRotation = Quaternion.Euler(90f, 0f, 0f) * Quaternion.Euler(0f, k * 400f, 0f);
        wheelB.localRotation = wheelA.localRotation;
        float wy = top + lift + 4.6f;
        LabFeatures2.Stretch(ropeA, new Vector3(platX, plat.position.y + 0.15f, 0.3f), new Vector3(platX, wy - 0.4f, 0.3f), 0.06f);
        LabFeatures2.Stretch(ropeB, new Vector3(platX, wy + 0.4f, 0.3f), new Vector3(bucketX, wy + 0.4f, 0.3f), 0.06f);
        LabFeatures2.Stretch(ropeC, new Vector3(bucketX, wy - 0.4f, 0.3f), new Vector3(bucketX, bucket.position.y + 1.25f, 0.3f), 0.06f);

        bool wait = r != null && gm.IsPlaying && !delivered && r.transform.position.x > platX - 0.2f && r.transform.position.x < platX + 0.6f && r.transform.position.y - top < lift + 1.5f;
        if (wait && !prompted) { prompted = true; gm.UI.ShowMessage("", 3.6f, "مصعد بحبل! ارسمي دايرة تطيح في السطل الأزرق... يثقل وينزل وانتي تطلعين"); }
        HoldHelper.Hold(ref holding, wait);
        if (!delivered) LabFeatures.Show(Mathf.Min(platX, bucketX) - 5f, platX + 0.6f, new Vector3(Mathf.Min(platX, bucketX) - 1.5f, top - 0.5f, 0f), new Vector3(platX + 1.8f, wy + 0.8f, 0f));
    }
}

// ---------- البركان: رغوة تطلع من تحت وترتفع شوي شوي، لو لمست رويدا تخسر ----------
public class VolcanoFoam : MonoBehaviour
{
    float startX, endX, foamY, speed, delay = 3f;
    bool told;
    Transform foam, crater;
    readonly List<Transform> blobs = new List<Transform>();
    float nextRumble, nextSpit;

    public static void Create(Transform root, Vector4 s, GameManager gm)
    {
        var go = new GameObject("Volcano");
        go.transform.SetParent(root, false);
        var v = go.AddComponent<VolcanoFoam>();
        v.startX = s.x; v.endX = s.y; v.foamY = s.z; v.speed = s.w;
        Material rock = gm.Mat("Volcano Rock", new Color(0.45f, 0.3f, 0.25f), 0.3f);
        Material dark = gm.Mat("Volcano Dark", new Color(0.3f, 0.2f, 0.18f), 0.3f);
        Material lava = gm.Mat("Volcano Glow", new Color(2.4f, 0.8f, 1.2f), 0.5f);
        Material foamM = gm.Mat("Volcano Foam", new Color(1f, 0.85f, 0.95f), 0.3f);
        // البركان (تجربة مدرسية كبيرة) ورا البداية
        Vector3 b = new Vector3(v.startX - 4f, -1f, 4f);
        for (int k = 0; k < 7; k++)
        {
            float rr = 7f - k * 0.85f;
            LevelBuilder.Visual("Volcano Layer", PrimitiveType.Cylinder, go.transform, b + new Vector3(0f, k * 0.9f, 0f), new Vector3(rr, 0.45f, rr), k % 2 == 0 ? rock : dark);
        }
        v.crater = LevelBuilder.Visual("Crater", PrimitiveType.Sphere, go.transform, b + new Vector3(0f, 6.4f, 0f), new Vector3(1.8f, 0.6f, 1.8f), lava, false).transform;
        for (int k = 0; k < 5; k++) LevelBuilder.Visual("Foam Drip", PrimitiveType.Sphere, go.transform, b + new Vector3(Mathf.Cos(k * 1.3f) * 1.2f, 5.2f - k * 0.7f, -Mathf.Abs(Mathf.Sin(k * 1.3f)) * 1.5f - 0.6f), new Vector3(0.7f, 1.3f, 0.7f), foamM, false);
        // جسم الرغوة
        v.foam = LevelBuilder.Visual("Foam", PrimitiveType.Cube, go.transform, Vector3.zero, Vector3.one, LabFeatures2.Tint("Foam Body", new Color(1f, 0.82f, 0.93f, 0.85f)), false).transform;
        for (int k = 0; k < 40; k++)
            v.blobs.Add(LevelBuilder.Visual("Foam Blob", PrimitiveType.Sphere, go.transform, Vector3.zero, Vector3.one * Random.Range(0.8f, 1.5f), foamM, false).transform);
        gm.Respawned += v.OnRespawned;
    }

    void OnDestroy() { if (GameManager.Instance != null) GameManager.Instance.Respawned -= OnRespawned; }

    void OnRespawned(Vector3 pos)
    {
        foamY = Mathf.Min(foamY, pos.y - 4f);
        delay = 2.5f;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time, dt = Time.deltaTime;
        RowaidaController r = gm.Rowaida;
        if (gm.IsPlaying)
        {
            if (delay > 0f) delay -= dt;
            else
            {
                float s = speed;
                // لو صارت بعيدة مرة، الرغوة تسرع شوي (عشان يظل فيه حماس)
                if (r != null && r.transform.position.y - foamY > 9f) s *= 1.8f;
                foamY += s * dt;
            }
            if (!told) { told = true; gm.UI.ShowMessage("", 3.8f, "البركان انفجر! الرغوة طالعة... ارسمي درج (خط مايل) لكل درجة بسرعة"); }
        }
        float w = endX - startX + 30f, mid = (startX + endX) * 0.5f;
        foam.position = new Vector3(mid, foamY - 6f, 0.3f);
        foam.localScale = new Vector3(w, 12f, 5f);
        for (int i = 0; i < blobs.Count; i++)
        {
            float bx = startX - 12f + (i / (float)blobs.Count) * w + Mathf.Sin(t * 0.7f + i) * 0.4f;
            blobs[i].position = new Vector3(bx, foamY + Mathf.Sin(t * 2.3f + i * 1.7f) * 0.15f, -1.8f + (i % 5) * 0.9f);
        }
        crater.localScale = new Vector3(1.8f, 0.6f + Mathf.Abs(Mathf.Sin(t * 3f)) * 0.3f, 1.8f);
        if (t > nextSpit)
        {
            nextSpit = t + 0.08f;
            var d = LevelBuilder.Visual("Spit", PrimitiveType.Sphere, null, crater.position + new Vector3(0f, 0.3f, 0f), Vector3.one * Random.Range(0.3f, 0.6f), gm.Mat("Volcano Foam", new Color(1f, 0.85f, 0.95f), 0.3f), false);
            var fx = d.AddComponent<FxParticle>();
            fx.velocity = new Vector3(Random.Range(-2f, 3f), Random.Range(6f, 9f), Random.Range(-1f, 1f)); fx.gravity = 9f; fx.life = 1.6f;
        }
        if (r == null || !gm.IsPlaying) return;
        float gap = r.transform.position.y - 0.4f - foamY;
        if (t > nextRumble && gap < 3f)
        {
            nextRumble = t + 0.9f;
            LevelSfx.Play("gloop", Mathf.Lerp(0.8f, 0.3f, gap / 3f), 0.6f);
            var cam = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
            if (cam != null) cam.Shake(0.05f, 0.3f);
        }
        if (gap < 0f) gm.KillRowaida();
    }
}

// ---------- روبوت مساعد: يقلد أي خط ترسمينه وينسخه للدرجة اللي بعدها ----------
public class RobotHelper : MonoBehaviour
{
    class Job { public InkMark src; public Vector3[] path; }
    public class Copy : MonoBehaviour { }
    float x0, x1;
    Vector3 offset;
    readonly HashSet<InkMark> seen = new HashSet<InkMark>();
    readonly Queue<Job> jobs = new Queue<Job>();
    Job current;
    float jobD, told;
    float[] cum;
    Transform robot, eyeL, eyeR, arm;
    Vector3 vel;

    public static void Create(Transform root, Vector4 s, GameManager gm)
    {
        var go = new GameObject("Robot Helper");
        go.transform.SetParent(root, false);
        var h = go.AddComponent<RobotHelper>();
        h.x0 = s.x; h.x1 = s.y; h.offset = new Vector3(s.z, s.w, 0f);
        Material body = gm.Mat("Robot White", new Color(0.92f, 0.94f, 0.97f), 0.6f, 0.2f);
        Material screen = gm.Mat("Robot Screen", new Color(0.1f, 0.12f, 0.18f), 0.8f);
        Material eye = gm.Mat("Robot Eye", new Color(0.4f, 2f, 2.2f), 0.5f);
        Material orange = gm.Mat("Robot Orange", new Color(1f, 0.55f, 0.15f), 0.5f);
        h.robot = new GameObject("Robot").transform;
        h.robot.SetParent(go.transform, false);
        h.robot.position = new Vector3(h.x0, 4f, -0.6f);
        LevelBuilder.Visual("Robot Body", PrimitiveType.Sphere, h.robot, Vector3.zero, new Vector3(0.9f, 0.8f, 0.8f), body);
        LevelBuilder.Visual("Robot Screen", PrimitiveType.Sphere, h.robot, new Vector3(0f, 0.05f, -0.3f), new Vector3(0.65f, 0.45f, 0.3f), screen, false);
        h.eyeL = LevelBuilder.Visual("Eye", PrimitiveType.Sphere, h.robot, new Vector3(-0.14f, 0.08f, -0.45f), new Vector3(0.12f, 0.16f, 0.05f), eye, false).transform;
        h.eyeR = LevelBuilder.Visual("Eye", PrimitiveType.Sphere, h.robot, new Vector3(0.14f, 0.08f, -0.45f), new Vector3(0.12f, 0.16f, 0.05f), eye, false).transform;
        LevelBuilder.Visual("Antenna", PrimitiveType.Cylinder, h.robot, new Vector3(0f, 0.55f, 0f), new Vector3(0.04f, 0.2f, 0.04f), body, false);
        LevelBuilder.Visual("Antenna Ball", PrimitiveType.Sphere, h.robot, new Vector3(0f, 0.78f, 0f), Vector3.one * 0.14f, orange, false);
        LevelBuilder.Visual("Propeller", PrimitiveType.Cube, h.robot, new Vector3(0f, 0.45f, 0f), new Vector3(1.1f, 0.03f, 0.12f), orange, false);
        h.arm = new GameObject("Arm").transform;
        h.arm.SetParent(h.robot, false);
        h.arm.localPosition = new Vector3(0.3f, -0.3f, 0f);
        LevelBuilder.Visual("Arm", PrimitiveType.Cylinder, h.arm, new Vector3(0f, -0.25f, 0f), new Vector3(0.08f, 0.25f, 0.08f), body, false);
        LevelBuilder.Visual("Pencil", PrimitiveType.Cylinder, h.arm, new Vector3(0f, -0.6f, 0f), new Vector3(0.1f, 0.18f, 0.1f), gm.Mat("Hint Pencil", new Color(1f, 0.8f, 0.2f), 0.4f), false);
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.Rowaida == null) return;
        float t = Time.time, dt = Time.deltaTime;
        // خطوط جديدة بمنطقته
        foreach (InkMark m in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
        {
            if (m.closed || m.path == null || m.path.Length < 2 || seen.Contains(m) || m.GetComponent<Copy>() != null) continue;
            seen.Add(m);
            if (m.path[0].x < x0 || m.path[0].x > x1) continue;
            var p = new Vector3[m.path.Length];
            for (int i = 0; i < p.Length; i++) p[i] = m.path[i] + offset;
            jobs.Enqueue(new Job { src = m, path = p });
        }
        Vector3 rp = gm.Rowaida.transform.position;
        Vector3 goal = rp + new Vector3(-1.4f, 3f + Mathf.Sin(t * 2f) * 0.2f, -0.6f);
        if (current == null && jobs.Count > 0)
        {
            current = jobs.Dequeue();
            if (current.src == null) current = null;
            else { cum = LabFeatures.Cumulative(current.path); jobD = -1f; LevelSfx.Play("motor", 0.5f, 1.6f); }
        }
        if (current != null)
        {
            if (jobD < 0f)
            {
                // يطير لأول الخط
                goal = current.path[0] + new Vector3(0f, 0.9f, -0.6f);
                if (Vector3.Distance(robot.position, goal) < 0.3f) jobD = 0f;
            }
            else
            {
                // يرسم على طول الخط
                jobD += dt * 4.5f;
                Vector3 at = LabFeatures.PointAt(current.path, cum, jobD);
                goal = at + new Vector3(0f, 0.9f, -0.6f);
                robot.position = Vector3.Lerp(robot.position, goal, 0.5f);
                Fx.Sparkle(at + new Vector3(0f, 0f, -0.2f), new Color(1f, 0.85f, 0.3f), 1);
                if (jobD >= cum[cum.Length - 1])
                {
                    if (current.src != null)
                    {
                        var dup = Instantiate(current.src.gameObject, current.src.transform.parent);
                        dup.transform.position = current.src.transform.position + offset;
                        dup.AddComponent<Copy>();
                        var mk = dup.GetComponent<InkMark>();
                        if (mk != null) mk.path = current.path;
                        LevelSfx.Play("chime", 0.6f, 1.5f);
                        Fx.Sparkle(current.path[current.path.Length / 2], new Color(1f, 0.85f, 0.3f), 18);
                        if (t - told > 20f) { told = t; gm.UI.ShowMessage("", 2.2f, "الروبوت قلد رسمتك للدرجة اللي بعدها!"); }
                    }
                    current = null;
                }
            }
        }
        robot.position = Vector3.SmoothDamp(robot.position, goal, ref vel, current != null && jobD >= 0f ? 0.05f : 0.35f);
        robot.localRotation = Quaternion.Euler(0f, 0f, Mathf.Clamp(-vel.x * 4f, -20f, 20f));
        arm.localRotation = Quaternion.Euler(0f, 0f, current != null ? Mathf.Sin(t * 20f) * 15f : 0f);
        float blink = Mathf.Repeat(t, 3f) < 0.12f ? 0.02f : 0.16f;
        eyeL.localScale = eyeR.localScale = new Vector3(0.12f, blink, 0.05f);
    }
}

// ---------- دورق ينفجر كل شوي ويدف رويدا لورا (تستنى لين ينفجر وتعدي) ----------
public class BlastFlask : MonoBehaviour
{
    float x, top, period, t0;
    bool holding, prompted;
    Transform flask, liquid;
    Renderer liquidR;

    public static void Create(Transform root, Vector3 s, GameManager gm)
    {
        var go = new GameObject("Blast Flask");
        go.transform.SetParent(root, false);
        var b = go.AddComponent<BlastFlask>();
        b.x = s.x; b.top = s.y; b.period = s.z;
        b.t0 = Time.time;
        b.flask = new GameObject("Flask").transform;
        b.flask.SetParent(go.transform, false);
        b.flask.position = new Vector3(b.x, b.top, 1.3f);
        LevelBuilder.Visual("Glass", PrimitiveType.Sphere, b.flask, new Vector3(0f, 0.9f, 0f), Vector3.one * 1.8f, LabFeatures.Glass(0.3f), false);
        LevelBuilder.Visual("Neck", PrimitiveType.Cylinder, b.flask, new Vector3(0f, 2.1f, 0f), new Vector3(0.6f, 0.5f, 0.6f), LabFeatures.Glass(0.4f), false);
        LevelBuilder.Visual("Cork", PrimitiveType.Cylinder, b.flask, new Vector3(0f, 2.65f, 0f), new Vector3(0.55f, 0.15f, 0.55f), gm.Mat("Cork", new Color(0.75f, 0.55f, 0.35f), 0.2f), false);
        b.liquid = LevelBuilder.Visual("Liquid", PrimitiveType.Sphere, b.flask, new Vector3(0f, 0.75f, 0f), new Vector3(1.5f, 1.2f, 1.5f), gm.Mat("Blast Green", new Color(0.5f, 2f, 0.4f), 0.5f), false).transform;
        b.liquidR = b.liquid.GetComponent<Renderer>();
        // علامة خطر
        LevelBuilder.Visual("Warning", PrimitiveType.Cube, go.transform, new Vector3(b.x - 1.4f, b.top + 1.4f, 1.1f), new Vector3(0.7f, 0.7f, 0.05f), gm.Mat("Hazard Yellow", new Color(1f, 0.82f, 0.15f), 0.4f), false).transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time, dt = Time.deltaTime;
        float phase = Mathf.Repeat(t - t0, period), left = period - phase;
        float fill = Mathf.Clamp01(phase / (period * 0.8f));
        liquid.localScale = new Vector3(1.5f, 0.3f + fill * 0.9f, 1.5f);
        liquid.localPosition = new Vector3(0f, 0.3f + fill * 0.45f, 0f);
        flask.position = new Vector3(x + (left < 1f ? Mathf.Sin(t * 60f) * 0.06f : 0f), top, 1.3f);
        if (Random.value < fill * 0.3f) Fx.Sparkle(flask.position + new Vector3(Random.Range(-0.5f, 0.5f), 1f, -1f), new Color(0.6f, 1f, 0.5f), 1);
        if (phase < dt && t - t0 > 1f)
        {
            // بووم
            LevelSfx.Play("pop", 1f, 0.6f);
            LevelSfx.Play("rumble", 0.6f, 1.4f);
            for (int k = 0; k < 6; k++) SmokeCloud.Puff(flask.position + new Vector3(Random.Range(-1f, 1f), Random.Range(0.5f, 2f), -1.2f), Random.Range(0.8f, 1.2f));
            for (int k = 0; k < 16; k++)
            {
                var d = LevelBuilder.Visual("Splash", PrimitiveType.Sphere, null, flask.position + new Vector3(0f, 1.2f, -1f), Vector3.one * 0.2f, gm.Mat("Blast Green", new Color(0.5f, 2f, 0.4f), 0.5f), false);
                var fx = d.AddComponent<FxParticle>();
                fx.velocity = new Vector3(Random.Range(-5f, 5f), Random.Range(2f, 7f), Random.Range(-1f, 1f)); fx.gravity = 12f; fx.life = 0.8f;
            }
            RowaidaController r = gm.Rowaida;
            if (r != null && Mathf.Abs(r.transform.position.x - x) < 2f && Mathf.Abs(r.transform.position.y - top - 0.7f) < 2f && !r.IsKnocked)
                r.Knockback(-1f);
        }
        // تستنى قبل الدورق إذا قرب ينفجر
        float cross = 4.4f / 2.8f;
        bool wait = HoldHelper.Before(x - 1.6f) && (left < cross || phase < 0.25f);
        if (wait && !prompted) { prompted = true; gm.UI.ShowMessage("", 2.6f, "دورق بينفجر! استني لين يفرقع وبعدين عدي"); }
        HoldHelper.Hold(ref holding, wait);
    }
}
