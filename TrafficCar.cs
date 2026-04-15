// ============================================================
//  TrafficCar.cs  (v6 — direction-correct spawning)
// ============================================================
//
//  HOW TO ADD A NEW SCRIPT IN UNITY
//  ──────────────────────────────────────────────────────────
//  1. In the Project window, right-click the folder you want
//     (e.g. Assets/Scripts) → Create → C# Script.
//     Name it exactly the same as the class inside it
//     (e.g. "TrafficCar"). Capitalisation matters.
//
//  2. Double-click the new file to open it in VS Code or Rider.
//     Select ALL the default contents and replace them with
//     the code you want to use.
//
//  3. Save the file (Ctrl+S / Cmd+S) and switch back to Unity.
//     Wait for the status bar at the bottom to finish compiling.
//     Fix any red errors in the Console before continuing.
//
//  4. Attach the script to a GameObject:
//     • Select the GameObject in the Hierarchy.
//     • Drag the script from the Project window onto the Inspector,
//       OR click "Add Component" in the Inspector and search by name.
//
//  5. If [RequireComponent] is present, Unity auto-adds dependencies
//     (e.g. Rigidbody2D). You don't need to add those manually.
//
//  6. Fill in any public / [SerializeField] fields that appear in
//     the Inspector before pressing Play.
//
//  PREFAB WORKFLOW (for spawned objects like traffic cars):
//  ─────────────────────────────────────────────────────────
//  1. Build the GameObject in the scene (add components, set values).
//  2. Drag it from the Hierarchy into your Project window (Assets folder).
//     Unity creates a Prefab — the original in the scene becomes a
//     Prefab Instance (shown with a blue cube icon).
//  3. To edit the Prefab later, double-click it in the Project window.
//     Changes propagate to all instances in every scene.
//  4. If you only want to change one instance, select it in the
//     Hierarchy and edit it there. Use "Overrides → Apply All" in
//     the Inspector if you later want those changes on the Prefab too.
//
//  THIS SCRIPT SPECIFICALLY
//  ──────────────────────────
//  You don't set up TrafficCar manually — TrafficSpawner
//  instantiates it and calls AssignRouteStaggered() automatically.
//
//  If you want to place a car in the scene by hand (without the
//  spawner), drag a TrafficRoute into the "Route" field and the
//  car will pick a random starting position between waypoints at
//  runtime, oriented in the route's forward direction.
//
//  PREFAB SETUP
//  ────────────
//  1. Create your traffic car GameObject (Sprite + Collider2D
//     + the "TrafficCar" tag).
//  2. Attach this script. Unity auto-adds a Rigidbody2D.
//  3. Set the Rigidbody2D to Kinematic, freeze Z rotation.
//  4. Leave "Route" empty — TrafficSpawner fills it in.
//  5. Save as a Prefab in your Project window.
//  6. Drag that Prefab into TrafficSpawner's "Car Prefab" field.
//
//  PHYSICS COLLISION MATRIX (prevents traffic-vs-traffic penalty)
//  ───────────────────────────────────────────────────────────────
//  Edit → Project Settings → Physics 2D → Layer Collision Matrix.
//  Make sure "TrafficCar vs TrafficCar" is CHECKED (collide) but
//  ensure TrafficCollision.cs only fires on the Player's collider,
//  not on traffic-vs-traffic hits (it already does — it checks the
//  "TrafficCar" tag on the OTHER object, so two traffic cars bumping
//  each other will NOT apply a score penalty to the player).
//
//  TRAFFIC AVOIDANCE SETUP
//  ────────────────────────
//  1. In Unity's Layers dropdown (top-right of the Editor),
//     add a new layer called "TrafficCar".
//  2. Select your TrafficCar prefab and set its Layer to "TrafficCar".
//  3. On this component, set the "Traffic Layer" mask to "TrafficCar".
//  4. Tune "Look Ahead Distance" — 1.2 works well for most car sizes.
//     Increase it if cars still clip each other at high speed.
//  5. "Slow Down Distance" controls how far out a car starts braking
//     before fully stopping. Set it to 1.5–2× lookAheadDistance.
//
//  WHAT CHANGED IN v6 — DIRECTION-CORRECT SPAWNING
//  ────────────────────────────────────────────────────────────
//
//  PROBLEM: v5 detected head-on collisions after the fact and
//  resolved them by respawning one car, which could cause visible
//  pops and wasted frames.
//
//  FIX: Cars are now always spawned facing along the route's
//  waypoint order (0 → 1 → 2 → …).  Because every car on a
//  given route travels in the same direction, head-on conflicts
//  between same-route cars are impossible by construction.
//
//  For two-way traffic on a road, create two separate
//  TrafficRoute objects with opposite waypoint orders, one per
//  lane.  See TrafficRoute.cs for details.
//
//  The head-on detection and yielding logic has been removed.
//  Deadlock recovery (stuck timer → respawn) is still present
//  for cars that get wedged on geometry.
// ============================================================

