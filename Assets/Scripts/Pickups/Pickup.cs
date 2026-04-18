using UnityEngine;

public abstract class Pickup : MonoBehaviour
{
    private const int GizmoCircleSegments = 48;

    [SerializeField] private float pickupRadius = 0.35f;
    [SerializeField] private AudioClip pickupSound;

    private ZoneManager zoneManager;
    private Transform playerTransform;
    private bool hasBeenCollected;

    public void Init(ZoneManager manager)
    {
        zoneManager = manager;
        if (zoneManager != null && zoneManager.MainCameraTransform != null)
        {
            playerTransform = zoneManager.MainCameraTransform;
            return;
        }

        if (Camera.main != null)
        {
            playerTransform = Camera.main.transform;
        }
    }

    protected ZoneManager ZoneManager
    {
        get { return zoneManager; }
    }

    private void Update()
    {
        if (hasBeenCollected)
        {
            return;
        }

        if (playerTransform == null)
        {
            // Late binding in case the camera wasn't ready during Init.
            if (Camera.main != null)
            {
                playerTransform = Camera.main.transform;
            }
            else
            {
                return;
            }
        }

        Vector3 playerPosition = playerTransform.position;
        Vector3 pickupPosition = transform.position;
        float deltaX = playerPosition.x - pickupPosition.x;
        float deltaZ = playerPosition.z - pickupPosition.z;
        float distanceSquared = (deltaX * deltaX) + (deltaZ * deltaZ);
        float radiusSquared = pickupRadius * pickupRadius;

        if (distanceSquared <= radiusSquared)
        {
            Collect();
        }
    }

    private void Collect()
    {
        hasBeenCollected = true;

        if (pickupSound != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySfx(pickupSound);
        }

        ApplyEffect();
        Destroy(gameObject);
    }

    protected abstract void ApplyEffect();

    // Editor-only visualization of the XZ pickup radius so designers can tune it in the Scene view.
    private void OnDrawGizmos()
    {
        if (pickupRadius <= 0f)
        {
            return;
        }

        Gizmos.color = Color.green;

        Vector3 center = transform.position;
        Vector3 previousPoint = center + new Vector3(pickupRadius, 0f, 0f);
        for (int segmentIndex = 1; segmentIndex <= GizmoCircleSegments; segmentIndex++)
        {
            float angle = segmentIndex * (Mathf.PI * 2f / GizmoCircleSegments);
            Vector3 nextPoint = center + new Vector3(Mathf.Cos(angle) * pickupRadius, 0f, Mathf.Sin(angle) * pickupRadius);
            Gizmos.DrawLine(previousPoint, nextPoint);
            previousPoint = nextPoint;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (pickupRadius <= 0f)
        {
            return;
        }

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}
