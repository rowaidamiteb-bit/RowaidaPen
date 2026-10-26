using System.Collections.Generic;
using UnityEngine;

// الجزء الثاني — فصل الموسيقى: بيانو، طبول، نوتات، أوتار، وحفلة
public static class MusicFeatures
{
    // ألوان مفاتيح البيانو (أحمر، برتقالي، أصفر، أخضر، أزرق) ونغمتها
    public static readonly Color[] KeyColors =
    {
        new Color(0.95f, 0.3f, 0.35f), new Color(1f, 0.6f, 0.2f), new Color(1f, 0.85f, 0.25f),
        new Color(0.35f, 0.8f, 0.4f), new Color(0.3f, 0.55f, 1f),
    };
    // دو ري مي صول لا (نغمات حلوة مع بعض)
    public static readonly float[] KeyPitch = { 1f, 1.122f, 1.26f, 1.498f, 1.682f };

    // مرحلة البيانو: النغمات ترتفع شوي شوي من أول المرحلة لآخرها (على السلم الكبير)
    public static bool Rising;
    static float riseFrom, riseTo;
    static readonly int[] Major = { 0, 2, 4, 5, 7, 9, 11 };

    public static float RisePitch(float x)
    {
        float k = Mathf.Clamp01(Mathf.InverseLerp(riseFrom, riseTo, x));
        int degree = Mathf.RoundToInt(k * 13f);   // تقريبًا أوكتافين
        int semis = Major[degree % 7] + 12 * (degree / 7);
        return 0.75f * Mathf.Pow(2f, semis / 12f);
    }

    public static void Build(Transform root, LevelData data, GameManager gm, System.Random rng)
    {
        Rising = data.risingPianos;
        riseFrom = data.spawn.x; riseTo = data.door.x;
        foreach (PianoSpec p in data.pianos) PianoPuzzle.Create(root, p, gm);
        foreach (Vector3 d in data.drumPits) DrumPit.Create(root, d, gm);
        foreach (NoteSpec n in data.noteSections) NoteSection.Create(root, n, gm);
        foreach (Vector3 s in data.strings) StringBridge.Create(root, s, gm);
        foreach (StaffSpec s in data.staffs) StaffBridge.Create(root, s, gm);
    }
}

// بيانو: ورقة نوتة فيها ألوان بالترتيب، ومفاتيح ملونة طايرة فوق الطريق.
// ارمي كور على المفاتيح بنفس الترتيب: يصح اللحن وينفتح الباب. لو غلطتي يبدا اللحن من أول
public class PianoSpec
{
    public float gateX, keysX, keysY = 3f;
    public int[] song;   // أرقام المفاتيح بالترتيب (0 أحمر .. 4 أزرق)
    public PianoSpec(float gateX, float keysX, params int[] song) { this.gateX = gateX; this.keysX = keysX; this.song = song; }
}

public class PianoPuzzle : MonoBehaviour
{
    const float Spacing = 1.3f;
    PianoSpec spec;
    readonly List<Transform> keys = new List<Transform>();
    readonly List<Transform> sheetNotes = new List<Transform>();
    readonly HashSet<GameObject> used = new HashSet<GameObject>();
    int progress;
    float openT = -1f, flash;
    Transform door;
    BoxCollider doorCol;
    bool holding;
    int prompted;

