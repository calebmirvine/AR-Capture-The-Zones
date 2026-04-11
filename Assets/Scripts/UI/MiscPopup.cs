using UnityEngine;

public class MiscPopup : BasePopup
{
    [SerializeField] private SettingsPopup settingsPopup;

    public void OnBackButton()
    {
        if (settingsPopup == null)
        {
            Debug.LogWarning("MiscPopup settingsPopup reference is missing.");
            return;
        }

        settingsPopup.Open();
        Close();
    }
}
