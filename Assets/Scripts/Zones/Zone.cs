using UnityEngine;

// One floor cell: tinted quad plus an owner-specific flag prefab.
public class Zone : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Who owns this tile right now.")]
    private ZoneOwner owner;

    // Half-extents in local XZ (used for point-in-zone tests and mesh size).
    private float halfSizeX;
    private float halfSizeZ;

    private ZoneManager zoneManager;
    private MeshRenderer floorQuadRenderer;
    private GameObject activeFlagObject;

    public ZoneOwner Owner {
        get { return owner; }
    }

    // Initializes this zone's geometry and flag visuals.
    public void Setup(
        float worldCellSizeX,
        float worldCellSizeZ,
        ZoneManager manager)
    {
        owner = ZoneOwner.Neutral;
        zoneManager = manager;

        halfSizeX = worldCellSizeX * 0.5f;
        halfSizeZ = worldCellSizeZ * 0.5f;

        BuildFloorQuad();

        ApplyMaterialsForCurrentOwner();
    }

    // Creates a flat floor quad slightly above the plane.
    private void BuildFloorQuad() {
        GameObject quadObject = new GameObject("FloorQuad");
        quadObject.transform.SetParent(transform, false);
        quadObject.transform.localPosition = new Vector3(0f, 0.001f, 0f);
        quadObject.transform.localRotation = Quaternion.identity;

        Mesh mesh = new Mesh();
        Vector3 v0 = new Vector3(-halfSizeX, 0f, -halfSizeZ);
        Vector3 v1 = new Vector3(halfSizeX, 0f, -halfSizeZ);
        Vector3 v2 = new Vector3(halfSizeX, 0f, halfSizeZ);
        Vector3 v3 = new Vector3(-halfSizeX, 0f, halfSizeZ);

        mesh.vertices = new Vector3[] { v0, v1, v2, v3 };
        mesh.triangles = new int[] { 0, 2, 1, 0, 3, 2 };
        mesh.uv = new Vector2[] {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f)
        };

        MeshFilter meshFilter = quadObject.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;

        floorQuadRenderer = quadObject.AddComponent<MeshRenderer>();
        floorQuadRenderer.material = zoneManager.GetFloorMaterial(ZoneOwner.Neutral);
    }

    // Sets the owner and updates floor/flag visuals.
    public void SetOwner(ZoneOwner newOwner) {
        owner = newOwner;
        ApplyMaterialsForCurrentOwner();
    }

    // Applies floor and flag materials based on current owner.
    private void ApplyMaterialsForCurrentOwner() {
        floorQuadRenderer.material = zoneManager.GetFloorMaterial(owner);
        ApplyFlagPrefabForCurrentOwner();
    }

    // Replaces the current flag object with this owner's prefab.
    private void ApplyFlagPrefabForCurrentOwner() {
        if (activeFlagObject != null) {
            Destroy(activeFlagObject);
            activeFlagObject = null;
        }

        GameObject flagPrefab = zoneManager.GetFlagPrefab(owner);
        activeFlagObject = Instantiate(flagPrefab, transform);
        activeFlagObject.transform.localPosition = new Vector3(0f, 0.05f, 0f);
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
