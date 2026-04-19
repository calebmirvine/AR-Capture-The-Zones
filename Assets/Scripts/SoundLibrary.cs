using UnityEngine;

public class SoundLibrary : MonoBehaviour
{
    public static SoundLibrary Instance;

    //SFX
    [SerializeField] private AudioClip menuNavSfx;
    [SerializeField] private AudioClip victorySfx;
    [SerializeField] private AudioClip playerZoneCaptureSfx;
    [SerializeField] private AudioClip enemyZoneCaptureSfx;

    [Header("Match / player")]
    [SerializeField] private AudioClip playerDeadSfx;
    [SerializeField] private AudioClip enemyWinSfx;

    [Header("Pickup — world collect")]
    [SerializeField] private AudioClip defaultPickupSfx;

    [Header("Pickup — powerup activation (HUD button)")]
    [SerializeField] private AudioClip instantCapturePickupSfx;
    [SerializeField] private AudioClip grenadeReadyPickupSfx;
    [SerializeField] private AudioClip timeSlowPickupSfx;
    [SerializeField] private AudioClip swapZonesPickupSfx;

    [Header("Grenade explosion")]
    [SerializeField] private AudioClip grenadeExplosionSfx;

    //Music
    [Header("Music")]
    [SerializeField] private AudioClip scanningMusic;
    [SerializeField] private AudioClip gameMusic;

    public AudioClip MenuNavSfx => menuNavSfx;
    public AudioClip VictorySfx => victorySfx;
    public AudioClip PlayerZoneCaptureSfx => playerZoneCaptureSfx;
    public AudioClip EnemyZoneCaptureSfx => enemyZoneCaptureSfx;
    public AudioClip PlayerDeadSfx => playerDeadSfx;
    public AudioClip EnemyWinSfx => enemyWinSfx;
    public AudioClip ScanningMusic => scanningMusic;
    public AudioClip GameMusic => gameMusic;
    public AudioClip GrenadeExplosionSfx => grenadeExplosionSfx;
    public AudioClip DefaultPickupSfx => defaultPickupSfx;

    public AudioClip GetPickupActivationSfx(PickupKind kind)
    {
        switch (kind)
        {
            case PickupKind.InstantCapture:
                return instantCapturePickupSfx;
            case PickupKind.GrenadeReady:
                return grenadeReadyPickupSfx;
            case PickupKind.TimeSlow:
                return timeSlowPickupSfx;
            case PickupKind.SwapZones:
                return swapZonesPickupSfx;
            default:
                return null;
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);
        }
    }
 
}