using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class TrafficCar : MonoBehaviour
{
    [Header("Route")]
    [Tooltip("Assigned at runtime by TrafficSpawner. You can also set it manually for hand-placed cars.")]
    public TrafficRoute route;

    [Header("Movement")]
    public float speed = 4f;

    [Tooltip("How close the car needs to get before advancing to the next waypoint.")]
    public float waypointReachDistance = 0.2f;

    [Header("Traffic Avoidance")]
    [Tooltip("How far ahead (in world units) to scan for another traffic car before stopping.")]
    public float lookAheadDistance = 1.2f;

    [Tooltip("Distance at which the car starts slowing down before fully stopping. " +
             "Should be >= lookAheadDistance for a smooth deceleration.")]
    public float slowDownDistance = 2.0f;

    [Tooltip("Radius of the forward sweep used to detect nearby cars. " +
             "Match this roughly to half the width of your car sprite.")]
    public float avoidanceRadius = 0.3f;

    [Tooltip("Layer mask for traffic cars. Create a 'TrafficCar' layer, assign it to " +
             "your prefab, then select it here.")]
    public LayerMask trafficLayer;

    [Header("Deadlock Recovery")]
    [Tooltip("Seconds the car must be nearly stopped before it gets respawned. " +
             "Lower = faster recovery but less realistic queuing.")]
    public float stuckTimeout = 3f;

    [Header("Separation (pileup prevention)")]
    [Tooltip("Cars closer than this along the route tangent will be nudged apart. " +
             "Set this to roughly the length of your car sprite.")]
    public float minSeparationDistance = 0.8f;

    [Tooltip("Strength of the tangent-projected push when two cars are too close. " +
             "Tune upward if cars still stack; tune down if movement looks jittery.")]
    public float separationForce = 4f;

    [Header("Path Clamping")]
    [Tooltip("Maximum distance the car is allowed to drift from its route segment " +
             "before being snapped back. Prevents cars from leaving the road.")]
    public float maxOffPathDistance = 0.05f;

    // ── Runtime state ──────────────────────────────────────────────────────
    private Transform[]  waypoints;
    private int          currentWaypoint;
    private float        currentSpeed;
    private Rigidbody2D  rb;
    private Collider2D   ownCollider;

    // Deadlock detection — based on actual distance travelled
    private float   stuckTimer;
    private Vector2 lastStuckCheckPos;

    // Set to true when this car has requested a respawn — prevents
    // double-requests during the frame before destruction.
    private bool pendingRespawn;

    // Reusable buffer for overlap checks — avoids per-frame allocations
    private static readonly Collider2D[] _overlapBuffer = new Collider2D[8];

    // ── Unity messages ─────────────────────────────────────────────────────

    private void Awake()
    {
        rb              = GetComponent<Rigidbody2D>();
        ownCollider     = GetComponent<Collider2D>();
        rb.bodyType     = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.constraints  = RigidbodyConstraints2D.FreezeRotation;
    }

    private void Start()
    {
        currentSpeed      = speed;
        lastStuckCheckPos = rb.position;
        if (route != null)
            InitialiseFromRoute(route);
    }

    private void FixedUpdate()
    {
        if (waypoints == null || waypoints.Length == 0) return;
        if (pendingRespawn) return; // Waiting for destruction — do nothing

        Transform target      = waypoints[currentWaypoint];
        Vector3   waypointDir = (target.position - transform.position).normalized;

        // ── Forward avoidance cast ─────────────────────────────────────────
        float distToBlocking = DistanceToCarAhead(waypointDir);

        float targetSpeed;
        if (distToBlocking <= lookAheadDistance)
            targetSpeed = 0f;
        else if (distToBlocking <= slowDownDistance)
            targetSpeed = speed * Mathf.InverseLerp(lookAheadDistance, slowDownDistance, distToBlocking);
        else
            targetSpeed = speed;

        // ── Deadlock recovery (stuck timer) ────────────────────────────────
        float movedSinceCheck = Vector2.Distance(rb.position, lastStuckCheckPos);
        if (movedSinceCheck < speed * 0.02f * Time.fixedDeltaTime)
        {
            stuckTimer += Time.fixedDeltaTime;
        }
        else
        {
            stuckTimer = Mathf.Max(0f, stuckTimer - Time.fixedDeltaTime * 2f);
        }
        lastStuckCheckPos = rb.position;

        if (stuckTimer >= stuckTimeout)
        {
            RequestRespawn("deadlock timeout");
            return;
        }

        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, speed * 4f * Time.fixedDeltaTime);

        // ── Move along route ───────────────────────────────────────────────
        Vector2 newPos = rb.position + (Vector2)(waypointDir * currentSpeed * Time.fixedDeltaTime);
        rb.MovePosition(newPos);

        // ── Rotate to face travel direction ────────────────────────────────
        if (currentSpeed > 0.05f)
        {
            float angle = Mathf.Atan2(waypointDir.y, waypointDir.x) * Mathf.Rad2Deg - 90f;
            rb.MoveRotation(Mathf.MoveTowardsAngle(rb.rotation, angle, 300f * Time.fixedDeltaTime));
        }

        // ── Route-projected separation ─────────────────────────────────────
        PushAlongRouteTangent(waypointDir);

        // ── Path clamping ──────────────────────────────────────────────────
        ClampToRouteSegment();

        // ── Advance waypoint ───────────────────────────────────────────────
        if (Vector3.Distance(transform.position, target.position) <= waypointReachDistance)
        {
            AdvanceWaypoint();
        }
    }

    // ── Public API ─────────────────────────────────────────────────────────

    /// <summary>
    /// Called by TrafficSpawner (or manually) to assign a route and
    /// place the car at a random position between two waypoints,
    /// facing in the route's forward direction.
    /// </summary>
    public void AssignRoute(TrafficRoute newRoute)
    {
        route = newRoute;
        InitialiseFromRoute(newRoute);
    }

    /// <summary>
    /// Called by TrafficSpawner when distributing cars across routes.
    /// Places each car at a staggered position so they never stack,
    /// always oriented in the route's intended travel direction.
    /// </summary>
    public void AssignRouteStaggered(TrafficRoute newRoute, int spawnIndex, int stride = 3)
    {
        route     = newRoute;
        waypoints = newRoute.GetWaypoints();

        if (waypoints.Length == 0)
        {
            Debug.LogWarning($"{gameObject.name}: Route '{newRoute.name}' has no waypoints.");
            return;
        }

        int segmentCount = route.loop ? waypoints.Length : waypoints.Length - 1;
        if (segmentCount <= 0) segmentCount = 1;

        int   segmentIndex = (spawnIndex * stride) % segmentCount;
        int   wpA          = segmentIndex;
        int   wpB          = (segmentIndex + 1) % waypoints.Length;

        float t = Random.Range(0.05f, 0.95f);
        transform.position = Vector3.Lerp(waypoints[wpA].position, waypoints[wpB].position, t);
        currentWaypoint    = wpB;

        // Always face from wpA → wpB (the route's forward direction)
        Vector3 dir = (waypoints[wpB].position - waypoints[wpA].position).normalized;
        if (dir.sqrMagnitude > 0.001f)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    /// <summary>
    /// Updates the waypoint the car is currently driving toward.
    /// Called by TrafficSpawner.EnsureSpawnSeparation() after
    /// repositioning a car to a different route segment.
    /// </summary>
    public void SetCurrentWaypoint(int waypointIndex)
    {
        currentWaypoint = waypointIndex;
    }

    // ── Private helpers ────────────────────────────────────────────────────

    /// <summary>Advance to the next waypoint, wrapping on looping routes.</summary>
    private void AdvanceWaypoint()
    {
        currentWaypoint++;
        if (currentWaypoint >= waypoints.Length)
        {
            if (route != null && route.loop)
                currentWaypoint = 0;
            else
                currentWaypoint = waypoints.Length - 1;
        }
    }

    /// <summary>
    /// Disables this car's collider immediately (so it can't hit the
    /// player during its final frame) and asks TrafficSpawner to
    /// destroy it and spawn a replacement on a different route.
    /// </summary>
    private void RequestRespawn(string reason)
    {
        if (pendingRespawn) return;
        pendingRespawn = true;

        if (ownCollider != null)
            ownCollider.enabled = false;

        // Spawn an explosion at the car's position before it disappears
        if (ExplosionSpawner.Instance != null)
            ExplosionSpawner.Instance.Spawn(transform.position);

        Debug.Log($"{gameObject.name}: Requesting respawn — {reason}");

        if (TrafficSpawner.Instance != null)
            TrafficSpawner.Instance.RespawnCar(gameObject);
        else
            Destroy(gameObject);
    }

    private void InitialiseFromRoute(TrafficRoute r)
    {
        waypoints = r.GetWaypoints();

        if (waypoints.Length == 0)
        {
            Debug.LogWarning($"{gameObject.name}: Route '{r.name}' has no waypoints.");
            return;
        }

        int segmentCount = r.loop ? waypoints.Length : Mathf.Max(1, waypoints.Length - 1);
        int segIndex     = Random.Range(0, segmentCount);
        int wpA          = segIndex;
        int wpB          = (segIndex + 1) % waypoints.Length;

        float t = Random.Range(0.05f, 0.95f);
        transform.position = Vector3.Lerp(waypoints[wpA].position, waypoints[wpB].position, t);
        currentWaypoint    = wpB;

        // Face along the segment direction (wpA → wpB), not toward
        // the car's interpolated position.  This guarantees the car
        // is oriented in the route's intended travel direction.
        Vector3 dir = (waypoints[wpB].position - waypoints[wpA].position).normalized;
        if (dir.sqrMagnitude > 0.001f)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    /// <summary>
    /// Dual-direction circle-cast with dynamic origin offset.
    /// Returns the distance to the nearest traffic car ahead.
    /// </summary>
    private float DistanceToCarAhead(Vector3 waypointDir)
    {
        float dist = Mathf.Infinity;

        float halfExtent   = ownCollider != null ? ownCollider.bounds.extents.y : 0.5f;
        float originOffset = halfExtent + avoidanceRadius + 0.05f;

        Vector2 facingDir  = transform.up;
        Vector2 castOrigin = (Vector2)transform.position + facingDir * originOffset;

        // Cast 1: current facing direction
        RaycastHit2D hitFacing = Physics2D.CircleCast(
            castOrigin, avoidanceRadius,
            facingDir, slowDownDistance, trafficLayer);

        if (hitFacing.collider != null && hitFacing.collider.gameObject != gameObject)
        {
            if (hitFacing.distance < dist)
                dist = hitFacing.distance;
        }

        // Cast 2: toward next waypoint (handles pre-turn detection)
        Vector2 wpDir2D = (Vector2)waypointDir;
        if (wpDir2D.sqrMagnitude > 0.001f)
        {
            Vector2 castOriginWP = (Vector2)transform.position + wpDir2D.normalized * originOffset;

            RaycastHit2D hitWaypoint = Physics2D.CircleCast(
                castOriginWP, avoidanceRadius,
                wpDir2D, slowDownDistance, trafficLayer);

            if (hitWaypoint.collider != null && hitWaypoint.collider.gameObject != gameObject)
            {
                if (hitWaypoint.distance < dist)
                    dist = hitWaypoint.distance;
            }
        }

        return dist;
    }

    /// <summary>
    /// Route-projected separation. Finds all traffic cars within
    /// minSeparationDistance and pushes THIS car away along the
    /// route tangent only.
    /// </summary>
    private void PushAlongRouteTangent(Vector3 waypointDir)
    {
        int count = Physics2D.OverlapCircleNonAlloc(
            transform.position, minSeparationDistance,
            _overlapBuffer, trafficLayer);

        Vector2 tangent = ((Vector2)waypointDir).normalized;
        if (tangent.sqrMagnitude < 0.001f) return;

        for (int i = 0; i < count; i++)
        {
            Collider2D other = _overlapBuffer[i];
            if (other == null || other.gameObject == gameObject) continue;

            Vector2 away = (Vector2)(transform.position - other.transform.position);
            float   dist = away.magnitude;

            if (dist < 0.001f)
            {
                away = -tangent;
                dist = 0.001f;
            }

            float tangentDot = Vector2.Dot(away.normalized, tangent);
            Vector2 projectedPush = tangent * tangentDot;

            float strength = separationForce
                           * (1f - Mathf.Clamp01(dist / minSeparationDistance))
                           * Time.fixedDeltaTime;

            rb.MovePosition(rb.position + projectedPush * strength);
        }
    }

    /// <summary>
    /// Snaps the car back to the nearest point on its current route segment.
    /// </summary>
    private void ClampToRouteSegment()
    {
        if (waypoints == null || waypoints.Length < 2) return;

        int prevWP = currentWaypoint - 1;
        if (prevWP < 0)
            prevWP = (route != null && route.loop) ? waypoints.Length - 1 : 0;

        Vector2 a = waypoints[prevWP].position;
        Vector2 b = waypoints[currentWaypoint].position;
        Vector2 segDir = b - a;
        float   segLen = segDir.magnitude;

        if (segLen < 0.001f) return;

        Vector2 carPos = rb.position;
        float   t      = Vector2.Dot(carPos - a, segDir / segLen) / segLen;
        t = Mathf.Clamp01(t);

        Vector2 closestPoint = a + segDir * t;
        float   offDist      = Vector2.Distance(carPos, closestPoint);

        if (offDist > maxOffPathDistance)
        {
            rb.MovePosition(Vector2.Lerp(carPos, closestPoint, 0.5f));
        }
    }

    // ── Editor helpers ─────────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        Vector3 forward = transform.up;
        // Yellow = hard stop distance
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + forward * lookAheadDistance, avoidanceRadius);
        // Orange = slow-down distance
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position + forward * slowDownDistance, avoidanceRadius);
        // Cyan = separation radius
        Gizmos.color = new Color(0f, 0.9f, 0.9f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, minSeparationDistance);
    }
}