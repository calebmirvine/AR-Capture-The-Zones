using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;
using Unity.AI.Navigation;

// After floor confirm: spawns zones, optional grid lines, and reads player position from the assigned Main Camera.
public class ZoneManager : MonoBehaviour
{
    private const float MinCaptureSeconds = 0.1f;

    [SerializeField] private Transform mainCameraTransform;

    public Transform MainCameraTransform => mainCameraTransform;

    private const int MinGridSize = 2;
    private const int MaxGridSize = 4;

    [SerializeField] private int columns = 3;
    [SerializeField] private int rows = 2;

    [SerializeField] private Material floorMaterialPlayer;
    [SerializeField] private Material floorMaterialEnemy;
    [SerializeField] private Material floorMaterialNeutral;
    [SerializeField] private Material floorMaterialContested;

    [SerializeField] private GameObject flagPrefabNeutral;
    [SerializeField] private GameObject flagPrefabPlayer;
    [SerializeField] private GameObject flagPrefabEnemy;
    [SerializeField] private GameObject flagPrefabContested;

    private NavMeshSurface runtimeNavMeshSurface;

    [SerializeField] private float playerSecondsToCapture = 3f;
    [SerializeField] private float enemySecondsToCapture = 3f;
    [SerializeField] private readonly float secondsToDrain = 2f;

    public List<Zone> zones = new List<Zone>();

    public void SetRows(int value) =>
        rows = Mathf.Clamp(value, MinGridSize, MaxGridSize);

    public void SetColumns(int value) =>
        columns = Mathf.Clamp(value, MinGridSize, MaxGridSize);

    public void SetPlayerSecondsToCapture(float value) =>
        playerSecondsToCapture = Mathf.Max(MinCaptureSeconds, value);

    public void SetEnemySecondsToCapture(float value) =>
        enemySecondsToCapture = Mathf.Max(MinCaptureSeconds, value);
    

    private void OnValidate()
    {
        rows = Mathf.Clamp(rows, MinGridSize, MaxGridSize);
        columns = Mathf.Clamp(columns, MinGridSize, MaxGridSize);
        playerSecondsToCapture = Mathf.Max(MinCaptureSeconds, playerSecondsToCapture);
        enemySecondsToCapture = Mathf.Max(MinCaptureSeconds, enemySecondsToCapture);
    }

    private void OnEnable()
    {
        Messenger<Transform, Vector2>.AddListener(GameEvent.FLOOR_CONFIRMED, OnFloorConfirmed);
        Messenger.AddListener(GameEvent.GAME_RESET_REQUESTED, OnGameResetRequested);
    }

    private void OnDisable()
    {
        Messenger<Transform, Vector2>.RemoveListener(GameEvent.FLOOR_CONFIRMED, OnFloorConfirmed);
        Messenger.RemoveListener(GameEvent.GAME_RESET_REQUESTED, OnGameResetRequested);
    }

    private void OnFloorConfirmed(Transform planeTransform, Vector2 planeSize)
    {
        GenerateZones(planeTransform, planeSize);
        BuildRuntimeNavMesh();
        Messenger.Broadcast(GameEvent.GAMEPLAY_STARTED, MessengerMode.DONT_REQUIRE_LISTENER);
    }

    private void OnGameResetRequested()
    {
        ClearAllZones();
        if (runtimeNavMeshSurface != null)
        {
            runtimeNavMeshSurface.RemoveData();
        }
    }

