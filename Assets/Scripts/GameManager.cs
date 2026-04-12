using UnityEngine;
using UnityEngine.AI;

// Owns gameplay-only orchestration after setup is complete.
public class GameManager : MonoBehaviour
{
    private const float EnemySpawnSampleRadius = 0.75f;

    // Try to spawn the enemy in 6 random zones so we have a better chance of finding a valid zone.
    private const int EnemySpawnZoneAttempts = 6;

    [SerializeField] private ZoneManager zoneManager;

    [SerializeField] private GameObject enemyPrefab;

    private void OnEnable()
    {
        Messenger.AddListener(GameEvent.GAMEPLAY_STARTED, OnGameplayStarted);
        Messenger<Zone>.AddListener(GameEvent.PLAYER_CAPTURED_ZONE, OnPlayerCapturedZone);
    }

    private void OnDisable()
    {
        Messenger.RemoveListener(GameEvent.GAMEPLAY_STARTED, OnGameplayStarted);
        Messenger<Zone>.RemoveListener(GameEvent.PLAYER_CAPTURED_ZONE, OnPlayerCapturedZone);
    }

    private void OnGameplayStarted()
    {
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

            GameObject spawnedEnemyObject = Instantiate(enemyPrefab, navHit.position, Quaternion.identity);
            Enemy spawnedEnemy = spawnedEnemyObject.GetComponentInChildren<Enemy>(true);
            if (spawnedEnemy != null)
            {
                // Set the zone manager for the enemy, so it can access zones and navmesh state.
                spawnedEnemy.ZoneManager = zoneManager;
            }

            return;
        }
    }

    private void OnPlayerCapturedZone(Zone capturedZone)
    {
        Debug.Log($"Player captured zone: {capturedZone.gameObject.name}; Vibrate Device");
        Handheld.Vibrate();
    }
}
