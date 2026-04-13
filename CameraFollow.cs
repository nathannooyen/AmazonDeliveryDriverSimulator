// CameraFollow.cs
// Attach this to your Main Camera GameObject.
// Drag your car GameObject into the "Target" field in the Inspector.

using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("The Transform the camera will follow (your car).")]
    public Transform target;

    [Header("Follow Settings")]
    [Tooltip("How quickly the camera position catches up. Lower = lazier follow.")]
    [Range(1f, 20f)]
    public float smoothSpeed = 5f;

    [Tooltip("Offset from the target in world space. Z should stay negative (e.g. -10).")]
    public Vector3 offset = new Vector3(0f, 0f, -10f);

    [Header("Rotation")]
    [Tooltip("Whether the camera rotates to match the car's facing direction.")]
    public bool followRotation = true;

    [Tooltip("How quickly the camera rotation catches up. Lower = more lag.")]
    [Range(1f, 20f)]
    public float rotationSmoothSpeed = 4f;

    [Tooltip("Only rotate the camera when the car exceeds this speed. Prevents spinning when nearly stopped.")]
    public float minSpeedToRotate = 0.5f;

    [Header("Look-Ahead (optional)")]
    [Tooltip("Peek ahead in the car's travel direction. 0 = disabled.")]
    [Range(0f, 5f)]
    public float lookAheadDistance = 1.5f;

    private Rigidbody2D targetRb;
    private float currentAngle;

    void Start()
    {
        if (target != null)
        {
            targetRb = target.GetComponent<Rigidbody2D>();
            // Initialise to the car's starting angle so there's no snap on play
            currentAngle = target.eulerAngles.z;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // ── Position ───────────────────────────────────────────────────────
        Vector3 lookAhead = Vector3.zero;
        if (targetRb != null && lookAheadDistance > 0f)
            lookAhead = (Vector3)targetRb.linearVelocity.normalized * lookAheadDistance;

        Vector3 desiredPosition = target.position + offset + lookAhead;

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            smoothSpeed * Time.deltaTime
        );

        // ── Rotation ───────────────────────────────────────────────────────
        if (followRotation)
        {
            float speed = targetRb != null ? targetRb.linearVelocity.magnitude : 0f;

            // Only chase the car's angle when it's actually moving
            if (speed >= minSpeedToRotate)
            {
                float targetAngle = target.eulerAngles.z;

                // LerpAngle handles the 0/360 wrap-around correctly
                currentAngle = Mathf.LerpAngle(
                    currentAngle,
                    targetAngle,
                    rotationSmoothSpeed * Time.deltaTime
                );
            }

            transform.rotation = Quaternion.Euler(0f, 0f, currentAngle);
        }
        else
        {
            transform.rotation = Quaternion.identity;
        }
    }
}