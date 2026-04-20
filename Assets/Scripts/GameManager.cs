using UnityEngine;
using UnityEngine.AI;

// Owns gameplay-only orchestration after setup is complete.
public class GameManager : MonoBehaviour
{
    private const float EnemySpawnSampleRadius = 0.75f;
    private const string PP_VIBRATION_ENABLED = "VibrationEnabled";
    // Try to spawn the enemy in 6 random zones so we have a better chance of finding a valid zone.
    private const int EnemySpawnZoneAttempts = 6;
    [SerializeField] private ZoneManager zoneManager;
    [SerializeField] private GameObject enemyPrefab;
    private GameObject activeEnemy;
    private bool vibrationEnabled = true;

    private void Awake() =>
        vibrationEnabled = PlayerPrefs.GetInt(PP_VIBRATION_ENABLED, 1) == 1;
        

    private void OnEnable()
    {
        Messenger.AddListener(GameEvent.GAMEPLAY_STARTED, OnGameplayStarted);
        Messenger.AddListener(GameEvent.GAME_RESET_REQUESTED, OnGameResetRequested);
        Messenger.AddListener(GameEvent.VIBRATION_ENABLED, OnVibrationEnabled);
        Messenger.AddListener(GameEvent.VIBRATION_DISABLED, OnVibrationDisabled);
        Messenger<Zone>.AddListener(GameEvent.PLAYER_CAPTURED_ZONE, OnPlayerCapturedZone);
    }

    private void OnDisable()
    {
        Messenger.RemoveListener(GameEvent.GAMEPLAY_STARTED, OnGameplayStarted);
        Messenger.RemoveListener(GameEvent.GAME_RESET_REQUESTED, OnGameResetRequested);
        Messenger.RemoveListener(GameEvent.VIBRATION_ENABLED, OnVibrationEnabled);
        Messenger.RemoveListener(GameEvent.VIBRATION_DISABLED, OnVibrationDisabled);
        Messenger<Zone>.RemoveListener(GameEvent.PLAYER_CAPTURED_ZONE, OnPlayerCapturedZone);
    }

    private void OnGameplayStarted()
    {
        DestroyActiveEnemy();
        SoundManager.Instance.PlayMusicPlaylist(SoundLibrary.Instance.GetMusicPlaylist());
        SpawnEnemyInRandomZone();
    }

    private void SpawnEnemyInRandomZone()
    {
        for (int attempt = 0; attempt < EnemySpawnZoneAttempts; attempt++)
        {
            Zone randomZone = zoneManager.GetRandomZone();
            if (randomZone == null)
            {
                continue;
            }

            Vector3 zonePosition = randomZone.transform.position;
            if (!NavMesh.SamplePosition(zonePosition, out NavMeshHit navHit, EnemySpawnSampleRadius, NavMesh.AllAreas))
            {
                continue;
            }

            activeEnemy = Instantiate(enemyPrefab, navHit.position, Quaternion.identity);
            Enemy spawnedEnemy = activeEnemy.GetComponentInChildren<Enemy>(true);
            if (spawnedEnemy != null)
            {
                // Set the zone manager for the enemy, so it can access zones and navmesh state.
                spawnedEnemy.ZoneManager = zoneManager;
            }

            return;
        }
    }

    private void OnPlayerCapturedZone(Zone capturedZone) => Handheld.Vibrate();

    private void OnGameResetRequested() => DestroyActiveEnemy();

    private void DestroyActiveEnemy()
    {
        if (activeEnemy == null)
        {
            return;
        }

        Destroy(activeEnemy);
        activeEnemy = null;
    }

    private void OnVibrationEnabled()
    {
        vibrationEnabled = true;
        PlayerPrefs.SetInt(PP_VIBRATION_ENABLED, 1);
    }

    private void OnVibrationDisabled()
    {
        vibrationEnabled = false;
        PlayerPrefs.SetInt(PP_VIBRATION_ENABLED, 0);
    }
}
