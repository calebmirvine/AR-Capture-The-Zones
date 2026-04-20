using UnityEngine;
using Unity.AI.Navigation;

// Invisible play-area trigger for soft boundary.
// Listens for FLOOR_CONFIRMED / GAME_RESET_REQUESTED (no reference from ZoneManager required).
// Gernade uses static bounds for one-way XZ clamp after entering the PlayArea trigger.
public class ZonePerimeter : MonoBehaviour
{
    private const string PlayAreaTagName = "PlayArea";

    [Header("Soft boundary (one-way: in allowed, then cannot leave XZ)")]
    [SerializeField] [Min(1f)] private float playAreaTriggerHeight = 6f;

    public static Transform ActivePlaneTransform { get; private set; }
    public static Vector2 ActivePlaneSize { get; private set; }
    public static bool HasActiveBounds => ActivePlaneTransform != null;

    private GameObject root;

    private void OnEnable()
    {
        Messenger<Transform, Vector2>.AddListener(GameEvent.FLOOR_CONFIRMED, OnFloorConfirmed);
        Messenger.AddListener(GameEvent.GAME_RESET_REQUESTED, OnGameResetRequested);
    }

    private void OnDisable()
    {
        Messenger<Transform, Vector2>.RemoveListener(GameEvent.FLOOR_CONFIRMED, OnFloorConfirmed);
        Messenger.RemoveListener(GameEvent.GAME_RESET_REQUESTED, OnGameResetRequested);
    }

    private void OnFloorConfirmed(Transform planeTransform, Vector2 planeSize)
    {
        Build(planeTransform, planeSize);
    }

    private void OnGameResetRequested()
    {
        Clear();
    }

    private void OnValidate()
    {
        playAreaTriggerHeight = Mathf.Max(1f, playAreaTriggerHeight);
    }

    public void Clear()
    {
        if (root != null)
        {
            Destroy(root);
            root = null;
        }

        ActivePlaneTransform = null;
        ActivePlaneSize = default;
    }

    public void Build(Transform planeTransform, Vector2 planeSize)
    {
        Clear();

        ActivePlaneTransform = planeTransform;
        ActivePlaneSize = planeSize;

        root = new GameObject("PerimeterRoot");
        root.transform.SetParent(planeTransform, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        NavMeshModifier navMeshModifier = root.AddComponent<NavMeshModifier>();
        navMeshModifier.ignoreFromBuild = true;
        navMeshModifier.applyToChildren = true;

        CreatePlayAreaTrigger(root.transform, planeSize);
    }

    private void CreatePlayAreaTrigger(Transform parent, Vector2 planeSize)
    {
        GameObject triggerObject = new GameObject("PlayAreaVolume");
        triggerObject.transform.SetParent(parent, false);
        triggerObject.transform.localPosition = new Vector3(0f, playAreaTriggerHeight * 0.5f, 0f);
        triggerObject.transform.localRotation = Quaternion.identity;
        triggerObject.transform.localScale = Vector3.one;
        triggerObject.tag = PlayAreaTagName;

        BoxCollider boxCollider = triggerObject.AddComponent<BoxCollider>();
        boxCollider.isTrigger = true;
        boxCollider.center = Vector3.zero;
        boxCollider.size = new Vector3(planeSize.x, playAreaTriggerHeight, planeSize.y);
    }
}
