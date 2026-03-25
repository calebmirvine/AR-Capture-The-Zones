using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;

// Builds the zone grid after AR floor confirm; handles grid lines, capture progress, and NavMesh bake.
public class ZoneManager : MonoBehaviour
{
    [Header("Player position (for walking into zones)")]
    [SerializeField] private Transform mainCameraTransform;

    [Header("Grid size")]
    [SerializeField] private int columns = 3;
    [SerializeField] private int rows = 2;

    [Header("NavMesh")]
    [SerializeField] private NavMeshSurface navMeshSurface;

    [Header("Zone materials — floor (transparent tint)")]
    [SerializeField] private Material floorMaterialPlayer;
    [SerializeField] private Material floorMaterialEnemy;
    [SerializeField] private Material floorMaterialNeutral;
    [SerializeField] private Material floorMaterialContested;

    [Header("Zone materials — flag (opaque)")]
    [SerializeField] private Material flagMaterialPlayer;
    [SerializeField] private Material flagMaterialEnemy;
    [SerializeField] private Material flagMaterialNeutral;
    [SerializeField] private Material flagMaterialContested;

    [Header("Flag")]
    [SerializeField] private GameObject flagPrefab;

    [Header("Capture timing")]
    [SerializeField] private float secondsToCapture = 3f;
    [SerializeField] private float secondsToDrain = 2f;

    private List<Zone> zones = new List<Zone>();
    private float[] captureProgressByZone = System.Array.Empty<float>();
    private Zone zoneUnderPlayer;
    private readonly List<Transform> registeredEnemies = new List<Transform>();

    public void RegisterEnemy(Transform enemyRoot) {
        if (enemyRoot == null) return;
        if (!registeredEnemies.Contains(enemyRoot)) {
            registeredEnemies.Add(enemyRoot);
        }
    }

    public void UnregisterEnemy(Transform enemyRoot) {
        if (enemyRoot == null) return;
        registeredEnemies.Remove(enemyRoot);
    }

    bool AnyEnemyInZone(Zone zone) {
        for (int i = registeredEnemies.Count - 1; i >= 0; i--) {
            Transform t = registeredEnemies[i];
            if (t == null) {
                registeredEnemies.RemoveAt(i);
                continue;
            }
            if (zone.Contains(t.position)) {
                return true;
            }
        }
        return false;
    }

    public bool IsZoneContested(Zone zone) {
        if (zone == null) return false;
        bool playerHere = zoneUnderPlayer == zone;
        return playerHere && AnyEnemyInZone(zone);
    }

    ZoneMaterials BuildZoneMaterials() {
        return ZoneMaterials.FromInspector(
            floorMaterialPlayer,
            floorMaterialEnemy,
            floorMaterialNeutral,
            floorMaterialContested,
            flagMaterialPlayer,
            flagMaterialEnemy,
            flagMaterialNeutral,
            flagMaterialContested);
    }

    public void GenerateZones(Transform planeTransform, Vector2 planeSize) {
        float cellWidth = planeSize.x / columns;
        float cellHeight = planeSize.y / rows;
        float anchorX = -planeSize.x * 0.5f;
        float anchorZ = -planeSize.y * 0.5f;

        ZoneMaterials mats = BuildZoneMaterials();

        // Bake NavMesh
        if (navMeshSurface != null) {
            navMeshSurface.BuildNavMesh();
        }

        // Generate zones
        for (int rowIndex = 0; rowIndex < rows; rowIndex++) {
            for (int colIndex = 0; colIndex < columns; colIndex++) {
                float localX = anchorX + (colIndex + 0.5f) * cellWidth;
                float localZ = anchorZ + (rowIndex + 0.5f) * cellHeight;

                Vector3 localCenter = new Vector3(localX, 0f, localZ);
                Vector3 worldCenter = planeTransform.TransformPoint(localCenter);
                Vector3 worldCellSize = new Vector3(
                    cellWidth * planeTransform.lossyScale.x,
                    0.1f,
                    cellHeight * planeTransform.lossyScale.z
                );

                GameObject zoneObject = new GameObject("Zone_" + rowIndex + "_" + colIndex);
                zoneObject.transform.SetParent(transform);
                zoneObject.transform.position = worldCenter;
                zoneObject.transform.rotation = planeTransform.rotation;

                Zone zone = zoneObject.AddComponent<Zone>();
                zone.Setup(rowIndex, colIndex, worldCenter, worldCellSize, mats, flagPrefab);
                zones.Add(zone);
            }
        }

        captureProgressByZone = new float[zones.Count];

        if (navMeshSurface != null) {
            navMeshSurface.BuildNavMesh();
        }
    }

