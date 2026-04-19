using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Transform playerCameraTransform;
    [SerializeField] private ZoneManager zoneManager;

    [SerializeField] private float attackRange = 1f;
    [SerializeField] private float attackRangeStop = 2f;
    [SerializeField] private float chaseRange = 5f;

    private Zone currentTargetZone;

    public float ChaseRange => chaseRange;
    public float AttackRange => attackRange;
    public float AttackRangeStop => attackRangeStop;

    public NavMeshAgent Agent => agent;

    public ZoneManager ZoneManager
    {
        get => zoneManager;
        set => zoneManager = value;
    }

    public Zone CurrentTargetZone => currentTargetZone;

    public Zone GetZoneUnderFoot() => zoneManager.GetZoneAtWorldPosition(transform.position);

    public Zone GetPlayerZone() => zoneManager.GetZoneAtWorldPosition(playerCameraTransform.position);

    public bool IsZoneContestedWithPlayer()
    {
        Zone playerZone = GetPlayerZone();
        Zone enemyZone = GetZoneUnderFoot();

        return playerZone != null
            && enemyZone != null
            && playerZone == enemyZone;
    }

    public bool IsInCapturableZone()
    {
        Zone zone = GetZoneUnderFoot();

        return zone != null
            && (ZoneManager.CanEnemyCapture(zone)
                || zone.Owner == ZoneManager.ZoneOwner.Contested);
    }

    public bool ShouldRotateFromContestedZone()
    {
        Zone enemyZone = GetZoneUnderFoot();
        return enemyZone != null
            && enemyZone.Owner == ZoneManager.ZoneOwner.Contested
            && !IsZoneContestedWithPlayer();
    }

    public bool ShouldChasePlayer()
    {
        return Vector3.Distance(transform.position, playerCameraTransform.position) <= chaseRange;
    }

    public bool IsPlayerInAttackRange()
    {
        return Vector3.Distance(transform.position, playerCameraTransform.position) <= attackRange;
    }

    public void StopMovement()
    {
        agent.isStopped = true;
    }

    public void ResumeMovement()
    {
        agent.isStopped = false;
    }

    public void FindAndMoveToZone()
    {
        currentTargetZone = zoneManager.GetNearestEnemyTargetZone(transform.position);
        if (currentTargetZone == null)
        {
            return;
        }

        agent.SetDestination(currentTargetZone.GetRandomWorldPointInside());
    }

    public void SetChaseDestinationToPlayer()
    {
        agent.SetDestination(playerCameraTransform.position);
    }

    public bool HasReachedZone()
    {
        return agent.hasPath && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance;
    }

    public bool ShouldChooseNewZone()
    {
        if (currentTargetZone == null)
        {
            return true;
        }

        if (!IsPatrolZoneStillValid(currentTargetZone))
        {
            return true;
        }

        return HasReachedZone();
    }

    private bool IsPatrolZoneStillValid(Zone zone)
    {
        return ZoneManager.CanEnemyCapture(zone) || zone.Owner == ZoneManager.ZoneOwner.Contested;
    }
}
