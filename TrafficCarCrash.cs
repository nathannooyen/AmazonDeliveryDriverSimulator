// ============================================================
//  TrafficCarCrash.cs  — car-vs-car explosion on traffic prefabs
// ============================================================
//  HOW TO ADD THIS SCRIPT IN UNITY
//  ──────────────────────────────────────────────────────────
//  1. Right-click your Scripts folder → Create → C# Script.
//     Name it exactly "TrafficCarCrash".
//
//  2. Open your TrafficCar prefab (double-click it in the
//     Project window).
//
//  3. Add this script as a component on the prefab root.
//     Unity may ask you to add a Rigidbody2D — it already
//     exists on TrafficCar prefabs, so dismiss that prompt.
//
//  4. REQUIRED ON THE TRAFFIC CAR PREFAB:
//     - Rigidbody2D set to Kinematic (already there).
//     - A 2D Collider (non-trigger) — already there.
//     - The tag "TrafficCar" — already set.
//
//  5. EXPLOSION SETUP:
//     - Make sure ExplosionSpawner is in your scene and has
//       a ParticleSystem prefab assigned.
//     - No extra wiring needed — TrafficCarCrash calls
//       ExplosionSpawner.Instance.Spawn() automatically.
//
//  6. WHAT IT COVERS:
//     - Traffic car hitting ANOTHER traffic car → explosion.
//     - Traffic car being hit BY the player        → explosion
//       is also triggered here, so you get a burst on the
//       traffic car side in addition to TrafficCollision's
//       burst on the player side (effectively two bursts
//       slightly offset → looks great).
//     - You can disable player-side triggering via the
//       "React To Player" toggle in the Inspector if you
//       only want one explosion per crash.
//
//  7. COOLDOWN
//     Each traffic car tracks its own cooldown independently,
//     so a pile-up still produces one explosion per car pair.
// ============================================================

using UnityEngine;

public class TrafficCarCrash : MonoBehaviour
{
    [Header("Cooldown")]
    [Tooltip("Minimum seconds between explosions on this car. " +
             "Prevents rapid repeat triggers during a slow scrape.")]
    public float collisionCooldown = 1.0f;

    [Header("Triggers")]
    [Tooltip("Play explosion when this traffic car hits another traffic car.")]
    public bool reactToTraffic = true;

    [Tooltip("Play explosion when this traffic car is hit by the player car. " +
             "Disable this if you want only TrafficCollision.cs to show the effect.")]
    public bool reactToPlayer = true;

    private float lastCollisionTime = -999f;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        bool hitTraffic = reactToTraffic && collision.gameObject.CompareTag("TrafficCar");
        bool hitPlayer  = reactToPlayer  && collision.gameObject.CompareTag("Player");

        if (!hitTraffic && !hitPlayer) return;
        if (Time.time - lastCollisionTime < collisionCooldown) return;

        lastCollisionTime = Time.time;

        // Use the contact point for a precise burst origin
        Vector2 contactPoint = collision.contacts.Length > 0
            ? collision.contacts[0].point
            : (Vector2)transform.position;

        if (ExplosionSpawner.Instance != null)
            ExplosionSpawner.Instance.Spawn(contactPoint);

        Debug.Log($"{gameObject.name} crash explosion at {contactPoint} " +
                  $"(hit: {collision.gameObject.name})");
    }
}