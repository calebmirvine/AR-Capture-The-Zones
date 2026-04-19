using UnityEngine;

public class TimeSlowPickup : Pickup
{
    protected override PickupKind Kind => PickupKind.TimeSlow;

    [SerializeField] private float durationSeconds = 5f;
    [SerializeField, Range(0.05f, 1f)] private float timeScale = 0.3f;

    protected override void RegisterPending()
    {
        PickupEffects.Instance.SetPendingTimeSlow(durationSeconds, timeScale);
    }
}
