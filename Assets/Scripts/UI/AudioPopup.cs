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
        musicToggle.SetIsOnWithoutNotify(SoundManager.Instance.MusicPlaying);

        //SFX
        sfxSlider.value = SoundManager.Instance.SfxVolume;
        sfxToggle.SetIsOnWithoutNotify(SoundManager.Instance.SfxPlaying);
    }

    public void OnMusicPlayingToggleChanged(bool isPlaying)
    {
        PlayNavigationSfx();
        SoundManager.Instance.MusicPlaying = isPlaying;
    }

    public void OnSfxPlayingToggleChanged(bool isPlaying)
    {
        PlayNavigationSfx();
        SoundManager.Instance.SfxPlaying = isPlaying;
    }

}
