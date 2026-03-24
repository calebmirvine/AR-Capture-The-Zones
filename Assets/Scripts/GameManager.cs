using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

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

    // Largest horizontal plane seen this frame (used for Confirm and for zone bounds).
    private ARPlane largestPlane;
    private float largestPlaneArea;

    private void Start() {
        // Hide Confirm until the floor is large enough (see Update).
        confirmButton.gameObject.SetActive(false);
        confirmButton.onClick.AddListener(OnConfirmScan);
    }

    // While scanning: track biggest floor and toggle the Confirm button.
    private void Update() {
        if (arPlaneManager.enabled == false) {
            return;
        }

        largestPlane = null;
        largestPlaneArea = 0f;

        // Iterate over each plane and find the largest one.
        foreach (ARPlane plane in arPlaneManager.trackables) {
            // Calculate the area of the plane.
            float area = plane.size.x * plane.size.y;
            if (area > largestPlaneArea) {
                // Update the largest plane area and plane.
                largestPlaneArea = area;
                largestPlane = plane;
            }
        }

        // Check if the largest plane meets the minimum area.
        bool meetsMinimum = false;
        if (largestPlane != null && largestPlaneArea >= minimumPlaneArea) {
            meetsMinimum = true;
        }

        if (meetsMinimum) {
            if (confirmButton.gameObject.activeSelf == false) {
                confirmButton.gameObject.SetActive(true);
            }
        } else {
            if (confirmButton.gameObject.activeSelf) {
                confirmButton.gameObject.SetActive(false);
            }
        }
    }

    // User tapped Confirm: lock in the current plane and spawn zones on it.
    private void OnConfirmScan() {
        if (zoneManager == null) {
            return;
        }

        // Resolve plane here (same rules as Update) so we don't use a stale or null largestPlane.
        ARPlane planeToUse = null;
        float bestArea = 0f;
        foreach (ARPlane plane in arPlaneManager.trackables) {
            float area = plane.size.x * plane.size.y;
            if (area > bestArea) {
                bestArea = area;
                planeToUse = plane;
            }
        }

        // Check if the plane to use is null or if the area is less than the minimum area.
        if (planeToUse == null || bestArea < minimumPlaneArea) {
            return;
        }
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
}
