using UnityEngine;

public abstract class Pickup : MonoBehaviour
{
    private const int GizmoCircleSegments = 48;

    [SerializeField] private float pickupRadius = 0.35f;
    [SerializeField] private AudioClip pickupSound;

    private ZoneManager zoneManager;
    private bool hasBeenCollected;

    public void Init(ZoneManager manager)
    {
        zoneManager = manager;
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

        if (zoneManager == null)
        {
            return;
        }

        HealthSystem health = HealthSystem.Instance;
        if (health != null && health.IsGhost)
        {
            return;
        }

        PickupEffects effects = PickupEffects.Instance;
        if (effects != null && effects.HasPendingPowerup)
        {
            return;
        }

        Vector3 playerPosition = zoneManager.MainCameraTransform.position;
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

        if (pickupSound != null)
        {
            SoundManager.Instance.PlaySfx(pickupSound);
        }

        if (PickupEffects.Instance != null)
        {
            RegisterPending();
        }

        Destroy(gameObject);
    }

    protected abstract void RegisterPending();

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
