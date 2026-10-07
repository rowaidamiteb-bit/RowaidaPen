using System.Collections.Generic;
using UnityEngine;

// ألوان الحبر (صفحة الألوان): كل لون له قوة
//  الأحمر: ينطط رويدا لفوق
//  الأزرق: يزلّقها ويسرّعها (وتطير لو كان طريق مايل)
//  الأخضر: الخط يطول لحاله لين يوصل لشي
public enum InkColor { Black, Red, Blue, Green }

public class InkColorEffect : MonoBehaviour
{
    public InkColor color;

    // للأخضر: الخط يكبر من آخره
    List<Vector3> pts;
    Mesh mesh;
    float radius;
    float grown, nextColliders;
    bool growing;
    Vector3 growDir;
    const float GrowSpeed = 2.2f, MaxGrow = 7f, Step = 0.12f;

    // للأحمر: يتمطط لما تنط عليه
    float squash;

    public static Color ColorOf(InkColor c)
    {
        switch (c)
        {
            case InkColor.Red: return new Color(0.92f, 0.25f, 0.32f);
            case InkColor.Blue: return new Color(0.25f, 0.55f, 1f);
            case InkColor.Green: return new Color(0.3f, 0.8f, 0.38f);
            default: return new Color(0.21f, 0.21f, 0.23f);
        }
    }

    public void StartGrowing(Mesh strokeMesh, List<Vector3> points, float tubeRadius)
    {
        if (points.Count < 2) return;
        mesh = strokeMesh;
        pts = new List<Vector3>(points);
        radius = tubeRadius;
        Vector3 d = pts[pts.Count - 1] - pts[Mathf.Max(0, pts.Count - 4)];
        growDir = d.sqrMagnitude > 1e-4f ? d.normalized : Vector3.right;
        growDir.z = 0f;
        growing = true;
    }

    void Update()
    {
        if (growing) Grow();
        if (squash > 0f)
        {
            squash = Mathf.Max(0f, squash - Time.deltaTime * 4f);
            transform.localScale = new Vector3(1f, 1f - Mathf.Sin(squash * Mathf.PI) * 0.15f, 1f);
        }
    }

    void Grow()
    {
        float add = GrowSpeed * Time.deltaTime;
        Vector3 tip = pts[pts.Count - 1];
        bool stop = false;
        while (add > 0f && !stop)
        {
            float step = Mathf.Min(Step, add);
            Vector3 next = tip + growDir * step;
            // يوقف لما يوصل لصفحة أو لرويدا
            foreach (Collider c in Physics.OverlapSphere(next, radius * 0.9f))
            {
                if (c.isTrigger || c.transform.IsChildOf(transform)) continue;
                stop = true;
                break;
            }
            if (stop) break;
            pts.Add(next);
            tip = next;
            add -= step;
            grown += step;
            if (grown >= MaxGrow) stop = true;
        }
        TubeMesh.Build(mesh, pts, radius, false);
        if (stop || Time.time >= nextColliders)
        {
            nextColliders = Time.time + 0.15f;
            foreach (Transform child in transform)
                if (child.name == "Collider") Destroy(child.gameObject);
            InkDrawer.BuildColliders(transform, pts, false, radius);
        }
        if (stop)
        {
            growing = false;
            GameManager.Instance.Sound.PlayPoof();
        }
    }

    // رويدا لمست الخط
    public void OnRowaida(RowaidaController rowaida)
    {
        switch (color)
        {
            case InkColor.Red:
                if (rowaida.Bounce(8.5f))
                {
                    squash = 1f;
                    GameManager.Instance.Sound.PlayBoing();
                }
                break;
            case InkColor.Blue:
                rowaida.SetBoost(true);
                break;
        }
    }
}
