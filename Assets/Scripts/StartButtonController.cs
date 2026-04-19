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
        SetConfirmButtonVisible(false);
        Messenger.AddListener(GameEvent.GAME_RESET_REQUESTED, OnGameResetRequested);
        TryPlayScanningMusic();
    }

    private void OnDisable()
    {
        Messenger.RemoveListener(GameEvent.GAME_RESET_REQUESTED, OnGameResetRequested);
    }

    private void Update()
    {
        if (arPlaneManager == null)
        {
            return;
        }

        if (!arPlaneManager.enabled)
        {
            return;
        }

        ARPlane largestPlane = GetLargestPlane();
        bool hasValidPlane = largestPlane != null &&
                             (largestPlane.size.x * largestPlane.size.y) >= minimumPlaneArea;

        SetConfirmButtonVisible(hasValidPlane);
    }

    public void OnConfirmScan()
    {
        PlayNavigationSfx();

        ARPlane planeToUse = GetLargestPlane();
        if (planeToUse == null)
        {
            return;
        }

        Transform planeTransform = planeToUse.transform;
        Vector2 planeSize = planeToUse.size;

        SetConfirmButtonVisible(false);
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
        if (arPlaneManager == null)
        {
            Debug.LogWarning("StartButtonController arPlaneManager reference is missing.");
            return;
        }

        if (arSession != null)
        {
        arSession.Reset();
        }

        arPlaneManager.enabled = true;
        //.trackables is the live list of planes that are being tracked by the ARPlaneManager
        foreach (ARPlane plane in arPlaneManager.trackables) 
        {
            plane.gameObject.SetActive(true);
        }

        SetConfirmButtonVisible(false);
        TryPlayScanningMusic();
    }

    private static void TryPlayScanningMusic()
    {
        SoundLibrary library = SoundLibrary.Instance;
        if (library == null)
        {
            return;
        }

        AudioClip clip = library.ScanningMusic;
        if (clip == null || SoundManager.Instance == null)
        {
            return;
        }

        SoundManager.Instance.PlayMusic(clip);
    }

//A plane grows and shifts, we need to find the largest plane to use for the game
//we compare all our little "patches" to find the largest one to use for the game
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

    private void PlayNavigationSfx()
    {
        SoundManager.Instance.PlaySfx(SoundLibrary.Instance.MenuNavSfx);
    }

    private void SetConfirmButtonVisible(bool visible)
    {
        if (confirmButton == null)
        {
            Debug.LogWarning("StartButtonController confirmButton reference is missing.");
            return;
        }

        confirmButton.gameObject.SetActive(visible);
    }
}
