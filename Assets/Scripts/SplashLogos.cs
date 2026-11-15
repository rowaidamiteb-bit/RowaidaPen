using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// شعارات قبل اللعبة: كل صورة في Resources/Splash تطلع بالترتيب (حسب اسم الملف: 1_... ثم 2_...)
// على خلفية سودا، تظهر بهدوء وتبقى شوي وتختفي، وبعد آخر شعار تختفي الخلفية وتبان القائمة.
// لمس الشاشة يتخطى الشعار الحالي
public class SplashLogos : MonoBehaviour, IPointerDownHandler
{
    public static bool Skip;   // أداة التصوير بالمحرر تتخطاها
    static readonly Color Back = new Color(9f / 255f, 9f / 255f, 10f / 255f);

    Image logo;
    CanvasGroup all, logoGroup;
    bool tapped;

    public static void Play(Action done)
    {
        Sprite[] sprites = Resources.LoadAll<Sprite>("Splash");
        if (Skip || sprites == null || sprites.Length == 0) { done?.Invoke(); return; }
        Array.Sort(sprites, (a, b) => string.CompareOrdinal(a.name, b.name));

        var go = new GameObject("Splash Logos", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;   // فوق القوائم وكل شي
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        bg.transform.SetParent(go.transform, false);
        bg.color = Back;
        bg.raycastTarget = true;
        var bgRt = bg.rectTransform;
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;

        var holder = new GameObject("Logo", typeof(RectTransform), typeof(CanvasGroup)).GetComponent<RectTransform>();
        holder.SetParent(go.transform, false);
        holder.sizeDelta = new Vector2(1000f, 1000f);
        var img = new GameObject("Image", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        img.transform.SetParent(holder, false);
        img.preserveAspect = true;
        img.raycastTarget = false;
        var imgRt = img.rectTransform;
        imgRt.anchorMin = Vector2.zero;
        imgRt.anchorMax = Vector2.one;
        imgRt.offsetMin = imgRt.offsetMax = Vector2.zero;

        var s = bg.gameObject.AddComponent<SplashLogos>();
        s.logo = img;
        s.all = go.GetComponent<CanvasGroup>();
        s.logoGroup = holder.GetComponent<CanvasGroup>();
        s.StartCoroutine(s.Run(sprites, done));
    }

    IEnumerator Run(Sprite[] sprites, Action done)
    {
        foreach (Sprite sp in sprites)
        {
            logo.sprite = sp;
            tapped = false;
            yield return Fade(logoGroup, 0f, 1f, 0.6f);
            for (float t = 0f; t < 1.7f && !tapped && !Skip; t += Time.unscaledDeltaTime) yield return null;
            yield return Fade(logoGroup, 1f, 0f, 0.45f);
            if (Skip) break;
        }
        yield return Fade(all, 1f, 0f, 0.4f);
        Destroy(all.gameObject);
        done?.Invoke();
    }

    IEnumerator Fade(CanvasGroup g, float from, float to, float time)
    {
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / time)
        {
            if (Skip) break;
            g.alpha = Mathf.Lerp(from, to, t * t * (3f - 2f * t));
            yield return null;
        }
        g.alpha = to;
    }

    public void OnPointerDown(PointerEventData e) => tapped = true;
}
