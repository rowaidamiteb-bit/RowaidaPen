using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// مدير اللعبة: يبني المرحلة، يتابع الحبر، يعيد المرحلة إذا طاحت رويدا، وينقل للمرحلة اللي بعدها
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("المواد")]
    public Material paperMaterial;
    public Material inkMaterial;
    public Material rowaidaMaterial;

    [Header("الإعدادات")]
    public int startLevel = 0;

    public float InkMax { get; private set; }
    public float Ink { get; private set; }
    public bool IsPlaying { get; private set; }
    public RowaidaController Rowaida { get; private set; }
    public Transform DrawingsRoot { get; private set; }

    int levelIndex;
    Transform levelRoot;
    GameUI ui;
    InkDrawer drawer;
    CameraFollow cameraFollow;

    void Awake()
    {
        Instance = this;
        EnsureMaterials();
        CreateCameraAndLight();

        cameraFollow = Camera.main.GetComponent<CameraFollow>();
        ui = gameObject.AddComponent<GameUI>();
        ui.Build();
        drawer = gameObject.AddComponent<InkDrawer>();
        LevelBuilder.BuildBackground(this);
    }

    void Start()
    {
        LoadLevel(Mathf.Clamp(startLevel, 0, Levels.All.Count - 1));
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
            RestartLevel();
        else if (IsPlaying && Rowaida != null && Rowaida.transform.position.y < Levels.All[levelIndex].deathY)
            RestartLevel();

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
        IsPlaying = true;
        ui.SetLevel(index + 1);
        ui.ShowMessage(data.title, 1.5f);
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
        StartCoroutine(NextLevelRoutine());
    }

    IEnumerator NextLevelRoutine()
    {
        bool last = levelIndex + 1 >= Levels.All.Count;
        ui.ShowMessage(last ? "You finished all levels!" : "Level Complete!", 2f);
        yield return new WaitForSeconds(2f);
        LoadLevel(last ? 0 : levelIndex + 1);
    }

    void EnsureMaterials()
    {
        if (paperMaterial == null) paperMaterial = MakeMaterial(new Color(0.96f, 0.96f, 0.94f), 0.05f);
        if (inkMaterial == null) inkMaterial = MakeMaterial(new Color(0.07f, 0.07f, 0.08f), 0.35f);
        if (rowaidaMaterial == null) rowaidaMaterial = MakeMaterial(new Color(0.32f, 0.32f, 0.33f), 0.25f);
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

    // كاميرا جانبية + إضاءة ناعمة بظلال (تُستخدم وقت اللعب وأيضًا لما نبني المشهد في المحرر)
    public static void CreateCameraAndLight()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
        }
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.96f, 0.96f, 0.94f);
        cam.fieldOfView = 40f;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 200f;
        var follow = cam.GetComponent<CameraFollow>();
        if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
        cam.transform.position = new Vector3(0f, 3.5f, -14f);
        cam.transform.LookAt(follow.lookOffset);

        Light sun = null;
        foreach (Light l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (l.type == LightType.Directional) { sun = l; break; }
        if (sun == null)
        {
            sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
        }
        sun.color = new Color(1f, 0.98f, 0.94f);
        sun.intensity = 1.2f;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.55f;
        sun.transform.rotation = Quaternion.Euler(45f, -35f, 0f);

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.62f, 0.62f, 0.62f);
        RenderSettings.fog = false;
    }
}
