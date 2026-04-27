// ============================================================
//  DeliveryManager.cs
// ============================================================
//  HOW TO ADD THIS SCRIPT IN UNITY
// ============================================================
//  1. Right-click your Scripts folder → Create → C# Script.
//     Name it exactly "DeliveryManager".
//
//  2. Create an empty GameObject in the Hierarchy.
//     Name it "DeliveryManager".
//
//  3. Drag the script onto that GameObject (or use Add Component).
//
//  4. You do NOT need to assign any Houses manually — the script
//     finds all House objects in the scene automatically at Start.
// ============================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeliveryManager : MonoBehaviour
{
    [Header("Spawning Limits")]
    [Tooltip("How often (in seconds) the game tries to spawn a new delivery.")]
    public float spawnDelay = 2f;

    [Tooltip("The maximum number of deliveries allowed on the map at the exact same time.")]
    public int maxActiveDeliveries = 3;

    [Tooltip("Seconds to wait after a delivery is completed before THAT specific house can be picked again.")]
    public float postDeliveryDelay = 1.5f;

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
            // 1. Count how many houses currently have an active delivery
            int activeCount = 0;
            foreach (House h in allHouses)
            {
                if (h.WantsDelivery) activeCount++;
            }

            // 2. If we haven't hit our maximum limit, try to spawn another one!
            if (activeCount < maxActiveDeliveries)
            {
                House chosen = PickRandomEligibleHouse();

                if (chosen != null)
                {
                    // Start a separate background process just for this house
                    StartCoroutine(HandleHouseLifecycle(chosen));
                }
            }

            // 3. Wait a few seconds before trying to spawn again
            yield return new WaitForSeconds(spawnDelay);
        }
    }

    private IEnumerator HandleHouseLifecycle(House house)
    {
        // Turn the house "On"
        house.RequestDelivery();
        Debug.Log($"DeliveryManager: Delivery requested at {house.gameObject.name}");

        // Wait here until the player actually delivers to THIS specific house
        yield return new WaitUntil(() => house.IsDeliveryComplete());

        // Once delivered, wait for the cooldown timer
        yield return new WaitForSeconds(postDeliveryDelay);

        // Reset the house so it is empty and ready to be picked again in the future
        house.ResetDelivery();
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a random house that is empty and ready for a delivery.
    /// </summary>
    private House PickRandomEligibleHouse()
    {
        List<House> eligible = new List<House>();

        foreach (House h in allHouses)
        {
            // A house is eligible if it does NOT want a delivery right now, 
            // and it is NOT currently waiting on its cooldown timer.
            if (!h.WantsDelivery && !h.IsDeliveryComplete())
            {
                eligible.Add(h);
            }
        }

        if (eligible.Count == 0) return null;

        return eligible[Random.Range(0, eligible.Count)];
    }

    // ── Public API ─────────────────────────────────────────────────────────

    public void RestartDeliveries()
    {
        if (managerCoroutine != null) StopCoroutine(managerCoroutine);

        StopAllCoroutines(); // Stop all individual house timers too

        foreach (House h in allHouses) h.ResetDelivery();

        managerCoroutine = StartCoroutine(ManageDeliveries());
    }
}