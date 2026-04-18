using UnityEngine;

// Spawns particle + SFX feedback when a zone is captured.
// Listens to PLAYER_CAPTURED_ZONE and ENEMY_CAPTURED_ZONE from ZoneManager.
public class ZoneCaptureFeedback : MonoBehaviour
{
    [SerializeField] private ZoneManager zoneManager;

    [SerializeField] private GameObject playerCaptureParticlePrefab;
    [SerializeField] private GameObject enemyCaptureParticlePrefab;

    [SerializeField] private float particleLifetime = 3f;

    private void OnEnable()
    {
        Messenger<Zone>.AddListener(GameEvent.PLAYER_CAPTURED_ZONE, OnPlayerCapturedZone);
        Messenger<Zone>.AddListener(GameEvent.ENEMY_CAPTURED_ZONE, OnEnemyCapturedZone);
    }

    private void OnDisable()
    {
        Messenger<Zone>.RemoveListener(GameEvent.PLAYER_CAPTURED_ZONE, OnPlayerCapturedZone);
        Messenger<Zone>.RemoveListener(GameEvent.ENEMY_CAPTURED_ZONE, OnEnemyCapturedZone);
    }

    private void OnPlayerCapturedZone(Zone capturedZone)
    {
        Transform playerTransform = zoneManager != null ? zoneManager.MainCameraTransform : null;
        if (playerTransform == null && Camera.main != null)
        {
            playerTransform = Camera.main.transform;
        }

        SpawnFeedback(
            capturedZone,
            playerCaptureParticlePrefab,
            playerTransform,
            SoundLibrary.Instance != null ? SoundLibrary.Instance.PlayerZoneCaptureSfx : null);
    }

    private void OnEnemyCapturedZone(Zone capturedZone)
    {
        GameObject enemyObject = GameObject.FindWithTag("Enemy");
        Transform enemyTransform = enemyObject != null ? enemyObject.transform : null;

        SpawnFeedback(
            capturedZone,
            enemyCaptureParticlePrefab,
            enemyTransform,
            SoundLibrary.Instance != null ? SoundLibrary.Instance.EnemyZoneCaptureSfx : null);
    }

    private void SpawnFeedback(Zone capturedZone, GameObject particlePrefab, Transform actorTransform, AudioClip sfx)
    {
        if (capturedZone != null && particlePrefab != null)
        {
            GameObject zoneParticle = Instantiate(
                particlePrefab,
                capturedZone.transform.position,
                Quaternion.identity);
            Destroy(zoneParticle, particleLifetime);
        }

        if (actorTransform != null && particlePrefab != null)
        {
            GameObject actorParticle = Instantiate(
                particlePrefab,
                actorTransform.position,
                Quaternion.identity,
                actorTransform);
            Destroy(actorParticle, particleLifetime);
        }

        if (sfx != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySfx(sfx);
        }
    }
}
