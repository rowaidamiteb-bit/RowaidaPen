using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// أداة مساعدة لـ Claude: لما ينحط ملف طلب في Temp، تحدّث المشروع، تشغل اللعبة،
// تاخذ صور للشاشة، وتوقف اللعبة. الصور تنحفظ في Temp/ClaudeCaptures
[InitializeOnLoad]
public static class ClaudeCapture
{
    const string RequestFile = "Temp/claude_capture_request.txt";
    const string OutDir = "Temp/ClaudeCaptures";
    const string StatusFile = "Temp/ClaudeCaptures/status.txt";
    const string StateKey = "ClaudeCapture.State";
    const string ShotsKey = "ClaudeCapture.Shots";
    const string StartKey = "ClaudeCapture.PlayStart";
    const string DoneKey = "ClaudeCapture.Done";

    static double nextTick;

    static ClaudeCapture()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += change =>
        {
            if (change == PlayModeStateChange.EnteredPlayMode && SessionState.GetInt(StateKey, 0) == 2)
                SessionState.SetFloat(StartKey, (float)EditorApplication.timeSinceStartup);
        };
    }

    static void Tick()
    {
        double now = EditorApplication.timeSinceStartup;
        int state = SessionState.GetInt(StateKey, 0);

        // وقت اللعب: نوقف اللعبة ونقدمها إطار بإطار بنفسنا (30 إطار بالثانية)
        // عشان تشتغل حتى لو نافذة Unity مو قدام
        if (state == 2 && EditorApplication.isPlaying && SessionState.GetFloat(StartKey, -1f) >= 0f)
        {
            Time.captureFramerate = 30;
            if (!EditorApplication.isPaused) EditorApplication.isPaused = true;
            EditorApplication.Step();
        }

        if (now < nextTick) return;
        nextTick = now + (state == 2 ? 0.02 : 0.25);
        if (state == 0)
        {
            if (!File.Exists(RequestFile)) return;
            string request = File.ReadAllText(RequestFile).Trim();
            File.Delete(RequestFile);
            Directory.CreateDirectory(OutDir);
            foreach (string f in Directory.GetFiles(OutDir)) File.Delete(f);
            SessionState.SetString(ShotsKey, request);
            SessionState.SetString(DoneKey, "");
            SessionState.SetInt(StateKey, 1);
            Status("refreshing");
            AssetDatabase.Refresh();
            return;
        }

        if (state == 1)
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (EditorUtility.scriptCompilationFailed)
            {
                Status("compile-error");
                SessionState.SetInt(StateKey, 0);
                return;
            }
            if (EditorApplication.isPlaying) return;
            if (EditorSceneManager.GetActiveScene().path != "Assets/Scenes/Game.unity")
                EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
            SessionState.SetInt(StateKey, 2);
            SessionState.SetFloat(StartKey, -1f);
            PlayerSettings.runInBackground = true;
            Status("starting-play");
            EditorApplication.isPlaying = true;
            return;
        }

        if (state == 2)
        {
            if (!EditorApplication.isPlaying) return;
            float start = SessionState.GetFloat(StartKey, -1f);
            if (start < 0f) return;
            // نستخدم وقت اللعبة نفسها، ومهلة قصوى دقيقتين
            float elapsed = Time.timeSinceLevelLoad;
            if (now - start > 120f) elapsed = float.MaxValue;

            // كل طلب: "الوقت:نوع" مثل 2:main أو 3:close
            string done = SessionState.GetString(DoneKey, "");
            var shots = SessionState.GetString(ShotsKey, "").Split(new[] { ',', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            bool allDone = true;
            for (int i = 0; i < shots.Length; i++)
            {
                string tag = "[" + i + "]";
                if (done.Contains(tag)) continue;
                string[] parts = shots[i].Trim().Split(':');
                float t = float.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture);
                string kind = parts.Length > 1 ? parts[1] : "main";
                if (elapsed >= t)
                {
                    RunCommand(kind, i);
                    done += tag;
                    SessionState.SetString(DoneKey, done);
                }
                else allDone = false;
            }

            if (allDone)
            {
                SessionState.SetInt(StateKey, 0);
                Status("done frames=" + Time.frameCount + " time=" + Time.timeSinceLevelLoad.ToString("0.0"));
                Time.captureFramerate = 0;
                EditorApplication.isPaused = false;
                EditorApplication.isPlaying = false;
            }
        }
    }

    static void RunCommand(string kind, int index)
    {
        try
        {
            switch (kind)
            {
                case "bridge":
                    // يرسم خط فوق الحفرة الأولى (للتجربة)
                    InkDrawer drawer = Object.FindAnyObjectByType<InkDrawer>();
                    if (drawer != null) drawer.DebugDraw(new List<Vector3> { new Vector3(6.2f, 0.25f, 0f), new Vector3(11.8f, 0.25f, 0f) });
                    break;
                case "bridge2":
                    InkDrawer d3 = Object.FindAnyObjectByType<InkDrawer>();
                    if (d3 != null) d3.DebugDraw(new List<Vector3> { new Vector3(20.2f, 0.25f, 0f), new Vector3(25.3f, 0.25f, 0f) });
                    break;
                case "circle":
                    InkDrawer d2 = Object.FindAnyObjectByType<InkDrawer>();
                    var pts = new List<Vector3>();
                    for (int i = 0; i <= 24; i++)
                    {
                        float a = i / 24f * Mathf.PI * 2f;
                        pts.Add(new Vector3(2f + Mathf.Cos(a) * 0.8f, 3f + Mathf.Sin(a) * 0.8f, 0f));
                    }
                    if (d2 != null) d2.DebugDraw(pts);
                    break;
                default:
                    Capture(kind, index);
                    break;
            }
        }
        catch (System.Exception e)
        {
            File.AppendAllText(StatusFile, "\nerror: " + e);
        }
    }

    static void Capture(string kind, int index)
    {
        Camera main = Camera.main;
        if (main == null) return;

        Camera cam = main;
        GameObject temp = null;
        if (kind == "wide")
        {
            temp = Object.Instantiate(main.gameObject);
            foreach (var c in temp.GetComponents<MonoBehaviour>()) Object.DestroyImmediate(c);
            var listener = temp.GetComponent<AudioListener>();
            if (listener != null) Object.DestroyImmediate(listener);
            cam = temp.GetComponent<Camera>();
            cam.transform.position = new Vector3(14.5f, 9f, -27f);
            cam.transform.LookAt(new Vector3(14.5f, -0.5f, 0f));
            cam.fieldOfView = 42f;
        }
        else if (kind == "close" || kind == "face")
        {
            var rowaida = Object.FindAnyObjectByType<RowaidaController>();
            temp = Object.Instantiate(main.gameObject);
            foreach (var c in temp.GetComponents<MonoBehaviour>()) Object.DestroyImmediate(c);
            var listener = temp.GetComponent<AudioListener>();
            if (listener != null) Object.DestroyImmediate(listener);
            cam = temp.GetComponent<Camera>();
            Vector3 target = rowaida.transform.position + Vector3.up * (kind == "face" ? 0.36f : 0.05f);
            float dist = kind == "face" ? 1.3f : 3.2f;
            cam.transform.position = target + new Vector3(0.9f, 0.25f, -1f).normalized * dist;
            cam.transform.LookAt(target);
            cam.fieldOfView = 35f;
        }

        var rt = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32);
        RenderTexture prevTarget = cam.targetTexture;
        cam.targetTexture = rt;
        cam.Render();
        cam.targetTexture = prevTarget;

        RenderTexture prevActive = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        RenderTexture.active = prevActive;

        File.WriteAllBytes(Path.Combine(OutDir, $"shot{index}_{kind}.png"), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        rt.Release();
        Object.DestroyImmediate(rt);
        if (temp != null) Object.DestroyImmediate(temp);
    }

    static void Status(string s)
    {
        Directory.CreateDirectory(OutDir);
        File.WriteAllText(StatusFile, s);
    }
}
