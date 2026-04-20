using UnityEngine;

// Animation event receiver: toggles the foot hitbox on/off during kick frames.
public class EnemyAnimEvents : MonoBehaviour
{
    [SerializeField] private Collider footCollider;   // reference to the foot collider

    public void EnableFootDamageEvent()
    {
        if (footCollider == null) return;
        footCollider.enabled = true;   // enable the foot collider
    }

    public void DisableFootDamageEvent()
    {
        if (footCollider == null) return;
        footCollider.enabled = false;  // disable the collider
    }
}
