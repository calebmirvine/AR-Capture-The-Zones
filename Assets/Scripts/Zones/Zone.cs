using UnityEngine;

// One grid cell: procedural floor + instantiated flag; visuals driven by ZoneMaterials.
public class Zone : MonoBehaviour
{
    [SerializeField] private int row;
    [SerializeField] private int col;
    [SerializeField] private ZoneOwner owner;

    private float halfSizeX;
    private float halfSizeZ;

    private MeshRenderer floorQuadRenderer;
    private Renderer[] flagRenderers;
    private ZoneMaterials materials;
    private bool showContestedDisplay;

    public int Row => row;
    public int Col => col;
    public ZoneOwner Owner => owner;

    // Create floor, flag instance, and initial neutral materials.
    public void Setup(
        int rowIndex,
        int colIndex,
        Vector3 worldCenter,
        Vector3 worldCellSize,
        ZoneMaterials zoneMaterials,
        GameObject flagPrefab)
    {
        row = rowIndex;
        col = colIndex;
        owner = ZoneOwner.Neutral;
        materials = zoneMaterials;

        transform.position = worldCenter;

        halfSizeX = worldCellSize.x * 0.5f;
        halfSizeZ = worldCellSize.z * 0.5f;

        floorQuadRenderer = ZoneFloorMesh.Build(transform, halfSizeX, halfSizeZ, materials.GetFloor(ZoneOwner.Neutral));

        GameObject flagObject = Instantiate(flagPrefab, transform);
        flagObject.transform.localPosition = new Vector3(0f, 0.05f, 0f);
        flagObject.transform.localRotation = Quaternion.identity;
        flagRenderers = ZoneFlagVisuals.CollectRenderers(flagObject);
        Material neutralFlag = materials.GetFlag(ZoneOwner.Neutral);
        if (flagRenderers.Length == 0) {
            Debug.LogWarning("Zone: flag prefab has no MeshRenderer/SkinnedMeshRenderer children; flag tint will not apply.");
        } else if (neutralFlag == null) {
            Debug.LogWarning("Zone: flagMaterialNeutral not assigned on ZoneManager.");
        } else {
            ZoneFlagVisuals.ApplyMaterial(flagRenderers, neutralFlag);
        }

        ApplyMaterialsForCurrentOwner();
    }

    public void SetOwner(ZoneOwner newOwner) {
        owner = newOwner;
        RefreshVisuals();
    }

    // Visual-only: yellow contested floor/flag while player and enemy overlap; does not change Owner.
    public void SetContestedDisplay(bool contested) {
        showContestedDisplay = contested;
        RefreshVisuals();
    }


    void ApplyMaterialsForCurrentOwner() {
        showContestedDisplay = false;
        RefreshVisuals();
    }

    void RefreshVisuals() {
        ZoneOwner visualOwner = showContestedDisplay ? ZoneOwner.Contested : owner;

        Material floorTemplate = ResolveFloorMaterial(visualOwner);
        if (floorTemplate != null && floorQuadRenderer != null) {
            floorQuadRenderer.material = floorTemplate;
        }

        Material flagTemplate = ResolveFlagMaterial(visualOwner);
        if (flagTemplate != null) {
            ZoneFlagVisuals.ApplyMaterial(flagRenderers, flagTemplate);
        }
    }

    Material ResolveFloorMaterial(ZoneOwner visualOwner) {
        Material resolved = materials.GetFloor(visualOwner);
        if (resolved != null) return resolved;

        // Avoid magenta fallback if any specific material is unassigned.
        resolved = materials.GetFloor(owner);
        if (resolved != null) return resolved;
        return materials.GetFloor(ZoneOwner.Neutral);
    }

    Material ResolveFlagMaterial(ZoneOwner visualOwner) {
        Material resolved = materials.GetFlag(visualOwner);
        if (resolved != null) return resolved;

        resolved = materials.GetFlag(owner);
        if (resolved != null) return resolved;
        return materials.GetFlag(ZoneOwner.Neutral);
    }

    public bool Contains(Vector3 worldPosition) {
        Vector3 localPosition = transform.InverseTransformPoint(worldPosition);
        bool insideX = Mathf.Abs(localPosition.x) <= halfSizeX;
        bool insideZ = Mathf.Abs(localPosition.z) <= halfSizeZ;
        return insideX && insideZ;
    }

    // Random point on the cell floor (y = 0 in zone space). Callers should snap to NavMesh.
    public Vector3 GetRandomPointOnFloor() {
        float rx = Random.Range(-halfSizeX, halfSizeX);
        float rz = Random.Range(-halfSizeZ, halfSizeZ);
        Vector3 local = new Vector3(rx, 0f, rz);
        return transform.TransformPoint(local);
    }
}
