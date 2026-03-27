using UnityEngine;

// One floor cell: tinted quad plus an owner-specific flag prefab.
public class Zone : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Who owns this tile right now.")]
    private ZoneManager.ZoneOwner owner;

    private float halfSizeX;
    private float halfSizeZ;
    private ZoneManager zoneManager;
    private MeshRenderer floorQuadRenderer;
    private GameObject activeFlagObject;

    public ZoneManager.ZoneOwner Owner {
        get { return owner; }
    }

    // Initializes this zone's geometry and flag visuals.
    public void Setup(float worldCellSizeX, float worldCellSizeZ, ZoneManager manager) {
        owner = ZoneManager.ZoneOwner.Neutral;
        zoneManager = manager;

        halfSizeX = worldCellSizeX * 0.5f;
        halfSizeZ = worldCellSizeZ * 0.5f;

        BuildFloorQuad();
        ApplyVisualsForCurrentOwner();
    }

    // Creates a flat floor quad slightly above the plane.
    private void BuildFloorQuad() {
        GameObject quadObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quadObject.name = "FloorQuad";
        quadObject.transform.SetParent(transform, false);
        quadObject.transform.localPosition = new Vector3(0f, 0.001f, 0f);
        quadObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        quadObject.transform.localScale = new Vector3(halfSizeX * 2f, halfSizeZ * 2f, 1f);

        // Destroy(quadObject.GetComponent<Collider>());

        floorQuadRenderer = quadObject.GetComponent<MeshRenderer>();
        floorQuadRenderer.material = zoneManager.GetFloorMaterial(ZoneManager.ZoneOwner.Neutral);
    }

    // Sets the owner and updates floor/flag visuals.
    public void SetOwner(ZoneManager.ZoneOwner newOwner) {
        owner = newOwner;
        ApplyVisualsForCurrentOwner();
    }

    // Applies floor material and owner-specific flag prefab.
    private void ApplyVisualsForCurrentOwner() {
        floorQuadRenderer.material = zoneManager.GetFloorMaterial(owner);
        ApplyFlagPrefabForCurrentOwner();
    }

    // Replaces the current flag object with this owner's prefab.
    private void ApplyFlagPrefabForCurrentOwner() {
        if (activeFlagObject != null) {
            Destroy(activeFlagObject);
        }

        GameObject flagPrefab = zoneManager.GetFlagPrefab(owner);
        activeFlagObject = Instantiate(flagPrefab, transform);
        // Flag is 5cm above the floor.
        activeFlagObject.transform.localPosition = new Vector3(0f, 0.05f, 0f);
        // Flag is not rotated.
        activeFlagObject.transform.localRotation = Quaternion.identity;
    }

    // Returns true when the world point lies inside this zone bounds.
    public bool Contains(Vector3 worldPosition) {
        Vector3 localPosition = transform.InverseTransformPoint(worldPosition);
        bool insideX = Mathf.Abs(localPosition.x) <= halfSizeX;
        bool insideZ = Mathf.Abs(localPosition.z) <= halfSizeZ;
        return insideX && insideZ;
    }
}
