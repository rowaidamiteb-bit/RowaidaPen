using UnityEngine;

// كاميرا جانبية تتابع رويدا، بزاوية خفيفة عشان يبان العمق الثلاثي الأبعاد
public class CameraFollow : MonoBehaviour
{
    public Vector3 offset = new Vector3(0f, 3.5f, -14f);
    public Vector3 lookOffset = new Vector3(2.5f, 0.5f, 0f);
    public float smoothTime = 0.35f;
    public float minY = -3f;   // ما تنزل الكاميرا أكثر من كذا لما تطيح رويدا

    Transform target;
    Vector3 focus;
    Vector3 velocity;
    float shake, shakeTime;

    // شي كبير قريب (مثل التنين) يطلب الكاميرا ترجع لورا وتطلع فوق شوي عشان يبان
    public static float PullRequest;
    // وشي يلحقها من ورا (مثل الممحاة) يطلب الكاميرا تطالع لورا شوي
    public static float ShiftRequest;
    float shift, shiftVel;
    float pull, pullVel;

    // عقبة تطلب من الكاميرا تبين منطقة كاملة (رويدا والعقبة وكل مكان لازم ترسمين فيه)، تنطلب كل فريم
    static bool frameAsked;
    static Vector2 frameMin, frameMax;
    float frameK, frameKVel;
    Vector3 frameCenter, frameCenterVel;
    float frameDist, frameDistVel;

    public static void Frame(params Vector3[] pts)
    {
        foreach (Vector3 p in pts)
        {
            if (!frameAsked) { frameMin = frameMax = p; frameAsked = true; }
            frameMin = Vector2.Min(frameMin, p);
            frameMax = Vector2.Max(frameMax, p);
        }
    }

    public void SetTarget(Transform newTarget, bool snap)
    {
        target = newTarget;
        if (target == null) return;
        if (snap)
        {
            focus = Focus();
            velocity = Vector3.zero;
            Apply();
        }
    }

    // هزة للكاميرا (قوتها بالمتر ومدتها بالثواني)
    public void Shake(float strength, float duration)
    {
        if (strength < shake * shakeTime) return;
        shake = strength;
        shakeTime = duration;
    }

    void LateUpdate()
    {
        if (target == null) return;
        focus = Vector3.SmoothDamp(focus, Focus(), ref velocity, smoothTime);
        pull = Mathf.SmoothDamp(pull, PullRequest, ref pullVel, 0.6f);
        PullRequest = 0f;
        shift = Mathf.SmoothDamp(shift, ShiftRequest, ref shiftVel, 0.8f);
        ShiftRequest = 0f;
        // نحسب وين تروح الكاميرا عشان المنطقة المطلوبة كلها تبان بالشاشة
        if (frameAsked)
        {
            var cam = GetComponent<Camera>();
            float tanV = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad), tanH = tanV * cam.aspect;
            Vector3 c = new Vector3((frameMin.x + frameMax.x) * 0.5f, (frameMin.y + frameMax.y) * 0.5f, 0f);
            float need = Mathf.Max((frameMax.y - frameMin.y) * 0.5f + 1.6f, 0f) / tanV;
            need = Mathf.Max(need, ((frameMax.x - frameMin.x) * 0.5f + 1.6f) / tanH);
            need = Mathf.Max(need, offset.magnitude);
            if (frameK < 0.01f) { frameCenter = c; frameDist = need; }
            frameCenter = Vector3.SmoothDamp(frameCenter, c, ref frameCenterVel, 0.5f);
            frameDist = Mathf.SmoothDamp(frameDist, need, ref frameDistVel, 0.5f);
        }
        frameK = Mathf.SmoothDamp(frameK, frameAsked ? 1f : 0f, ref frameKVel, 0.6f);
        frameAsked = false;
        Apply();
    }

    Vector3 Focus()
    {
        Vector3 p = target.position;
        p.y = Mathf.Max(p.y, minY);
        p.z = 0f;
        return p;
    }

    void Apply()
    {
        Vector3 jitter = Vector3.zero;
        if (shakeTime > 0f)
        {
            shakeTime -= Time.unscaledDeltaTime;
            float t = Time.unscaledTime * 38f;
            float k = shake * Mathf.Clamp01(shakeTime * 4f);
            jitter = new Vector3(Mathf.PerlinNoise(t, 0.3f) - 0.5f, Mathf.PerlinNoise(0.7f, t) - 0.5f, 0f) * 2f * k;
        }
        Vector3 back = new Vector3(shift, 0f, 0f);
        Vector3 pos = focus + back + offset + new Vector3(0f, 1.6f, -5f) * pull;
        Vector3 look = focus + back + lookOffset + new Vector3(1f, 1.8f, 0f) * pull;
        if (frameK > 0.001f)
        {
            // نفس ميلة الكاميرا العادية، بس ترجع لورا لين المنطقة كلها تبان
            Vector3 dir = (lookOffset - offset).normalized;
            pos = Vector3.Lerp(pos, frameCenter - dir * frameDist, frameK);
            look = Vector3.Lerp(look, frameCenter, frameK);
        }
        transform.position = pos + jitter;
        transform.LookAt(look + jitter * 0.5f);
    }
}
