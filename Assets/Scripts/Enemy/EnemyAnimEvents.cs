using UnityEngine;

// """
// Create an EnemyAnimEvents.cs script for detecting animation events. This allows us to
// turn the foot collider on and off. It needs to be attached to the object with the Animator
// component, so attach it to the Enemy’s active model (YBot). In the script, add a private
// Collider footCollider, serialize it, and then drag the Enemy’s RightFoot object into the
// Collider field (in the Inspector). Write a public EnableFootDamageEvent() which sets
// footCollider.enabled = true. Write a matching DisableFootDamageEvent() method to
// disable the collider.
// """
public class EnemyAnimEvents : MonoBehaviour
{
    [SerializeField] private Collider footCollider;   // reference to the foot collider

    public void EnableFootDamageEvent() {
        // Debug.Log("Enabling foot damage event!");   // log message for debugging
        footCollider.enabled = true;   // enable the foot collider
    }
    public void DisableFootDamageEvent() {
        // Debug.Log("Disable foot damage event!");   // log message for debugging
        footCollider.enabled = false;  // disable the foot collider
    }
}
