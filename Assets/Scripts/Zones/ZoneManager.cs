using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;
using Unity.AI.Navigation;

// Grid of capture cells on the AR floor: ownership, contested overlap, per-side capture meters, and runtime NavMesh.
// Player position comes from Main Camera; enemy from the "Enemy" tag. Grid size / capture times persist in PlayerPrefs.
public class ZoneManager : MonoBehaviour
{
    private const float MinCaptureSeconds = 0.1f;
    private const int MinGridSize = 2;
    private const int MaxGridSize = 4;

    private const string PP_ROWS = "ConfigRows";
    private const string PP_COLUMNS = "ConfigColumns";
    private const string PP_PLAYER_CAPTURE = "ConfigPlayerCaptureSeconds";
    private const string PP_ENEMY_CAPTURE = "ConfigEnemyCaptureSeconds";

    public enum ZoneOwner
    {
        Neutral,
        Contested,
        Player,
        Enemy
    }

    [SerializeField] private Transform mainCameraTransform;
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

    [SerializeField] private float playerSecondsToCapture = 3f;
    [SerializeField] private float enemySecondsToCapture = 3f;
    [SerializeField] private readonly float secondsToDrain = 2f;

    private NavMeshSurface runtimeNavMeshSurface;

    public List<Zone> zones = new List<Zone>();

    // Per-side capture meter on neutral / enemy / contested tiles. Progress is 0..1 until the tile flips.
    private class CaptureState
    {
        public Zone activeZone;
        public float progress;
    }

    private CaptureState playerCaptureState = new CaptureState();
    private CaptureState enemyCaptureState = new CaptureState();

    private Zone activeContestedZone;

    // When overlap ends, revert contested visuals to whoever owned the tile before Contested.
    private ZoneOwner activeContestedPreviousOwner = ZoneOwner.Neutral;

    public Transform MainCameraTransform => mainCameraTransform;

    public int Rows => rows;
    public int Columns => columns;
    public float PlayerSecondsToCapture => playerSecondsToCapture;
    public float EnemySecondsToCapture => enemySecondsToCapture;

    public void SetRows(int value) => rows = Mathf.Clamp(value, MinGridSize, MaxGridSize);
    public void SetColumns(int value) => columns = Mathf.Clamp(value, MinGridSize, MaxGridSize);

    public void SetPlayerSecondsToCapture(float value) =>
        playerSecondsToCapture = Mathf.Max(MinCaptureSeconds, value);

    public void SetEnemySecondsToCapture(float value) =>
        enemySecondsToCapture = Mathf.Max(MinCaptureSeconds, value);

    private void Awake() => LoadConfigFromPlayerPrefs();

