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
    public GameUI UI => ui;

    // لما ترجع رويدا بعد ما تخسر قلب (مكانها الجديد)
    public event System.Action<Vector3> Respawned;
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
        menu = gameObject.AddComponent<MenuUI>();
        menu.Build(this);

        // للتجربة من المحرر: نبدأ المرحلة على طول بدون القائمة
        if (!playIntro)
        {
            LoadLevel(Mathf.Clamp(startLevel, 0, Levels.All.Count - 1));
            return;
        }
        ShowMainMenu();
    }

    // ---------- القوائم ----------

    MenuUI menu;
    public bool Paused { get; private set; }

    // القائمة الرئيسية وورها آخر مرحلة وصلتها (واقفة كخلفية)
    void ShowMainMenu()
    {
        LoadLevel(SaveData.Unlocked, true);
        ui.SetVisible(false);
        menu.ShowMain();
    }

    public void BackToMain()
    {
        ui.SetVisible(false);
        menu.ShowMain();
    }

    public void GoToMainMenu()
    {
        menu.Transition(ShowMainMenu);
    }

    // "ابدئي": أول مرة تشوف البداية في غرفتها، بعدين تكمل من آخر مرحلة وصلتها
    public void PlayFromMenu()
    {
        if (menu.Transitioning) return;
        if (!SaveData.IntroSeen) menu.Transition(StartIntro);
        else StartLevelFromMenu(SaveData.Unlocked);
    }

    public void StartLevelFromMenu(int index)
    {
        menu.Transition(() => LoadLevel(index));
    }

    void StartIntro()
    {
        menu.HideAll();
        ClearLevel();
        ApplyTheme(LevelTheme.Classic());
        IntroActive = true;
        ui.SetVisible(false);
        gameObject.AddComponent<IntroCutscene>().Play(this, () =>
        {
            IntroActive = false;
            SaveData.IntroSeen = true;
            LoadLevel(0);
        });
    }

    void ClearLevel()
    {
        StopAllCoroutines();
        drawer.CancelStroke();
        if (levelRoot != null) Destroy(levelRoot.gameObject);
        if (DrawingsRoot != null) Destroy(DrawingsRoot.gameObject);
        levelRoot = null;
        Rowaida = null;
        IsPlaying = false;
        cameraFollow.SetTarget(null, false);
        weather.Apply(null);
    }

    void Pause()
    {
        if (!IsPlaying || menu.AnyOpen || menu.Transitioning) return;
        Paused = true;
        IsPlaying = false;
        Time.timeScale = 0f;
        Sound.SetPencil(false, 0f);
        menu.ShowPause();
    }

    public void Resume()
    {
        if (!Paused) return;
        Paused = false;
        IsPlaying = true;
        Time.timeScale = 1f;
        menu.HideAll();
    }

    void Update()
    {
        if (IntroActive) return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            if (Paused && menu.PauseOpen) Resume();
            else Pause();
        }
        if (keyboard != null && keyboard.rKey.wasPressedThisFrame && (IsPlaying || Paused))
            RestartLevel();
        else if (IsPlaying && Rowaida != null)
        {
            UpdateCheckpoint();
            // نحسب الارتفاع من جهة رويدا (في الصفحة المقلوبة "تحت" يعني فوق)
            float h = Up * Rowaida.transform.position.y;
            // أول ما تطيح تحت أوطى صفحة نشغل صوت الخسارة، ولما تنزل أكثر تخسر قلب
            if (!falling && h < LowestPageTop() - 1.8f)
            {
                falling = true;
                Sound.PlayLose();
            }
            if (h < Up * Levels.All[levelIndex].deathY) OnRowaidaFell();
        }

        ui.SetInk(InkMax > 0f ? Ink / InkMax : 0f);
        ui.SetWind(Weather.Wind, CurrentLevel != null && CurrentLevel.Windy);
        ui.SetPalette(CurrentLevel != null && CurrentLevel.colors, drawer.CurrentColor);
        UpdateTimer();
    }

    // ---------- القلوب ونقاط الرجوع ----------

    const int MaxHearts = 3;
    int hearts;
    public int Hearts => hearts;
    public int LevelIndex => levelIndex;
    int checkpointPage;      // آخر صفحة وقفت عليها رويدا
    float checkpointInk;     // كمية الحبر لما وصلتها

    // اتجاه "فوق" بالنسبة لرويدا (في الصفحة المقلوبة يصير تحت)
    float Up => CurrentLevel != null && CurrentLevel.flipped ? -1f : 1f;

    // السطح اللي تمشي عليه رويدا في الصفحة
    float Surface(Block b) => b.center.y + Up * b.size.y * 0.5f;

    // أوطى سطح (من جهة رويدا)
    float LowestPageTop()
    {
        float lowest = 0f;
        foreach (Block b in Levels.All[levelIndex].blocks)
            lowest = Mathf.Min(lowest, Up * Surface(b));
        return lowest;
    }

    // لما توقف رويدا على صفحة جديدة (أبعد من اللي قبل) نحفظها كنقطة رجوع
    void UpdateCheckpoint()
    {
        Vector3 p = Rowaida.transform.position;
        var blocks = Levels.All[levelIndex].blocks;
        for (int i = checkpointPage + 1; i < blocks.Count; i++)
        {
            Block b = blocks[i];
            float top = Up * Surface(b);
            bool overPage = p.x > b.center.x - b.size.x * 0.5f + 0.3f && p.x < b.center.x + b.size.x * 0.5f;
            float h = Up * p.y;
            bool standing = h > top + 0.55f && h < top + 0.9f;
            if (overPage && standing)
            {
                checkpointPage = i;
                checkpointInk = Ink;
            }
        }
    }

    // ---------- الوقت المحدود (صفحة الطيّة) ----------

    float timeLeft;

    void UpdateTimer()
    {
        LevelData level = CurrentLevel;
        bool timed = level != null && level.timeLimit > 0f;
        if (timed && IsPlaying)
        {
            timeLeft -= Time.deltaTime;
            if (timeLeft <= 0f)
            {
                timeLeft = 0f;
                ui.ShowMessage("الصفحة انطوت!", 1.5f);
                KillRowaida();
            }
        }
        ui.SetTimer(timed, timeLeft);
    }

    // شي قتل رويدا (مثل الحبر المنسكب): تخسر قلب
    public void KillRowaida()
    {
        if (!IsPlaying) return;
        Sound.PlayLose();
        OnRowaidaFell();
    }

    void OnRowaidaFell()
    {
        hearts--;
        ui.SetHearts(hearts, true);
        cameraFollow.Shake(0.22f, 0.35f);
        if (hearts > 0)
        {
            RespawnAtCheckpoint();
            menu.Flash();
            Fx.Sparkle(Rowaida.transform.position, new Color(1f, 0.9f, 0.6f), 16);
            ui.ShowMessage("", 1.6f, hearts == 1 ? "باقي لك قلب واحد!" : "باقي لك قلبين");
        }
        else
        {
            StartCoroutine(OutOfHeartsRoutine());
        }
    }

    // ترجع رويدا لبداية آخر صفحة وصلتها، والحبر يرجع مثل ما كان هناك
    void RespawnAtCheckpoint()
    {
        LevelData data = Levels.All[levelIndex];
        drawer.CancelStroke();
        if (DrawingsRoot != null) Destroy(DrawingsRoot.gameObject);
        DrawingsRoot = new GameObject("Drawings").transform;

        Vector3 pos = data.spawn;
        if (checkpointPage > 0)
        {
            Block b = data.blocks[checkpointPage];
            pos = new Vector3(b.center.x - b.size.x * 0.5f + 1f, Surface(b) + Up, 0f);
        }
        Rowaida.Respawn(pos);
        Respawned?.Invoke(pos);
        // نرجع لها شوي وقت عشان تلحق
        if (data.timeLimit > 0f) timeLeft = Mathf.Max(timeLeft, data.timeLimit * 0.6f);
        cameraFollow.SetTarget(Rowaida.transform, true);
        Ink = checkpointInk;
        falling = false;
    }

    IEnumerator OutOfHeartsRoutine()
    {
        IsPlaying = false;
        Rowaida.Stop();
        ui.ShowMessage("خسرتي كل القلوب!", 2.2f, "نعيد المرحلة من البداية");
        yield return new WaitForSeconds(2f);
        int index = levelIndex;
        menu.Transition(() => LoadLevel(index));
    }

    // showcase = المرحلة واقفة كخلفية للقائمة الرئيسية (رويدا ما تمشي)
    public void LoadLevel(int index, bool showcase = false)
    {
        StopAllCoroutines();
        Time.timeScale = 1f;
        Paused = false;
        if (!showcase)
        {
            menu.HideAll();
            ui.SetVisible(true);
        }
        drawer.CancelStroke();
        if (levelRoot != null) Destroy(levelRoot.gameObject);
        if (DrawingsRoot != null) Destroy(DrawingsRoot.gameObject);

        levelIndex = index;
        LevelData data = Levels.All[index];

        // الجاذبية والكاميرا: في الصفحة المقلوبة الجاذبية لفوق والكاميرا تطالع من تحت
        Physics.gravity = new Vector3(0f, (data.flipped ? 9.81f : -9.81f) * data.gravityScale, 0f);
        cameraFollow.offset = new Vector3(-0.6f, data.flipped ? -2.0f : 2.0f, -7.8f);
        cameraFollow.lookOffset = new Vector3(2f, data.flipped ? -0.7f : 0.7f, 0f);
        ApplyTheme(data.theme);
        levelRoot = LevelBuilder.Build(data, this).transform;
        DrawingsRoot = new GameObject("Drawings").transform;
        Rowaida = levelRoot.GetComponentInChildren<RowaidaController>();
        cameraFollow.SetTarget(Rowaida.transform, true);

        InkMax = Ink = data.ink;
        falling = false;
        hearts = MaxHearts;
        checkpointPage = 0;
        checkpointInk = data.ink;
        timeLeft = data.timeLimit;
        ui.SetHearts(hearts, false);
        IsPlaying = !showcase;
        ui.SetLevel(index + 1);
        weather.Apply(data);
        if (showcase)
        {
            Rowaida.enabled = false;   // تنزل على الأرض وتوقف بدون ما تمشي
            return;
        }
        ui.ShowMessage(data.title, data.subtitle != null ? 4f : 1.5f, data.subtitle);
    }

    // شكل المرحلة: ألوان القلم والحلقات، السما، الإضاءة، والأشياء الطايرة
    void ApplyTheme(LevelTheme theme)
    {
        pencilMaterial.SetColor("_BaseColor", theme.pencil);
        metalMaterial.SetColor("_BaseColor", theme.ringColor);
        pageEdgeMaterial.SetColor("_BaseColor", theme.pageEdge);
        cameraFollow.GetComponent<Camera>().backgroundColor = theme.fog;
        weather.SetBaseline(theme);
        var sky = FindAnyObjectByType<NightSky>();
        if (sky != null) sky.ApplyTheme(theme);
        var floating = FindAnyObjectByType<FloatingPapers>();
        if (floating != null) floating.ApplyTheme(theme);
    }

    public void RestartLevel()
    {
        int index = levelIndex;
        menu.Transition(() => LoadLevel(index));
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
        drawer.CancelStroke();
        Sound.PlayWin();
        // احتفال: قصاصات ورق ملونة وهزة خفيفة
        Fx.Confetti(Rowaida.transform.position + Vector3.up * Up * 1.2f, 70);
        cameraFollow.Shake(0.12f, 0.3f);
        if (CurrentLevel.drawDoor) StartCoroutine(FinalRoutine());
        else StartCoroutine(LevelCompleteRoutine());
    }

    // الصفحة الأخيرة: رويدا تدخل الباب اللي رسمتيه، الشاشة تبيض، وتبدأ النهاية في غرفتها
    IEnumerator FinalRoutine()
    {
        int index = levelIndex;
        bool[] earned = { true, hearts == MaxHearts, InkMax > 0f && Ink / InkMax >= 0.15f };
        int stars = 0;
        foreach (bool e in earned) if (e) stars++;
        SaveData.SetStars(index, stars);

        // تمشي لداخل النور وتختفي
        Transform r = Rowaida.transform;
        Vector3 from = r.position;
        for (float t = 0f; t < 1f; t += Time.deltaTime / 1.1f)
        {
            r.position = from + new Vector3(t * 0.6f, 0f, t * 1.2f);
            r.localScale = Vector3.one * (1f - t * 0.7f);
            yield return null;
        }
        if (FindAnyObjectByType<DebugAutoPlayer>() != null) { LoadLevel(0); yield break; }

        ui.SetVisible(false);
        IntroActive = true;
        gameObject.AddComponent<EndingCutscene>().Play(this, () =>
        {
            IntroActive = false;
            ClearLevel();
            ShowMainMenu();
        });
    }

    IEnumerator LevelCompleteRoutine()
    {
        int index = levelIndex;
        bool last = index + 1 >= Levels.All.Count;

        // النجوم: خلصتيها + بدون ما تخسرين قلب + باقي حبر 15% أو أكثر
        bool[] earned = { true, hearts == MaxHearts, InkMax > 0f && Ink / InkMax >= 0.15f };
        int stars = 0;
        foreach (bool e in earned) if (e) stars++;
        SaveData.SetStars(index, stars);
        if (!last) SaveData.Unlock(index + 1);

        yield return new WaitForSeconds(1.4f);

        // اللاعب الآلي (للتجربة) يكمل على طول
        if (FindAnyObjectByType<DebugAutoPlayer>() != null)
        {
            LoadLevel(last ? 0 : index + 1);
            yield break;
        }
        ui.SetVisible(false);
        menu.ShowComplete(index, earned, last, () =>
        {
            if (last) GoToMainMenu();
            else StartLevelFromMenu(index + 1);
        });
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
