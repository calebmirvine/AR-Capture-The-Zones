using UnityEngine;

public class SettingsPopup : BasePopup
{
    [SerializeField] private AudioPopup audioPopup;
    [SerializeField] private MiscPopup miscPopup;

    public void OnAudioButton()
    {
        if (audioPopup == null)
        {
            Debug.LogWarning("SettingsPopup audioPopup reference is missing.");
            return;
        }

        audioPopup.Open();
        Close();
    }

    public void OnMiscButton()
    {
        if (miscPopup == null)
        {
            Debug.LogWarning("SettingsPopup miscPopup reference is missing.");
            return;
        }

        miscPopup.Open();
        Close();
    }

    public void OnResetButton()
    {
        Debug.Log("Reset clicked (not implemented yet).");
    }

    public void OnReturnToGameButton()
    {
        Close();
    }
}