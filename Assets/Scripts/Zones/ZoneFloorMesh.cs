using UnityEngine;

// Builds the procedural floor quad (mesh, collider, NavMesh-friendly) for one zone cell.
public static class ZoneFloorMesh
{
    public static MeshRenderer Build(Transform zoneTransform, float halfSizeX, float halfSizeZ, Material floorMaterial) {
        GameObject quadObject = new GameObject("FloorQuad");
        quadObject.transform.SetParent(zoneTransform, false);
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

        MeshCollider meshCollider = quadObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;
        meshCollider.convex = false;

        MeshRenderer meshRenderer = quadObject.AddComponent<MeshRenderer>();
        meshRenderer.material = floorMaterial;
        return meshRenderer;
    }
}
