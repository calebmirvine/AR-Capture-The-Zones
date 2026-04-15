using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnItems : MonoBehaviour
{
    [SerializeField] private ZoneManager zoneManager;

    [SerializeField] private List<GameObject> pickupPrefabs = new List<GameObject>();

    [SerializeField] private float initialSpawnDelay = 5f;

    [SerializeField] private float minSpawnDelay = 3f;

    [SerializeField] private float maxSpawnDelay = 7f;

    [SerializeField] private float spawnHeightOffset = 0.05f;

    private Coroutine spawnLoopCoroutine;
    private bool hasStartedSpawning;
    private readonly List<GameObject> activePickups = new List<GameObject>();

    private void OnEnable()
    {
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
        if (hasStartedSpawning)
        {
            return;
        }

        if (zoneManager == null || pickupPrefabs.Count == 0)
        {
            Debug.LogError("Zone manager or pickup prefabs not set");
            return;
        }

        hasStartedSpawning = true;
        spawnLoopCoroutine = StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        yield return new WaitForSeconds(initialSpawnDelay);

        float lowDelay = Mathf.Min(minSpawnDelay, maxSpawnDelay);
        float highDelay = Mathf.Max(minSpawnDelay, maxSpawnDelay);

        while (true)
        {
            GameObject pickupPrefab = pickupPrefabs[Random.Range(0, pickupPrefabs.Count)];
            Zone randomZone = zoneManager.GetRandomNeutralFirstZone();
            if (randomZone == null)
            {
                yield return new WaitForSeconds(lowDelay);
                continue;
            }

            Vector3 spawnPosition = randomZone.GetRandomWorldPointInside();
            spawnPosition.y += spawnHeightOffset;

            GameObject spawnedPickup = Instantiate(pickupPrefab, spawnPosition, Quaternion.identity);
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
