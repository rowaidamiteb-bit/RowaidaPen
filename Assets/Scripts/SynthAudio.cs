using System.Collections.Generic;
using System.IO;
using UnityEngine;

// يصنع كل أصوات اللعبة بالكود (بدون ملفات): موسيقى صندوق موسيقى، خربشة قلم رصاص، خطوات، خسارة، وفوز
public static class SynthAudio
{
    public const int Rate = 44100;

    // ---------- الموسيقى ----------

    // لحن هادي مثل صندوق الموسيقى: لحن + نغمات مكسرة + خلفية ناعمة + صدى. يعيد نفسه بسلاسة
    public static AudioClip Music()
    {
        const float bpm = 70f;
        float beat = 60f / bpm;
        const int bars = 8;
        int length = Mathf.RoundToInt(bars * 4 * beat * Rate);
        var dry = new float[length];

        // الكوردات لكل بار: C, Am, F, G, C, Am, G, C
        int[][] chords =
        {
            new[] { 48, 52, 55 }, new[] { 45, 48, 52 }, new[] { 41, 45, 48 }, new[] { 43, 47, 50 },
            new[] { 48, 52, 55 }, new[] { 45, 48, 52 }, new[] { 43, 47, 50 }, new[] { 48, 52, 55 },
        };

        // اللحن: (البداية بالنبضات، النوتة، المدة)
        float[,] melody =
        {
            { 0, 76, 1 }, { 1, 79, 1 }, { 2, 84, 1.5f }, { 3.5f, 83, 0.5f },
            { 4, 81, 1 }, { 5, 79, 1 }, { 6, 76, 2 },
            { 8, 77, 1 }, { 9, 81, 1 }, { 10, 84, 1 }, { 11, 81, 1 },
            { 12, 79, 1.5f }, { 13.5f, 77, 0.5f }, { 14, 74, 2 },
            { 16, 76, 1 }, { 17, 79, 1 }, { 18, 84, 1 }, { 19, 86, 1 },
            { 20, 88, 1.5f }, { 21.5f, 86, 0.5f }, { 22, 84, 1 }, { 23, 81, 1 },
            { 24, 79, 1 }, { 25, 77, 1 }, { 26, 74, 1 }, { 27, 71, 1 },
            { 28, 72, 3 },
        };

        for (int i = 0; i < melody.GetLength(0); i++)
            Bell(dry, melody[i, 0] * beat, Midi(melody[i, 1]), 0.22f, 2.2f, true);

        for (int b = 0; b < bars; b++)
        {
            int[] c = chords[b];
            int[] pattern = { c[0] + 12, c[1] + 12, c[2] + 12, c[1] + 12, c[0] + 24, c[2] + 12, c[1] + 12, c[2] + 12 };
            for (int k = 0; k < 8; k++)
                Bell(dry, (b * 4 + k * 0.5f) * beat, Midi(pattern[k]), 0.075f, 3.2f, true);
            foreach (int note in c)
                Pad(dry, b * 4 * beat, 4 * beat, Midi(note), 0.045f, true);
        }

        float[] wet = LoopReverb(dry, 0.55f);
        Normalize(wet, 0.75f);
        return Make("Lullaby", wet);
    }

    // نوتة جرس/صندوق موسيقى
    static void Bell(float[] buf, float start, float freq, float amp, float decay, bool wrap)
    {
        int s0 = Mathf.RoundToInt(start * Rate);
        int len = Mathf.RoundToInt(2.5f * Rate);
        float[] partials = { 1f, 2f, 3f, 5.4f };
        float[] amps = { 1f, 0.35f, 0.12f, 0.06f };
        for (int i = 0; i < len; i++)
        {
            float t = i / (float)Rate;
            float env = Mathf.Min(1f, t / 0.004f) * Mathf.Exp(-t * decay);
            if (t > 0.01f && env < 0.0005f) break;
            float v = 0f;
            for (int p = 0; p < partials.Length; p++)
                v += Mathf.Sin(2f * Mathf.PI * freq * partials[p] * t) * amps[p] * Mathf.Exp(-t * decay * p * 0.6f);
            Add(buf, s0 + i, v * env * amp, wrap);
        }
    }

