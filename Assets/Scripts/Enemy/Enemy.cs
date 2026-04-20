using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// NavMesh enemy for zone capture mode: picks capturable tiles, chases the player focus (AR camera),
// and uses horizontal distance checks so XR camera offset still feels fair for chase and kick range.
public class Enemy : MonoBehaviour
{
    private const string IsStunnedAnimatorParam = "IsStunned";

    // Last enabled instance wins; time-slow and similar systems grab Enemy.Active without a direct reference.
    public static Enemy Active { get; private set; }
    
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private ZoneManager zoneManager;

    [Header("Ranges")]
    [SerializeField] private float attackRange = 1f;
    [SerializeField] private float attackRangeStop = 2f;
    [SerializeField] private float chaseRange = 5f;

    [Header("Chase handoff")]
    [SerializeField] private float chaseHandoffDelay = 0.28f;

    [Header("Grenade knockback")]
    [SerializeField] private float grenadeKnockbackDistancePerForce = 0.012f;
    [SerializeField] private float grenadeStunDuration = 5f;
    [SerializeField] private float grenadeKnockbackArcDuration = 0.45f;
    [SerializeField] private float grenadeKnockbackArcHeight = 0.85f;

    [SerializeField] private Animator animator;

    private Animator stateMachineHostAnimator;
    // Nearest neutral/player zone we are pathing toward for capture; null when idle or repicking
    private Zone currentTargetZone;
    // Wall-clock time when stun ends; compared in IsGrenadeStunActive.
    private float grenadeStunEndTime;
    private Coroutine grenadeKnockbackRoutine;
    // After stun expires, clear IsStunned on the next LateUpdate so transition exit has a full frame to run.
    private bool grenadeStunAnimatorClearPending;

    public float ChaseRange => chaseRange;
    public float AttackRange => attackRange;
    public float AttackRangeStop => attackRangeStop;
    public NavMeshAgent Agent => agent;
    public Animator Animator => animator;
    public ZoneManager ZoneManager
    {
        get => zoneManager;
        set => zoneManager = value;
    }
    public Zone CurrentTargetZone => currentTargetZone;
    public float ChaseHandoffDelay => chaseHandoffDelay;
    // While true, IsStunned stays set and other animator SMBs stay blocked until the timer ends.
    public bool IsGrenadeStunActive => grenadeStunEndTime > 0f && Time.time < grenadeStunEndTime;
    // Grenade and cleanup always drive the same Animator that owns IsStunned in the active graph.
    private Animator AnimatorForIsStunnedParam =>
        stateMachineHostAnimator != null ? stateMachineHostAnimator : animator;


    // Called from the enemy state machine prefab so IsStunned toggles match the layer with SMB transitions.
    public void SetStateMachineHostAnimator(Animator host)
    {
        if (host != null)
        {
            stateMachineHostAnimator = host;
        }
    }

    private void Awake()
    {
        if (agent == null)
        {
            agent = GetComponent<NavMeshAgent>();
        }
    }

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

