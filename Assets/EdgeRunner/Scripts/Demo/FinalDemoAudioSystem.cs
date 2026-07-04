using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum FinalDemoAudioCue
{
    Pickup,
    Jump,
    Failure,
    Goal,
    Stomp,
    UIClick
}

public sealed class FinalDemoAudioSystem : MonoBehaviour
{
    private const string RootName = "FinalDemo_AudioSystem";
    private const int SampleRate = 44100;
    private const float InitialMusicVolume = 0.65f;
    private const float InitialSfxVolume = 0.85f;
    private const float VolumeStep = 0.05f;

    private static FinalDemoAudioSystem instance;
    private AudioSource musicSource;
    private AudioSource sfxSource;
    private AudioListener fallbackListener;
    private AudioClip pickupClip;
    private AudioClip jumpClip;
    private AudioClip failureClip;
    private AudioClip goalClip;
    private AudioClip stompClip;
    private AudioClip uiClickClip;
    private float nextCueTime;
    private float nextPlaybackCheckTime;
    private float musicVolume = InitialMusicVolume;
    private bool muted;

    public static bool IsMuted => instance != null && instance.muted;
    public static float CurrentVolume => instance != null ? instance.musicVolume : InitialMusicVolume;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
        {
            return;
        }

        GameObject root = new GameObject(RootName);
        DontDestroyOnLoad(root);
        instance = root.AddComponent<FinalDemoAudioSystem>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        musicSource = gameObject.AddComponent<AudioSource>();
        sfxSource = gameObject.AddComponent<AudioSource>();
        fallbackListener = gameObject.AddComponent<AudioListener>();
        ConfigureSource(musicSource, true, musicVolume, 32);
        ConfigureSource(sfxSource, false, InitialSfxVolume, 16);

