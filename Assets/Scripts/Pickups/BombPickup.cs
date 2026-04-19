using UnityEngine;

public class BombPickup : Pickup
{
    protected override PickupKind Kind => PickupKind.GrenadeReady;

    [SerializeField] private GameObject grenadePrefab;
    [SerializeField] private float throwForce = 10f;
    [SerializeField, Range(0f, 60f)] private float upwardAngleDegrees = 20f;

    protected override void RegisterPending()
    {
        PickupEffects.Instance.SetPendingGrenade(grenadePrefab, throwForce, upwardAngleDegrees);
    }
}
