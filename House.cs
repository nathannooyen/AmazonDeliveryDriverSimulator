using UnityEngine;

public class House : MonoBehaviour
{
    [Header("Delivery Settings")]
    [SerializeField] private string carTag = "Player";
    [SerializeField] private bool requireStop = false;
    [SerializeField] private float maxDeliverySpeed = 1f;

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite notDeliveredSprite;
    [SerializeField] private Sprite deliveredSprite;

    private bool deliveryCompleted = false;
    private bool carInZone = false;
    private Rigidbody2D carRb;

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
        if (deliveryCompleted || !carInZone)
            return;

        if (!requireStop)
        {
            CompleteDelivery();
        }
        else if (carRb != null && carRb.linearVelocity.magnitude <= maxDeliverySpeed)
        {
            CompleteDelivery();
        }
    }

    private void CompleteDelivery()
    {
        deliveryCompleted = true;
        UpdateSprite();
        Debug.Log("Delivery completed at " + gameObject.name);
    }

    private void UpdateSprite()
    {
        if (spriteRenderer == null) return;

        spriteRenderer.sprite = deliveryCompleted 
            ? deliveredSprite 
            : notDeliveredSprite;
    }

    public bool IsDeliveryComplete()
    {
        return deliveryCompleted;
    }

    public void ResetDelivery()
    {
        deliveryCompleted = false;
        UpdateSprite();
    }
}