    public static void Create(Transform root, PianoSpec spec, GameManager gm)
    {
        var go = new GameObject("Piano");
        go.transform.SetParent(root, false);
        var p = go.AddComponent<PianoPuzzle>();
        p.spec = spec;
        Material wood = gm.Mat("Piano Wood", new Color(0.15f, 0.12f, 0.14f), 0.7f);
        Material ivory = gm.Mat("Piano Ivory", new Color(0.97f, 0.96f, 0.92f), 0.6f);
        Material paper = gm.Mat("Sheet Paper", new Color(1f, 0.98f, 0.92f), 0.2f);
        Material line = gm.Mat("Sheet Line", new Color(0.2f, 0.2f, 0.25f), 0.3f);

        // لوحة المفاتيح
        int n = MusicFeatures.KeyColors.Length;
        float cx = spec.keysX + (n - 1) * Spacing * 0.5f;
        var body = LevelBuilder.Visual("Piano Body", PrimitiveType.Cube, go.transform, new Vector3(cx, spec.keysY - 0.35f, 0.1f), new Vector3(n * Spacing + 0.5f, 0.6f, 2.2f), wood);
        body.AddComponent<BoxCollider>();
        for (int i = 0; i < n; i++)
        {
            float x = spec.keysX + i * Spacing;
            var key = LevelBuilder.Visual("Key", PrimitiveType.Cube, go.transform, new Vector3(x, spec.keysY + 0.05f, 0f), new Vector3(1.15f, 0.2f, 1.9f), ivory).transform;
            key.gameObject.AddComponent<BoxCollider>();
            // نقطة ملونة على المفتاح
            LevelBuilder.Visual("Dot", PrimitiveType.Cylinder, key, new Vector3(0f, 0.55f, -0.25f), new Vector3(0.55f, 0.3f, 0.3f), gm.Mat("Piano Key " + i, MusicFeatures.KeyColors[i], 0.5f), false);
            p.keys.Add(key);
        }

        // ورقة النوتة فوق: خمس خطوط وعليها النوتات بالألوان المطلوبة
        float sw = Mathf.Max(3.5f, spec.song.Length * 1.1f + 1f);
        LevelBuilder.Visual("Sheet", PrimitiveType.Cube, go.transform, new Vector3(cx, spec.keysY + 1.9f, 0.5f), new Vector3(sw, 1.4f, 0.05f), paper, false);
        for (int l = 0; l < 5; l++)
            LevelBuilder.Visual("Staff", PrimitiveType.Cube, go.transform, new Vector3(cx, spec.keysY + 1.45f + l * 0.22f, 0.46f), new Vector3(sw - 0.3f, 0.025f, 0.02f), line, false);
        for (int s = 0; s < spec.song.Length; s++)
        {
            float x = cx - (spec.song.Length - 1) * 0.55f + s * 1.1f;
            var note = new GameObject("Note").transform;
            note.SetParent(go.transform, false);
            note.position = new Vector3(x, spec.keysY + 1.65f + spec.song[s] * 0.11f, 0.4f);
            Material m = gm.Mat("Piano Key " + spec.song[s], MusicFeatures.KeyColors[spec.song[s]], 0.5f);
            LevelBuilder.Visual("Head", PrimitiveType.Sphere, note, Vector3.zero, new Vector3(0.42f, 0.32f, 0.1f), m, false);
            LevelBuilder.Visual("Stem", PrimitiveType.Cube, note, new Vector3(0.18f, 0.35f, 0f), new Vector3(0.05f, 0.7f, 0.04f), line, false);
            p.sheetNotes.Add(note);
        }

        // الباب: غطا بيانو أسود
        p.door = new GameObject("Piano Door").transform;
        p.door.SetParent(go.transform, false);
        p.door.position = new Vector3(spec.gateX, 0f, 0f);
        LevelBuilder.Visual("Door", PrimitiveType.Cube, p.door, new Vector3(0f, 1.6f, 0f), new Vector3(0.6f, 3.2f, 2.3f), wood);
        for (int i = 0; i < 4; i++)
            LevelBuilder.Visual("Stripe", PrimitiveType.Cube, p.door, new Vector3(-0.31f, 0.5f + i * 0.7f, 0f), new Vector3(0.02f, 0.3f, 1.8f), ivory, false);
        p.doorCol = p.door.gameObject.AddComponent<BoxCollider>();
        p.doorCol.center = new Vector3(0f, 1.6f, 0f);
        p.doorCol.size = new Vector3(0.6f, 3.2f, 2.3f);
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        // النوتات اللي انعزفت تكبر وتلمع
        for (int s = 0; s < sheetNotes.Count; s++)
        {
            float big = s < progress ? 1.35f + Mathf.Sin(Time.time * 6f + s) * 0.08f : 1f;
            sheetNotes[s].localScale = Vector3.one * big;
        }
        if (flash > 0f) flash -= Time.deltaTime;

        if (openT >= 0f)
        {
            openT += Time.deltaTime;
            door.position = new Vector3(spec.gateX, -Mathf.SmoothStep(0f, 3.6f, Mathf.Clamp01(openT / 0.8f)), 0f);
        }
        else
        {
            // كورة نزلت على مفتاح؟
            for (int i = 0; i < keys.Count; i++)
            {
                Vector3 k = keys[i].position;
                foreach (InkMark mark in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
                {
                    if (!mark.closed || used.Contains(mark.gameObject)) continue;
                    Vector3 c = mark.transform.position;
                    if (Mathf.Abs(c.x - k.x) > 0.65f || c.y < k.y || c.y > k.y + 1.5f) continue;
                    used.Add(mark.gameObject);
                    Play(i, mark.gameObject, gm);
                    break;
                }
            }
        }

        RowaidaController r = gm.Rowaida;
        if (r == null) return;
        float px = r.transform.position.x;
        bool wait = openT < 0f && px > spec.gateX - 1.6f && px < spec.gateX - 0.3f && gm.IsPlaying;
        if (wait && prompted == 0)
        {
            prompted = 1;
            gm.UI.ShowMessage("", 2.8f, "اعزفي اللحن! ارمي كور على المفاتيح بنفس ترتيب ألوان النوتة");
        }
        if (wait) { r.Held = true; r.CarryX = 0f; holding = true; CameraFollow.PullRequest = Mathf.Max(CameraFollow.PullRequest, 1f); }
        else if (holding) { r.Held = false; holding = false; }
    }

    void Play(int key, GameObject ball, GameManager gm)
    {
        // كل بيانو بالمرحلة نغماته أعلى من اللي قبله
        float lift = MusicFeatures.Rising ? MusicFeatures.RisePitch(keys[key].position.x) / 0.75f * 0.8f : 1f;
        LevelSfx.Play("piano", 1f, MusicFeatures.KeyPitch[key] * lift);
        StartCoroutine(Press(keys[key]));
        Fx.Sparkle(keys[key].position + Vector3.up * 0.5f, MusicFeatures.KeyColors[key], 14);
        // الكورة تختفي بعد ما تعزف (عشان تقدرين تعزفين نفس المفتاح مرة ثانية)
        Destroy(ball, 0.4f);
        if (spec.song[progress] == key)
        {
            progress++;
            if (progress >= spec.song.Length)
            {
                openT = 0f;
                doorCol.enabled = false;
                LevelSfx.Play("success");
                Fx.Confetti(new Vector3(spec.gateX, 2f, -0.5f), 30);
                gm.UI.ShowMessage("", 1.8f, "لحن حلو! انفتح الباب");
            }
        }
        else
        {
            progress = spec.song[0] == key ? 1 : 0;
            flash = 0.6f;
            LevelSfx.Play("error");
            gm.UI.ShowMessage("", 2f, "نوتة غلط! ابدئي اللحن من أوله");
        }
    }

    System.Collections.IEnumerator Press(Transform key)
    {
        Vector3 home = key.localPosition;
        key.localPosition = home + Vector3.down * 0.15f;
        yield return new WaitForSeconds(0.25f);
        key.localPosition = home;
    }
}

// حفرة طبول: رويدا تنزل لقاع الحفرة وقدامها جدار عالي. ارسمي دايرة بالقاع: تصير طبل، ورويدا تنط عليه لفوق الجدار
public class DrumPit : MonoBehaviour
{
    float from, to, floorY;
    Transform drum, skin;
    float drumX, drumW, squash, nextBounce;
    bool holding;
    int prompted;

