using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    private const string PlayerTag = "Player";

    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private ZoneManager zoneManager;

    [SerializeField] private float attackRange = 1f;
    [SerializeField] private float attackRangeStop = 2f;
    [SerializeField] private float chaseRange = 5f;

    private Zone currentTargetZone;
    private Transform playerPoint;

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

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag(PlayerTag);

        Camera cam = playerObj.GetComponentInChildren<Camera>(true);
        playerPoint = cam != null ? cam.transform : playerObj.transform;
    }

    private float HorizontalDistanceToPlayer()
    {
        if (playerPoint == null)
        {
            return float.PositiveInfinity;
        }

        Vector3 enemyWorldPosition = transform.position;
        Vector3 playerWorldPosition = playerPoint.position;
        float offsetOnXAxis = enemyWorldPosition.x - playerWorldPosition.x;
        float offsetOnZAxis = enemyWorldPosition.z - playerWorldPosition.z;
        return Mathf.Sqrt((offsetOnXAxis * offsetOnXAxis) + (offsetOnZAxis * offsetOnZAxis));
    }

    public Zone GetZoneUnderFoot() => zoneManager.GetZoneAtWorldPosition(transform.position);

    public Zone GetPlayerZone()
    {
        if (playerPoint == null)
        {
            return null;
        }

        return zoneManager.GetZoneAtWorldPosition(playerPoint.position);
    }

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

        return zone != null && ZoneManager.CanEnemyCapture(zone);
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
        if (playerPoint == null)
        {
            return false;
        }

        return HorizontalDistanceToPlayer() <= chaseRange;
    }

    public bool IsPlayerInAttackRange()
    {
        if (playerPoint == null)
        {
            return false;
        }

        return HorizontalDistanceToPlayer() <= attackRange;
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
        if (playerPoint == null)
        {
            return;
        }

        agent.SetDestination(playerPoint.position);
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
        return ZoneManager.CanEnemyCapture(zone);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, attackRangeStop);
    }
}
