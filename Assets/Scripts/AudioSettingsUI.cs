using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Conecta los sliders y el toggle del menú con AudioManager.
/// Los valores se leen de AudioManager (que a su vez usa PlayerPrefs).
/// </summary>
public class AudioSettingsUI : MonoBehaviour
{
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Toggle muteToggle;

    private AudioManager audioManager;
    private bool listenersBound;

    private void Awake()
    {
        TryInitialize();
    }

    private void Start()
    {
        if (!listenersBound)
        {
            TryInitialize();
        }
    }

    private void OnEnable()
    {
        if (!listenersBound)
        {
            TryInitialize();
        }
        else
        {
            RefreshValues();
        }
    }

    private void OnDestroy()
    {
        if (musicSlider != null) musicSlider.onValueChanged.RemoveListener(HandleMusicChanged);
        if (sfxSlider != null) sfxSlider.onValueChanged.RemoveListener(HandleSfxChanged);
        if (muteToggle != null) muteToggle.onValueChanged.RemoveListener(HandleMuteChanged);
        listenersBound = false;
    }

    private void TryInitialize()
    {
        if (listenersBound)
        {
            RefreshValues();
            return;
        }

        audioManager = AudioManager.Instance;
        if (audioManager == null)
        {
            return;
        }

        if (musicSlider != null)
        {
            musicSlider.minValue = 0.0001f;
            musicSlider.maxValue = 1f;
            musicSlider.onValueChanged.AddListener(HandleMusicChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.minValue = 0.0001f;
            sfxSlider.maxValue = 1f;
            sfxSlider.onValueChanged.AddListener(HandleSfxChanged);
        }

        if (muteToggle != null)
        {
            muteToggle.onValueChanged.AddListener(HandleMuteChanged);
        }

        listenersBound = true;
        RefreshValues();
    }

    private void RefreshValues()
    {
        if (audioManager == null)
        {
            audioManager = AudioManager.Instance;
        }

        if (audioManager == null)
        {
            return;
        }

        if (musicSlider != null) musicSlider.SetValueWithoutNotify(audioManager.MusicVolume);
        if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(audioManager.SfxVolume);
        if (muteToggle != null) muteToggle.SetIsOnWithoutNotify(audioManager.IsMuted);
    }


    private void HandleMusicChanged(float value)
    {
        if (audioManager != null) audioManager.SetMusicVolume(value);
    }

    private void HandleSfxChanged(float value)
    {
        if (audioManager != null) audioManager.SetSfxVolume(value);
    }

    private void HandleMuteChanged(bool muted)
    {
        if (audioManager != null) audioManager.SetMute(muted);
    }
}
