using UnityEngine;

public class ZoneSwapPickup : Pickup
{
    protected override PickupKind Kind => PickupKind.SwapZones;

    [SerializeField, Min(0f)] private float hudDisplaySeconds = 2f;

    protected override void RegisterPending()
    {
        PickupEffects.Instance.SetPendingZoneSwap(ZoneManager, hudDisplaySeconds);
    }
}
