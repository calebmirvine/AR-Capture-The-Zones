using UnityEngine;
using UnityEngine.InputSystem;

public class Shoot : MonoBehaviour {

    [SerializeField] private GameObject arCamera;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 10f;
    [SerializeField] private float projectileLifetime = 3f;

    // Fire on first mobile touch or editor mouse click.
    void Update() {
        bool tapBegan = Touchscreen.current != null &&
                        Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
        bool clickBegan = Mouse.current != null &&
                          Mouse.current.leftButton.wasPressedThisFrame;

        if (tapBegan || clickBegan) {
            if (arCamera == null || projectilePrefab == null) return;

            GameObject projectile = Instantiate(projectilePrefab, arCamera.transform.position, arCamera.transform.rotation);
            Rigidbody rb = projectile.GetComponent<Rigidbody>();
            if (rb != null) {
                rb.AddForce(arCamera.transform.forward * projectileSpeed, ForceMode.Impulse);
            }
            Destroy(projectile, projectileLifetime);
        }
    }

}
