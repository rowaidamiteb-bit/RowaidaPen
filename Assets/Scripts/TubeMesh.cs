using System.Collections.Generic;
using UnityEngine;

// يبني مجسم أنبوب (مثل خط حبر سميك) على طول مجموعة نقاط في مستوى XY
public static class TubeMesh
{
    const int Sides = 10;     // عدد أضلاع دائرة الأنبوب
    const int CapRings = 2;   // حلقات الطرف المدوّر

    static readonly List<Vector3> verts = new List<Vector3>();
    static readonly List<Vector3> normals = new List<Vector3>();
    static readonly List<int> tris = new List<int>();

    public static void Build(Mesh mesh, IList<Vector3> pts, float radius, bool closed)
    {
        mesh.Clear();
        verts.Clear();
        normals.Clear();
        tris.Clear();

        int n = pts.Count;
        if (n < 2) return;

        // حلقة دائرية حول كل نقطة
        for (int i = 0; i < n; i++)
        {
            Vector3 side = SideAxis(pts, i, closed);
            AddRing(pts[i], side, Vector3.zero, radius, 0f);
        }

        // نوصل كل حلقة باللي بعدها
        int segments = closed ? n : n - 1;
        for (int i = 0; i < segments; i++)
            Connect(i * Sides, ((i + 1) % n) * Sides, true);

        // أطراف مدوّرة للخط المفتوح
        if (!closed)
        {
            AddCap(pts[0], -Tangent(pts, 0, false), SideAxis(pts, 0, false), radius, 0, false);
            AddCap(pts[n - 1], Tangent(pts, n - 1, false), SideAxis(pts, n - 1, false), radius, (n - 1) * Sides, true);
        }

        mesh.SetVertices(verts);
        mesh.SetNormals(normals);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
    }

    static Vector3 Tangent(IList<Vector3> pts, int i, bool closed)
    {
        int n = pts.Count;
        Vector3 prev = i > 0 ? pts[i - 1] : (closed ? pts[n - 1] : pts[i]);
        Vector3 next = i < n - 1 ? pts[i + 1] : (closed ? pts[0] : pts[i]);
        Vector3 t = next - prev;
        t.z = 0f;
        return t.sqrMagnitude < 1e-8f ? Vector3.right : t.normalized;
    }

    // محور عمودي على الخط داخل مستوى الرسم
    static Vector3 SideAxis(IList<Vector3> pts, int i, bool closed)
    {
        return Vector3.Cross(Vector3.forward, Tangent(pts, i, closed)).normalized;
    }

    static void AddRing(Vector3 center, Vector3 side, Vector3 dir, float radius, float theta)
    {
        float cosT = Mathf.Cos(theta), sinT = Mathf.Sin(theta);
        for (int j = 0; j < Sides; j++)
        {
            float a = j * Mathf.PI * 2f / Sides;
            Vector3 radial = side * Mathf.Cos(a) + Vector3.forward * Mathf.Sin(a);
            verts.Add(center + dir * (radius * sinT) + radial * (radius * cosT));
            normals.Add((radial * cosT + dir * sinT).normalized);
        }
    }

    static void Connect(int ringA, int ringB, bool forward)
    {
        for (int j = 0; j < Sides; j++)
        {
            int j2 = (j + 1) % Sides;
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

    static void AddCap(Vector3 center, Vector3 dir, Vector3 side, float radius, int baseRing, bool isEnd)
    {
        int prev = baseRing;
        for (int k = 1; k <= CapRings; k++)
        {
            float theta = k * (Mathf.PI * 0.5f) / (CapRings + 1);
            int start = verts.Count;
            AddRing(center, side, dir, radius, theta);
            Connect(prev, start, isEnd);
            prev = start;
        }

        int tip = verts.Count;
        verts.Add(center + dir * radius);
        normals.Add(dir);
        for (int j = 0; j < Sides; j++)
        {
            int a = prev + j, b = prev + (j + 1) % Sides;
            if (isEnd) { tris.Add(a); tris.Add(b); tris.Add(tip); }
            else { tris.Add(b); tris.Add(a); tris.Add(tip); }
        }
    }
}
