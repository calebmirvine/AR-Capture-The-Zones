using UnityEngine;

public class ZoneSwapPickup : Pickup
{
    protected override void ApplyEffect()
    {
        if (ZoneManager == null)
        {
            Debug.LogWarning("ZoneSwapPickup collected but ZoneManager is null.");
            return;
        }

        ZoneManager.SwapPlayerAndEnemyZones();
    }
}
