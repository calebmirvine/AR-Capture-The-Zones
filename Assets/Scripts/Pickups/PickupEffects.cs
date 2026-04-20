using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public enum PickupKind
{
    InstantCapture,
    GrenadeReady,
    TimeSlow,
    SwapZones,
}

// Run after default gameplay scripts so we re-apply slow after anything that might reset NavMeshAgent speeds.
[DefaultExecutionOrder(10000)]
public class PickupEffects : MonoBehaviour
{
    public static PickupEffects Instance { get; private set; }

    // The pickup HUD will stay visible for a short time after the pickup is consumed.
    public const float ActivePickupHudLingerSeconds = 0.5f;

    private float instantCaptureExpireAt;
    private float zoneSwapHudExpireAt;
    private float timeSlowHudExpireAt;
    private Coroutine enemySlowCoroutine;

    // Cached enemy state so we can restore it cleanly.
    private NavMeshAgent slowedAgent;
    private Animator slowedAnimator;
    private float originalAgentSpeed;
    private float originalAgentAngularSpeed;
    private float originalAgentAcceleration;
    private float originalAnimatorSpeed;
    private float activeTimeSlowScale = 1f;

    private bool hasPending;
    private PickupKind pendingKind;
    private float pendingInstantCaptureSeconds;
    private float pendingTimeSlowSeconds;
    private float pendingTimeSlowScale;
    private GameObject pendingGrenadePrefab;
    private float pendingGrenadeThrowForce;
    private float pendingGrenadeUpwardAngleDegrees;
    private ZoneManager pendingZoneManager;
    private float pendingZoneSwapHudSeconds;

    public bool HasPendingPowerup
    {
        get { return hasPending; }
    }

    /// <summary>
    /// When true, world pickups should not be collected: either a pickup is held for HUD activation,
    /// or a timed buff from a consumed pickup is still running.
    /// </summary>
    public bool IsPickupSlotOccupied
    {
        get
        {
            return hasPending
                || IsInstantPlayerCaptureActive
                || IsTimeSlowActive;
        }
    }

    public PickupKind PendingKind
    {
        get { return pendingKind; }
    }

    public bool IsInstantPlayerCaptureActive
    {
        get { return Time.time < instantCaptureExpireAt; }
    }

    public bool IsTimeSlowActive
    {
        get { return enemySlowCoroutine != null; }
    }

    public bool IsZoneSwapHudActive
    {
        get { return Time.time < zoneSwapHudExpireAt; }
    }

    public bool IsInstantCapturePickupHudActive
    {
        get { return Time.time < instantCaptureExpireAt + ActivePickupHudLingerSeconds; }
    }

    public bool IsTimeSlowPickupHudActive
    {
        get { return Time.time < timeSlowHudExpireAt; }
    }

    // Ensure there is only one instance of the PickupEffects class in the scene.
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void LateUpdate()
    {
        if (enemySlowCoroutine == null)
        {
            return;
        }

        // NavMesh/AI can reset agent settings after we assign them; keep the debuff applied every frame.
        if (slowedAgent != null)
        {
            slowedAgent.speed = originalAgentSpeed * activeTimeSlowScale;
            slowedAgent.angularSpeed = originalAgentAngularSpeed * activeTimeSlowScale;
            slowedAgent.acceleration = originalAgentAcceleration * activeTimeSlowScale;
        }

        if (slowedAnimator != null)
        {
            slowedAnimator.speed = originalAnimatorSpeed * activeTimeSlowScale;
        }
    }

    private void OnEnable()
    {
        Messenger.AddListener(GameEvent.GAME_RESET_REQUESTED, OnGameResetRequested);
    }

