using System.Collections.Generic;
using UnityEngine;

// أصوات خاصة لكل مرحلة (الجزء الثاني): صوت خلفية يدور (بحر، ساعة، مصنع، حفلة...) وأصوات للأحداث.
// كل الأصوات تنصنع بالكود مثل باقي أصوات اللعبة
public static class LevelSfx
{
    const int Rate = 44100;
    static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
    static AudioSource oneShot, ambience;
    static AmbiencePlayer player;

    // ---------- التشغيل ----------

    static void EnsureSources()
    {
        if (oneShot != null) return;
        var go = new GameObject("Level Sfx");
        Object.DontDestroyOnLoad(go);
        oneShot = go.AddComponent<AudioSource>();
        oneShot.playOnAwake = false;
        ambience = go.AddComponent<AudioSource>();
        ambience.loop = true;
        ambience.playOnAwake = false;
        player = go.AddComponent<AmbiencePlayer>();
    }

    public static void Play(string key, float volume = 1f, float pitch = 1f)
    {
        EnsureSources();
        AudioClip c = Get(key, pitch);
        if (c == null) return;
        oneShot.PlayOneShot(c, volume * 0.8f);
    }

    // صوت خلفية المرحلة (null يوقفه). مع أصوات تطلع كل شوي (نوارس، بوق المنارة...)
    public static void SetAmbience(string key)
    {
        EnsureSources();
        player.Configure(key);
        if (string.IsNullOrEmpty(key)) { ambience.Stop(); ambience.clip = null; return; }
        AudioClip c = Get("amb_" + key);
        if (ambience.clip == c && ambience.isPlaying) return;
        ambience.clip = c;
        ambience.volume = 0.32f;
        ambience.Play();
    }

    // مصدر صوت مستمر لشي بالمرحلة (مثل الوتر وهو يهتز)
    public static AudioSource LoopSource(GameObject owner, string key, float volume)
    {
        var s = owner.AddComponent<AudioSource>();
        s.clip = Get(key);
        s.loop = true;
        s.volume = volume;
        s.playOnAwake = false;
        s.spatialBlend = 0f;
        return s;
    }

    static AudioClip Get(string key, float pitch = 1f)
    {
        string id = pitch == 1f ? key : key + "@" + pitch.ToString("0.000");
        if (cache.TryGetValue(id, out AudioClip c)) return c;
        float[] d = Generate(key, pitch);
        if (d == null) return null;
        c = AudioClip.Create(id, d.Length, 1, Rate, false);
        c.SetData(d, 0);
        cache[id] = c;
        return c;
    }

    // لحن تجريبي (نسمع فيه الصوت برا اللعبة): نفس الصوت بنغمات ورا بعض
    public static AudioClip Demo(string key, float[] pitches, float gap)
    {
        var parts = new List<float[]>();
        foreach (float p in pitches) parts.Add(Generate(key, p));
        int len = Mathf.RoundToInt(gap * Rate) * (parts.Count - 1) + parts[parts.Count - 1].Length;
        var mix = new float[len];
        for (int k = 0; k < parts.Count; k++)
        {
            int s = Mathf.RoundToInt(gap * Rate) * k;
            for (int i = 0; i < parts[k].Length && s + i < len; i++) mix[s + i] += parts[k][i] * 0.7f;
        }
        var c = AudioClip.Create("demo", len, 1, Rate, false);
        c.SetData(mix, 0);
        return c;
    }

    // ---------- أدوات ----------

    static float[] Buf(float seconds) => new float[Mathf.RoundToInt(seconds * Rate)];
    static float Noise(System.Random r) => (float)r.NextDouble() * 2f - 1f;

    static float[] Normalize(float[] b, float peak)
    {
        float m = 0f;
        foreach (float v in b) m = Mathf.Max(m, Mathf.Abs(v));
        if (m > 0f) for (int i = 0; i < b.Length; i++) b[i] *= peak / m;
        return b;
    }

    // يدور بسلاسة: نخلط آخره بأوله
    static float[] Loop(float[] b, float fadeSeconds)
    {
        int f = Mathf.RoundToInt(fadeSeconds * Rate);
        var o = new float[b.Length - f];
        for (int i = 0; i < o.Length; i++) o[i] = b[i];
        for (int i = 0; i < f; i++)
        {
            float k = i / (float)f;
            o[i] = b[i] * k + b[o.Length + i] * (1f - k);
        }
        return o;
    }

    static void AddTone(float[] b, float start, float dur, float freq, float vol, float decay, float harm = 0.3f)
    {
        int s = Mathf.RoundToInt(start * Rate), n = Mathf.RoundToInt(dur * Rate);
        for (int i = 0; i < n && s + i < b.Length; i++)
        {
            float t = i / (float)Rate;
            float env = Mathf.Clamp01(t / 0.004f) * Mathf.Exp(-t * decay);
            float w = Mathf.Sin(2f * Mathf.PI * freq * t) + harm * Mathf.Sin(4f * Mathf.PI * freq * t) + harm * 0.4f * Mathf.Sin(6f * Mathf.PI * freq * t);
            b[s + i] += w * env * vol;
        }
    }

    static void AddClick(float[] b, float start, float freq, float vol, System.Random r)
    {
        int s = Mathf.RoundToInt(start * Rate);
        for (int i = 0; i < Rate / 25 && s + i < b.Length; i++)
        {
            float t = i / (float)Rate;
            b[s + i] += (Mathf.Sin(2f * Mathf.PI * freq * t) * 0.7f + Noise(r) * 0.3f) * Mathf.Exp(-t * 90f) * vol;
        }
    }

    // وتر (Karplus-Strong): صوت نقرة وتر طبيعي
    static void AddPluck(float[] b, float start, float freq, float vol, float damp, System.Random r)
    {
        int period = Mathf.Max(2, Mathf.RoundToInt(Rate / freq));
        var ring = new float[period];
        for (int i = 0; i < period; i++) ring[i] = Noise(r);
        int s = Mathf.RoundToInt(start * Rate);
        int idx = 0;
        for (int i = 0; s + i < b.Length; i++)
        {
            int next = (idx + 1) % period;
            float v = ring[idx];
            ring[idx] = (ring[idx] + ring[next]) * 0.5f * damp;
            idx = next;
            b[s + i] += v * vol;
            if (i > Rate * 3) break;
        }
    }

    // ---------- الأصوات ----------

