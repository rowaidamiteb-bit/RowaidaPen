using UnityEngine;
using UnityEngine.Rendering;

// الجو حسب المرحلة: صافي (هلال ونجوم) أو مطر (قطرات، رشاش، برق ورعد، إضاءة زرقا)
public class Weather : MonoBehaviour
{
    GameManager gm;
    ParticleSystem rain, splash;
    Light sun;
    bool raining;
    float nextLightning;
    float flash;
    float thunderAt = -1f;

    // إعدادات الجو الصافي (نرجعها لما يوقف المطر)
    float clearSunIntensity;
    Color clearSunColor, clearSky, clearEquator, clearGround, clearFog;
    float clearFogStart, clearFogEnd;

    public void Build(GameManager manager)
    {
        gm = manager;
        foreach (Light l in FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (l.type == LightType.Directional) { sun = l; break; }

        clearSunIntensity = sun.intensity;
        clearSunColor = sun.color;
        clearSky = RenderSettings.ambientSkyColor;
        clearEquator = RenderSettings.ambientEquatorColor;
        clearGround = RenderSettings.ambientGroundColor;
        clearFog = RenderSettings.fogColor;
        clearFogStart = RenderSettings.fogStartDistance;
        clearFogEnd = RenderSettings.fogEndDistance;

        rain = CreateRain();
        splash = CreateSplash();
        rain.gameObject.SetActive(false);
        splash.gameObject.SetActive(false);
    }

    public void Apply(LevelData level)
    {
        raining = level != null && level.Rain;
        rain.gameObject.SetActive(raining);
        splash.gameObject.SetActive(raining);
        if (raining)
        {
            rain.Play();
            splash.Play();
        }

        var sky = FindAnyObjectByType<NightSky>();
        if (sky != null) sky.SetStormy(raining);
        gm.Sound.SetRain(raining);
        nextLightning = Time.time + Random.Range(3f, 6f);
        ApplyLighting(0f);
    }

    void ApplyLighting(float flashAmount)
    {
        if (raining)
        {
            sun.intensity = 0.75f + flashAmount * 3.5f;
            sun.color = Color.Lerp(new Color(0.7f, 0.78f, 1f), Color.white, flashAmount);
            RenderSettings.ambientSkyColor = Color.Lerp(new Color(0.34f, 0.38f, 0.52f), new Color(0.9f, 0.92f, 1f), flashAmount);
            RenderSettings.ambientEquatorColor = Color.Lerp(new Color(0.24f, 0.27f, 0.38f), new Color(0.7f, 0.72f, 0.85f), flashAmount);
            RenderSettings.ambientGroundColor = new Color(0.1f, 0.11f, 0.16f);
            RenderSettings.fogColor = Color.Lerp(new Color(0.07f, 0.08f, 0.14f), new Color(0.4f, 0.42f, 0.55f), flashAmount);
            RenderSettings.fogStartDistance = 14f;
            RenderSettings.fogEndDistance = 110f;
        }
        else
        {
            sun.intensity = clearSunIntensity;
            sun.color = clearSunColor;
            RenderSettings.ambientSkyColor = clearSky;
            RenderSettings.ambientEquatorColor = clearEquator;
            RenderSettings.ambientGroundColor = clearGround;
            RenderSettings.fogColor = clearFog;
            RenderSettings.fogStartDistance = clearFogStart;
            RenderSettings.fogEndDistance = clearFogEnd;
        }
    }

    void Update()
    {
        if (!raining || gm.IntroActive) return;

        // البرق: ومضتين سريعة، والرعد بعدها بشوي
        if (Time.time >= nextLightning)
        {
            nextLightning = Time.time + Random.Range(7f, 13f);
            flash = 1f;
            thunderAt = Time.time + Random.Range(0.5f, 1.2f);
        }
        if (thunderAt > 0f && Time.time >= thunderAt)
        {
            thunderAt = -1f;
            gm.Sound.PlayThunder();
        }
        if (flash > 0f)
        {
            flash = Mathf.Max(0f, flash - Time.deltaTime * 3.5f);
            float f = flash * (0.6f + 0.4f * Mathf.Abs(Mathf.Sin(flash * 30f)));
            ApplyLighting(f);
        }
    }

    void LateUpdate()
    {
        if (!raining) return;
        Camera cam = Camera.main;
        if (cam == null) return;
        // المطر يتبع الكاميرا
        Vector3 c = cam.transform.position;
        // نخلي المطر يبدأ بعيد شوي عن الكاميرا عشان ما تطلع قطرات ضخمة قدامها
        rain.transform.position = new Vector3(c.x + 3f, 12f, 6f);
        splash.transform.position = new Vector3(c.x + 3f, 0.03f, 0f);
    }

    // ---------- القطرات ----------

    ParticleSystem CreateRain()
    {
        var go = new GameObject("Rain");
        go.transform.SetParent(transform, false);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = true;
        main.startLifetime = 1.3f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.045f);
        main.startColor = new Color(0.75f, 0.8f, 0.95f, 0.45f);
        main.maxParticles = 4000;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 1400f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(42f, 0.5f, 16f);

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(-3f, -2f);
        vel.y = new ParticleSystem.MinMaxCurve(-20f, -16f);
        vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Stretch;
        r.velocityScale = 0.035f;
        r.lengthScale = 1f;
        r.material = ParticleMaterial("Rain Drop", StreakTexture());
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        return ps;
    }

    // رشاش صغير لما تطيح القطرة على الصفحة
    ParticleSystem CreateSplash()
    {
        var go = new GameObject("Rain Splash");
        go.transform.SetParent(transform, false);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.3f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.06f);
        main.startColor = new Color(0.8f, 0.85f, 1f, 0.6f);
        main.gravityModifier = 0.6f;
        main.maxParticles = 1500;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 260f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        // نلف الصندوق عشان القطرات تطير لفوق، فالعمق (3.8) يصير على محور y قبل اللف
        shape.scale = new Vector3(34f, 3.8f, 0.01f);
        shape.rotation = new Vector3(-90f, 0f, 0f);
        shape.randomDirectionAmount = 0.5f;

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.material = ParticleMaterial("Splash", DotTexture());
        r.shadowCastingMode = ShadowCastingMode.Off;
        return ps;
    }

    static Material ParticleMaterial(string name, Texture2D tex)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        var m = new Material(shader) { name = name };
        m.SetTexture("_BaseMap", tex);
        m.SetColor("_BaseColor", Color.white);
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", 0f);
        m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0f);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.SetOverrideTag("RenderType", "Transparent");
        m.renderQueue = (int)RenderQueue.Transparent;
        return m;
    }

    static Texture2D StreakTexture()
    {
        const int w = 8, h = 64;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = "Rain Streak", wrapMode = TextureWrapMode.Clamp };
        var px = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float u = Mathf.Abs((x + 0.5f) / w * 2f - 1f);
                float v = (y + 0.5f) / h;
                float a = (1f - u * u) * Mathf.Sin(v * Mathf.PI);
                px[y * w + x] = new Color(1f, 1f, 1f, a);
            }
        tex.SetPixels(px);
        tex.Apply();
        return tex;
    }

    static Texture2D DotTexture()
    {
        const int s = 32;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { name = "Splash Dot", wrapMode = TextureWrapMode.Clamp };
        var px = new Color[s * s];
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float dx = (x + 0.5f) / s * 2f - 1f, dy = (y + 0.5f) / s * 2f - 1f;
                px[y * s + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy)));
            }
        tex.SetPixels(px);
        tex.Apply();
        return tex;
    }
}
