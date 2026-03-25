using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

// Scan phase: finds horizontal AR planes, shows Confirm when the largest is big enough.
// After confirm: hides planes, stops tracking, and tells ZoneManager to build the grid.
public class GameManager : MonoBehaviour
{
    [Header("AR")]
    [SerializeField]
    private ARPlaneManager arPlaneManager;

    [SerializeField]
    private ZoneManager zoneManager;

    [SerializeField]
    private Button confirmButton;

    [Header("Enemies (after zones + NavMesh bake)")]
    [SerializeField]
    private GameObject enemyPrefab;

    [SerializeField]
    private int enemyCount = 1;

    [Header("Plane size")]
    [SerializeField]
    private float minimumPlaneArea = 1f;

    private ARPlane largestPlane;
    private float largestPlaneArea;

    // Wire confirm listener; hide button until a plane is large enough.
    private void Start() {
        confirmButton.gameObject.SetActive(false);
        confirmButton.onClick.AddListener(OnConfirmScan);
    }

    // While scanning: track biggest floor and toggle the Confirm button.
    private void Update() {
        if (arPlaneManager.enabled == false) {
            return;
        }

        largestPlane = null;
        largestPlaneArea = 0f;

        foreach (ARPlane plane in arPlaneManager.trackables) {
            float area = plane.size.x * plane.size.y;
            if (area > largestPlaneArea) {
                largestPlaneArea = area;
                largestPlane = plane;
            }
        }

        bool meetsMinimum = false;
        if (largestPlane != null && largestPlaneArea >= minimumPlaneArea) {
            meetsMinimum = true;
        }

        if (meetsMinimum) {
            if (confirmButton.gameObject.activeSelf == false) {
                confirmButton.gameObject.SetActive(true);
            }
        } else {
            if (confirmButton.gameObject.activeSelf) {
                confirmButton.gameObject.SetActive(false);
            }
        }
    }

    // User tapped Confirm: lock in the current plane and spawn zones on it.
    private void OnConfirmScan() {
        if (zoneManager == null) {
            return;
        }

        ARPlane planeToUse = null;
        float bestArea = 0f;
        foreach (ARPlane plane in arPlaneManager.trackables) {
            float area = plane.size.x * plane.size.y;
            if (area > bestArea) {
                bestArea = area;
                planeToUse = plane;
            }
        }

        if (planeToUse == null || bestArea < minimumPlaneArea) {
            return;
        }
        Transform planeTransform = planeToUse.transform;
        Vector2 planeSize = planeToUse.size;

        confirmButton.gameObject.SetActive(false);

        foreach (ARPlane plane in arPlaneManager.trackables) {
            plane.gameObject.SetActive(false);
        }

        arPlaneManager.enabled = false;

        zoneManager.GenerateZones(planeTransform, planeSize);
        SpawnEnemiesInRandomZones();
    }

    // Instantiate enemies on NavMesh inside random zone cells (after GenerateZones).
    void SpawnEnemiesInRandomZones() {
        if (enemyPrefab == null || zoneManager == null) {
            return;
        }

        List<Zone> zones = zoneManager.GetAllZones();
        if (zones == null || zones.Count == 0) {
            return;
        }

        int count = Mathf.Max(0, enemyCount);
        for (int i = 0; i < count; i++) {
            Zone zone = zones[Random.Range(0, zones.Count)];
            Vector3 candidate = zone.GetRandomPointOnFloor();
            const float sampleRadius = 2f;
            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, sampleRadius, NavMesh.AllAreas)) {
                if (!NavMesh.SamplePosition(zone.transform.position, out hit, sampleRadius * 2f, NavMesh.AllAreas)) {
                    continue;
                }
            }

            GameObject enemy = Instantiate(enemyPrefab, hit.position, Quaternion.identity);
            EnemyZonePatrol patrol = enemy.GetComponent<EnemyZonePatrol>();
            if (patrol != null) {
                patrol.Configure(zoneManager);
            }
        }
    }
}