    private void OnDisable()
    {
        Messenger.RemoveListener(GameEvent.GAME_RESET_REQUESTED, OnGameResetRequested);
        ClearAllBuffs();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void SetPendingInstantCapture(float seconds)
    {
        BeginPending(PickupKind.InstantCapture);
        pendingInstantCaptureSeconds = seconds;
    }

    public void SetPendingTimeSlow(float seconds, float scale)
    {
        BeginPending(PickupKind.TimeSlow);
        pendingTimeSlowSeconds = seconds;
        pendingTimeSlowScale = scale;
    }

    public void SetPendingGrenade(GameObject prefab, float throwForce, float upwardAngleDegrees)
    {
        BeginPending(PickupKind.GrenadeReady);
        pendingGrenadePrefab = prefab;
        pendingGrenadeThrowForce = throwForce;
        pendingGrenadeUpwardAngleDegrees = upwardAngleDegrees;
    }

    public void SetPendingZoneSwap(ZoneManager zoneManager, float hudDisplaySeconds)
    {
        BeginPending(PickupKind.SwapZones);
        pendingZoneManager = zoneManager;
        pendingZoneSwapHudSeconds = hudDisplaySeconds;
    }

    public bool TryConsumePending()
    {
        if (!hasPending)
        {
            return false;
        }

        PickupKind kind = pendingKind;
        bool activated = false;

        switch (kind)
        {
            case PickupKind.InstantCapture:
                ActivateInstantPlayerCapture(pendingInstantCaptureSeconds);
                activated = true;
                break;
            case PickupKind.GrenadeReady:
                ThrowGrenade(pendingGrenadePrefab, pendingGrenadeThrowForce, pendingGrenadeUpwardAngleDegrees);
                activated = true;
                break;
            case PickupKind.TimeSlow:
                activated = ActivateTimeSlow(pendingTimeSlowSeconds, pendingTimeSlowScale);
                break;
            case PickupKind.SwapZones:
                if (pendingZoneManager != null)
                {
                    pendingZoneManager.SwapPlayerAndEnemyZones();
                    ShowZoneSwapHud(pendingZoneSwapHudSeconds);
                    activated = true;
                }

                break;
        }

        if (!activated)
        {
            return false;
        }

        SoundLibrary library = SoundLibrary.Instance;
        if (library != null)
        {
            AudioClip clip = library.GetPickupActivationSfx(kind);
            if (clip != null && SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySfx(clip);
            }
        }

        ClearPending();
        return true;
    }

    private void BeginPending(PickupKind kind)
    {
        ClearPending();
        hasPending = true;
        pendingKind = kind;
    }

    private void ClearPending()
    {
        hasPending = false;
        pendingGrenadePrefab = null;
        pendingZoneManager = null;
    }

    private static void ThrowGrenade(GameObject prefab, float throwForce, float upwardAngleDegrees)
    {
        Transform camTransform = Camera.main.transform;
        Vector3 spawnPosition = camTransform.position + camTransform.forward * 0.2f;
        Quaternion arc = Quaternion.AngleAxis(-upwardAngleDegrees, camTransform.right);
        Vector3 throwDirection = (arc * camTransform.forward).normalized;

        GameObject grenade = Object.Instantiate(prefab, spawnPosition, camTransform.rotation);
        grenade.GetComponent<Rigidbody>().AddForce(throwDirection * throwForce, ForceMode.Impulse);
    }

    public void ActivateInstantPlayerCapture(float seconds)
    {
        float candidate = Time.time + seconds;
        if (candidate > instantCaptureExpireAt)
        {
            instantCaptureExpireAt = candidate;
        }
    }

    public void ShowZoneSwapHud(float seconds)
    {
        float candidate = Time.time + seconds + ActivePickupHudLingerSeconds;
        if (candidate > zoneSwapHudExpireAt)
        {
            zoneSwapHudExpireAt = candidate;
        }
    }

    //Slow down the enemy movement and animation speed but keep the player's movement and camera speed unaffected.
    /// <returns>False if no enemy exists yet (pending power-up is not cleared so the player can try again).</returns>
    public bool ActivateTimeSlow(float seconds, float scale)
    {
        StopEnemySlow();

        Enemy enemy = FindEnemy();
        if (enemy == null)
        {
            timeSlowHudExpireAt = 0f;
            return false;
        }

        timeSlowHudExpireAt = Time.time + seconds + ActivePickupHudLingerSeconds;

        slowedAgent = ResolveNavMeshAgent(enemy);
        slowedAnimator = ResolveAnimator(enemy);
        float clampedScale = Mathf.Clamp(scale, 0.05f, 1f);
        activeTimeSlowScale = clampedScale;

        if (slowedAgent != null)
        {
            originalAgentSpeed = slowedAgent.speed;
            originalAgentAngularSpeed = slowedAgent.angularSpeed;
            originalAgentAcceleration = slowedAgent.acceleration;
            slowedAgent.speed = originalAgentSpeed * activeTimeSlowScale;
            slowedAgent.angularSpeed = originalAgentAngularSpeed * activeTimeSlowScale;
            slowedAgent.acceleration = originalAgentAcceleration * activeTimeSlowScale;
        }

        if (slowedAnimator != null)
        {
            originalAnimatorSpeed = slowedAnimator.speed;
            slowedAnimator.speed = originalAnimatorSpeed * activeTimeSlowScale;
        }

        enemySlowCoroutine = StartCoroutine(RestoreEnemyAfter(seconds));
        return true;
    }

    private IEnumerator RestoreEnemyAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        StopEnemySlow();
    }

