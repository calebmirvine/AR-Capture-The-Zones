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

    public NavMeshAgent Agent
    {
        get { return navMeshAgent; }
    }

    public Zone CurrentTargetZone
    {
        get { return activeTargetZone; }
    }

    private Zone activeTargetZone;
    private float nextRetargetAtTime = 0f;

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
        if (IsInCapturableZone()) return;

        animator.SetBool(CapturingParam, false);
    }

    public bool ShouldChooseNewTarget()
    {
        if (IsInContestedZone())
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
        activeTargetZone = zoneManager.GetNearestEnemyTargetZone(transform.position);
        if (activeTargetZone == null)
        {
            return;
        }

        Vector3 targetPosition = activeTargetZone.transform.position;
        navMeshAgent.SetDestination(targetPosition);
        nextRetargetAtTime = Time.time + Mathf.Max(MinRetargetInterval, IdleTime);
    }

    public bool IsInCapturableZone()
    {
        return ZoneManager.CanEnemyCapture(zoneManager.GetZoneAtWorldPosition(transform.position));
    }

    public bool IsInContestedZone()
    {
        Zone currentZone = zoneManager.GetZoneAtWorldPosition(transform.position);
        return currentZone != null && currentZone.Owner == ZoneManager.ZoneOwner.Contested;
    }

    public void HoldPositionInCurrentZone()
    {
        // Keep enemy in place while contested instead of continuing an old path out.
        navMeshAgent.isStopped = true;
        navMeshAgent.ResetPath();
    }

    public void ResumeMovement()
    {
        navMeshAgent.isStopped = false;
    }

    public bool HasReachedDestination()
    {
        if (navMeshAgent.pathPending) return false;
        if (navMeshAgent.remainingDistance > navMeshAgent.stoppingDistance) return false;
        return !navMeshAgent.hasPath || navMeshAgent.velocity.sqrMagnitude < MinIdleVelocitySquared;
    }
}