// BuildingCollision.cs
// Attach this to your Player car GameObject.
// The car must have a Rigidbody2D and a 2D Collider (non-trigger).
// The Buildings tilemap must have a Composite Collider 2D and the tag "Building".

using UnityEngine;

public class BuildingCollision : MonoBehaviour
{
    [Header("Penalties")]
    [Tooltip("Score deducted on impact with a building.")]
    public int scorePenalty = 200;

    [Tooltip("Money deducted on impact with a building.")]
    public int moneyPenalty = 15;

    [Header("Cooldown")]
    [Tooltip("Seconds before another collision can trigger a penalty. Prevents rapid repeated hits.")]
    public float collisionCooldown = 1.5f;

    [Header("Feedback (optional)")]
    public ParticleSystem crashEffect;

    private float lastCollisionTime = -999f;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Only respond to the Buildings tilemap
        if (!collision.gameObject.CompareTag("Building")) return;

        // Cooldown check — prevents multiple penalties in one crash
        if (Time.time - lastCollisionTime < collisionCooldown) return;

        lastCollisionTime = Time.time;

        if (GameManager.Instance != null)
            GameManager.Instance.ApplyCollisionPenalty(scorePenalty, moneyPenalty);

        if (crashEffect != null)
            crashEffect.Play();

        Debug.Log("Hit a building! Penalty applied.");
    }
}