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

public class PickupEffects : MonoBehaviour
{
    public static PickupEffects Instance { get; private set; }

    private float instantCaptureExpireAt;
    private float zoneSwapHudExpireAt;
    private Coroutine enemySlowCoroutine;

    // Cached enemy state so we can restore it cleanly.
    private NavMeshAgent slowedAgent;
    private Animator slowedAnimator;
    private float originalAgentSpeed;
    private float originalAgentAngularSpeed;
    private float originalAgentAcceleration;
    private float originalAnimatorSpeed;

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

        switch (pendingKind)
        {
            case PickupKind.InstantCapture:
                ActivateInstantPlayerCapture(pendingInstantCaptureSeconds);
                break;
            case PickupKind.GrenadeReady:
                ThrowGrenade(pendingGrenadePrefab, pendingGrenadeThrowForce, pendingGrenadeUpwardAngleDegrees);
                break;
            case PickupKind.TimeSlow:
                ActivateTimeSlow(pendingTimeSlowSeconds, pendingTimeSlowScale);
                break;
            case PickupKind.SwapZones:
                pendingZoneManager.SwapPlayerAndEnemyZones();
                ShowZoneSwapHud(pendingZoneSwapHudSeconds);
                break;
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
        float candidate = Time.time + seconds;
        if (candidate > zoneSwapHudExpireAt)
        {
            zoneSwapHudExpireAt = candidate;
        }
    }

    public void ActivateTimeSlow(float seconds, float scale)
    {
        StopEnemySlow();

        Enemy enemy = FindEnemy();
        if (enemy == null) return;

        slowedAgent = enemy.Agent;
        slowedAnimator = enemy.GetComponentInParent<Animator>();
        float clampedScale = Mathf.Clamp(scale, 0.05f, 1f);

        if (slowedAgent != null)
        {
            originalAgentSpeed = slowedAgent.speed;
            originalAgentAngularSpeed = slowedAgent.angularSpeed;
            originalAgentAcceleration = slowedAgent.acceleration;
            slowedAgent.speed = originalAgentSpeed * clampedScale;
            slowedAgent.angularSpeed = originalAgentAngularSpeed * clampedScale;
            slowedAgent.acceleration = originalAgentAcceleration * clampedScale;
        }

        if (slowedAnimator != null)
        {
            originalAnimatorSpeed = slowedAnimator.speed;
            slowedAnimator.speed = originalAnimatorSpeed * clampedScale;
        }

        enemySlowCoroutine = StartCoroutine(RestoreEnemyAfter(seconds));
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
    }

    private static Enemy FindEnemy()
    {
        GameObject enemyObject = GameObject.FindWithTag("Enemy");
        Enemy enemy = enemyObject?.GetComponentInParent<Enemy>();
        return enemy != null ? enemy : enemyObject?.GetComponentInChildren<Enemy>(true);
    }

    private void OnGameResetRequested()
    {
        ClearAllBuffs();
    }

    private void ClearAllBuffs()
    {
        instantCaptureExpireAt = 0f;
        zoneSwapHudExpireAt = 0f;
        ClearPending();
        StopEnemySlow();
    }
}
