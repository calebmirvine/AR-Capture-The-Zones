using UnityEngine;

// Built at runtime from ZoneManager’s Inspector fields (nested struct alone is unreliable in Inspector).
public struct ZoneMaterials
{
    public Material floorPlayer;
    public Material floorEnemy;
    public Material floorNeutral;
    public Material floorContested;

    public Material flagPlayer;
    public Material flagEnemy;
    public Material flagNeutral;
    public Material flagContested;

    public static ZoneMaterials FromInspector(
        Material floorPlayer,
        Material floorEnemy,
        Material floorNeutral,
        Material floorContested,
        Material flagPlayer,
        Material flagEnemy,
        Material flagNeutral,
        Material flagContested)
    {
        return new ZoneMaterials {
            floorPlayer = floorPlayer,
            floorEnemy = floorEnemy,
            floorNeutral = floorNeutral,
            floorContested = floorContested,
            flagPlayer = flagPlayer,
            flagEnemy = flagEnemy,
            flagNeutral = flagNeutral,
            flagContested = flagContested
        };
    }

    // Floor tint material for the given zone owner.
    public Material GetFloor(ZoneOwner owner) {
        switch (owner) {
            case ZoneOwner.Player:    return floorPlayer;
            case ZoneOwner.Enemy:     return floorEnemy;
            case ZoneOwner.Contested: return floorContested;
            default:                  return floorNeutral;
        }
    }

    // Flag material for the given zone owner.
    public Material GetFlag(ZoneOwner owner) {
        switch (owner) {
            case ZoneOwner.Player:    return flagPlayer;
            case ZoneOwner.Enemy:     return flagEnemy;
            case ZoneOwner.Contested: return flagContested;
            default:                  return flagNeutral;
        }
    }
}
