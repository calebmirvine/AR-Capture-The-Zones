using UnityEngine;

public class AudioPopup : BasePopup
{
    [SerializeField] private SettingsPopup settingsPopup;

    public void OnBackButton()
    {
        settingsPopup.Open();
        Close();
    }
}
