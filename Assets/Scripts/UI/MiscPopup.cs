using UnityEngine;
using UnityEngine.UI;

public class MiscPopup : BasePopup
{
    private const string PP_VIBRATION_ENABLED = "VibrationEnabled";

    [SerializeField] private SettingsPopup settingsPopup;
    [SerializeField] private Toggle vibrationToggle;

    private void OnEnable()
    {
        SyncVibrationToggle();
    }

    public void OnBackButton()
    {
        PlayNavigationSfx();

        settingsPopup.Open();
        Close();
    }

    public void OnVibrationToggleChanged(bool isEnabled)
    {
        PlayNavigationSfx();
        PlayerPrefs.SetInt(PP_VIBRATION_ENABLED, isEnabled ? 1 : 0);

        if (isEnabled)
        {
            Handheld.Vibrate();
            Messenger.Broadcast(GameEvent.VIBRATION_ENABLED, MessengerMode.DONT_REQUIRE_LISTENER);
        }
        else
        {
            Messenger.Broadcast(GameEvent.VIBRATION_DISABLED, MessengerMode.DONT_REQUIRE_LISTENER);
        }
    }

    private void SyncVibrationToggle()
    {
        bool isEnabled = PlayerPrefs.GetInt(PP_VIBRATION_ENABLED, 1) == 1;
        vibrationToggle.SetIsOnWithoutNotify(isEnabled);
    }

    private void PlayNavigationSfx()
    {
        SoundManager.Instance.PlaySfx(SoundLibrary.Instance.MenuNavSfx);
    }
}
