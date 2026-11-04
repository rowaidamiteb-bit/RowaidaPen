using System.Collections.Generic;
using UnityEngine;

// أنبوب ناعم يمر على نقاط تتحرك (مثل أذرع الأخطبوط): الكور القديمة تصير نقاط مخفية،
// وكل إطار نبني جلد ناعم عليها (أعرض عند البداية وينحف للطرف)
public class TubeFollower : MonoBehaviour
{
    public Transform[] points;
    public float[] radii;
    const int Seg = 12, Sub = 3;
    Mesh mesh;
    readonly List<Vector3> verts = new List<Vector3>();
    readonly List<int> tris = new List<int>();
    readonly List<Vector3> pts = new List<Vector3>();
    readonly List<float> rs = new List<float>();

    // يخفي رسم الكور ويحط الجلد عليها (الأولاد مثل المصاصات والعيون يظلون)
    public static TubeFollower Attach(Transform parent, IList<Transform> spheres, Material mat, float radiusScale = 0.5f)
    {
        var go = new GameObject("Smooth Skin");
        go.transform.SetParent(parent, false);
        var f = go.AddComponent<TubeFollower>();
        f.points = new Transform[spheres.Count];
        f.radii = new float[spheres.Count];
        for (int i = 0; i < spheres.Count; i++)
        {
            f.points[i] = spheres[i];
            f.radii[i] = spheres[i].localScale.x * radiusScale;
            var r = spheres[i].GetComponent<MeshRenderer>();
            if (r != null) r.enabled = false;
        }
        f.mesh = new Mesh { name = "Smooth Skin" };
        f.mesh.MarkDynamic();
        go.AddComponent<MeshFilter>().sharedMesh = f.mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        return f;
    }

    void LateUpdate()
    {
        pts.Clear(); rs.Clear();
        for (int i = 0; i < points.Length; i++)
            if (points[i] != null && points[i].gameObject.activeInHierarchy)
            {
                pts.Add(transform.InverseTransformPoint(points[i].position));
                rs.Add(radii[i] * points[i].lossyScale.x / Mathf.Max(0.0001f, points[i].localScale.x));
            }
        verts.Clear(); tris.Clear();
        if (pts.Count >= 2)
        {
            // نعومة بين النقاط (كاتمل رووم)
            var p = new List<Vector3>();
            var r = new List<float>();
            for (int i = 0; i < pts.Count - 1; i++)
                for (int k = 0; k < Sub; k++)
                {
                    float t = k / (float)Sub;
                    Vector3 a = pts[Mathf.Max(0, i - 1)], b = pts[i], c = pts[i + 1], d = pts[Mathf.Min(pts.Count - 1, i + 2)];
                    p.Add(0.5f * (2f * b + (-a + c) * t + (2f * a - 5f * b + 4f * c - d) * t * t + (-a + 3f * b - 3f * c + d) * t * t * t));
                    r.Add(Mathf.Lerp(rs[i], rs[i + 1], t));
                }
            p.Add(pts[pts.Count - 1]); r.Add(rs[rs.Count - 1] * 0.6f);
            Vector3 side = Vector3.forward;
            for (int i = 0; i < p.Count; i++)
            {
                Vector3 fwd = (p[Mathf.Min(p.Count - 1, i + 1)] - p[Mathf.Max(0, i - 1)]);
                if (fwd.sqrMagnitude < 1e-6f) fwd = Vector3.up;
                fwd.Normalize();
                side = Vector3.ProjectOnPlane(side, fwd);
                if (side.sqrMagnitude < 1e-4f) side = Vector3.Cross(fwd, Vector3.right);
                side.Normalize();
                Vector3 n2 = Vector3.Cross(side, fwd);
                for (int j = 0; j < Seg; j++)
                {
                    float a = j * Mathf.PI * 2f / Seg;
                    verts.Add(p[i] + (side * Mathf.Cos(a) + n2 * Mathf.Sin(a)) * r[i]);
                }
            }
            for (int i = 0; i < p.Count - 1; i++)
                for (int j = 0; j < Seg; j++)
                {
                    int a = i * Seg + j, b = i * Seg + (j + 1) % Seg, c = a + Seg, d = b + Seg;
                    tris.Add(a); tris.Add(c); tris.Add(b);
                    tris.Add(b); tris.Add(c); tris.Add(d);
                }
            // سد البداية والطرف
            int c0 = verts.Count; verts.Add(p[0]);
            int c1 = verts.Count; verts.Add(p[p.Count - 1] + (p[p.Count - 1] - p[p.Count - 2]).normalized * r[r.Count - 1]);
            int last = (p.Count - 1) * Seg;
            for (int j = 0; j < Seg; j++)
            {
                int j2 = (j + 1) % Seg;
                tris.Add(c0); tris.Add(j2); tris.Add(j);
                tris.Add(c1); tris.Add(last + j); tris.Add(last + j2);
            }
        }
        mesh.Clear();
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }
}
