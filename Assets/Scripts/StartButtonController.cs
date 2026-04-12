using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

// Owns scan/confirm UI and notifies gameplay systems when floor is locked in.
public class StartButtonController : MonoBehaviour
{
    [SerializeField] private ARPlaneManager arPlaneManager;
    [SerializeField] private Button confirmButton;
    [SerializeField] private float minimumPlaneArea = 1f;

    private void OnEnable()
    {
        confirmButton.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!arPlaneManager.enabled)
        {
            return;
        }

        ARPlane largestPlane = GetLargestPlane();
        bool hasValidPlane = largestPlane != null &&
                             (largestPlane.size.x * largestPlane.size.y) >= minimumPlaneArea;

        confirmButton.gameObject.SetActive(hasValidPlane);
    }

    public void OnConfirmScan()
    {
        ARPlane planeToUse = GetLargestPlane();
        if (planeToUse == null)
        {
            return;
        }

        Transform planeTransform = planeToUse.transform;
        Vector2 planeSize = planeToUse.size;

        confirmButton.gameObject.SetActive(false);
        foreach (ARPlane plane in arPlaneManager.trackables)
        {
            plane.gameObject.SetActive(false);
        }

        arPlaneManager.enabled = false;
        Messenger<Transform, Vector2>.Broadcast(
            GameEvent.FLOOR_CONFIRMED,
            planeTransform,
            planeSize);
    }

    private ARPlane GetLargestPlane()
    {
        ARPlane largestPlane = null;
        float largestPlaneArea = 0f;

        foreach (ARPlane plane in arPlaneManager.trackables)
        {
            float area = plane.size.x * plane.size.y;
            if (area <= largestPlaneArea)
            {
                continue;
            }

            largestPlaneArea = area;
            largestPlane = plane;
        }

        return largestPlane;
    }
}
