using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Travels to Neutral/Player zones, holds until ZoneManager flips ownership to Enemy, then waits before the next zone.
// ZoneManager is assigned at runtime (e.g. GameManager.Configure after spawn), not in the Inspector.
public class EnemyZonePatrol : MonoBehaviour
{
    private enum PatrolState {
        Traveling,
        Capturing,
        CooldownBetweenZones,
    }

    private ZoneManager zoneManager;

    [Header("Movement")]
    [SerializeField] private float patrolSpeed = 2f;
    [Tooltip("NavMeshAgent acceleration; lower than default for slower-feeling starts/stops.")]
    [SerializeField] private float patrolAcceleration = 6f;

    [Header("Pathfinding")]
    [Min(0.1f)]
    [SerializeField] private float navMeshSampleRadius = 2f;

    const float TargetReachedEpsilon = 0.25f;

    [Header("Capture / pacing")]
    [Tooltip("Seconds to wait after a zone is captured before moving to the next target.")]
    [SerializeField] private float secondsBeforeNextZone = 2f;

    private NavMeshAgent agent;
    private PatrolState state = PatrolState.Traveling;
    private Zone currentTargetZone;
    private float cooldownEndTime;
    private float lastDestinationSetTime = -999f;

    void Awake() {
        agent = GetComponent<NavMeshAgent>();
    }

    void OnDestroy() {
        if (zoneManager != null) {
            zoneManager.UnregisterEnemy(transform);
        }
    }

    public void Configure(ZoneManager manager) {
        zoneManager = manager;
        ApplyPatrolMovement();
        zoneManager.RegisterEnemy(transform);
    }

    void ApplyPatrolMovement() {
        if (agent == null) return;
        agent.speed = patrolSpeed;
        agent.acceleration = patrolAcceleration;
    }

    void Start() {
        if (zoneManager != null && agent != null) {
            BeginTravelToRandomZone();
        }
    }

    void Update() {
        if (zoneManager == null || agent == null || !agent.enabled) {
            return;
        }
        if (state == PatrolState.Traveling && currentTargetZone == null) {
            BeginTravelToRandomZone();
        }

        switch (state) {
            case PatrolState.Traveling:
                UpdateTraveling();
                break;
            case PatrolState.Capturing:
                UpdateCapturing();
                break;
            case PatrolState.CooldownBetweenZones:
                UpdateCooldown();
                break;
        }
    }

    void UpdateTraveling() {
        if (currentTargetZone == null) {
            return;
        }
        if (!agent.isOnNavMesh) {
            return;
        }
        if (agent.pathPending) {
            return;
        }
        float dist = agent.remainingDistance;
        if (float.IsInfinity(dist) || float.IsNaN(dist)) {
            return;
        }
        if (dist > agent.stoppingDistance + TargetReachedEpsilon) {
            return;
        }
        if (agent.velocity.sqrMagnitude > 0.01f) {
            return;
        }
        if (!currentTargetZone.Contains(transform.position)) {
            AbortCaptureAndTravel();
            return;
        }

        state = PatrolState.Capturing;
        agent.isStopped = true;
        agent.ResetPath();
    }

    void UpdateCapturing() {
        if (currentTargetZone == null) {
            AbortCaptureAndTravel();
            return;
        }
        if (!currentTargetZone.Contains(transform.position)) {
            AbortCaptureAndTravel();
            return;
        }

        if (currentTargetZone.Owner == ZoneOwner.Enemy) {
            state = PatrolState.CooldownBetweenZones;
            cooldownEndTime = Time.time + Mathf.Max(0f, secondsBeforeNextZone);
            currentTargetZone = null;
            agent.isStopped = true;
        }
    }

    void UpdateCooldown() {
        if (Time.time < cooldownEndTime) {
            return;
        }
        agent.isStopped = false;
        state = PatrolState.Traveling;
        BeginTravelToRandomZone();
    }

    void AbortCaptureAndTravel() {
        currentTargetZone = null;
        agent.isStopped = false;
        state = PatrolState.Traveling;
        BeginTravelToRandomZone();
    }

    void BeginTravelToRandomZone() {
        if (Time.time - lastDestinationSetTime < 0.15f) {
            return;
        }
        List<Zone> all = zoneManager.GetAllZones();
        if (all == null || all.Count == 0) {
            return;
        }

        List<Zone> eligible = new List<Zone>();
        for (int i = 0; i < all.Count; i++) {
            Zone z = all[i];
            if (z.Owner == ZoneOwner.Neutral || z.Owner == ZoneOwner.Player) {
                eligible.Add(z);
            }
        }
        if (eligible.Count == 0) {
            eligible.AddRange(all);
        }

        float effectiveSampleRadius = Mathf.Max(0.1f, navMeshSampleRadius);
        const int maxAttempts = 12;
        for (int attempt = 0; attempt < maxAttempts; attempt++) {
            Zone z = eligible[Random.Range(0, eligible.Count)];
            Vector3 candidate = z.GetRandomPointOnFloor();
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, effectiveSampleRadius, NavMesh.AllAreas)) {
                currentTargetZone = z;
                agent.isStopped = false;
                agent.SetDestination(hit.position);
                lastDestinationSetTime = Time.time;
                return;
            }
        }

        Zone fallback = eligible[Random.Range(0, eligible.Count)];
        if (NavMesh.SamplePosition(fallback.transform.position, out NavMeshHit hit2, effectiveSampleRadius * 2f, NavMesh.AllAreas)) {
            currentTargetZone = fallback;
            agent.isStopped = false;
            agent.SetDestination(hit2.position);
            lastDestinationSetTime = Time.time;
            return;
        }

        lastDestinationSetTime = Time.time;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected() {
        if (agent == null) {
            agent = GetComponent<NavMeshAgent>();
        }
        if (agent == null) {
            return;
        }

        switch (state) {
            case PatrolState.Traveling:
                Gizmos.color = Color.cyan;
                break;
            case PatrolState.Capturing:
                Gizmos.color = Color.yellow;
                break;
            case PatrolState.CooldownBetweenZones:
                Gizmos.color = new Color(1f, 0.5f, 0f);
                break;
        }

        Gizmos.DrawWireSphere(transform.position, agent.radius);

        if (agent.hasPath && agent.path.corners.Length > 0) {
            Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.9f);
            Vector3[] corners = agent.path.corners;
            for (int i = 0; i < corners.Length - 1; i++) {
                Gizmos.DrawLine(corners[i], corners[i + 1]);
            }
        }

        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, agent.destination);
        Gizmos.DrawWireSphere(agent.destination, 0.15f);

        float arriveRadius = agent.stoppingDistance + TargetReachedEpsilon;
        Gizmos.color = new Color(0f, 1f, 0f, 0.35f);
        Gizmos.DrawWireSphere(agent.destination, arriveRadius);

        if (currentTargetZone != null) {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(currentTargetZone.transform.position, 0.35f);
            Gizmos.DrawLine(transform.position, currentTargetZone.transform.position);
        }

        Handles.Label(transform.position + Vector3.up * 2.2f, "Patrol: " + state);
    }
#endif
}
