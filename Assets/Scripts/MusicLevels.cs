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
    public static readonly float[] KeyPitch = { 1f, 1.12f, 1.26f, 1.33f, 1.5f };

    public static void Build(Transform root, LevelData data, GameManager gm, System.Random rng)
    {
        foreach (PianoSpec p in data.pianos) PianoPuzzle.Create(root, p, gm);
        foreach (Vector3 d in data.drumPits) DrumPit.Create(root, d, gm);
        foreach (NoteSpec n in data.noteSections) NoteSection.Create(root, n, gm);
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
        gm.Sound.PlaySqueak(MusicFeatures.KeyPitch[key]);
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
                gm.Sound.PlayBoing();
                Fx.Confetti(new Vector3(spec.gateX, 2f, -0.5f), 30);
                gm.UI.ShowMessage("", 1.8f, "لحن حلو! انفتح الباب");
            }
        }
        else
        {
            progress = spec.song[0] == key ? 1 : 0;
            flash = 0.6f;
            gm.Sound.PlayPoof();
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
        gm.Sound.PlayBoing();
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
                gm.Sound.PlayBoing();
                gm.Sound.PlayRumble(0.3f);
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
                gm.Sound.PlaySqueak(MusicFeatures.KeyPitch[i % MusicFeatures.KeyPitch.Length] * 1.2f);
                Fx.Sparkle(n.position, MusicFeatures.KeyColors[i % 5], 18);
                // النوتة تنحط على الباب
                icons[Collected() - 1].GetComponent<Renderer>().sharedMaterial = n.GetComponentInChildren<Renderer>().sharedMaterial;
            }
        }
        if (openT < 0f && Collected() == notes.Count)
        {
            openT = 0f;
            doorCol.enabled = false;
            gm.Sound.PlayBoing();
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
            gm.Sound.PlayPoof();
            r.Respawn(new Vector3(spec.startX - 1f, 1f, 0f));
            started = false;
            Camera.main.GetComponent<CameraFollow>().SetTarget(r.transform, true);
            gm.UI.ShowMessage("", 2.4f, "فاتتك نوتة! ارسمي طريق يوصلك لها");
        }
    }
}
