// ============================================================
//  TrafficSpawner.cs
// ============================================================
//
//  HOW TO SET UP IN UNITY
//  ──────────────────────
//  1. Create an empty GameObject in the Hierarchy.
//     Name it "TrafficSpawner".
//
//  2. Add this script as a component.
//
//  3. INSPECTOR FIELDS TO FILL
//     ┌─────────────────────────────────────────────────────┐
//     │ Car Prefab      → drag your TrafficCar prefab here  │
//     │ Routes          → drag all TrafficRoute objects here │
//     │ Difficulty      → 1 (loaded from save automatically) │
//     │ Cars Per Diff   → extra cars per difficulty level    │
//     │                   (e.g. 1)                          │
//     │ Base Car Count  → cars at difficulty 1 (e.g. 2)     │
//     │ Max Cars        → hard ceiling (e.g. 10)            │
//     │ Spawn Stride    → waypoint offset between cars on   │
//     │                   the same route (default 3)        │
//     │ Min Player Dist → how far from the player a         │
//     │                   respawned car must appear (def 15)│
//     └─────────────────────────────────────────────────────┘
//
//  4. HOW DIFFICULTY SCALES
//     totalCars = Mathf.Clamp(baseCars + (difficulty-1) * carsPerDiff,
//                             1, maxCars)
//     At difficulty 1 with baseCars=2 and carsPerDiff=1:
//       D1 → 2 cars,  D2 → 3,  D3 → 4 … up to maxCars.
//
//  5. HOW SPAWN DISTRIBUTION WORKS
//     Cars are assigned to routes in round-robin order (0,1,2,0,1,2…)
//     instead of randomly, so they spread across routes before any
//     route receives a second car.
//     Within each route, cars start at staggered waypoints
//     (index × spawnStride) so they never stack at the same position.
//     All cars face along the route's waypoint order, preventing
//     head-on conflicts between same-route cars.
//
//  6. HOOKING UP TO SCORE
//     In GameManager.OnDeliveryComplete(), call:
//         TrafficSpawner.Instance.SetDifficulty(newDifficulty);
//     This despawns/spawns cars to match the new count.
//
//  7. TWO-WAY TRAFFIC
//     To have cars going in both directions on a road, create
//     two TrafficRoute objects with opposite waypoint orders
//     (one per lane) and add both to the Routes list.
//     See TrafficRoute.cs for setup details.
// ============================================================

using System.Collections.Generic;
using UnityEngine;

public class TrafficSpawner : MonoBehaviour
{
    public static TrafficSpawner Instance { get; private set; }

    [Header("Prefab & Routes")]
    public GameObject        carPrefab;
    public List<TrafficRoute> routes = new List<TrafficRoute>();

    [Header("Difficulty Scaling")]
    [Tooltip("When enabled, the difficulty value set here is used on Start instead of the saved value. " +
             "Useful for testing. Disable in production so difficulty persists between sessions.")]
    public bool overrideStartDifficulty = false;

    [Tooltip("Starting difficulty. Only used if Override Start Difficulty is enabled; " +
             "otherwise the saved value is loaded automatically.")]
    public int difficulty             = 1;
    public int baseCarCount           = 2;
    public int carsPerDifficultyLevel = 1;
    public int maxCars                = 10;

    [Header("Spawn Distribution")]
    [Tooltip("How many waypoints to skip between cars spawned on the same route. " +
             "Higher = more spread. 3 works well for most route lengths.")]
    public int spawnStride = 3;

    [Tooltip("Minimum distance between any two spawned cars. If a new car spawns " +
             "within this radius of an existing one, it is repositioned to a safe " +
             "spot further along the route. Should be larger than the car's " +
             "lookAheadDistance + avoidanceRadius to prevent instant deadlock.")]
    public float minSpawnSeparation = 3f;

    [Tooltip("Maximum attempts to find a valid spawn position before giving up. " +
             "Prevents infinite loops when a route is too short for the car count.")]
    public int maxRepositionAttempts = 10;

    private readonly List<GameObject> activeCars = new List<GameObject>();

    private int spawnIndex = 0;

    // ── Unity messages ─────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (overrideStartDifficulty)
        {
            difficulty = Mathf.Max(1, difficulty);
            Debug.Log($"TrafficSpawner: Using Inspector difficulty = {difficulty} (override enabled)");
        }
        else
        {
            difficulty = SaveManager.LoadDifficulty();
            Debug.Log($"TrafficSpawner: Loaded difficulty = {difficulty} (from save)");
        }

