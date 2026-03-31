using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private ZoneManager zoneManager;

    // Spawns an enemy prefab in a random zone
    public void SpawnEnemyInRandomZone()
    {
        Zone randomZone = zoneManager.GetRandomZone();
        if (randomZone == null) return;

        // Instantiate the enemy prefab at the random zone's position
        Instantiate(enemyPrefab, randomZone.transform.position, Quaternion.identity);
    }
}
