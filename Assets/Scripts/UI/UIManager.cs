using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    private const string PP_GAME_TIME = "ConfigGameTimeSeconds";

    private const string InGameUiLayerName = "InGameUI";
    private const string PreGameUiLayerName = "PreGameUI";
    private const string SettingsUiLayerName = "SettingsUI";
    [SerializeField] private TextMeshProUGUI playerScore;
    [SerializeField] private TextMeshProUGUI enemyScore;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private GameOverPopup gameOverPopup;
    [SerializeField] private ZoneManager zoneManager;
    private float matchDurationSeconds = ConfigPopup.DefaultGameTimeSeconds;

    public float MatchDurationSeconds => matchDurationSeconds;

    private int popupsActive;
    private int playerCapturedCount;
    private int enemyCapturedCount;
    private int totalZones;

    private float timeRemaining;
    private bool isMatchRunning;
    private int inGameUiLayer;
    private int preGameUiLayer;
    private int settingsUiLayer;

    private const string PlayerScoreLabelFormat = "P1: {0}/{1}";
    private const string EnemyScoreLabelFormat = "AI: {0}/{1}";

    private void Awake()
    {
        //Get the layer index 
        inGameUiLayer = LayerMask.NameToLayer(InGameUiLayerName);
        preGameUiLayer = LayerMask.NameToLayer(PreGameUiLayerName);
        settingsUiLayer = LayerMask.NameToLayer(SettingsUiLayerName);

        Messenger.AddListener(GameEvent.POPUP_OPENED, OnPopupOpened);
        Messenger.AddListener(GameEvent.POPUP_CLOSED, OnPopupClosed);
        Messenger.AddListener(GameEvent.GAMEPLAY_STARTED, OnGameplayStarted);
        Messenger.AddListener(GameEvent.GAME_RESET_REQUESTED, OnGameResetRequested);
        Messenger<Zone>.AddListener(GameEvent.ZONE_BECAME_NEUTRAL, OnZoneOwnershipChanged);
        Messenger<Zone>.AddListener(GameEvent.ZONE_BECAME_CONTESTED, OnZoneOwnershipChanged);
        Messenger<Zone>.AddListener(GameEvent.ZONE_BECAME_PLAYER, OnZoneOwnershipChanged);
        Messenger<Zone>.AddListener(GameEvent.ZONE_BECAME_ENEMY, OnZoneOwnershipChanged);
        Messenger<Zone>.AddListener(GameEvent.PLAYER_CAPTURED_ZONE, OnPlayerCapturedZone);
        Messenger<Zone>.AddListener(GameEvent.ENEMY_CAPTURED_ZONE, OnEnemyCapturedZone);

        LoadMatchDurationFromPlayerPrefs();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveMatchDurationPrefs();
        }
    }

    private void OnDestroy()
    {
        SaveMatchDurationPrefs();
        Messenger.RemoveListener(GameEvent.POPUP_OPENED, OnPopupOpened);
        Messenger.RemoveListener(GameEvent.POPUP_CLOSED, OnPopupClosed);
        Messenger.RemoveListener(GameEvent.GAMEPLAY_STARTED, OnGameplayStarted);
        Messenger.RemoveListener(GameEvent.GAME_RESET_REQUESTED, OnGameResetRequested);
        Messenger<Zone>.RemoveListener(GameEvent.ZONE_BECAME_NEUTRAL, OnZoneOwnershipChanged);
        Messenger<Zone>.RemoveListener(GameEvent.ZONE_BECAME_CONTESTED, OnZoneOwnershipChanged);
        Messenger<Zone>.RemoveListener(GameEvent.ZONE_BECAME_PLAYER, OnZoneOwnershipChanged);
        Messenger<Zone>.RemoveListener(GameEvent.ZONE_BECAME_ENEMY, OnZoneOwnershipChanged);
        Messenger<Zone>.RemoveListener(GameEvent.PLAYER_CAPTURED_ZONE, OnPlayerCapturedZone);
        Messenger<Zone>.RemoveListener(GameEvent.ENEMY_CAPTURED_ZONE, OnEnemyCapturedZone);
    }

    private void LoadMatchDurationFromPlayerPrefs()
    {
        matchDurationSeconds = Mathf.Max(
            0f,
            PlayerPrefs.GetInt(PP_GAME_TIME, Mathf.RoundToInt(matchDurationSeconds)));
    }

    private void SaveMatchDurationPrefs()
    {
        PlayerPrefs.SetInt(PP_GAME_TIME, Mathf.RoundToInt(matchDurationSeconds));
        PlayerPrefs.Save();
    }

    private void Start()
    {
        SetGameActive(true);
        RefreshZoneCounts();
        UpdateScoreLabels();
        UpdateTimerLabel(Mathf.Max(0f, matchDurationSeconds));
    }

    private void Update()
    {
        if (!isMatchRunning)
        {
            return;
        }

        if (IsUntimedMode())
        {
            if (AreAllZonesOwned())
            {
                EndMatchAndBroadcastResult();
            }
        }
        else
        {
            timeRemaining -= Time.deltaTime;
            if (timeRemaining <= 0f)
            {
                timeRemaining = 0f;
                EndMatchAndBroadcastResult();
            }

            UpdateTimerLabel(timeRemaining);
        }
    }

    private void OnPopupOpened()
    {
        if (popupsActive == 0)
        {
            SetGameActive(false);
        }
        popupsActive++;
    }

    private void OnPopupClosed()
    {
        popupsActive = Mathf.Max(0, popupsActive - 1);

        if (popupsActive == 0)
        {
            SetGameActive(true);
        }
    }

    private void OnGameplayStarted()
    {
        SetChildrenActiveForLayer(inGameUiLayer, true);
        SetChildrenActiveForLayer(preGameUiLayer, false);

        RefreshZoneCounts();
        UpdateScoreLabels();

        timeRemaining = Mathf.Max(0f, matchDurationSeconds);
        isMatchRunning = true;
        UpdateTimerLabel(timeRemaining);
    }

    private void OnGameResetRequested()
    {
        isMatchRunning = false;
        popupsActive = 0;
        timeRemaining = Mathf.Max(0f, matchDurationSeconds);
        RefreshZoneCounts();
        UpdateScoreLabels();
        UpdateTimerLabel(timeRemaining);
        SetChildrenActiveForLayer(inGameUiLayer, false);
        SetChildrenActiveForLayer(preGameUiLayer, true);
        SetChildrenActiveForLayer(settingsUiLayer, true);
        SetGameActive(true);
    }

    public void SetMatchDurationSeconds(float value)
    {
        matchDurationSeconds = Mathf.Max(0f, value);

        if (!isMatchRunning)
        {
            UpdateTimerLabel(matchDurationSeconds);
            return;
        }

        if (IsUntimedMode())
        {
            timeRemaining = 0f;
            UpdateTimerLabel(timeRemaining);
            return;
        }

        timeRemaining = Mathf.Max(0f, matchDurationSeconds);
        UpdateTimerLabel(timeRemaining);
    }

    private void SetChildrenActiveForLayer(int targetLayer, bool active)
    {
        ApplyLayerToHierarchy(transform, targetLayer, active);
    }

    // Toggles every object in the subtree whose GameObject.layer matches (use Unity Layer "InGameUI", not the Tag field).
    private static void ApplyLayerToHierarchy(Transform root, int targetLayer, bool active)
    {
        if (root.gameObject.layer == targetLayer)
        {
            root.gameObject.SetActive(active);
        }

        foreach (Transform child in root)
        {
            ApplyLayerToHierarchy(child, targetLayer, active);
        }
    }

    private void OnPlayerCapturedZone(Zone capturedZone)
    {
        _ = capturedZone;
        RefreshZoneCounts();
        UpdateScoreLabels();
    }

    private void OnEnemyCapturedZone(Zone capturedZone)
    {
        _ = capturedZone;
        RefreshZoneCounts();
        UpdateScoreLabels();
    }

    private void OnZoneOwnershipChanged(Zone changedZone)
    {
        _ = changedZone;
        RefreshZoneCounts();
        UpdateScoreLabels();
    }

    private void RefreshZoneCounts()
    {
        playerCapturedCount = 0;
        enemyCapturedCount = 0;
        totalZones = 0;


        totalZones = zoneManager.zones.Count;
        foreach (Zone zone in zoneManager.zones)
        {
            if (zone.Owner == ZoneManager.ZoneOwner.Player)
            {
                playerCapturedCount++;
            }
            else if (zone.Owner == ZoneManager.ZoneOwner.Enemy)
            {
                enemyCapturedCount++;
            }
        }
    }

    private void UpdateScoreLabels()
    {
   
        playerScore.text = string.Format(PlayerScoreLabelFormat, playerCapturedCount, totalZones);
    
        enemyScore.text = string.Format(EnemyScoreLabelFormat, enemyCapturedCount, totalZones); 
       
    }

    private void UpdateTimerLabel(float remainingSeconds)
    {
  
        int secondsRemaining = Mathf.CeilToInt(Mathf.Max(0f, remainingSeconds));
        int minutes = secondsRemaining / 60;
        int seconds = secondsRemaining % 60;
        timerText.text = $"{minutes}:{seconds:00}";
    }

    private bool IsUntimedMode()
    {
        return Mathf.Approximately(matchDurationSeconds, 0f);
    }

    private bool AreAllZonesOwned()
    {
        if (zoneManager.zones.Count == 0)
        {
            return false;
        }

        foreach (Zone zone in zoneManager.zones)
        {
            if (zone.Owner != ZoneManager.ZoneOwner.Player && zone.Owner != ZoneManager.ZoneOwner.Enemy)
            {
                return false;
            }
        }

        return true;
    }

    private void EndMatchAndBroadcastResult()
    {
        RefreshZoneCounts();
        UpdateScoreLabels();
        isMatchRunning = false;
        BroadcastMatchResult();
    }

    private void BroadcastMatchResult()
    {
        SetChildrenActiveForLayer(inGameUiLayer, false);
        SetChildrenActiveForLayer(settingsUiLayer, false);

        if (playerCapturedCount > enemyCapturedCount)
        {
            gameOverPopup.ShowPlayerWinResult();
        }
        else if (enemyCapturedCount > playerCapturedCount)
        {
            gameOverPopup.ShowEnemyWinResult();
        }
        else
        {
            gameOverPopup.ShowTieResult();
        }
    }

    public void SetGameActive(bool active)
    {
        if (active)
        {
            Time.timeScale = 1;
            Messenger.Broadcast(GameEvent.GAME_ACTIVE);
        }
        else
        {
            Time.timeScale = 0;
            Messenger.Broadcast(GameEvent.GAME_INACTIVE);
        }
    }
}
