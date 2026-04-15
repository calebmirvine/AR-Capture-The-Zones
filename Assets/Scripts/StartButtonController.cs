using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

// Owns scan/confirm UI and notifies gameplay systems when floor is locked in.
public class StartButtonController : MonoBehaviour
{
    [SerializeField] private ARPlaneManager arPlaneManager;
    [SerializeField] private ARSession arSession;
    [SerializeField] private Button confirmButton;
    [SerializeField] private float minimumPlaneArea = 1f;

    private void OnEnable()
    {
        if (confirmButton != null)
        {
            confirmButton.gameObject.SetActive(false);
        }

        Messenger.AddListener(GameEvent.GAME_RESET_REQUESTED, OnGameResetRequested);
    }

    private void OnDisable()
    {
        Messenger.RemoveListener(GameEvent.GAME_RESET_REQUESTED, OnGameResetRequested);
    }

    private void Update()
    {
        if (arPlaneManager == null || confirmButton == null)
        {
            return;
        }

        if (!arPlaneManager.enabled)
        {
            confirmButton.gameObject.SetActive(false);
            return;
        }

        ARPlane largestPlane = GetLargestPlane();
        bool hasValidPlane = largestPlane != null &&
                             (largestPlane.size.x * largestPlane.size.y) >= minimumPlaneArea;

        confirmButton.gameObject.SetActive(hasValidPlane);
    }

    public void OnConfirmScan()
    {
        PlayNavigationSfx();

        if (arPlaneManager == null || confirmButton == null)
        {
            return;
        }

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

    private void OnGameResetRequested()
    {
        if (confirmButton != null)
        {
            confirmButton.gameObject.SetActive(false);
        }

        if (arSession != null)
        {
            arSession.Reset();
        }

        if (arPlaneManager == null)
        {
            return;
        }

        arPlaneManager.enabled = true;
        foreach (ARPlane plane in arPlaneManager.trackables)
        {
            plane.gameObject.SetActive(true);
        }
    }

    private ARPlane GetLargestPlane()
    {
        if (arPlaneManager == null)
        {
            return null;
        }

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

    private void PlayNavigationSfx()
    {
        SoundManager.Instance.PlaySfx(SoundLibrary.Instance.MenuNavSfx);
    }

}
