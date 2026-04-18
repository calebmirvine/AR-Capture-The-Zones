using UnityEngine;

public class BombPickup : Pickup
{
    [SerializeField] private GameObject grenadePrefab;
    [SerializeField] private float throwForce = 10f;
    [SerializeField, Range(0f, 60f)] private float upwardAngleDegrees = 20f;

    protected override void ApplyEffect()
    {
        if (grenadePrefab == null)
        {
            Debug.LogWarning("BombPickup collected but grenadePrefab is not assigned.");
            return;
        }

        if (PickupEffects.Instance == null)
        {
            Debug.LogWarning("BombPickup collected but PickupEffects.Instance is null.");
            return;
        }

        PickupEffects.Instance.StoreGrenade(grenadePrefab, throwForce, upwardAngleDegrees);
    }
}
