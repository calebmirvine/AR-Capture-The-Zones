using TMPro;
using UnityEngine;

public class GhostModeUI : MonoBehaviour
{
    [SerializeField] private HealthSystem healthSystem;
    [SerializeField] private TextMeshProUGUI remainingLabel;
    [SerializeField] private GameObject ghostPlayerPanel;

    private void Awake()
    {
        if (!healthSystem)
        {
            healthSystem = FindAnyObjectByType<HealthSystem>();
        }
    }

    private void Update()
    {
        bool shouldShow = healthSystem.IsGhost;
        ghostPlayerPanel.SetActive(shouldShow);

        if (!shouldShow)
        {
            return;
        }

        float remaining = Mathf.Max(0f, healthSystem.GhostTimeRemaining);
        int totalSeconds = Mathf.CeilToInt(remaining);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        remainingLabel.text = string.Format("{0}:{1:00} Remaining", minutes, seconds);
    }
}
