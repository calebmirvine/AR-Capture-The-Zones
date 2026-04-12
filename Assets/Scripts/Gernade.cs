using UnityEngine;

public class Gernade : MonoBehaviour

{
    [SerializeField] public float delay = 3f;

    [SerializeField] public float explosionRadius = 5f;

    [SerializeField] public float explosionForce = 700f;

    [SerializeField] public GameObject explosionPrefab;

    float countdown;
    bool exploded = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        countdown = delay;
    }

    // Update is called once per frame
    void Update()
    {
        countdown -= Time.deltaTime;
        if (countdown <= 0f && !exploded)
        {
            Explode();
            exploded = true;
        }
    }


    public void Explode()
    {
        //Explosion Effect
        GameObject explosion = Instantiate(explosionPrefab, transform.position, transform.rotation);

        //Get nearby Enemies
        Collider[] nearbyEnemies = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider enemy in nearbyEnemies)
        {
            if (enemy.CompareTag("Enemy"))
            {
                Rigidbody rb = enemy.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
                }
            }
        }

        Destroy(explosion, 1f);
        Destroy(gameObject);
    }


    public void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
