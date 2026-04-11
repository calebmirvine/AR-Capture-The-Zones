using UnityEngine;
using System.Collections.Generic;
using Unity.AI.Navigation;

// After floor confirm: spawns zones, optional grid lines, and reads player position from the assigned Main Camera.
public class ZoneManager : MonoBehaviour
{
    [Header("Player position (will be AR Main Camera)")] [SerializeField]
    public Transform mainCameraTransform;


    [Header("Grid size")] [SerializeField] private int columns = 3;
    [SerializeField] private int rows = 2;

    [Header("Zone materials — floor (transparent)")] [SerializeField]
    private Material floorMaterialPlayer;

    [SerializeField] private Material floorMaterialEnemy;
    [SerializeField] private Material floorMaterialNeutral;
    [SerializeField] private Material floorMaterialContested;

    [Header("Flag prefabs")] [SerializeField]
    private GameObject flagPrefabNeutral;

    [SerializeField] private GameObject flagPrefabPlayer;
    [SerializeField] private GameObject flagPrefabEnemy;
    [SerializeField] private GameObject flagPrefabContested;

    [Header("Runtime Navigation")] private NavMeshSurface runtimeNavMeshSurface;


    [SerializeField] private float secondsToCapture = 3f;

    [SerializeField] private float secondsToDrain = 2f;

    private readonly List<Zone> zones = new List<Zone>();


    public int GetZoneCount()
    {
        return zones.Count;
    }


    // Capture state for each owner.
    // activeZone: current zone this owner is trying to capture.
    // progress: capture progress in range [0..secondsToCapture].
    private class CaptureState
    {
        public Zone activeZone;
        public float progress;
    }

    private CaptureState playerCaptureState = new CaptureState();
    private CaptureState enemyCaptureState = new CaptureState();

    private Zone activeContestedZone;

    // The previous owner of the contested zone, so it can return to previous owner when contested zone is lost.
    private ZoneOwner activeContestedPreviousOwner = ZoneOwner.Neutral;

    // The owner of a zone. Neutral by default
    public enum ZoneOwner
    {
        Neutral,
        Contested,
        Player,
        Enemy
    }

    // Returns true when this zone is neutral or player-held so the enemy may contest it; false if zone is null.
    public static bool CanEnemyCapture(Zone zone)
    {
        if (zone == null)
        {
            return false;
        }

        return zone.Owner == ZoneOwner.Neutral || zone.Owner == ZoneOwner.Player;
    }

    // Destroys previously spawned zones. 
    //Can be used to reset the zone manager to a new game.
    private void ClearAllZones()
    {
        foreach (Zone zone in zones)
        {
            Destroy(zone.gameObject);
        }

        zones.Clear();

        playerCaptureState.activeZone = null;
        playerCaptureState.progress = 0f;
        enemyCaptureState.activeZone = null;
        enemyCaptureState.progress = 0f;
        activeContestedZone = null;
        activeContestedPreviousOwner = ZoneOwner.Neutral;
    }
    // Builds zones from the confirmed floor plane.
    public void GenerateZones(Transform planeTransform, Vector2 planeSize)
    {
        //Clear all zones and reset capture states and contested zone.
        ClearAllZones();

        for (int rowIndex = 0; rowIndex < rows; rowIndex++)
        {
            for (int colIndex = 0; colIndex < columns; colIndex++)
            {
                Zone zone = CreateZone(planeTransform, planeSize, rowIndex, colIndex);
                zones.Add(zone);
            }
        }

        // Reset capture states and contested zone.

        playerCaptureState.activeZone = null;
        playerCaptureState.progress = 0f;

        enemyCaptureState.activeZone = null;
        enemyCaptureState.progress = 0f;

        activeContestedZone = null;
        activeContestedPreviousOwner = ZoneOwner.Neutral;
    }

    // Material lookup for floor by zone owner.
    // Material lookup for floor by zone owner.
    public Material GetFloorMaterial(ZoneOwner owner)
    {
        switch (owner)
        {
            case ZoneOwner.Player: return floorMaterialPlayer;
            case ZoneOwner.Enemy: return floorMaterialEnemy;
            case ZoneOwner.Contested: return floorMaterialContested;
            default: return floorMaterialNeutral;
        }
    }

