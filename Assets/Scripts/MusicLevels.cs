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
