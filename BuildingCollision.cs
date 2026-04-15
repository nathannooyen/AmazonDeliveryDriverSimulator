// ============================================================
//  BuildingCollision.cs
// ============================================================
//  HOW TO ADD THIS SCRIPT IN UNITY
// ============================================================
//  1. Right-click your Scripts folder → Create → C# Script.
//     Name it exactly "BuildingCollision".
//
//  2. Attach it to your Player car GameObject
//     (the same one that has CarController and Rigidbody2D).
//
//  3. REQUIRED ON THE PLAYER CAR:
//     - Rigidbody2D   (already there from CarController)
//     - A 2D Collider (e.g. BoxCollider2D) set to NON-trigger.
//
//  4. REQUIRED ON THE BUILDINGS TILEMAP:
//     - A TilemapCollider2D with "Used By Composite" checked.
//     - A CompositeCollider2D component.
//     - A Rigidbody2D set to Static body type.
//     - The tag "Building" (create via Tags & Layers if needed).
//
//  5. Optionally drag a ParticleSystem into the "Crash Effect"
//     field for visual feedback on impact.
//
//  6. Tune the penalty values and cooldown in the Inspector.
// ============================================================

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