using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

// Scan phase: finds horizontal AR planes, shows Confirm when the largest is big enough.
// After confirm: hides planes, stops tracking, and tells ZoneManager to build the grid.
public class GameManager : MonoBehaviour
{
    [Header("AR")]
    [SerializeField]
    private ARPlaneManager arPlaneManager;

    [SerializeField]
    private ZoneManager zoneManager;

    [SerializeField]
    private Button confirmButton;

    [Header("Plane size")]
    [Tooltip("Minimum floor area in square meters before Confirm is allowed.")]
    [SerializeField]
    private float minimumPlaneArea = 1f;

    private void Start() {
        // Hide Confirm until the floor is large enough.
        confirmButton.gameObject.SetActive(false);
        confirmButton.onClick.AddListener(OnConfirmScan);
    }

    private void OnDestroy() {
        confirmButton.onClick.RemoveListener(OnConfirmScan);
    }

    // While scanning, show Confirm once a large enough plane is found.
    private void Update() {
        if (!arPlaneManager.enabled) return;

        ARPlane largestPlane = GetLargestPlane();
        bool hasValidPlane = largestPlane != null &&
            (largestPlane.size.x * largestPlane.size.y) >= minimumPlaneArea;
        confirmButton.gameObject.SetActive(hasValidPlane);
    }

    // User tapped Confirm: lock in the current plane and spawn zones on it.
    private void OnConfirmScan() {
        ARPlane planeToUse = GetLargestPlane();
        if (planeToUse == null) return;

        float largestArea = planeToUse.size.x * planeToUse.size.y;
        if (largestArea < minimumPlaneArea) return;

        Transform planeTransform = planeToUse.transform;
        Vector2 planeSize = planeToUse.size;

        // Disable the confirm button.
        confirmButton.gameObject.SetActive(false);

        // Disable all planes.
        foreach (ARPlane plane in arPlaneManager.trackables) {
            plane.gameObject.SetActive(false);
        }

        // Disable the plane manager.
        arPlaneManager.enabled = false;

        // Generate the zones.
        zoneManager.GenerateZones(planeTransform, planeSize);
    }

    // Returns the largest tracked AR plane, or null when none exist.
    private ARPlane GetLargestPlane() {
        ARPlane largestPlane = null;
        float largestPlaneArea = 0f;

        foreach (ARPlane plane in arPlaneManager.trackables) {
            float area = plane.size.x * plane.size.y;
            if (area <= largestPlaneArea) continue;

            largestPlaneArea = area;
            largestPlane = plane;
        }

        return largestPlane;
    }
}
