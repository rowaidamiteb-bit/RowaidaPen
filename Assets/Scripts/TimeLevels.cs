using System.Collections.Generic;
using UnityEngine;

// الجزء الثاني — فصل الوقت: ساعة، ساعة رملية، تروس، بندول، وليل ونهار
public static class TimeFeatures
{
    public static void Build(Transform root, LevelData data, GameManager gm, System.Random rng)
    {
        foreach (Vector3 c in data.clocks) ClockBridge.Create(root, c, gm);
    }

    // نص بالأرقام (TextMesh) يواجه الكاميرا
    public static TextMesh Label(Transform parent, string text, Vector3 pos, float size, Color color)
    {
        var go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        var t = go.AddComponent<TextMesh>();
        t.text = text;
        t.font = ArabicText.Font;
        t.fontSize = 80;
        t.characterSize = size * 0.1f;
        t.anchor = TextAnchor.MiddleCenter;
        t.alignment = TextAlignment.Center;
        t.color = color;
        go.GetComponent<MeshRenderer>().sharedMaterial = ArabicText.Font.material;
        return t;
    }
}

// ساعة كبيرة على حفرة: عقرب الدقايق هو الجسر بس يلف بدون ما يوقف.
// ارسمي خط صغير تحت المكان اللي يصير فيه العقرب مستوي: العقرب يوقف عليه ورويدا تمشي فوقه
public class ClockBridge : MonoBehaviour
{
    float from, to, speed, angle;   // الزاوية مع عقارب الساعة من الساعة 12
    bool jammed, holding;
    int prompted = -1;
    Transform hand;
    BoxCollider handCol;
    float Len => to - from + 0.2f;

    public static void Create(Transform root, Vector3 spec, GameManager gm)
    {
        var go = new GameObject("Clock Bridge");
        go.transform.SetParent(root, false);
        var c = go.AddComponent<ClockBridge>();
        c.from = spec.x; c.to = spec.y; c.speed = spec.z;
        c.angle = 200f;
        Vector3 pivot = new Vector3(c.from - 0.2f, -0.15f, 0f);
        float r = Mathf.Max(2.4f, c.Len * 0.55f);   // وجه الساعة أصغر من العقرب (شكل كرتوني)

        // وجه الساعة ورا (دايرة كريمية، شرطات، وأرقام)
        Material face = gm.Mat("Clock Face", new Color(1f, 0.97f, 0.88f), 0.3f);
        Material rim = gm.Mat("Clock Rim", new Color(0.55f, 0.35f, 0.25f), 0.4f);
        Material ink = gm.Mat("Clock Ink", new Color(0.15f, 0.13f, 0.12f), 0.3f);
        var faceT = LevelBuilder.Visual("Face", PrimitiveType.Cylinder, go.transform, pivot + new Vector3(0f, 0f, 2f), new Vector3(r * 2f, 0.05f, r * 2f), face, false).transform;
        faceT.rotation = Quaternion.Euler(90f, 0f, 0f);
        var rimT = LevelBuilder.Visual("Rim", PrimitiveType.Cylinder, go.transform, pivot + new Vector3(0f, 0f, 2.05f), new Vector3(r * 2f + 0.4f, 0.04f, r * 2f + 0.4f), rim, false).transform;
        rimT.rotation = Quaternion.Euler(90f, 0f, 0f);
        for (int i = 0; i < 12; i++)
        {
            float a = i * 30f * Mathf.Deg2Rad;
            var tick = LevelBuilder.Visual("Tick", PrimitiveType.Cube, go.transform, pivot + new Vector3(Mathf.Sin(a), Mathf.Cos(a), 0f) * (r - 0.35f) + new Vector3(0f, 0f, 1.9f), new Vector3(0.12f, i % 3 == 0 ? 0.5f : 0.3f, 0.05f), ink, false).transform;
            tick.rotation = Quaternion.Euler(0f, 0f, -i * 30f);
            if (i % 3 == 0)
                TimeFeatures.Label(go.transform, i == 0 ? "12" : (i).ToString(), pivot + new Vector3(Mathf.Sin(a), Mathf.Cos(a), 0f) * (r - 0.85f) + new Vector3(0f, 0f, 1.85f), 0.8f, ink.color);
        }
        // عقرب الساعات (زينة) يأشر على 3
        var hour = LevelBuilder.Visual("Hour Hand", PrimitiveType.Cube, go.transform, pivot + new Vector3(r * 0.25f, 0f, 1.7f), new Vector3(r * 0.5f, 0.35f, 0.08f), ink, false).transform;
        hour.rotation = Quaternion.Euler(0f, 0f, 60f);
        hour.position = pivot + hour.rotation * new Vector3(r * 0.25f, 0f, 0f) + new Vector3(0f, 0f, 1.7f);

        // عقرب الدقايق: هو الجسر (له جسم يصطدم بس لما يوقف)
        c.hand = new GameObject("Minute Hand").transform;
        c.hand.SetParent(go.transform, false);
        c.hand.position = pivot;
        Material handMat = gm.Mat("Clock Hand", new Color(0.2f, 0.18f, 0.25f), 0.4f);
        LevelBuilder.Visual("Hand", PrimitiveType.Cube, c.hand, new Vector3(c.Len * 0.5f, 0f, 0.6f), new Vector3(c.Len, 0.3f, 0.5f), handMat);
        var tip = LevelBuilder.Visual("Tip", PrimitiveType.Cube, c.hand, new Vector3(c.Len - 0.1f, 0f, 0.6f), new Vector3(0.6f, 0.6f, 0.5f), handMat).transform;
        tip.localRotation = Quaternion.Euler(0f, 0f, 45f);
        LevelBuilder.Visual("Center", PrimitiveType.Cylinder, c.hand, new Vector3(0f, 0f, 0.3f), new Vector3(0.6f, 0.1f, 0.6f), rim, false).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        c.handCol = c.hand.gameObject.AddComponent<BoxCollider>();
        c.handCol.center = new Vector3(c.Len * 0.5f, 0f, 0f);
        c.handCol.size = new Vector3(c.Len, 0.3f, 2f);
        c.handCol.enabled = false;
        c.Apply();
    }

