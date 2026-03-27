using UnityEngine;
using UnityEngine.InputSystem;


public class Shoot : MonoBehaviour {

    [SerializeField] private GameObject arCamera;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 10f;

    void Update() {
        bool tapBegan = Touchscreen.current != null &&
                        Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
        bool clickBegan = Mouse.current != null &&
                          Mouse.current.leftButton.wasPressedThisFrame;

        if (tapBegan || clickBegan) {
            GameObject projectile = Instantiate(projectilePrefab, arCamera.transform.position, arCamera.transform.rotation);
            projectile.GetComponent<Rigidbody>().AddForce(arCamera.transform.forward * projectileSpeed, ForceMode.Impulse);
        }
    }

}