        SpawnCarsForCurrentDifficulty();
    }

    // ── Public API ─────────────────────────────────────────────────────────

    public void SetDifficulty(int newDifficulty)
    {
        difficulty = Mathf.Max(1, newDifficulty);
        SaveManager.SaveDifficulty(difficulty);
        SpawnCarsForCurrentDifficulty();
    }

    // ── Private helpers ────────────────────────────────────────────────────

    private int TargetCarCount()
    {
        return Mathf.Clamp(
            baseCarCount + (difficulty - 1) * carsPerDifficultyLevel,
            1, maxCars);
    }

    private void SpawnCarsForCurrentDifficulty()
    {
        if (carPrefab == null)
        {
            Debug.LogWarning("TrafficSpawner: No Car Prefab assigned.");
            return;
        }
        if (routes == null || routes.Count == 0)
        {
            Debug.LogWarning("TrafficSpawner: No routes assigned.");
            return;
        }

        CleanNullEntries();

        int target = TargetCarCount();

        while (activeCars.Count < target)
            SpawnOneCar();

        while (activeCars.Count > target)
        {
            int last = activeCars.Count - 1;
            if (activeCars[last] != null) Destroy(activeCars[last]);
            activeCars.RemoveAt(last);
        }

        Debug.Log($"TrafficSpawner: Difficulty {difficulty} → {activeCars.Count} cars active.");
    }

    private void SpawnOneCar()
    {
        TrafficRoute chosenRoute = routes[spawnIndex % routes.Count];

        if (chosenRoute.WaypointCount == 0)
        {
            Debug.LogWarning($"TrafficSpawner: Route '{chosenRoute.name}' has no waypoints — skipping.");
            spawnIndex++;
            return;
        }

        GameObject car = Instantiate(carPrefab, Vector3.zero, Quaternion.identity);
        TrafficCar tc  = car.GetComponent<TrafficCar>();

        if (tc != null)
        {
            tc.AssignRouteStaggered(chosenRoute, spawnIndex, spawnStride);
            EnsureSpawnSeparation(car, tc, chosenRoute);
        }
        else
        {
            Debug.LogWarning("TrafficSpawner: Car Prefab is missing a TrafficCar component.");
        }

        activeCars.Add(car);
        spawnIndex++;
    }

    private void EnsureSpawnSeparation(GameObject car, TrafficCar tc, TrafficRoute route)
    {
        Transform[] waypoints = route.GetWaypoints();
        if (waypoints.Length < 2) return;

        int segmentCount = route.loop ? waypoints.Length : waypoints.Length - 1;

        for (int attempt = 0; attempt < maxRepositionAttempts; attempt++)
        {
            if (!IsTooCloseToExisting(car))
                return;

            int segIndex = Random.Range(0, segmentCount);
            int wpA = segIndex;
            int wpB = (segIndex + 1) % waypoints.Length;

            float t = Random.Range(0.1f, 0.9f);
            car.transform.position = Vector3.Lerp(
                waypoints[wpA].position, waypoints[wpB].position, t);

            // Face along the segment direction (wpA → wpB)
            Vector3 dir = (waypoints[wpB].position - waypoints[wpA].position).normalized;
            if (dir.sqrMagnitude > 0.001f)
            {
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
                car.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }

            // Update the car's target waypoint to match its new segment
            tc.SetCurrentWaypoint(wpB);
        }

        if (IsTooCloseToExisting(car))
            Debug.LogWarning($"TrafficSpawner: Could not find a safe spawn position for " +
                             $"'{car.name}' on route '{route.name}' after {maxRepositionAttempts} attempts.");
    }

    private bool IsTooCloseToExisting(GameObject newCar)
    {
        float sqrMinDist = minSpawnSeparation * minSpawnSeparation;

        foreach (GameObject existing in activeCars)
        {
            if (existing == null || existing == newCar) continue;

            float sqrDist = (existing.transform.position - newCar.transform.position).sqrMagnitude;
            if (sqrDist < sqrMinDist)
                return true;
        }

        return false;
    }

    // ── Respawn API (called by TrafficCar on deadlock) ─────────────────

    [Header("Respawn Safety")]
    [Tooltip("Minimum distance from the player that a respawned car must appear.")]
    public float minPlayerDistance = 15f;

    [Tooltip("Maximum attempts to find a spawn position far enough from the player.")]
    public int maxPlayerDistanceAttempts = 10;

    public void RespawnCar(GameObject oldCar)
    {
        activeCars.Remove(oldCar);
        Destroy(oldCar);

        CleanNullEntries();

        int target = TargetCarCount();
        if (activeCars.Count < target)
        {
            SpawnOneCarAwayFromPlayer();
        }

        Debug.Log($"TrafficSpawner: Respawned car. {activeCars.Count}/{target} cars active.");
    }

    private void SpawnOneCarAwayFromPlayer()
    {
        if (carPrefab == null || routes == null || routes.Count == 0) return;

        Vector3 playerPos = Vector3.zero;
        bool    hasPlayer = false;
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerPos = playerObj.transform.position;
            hasPlayer = true;
        }

        List<int> routeOrder = new List<int>();
        for (int i = 0; i < routes.Count; i++) routeOrder.Add(i);
        ShuffleList(routeOrder);

        GameObject bestCar      = null;
        float      bestDistance  = -1f;

        for (int attempt = 0; attempt < maxPlayerDistanceAttempts; attempt++)
        {
            int routeIdx = routeOrder[attempt % routeOrder.Count];
            TrafficRoute chosenRoute = routes[routeIdx];
            if (chosenRoute.WaypointCount == 0) continue;

            GameObject car = Instantiate(carPrefab, Vector3.zero, Quaternion.identity);
            TrafficCar tc  = car.GetComponent<TrafficCar>();

            if (tc != null)
            {
                tc.AssignRouteStaggered(chosenRoute, spawnIndex + attempt, spawnStride);
            }

            float distToPlayer = hasPlayer
                ? Vector3.Distance(car.transform.position, playerPos)
                : float.MaxValue;

            bool tooCloseToOther = IsTooCloseToExisting(car);

            if (distToPlayer >= minPlayerDistance && !tooCloseToOther)
            {
                activeCars.Add(car);
                spawnIndex++;
                return;
            }

            if (distToPlayer > bestDistance)
            {
                if (bestCar != null) Destroy(bestCar);
                bestCar     = car;
                bestDistance = distToPlayer;
            }
            else
            {
                Destroy(car);
            }
        }

        if (bestCar != null)
        {
            activeCars.Add(bestCar);
            spawnIndex++;
            Debug.LogWarning($"TrafficSpawner: Respawned car at {bestDistance:F1}m from player " +
                             $"(wanted {minPlayerDistance}m+). Best available position used.");
        }
        else if (activeCars.Count < TargetCarCount())
        {
            SpawnOneCar();
        }
    }

    private void CleanNullEntries()
    {
        activeCars.RemoveAll(car => car == null);
    }

    private static void ShuffleList(List<int> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
}