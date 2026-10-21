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
                b = Buf(1.4f);
                float f = 261.6f * p;
                AddTone(b, 0f, 1.4f, f, 0.6f, 3f, 0.45f);
                AddTone(b, 0f, 1.4f, f * 2.003f, 0.15f, 5f, 0f);
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
    float minGap, maxGap, nextAt, volume;

    public void Configure(string ambienceKey)
    {
        extraKey = null;
        switch (ambienceKey)
        {
            case "sea": extraKey = "gull"; minGap = 5f; maxGap = 10f; volume = 0.5f; break;
            case "night_sea": extraKey = "foghorn"; minGap = 9f; maxGap = 14f; volume = 0.45f; break;
            case "storm": extraKey = "swirl"; minGap = 4f; maxGap = 8f; volume = 0.4f; break;
            case "underwater": extraKey = "bubble"; minGap = 1.5f; maxGap = 4f; volume = 0.35f; break;
            case "day": extraKey = null; break;
        }
        nextAt = Time.time + Random.Range(2f, 5f);
    }

    void Update()
    {
        if (string.IsNullOrEmpty(extraKey) || Time.time < nextAt) return;
        nextAt = Time.time + Random.Range(minGap, maxGap);
        GameManager gm = GameManager.Instance;
        if (gm == null || !gm.IsPlaying) return;
        LevelSfx.Play(extraKey, volume, extraKey == "bubble" ? Random.Range(0.8f, 1.4f) : 1f);
    }
}
