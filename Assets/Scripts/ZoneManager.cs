using UnityEngine;
using System.Collections.Generic;

// After floor confirm: spawns zones, optional grid lines, and reads player position from the assigned Main Camera.
public class ZoneManager : MonoBehaviour
{
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
    [Tooltip("Captured tile tint; use a similar color to flagMaterialPlayer if you want them to match.")]
    [SerializeField]
    private Material floorMaterialPlayer;
    [SerializeField]
    private Material floorMaterialEnemy;
    [SerializeField]
    private Material floorMaterialNeutral;
    [SerializeField]
    private Material floorMaterialContested;

    [Header("Zone materials — flag (opaque)")]
    [Tooltip("Flag when the player captures this zone — pick the same color/style as floorMaterialPlayer for a matching look.")]
    [SerializeField]
    private Material flagMaterialPlayer;
    [SerializeField]
    private Material flagMaterialEnemy;
    [SerializeField]
    private Material flagMaterialNeutral;
    [SerializeField]
    private Material flagMaterialContested;

    [Header("Flag")]
    [SerializeField]
    private GameObject flagPrefab;

    [Tooltip("After assigning flagMaterialPlayer, push its base color onto every child Renderer via MaterialPropertyBlock (helps when the prefab has many meshes/materials).")]
    [SerializeField]
    private bool applyPlayerFlagSurfaceColorToAllFlagRenderers = true;

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
    private List<Zone> zones = new List<Zone>();
    // List of grid lines.
    private List<LineRenderer> gridLines = new List<LineRenderer>();
    // Per-zone capture progress 0–1; resets when captured or when the player leaves mid-capture.
    private float[] captureFill01 = System.Array.Empty<float>();
    // Cached: which zone contains the player this frame (null if none).
    private Zone zoneUnderPlayer;

    ZoneMaterials BuildZoneMaterials() {
        return new ZoneMaterials {
            floorPlayer = floorMaterialPlayer,
            floorEnemy = floorMaterialEnemy,
            floorNeutral = floorMaterialNeutral,
            floorContested = floorMaterialContested,
            flagPlayer = flagMaterialPlayer,
            flagEnemy = flagMaterialEnemy,
            flagNeutral = flagMaterialNeutral,
            flagContested = flagMaterialContested
        };
    }

    // Called from GameManager when the user confirms the floor.
    public void GenerateZones(Transform planeTransform, Vector2 planeSize) {
        // Calculate the width and height of each cell.
        float cellWidth = planeSize.x / columns;
        float cellHeight = planeSize.y / rows;

        float anchorX = -planeSize.x * 0.5f;
        float anchorZ = -planeSize.y * 0.5f;

        // Iterate over each cell in the grid.
        for (int rowIndex = 0; rowIndex < rows; rowIndex++) {
            // Iterate over each column in the row.
            for (int colIndex = 0; colIndex < columns; colIndex++) {

                // Calculate the local position of the center of the cell.
                float localX = anchorX + (colIndex + 0.5f) * cellWidth;
                float localZ = anchorZ + (rowIndex + 0.5f) * cellHeight;

                // Transform the local position to world space.
                Vector3 localCenter = new Vector3(localX, 0f, localZ);
                Vector3 worldCenter = planeTransform.TransformPoint(localCenter);

                Vector3 worldCellSize = new Vector3(
                    cellWidth * planeTransform.lossyScale.x,
                    0.1f,
                    cellHeight * planeTransform.lossyScale.z
                );

                // Create a new GameObject for the zone.
                GameObject zoneObject = new GameObject("Zone_" + rowIndex + "_" + colIndex);
                zoneObject.transform.SetParent(transform);
                zoneObject.transform.position = worldCenter;
                zoneObject.transform.rotation = planeTransform.rotation;

                // Add a Zone component to the GameObject.
                Zone zone = zoneObject.AddComponent<Zone>();
                zone.Setup(
                    rowIndex,
                    colIndex,
                    worldCenter,
                    worldCellSize,
                    BuildZoneMaterials(),
                    flagPrefab,
                    applyPlayerFlagSurfaceColorToAllFlagRenderers);

                // Add the zone to the list of zones.
                zones.Add(zone);
            }
        }

        // Initialize the capture fill array.
        captureFill01 = new float[zones.Count];

        BuildGridLines(planeTransform, planeSize, cellWidth, cellHeight);
    }

