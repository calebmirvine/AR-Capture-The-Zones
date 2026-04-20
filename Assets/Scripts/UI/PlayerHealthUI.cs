using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
    [SerializeField] private HealthSystem healthSystem;
    [SerializeField] private TextMeshProUGUI healthLabel;
    [SerializeField] private Image healthBar;

    private const string HealthFormat = "{0}/{1}";

    private void OnEnable()
    {
        if (!healthSystem)
        {
            healthSystem = FindAnyObjectByType<HealthSystem>();
        }

        Messenger<float>.AddListener(GameEvent.PLAYER_HEALTH_CHANGED, UpdateHealth);
        UpdateHealth(healthSystem.HealthNormalized);
    }

    private void OnDisable()
    {
        Messenger<float>.RemoveListener(GameEvent.PLAYER_HEALTH_CHANGED, UpdateHealth);
    }

    private void Update()
    {
        int current = healthSystem.CurrentHealth;
        int maximum = healthSystem.MaxHealth;
        healthLabel.text = string.Format(HealthFormat, current, maximum);
    }

    private void UpdateHealth(float healthPercentage)
    {
        Color lowHealthColor = Color.red;
        Color fullHealthColor = Color.green;
        Color displayColor = Color.Lerp(lowHealthColor, fullHealthColor, healthPercentage);

        healthBar.fillAmount = healthPercentage;
        healthBar.color = displayColor;
    }
}