        CleanupGrenadeEffects();
    }

    private void LateUpdate()
    {
        // Defer turning off IsStunned until after the animator has processed the end of the stun window.
        if (!grenadeStunAnimatorClearPending || IsGrenadeStunActive)
        {
            return;
        }

        Animator stunAnimator = AnimatorForIsStunnedParam;
        if (stunAnimator == null)
        {
            grenadeStunAnimatorClearPending = false;
            return;
        }

        stunAnimator.SetBool(IsStunnedAnimatorParam, false);
        grenadeStunAnimatorClearPending = false;
    }

    // Called from OnDisable and when tearing down knockback so NavMesh sync and animator bools never leak across disable.
    private void CleanupGrenadeEffects()
    {
        if (grenadeKnockbackRoutine != null)
        {
            StopCoroutine(grenadeKnockbackRoutine);
            grenadeKnockbackRoutine = null;
        }

        grenadeStunEndTime = 0f;
        grenadeStunAnimatorClearPending = false;

        Animator stunAnimator = AnimatorForIsStunnedParam;
        if (stunAnimator != null)
        {
            stunAnimator.SetBool(IsStunnedAnimatorParam, false);
        }

        if (agent != null)
        {
            agent.updatePosition = true;
            agent.updateRotation = true;
        }
    }

    // --- Player focus and horizontal distance (XZ only) ---

    // Matches zone capture: ZoneManager’s AR camera when wired; otherwise the scene main camera.
    private Transform ResolvePlayerFocusTransform()
    {
        if (zoneManager != null && zoneManager.MainCameraTransform != null)
        {
            return zoneManager.MainCameraTransform;
        }

        return Camera.main.transform;
    }

    // Ignores Y so slopes and camera height do not shrink or inflate chase and attack checks.
    private float HorizontalDistanceToPlayer()
    {
        Transform focus = ResolvePlayerFocusTransform();
        if (focus == null)
        {
            return float.PositiveInfinity;
        }

        Vector3 delta = transform.position - focus.position;
        delta.y = 0f;
        return delta.magnitude;
    }

    // --- Zone queries (used by capture state machine and ZoneManager rules) ---

    public bool HasEnemyCaptureTargets()
    {
        if (zoneManager == null)
        {
            return false;
        }

        return zoneManager.GetNearestEnemyTargetZone(transform.position) != null;
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

    /// Player focus (AR camera) is on a tile currently marked
    public bool IsPlayerOnContestedZone()
    {
        if (zoneManager == null)
        {
            return false;
        }

        Transform focus = ResolvePlayerFocusTransform();
        if (focus == null)
        {
            return false;
        }

        return zoneManager.IsContestedZoneAtWorldPosition(focus.position);
    }

    public bool IsZoneContestedWithPlayer()
    {
        Zone playerZone = GetPlayerZone();
        Zone enemyZone = GetZoneUnderFoot();

        // True when both stand in the same cell; ZoneManager treats that as contested and pauses capture meters.
        return playerZone != null
            && enemyZone != null
            && playerZone == enemyZone;
    }

    public bool IsInCapturableZone()
    {
        // Same tile as the player is a contest — fight or chase, not capture.
        if (IsZoneContestedWithPlayer())
        {
            return false;
        }

        Zone zone = GetZoneUnderFoot();

        return zone != null && ZoneManager.CanEnemyCapture(zone);
    }

    // Tile shows Contested but player left: SMB can rotate out without sharing the cell anymore.
    public bool ShouldRotateFromContestedZone()
    {
        Zone enemyZone = GetZoneUnderFoot();
        return enemyZone != null
            && enemyZone.Owner == ZoneManager.ZoneOwner.Contested
            && !IsZoneContestedWithPlayer();
    }

    // Always chase during a shared-cell contest; otherwise chase only inside chaseRange on the horizontal plane.
    public bool ShouldChasePlayer()
    {
        Transform focus = ResolvePlayerFocusTransform();
        if (focus == null)
        {
            return false;
        }

        if (IsZoneContestedWithPlayer())
        {
            return true;
        }

        Vector3 delta = transform.position - focus.position;
        delta.y = 0f;
        return delta.magnitude <= chaseRange;
    }

    // Ghost players cannot be hit by kicks.
    public bool IsPlayerInAttackRange()
    {
        if (HealthSystem.Instance?.IsGhost == true)
        {
            return false;
        }

        Transform focus = ResolvePlayerFocusTransform();
        if (focus == null)
        {
            return false;
        }

        float maxRange = Mathf.Max(attackRange, attackRangeStop);
        if (agent != null)
        {
            maxRange = Mathf.Max(maxRange, agent.stoppingDistance + 0.2f);
        }

        Vector3 delta = transform.position - focus.position;
        delta.y = 0f;
        return delta.magnitude <= maxRange;
    }

    // NavMesh movement stop
    public void StopMovement()
    {
        if (agent == null)
        {
            return;
        }

        agent.isStopped = true;
    }

    // NavMesh movement resume
    public void ResumeMovement()
    {
        if (agent == null || IsGrenadeStunActive)
        {
            return;
        }

        agent.isStopped = false;
    }

    // Hard stop plus ResetPath: avoids sliding toward an old corner after state changes.
    public void StopAndClearNavPath()
    {
        if (agent == null)
        {
            return;
        }

        agent.isStopped = true;
        agent.ResetPath();
    }

    // Instant yaw toward focus; disables agent rotation so root rotation stays locked during the blend.
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
        const float minLookDirectionDistance = 0.01f;
        if (flat.sqrMagnitude < minLookDirectionDistance * minLookDirectionDistance)
        {
            return;
        }

        transform.rotation = Quaternion.LookRotation(flat.normalized, Vector3.up);
    }

    // Pick nearest capturable zone and path to a random point inside; clears path if nothing is left to take.
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

    // Raw world position of the focus; caller should ensure chase state is appropriate.
    public void SetChaseDestinationToPlayer()
    {
        if (agent == null)
        {
            return;
        }

        Transform focus = ResolvePlayerFocusTransform();
        if (focus == null)
        {
            return;
        }

        agent.SetDestination(focus.position);
    }

    // Typical "arrived" check for capture patrol: valid path, not repathing, within stopping distance.
    public bool HasReachedZone()
    {
        if (agent == null)
        {
            return false;
        }

        return agent.hasPath && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance;
    }

    // Repick when we have no target, the tile flipped to non-capturable, or we finished the current path.
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

    // Mirrors ZoneManager.CanEnemyCapture: neutral or player-owned cells only.
    private bool IsPatrolZoneStillValid(Zone zone)
    {
        return ZoneManager.CanEnemyCapture(zone);
    }

    // --- Grenade reaction ---

    // Kinematic rigidbody ignores AddExplosionForce: horizontal push scaled by falloff, NavMesh snap, parabolic arc, then stun window.
    public void ApplyGrenadeKnockback(Vector3 explosionCenter, float blastRadius, float explosionForce)
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
        {
            return;
        }

        const float minHorizontalDistance = 0.02f;

        Vector3 flatOffset = transform.position - explosionCenter;
        flatOffset.y = 0f;

        float horizontalDistance = flatOffset.magnitude;
        if (horizontalDistance < minHorizontalDistance || horizontalDistance > blastRadius)
        {
            return;
        }

        // 1 at the blast center, 0 at blastRadius.
        float distanceFalloff = 1f - (horizontalDistance / blastRadius);
        Vector3 knockbackDirection = flatOffset / horizontalDistance;
        float knockbackLength = explosionForce * distanceFalloff * grenadeKnockbackDistancePerForce;
        Vector3 horizontalKnockback = knockbackDirection * knockbackLength;
        const float minKnockbackDistance = 0.01f;
        if (horizontalKnockback.sqrMagnitude < minKnockbackDistance * minKnockbackDistance)
        {
            return;
        }

        Vector3 unsnappedDestination = transform.position + horizontalKnockback;
        float navMeshSearchRadius = Mathf.Max(horizontalKnockback.magnitude + 1.5f, agent.height);
        if (!NavMesh.SamplePosition(unsnappedDestination, out NavMeshHit navHit, navMeshSearchRadius, NavMesh.AllAreas))
        {
            return;
        }

        float stunDuration = Mathf.Max(0.02f, grenadeStunDuration);
        grenadeStunEndTime = Time.time + stunDuration;
        grenadeStunAnimatorClearPending = true;

        Animator animatorForStun = AnimatorForIsStunnedParam;
        if (animatorForStun != null)
        {
            animatorForStun.SetBool(IsStunnedAnimatorParam, true);
        }

        agent.isStopped = true;
        agent.ResetPath();

        if (grenadeKnockbackRoutine != null)
        {
            StopCoroutine(grenadeKnockbackRoutine);
        }

        grenadeKnockbackRoutine = StartCoroutine(GrenadeKnockbackArcRoutine(navHit.position));
    }

    // Manually move the transform for the arc; agent Warp at the end resyncs internal NavMesh position.
    private IEnumerator GrenadeKnockbackArcRoutine(Vector3 landPosition)
    {
        const float minArcDuration = 0.05f;

        if (agent == null)
        {
            grenadeKnockbackRoutine = null;
            yield break;
        }

        agent.isStopped = true;
        // Prevent the agent from fighting the coroutine-driven root motion for the duration of the arc.
        agent.updatePosition = false;
        agent.updateRotation = false;

        Vector3 arcStart = transform.position;
        float arcDuration = Mathf.Max(minArcDuration, grenadeKnockbackArcDuration);
        float peakHeight = grenadeKnockbackArcHeight;
        float elapsedTime = 0f;

        while (elapsedTime < arcDuration)
        {
            elapsedTime += Time.deltaTime;
            float arcProgress = Mathf.Clamp01(elapsedTime / arcDuration);
            
            // Parabola peak at arcProgress=0.5: 4*t*(1-t) lift in world units.
            float parabolaLift = 4f * peakHeight * arcProgress * (1f - arcProgress);

            Vector3 positionOnArc = Vector3.Lerp(arcStart, landPosition, arcProgress);
            positionOnArc.y = Mathf.Lerp(arcStart.y, landPosition.y, arcProgress) + parabolaLift;
            transform.position = positionOnArc;
            
            yield return null;
        }

        transform.position = landPosition;
        // Warp the agent to the land position (Warp is more accurate than MovePosition)
        agent.Warp(landPosition);
        agent.updatePosition = true;
        agent.updateRotation = true;
        agent.isStopped = true;

        grenadeKnockbackRoutine = null;
    }

    private void OnDrawGizmos()
    {
        // Red: chase radius. Blue / green: inner and outer attack tuning spheres (see IsPlayerInAttackRange).
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, attackRangeStop);
    }
}