    // spec: (من x، لـ x، قاع الحفرة)
    public static void Create(Transform root, Vector3 spec, GameManager gm)
    {
        var go = new GameObject("Drum Pit");
        go.transform.SetParent(root, false);
        var d = go.AddComponent<DrumPit>();
        d.from = spec.x; d.to = spec.y; d.floorY = spec.z;
    }

    void MakeDrum(InkMark mark, GameManager gm)
    {
        var r = mark.GetComponent<MeshRenderer>();
        Bounds b = r.bounds;
        drumX = Mathf.Clamp(b.center.x, from + 2.2f, to - 1.6f);   // بعيد شوي عن الجدار عشان تلحق تطلع فوقه
        drumW = Mathf.Clamp(b.size.x, 1f, 2.2f);
        Destroy(mark.gameObject);
        Material red = gm.Mat("Drum Red", new Color(0.9f, 0.2f, 0.25f), 0.5f);
        Material gold = gm.Mat("Drum Gold", new Color(1f, 0.8f, 0.3f), 0.8f, 0.5f);
        Material white = gm.Mat("Drum Skin", new Color(0.98f, 0.96f, 0.9f), 0.3f);
        drum = new GameObject("Drum").transform;
        drum.SetParent(transform, false);
        drum.position = new Vector3(drumX, floorY, 0f);
        LevelBuilder.Visual("Body", PrimitiveType.Cylinder, drum, new Vector3(0f, 0.45f, 0f), new Vector3(drumW, 0.45f, drumW), red);
        LevelBuilder.Visual("Rim Top", PrimitiveType.Cylinder, drum, new Vector3(0f, 0.88f, 0f), new Vector3(drumW + 0.08f, 0.05f, drumW + 0.08f), gold);
        LevelBuilder.Visual("Rim Bottom", PrimitiveType.Cylinder, drum, new Vector3(0f, 0.04f, 0f), new Vector3(drumW + 0.08f, 0.05f, drumW + 0.08f), gold);
        skin = LevelBuilder.Visual("Skin", PrimitiveType.Cylinder, drum, new Vector3(0f, 0.92f, 0f), new Vector3(drumW - 0.05f, 0.02f, drumW - 0.05f), white).transform;
        for (int i = 0; i < 6; i++)
        {
            var zig = LevelBuilder.Visual("Zigzag", PrimitiveType.Cube, drum, new Vector3(-drumW * 0.4f + i * drumW * 0.16f, 0.45f, -drumW * 0.5f - 0.01f), new Vector3(0.05f, 0.7f, 0.02f), gold, false).transform;
            zig.localRotation = Quaternion.Euler(0f, 0f, i % 2 == 0 ? 25f : -25f);
        }
        LevelSfx.Play("drum");
        Fx.Sparkle(drum.position + Vector3.up, new Color(1f, 0.8f, 0.3f), 16);
        gm.UI.ShowMessage("", 1.6f, "صار طبل!");
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        // دايرة نزلت لقاع الحفرة؟ تصير طبل
        if (drum == null)
            foreach (InkMark mark in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
            {
                if (!mark.closed) continue;
                var r = mark.GetComponent<MeshRenderer>();
                if (r == null) continue;
                Bounds b = r.bounds;
                if (b.center.x < from || b.center.x > to || b.min.y > floorY + 0.4f) continue;
                MakeDrum(mark, gm);
                break;
            }

        if (drum != null)
        {
            squash = Mathf.Max(0f, squash - Time.deltaTime * 4f);
            drum.localScale = new Vector3(1f + squash * 0.15f, 1f - squash * 0.25f, 1f + squash * 0.15f);
        }

        RowaidaController r0 = gm.Rowaida;
        if (r0 == null) return;
        Vector3 p = r0.transform.position;
        // نطت على الطبل؟ تطير لفوق
        if (drum != null && Time.time >= nextBounce && Mathf.Abs(p.x - drumX) < drumW * 0.5f + 0.2f && p.y < floorY + 2.2f)
        {
            nextBounce = Time.time + 0.5f;
            if (r0.Bounce(11.5f))
            {
                squash = 1f;
                LevelSfx.Play("drum");
                
                Fx.Sparkle(drum.position + Vector3.up, new Color(1f, 0.85f, 0.3f), 12);
            }
        }
        // بقاع الحفرة بدون طبل: تستنى قبل الجدار
        bool inPit = p.x > from && p.x < to && p.y < floorY + 1.5f;
        bool wait = inPit && drum == null && gm.IsPlaying && p.x > from + 0.6f;   // توقف أول ما تنزل، والطبل يكون قدامها
        if (inPit && drum == null && prompted == 0)
        {
            prompted = 1;
            gm.UI.ShowMessage("", 2.8f, "الجدار عالي! ارسمي دايرة بقاع الحفرة تصير طبل وتنطين عليه");
        }
        if (wait) { r0.Held = true; r0.CarryX = 0f; holding = true; }
        else if (holding) { r0.Held = false; holding = false; }
    }
}

// قسم نوتات: نوتات طايرة فوق الطريق، وباب ما ينفتح إلا إذا جمعتها كلها.
// رويدا تستنى عند بداية القسم لين ترسمين طريق (مثل التلة) يوصلها للنوتات. لو فاتتها نوتة ترجع للبداية
public class NoteSpec
{
    public float startX, gateX;
    public Vector2[] notes;
    public NoteSpec(float startX, float gateX, params Vector2[] notes) { this.startX = startX; this.gateX = gateX; this.notes = notes; }
}

public class NoteSection : MonoBehaviour
{
    NoteSpec spec;
    readonly List<Transform> notes = new List<Transform>();
    readonly List<Transform> icons = new List<Transform>();
    Transform door;
    BoxCollider doorCol;
    float openT = -1f;
    bool started, holding;
    int prompted;

