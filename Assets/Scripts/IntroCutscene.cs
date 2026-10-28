using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// المشهد الافتتاحي: رويدا ترسم على سريرها، تنام، يطيح الدفتر، يضوي، وتدخل داخله
public class IntroCutscene : MonoBehaviour
{
    static readonly Vector3 RoomOrigin = new Vector3(-300f, 0f, 0f);
    const float EndTime = 18.6f;

    struct Shot { public float t; public Vector3 pos, look; }
    struct Caption { public float from, to; public string text, voice; }

    GameManager gm;
    System.Action onFinished;
    Bedroom room;
    Camera cam;
    float t, realT;
    bool finished;
    TimeWarp warp;
    SkipButton skip;

    Image fade;
    Text caption;
    Canvas canvas;

    readonly List<Transform> sparkles = new List<Transform>();
    readonly List<Vector3> sparkleVel = new List<Vector3>();
    Material sparkleMat;

    bool bookLanded, magicPlayed;
    Vector3 bookStart;
    Quaternion bookStartRot;

    // إضاءة اللعبة نرجعها بعد المشهد
    Light sun;
    float sunIntensity;
    Color sunColor, ambSky, ambEq, ambGround;

    Shot[] shots;
    Caption[] captions;

    public void Play(GameManager manager, System.Action finishedCallback)
    {
        gm = manager;
        onFinished = finishedCallback;
        cam = Camera.main;
        room = Bedroom.Build(gm, RoomOrigin);

        shots = new[]
        {
            new Shot { t = 0f, pos = new Vector3(2.4f, 1.9f, -3.0f), look = new Vector3(-0.6f, 0.9f, 1.4f) },
            new Shot { t = 5.5f, pos = new Vector3(0.9f, 1.35f, -0.5f), look = new Vector3(-0.8f, 0.9f, 1.45f) },
            new Shot { t = 8.8f, pos = new Vector3(0.15f, 1.18f, 0.55f), look = new Vector3(-0.85f, 1.02f, 1.5f) },
            new Shot { t = 10.4f, pos = new Vector3(0.7f, 1.55f, -0.7f), look = new Vector3(-0.9f, 0.7f, 1.4f) },
            new Shot { t = 12.3f, pos = new Vector3(1.0f, 1.25f, -1.3f), look = new Vector3(-0.2f, 0.3f, 0.8f) },
            new Shot { t = 15.0f, pos = new Vector3(0.35f, 0.8f, -0.55f), look = room.NotebookFloor },
            new Shot { t = 16.2f, pos = new Vector3(0.25f, 0.65f, -0.35f), look = room.NotebookFloor },
            new Shot { t = 18.5f, pos = room.NotebookFloor + new Vector3(0f, 0.12f, -0.02f), look = room.NotebookFloor },
        };

        // رويدا وهي كبيرة تحكي قصتها لما كانت صغيرة
        captions = new[]
        {
            new Caption { from = 0.8f, to = 5.8f, voice = "intro_0", text = "كنت أحب الرسم... كل ليلة أرسم في دفتري قبل أن أنام" },
            new Caption { from = 6.3f, to = 9.8f, voice = "intro_1", text = "لكن تلك الليلة... كنت متعبة جدا" },
            new Caption { from = 10.1f, to = 13.2f, voice = "intro_2", text = "نمت وأنا أمسك قلمي... وسقط الدفتر" },
            new Caption { from = 13.5f, to = 16.2f, voice = "intro_3", text = "وفجأة... صار الدفتر يضيء!" },
            new Caption { from = 16.4f, to = 18.6f, voice = "intro_4", text = "ووجدت نفسي داخل دفتر رسمي..." },
        };

        // المشهد يبطئ بالأماكن اللي يحتاج فيها صوت رويدا وقت أطول
        var marks = new List<float> { 0f };
        foreach (Caption c in captions) marks.Add(c.from);
        marks.Add(EndTime);
        var minReal = new float[marks.Count - 1];
        for (int i = 0; i < captions.Length; i++) minReal[i + 1] = Narration.Length(captions[i].voice) + 0.6f;
        warp = new TimeWarp(marks.ToArray(), minReal);

        SetupLighting();
        BuildOverlay();
        sparkleMat = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "Magic Sparkle" };
        sparkleMat.SetColor("_BaseColor", new Color(3f, 2.7f, 2f));
        Apply();
    }

    public void Skip()
    {
        if (finished || t >= EndTime) return;
        realT = warp.RealEnd;
        t = EndTime;
        Narration.Stop();
    }

    void Update()
    {
        if (room == null) return;

        if (!finished)
        {
            // زر التخطي يطلع بعد ما يمر نص المشهد
            bool half = realT >= warp.RealEnd * 0.5f;
            if (half && !skip.Shown) skip.SetVisible(true);
            if (half && SkipPressed()) Skip();
            float prev = t;
            realT += Time.deltaTime;
            t = warp.Anim(realT);
            // صوت رويدا يبدأ مع كل جملة
            foreach (Caption c in captions) if (prev < c.from && t >= c.from) Narration.Say(c.voice);
            Apply();
            if (t >= EndTime) { skip.SetVisible(false); StartCoroutine(Finish()); }
        }
    }

    static bool SkipPressed()
    {
        var kb = Keyboard.current;
        return kb != null && (kb.escapeKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame);
    }

    // ---------- كل إطار: الكاميرا، رويدا، الدفتر، الكلام، الإضاءة ----------

    void Apply()
    {
        Transform rootT = room.Root;

        // الكاميرا
        GetShot(t, out Vector3 camPos, out Vector3 camLook);
        cam.transform.position = rootT.TransformPoint(camPos);
        cam.transform.LookAt(rootT.TransformPoint(camLook));

        PoseRowaida();
        AnimateNotebook();
        UpdateSparkles();

        // الكلام
        string text = "";
        float alpha = 0f;
        foreach (Caption c in captions)
            if (t >= c.from && t <= c.to)
            {
                text = c.text;
                alpha = Mathf.Clamp01((t - c.from) / 0.4f) * Mathf.Clamp01((c.to - t) / 0.4f);
            }
        string shaped = ArabicText.Fix(text);
        if (caption.text != shaped) caption.text = shaped;
        caption.color = new Color(1f, 1f, 1f, alpha);

        // الشاشة: تبدأ سودا وتفتح، وفي الآخر تبيض
        if (t < 1.5f) fade.color = new Color(0f, 0f, 0f, 1f - t / 1.5f);
        else if (t > 17.4f) fade.color = new Color(1f, 0.98f, 0.94f, Mathf.Clamp01((t - 17.4f) / 1.1f));
        else fade.color = Color.clear;

    }

    void GetShot(float time, out Vector3 pos, out Vector3 look)
    {
        if (time <= shots[0].t) { pos = shots[0].pos; look = shots[0].look; return; }
        for (int i = 0; i < shots.Length - 1; i++)
        {
            if (time <= shots[i + 1].t)
            {
                float k = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(shots[i].t, shots[i + 1].t, time));
                pos = Vector3.Lerp(shots[i].pos, shots[i + 1].pos, k);
                look = Vector3.Lerp(shots[i].look, shots[i + 1].look, k);
                return;
            }
        }
        pos = shots[shots.Length - 1].pos;
        look = shots[shots.Length - 1].look;
    }

    void PoseRowaida()
    {
        RowaidaModel r = room.Rowaida;

        // تنسدح على المخدة بين 9.5 و 11 ثانية
        float lie = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(9.5f, 11f, t)) * 82f;

        // تغفى: راسها يميل لقدام، وفيه هزة صغيرة لما تنتبه
        float nod = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(6.5f, 9f, t)) * 12f;
        if (t > 7.9f && t < 8.3f) nod -= Mathf.Sin(Mathf.InverseLerp(7.9f, 8.3f, t) * Mathf.PI) * 9f;
        nod *= 1f - lie / 82f;

        Quaternion rot = room.Facing * Quaternion.Euler(-lie, 0f, 0f);
        Vector3 hipOffset = new Vector3(0f, -0.2f, 0f);
        r.transform.localRotation = rot;
        r.transform.localPosition = room.HipSitting - rot * hipOffset + Vector3.up * (lie / 82f) * 0.06f;
        r.Model.localRotation = Quaternion.Euler(8f + nod, 0f, 0f);

        // الرجول ممدودة لقدام على السرير
        Quaternion legs = Quaternion.Euler(-90f + lie, 0f, 0f);
        r.LegL.localRotation = legs * Quaternion.Euler(0f, 0f, -3f);
        r.LegR.localRotation = legs * Quaternion.Euler(0f, 0f, 3f);

        // اليدين: ماسكة الدفتر وترسم، وبعدين ترتخي
        float relax = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(8.8f, 10.5f, t));
        float drawing = t < 6.5f ? 1f : Mathf.Clamp01(1f - (t - 6.5f) / 1.5f);
        float write = Mathf.Sin(t * 9f) * 6f * drawing;
        float wiggle = Mathf.Sin(t * 5.3f) * 4f * drawing;
        r.ArmL.localRotation = Quaternion.Slerp(Quaternion.Euler(-50f, 0f, -12f), Quaternion.Euler(-10f, 0f, -8f), relax);
        r.ArmR.localRotation = Quaternion.Slerp(Quaternion.Euler(-62f + write, wiggle, 14f), Quaternion.Euler(-10f, 0f, 8f), relax);

        // تبطئ الرمش وتسكر عيونها
        bool closed = (t > 6.8f && t < 6.95f) || (t > 7.55f && t < 7.85f) || t > 8.45f;
        r.SetEyesClosed(closed);

        // صوت القلم وهي ترسم
        gm.Sound.SetPencil(drawing > 0.2f && t > 1f && Mathf.Sin(t * 1.7f) > -0.4f, 2.5f * drawing);
    }

    void AnimateNotebook()
    {
        Transform nb = room.Notebook;
        RowaidaModel r = room.Rowaida;

        // في حضنها (فوق رجولها) قبل ما يطيح
        Vector3 lapPos = room.HipSitting + room.Facing * new Vector3(0f, 0.11f, 0.3f);
        Quaternion lapRot = room.Facing * Quaternion.Euler(-28f, 0f, 0f);

        const float fallStart = 10.6f, fallEnd = 11.5f;
        if (t < fallStart)
        {
            nb.localPosition = lapPos;
            nb.localRotation = lapRot;
            bookStart = lapPos;
            bookStartRot = lapRot;
            bookLanded = false;
        }
        else if (t < fallEnd)
        {
            float k = Mathf.InverseLerp(fallStart, fallEnd, t);
            Vector3 p = Vector3.Lerp(bookStart, room.NotebookFloor, k);
            p.y = Mathf.Lerp(bookStart.y, room.NotebookFloor.y, k * k) + Mathf.Sin(k * Mathf.PI) * 0.18f;
            nb.localPosition = p;
            nb.localRotation = Quaternion.Slerp(bookStartRot, Quaternion.Euler(0f, 25f, 0f), k) * Quaternion.Euler(Mathf.Sin(k * Mathf.PI) * 70f, 0f, 0f);
        }
        else
        {
            if (!bookLanded)
            {
                bookLanded = true;
                if (t < fallEnd + 0.5f) gm.Sound.Play(SoundClips.Flop, 0.9f);
            }
            float b = Mathf.InverseLerp(fallEnd, fallEnd + 0.35f, t);
            float bounce = Mathf.Sin(b * Mathf.PI) * 0.025f * (1f - b);
            nb.localPosition = room.NotebookFloor + Vector3.up * bounce;
            nb.localRotation = Quaternion.Euler(0f, 25f, 0f);
        }

        // الدفتر يضوي
        float glow = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(13.2f, 16.5f, t));
        room.NotebookGlow.intensity = glow * (1.3f + Mathf.Sin(t * 6f) * 0.15f);

        if (!magicPlayed && t > 13.3f && t < EndTime)
        {
            magicPlayed = true;
            gm.Sound.Play(SoundClips.Magic, 0.9f);
        }
    }

    // لمعات تطلع من الدفتر
    void UpdateSparkles()
    {
        if (t > 13.6f && t < EndTime && Random.value < Time.deltaTime * 30f)
        {
            var s = LevelBuilder.Visual("Sparkle", PrimitiveType.Sphere, room.Root, room.NotebookFloor + new Vector3(Random.Range(-0.25f, 0.25f), 0.05f, Random.Range(-0.18f, 0.18f)), Vector3.one * Random.Range(0.012f, 0.03f), sparkleMat, false);
            sparkles.Add(s.transform);
            sparkleVel.Add(new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(0.25f, 0.6f), Random.Range(-0.1f, 0.1f)));
        }
        for (int i = sparkles.Count - 1; i >= 0; i--)
        {
            Transform s = sparkles[i];
            s.localPosition += sparkleVel[i] * Time.deltaTime;
            s.localScale *= 1f - Time.deltaTime * 0.6f;
            if (s.localScale.x < 0.004f)
            {
                Destroy(s.gameObject);
                sparkles.RemoveAt(i);
                sparkleVel.RemoveAt(i);
            }
        }
    }

    // ---------- النهاية: ننتقل للمرحلة الأولى ----------

    System.Collections.IEnumerator Finish()
    {
        finished = true;
        caption.text = "";
        fade.color = new Color(1f, 0.98f, 0.94f, 1f);
        gm.Sound.SetPencil(false, 0f);
        yield return null;

        RestoreLighting();
        Destroy(room.Root.gameObject);
        room = null;
        onFinished?.Invoke();
        yield return null;

        // الشاشة البيضا تختفي، وجملة تشرح الهدف
        string goal = ArabicText.Fix("الطريق الوحيد للعودة إلى غرفتي... هو الوصول إلى آخر صفحة");
        float show = Mathf.Max(4.5f, Narration.Length("intro_5") + 1.6f);
        bool said = false;
        for (float k = 0f; k < show; k += Time.deltaTime)
        {
            if (!said && k > 0.8f) { said = true; Narration.Say("intro_5"); }
            fade.color = new Color(1f, 0.98f, 0.94f, 1f - Mathf.Clamp01(k / 1.2f));
            caption.text = goal;
            caption.color = new Color(1f, 1f, 1f, Mathf.Clamp01((k - 0.8f) / 0.4f) * Mathf.Clamp01((show - k) / 0.6f));
            yield return null;
        }
        Destroy(canvas.gameObject);
        Destroy(this);
    }

    // ---------- الإضاءة والواجهة ----------

    void SetupLighting()
    {
        foreach (Light l in FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (l.type == LightType.Directional && l.name != "Rim Light") { sun = l; break; }
        if (sun != null)
        {
            sunIntensity = sun.intensity;
            sunColor = sun.color;
            sun.intensity = 0.12f;
            sun.color = new Color(0.6f, 0.66f, 1f);
        }
        ambSky = RenderSettings.ambientSkyColor;
        ambEq = RenderSettings.ambientEquatorColor;
        ambGround = RenderSettings.ambientGroundColor;
        RenderSettings.ambientSkyColor = new Color(0.26f, 0.25f, 0.4f);
        RenderSettings.ambientEquatorColor = new Color(0.2f, 0.18f, 0.3f);
        RenderSettings.ambientGroundColor = new Color(0.1f, 0.09f, 0.14f);
    }

    void RestoreLighting()
    {
        if (sun != null)
        {
            sun.intensity = sunIntensity;
            sun.color = sunColor;
        }
        RenderSettings.ambientSkyColor = ambSky;
        RenderSettings.ambientEquatorColor = ambEq;
        RenderSettings.ambientGroundColor = ambGround;
    }

    void BuildOverlay()
    {
        var go = new GameObject("Intro UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 0.4f;
        canvas.sortingOrder = 100;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        fade = new GameObject("Fade", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        fade.transform.SetParent(go.transform, false);
        fade.rectTransform.anchorMin = Vector2.zero;
        fade.rectTransform.anchorMax = Vector2.one;
        fade.rectTransform.offsetMin = fade.rectTransform.offsetMax = Vector2.zero;
        fade.color = Color.black;
        fade.raycastTarget = false;

        caption = NewText(go.transform, 50, new Vector2(0.5f, 0f), new Vector2(0f, 120f), new Vector2(1700f, 90f));
        skip = SkipButton.Create(go.transform, Skip);
    }

    static Text NewText(Transform parent, int size, Vector2 anchor, Vector2 pos, Vector2 box)
    {
        var t = new GameObject("Text", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
        t.transform.SetParent(parent, false);
        t.font = ArabicText.Font;
        t.fontSize = ArabicText.Size(size);
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.gameObject.AddComponent<Outline>().effectColor = new Color(0.05f, 0.03f, 0.1f, 0.9f);
        var shadow = t.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
        shadow.effectDistance = new Vector2(2f, -2f);
        var rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = box;
        return t;
    }
}

// أصوات المشهد الافتتاحي (تنصنع مرة وحدة)
public static class SoundClips
{
    static AudioClip flop, magic;
    public static AudioClip Flop => flop != null ? flop : (flop = SynthAudio.BookFlop());
    public static AudioClip Magic => magic != null ? magic : (magic = SynthAudio.Magic());
}
