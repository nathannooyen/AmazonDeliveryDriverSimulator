using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class CarController : MonoBehaviour
{
    [Header("Movement")]
    public float accelerationForce = 15f;
    public float maxSpeed = 10f;
    public float turnSpeed = 160f;

    [Header("Drift")]
    public float normalGrip = 0.9f;          // lateral velocity damping when not drifting
    public float driftGrip = 0.3f;           // lower = more slide
    public float driftSpeedThreshold = 4f;   // minimum speed to trigger drift
    public float driftTransitionSpeed = 6f;  // how fast grip blends in/out

    [Header("Feel")]
    public float visualTiltAmount = 8f;      // degrees the sprite tilts when turning
    public float visualTiltSpeed = 8f;

    // ── internals ──────────────────────────────────────────────────────────
    private Rigidbody2D rb;
    private float currentGrip;
    private float targetGrip;
    private float visualTiltAngle;
    private bool isDrifting;

    // Input
    private float throttle;
    private float steer;
    private bool driftInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        currentGrip = normalGrip;
    }

    void Update()
    {
        // ── Read input ─────────────────────────────────────────────────────
        // Vertical input (W/S or Up/Down)
        throttle = 0f;
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
            throttle = 1f;
        else if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
            throttle = -1f;

        // Horizontal input (A/D or Left/Right)
        steer = 0f;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            steer = -1f;
        else if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            steer = 1f;

        // Drift input (Shift)
        driftInput = Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;

        // ── Drift eligibility ──────────────────────────────────────────────
        isDrifting = driftInput && rb.linearVelocity.magnitude >= driftSpeedThreshold;
        targetGrip = isDrifting ? driftGrip : normalGrip;

        // ── Smooth grip transition ─────────────────────────────────────────
        currentGrip = Mathf.Lerp(currentGrip, targetGrip, Time.deltaTime * driftTransitionSpeed);

        // ── Visual tilt (cosmetic, does not affect physics) ────────────────
        float tiltTarget = -steer * visualTiltAmount * (rb.linearVelocity.magnitude / maxSpeed);
        visualTiltAngle  = Mathf.Lerp(visualTiltAngle, tiltTarget, Time.deltaTime * visualTiltSpeed);
    }

    void FixedUpdate()
    {
        // ── Steering  (only when moving) ──────────────────────────────────
        //float speedFactor   = rb.linearVelocity.magnitude / maxSpeed;
        //float rotationDelta = steer * turnSpeed * Mathf.Clamp01(speedFactor) * Time.fixedDeltaTime;
        //rb.MoveRotation(rb.rotation + rotationDelta);
        // ── Steering (enhanced for drifting) ─────────────────────────────

        // How sideways the car is moving (0 = straight, 1 = fully sideways)
        Vector2 velocityDir = rb.linearVelocity.normalized;
        float driftAmount = Mathf.Abs(Vector2.Dot(velocityDir, transform.right));

        // Boost turning when drifting
        float driftTurnMultiplier = 1.8f; // tweak this (1.5–2.5 feels good)

        // Optional: extra boost only when drift button is held
        float driftBoost = isDrifting ? driftTurnMultiplier : 1f;

        // Combine everything
        float speedFactor = rb.linearVelocity.magnitude / maxSpeed;
        float finalTurnSpeed = turnSpeed * Mathf.Lerp(1f, driftBoost, driftAmount);

        float rotationDelta = steer * finalTurnSpeed * Mathf.Clamp01(speedFactor) * Time.fixedDeltaTime;

        rb.MoveRotation(rb.rotation + rotationDelta);

        // ── Acceleration  (forward in the direction the car faces) ────────
        if (throttle != 0f)
        {
            Vector2 forwardDir = transform.up;
            Vector2 force      = forwardDir * throttle * accelerationForce;

            // Only apply force if under max speed (or braking)
            if (rb.linearVelocity.magnitude < maxSpeed || throttle < 0f)
                rb.AddForce(force, ForceMode2D.Force);
        }

        // ── Grip / lateral damping ─────────────────────────────────────────
        // Cancel out side-slip proportional to grip value
        Vector2 rightDir     = transform.right;
        float   lateralSpeed = Vector2.Dot(rb.linearVelocity, rightDir);
        Vector2 lateralVel   = rightDir * lateralSpeed;
        rb.linearVelocity   -= lateralVel * currentGrip;

        // ── Apply visual tilt to the sprite ───────────────────────────────
        // We rotate the visual child (or the whole object if no child).
        // Here we bake it into the rigidbody rotation offset via the sprite.
        // If you have a separate sprite child, rotate that instead.
        // For simplicity, we apply a small rotation on top of physics rotation:
        transform.rotation = Quaternion.Euler(0f, 0f, rb.rotation + visualTiltAngle);
    }

    // ── Public helpers for future extensions ──────────────────────────────

    /// <summary>Returns 0-1 where 1 is full drift.</summary>
    public float DriftIntensity => isDrifting ? 1f - currentGrip : 0f;

    /// <summary>Current speed as 0-1 fraction of maxSpeed.</summary>
    public float SpeedFraction => rb.linearVelocity.magnitude / maxSpeed;
}