    public static void Create(Transform root, NoteSpec spec, GameManager gm)
    {
        var go = new GameObject("Note Section");
        go.transform.SetParent(root, false);
        var s = go.AddComponent<NoteSection>();
        s.spec = spec;
        Material black = gm.Mat("Note Black", new Color(0.12f, 0.1f, 0.15f), 0.6f);
        for (int i = 0; i < spec.notes.Length; i++)
        {
            Color c = MusicFeatures.KeyColors[(i * 2 + (int)spec.startX) % MusicFeatures.KeyColors.Length];
            Material m = gm.Mat("Note Glow " + i % 5, c * 1.3f, 0.6f);
            var n = new GameObject("Note").transform;
            n.SetParent(go.transform, false);
            n.position = new Vector3(spec.notes[i].x, spec.notes[i].y, 0f);
            LevelBuilder.Visual("Head", PrimitiveType.Sphere, n, new Vector3(-0.12f, -0.3f, 0f), new Vector3(0.55f, 0.42f, 0.25f), m);
            LevelBuilder.Visual("Stem", PrimitiveType.Cube, n, new Vector3(0.12f, 0.15f, 0f), new Vector3(0.08f, 0.9f, 0.08f), black);
            var flag = LevelBuilder.Visual("Flag", PrimitiveType.Cube, n, new Vector3(0.3f, 0.5f, 0f), new Vector3(0.35f, 0.12f, 0.08f), black).transform;
            flag.localRotation = Quaternion.Euler(0f, 0f, -35f);
            var light = n.gameObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = c;
            light.range = 2.5f;
            light.intensity = 1.2f;
            s.notes.Add(n);
        }
        // الباب: عليه نوتات صغيرة (وحدة لكل نوتة لازم تنجمع)
        Material wood = gm.Mat("Piano Wood", new Color(0.15f, 0.12f, 0.14f), 0.7f);
        Material dim = gm.Mat("Note Dim", new Color(0.5f, 0.5f, 0.55f), 0.3f);
        s.door = new GameObject("Note Door").transform;
        s.door.SetParent(go.transform, false);
        s.door.position = new Vector3(spec.gateX, 0f, 0f);
        LevelBuilder.Visual("Door", PrimitiveType.Cube, s.door, new Vector3(0f, 1.6f, 0f), new Vector3(0.6f, 3.2f, 2.3f), wood);
        for (int i = 0; i < spec.notes.Length; i++)
            s.icons.Add(LevelBuilder.Visual("Slot", PrimitiveType.Sphere, s.door, new Vector3(-0.32f, 2.6f - i * 0.6f, 0f), new Vector3(0.05f, 0.35f, 0.45f), dim, false).transform);
        s.doorCol = s.door.gameObject.AddComponent<BoxCollider>();
        s.doorCol.center = new Vector3(0f, 1.6f, 0f);
        s.doorCol.size = new Vector3(0.6f, 3.2f, 2.3f);
    }

    int Collected() { int c = 0; foreach (Transform n in notes) if (!n.gameObject.activeSelf) c++; return c; }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time;
        RowaidaController r = gm.Rowaida;

        // النوتات تتمايل، ورويدا تجمعها إذا لمستها
        for (int i = 0; i < notes.Count; i++)
        {
            Transform n = notes[i];
            if (!n.gameObject.activeSelf) continue;
            n.position = new Vector3(spec.notes[i].x, spec.notes[i].y + Mathf.Sin(t * 2f + i) * 0.12f, 0f);
            n.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.5f + i) * 10f);
            if (r != null && (r.transform.position + Vector3.up * 0.3f - n.position).sqrMagnitude < 1.1f * 1.1f)
            {
                n.gameObject.SetActive(false);
                LevelSfx.Play("chime", 0.9f, MusicFeatures.KeyPitch[(spec.notes.Length - notes.FindAll(x => x.gameObject.activeSelf).Count) % MusicFeatures.KeyPitch.Length]);
                Fx.Sparkle(n.position, MusicFeatures.KeyColors[i % 5], 18);
                // النوتة تنحط على الباب
                icons[Collected() - 1].GetComponent<Renderer>().sharedMaterial = n.GetComponentInChildren<Renderer>().sharedMaterial;
            }
        }
        if (openT < 0f && Collected() == notes.Count)
        {
            openT = 0f;
            doorCol.enabled = false;
            LevelSfx.Play("success");
            Fx.Confetti(new Vector3(spec.gateX, 2f, -0.5f), 30);
        }
        if (openT >= 0f && openT < 1f)
        {
            openT += Time.deltaTime / 0.8f;
            door.position = new Vector3(spec.gateX, -Mathf.SmoothStep(0f, 3.6f, Mathf.Clamp01(openT)), 0f);
        }

