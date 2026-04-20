using System.Collections.Generic;
using UnityEngine;

public class Gernade : MonoBehaviour
{
    private const string GrenadeTagName = "Grenade";

    [SerializeField] public float delay = 3f;
    [SerializeField] public float explosionRadius = 5f;
    [SerializeField] public float explosionForce = 700f;
    [SerializeField] public GameObject explosionPrefab;

    float countdown;
    bool exploded = false;

    private Rigidbody rigidBody;
    private bool playAreaContainmentActive;

    private void Awake()
    {
        rigidBody = GetComponent<Rigidbody>();
        if (rigidBody != null)
        {
            rigidBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        try
        {
            gameObject.tag = GrenadeTagName;
        }
        catch (UnityException)
        {
            // Tag "Grenade" missing from Tag Manager — set on prefab instead.
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryMarkInsidePlayArea(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryMarkInsidePlayArea(other);
    }

    private void TryMarkInsidePlayArea(Collider other)
    {
        if (other != null && other.CompareTag("PlayArea"))
        {
            playAreaContainmentActive = true;
        }
    }

    private static bool IsInsideFloorXZ(Vector3 localPos, float halfX, float halfZ)
    {
        return Mathf.Abs(localPos.x) <= halfX && Mathf.Abs(localPos.z) <= halfZ;
    }

    private void FixedUpdate()
    {
        if (!CompareTag(GrenadeTagName))
        {
            return;
        }

        if (exploded || !ZonePerimeter.HasActiveBounds || rigidBody == null)
        {
            return;
        }

        Transform plane = ZonePerimeter.ActivePlaneTransform;
        Vector2 planeSize = ZonePerimeter.ActivePlaneSize;

        Vector3 localPos = plane.InverseTransformPoint(rigidBody.position);
        float halfX = planeSize.x * 0.5f;
        float halfZ = planeSize.y * 0.5f;

        // Trigger volumes can miss (layers, tunneling). Also mark containment when over the floor rect.
        if (!playAreaContainmentActive && IsInsideFloorXZ(localPos, halfX, halfZ))
        {
            playAreaContainmentActive = true;
        }

        if (!playAreaContainmentActive)
        {
            return;
        }

        bool outsideX = Mathf.Abs(localPos.x) > halfX;
        bool outsideZ = Mathf.Abs(localPos.z) > halfZ;
        if (!outsideX && !outsideZ)
        {
            return;
        }

        localPos.x = Mathf.Clamp(localPos.x, -halfX, halfX);
        localPos.z = Mathf.Clamp(localPos.z, -halfZ, halfZ);
        rigidBody.MovePosition(plane.TransformPoint(localPos));

        Vector3 localVel = plane.InverseTransformDirection(rigidBody.linearVelocity);
        if (outsideX)
        {
            localVel.x = 0f;
        }

        if (outsideZ)
        {
            localVel.z = 0f;
        }

        rigidBody.linearVelocity = plane.TransformDirection(localVel);
    }

    void Start()
    {
        countdown = delay;
    }

    void Update()
    {
        countdown -= Time.deltaTime;
        if (countdown <= 0f && !exploded)
        {
            Explode();
            exploded = true;
        }
    }

    public void Explode()
    {
        SoundLibrary library = SoundLibrary.Instance;
        if (library != null)
        {
            AudioClip explosionSfx = library.GrenadeExplosionSfx;
            if (explosionSfx != null)
            {
                SoundManager.Instance.PlaySfx(explosionSfx);
            }
        }

        GameObject explosion = Instantiate(explosionPrefab, transform.position, transform.rotation);

        Collider[] nearby = Physics.OverlapSphere(transform.position, explosionRadius);
        var enemiesProcessed = new HashSet<Enemy>();
        foreach (Collider hit in nearby)
        {
            if (hit == null)
            {
                continue;
            }

            Enemy enemy = hit.GetComponentInParent<Enemy>();
            if (enemy != null)
            {
                if (enemiesProcessed.Add(enemy))
                {
                    enemy.ApplyGrenadeKnockback(transform.position, explosionRadius, explosionForce);
                }

                continue;
            }

            Rigidbody rb = hit.attachedRigidbody;
            if (rb != null && !rb.isKinematic)
            {
                rb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
            }
        }

        Destroy(explosion, 1f);
        Destroy(gameObject);
    }

    public void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
