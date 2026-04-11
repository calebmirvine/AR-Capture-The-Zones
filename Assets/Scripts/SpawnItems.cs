using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnItems : MonoBehaviour
{
    [SerializeField]
    private ZoneManager zoneManager;

    [SerializeField]
    private List<GameObject> pickupPrefabs = new List<GameObject>();

    [SerializeField]
    private float initialSpawnDelay = 5f;

    [SerializeField]
    private float minSpawnDelay = 3f;

    [SerializeField]
    private float maxSpawnDelay = 7f;

    [SerializeField]
    private float spawnHeightOffset = 0.05f;

    private Coroutine spawnLoopRoutine;

    public void SpawnInitialPickups() {
        spawnLoopRoutine = StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop() {
        yield return new WaitForSeconds(initialSpawnDelay);

        float lowDelay = Mathf.Min(minSpawnDelay, maxSpawnDelay);
        float highDelay = Mathf.Max(minSpawnDelay, maxSpawnDelay);

        while (true) {
            GameObject pickupPrefab = pickupPrefabs[Random.Range(0, pickupPrefabs.Count)];
            Zone randomZone = zoneManager.GetRandomNeutralFirstZone();
            Vector3 spawnPosition = randomZone.GetRandomWorldPointInside();
            spawnPosition.y += spawnHeightOffset;

            Instantiate(pickupPrefab, spawnPosition, Quaternion.identity);

            float nextDelay = Random.Range(lowDelay, highDelay);
            yield return new WaitForSeconds(nextDelay);
        }
    }
}