        if (r == null) return;
        Vector3 p = r.transform.position;
        // بداية القسم: تستنى لين ينرسم خط بالقسم
        if (!started)
        {
            bool line = false;
            foreach (InkMark mark in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
            {
                var rd = mark.GetComponent<MeshRenderer>();
                if (rd != null && rd.bounds.max.x > spec.startX && rd.bounds.min.x < spec.gateX) { line = true; break; }
            }
            bool atStart = p.x > spec.startX - 1.2f && p.x < spec.startX && gm.IsPlaying;
            if (atStart && prompted == 0)
            {
                prompted = 1;
                gm.UI.ShowMessage("", 2.8f, "جمعي النوتات! ارسمي طريق مثل التلة يوصل رويدا لها");
            }
            if (line) started = true;
            if (atStart && !line) { r.Held = true; r.CarryX = 0f; holding = true; CameraFollow.PullRequest = Mathf.Max(CameraFollow.PullRequest, 1f); }
            else if (holding) { r.Held = false; holding = false; }
            return;
        }
        if (holding) { r.Held = false; holding = false; }
        // وصلت الباب وفاتتها نوتة: ترجع لبداية القسم
        if (openT < 0f && p.x > spec.gateX - 1.2f && p.x < spec.gateX && gm.IsPlaying)
        {
            Fx.Sparkle(p, new Color(0.8f, 0.7f, 1f), 16);
            LevelSfx.Play("whoosh");
            r.Respawn(new Vector3(spec.startX - 1f, 1f, 0f));
            started = false;
            Camera.main.GetComponent<CameraFollow>().SetTarget(r.transform, true);
            gm.UI.ShowMessage("", 2.4f, "فاتتك نوتة! ارسمي طريق يوصلك لها");
        }
    }
}

// وتر مشدود على حفرة: يهتز طول الوقت وما تقدر رويدا تمشي عليه.
// ارسمي خط يلمسه (مثل الإصبع): يسكت ويصير جسر. بعض الأوتار ترجع تهتز بعد شوي
public class StringBridge : MonoBehaviour
{
    float from, to, unmuteAfter, mutedAt = -1f, phase;
    Mesh mesh;
    BoxCollider col;
    bool holding;
    int prompted = -1;
    Transform pegL, pegR;
    readonly List<Vector3> pts = new List<Vector3>();
    AudioSource buzz;
    bool Muted => mutedAt >= 0f && (unmuteAfter <= 0f || Time.time - mutedAt < unmuteAfter);

    // spec: (من x، لـ x، يرجع يهتز بعد كم ثانية (0 = أبد))
    public static void Create(Transform root, Vector3 spec, GameManager gm)
    {
        var go = new GameObject("String Bridge");
        go.transform.SetParent(root, false);
        var s = go.AddComponent<StringBridge>();
        s.from = spec.x; s.to = spec.y; s.unmuteAfter = spec.z;
        s.phase = spec.x;
        Material steel = gm.Mat("String Steel", new Color(0.85f, 0.75f, 0.5f), 0.9f, 0.7f);
        Material wood = gm.Mat("Guitar Wood", new Color(0.55f, 0.3f, 0.15f), 0.5f);
        Material pearl = gm.Mat("Guitar Peg", new Color(0.97f, 0.95f, 0.9f), 0.8f);
        var stringGo = new GameObject("String");
        stringGo.transform.SetParent(go.transform, false);
        s.mesh = new Mesh { name = "String" };
        stringGo.AddComponent<MeshFilter>().sharedMesh = s.mesh;
        stringGo.AddComponent<MeshRenderer>().sharedMaterial = steel;
        // مسامير الشد على الطرفين
        for (int e = 0; e < 2; e++)
        {
            float x = e == 0 ? s.from - 0.1f : s.to + 0.1f;
            var peg = LevelBuilder.Visual("Peg", PrimitiveType.Cylinder, go.transform, new Vector3(x, 0.1f, -0.3f), new Vector3(0.35f, 0.15f, 0.35f), pearl).transform;
            peg.localRotation = Quaternion.Euler(90f, 0f, 0f);
            LevelBuilder.Visual("Bridge Nut", PrimitiveType.Cube, go.transform, new Vector3(x, -0.12f, 0f), new Vector3(0.3f, 0.25f, 1.6f), wood);
            if (e == 0) s.pegL = peg; else s.pegR = peg;
        }
        s.col = go.AddComponent<BoxCollider>();
        s.col.center = new Vector3((s.from + s.to) * 0.5f, -0.1f, 0f);
        s.col.size = new Vector3(s.to - s.from, 0.2f, 1.6f);
        s.col.enabled = false;
        s.buzz = LevelSfx.LoopSource(go, "string_buzz", 0f);
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        bool muted = Muted;
        // الوتر وهو يهتز يطن طنين مزعج (يعلى كل ما قربت رويدا)، ولما يسكت يسكت الطنين
        if (buzz != null)
        {
            float dist = gm.Rowaida != null ? Mathf.Abs(gm.Rowaida.transform.position.x - (from + to) * 0.5f) : 99f;
            float want = muted || !gm.IsPlaying ? 0f : Mathf.Clamp01(1f - dist / 14f) * 0.35f;
            buzz.volume = Mathf.MoveTowards(buzz.volume, want, Time.deltaTime * 2f);
            if (buzz.volume > 0.01f && !buzz.isPlaying) buzz.Play();
            else if (buzz.volume <= 0.01f && buzz.isPlaying) buzz.Stop();
        }
        col.enabled = muted;
        float t = Time.time;

        // شكل الوتر: يتموج وهو يهتز، ومستقيم وهو ساكت
        pts.Clear();
        float len = to - from;
        for (int i = 0; i <= 30; i++)
        {
            float k = i / 30f;
            // موجتين على طول الوتر وتتحرك (تبان إنه يهتز حتى بالصورة)
            float wave = muted ? 0f : Mathf.Sin(k * Mathf.PI) * Mathf.Sin(k * Mathf.PI * 3f - t * 9f + phase) * 0.5f;
            pts.Add(new Vector3(from + k * len, wave, 0f));
        }
        TubeMesh.Build(mesh, pts, 0.1f, false, 6);
        pegL.localRotation = pegR.localRotation = Quaternion.Euler(90f, muted ? 0f : t * 200f, 0f);

        if (!muted)
        {
            // خط يلمس الوتر: يسكته (والخط يختفي)
            foreach (InkMark mark in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
            {
                var r = mark.GetComponent<MeshRenderer>();
                if (r == null) continue;
                Bounds b = r.bounds;
                if (b.max.x < from + 0.3f || b.min.x > to - 0.3f || b.min.y > 0.9f || b.max.y < -0.7f) continue;
                mutedAt = t;
                LevelSfx.Play("pluck", 1f, 1f + (from % 3f) * 0.12f);
                Fx.Sparkle(b.center, new Color(1f, 0.85f, 0.5f), 14);
                Destroy(mark.gameObject);
                break;
            }
            if (mutedAt >= 0f && unmuteAfter > 0f && t - mutedAt >= unmuteAfter && t - mutedAt < unmuteAfter + 0.05f)
                gm.Sound.PlayRumble(0.2f);
        }

        RowaidaController rw = gm.Rowaida;
        if (rw == null) return;
        Vector3 p = rw.transform.position;
        bool atEdge = p.x > from - 1.6f && p.x < from - 0.4f && p.y > 0.3f;
        bool wait = atEdge && !muted && gm.IsPlaying;
        int stage = mutedAt < 0f ? 0 : 1;
        if (wait && prompted != stage)
        {
            prompted = stage;
            gm.UI.ShowMessage("", 2.6f, stage == 0 ? "الوتر يهتز! ارسمي خط يلمسه عشان يسكت" : "رجع يهتز! سكتيه مرة ثانية");
        }
        if (wait) { rw.Held = true; rw.CarryX = 0f; holding = true; }
        else if (holding) { rw.Held = false; holding = false; }
    }
}

// نوتة زعلانة (نشاز): رمادية بنفسجية ووجهها معصب. لو لمستها رويدا تطلع صوت نشاز وترجعها لورا
public class BadNote : MonoBehaviour
{
    Vector3 home;
    float nextHit;

