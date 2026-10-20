using UnityEngine;

// نهاية الجزء الأول: ممحاة عملاقة تلحق رويدا وتمسح كل الرسم اللي وراها.
// تندفع لين تقرب، وبعدين تمشي أبطأ من رويدا بشوي (أي وقفة أو غلطة = تمسكها)
public class GiantEraser : MonoBehaviour
{
    const float RushSpeed = 4.4f, CloseSpeed = 2.6f, WaitSpeed = 1.3f;


    float endX, delay = 2.4f, nextRumble, nextCrumb, nextShake;
    Transform body;
    Material crumbMat;

    float Front => transform.position.x + 1.6f;

    public static void Create(Transform root, float startX, float endX, GameManager gm)
    {
        var go = new GameObject("Giant Eraser");
        go.transform.SetParent(root, false);
        go.transform.position = new Vector3(startX, 0f, 0f);
        var e = go.AddComponent<GiantEraser>();
        e.endX = endX;
        e.Build(gm);
        gm.Respawned += e.OnRespawned;
    }

    void Build(GameManager gm)
    {
        Material rubber = gm.Mat("Eraser Rubber", new Color(1f, 0.6f, 0.66f), 0.25f);
        Material worn = gm.Mat("Eraser Worn", new Color(0.92f, 0.5f, 0.56f), 0.2f);
        Material sleeve = gm.Mat("Eraser Sleeve", new Color(0.28f, 0.48f, 0.9f), 0.35f);
        Material stripe = gm.Mat("Eraser Stripe", new Color(0.97f, 0.96f, 0.92f), 0.3f);
        Material white = gm.Mat("Monster Eye", Color.white, 0.7f);
        Material black = gm.Mat("Monster Pupil", new Color(0.05f, 0.04f, 0.06f), 0.9f);
        crumbMat = gm.Mat("Eraser Crumb", new Color(0.95f, 0.55f, 0.62f), 0.2f);

        body = new GameObject("Body").transform;
        body.SetParent(transform, false);
        body.localPosition = new Vector3(0f, -1.2f, 0f);   // نلفها من تحت (تنحني لقدام وهي تمسح)
        LevelBuilder.Visual("Rubber", PrimitiveType.Cube, body, new Vector3(0f, 2.6f, 0f), new Vector3(3.2f, 5.2f, 3.2f), rubber);
        LevelBuilder.Visual("Worn Edge", PrimitiveType.Cube, body, new Vector3(0.9f, 0.35f, 0f), new Vector3(1.5f, 0.7f, 3.25f), worn);
        LevelBuilder.Visual("Sleeve", PrimitiveType.Cube, body, new Vector3(0f, 4f, 0f), new Vector3(3.3f, 2.6f, 3.3f), sleeve);
        LevelBuilder.Visual("Stripe", PrimitiveType.Cube, body, new Vector3(0f, 4.3f, 0f), new Vector3(3.34f, 0.35f, 3.34f), stripe);
        // وجه زعلان على الغلاف
        for (int s = -1; s <= 1; s += 2)
        {
            LevelBuilder.Visual("Eye", PrimitiveType.Sphere, body, new Vector3(0.75f + s * 0.45f, 3.65f, -1.68f), new Vector3(0.5f, 0.55f, 0.1f), white, false);
            LevelBuilder.Visual("Pupil", PrimitiveType.Sphere, body, new Vector3(0.88f + s * 0.45f, 3.6f, -1.73f), new Vector3(0.22f, 0.26f, 0.06f), black, false);
            var brow = LevelBuilder.Visual("Brow", PrimitiveType.Cube, body, new Vector3(0.75f + s * 0.45f, 4.05f, -1.72f), new Vector3(0.5f, 0.1f, 0.05f), black, false);
            brow.transform.localRotation = Quaternion.Euler(0f, 0f, s * 18f);
        }
        LevelBuilder.Visual("Mouth", PrimitiveType.Cube, body, new Vector3(0.8f, 3.05f, -1.68f), new Vector3(0.7f, 0.12f, 0.05f), black, false);
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.Respawned -= OnRespawned;
    }

    // رويدا خسرت قلب: ترجع الممحاة بعيد وراها
    void OnRespawned(Vector3 pos)
    {
        transform.position = new Vector3(pos.x - 12f, 0f, 0f);
        delay = 2.5f;
    }

    float CurrentSpeed(RowaidaController rowaida)
    {
        if (rowaida == null) return RushSpeed;
        float gap = rowaida.transform.position.x - Front;
        if (rowaida.Held) return gap < 6f ? WaitSpeed : RushSpeed;
        return gap < 3.5f ? CloseSpeed : RushSpeed;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time;

        // تفرك الورق: تتمايل لقدام ولورا
        body.localRotation = Quaternion.Euler(0f, 0f, -6f + Mathf.Sin(t * 14f) * 4f);
        body.localPosition = new Vector3(Mathf.Sin(t * 14f) * 0.12f, -1.2f, 0f);

        if (!gm.IsPlaying) return;
        if (delay > 0f) { delay -= Time.deltaTime; return; }

        RowaidaController rowaida = gm.Rowaida;
        if (Front < endX) transform.position += new Vector3(CurrentSpeed(rowaida) * Time.deltaTime, 0f, 0f);

        // فتات ممحاة يطير
        if (t >= nextCrumb)
        {
            nextCrumb = t + 0.04f;
            var c = LevelBuilder.Visual("Crumb", PrimitiveType.Cube, null, new Vector3(Front - Random.Range(0f, 0.6f), Random.Range(0f, 0.4f), Random.Range(-1.6f, 1.6f)), Vector3.one * Random.Range(0.08f, 0.2f), crumbMat, false);
            c.transform.rotation = Random.rotation;
            var p = c.AddComponent<FxParticle>();
            p.velocity = new Vector3(Random.Range(-2.5f, 0.5f), Random.Range(1f, 3.5f), 0f);
            p.gravity = 9f;
            p.spin = Random.insideUnitSphere * 400f;
            p.life = 0.8f;
        }

        // تمسح الحبر اللي تعدّيه
        foreach (InkMark mark in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
        {
            var r = mark.GetComponent<MeshRenderer>();
            if (r != null && r.bounds.min.x < Front - 0.2f)
            {
                Fx.Graphite(r.bounds.center);
                Destroy(mark.gameObject);
            }
        }

        if (rowaida == null) return;
        float gap = rowaida.transform.position.x - Front;
        if (gap < 9f) CameraFollow.ShiftRequest = -Mathf.Lerp(3.5f, 1f, gap / 9f);
        if (t >= nextRumble && gap < 12f)
        {
            nextRumble = t + 0.9f;
            gm.Sound.PlayRumble(Mathf.Lerp(0.5f, 0.12f, gap / 12f));
        }
        if (t >= nextShake && gap < 5f)
        {
            nextShake = t + 0.25f;
            var cam = Camera.main.GetComponent<CameraFollow>();
            if (cam != null) cam.Shake(Mathf.Lerp(0.08f, 0.02f, gap / 5f), 0.3f);
        }

        // مسكتها
        if (gap < 0.2f && rowaida.transform.position.y > -3f)
        {
            Fx.Sparkle(rowaida.transform.position, new Color(1f, 0.6f, 0.66f), 18);
            gm.KillRowaida();
        }
    }
}
