using System.Collections.Generic;
using UnityEngine;

// يبني مجسم أنبوب (مثل خط حبر أو قلم رصاص) على طول مجموعة نقاط
public static class TubeMesh
{
    const int CapRings = 2;   // حلقات الطرف المدوّر

    static readonly List<Vector3> verts = new List<Vector3>();
    static readonly List<Vector3> normals = new List<Vector3>();
    static readonly List<int> tris = new List<int>();
    static int sides = 10;

    public static void Build(Mesh mesh, IList<Vector3> pts, float radius, bool closed, int sideCount = 10, IList<float> radii = null)
    {
        mesh.Clear();
        verts.Clear();
        normals.Clear();
        tris.Clear();
        sides = sideCount;

        int n = pts.Count;
        if (n < 2) return;

        // حلقة دائرية حول كل نقطة
        for (int i = 0; i < n; i++)
        {
            Frame(Tangent(pts, i, closed), out Vector3 side, out Vector3 up);
            AddRing(pts[i], side, up, Vector3.zero, R(radii, i, radius), 0f);
        }

        // نوصل كل حلقة باللي بعدها
        int segments = closed ? n : n - 1;
        for (int i = 0; i < segments; i++)
            Connect(i * sides, ((i + 1) % n) * sides, true);

        // أطراف مدوّرة للخط المفتوح
        if (!closed)
        {
            Vector3 t0 = Tangent(pts, 0, false);
            Frame(t0, out Vector3 s0, out Vector3 u0);
            AddCap(pts[0], -t0, s0, u0, R(radii, 0, radius), 0, false);

            Vector3 t1 = Tangent(pts, n - 1, false);
            Frame(t1, out Vector3 s1, out Vector3 u1);
            AddCap(pts[n - 1], t1, s1, u1, R(radii, n - 1, radius), (n - 1) * sides, true);
        }

        mesh.SetVertices(verts);
        mesh.SetNormals(normals);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
    }

    static float R(IList<float> radii, int i, float fallback)
    {
        return radii != null && i < radii.Count ? radii[i] : fallback;
    }

    static Vector3 Tangent(IList<Vector3> pts, int i, bool closed)
    {
        int n = pts.Count;
        Vector3 prev = i > 0 ? pts[i - 1] : (closed ? pts[n - 1] : pts[i]);
        Vector3 next = i < n - 1 ? pts[i + 1] : (closed ? pts[0] : pts[i]);
        Vector3 t = next - prev;
        return t.sqrMagnitude < 1e-10f ? Vector3.right : t.normalized;
    }

    // محورين عموديين على اتجاه الخط
    static void Frame(Vector3 t, out Vector3 side, out Vector3 up)
    {
        Vector3 reference = Mathf.Abs(Vector3.Dot(t, Vector3.forward)) < 0.9f ? Vector3.forward : Vector3.up;
        side = Vector3.Cross(reference, t).normalized;
        up = Vector3.Cross(t, side);
    }

    static void AddRing(Vector3 center, Vector3 side, Vector3 up, Vector3 dir, float radius, float theta)
    {
        float cosT = Mathf.Cos(theta), sinT = Mathf.Sin(theta);
        for (int j = 0; j < sides; j++)
        {
            float a = j * Mathf.PI * 2f / sides;
            Vector3 radial = side * Mathf.Cos(a) + up * Mathf.Sin(a);
            verts.Add(center + dir * (radius * sinT) + radial * (radius * cosT));
            normals.Add((radial * cosT + dir * sinT).normalized);
        }
    }

    static void Connect(int ringA, int ringB, bool forward)
    {
        for (int j = 0; j < sides; j++)
        {
            int j2 = (j + 1) % sides;
            int a = ringA + j, b = ringA + j2, c = ringB + j, d = ringB + j2;
            if (forward)
            {
                tris.Add(a); tris.Add(b); tris.Add(c);
                tris.Add(b); tris.Add(d); tris.Add(c);
            }
            else
            {
                tris.Add(a); tris.Add(c); tris.Add(b);
                tris.Add(b); tris.Add(c); tris.Add(d);
            }
        }
    }

    static void AddCap(Vector3 center, Vector3 dir, Vector3 side, Vector3 up, float radius, int baseRing, bool isEnd)
    {
        int prev = baseRing;
        for (int k = 1; k <= CapRings; k++)
        {
            float theta = k * (Mathf.PI * 0.5f) / (CapRings + 1);
            int start = verts.Count;
            AddRing(center, side, up, dir, radius, theta);
            Connect(prev, start, isEnd);
            prev = start;
        }

        int tip = verts.Count;
        verts.Add(center + dir * radius);
        normals.Add(dir);
        for (int j = 0; j < sides; j++)
        {
            int a = prev + j, b = prev + (j + 1) % sides;
            if (isEnd) { tris.Add(a); tris.Add(b); tris.Add(tip); }
            else { tris.Add(b); tris.Add(a); tris.Add(tip); }
        }
    }
}