    // Capture state for each owner
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
    private void GenerateZones(Transform planeTransform, Vector2 planeSize)
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
    }

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
    private void BuildRuntimeNavMesh()
    {
        if (runtimeNavMeshSurface == null)
        {
            runtimeNavMeshSurface = GetComponent<NavMeshSurface>();
            if (runtimeNavMeshSurface == null)
            {
                runtimeNavMeshSurface = gameObject.AddComponent<NavMeshSurface>();
            }
        }

        runtimeNavMeshSurface.collectObjects = CollectObjects.Children;
        runtimeNavMeshSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
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

    private static bool IsPlayerDead()
    {
        return HealthSystem.Instance != null && HealthSystem.Instance.IsGhost;
    }

    // Updates zone capture and drain progress each frame.
    private void Update()
    {
        // Clamp durations so capture math never divides by zero.
        float playerCaptureRate = Mathf.Max(playerSecondsToCapture, MinCaptureSeconds);
        float enemyCaptureRate = Mathf.Max(enemySecondsToCapture, MinCaptureSeconds);
        float drainRate = Mathf.Max(secondsToDrain, MinCaptureSeconds);

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

        if (TryResolveInstantContestedCapture(sharedZone))
        {
            return;
        }

        UpdateContestedVisualState(sharedZone);

        // If both are in the same zone, that zone is contested and capture is paused.
        if (sharedZone != null)
        {
            return;
        }

        Zone playerZoneForCapture = IsPlayerDead() ? null : playerZone;

        UpdateCaptureForOwner(
            ZoneOwner.Player,
            playerZoneForCapture,
            playerCaptureRate,
            drainRate);


        UpdateCaptureForOwner(
            ZoneOwner.Enemy,
            enemyZone,
            enemyCaptureRate,
            drainRate);
    }

    private bool TryResolveInstantContestedCapture(Zone sharedZone)
    {
        if (sharedZone == null)
        {
            return false;
        }

        if (IsPlayerDead())
        {
            return false;
        }

        if (PickupEffects.Instance == null || !PickupEffects.Instance.IsInstantPlayerCaptureActive)
        {
            return false;
        }

        ZoneOwner previousOwner = sharedZone.Owner;
        SetZoneOwner(sharedZone, ZoneOwner.Player);
        if (previousOwner != ZoneOwner.Player)
        {
            Messenger<Zone>.Broadcast(GameEvent.PLAYER_CAPTURED_ZONE, sharedZone, MessengerMode.DONT_REQUIRE_LISTENER);
        }

        sharedZone.ApplyOwnerColor();

        activeContestedZone = null;
        activeContestedPreviousOwner = ZoneOwner.Neutral;

        if (playerCaptureState.activeZone == sharedZone)
        {
            playerCaptureState.activeZone = null;
        }

        if (enemyCaptureState.activeZone == sharedZone)
        {
            enemyCaptureState.activeZone = null;
        }

        playerCaptureState.progress = 0f;
        enemyCaptureState.progress = 0f;
        return true;
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
                SetZoneOwner(activeContestedZone, activeContestedPreviousOwner);
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
            SetZoneOwner(sharedZone, ZoneOwner.Contested);
        }
    }

    // Shared capture flow for a given owner using a switch on zone ownership.
    private void UpdateCaptureForOwner(ZoneOwner capturerOwner, Zone zoneUnder, float captureRate, float drainRate)
    {
        CaptureState ownerState = GetCaptureState(capturerOwner);

        if (zoneUnder == null)
        {
            if (ownerState.activeZone != null)
            {
                ownerState.progress = Mathf.Max(0f, ownerState.progress - (Time.deltaTime / drainRate));
                ownerState.activeZone.ApplyCapturePreview(capturerOwner, ownerState.progress);
            }

            if (ownerState.progress == 0f)
            {
                if (ownerState.activeZone != null)
                {
                    ownerState.activeZone.ApplyOwnerColor();
                }

                ownerState.activeZone = null;
            }

            return;
        }

        switch (zoneUnder.Owner)
        {
            case ZoneOwner.Player:
                if (capturerOwner == ZoneOwner.Player)
                {
                    if (ownerState.activeZone != null)
                    {
                        ownerState.activeZone.ApplyOwnerColor();
                    }

                    ownerState.activeZone = null;
                    ownerState.progress = 0f;
                    return;
                }

                break;
            case ZoneOwner.Enemy:
                if (capturerOwner == ZoneOwner.Enemy)
                {
                    if (ownerState.activeZone != null)
                    {
                        ownerState.activeZone.ApplyOwnerColor();
                    }

                    ownerState.activeZone = null;
                    ownerState.progress = 0f;
                    return;
                }

                break;
            case ZoneOwner.Neutral:
                break;
            case ZoneOwner.Contested:
                break;
            default:
                return;
        }

        if (ownerState.activeZone != zoneUnder)
        {
            if (ownerState.activeZone != null)
            {
                ownerState.activeZone.ApplyOwnerColor();
            }

            ownerState.activeZone = zoneUnder;
            ownerState.progress = 0f;
        }

        bool instantCaptureBuffActive =
            capturerOwner == ZoneOwner.Player
            && PickupEffects.Instance != null
            && PickupEffects.Instance.IsInstantPlayerCaptureActive;

        if (instantCaptureBuffActive)
        {
            ownerState.progress = 1f;
        }
        else
        {
            ownerState.progress += Time.deltaTime / captureRate;
        }

        zoneUnder.ApplyCapturePreview(capturerOwner, ownerState.progress);
        if (ownerState.progress < 1f)
        {
            return;
        }

        SetZoneOwner(zoneUnder, capturerOwner);
        if (capturerOwner == ZoneOwner.Player)
        {
            Messenger<Zone>.Broadcast(GameEvent.PLAYER_CAPTURED_ZONE, zoneUnder, MessengerMode.DONT_REQUIRE_LISTENER);
        }
        else if (capturerOwner == ZoneOwner.Enemy)
        {
            Messenger<Zone>.Broadcast(GameEvent.ENEMY_CAPTURED_ZONE, zoneUnder, MessengerMode.DONT_REQUIRE_LISTENER);
        }

        zoneUnder.ApplyOwnerColor();
        ownerState.activeZone = null;
        ownerState.progress = 0f;
    }

    // Returns the capture state for a given owner.
    private CaptureState GetCaptureState(ZoneOwner owner)
    {
        if (owner == ZoneOwner.Enemy)
        {
            return enemyCaptureState;
        }

        return playerCaptureState;
    }

    private void SetZoneOwner(Zone zone, ZoneOwner newOwner)
    {
        if (zone == null)
        {
            return;
        }

        ZoneOwner previousOwner = zone.Owner;
        if (previousOwner == newOwner)
        {
            return;
        }

        zone.SetOwner(newOwner);
        switch (newOwner)
        {
            case ZoneOwner.Neutral:
                Messenger<Zone>.Broadcast(GameEvent.ZONE_BECAME_NEUTRAL, zone, MessengerMode.DONT_REQUIRE_LISTENER);
                break;
            case ZoneOwner.Contested:
                Messenger<Zone>.Broadcast(GameEvent.ZONE_BECAME_CONTESTED, zone, MessengerMode.DONT_REQUIRE_LISTENER);
                break;
            case ZoneOwner.Player:
                Messenger<Zone>.Broadcast(GameEvent.ZONE_BECAME_PLAYER, zone, MessengerMode.DONT_REQUIRE_LISTENER);
                break;
            case ZoneOwner.Enemy:
                Messenger<Zone>.Broadcast(GameEvent.ZONE_BECAME_ENEMY, zone, MessengerMode.DONT_REQUIRE_LISTENER);
                break;
        }
    }

    // Swaps every Player zone with every Enemy zone. Neutral and Contested zones are untouched.
    public void SwapPlayerAndEnemyZones()
    {
        List<Zone> playerZones = new List<Zone>();
        List<Zone> enemyZones = new List<Zone>();
        foreach (Zone zone in zones)
        {
            if (zone == null)
            {
                continue;
            }

            if (zone.Owner == ZoneOwner.Player)
            {
                playerZones.Add(zone);
            }
            else if (zone.Owner == ZoneOwner.Enemy)
            {
                enemyZones.Add(zone);
            }
        }

        // Snapshot first so newly-flipped zones aren't re-swapped back on the second pass.
        foreach (Zone zone in playerZones)
        {
            SetZoneOwner(zone, ZoneOwner.Enemy);
        }

        foreach (Zone zone in enemyZones)
        {
            SetZoneOwner(zone, ZoneOwner.Player);
        }

        // Keep the contested "previous owner" consistent with the swap so the
        // contested zone resolves to the now-correct side once overlap ends.
        if (activeContestedPreviousOwner == ZoneOwner.Player)
        {
            activeContestedPreviousOwner = ZoneOwner.Enemy;
        }
        else if (activeContestedPreviousOwner == ZoneOwner.Enemy)
        {
            activeContestedPreviousOwner = ZoneOwner.Player;
        }

        // Clear any in-progress captures so stale progress doesn't carry over onto flipped zones.
        playerCaptureState.activeZone = null;
        playerCaptureState.progress = 0f;
        enemyCaptureState.activeZone = null;
        enemyCaptureState.progress = 0f;
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