    static float[] Generate(string key, float p)
    {
        var r = new System.Random(key.GetHashCode());
        float[] b;
        switch (key)
        {
            // ===== خلفيات =====
            case "amb_sea":
            {
                // موج يجي ويروح كل 4 ثواني + زبد
                b = Buf(8.6f);
                float lp = 0f, lp2 = 0f;
                for (int i = 0; i < b.Length; i++)
                {
                    float t = i / (float)Rate;
                    float swell = Mathf.Pow(0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 2f / 4f - 1.5f), 2f);
                    float n = Noise(r);
                    lp += (n - lp) * (0.02f + 0.08f * swell);
                    lp2 += (lp - lp2) * 0.3f;
                    b[i] = lp2 * (0.25f + swell) + (n - lp) * 0.04f * swell;
                }
                return Normalize(Loop(b, 0.6f), 0.7f);
            }
            case "amb_underwater":
            {
                b = Buf(6.5f);
                float lp = 0f, lp2 = 0f;
                for (int i = 0; i < b.Length; i++)
                {
                    lp += (Noise(r) - lp) * 0.004f;
                    lp2 += (lp - lp2) * 0.02f;
                    b[i] = lp2 * 30f;
                }
                // فقاعات صغيرة
                for (int k = 0; k < 14; k++)
                {
                    int s = r.Next(b.Length - Rate / 5);
                    float f0 = 300f + (float)r.NextDouble() * 500f;
                    for (int i = 0; i < Rate / 12; i++)
                    {
                        float t = i / (float)Rate;
                        b[s + i] += Mathf.Sin(2f * Mathf.PI * (f0 + t * 6000f) * t) * Mathf.Exp(-t * 40f) * 0.25f;
                    }
                }
                return Normalize(Loop(b, 0.5f), 0.6f);
            }
            case "amb_storm":
            {
                b = Generate("amb_sea", 1f);
                float lp = 0f, lp2 = 0f;
                for (int i = 0; i < b.Length; i++)
                {
                    float t = i / (float)Rate;
                    float gust = 0.5f + 0.5f * Mathf.Sin(t * 1.3f) * Mathf.Sin(t * 0.7f + 1f);
                    lp += (Noise(r) - lp) * (0.04f + 0.05f * gust);
                    lp2 += (lp - lp2) * 0.1f;
                    b[i] = b[i] * 0.8f + (lp - lp2) * gust * 0.9f;
                }
                return Normalize(b, 0.7f);
            }
            case "amb_night_sea":
            {
                b = Generate("amb_sea", 1f);
                for (int i = 0; i < b.Length; i++) b[i] *= 0.6f;
                // صراصير الليل
                for (float t = 0.2f; t < b.Length / (float)Rate - 0.3f; t += 0.9f + (float)r.NextDouble() * 0.6f)
                    for (int c = 0; c < 3; c++) AddTone(b, t + c * 0.06f, 0.04f, 4400f, 0.05f, 60f, 0f);
                return Normalize(b, 0.6f);
            }
            case "amb_classroom":
            {
                // همهمة صف بعيدة + خربشة أقلام + ساعة الحايط
                b = Buf(6.5f);
                float lp = 0f;
                for (int i = 0; i < b.Length; i++)
                {
                    float t = i / (float)Rate;
                    lp += (Noise(r) - lp) * 0.05f;
                    b[i] = lp * (0.25f + 0.15f * Mathf.Sin(t * 2.1f) * Mathf.Sin(t * 0.9f));
                }
                for (float t = 0.1f; t < 6.2f; t += 1f) AddClick(b, t, 2200f, 0.15f, r);
                for (int k = 0; k < 6; k++)
                {
                    int s = r.Next(b.Length - Rate);
                    float lp3 = 0f;
                    for (int i = 0; i < Rate / 3; i++) { lp3 += (Noise(r) - lp3) * 0.6f; b[s + i] += (Noise(r) - lp3) * 0.06f * Mathf.Sin(i / (float)Rate * 60f); }
                }
                return Normalize(Loop(b, 0.4f), 0.5f);
            }
            case "amb_clock":
            {
                // تك تاك واضح كل نص ثانية + طنين خفيف
                b = Buf(4.4f);
                for (int k = 0; k < 8; k++) AddClick(b, 0.1f + k * 0.5f, k % 2 == 0 ? 2600f : 1700f, 0.6f, r);
                for (int i = 0; i < b.Length; i++) b[i] += Mathf.Sin(i / (float)Rate * 2f * Mathf.PI * 60f) * 0.01f;
                return Normalize(Loop(b, 0.3f), 0.6f);
            }
            case "amb_desert":
            {
                b = Buf(6.5f);
                float lp = 0f, lp2 = 0f;
                for (int i = 0; i < b.Length; i++)
                {
                    float t = i / (float)Rate;
                    float swell = 0.5f + 0.5f * Mathf.Sin(t * 0.9f) * Mathf.Sin(t * 0.4f + 2f);
                    lp += (Noise(r) - lp) * (0.03f + 0.05f * swell);
                    lp2 += (lp - lp2) * 0.04f;
                    b[i] = (lp - lp2) * swell + Noise(r) * 0.01f;
                }
                return Normalize(Loop(b, 0.5f), 0.5f);
            }
            case "amb_factory":
            {
                // طنين ماكينة + قعقعة تروس منتظمة
                b = Buf(4.6f);
                for (int i = 0; i < b.Length; i++)
                {
                    float t = i / (float)Rate;
                    b[i] = (Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.15f + Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.07f) * (0.8f + 0.2f * Mathf.Sin(t * 12f));
                }
                for (int k = 0; k < 6; k++)
                {
                    float t0 = 0.2f + k * 0.7f;
                    AddTone(b, t0, 0.3f, 523f, 0.15f, 18f, 0.6f);
                    AddTone(b, t0, 0.3f, 787f, 0.1f, 22f, 0.4f);
                }
                return Normalize(Loop(b, 0.3f), 0.55f);
            }
            case "amb_day":
            {
                // عصافير الصبح
                b = Buf(6.5f);
                float lp = 0f;
                for (int i = 0; i < b.Length; i++) { lp += (Noise(r) - lp) * 0.02f; b[i] = lp * 0.2f; }
                for (int k = 0; k < 10; k++)
                {
                    float t0 = (float)r.NextDouble() * 6f, f0 = 2600f + (float)r.NextDouble() * 1500f;
                    for (int c = 0; c < 3; c++)
                    {
                        int s = Mathf.RoundToInt((t0 + c * 0.11f) * Rate);
                        for (int i = 0; i < Rate / 14 && s + i < b.Length; i++)
                        {
                            float t = i / (float)Rate;
                            b[s + i] += Mathf.Sin(2f * Mathf.PI * (f0 + 900f * Mathf.Sin(t * 70f)) * t) * Mathf.Sin(t / 0.07f * Mathf.PI) * 0.18f;
                        }
                    }
                }
                return Normalize(Loop(b, 0.5f), 0.5f);
            }
            case "amb_forest":
            case "amb_river":
            case "amb_hive":
            case "amb_magic":
            {
                // الغابة: حفيف أوراق (نويز ناعم يتموج)، وأصوات طبيعة حسب المكان
                b = Buf(8.5f);
                float lp = 0f, lp2 = 0f;
                for (int i = 0; i < b.Length; i++)
                {
                    float t = i / (float)Rate;
                    lp += (Noise(r) - lp) * 0.05f;
                    lp2 += (lp - lp2) * 0.3f;
                    float gust = 0.55f + 0.45f * Mathf.Sin(t * 0.7f) * Mathf.Sin(t * 0.31f + 1f);
                    b[i] = (lp - lp2) * 1.4f * gust;
                }
                if (key == "amb_river")
                {
                    // مويه تجري: خرير وفقاعات صغيرة
                    float w = 0f;
                    for (int i = 0; i < b.Length; i++) { w += (Noise(r) - w) * 0.25f; b[i] += w * 0.35f; }
                    for (int k = 0; k < 70; k++)
                    {
                        int s = r.Next(b.Length - Rate / 10);
                        float f0 = 500f + (float)r.NextDouble() * 900f;
                        for (int i = 0; i < Rate / 30; i++)
                        {
                            float t = i / (float)Rate;
                            b[s + i] += Mathf.Sin(2f * Mathf.PI * f0 * (1f + t * 12f) * t) * Mathf.Exp(-t * 90f) * 0.25f;
                        }
                    }
                }
                if (key == "amb_hive")
                {
                    // طنين نحل بعيد يطلع وينزل
                    for (int i = 0; i < b.Length; i++)
                    {
                        float t = i / (float)Rate;
                        float f = 210f + Mathf.Sin(t * 1.3f) * 15f;
                        float saw = Mathf.Repeat(f * t, 1f) * 2f - 1f;
                        b[i] += saw * 0.06f * (0.6f + 0.4f * Mathf.Sin(t * 0.9f));
                    }
                }
                if (key == "amb_magic")
                {
                    // ليل سحري: صراصير ورنات ناعمة
                    for (int i = 0; i < b.Length; i++)
                    {
                        float t = i / (float)Rate;
                        float chirp = Mathf.Repeat(t * 3.1f, 1f) < 0.25f ? 1f : 0f;
                        b[i] += Mathf.Sin(2f * Mathf.PI * 4400f * t) * chirp * Mathf.Abs(Mathf.Sin(t * 120f)) * 0.03f;
                    }
                    for (int k = 0; k < 9; k++)
                        AddTone(b, (float)r.NextDouble() * 7.5f, 1f, 1046f * (k % 3 == 0 ? 1f : k % 3 == 1 ? 1.26f : 1.5f), 0.07f, 3f, 0.1f);
                }
                else
                {
                    // عصافير بعيدة
                    for (int k = 0; k < 6; k++)
                    {
                        float t0 = (float)r.NextDouble() * 7.5f, f0 = 2800f + (float)r.NextDouble() * 1400f;
                        for (int c = 0; c < 2 + r.Next(3); c++)
                        {
                            int s = Mathf.RoundToInt((t0 + c * 0.13f) * Rate);
                            for (int i = 0; i < Rate / 16 && s + i < b.Length; i++)
                            {
                                float t = i / (float)Rate;
                                b[s + i] += Mathf.Sin(2f * Mathf.PI * (f0 + 700f * Mathf.Sin(t * 60f)) * t) * Mathf.Sin(t / 0.0625f * Mathf.PI) * 0.09f;
                            }
                        }
                    }
                }
                return Normalize(Loop(b, 0.5f), 0.45f);
            }
            case "amb_kitchen":
            {
                // مطبخ: طنين ثلاجة، قدر يغلي بعيد، وطقات صحون خفيفة
                b = Buf(7f);
                for (int i = 0; i < b.Length; i++)
                {
                    float t = i / (float)Rate;
                    b[i] = (Mathf.Sin(2f * Mathf.PI * 60f * t) * 0.5f + Mathf.Sin(2f * Mathf.PI * 120f * t) * 0.25f) * 0.08f * (0.8f + 0.2f * Mathf.Sin(t * 0.5f));
                }
                for (int k = 0; k < 90; k++)
                {
                    int s = r.Next(b.Length - Rate / 20);
                    float f0 = 200f + (float)r.NextDouble() * 500f;
                    for (int i = 0; i < Rate / 40; i++) { float t = i / (float)Rate; b[s + i] += Mathf.Sin(2f * Mathf.PI * f0 * (1f + t * 20f) * t) * Mathf.Exp(-t * 120f) * 0.12f; }
                }
                for (int k = 0; k < 4; k++) AddTone(b, (float)r.NextDouble() * 6.5f, 0.4f, 2200f + r.Next(1500), 0.05f, 10f, 0.1f);
                return Normalize(Loop(b, 0.5f), 0.4f);
            }
            case "amb_candy":
            {
                // أرض الحلويات: صندوق موسيقى يعزف لحن ناعم، ولمعات
                b = Buf(6.4f);
                float[] tune = { 1f, 1.26f, 1.498f, 2f, 1.682f, 1.498f, 1.26f, 1.335f, 1.498f, 1.26f, 1.122f, 1f, 1.26f, 1.498f, 1.682f, 2f };
                for (int k = 0; k < tune.Length; k++)
                {
                    AddTone(b, k * 0.4f, 1.2f, 1046f * tune[k] * 0.5f, 0.18f, 4f, 0.05f);
                    AddTone(b, k * 0.4f, 0.8f, 1046f * tune[k], 0.06f, 7f, 0f);
                }
                for (int k = 0; k < 8; k++) AddTone(b, (float)r.NextDouble() * 6f, 0.3f, 3500f + r.Next(1500), 0.04f, 14f, 0f);
                return Normalize(Loop(b, 0.3f), 0.35f);
            }
            case "sizzle":
            {
                b = Buf(1f);
                for (int i = 0; i < b.Length; i++)
                {
                    float t = i / (float)Rate;
                    b[i] = Noise(r) * (r.NextDouble() < 0.06 ? 1f : 0.35f) * Mathf.Clamp01(t * 10f) * Mathf.Clamp01((1f - t) * 3f);
                }
                return Normalize(b, 0.4f);
            }
            case "sneeze":
            {
                // آآآ... تشو!
                b = Buf(0.9f);
                for (int i = 0; i < Rate * 0.55f; i++)
                {
                    float t = i / (float)Rate;
                    b[i] = Mathf.Sin(2f * Mathf.PI * (380f + t * 300f) * t) * Mathf.Clamp01(t * 4f) * 0.35f + Noise(r) * 0.05f;
                }
                for (int i = Mathf.RoundToInt(0.58f * Rate); i < b.Length; i++)
                {
                    float t = i / (float)Rate - 0.58f;
                    b[i] = Noise(r) * Mathf.Exp(-t * 14f) + Mathf.Sin(2f * Mathf.PI * 300f * t) * Mathf.Exp(-t * 20f) * 0.4f;
                }
                return Normalize(b, 0.6f);
            }
            case "toaster":
            {
                b = Buf(0.9f);
                AddClick(b, 0f, 900f, 1f, r);
                for (int i = 0; i < Rate * 0.3f; i++) { float t = i / (float)Rate; b[i] += Mathf.Sin(2f * Mathf.PI * (200f + 600f * t) * t) * Mathf.Exp(-t * 6f) * 0.5f; }
                AddTone(b, 0.15f, 0.7f, 1568f, 0.4f, 4f, 0.2f);
                return Normalize(b, 0.55f);
            }
            case "boil":
            {
                b = Buf(1.5f);
                for (int k = 0; k < 40; k++)
                {
                    int s = r.Next(b.Length - Rate / 10);
                    float f0 = 150f + (float)r.NextDouble() * 350f;
                    for (int i = 0; i < Rate / 20; i++) { float t = i / (float)Rate; b[s + i] += Mathf.Sin(2f * Mathf.PI * f0 * (1f + t * 15f) * t) * Mathf.Exp(-t * 60f) * 0.5f; }
                }
                return Normalize(b, 0.5f);
            }
            case "whistle":
            {
                // إبريق يصفر
                b = Buf(1.3f);
                for (int i = 0; i < b.Length; i++)
                {
                    float t = i / (float)Rate;
                    float f = 1800f + 400f * Mathf.Clamp01(t * 2f) + Mathf.Sin(t * 40f) * 30f;
                    b[i] = (Mathf.Sin(2f * Mathf.PI * f * t) * 0.6f + Noise(r) * 0.15f) * Mathf.Clamp01(t * 3f) * Mathf.Clamp01((1.3f - t) * 4f);
                }
                return Normalize(b, 0.4f);
            }
            case "cold":
            {
                // ريح باردة من الثلاجة
                b = Buf(1.4f);
                float lp = 0f;
                for (int i = 0; i < b.Length; i++)
                {
                    float t = i / (float)Rate;
                    lp += (Noise(r) - lp) * 0.08f;
                    b[i] = lp * 2.2f * Mathf.Sin(Mathf.Clamp01(t / 1.4f) * Mathf.PI);
                }
                for (int k = 0; k < 6; k++) AddTone(b, 0.2f + k * 0.15f, 0.3f, 3500f + k * 300f, 0.05f, 10f, 0f);
                return Normalize(b, 0.45f);
            }
            case "rattle":
            {
                // كور علك تطيح وتتخبط
                b = Buf(1.6f);
                for (int k = 0; k < 45; k++) AddClick(b, (float)r.NextDouble() * 1.4f, 1200f + r.Next(1800), 0.4f + (float)r.NextDouble() * 0.4f, r);
                return Normalize(b, 0.5f);
            }
            case "slurp":
            {
                b = Buf(0.6f);
                float lp = 0f;
                for (int i = 0; i < b.Length; i++)
                {
                    float t = i / (float)Rate;
                    lp += (Noise(r) - lp) * 0.2f;
                    b[i] = (lp + Mathf.Sin(2f * Mathf.PI * (400f - 300f * t) * t) * 0.4f) * Mathf.Sin(t / 0.6f * Mathf.PI);
                }
                return Normalize(b, 0.45f);
            }
            case "wobble":
            {
                // جيلي يتهز
                b = Buf(0.7f);
                for (int i = 0; i < b.Length; i++)
                {
                    float t = i / (float)Rate;
                    b[i] = Mathf.Sin(2f * Mathf.PI * (220f + Mathf.Sin(t * 50f) * 60f) * t) * Mathf.Exp(-t * 5f);
                }
                return Normalize(b, 0.5f);
            }
            case "motor":
            {
                b = Buf(1f);
                for (int i = 0; i < b.Length; i++)
                {
                    float t = i / (float)Rate;
                    float saw = Mathf.Repeat(90f * t, 1f) * 2f - 1f;
                    b[i] = saw * 0.5f + Mathf.Sin(2f * Mathf.PI * 180f * t) * 0.2f;
                }
                return Normalize(Loop(b, 0.1f), 0.25f);
            }
            case "amb_chase":
            {
                // موسيقى مطاردة: أوتار سريعة ونبضة
                b = Buf(4f);
                float[] line = { 1f, 1.189f, 1.335f, 1.498f, 1.335f, 1.189f, 1f, 0.891f, 1f, 1.189f, 1.498f, 1.782f, 1.498f, 1.335f, 1.189f, 1.335f };
                for (int k = 0; k < 32; k++)
                {
                    AddPluck(b, k * 0.125f, 330f * line[k % line.Length], 0.35f, 0.996f, r);
                    if (k % 4 == 0) { AddTone(b, k * 0.125f, 0.2f, 55f, 0.5f, 18f, 0.3f); AddClick(b, k * 0.125f, 120f, 0.5f, r); }
                    if (k % 4 == 2) AddClick(b, k * 0.125f, 3000f, 0.25f, r);
                }
                return Normalize(Loop(b, 0.05f), 0.45f);
            }
            case "faucet":
            {
                // مويه تنزل من الحنفية
                b = Buf(2f);
                float lp = 0f;
                for (int i = 0; i < b.Length; i++)
                {
                    float t = i / (float)Rate;
                    lp += (Noise(r) - lp) * 0.45f;
                    b[i] = lp * (0.7f + 0.3f * Mathf.Sin(t * 37f)) ;
                }
                for (int k = 0; k < 30; k++)
                {
                    int s = r.Next(b.Length - Rate / 20);
                    float f0 = 600f + (float)r.NextDouble() * 900f;
                    for (int i = 0; i < Rate / 50; i++) { float t = i / (float)Rate; b[s + i] += Mathf.Sin(2f * Mathf.PI * f0 * (1f + t * 25f) * t) * Mathf.Exp(-t * 150f) * 0.4f; }
                }
                return Normalize(Loop(b, 0.2f), 0.4f);
            }
            case "mouse":
            {
                // فار يصيح: تيك تيك عالية
                b = Buf(0.45f);
                for (int c = 0; c < 3; c++)
                {
                    int s = Mathf.RoundToInt(c * 0.12f * Rate);
                    for (int i = 0; i < Rate / 14 && s + i < b.Length; i++)
                    {
                        float t = i / (float)Rate;
                        b[s + i] += Mathf.Sin(2f * Mathf.PI * (3200f + 1500f * Mathf.Sin(t * 90f)) * t) * Mathf.Sin(t / 0.07f * Mathf.PI) * 0.6f;
                    }
                }
                return Normalize(b, 0.45f);
            }
            case "fizz":
            {
                // صودا تفور
                b = Buf(2.2f);
                for (int i = 0; i < b.Length; i++)
                {
                    float t = i / (float)Rate;
                    b[i] = Noise(r) * (r.NextDouble() < 0.12 ? 1f : 0.25f) * Mathf.Clamp01(t * 6f) * Mathf.Clamp01((2.2f - t) * 1.5f);
                }
                for (int i = 0; i < Rate * 0.4f; i++) { float t = i / (float)Rate; b[i] += Mathf.Sin(2f * Mathf.PI * (120f + 400f * t) * t) * Mathf.Exp(-t * 5f) * 0.6f; }
                return Normalize(b, 0.55f);
            }
            case "blender":
            {
                b = Buf(1f);
                for (int i = 0; i < b.Length; i++)
                {
                    float t = i / (float)Rate;
                    float saw = Mathf.Repeat(160f * t, 1f) * 2f - 1f;
                    b[i] = saw * 0.4f + Mathf.Sin(2f * Mathf.PI * 320f * t) * 0.25f + Noise(r) * 0.15f;
                }
                return Normalize(Loop(b, 0.08f), 0.35f);
            }
            case "snap":
            {
                // مصيدة تنطبق
                b = Buf(0.4f);
                AddClick(b, 0f, 1500f, 1f, r);
                AddClick(b, 0.01f, 600f, 1f, r);
                AddTone(b, 0f, 0.4f, 180f, 0.6f, 14f, 0.5f);
                return Normalize(b, 0.7f);
            }
            case "bake":
            {
                // الكوكيز ينخبز: فحيح ورنة جرس الفرن
                b = Buf(1f);
                for (int i = 0; i < Rate * 0.5f; i++) b[i] = Noise(r) * 0.25f * Mathf.Sin(i / (Rate * 0.5f) * Mathf.PI);
                AddTone(b, 0.4f, 0.6f, 1760f, 0.5f, 6f, 0.2f);
                AddTone(b, 0.4f, 0.6f, 2637f, 0.2f, 8f, 0f);
                return Normalize(b, 0.5f);
            }
            case "popcorn":
            {
                b = Buf(0.7f);
                for (int k = 0; k < 9; k++) AddClick(b, (float)r.NextDouble() * 0.6f, 700f + r.Next(900), 0.8f, r);
                return Normalize(b, 0.55f);
            }
            case "amb_lab":
            {
                // مختبر: طنين أجهزة، فقاعات دوارق، وصفارات صغيرة
                b = Buf(7f);
                for (int i = 0; i < b.Length; i++)
                {
                    float t = i / (float)Rate;
                    b[i] = (Mathf.Sin(2f * Mathf.PI * 100f * t) * 0.4f + Mathf.Sin(2f * Mathf.PI * 300f * t) * 0.1f) * 0.06f * (0.85f + 0.15f * Mathf.Sin(t * 0.6f));
                }
                for (int k = 0; k < 140; k++)
                {
                    int s = r.Next(b.Length - Rate / 20);
                    float f0 = 300f + (float)r.NextDouble() * 900f;
                    for (int i = 0; i < Rate / 45; i++) { float t = i / (float)Rate; b[s + i] += Mathf.Sin(2f * Mathf.PI * f0 * (1f + t * 30f) * t) * Mathf.Exp(-t * 140f) * 0.14f; }
                }
                for (int k = 0; k < 6; k++) { float t0 = (float)r.NextDouble() * 6.5f; AddTone(b, t0, 0.08f, 1800f + r.Next(1200), 0.08f, 20f, 0f); AddTone(b, t0 + 0.1f, 0.08f, 2400f, 0.06f, 20f, 0f); }
                return Normalize(Loop(b, 0.5f), 0.4f);
            }
            case "crackle":
            {
                // كهربا تمشي بالسلك (تتكرر)
                b = Buf(1f);
                for (int i = 0; i < b.Length; i++)
                {
                    float t = i / (float)Rate;
                    b[i] = Noise(r) * (r.NextDouble() < 0.04 ? 1f : 0.12f) + Mathf.Sin(2f * Mathf.PI * 120f * t) * 0.15f;
                }
                return Normalize(Loop(b, 0.1f), 0.35f);
            }
            case "laserhum":
            {
                b = Buf(1f);
                for (int i = 0; i < b.Length; i++)
                {
                    float t = i / (float)Rate;
                    b[i] = Mathf.Sin(2f * Mathf.PI * 880f * t + Mathf.Sin(2f * Mathf.PI * 6f * t) * 2f) * 0.5f + Mathf.Sin(2f * Mathf.PI * 440f * t) * 0.2f;
                }
                return Normalize(Loop(b, 0.1f), 0.25f);
            }
            case "gloop":
            {
                // سلايم يبلع: غلوب
                b = Buf(0.5f);
                for (int i = 0; i < b.Length; i++)
                {
                    float t = i / (float)Rate;
                    float f = 260f - 180f * t + Mathf.Sin(t * 60f) * 30f;
                    b[i] = Mathf.Sin(2f * Mathf.PI * f * t) * Mathf.Sin(t / 0.5f * Mathf.PI) + Noise(r) * 0.05f;
                }
                return Normalize(b, 0.55f);
            }
            case "tesla":
            {
                // صاعقة: طقطقة قوية مع طنين
                b = Buf(0.8f);
                for (int i = 0; i < b.Length; i++)
                {
                    float t = i / (float)Rate;
                    b[i] = Noise(r) * (r.NextDouble() < 0.15 ? 1f : 0.3f) * Mathf.Exp(-t * 3f) + (Mathf.Repeat(t * 120f, 1f) * 2f - 1f) * 0.3f * Mathf.Exp(-t * 4f);
                }
                return Normalize(b, 0.6f);
            }
            case "pour":
            {
                // سائل يصب بالدورق
                b = Buf(1.5f);
                float lp = 0f;
                for (int i = 0; i < b.Length; i++)
                {
                    float t = i / (float)Rate;
                    lp += (Noise(r) - lp) * 0.3f;
                    b[i] = lp * (0.6f + 0.4f * Mathf.Sin(t * 30f)) * Mathf.Clamp01(t * 5f) * Mathf.Clamp01((1.5f - t) * 3f);
                }
                for (int k = 0; k < 12; k++) AddTone(b, (float)r.NextDouble() * 1.3f, 0.1f, 500f + r.Next(700), 0.2f, 40f, 0f);
                return Normalize(b, 0.5f);
            }
            case "croak":
            {
                // ضفدع: نقيق خشن مرتين
                b = Buf(0.6f);
                for (int c = 0; c < 2; c++)
                {
                    int s = Mathf.RoundToInt(c * 0.22f * Rate);
                    for (int i = 0; i < Rate / 6 && s + i < b.Length; i++)
                    {
                        float t = i / (float)Rate;
                        float f = 140f * p + Mathf.Sin(t * 2f * Mathf.PI * 30f) * 20f;
                        float saw = Mathf.Repeat(f * t, 1f) * 2f - 1f;
                        float pulse = Mathf.Repeat(t * 45f, 1f) < 0.5f ? 1f : 0.4f;
                        b[s + i] += saw * pulse * Mathf.Sin(t / 0.166f * Mathf.PI);
                    }
                }
                return Normalize(b, 0.5f);
            }
            case "rip":
            {
                // الشبكة تنقطع: خيوط تتمزع
                b = Buf(0.5f);
                for (int k = 0; k < 14; k++) AddClick(b, k * 0.025f + (float)r.NextDouble() * 0.01f, 2500f + r.Next(2500), 0.5f, r);
                float lp = 0f;
                for (int i = 0; i < b.Length; i++) { float t = i / (float)Rate; lp += (Noise(r) - lp) * 0.6f; b[i] += lp * Mathf.Exp(-t * 6f) * 0.3f; }
                return Normalize(b, 0.5f);
            }
            case "timber":
            {
                // شجرة تطيح: صرير طويل وبعدين ضربة قوية
                b = Buf(1.8f);
                for (int i = 0; i < Rate * 1.1f; i++)
                {
                    float t = i / (float)Rate;
                    b[i] = Mathf.Sin(2f * Mathf.PI * (60f + t * 40f) * t + Mathf.Sin(t * 23f) * 4f) * (Mathf.Repeat(t * 7f, 1f) < 0.6f ? 0.5f : 0.15f);
                }
                AddTone(b, 1.1f, 0.7f, 55f, 1f, 6f, 0.5f);
                for (int i = Mathf.RoundToInt(1.1f * Rate); i < b.Length; i++) { float t = i / (float)Rate - 1.1f; b[i] += Noise(r) * Mathf.Exp(-t * 9f) * 0.6f; }
                return Normalize(b, 0.7f);
            }
            case "dig":
            {
                b = Buf(0.5f);
                for (int k = 0; k < 6; k++)
                {
                    int s = Mathf.RoundToInt(k * 0.07f * Rate);
                    for (int i = 0; i < Rate / 25 && s + i < b.Length; i++) b[s + i] += Noise(r) * Mathf.Exp(-i / (float)Rate * 60f);
                }
                AddTone(b, 0f, 0.3f, 90f, 0.4f, 12f, 0.2f);
                return Normalize(b, 0.5f);
            }
            case "munch":
            {
                b = Buf(0.6f);
                for (int k = 0; k < 4; k++) { AddClick(b, k * 0.13f, 700f, 0.7f, r); AddTone(b, k * 0.13f, 0.08f, 180f, 0.4f, 30f, 0.3f); }
                return Normalize(b, 0.45f);
            }
            case "rumble":
            {
                // جذع يتدحرج
                b = Buf(1.2f);
                float lp = 0f;
                for (int i = 0; i < b.Length; i++)
                {
                    float t = i / (float)Rate;
                    lp += (Noise(r) - lp) * 0.03f;
                    b[i] = lp * 3f * (0.6f + 0.4f * Mathf.Sin(t * 2f * Mathf.PI * 5f)) * Mathf.Clamp01(t * 6f) * Mathf.Clamp01((1.2f - t) * 4f);
                }
                return Normalize(b, 0.6f);
            }
            case "plop":
            {
                b = Buf(0.35f);
                for (int i = 0; i < b.Length; i++)
                {
                    float t = i / (float)Rate;
                    b[i] = Mathf.Sin(2f * Mathf.PI * (300f + 900f * t) * t) * Mathf.Exp(-t * 14f) + Noise(r) * Mathf.Exp(-t * 40f) * 0.3f;
                }
                return Normalize(b, 0.5f);
            }
            case "chirp":
            {
                // عصفور يغرد: تغريدات قصيرة صاعدة
                b = Buf(0.9f);
                float f0 = 2600f + (float)r.NextDouble() * 1200f;
                int n = 3 + r.Next(4);
                for (int c = 0; c < n; c++)
                {
                    int s = Mathf.RoundToInt(c * 0.12f * Rate);
                    float up = c == n - 1 ? 1.4f : 1f;
                    for (int i = 0; i < Rate / 12 && s + i < b.Length; i++)
                    {
                        float t = i / (float)Rate;
                        float f = f0 * up + 1600f * t / 0.083f * (c % 2 == 0 ? 1f : -0.5f);
                        b[s + i] += Mathf.Sin(2f * Mathf.PI * f * t) * Mathf.Sin(t / 0.083f * Mathf.PI) * 0.6f;
                    }
                }
                return Normalize(b, 0.45f);
            }
            case "woodpecker":
            {
                b = Buf(0.8f);
                for (int k = 0; k < 12; k++) AddClick(b, k * 0.045f, 900f + r.Next(200), 1f - k * 0.04f, r);
                return Normalize(b, 0.5f);
            }
            case "grow":
            {
                // شجرة تطلع: صرير خشب وحفيف أوراق يطلع
                b = Buf(2f);
                float lp = 0f;
                for (int i = 0; i < b.Length; i++)
                {
                    float t = i / (float)Rate;
                    float creak = Mathf.Sin(2f * Mathf.PI * (90f + 60f * t) * t + Mathf.Sin(t * 37f) * 3f) * (Mathf.Repeat(t * 9f, 1f) < 0.5f ? 1f : 0.3f);
                    lp += (Noise(r) - lp) * Mathf.Lerp(0.02f, 0.3f, t / 2f);
                    b[i] = creak * 0.35f * Mathf.Clamp01(1.6f - t) + lp * 0.5f * Mathf.Clamp01(t * 2f) * Mathf.Clamp01((2f - t) * 2f);
                }
                AddTone(b, 1.4f, 0.6f, 784f, 0.25f, 4f, 0.2f);
                AddTone(b, 1.55f, 0.45f, 1175f, 0.2f, 5f, 0.2f);
                return Normalize(b, 0.55f);
            }
            case "boing":
            {
                b = Buf(0.6f);
                for (int i = 0; i < b.Length; i++)
                {
                    float t = i / (float)Rate;
                    float f = 180f + 260f * (1f - Mathf.Exp(-t * 8f)) + Mathf.Sin(t * 70f) * 40f * Mathf.Exp(-t * 4f);
                    b[i] = Mathf.Sin(2f * Mathf.PI * f * t) * Mathf.Exp(-t * 5f);
                }
                return Normalize(b, 0.6f);
            }
            case "pop":
            {
                b = Buf(0.18f);
                for (int i = 0; i < b.Length; i++)
                {
                    float t = i / (float)Rate;
                    b[i] = Mathf.Sin(2f * Mathf.PI * (900f * p - 3000f * t) * t) * Mathf.Exp(-t * 35f) + Noise(r) * Mathf.Exp(-t * 200f) * 0.4f;
                }
                return Normalize(b, 0.55f);
            }
            case "rope":
            {
                // حبل ينرمي وينشد: صفير قصير وطقة
                b = Buf(0.45f);
                float lp = 0f;
                for (int i = 0; i < b.Length; i++)
                {
                    float t = i / (float)Rate;
                    lp += (Noise(r) - lp) * 0.4f;
                    b[i] = lp * Mathf.Sin(Mathf.Clamp01(t / 0.25f) * Mathf.PI) * 0.6f;
                }
                AddClick(b, 0.26f, 600f, 1f, r);
                AddTone(b, 0.26f, 0.19f, 140f, 0.5f, 18f, 0.3f);
                return Normalize(b, 0.55f);
            }
            case "crack":
            {
                b = Buf(0.5f);
                for (int k = 0; k < 5; k++) AddClick(b, k * 0.03f + (float)r.NextDouble() * 0.02f, 400f + r.Next(500), 1f, r);
                AddTone(b, 0f, 0.5f, 70f, 0.5f, 9f, 0.4f);
                return Normalize(b, 0.6f);
            }
            case "bee":
            {
                // طنين نحلة (يتكرر)
                b = Buf(1f);
                for (int i = 0; i < b.Length; i++)
                {
                    float t = i / (float)Rate;
                    float f = 230f * p + Mathf.Sin(t * 2f * Mathf.PI * 6f) * 12f;
                    float saw = Mathf.Repeat(f * t, 1f) * 2f - 1f;
                    b[i] = saw * (0.7f + 0.3f * Mathf.Sin(t * 2f * Mathf.PI * 3f));
                }
                return Normalize(Loop(b, 0.1f), 0.3f);
            }
            case "sticker":
            {
                // جمعتي ملصق: أربع نغمات تطلع ولمعة
                b = Buf(1.1f);
                float[] notes = { 1046f, 1318f, 1568f, 2093f };
                for (int k = 0; k < notes.Length; k++) AddTone(b, k * 0.08f, 0.8f, notes[k], 0.35f, 5f, 0.15f);
                for (int k = 0; k < 6; k++) AddTone(b, 0.35f + k * 0.05f, 0.3f, 3000f + k * 300f, 0.06f, 12f, 0f);
                return Normalize(b, 0.55f);
            }
            case "amb_night":
            {
                b = Buf(6.5f);
                for (float t = 0.1f; t < 6.2f; t += 0.45f + (float)r.NextDouble() * 0.3f)
                    for (int c = 0; c < 3; c++) AddTone(b, t + c * 0.055f, 0.04f, 4300f + (float)r.NextDouble() * 400f, 0.12f, 60f, 0f);
                float lp = 0f;
                for (int i = 0; i < b.Length; i++) { lp += (Noise(r) - lp) * 0.01f; b[i] += lp * 0.3f; }
                return Normalize(Loop(b, 0.5f), 0.45f);
            }
            case "amb_hall":
            {
                // قاعة موسيقى: صدى ناعم ونغمات خفيفة بعيدة
                b = Buf(8.6f);
                float[] notes = { 261.6f, 329.6f, 392f, 523.3f, 440f, 349.2f };
                for (int k = 0; k < 10; k++) AddTone(b, k * 0.8f + 0.1f, 2f, notes[k % notes.Length] * 0.5f, 0.08f, 1.5f, 0.2f);
                float lp = 0f;
                for (int i = 0; i < b.Length; i++) { lp += (Noise(r) - lp) * 0.01f; b[i] += lp * 0.4f; }
                return Normalize(Loop(b, 0.6f), 0.45f);
            }
            case "amb_crowd":
            {
                // جمهور حفلة: همهمة وتصفيق خفيف
                b = Buf(6.5f);
                float lp = 0f;
                for (int i = 0; i < b.Length; i++) { lp += (Noise(r) - lp) * 0.08f; b[i] = lp * (0.5f + 0.2f * Mathf.Sin(i / (float)Rate * 1.7f)); }
                for (int k = 0; k < 60; k++) AddClick(b, (float)r.NextDouble() * 6.3f, 1200f + (float)r.NextDouble() * 1500f, 0.12f, r);
                return Normalize(Loop(b, 0.5f), 0.5f);
            }

            // ===== أحداث =====
            case "gull":
            {
                b = Buf(0.6f);
                for (int i = 0; i < b.Length; i++)
                {
                    float t = i / (float)Rate;
                    float f = 1900f - 900f * t + 250f * Mathf.Sin(t * 45f);
                    b[i] = Mathf.Sin(2f * Mathf.PI * f * t) * Mathf.Sin(Mathf.Clamp01(t / 0.6f) * Mathf.PI) * (0.7f + 0.3f * Mathf.Sin(t * 30f));
                }
                return Normalize(b, 0.4f);
            }
            case "splash":
            {
                b = Buf(0.8f);
                float lp = 0f;
                for (int i = 0; i < b.Length; i++)
                {
                    float t = i / (float)Rate;
                    lp += (Noise(r) - lp) * (0.4f - t * 0.4f);
                    b[i] = lp * Mathf.Exp(-t * 6f) * Mathf.Clamp01(t / 0.01f);
                }
                AddTone(b, 0.05f, 0.2f, 400f, 0.2f, 20f, 0f);
                return Normalize(b, 0.7f);
            }
            case "bubble":
            {
                b = Buf(0.18f);
                for (int i = 0; i < b.Length; i++)
                {
                    float t = i / (float)Rate;
                    b[i] = Mathf.Sin(2f * Mathf.PI * (500f * p + t * 5000f) * t) * Mathf.Exp(-t * 25f);
                }
                return Normalize(b, 0.6f);
            }
            case "zap":
            {
                b = Buf(0.35f);
                for (int i = 0; i < b.Length; i++)
                {
                    float t = i / (float)Rate;
                    b[i] = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * (180f + 40f * Mathf.Sin(t * 300f)) * t)) * Mathf.Exp(-t * 8f) * 0.5f + Noise(r) * 0.2f * Mathf.Exp(-t * 12f);
                }
                return Normalize(b, 0.45f);
            }
            case "squish":
            {
                b = Buf(0.45f);
                float lp = 0f;
                for (int i = 0; i < b.Length; i++)
                {
                    float t = i / (float)Rate;
                    lp += (Noise(r) - lp) * 0.08f;
                    b[i] = lp * Mathf.Exp(-t * 7f) * 2f + Mathf.Sin(2f * Mathf.PI * (220f - t * 300f) * t) * Mathf.Exp(-t * 9f) * 0.5f;
                }
                return Normalize(b, 0.6f);
            }
            case "slap":
            {
                b = Buf(0.4f);
                for (int i = 0; i < b.Length; i++)
                {
                    float t = i / (float)Rate;
                    b[i] = Noise(r) * Mathf.Exp(-t * 40f) + Mathf.Sin(2f * Mathf.PI * (120f - t * 120f) * t) * Mathf.Exp(-t * 10f);
                }
                return Normalize(b, 0.8f);
            }
            case "gulp":
            {
                b = Buf(0.5f);
                AddTone(b, 0f, 0.2f, 180f, 0.6f, 12f, 0.2f);
                AddTone(b, 0.18f, 0.25f, 140f, 0.6f, 12f, 0.2f);
                return Normalize(b, 0.6f);
            }
            case "snore":
            {
                b = Buf(1.4f);
                float lp = 0f;
                for (int i = 0; i < b.Length; i++)
                {
                    float t = i / (float)Rate;
                    lp += (Noise(r) - lp) * 0.05f;
                    b[i] = (lp * 2f + Mathf.Sin(2f * Mathf.PI * 70f * t) * 0.3f) * Mathf.Sin(t / 1.4f * Mathf.PI) * (0.6f + 0.4f * Mathf.Sin(t * 40f));
                }
                return Normalize(b, 0.4f);
            }
            case "swirl":
            {
                b = Buf(1.2f);
                float lp = 0f;
                for (int i = 0; i < b.Length; i++)
                {
                    float t = i / (float)Rate;
                    lp += (Noise(r) - lp) * (0.02f + 0.1f * Mathf.Abs(Mathf.Sin(t * 10f)));
                    b[i] = lp * Mathf.Sin(t / 1.2f * Mathf.PI);
                }
                return Normalize(b, 0.6f);
            }
            case "foghorn":
            {
                b = Buf(2.2f);
                float lp = 0f;
                for (int i = 0; i < b.Length; i++)
                {
                    float t = i / (float)Rate;
                    float saw = (t * 98f + 0.02f * Mathf.Sin(t * 5f)) % 1f * 2f - 1f;
                    lp += (saw - lp) * 0.06f;
                    b[i] = lp * Mathf.Clamp01(t / 0.3f) * Mathf.Clamp01((2.2f - t) / 0.5f);
                }
                return Normalize(b, 0.55f);
            }
            case "crab":
            {
                b = Buf(0.3f);
                AddClick(b, 0f, 3200f, 0.8f, r);
                AddClick(b, 0.09f, 2800f, 0.7f, r);
                return Normalize(b, 0.5f);
            }
            case "clack":
            {
                b = Buf(0.3f);
                AddClick(b, 0f, 900f, 1f, r);
                AddTone(b, 0f, 0.25f, 640f, 0.4f, 25f, 0.5f);
                return Normalize(b, 0.6f);
            }
            case "crayon":
            {
                b = Buf(0.5f);
                float lp = 0f;
                for (int i = 0; i < b.Length; i++)
                {
                    float t = i / (float)Rate;
                    lp += (Noise(r) - lp) * 0.5f;
                    b[i] = (Noise(r) - lp) * (0.5f + 0.5f * Mathf.Sin(t * 70f)) * Mathf.Sin(t / 0.5f * Mathf.PI);
                }
                return Normalize(b, 0.4f);
            }
            case "buzz":
            {
                b = Buf(0.4f);
                for (int i = 0; i < b.Length; i++)
                {
                    float t = i / (float)Rate;
                    b[i] = ((t * 110f) % 1f * 2f - 1f) * 0.5f + ((t * 116f) % 1f * 2f - 1f) * 0.5f;
                    b[i] *= Mathf.Clamp01((0.4f - t) / 0.1f);
                }
                return Normalize(b, 0.4f);
            }
            case "beep":
            {
                b = Buf(0.12f);
                for (int i = 0; i < b.Length; i++)
                {
                    float t = i / (float)Rate;
                    b[i] = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 1100f * p * t)) * Mathf.Clamp01((0.12f - t) / 0.02f);
                }
                return Normalize(b, 0.3f);
            }
            case "success":
            {
                b = Buf(1f);
                AddTone(b, 0f, 0.5f, 523.3f, 0.5f, 6f);
                AddTone(b, 0.12f, 0.5f, 659.3f, 0.5f, 6f);
                AddTone(b, 0.24f, 0.7f, 784f, 0.5f, 5f);
                AddTone(b, 0.36f, 0.7f, 1046.5f, 0.4f, 4f);
                return Normalize(b, 0.55f);
            }
            case "error":
            {
                b = Buf(0.6f);
                AddTone(b, 0f, 0.25f, 220f, 0.6f, 6f, 0.8f);
                AddTone(b, 0.25f, 0.35f, 196f, 0.6f, 6f, 0.8f);
                return Normalize(b, 0.5f);
            }
            case "snip":
            {
                b = Buf(0.35f);
                for (int i = 0; i < b.Length; i++)
                {
                    float t = i / (float)Rate;
                    b[i] = Noise(r) * Mathf.Exp(-t * 60f) + Mathf.Sin(2f * Mathf.PI * 3400f * t) * Mathf.Exp(-t * 18f) * 0.4f;
                }
                return Normalize(b, 0.7f);
            }
            case "grind":
            {
                // البراية تبري: خشخشة خشب متقطعة
                b = Buf(1.2f);
                for (int i = 0; i < b.Length; i++)
                {
                    float t = i / (float)Rate;
                    float chop = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 14f * t);
                    b[i] = Noise(r) * chop * (0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * 180f * t)) * Mathf.Clamp01((1.2f - t) / 0.2f);
                }
                return Normalize(b, 0.45f);
            }
            case "zip":
            {
                // سحاب ينسحب: تكات سريعة ورا بعض
                b = Buf(0.7f);
                for (int k = 0; k < 40; k++) AddClick(b, k * 0.016f, 2500f + k * 40f, 0.6f, r);
                for (int i = 0; i < b.Length; i++) b[i] += Noise(r) * 0.15f * Mathf.Clamp01((0.65f - i / (float)Rate) / 0.1f);
                return Normalize(b, 0.5f);
            }
            case "glue":
            {
                // صمغ يلزق: صوت مطاطي واطي
                b = Buf(0.45f);
                for (int i = 0; i < b.Length; i++)
                {
                    float t = i / (float)Rate;
                    float f = 140f + 260f * t;
                    b[i] = Mathf.Sin(2f * Mathf.PI * f * t + Mathf.Sin(t * 60f) * 2f) * Mathf.Exp(-t * 6f);
                }
                return Normalize(b, 0.5f);
            }
            case "tock":
            {
                b = Buf(0.15f);
                AddTone(b, 0f, 0.15f, 880f * p, 0.6f, 40f, 0f);
                AddClick(b, 0f, 1500f, 0.8f, r);
                return Normalize(b, 0.6f);
            }
            case "clank":
            {
                b = Buf(0.6f);
                AddTone(b, 0f, 0.6f, 523f, 0.5f, 9f, 0.7f);
                AddTone(b, 0f, 0.6f, 811f, 0.35f, 12f, 0.5f);
                AddTone(b, 0f, 0.6f, 1307f, 0.2f, 16f, 0.3f);
                AddClick(b, 0f, 2000f, 0.6f, r);
                return Normalize(b, 0.6f);
            }
            case "tick":
            {
                b = Buf(0.06f);
                AddClick(b, 0f, 2400f * p, 1f, r);
                return Normalize(b, 0.5f);
            }
            case "whoosh":
            {
                b = Buf(0.6f);
                float lp = 0f;
                for (int i = 0; i < b.Length; i++)
                {
                    float t = i / (float)Rate;
                    lp += (Noise(r) - lp) * (0.02f + 0.3f * Mathf.Sin(t / 0.6f * Mathf.PI));
                    b[i] = lp * Mathf.Sin(t / 0.6f * Mathf.PI);
                }
                return Normalize(b, 0.6f);
            }
            case "sand":
            {
                b = Buf(1.5f);
                float lp = 0f;
                for (int i = 0; i < b.Length; i++)
                {
                    float t = i / (float)Rate;
                    lp += (Noise(r) - lp) * 0.3f;
                    b[i] = (Noise(r) - lp * 0.5f) * Mathf.Clamp01(t / 0.1f) * Mathf.Clamp01((1.5f - t) / 0.4f) * (r.NextDouble() < 0.02 ? 2f : 0.5f);
                }
                return Normalize(b, 0.3f);
            }
            case "thud":
            {
                b = Buf(0.5f);
                AddTone(b, 0f, 0.5f, 70f, 1f, 9f, 0.3f);
                for (int i = 0; i < Rate / 20; i++) b[i] += Noise(r) * Mathf.Exp(-i / (float)Rate * 60f) * 0.5f;
                return Normalize(b, 0.7f);
            }
            case "alarm":
            {
                // جرس منبه: دقات سريعة
                b = Buf(1.6f);
                for (int k = 0; k < 24; k++) AddTone(b, k * 0.065f, 0.12f, 1900f + (k % 2) * 120f, 0.5f, 40f, 0.6f);
                return Normalize(b, 0.55f);
            }
            case "meow":
            {
                b = Buf(0.7f);
                for (int i = 0; i < b.Length; i++)
                {
                    float t = i / (float)Rate, k = t / 0.7f;
                    float f = 500f + 450f * Mathf.Sin(k * Mathf.PI) + 20f * Mathf.Sin(t * 35f);
                    b[i] = (Mathf.Sin(2f * Mathf.PI * f * t) + 0.5f * Mathf.Sin(4f * Mathf.PI * f * t) + 0.25f * Mathf.Sin(6f * Mathf.PI * f * t)) * Mathf.Sin(k * Mathf.PI);
                }
                return Normalize(b, 0.45f);
            }
            case "rooster":
            {
                b = Buf(1.3f);
                float[] fs = { 600f, 900f, 1100f, 800f };
                float[] ts = { 0f, 0.15f, 0.35f, 0.9f };
                for (int s = 0; s < 4; s++)
                {
                    float dur = s == 2 ? 0.55f : 0.2f;
                    int st = Mathf.RoundToInt(ts[s] * Rate);
                    for (int i = 0; i < dur * Rate && st + i < b.Length; i++)
                    {
                        float t = i / (float)Rate;
                        float f = fs[s] + 60f * Mathf.Sin(t * 50f);
                        b[st + i] += (Mathf.Sin(2f * Mathf.PI * f * t) + 0.6f * Mathf.Sin(4f * Mathf.PI * f * t)) * Mathf.Sin(t / dur * Mathf.PI);
                    }
                }
                return Normalize(b, 0.45f);
            }
            case "owl":
            {
                b = Buf(1.4f);
                AddTone(b, 0f, 0.4f, 380f, 0.6f, 4f, 0.1f);
                AddTone(b, 0.55f, 0.7f, 360f, 0.6f, 3f, 0.1f);
                return Normalize(b, 0.45f);
            }
            case "piano":
            {
                // بيانو أقرب للحقيقي: ثلاث أوتار متقاربة لكل نغمة (تعطي رنة دافية)، توافقيات تخف بسرعة
                // (الحادة أسرع)، ضربة مطرقة ناعمة بالبداية، وصوت يخف على مرحلتين مثل البيانو
                float dur = 2.2f;
                b = Buf(dur);
                float f = 261.6f * p;
                float[] detune = { 0.9993f, 1f, 1.0008f };
                for (int n = 1; n <= 8; n++)
                {
                    float fn = n * f * Mathf.Sqrt(1f + 0.0004f * n * n);
                    if (fn > 9000f) break;
                    float amp = Mathf.Pow(n, -1.4f) * (n == 2 ? 0.8f : 1f) * Mathf.Lerp(1f, 0.6f, Mathf.Clamp01((f - 300f) / 600f) * (n > 1 ? 1f : 0f));
                    float d = 0.9f + 0.7f * n + f / 500f;
                    foreach (float dt in detune)
                    {
                        float ph = (float)r.NextDouble() * 6.28f;
                        for (int i = 0; i < b.Length; i++)
                        {
                            float t = i / (float)Rate;
                            float env = Mathf.Clamp01(t / 0.003f) * (0.65f * Mathf.Exp(-t * d * 2.2f) + 0.35f * Mathf.Exp(-t * d * 0.35f));
                            b[i] += Mathf.Sin(2f * Mathf.PI * fn * dt * t + ph) * env * amp * 0.33f;
                        }
                    }
                }
                // ضربة المطرقة: نويز ناعم قصير
                float lp = 0f;
                for (int i = 0; i < Rate / 40 && i < b.Length; i++)
                {
                    float t = i / (float)Rate;
                    lp += (Noise(r) - lp) * 0.15f;
                    b[i] += lp * Mathf.Exp(-t * 160f) * 0.5f;
                }
                // نهاية ناعمة
                int fade = Rate / 5;
                for (int i = 0; i < fade; i++) b[b.Length - 1 - i] *= i / (float)fade;
                // صدى خفيف (كأنها بقاعة)
                int echo = Rate * 9 / 100;
                for (int i = echo; i < b.Length; i++) b[i] += b[i - echo] * 0.18f;
                return Normalize(b, 0.55f);
            }
            case "drum":
            {
                b = Buf(0.6f);
                for (int i = 0; i < b.Length; i++)
                {
                    float t = i / (float)Rate;
                    float f = 50f + 110f * Mathf.Exp(-t * 18f);
                    b[i] = Mathf.Sin(2f * Mathf.PI * f * t) * Mathf.Exp(-t * 6f) + Noise(r) * Mathf.Exp(-t * 35f) * 0.4f;
                }
                return Normalize(b, 0.8f);
            }
            case "chime":
            {
                b = Buf(1.2f);
                AddTone(b, 0f, 1.2f, 880f * p, 0.5f, 4f, 0.2f);
                AddTone(b, 0f, 1.2f, 880f * p * 2.76f, 0.15f, 7f, 0f);
                return Normalize(b, 0.5f);
            }
            case "string_buzz":
            {
                // وتر يطن طنين مزعج (لوب): نغمتين قريبات من بعض يتخالفون
                b = Buf(2.2f);
                for (int i = 0; i < b.Length; i++)
                {
                    float t = i / (float)Rate;
                    float a = (t * 98f) % 1f * 2f - 1f, c = (t * 103.5f) % 1f * 2f - 1f;
                    b[i] = (a + c) * 0.5f * (0.7f + 0.3f * Mathf.Sin(t * 2f * Mathf.PI * 7f)) + Noise(r) * 0.05f;
                }
                return Normalize(Loop(b, 0.2f), 0.5f);
            }
            case "pluck":
            {
                b = Buf(2.2f);
                AddPluck(b, 0f, 196f * p, 0.8f, 0.996f, r);
                AddPluck(b, 0.02f, 293.7f * p, 0.4f, 0.995f, r);
                return Normalize(b, 0.6f);
            }
            case "applause":
            {
                b = Buf(2.5f);
                for (int k = 0; k < 220; k++)
                {
                    float t0 = (float)r.NextDouble() * 2.2f;
                    AddClick(b, t0, 900f + (float)r.NextDouble() * 1600f, 0.3f * (1f - t0 / 2.5f), r);
                }
                return Normalize(b, 0.6f);
            }
            case "cuckoo":
            {
                b = Buf(0.9f);
                AddTone(b, 0f, 0.3f, 784f, 0.7f, 6f, 0.15f);
                AddTone(b, 0.32f, 0.45f, 622f, 0.7f, 5f, 0.15f);
                return Normalize(b, 0.55f);
            }
            case "steam":
            {
                b = Buf(1.1f);
                float lp = 0f;
                for (int i = 0; i < b.Length; i++)
                {
                    float t = i / (float)Rate;
                    lp += (Noise(r) - lp) * 0.6f;
                    b[i] = (Noise(r) - lp) * Mathf.Clamp01(t / 0.03f) * Mathf.Exp(-t * 2.5f);
                }
                return Normalize(b, 0.5f);
            }
            case "cymbal":
            {
                b = Buf(1.6f);
                float hp = 0f;
                for (int i = 0; i < b.Length; i++)
                {
                    float t = i / (float)Rate;
                    float n = Noise(r);
                    hp += (n - hp) * 0.2f;
                    b[i] = (n - hp) * Mathf.Exp(-t * 2.8f) + Mathf.Sin(2f * Mathf.PI * 3100f * t) * Mathf.Exp(-t * 4f) * 0.2f;
                }
                return Normalize(b, 0.55f);
            }
            case "shark":
            {
                // دن... دن (موسيقى القرش)
                b = Buf(1.2f);
                AddTone(b, 0f, 0.4f, 82f, 0.8f, 5f, 0.6f);
                AddTone(b, 0.5f, 0.5f, 87f, 0.8f, 4f, 0.6f);
                return Normalize(b, 0.6f);
            }
        }
        return null;
    }
}

