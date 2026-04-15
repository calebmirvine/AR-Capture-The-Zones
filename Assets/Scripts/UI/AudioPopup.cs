using UnityEngine;
using UnityEngine.UI;

public class AudioPopup : BasePopup
{
    [SerializeField] private SettingsPopup settingsPopup;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Toggle musicToggle;
    [SerializeField] private Toggle sfxToggle;

    private void OnEnable()
    {
        SyncSliderValuesFromSoundManager();
    }

    public void OnBackButton()
    {
        PlayNavigationSfx();
        settingsPopup.Open();
        Close();
    }

    public void OnMusicSliderChanged(float value)
    {
        SoundManager.Instance.MusicVolume = value;
    }

    public void OnSfxSliderChanged(float value)
    {
        SoundManager.Instance.SfxVolume = value;
    }

    private void SyncSliderValuesFromSoundManager()
    {
        //Music
        musicSlider.value = SoundManager.Instance.MusicVolume;
        musicToggle.isOn = SoundManager.Instance.MusicPlaying;

        //SFX
        sfxSlider.value = SoundManager.Instance.SfxVolume;
        sfxToggle.isOn = SoundManager.Instance.SfxPlaying;
    }

    public void OnMusicPlayingToggleChanged(bool isPlaying)
    {
        SoundManager.Instance.MusicPlaying = isPlaying;
    }

    public void OnSfxPlayingToggleChanged(bool isPlaying)
    {
        SoundManager.Instance.SfxPlaying = isPlaying;
    }

    private void PlayNavigationSfx()
    {
        SoundManager.Instance.PlaySfx(SoundLibrary.Instance.MenuNavSfx);
    }
}
