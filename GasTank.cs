// ============================================================
//  GasTank.cs
// ============================================================
//  HOW TO ADD THIS SCRIPT IN UNITY
// ============================================================
//  1. Right-click your Scripts folder -> Create -> C# Script.
//     Name it exactly "GasTank".
//
//  2. Attach it to the same GameObject as CarController
//     (your player car).
//
//  3. In the Inspector, set your desired values:
//     - Max Gas          : total fuel capacity (default 100)
//     - Drain Rate       : fuel lost per second while driving
//     - Idle Drain Rate  : fuel lost per second while stationary
//     - Speed Threshold  : minimum speed to count as "driving"
//
//  4. Wire up the UI:
//     - Create a UI Slider (GameObject -> UI -> Slider).
//     - Drag it into the "Gas Slider" field on this component.
//     - Set the Slider's Min Value = 0, Max Value = 1.
//     - Disable "Interactable" so the player can't drag it.
//
//  5. GasTank automatically calls GameManager.Instance
//     .OnGasEmpty() when fuel hits zero. Make sure
//     GameManager has that method (see GameManager additions
//     below).
// ============================================================

using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CarController))]
public class GasTank : MonoBehaviour
{
    [Header("Fuel Settings")]
    [Tooltip("Maximum fuel capacity.")]
    public float maxGas = 100f;

    [Tooltip("Fuel drained per second while the car is moving.")]
    public float drainRate = 5f;

    [Tooltip("Fuel drained per second while the car is stationary (engine idling).")]
    public float idleDrainRate = 1f;

    [Tooltip("Speed (units/s) above which the driving drain rate is used.")]
    public float speedThreshold = 0.5f;

    [Header("UI (optional)")]
    [Tooltip("Drag a UI Slider here to show the fuel gauge.")]
    public Slider gasSlider;

    [Tooltip("Colour of the slider fill when fuel is healthy.")]
    public Color fullColour = new Color(0.2f, 0.85f, 0.3f);

    [Tooltip("Colour of the slider fill when fuel is critically low.")]
    public Color emptyColour = new Color(0.95f, 0.2f, 0.2f);

    [Tooltip("Fuel fraction below which the low-fuel colour kicks in.")]
    [Range(0f, 0.5f)]
    public float lowFuelThreshold = 0.2f;

    // ── State ──────────────────────────────────────────────────────────────
    private float currentGas;
    private bool  gasEmpty = false;
    private Rigidbody2D rb;
    private Image sliderFill;

    public float FuelFraction => currentGas / maxGas;

    // ── Unity messages ─────────────────────────────────────────────────────

    private void Awake()
    {
        rb         = GetComponent<Rigidbody2D>();
        currentGas = maxGas;

        if (gasSlider != null)
        {
            gasSlider.minValue = 0f;
            gasSlider.maxValue = 1f;
            gasSlider.value    = 1f;

            // Cache the fill image for colour changes
            Transform fillArea = gasSlider.transform.Find("Fill Area/Fill");
            if (fillArea != null)
                sliderFill = fillArea.GetComponent<Image>();
        }
    }

    private void Update()
    {
        if (gasEmpty) return;

        // Drain fuel
        float speed = rb != null ? rb.linearVelocity.magnitude : 0f;
        float drain = speed >= speedThreshold ? drainRate : idleDrainRate;
        currentGas  = Mathf.Max(0f, currentGas - drain * Time.deltaTime);

        // Update UI
        if (gasSlider != null)
        {
            gasSlider.value = FuelFraction;

            if (sliderFill != null)
                sliderFill.color = Color.Lerp(emptyColour, fullColour,
                    Mathf.InverseLerp(0f, lowFuelThreshold, FuelFraction));
        }

        // Trigger game-over when empty
        if (currentGas <= 0f)
        {
            gasEmpty = true;
            Debug.Log("Out of gas!");

            if (GameManager.Instance != null)
                GameManager.Instance.OnGasEmpty();
        }
    }

    // ── Public API ─────────────────────────────────────────────────────────

    /// <summary>Refuel by an absolute amount (clamped to maxGas).</summary>
    public void Refuel(float amount)
    {
        currentGas = Mathf.Min(currentGas + amount, maxGas);
        gasEmpty   = false;
        Debug.Log($"Refuelled +{amount}. Current: {currentGas}/{maxGas}");
    }

    /// <summary>Instantly fill the tank to max (call after an upgrade).</summary>
    public void RefuelFull()
    {
        currentGas = maxGas;
        gasEmpty   = false;
    }

    /// <summary>Upgrade the tank capacity and optionally refuel.</summary>
    public void UpgradeCapacity(float newMax, bool refillAfterUpgrade = true)
    {
        maxGas = newMax;
        if (refillAfterUpgrade) RefuelFull();
    }
}