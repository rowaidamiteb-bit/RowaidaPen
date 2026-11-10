using System;
using Unity.Services.LevelPlay;
using UnityEngine;
using UnityEngine.UI;

// الإعلانات (يونتي LevelPlay): بعد كل خمس مراحل يطلع إعلان قبل المرحلة الجاية (ما يقاطع اللعب أبدًا).
// لو شافته اللاعبة للآخر تتدبل الكوينز اللي جمعتها من آخر خمس مراحل.
// المفتاح ورموز الإعلانين من لوحة LevelPlay (لو فاضية: ما فيه إعلانات واللعبة تكمل عادي)
public static class Ads
{
    public const int Every = 5;
#if UNITY_IOS
    const string AppKey = "";
    const string RewardedId = "";
    const string InterstitialId = "";
#else
    // أندرويد: من حساب LevelPlay (تطبيق "قلم رويدا")
    const string AppKey = "296e24f65";
    const string RewardedId = "jglhr81kmtpagg0a";       // Double Coins Rewarded
    const string InterstitialId = "bngenpt4djx3l4ci";   // Between Levels Interstitial
#endif
    const string CountKey = "ads_levels_done";
    const string BonusKey = "ads_bonus";   // كوينز آخر خمس مراحل (تنضاف مرة ثانية لو شافت الإعلان كامل)
    static bool pending, started;
    static LevelPlayRewardedAd rewarded;
    static LevelPlayInterstitialAd interstitial;
    static Action afterAd;   // نكمل بعد ما يتسكر الإعلان
    static int rewardBonus;  // الكوينز اللي تنضاف لو شافت الإعلان الحالي للآخر

    public static void Init()
    {
        if (started || string.IsNullOrEmpty(AppKey)) return;
        started = true;
        LevelPlay.OnInitSuccess += config => CreateAds();
        LevelPlay.OnInitFailed += error => Debug.LogWarning("الإعلانات ما اشتغلت: " + error);
        LevelPlay.Init(AppKey);
    }

    // نجهز الإعلانين من قبل عشان يطلعون على طول وقت ما نحتاجهم
    static void CreateAds()
    {
        rewarded = new LevelPlayRewardedAd(RewardedId);
        rewarded.OnAdRewarded += (info, reward) =>
        {
            if (rewardBonus <= 0) return;
            Coins.Add(rewardBonus);
            GameManager.Instance.Menu.Toast("تضاعفت الكوينز! حصلنا على " + rewardBonus + " إضافية");
            rewardBonus = 0;
        };
        rewarded.OnAdClosed += info => { rewarded.LoadAd(); Continue(); };
        rewarded.OnAdDisplayFailed += (info, error) => { rewarded.LoadAd(); Continue(); };
        rewarded.LoadAd();

        interstitial = new LevelPlayInterstitialAd(InterstitialId);
        interstitial.OnAdClosed += info => { interstitial.LoadAd(); Continue(); };
        interstitial.OnAdDisplayFailed += (info, error) => { interstitial.LoadAd(); Continue(); };
        interstitial.LoadAd();
    }

    static void Continue()
    {
        Action next = afterAd;
        afterAd = null;
        next?.Invoke();
    }

    // خلصت مرحلة: نحسب كوينزها للمضاعفة، وكل خمس مراحل يصير فيه إعلان ينتظر
    public static void LevelCompleted(int coins)
    {
        int n = PlayerPrefs.GetInt(CountKey, 0) + 1;
        PlayerPrefs.SetInt(CountKey, n);
        PlayerPrefs.SetInt(BonusKey, PlayerPrefs.GetInt(BonusKey, 0) + coins);
        PlayerPrefs.Save();
        if (n % Every == 0) pending = true;
    }

