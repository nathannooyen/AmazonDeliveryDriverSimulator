// ============================================================
//  HOW TO ADD A NEW SCRIPT IN UNITY
// ============================================================
//  1. In the Project window, right-click the folder you want
//     (e.g. Assets/Scripts) -> Create -> C# Script.
//     Name it exactly the same as the class inside it.
//
//  2. Double-click the file to open it in your editor (VS or Rider).
//     Replace the default contents with your script.
//
//  3. Attach the script to a GameObject:
//     - Select the GameObject in the Hierarchy.
//     - Drag the script from the Project window onto the Inspector,
//       OR click "Add Component" in the Inspector and search by name.
//
//  4. If the script requires other components (e.g. Rigidbody2D),
//     Unity will add them automatically when [RequireComponent] is present.
//
//  5. Set any public / [SerializeField] fields that appear in the Inspector.
//
//  For THIS project specifically:
//  - CarController   -> attach to your car GameObject (needs Rigidbody2D).
//  - House           -> attach to each house GameObject (needs a 2D Collider
//                       set to "Is Trigger", and a SpriteRenderer).
//  - DeliveryManager -> attach to any persistent/empty GameObject in the scene.
//                       All Houses are found automatically at runtime.
// ============================================================

using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public struct CarStats
{
    [Header("Movement")]
    public float accelerationForce;
    public float maxSpeed;
    public float baseTurnSpeed;

    [Tooltip("How quickly the car slows when no throttle is applied.\n" +
             "0 = coasts forever. Higher values = faster stop.")]
    public float coastingDrag;

    [Header("Speed-Sensitive Steering")]
    [Tooltip("X axis = speed fraction (0-1), Y axis = turn speed multiplier.")]
    public AnimationCurve turnSpeedCurve;

    [Header("Steering Feel")]
    [Tooltip("How quickly steering ramps up when a key is pressed. Higher = more instant.")]
    public float steerInSpeed;

    [Tooltip("How quickly steering fades out when the key is released. Higher = snappier stop.")]
    public float steerOutSpeed;

    [Header("Drift / Brake (Shift)")]
    public float normalGrip;
    public float driftGrip;
    public float driftSpeedThreshold;
    public float driftTransitionSpeed;
    public float driftTurnMultiplier;

    [Tooltip("Braking force applied while Shift is held.\n" +
             "Higher = harder stop. Stacks with reduced drift grip at speed for a skid feel.")]
    public float brakeForce;

    [Header("Feel")]
    public float visualTiltAmount;
    public float visualTiltSpeed;

    public static CarStats Default()
    {
        CarStats s;
        s.accelerationForce    = 15f;
        s.maxSpeed             = 10f;
        s.baseTurnSpeed        = 160f;
        s.coastingDrag         = 1f;

        Keyframe[] keys = new Keyframe[]
        {
            new Keyframe(0f,   1.6f),
            new Keyframe(0.3f, 1.2f),
            new Keyframe(0.6f, 0.8f),
            new Keyframe(1f,   0.4f),
        };
        s.turnSpeedCurve = new AnimationCurve(keys);

        s.steerInSpeed         = 12f;
        s.steerOutSpeed        = 8f;

        s.normalGrip           = 0.9f;
        s.driftGrip            = 0.3f;
        s.driftSpeedThreshold  = 4f;
        s.driftTransitionSpeed = 6f;
        s.driftTurnMultiplier  = 1.8f;
        s.brakeForce           = 18f;

        s.visualTiltAmount = 8f;
        s.visualTiltSpeed  = 8f;
        return s;
    }
}

[RequireComponent(typeof(Rigidbody2D))]
public class CarController : MonoBehaviour
{
    [Header("Stats (edit here or call ApplyUpgrade at runtime)")]
    public CarStats stats = CarStats.Default();

    private Rigidbody2D rb;
    private float currentGrip;
    private float targetGrip;
    private float visualTiltAngle;
    private bool  isDrifting;

    private float throttle;
    private float steer;         // raw input (-1, 0, 1)
    private float currentSteer;  // smoothed value used for rotation
    private bool  brakeInput;

    void Awake()
    {
        rb          = GetComponent<Rigidbody2D>();
        currentGrip = stats.normalGrip;
    }

