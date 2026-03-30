using UnityEngine;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine.AI;

// After floor confirm: spawns zones, optional grid lines, and reads player position from the assigned Main Camera.
public class ZoneManager : MonoBehaviour
{
    // Non-zero clamp to a minimum.
    private const float MIN_CAPTURE = 0.1f;

    [Header("Player position (will be AR Main Camera)")]
    [SerializeField]
    private Transform mainCameraTransform;

    [Header("Grid size")]
    [SerializeField]
    private int columns = 3;
    [SerializeField]
    private int rows = 2;

    [Header("Zone materials — floor (transparent)")]
    [SerializeField]
    private Material floorMaterialPlayer;
    [SerializeField]
    private Material floorMaterialEnemy;
    [SerializeField]
    private Material floorMaterialNeutral;
    [SerializeField]
    private Material floorMaterialContested;

    [Header("Flag prefabs")]
    [SerializeField]
    private GameObject flagPrefabNeutral;
    [SerializeField]
    private GameObject flagPrefabPlayer;
    [SerializeField]
    private GameObject flagPrefabEnemy;
    [SerializeField]
    private GameObject flagPrefabContested;

    // How long it takes to capture a zone when standing in it.
    [SerializeField]
    private float secondsToCapture = 3f;

    //How long it takes to lose capture progress when stepping out of a zone.
    [SerializeField]
    private float secondsToDrain = 2f;

    // All cells in row-major order (row 0 col 0, row 0 col 1, …).
    // Empty until zones are generated after floor confirm.
    private readonly List<Zone> zones = new List<Zone>();
    
    // NavMesh surface for enemy AI navigation
    private NavMeshSurface navMeshSurface;
    
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

        // Bake the NavMesh for enemy AI navigation
        BakeNavMesh();
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
        float captureRate = Mathf.Max(secondsToCapture, MIN_CAPTURE);
        float drainRate = Mathf.Max(secondsToDrain, MIN_CAPTURE);

        // If the player is not in a zone, drain the capture progress.
        if (playerZone == null) {
            // Drain the capture progress. activeCaptureProgress is 0..1. 
            activeCaptureProgress = Mathf.Max(0f, activeCaptureProgress - (deltaTime / drainRate));
            if (activeCaptureProgress == 0f) {
                activeCaptureZone = null;
            }
            return;
        }

        // If the player is in a zone, and the zone is owned by the player, do nothing.
        if (playerZone.Owner == ZoneOwner.Player) {
            activeCaptureZone = null;
            activeCaptureProgress = 0f;
            return;
        }

        // If the player is in a zone, and the zone is not owned by the player, start capturing the zone.
        if (activeCaptureZone != playerZone) {
            activeCaptureZone = playerZone;
            activeCaptureProgress = 0f;
        }

        // Capture the zone. activeCaptureProgress is 0..1.
        activeCaptureProgress += deltaTime / captureRate;
        if (activeCaptureProgress >= 1f) {
            // The zone is captured. Set the zone owner to the player.
            playerZone.SetOwner(ZoneOwner.Player);

            // Reset the active capture zone and progress.
            activeCaptureZone = null;
            activeCaptureProgress = 0f;
        }
    }

    // Returns the first zone containing this world position, if any.
    private Zone GetZoneAt(Vector3 position) {
        foreach (Zone zone in zones) {
            if (zone.Contains(position)) return zone;
        }
        return null;
    }

    // Returns a random zone from all available zones.
    public Zone GetRandomZone() {
        if (zones.Count == 0) {
            Debug.Log("Zones list empty. Cannot get random zone.");
            return null;
        }
        int randomIndex = Random.Range(0, zones.Count);
        return zones[randomIndex];
    }

    // Creates one zone GameObject and initializes its Zone component.
    private Zone CreateZone(Transform planeTransform, Vector2 planeSize, int rowIndex, int colIndex)
    {
        float cellWidth = planeSize.x / columns;
        float cellHeight = planeSize.y / rows;

        //Anchor the zone to the bottom left of the plane. Calculate the local center of the cell, then convert to world position.
        float anchorX = -planeSize.x * 0.5f;
        float anchorZ = -planeSize.y * 0.5f;

        float localX = anchorX + (colIndex + 0.5f) * cellWidth;
        float localZ = anchorZ + (rowIndex + 0.5f) * cellHeight;

        Vector3 localCenter = new Vector3(localX, 0f, localZ);
        Vector3 worldCenter = planeTransform.TransformPoint(localCenter);

        //Convert local cell size to world cell size. The planeTransform.lossyScale is the scale of the plane.
        float worldCellSizeX = cellWidth * planeTransform.lossyScale.x;
        float worldCellSizeZ = cellHeight * planeTransform.lossyScale.z;

        //Create the zone object. With name reflecting the row and column index.
        GameObject zoneObject = new GameObject("Zone_" + rowIndex + "_" + colIndex);
        zoneObject.transform.SetParent(transform);
        zoneObject.transform.position = worldCenter;
        zoneObject.transform.rotation = planeTransform.rotation; // The zone is not rotated.

        Zone zone = zoneObject.AddComponent<Zone>();
        zone.Setup(worldCellSizeX, worldCellSizeZ, this); 
        return zone;
    }

    // Destroys previously spawned zones. 
    //Can be used to reset the zone manager to a new game.
    private void ClearAllZones() {
        foreach (Zone zone in zones) {
            Destroy(zone.gameObject);
        }
        zones.Clear();

        activeCaptureZone = null;
        activeCaptureProgress = 0f;
    }

    // Bakes the NavMesh surface after zones are generated
    private void BakeNavMesh() {
        // Create or reuse the NavMeshSurface component
        if (navMeshSurface == null) {
            navMeshSurface = gameObject.AddComponent<NavMeshSurface>();
        }

        // Configure the NavMeshSurface to bake all zone colliders
        navMeshSurface.collectObjects = CollectObjects.Children;
        navMeshSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        
        // Bake the NavMesh
        navMeshSurface.BuildNavMesh();
    }
}
