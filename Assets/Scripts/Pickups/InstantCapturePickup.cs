using UnityEngine;

public class InstantCapturePickup : Pickup
{
    [SerializeField] private float durationSeconds = 5f;

    protected override void RegisterPending()
    {
        PickupEffects.Instance.SetPendingInstantCapture(durationSeconds);
    }
}
