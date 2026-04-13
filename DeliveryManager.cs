using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Randomly selects a House to want a delivery.
/// - If no house currently needs a delivery (or there is only 1 house total),
///   waits <see cref="retryDelay"/> seconds before assigning a new one.
/// - Attach this to any persistent GameObject in the scene.
///   All House objects are found automatically on Start.
/// </summary>
public class DeliveryManager : MonoBehaviour
{
    [Header("Timing")]
    [Tooltip("Seconds to wait before assigning a new delivery when the queue is empty or only 1 house exists.")]
    public float retryDelay = 3f;

    [Tooltip("Seconds to wait after a delivery is completed before picking the next house.")]
    public float postDeliveryDelay = 1.5f;

    [Header("Optional")]
    [Tooltip("If true, multiple houses can want a delivery at the same time.")]
    public bool allowMultipleActiveDeliveries = false;

    // ── internals ──────────────────────────────────────────────────────────
    private House[] allHouses;
    private Coroutine managerCoroutine;

    void Start()
    {
        allHouses = FindObjectsByType<House>(FindObjectsSortMode.None);

        if (allHouses.Length == 0)
        {
            Debug.LogWarning("DeliveryManager: No House objects found in the scene.");
            return;
        }

        managerCoroutine = StartCoroutine(ManageDeliveries());
    }

    private IEnumerator ManageDeliveries()
    {
        while (true)
        {
            // ── Edge-case: single house or no pending delivery ─────────────
            bool edgeCase = allHouses.Length <= 1 || !AnyHouseWantsDelivery();

            if (edgeCase)
            {
                yield return new WaitForSeconds(retryDelay);
            }

            // ── Pick a random eligible house ───────────────────────────────
            House chosen = PickRandomEligibleHouse();

            if (chosen != null)
            {
                chosen.RequestDelivery();
                Debug.Log($"DeliveryManager: Delivery requested at {chosen.gameObject.name}");

                // ── Wait until that delivery is complete ───────────────────
                yield return new WaitUntil(() => chosen.IsDeliveryComplete());
                yield return new WaitForSeconds(postDeliveryDelay);

                // Reset the house so it can receive future deliveries
                chosen.ResetDelivery();
            }
            else
            {
                // All houses already have pending or completed deliveries
                yield return new WaitForSeconds(retryDelay);
            }
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    /// <summary>Returns true if at least one house currently wants a delivery.</summary>
    private bool AnyHouseWantsDelivery()
    {
        foreach (House h in allHouses)
            if (h.WantsDelivery) return true;
        return false;
    }

    /// <summary>
    /// Returns a random house that is eligible to receive a new delivery request:
    /// not already completed and (if multi-delivery is off) not already waiting.
    /// </summary>
    private House PickRandomEligibleHouse()
    {
        List<House> eligible = new List<House>();

        foreach (House h in allHouses)
        {
            bool alreadyDone    = h.IsDeliveryComplete();
            bool alreadyWaiting = allowMultipleActiveDeliveries ? false : h.WantsDelivery;

            if (!alreadyDone && !alreadyWaiting)
                eligible.Add(h);
        }

        if (eligible.Count == 0) return null;

        return eligible[Random.Range(0, eligible.Count)];
    }

    // ── Public API ─────────────────────────────────────────────────────────

    /// <summary>Force an immediate re-assignment (e.g. after resetting the level).</summary>
    public void RestartDeliveries()
    {
        if (managerCoroutine != null) StopCoroutine(managerCoroutine);
        foreach (House h in allHouses) h.ResetDelivery();
        managerCoroutine = StartCoroutine(ManageDeliveries());
    }
}