using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    private const string InGameUiLayerName = "InGameUI";
    private const string PreGameUiLayerName = "PreGameUI";
    [SerializeField] private TextMeshProUGUI playerScore;
    [SerializeField] private TextMeshProUGUI enemyScore;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private ZoneManager zoneManager;
    [SerializeField] private float matchDurationSeconds = 60f;

    private int popupsActive;
    private int playerCapturedCount;
    private int enemyCapturedCount;
    private int totalZones;

    private float timeRemaining;
    private bool isMatchRunning;
    private int inGameUiLayer;
    private int preGameUiLayer;

    private const string PlayerScoreLabelFormat = "P1: {0}/{1}";
    private const string EnemyScoreLabelFormat = "AI: {0}/{1}";

    private void Awake()
    {
        //Get the layer index 
        inGameUiLayer = LayerMask.NameToLayer(InGameUiLayerName);
        preGameUiLayer = LayerMask.NameToLayer(PreGameUiLayerName);

        Messenger.AddListener(GameEvent.POPUP_OPENED, OnPopupOpened);
        Messenger.AddListener(GameEvent.POPUP_CLOSED, OnPopupClosed);
        Messenger.AddListener(GameEvent.GAMEPLAY_STARTED, OnGameplayStarted);
        Messenger.AddListener(GameEvent.GAME_RESET_REQUESTED, OnGameResetRequested);
        Messenger<Zone>.AddListener(GameEvent.PLAYER_CAPTURED_ZONE, OnPlayerCapturedZone);
        Messenger<Zone>.AddListener(GameEvent.ENEMY_CAPTURED_ZONE, OnEnemyCapturedZone);
    }

    private void OnDestroy()
    {
        Messenger.RemoveListener(GameEvent.POPUP_OPENED, OnPopupOpened);
        Messenger.RemoveListener(GameEvent.POPUP_CLOSED, OnPopupClosed);
        Messenger.RemoveListener(GameEvent.GAMEPLAY_STARTED, OnGameplayStarted);
        Messenger.RemoveListener(GameEvent.GAME_RESET_REQUESTED, OnGameResetRequested);
        Messenger<Zone>.RemoveListener(GameEvent.PLAYER_CAPTURED_ZONE, OnPlayerCapturedZone);
        Messenger<Zone>.RemoveListener(GameEvent.ENEMY_CAPTURED_ZONE, OnEnemyCapturedZone);
    }

    private void Start()
    {
        SetGameActive(true);
        RefreshZoneCounts();
        UpdateScoreLabels();
        UpdateTimerLabel(matchDurationSeconds);
    }

    private void Update()
    {
        if (!isMatchRunning)
        {
            return;
        }

        timeRemaining -= Time.deltaTime;
        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            isMatchRunning = false;
            Messenger.Broadcast(GameEvent.GAME_TIMER_EXPIRED, MessengerMode.DONT_REQUIRE_LISTENER);
        }

        UpdateTimerLabel(timeRemaining);
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
        if (popupsActive > 0)
        {
            popupsActive--;
        }
        else
        {
            popupsActive = 0;
            Debug.LogWarning("UIManager received POPUP_CLOSED with no active popups.");
        }

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
        SetGameActive(true);
    }

    private void SetChildrenActiveForLayer(int targetLayer, bool active)
    {
        if (targetLayer < 0)
        {
            return;
        }

        SetChildrenActiveForLayerRecursive(transform, targetLayer, active);
    }

    private void SetChildrenActiveForLayerRecursive(Transform current, int targetLayer, bool active)
    {
        foreach (Transform child in current)
        {
            if (child.gameObject.layer == targetLayer)
            {
                child.gameObject.SetActive(active);
            }

            SetChildrenActiveForLayerRecursive(child, targetLayer, active);
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

    private void RefreshZoneCounts()
    {
        playerCapturedCount = 0;
        enemyCapturedCount = 0;
        totalZones = 0;


        totalZones = zoneManager.zones.Count;
        foreach (Zone zone in zoneManager.zones)
        {
            if (zone == null)
            {
                continue;
            }

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
