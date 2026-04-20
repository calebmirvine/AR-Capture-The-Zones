using UnityEngine;

public class EnemyFootDamage : MonoBehaviour
{
    private const string PlayerTag = "Player";
    [SerializeField] private int damage = 1;
    [Tooltip("Physics check at the kick frame when animation enables the foot (XR-friendly).")]
    [SerializeField] private float overlapRadius = 0.55f;

    // One hit per kick: EnemyAnimEvents calls BeginKickDamageWindow when the foot hitbox turns on from off.
    // Both start at 0 so no damage runs until the first Begin increments the window id.
    private int kickDamageWindowId;
    private int kickWindowIdAtLastPlayerHit;

    /// <summary>
    /// Opens a new kick damage window (at most one player hit until the next kick).
    /// </summary>
    public void BeginKickDamageWindow()
    {
        kickDamageWindowId++;
    }

    public void ApplyKickOverlapProbe(Vector3 worldCenter)
    {
        Collider[] cols = Physics.OverlapSphere(
            worldCenter,
            overlapRadius,
            layerMask: ~0,
            queryTriggerInteraction: QueryTriggerInteraction.Collide);

        for (int i = 0; i < cols.Length; i++)
        {
            if (TryDamageFromCollider(cols[i]))
            {
                return;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryDamageFromCollider(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryDamageFromCollider(other);
    }

    private bool TryDamageFromCollider(Collider other)
    {
        if (kickWindowIdAtLastPlayerHit == kickDamageWindowId)
        {
            return false;
        }

        if (!ColliderBelongsToPlayer(other))
        {
            return false;
        }

        HealthSystem healthSystem = other.GetComponentInParent<HealthSystem>();
        if (healthSystem == null)
        {
            return false;
        }

        healthSystem.TakeDamage(damage);
        kickWindowIdAtLastPlayerHit = kickDamageWindowId;
        return true;
    }

    private static bool ColliderBelongsToPlayer(Collider other)
    {
        Transform t = other.transform;
        while (t != null)
        {
            if (t.CompareTag(PlayerTag))
            {
                return true;
            }

            t = t.parent;
        }

        return false;
    }
}
