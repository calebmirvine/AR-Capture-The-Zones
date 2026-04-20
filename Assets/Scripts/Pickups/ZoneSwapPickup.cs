using UnityEngine;

public class ZoneSwapPickup : Pickup
{
    protected override PickupKind Kind => PickupKind.SwapZones;

    [SerializeField, Min(0f)] private float hudDisplaySeconds = 2f;
    [SerializeField] private bool swapAllZones = true;
    [SerializeField, Min(0)] private int zonesToSwap = 1;

    protected override void RegisterPending()
    {
        PickupEffects.Instance.SetPendingZoneSwap(
            ZoneManager,
            hudDisplaySeconds,
            swapAllZones,
            zonesToSwap);
    }
}
