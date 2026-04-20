using UnityEngine;

public class EnemyAnimEvents : MonoBehaviour
{
    [SerializeField] private Collider footCollider;

    private EnemyFootDamage footDamage;

    private void Awake()
    {
        if (footCollider != null)
        {
            footDamage = footCollider.GetComponent<EnemyFootDamage>();
        }
    }

    public void EnableFootDamageEvent()
    {
        if (footCollider == null)
        {
            return;
        }

        // New kick window only when the hitbox was off. If Enable runs twice while the collider stays on, we do not open a second damage window.
        bool footHitboxWasOff = !footCollider.enabled;
        if (footHitboxWasOff)
        {
            footDamage?.BeginKickDamageWindow();
        }

        footCollider.enabled = true;
    }

    public void DisableFootDamageEvent()
    {
        if (footCollider == null)
        {
            return;
        }

        footCollider.enabled = false;
    }
}