    // نغمة خلفية ناعمة طويلة
    static void Pad(float[] buf, float start, float duration, float freq, float amp, bool wrap)
    {
        int s0 = Mathf.RoundToInt(start * Rate);
        int len = Mathf.RoundToInt((duration + 0.8f) * Rate);
        for (int i = 0; i < len; i++)
        {
            float t = i / (float)Rate;
            float env = Mathf.Clamp01(t / 0.7f) * Mathf.Clamp01((duration + 0.8f - t) / 0.9f);
            float vib = 1f + 0.002f * Mathf.Sin(2f * Mathf.PI * 4.5f * t);
            float v = Mathf.Sin(2f * Mathf.PI * freq * vib * t) + 0.25f * Mathf.Sin(2f * Mathf.PI * freq * 2.003f * t);
            Add(buf, s0 + i, v * env * amp, wrap);
        }
    }

    static void Add(float[] buf, int i, float v, bool wrap)
    {
        if (wrap) buf[((i % buf.Length) + buf.Length) % buf.Length] += v;
        else if (i >= 0 && i < buf.Length) buf[i] += v;
    }

    static float Midi(float note) => 440f * Mathf.Pow(2f, (note - 69f) / 12f);

    // صدى (Schroeder reverb). نمر على الصوت مرتين عشان ذيل الصدى يلف لبداية اللوب
    static float[] LoopReverb(float[] dry, float mix)
    {
        int n = dry.Length;
        int[] combDelays = { 1557, 1617, 1491, 1422 };
        int[] apDelays = { 225, 556 };
        var combs = new float[combDelays.Length][];
        var combIdx = new int[combDelays.Length];
        for (int c = 0; c < combs.Length; c++) combs[c] = new float[combDelays[c] * 2];
        var aps = new float[apDelays.Length][];
        var apIdx = new int[apDelays.Length];
        for (int a = 0; a < aps.Length; a++) aps[a] = new float[apDelays[a]];

        var outBuf = new float[n];
        for (int pass = 0; pass < 2; pass++)
            for (int i = 0; i < n; i++)
            {
                float x = dry[i];
                float acc = 0f;
                for (int c = 0; c < combs.Length; c++)
                {
                    float[] cb = combs[c];
                    int len = combDelays[c] * 2;
                    float y = cb[combIdx[c]];
                    cb[combIdx[c]] = x + y * 0.84f;
                    combIdx[c] = (combIdx[c] + 1) % len;
                    acc += y;
                }
                acc *= 0.25f;
                for (int a = 0; a < aps.Length; a++)
                {
                    float[] ab = aps[a];
                    float bufOut = ab[apIdx[a]];
                    float y = -acc + bufOut;
                    ab[apIdx[a]] = acc + bufOut * 0.5f;
                    apIdx[a] = (apIdx[a] + 1) % ab.Length;
                    acc = y;
                }
                if (pass == 1) outBuf[i] = x + acc * mix;
            }
        return outBuf;
    }

    // ---------- المؤثرات ----------

    // خربشة قلم رصاص على ورق (لوب)
    public static AudioClip PencilScratch()
    {
        int n = Mathf.RoundToInt(1.6f * Rate);
        var buf = new float[n];
        var rng = new System.Random(4);
        var bp = Biquad.BandPass(3200f, 0.7f);
        var hp = Biquad.HighPass(900f, 0.7f);
        float grain = 0f, grainTarget = 1f;
        for (int i = 0; i < n; i++)
        {
            if (i % 600 == 0) grainTarget = 0.35f + (float)rng.NextDouble() * 0.9f;
            grain += (grainTarget - grain) * 0.004f;
            float noise = (float)rng.NextDouble() * 2f - 1f;
            float flutter = 0.7f + 0.3f * Mathf.Sin(i * 2f * Mathf.PI * 38f / Rate);
            float click = rng.NextDouble() < 0.0015 ? ((float)rng.NextDouble() * 2f - 1f) * 3f : 0f;
            buf[i] = hp.Process(bp.Process(noise + click)) * grain * flutter;
        }
        buf = CrossfadeLoop(buf, Mathf.RoundToInt(0.08f * Rate));
        Normalize(buf, 0.7f);
        return Make("Pencil Scratch", buf);
    }

