using UnityEngine;

public class SoundLibrary : MonoBehaviour
{
    public static SoundLibrary Instance;

    //SFX
    [SerializeField] private AudioClip menuNavSfx;
    [SerializeField] private AudioClip victorySfx;

    //Music
    [SerializeField] private AudioClip gameMusic;

    public AudioClip MenuNavSfx => menuNavSfx;
    public AudioClip VictorySfx => victorySfx;
    public AudioClip GameMusic => gameMusic;

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
