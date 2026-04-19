using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns pickup prefabs in random neutral zones while gameplay is active, up to <see cref="maxActivePickups"/>.
/// </summary>
public class SpawnItems : MonoBehaviour
{
    [SerializeField] private ZoneManager zoneManager;

    [SerializeField] private List<GameObject> pickupPrefabs = new List<GameObject>();

    [SerializeField] private float minSpawnDelay = 3f;

    [SerializeField] private float maxSpawnDelay = 7f;

    [SerializeField] private int maxActivePickups = 2;

    [SerializeField] private float spawnHeightOffset = 0.5f;

    private Coroutine spawnLoopCoroutine;
    private bool hasStartedSpawning;
    private readonly List<GameObject> activePickups = new List<GameObject>();

    private void OnEnable()
    {
        // GAME_RESET_REQUESTED stops the loop and clears pickups; GAMEPLAY_STARTED begins fresh.
        Messenger.AddListener(GameEvent.GAMEPLAY_STARTED, OnGameplayStarted);
        Messenger.AddListener(GameEvent.GAME_RESET_REQUESTED, OnGameResetRequested);
    }

    private void OnDisable()
    {
        Messenger.RemoveListener(GameEvent.GAMEPLAY_STARTED, OnGameplayStarted);
        Messenger.RemoveListener(GameEvent.GAME_RESET_REQUESTED, OnGameResetRequested);
        StopSpawnLoop();
        DestroyAllSpawnedPickups();
    }

    private void OnGameplayStarted()
    {
        DestroyAllSpawnedPickups();
        SpawnInitialPickups();
    }

    public void SpawnInitialPickups()
    {
        // One coroutine per session; reset clears this via StopSpawnLoop.
        if (hasStartedSpawning)
        {
            return;
        }

        hasStartedSpawning = true;
        spawnLoopCoroutine = StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        // Normalize in case min/max are reversed in the inspector.
        float lowDelay = Mathf.Min(minSpawnDelay, maxSpawnDelay);
        float highDelay = Mathf.Max(minSpawnDelay, maxSpawnDelay);

        while (true)
        {
            // Drop destroyed (collected) pickups before enforcing the cap.
            activePickups.RemoveAll(pickup => pickup == null);

            if (activePickups.Count >= maxActivePickups)
            {
                yield return new WaitForSeconds(lowDelay);
                continue;
            }

            GameObject pickupPrefab = pickupPrefabs[Random.Range(0, pickupPrefabs.Count)];
            Zone randomZone = zoneManager.GetRandomNeutralFirstZone();
            // No eligible zone yet — retry after a short wait instead of busy-looping.
            if (randomZone == null)
            {
                yield return new WaitForSeconds(lowDelay);
                continue;
            }

            Vector3 spawnPosition = randomZone.GetRandomWorldPointInside();
            spawnPosition.y += spawnHeightOffset;
            GameObject spawnedPickup = Instantiate(pickupPrefab, spawnPosition, Quaternion.identity);
            Pickup spawnedPickupComponent = spawnedPickup.GetComponent<Pickup>();
            if (spawnedPickupComponent != null)
            {
                spawnedPickupComponent.Init(zoneManager);
            }

            activePickups.Add(spawnedPickup);

            float nextDelay = Random.Range(lowDelay, highDelay);
            yield return new WaitForSeconds(nextDelay);
        }
    }

    private void StopSpawnLoop()
    {
        if (spawnLoopCoroutine != null)
        {
            StopCoroutine(spawnLoopCoroutine);
            spawnLoopCoroutine = null;
        }

        hasStartedSpawning = false;
    }

    private void OnGameResetRequested()
    {
        StopSpawnLoop();
        DestroyAllSpawnedPickups();
    }

    private void DestroyAllSpawnedPickups()
    {
        foreach (GameObject activePickup in activePickups)
        {
            if (activePickup == null)
            {
                continue;
            }

            Destroy(activePickup);
        }

        activePickups.Clear();
    }
}
