using UnityEngine;

public class InstantCapturePickup : Pickup
{
    [SerializeField] private float durationSeconds = 5f;

    protected override void ApplyEffect()
    {
        if (PickupEffects.Instance == null)
        {
            Debug.LogWarning("InstantCapturePickup collected but PickupEffects.Instance is null.");
            return;
        }

        PickupEffects.Instance.ActivateInstantPlayerCapture(durationSeconds);
    }
}