    private void StopEnemySlow()
    {
        if (enemySlowCoroutine != null)
        {
            StopCoroutine(enemySlowCoroutine);
            enemySlowCoroutine = null;
        }

        if (slowedAgent != null)
        {
            slowedAgent.speed = originalAgentSpeed;
            slowedAgent.angularSpeed = originalAgentAngularSpeed;
            slowedAgent.acceleration = originalAgentAcceleration;
        }

        if (slowedAnimator != null)
        {
            slowedAnimator.speed = originalAnimatorSpeed;
        }

        slowedAgent = null;
        slowedAnimator = null;
        activeTimeSlowScale = 1f;
    }

    private static Animator ResolveAnimator(Enemy enemy)
    {
        if (enemy == null)
        {
            return null;
        }

        // Prefer self/parents (skip Animators with no controller — do not use layerCount; it throws).
        for (Transform t = enemy.transform; t != null; t = t.parent)
        {
            Animator a = t.GetComponent<Animator>();
            if (IsUsableCharacterAnimator(a))
            {
                return a;
            }
        }

        // Fall back: animator on a child mesh (e.g. Y Bot under the Enemy root).
        Animator[] children = enemy.GetComponentsInChildren<Animator>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Animator a = children[i];
            if (IsUsableCharacterAnimator(a))
            {
                return a;
            }
        }

        return null;
    }

    private static bool IsUsableCharacterAnimator(Animator animator)
    {
        return animator != null
            && animator.enabled
            && animator.runtimeAnimatorController != null;
    }

    private static NavMeshAgent ResolveNavMeshAgent(Enemy enemy)
    {
        if (enemy == null)
        {
            return null;
        }

        NavMeshAgent agent = enemy.Agent;
        if (agent != null)
        {
            return agent;
        }

        agent = enemy.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            return agent;
        }

        return enemy.GetComponentInChildren<NavMeshAgent>(true);
    }

    private static Enemy FindEnemy()
    {
        if (Enemy.Active != null)
        {
            return Enemy.Active;
        }

        GameObject enemyObject = GameObject.FindGameObjectWithTag("Enemy");
        if (enemyObject != null)
        {
            Enemy enemy = enemyObject.GetComponentInParent<Enemy>();
            if (enemy != null)
            {
                return enemy;
            }

            enemy = enemyObject.GetComponentInChildren<Enemy>(true);
            if (enemy != null)
            {
                return enemy;
            }
        }

        return Object.FindAnyObjectByType<Enemy>(FindObjectsInactive.Exclude);
    }

    private void OnGameResetRequested()
    {
        ClearAllBuffs();
    }

    private void ClearAllBuffs()
    {
        instantCaptureExpireAt = 0f;
        zoneSwapHudExpireAt = 0f;
        timeSlowHudExpireAt = 0f;
        ClearPending();
        StopEnemySlow();
    }
}