    public Zone GetZoneAt(Vector3 worldPosition) {
        for (int i = 0; i < zones.Count; i++) {
            if (zones[i].Contains(worldPosition)) return zones[i];
        }
        return null;
    }

    public List<Zone> GetAllZones() => zones;

    public float GetFill(int zoneIndex) {
        if (zoneIndex < 0 || zoneIndex >= captureProgressByZone.Length) return 0f;
        return captureProgressByZone[zoneIndex];
    }

    public float GetFill(Zone zone) => GetFill(zones.IndexOf(zone));

    public float GetCaptureProgress(int zoneIndex) => GetFill(zoneIndex);

    public float GetCaptureProgress(Zone zone) => GetFill(zone);

    void Update() {
        if (zones.Count == 0) return;

        zoneUnderPlayer = mainCameraTransform != null
            ? GetZoneAt(mainCameraTransform.position)
            : null;

        float deltaTime = Time.deltaTime;
        float captureSeconds = Mathf.Max(secondsToCapture, 0.1f);
        float drainSeconds = Mathf.Max(secondsToDrain, 0.1f);

        for (int i = 0; i < zones.Count; i++) {
            Zone z = zones[i];
            bool playerHere = z == zoneUnderPlayer;
            bool enemyHere = AnyEnemyInZone(z);
            ProcessCaptureForOneZone(i, z, playerHere, enemyHere, deltaTime, captureSeconds, drainSeconds);
            z.SetContestedDisplay(playerHere && enemyHere);
        }
    }

    void ProcessCaptureForOneZone(
        int zoneIndex,
        Zone zone,
        bool playerInZone,
        bool enemyInZone,
        float deltaTime,
        float captureSeconds,
        float drainSeconds)
    {
        if (playerInZone && enemyInZone) {
            if (captureProgressByZone[zoneIndex] > 0f) {
                captureProgressByZone[zoneIndex] -= deltaTime / drainSeconds;
                if (captureProgressByZone[zoneIndex] < 0f) captureProgressByZone[zoneIndex] = 0f;
            }
            return;
        }

        if (playerInZone) {
            if (zone.Owner != ZoneOwner.Player) {
                captureProgressByZone[zoneIndex] += deltaTime / captureSeconds;
                if (captureProgressByZone[zoneIndex] >= 1f) {
                    captureProgressByZone[zoneIndex] = 0f;
                    zone.SetOwner(ZoneOwner.Player);
                }
            } else {
                captureProgressByZone[zoneIndex] = 0f;
            }
            return;
        }

        if (enemyInZone) {
            if (zone.Owner != ZoneOwner.Enemy) {
                captureProgressByZone[zoneIndex] += deltaTime / captureSeconds;
                if (captureProgressByZone[zoneIndex] >= 1f) {
                    captureProgressByZone[zoneIndex] = 0f;
                    zone.SetOwner(ZoneOwner.Enemy);
                }
            } else {
                captureProgressByZone[zoneIndex] = 0f;
            }
            return;
        }

        if (captureProgressByZone[zoneIndex] > 0f) {
            captureProgressByZone[zoneIndex] -= deltaTime / drainSeconds;
            if (captureProgressByZone[zoneIndex] < 0f) captureProgressByZone[zoneIndex] = 0f;
        }
    }
}
