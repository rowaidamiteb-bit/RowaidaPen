using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// مدير اللعبة: يبني المرحلة، يتابع الحبر، يعيد المرحلة إذا طاحت رويدا، وينقل للمرحلة اللي بعدها
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("مادة أساسية (كل المواد تنسخ منها)")]
    public Material paperMaterial;
    public Material inkMaterial;
    public Material rowaidaMaterial;

    [Header("الإعدادات")]
    public int startLevel = 0;
    public bool playIntro = true;   // المشهد الافتتاحي (رويدا في غرفتها)

    public bool IntroActive { get; private set; }

    // مواد تنسوي وقت اللعب
    [System.NonSerialized] public Material pageEdgeMaterial;
    [System.NonSerialized] public Material pencilMaterial;
    [System.NonSerialized] public Material metalMaterial;

    public float InkMax { get; private set; }
    public float Ink { get; private set; }
    public bool IsPlaying { get; private set; }
    public RowaidaController Rowaida { get; private set; }
    public Transform DrawingsRoot { get; private set; }
    public SoundManager Sound { get; private set; }
    public LevelData CurrentLevel => levelIndex >= 0 && levelIndex < Levels.All.Count ? Levels.All[levelIndex] : null;

    readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
    Material template;
    int levelIndex;
    Transform levelRoot;
    GameUI ui;
    InkDrawer drawer;
    CameraFollow cameraFollow;
    Weather weather;
    bool falling;

    void Awake()
    {
        Instance = this;
        Application.runInBackground = true;
        CreateMaterials();
        CreateCameraAndLight();
        CreatePostProcessing(Camera.main);

        cameraFollow = Camera.main.GetComponent<CameraFollow>();
        ui = gameObject.AddComponent<GameUI>();
        ui.Build();
        drawer = gameObject.AddComponent<InkDrawer>();
        Sound = gameObject.AddComponent<SoundManager>();
        Sound.Build();
        weather = gameObject.AddComponent<Weather>();
        weather.Build(this);
        LevelBuilder.BuildBackdrop(this);
    }

    void Start()
    {
        int first = Mathf.Clamp(startLevel, 0, Levels.All.Count - 1);
        if (!playIntro)
        {
            LoadLevel(first);
            return;
        }

        // المشهد الافتتاحي أول، وبعده المرحلة الأولى
        IntroActive = true;
        ui.SetVisible(false);
        gameObject.AddComponent<IntroCutscene>().Play(this, () =>
        {
            IntroActive = false;
            ui.SetVisible(true);
            LoadLevel(first);
        });
    }

    void Update()
    {
        if (IntroActive) return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
            RestartLevel();
        else if (IsPlaying && Rowaida != null)
        {
            float y = Rowaida.transform.position.y;
            // أول ما تطيح تحت مستوى الصفحات نشغل صوت الخسارة، وبعدين نعيد المرحلة
            if (!falling && y < -1.8f)
            {
                falling = true;
                Sound.PlayLose();
            }
            if (y < Levels.All[levelIndex].deathY) RestartLevel();
        }

        ui.SetInk(InkMax > 0f ? Ink / InkMax : 0f);
    }

    public void LoadLevel(int index)
    {
        StopAllCoroutines();
        drawer.CancelStroke();
        if (levelRoot != null) Destroy(levelRoot.gameObject);
        if (DrawingsRoot != null) Destroy(DrawingsRoot.gameObject);

        levelIndex = index;
        LevelData data = Levels.All[index];
        levelRoot = LevelBuilder.Build(data, this).transform;
        DrawingsRoot = new GameObject("Drawings").transform;
        Rowaida = levelRoot.GetComponentInChildren<RowaidaController>();
        cameraFollow.SetTarget(Rowaida.transform, true);

        InkMax = Ink = data.ink;
        falling = false;
        IsPlaying = true;
        ui.SetLevel(index + 1);
        weather.Apply(data);
        ui.ShowMessage(data.title, data.subtitle != null ? 4f : 1.5f, data.subtitle);
    }

    public void RestartLevel()
    {
        LoadLevel(levelIndex);
    }

    // يستهلك حبر ويرجع الكمية اللي فعلاً انصرفت
    public float UseInk(float amount)
    {
        float used = Mathf.Min(amount, Ink);
        Ink -= used;
        return used;
    }

    public void RefundInk(float amount)
    {
        Ink = Mathf.Min(InkMax, Ink + amount);
    }

    public void OnRowaidaReachedDoor()
    {
        if (!IsPlaying) return;
        IsPlaying = false;
        Rowaida.Stop();
        Sound.PlayWin();
        StartCoroutine(NextLevelRoutine());
    }

    IEnumerator NextLevelRoutine()
    {
        bool last = levelIndex + 1 >= Levels.All.Count;
        ui.ShowMessage(last ? "خلصتي كل المراحل!" : "أحسنتِ! خلصتي المرحلة", 2f);
        yield return new WaitForSeconds(2f);
        LoadLevel(last ? 0 : levelIndex + 1);
    }

    // ---------- المواد ----------

    void CreateMaterials()
    {
        template = paperMaterial;

        paperMaterial = Mat("Paper", Color.white, 0.08f);
        paperMaterial.SetTexture("_BaseMap", PaperMeshes.PaperGrain());

        pageEdgeMaterial = Mat("Page Edges", Color.white, 0.05f);
        pageEdgeMaterial.SetTexture("_BaseMap", PaperMeshes.PageEdges());

        // الحبر: رصاص غامق لامع شوي مثل الجرافيت
        inkMaterial = Mat("Ink", new Color(0.21f, 0.21f, 0.23f), 0.42f, 0.2f);
        pencilMaterial = Mat("Pencil Line", new Color(0.33f, 0.33f, 0.36f), 0.3f);
        metalMaterial = Mat("Metal", new Color(0.2f, 0.2f, 0.22f), 0.75f, 0.85f);
    }

    // يرجع مادة بلون معين (ويحفظها عشان ما تتكرر)
    public Material Mat(string key, Color color, float smoothness, float metallic = 0f)
    {
        if (materials.TryGetValue(key, out Material existing)) return existing;

        Material m = template != null ? new Material(template) : MakeMaterial(color, smoothness);
        m.name = key;
        if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", null);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
        if (m.HasProperty("_Color")) m.SetColor("_Color", color);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
        materials[key] = m;
        return m;
    }

    public static Material MakeMaterial(Color color, float smoothness)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        var m = new Material(shader);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
        if (m.HasProperty("_Color")) m.SetColor("_Color", color);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
        return m;
    }

    // ---------- الكاميرا والإضاءة ----------

    // كاميرا جانبية + إضاءة دافية ناعمة بظلال وخلفية حالمة
    public static void CreateCameraAndLight()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
        }
        Color night = new Color(0.07f, 0.07f, 0.14f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = night;
        cam.fieldOfView = 38f;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 200f;

        var follow = cam.GetComponent<CameraFollow>();
        if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
        follow.offset = new Vector3(-0.6f, 2.0f, -7.8f);
        follow.lookOffset = new Vector3(2f, 0.7f, 0f);
        cam.transform.position = follow.offset;
        cam.transform.LookAt(follow.lookOffset);

        Light sun = null;
        foreach (Light l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (l.type == LightType.Directional) { sun = l; break; }
        if (sun == null)
        {
            sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
        }
        sun.color = new Color(1f, 0.9f, 0.78f);
        sun.intensity = 1.35f;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.85f;
        sun.transform.rotation = Quaternion.Euler(42f, -40f, 0f);

        // إضاءة محيطة: سماء بنفسجية، أرض كحلية
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.52f, 0.5f, 0.68f);
        RenderSettings.ambientEquatorColor = new Color(0.38f, 0.36f, 0.5f);
        RenderSettings.ambientGroundColor = new Color(0.16f, 0.15f, 0.24f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = night;
        RenderSettings.fogStartDistance = 25f;
        RenderSettings.fogEndDistance = 220f;
    }

    // تأثيرات الصورة: توهج خفيف، أطراف غامقة، وتباين ألوان
    static void CreatePostProcessing(Camera cam)
    {
        var camData = cam.GetUniversalAdditionalCameraData();
        camData.renderPostProcessing = true;
        camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        camData.antialiasingQuality = AntialiasingQuality.High;

        var volume = new GameObject("Post Processing").AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10f;
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        volume.profile = profile;

        var bloom = profile.Add<Bloom>(true);
        bloom.intensity.Override(0.45f);
        bloom.threshold.Override(0.95f);
        bloom.scatter.Override(0.7f);

        var vignette = profile.Add<Vignette>(true);
        vignette.intensity.Override(0.35f);
        vignette.smoothness.Override(0.5f);
        vignette.color.Override(new Color(0.04f, 0.03f, 0.1f));

        var tonemap = profile.Add<Tonemapping>(true);
        tonemap.mode.Override(TonemappingMode.Neutral);

        var color = profile.Add<ColorAdjustments>(true);
        color.postExposure.Override(0.2f);
        color.contrast.Override(14f);
        color.saturation.Override(-4f);
    }
}
