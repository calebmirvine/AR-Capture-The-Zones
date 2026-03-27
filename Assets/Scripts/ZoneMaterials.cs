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

    public Material GetFloor(ZoneOwner owner) {
        switch (owner) {
            case ZoneOwner.Player:    return floorPlayer;
            case ZoneOwner.Enemy:     return floorEnemy;
            case ZoneOwner.Contested: return floorContested;
            default:                  return floorNeutral;
        }
    }

    public Material GetFlag(ZoneOwner owner) {
        switch (owner) {
            case ZoneOwner.Player:    return flagPlayer;
            case ZoneOwner.Enemy:     return flagEnemy;
            case ZoneOwner.Contested: return flagContested;
            default:                  return flagNeutral;
        }
    }
}
