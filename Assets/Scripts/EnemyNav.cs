using UnityEngine;
using UnityEngine.AI;

public class EnemyNav : MonoBehaviour
{
    private const float MinRetargetInterval = 0.1f;
    private const float MinSampleDistance = 1f;
    private const float MinIdleVelocitySquared = 0.01f;

    private ZoneManager zoneManager;

    [SerializeField]
    private NavMeshAgent navMeshAgent;

    [SerializeField]
    private float retargetIntervalSeconds = 0.5f;

    [SerializeField]
    private float arrivalDistance = 0.2f;

    private Zone activeTargetZone;
    private float nextRetargetAtTime;

    private void OnEnable()
    {
        SetupReferences();
        nextRetargetAtTime = 0f;
    }

    // Validate movement state, then retarget only when needed.
    private void Update()
    {
        if (!CanNavigate()) return;
        if (!ShouldChooseNewTarget()) return;

        ChooseNearestTargetAndMove();
        nextRetargetAtTime = Time.time + Mathf.Max(MinRetargetInterval, retargetIntervalSeconds);
    }

    private bool CanNavigate()
    {
        return zoneManager != null &&
               navMeshAgent != null &&
               navMeshAgent.isOnNavMesh;
    }

    private void SetupReferences()
    {
        if (zoneManager == null) {
            zoneManager = FindFirstObjectByType<ZoneManager>();
        }

        if (navMeshAgent == null) {
            navMeshAgent = GetComponent<NavMeshAgent>();
        }
    }

    private bool ShouldChooseNewTarget()
    {
        // Reacquire when target is missing, no longer capturable, reached, or timer elapsed.
        if (activeTargetZone == null) return true;
        if (!IsCapturableByEnemy(activeTargetZone)) return true;
        if (HasReachedCurrentDestination()) return true;

        return Time.time >= nextRetargetAtTime;
    }

    private void ChooseNearestTargetAndMove()
    {
        // Enemy can capture neutral and player-owned zones.
        activeTargetZone = zoneManager.GetNearestEnemyTargetZone(transform.position);
        if (activeTargetZone == null) return;

        Vector3 targetPosition = activeTargetZone.transform.position;
        SetBestDestination(targetPosition);
    }

    private void SetBestDestination(Vector3 targetPosition)
    {
        // Fall back to nearest sampled NavMesh point if exact center is not directly settable.
        if (!navMeshAgent.SetDestination(targetPosition) &&
            NavMesh.SamplePosition(targetPosition, out NavMeshHit nearestPoint, MinSampleDistance, NavMesh.AllAreas)) {
            navMeshAgent.SetDestination(nearestPoint.position);
        }
    }

    private bool HasReachedCurrentDestination()
    {
        if (navMeshAgent.pathPending) return false;
        if (navMeshAgent.remainingDistance > Mathf.Max(arrivalDistance, navMeshAgent.stoppingDistance)) return false;
        return !navMeshAgent.hasPath || navMeshAgent.velocity.sqrMagnitude < MinIdleVelocitySquared;
    }

    private static bool IsCapturableByEnemy(Zone zone)
    {
        return zone.Owner == ZoneManager.ZoneOwner.Neutral || zone.Owner == ZoneManager.ZoneOwner.Player;
    }
}
