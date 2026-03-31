using UnityEngine;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine.AI;

// After floor confirm: spawns zones, optional grid lines, and reads player position from the assigned Main Camera.
public class ZoneManager : MonoBehaviour
{
    // Non-zero clamp to a minimum.
    private const float MIN_CAPTURE = 0.1f;
    private const string PLAYER_TAG = "player";
    private const string ENEMY_TAG = "enemy";

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

        ResetCaptureState(playerCaptureState);
        ResetCaptureState(enemyCaptureState);

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
        // Clamp configured durations so capture math never divides by zero.
        float captureRate = Mathf.Max(secondsToCapture, MIN_CAPTURE);
        float drainRate = Mathf.Max(secondsToDrain, MIN_CAPTURE);

        // Resolve the player's current zone from the object tagged "player".
        GameObject player = GameObject.FindWithTag(PLAYER_TAG);
        Zone playerZone;
        if (player != null) {
            playerZone = GetZoneAt(player.transform.position);
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

        // If both are in the same zone, that zone is contested and capture is paused.
        if (playerZone != null && enemyZone != null && playerZone == enemyZone) {
            if (playerZone.Owner != ZoneOwner.Contested) {
                playerZone.SetOwner(ZoneOwner.Contested);
            }
            return;
        }

        UpdateCaptureForOwner(
            ZoneOwner.Player,
            playerZone,
            captureRate,
            drainRate);

        if (enemy == null) return;

        UpdateCaptureForOwner(
            ZoneOwner.Enemy,
            enemyZone,
            captureRate,
            drainRate);
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
            state.progress = Mathf.Max(0f, state.progress - (Time.deltaTime / drainRate));
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
        return owner == ZoneOwner.Enemy ? enemyCaptureState : playerCaptureState;
    }

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
    }

}
