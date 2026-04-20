using System.Collections.Generic;
using UnityEngine;

public class SoundLibrary : MonoBehaviour
{
    public static SoundLibrary Instance;

    [Header("Game SFX")]
    [SerializeField] private AudioClip menuNavSfx;
    [SerializeField] private AudioClip victorySfx;
    [SerializeField] private AudioClip playerZoneCaptureSfx;
    [SerializeField] private AudioClip enemyZoneCaptureSfx;

    [Header("Match / player")]
    [SerializeField] private AudioClip playerHurtSfx;
    [SerializeField] private AudioClip playerDeadSfx;
    [SerializeField] private AudioClip enemyWinSfx;

    [Header("Pickup — world collect")]
    [SerializeField] private AudioClip defaultPickupSfx;

    [Header("Pickup — powerup activation (HUD button)")]
    [SerializeField] private AudioClip instantCapturePickupSfx;
    [SerializeField] private AudioClip grenadeReadyPickupSfx;
    [SerializeField] private AudioClip timeSlowPickupSfx;
    [SerializeField] private AudioClip swapZonesPickupSfx;
    [SerializeField] private AudioClip shuffleZonesPickupSfx;

    [Header("Grenade explosion")]
    [SerializeField] private AudioClip grenadeExplosionSfx;

    [Header("Music — AR setup / scan")]
    [SerializeField] private List<AudioClip> setupMusicTracks = new List<AudioClip>();

    [Header("Music — gameplay")]
    [SerializeField] private List<AudioClip> musicTracks = new List<AudioClip>();

    public AudioClip MenuNavSfx => menuNavSfx;
    public AudioClip VictorySfx => victorySfx;
    public AudioClip PlayerZoneCaptureSfx => playerZoneCaptureSfx;
    public AudioClip EnemyZoneCaptureSfx => enemyZoneCaptureSfx;
    public AudioClip PlayerHurtSfx => playerHurtSfx;
    public AudioClip PlayerDeadSfx => playerDeadSfx;
    public AudioClip EnemyWinSfx => enemyWinSfx;

    public List<AudioClip> GetSetupMusicPlaylist() => BuildPlaylist(setupMusicTracks);
    public List<AudioClip> GetMusicPlaylist() => BuildPlaylist(musicTracks);


    //builds a playlist of audio clips
    private static List<AudioClip> BuildPlaylist(List<AudioClip> tracks)
    {
        var playlist = new List<AudioClip>();
        if (tracks == null)
        {
            return playlist;
        }

        foreach (AudioClip track in tracks)
        {
            if (track != null)
            {
                playlist.Add(track);
            }
        }

        return playlist;
    }
    public AudioClip GrenadeExplosionSfx => grenadeExplosionSfx;
    public AudioClip DefaultPickupSfx => defaultPickupSfx;


    //Switch statement to return the correct audio clip for the pickup kind
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
            case PickupKind.ShuffleZones:
                return shuffleZonesPickupSfx;
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
