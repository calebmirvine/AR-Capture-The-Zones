using System.Collections.Generic;
using UnityEngine;

// Applies one material to every MeshRenderer/SkinnedMeshRenderer under the flag root (all material slots).
public static class ZoneFlagVisuals
{
    public static Renderer[] CollectRenderers(GameObject flagRoot) {
        Renderer[] all = flagRoot.GetComponentsInChildren<Renderer>(true);
        List<Renderer> list = new List<Renderer>();
        for (int i = 0; i < all.Length; i++) {
            Renderer r = all[i];
            if (r is LineRenderer || r is ParticleSystemRenderer || r is TrailRenderer) continue;
            list.Add(r);
        }
        return list.ToArray();
    }

    public static void ApplyMaterial(Renderer[] renderers, Material material) {
        if (renderers == null || material == null) {
            return;
        }

        for (int i = 0; i < renderers.Length; i++) {
            Renderer r = renderers[i];
            if (r == null) {
                continue;
            }

            Material[] slots = r.sharedMaterials;
            if (slots == null || slots.Length == 0) {
                continue;
            }

            for (int j = 0; j < slots.Length; j++) {
                slots[j] = material;
            }
            r.sharedMaterials = slots;
        }
    }
}