    // خطوة خفيفة على ورق
    public static AudioClip Footstep(int variant)
    {
        int n = Mathf.RoundToInt(0.16f * Rate);
        var buf = new float[n];
        var rng = new System.Random(100 + variant);
        float lp = 0f;
        float cutoff = 0.12f + variant * 0.03f;
        float thumpFreq = 95f + variant * 12f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Rate;
            float noise = (float)rng.NextDouble() * 2f - 1f;
            lp += (noise - lp) * cutoff;
            float env = Mathf.Min(1f, t / 0.002f) * Mathf.Exp(-t * 48f);
            float thump = Mathf.Sin(2f * Mathf.PI * thumpFreq * t) * Mathf.Exp(-t * 35f);
            buf[i] = (lp * 1.4f + thump * 0.6f) * env;
        }
        Normalize(buf, 0.6f);
        return Make("Footstep " + variant, buf);
    }

    // خسارة: هواء طيحة + نغمات نازلة
    public static AudioClip Lose()
    {
        int n = Mathf.RoundToInt(2f * Rate);
        var buf = new float[n];
        var rng = new System.Random(9);
        float lp = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Rate;
            float cutoff = Mathf.Lerp(0.25f, 0.02f, Mathf.Clamp01(t / 0.9f));
            lp += (((float)rng.NextDouble() * 2f - 1f) - lp) * cutoff;
            float env = Mathf.Clamp01(t / 0.05f) * Mathf.Exp(-t * 2.5f);
            buf[i] = lp * env * 0.5f;
        }
        float[] notes = { 79, 75, 72, 67 };
        for (int k = 0; k < notes.Length; k++)
            Bell(buf, 0.15f + k * 0.28f, Midi(notes[k]), k == notes.Length - 1 ? 0.45f : 0.32f, k == notes.Length - 1 ? 1.6f : 3.5f, false);
        Normalize(buf, 0.75f);
        return Make("Lose", buf);
    }

    // فوز: نغمات طالعة + لمعات
    public static AudioClip Win()
    {
        int n = Mathf.RoundToInt(2.6f * Rate);
        var buf = new float[n];
        float[] notes = { 72, 76, 79, 84, 88 };
        for (int k = 0; k < notes.Length; k++)
            Bell(buf, k * 0.11f, Midi(notes[k]), 0.3f, k == notes.Length - 1 ? 1.4f : 2.8f, false);
        Bell(buf, 0.55f, Midi(84), 0.2f, 1.6f, false);
        Bell(buf, 0.55f, Midi(79), 0.18f, 1.6f, false);
        var rng = new System.Random(3);
        for (int k = 0; k < 9; k++)
            Bell(buf, 0.5f + (float)rng.NextDouble() * 1.2f, Midi(96 + rng.Next(8)), 0.07f, 6f, false);
        Normalize(buf, 0.75f);
        return Make("Win", buf);
    }

    // الدفتر يطيح على الأرض: خبطة ناعمة + حفيف ورق
    public static AudioClip BookFlop()
    {
        int n = Mathf.RoundToInt(0.7f * Rate);
        var buf = new float[n];
        var rng = new System.Random(12);
        var bp = Biquad.BandPass(2500f, 0.9f);
        float lp = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Rate;
            float noise = (float)rng.NextDouble() * 2f - 1f;
            lp += (noise - lp) * 0.08f;
            float thump = (Mathf.Sin(2f * Mathf.PI * 70f * t) * 0.8f + lp * 1.5f) * Mathf.Exp(-t * 22f);
            float rustle = bp.Process(noise) * Mathf.Exp(-t * 7f) * (0.6f + 0.4f * Mathf.PerlinNoise(t * 60f, 0f));
            buf[i] = thump + rustle * 1.4f;
        }
        Normalize(buf, 0.7f);
        return Make("Book Flop", buf);
    }

    // سحر: نغمات لامعة طالعة + هواء يعلى (للدخول داخل الدفتر)
    public static AudioClip Magic()
    {
        int n = Mathf.RoundToInt(4f * Rate);
        var buf = new float[n];
        var rng = new System.Random(8);
        float lp = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Rate;
            float cutoff = Mathf.Lerp(0.01f, 0.3f, Mathf.Clamp01(t / 3.2f));
            lp += (((float)rng.NextDouble() * 2f - 1f) - lp) * cutoff;
            float env = Mathf.Clamp01(t / 2.5f) * Mathf.Clamp01((4f - t) / 0.8f);
            buf[i] = lp * env * 0.35f;
        }
        float[] notes = { 72, 76, 79, 83, 84, 88, 91, 95, 96 };
        for (int k = 0; k < notes.Length; k++)
            Bell(buf, 0.3f + k * 0.3f, Midi(notes[k]), 0.16f, 2.2f, false);
        for (int k = 0; k < 14; k++)
            Bell(buf, 1.2f + (float)rng.NextDouble() * 2.4f, Midi(96 + rng.Next(10)), 0.06f, 5f, false);
        Normalize(buf, 0.7f);
        return Make("Magic", buf);
    }

    // صوت مطر مستمر (لوب): هسهسة ناعمة + نقرات قطرات
    public static AudioClip Rain()
    {
        int n = Mathf.RoundToInt(4.2f * Rate);
        var buf = new float[n];
        var rng = new System.Random(31);
        var hp = Biquad.HighPass(400f, 0.7f);
        var bp = Biquad.BandPass(5000f, 1.2f);
        float lp = 0f, slow = 0f;
        for (int i = 0; i < n; i++)
        {
            float noise = (float)rng.NextDouble() * 2f - 1f;
            lp += (noise - lp) * 0.35f;
            slow += (((float)rng.NextDouble() * 2f - 1f) - slow) * 0.0004f;
            float hiss = hp.Process(lp) * (0.8f + slow * 3f);
            float drop = rng.NextDouble() < 0.004 ? ((float)rng.NextDouble() * 2f - 1f) * 4f : 0f;
            buf[i] = hiss * 0.6f + bp.Process(drop) * 0.9f;
        }
        buf = CrossfadeLoop(buf, Mathf.RoundToInt(0.2f * Rate));
        Normalize(buf, 0.6f);
        return Make("Rain", buf);
    }

    // رعد بعيد: هدير منخفض يعلى وينزل
    public static AudioClip Thunder()
    {
        int n = Mathf.RoundToInt(4f * Rate);
        var buf = new float[n];
        var rng = new System.Random(66);
        float lp1 = 0f, lp2 = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Rate;
            float noise = (float)rng.NextDouble() * 2f - 1f;
            lp1 += (noise - lp1) * 0.02f;
            lp2 += (lp1 - lp2) * 0.05f;
            float crack = Mathf.Exp(-t * 9f) * noise * 0.25f;
            float rumble = Mathf.Clamp01(t / 0.25f) * Mathf.Exp(-t * 0.9f) * (0.7f + 0.3f * Mathf.Sin(t * 7f + Mathf.Sin(t * 2.3f) * 3f));
            buf[i] = lp2 * 8f * rumble + crack;
        }
        Normalize(buf, 0.8f);
        return Make("Thunder", buf);
    }

    // صوت ريح مستمر (لوب): هوا يعلى وينزل
    public static AudioClip Wind()
    {
        int n = Mathf.RoundToInt(6f * Rate);
        var buf = new float[n];
        var rng = new System.Random(47);
        float lp = 0f, lp2 = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Rate;
            float noise = (float)rng.NextDouble() * 2f - 1f;
            float swell = 0.6f + 0.4f * Mathf.Sin(t * Mathf.PI * 2f / 3f) * Mathf.Sin(t * Mathf.PI * 2f / 6f + 1f);
            float cutoff = 0.03f + 0.05f * swell;
            lp += (noise - lp) * cutoff;
            lp2 += (lp - lp2) * cutoff;
            buf[i] = (lp - lp2 * 0.6f) * swell;
        }
        buf = CrossfadeLoop(buf, Mathf.RoundToInt(0.3f * Rate));
        Normalize(buf, 0.6f);
        return Make("Wind", buf);
    }

    // صوت وحش صغير: صفرة تطلع وتنزل
    public static AudioClip Squeak()
    {
        int n = Mathf.RoundToInt(0.35f * Rate);
        var buf = new float[n];
        float ph = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Rate;
            float k = t / 0.35f;
            float f = 520f + 380f * Mathf.Sin(k * Mathf.PI) + 60f * Mathf.Sin(t * 40f);
            ph += 2f * Mathf.PI * f / Rate;
            float env = Mathf.Clamp01(t / 0.02f) * Mathf.Clamp01((0.35f - t) / 0.08f);
            buf[i] = (Mathf.Sin(ph) + 0.3f * Mathf.Sin(ph * 2f)) * env;
        }
        Normalize(buf, 0.6f);
        return Make("Squeak", buf);
    }

    // مسدس ليزر فضائي: "بيو" نغمة تنزل بسرعة
    public static AudioClip Laser()
    {
        int n = Mathf.RoundToInt(0.22f * Rate);
        var buf = new float[n];
        float ph = 0f, ph2 = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Rate;
            float k = t / 0.22f;
            float f = Mathf.Lerp(2200f, 260f, Mathf.Pow(k, 0.55f));
            ph += 2f * Mathf.PI * f / Rate;
            ph2 += 2f * Mathf.PI * f * 1.5f / Rate;
            float square = Mathf.Sign(Mathf.Sin(ph)) * 0.35f + Mathf.Sin(ph) * 0.5f + Mathf.Sin(ph2) * 0.2f;
            float env = Mathf.Clamp01(t / 0.004f) * Mathf.Pow(1f - k, 1.5f);
            buf[i] = square * env;
        }
        Normalize(buf, 0.5f);
        return Make("Laser", buf);
    }

    // ضغطة زر: نقرة قلم خفيفة على الورق
    public static AudioClip Click()
    {
        int n = Mathf.RoundToInt(0.07f * Rate);
        var buf = new float[n];
        var rng = new System.Random(5);
        float ph = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Rate;
            ph += 2f * Mathf.PI * Mathf.Lerp(1400f, 700f, t / 0.07f) / Rate;
            float env = Mathf.Exp(-t * 70f);
            buf[i] = (Mathf.Sin(ph) * 0.7f + ((float)rng.NextDouble() * 2f - 1f) * 0.3f) * env;
        }
        Normalize(buf, 0.5f);
        return Make("Click", buf);
    }

    // نجمة: رنة جرس صافية (pitch يعلى مع كل نجمة)
    public static AudioClip Ding(float pitch)
    {
        int n = Mathf.RoundToInt(0.9f * Rate);
        var buf = new float[n];
        float f = 880f * pitch;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Rate;
            float env = Mathf.Clamp01(t / 0.004f) * Mathf.Exp(-t * 5f);
            buf[i] = (Mathf.Sin(2f * Mathf.PI * f * t) + 0.45f * Mathf.Sin(2f * Mathf.PI * f * 2.01f * t) * Mathf.Exp(-t * 6f)
                    + 0.2f * Mathf.Sin(2f * Mathf.PI * f * 3f * t) * Mathf.Exp(-t * 12f)) * env;
        }
        Normalize(buf, 0.55f);
        return Make("Ding", buf);
    }

    // صفحة تنقلب (للانتقال بين المراحل): خشخشة ورق تطلع وتنزل
    public static AudioClip PageTurn()
    {
        int n = Mathf.RoundToInt(0.5f * Rate);
        var buf = new float[n];
        var rng = new System.Random(23);
        var bp = Biquad.BandPass(2400f, 0.7f);
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Rate;
            float k = t / 0.5f;
            float env = Mathf.Sin(k * Mathf.PI) * (0.6f + 0.4f * Mathf.PerlinNoise(t * 60f, 0.3f));
            buf[i] = bp.Process((float)rng.NextDouble() * 2f - 1f) * env;
        }
        Normalize(buf, 0.5f);
        return Make("Page Turn", buf);
    }

    // قضمة: خبطة قصيرة
    public static AudioClip Chomp()
    {
        int n = Mathf.RoundToInt(0.12f * Rate);
        var buf = new float[n];
        var rng = new System.Random(19);
        float lp = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Rate;
            lp += (((float)rng.NextDouble() * 2f - 1f) - lp) * 0.25f;
            buf[i] = (lp + Mathf.Sin(2f * Mathf.PI * 180f * t) * 0.6f) * Mathf.Exp(-t * 40f);
        }
        Normalize(buf, 0.6f);
        return Make("Chomp", buf);
    }

    // بوينق: لما رويدا تصدم وحش
    public static AudioClip Boing()
    {
        int n = Mathf.RoundToInt(0.45f * Rate);
        var buf = new float[n];
        float ph = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Rate;
            float f = 180f + 260f * Mathf.Exp(-t * 6f) * (1f + 0.3f * Mathf.Sin(t * 70f));
            ph += 2f * Mathf.PI * f / Rate;
            buf[i] = Mathf.Sin(ph) * Mathf.Exp(-t * 6f);
        }
        Normalize(buf, 0.6f);
        return Make("Boing", buf);
    }

    // بوف: الوحش يختفي بسحابة
    public static AudioClip Poof()
    {
        int n = Mathf.RoundToInt(0.6f * Rate);
        var buf = new float[n];
        var rng = new System.Random(23);
        float lp = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Rate;
            lp += (((float)rng.NextDouble() * 2f - 1f) - lp) * Mathf.Lerp(0.5f, 0.05f, t / 0.6f);
            buf[i] = lp * Mathf.Clamp01(t / 0.01f) * Mathf.Exp(-t * 7f);
        }
        Bell(buf, 0.05f, Midi(84), 0.25f, 6f, false);
        Bell(buf, 0.12f, Midi(91), 0.2f, 6f, false);
        Normalize(buf, 0.65f);
        return Make("Poof", buf);
    }

    // ---------- أدوات ----------

    // نخلط آخر الصوت ببدايته ونقصه، عشان اللوب ما يطقطق عند الرجوع للبداية
    static float[] CrossfadeLoop(float[] buf, int fade)
    {
        int n = buf.Length - fade;
        var result = new float[n];
        System.Array.Copy(buf, result, n);
        for (int i = 0; i < fade; i++)
        {
            float k = i / (float)fade;
            result[i] = buf[i] * k + buf[n + i] * (1f - k);
        }
        return result;
    }

    static void Normalize(float[] buf, float peak)
    {
        float max = 1e-6f;
        foreach (float v in buf) max = Mathf.Max(max, Mathf.Abs(v));
        float k = peak / max;
        for (int i = 0; i < buf.Length; i++) buf[i] *= k;
    }

    static AudioClip Make(string name, float[] data)
    {
        var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // يحفظ الصوت كملف WAV (للتجربة)
    public static void SaveWav(AudioClip clip, string path)
    {
        var data = new float[clip.samples * clip.channels];
        clip.GetData(data, 0);
        using (var w = new BinaryWriter(File.Create(path)))
        {
            int bytes = data.Length * 2;
            w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + bytes);
            w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); w.Write(16); w.Write((short)1);
            w.Write((short)clip.channels); w.Write(clip.frequency); w.Write(clip.frequency * clip.channels * 2);
            w.Write((short)(clip.channels * 2)); w.Write((short)16);
            w.Write(System.Text.Encoding.ASCII.GetBytes("data")); w.Write(bytes);
            foreach (float v in data) w.Write((short)(Mathf.Clamp(v, -1f, 1f) * 32767));
        }
    }

    // فلتر صوت بسيط (RBJ biquad)
    class Biquad
    {
        float b0, b1, b2, a1, a2, x1, x2, y1, y2;

        public static Biquad BandPass(float f, float q) => Make(f, q, true);
        public static Biquad HighPass(float f, float q) => Make(f, q, false);

        static Biquad Make(float f, float q, bool bandPass)
        {
            float w0 = 2f * Mathf.PI * f / Rate, alpha = Mathf.Sin(w0) / (2f * q), cos = Mathf.Cos(w0);
            float a0 = 1f + alpha;
            var bq = new Biquad { a1 = -2f * cos / a0, a2 = (1f - alpha) / a0 };
            if (bandPass) { bq.b0 = alpha / a0; bq.b1 = 0f; bq.b2 = -alpha / a0; }
            else { bq.b0 = (1f + cos) / 2f / a0; bq.b1 = -(1f + cos) / a0; bq.b2 = (1f + cos) / 2f / a0; }
            return bq;
        }

        public float Process(float x)
        {
            float y = b0 * x + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2;
            x2 = x1; x1 = x; y2 = y1; y1 = y;
            return y;
        }
    }
}
