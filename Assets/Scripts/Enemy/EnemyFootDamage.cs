using UnityEngine;

public class EnemyFootDamage : MonoBehaviour
{
    private const string PlayerTag = "Player";

    private int damage = 1;

    private void OnTriggerEnter(Collider other)
    {
        if (!ColliderBelongsToPlayer(other))
        {
            return;
        }

        HealthSystem healthSystem = other.GetComponentInParent<HealthSystem>();
        if (healthSystem == null)
        {
            healthSystem = other.GetComponent<HealthSystem>();
        }

        if (healthSystem == null)
        {
            return;
        }

        healthSystem.TakeDamage(damage);
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
