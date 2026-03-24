using System.Collections.Generic;
using UnityEngine;

// One floor cell: tinted quad and flag. Materials come from named slots on ZoneManager.
public class Zone : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Set by code when the grid is built.")]
    private int row;

    [SerializeField]
    [Tooltip("Set by code when the grid is built.")]
    private int col;

    [SerializeField]
    [Tooltip("Who owns this tile right now.")]
    private ZoneOwner owner;

    // Half-extents in local XZ (used for point-in-zone tests and mesh size).
    private float halfSizeX;
    private float halfSizeZ;

    private MeshRenderer floorQuadRenderer;
    private Renderer[] flagRenderers;
    private ZoneMaterials materials;
    private bool applyPlayerFlagSurfaceColorToAllRenderers;

    MaterialPropertyBlock flagPropertyBlock;

    public int Row {
        get { return row; }
    }

    public int Col {
        get { return col; }
    }

    public ZoneOwner Owner {
        get { return owner; }
    }

    public void Setup(
        int rowIndex,
        int colIndex,
        Vector3 worldCenter,
        Vector3 worldCellSize,
        ZoneMaterials zoneMaterials,
        GameObject flagPrefab,
        bool pushPlayerFlagColorToAllRenderers)
    {
        row = rowIndex;
        col = colIndex;
        owner = ZoneOwner.Neutral;
        materials = zoneMaterials;
        applyPlayerFlagSurfaceColorToAllRenderers = pushPlayerFlagColorToAllRenderers;

        transform.position = worldCenter;

        halfSizeX = worldCellSize.x * 0.5f;
        halfSizeZ = worldCellSize.z * 0.5f;

        BuildFloorQuad();

        GameObject flagObject = Instantiate(flagPrefab, transform);
        flagObject.transform.localPosition = new Vector3(0f, 0.05f, 0f);
        flagObject.transform.localRotation = Quaternion.identity;
        flagRenderers = CollectFlagRenderers(flagObject);

        ApplyMaterialsForCurrentOwner();
    }

    // Creates a flat mesh slightly above the floor to reduce z-fighting.
    private void BuildFloorQuad() {
        // Create a new GameObject for the quad.
        GameObject quadObject = new GameObject("FloorQuad");
        // Set the parent of the quad object to the zone object.
        quadObject.transform.SetParent(transform, false);
        // Set the local position of the quad object to the center of the zone.
        quadObject.transform.localPosition = new Vector3(0f, 0.001f, 0f);
        // Keep local identity so it inherits zone/world plane rotation exactly once.
        quadObject.transform.localRotation = Quaternion.identity;

        // Create a new mesh for the quad.
        Mesh mesh = new Mesh();

        // Create the vertices for the quad. 
        // Bottom left
        Vector3 v0 = new Vector3(-halfSizeX, 0f, -halfSizeZ);
        // Bottom right
        Vector3 v1 = new Vector3(halfSizeX, 0f, -halfSizeZ);
        // Top right
        Vector3 v2 = new Vector3(halfSizeX, 0f, halfSizeZ);
        // Top left
        Vector3 v3 = new Vector3(-halfSizeX, 0f, halfSizeZ);

        // Bottom left, top right, bottom right, top left
        mesh.vertices = new Vector3[] { v0, v1, v2, v3 };
        
        mesh.triangles = new int[] { 0, 2, 1, 0, 3, 2 };

        mesh.uv = new Vector2[] {
            new Vector2(0f, 0f), // v0
            new Vector2(1f, 0f), // v1
            new Vector2(1f, 1f), // v2
            new Vector2(0f, 1f)  // v3
        };

        // Add a mesh filter to the quad object and set the mesh.
        MeshFilter meshFilter = quadObject.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;

        // Add a mesh renderer to the quad object.
        floorQuadRenderer = quadObject.AddComponent<MeshRenderer>();
        // Set the material for the floor quad to the neutral material.
        floorQuadRenderer.material = materials.GetFloor(ZoneOwner.Neutral);
    }

    // Set the owner of the zone and apply the materials for the current owner.
    public void SetOwner(ZoneOwner newOwner) {
        owner = newOwner;
        ApplyMaterialsForCurrentOwner();
    }

    private void ApplyMaterialsForCurrentOwner() {
        floorQuadRenderer.material = materials.GetFloor(owner);

        Material flagMat = materials.GetFlag(owner);
        ApplyFlagMaterial(flagMat);

        if (owner == ZoneOwner.Player && applyPlayerFlagSurfaceColorToAllRenderers && flagMat != null) {
            ApplyCapturedFlagSurfaceColorToAllRenderers(flagMat);
        } else {
            ClearFlagPropertyBlocks();
        }
    }

    static Renderer[] CollectFlagRenderers(GameObject flagRoot) {
        Renderer[] all = flagRoot.GetComponentsInChildren<Renderer>(true);
        List<Renderer> list = new List<Renderer>();
        for (int i = 0; i < all.Length; i++) {
            Renderer r = all[i];
            if (r is LineRenderer || r is ParticleSystemRenderer || r is TrailRenderer) continue;
            list.Add(r);
        }
        return list.ToArray();
    }

    void ApplyFlagMaterial(Material flagMat) {
        if (flagMat == null || flagRenderers == null || flagRenderers.Length == 0) {
            if (owner == ZoneOwner.Player && flagMat == null) {
                Debug.LogWarning("Zone: flagMaterialPlayer is not assigned on ZoneManager — flag will not change on capture.");
            }
            return;
        }

        for (int i = 0; i < flagRenderers.Length; i++) {
            Renderer r = flagRenderers[i];
            if (r == null) continue;

            int slotCount = r.sharedMaterials.Length;
            if (slotCount <= 1) {
                r.sharedMaterial = flagMat;
            } else {
                Material[] slots = new Material[slotCount];
                for (int s = 0; s < slotCount; s++) slots[s] = flagMat;
                r.sharedMaterials = slots;
            }
        }
    }

    void ApplyCapturedFlagSurfaceColorToAllRenderers(Material source) {
        if (flagPropertyBlock == null) flagPropertyBlock = new MaterialPropertyBlock();
        bool hasBase = source.HasProperty("_BaseColor");
        bool hasColor = source.HasProperty("_Color");
        if (!hasBase && !hasColor) return;

        Color c = hasBase ? source.GetColor("_BaseColor") : source.GetColor("_Color");

        for (int i = 0; i < flagRenderers.Length; i++) {
            Renderer r = flagRenderers[i];
            if (r == null) continue;
            int n = r.sharedMaterials.Length;
            for (int m = 0; m < n; m++) {
                flagPropertyBlock.Clear();
                if (hasBase) flagPropertyBlock.SetColor("_BaseColor", c);
                if (hasColor) flagPropertyBlock.SetColor("_Color", c);
                r.SetPropertyBlock(flagPropertyBlock, m);
            }
        }
    }

    void ClearFlagPropertyBlocks() {
        if (flagRenderers == null) return;
        for (int i = 0; i < flagRenderers.Length; i++) {
            Renderer r = flagRenderers[i];
            if (r == null) continue;
            r.SetPropertyBlock(null);
        }
    }

    // True if world point is inside this cell (tested in local XZ on the rotated plane).
    public bool Contains(Vector3 worldPosition) {
        Vector3 localPosition = transform.InverseTransformPoint(worldPosition);
        bool insideX = Mathf.Abs(localPosition.x) <= halfSizeX;
        bool insideZ = Mathf.Abs(localPosition.z) <= halfSizeZ;
        return insideX && insideZ;
    }
}
