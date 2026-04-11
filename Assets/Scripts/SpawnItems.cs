using System.Collections.Generic;
using UnityEngine;

public class SpawnItems : MonoBehaviour
{
    [SerializeField]
    private ZoneManager zoneManager;

    [SerializeField]
    private List<GameObject> pickupPrefabs = new List<GameObject>();

    [SerializeField]
    private int totalPickupCount = 6;

    [SerializeField]
    private float spawnHeightOffset = 0.05f;

    public void SpawnInitialPickups() {
        if (zoneManager == null) return;
        if (zoneManager.GetZoneCount() == 0) return;
        if (pickupPrefabs.Count == 0) return;
        if (totalPickupCount <= 0) return;

        for (int i = 0; i < totalPickupCount; i++) {
            GameObject pickupPrefab = pickupPrefabs[Random.Range(0, pickupPrefabs.Count)];
            Zone randomZone = zoneManager.GetRandomZone();
            Vector3 spawnPosition = randomZone.GetRandomWorldPointInside();
            spawnPosition.y += spawnHeightOffset;

            Instantiate(pickupPrefab, spawnPosition, Quaternion.identity);
        }
    }
}
