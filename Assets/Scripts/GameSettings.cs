using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine;

// إعدادات الصوت (تنحفظ): الموسيقى، المؤثرات، وصوت الراوي
// المؤثرات تتحكم فيها بمستوى السماعة كله، والموسيقى والراوي يتجاهلونه ولهم مستواهم الخاص
public static class GameSettings
{
    public static float Music
    {
        get => PlayerPrefs.GetFloat("set_music", 1f);
        set { PlayerPrefs.SetFloat("set_music", Mathf.Clamp01(value)); Apply(); }
    }

    public static float Effects
    {
        get => PlayerPrefs.GetFloat("set_effects", 1f);
        set { PlayerPrefs.SetFloat("set_effects", Mathf.Clamp01(value)); Apply(); }
    }

    public static bool MusicOn
    {
        get => PlayerPrefs.GetInt("set_music_on", 1) == 1;
        set { PlayerPrefs.SetInt("set_music_on", value ? 1 : 0); Apply(); }
    }

    public static bool EffectsOn
    {
        get => PlayerPrefs.GetInt("set_effects_on", 1) == 1;
        set { PlayerPrefs.SetInt("set_effects_on", value ? 1 : 0); Apply(); }
    }

    public static bool NarrationOn
    {
        get => PlayerPrefs.GetInt("set_voice_on", 1) == 1;
        set { PlayerPrefs.SetInt("set_voice_on", value ? 1 : 0); Apply(); }
    }

    // مستوى الموسيقى الفعلي (صفر لو مكتومة)
    public static float MusicLevel => MusicOn ? Music : 0f;

    public static void Apply()
    {
        AudioListener.volume = EffectsOn ? Effects : 0f;
        Narration.ApplyVolume();
    }
}

// صوت الراوي بالمشهد الأول والأخير: ملفات مسجلة بـ Resources/Narration
public static class Narration
{
    static AudioSource source;
    static readonly Dictionary<string, float> speech = new Dictionary<string, float>();

    static AudioSource Source
    {
        get
        {
            if (source == null)
            {
                var go = new GameObject("Narrator");
                Object.DontDestroyOnLoad(go);
                source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                source.ignoreListenerVolume = true;   // ما يتأثر بكتم المؤثرات
                ApplyVolume();
            }
            return source;
        }
    }

    public static void ApplyVolume()
    {
        if (source != null) source.volume = GameSettings.NarrationOn ? 1f : 0f;
    }

    public static AudioClip Clip(string name) => Resources.Load<AudioClip>("Narration/" + name);

    // طول الكلام نفسه (بدون السكوت اللي بآخر الملف). صفر لو الراوي طافي
    public static float Length(string name)
    {
        if (!GameSettings.NarrationOn) return 0f;
        if (speech.TryGetValue(name, out float len)) return len;
        AudioClip c = Clip(name);
        len = 0f;
        if (c != null)
        {
            len = c.length;
            var data = new float[c.samples * c.channels];
            if (c.GetData(data, 0))
                for (int i = data.Length - 1; i >= 0; i -= 32)
                    if (Mathf.Abs(data[i]) > 0.03f) { len = (float)i / c.channels / c.frequency; break; }
        }
        speech[name] = len;
        return len;
    }

    public static void Say(string name)
    {
        if (!GameSettings.NarrationOn) return;
        AudioClip c = Clip(name);
        if (c == null) return;
        Source.Stop();
        Source.clip = c;
        Source.Play();
    }

    public static void Stop()
    {
        if (source != null) source.Stop();
    }
}

// وقت المشهد يتمطط بالأماكن اللي يحتاج فيها الراوي وقت أكثر (الحركات تبطئ شوي بس هناك)
public class TimeWarp
{
    readonly float[] animMarks, realMarks;

    // marks: نقاط وقت المشهد بالترتيب (أولها صفر وآخرها النهاية). minReal: أقل وقت حقيقي لكل مقطع بين نقطتين
    public TimeWarp(float[] marks, float[] minReal)
    {
        animMarks = marks;
        realMarks = new float[marks.Length];
        for (int i = 1; i < marks.Length; i++)
            realMarks[i] = realMarks[i - 1] + Mathf.Max(marks[i] - marks[i - 1], minReal[i - 1]);
    }

    public float RealEnd => realMarks[realMarks.Length - 1];

    public float Anim(float real)
    {
        if (real <= 0f) return 0f;
        for (int i = 1; i < realMarks.Length; i++)
            if (real <= realMarks[i])
                return Mathf.Lerp(animMarks[i - 1], animMarks[i], Mathf.InverseLerp(realMarks[i - 1], realMarks[i], real));
        return animMarks[animMarks.Length - 1] + (real - RealEnd);
    }

    public float Real(float anim)
    {
        if (anim <= 0f) return 0f;
        for (int i = 1; i < animMarks.Length; i++)
            if (anim <= animMarks[i])
                return Mathf.Lerp(realMarks[i - 1], realMarks[i], Mathf.InverseLerp(animMarks[i - 1], animMarks[i], anim));
        return RealEnd + (anim - animMarks[animMarks.Length - 1]);
    }
}

// زر "تخطَّ" بالمشاهد: يطلع بعد ما يمر نص المشهد، تحت على اليسار
public class SkipButton : MonoBehaviour
{
    CanvasGroup group;
    System.Action onSkip;
    bool shown;

    public static SkipButton Create(Transform canvas, System.Action onSkip)
    {
        var go = new GameObject("Skip Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(CanvasGroup));
        go.transform.SetParent(canvas, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = new Vector2(48f, 40f);
        rt.sizeDelta = new Vector2(330f, 84f);
        var img = go.GetComponent<Image>();
        img.sprite = MenuUI.RoundedSprite();
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 0.5f;
        img.color = new Color(1f, 0.47f, 0.56f);
        var label = new GameObject("Text", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
        label.transform.SetParent(go.transform, false);
        label.font = ArabicText.Font;
        label.fontSize = ArabicText.Size(40);
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.raycastTarget = false;
        label.text = ArabicText.Fix("تخطي المشهد  «");
        var lr = label.rectTransform;
        lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one; lr.offsetMin = lr.offsetMax = Vector2.zero;
        var s = go.AddComponent<SkipButton>();
        s.group = go.GetComponent<CanvasGroup>();
        s.group.alpha = 0f;
        s.group.interactable = s.group.blocksRaycasts = false;
        s.onSkip = onSkip;
        go.GetComponent<Button>().onClick.AddListener(() => { if (s.shown) s.onSkip?.Invoke(); });
        return s;
    }

    public bool Shown => shown;

    public void SetVisible(bool on)
    {
        shown = on;
        group.interactable = group.blocksRaycasts = on;
    }

    void Update()
    {
        group.alpha = Mathf.MoveTowards(group.alpha, shown ? 1f : 0f, Time.unscaledDeltaTime * 3f);
        transform.localScale = Vector3.one * (shown ? 1f + Mathf.Sin(Time.unscaledTime * 3f) * 0.03f : 1f);
    }
}
