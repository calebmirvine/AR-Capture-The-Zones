using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    private const float MinRetargetInterval = 0.1f;
    private const float MinIdleVelocitySquared = 0.01f;
    private const string CapturingParam = "IsCapturing";

    [SerializeField] private ZoneManager zoneManager;

    public ZoneManager ZoneManager
    {
        set { zoneManager = value; }
    }

    [SerializeField] private NavMeshAgent navMeshAgent;

    //Time to wait before retargeting
    [SerializeField] public float IdleTime = 0.5f;
    [SerializeField] private float contestedHoldSecondsWhenWinning = 6f;
    [SerializeField] private float contestedHoldSecondsWhenEven = 4f;
    [SerializeField] private float contestedHoldSecondsWhenLosing = 2f;
    [SerializeField] private int zoneLeadForWinningState = 2;
    [SerializeField] private float contestedRotateCooldownSeconds = 3f;

    public NavMeshAgent Agent
    {
        get { return navMeshAgent; }
    }

    public Animator Animator
    {
        get { return animator; }
    }

    public Zone CurrentTargetZone
    {
        get { return activeTargetZone; }
    }

    private Zone activeTargetZone;
    private float nextRetargetAtTime = 0f;
    private float contestedEnteredAtTime = -1f;
    private float contestedRotateBlockedUntilTime;
    private bool hasCommittedToContestedRotation;

    [SerializeField] private Animator animator;

    [SerializeField] private GameObject projectilePrefab;

    [SerializeField] public Transform projectileSpawnPoint;

    private float projectileForce = 35f;

    [SerializeField] private GameObject impactPrefab;

    private void OnCollisionEnter(Collision other)
    {
        GameObject impact = Instantiate(impactPrefab, transform.position, Quaternion.identity);
        Destroy(impact, 2f); // Destroy the impact effect after 2 seconds
        Destroy(gameObject); //Destroy projectile 
    }

    public void ShootEvent()
    {
        //Spawn projectile at spawn point
        GameObject projectile =
            Instantiate(projectilePrefab, projectileSpawnPoint.position, projectileSpawnPoint.rotation);
        //Move it forward
        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        rb.AddForce(transform.forward * projectileForce, ForceMode.Impulse);
    }

    // State behaviours drive transitions; runtime update only keeps references healthy.
    private void Update()
    {
        if (animator == null) return;
        if (!animator.GetBool(CapturingParam)) return;
        if (!ShouldForceIdle() && IsInCapturableZone()) return;

        animator.SetBool(CapturingParam, false);
    }

    public bool ShouldChooseNewTarget()
    {
        if (ShouldForceIdle())
        {
            return false;
        }

        // While standing in a neutral/player zone, keep the current goal/path
        if (IsInCapturableZone()) return false;

        // Refind when target is missing, no longer capturable, reached, or timer elapsed.
        if (activeTargetZone == null) return true;
        if (!ZoneManager.CanEnemyCapture(activeTargetZone)) return true;
        if (HasReachedDestination()) return true;

        return Time.time >= nextRetargetAtTime;
    }

    public void FindAndMoveToTarget()
    {
        if (zoneManager == null || navMeshAgent == null)
        {
            return;
        }

        navMeshAgent.isStopped = false;
        bool rotatingFromContested = ShouldRotateFromContestedZone();
        if (rotatingFromContested)
        {
            activeTargetZone = GetClosestPlayerOwnedZone();
        }
        else
        {
            activeTargetZone = zoneManager.GetNearestEnemyTargetZone(transform.position);
        }

        if (activeTargetZone == null && rotatingFromContested)
        {
            // Safety fallback if no player-owned zone exists when the timer elapses.
            activeTargetZone = zoneManager.GetNearestEnemyTargetZone(transform.position);
        }

        if (activeTargetZone == null)
        {
            return;
        }

        Vector3 targetPosition = activeTargetZone.transform.position;
        navMeshAgent.SetDestination(targetPosition);

        if (rotatingFromContested)
        {
            CommitContestedRotation();
        }

        nextRetargetAtTime = Time.time + Mathf.Max(MinRetargetInterval, IdleTime);
    }

    public bool IsInCapturableZone()
    {
        return ZoneManager.CanEnemyCapture(zoneManager.GetZoneAtWorldPosition(transform.position));
    }

    public bool IsInContestedZone()
    {
        if (zoneManager == null)
        {
            return false;
        }

        Zone currentZone = zoneManager.GetZoneAtWorldPosition(transform.position);
        return currentZone != null && currentZone.Owner == ZoneManager.ZoneOwner.Contested;
    }

    public bool AreAllZonesEnemyControlled()
    {
        if (zoneManager == null || zoneManager.zones.Count == 0)
        {
            return false;
        }

        foreach (Zone zone in zoneManager.zones)
        {
            if (zone == null)
            {
                continue;
            }

            if (zone.Owner != ZoneManager.ZoneOwner.Enemy)
            {
                return false;
            }
        }

        return true;
    }

    public bool ShouldForceIdle()
    {
        if (AreAllZonesEnemyControlled())
        {
            return true;
        }

        if (!IsInContestedZone())
        {
            contestedEnteredAtTime = -1f;
            hasCommittedToContestedRotation = false;
            return false;
        }

        // Once we've committed to leave this contested zone, allow movement out.
        if (hasCommittedToContestedRotation)
        {
            return false;
        }

        if (contestedEnteredAtTime < 0f)
        {
            contestedEnteredAtTime = Time.time;
        }

        float holdDuration = GetContestedHoldSeconds();
        return (Time.time - contestedEnteredAtTime) < holdDuration;
    }

    public bool ShouldRotateFromContestedZone()
    {
        if (!IsInContestedZone())
        {
            return false;
        }

        if (hasCommittedToContestedRotation)
        {
            return false;
        }

        if (Time.time < contestedRotateBlockedUntilTime)
        {
            return false;
        }

        if (contestedEnteredAtTime < 0f)
        {
            contestedEnteredAtTime = Time.time;
        }

        float holdDuration = GetContestedHoldSeconds();
        return (Time.time - contestedEnteredAtTime) >= holdDuration;
    }

    public void HoldPositionInCurrentZone()
    {
        if (navMeshAgent == null)
        {
            return;
        }

        // Keep enemy in place while contested instead of continuing an old path out.
        navMeshAgent.isStopped = true;
        navMeshAgent.ResetPath();
    }

    public void ResumeMovement()
    {
        if (navMeshAgent == null)
        {
            return;
        }

        navMeshAgent.isStopped = false;
    }

    public bool HasReachedDestination()
    {
        if (navMeshAgent.pathPending) return false;
        if (navMeshAgent.remainingDistance > navMeshAgent.stoppingDistance) return false;
        return !navMeshAgent.hasPath || navMeshAgent.velocity.sqrMagnitude < MinIdleVelocitySquared;
    }

    private void CommitContestedRotation()
    {
        hasCommittedToContestedRotation = true;
        contestedRotateBlockedUntilTime = Time.time + Mathf.Max(0f, contestedRotateCooldownSeconds);
    }

    private float GetContestedHoldSeconds()
    {
        if (zoneManager == null || zoneManager.zones.Count == 0)
        {
            return Mathf.Max(0f, contestedHoldSecondsWhenEven);
        }

        int enemyZones = 0;
        int playerZones = 0;
        foreach (Zone zone in zoneManager.zones)
        {
            if (zone == null)
            {
                continue;
            }

            if (zone.Owner == ZoneManager.ZoneOwner.Enemy)
            {
                enemyZones++;
                continue;
            }

            if (zone.Owner == ZoneManager.ZoneOwner.Player)
            {
                playerZones++;
            }
        }

        int leadDelta = enemyZones - playerZones;
        int winningLeadThreshold = Mathf.Max(1, zoneLeadForWinningState);
        if (leadDelta >= winningLeadThreshold)
        {
            return Mathf.Max(0f, contestedHoldSecondsWhenWinning);
        }

        if (leadDelta <= -winningLeadThreshold)
        {
            return Mathf.Max(0f, contestedHoldSecondsWhenLosing);
        }

        return Mathf.Max(0f, contestedHoldSecondsWhenEven);
    }

    private Zone GetClosestPlayerOwnedZone()
    {
        if (zoneManager == null)
        {
            return null;
        }

        Zone nearestZone = null;
        float nearestDistance = float.MaxValue;
        foreach (Zone zone in zoneManager.zones)
        {
            if (zone == null || zone.Owner != ZoneManager.ZoneOwner.Player)
            {
                continue;
            }

            float distance = Vector3.Distance(zone.transform.position, transform.position);
            if (distance >= nearestDistance)
            {
                continue;
            }

            nearestDistance = distance;
            nearestZone = zone;
        }

        return nearestZone;
    }
}
