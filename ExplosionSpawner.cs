// ============================================================
//  ExplosionSpawner.cs  — pooled explosion utility
// ============================================================
//  HOW TO SET UP IN UNITY
//  ──────────────────────
//  1. Create a ParticleSystem GameObject in your scene:
//       GameObject → Effects → Particle System.
//     Name it "ExplosionEffect".
//
//  2. Configure the ParticleSystem to look like an explosion:
//       • Start Lifetime  : 0.5 – 0.8
//       • Start Speed     : 3 – 6
//       • Start Size      : 0.2 – 0.5
//       • Emission → Burst: Count 15–30, once at time 0
//       • Shape           : Circle, Radius 0.1
//       • Color over Lifetime: orange → red → transparent
//       • Stop Action     : None  (the pool reuses the object)
//     Disable "Play on Awake" and "Looping".
//
//  3. Drag the ParticleSystem into the "Explosion Prefab" field
//     on this component (which lives on a persistent GameObject
//     such as your GameManager or a dedicated "FX Manager").
//
//  4. Call  ExplosionSpawner.Instance.Spawn(position)
//     from any script to play the effect.
//
//  NOTES
//  ─────
//  • Objects are reused from a pool — no per-crash allocations.
//  • Pool pre-warms on Start; size is configurable.
//  • The spawner is a singleton so any script can call it.
// ============================================================

using System.Collections.Generic;
using UnityEngine;

public class ExplosionSpawner : MonoBehaviour
{
    public static ExplosionSpawner Instance { get; private set; }

    [Header("Effect")]
    [Tooltip("Drag your explosion ParticleSystem prefab here.")]
    public ParticleSystem explosionPrefab;

    [Header("Pool")]
    [Tooltip("Number of explosion objects to pre-create. " +
             "Raise this if you expect many simultaneous explosions.")]
    public int poolSize = 6;

    private readonly Queue<ParticleSystem> pool = new Queue<ParticleSystem>();

    // ── Unity messages ─────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (explosionPrefab == null)
        {
            Debug.LogWarning("ExplosionSpawner: No explosion prefab assigned.");
            return;
        }

        for (int i = 0; i < poolSize; i++)
            pool.Enqueue(CreateInstance());
    }

    // ── Public API ─────────────────────────────────────────────────────────

    /// <summary>
    /// Play the explosion effect at the given world position.
    /// Safe to call even if no prefab is assigned (does nothing).
    /// </summary>
    public void Spawn(Vector3 worldPosition)
    {
        if (explosionPrefab == null) return;

        ParticleSystem ps = GetFromPool();
        ps.transform.position = worldPosition;
        ps.gameObject.SetActive(true);
        ps.Play();

        // Return to pool once the effect finishes
        StartCoroutine(ReturnAfterDelay(ps, ps.main.duration + ps.main.startLifetime.constantMax));
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private ParticleSystem GetFromPool()
    {
        // Re-use a pooled instance, or create a new one if the pool is dry
        while (pool.Count > 0)
        {
            ParticleSystem ps = pool.Dequeue();
            if (ps != null) return ps;
        }
        Debug.LogWarning("ExplosionSpawner: Pool exhausted — creating extra instance.");
        return CreateInstance();
    }

    private ParticleSystem CreateInstance()
    {
        ParticleSystem ps = Instantiate(explosionPrefab, transform);
        ps.gameObject.SetActive(false);
        return ps;
    }

    private System.Collections.IEnumerator ReturnAfterDelay(ParticleSystem ps, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (ps != null)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.gameObject.SetActive(false);
            pool.Enqueue(ps);
        }
    }
}