using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

public class Enemy : MonoBehaviour
{
    private const string PlayerTag = "Player";

    /// <summary>Last enabled enemy in play mode (this project spawns one at a time). Used for time-slow and avoids fragile tag lookups.</summary>
    public static Enemy Active { get; private set; }

    [FormerlySerializedAs("navMeshAgent")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private ZoneManager zoneManager;

    [SerializeField] private float attackRange = 1f;
    [SerializeField] private float attackRangeStop = 2f;
    [SerializeField] private float chaseRange = 5f;
    [Tooltip("Extra horizontal reach so attack bool does not flicker at the edge of range.")]
    [SerializeField] private float attackEngagePadding = 0.3f;

    [Header("Grenade knockback")]
    [Tooltip("NavMesh enemies use kinematic rigidbodies; scale maps grenade explosion force to horizontal shove (world units).")]
    [SerializeField] private float grenadeKnockbackDistancePerForce = 0.012f;

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

    private void OnEnable()
    {
        Active = this;
    }

    private void OnDisable()
    {
        if (Active == this)
        {
            Active = null;
        }
    }

    private void Awake()
    {
        if (agent == null)
        {
            agent = GetComponent<NavMeshAgent>();
        }

        // NavMesh-driven enemies should not be shoved around by the player’s CharacterController / physics.
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    /// <summary>True if there is at least one neutral or player-owned zone the enemy can head toward.</summary>
    public bool HasEnemyCaptureTargets()
    {
        if (zoneManager == null)
        {
            return false;
        }

        return zoneManager.GetNearestEnemyTargetZone(transform.position) != null;
    }

    private void Start()
    {
        if (zoneManager != null && zoneManager.MainCameraTransform != null)
        {
            playerPoint = zoneManager.MainCameraTransform;
        }
        else
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag(PlayerTag);
            if (playerObj != null)
            {
                Camera cam = playerObj.GetComponentInChildren<Camera>(true);
                playerPoint = cam != null ? cam.transform : playerObj.transform;
            }
            else if (Camera.main != null)
            {
                playerPoint = Camera.main.transform;
            }
        }
    }

    /// <summary>
    /// Same transform <see cref="ZoneManager"/> uses for capture/contest (AR main camera when assigned).
    /// </summary>
    private Transform ResolvePlayerFocusTransform()
    {
        if (zoneManager != null && zoneManager.MainCameraTransform != null)
        {
            return zoneManager.MainCameraTransform;
        }

        return playerPoint;
    }

    private float HorizontalDistanceToPlayer()
    {
        Transform focus = ResolvePlayerFocusTransform();
        if (focus == null)
        {
            return float.PositiveInfinity;
        }

        Vector3 enemyWorldPosition = transform.position;
        Vector3 playerWorldPosition = focus.position;
        float offsetOnXAxis = enemyWorldPosition.x - playerWorldPosition.x;
        float offsetOnZAxis = enemyWorldPosition.z - playerWorldPosition.z;
        return Mathf.Sqrt((offsetOnXAxis * offsetOnXAxis) + (offsetOnZAxis * offsetOnZAxis));
    }

    public Zone GetZoneUnderFoot()
    {
        if (zoneManager == null)
        {
            return null;
        }

        return zoneManager.GetZoneAtWorldPosition(transform.position);
    }

    public Zone GetPlayerZone()
    {
        if (zoneManager == null)
        {
            return null;
        }

        Transform focus = ResolvePlayerFocusTransform();
        if (focus == null)
        {
            return null;
        }

        return zoneManager.GetZoneAtWorldPosition(focus.position);
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
        // Same tile as the player is a contest — fight or chase, not capture dance.
        if (IsZoneContestedWithPlayer())
        {
            return false;
        }

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
        if (ResolvePlayerFocusTransform() == null)
        {
            return false;
        }

        if (IsZoneContestedWithPlayer())
        {
            return true;
        }

        return HorizontalDistanceToPlayer() <= chaseRange;
    }

    public bool IsPlayerInAttackRange()
    {
        if (HealthSystem.Instance != null && HealthSystem.Instance.IsGhost)
        {
            return false;
        }

        if (ResolvePlayerFocusTransform() == null)
        {
            return false;
        }

        // XR / NavMesh: camera horizontal offset often stays outside a tight attackRange; attackRangeStop
        // is the practical “start stomp” radius. Also respect agent stopping distance so we don’t require
        // overlapping pivots.
        float maxRange = Mathf.Max(attackRange, attackRangeStop) + attackEngagePadding;
        if (agent != null)
        {
            maxRange = Mathf.Max(maxRange, agent.stoppingDistance + 0.2f);
        }

        return HorizontalDistanceToPlayer() <= maxRange;
    }

    public void StopMovement()
    {
        agent.isStopped = true;
    }

    public void ResumeMovement()
    {
        agent.isStopped = false;
    }

    /// <summary>Stops the agent and drops the current path so it does not keep sliding from a stale tangent.</summary>
    public void StopAndClearNavPath()
    {
        if (agent == null)
        {
            return;
        }

        agent.isStopped = true;
        agent.ResetPath();
    }

    /// <summary>
    /// Kinematic NavMesh agents ignore <see cref="Rigidbody.AddExplosionForce"/>; shove using the navmesh instead.
    /// </summary>
    public void ApplyGrenadeKnockback(Vector3 explosionCenter, float radius, float force)
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
        {
            return;
        }

        Vector3 delta = transform.position - explosionCenter;
        delta.y = 0f;
        float dist = delta.magnitude;
        if (dist < 0.02f || dist > radius)
        {
            return;
        }

        float falloff = 1f - (dist / radius);
        Vector3 push = delta.normalized * (force * falloff * grenadeKnockbackDistancePerForce);
        if (push.sqrMagnitude < 1e-8f)
        {
            return;
        }

        Vector3 candidate = transform.position + push;
        float sampleMaxDistance = Mathf.Max(push.magnitude + 1.5f, agent.height);
        if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, sampleMaxDistance, NavMesh.AllAreas))
        {
            return;
        }

        agent.Warp(hit.position);
    }

    /// <summary>Instant horizontal face toward the player / camera focus used for chase and contest.</summary>
    public void FaceTowardPlayer()
    {
        if (agent != null)
        {
            agent.updateRotation = false;
        }

        Transform focus = ResolvePlayerFocusTransform();
        if (focus == null)
        {
            return;
        }

        Vector3 flat = focus.position - transform.position;
        flat.y = 0f;
        if (flat.sqrMagnitude < 1e-6f)
        {
            return;
        }

        transform.rotation = Quaternion.LookRotation(flat.normalized, Vector3.up);
    }

    public void FindAndMoveToZone()
    {
        if (zoneManager == null || agent == null)
        {
            return;
        }

        currentTargetZone = zoneManager.GetNearestEnemyTargetZone(transform.position);
        if (currentTargetZone == null)
        {
            StopAndClearNavPath();
            return;
        }

        agent.SetDestination(currentTargetZone.GetRandomWorldPointInside());
    }

    public void SetChaseDestinationToPlayer()
    {
        Transform focus = ResolvePlayerFocusTransform();
        if (focus == null)
        {
            return;
        }

        agent.SetDestination(focus.position);
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