    // قبل ما نبدأ المرحلة الجاية: لو فيه إعلان ينتظر نعرضه، وبعدين نكمل
    public static void ShowIfDue(Action then)
    {
        if (!pending) { then(); return; }
        pending = false;
        int bonus = PlayerPrefs.GetInt(BonusKey, 0);
        PlayerPrefs.SetInt(BonusKey, 0);
        PlayerPrefs.Save();
        try
        {
            // فيه كوينز تتدبل: إعلان بمكافأة (قبله بطاقة صغيرة تقول المكافأة، وتقدر تتخطاه)
            if (bonus > 0 && rewarded != null && rewarded.IsAdReady())
            {
                AdIntro.Open(bonus, watch =>
                {
                    if (!watch) { then(); return; }
                    rewardBonus = bonus;
                    Show(() => rewarded.ShowAd(), then);
                });
                return;
            }
            if (interstitial != null && interstitial.IsAdReady()) { Show(() => interstitial.ShowAd(), then); return; }
            // ما جهز (ما فيه نت مثلًا): نطلب واحد للمرة الجاية
            if (rewarded != null && !rewarded.IsAdReady()) rewarded.LoadAd();
            if (interstitial != null && !interstitial.IsAdReady()) interstitial.LoadAd();
        }
        catch (Exception e) { Debug.LogWarning(e); }
        then();
    }

    static void Show(Action show, Action then)
    {
        afterAd = then;
        try { show(); }
        catch (Exception e) { Debug.LogWarning(e); Continue(); }
    }
}

// قبل الإعلان بمكافأة: بطاقة تقول كم كوينز تتدبل، وعد تنازلي ثلاث ثواني، وزر تخطي
public class AdIntro : MonoBehaviour
{
    const float Wait = 3f;
    Action<bool> done;
    float age;
    Text countdown;

    public static void Open(int bonus, Action<bool> onDone)
    {
        var go = new GameObject("Ad Intro", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        var intro = go.AddComponent<AdIntro>();
        intro.done = onDone;
        intro.Build((RectTransform)go.transform, bonus);
    }

    void Build(RectTransform root, int bonus)
    {
        Sprite round = MenuUI.RoundedSprite();
        var dim = Box(root, "Dim", new Color(0.08f, 0.06f, 0.12f, 0.85f), null, Vector2.zero, Vector2.zero);
        dim.rectTransform.anchorMin = Vector2.zero; dim.rectTransform.anchorMax = Vector2.one;
        dim.raycastTarget = true;   // ما تقدر ترسم ورا البطاقة
        var card = Box(root, "Card", new Color(1f, 0.97f, 0.9f), round, Vector2.zero, new Vector2(1000f, 600f));
        Label(card.rectTransform, "إعلان قصير", 64, new Vector2(0f, 200f), new Color(1f, 0.47f, 0.56f));
        Label(card.rectTransform, "نشاهده حتى النهاية فتتضاعف كوينز آخر خمس مراحل", 34, new Vector2(0f, 105f), new Color(0.17f, 0.24f, 0.31f));
        var coin = Box(card.rectTransform, "Coin", Color.white, MenuUI.CoinSprite(), new Vector2(150f, 5f), new Vector2(110f, 110f));
        coin.type = Image.Type.Simple;
        Label(card.rectTransform, "", 80, new Vector2(-55f, 5f), new Color(0.93f, 0.6f, 0.15f)).text = "+" + bonus;   // رقم بس: بدون قلب الحروف
        countdown = Label(card.rectTransform, "", 32, new Vector2(0f, -100f), new Color(0.45f, 0.47f, 0.5f));
        // زر التخطي (بدون مضاعفة)
        var skip = Box(card.rectTransform, "Skip", new Color(0.6f, 0.59f, 0.57f), round, new Vector2(0f, -205f), new Vector2(260f, 84f));
        skip.raycastTarget = true;
        Label(skip.rectTransform, "تخطي", 36, new Vector2(0f, 2f), Color.white);
        skip.gameObject.AddComponent<Button>().onClick.AddListener(() => Finish(false));
    }

    void Update()
    {
        age += Time.unscaledDeltaTime;
        int left = Mathf.CeilToInt(Wait - age);
        countdown.text = ArabicText.Fix("يبدأ الإعلان بعد " + Mathf.Max(left, 1));
        if (age >= Wait) Finish(true);
    }

    void Finish(bool watch)
    {
        if (done == null) return;
        Action<bool> d = done;
        done = null;
        Destroy(gameObject);
        d(watch);
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
        t.rectTransform.sizeDelta = new Vector2(900f, 100f);
        return t;
    }
}