// صوت خلفية المرحلة مع أصوات تطلع كل شوي (نوارس، بوق...)
public class AmbiencePlayer : MonoBehaviour
{
    string extraKey;
    string[] extras;   // أكثر من صوت يطلع (نختار واحد عشوائي)
    float minGap, maxGap, nextAt, volume;

    public void Configure(string ambienceKey)
    {
        extraKey = null;
        extras = null;
        switch (ambienceKey)
        {
            case "sea": extraKey = "gull"; minGap = 5f; maxGap = 10f; volume = 0.5f; break;
            case "night_sea": extraKey = "foghorn"; minGap = 9f; maxGap = 14f; volume = 0.45f; break;
            case "storm": extraKey = "swirl"; minGap = 4f; maxGap = 8f; volume = 0.4f; break;
            case "underwater": extraKey = "bubble"; minGap = 1.5f; maxGap = 4f; volume = 0.35f; break;
            case "day": extraKey = null; break;
            case "forest": extras = new[] { "chirp", "chirp", "woodpecker" }; minGap = 3f; maxGap = 7f; volume = 0.35f; break;
            case "river": extras = new[] { "chirp", "croak", "croak", "splash" }; minGap = 4f; maxGap = 8f; volume = 0.3f; break;
            case "hive": extras = new[] { "chirp", "bee" }; minGap = 3f; maxGap = 6f; volume = 0.3f; break;
            case "magic": extras = new[] { "owl", "chime", "croak" }; minGap = 5f; maxGap = 9f; volume = 0.3f; break;
            case "lab": extras = new[] { "beep", "bubble", "zap" }; minGap = 3f; maxGap = 6f; volume = 0.25f; break;
            case "chase": extras = new[] { "mouse" }; minGap = 3f; maxGap = 6f; volume = 0.3f; break;
            case "kitchen": extras = new[] { "clank", "boil", "tick" }; minGap = 4f; maxGap = 8f; volume = 0.25f; break;
            case "candy": extras = new[] { "chime", "pop" }; minGap = 3f; maxGap = 6f; volume = 0.25f; break;
        }
        nextAt = Time.time + Random.Range(2f, 5f);
    }

    void Update()
    {
        if (extras != null) extraKey = extras[Random.Range(0, extras.Length)];
        if (string.IsNullOrEmpty(extraKey) || Time.time < nextAt) return;
        nextAt = Time.time + Random.Range(minGap, maxGap);
        GameManager gm = GameManager.Instance;
        if (gm == null || !gm.IsPlaying) return;
        LevelSfx.Play(extraKey, volume, extraKey == "bubble" || extraKey == "chirp" || extraKey == "chime" ? Random.Range(0.8f, 1.3f) : 1f);
    }
}