        musicSource.clip = CreateMusic();
        pickupClip = CreatePickupTone();
        jumpClip = CreateJumpBoost();
        failureClip = CreateFailureGlitch();
        goalClip = CreateGoalImpact();
        stompClip = CreateMetalStomp();
        uiClickClip = CreateUIClick();

        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        AudioSettings.OnAudioConfigurationChanged -= OnAudioConfigurationChanged;
        AudioSettings.OnAudioConfigurationChanged += OnAudioConfigurationChanged;
        AudioListener.pause = false;
        AudioListener.volume = 1f;
        RefreshListenerFallback();
        EnsureMusicPlaying();
        EditorLog("AudioSystem initialized");
        LogPlaybackState();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            AudioSettings.OnAudioConfigurationChanged -= OnAudioConfigurationChanged;
            instance = null;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.U))
        {
            SetMuted(!muted);
        }
        if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            SetMusicVolume(musicVolume + VolumeStep);
        }
        if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
        {
            SetMusicVolume(musicVolume - VolumeStep);
        }

        if (Time.unscaledTime >= nextPlaybackCheckTime)
        {
            nextPlaybackCheckTime = Time.unscaledTime + 1f;
            EnsureMusicPlaying();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureMusicPlaying();
        StartCoroutine(BindSceneAudio());
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            EnsureMusicPlaying();
        }
    }

    private void OnAudioConfigurationChanged(bool deviceWasChanged)
    {
        EnsureMusicPlaying();
        EditorLog($"audio configuration changed (device={deviceWasChanged})");
        LogPlaybackState();
    }

    private IEnumerator BindSceneAudio()
    {
        yield return null;

        RefreshListenerFallback();

        BindTriggers(FindObjectsByType<FinalDemoVisualCollectible>(FindObjectsInactive.Exclude), FinalDemoAudioCue.Pickup);
        BindTriggers(FindObjectsByType<ScoreAttackCoin>(FindObjectsInactive.Exclude), FinalDemoAudioCue.Pickup);
        BindTriggers(FindObjectsByType<FinalDemoGoalObserver>(FindObjectsInactive.Exclude), FinalDemoAudioCue.Goal);
        BindTriggers(FindObjectsByType<SpeedRunObstacleHazard>(FindObjectsInactive.Exclude), FinalDemoAudioCue.Failure);
        BindTriggers(FindObjectsByType<DeathZone>(FindObjectsInactive.Exclude), FinalDemoAudioCue.Failure);

        EdgeRunnerAgentV5 agent = FindAnyObjectByType<EdgeRunnerAgentV5>();
        if (agent != null && agent.GetComponent<FinalDemoJumpAudioMonitor>() == null)
        {
            agent.gameObject.AddComponent<FinalDemoJumpAudioMonitor>();
        }

        StompableAndroidEnemy[] stompables =
            FindObjectsByType<StompableAndroidEnemy>(FindObjectsInactive.Exclude);
        for (int i = 0; i < stompables.Length; i++)
        {
            if (stompables[i].GetComponent<FinalDemoStompAudioMonitor>() == null)
            {
                stompables[i].gameObject.AddComponent<FinalDemoStompAudioMonitor>();
            }
        }
    }

    private void RefreshListenerFallback()
    {
        if (fallbackListener == null)
        {
            return;
        }

        AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude);
        AudioListener selectedListener = null;
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            AudioListener mainListener = mainCamera.GetComponent<AudioListener>();
            if (mainListener != null)
            {
                mainListener.enabled = true;
                selectedListener = mainListener;
            }
        }

        for (int i = 0; i < listeners.Length; i++)
        {
            AudioListener listener = listeners[i];
            if (listener != null && listener != fallbackListener && listener.enabled)
            {
                if (listener != selectedListener)
                {
                    listener.enabled = false;
                }
            }
        }

        fallbackListener.enabled = selectedListener == null;
        EditorLog(selectedListener != null
            ? $"listener found: {selectedListener.gameObject.name}"
            : "listener created: persistent fallback");
    }

    private static void ConfigureSource(AudioSource source, bool loop, float volume, int priority)
    {
        source.playOnAwake = false;
        source.loop = loop;
        source.volume = volume;
        source.spatialBlend = 0f;
        source.priority = priority;
        source.ignoreListenerPause = true;
        source.bypassEffects = true;
        source.bypassListenerEffects = true;
        source.bypassReverbZones = true;
    }

    private void EnsureMusicPlaying()
    {
        if (musicSource == null || musicSource.clip == null || musicSource.isPlaying)
        {
            return;
        }

        musicSource.Play();
        LogPlaybackState();
    }

    private void SetMuted(bool value)
    {
        muted = value;
        musicSource.mute = value;
        sfxSource.mute = value;
        EditorLog($"current volume={musicVolume:0.00}, muted={muted}");
    }

    private void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(Mathf.Round(value * 20f) / 20f);
        musicSource.volume = musicVolume;
        sfxSource.volume = Mathf.Clamp01(musicVolume + 0.20f);
        EditorLog($"current volume={musicVolume:0.00}, muted={muted}");
    }

    private void LogPlaybackState()
    {
        EditorLog($"music source playing {(musicSource != null && musicSource.isPlaying).ToString().ToLowerInvariant()}");
        EditorLog($"current volume={musicVolume:0.00}, muted={muted}");
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private static void EditorLog(string message)
    {
        Debug.Log($"[FINAL DEMO AUDIO] {message}");
    }

    private static void BindTriggers<T>(T[] components, FinalDemoAudioCue cue) where T : Component
    {
        for (int i = 0; i < components.Length; i++)
        {
            FinalDemoAudioTrigger trigger = components[i].GetComponent<FinalDemoAudioTrigger>();
            if (trigger == null)
            {
                trigger = components[i].gameObject.AddComponent<FinalDemoAudioTrigger>();
            }
            trigger.Configure(cue);
        }
    }

    public static void Play(FinalDemoAudioCue cue)
    {
        if (instance == null || instance.sfxSource == null || Time.unscaledTime < instance.nextCueTime)
        {
            return;
        }

        AudioClip clip = cue switch
        {
            FinalDemoAudioCue.Pickup => instance.pickupClip,
            FinalDemoAudioCue.Jump => instance.jumpClip,
            FinalDemoAudioCue.Failure => instance.failureClip,
            FinalDemoAudioCue.Goal => instance.goalClip,
            FinalDemoAudioCue.Stomp => instance.stompClip,
            FinalDemoAudioCue.UIClick => instance.uiClickClip,
            _ => null
        };
        if (clip == null)
        {
            return;
        }

        instance.nextCueTime = Time.unscaledTime + 0.055f;
        instance.sfxSource.PlayOneShot(clip);
    }

    private static AudioClip CreateMusic()
    {
        const float duration = 24f;
        int samples = Mathf.RoundToInt(duration * SampleRate);
        float[] data = new float[samples * 2];
        float[] roots = { 55f, 49f, 46.25f, 41.20f, 55f, 61.74f };
        float bassPhase = 0f;
        float padPhase = 0f;
        float machinePhase = 0f;
        float filteredNoiseLeft = 0f;
        float filteredNoiseRight = 0f;
        for (int i = 0; i < samples; i++)
        {
            float time = i / (float)SampleRate;
            int section = Mathf.FloorToInt(time / 4f) % roots.Length;
            float root = roots[section];
            float bassCycle = Mathf.Repeat(time * 0.5f, 1f);
            float slowLfo = 0.82f + Mathf.Sin(2f * Mathf.PI * time / 8f) * 0.18f;

            bassPhase += 2f * Mathf.PI * root * 2f / SampleRate;
            padPhase += 2f * Mathf.PI * root * 2f / SampleRate;
            machinePhase += 2f * Mathf.PI * root * 2.5f / SampleRate;
            float bassEnvelope = Mathf.Exp(-2.7f * bassCycle) *
                Mathf.SmoothStep(0f, 1f, bassCycle * 10f);
            float bass = (Mathf.Sin(bassPhase) * 0.13f + Mathf.Sin(bassPhase * 0.5f) * 0.08f) *
                bassEnvelope;

            float pad = Mathf.Sin(padPhase + 0.2f) * 0.115f;
            pad += Mathf.Sin(padPhase * 1.5f + 1.1f) * 0.065f;
            pad += Mathf.Sin(padPhase * 2f + 2.4f) * 0.025f;
            pad *= slowLfo;

            float distantMachine = Mathf.Sin(machinePhase +
                Mathf.Sin(2f * Mathf.PI * time / 12f) * 0.7f) * 0.018f;
            filteredNoiseLeft = Mathf.Lerp(filteredNoiseLeft, SignedNoise(i + 117), 0.012f);
            filteredNoiseRight = Mathf.Lerp(filteredNoiseRight, SignedNoise(i + 991), 0.012f);
            float textureLevel = 0.012f + 0.007f *
                (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * time / 12f));
            float industrialLeft = filteredNoiseLeft * textureLevel;
            float industrialRight = filteredNoiseRight * textureLevel;

            float common = pad + bass + distantMachine;
            data[i * 2] = SoftClip(common + industrialLeft);
            data[i * 2 + 1] = SoftClip(common * 0.98f + industrialRight);
        }

        SmoothLoopEdges(data, samples, 2, Mathf.RoundToInt(SampleRate * 0.08f));

        AudioClip clip = AudioClip.Create("FinalDemo_DarkNeonIndustrialLoop", samples, 2, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static AudioClip CreatePickupTone()
    {
        const float duration = 0.34f;
        int samples = Mathf.RoundToInt(duration * SampleRate);
        float[] data = new float[samples];
        float chargePhase = 0f;
        float bodyPhase = 0f;
        float filteredElectric = 0f;
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)Mathf.Max(1, samples - 1);
            float frequency = Mathf.Lerp(145f, 235f, Mathf.SmoothStep(0f, 1f, t));
            chargePhase += 2f * Mathf.PI * frequency / SampleRate;
            bodyPhase += 2f * Mathf.PI * 82f / SampleRate;
            float envelope = Mathf.Pow(1f - t, 1.65f) * Mathf.SmoothStep(0f, 1f, t * 20f);
            float charge = Mathf.Sin(chargePhase) * 0.34f + Mathf.Sin(bodyPhase) * 0.12f;
            filteredElectric = Mathf.Lerp(filteredElectric, SignedNoise(i + 41), 0.08f);
            float electric = filteredElectric * Mathf.Exp(-9f * t) * 0.035f;
            data[i] = SoftClip((charge + electric) * envelope);
        }
        AudioClip clip = AudioClip.Create("FinalDemo_ElectricPickup", samples, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static AudioClip CreateJumpBoost()
    {
        const float duration = 0.21f;
        int samples = Mathf.RoundToInt(duration * SampleRate);
        float[] data = new float[samples];
        float phaseLow = 0f;
        float phaseServo = 0f;
        float filteredExhaust = 0f;
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)Mathf.Max(1, samples - 1);
            phaseLow += 2f * Mathf.PI * Mathf.Lerp(112f, 56f, t) / SampleRate;
            phaseServo += 2f * Mathf.PI * Mathf.Lerp(205f, 132f, t) / SampleRate;
            float envelope = Mathf.Pow(1f - t, 1.45f) * Mathf.SmoothStep(0f, 1f, t * 34f);
            float servoModulation = 0.72f + 0.28f * Mathf.Sin(2f * Mathf.PI * 31f * t * duration);
            filteredExhaust = Mathf.Lerp(filteredExhaust, SignedNoise(i + 73), 0.10f);
            float exhaust = filteredExhaust * Mathf.Exp(-6f * t) * 0.045f;
            data[i] = SoftClip((Mathf.Sin(phaseLow) * 0.43f +
                Mathf.Sin(phaseServo) * servoModulation * 0.08f + exhaust) * envelope);
        }
        AudioClip clip = AudioClip.Create("FinalDemo_ServoBoost", samples, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static AudioClip CreateFailureGlitch()
    {
        const float duration = 0.48f;
        int samples = Mathf.RoundToInt(duration * SampleRate);
        float[] data = new float[samples];
        float phase = 0f;
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)Mathf.Max(1, samples - 1);
            phase += 2f * Mathf.PI * Mathf.Lerp(105f, 38f, t) / SampleRate;
            float gate = ((i / 280) & 1) == 0 ? 1f : 0.2f;
            float envelope = Mathf.Pow(1f - t, 1.2f) * Mathf.SmoothStep(0f, 1f, t * 35f);
            float digitalRasp = SignedNoise(i / 4 + 131) * 0.10f;
            data[i] = SoftClip((Mathf.Sin(phase) * 0.58f + digitalRasp) * envelope * gate);
        }
        AudioClip clip = AudioClip.Create("FinalDemo_GlitchFailure", samples, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static AudioClip CreateGoalImpact()
    {
        const float duration = 0.85f;
        int samples = Mathf.RoundToInt(duration * SampleRate);
        float[] data = new float[samples];
        float[] notes = { 110f, 164.81f, 220f, 277.18f };
        for (int i = 0; i < samples; i++)
        {
            float time = i / (float)SampleRate;
            float envelope = Mathf.Exp(-3.1f * time) * Mathf.SmoothStep(0f, 1f, time * 35f);
            float value = Mathf.Sin(2f * Mathf.PI * 55f * time) * Mathf.Exp(-9f * time) * 0.38f;
            for (int n = 0; n < notes.Length; n++)
            {
                value += Mathf.Sin(2f * Mathf.PI * notes[n] * time + n * 0.38f) * envelope * 0.105f;
            }
            data[i] = SoftClip(value);
        }
        AudioClip clip = AudioClip.Create("FinalDemo_SynthGoalImpact", samples, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static AudioClip CreateMetalStomp()
    {
        const float duration = 0.42f;
        int samples = Mathf.RoundToInt(duration * SampleRate);
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)Mathf.Max(1, samples - 1);
            float envelope = Mathf.Exp(-8.5f * t) * Mathf.SmoothStep(0f, 1f, t * 45f);
            float low = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(88f, 47f, t) * i / SampleRate) * 0.52f;
            float metal = (Mathf.Sin(2f * Mathf.PI * 173f * i / SampleRate) +
                Mathf.Sin(2f * Mathf.PI * 263f * i / SampleRate + 0.8f)) * 0.13f;
            float spark = SignedNoise(i + 211) * Mathf.Exp(-26f * t) * 0.13f;
            data[i] = SoftClip((low + metal + spark) * envelope);
        }
        AudioClip clip = AudioClip.Create("FinalDemo_MetalStomp", samples, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static AudioClip CreateUIClick()
    {
        // Short, subtle cyberpunk confirm blip for menu buttons — quick pitch drop + a
        // faint digital tick, well under audible fatigue length.
        const float duration = 0.09f;
        int samples = Mathf.RoundToInt(duration * SampleRate);
        float[] data = new float[samples];
        float phase = 0f;
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)Mathf.Max(1, samples - 1);
            phase += 2f * Mathf.PI * Mathf.Lerp(1180f, 620f, t) / SampleRate;
            float envelope = Mathf.Exp(-14f * t) * Mathf.SmoothStep(0f, 1f, t * 60f);
            float tick = SignedNoise(i + 331) * Mathf.Exp(-40f * t) * 0.06f;
            data[i] = SoftClip((Mathf.Sin(phase) * 0.22f + tick) * envelope);
        }
        AudioClip clip = AudioClip.Create("FinalDemo_UIClick", samples, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static float SignedNoise(int value)
    {
        float noise = Mathf.Sin(value * 12.9898f + 78.233f) * 43758.5453f;
        return Mathf.Repeat(noise, 1f) * 2f - 1f;
    }

    private static void SmoothLoopEdges(float[] data, int sampleFrames, int channels, int fadeFrames)
    {
        int safeFadeFrames = Mathf.Clamp(fadeFrames, 1, sampleFrames / 2);
        for (int frame = 0; frame < safeFadeFrames; frame++)
        {
            float normalized = frame / (float)safeFadeFrames;
            float fadeIn = Mathf.Sin(normalized * Mathf.PI * 0.5f);
            float fadeOut = Mathf.Cos(normalized * Mathf.PI * 0.5f);
            int tailFrame = sampleFrames - safeFadeFrames + frame;
            for (int channel = 0; channel < channels; channel++)
            {
                data[frame * channels + channel] *= fadeIn;
                data[tailFrame * channels + channel] *= fadeOut;
            }
        }
    }

    private static float SoftClip(float value)
    {
        float driven = value * 1.35f;
        return driven / (1f + Mathf.Abs(driven));
    }
}

public sealed class FinalDemoAudioTrigger : MonoBehaviour
{
    [SerializeField] private FinalDemoAudioCue cue;
    private bool played;

    public void Configure(FinalDemoAudioCue value)
    {
        cue = value;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (played || other.GetComponentInParent<EdgeRunnerAgentV5>() == null)
        {
            return;
        }

        if (cue == FinalDemoAudioCue.Goal)
        {
            ScoreAttackManager manager = FindAnyObjectByType<ScoreAttackManager>();
            if (manager != null && !manager.ObjectivesComplete)
            {
                return;
            }
        }

        played = true;
        FinalDemoAudioSystem.Play(cue);
    }
}

public sealed class FinalDemoJumpAudioMonitor : MonoBehaviour
{
    private Rigidbody2D body;
    private float previousVerticalVelocity;
    private float nextSoundTime;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        if (body == null)
        {
            return;
        }

        float velocity = body.linearVelocity.y;
        if (velocity > 1.7f && previousVerticalVelocity <= 0.8f && Time.unscaledTime >= nextSoundTime)
        {
            nextSoundTime = Time.unscaledTime + 0.18f;
            FinalDemoAudioSystem.Play(FinalDemoAudioCue.Jump);
        }
        previousVerticalVelocity = velocity;
    }
}

public sealed class FinalDemoStompAudioMonitor : MonoBehaviour
{
    private StompableAndroidEnemy target;
    private bool wasAlive;

    private void Awake()
    {
        target = GetComponent<StompableAndroidEnemy>();
        wasAlive = target != null && target.IsAlive;
    }

    private void Update()
    {
        bool alive = target != null && target.IsAlive;
        if (wasAlive && !alive)
        {
            FinalDemoAudioSystem.Play(FinalDemoAudioCue.Stomp);
        }
        wasAlive = alive;
    }
}
