using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Kind linked to the pickup prefab.
[Serializable]
public class PickupHudEntry
{
    [SerializeField] private PickupKind kind;
    [SerializeField] private GameObject displayPrefab;

    public PickupKind Kind => kind;
    public GameObject DisplayPrefab => displayPrefab;
}

public class ActivePickupHudView : MonoBehaviour
{
    private static readonly Color ActivePowerupConsumedColor = new Color(1f, 0.22f, 0.22f, 1f);

    [SerializeField] private RectTransform container;
    [SerializeField] private List<PickupHudEntry> entries = new List<PickupHudEntry>();
    [SerializeField] private Button pickupButton;

    private Graphic buttonTintGraphic;
    private GameObject spawnedDisplay;
    private PickupKind? lastShownKind;
    private bool wasPendingPowerup;
    private Color defaultTintColor = Color.white;
    private bool hasCachedDefaultTint;
    private ColorBlock pickupButtonColorsInitial;
    private bool hasPickupButtonColorsInitial;

    private void Awake()
    {
        if (pickupButton != null)
        {
            pickupButtonColorsInitial = pickupButton.colors;
            hasPickupButtonColorsInitial = true;

            if (pickupButton.targetGraphic != null)
            {
                buttonTintGraphic = pickupButton.targetGraphic;
                defaultTintColor = buttonTintGraphic.color;
                hasCachedDefaultTint = true;
            }
        }
    }

    private void OnEnable()
    {
        Messenger.AddListener(GameEvent.GAME_RESET_REQUESTED, OnGameResetRequested);
    }

    private void OnDisable()
    {
        Messenger.RemoveListener(GameEvent.GAME_RESET_REQUESTED, OnGameResetRequested);
    }

    public void ConsumePendingPickup()
    {
        PickupEffects fx = PickupEffects.Instance;
        if (fx != null)
        {
            fx.TryConsumePending();
        }
    }

    private void LateUpdate()
    {
        PickupEffects fx = PickupEffects.Instance;
        if (fx == null)
        {
            return;
        }

        bool pendingNow = fx.HasPendingPowerup;
        if (pendingNow != wasPendingPowerup)
        {
            lastShownKind = null;
            ClearSpawnedDisplay();
        }

        wasPendingPowerup = pendingNow;

        PickupKind? current = null;
        GameObject prefab = null;

        if (fx.HasPendingPowerup)
        {
            PickupKind pendingKind = fx.PendingKind;
            for (int i = 0; i < entries.Count; i++)
            {
                PickupHudEntry entry = entries[i];
                if (entry.Kind != pendingKind)
                {
                    continue;
                }

                current = pendingKind;
                prefab = entry.DisplayPrefab;
                break;
            }
        }
        else
        {
            for (int i = 0; i < entries.Count; i++)
            {
                PickupHudEntry entry = entries[i];
                if (!IsKindActive(entry.Kind, fx))
                {
                    continue;
                }

                current = entry.Kind;
                prefab = entry.DisplayPrefab;
                break;
            }
        }

        bool consumedActiveDisplay = current != null && !fx.HasPendingPowerup;

        if (pickupButton != null)
        {
            pickupButton.interactable = fx.HasPendingPowerup;

            if (hasPickupButtonColorsInitial)
            {
                if (consumedActiveDisplay)
                {
                    ColorBlock cb = pickupButtonColorsInitial;
                    cb.disabledColor = ActivePowerupConsumedColor;
                    cb.colorMultiplier = 1f;
                    pickupButton.colors = cb;
                }
                else
                {
                    pickupButton.colors = pickupButtonColorsInitial;
                }
            }
        }

        ApplyTint(consumedActiveDisplay, current == null);

        if (current == lastShownKind)
        {
            return;
        }

        lastShownKind = current;
        ClearSpawnedDisplay();

        if (current != null && prefab != null)
        {
            spawnedDisplay = Instantiate(prefab, container);
        }
    }

    private void OnGameResetRequested()
    {
        lastShownKind = null;
        wasPendingPowerup = false;
        ClearSpawnedDisplay();
    }

    private void ClearSpawnedDisplay()
    {
        Destroy(spawnedDisplay);
        spawnedDisplay = null;
    }

    private void ApplyTint(bool consumedActiveDisplay, bool noPowerupShown)
    {
        if (buttonTintGraphic == null || !hasCachedDefaultTint)
        {
            return;
        }

        if (noPowerupShown || !consumedActiveDisplay)
        {
            buttonTintGraphic.color = defaultTintColor;
        }
        else
        {
            Color c = ActivePowerupConsumedColor;
            c.a = 1f;
            buttonTintGraphic.color = c;
        }
    }

    private static bool IsKindActive(PickupKind kind, PickupEffects fx)
    {
        switch (kind)
        {
            case PickupKind.InstantCapture:
                return fx.IsInstantCapturePickupHudActive;
            case PickupKind.GrenadeReady:
                return false;
            case PickupKind.TimeSlow:
                return fx.IsTimeSlowPickupHudActive;
            case PickupKind.SwapZones:
                return fx.IsZoneSwapHudActive;
            default:
                return false;
        }
    }
}
