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

    void Start() {
        currentHitPoints = maxHitPoints;
    }

    public void ReactToHit() {
        if (isDead) return;

        currentHitPoints--;

        if (currentHitPoints <= 0) {
            Die();
        }
    }

    void Die() {
        isDead = true;
        ExplodeAndDestroy();
    }

    void ExplodeAndDestroy() {
        Vector3 center = GetComponentInChildren<Renderer>().bounds.center;
        GameObject explosion = Instantiate(explosionPrefab, center, transform.rotation);
        Destroy(explosion, explosionDuration);
        Destroy(gameObject);
    }
}
