using UnityEngine;

public class TrafficCar : MonoBehaviour
{
    [Header("Waypoints")]
    public Transform[] waypoints;   // Drag your empty GameObjects in here
    public float speed = 4f;
    public bool loop = true;        // Loop back to start when finished

    [Header("Steering")]
    [Tooltip("How close the car needs to get before moving to the next waypoint.")]
    public float waypointReachDistance = 0.2f;

    private int currentWaypoint = 0;

    void Update()
    {
        if (waypoints.Length == 0) return;

        Transform target = waypoints[currentWaypoint];
        Vector3 dir = (target.position - transform.position).normalized;

        // Move toward waypoint
        transform.position += dir * speed * Time.deltaTime;

        // Rotate to face direction of travel
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            Quaternion.Euler(0, 0, angle),
            300f * Time.deltaTime
        );

        // Advance to next waypoint when close enough
        if (Vector3.Distance(transform.position, target.position) <= waypointReachDistance)
        {
            currentWaypoint++;
            if (currentWaypoint >= waypoints.Length)
                currentWaypoint = loop ? 0 : waypoints.Length - 1;
        }
    }
}