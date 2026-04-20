using UnityEngine;

public class ZoneShufflePickup : Pickup
{
    protected override PickupKind Kind => PickupKind.ShuffleZones;

    [SerializeField, Min(0f)] private float hudDisplaySeconds = 2f;

    protected override void RegisterPending()
    {
        PickupEffects.Instance.SetPendingZoneShuffle(ZoneManager, hudDisplaySeconds);
    }
}

