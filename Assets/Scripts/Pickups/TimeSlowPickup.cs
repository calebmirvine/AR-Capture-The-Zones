using UnityEngine;

public class TimeSlowPickup : Pickup
{
    [SerializeField] private float durationSeconds = 5f;
    [SerializeField, Range(0.05f, 1f)] private float timeScale = 0.3f;

    protected override void ApplyEffect()
    {
        if (PickupEffects.Instance == null)
        {
            Debug.LogWarning("TimeSlowPickup collected but PickupEffects.Instance is null.");
            return;
        }

        PickupEffects.Instance.ActivateTimeSlow(durationSeconds, timeScale);
    }
}
