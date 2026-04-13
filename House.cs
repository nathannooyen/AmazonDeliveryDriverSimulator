using UnityEngine;

public class House : MonoBehaviour
{
    [Header("Delivery Settings")]
    [SerializeField] private string carTag = "Player";
    [SerializeField] private bool requireStop = false;
    [SerializeField] private float maxDeliverySpeed = 1f;

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite idleSprite;          // no delivery requested
    [SerializeField] private Sprite requestedSprite;     // delivery has been requested
    // deliveredSprite is intentionally removed — hook your animation/VFX here instead

    [Header("Success Feedback (optional)")]
    [Tooltip("How long to show the success state before the house returns to idle.")]
    [SerializeField] private float successDisplayTime = 1.5f;
    // Drop a ParticleSystem, Animator, etc. here and call it from OnDeliverySuccess()
    [SerializeField] private ParticleSystem successEffect;

    // ── State ──────────────────────────────────────────────────────────────
    private bool deliveryCompleted = false;
    private bool wantsDelivery = false;
    private bool carInZone = false;
    private Rigidbody2D carRb;

    /// <summary>True while this house is waiting for a delivery.</summary>
    public bool WantsDelivery => wantsDelivery;

    // ── Unity messages ─────────────────────────────────────────────────────

    private void Start()
    {
        UpdateSprite();
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
        wantsDelivery     = true;
        UpdateSprite();
    }

    private void CompleteDelivery()
    {
        deliveryCompleted = true;
        wantsDelivery     = false;
        Debug.Log("Delivery completed at " + gameObject.name);
        StartCoroutine(OnDeliverySuccess());
    }

    /// <summary>
    /// Handles the success state. Add your animation / VFX / sound here.
    /// The house stays in "success" for successDisplayTime, then returns to idle.
    /// </summary>
    private System.Collections.IEnumerator OnDeliverySuccess()
    {
        // ── Play effects ───────────────────────────────────────────────────
        if (successEffect != null)
            successEffect.Play();

        // TODO: trigger an Animator state, show a score pop-up, play a sound, etc.

        // ── Wait, then return to idle sprite ───────────────────────────────
        yield return new WaitForSeconds(successDisplayTime);
        UpdateSprite();
    }

    private void UpdateSprite()
    {
        if (spriteRenderer == null) return;
        spriteRenderer.sprite = wantsDelivery ? requestedSprite : idleSprite;
    }

    // ── Public API ─────────────────────────────────────────────────────────

    public bool IsDeliveryComplete() => deliveryCompleted;

    public void ResetDelivery()
    {
        deliveryCompleted = false;
        wantsDelivery     = false;
        UpdateSprite();
    }
}
