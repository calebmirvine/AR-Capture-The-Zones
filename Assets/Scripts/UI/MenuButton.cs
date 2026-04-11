using UnityEngine;

public class MenuButton : MonoBehaviour
{
    [SerializeField] private SettingsPopup settingsPopup;

    public void OnMenuButton()
    {
        if (settingsPopup == null)
        {
            Debug.LogWarning("MenuButton settingsPopup reference is missing.");
            return;
        }

        if (settingsPopup.IsActive())
        {
            settingsPopup.Close();
            return;
        }

        settingsPopup.Open();
    }
}