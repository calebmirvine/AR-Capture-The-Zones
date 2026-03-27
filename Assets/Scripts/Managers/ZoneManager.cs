using UnityEngine;
using System.Collections.Generic;

// After floor confirm: spawns zones, optional grid lines, and reads player position from the assigned Main Camera.
public class ZoneManager : MonoBehaviour
{
    // Prevent divide-by-zero in capture/drain rates.
    private const float MinCaptureSeconds = 0.1f;

    [Header("Player position (for walking into zones)")]
    [Tooltip("Assign your AR / Main Camera transform (tracked device position in the world).")]
    [SerializeField]
    private Transform mainCameraTransform;

    [Header("Grid size")]
    [SerializeField]
    private int columns = 3;
    [SerializeField]
    private int rows = 2;

    [Header("Zone materials — floor (transparent tint)")]
    [SerializeField]
    private Material floorMaterialPlayer;
    [SerializeField]
    private Material floorMaterialEnemy;
    [SerializeField]
    private Material floorMaterialNeutral;
    [SerializeField]
    private Material floorMaterialContested;

    [Header("Flag prefabs")]
    [Tooltip("Default flag shown for neutral zones.")]
    [SerializeField]
    private GameObject flagPrefabNeutral;
    [SerializeField]
    private GameObject flagPrefabPlayer;
    [SerializeField]
    private GameObject flagPrefabEnemy;
    [SerializeField]
    private GameObject flagPrefabContested;

    [Header("Capture timing")]
    [SerializeField]
    private float secondsToCapture = 3f;

    [SerializeField]
    private float secondsToDrain = 2f;

    // All cells in row-major order (row 0 col 0, row 0 col 1, …).
    private readonly List<Zone> zones = new List<Zone>();
    
    // Current capture target and progress (0..1).
    private Zone activeCaptureZone;
    private float activeCaptureProgress;

    public enum ZoneOwner {
        Neutral,
        Contested,
        Player,
        Enemy
    }

    // Builds zones from the confirmed floor plane.
    public void GenerateZones(Transform planeTransform, Vector2 planeSize) {
        ClearAllZones();

        for (int rowIndex = 0; rowIndex < rows; rowIndex++) {
            for (int colIndex = 0; colIndex < columns; colIndex++) {
                Zone zone = CreateZone(planeTransform, planeSize, rowIndex, colIndex);
                zones.Add(zone);
            }
        }

        activeCaptureZone = null;
        activeCaptureProgress = 0f;

    }

    // Material lookup for floor by zone owner.
    public Material GetFloorMaterial(ZoneOwner owner) {
        switch (owner) {
            case ZoneOwner.Player: return floorMaterialPlayer;
            case ZoneOwner.Enemy: return floorMaterialEnemy;
            case ZoneOwner.Contested: return floorMaterialContested;
            default: return floorMaterialNeutral;
        }
    }

    // Flag prefab lookup by zone owner.
    public GameObject GetFlagPrefab(ZoneOwner owner) {
        switch (owner) {
            case ZoneOwner.Player: return flagPrefabPlayer;
            case ZoneOwner.Enemy: return flagPrefabEnemy;
            case ZoneOwner.Contested: return flagPrefabContested;
            default: return flagPrefabNeutral;
        }
    }

    // Updates zone capture and drain progress each frame.
    private void Update() {
        if (zones.Count == 0) return;

        Zone playerZone = GetZoneAt(mainCameraTransform.position);

        float deltaTime = Time.deltaTime;
        float captureRate = Mathf.Max(secondsToCapture, MinCaptureSeconds);
        float drainRate = Mathf.Max(secondsToDrain, MinCaptureSeconds);

        if (playerZone == null) {
            activeCaptureProgress = Mathf.Max(0f, activeCaptureProgress - (deltaTime / drainRate));
            if (activeCaptureProgress == 0f) {
                activeCaptureZone = null;
            }
            return;
        }

        if (playerZone.Owner == ZoneOwner.Player) {
            activeCaptureZone = null;
            activeCaptureProgress = 0f;
            return;
        }

        if (activeCaptureZone != playerZone) {
            activeCaptureZone = playerZone;
            activeCaptureProgress = 0f;
        }

        activeCaptureProgress += deltaTime / captureRate;
        if (activeCaptureProgress >= 1f) {
            playerZone.SetOwner(ZoneOwner.Player);
            activeCaptureZone = null;
            activeCaptureProgress = 0f;
        }
    }

    // Returns the first zone containing this world position, if any.
    private Zone GetZoneAt(Vector3 worldPosition) {
        foreach (Zone zone in zones) {
            if (zone.Contains(worldPosition)) {
                return zone;
            }
        }
        return null;
    }

    // Creates one zone GameObject and initializes its Zone component.
    private Zone CreateZone(
        Transform planeTransform,
        Vector2 planeSize,
        int rowIndex,
        int colIndex)
    {
        float cellWidth = planeSize.x / columns;
        float cellHeight = planeSize.y / rows;
        float anchorX = -planeSize.x * 0.5f;
        float anchorZ = -planeSize.y * 0.5f;

        float localX = anchorX + (colIndex + 0.5f) * cellWidth;
        float localZ = anchorZ + (rowIndex + 0.5f) * cellHeight;
        Vector3 localCenter = new Vector3(localX, 0f, localZ);
        Vector3 worldCenter = planeTransform.TransformPoint(localCenter);

        float worldCellSizeX = cellWidth * planeTransform.lossyScale.x;
        float worldCellSizeZ = cellHeight * planeTransform.lossyScale.z;

        GameObject zoneObject = new GameObject("Zone_" + rowIndex + "_" + colIndex);
        zoneObject.transform.SetParent(transform);
        zoneObject.transform.position = worldCenter;
        zoneObject.transform.rotation = planeTransform.rotation;

        Zone zone = zoneObject.AddComponent<Zone>();
        zone.Setup(worldCellSizeX, worldCellSizeZ, this);
        return zone;
    }

    // Destroys previously spawned zones.
    private void ClearAllZones() {
        foreach (Zone zone in zones) {
            Destroy(zone.gameObject);
        }
        zones.Clear();

        activeCaptureZone = null;
        activeCaptureProgress = 0f;
    }
}
