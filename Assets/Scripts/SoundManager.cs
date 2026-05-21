using UnityEngine;
using System.Collections.Generic;

public class SoundManager : MonoBehaviour
{
    private static SoundManager instance;
    public static SoundManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<SoundManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("SoundManager");
                    instance = go.AddComponent<SoundManager>();
                }
            }
            return instance;
        }
    }

    private AudioSource bgmSource;
    private AudioSource sfxSource;

    private Dictionary<string, AudioClip> sfxCache = new Dictionary<string, AudioClip>();
    private Dictionary<string, AudioClip> bgmCache = new Dictionary<string, AudioClip>();

    private float bgmVolume = 0.5f;
    private float sfxVolume = 0.6f;

    private string currentPlayingBGM = "";
    private bool isMuted = false;
    public bool IsMuted => isMuted;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioSources();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void InitializeAudioSources()
    {
        // Add AudioSources programmatically so the user doesn't need to configure them in inspector
        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.volume = bgmVolume;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.volume = sfxVolume;

        // Load mute state from PlayerPrefs
        isMuted = PlayerPrefs.GetInt("Muted", 0) == 1;
        ApplyMuteState();

        // Ensure there is an AudioListener in the scene
        EnsureAudioListener();
    }

    public void ToggleMute()
    {
        isMuted = !isMuted;
        PlayerPrefs.SetInt("Muted", isMuted ? 1 : 0);
        PlayerPrefs.Save();
        ApplyMuteState();
        Debug.Log($"[SoundManager] Mute toggled: {isMuted}");
    }

    private void ApplyMuteState()
    {
        if (bgmSource != null) bgmSource.mute = isMuted;
        if (sfxSource != null) sfxSource.mute = isMuted;
    }

    private void EnsureAudioListener()
    {
        AudioListener listener = FindAnyObjectByType<AudioListener>();
        if (listener == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                listener = mainCam.gameObject.AddComponent<AudioListener>();
                Debug.Log("[SoundManager] Added AudioListener to Main Camera.");
            }
            else
            {
                listener = gameObject.AddComponent<AudioListener>();
                Debug.LogWarning("[SoundManager] No Main Camera found. Added AudioListener to SoundManager.");
            }
        }
    }

    // --- PLAY BGM ---
    public void PlayBGM(string clipName, bool loop = true)
    {
        if (currentPlayingBGM == clipName && bgmSource.isPlaying) return;

        bgmSource.loop = loop;
        AudioClip clip = LoadBGMClip(clipName);

        if (clip != null)
        {
            bgmSource.clip = clip;
            bgmSource.Play();
            currentPlayingBGM = clipName;
            Debug.Log($"[SoundManager] Playing BGM: {clipName}");
        }
        else
        {
            Debug.LogWarning($"[SoundManager] BGM Clip '{clipName}' not found in resources. Playing procedural fallback.");
            PlayProceduralBGM(clipName, loop);
        }
    }

    public void StopBGM()
    {
        bgmSource.Stop();
        currentPlayingBGM = "";
    }

    // --- PLAY SFX ---
    public void PlaySFX(string clipName, float volumeScale = 1.0f)
    {
        AudioClip clip = LoadSFXClip(clipName);

        if (clip != null)
        {
            sfxSource.PlayOneShot(clip, volumeScale);
        }
        else
        {
            Debug.LogWarning($"[SoundManager] SFX Clip '{clipName}' not found in resources. Playing procedural fallback.");
            PlayProceduralSFX(clipName, volumeScale);
        }
    }

    // --- DYNAMIC LOADING WITH PROCEDURAL FALLBACKS ---
    private AudioClip LoadSFXClip(string name)
    {
        if (sfxCache.TryGetValue(name, out AudioClip cachedClip))
        {
            return cachedClip;
        }

        // Try load from Assets/Resources/Audio/SFX/
        AudioClip clip = Resources.Load<AudioClip>("Audio/SFX/" + name);
        if (clip != null)
        {
            sfxCache[name] = clip;
            return clip;
        }
        return null;
    }

    private AudioClip LoadBGMClip(string name)
    {
        if (bgmCache.TryGetValue(name, out AudioClip cachedClip))
        {
            return cachedClip;
        }

        // Try load from Assets/Resources/Audio/BGM/
        AudioClip clip = Resources.Load<AudioClip>("Audio/BGM/" + name);
        if (clip != null)
        {
            bgmCache[name] = clip;
            return clip;
        }
        return null;
    }

    // --- PROCEDURAL AUDIO GENERATION (NO EXTERNAL ASSETS REQUIRED!) ---
    private void PlayProceduralSFX(string name, float volumeScale)
    {
        AudioClip clip = null;

        switch (name.ToLower())
        {
            case "click":
                clip = CreateClickClip();
                break;
            case "gem_collect":
            case "gem":
                clip = CreateGemCollectClip();
                break;
            case "shoot":
            case "projectile":
                clip = CreateShootClip();
                break;
            case "player_hurt":
            case "hurt":
                clip = CreatePlayerHurtClip();
                break;
            case "player_death":
            case "death":
                clip = CreatePlayerDeathClip();
                break;
            case "boss_warning":
            case "warning":
                clip = CreateBossWarningClip();
                break;
            case "shield_blast":
            case "shield":
                clip = CreateShieldBlastClip();
                break;
            case "samba_sprint":
            case "samba":
                clip = CreateSambaSprintClip();
                break;
            default:
                Debug.LogWarning($"[SoundManager] No procedural SFX defined for: {name}");
                break;
        }

        if (clip != null)
        {
            sfxSource.PlayOneShot(clip, volumeScale);
        }
    }

    private void PlayProceduralBGM(string name, bool loop)
    {
        AudioClip clip = null;

        if (name.Contains("menu"))
        {
            clip = CreateProceduralBGMClip("menu");
        }
        else if (name.Contains("samba") || name.Contains("sprint"))
        {
            clip = CreateProceduralBGMClip("samba");
        }
        else // battle / gameplay
        {
            clip = CreateProceduralBGMClip("battle");
        }

        if (clip != null)
        {
            bgmSource.clip = clip;
            bgmSource.loop = loop;
            bgmSource.Play();
            currentPlayingBGM = name;
        }
    }

    // --- SYNTHESIZERS ---

    private static AudioClip CreateGemCollectClip()
    {
        int sampleRate = 44100;
        float duration1 = 0.08f;
        float duration2 = 0.15f;
        int samples1 = (int)(sampleRate * duration1);
        int samples2 = (int)(sampleRate * duration2);
        int totalSamples = samples1 + samples2;
        float[] data = new float[totalSamples];
        
        // Note 1: C5 (523.25 Hz)
        float freq1 = 523.25f;
        for (int i = 0; i < samples1; i++)
        {
            float t = (float)i / sampleRate;
            float env = 1f - ((float)i / samples1);
            data[i] = Mathf.Sin(2f * Mathf.PI * freq1 * t) * 0.25f * env;
        }
        
        // Note 2: E5 (659.25 Hz)
        float freq2 = 659.25f;
        for (int i = 0; i < samples2; i++)
        {
            float t = (float)i / sampleRate;
            float env = 1f - ((float)i / samples2);
            data[samples1 + i] = Mathf.Sin(2f * Mathf.PI * freq2 * t) * 0.25f * env;
        }
        
        AudioClip clip = AudioClip.Create("SFX_Gem", totalSamples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static AudioClip CreateClickClip()
    {
        int sampleRate = 44100;
        float duration = 0.04f;
        int totalSamples = (int)(sampleRate * duration);
        float[] data = new float[totalSamples];
        float freq = 900f;
        for (int i = 0; i < totalSamples; i++)
        {
            float t = (float)i / sampleRate;
            float env = 1f - ((float)i / totalSamples);
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.2f * env;
        }
        AudioClip clip = AudioClip.Create("SFX_Click", totalSamples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static AudioClip CreateShootClip()
    {
        int sampleRate = 44100;
        float duration = 0.1f;
        int totalSamples = (int)(sampleRate * duration);
        float[] data = new float[totalSamples];
        for (int i = 0; i < totalSamples; i++)
        {
            float pct = (float)i / totalSamples;
            float freq = Mathf.Lerp(1100f, 300f, pct);
            float t = (float)i / sampleRate;
            float env = 1f - pct;
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.15f * env;
        }
        AudioClip clip = AudioClip.Create("SFX_Shoot", totalSamples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static AudioClip CreatePlayerHurtClip()
    {
        int sampleRate = 44100;
        float duration = 0.15f;
        int totalSamples = (int)(sampleRate * duration);
        float[] data = new float[totalSamples];
        for (int i = 0; i < totalSamples; i++)
        {
            float pct = (float)i / totalSamples;
            float freq = Mathf.Lerp(220f, 90f, pct);
            float t = (float)i / sampleRate;
            float env = 1f - pct;
            float sine = Mathf.Sin(2f * Mathf.PI * freq * t);
            float noise = Random.Range(-0.15f, 0.15f);
            data[i] = (sine + noise) * 0.2f * env;
        }
        AudioClip clip = AudioClip.Create("SFX_Hurt", totalSamples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static AudioClip CreatePlayerDeathClip()
    {
        int sampleRate = 44100;
        float duration = 0.6f;
        int totalSamples = (int)(sampleRate * duration);
        float[] data = new float[totalSamples];
        for (int i = 0; i < totalSamples; i++)
        {
            float pct = (float)i / totalSamples;
            float freq = Mathf.Lerp(300f, 40f, pct);
            float t = (float)i / sampleRate;
            float env = 1f - pct;
            float sine = Mathf.Sin(2f * Mathf.PI * freq * t);
            float noise = Random.Range(-0.1f, 0.1f);
            data[i] = (sine + noise) * 0.3f * env;
        }
        AudioClip clip = AudioClip.Create("SFX_Death", totalSamples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static AudioClip CreateBossWarningClip()
    {
        int sampleRate = 44100;
        float duration = 0.9f;
        int totalSamples = (int)(sampleRate * duration);
        float[] data = new float[totalSamples];
        for (int i = 0; i < totalSamples; i++)
        {
            float pct = (float)i / totalSamples;
            float freq = (Mathf.Floor(pct * 6f) % 2 == 0) ? 580f : 440f;
            float t = (float)i / sampleRate;
            float env = 1f - (pct * 0.2f);
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.3f * env;
        }
        AudioClip clip = AudioClip.Create("SFX_Warning", totalSamples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static AudioClip CreateShieldBlastClip()
    {
        int sampleRate = 44100;
        float duration = 0.5f;
        int totalSamples = (int)(sampleRate * duration);
        float[] data = new float[totalSamples];
        for (int i = 0; i < totalSamples; i++)
        {
            float pct = (float)i / totalSamples;
            float freq = Mathf.Lerp(140f, 50f, pct);
            float t = (float)i / sampleRate;
            float env = 1f - pct;
            float noise = Random.Range(-0.25f, 0.25f);
            data[i] = (Mathf.Sin(2f * Mathf.PI * freq * t) + noise) * 0.3f * env;
        }
        AudioClip clip = AudioClip.Create("SFX_Shield", totalSamples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static AudioClip CreateSambaSprintClip()
    {
        int sampleRate = 44100;
        float duration = 0.45f;
        int totalSamples = (int)(sampleRate * duration);
        float[] data = new float[totalSamples];
        for (int i = 0; i < totalSamples; i++)
        {
            float pct = (float)i / totalSamples;
            float t = (float)i / sampleRate;
            float env = 1f - pct;
            float[] notes = { 440.00f, 554.37f, 659.25f, 880.00f }; // A4, C#5, E5, A5
            float freq = notes[Mathf.Min((int)(pct * 4f), 3)];
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.2f * env;
        }
        AudioClip clip = AudioClip.Create("SFX_Samba", totalSamples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static AudioClip CreateProceduralBGMClip(string themeType)
    {
        int sampleRate = 22050; // Half rate is fine for procedural 8-bit BGM (saves memory)
        float duration = 4.0f;
        int totalSamples = (int)(sampleRate * duration);
        float[] data = new float[totalSamples];

        // Basic notes
        float[] chord1 = { 261.63f, 329.63f, 392.00f }; // C
        float[] chord2 = { 349.23f, 440.00f, 261.63f }; // F
        float[] chord3 = { 392.00f, 493.88f, 293.66f }; // G
        float[] chord4 = { 220.00f, 261.63f, 329.63f }; // Am

        if (themeType == "battle")
        {
            chord1 = new float[] { 220.00f, 261.63f, 329.63f }; // Am
            chord2 = new float[] { 293.66f, 349.23f, 440.00f }; // Dm
            chord3 = new float[] { 329.63f, 392.00f, 493.88f }; // Em
            chord4 = new float[] { 220.00f, 261.63f, 329.63f }; // Am
        }
        else if (themeType == "samba")
        {
            chord1 = new float[] { 261.63f, 329.63f, 392.00f }; // C
            chord2 = new float[] { 293.66f, 369.99f, 440.00f }; // D
            chord3 = new float[] { 349.23f, 440.00f, 523.25f }; // F
            chord4 = new float[] { 392.00f, 493.88f, 587.33f }; // G
        }

        float tempo = themeType == "samba" ? 0.12f : 0.16f;

        for (int i = 0; i < totalSamples; i++)
        {
            float time = (float)i / sampleRate;

            float[] currentChord = chord1;
            float segment = time / 1.0f; // 1 second per chord segment
            int segmentIndex = Mathf.FloorToInt(segment) % 4;

            if (segmentIndex == 1) currentChord = chord2;
            else if (segmentIndex == 2) currentChord = chord3;
            else if (segmentIndex == 3) currentChord = chord4;

            int noteIndex = Mathf.FloorToInt(time / tempo) % currentChord.Length;
            float freq = currentChord[noteIndex];

            // Chiptune square wave synth
            float angle = 2f * Mathf.PI * freq * time;
            float val = Mathf.Sign(Mathf.Sin(angle));

            // Envelope decay for retro step notes
            float noteTime = time % tempo;
            float noteEnv = Mathf.Max(0f, 1f - (noteTime / tempo));

            data[i] = val * 0.04f * noteEnv; // Quiet volume
        }

        AudioClip clip = AudioClip.Create("BGM_" + themeType, totalSamples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
