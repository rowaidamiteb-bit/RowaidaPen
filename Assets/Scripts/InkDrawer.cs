using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// الرسم بالماوس: الرسمة تتحول لأنبوب حبر ثلاثي الأبعاد في نفس عمق رويدا (z = 0)
// - خط مفتوح  => منصة ثابتة تمشي عليها رويدا
// - شكل مغلق  => جسم صلب يطيح بالجاذبية ويتدحرج
public class InkDrawer : MonoBehaviour
{
    public float tubeRadius = 0.17f;
    public float pointSpacing = 0.12f;
    public float closeDistance = 0.7f;     // إذا آخر الخط قريب من أوله بهالمسافة يعتبر مغلق
    public float minClosedLength = 2.5f;
    public float minStrokeLength = 0.3f;
    public int colliderStep = 3;           // كل كم نقطة نحط جزء تصادم

    static PhysicsMaterial inkPhysics;

    readonly List<Vector3> points = new List<Vector3>();
    readonly Plane drawPlane = new Plane(Vector3.forward, Vector3.zero);
    float strokeLength;
    bool drawing;
    GameObject stroke;
    Mesh strokeMesh;

    GameManager Game => GameManager.Instance;

    void Update()
    {
        Mouse mouse = Mouse.current;
        if (Game == null || mouse == null) return;

        if (!Game.IsPlaying)
        {
            if (drawing) FinishStroke();
            return;
        }

        Vector2 screen = mouse.position.ReadValue();
        if (mouse.leftButton.wasPressedThisFrame)
            BeginStroke(screen);
        else if (drawing && mouse.leftButton.isPressed)
            ContinueStroke(screen);

        if (drawing && !mouse.leftButton.isPressed)
            FinishStroke();
    }

    // صوت القلم: يعلى ويسرع مع سرعة الرسم، ويسكت لما يوقف القلم
    float lastLength;
    float drawSpeed;

    void LateUpdate()
    {
        if (Game == null || Game.Sound == null || Game.IntroActive) return;
        float delta = drawing ? Mathf.Max(0f, strokeLength - lastLength) : 0f;
        lastLength = drawing ? strokeLength : 0f;
        float instant = delta / Mathf.Max(Time.deltaTime, 1e-4f);
        drawSpeed = Mathf.Lerp(drawSpeed, instant, 0.35f);
        Game.Sound.SetPencil(drawing && drawSpeed > 0.15f, drawSpeed);
    }

    // رسم تلقائي من نقاط جاهزة (للتجربة فقط)
    public void DebugDraw(List<Vector3> path)
    {
        if (Game == null || path.Count < 2) return;
        CancelStroke();
        BeginStrokeAt(path[0]);
        if (!drawing) return;
        for (int i = 1; i < path.Count && drawing; i++)
            ExtendTo(path[i]);
        if (drawing)
        {
            RebuildMesh();
            FinishStroke();
        }
    }

    public void CancelStroke()
    {
        drawing = false;
        if (stroke != null) Destroy(stroke);
        stroke = null;
        points.Clear();
        strokeLength = 0f;
    }

    bool ScreenToWorld(Vector2 screen, out Vector3 world)
    {
        world = default;
        Camera cam = Camera.main;
        if (cam == null) return false;
        Ray ray = cam.ScreenPointToRay(screen);
        if (!drawPlane.Raycast(ray, out float distance)) return false;
        world = ray.GetPoint(distance);
        world.z = 0f;
        return true;
    }

    void BeginStroke(Vector2 screen)
    {
        if (ScreenToWorld(screen, out Vector3 p)) BeginStrokeAt(p);
    }

    void BeginStrokeAt(Vector3 p)
    {
        if (Game.Ink <= 0.01f || TouchesRowaida(p)) return;

        points.Clear();
        points.Add(p);
        strokeLength = 0f;
        drawing = true;

        stroke = new GameObject("Ink Stroke");
        stroke.transform.SetParent(Game.DrawingsRoot, false);
        strokeMesh = new Mesh { name = "Ink Tube" };
        stroke.AddComponent<MeshFilter>().sharedMesh = strokeMesh;
        stroke.AddComponent<MeshRenderer>().sharedMaterial = Game.inkMaterial;
    }

    void ContinueStroke(Vector2 screen)
    {
        if (ScreenToWorld(screen, out Vector3 target)) ExtendTo(target);
    }