    public static void Create(Transform root, Vector2 pos, GameManager gm)
    {
        var go = new GameObject("Bad Note");
        go.transform.SetParent(root, false);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        var n = go.AddComponent<BadNote>();
        n.home = go.transform.position;
        Material sour = gm.Mat("Sour Note", new Color(0.45f, 0.3f, 0.55f), 0.5f);
        Material black = gm.Mat("Note Black", new Color(0.12f, 0.1f, 0.15f), 0.6f);
        Material white = gm.Mat("Monster Eye", Color.white, 0.7f);
        LevelBuilder.Visual("Head", PrimitiveType.Sphere, go.transform, new Vector3(-0.12f, -0.3f, 0f), new Vector3(0.65f, 0.5f, 0.3f), sour);
        LevelBuilder.Visual("Stem", PrimitiveType.Cube, go.transform, new Vector3(0.18f, 0.15f, 0f), new Vector3(0.08f, 0.9f, 0.08f), black);
        // عيون معصبة
        for (int s = -1; s <= 1; s += 2)
        {
            LevelBuilder.Visual("Eye", PrimitiveType.Sphere, go.transform, new Vector3(-0.12f + s * 0.13f, -0.25f, -0.15f), Vector3.one * 0.12f, white, false);
            var brow = LevelBuilder.Visual("Brow", PrimitiveType.Cube, go.transform, new Vector3(-0.12f + s * 0.13f, -0.15f, -0.17f), new Vector3(0.14f, 0.03f, 0.02f), black, false).transform;
            brow.localRotation = Quaternion.Euler(0f, 0f, s * -25f);
        }
        // سحابة صغيرة غامقة فوقها (عشان تبان إنها شي سيئ)
        LevelBuilder.Visual("Cloud", PrimitiveType.Sphere, go.transform, new Vector3(0.1f, 0.75f, 0.1f), new Vector3(0.6f, 0.3f, 0.3f), sour, false);
    }

    void Update()
    {
        float t = Time.time;
        transform.position = home + new Vector3(Mathf.Sin(t * 1.3f + home.x) * 0.25f, Mathf.Sin(t * 2.1f + home.x) * 0.15f, 0f);
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.Rowaida == null || t < nextHit) return;
        Vector3 d = gm.Rowaida.transform.position + Vector3.up * 0.3f - transform.position;
        if (d.sqrMagnitude < 0.95f * 0.95f)
        {
            nextHit = t + 1.2f;
            gm.Rowaida.Knockback(-1.2f);
            LevelSfx.Play("error");
            Fx.Sparkle(transform.position, new Color(0.5f, 0.3f, 0.6f), 14);
        }
    }
}

// مفاتيح بيانو مرسومة على الأرض: كل ما تمشي رويدا على مفتاح يضوي ويعزف نغمته
public class FloorPiano : MonoBehaviour
{
    float from, to;
    readonly List<Transform> keys = new List<Transform>();
    readonly List<Material> lit = new List<Material>();
    Material ivory, ebony;
    int lastKey = -1;
    float[] glow;

