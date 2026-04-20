using UnityEngine;

public abstract class Pickup : MonoBehaviour
{
    [SerializeField] private float pickupRadius = 0.35f;

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
        if (effects != null && effects.IsPickupSlotOccupied)
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

        SoundLibrary library = SoundLibrary.Instance;
        if (library != null)
        {
            AudioClip clip = library.DefaultPickupSfx;
            if (clip != null)
            {
                SoundManager.Instance.PlayOneShot(clip);
            }
        }

        if (PickupEffects.Instance != null)
        {
            RegisterPending();
        }

        Destroy(gameObject);
    }

    protected abstract PickupKind Kind { get; }

    protected abstract void RegisterPending();

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(transform.position, 0.06f);
    }
}