    // Flag prefab lookup by zone owner.
    // Flag prefab lookup by zone owner.
    public GameObject GetFlagPrefab(ZoneOwner owner)
    {
        switch (owner)
        {
            case ZoneOwner.Player: return flagPrefabPlayer;
            case ZoneOwner.Enemy: return flagPrefabEnemy;
            case ZoneOwner.Contested: return flagPrefabContested;
            default: return flagPrefabNeutral;
        }
    }

    // Builds navmesh from generated runtime geometry.
    public void BuildRuntimeNavMesh()
    {
        runtimeNavMeshSurface.BuildNavMesh();
    }

    // Returns nearest zone that the enemy should try to capture next.

    public Zone GetNearestEnemyTargetZone(Vector3 fromPosition)
    {
        Zone nearestZone = null;

        float nearestDistance = float.MaxValue;

        foreach (Zone zone in zones)
        {
            if (!CanEnemyCapture(zone))
            {
                continue; //If the zone is not captureable, continue to the next zone.
            }

            float distance = Vector3.Distance(zone.transform.position, fromPosition);
            if (distance >= nearestDistance)
            {
                continue; //If the distance is not the nearest, continue to the next zone.
            }

            nearestDistance = distance;
            nearestZone = zone;
        }

        return nearestZone;
    }

    // Returns the first zone containing this world position, if any.
    public Zone GetZoneAtWorldPosition(Vector3 position)
    {
        foreach (Zone zone in zones)
        {
            if (zone.Contains(position)) return zone;
        }

        return null;
    }

    // Updates zone capture and drain progress each frame.
    private void Update()
    {
        const float minCapture = 0.1f;
        // Clamp durations so capture math never divides by zero.
        float captureRate = Mathf.Max(secondsToCapture, minCapture);
        float drainRate = Mathf.Max(secondsToDrain, minCapture);

        // Resolve the player's current zone from the assigned AR Main Camera transform.
        Zone playerZone;
        if (mainCameraTransform != null)
        {
            playerZone = GetZoneAtWorldPosition(mainCameraTransform.position);
        }
        else
        {
            playerZone = null;
        }

        // Resolve the enemy's current zone from the object tagged "enemy".
        GameObject enemy = GameObject.FindWithTag("Enemy");
        Zone enemyZone;
        if (enemy != null)
        {
            enemyZone = GetZoneAtWorldPosition(enemy.transform.position);
        }
        else
        {
            enemyZone = null;
        }

        Zone sharedZone;
        if (playerZone != null && enemyZone != null && playerZone == enemyZone)
        {
            sharedZone = playerZone;
        }
        else
        {
            sharedZone = null;
        }

        UpdateContestedVisualState(sharedZone);

        // If both are in the same zone, that zone is contested and capture is paused.
        if (sharedZone != null)
        {
            return;
        }

        UpdateCaptureForOwner(
            ZoneOwner.Player,
            playerZone,
            captureRate,
            drainRate);


        UpdateCaptureForOwner(
            ZoneOwner.Enemy,
            enemyZone,
            captureRate,
            drainRate);
    }

    // Contested visuals are temporary and only active while both actors share the same zone.
    // When overlap ends, the zone is restored to its previous owner.
    private void UpdateContestedVisualState(Zone sharedZone)
    {
        // If the active contested zone is not the shared zone, reset the contested zone.
        if (activeContestedZone != null && activeContestedZone != sharedZone)
        {
            if (activeContestedZone.Owner == ZoneOwner.Contested)
            {
                activeContestedZone.SetOwner(activeContestedPreviousOwner);
            }

            activeContestedZone = null;
            activeContestedPreviousOwner = ZoneOwner.Neutral;
        }

        // If there is no shared zone, return.
        if (sharedZone == null) return;

        // Set the contested zone to the shared zone.
        if (activeContestedZone != sharedZone)
        {
            activeContestedPreviousOwner = sharedZone.Owner;
            activeContestedZone = sharedZone;
        }

        // Set the zone to contested if it is not already contested.
        if (sharedZone.Owner != ZoneOwner.Contested)
        {
            sharedZone.SetOwner(ZoneOwner.Contested);
        }
    }