    public static void Create(Transform root, Vector2 spec, GameManager gm)
    {
        var go = new GameObject("Floor Piano");
        go.transform.SetParent(root, false);
        var f = go.AddComponent<FloorPiano>();
        f.from = spec.x; f.to = spec.y;
        f.ivory = gm.Mat("Piano Ivory", new Color(0.97f, 0.96f, 0.92f), 0.6f);
        f.ebony = gm.Mat("Piano Wood", new Color(0.15f, 0.12f, 0.14f), 0.7f);
        int n = Mathf.FloorToInt((f.to - f.from) / 0.9f);
        for (int i = 0; i < n; i++)
        {
            float x = f.from + 0.45f + i * 0.9f;
            f.keys.Add(LevelBuilder.Visual("Floor Key", PrimitiveType.Cube, go.transform, new Vector3(x, 0.02f, 0f), new Vector3(0.82f, 0.04f, 1.8f), f.ivory, false).transform);
            f.lit.Add(gm.Mat("Floor Key Lit " + i % 5, MusicFeatures.KeyColors[i % 5] * 1.3f, 0.6f));
            // مفاتيح سودا صغيرة بين بعض المفاتيح
            if (i % 7 != 2 && i % 7 != 6 && i < n - 1)
                LevelBuilder.Visual("Black Key", PrimitiveType.Cube, go.transform, new Vector3(x + 0.45f, 0.04f, 0.4f), new Vector3(0.45f, 0.05f, 1f), f.ebony, false);
        }
        f.glow = new float[n];
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.Rowaida == null) return;
        float x = gm.Rowaida.transform.position.x;
        int k = Mathf.FloorToInt((x - from) / 0.9f);
        if (k >= 0 && k < keys.Count && k != lastKey && gm.Rowaida.transform.position.y < 1.2f)
        {
            lastKey = k;
            glow[k] = 1f;
            // سلم موسيقي يطلع وينزل
            float[] scale = MusicFeatures.KeyPitch;
            int step = k % (scale.Length * 2 - 2);
            int idx = step < scale.Length ? step : scale.Length * 2 - 2 - step;
            float pitch = scale[idx];
            // مرحلة البيانو: كل مفتاح أعلى من اللي قبله لين آخر المرحلة
            if (MusicFeatures.Rising) pitch = MusicFeatures.RisePitch(from + (k + 0.5f) * 0.9f);
            LevelSfx.Play("piano", MusicFeatures.Rising ? 0.6f : 0.45f, pitch);
        }
        for (int i = 0; i < keys.Count; i++)
        {
            glow[i] = Mathf.Max(0f, glow[i] - Time.deltaTime * 2f);
            keys[i].GetComponent<Renderer>().sharedMaterial = glow[i] > 0.05f ? lit[i] : ivory;
            keys[i].localPosition = new Vector3(keys[i].localPosition.x, 0.02f - glow[i] * 0.03f, 0f);
        }
    }
}

// سطر موسيقي (المدرج) فوق حفرة. عليه دواير منقطة: ارسمي نوتة (دايرة) بكل مكان منقط.
// كل نوتة تعزف نغمتها، ولما يكتمل اللحن يعزف كله والسطور تصير جسر
public class StaffSpec
{
    public float from, to, top;
    public Vector2[] notes;   // (x، درجة على السطر: 0 أوطى سطر، كل 1 = نص مسافة)
    public StaffSpec(float from, float to, float top, params Vector2[] notes) { this.from = from; this.to = to; this.top = top; this.notes = notes; }
}

public class StaffBridge : MonoBehaviour
{
    // سطر اكتمل لحنه (الجمهور يصفق)
    public static System.Action<Vector3> Completed;
    // دو ري مي فا صول لا سي دو ري
    static readonly float[] Scale = { 1f, 1.122f, 1.26f, 1.335f, 1.498f, 1.682f, 1.888f, 2f, 2.245f };
    const float Line0 = 0.55f, Step = 0.24f;

    StaffSpec spec;
    readonly List<Transform> lines = new List<Transform>();
    readonly List<GameObject> hints = new List<GameObject>();
    Transform[] heads;
    bool[] filled;
    bool done, holding, prompted;
    float doneAt = -1f, nextWrong;
    int played;
    BoxCollider bridge;
    Material ink, glow, hintMat;

    // نغمة النوتة: درجتها على السطر، وكل سطر أعلى من اللي قبله
    float NotePitch(int n)
    {
        float p = Scale[Mathf.Clamp(Mathf.RoundToInt(spec.notes[n].y), 0, Scale.Length - 1)];
        return MusicFeatures.Rising ? p * MusicFeatures.RisePitch(spec.from) / 0.75f * 0.8f : p;
    }

    Vector3 Target(int i) => new Vector3(spec.notes[i].x, spec.top + Line0 + spec.notes[i].y * Step, 0f);

