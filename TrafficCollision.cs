using UnityEngine;

public class TrafficCollision : MonoBehaviour
{
    [Header("Penalties")]
    public int scorePenalty = 150;
    public int moneyPenalty = 10;

    [Header("Cooldown")]
    [Tooltip("Seconds before another collision can trigger a penalty. Prevents rapid repeated hits.")]
    public float collisionCooldown = 1.5f;

    [Header("Feedback (optional)")]
    public ParticleSystem crashEffect;

    private float lastCollisionTime = -999f;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("TrafficCar")) return;
        if (Time.time - lastCollisionTime < collisionCooldown) return;

        lastCollisionTime = Time.time;

        // Deduct score and money via GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ApplyCollisionPenalty(scorePenalty, moneyPenalty);
        }

        if (crashEffect != null)
            crashEffect.Play();

        Debug.Log("Hit a traffic car! Penalty applied.");
    }
}