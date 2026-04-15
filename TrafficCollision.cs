// ============================================================
//  TrafficCollision.cs  (v2 — explosion at contact point)
// ============================================================
//  HOW TO ADD THIS SCRIPT IN UNITY
// ============================================================
//  1. Right-click your Scripts folder → Create → C# Script.
//     Name it exactly "TrafficCollision".
//
//  2. Attach it to your Player car GameObject
//     (the same one that has CarController and Rigidbody2D).
//
//  3. REQUIRED ON THE PLAYER CAR:
//     - Rigidbody2D   (already there from CarController)
//     - A 2D Collider (e.g. BoxCollider2D) set to NON-trigger.
//
//  4. REQUIRED ON TRAFFIC CARS:
//     - The tag "TrafficCar" (create via Tags & Layers if needed).
//     - A 2D Collider (non-trigger).
//     - A Rigidbody2D (set to Kinematic for TrafficCar script).
//
//  5. EXPLOSION SETUP:
//     - Add ExplosionSpawner.cs to any persistent GameObject
//       (e.g. GameManager or a dedicated "FX Manager").
//     - Assign your explosion ParticleSystem prefab to it.
//     - The explosion plays automatically at the contact point.
//     - You can ALSO assign a "Crash Effect" ParticleSystem
//       directly to this component for a secondary local effect.
//
//  6. Tune the penalty values and cooldown in the Inspector.
//
//  7. NOTE: This script only fires when the PLAYER hits a
//     traffic car. Traffic-vs-traffic collisions use the
//     separate TrafficCarCrash script on traffic car prefabs.
// ============================================================

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
    [Tooltip("Optional secondary ParticleSystem attached to the player car. " +
             "The shared ExplosionSpawner effect plays regardless of this field.")]
    public ParticleSystem crashEffect;

    private float lastCollisionTime = -999f;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("TrafficCar")) return;
        if (Time.time - lastCollisionTime < collisionCooldown) return;

        lastCollisionTime = Time.time;

        // ── Penalty ────────────────────────────────────────────────────────
        if (GameManager.Instance != null)
            GameManager.Instance.ApplyCollisionPenalty(scorePenalty, moneyPenalty);

        // ── Explosion at contact point ─────────────────────────────────────
        Vector2 contactPoint = collision.contacts.Length > 0
            ? collision.contacts[0].point
            : (Vector2)transform.position;

        if (ExplosionSpawner.Instance != null)
            ExplosionSpawner.Instance.Spawn(contactPoint);

        // ── Optional secondary effect on the player car ────────────────────
        if (crashEffect != null)
            crashEffect.Play();

        // ── Destroy the traffic car and respawn a replacement ──────────────
        if (TrafficSpawner.Instance != null)
            TrafficSpawner.Instance.RespawnCar(collision.gameObject);
        else
            Destroy(collision.gameObject);

        Debug.Log($"Hit a traffic car at {contactPoint}! Penalty applied, traffic car destroyed.");
    }
}