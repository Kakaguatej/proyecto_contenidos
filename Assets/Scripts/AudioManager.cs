using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

/// <summary>
/// Gestor de audio persistente del juego. Centraliza música, SFX, mezcla y
/// suscripción a GameState para no repetir lógica en cada escena.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Fuentes de Audio")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Mezclador")]
    [SerializeField] private AudioMixer mainMixer;

    [Header("Música")]
    [SerializeField] private AudioClip menuMusic;
    [SerializeField] private AudioClip levelMusic;

    [Header("Efectos de Sonido")]
    [SerializeField] private AudioClip uiButtonClip;
    [SerializeField] private AudioClip chipClickClip;
    [SerializeField] private AudioClip dashClip;
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip spawnOrHealClip;
    [SerializeField] private AudioClip enemyShotClip;
    [SerializeField] private AudioClip puzzleSuccessClip;
    [SerializeField] private AudioClip puzzleFailClip;
    [SerializeField] private AudioClip damageClip;
    [SerializeField] private AudioClip deathClip;
    [SerializeField] private AudioClip shieldHitClip;

    private const string MusicVolumeKey = "vol_music";
    private const string SfxVolumeKey = "vol_sfx";
    private const string MuteKey = "mute";
    private const float SfxCooldown = 0.08f;
    private const float MusicFadeDuration = 0.5f;
    private const float DangerDuckFactor = 0.35f;
    private const float DangerExitMargin = 5f;
    private const float DefaultVolume = 0.8f;

    private readonly Dictionary<AudioClip, float> lastSfxTime = new Dictionary<AudioClip, float>();

    private float musicVolume = DefaultVolume;
    private float sfxVolume = DefaultVolume;
    private bool isMuted;
    private float musicGain = 1f;
    private float dangerFactor = 1f;
    private bool dangerActive;
    private AudioClip currentMusic;
    private Coroutine musicFadeCoroutine;
    private Coroutine dangerFadeCoroutine;
    private GameState subscribedState;
    private float lastHealth = -1f;
    private float lastTemperature = -1f;
    private bool sceneLoadHandled;
    private int lastHandledFrame = -1;
    private bool mixerWarningShown;

    public float MusicVolume => musicVolume;
    public float SfxVolume => sfxVolume;
    public bool IsMuted => isMuted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (musicSource != null)
        {
            musicSource.playOnAwake = false;
            musicSource.loop = true;
        }

        if (sfxSource != null)
        {
            sfxSource.playOnAwake = false;
        }

        LoadSettings();
        ApplyVolumes();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        SceneManager.sceneUnloaded += HandleSceneUnloaded;
    }

    private void Start()
    {
        if (Instance != this)
        {
            return;
        }

        // La escena inicial no siempre dispara sceneLoaded antes de Start.
        if (!sceneLoadHandled)
        {
            HandleScene(SceneManager.GetActiveScene());
        }
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneUnloaded -= HandleSceneUnloaded;
        UnsubscribeFromGameState();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        UnsubscribeFromGameState();
        PlayerPrefs.Save();
    }

    // ==================== MÚSICA ====================

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("[AudioManager] Música sin asignar.");
            return;
        }

        if (musicSource == null)
        {
            Debug.LogWarning("[AudioManager] musicSource sin asignar.");
            return;
        }

        if (currentMusic == clip && musicSource.isPlaying)
        {
            return;
        }

        if (musicFadeCoroutine != null)
        {
            StopCoroutine(musicFadeCoroutine);
        }

        musicFadeCoroutine = StartCoroutine(CrossFadeTo(clip));
    }

    public void StopMusic(float fadeDuration = MusicFadeDuration)
    {
        if (musicSource == null)
        {
            return;
        }

        if (musicFadeCoroutine != null)
        {
            StopCoroutine(musicFadeCoroutine);
        }

        musicFadeCoroutine = StartCoroutine(FadeOutAndStop(fadeDuration));
    }

    private IEnumerator CrossFadeTo(AudioClip clip)
    {
        if (musicSource.isPlaying)
        {
            float startGain = musicGain;
            float elapsed = 0f;

            while (elapsed < MusicFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                musicGain = Mathf.Lerp(startGain, 0f, elapsed / MusicFadeDuration);
                ApplyMusicVolume();
                yield return null;
            }

            musicSource.Stop();
        }

        currentMusic = clip;
        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();

        musicGain = 0f;
        ApplyMusicVolume();

        float fadeElapsed = 0f;
        while (fadeElapsed < MusicFadeDuration)
        {
            fadeElapsed += Time.unscaledDeltaTime;
            musicGain = Mathf.Lerp(0f, 1f, fadeElapsed / MusicFadeDuration);
            ApplyMusicVolume();
            yield return null;
        }

        musicGain = 1f;
        ApplyMusicVolume();
        musicFadeCoroutine = null;
    }

    private IEnumerator FadeOutAndStop(float fadeDuration)
    {
        float startGain = musicGain;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            musicGain = Mathf.Lerp(startGain, 0f, elapsed / fadeDuration);
            ApplyMusicVolume();
            yield return null;
        }

        musicGain = 0f;
        ApplyMusicVolume();

        if (musicSource != null)
        {
            musicSource.Stop();
            musicSource.clip = null;
        }

        currentMusic = null;
        musicFadeCoroutine = null;
    }

    // ==================== SFX ====================

    public void PlaySfx(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null)
        {
            Debug.LogWarning("[AudioManager] Clip de SFX sin asignar.");
            return;
        }

        if (sfxSource == null)
        {
            Debug.LogWarning("[AudioManager] sfxSource sin asignar.");
            return;
        }

        float lastTime;
        if (lastSfxTime.TryGetValue(clip, out lastTime) && Time.unscaledTime - lastTime < SfxCooldown)
        {
            return;
        }

        lastSfxTime[clip] = Time.unscaledTime;
        sfxSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
    }

    public void PlayJump() => PlaySfx(jumpClip);
    public void PlayDash() => PlaySfx(dashClip);
    public void PlayEnemyShot() => PlaySfx(enemyShotClip);
    public void PlayUiButton() => PlaySfx(uiButtonClip);
    public void PlayChipClick() => PlaySfx(chipClickClip);
    public void PlayPuzzleSuccess() => PlaySfx(puzzleSuccessClip);
    public void PlayPuzzleFail() => PlaySfx(puzzleFailClip);
    public void PlayShieldHit() => PlaySfx(shieldHitClip);
    public void PlayDamage() => PlaySfx(damageClip);
    public void PlayDeath() => PlaySfx(deathClip);
    public void PlaySpawnOrHeal() => PlaySfx(spawnOrHealClip);

    // ==================== VOLUMEN Y PERSISTENCIA ====================

    public void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp(value, 0.0001f, 1f);
        PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);
        PlayerPrefs.Save();
        ApplyVolumes();
    }

    public void SetSfxVolume(float value)
    {
        sfxVolume = Mathf.Clamp(value, 0.0001f, 1f);
        PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolume);
        PlayerPrefs.Save();
        ApplyVolumes();
    }

    public void SetMute(bool muted)
    {
        isMuted = muted;
        PlayerPrefs.SetInt(MuteKey, isMuted ? 1 : 0);
        PlayerPrefs.Save();
        AudioListener.volume = isMuted ? 0f : 1f;
    }

    private void LoadSettings()
    {
        musicVolume = Mathf.Clamp(PlayerPrefs.GetFloat(MusicVolumeKey, DefaultVolume), 0.0001f, 1f);
        sfxVolume = Mathf.Clamp(PlayerPrefs.GetFloat(SfxVolumeKey, DefaultVolume), 0.0001f, 1f);
        isMuted = PlayerPrefs.GetInt(MuteKey, 0) == 1;
    }

    private void ApplyVolumes()
    {
        AudioListener.volume = isMuted ? 0f : 1f;

        if (mainMixer != null)
        {
            SetMixerVolume("MusicVol", musicVolume);
            SetMixerVolume("SfxVol", sfxVolume);
        }

        if (sfxSource != null)
        {
            sfxSource.volume = mainMixer == null ? sfxVolume : 1f;
        }

        ApplyMusicVolume();
    }

    private void ApplyMusicVolume()
    {
        if (musicSource == null)
        {
            return;
        }

        float userVolume = mainMixer == null ? musicVolume : 1f;
        musicSource.volume = musicGain * dangerFactor * userVolume;
    }

    private void SetMixerVolume(string parameterName, float linearVolume)
    {
        float decibels = Mathf.Log10(Mathf.Max(linearVolume, 0.0001f)) * 20f;
        if (!mainMixer.SetFloat(parameterName, decibels) && !mixerWarningShown)
        {
            mixerWarningShown = true;
            Debug.LogWarning("[AudioManager] No se encontró el parámetro '" + parameterName + "' en el mixer.");
        }
    }

    // ==================== SUSCRIPCIÓN A GAMESTATE ====================

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (Instance != this || mode != LoadSceneMode.Single)
        {
            return;
        }

        sceneLoadHandled = true;
        HandleScene(scene);
    }

    private void HandleSceneUnloaded(Scene scene)
    {
        UnsubscribeFromGameState();
    }

    private void HandleScene(Scene scene)
    {
        if (lastHandledFrame == Time.frameCount)
        {
            return;
        }

        lastHandledFrame = Time.frameCount;
        UnsubscribeFromGameState();
        ResetDangerState();

        if (scene.name == "MainMenuScene")
        {
            PlayMusic(menuMusic);
            return;
        }

        if (scene.name == "Level1Scene")
        {
            PlayMusic(levelMusic);
            PlaySpawnOrHeal();
            StartCoroutine(SubscribeWhenReady());
            return;
        }

        StopMusic(0.2f);
    }

    private IEnumerator SubscribeWhenReady()
    {
        float timeout = 3f;

        while (GameState.Instance == null && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (GameState.Instance == null)
        {
            Debug.LogWarning("[AudioManager] No se encontró GameState en Level1Scene.");
            yield break;
        }

        // Espera un frame para que GameState.Start ejecute ResetState.
        yield return null;

        SubscribeToGameState(GameState.Instance);
    }

    private void SubscribeToGameState(GameState state)
    {
        if (state == null || subscribedState == state)
        {
            return;
        }

        UnsubscribeFromGameState();
        subscribedState = state;
        subscribedState.OnHealthChanged += HandleHealthChanged;
        subscribedState.OnTemperatureChanged += HandleTemperatureChanged;
        subscribedState.OnPlayerDied += HandlePlayerDied;

        lastHealth = state.CurrentHealth;
        lastTemperature = state.CurrentTemperature;
        UpdateDangerState(state.CurrentTemperature);
    }

    private void UnsubscribeFromGameState()
    {
        if (subscribedState != null)
        {
            subscribedState.OnHealthChanged -= HandleHealthChanged;
            subscribedState.OnTemperatureChanged -= HandleTemperatureChanged;
            subscribedState.OnPlayerDied -= HandlePlayerDied;
        }

        subscribedState = null;
        lastHealth = -1f;
        lastTemperature = -1f;
    }

    private void HandleHealthChanged(float current, float max)
    {
        if (subscribedState == null)
        {
            return;
        }

        float previous = lastHealth;
        lastHealth = current;

        if (previous < 0f)
        {
            return;
        }

        if (current < previous - 0.01f)
        {
            if (current > 0.01f)
            {
                PlayDamage();
            }
        }
        else if (current > previous + 0.01f)
        {
            PlaySpawnOrHeal();
        }
    }

    private void HandleTemperatureChanged(float current, float normalized)
    {
        if (subscribedState == null)
        {
            return;
        }

        float previous = lastTemperature;
        lastTemperature = current;

        if (previous >= 0f)
        {
            float delta = current - previous;
            if (delta >= 3f)
            {
                PlayPuzzleFail();
            }
            else if (delta <= -1f)
            {
                PlayPuzzleSuccess();
            }
        }

        UpdateDangerState(current);
    }

    private void HandlePlayerDied(DeathReason reason)
    {
        PlayDeath();
        ResetDangerState();
        StopMusic(1f);
    }

    private void UpdateDangerState(float currentTemperature)
    {
        float threshold = 130f;
        if (subscribedState != null && subscribedState.Config != null)
        {
            threshold = subscribedState.Config.tempHotDangerThreshold;
        }

        if (currentTemperature >= threshold)
        {
            SetDangerActive(true);
        }
        else if (currentTemperature <= threshold - DangerExitMargin)
        {
            SetDangerActive(false);
        }
    }

    private void SetDangerActive(bool active)
    {
        if (dangerActive == active)
        {
            return;
        }

        dangerActive = active;
        if (dangerFadeCoroutine != null)
        {
            StopCoroutine(dangerFadeCoroutine);
        }

        dangerFadeCoroutine = StartCoroutine(FadeDanger(active ? DangerDuckFactor : 1f));
    }

    private IEnumerator FadeDanger(float target)
    {
        float start = dangerFactor;
        float elapsed = 0f;
        const float duration = 0.4f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            dangerFactor = Mathf.Lerp(start, target, elapsed / duration);
            ApplyMusicVolume();
            yield return null;
        }

        dangerFactor = target;
        ApplyMusicVolume();
        dangerFadeCoroutine = null;
    }

    private void ResetDangerState()
    {
        dangerActive = false;
        dangerFactor = 1f;
        if (dangerFadeCoroutine != null)
        {
            StopCoroutine(dangerFadeCoroutine);
            dangerFadeCoroutine = null;
        }
        ApplyMusicVolume();
    }
}
