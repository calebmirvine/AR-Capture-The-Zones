using UnityEngine;

public class ConfigButton : MonoBehaviour
{
    [SerializeField] private ConfigPopup configPopup;

    public void OnConfigButton()
    {
        PlayNavigationSfx();

        if (configPopup.IsActive())
        {
            configPopup.Close();
            return;
        }

        configPopup.Open();
    }

    private void PlayNavigationSfx()
    {
        SoundManager.Instance.PlayOneShot(SoundLibrary.Instance.MenuNavSfx);
    }
}