    // عقارب الساعة: 0 = فوق، 90 = يمين (مستوي = جسر)
    void Apply() => hand.localRotation = Quaternion.Euler(0f, 0f, 90f - angle);

    // فيه خط تحت مكان العقرب لما يصير مستوي؟
    bool Stopper()
    {
        foreach (InkMark mark in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
        {
            var r = mark.GetComponent<MeshRenderer>();
            if (r == null) continue;
            Bounds b = r.bounds;
            if (b.max.x > from + 0.8f && b.min.x < to && b.max.y < 0.1f && b.max.y > -1.6f) return true;
        }
        return false;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        if (!jammed)
        {
            float next = angle + speed * Time.deltaTime;
            // وصل للمستوي وفيه خط يوقفه؟
            if (Mathf.Repeat(angle, 360f) < 90f && Mathf.Repeat(next, 360f) >= 90f && Stopper())
            {
                angle = 90f;
                jammed = true;
                handCol.enabled = true;
                gm.Sound.PlayBoing();
                Fx.Sparkle(new Vector3(to - 0.5f, 0f, 0f), new Color(1f, 0.85f, 0.4f), 18);
                gm.UI.ShowMessage("", 1.8f, "وقف العقرب!");
            }
            else angle = Mathf.Repeat(next, 360f);
            Apply();
        }

        RowaidaController rw = gm.Rowaida;
        if (rw == null) return;
        Vector3 rp = rw.transform.position;
        bool atEdge = rp.x > from - 1.6f && rp.x < from - 0.4f && rp.y > 0.3f;
        bool wait = atEdge && !jammed && gm.IsPlaying;
        if (wait && prompted != 0)
        {
            prompted = 0;
            gm.UI.ShowMessage("", 2.8f, "العقرب يلف! ارسمي خط تحت مكانه لما يصير مستوي يوقفه");
        }
        if (wait) { rw.Held = true; rw.CarryX = 0f; holding = true; CameraFollow.PullRequest = Mathf.Max(CameraFollow.PullRequest, 1f); }
        else if (holding) { rw.Held = false; holding = false; }
    }
}
