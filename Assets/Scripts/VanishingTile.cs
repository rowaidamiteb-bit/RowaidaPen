using UnityEngine;

// قطعة أرض تهتز وتختفي لما تقرب منها رويدا (الصفحة المتلاشية).
// لما رويدا تخسر قلب وترجع، القطع اللي قدامها ترجع مكانها
public class VanishingTile : MonoBehaviour
{
    const float ShakeTime = 0.55f;

    float fromX, trigger, shake = -1f;
    bool gone;
    Vector3 home, homeScale;

    public static void Attach(GameObject segment, float fromX, float trigger)
    {
        var v = segment.AddComponent<VanishingTile>();
        v.fromX = fromX;
        v.trigger = trigger;
        v.home = segment.transform.position;
        v.homeScale = segment.transform.localScale;
        GameManager.Instance.Respawned += v.OnRespawned;
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.Respawned -= OnRespawned;
    }

    void OnRespawned(Vector3 pos)
    {
        if (fromX < pos.x) return;
        var es = GetComponent<ErasedSegment>();
        if (es != null) DestroyImmediate(es);
        gone = false;
        shake = -1f;
        transform.position = home;
        transform.localScale = homeScale;
        transform.rotation = Quaternion.identity;
        gameObject.SetActive(true);
        foreach (Collider c in GetComponentsInChildren<Collider>()) c.enabled = true;
    }

    void Update()
    {
        if (gone) return;
        GameManager gm = GameManager.Instance;
        RowaidaController rowaida = gm.Rowaida;
        if (rowaida == null) return;

        if (shake < 0f)
        {
            if (gm.IsPlaying && rowaida.transform.position.x > fromX - trigger)
            {
                shake = 0f;
                gm.Sound.PlaySqueak(0.7f);
            }
            return;
        }

        // تهتز شوي وبعدين تختفي
        shake += Time.deltaTime;
        float k = shake / ShakeTime;
        transform.position = home + new Vector3(Mathf.Sin(shake * 60f) * 0.05f * k, Mathf.Sin(shake * 47f) * 0.03f * k, 0f);
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(shake * 53f) * 1.2f * k);
        if (shake >= ShakeTime)
        {
            gone = true;
            transform.position = home;
            transform.rotation = Quaternion.identity;
            gameObject.AddComponent<ErasedSegment>().keep = true;
        }
    }
}
