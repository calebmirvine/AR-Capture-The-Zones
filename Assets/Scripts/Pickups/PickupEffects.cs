using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PickupEffects : MonoBehaviour
{
    public static PickupEffects Instance { get; private set; }

    private float instantCaptureExpireAt;
    private Coroutine enemySlowCoroutine;

    // Cached enemy state so we can restore it cleanly.
    private NavMeshAgent slowedAgent;
    private Animator slowedAnimator;
    private float originalAgentSpeed;
    private float originalAgentAngularSpeed;
    private float originalAgentAcceleration;
    private float originalAnimatorSpeed;

    // One-slot grenade inventory: set by BombPickup, thrown on next tap.
    private GameObject storedGrenadePrefab;
    private float storedThrowForce;
    private float storedUpwardAngleDegrees;

    public bool IsInstantPlayerCaptureActive
    {
        get { return Time.time < instantCaptureExpireAt; }
    }

    public bool HasStoredGrenade
    {
        get { return storedGrenadePrefab != null; }
    }

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

    private void Update()
    {
        if (!HasStoredGrenade) return;
        if (!WasTapThisFrame()) return;
        ThrowStoredGrenade();
    }

    public void StoreGrenade(GameObject prefab, float throwForce, float upwardAngleDegrees)
    {
        if (prefab == null) return;

        // One-slot inventory: a newer pickup simply overwrites the stored grenade.
        storedGrenadePrefab = prefab;
        storedThrowForce = throwForce;
        storedUpwardAngleDegrees = upwardAngleDegrees;
    }

    private void ThrowStoredGrenade()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            return;
        }

        Transform camTransform = cam.transform;
        Vector3 spawnPosition = camTransform.position + camTransform.forward * 0.2f;
        Quaternion arc = Quaternion.AngleAxis(-storedUpwardAngleDegrees, camTransform.right);
        Vector3 throwDirection = (arc * camTransform.forward).normalized;

        GameObject grenade = Instantiate(storedGrenadePrefab, spawnPosition, camTransform.rotation);
        Rigidbody rb = grenade.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(throwDirection * storedThrowForce, ForceMode.Impulse);
        }

        storedGrenadePrefab = null;
    }

    private static bool WasTapThisFrame()
    {
        bool tapped = false;
        if (Touchscreen.current != null)
        {
            tapped |= Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
        }

        if (Mouse.current != null)
        {
            tapped |= Mouse.current.leftButton.wasPressedThisFrame;
        }

        if (!tapped)
        {
            return false;
        }

        // Don't burn a grenade on UI taps (e.g. settings button, popup closes).
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return false;
        }

        return true;
    }

    public void ActivateInstantPlayerCapture(float seconds)
    {
        if (seconds <= 0f) return;

        float candidate = Time.time + seconds;
        if (candidate > instantCaptureExpireAt)
        {
            instantCaptureExpireAt = candidate;
        }
    }

    // A new pickup restarts the buff with the latest duration and scale.
    public void ActivateTimeSlow(float seconds, float scale)
    {
        if (seconds <= 0f) return;

        StopEnemySlow();

        Enemy enemy = FindEnemy();
        if (enemy == null) return;

        slowedAgent = enemy.Agent;
        slowedAnimator = enemy.Animator;
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

        // Enemy may have been destroyed mid-buff; skip safely via null checks.
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
        if (enemyObject == null) return null;

        Enemy enemy = enemyObject.GetComponentInParent<Enemy>();
        return enemy != null ? enemy : enemyObject.GetComponentInChildren<Enemy>(true);
    }

    private void OnGameResetRequested()
    {
        ClearAllBuffs();
    }

    private void ClearAllBuffs()
    {
        instantCaptureExpireAt = 0f;
        storedGrenadePrefab = null;
        StopEnemySlow();
    }
}