    private void OnDestroy() => SaveConfigPrefs();

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveConfigPrefs();
        }
    }

    private void LoadConfigFromPlayerPrefs()
    {
        SetRows(PlayerPrefs.GetInt(PP_ROWS, rows));
        SetColumns(PlayerPrefs.GetInt(PP_COLUMNS, columns));
        SetPlayerSecondsToCapture(PlayerPrefs.GetFloat(PP_PLAYER_CAPTURE, playerSecondsToCapture));
        SetEnemySecondsToCapture(PlayerPrefs.GetInt(PP_ENEMY_CAPTURE, Mathf.RoundToInt(enemySecondsToCapture)));
    }

    private void SaveConfigPrefs()
    {
        PlayerPrefs.SetInt(PP_ROWS, rows);
        PlayerPrefs.SetInt(PP_COLUMNS, columns);
        PlayerPrefs.SetFloat(PP_PLAYER_CAPTURE, playerSecondsToCapture);
        PlayerPrefs.SetInt(PP_ENEMY_CAPTURE, Mathf.RoundToInt(enemySecondsToCapture));
        PlayerPrefs.Save();
    }

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
        runtimeNavMeshSurface?.RemoveData();
    }

    public static bool CanEnemyCapture(Zone zone) =>
        zone != null && (zone.Owner == ZoneOwner.Neutral || zone.Owner == ZoneOwner.Player);

    private void ClearAllZones()
    {
        foreach (Zone zone in zones)
        {
            Destroy(zone.gameObject);
        }

        zones.Clear();
        ResetBothCaptureStates();
        ResetContestedOverlapTracking();
    }

    private void GenerateZones(Transform planeTransform, Vector2 planeSize)
    {
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

    private void BuildRuntimeNavMesh()
    {
        if (runtimeNavMeshSurface == null)
        {
            runtimeNavMeshSurface = GetComponent<NavMeshSurface>() ?? gameObject.AddComponent<NavMeshSurface>();
        }

        runtimeNavMeshSurface.collectObjects = CollectObjects.Children;
        runtimeNavMeshSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        runtimeNavMeshSurface.BuildNavMesh();
    }

    public Zone GetNearestEnemyTargetZone(Vector3 fromPosition)
    {
        Zone nearest = null;
        float bestSqr = float.MaxValue;
        foreach (Zone zone in zones)
        {
            if (!CanEnemyCapture(zone))
            {
                continue;
            }

            float sqr = (zone.transform.position - fromPosition).sqrMagnitude;
            if (sqr >= bestSqr)
            {
                continue;
            }

            bestSqr = sqr;
            nearest = zone;
        }

        return nearest;
    }

    public Zone GetZoneAtWorldPosition(Vector3 position)
    {
        foreach (Zone zone in zones)
        {
            if (zone.Contains(position)) return zone;
        }

        return null;
    }

    /// <summary>
    /// True when <paramref name="position"/> lies in a cell whose owner is <see cref="ZoneOwner.Contested"/> (active fight tile per <c>UpdateContestedVisualState</c>).
    /// </summary>
    public bool IsContestedZoneAtWorldPosition(Vector3 position)
    {
        Zone zone = GetZoneAtWorldPosition(position);
        return zone != null && zone.Owner == ZoneOwner.Contested;
    }

    private static bool IsPlayerDead()
    {
        return HealthSystem.Instance != null && HealthSystem.Instance.IsGhost;
    }

    // Resolve cells under player/enemy → contested overlap / instant win → otherwise run capture + drain.
    private void Update()
    {
        float playerCaptureRate = Mathf.Max(playerSecondsToCapture, MinCaptureSeconds);
        float enemyCaptureRate = Mathf.Max(enemySecondsToCapture, MinCaptureSeconds);
        float drainRate = Mathf.Max(secondsToDrain, MinCaptureSeconds);

        Zone playerZone = GetZoneUnderMainCamera();
        Zone enemyZone = GetZoneUnderEnemy();
        Zone sharedZone = GetSharedCellIfBothStandInSame(playerZone, enemyZone);

        if (TryResolveInstantContestedCapture(sharedZone))
        {
            return;
        }

        UpdateContestedVisualState(sharedZone);

        // Same cell while alive: contested visuals only; individual capture meters stay paused until they split.
        if (sharedZone != null)
        {
            return;
        }

        Zone playerZoneForCapture = IsPlayerDead() ? null : playerZone;
        UpdateCaptureForOwner(ZoneOwner.Player, playerZoneForCapture, playerCaptureRate, drainRate);
        UpdateCaptureForOwner(ZoneOwner.Enemy, enemyZone, enemyCaptureRate, drainRate);
    }

    private Zone GetZoneUnderMainCamera()
    {
        if (mainCameraTransform == null)
        {
            return null;
        }

        return GetZoneAtWorldPosition(mainCameraTransform.position);
    }

    private Zone GetZoneUnderEnemy()
    {
        GameObject enemy = GameObject.FindGameObjectWithTag("Enemy");
        if (enemy == null)
        {
            return null;
        }

        return GetZoneAtWorldPosition(enemy.transform.position);
    }

    private Zone GetSharedCellIfBothStandInSame(Zone playerZone, Zone enemyZone)
    {
        if (IsPlayerDead() || playerZone == null || enemyZone == null || playerZone != enemyZone)
        {
            return null;
        }

        return playerZone;
    }

    // Instant-capture pickup wins the standoff: grant the tile to the player and clear overlap bookkeeping.
    private bool TryResolveInstantContestedCapture(Zone sharedZone)
    {
        if (sharedZone == null || IsPlayerDead())
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
            PlayZoneCaptureSfx(isPlayerCapture: true);
            Messenger<Zone>.Broadcast(GameEvent.PLAYER_CAPTURED_ZONE, sharedZone, MessengerMode.DONT_REQUIRE_LISTENER);
        }

        sharedZone.ApplyOwnerColor();
        ResetContestedOverlapTracking();
        ClearCaptureStatesTargeting(sharedZone);
        ZeroBothCaptureProgress();
        return true;
    }

    private void UpdateContestedVisualState(Zone sharedZone)
    {
        EndStaleContestedVisual(sharedZone);

        if (sharedZone == null)
        {
            return;
        }

        if (activeContestedZone != sharedZone)
        {
            activeContestedPreviousOwner = sharedZone.Owner;
            activeContestedZone = sharedZone;
        }

        if (sharedZone.Owner != ZoneOwner.Contested)
        {
            SetZoneOwner(sharedZone, ZoneOwner.Contested);
        }
    }

    private void EndStaleContestedVisual(Zone sharedZone)
    {
        if (activeContestedZone == null || activeContestedZone == sharedZone)
        {
            return;
        }

        if (activeContestedZone.Owner == ZoneOwner.Contested)
        {
            SetZoneOwner(activeContestedZone, activeContestedPreviousOwner);
        }

        ResetContestedOverlapTracking();
    }

    private void ResetBothCaptureStates()
    {
        playerCaptureState.activeZone = null;
        playerCaptureState.progress = 0f;
        enemyCaptureState.activeZone = null;
        enemyCaptureState.progress = 0f;
    }

    private void ZeroBothCaptureProgress()
    {
        playerCaptureState.progress = 0f;
        enemyCaptureState.progress = 0f;
    }

    private void ResetContestedOverlapTracking()
    {
        activeContestedZone = null;
        activeContestedPreviousOwner = ZoneOwner.Neutral;
    }

    private void ClearCaptureStatesTargeting(Zone target)
    {
        if (playerCaptureState.activeZone == target)
        {
            playerCaptureState.activeZone = null;
        }

        if (enemyCaptureState.activeZone == target)
        {
            enemyCaptureState.activeZone = null;
        }
    }

    // Advances or drains one side's capture meter for the zone they are standing in (or drains when not in a zone).
    // `captureRate` / `drainRate` are full-bar times in seconds (already clamped in Update).
    private void UpdateCaptureForOwner(ZoneOwner capturerOwner, Zone zoneUnder, float captureRate, float drainRate)
    {
        CaptureState ownerState = GetCaptureState(capturerOwner);
        float captureDeltaTime = GetCaptureDeltaTimeForOwner(capturerOwner);

        // Not standing in any cell: decay in-progress capture on the last targeted zone, then clear when empty.
        if (zoneUnder == null)
        {
            DrainCaptureProgressWhileOutsideZone(ownerState, capturerOwner, captureDeltaTime, drainRate);
            return;
        }

        // Standing on a tile we already fully own (Player on Player, Enemy on Enemy). Drop any stray preview progress.
        // Neutral / Contested / opponent-owned tiles fall through so capture can continue or start.
        if (zoneUnder.Owner == capturerOwner)
        {
            ResetCaptureStateOnZone(ownerState, capturerOwner);
            return;
        }

        // Switched to a different capture target: restore the old tile visuals and restart progress from 0.
        if (ownerState.activeZone != zoneUnder)
        {
            if (ownerState.activeZone != null)
            {
                ownerState.activeZone.ApplyOwnerColor();
            }

            ownerState.activeZone = zoneUnder;
            ownerState.progress = 0f;
        }

        // Fill the meter (instant buff snaps to full; otherwise linear over captureRate seconds).
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
            ownerState.progress += captureDeltaTime / captureRate;
        }

        zoneUnder.ApplyCapturePreview(capturerOwner, ownerState.progress);

        // Still filling — ownership change happens only when progress reaches 1.
        if (ownerState.progress < 1f)
        {
            return;
        }

        CompleteZoneCapture(capturerOwner, zoneUnder, ownerState);
    }

    private static float GetCaptureDeltaTimeForOwner(ZoneOwner capturerOwner)
    {
        float captureDeltaTime = Time.deltaTime;
        if (capturerOwner == ZoneOwner.Enemy && PickupEffects.Instance != null)
        {
            captureDeltaTime *= PickupEffects.Instance.EnemySimulationTimeScale;
        }

        return captureDeltaTime;
    }

    // While the capturer is not in a zone, drain progress on the active target at `drainRate` until it hits zero.
    private static void DrainCaptureProgressWhileOutsideZone(
        CaptureState ownerState,
        ZoneOwner capturerOwner,
        float captureDeltaTime,
        float drainRate)
    {
        if (ownerState.activeZone != null)
        {
            ownerState.progress = Mathf.Max(0f, ownerState.progress - (captureDeltaTime / drainRate));
            ownerState.activeZone.ApplyCapturePreview(capturerOwner, ownerState.progress);
        }

        if (ownerState.progress != 0f)
        {
            return;
        }

        if (ownerState.activeZone != null)
        {
            ownerState.activeZone.ApplyOwnerColor();
        }

        ownerState.activeZone = null;
    }

    // Clears in-progress capture UI/state when the capturer idles on their own color.
    private static void ResetCaptureStateOnZone(CaptureState ownerState, ZoneOwner capturerOwner)
    {
        if (ownerState.activeZone != null)
        {
            ownerState.activeZone.ApplyOwnerColor();
        }

        ownerState.activeZone = null;
        ownerState.progress = 0f;
    }

    private void CompleteZoneCapture(ZoneOwner capturerOwner, Zone zoneUnder, CaptureState ownerState)
    {
        SetZoneOwner(zoneUnder, capturerOwner);
        if (capturerOwner == ZoneOwner.Player)
        {
            PlayZoneCaptureSfx(isPlayerCapture: true);
            Messenger<Zone>.Broadcast(GameEvent.PLAYER_CAPTURED_ZONE, zoneUnder, MessengerMode.DONT_REQUIRE_LISTENER);
        }
        else if (capturerOwner == ZoneOwner.Enemy)
        {
            PlayZoneCaptureSfx(isPlayerCapture: false);
            Messenger<Zone>.Broadcast(GameEvent.ENEMY_CAPTURED_ZONE, zoneUnder, MessengerMode.DONT_REQUIRE_LISTENER);
        }

        zoneUnder.ApplyOwnerColor();
        ownerState.activeZone = null;
        ownerState.progress = 0f;
    }

    private CaptureState GetCaptureState(ZoneOwner owner) =>
        owner == ZoneOwner.Enemy ? enemyCaptureState : playerCaptureState;

    private static void PlayZoneCaptureSfx(bool isPlayerCapture)
    {
        SoundLibrary library = SoundLibrary.Instance;
        if (library == null)
        {
            return;
        }

        AudioClip clip = isPlayerCapture ? library.PlayerZoneCaptureSfx : library.EnemyZoneCaptureSfx;
        if (clip == null || SoundManager.Instance == null)
        {
            return;
        }

        SoundManager.Instance.PlayOneShot(clip);
    }

    private void SetZoneOwner(Zone zone, ZoneOwner newOwner)
    {
        if (zone == null || zone.Owner == newOwner)
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

        ResetBothCaptureStates();
    }

    public Zone GetRandomZone()
    {
        return zones.Count == 0 ? null : zones[Random.Range(0, zones.Count)];
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

        return neutralZones.Count > 0 ? neutralZones[Random.Range(0, neutralZones.Count)] : GetRandomZone();
    }

    private Zone CreateZone(Transform planeTransform, Vector2 planeSize, int rowIndex, int colIndex)
    {
        float cellWidth = planeSize.x / columns;
        float cellHeight = planeSize.y / rows;

        // Bottom-left anchored grid in plane local space → world center and AABB size for Contains().
        float anchorX = -planeSize.x * 0.5f;
        float anchorZ = -planeSize.y * 0.5f;
        float localX = anchorX + (colIndex + 0.5f) * cellWidth;
        float localZ = anchorZ + (rowIndex + 0.5f) * cellHeight;
        Vector3 worldCenter = planeTransform.TransformPoint(new Vector3(localX, 0f, localZ));
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
}
