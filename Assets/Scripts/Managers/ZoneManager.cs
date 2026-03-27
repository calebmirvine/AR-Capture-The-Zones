using UnityEngine;
using System.Collections.Generic;

// After floor confirm: spawns zones, optional grid lines, and reads player position from the assigned Main Camera.
public class ZoneManager : MonoBehaviour
{
    private const float MinCaptureSeconds = 0.1f;
    private const float GridLineHeight = 0.002f;

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

    [Header("Grid lines")]
    [SerializeField]
    private float lineWidth = 0.005f;

    [SerializeField]
    private Color lineColor = Color.white;

    [Header("Capture timing")]
    [SerializeField]
    private float secondsToCapture = 3f;

    [SerializeField]
    private float secondsToDrain = 2f;

    // All cells in row-major order (row 0 col 0, row 0 col 1, …).
    private readonly List<Zone> zones = new List<Zone>();
    // List of grid lines.
    private readonly List<LineRenderer> gridLines = new List<LineRenderer>();
    // Per-zone capture progress 0–1; resets when captured or when the player leaves mid-capture.
    private float[] zoneCaptureProgress = System.Array.Empty<float>();

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

    // Builds zones and grid lines from the confirmed floor plane.
    public void GenerateZones(Transform planeTransform, Vector2 planeSize) {
        ClearAllZonesAndLines();

        float cellWidth = planeSize.x / columns;
        float cellHeight = planeSize.y / rows;

        float anchorX = -planeSize.x * 0.5f;
        float anchorZ = -planeSize.y * 0.5f;

        for (int rowIndex = 0; rowIndex < rows; rowIndex++) {
            for (int colIndex = 0; colIndex < columns; colIndex++) {
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
                zone.Setup(
                    worldCellSizeX,
                    worldCellSizeZ,
                    this);

                zones.Add(zone);
            }
        }

        zoneCaptureProgress = new float[zones.Count];

        BuildGridLines(planeTransform, planeSize, cellWidth, cellHeight);
    }

    // Creates vertical and horizontal line segments for the zone grid.
    private void BuildGridLines(Transform planeTransform, Vector2 planeSize, float cellWidth, float cellHeight) {
        float startX = -planeSize.x * 0.5f;
        float startZ = -planeSize.y * 0.5f;

        for (int c = 0; c <= columns; c++) {
            float localX = startX + c * cellWidth;
            Vector3 localA = new Vector3(localX, GridLineHeight, startZ);
            Vector3 localB = new Vector3(localX, GridLineHeight, startZ + planeSize.y);
            AddOneGridLine(planeTransform, localA, localB);
        }

        for (int r = 0; r <= rows; r++) {
            float localZ = startZ + r * cellHeight;
            Vector3 localA = new Vector3(startX, GridLineHeight, localZ);
            Vector3 localB = new Vector3(startX + planeSize.x, GridLineHeight, localZ);
            AddOneGridLine(planeTransform, localA, localB);
        }
    }

    // Adds one world-space line segment to the grid.
    private void AddOneGridLine(Transform planeTransform, Vector3 localStart, Vector3 localEnd) {
        GameObject lineObject = new GameObject("GridLine");
        lineObject.transform.SetParent(transform);

        LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, planeTransform.TransformPoint(localStart));
        lineRenderer.SetPosition(1, planeTransform.TransformPoint(localEnd));

        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;

        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;

        lineRenderer.useWorldSpace = true;
        gridLines.Add(lineRenderer);
    }

    // Returns the first zone containing this world position, if any.
    private Zone GetZoneAt(Vector3 worldPosition) {
        for (int i = 0; i < zones.Count; i++) {
            Zone zone = zones[i];
            if (zone.Contains(worldPosition)) {
                return zone;
            }
        }
        return null;
    }

    // Destroys previously spawned zones and grid lines.
    private void ClearAllZonesAndLines() {
        for (int i = 0; i < zones.Count; i++) {
            Destroy(zones[i].gameObject);
        }
        zones.Clear();

        for (int i = 0; i < gridLines.Count; i++) {
            Destroy(gridLines[i].gameObject);
        }
        gridLines.Clear();

        zoneCaptureProgress = System.Array.Empty<float>();
    }

    // Updates zone capture and drain progress each frame.
    private void Update() {
        if (zones.Count == 0) return;

        Zone playerZone = GetZoneAt(mainCameraTransform.position);

        float deltaTime = Time.deltaTime;
        float captureRate = Mathf.Max(secondsToCapture, MinCaptureSeconds);
        float drainRate = Mathf.Max(secondsToDrain, MinCaptureSeconds);

        for (int i = 0; i < zones.Count; i++) {
            Zone zone = zones[i];
            bool playerIsHere = zone == playerZone;
            ProcessCaptureForOneZone(i, zone, playerIsHere, deltaTime, captureRate, drainRate);
        }
    }

    // Applies per-frame capture and drain changes for one zone.
    private void ProcessCaptureForOneZone(
        int zoneIndex,
        Zone zone,
        bool playerIsHere,
        float deltaTime,
        float captureSeconds,
        float drainSeconds)
    {
        if (playerIsHere) {
            if (zone.Owner == ZoneOwner.Player) {
                zoneCaptureProgress[zoneIndex] = 0f;
                return;
            }

            zoneCaptureProgress[zoneIndex] += deltaTime / captureSeconds;
            if (zoneCaptureProgress[zoneIndex] < 1f) return;

            zoneCaptureProgress[zoneIndex] = 0f;
            zone.SetOwner(ZoneOwner.Player);
            return;
        }

        zoneCaptureProgress[zoneIndex] = Mathf.Max(0f, zoneCaptureProgress[zoneIndex] - (deltaTime / drainSeconds));
    }
}
