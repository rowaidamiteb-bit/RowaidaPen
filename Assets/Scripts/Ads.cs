using System;
using UnityEngine;
using UnityEngine.UI;

// الإعلانات: إعلان بين المراحل بعد كل خمس مراحل تخلصها اللاعبة (ما يقاطع اللعب أبدًا، يطلع قبل المرحلة الجاية).
// الحين بدون شركة إعلانات: بالمحرر والنسخ التجريبية يطلع إعلان تجريبي عشان نشوف مكانه،
// وبالنسخة الحقيقية ما يطلع شي لين نربط حساب الإعلانات
public static class Ads
{
    public const int Every = 5;
    const string CountKey = "ads_levels_done";
    static bool pending;

    // خلصت مرحلة: كل خمس مراحل يصير فيه إعلان ينتظر
    public static void LevelCompleted()
    {
        int n = PlayerPrefs.GetInt(CountKey, 0) + 1;
        PlayerPrefs.SetInt(CountKey, n);
        PlayerPrefs.Save();
        if (n % Every == 0) pending = true;
    }

    // قبل ما نبدأ المرحلة الجاية: لو فيه إعلان ينتظر نعرضه، وبعدين نكمل
    public static void ShowIfDue(Action then)
    {
        if (!pending) { then(); return; }
        pending = false;
        if (!Show(then)) then();
    }

    static bool Show(Action done)
    {
        // هنا نربط شركة الإعلانات لما يكون عندنا الحساب (ونخليها إعلانات مناسبة للأطفال)
        if (!Debug.isDebugBuild && !Application.isEditor) return false;
        AdPlaceholder.Open(done);
        return true;
    }
}

// إعلان تجريبي: شاشة تغطي اللعبة، فيها مكان الإعلان، وزر إغلاق يطلع بعد ثلاث ثواني
public class AdPlaceholder : MonoBehaviour
{
    Action done;
    float age;
    Text countdown;
    GameObject close;

    public static void Open(Action onDone)
    {
        var go = new GameObject("Ad", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        var ad = go.AddComponent<AdPlaceholder>();
        ad.done = onDone;
        ad.Build((RectTransform)go.transform);
    }

    void Build(RectTransform root)
    {
        Sprite round = MenuUI.RoundedSprite();
        var dim = Box(root, "Dim", new Color(0.08f, 0.06f, 0.12f, 0.92f), null, Vector2.zero, Vector2.zero);
        dim.rectTransform.anchorMin = Vector2.zero; dim.rectTransform.anchorMax = Vector2.one;
        dim.raycastTarget = true;   // ما تقدر ترسم ورا الإعلان
        var card = Box(root, "Card", new Color(1f, 0.97f, 0.9f), round, Vector2.zero, new Vector2(1100f, 620f));
        Label(card.rectTransform, "إعلان", 70, new Vector2(0f, 190f), new Color(1f, 0.47f, 0.56f));
        Label(card.rectTransform, "هنا يطلع الإعلان الحقيقي", 42, new Vector2(0f, 40f), new Color(0.17f, 0.24f, 0.31f));
        Label(card.rectTransform, "بعد ما نربط حساب الإعلانات", 34, new Vector2(0f, -30f), new Color(0.45f, 0.47f, 0.5f));
        countdown = Label(card.rectTransform, "", 34, new Vector2(0f, -190f), new Color(0.45f, 0.47f, 0.5f));
        // زر الإغلاق فوق يمين
        var x = Box(card.rectTransform, "Close", new Color(0.17f, 0.24f, 0.31f), MenuUI.CircleSprite(), new Vector2(500f, 260f), new Vector2(90f, 90f));
        x.raycastTarget = true;
        Label(x.rectTransform, "×", 60, new Vector2(0f, 4f), Color.white);
        x.gameObject.AddComponent<Button>().onClick.AddListener(Finish);
        close = x.gameObject;
        close.SetActive(false);
    }

    void Update()
    {
        age += Time.unscaledDeltaTime;
        int left = Mathf.CeilToInt(3f - age);
        countdown.text = left > 0 ? ArabicText.Fix("تقدرين تقفلينه بعد " + left) : "";
        if (left <= 0 && !close.activeSelf) close.SetActive(true);
    }

    void Finish()
    {
        Destroy(gameObject);
        done?.Invoke();
    }

    static Image Box(RectTransform parent, string name, Color c, Sprite sprite, Vector2 pos, Vector2 size)
    {
        var img = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        img.transform.SetParent(parent, false);
        img.color = c;
        img.raycastTarget = false;
        if (sprite != null) { img.sprite = sprite; img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = 1f; }
        img.rectTransform.anchoredPosition = pos;
        img.rectTransform.sizeDelta = size;
        return img;
    }

    static Text Label(RectTransform parent, string text, int size, Vector2 pos, Color c)
    {
        var t = new GameObject("Text", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
        t.transform.SetParent(parent, false);
        t.font = ArabicText.Font;
        t.text = ArabicText.Fix(text);
        t.fontSize = ArabicText.Size(size);
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = c;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.rectTransform.anchoredPosition = pos;
        t.rectTransform.sizeDelta = new Vector2(1000f, 100f);
        return t;
    }
}
