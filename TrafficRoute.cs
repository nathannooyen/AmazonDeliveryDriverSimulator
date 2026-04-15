// ============================================================
//  TrafficRoute.cs
// ============================================================
//
//  HOW TO SET UP IN UNITY
//  ──────────────────────
//  1. Create an empty GameObject in the Hierarchy.
//     Name it something like "Route_MainStreet".
//
//  2. Add this script as a component:
//     Select it → Inspector → Add Component → search "TrafficRoute".
//
//  3. Add waypoints as CHILD GameObjects:
//     Right-click "Route_MainStreet" → Create Empty.
//     Name each child "WP_0", "WP_1", etc. — the order in the
//     Hierarchy is what matters, not the names.
//     Position each child along the road.
//
//  4. Repeat for every road in your scene.
//
//  5. Drag all Route GameObjects into TrafficSpawner's "Routes" list.
//
//  TIP: Enable Gizmos in the Scene view to see the route as a
//  coloured line with numbered spheres at each waypoint.
//
//  DIRECTION MATTERS
//  ─────────────────
//  All traffic cars travel in waypoint-index order (0 → 1 → 2 → …).
//  This means the order of child GameObjects in the Hierarchy
//  defines the travel direction.  Cars are oriented to face
//  along their segment at spawn time, so head-on conflicts
//  between cars on the SAME route are impossible.
//
//  TWO-WAY ROADS
//  ─────────────
//  If you want traffic flowing in both directions on the same
//  road, create TWO TrafficRoute objects — one with waypoints
//  ordered left-to-right (or top-to-bottom), and a second with
//  waypoints in the REVERSE order.  Offset them slightly to
//  represent opposite lanes.
// ============================================================

using UnityEngine;

public class TrafficRoute : MonoBehaviour
{
    [Tooltip("Loop back from the last waypoint to the first.")]
    public bool loop = true;

    [Tooltip("Colour used to draw this route in the Scene view.")]
    public Color gizmoColour = Color.cyan;

    // ── Public API ─────────────────────────────────────────────────────────

    /// <summary>All direct child Transforms as an ordered waypoint array.</summary>
    public Transform[] GetWaypoints()
    {
        Transform[] pts = new Transform[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
            pts[i] = transform.GetChild(i);
        return pts;
    }

    /// <summary>Number of waypoints on this route.</summary>
    public int WaypointCount => transform.childCount;

    /// <summary>
    /// Returns the forward direction of the segment from waypoint
    /// segmentIndex to segmentIndex+1 (wrapping on loops).
    /// Used by TrafficSpawner to orient cars at spawn time so
    /// they always face along the route's intended travel direction.
    /// </summary>
    public Vector3 GetSegmentDirection(int segmentIndex)
    {
        if (transform.childCount < 2) return Vector3.up;

        int a = segmentIndex;
        int b = (segmentIndex + 1) % transform.childCount;
        Vector3 dir = (transform.GetChild(b).position - transform.GetChild(a).position).normalized;
        return dir.sqrMagnitude > 0.001f ? dir : Vector3.up;
    }

    // ── Gizmos ─────────────────────────────────────────────────────────────

    private void OnDrawGizmos()
    {
        if (transform.childCount < 2) return;
        Gizmos.color = gizmoColour;
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform cur  = transform.GetChild(i);
            Transform next = (i + 1 < transform.childCount)
                             ? transform.GetChild(i + 1)
                             : (loop ? transform.GetChild(0) : null);
            Gizmos.DrawSphere(cur.position, 0.15f);
            if (next != null) Gizmos.DrawLine(cur.position, next.position);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        for (int i = 0; i < transform.childCount; i++)
            UnityEditor.Handles.Label(
                transform.GetChild(i).position + Vector3.up * 0.35f, $"[{i}]");
    }
#endif
}