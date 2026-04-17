using UnityEngine;

public class SettingsPopup : BasePopup
{
    [SerializeField] private AudioPopup audioPopup;
    [SerializeField] private MiscPopup miscPopup;

    public void OnAudioButton()
    {
        PlayNavigationSfx();
        audioPopup.Open();
        Close();
    }

    public void OnMiscButton()
    {
        PlayNavigationSfx();
        miscPopup.Open();
        Close();
    }

    public void OnResetButton()
    {
        PlayNavigationSfx();
        SoundManager.Instance.StopMusic();

        if (IsActive())
        {
            Close();
        }

        Messenger.Broadcast(GameEvent.GAME_RESET_REQUESTED, MessengerMode.DONT_REQUIRE_LISTENER);
    }

}