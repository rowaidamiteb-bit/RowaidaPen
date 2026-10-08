using UnityEngine;

// أرضية طايرة في سما الدفتر: تتحرك يمين ويسار بهدوء.
// بعد ما تعديها رويدا تهتز وتختفي، ولما رويدا تخسر قلب وترجع، الأرضيات اللي قدامها ترجع
public class FloatingPlatform : MonoBehaviour
{
    const float ShakeTime = 0.5f;

    FloaterSpec spec;
    Rigidbody body;
    float clock, shake = -1f;
    bool gone;

    public static Transform Create(Transform parent, FloaterSpec spec)
    {
        var go = new GameObject("Floating Floor");
        go.transform.SetParent(parent, false);
        var p = go.AddComponent<FloatingPlatform>();
        p.spec = spec;
        p.body = go.AddComponent<Rigidbody>();
        p.body.isKinematic = true;
        p.body.interpolation = RigidbodyInterpolation.Interpolate;
        // تبدا عند الصفر عشان الصفحة اللي داخلها تنبني بمكانها الصحيح، وبعدين تتحرك
        GameManager.Instance.Respawned += p.OnRespawned;
        return go.transform;
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.Respawned -= OnRespawned;
    }

    Vector3 PositionAt(float t)
    {
        float x = spec.centerX + spec.amplitude * Mathf.Sin(t * Mathf.PI * 2f / spec.period + spec.phase);
        // تطفو شوي لفوق وتحت عشان تبان إنها طايرة (بسيط عشان ما يعلّق الجسر)
        float y = Mathf.Sin(t * 1.7f + spec.phase) * 0.04f;
        return new Vector3(x - spec.centerX, y, 0f);
    }

    void OnRespawned(Vector3 pos)
    {
        if (spec.centerX - spec.width * 0.5f < pos.x) return;
        var es = GetComponent<ErasedSegment>();
        if (es != null) DestroyImmediate(es);
        gone = false;
        shake = -1f;
        transform.localScale = Vector3.one;
        transform.position = PositionAt(clock);
        gameObject.SetActive(true);
        foreach (Collider c in GetComponentsInChildren<Collider>()) c.enabled = true;
    }

    void FixedUpdate()
    {
        if (gone) return;
        clock += Time.fixedDeltaTime;
        Vector3 p = PositionAt(clock);
        if (shake >= 0f) p += new Vector3(Mathf.Sin(shake * 60f) * 0.05f, Mathf.Sin(shake * 47f) * 0.03f, 0f);
        body.MovePosition(p);
    }

    void Update()
    {
        if (gone) return;
        GameManager gm = GameManager.Instance;
        RowaidaController rowaida = gm.Rowaida;
        if (rowaida == null) return;

        if (shake < 0f)
        {
            // رويدا عدّتها وهي واقفة على شي ثاني: تبدا تهتز
            Vector3 rp = rowaida.transform.position;
            float right = spec.centerX + transform.position.x + spec.width * 0.5f;
            if (gm.IsPlaying && rp.x > right + 1.8f && rp.y > -0.5f)
            {
                shake = 0f;
                gm.Sound.PlaySqueak(0.7f);
            }
            return;
        }

        shake += Time.deltaTime;
        if (shake >= ShakeTime)
        {
            gone = true;
            gameObject.AddComponent<ErasedSegment>().keep = true;
        }
    }
}
