// ============================================================
//  House.cs
// ============================================================
using UnityEngine;

public class House : MonoBehaviour
{
    [Header("Delivery Settings")]
    [SerializeField] private string carTag = "Player";
    [SerializeField] private bool requireStop = false;
    [SerializeField] private float maxDeliverySpeed = 1f;

    [Header("Active Timing")]
    [Tooltip("Minimum time the delivery request stays active.")]
    [SerializeField] private float minActiveTime = 15f;
    [Tooltip("Maximum time the delivery request stays active.")]
    [SerializeField] private float maxActiveTime = 45f;
    [Tooltip("How many seconds before despawning should it start blinking?")]
    [SerializeField] private float blinkDuration = 5f;
    [Tooltip("How fast the sprite blinks (smaller is faster).")]
    [SerializeField] private float blinkInterval = 0.2f;

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Success Feedback (optional)")]
    [Tooltip("How long to wait before allowing another delivery (cooldown).")]
    [SerializeField] private float successDisplayTime = 1.5f;
    [SerializeField] private ParticleSystem successEffect;

    // ── State ──────────────────────────────────────────────────────────────
    private bool deliveryCompleted = false;
    private bool wantsDelivery = false;
    private bool carInZone = false;
    private Rigidbody2D carRb;

    private Coroutine timeoutCoroutine;

    /// <summary>True while this house is waiting for a delivery.</summary>
    public bool WantsDelivery => wantsDelivery;

    // ── Unity messages ─────────────────────────────────────────────────────

    private void Start()
    {
        // Hide the package at the start of the game
        if (spriteRenderer != null) spriteRenderer.enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(carTag))
        {
            carInZone = true;
            carRb = other.GetComponent<Rigidbody2D>();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(carTag))
        {
            carInZone = false;
            carRb = null;
        }
    }

    private void Update()
    {
        if (deliveryCompleted || !wantsDelivery || !carInZone)
            return;

        bool speedOk = !requireStop || (carRb != null && carRb.linearVelocity.magnitude <= maxDeliverySpeed);

        if (speedOk)
            CompleteDelivery();
    }

    // ── Delivery logic ─────────────────────────────────────────────────────

    /// <summary>Called by DeliveryManager to mark this house as wanting a delivery.</summary>
    public void RequestDelivery()
    {
        deliveryCompleted = false;
        wantsDelivery = true;

        // Show the package!
        if (spriteRenderer != null) spriteRenderer.enabled = true;

        // Start the countdown timer for despawning
        if (timeoutCoroutine != null) StopCoroutine(timeoutCoroutine);
        timeoutCoroutine = StartCoroutine(DeliveryTimeoutRoutine());
    }

    private System.Collections.IEnumerator DeliveryTimeoutRoutine()
    {
        float activeDuration = Random.Range(minActiveTime, maxActiveTime);
        float solidTime = Mathf.Max(0, activeDuration - blinkDuration);

        // 1. Wait for the solid time
        yield return new WaitForSeconds(solidTime);

        // 2. Blinking phase
        float blinkTimer = 0f;
        while (blinkTimer < blinkDuration)
        {
            if (spriteRenderer != null)
                spriteRenderer.enabled = !spriteRenderer.enabled;

            yield return new WaitForSeconds(blinkInterval);
            blinkTimer += blinkInterval;
        }

        // 3. Timeout reached! Despawn the request.
        TimeoutDelivery();
    }

    private void CompleteDelivery()
    {
        if (timeoutCoroutine != null) StopCoroutine(timeoutCoroutine);

        // Immediately hide the package once delivered
        if (spriteRenderer != null) spriteRenderer.enabled = false;

        deliveryCompleted = true;
        wantsDelivery = false;
        Debug.Log("Delivery completed at " + gameObject.name);

        if (GameManager.Instance != null)
            GameManager.Instance.OnDeliveryComplete();

        StartCoroutine(OnDeliverySuccess());
    }

    private void TimeoutDelivery()
    {
        // Hide the package if the player missed it
        if (spriteRenderer != null) spriteRenderer.enabled = false;

        deliveryCompleted = true;
        wantsDelivery = false;
        Debug.Log("Delivery timed out (despawned) at " + gameObject.name);
    }

    /// <summary>
    /// Handles the success state. Add your animation / VFX / sound here.
    /// </summary>
    private System.Collections.IEnumerator OnDeliverySuccess()
    {
        // ── Play effects ───────────────────────────────────────────────────
        if (successEffect != null)
            successEffect.Play();

        // ── Wait before the house can be used again ────────────────────────
        yield return new WaitForSeconds(successDisplayTime);
    }

    // ── Public API ─────────────────────────────────────────────────────────

    public bool IsDeliveryComplete() => deliveryCompleted;

    public void ResetDelivery()
    {
        if (timeoutCoroutine != null) StopCoroutine(timeoutCoroutine);

        // Ensure it stays hidden during the reset phase
        if (spriteRenderer != null) spriteRenderer.enabled = false;

        deliveryCompleted = false;
        wantsDelivery = false;
    }
}