    public static void Create(Transform root, StaffSpec spec, GameManager gm)
    {
        var go = new GameObject("Staff");
        go.transform.SetParent(root, false);
        var s = go.AddComponent<StaffBridge>();
        s.spec = spec;
        s.filled = new bool[spec.notes.Length];
        s.heads = new Transform[spec.notes.Length];
        s.ink = gm.Mat("Note Black", new Color(0.12f, 0.1f, 0.15f), 0.6f);
        s.glow = gm.Mat("Staff Glow", new Color(1.4f, 1.1f, 0.4f), 0.6f);
        s.hintMat = gm.Mat("Staff Hint", new Color(0.55f, 0.6f, 0.95f), 0.3f);
        float w = spec.to - spec.from, cx = (spec.from + spec.to) * 0.5f;
        for (int i = 0; i < 5; i++)
            s.lines.Add(LevelBuilder.Visual("Staff Line", PrimitiveType.Cube, go.transform, new Vector3(cx, spec.top + Line0 + i * Step * 2f - 0.55f, 0f), new Vector3(w, 0.04f, 0.04f), s.ink, false).transform);
        // مفتاح صول بسيط على أول السطر
        var clef = LevelBuilder.Visual("Clef", PrimitiveType.Capsule, go.transform, new Vector3(spec.from + 0.35f, spec.top + Line0 + 4f * Step - 0.55f, 0f), new Vector3(0.18f, 0.75f, 0.1f), s.ink, false).transform;
        clef.localRotation = Quaternion.Euler(0f, 0f, -12f);
        LevelBuilder.Visual("Clef Loop", PrimitiveType.Sphere, go.transform, new Vector3(spec.from + 0.42f, spec.top + Line0 + 2f * Step - 0.55f, 0f), new Vector3(0.45f, 0.5f, 0.08f), s.ink, false);
        // أماكن النوتات المنقطة
        for (int n = 0; n < spec.notes.Length; n++)
        {
            var ring = new GameObject("Note Hint");
            ring.transform.SetParent(go.transform, false);
            Vector3 c = s.Target(n) - new Vector3(0f, 0.55f, 0f);
            for (int k = 0; k < 10; k++)
            {
                float a = k / 10f * Mathf.PI * 2f;
                LevelBuilder.Visual("Dot", PrimitiveType.Sphere, ring.transform, c + new Vector3(Mathf.Cos(a) * 0.34f, Mathf.Sin(a) * 0.26f, -0.05f), Vector3.one * 0.07f, s.hintMat, false);
            }
            s.hints.Add(ring);
        }
        var b = new GameObject("Staff Bridge");
        b.transform.SetParent(go.transform, false);
        b.transform.position = new Vector3(cx, spec.top - 0.15f, 0f);
        s.bridge = b.AddComponent<BoxCollider>();
        s.bridge.size = new Vector3(w + 0.3f, 0.3f, 2f);
        s.bridge.enabled = false;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time;

        // نوتة مرسومة؟ إذا جنب مكان منقط تثبت فيه، وإلا تطلع نشاز وتختفي
        if (!done)
            foreach (InkMark m in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
            {
                if (m.beingEaten) continue;
                var r = m.GetComponent<MeshRenderer>();
                if (r == null) continue;
                Vector3 c = r.bounds.center;
                Vector3 sz = r.bounds.size;
                bool overlaps = r.bounds.max.x > spec.from + 0.2f && r.bounds.min.x < spec.to - 0.2f && c.y > spec.top - 1.5f && c.y < spec.top + 3.2f;
                bool small = m.closed || (sz.x < 1.3f && sz.y < 1.3f);
                // خط طويل فوق السطر (يبي يعدي بدون لحن)؟ يختفي
                if (!small && overlaps)
                {
                    Destroy(m.gameObject);
                    LevelSfx.Play("error", 0.7f);
                    if (t >= nextWrong) { nextWrong = t + 2f; gm.UI.ShowMessage("", 2.4f, "السطر يبي نوتات مو خطوط! ارسمي دايرة بكل مكان منقط"); }
                    continue;
                }
                if (c.x < spec.from - 0.5f || c.x > spec.to + 0.5f || c.y < spec.top - 0.5f || c.y > spec.top + 3.2f) continue;
                int best = -1; float bd = 0.8f;
                for (int n = 0; n < filled.Length; n++)
                {
                    if (filled[n]) continue;
                    float d = Vector2.Distance(c, Target(n) - new Vector3(0f, 0.55f, 0f));
                    if (d < bd) { bd = d; best = n; }
                }
                Destroy(m.gameObject);
                if (best >= 0) Fill(best, gm);
                else if (t >= nextWrong)
                {
                    nextWrong = t + 0.3f;
                    LevelSfx.Play("error", 0.7f);
                    Fx.Sparkle(c, new Color(0.5f, 0.3f, 0.6f), 10);
                }
            }

        // كل النوتات تمام: اللحن يعزف كله، وبعدين السطور تضوي وتصير جسر
        if (!done && System.Array.TrueForAll(filled, f => f)) { done = true; doneAt = t; played = 0; }
        if (done)
        {
            float k = t - doneAt;
            while (played < heads.Length && k > 0.3f + played * 0.28f)
            {
                LevelSfx.Play("piano", 0.7f, NotePitch(played));
                Fx.Sparkle(heads[played].position, MusicFeatures.KeyColors[played % 5], 8);
                heads[played].localScale = new Vector3(0.62f, 0.46f, 0.2f) * 1.3f;
                played++;
            }
            for (int i = 0; i < heads.Length; i++) heads[i].localScale = Vector3.Lerp(heads[i].localScale, new Vector3(0.62f, 0.46f, 0.2f), Time.deltaTime * 6f);
            if (played >= heads.Length && !bridge.enabled)
            {
                bridge.enabled = true;
                LevelSfx.Play("chime", 0.8f, 1.5f);
                foreach (Transform l in lines) l.GetComponent<MeshRenderer>().sharedMaterial = glow;
                lines[0].localScale = new Vector3(lines[0].localScale.x, 0.12f, 1.2f);
                lines[0].position = new Vector3(lines[0].position.x, spec.top - 0.05f, 0f);
                StaffBridge.Completed?.Invoke(new Vector3((spec.from + spec.to) * 0.5f, spec.top + 1f, 0f));
            }
        }
        // السطور تتموج شوي (أقوى وهي تعزف)
        for (int i = 0; i < lines.Count; i++)
            if (!(bridge.enabled && i == 0))
                lines[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 2f + i) * (done ? 0.6f : 0.25f));

        bool wait = HoldHelper.Before(spec.from) && !bridge.enabled;
        if (wait && !prompted) { prompted = true; gm.UI.ShowMessage("", 3.2f, "ارسمي نوتة (دايرة) بكل مكان منقط عشان يكتمل اللحن"); }
        HoldHelper.Hold(ref holding, wait);
    }

    void Fill(int n, GameManager gm)
    {
        filled[n] = true;
        Destroy(hints[n]);
        Vector3 p = Target(n) - new Vector3(0f, 0.55f, 0f);
        var head = LevelBuilder.Visual("Note Head", PrimitiveType.Sphere, transform, p, new Vector3(0.62f, 0.46f, 0.2f), ink, false).transform;
        head.localRotation = Quaternion.Euler(0f, 0f, 20f);
        LevelBuilder.Visual("Stem", PrimitiveType.Cube, transform, p + new Vector3(0.28f, 0.6f, 0f), new Vector3(0.06f, 1.2f, 0.06f), ink, false);
        heads[n] = head;
        int step = Mathf.Clamp(Mathf.RoundToInt(spec.notes[n].y), 0, Scale.Length - 1);
        LevelSfx.Play("piano", 0.8f, NotePitch(n));
        Fx.Sparkle(p, MusicFeatures.KeyColors[n % 5], 12);
    }
}
