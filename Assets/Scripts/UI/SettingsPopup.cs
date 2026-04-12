using UnityEngine;

public class SettingsPopup : BasePopup
{
    [SerializeField] private AudioPopup audioPopup;
    [SerializeField] private MiscPopup miscPopup;

    public void OnAudioButton()
    {
        audioPopup.Open();
        Close();
    }

    public void OnMiscButton()
    {
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