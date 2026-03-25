// Who owns a zone tile (drives materials and capture).
public enum ZoneOwner
{
    Neutral,    // Never claimed
    Contested,  // Being fought over
    Player,     // Your team
    Enemy       // Opponent (reserved for AI)
}