    // Vertical and horizontal line segments in plane-local space, then transformed to world.
    private void BuildGridLines(Transform planeTransform, Vector2 planeSize, float cellWidth, float cellHeight) {
        float startX = -planeSize.x * 0.5f;
        float startZ = -planeSize.y * 0.5f;
        float lineHeight = 0.002f;

        // Build vertical lines
        for (int c = 0; c <= columns; c++) {
            float localX = startX + c * cellWidth;
            Vector3 localA = new Vector3(localX, lineHeight, startZ);
            Vector3 localB = new Vector3(localX, lineHeight, startZ + planeSize.y);
            AddOneGridLine(planeTransform, localA, localB);
        }

        // Build horizontal lines
        for (int r = 0; r <= rows; r++) {
            float localZ = startZ + r * cellHeight;
            Vector3 localA = new Vector3(startX, lineHeight, localZ);
            Vector3 localB = new Vector3(startX + planeSize.x, lineHeight, localZ);
            AddOneGridLine(planeTransform, localA, localB);
        }
    }

    // Adds a single line segment to the grid.
    private void AddOneGridLine(Transform planeTransform, Vector3 localStart, Vector3 localEnd) {
        // Create a new GameObject for the line.
        GameObject lineObject = new GameObject("GridLine");
        lineObject.transform.SetParent(transform);

        // Add a LineRenderer component to the GameObject.
        LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, planeTransform.TransformPoint(localStart));
        lineRenderer.SetPosition(1, planeTransform.TransformPoint(localEnd));

        // Set the width of the line.
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;

        // Set the color of the line.
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;

        // Set the line to use world space.
        lineRenderer.useWorldSpace = true;
        
        // Add the line to the list of grid lines.
        gridLines.Add(lineRenderer);
    }

    // First zone that contains this world position, or null.
    public Zone GetZoneAt(Vector3 worldPosition) {
        for (int i = 0; i < zones.Count; i++) {
            Zone zone = zones[i];
            if (zone.Contains(worldPosition)) {
                return zone;
            }
        }
        return null;
    }

    // All zones; list order matches GetFill(zoneIndex) indices.
    public List<Zone> GetAllZones() {
        return zones;
    }

    // Capture progress 0–1 for a zone by index.
    public float GetFill(int zoneIndex) {
        if (zoneIndex < 0 || zoneIndex >= captureFill01.Length) return 0f;
        return captureFill01[zoneIndex];
    }

    public float GetFill(Zone zone) {
        return GetFill(zones.IndexOf(zone));
    }

    public float GetCaptureProgress(int zoneIndex) {
        return GetFill(zoneIndex);
    }

    public float GetCaptureProgress(Zone zone) {
        return GetFill(zone);
    }

    // Destroys spawned zone objects and line renderers (e.g. before rebuilding the grid).
    private void ClearAllZonesAndLines() {
        for (int i = 0; i < zones.Count; i++) {
            Destroy(zones[i].gameObject);
        }
        zones.Clear();

        for (int i = 0; i < gridLines.Count; i++) {
            Destroy(gridLines[i].gameObject);
        }
        gridLines.Clear();

        captureFill01 = System.Array.Empty<float>();
    }

    private void Update() {
        // If there are no zones, return.
        if (zones.Count == 0) return;

        zoneUnderPlayer = GetZoneAt(mainCameraTransform.position);

        float deltaTime = Time.deltaTime;
        float captureRate = Mathf.Max(secondsToCapture, 0.1f);
        float drainRate = Mathf.Max(secondsToDrain, 0.1f);

        for (int i = 0; i < zones.Count; i++) {
            Zone zone = zones[i];
            bool playerIsHere = (zone == zoneUnderPlayer);
            ProcessCaptureForOneZone(i, zone, playerIsHere, deltaTime, captureRate, drainRate);
        }
    }

    private void ProcessCaptureForOneZone(
        int zoneIndex,
        Zone zone,
        bool playerIsHere,
        float deltaTime,
        float captureSeconds,
        float drainSeconds)
    {
        if (playerIsHere) {
            if (zone.Owner != ZoneOwner.Player) {
                captureFill01[zoneIndex] += deltaTime / captureSeconds;

                if (captureFill01[zoneIndex] >= 1f) {
                    captureFill01[zoneIndex] = 0f;
                    zone.SetOwner(ZoneOwner.Player);
                    Messenger.Broadcast(GameEvent.ZONE_CAPTURED, MessengerMode.DONT_REQUIRE_LISTENER);
                }
            } else {
                captureFill01[zoneIndex] = 0f;
            }
        } else {
            if (captureFill01[zoneIndex] > 0f) {
                captureFill01[zoneIndex] -= deltaTime / drainSeconds;
                if (captureFill01[zoneIndex] < 0f) {
                    captureFill01[zoneIndex] = 0f;
                }
            }
        }
    }
}
