using UnityEngine;
using System.Collections.Generic;

public class SpawnEnemies : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    private List<GameObject> enemies = new List<GameObject>();

    [Header("Spawn Settings")]
    [SerializeField] private float spawnInterval = 1f;
    private float spawnTimer = 0f;

    [Header("Enemy Spawn Settings")]
    [SerializeField] private int maxEnemies = 6;
    private int currentEnemies = 0;

    private bool isSpawning = false;
    private Transform spawnSurface;
    private Vector2 surfaceSize;

    // Store the AR floor (or plane) transform and extent used for random spawn positions.
    public void SetSpawnSurface(Transform surface, Vector2 size) {
        spawnSurface = surface;
        surfaceSize = size;
    }

    // Allow Update to start spawning enemies after scan / game phase is ready.
    public void StartSpawning() {
        isSpawning = true;
    }

    // Tick spawn timer and spawn at interval while under max count.
    void Update() {
        if (!isSpawning) return;

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval) {
            SpawnEnemy();
            spawnTimer = 0f;
        }
    }

    // Place one enemy at a random point on the spawn surface in local XZ.
    void SpawnEnemy() {
        if (currentEnemies >= maxEnemies) return;
        if (spawnSurface == null || enemyPrefab == null) return;

        float halfX = surfaceSize.x * 0.5f;
        float halfZ = surfaceSize.y * 0.5f;

        Vector3 localOffset = new Vector3(Random.Range(-halfX, halfX), 0f, Random.Range(-halfZ, halfZ));
        Vector3 spawnPos = spawnSurface.TransformPoint(localOffset);

        GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        enemies.Add(enemy);
        currentEnemies++;
    }
}
