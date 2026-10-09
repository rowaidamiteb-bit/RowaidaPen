using UnityEngine;

// يشغل أصوات اللعبة: الموسيقى، القلم وهو يرسم، الخطوات، الخسارة، والفوز
public class SoundManager : MonoBehaviour
{
    [Range(0f, 1f)] public float musicVolume = 0.32f;
    [Range(0f, 1f)] public float pencilVolume = 0.55f;
    [Range(0f, 1f)] public float stepVolume = 0.22f;
    [Range(0f, 1f)] public float effectsVolume = 0.7f;

    public AudioClip MusicClip { get; private set; }
    public AudioClip PencilClip { get; private set; }
    public AudioClip LoseClip { get; private set; }
    public AudioClip WinClip { get; private set; }
    public AudioClip[] StepClips { get; private set; }

    AudioSource music, pencil, steps, effects;
    float duckTimer;
    float pencilLevel;
    int lastStep = -1;

    public void Build()
    {
        MusicClip = SynthAudio.Music();
        PencilClip = SynthAudio.PencilScratch();
        LoseClip = SynthAudio.Lose();
        WinClip = SynthAudio.Win();
        StepClips = new[] { SynthAudio.Footstep(0), SynthAudio.Footstep(1), SynthAudio.Footstep(2), SynthAudio.Footstep(3) };

        music = NewSource("Music", MusicClip, true);
        music.volume = musicVolume;
        music.Play();

        pencil = NewSource("Pencil", PencilClip, true);
        pencil.volume = 0f;

        steps = NewSource("Steps", null, false);
        effects = NewSource("Effects", null, false);
    }

    AudioSource NewSource(string name, AudioClip clip, bool loop)
    {
        var go = new GameObject("Sound - " + name);
        go.transform.SetParent(transform, false);
        var s = go.AddComponent<AudioSource>();
        s.clip = clip;
        s.loop = loop;
        s.playOnAwake = false;
        s.spatialBlend = 0f;
        return s;
    }

    void Update()
    {
        // نخفض الموسيقى شوي وقت صوت الفوز أو الخسارة
        duckTimer -= Time.deltaTime;
        float targetMusic = duckTimer > 0f ? musicVolume * 0.35f : musicVolume;
        music.volume = Mathf.MoveTowards(music.volume, targetMusic, Time.deltaTime * 0.6f);
    }

    // speed: سرعة الرسم بالمتر في الثانية (0 = القلم واقف)
    public void SetPencil(bool drawing, float speed)
    {
        float target = drawing ? Mathf.Clamp01(0.25f + speed / 8f) : 0f;
        pencilLevel = Mathf.MoveTowards(pencilLevel, target, Time.deltaTime * (target > pencilLevel ? 12f : 6f));
        pencil.volume = pencilLevel * pencilVolume;
        pencil.pitch = 0.85f + Mathf.Clamp(speed, 0f, 12f) * 0.03f;

        if (pencilLevel > 0.001f && !pencil.isPlaying)
        {
            pencil.time = Random.Range(0f, PencilClip.length * 0.9f);
            pencil.Play();
        }
        else if (pencilLevel <= 0.001f && pencil.isPlaying)
        {
            pencil.Stop();
        }
    }

    public void PlayFootstep()
    {
        int i = Random.Range(0, StepClips.Length);
        if (i == lastStep) i = (i + 1) % StepClips.Length;
        lastStep = i;
        steps.pitch = Random.Range(0.92f, 1.08f);
        steps.PlayOneShot(StepClips[i], stepVolume);
    }

    // يشغل أي صوت مرة وحدة (مثل أصوات المشهد الافتتاحي)
    public void Play(AudioClip clip, float volume)
    {
        effects.PlayOneShot(clip, volume * effectsVolume);
    }

    // صوت المطر المستمر
    AudioSource rainSource;
    AudioClip thunderClip;

    public void SetRain(bool on)
    {
        if (rainSource == null)
        {
            rainSource = NewSource("Rain", SynthAudio.Rain(), true);
            rainSource.volume = 0.35f;
        }
        if (on && !rainSource.isPlaying) rainSource.Play();
        else if (!on && rainSource.isPlaying) rainSource.Stop();
    }

    // صوت الريح: intensity من 0 (ما فيه ريح) لـ 1 (ريح قوية)
    AudioSource windSource;

    public void SetWind(float intensity)
    {
        if (windSource == null)
        {
            if (intensity <= 0f) return;
            windSource = NewSource("Wind", SynthAudio.Wind(), true);
        }
        windSource.volume = intensity > 0f ? 0.12f + 0.35f * intensity : 0f;
        windSource.pitch = 0.8f + 0.4f * intensity;
        if (intensity > 0f && !windSource.isPlaying) windSource.Play();
        else if (intensity <= 0f && windSource.isPlaying) windSource.Stop();
    }

    // ---------- أصوات الوحوش ----------
    AudioClip squeakClip, chompClip, boingClip, poofClip;
    AudioSource monsterSource;

    AudioSource MonsterSource()
    {
        if (monsterSource == null) monsterSource = NewSource("Monsters", null, false);
        return monsterSource;
    }

    public void PlaySqueak(float pitch)
    {
        if (squeakClip == null) squeakClip = SynthAudio.Squeak();
        var s = MonsterSource();
        s.pitch = pitch;
        s.PlayOneShot(squeakClip, effectsVolume * 0.45f);
    }

    public void PlayChomp()
    {
        if (chompClip == null) chompClip = SynthAudio.Chomp();
        effects.PlayOneShot(chompClip, effectsVolume * 0.6f);
    }

    public void PlayBoing()
    {
        if (boingClip == null) boingClip = SynthAudio.Boing();
        effects.PlayOneShot(boingClip, effectsVolume * 0.6f);
    }

    public void PlayPoof()
    {
        if (poofClip == null) poofClip = SynthAudio.Poof();
        effects.PlayOneShot(poofClip, effectsVolume * 0.8f);
    }

    public void PlayThunder()
    {
        if (thunderClip == null) thunderClip = SynthAudio.Thunder();
        effects.PlayOneShot(thunderClip, effectsVolume * Random.Range(0.6f, 1f));
    }

    // هدير الانهيار الثلجي (نفس صوت الرعد بس أخف)
    public void PlayRumble(float volume)
    {
        if (thunderClip == null) thunderClip = SynthAudio.Thunder();
        effects.PlayOneShot(thunderClip, effectsVolume * volume);
    }

    public void PlayLose()
    {
        effects.PlayOneShot(LoseClip, effectsVolume);
        duckTimer = 1.8f;
    }

    public void PlayWin()
    {
        effects.PlayOneShot(WinClip, effectsVolume);
        duckTimer = 2.4f;
    }
}
