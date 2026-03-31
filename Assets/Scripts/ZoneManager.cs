using UnityEngine;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine.AI;

// After floor confirm: spawns zones, optional grid lines, and reads player position from the assigned Main Camera.
public class ZoneManager : MonoBehaviour
{
    // Non-zero clamp to a minimum.
    private const float MIN_CAPTURE = 0.1f;
    private const string ENEMY_TAG = "Enemy";

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

    [Header("Runtime Navigation")]
    private NavMeshSurface runtimeNavMeshSurface;

    // How long it takes to capture a zone when standing in it.
    [SerializeField]
    private float secondsToCapture = 3f;

    //How long it takes to lose capture progress when stepping out of a zone.
    [SerializeField]
    private float secondsToDrain = 2f;

    // All cells in row-major order (row 0 col 0, row 0 col 1, …).
    // Empty until zones are generated after floor confirm.
    private readonly List<Zone> zones = new List<Zone>();

    // Per-owner capture runtime data.
    // activeZone: current zone this owner is trying to capture.
    // progress: normalized capture progress in range [0..1].
    private class CaptureState {
        public Zone activeZone;
        public float progress;
    }

    // Independent capture state for each side so progress can be tracked separately.
    private readonly CaptureState playerCaptureState = new CaptureState();
    private readonly CaptureState enemyCaptureState = new CaptureState();
    private Zone activeContestedZone;
    private ZoneOwner activeContestedPreviousOwner = ZoneOwner.Neutral;

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

        // Reset capture states and contested zone.
        ResetCaptureState(playerCaptureState);
        ResetCaptureState(enemyCaptureState);
        activeContestedZone = null;
        activeContestedPreviousOwner = ZoneOwner.Neutral;

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

    // Builds navmesh from generated runtime geometry.
    public void BuildRuntimeNavMesh() {
        EnsureRuntimeNavMeshSurface();
        runtimeNavMeshSurface.BuildNavMesh();
    }

    // Returns nearest zone that the enemy should try to capture next.
    public Zone GetNearestEnemyTargetZone(Vector3 fromPosition) {
        return GetNearestZoneByOwners(fromPosition, ZoneOwner.Neutral, ZoneOwner.Player);
    }

    // Returns the first zone containing this world position, if any.
    public Zone GetZoneAtWorldPosition(Vector3 position) {
        return GetZoneAt(position);
    }

    // Updates zone capture and drain progress each frame.
    private void Update() {
        // Clamp durations so capture math never divides by zero.
        float captureRate = Mathf.Max(secondsToCapture, MIN_CAPTURE);
        float drainRate = Mathf.Max(secondsToDrain, MIN_CAPTURE);

        // Resolve the player's current zone from the assigned AR Main Camera transform.
        Zone playerZone;
        if (mainCameraTransform != null) {
            playerZone = GetZoneAt(mainCameraTransform.position);
        } else {
            playerZone = null;
        }

        // Resolve the enemy's current zone from the object tagged "enemy".
        GameObject enemy = GameObject.FindWithTag(ENEMY_TAG);
        Zone enemyZone;
        if (enemy != null) {
            enemyZone = GetZoneAt(enemy.transform.position);
        } else {
            enemyZone = null;
        }

        Zone sharedZone;
        if (playerZone != null && enemyZone != null && playerZone == enemyZone) {
            sharedZone = playerZone;
        } else {
            sharedZone = null;
        }

        UpdateContestedVisualState(sharedZone);

        // If both are in the same zone, that zone is contested and capture is paused.
        if (sharedZone != null) {
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
    private void UpdateContestedVisualState(Zone sharedZone) {
        if (activeContestedZone != null && activeContestedZone != sharedZone) {
            if (activeContestedZone.Owner == ZoneOwner.Contested) {
                activeContestedZone.SetOwner(activeContestedPreviousOwner);
            }
            activeContestedZone = null;
            activeContestedPreviousOwner = ZoneOwner.Neutral;
        }

        if (sharedZone == null) return;

        if (activeContestedZone != sharedZone) {
            activeContestedPreviousOwner = sharedZone.Owner;
            activeContestedZone = sharedZone;
        }

        if (sharedZone.Owner != ZoneOwner.Contested) {
            sharedZone.SetOwner(ZoneOwner.Contested);
        }
    }

    // Shared capture flow for a given owner using a switch on zone ownership.
    private void UpdateCaptureForOwner(
        ZoneOwner capturingOwner,
        Zone zoneUnderActor,
        float captureRate,
        float drainRate)
    {
        CaptureState state = GetCaptureState(capturingOwner);

        if (zoneUnderActor == null) {
            // If the actor is not in a zone, drain capture progress.
            state.progress = Mathf.Max(0f, state.progress - (Time.deltaTime / drainRate));
            // If the capture progress is 0, reset the active zone.
            if (state.progress == 0f) {
                state.activeZone = null;
            }
            return;
        }

        switch (zoneUnderActor.Owner) {
            case ZoneOwner.Player:
                if (capturingOwner == ZoneOwner.Player) {
                    ResetCaptureState(state);
                    return;
                }
                break;
            case ZoneOwner.Enemy:
                if (capturingOwner == ZoneOwner.Enemy) {
                    ResetCaptureState(state);
                    return;
                }
                break;
            case ZoneOwner.Neutral:
            case ZoneOwner.Contested:
                break;
            default:
                return;
        }

        if (state.activeZone != zoneUnderActor) {
            state.activeZone = zoneUnderActor;
            state.progress = 0f;
        }

        state.progress += Time.deltaTime / captureRate;
        if (state.progress < 1f) return;

        zoneUnderActor.SetOwner(capturingOwner);
        ResetCaptureState(state);
    }

    private CaptureState GetCaptureState(ZoneOwner owner) {
        if (owner == ZoneOwner.Enemy) {
            return enemyCaptureState;
        } else {
            return playerCaptureState;
        }
    }

    // Resets the capture state to the default values. activeZone is set to null and progress is set to 0.
    private static void ResetCaptureState(CaptureState state) {
        state.activeZone = null;
        state.progress = 0f;
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

    private Zone GetNearestZoneByOwners(Vector3 fromPosition, ZoneOwner firstAllowedOwner, ZoneOwner secondAllowedOwner) {
        Zone nearestZone = null;
        float nearestDistanceSquared = float.MaxValue;

        foreach (Zone zone in zones) {
            if (zone == null) continue;

            ZoneOwner owner = zone.Owner;
            if (owner != firstAllowedOwner && owner != secondAllowedOwner) continue;

            float distanceSquared = (zone.transform.position - fromPosition).sqrMagnitude;
            if (distanceSquared >= nearestDistanceSquared) continue;

            nearestDistanceSquared = distanceSquared;
            nearestZone = zone;
        }

        return nearestZone;
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

        ResetCaptureState(playerCaptureState);
        ResetCaptureState(enemyCaptureState);
        activeContestedZone = null;
        activeContestedPreviousOwner = ZoneOwner.Neutral;
    }

    private void EnsureRuntimeNavMeshSurface() {
        if (runtimeNavMeshSurface != null) return;

        runtimeNavMeshSurface = GetComponent<NavMeshSurface>();
        if (runtimeNavMeshSurface == null) {
            runtimeNavMeshSurface = gameObject.AddComponent<NavMeshSurface>();
        }

        runtimeNavMeshSurface.collectObjects = CollectObjects.Children;
        runtimeNavMeshSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
    }

}