    void ExtendTo(Vector3 target)
    {
        bool changed = false;
        while (true)
        {
            Vector3 last = points[points.Count - 1];
            float dist = Vector3.Distance(last, target);
            if (dist < pointSpacing) break;

            Vector3 dir = (target - last) / dist;
            Vector3 next = last + dir * pointSpacing;

            // ما نرسم فوق رويدا
            if (TouchesRowaida(next))
            {
                if (changed) RebuildMesh();
                FinishStroke();
                return;
            }

            float used = Game.UseInk(pointSpacing);
            if (used < pointSpacing)
            {
                // خلص الحبر
                if (used > 0.01f)
                {
                    points.Add(last + dir * used);
                    strokeLength += used;
                }
                RebuildMesh();
                FinishStroke();
                return;
            }

            points.Add(next);
            strokeLength += pointSpacing;
            changed = true;
        }

        if (changed) RebuildMesh();
    }

    bool TouchesRowaida(Vector3 p)
    {
        foreach (Collider c in Physics.OverlapSphere(p, tubeRadius + 0.05f))
            if (c.GetComponentInParent<RowaidaController>() != null) return true;
        return false;
    }

    void RebuildMesh()
    {
        TubeMesh.Build(strokeMesh, points, tubeRadius, false);
    }

    void FinishStroke()
    {
        drawing = false;

        if (strokeLength < minStrokeLength || points.Count < 2)
        {
            // خط قصير جدًا: نرجع الحبر ونحذفه
            Game.RefundInk(strokeLength);
            CancelStroke();
            return;
        }

        bool closed = strokeLength >= minClosedLength
                      && Vector3.Distance(points[0], points[points.Count - 1]) <= closeDistance;

        if (closed) MakeFallingShape();
        else MakePlatform();

        stroke = null;
        points.Clear();
        strokeLength = 0f;
    }

    void MakePlatform()
    {
        stroke.name = "Ink Platform";
        stroke.AddComponent<InkMark>().closed = false;
        TubeMesh.Build(strokeMesh, points, tubeRadius, false);
        AddColliders(stroke.transform, points, false);
        AddRainErosion(points, false);
        AddWindDrift();
    }

    void MakeFallingShape()
    {
        stroke.name = "Ink Shape";
        stroke.AddComponent<InkMark>().closed = true;

        // نخلي مركز الجسم في منتصف الشكل عشان يتدحرج صح
        Vector3 center = Vector3.zero;
        foreach (Vector3 p in points) center += p;
        center /= points.Count;

        var local = new List<Vector3>(points.Count);
        foreach (Vector3 p in points) local.Add(p - center);

        stroke.transform.position = center;
        TubeMesh.Build(strokeMesh, local, tubeRadius, true);
        AddColliders(stroke.transform, local, true);
        AddRainErosion(local, true);

        var rb = stroke.AddComponent<Rigidbody>();
        rb.mass = Mathf.Max(0.5f, strokeLength * 0.4f);
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.constraints = RigidbodyConstraints.FreezePositionZ
                         | RigidbodyConstraints.FreezeRotationX
                         | RigidbodyConstraints.FreezeRotationY;
        AddWindDrift();
    }

    // في مراحل المطر: الرسمة تنمسح بعد كم ثانية
    void AddRainErosion(List<Vector3> pts, bool closed)
    {
        LevelData level = Game.CurrentLevel;
        if (level == null || !level.Rain) return;
        stroke.AddComponent<InkErosion>().Init(strokeMesh, pts, tubeRadius, closed, level.rainEraseTime);
    }

    // في مراحل الريح: الريح تدفع الرسمة
    void AddWindDrift()
    {
        LevelData level = Game.CurrentLevel;
        if (level != null && level.Windy) stroke.AddComponent<InkDrift>();
    }

    // أجزاء تصادم على شكل كبسولات على طول الخط
    void AddColliders(Transform parent, List<Vector3> pts, bool closed)
    {
        if (inkPhysics == null)
            inkPhysics = new PhysicsMaterial("Ink") { dynamicFriction = 0.6f, staticFriction = 0.7f, bounciness = 0f };

        var simple = new List<Vector3>();
        for (int i = 0; i < pts.Count; i += colliderStep) simple.Add(pts[i]);
        if (!closed && simple[simple.Count - 1] != pts[pts.Count - 1]) simple.Add(pts[pts.Count - 1]);

        int count = closed ? simple.Count : simple.Count - 1;
        for (int i = 0; i < count; i++)
        {
            Vector3 a = simple[i];
            Vector3 b = simple[(i + 1) % simple.Count];
            Vector3 d = b - a;
            float len = d.magnitude;
            if (len < 0.001f) continue;

            var go = new GameObject("Collider");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = (a + b) * 0.5f;
            go.transform.localRotation = Quaternion.LookRotation(d / len, Vector3.forward);

            var cap = go.AddComponent<CapsuleCollider>();
            cap.direction = 2;
            cap.radius = tubeRadius;
            cap.height = len + tubeRadius * 2f;
            cap.sharedMaterial = inkPhysics;
        }
    }
}
