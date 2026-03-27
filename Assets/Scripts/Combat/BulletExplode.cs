using UnityEngine;

public class BulletExplode : MonoBehaviour
{
    [SerializeField] private float bulletLifetime = 3f;

    void Start() {
        Destroy(gameObject, bulletLifetime);
    }

    void OnCollisionEnter(Collision collision) {
        collision.gameObject.GetComponent<ReactiveTarget>().ReactToHit();
        Destroy(gameObject);
    }
}
