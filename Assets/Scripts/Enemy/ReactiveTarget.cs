using UnityEngine;

public class ReactiveTarget : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHitPoints = 3;
    private int currentHitPoints;
    private bool isDead = false;

    [Header("Death")]
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private float explosionDuration = 1f;

    // Reset hit points from max (Inspector).
    void Start() {
        currentHitPoints = maxHitPoints;
    }

    // Apply one hit; triggers death when HP reaches zero.
    public void ReactToHit() {
        if (isDead) return;

        currentHitPoints--;

        if (currentHitPoints <= 0) {
            Die();
        }
    }

    // Mark dead and run explosion teardown.
    void Die() {
        isDead = true;
        ExplodeAndDestroy();
    }

    // Spawn explosion at mesh center (or root position) and remove this enemy.
    void ExplodeAndDestroy() {
        Vector3 center = transform.position;
        Renderer rend = GetComponentInChildren<Renderer>();
        if (rend != null) {
            center = rend.bounds.center;
        }

        if (explosionPrefab != null) {
            GameObject explosion = Instantiate(explosionPrefab, center, transform.rotation);
            Destroy(explosion, explosionDuration);
        }

        Destroy(gameObject);
    }
}
