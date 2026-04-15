using UnityEngine;

public class MenuButton : MonoBehaviour
{
    [SerializeField] private SettingsPopup settingsPopup;

    public void OnMenuButton()
    {
        PlayNavigationSfx();

        if (settingsPopup.IsActive())
        {
            settingsPopup.Close();
            return;
        }

        settingsPopup.Open();
    }

    private void PlayNavigationSfx()
    {
        SoundManager.Instance.PlaySfx(SoundLibrary.Instance.MenuNavSfx);
    }
}