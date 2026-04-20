using UnityEngine;

// Spawns particle feedback when a zone is captured.
// SFX is played from ZoneManager (see PlayZoneCaptureSfx) so capture audio works without this component.
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
        SpawnFeedback(
            capturedZone,
            playerCaptureParticlePrefab,
            zoneManager.MainCameraTransform);
    }

    private void OnEnemyCapturedZone(Zone capturedZone)
    {
        SpawnFeedback(
            capturedZone,
            enemyCaptureParticlePrefab,
            GameObject.FindWithTag("Enemy")?.transform);
    }

    private void SpawnFeedback(Zone capturedZone, GameObject particlePrefab, Transform actorTransform)
    {
        if (particlePrefab != null)
        {
            GameObject zoneParticle = Instantiate(
                particlePrefab,
                capturedZone.transform.position,
                Quaternion.identity);
            Destroy(zoneParticle, particleLifetime);

            if (actorTransform != null)
            {
                GameObject actorParticle = Instantiate(
                    particlePrefab,
                    actorTransform.position,
                    Quaternion.identity,
                    actorTransform);
                Destroy(actorParticle, particleLifetime);
            }
        }
    }
}
