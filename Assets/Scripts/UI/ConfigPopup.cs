using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ConfigPopup : BasePopup
{
    public const int DefaultGameTimeSeconds = 120;
    public const int MinMatchDurationSeconds = 30;
    public const int MaxMatchDurationSeconds = 300;

    private const string RowsLabelFormat = "Rows: {0}";
    private const string ColumnsLabelFormat = "Columns: {0}";
    private const string PlayerCaptureLabelFormat = "Player: {0}";
    private const string EnemyCaptureLabelFormat = "Enemy: {0}";
    private const string GameTimeLabelFormat = "Time: {0}:{1:00}";

    [SerializeField] private Slider rowsSlider;
    [SerializeField] private Slider columnsSlider;
    [SerializeField] private Slider playerCaptureSlider;
    [SerializeField] private Slider enemyCaptureSlider;
    [SerializeField] private Slider gameTimeSlider;
    [SerializeField] private TMP_Text rowsText;
    [SerializeField] private TMP_Text columnsText;
    [SerializeField] private TMP_Text playerCaptureText;
    [SerializeField] private TMP_Text enemyCaptureText;
    [SerializeField] private TMP_Text gameTimeText;
    [SerializeField] private ZoneManager zoneManager;
    [SerializeField] private UIManager uiManager;

    private void OnEnable()
    {
        SyncSliderValuesFromManagers();
    }

    public void OnRowsSliderValueChanged(float value)
    {
        int rows = Mathf.RoundToInt(value);
        UpdateRowsLabel(rows);
        zoneManager.SetRows(rows);
    }

    public void OnColumnsSliderValueChanged(float value)
    {
        int columns = Mathf.RoundToInt(value);
        UpdateColumnsLabel(columns);
        zoneManager.SetColumns(columns);
    }

    public void OnPlayerCaptureSliderValueChanged(float value)
    {
        int playerCaptureSeconds = Mathf.RoundToInt(value);
        UpdatePlayerCaptureLabel(playerCaptureSeconds);
        zoneManager.SetPlayerSecondsToCapture(playerCaptureSeconds);
    }

    public void OnEnemyCaptureSliderValueChanged(float value)
    {
        int enemyCaptureSeconds = Mathf.RoundToInt(value);
        UpdateEnemyCaptureLabel(enemyCaptureSeconds);
        zoneManager.SetEnemySecondsToCapture(enemyCaptureSeconds);
    }

    public void OnGameTimeSliderValueChanged(float value)
    {
        int gameTimeSeconds = Mathf.RoundToInt(
            Mathf.Clamp(value, MinMatchDurationSeconds, MaxMatchDurationSeconds));
        UpdateGameTimeLabel(gameTimeSeconds);
        uiManager.SetMatchDurationSeconds(gameTimeSeconds);
    }

    private void SyncSliderValuesFromManagers()
    {
        rowsSlider.SetValueWithoutNotify(zoneManager.Rows);
        columnsSlider.SetValueWithoutNotify(zoneManager.Columns);
        playerCaptureSlider.SetValueWithoutNotify(zoneManager.PlayerSecondsToCapture);
        enemyCaptureSlider.SetValueWithoutNotify(zoneManager.EnemySecondsToCapture);
        float clampedGameTime = Mathf.Clamp(
            uiManager.MatchDurationSeconds,
            MinMatchDurationSeconds,
            MaxMatchDurationSeconds);
        gameTimeSlider.SetValueWithoutNotify(clampedGameTime);

        UpdateRowsLabel(zoneManager.Rows);
        UpdateColumnsLabel(zoneManager.Columns);
        UpdatePlayerCaptureLabel(Mathf.RoundToInt(zoneManager.PlayerSecondsToCapture));
        UpdateEnemyCaptureLabel(Mathf.RoundToInt(zoneManager.EnemySecondsToCapture));
        UpdateGameTimeLabel(Mathf.RoundToInt(clampedGameTime));
    }

    private void UpdateRowsLabel(int value)
    {
        rowsText.text = string.Format(RowsLabelFormat, value);
    }

    private void UpdateColumnsLabel(int value)
    {
        columnsText.text = string.Format(ColumnsLabelFormat, value);
    }

    private void UpdatePlayerCaptureLabel(int value)
    {
        playerCaptureText.text = string.Format(PlayerCaptureLabelFormat, value);
    }

    private void UpdateEnemyCaptureLabel(int value)
    {
        enemyCaptureText.text = string.Format(EnemyCaptureLabelFormat, value);
    }

    private void UpdateGameTimeLabel(int totalSeconds)
    {
        int safeSeconds = Mathf.Clamp(totalSeconds, MinMatchDurationSeconds, MaxMatchDurationSeconds);
        int minutes = safeSeconds / 60;
        int seconds = safeSeconds % 60;
        gameTimeText.text = string.Format(GameTimeLabelFormat, minutes, seconds);
    }
}
