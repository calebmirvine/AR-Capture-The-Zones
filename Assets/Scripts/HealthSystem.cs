using System.Collections;
using UnityEngine;

public class HealthSystem : MonoBehaviour
{
    public static HealthSystem Instance { get; private set; }

    [SerializeField] private int maxHealth = 3;
    [SerializeField] private float ghostDurationSeconds = 8f;
    [SerializeField] private float spawnInvulnerabilitySeconds = 1.5f;

    private int currentHealth;
    private bool isGhost;
    private float ghostTimeRemaining;
    private Coroutine ghostCoroutine;
    private float invulnerableUntilTime;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public float HealthNormalized => (float)currentHealth / maxHealth; //normalized health value between 0 and 1

    public bool IsGhost => isGhost;
    public float GhostTimeRemaining => ghostTimeRemaining;

    private void Awake()
    {
        Instance = this;
        maxHealth = Mathf.Max(1, maxHealth);
        currentHealth = maxHealth;
        invulnerableUntilTime = Time.time + spawnInvulnerabilitySeconds;
    }

    private void Start()
    {
        NotifyHealthChanged();
    }

    private void OnEnable()
    {
        Messenger.AddListener(GameEvent.GAME_RESET_REQUESTED, OnGameResetOrMatchStart);
        Messenger.AddListener(GameEvent.GAMEPLAY_STARTED, OnGameResetOrMatchStart);
    }

    private void OnDisable()
    {
        Messenger.RemoveListener(GameEvent.GAME_RESET_REQUESTED, OnGameResetOrMatchStart);
        Messenger.RemoveListener(GameEvent.GAMEPLAY_STARTED, OnGameResetOrMatchStart);
    }

    private void OnGameResetOrMatchStart()
    {
        ResetToFullHealth();
    }

    public void ResetToFullHealth()
    {
        if (ghostCoroutine != null)
        {
            StopCoroutine(ghostCoroutine);
            ghostCoroutine = null;
        }

        isGhost = false;
        ghostTimeRemaining = 0f;
        currentHealth = maxHealth;
        invulnerableUntilTime = Time.time + spawnInvulnerabilitySeconds;
        NotifyHealthChanged();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void TakeDamage(int damage)
    {
        if (isGhost || damage <= 0)
        {
            return;
        }

        if (Time.time < invulnerableUntilTime)
        {
            return;
        }

        currentHealth -= damage;
        Debug.Log($"Took {damage} damage, health remaining: {currentHealth}");

        SoundLibrary library = SoundLibrary.Instance;
        if (library != null)
        {
            AudioClip hurtSfx = library.PlayerHurtSfx;
            if (hurtSfx != null)
            {
                SoundManager.Instance.PlayOneShot(hurtSfx);
            }
        }

        if (currentHealth > 0)
        {
            NotifyHealthChanged();
            return;
        }

        currentHealth = 0;
        NotifyHealthChanged();
        EnterGhostMode();
    }

    private void NotifyHealthChanged()
    {
        Messenger<float>.Broadcast(GameEvent.PLAYER_HEALTH_CHANGED, HealthNormalized, MessengerMode.DONT_REQUIRE_LISTENER);
    }

    private void EnterGhostMode()
    {
        if (ghostCoroutine != null)
        {
            StopCoroutine(ghostCoroutine);
        }

        SoundLibrary library = SoundLibrary.Instance;
        if (library != null)
        {
            AudioClip deadSfx = library.PlayerDeadSfx;
            if (deadSfx != null)
            {
                SoundManager.Instance.PlayOneShot(deadSfx);
            }
        }

        isGhost = true;
        ghostTimeRemaining = ghostDurationSeconds;
        ghostCoroutine = StartCoroutine(GhostCountdown());
    }

    private IEnumerator GhostCountdown()
    {
        float duration = ghostDurationSeconds;
        ghostTimeRemaining = duration;

        while (ghostTimeRemaining > 0f)
        {
            ghostTimeRemaining -= Time.deltaTime;
            yield return null;
        }

        ghostTimeRemaining = 0f;
        isGhost = false;
        currentHealth = maxHealth;
        invulnerableUntilTime = Time.time + spawnInvulnerabilitySeconds;
        NotifyHealthChanged();
        ghostCoroutine = null;
    }
}
