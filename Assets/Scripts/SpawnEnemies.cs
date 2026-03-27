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

    public void SetSpawnSurface(Transform surface, Vector2 size) {
        spawnSurface = surface;
        surfaceSize = size;
    }

    public void StartSpawning() {
        isSpawning = true;
    }

    void Update() {
        if (!isSpawning) return;

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval) {
            SpawnEnemy();
            spawnTimer = 0f;
        }
    }

    void SpawnEnemy() {
        if (currentEnemies >= maxEnemies) return;

        float halfX = surfaceSize.x * 0.5f;
        float halfZ = surfaceSize.y * 0.5f;

        Vector3 localOffset = new Vector3(Random.Range(-halfX, halfX), 0f, Random.Range(-halfZ, halfZ));
        Vector3 spawnPos = spawnSurface.TransformPoint(localOffset);

        GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        enemies.Add(enemy);
        currentEnemies++;
    }
}
