using System.Collections.Generic;
using UnityEngine;

// لاعب آلي للتجربة فقط (Claude يستخدمه عشان يتأكد إن المراحل ممكنة):
// يمسح الوحوش القريبة، ويرسم جسر لما تقرب رويدا من حفرة
public class DebugAutoPlayer : MonoBehaviour
{
    public static int Deaths, LevelsCompleted;

    LevelData lastLevel;
    int lastHearts = -1;
    float nextErase, nextBridge;

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        // نحسب الطيحات والمراحل المخلوصة
        if (gm.CurrentLevel != lastLevel)
        {
            if (lastLevel != null && gm.LevelIndex > Levels.All.IndexOf(lastLevel)) LevelsCompleted++;
            lastLevel = gm.CurrentLevel;
            lastHearts = gm.Hearts;
        }
        if (gm.Hearts < lastHearts) Deaths++;
        lastHearts = gm.Hearts;

        if (!gm.IsPlaying || gm.Rowaida == null) return;
        InkDrawer drawer = FindAnyObjectByType<InkDrawer>();
        Vector3 r = gm.Rowaida.transform.position;

        // الوحوش: أي وحش قريب قدامها نشخبط عليه
        if (Time.time >= nextErase)
        {
            foreach (ScribbleMonster m in FindObjectsByType<ScribbleMonster>(FindObjectsSortMode.None))
            {
                Vector3 p = m.transform.position;
                float dx = p.x - r.x;
                if (dx > -1f && dx < 7f && p.y > -2f)
                {
                    var zig = new List<Vector3>();
                    for (int k = 0; k <= 8; k++)
                        zig.Add(p + new Vector3(k % 2 == 0 ? -0.35f : 0.35f, 0.3f - 0.6f * k / 8f, 0f));
                    drawer.DebugDraw(zig);
                    nextErase = Time.time + 0.3f;
                    return;
                }
            }
        }

        // الحفر: أول حفرة جاية قدامها
        var blocks = gm.CurrentLevel.blocks;
        for (int i = 0; i < blocks.Count - 1; i++)
        {
            Block a = blocks[i], b = blocks[i + 1];
            float x0 = a.center.x + a.size.x * 0.5f, x1 = b.center.x - b.size.x * 0.5f;
            float top0 = a.center.y + a.size.y * 0.5f, top1 = b.center.y + b.size.y * 0.5f;
            if (r.x > x1) continue;

            float lead = gm.CurrentLevel.Rain ? 1.1f : 2.2f;
            if (x0 - r.x > lead) return;
            if (Time.time < nextBridge || BridgeExists(x0, x1)) return;

            // نستنى لين تنمسح الوحوش اللي جنب الحفرة (عشان ما تاكل الجسر)
            foreach (ScribbleMonster m in FindObjectsByType<ScribbleMonster>(FindObjectsSortMode.None))
                if (m.transform.position.x > x0 - 4f && m.transform.position.x < x1 + 4f && m.transform.position.y > -2f) return;

            float wind = Weather.Wind;
            float start = Mathf.Max(x0 - 0.25f - Mathf.Max(0f, wind) * 0.8f, r.x + 0.75f);
            float end = x1 + 0.45f + Mathf.Max(0f, -wind) * 1.6f;
            if (start > x0 + 0.3f) return;   // متأخر مرة
            drawer.DebugDraw(new List<Vector3> { new Vector3(start, top0 + 0.2f, 0f), new Vector3(end, top1 + 0.2f, 0f) });
            nextBridge = Time.time + 0.4f;
            return;
        }
    }

    // فيه جسر يغطي الحفرة؟
    static bool BridgeExists(float x0, float x1)
    {
        foreach (InkMark mark in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
        {
            if (mark.closed) continue;
            var rend = mark.GetComponent<MeshRenderer>();
            if (rend == null) continue;
            Bounds bounds = rend.bounds;
            if (bounds.min.x <= x0 + 0.3f && bounds.max.x >= x1 - 0.3f) return true;
        }
        return false;
    }
}