    // Shared capture flow for a given owner using a switch on zone ownership.
    private void UpdateCaptureForOwner(ZoneOwner capturerOwner, Zone zoneUnder, float captureRate, float drainRate)
    {
        CaptureState ownerState = GetCaptureState(capturerOwner);

        if (zoneUnder == null)
        {
            ownerState.progress = Mathf.Max(0f, ownerState.progress - (Time.deltaTime / drainRate));
            if (ownerState.progress == 0f)
            {
                ownerState.activeZone = null;
            }
            return;
        }

        switch (zoneUnder.Owner)
        {
            case ZoneOwner.Player:
                if (capturerOwner == ZoneOwner.Player)
                {
                    ownerState.activeZone = null;
                    ownerState.progress = 0f;
                    return;
                }
                break;
            case ZoneOwner.Enemy:
                if (capturerOwner == ZoneOwner.Enemy)
                {
                    ownerState.activeZone = null;
                    ownerState.progress = 0f;
                    return;
                }
                break;
            case ZoneOwner.Neutral:
            case ZoneOwner.Contested:
                break;
            default:
                return;
        }

        if (ownerState.activeZone != zoneUnder)
        {
            ownerState.activeZone = zoneUnder;
            ownerState.progress = 0f;
        }

        ownerState.progress += Time.deltaTime / captureRate;
        if (ownerState.progress < 1f)
        {
            return;
        }

        zoneUnder.SetOwner(capturerOwner);
        if (capturerOwner == ZoneOwner.Player)
        {
            Handheld.Vibrate();
        }

        ownerState.activeZone = null;
        ownerState.progress = 0f;
    }

    // Returns the capture state for a given owner.
    private CaptureState GetCaptureState(ZoneOwner owner)
    {
        return owner == ZoneOwner.Enemy ? enemyCaptureState : playerCaptureState;
    }

    // Returns a random zone from all available zones.
    public Zone GetRandomZone()
    {
        if (zones.Count == 0)
        {
            return null;
        }

        int randomIndex = Random.Range(0, zones.Count);
        return zones[randomIndex];
    }

    public Zone GetRandomNeutralFirstZone()
    {
        List<Zone> neutralZones = new List<Zone>();

        foreach (Zone zone in zones)
        {
            if (zone.Owner == ZoneOwner.Neutral)
            {
                neutralZones.Add(zone);
            }
        }

        if (neutralZones.Count > 0)
        {
            int neutralRandomIndex = Random.Range(0, neutralZones.Count);
            return neutralZones[neutralRandomIndex];
        }

        return GetRandomZone();
    }

    // Creates one zone GameObject and initializes its Zone component.
    private Zone CreateZone(Transform planeTransform, Vector2 planeSize, int rowIndex, int colIndex)
    {
        float cellWidth = planeSize.x / columns;
        float cellHeight = planeSize.y / rows;

        //Anchor the zone to the bottom left of the plane.
        float anchorX = -planeSize.x * 0.5f;
        float anchorZ = -planeSize.y * 0.5f;

        // Calculate the local center of the cell.
        float localX = anchorX + (colIndex + 0.5f) * cellWidth;
        float localZ = anchorZ + (rowIndex + 0.5f) * cellHeight;

        // Convert the local center to world position.
        Vector3 localCenter = new Vector3(localX, 0f, localZ);
        Vector3 worldCenter = planeTransform.TransformPoint(localCenter);

        //Convert local cell size to world cell size.
        // The planeTransform.lossyScale is the scale of the plane.
        float worldCellSizeX = cellWidth * planeTransform.lossyScale.x;
        float worldCellSizeZ = cellHeight * planeTransform.lossyScale.z;

        //Create the zone object.
        // With name reflecting the row and column index.
        GameObject zoneObject = new GameObject("Zone_" + rowIndex + "_" + colIndex);
        zoneObject.transform.SetParent(transform);
        zoneObject.transform.position = worldCenter;
        zoneObject.transform.rotation = planeTransform.rotation;

        Zone zone = zoneObject.AddComponent<Zone>();
        // Initialize the zone with the world cell size and the zone manager.
        zone.Setup(worldCellSizeX, worldCellSizeZ, this);
        return zone;
    }

}
