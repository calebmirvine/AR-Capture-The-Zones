using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : MonoBehaviour
{
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioMixer mixer;

    private float sfxVolume = 1f;
    private float musicVolume = 1f;
    private bool sfxPlaying = true;
    private bool musicPlaying = true;
    private Coroutine musicPlaylistCoroutine;

    const string PP_MUSIC_VOLUME = "MusicVolume";
    const string PP_SFX_VOLUME = "SfxVolume";
    const string PP_MUSIC_PLAYING = "MusicPlaying";
    const string PP_SFX_PLAYING = "SfxPlaying";
    const float MUTE_DB = -80f;

    static public SoundManager Instance { get; private set; } = null;

     public float SfxVolume
    {
        get { return sfxVolume; }
        set{ 
            sfxVolume = Mathf.Clamp(value, 0f, 1f); 
            ApplySfxVolume();
        }
    }
    public float MusicVolume
    {
        get { return musicVolume; }
        set{ 
            musicVolume = Mathf.Clamp(value, 0f, 1f); 
            ApplyMusicVolume();
        }
    }
    public bool SfxPlaying
    {
        get { return sfxPlaying; }
        set
        {
            sfxPlaying = value;
            ApplySfxVolume();
            SaveAudioPrefs();
        }
    }
    public bool MusicPlaying
    {
        get { return musicPlaying; }
        set
        {
            musicPlaying = value;
            ApplyMusicVolume();
            SaveAudioPrefs();
        }
    }

    private void OnDestroy() => SaveAudioPrefs();

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveAudioPrefs();
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
            Init();
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    private void Init()
    {
        musicVolume = PlayerPrefs.GetFloat(PP_MUSIC_VOLUME, 1f);
        sfxVolume = PlayerPrefs.GetFloat(PP_SFX_VOLUME, 1f);
        musicPlaying = PlayerPrefs.GetInt(PP_MUSIC_PLAYING, 1) == 1;
        sfxPlaying = PlayerPrefs.GetInt(PP_SFX_PLAYING, 1) == 1;

        ApplyMusicVolume();
        ApplySfxVolume();
    }

    private void Start()
    {
        // AudioMixer may ignore SetFloat during Awake; re-apply once the audio system is ready.
        ApplyMusicVolume();
        ApplySfxVolume();
    }

    private void SaveAudioPrefs()
    {
        PlayerPrefs.SetFloat(PP_MUSIC_VOLUME, musicVolume);
        PlayerPrefs.SetFloat(PP_SFX_VOLUME, sfxVolume);
        PlayerPrefs.SetInt(PP_MUSIC_PLAYING, musicPlaying ? 1 : 0);
        PlayerPrefs.SetInt(PP_SFX_PLAYING, sfxPlaying ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void ApplyMusicVolume()
    {
        if (!musicPlaying)
        {
            mixer.SetFloat("MusicVolume", MUTE_DB);
            return;
        }

        mixer.SetFloat("MusicVolume", LinearToLog(musicVolume));
    }

    private void ApplySfxVolume()
    {
        if (!sfxPlaying)
        {
            mixer.SetFloat("SfxVolume", MUTE_DB);
            return;
        }

        mixer.SetFloat("SfxVolume", LinearToLog(sfxVolume));
    }

    private float LinearToLog(float value)
    {
        return value <= 0f ? MUTE_DB : Mathf.Log10(value) * 20f;
    }

   

    
     public void PlaySfx(AudioClip clip, float volume = 1f) 
    {
        sfxSource.PlayOneShot(clip, volume);
    }

    public void PlayMusic(AudioClip clip, float volume = 1f)
    {
        StopMusicPlaylist();
        musicSource.loop = true;
        musicSource.clip = clip;
        musicSource.volume = volume;
        musicSource.Play();
    }

    public void PlayMusicPlaylist(List<AudioClip> playlist, float volume = 1f)
    {
        StopMusicPlaylist();
        if (playlist == null || playlist.Count == 0)
        {
            return;
        }

        musicSource.loop = false;
        musicPlaylistCoroutine = StartCoroutine(RunMusicPlaylist(playlist, volume));
    }

    private IEnumerator RunMusicPlaylist(List<AudioClip> playlist, float volume)
    {
        int trackCount = playlist.Count;
        int index = Random.Range(0, trackCount);
        while (true)
        {
            AudioClip track = playlist[index];
            musicSource.clip = track;
            musicSource.volume = volume;
            musicSource.Play();
            while (musicSource.isPlaying)
            {
                yield return null;
            }

            index = (index + 1) % trackCount;
        }
    }

    private void StopMusicPlaylist()
    {
        if (musicPlaylistCoroutine != null)
        {
            StopCoroutine(musicPlaylistCoroutine);
            musicPlaylistCoroutine = null;
        }
    }

    public void StopMusic()
    {
        StopMusicPlaylist();
        musicSource.Stop();
    }
}