    void Update()
    {
        // ── Throttle ───────────────────────────────────────────────────────
        throttle = 0f;
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
            throttle = 1f;
        else if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
            throttle = -1f;

        // ── Steering (raw input) ───────────────────────────────────────────
        steer = 0f;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            steer = -1f;
        else if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            steer = 1f;

        // ── Brake / Drift ──────────────────────────────────────────────────
        brakeInput = Keyboard.current.leftShiftKey.isPressed ||
                     Keyboard.current.rightShiftKey.isPressed;

        isDrifting = brakeInput && rb.linearVelocity.magnitude >= stats.driftSpeedThreshold;
        targetGrip = isDrifting ? stats.driftGrip : stats.normalGrip;

        currentGrip = Mathf.Lerp(currentGrip, targetGrip,
                                 Time.deltaTime * stats.driftTransitionSpeed);

        // ── Visual tilt ────────────────────────────────────────────────────
        float tiltTarget = -steer * stats.visualTiltAmount *
                           (rb.linearVelocity.magnitude / stats.maxSpeed);
        visualTiltAngle  = Mathf.Lerp(visualTiltAngle, tiltTarget,
                                      Time.deltaTime * stats.visualTiltSpeed);
    }

    void FixedUpdate()
    {
        float speed         = rb.linearVelocity.magnitude;
        float speedFraction = speed / stats.maxSpeed;

        // ── Smooth steering ────────────────────────────────────────────────
        // Ramps in quickly when a key is held, eases out when released.
        float steerRate = (Mathf.Abs(steer) > 0.01f) ? stats.steerInSpeed : stats.steerOutSpeed;
        currentSteer    = Mathf.MoveTowards(currentSteer, steer, steerRate * Time.fixedDeltaTime);

        // ── Turn speed ─────────────────────────────────────────────────────
        float curveMultiplier = stats.turnSpeedCurve.Evaluate(speedFraction);

        Vector2 velocityDir = rb.linearVelocity.normalized;
        float   driftAmount = Mathf.Abs(Vector2.Dot(velocityDir, transform.right));
        float   driftBoost  = isDrifting
                                  ? Mathf.Lerp(1f, stats.driftTurnMultiplier, driftAmount)
                                  : 1f;

        float finalTurnSpeed = stats.baseTurnSpeed * curveMultiplier * driftBoost;

        float movingDir     = Mathf.Sign(Vector2.Dot(rb.linearVelocity, transform.up));
        float rotationDelta = currentSteer * finalTurnSpeed
                              * Mathf.Clamp01(speedFraction)
                              * movingDir
                              * Time.fixedDeltaTime;

        rb.MoveRotation(rb.rotation + rotationDelta);

        // ── Acceleration ───────────────────────────────────────────────────
        if (throttle != 0f)
        {
            Vector2 force = transform.up * throttle * stats.accelerationForce;
            if (speed < stats.maxSpeed || throttle < 0f)
                rb.AddForce(force, ForceMode2D.Force);
        }

        // ── Braking (Shift held) ───────────────────────────────────────────
        if (brakeInput && speed > 0.01f)
        {
            float   forwardSpeed = Vector2.Dot(rb.linearVelocity, transform.up);
            Vector2 brakeForce   = -(Vector2)transform.up
                                   * Mathf.Sign(forwardSpeed)
                                   * stats.brakeForce
                                   * Time.fixedDeltaTime;

            Vector2 newVelocity = rb.linearVelocity + brakeForce;
            if (Mathf.Sign(Vector2.Dot(newVelocity, transform.up)) != Mathf.Sign(forwardSpeed))
                newVelocity -= (Vector2)transform.up * Vector2.Dot(newVelocity, transform.up);

            rb.linearVelocity = newVelocity;
        }

        // ── Coasting drag ──────────────────────────────────────────────────
        if (throttle == 0f && !brakeInput && stats.coastingDrag > 0f)
        {
            float forwardSpeed = Vector2.Dot(rb.linearVelocity, transform.up);
            float dragForce    = forwardSpeed * stats.coastingDrag;
            rb.linearVelocity -= (Vector2)transform.up * dragForce * Time.fixedDeltaTime;
        }

        // ── Grip / lateral damping ─────────────────────────────────────────
        Vector2 rightDir     = transform.right;
        float   lateralSpeed = Vector2.Dot(rb.linearVelocity, rightDir);
        rb.linearVelocity   -= rightDir * lateralSpeed * currentGrip;

        // ── Visual tilt ────────────────────────────────────────────────────
        transform.rotation = Quaternion.Euler(0f, 0f, rb.rotation + visualTiltAngle);
    }

    // ── Public API ─────────────────────────────────────────────────────────

    public void ApplyUpgrade(CarStats newStats) => stats = newStats;

    /// <summary>Returns 0-1 where 1 is full drift.</summary>
    public float DriftIntensity => isDrifting ? 1f - currentGrip : 0f;

    /// <summary>Current speed as 0-1 fraction of maxSpeed.</summary>
    public float SpeedFraction => rb.linearVelocity.magnitude / stats.maxSpeed;
}
