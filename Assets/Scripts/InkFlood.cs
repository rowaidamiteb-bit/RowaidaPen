using UnityEngine;

// حبر منسكب يرتفع شوي شوي: لو لمس رويدا تخسر قلب.
// الأشكال المغلقة (قوارب) تطفو عليه وترتفع معه
public class InkFlood : MonoBehaviour
{
    float level, speed, maxLevel;
    float inkTouch;
    public float Level => level;
    Transform body, shine;
    GameManager gm;
    const float Depth = 40f;

    public static InkFlood Spawn(Transform parent, LevelData data, GameManager manager)
    {
        var go = new GameObject("Spilled Ink");
        go.transform.SetParent(parent, false);
        var f = go.AddComponent<InkFlood>();
        f.gm = manager;
        f.level = data.floodStart;
        f.speed = data.floodSpeed;
        f.maxLevel = data.floodMax;

        Material ink = manager.Mat("Spilled Ink", new Color(0.03f, 0.025f, 0.07f), 0.85f, 0f);
        f.body = LevelBuilder.Visual("Ink Body", PrimitiveType.Cube, go.transform, Vector3.zero, new Vector3(160f, Depth, 14f), ink, false).transform;
        // لمعة على سطح الحبر
        Material sheen = manager.Mat("Ink Sheen", new Color(0.09f, 0.08f, 0.2f), 0.97f, 0f);
        f.shine = LevelBuilder.Visual("Ink Shine", PrimitiveType.Cube, go.transform, Vector3.zero, new Vector3(160f, 0.03f, 14f), sheen, false).transform;
        f.Place();

        manager.Respawned += f.OnRespawn;
        return f;
    }

    void OnDestroy()
    {
        if (gm != null) gm.Respawned -= OnRespawn;
    }

    // لما ترجع رويدا بعد ما تخسر قلب: الحبر ينزل شوي عشان تلحق
    void OnRespawn(Vector3 pos)
    {
        level = Mathf.Min(level, pos.y - 3f);
        Place();
    }

    void Place()
    {
        float wave = Mathf.Sin(Time.time * 1.7f) * 0.03f;
        body.position = new Vector3(20f, level - Depth * 0.5f + wave, 4f);
        shine.position = new Vector3(20f, level + 0.01f + wave, 4f);
    }

    void Update()
    {
        if (!gm.IsPlaying) return;
        level = Mathf.Min(maxLevel, level + speed * Time.deltaTime);
        Place();

        // الحبر وصل لنص رويدا لفترة قصيرة؟ (نسمح بغطسة خفيفة لما تنط على القارب)
        var r = gm.Rowaida;
        if (r == null) return;
        // نحسب إنها غرقت لو الحبر وصل لنص جسمها (مو بس رجولها)
        float waist = r.transform.position.y - 0.2f;
        inkTouch = waist < level ? inkTouch + Time.deltaTime : 0f;
        if (inkTouch > 0.3f || r.transform.position.y + 0.3f < level)
        {
            inkTouch = 0f;
            gm.KillRowaida();
        }
    }

    void FixedUpdate()
    {
        // القوارب (الأشكال المغلقة) تطفو
        foreach (InkMark mark in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
        {
            if (!mark.closed) continue;
            var rb = mark.GetComponent<Rigidbody>();
            var rend = mark.GetComponent<MeshRenderer>();
            if (rb == null || rend == null || rb.isKinematic) continue;
            Bounds b = rend.bounds;
            float submerged = Mathf.Clamp01((level - b.min.y) / Mathf.Max(0.1f, b.size.y));
            if (submerged <= 0f) continue;
            rb.AddForce(Vector3.up * submerged * 3f * 9.81f, ForceMode.Acceleration);
            // الحبر ثقيل: يهدّي الحركة والدوران
            rb.linearVelocity *= 1f - 2.5f * submerged * Time.fixedDeltaTime;
            rb.angularVelocity *= 1f - 4f * submerged * Time.fixedDeltaTime;
            // القارب ما ينقلب لما تمشي عليه رويدا
            if (submerged > 0.05f)
            {
                rb.angularVelocity = Vector3.zero;
                rb.constraints |= RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezePositionX;
            }
        }
    }
}
