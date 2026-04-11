using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

// Scan phase: finds horizontal AR planes, shows Confirm when the largest is big enough.
// After confirm: hides planes, stops tracking, and tells ZoneManager to build the grid.
public class GameManager : MonoBehaviour
{
    private const float EnemySpawnSampleRadius = 0.75f;

    // Try to spawn the enemy in 6 random zones so we have a better chance of finding a valid zone.
    private const int EnemySpawnZoneAttempts = 6;

    [SerializeField]
    private ARPlaneManager arPlaneManager;

    [SerializeField]
    private ZoneManager zoneManager;

    [SerializeField]
    private GameObject enemyPrefab;

    [SerializeField]
    private SpawnItems spawnItems;

    [SerializeField]
    private Button confirmButton;

    [Header("Plane size - Minimum floor area in square meters before Confirm is allowed.")]
    [SerializeField]
    private float minimumPlaneArea = 1f;

    private void Start()
{
        confirmButton.gameObject.SetActive(false);
        confirmButton.onClick.AddListener(OnConfirmScan);
    }

    private void OnDestroy()
{
        confirmButton.onClick.RemoveListener(OnConfirmScan);
    }

    // While scanning, show Confirm once a large enough plane is found.
    private void Update()
{
        if (!arPlaneManager.enabled)
{
            return;
        }

        ARPlane largestPlane = GetLargestPlane();
        bool hasValidPlane = largestPlane != null &&
            (largestPlane.size.x * largestPlane.size.y) >= minimumPlaneArea;

        confirmButton.gameObject.SetActive(hasValidPlane);
    }

    // User tapped Confirm: lock in the current plane and spawn zones on it.
    private void OnConfirmScan()
{
        ARPlane planeToUse = GetLargestPlane();
        if (planeToUse == null)
{
            return;
        }

        Transform planeTransform = planeToUse.transform;
        Vector2 planeSize = planeToUse.size;

        // Hide the confirm button and all planes.
        confirmButton.gameObject.SetActive(false);
        foreach (ARPlane plane in arPlaneManager.trackables)
{
            plane.gameObject.SetActive(false);
        }
        arPlaneManager.enabled = false;

        // Build zones on the chosen plane.
        zoneManager.GenerateZones(planeTransform, planeSize);
        zoneManager.BuildRuntimeNavMesh();

        SpawnEnemyInRandomZone();
        if (spawnItems != null)
{
            spawnItems.SpawnInitialPickups();
        }
    }

    // Returns the largest tracked AR plane, or null when none exist.
    private ARPlane GetLargestPlane()
{
        ARPlane largestPlane = null;
        float largestPlaneArea = 0f;

        foreach (ARPlane plane in arPlaneManager.trackables)
{
            float area = plane.size.x * plane.size.y;
            if (area <= largestPlaneArea)
{
                continue;
            }

            largestPlaneArea = area;
            largestPlane = plane;
        }

        return largestPlane;
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
}
