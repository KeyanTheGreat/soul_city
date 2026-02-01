using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Movement : MonoBehaviour
{
    public Boolean isBeingDragged = false;
    public Boolean inProximity = false;

    public HashSet<Waypoint> allWaypoints;

    public float vertCheckDist = 0.5f;
    public float searchRadius = 100f; // max distance to consider
    public Waypoint currentWaypoint; //  the waypoint we are currently moving towards
    public float reachDistance = 1.0f;

    void Start()
    {
        allWaypoints = new HashSet<Waypoint>(FindObjectsOfType<Waypoint>());
        Debug.Log($"All waypoints: {allWaypoints.Count}");
    }

    void Update()
    {
        if (currentWaypoint == null || ReachedCurrentWaypoint())
        {
            var wayPoints = FindVisibleWaypoints().ToList();

            Debug.Log($"Choosing from {wayPoints.Count} visible waypoints.");
            if (wayPoints.Count > 0)
            {
                int index = UnityEngine.Random.Range(0, wayPoints.Count);
                currentWaypoint = wayPoints[index];
            }
        }
        if (currentWaypoint != null)
        {
            Vector3 direction = currentWaypoint.transform.position - transform.position;

            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }
    }

    public HashSet<Waypoint> Scan()
    {
        return FindVisibleWaypoints();
    }

    bool ReachedCurrentWaypoint()
    {
        Vector3 a = transform.position;
        Vector3 b = currentWaypoint.transform.position;
        a.y = 0f;
        b.y = 0f;

        return Vector3.Distance(a, b) <= reachDistance;
    }

    private HashSet<Waypoint> FindVisibleWaypoints()
    {
        HashSet<Waypoint> visibleWaypoints = new();
        Vector3 origin = transform.position + Vector3.up * vertCheckDist;

        foreach (var wp in allWaypoints)
        {
            if (wp == currentWaypoint)
                continue;
            Vector3 target = wp.transform.position + Vector3.up * 0.5f;
            Vector3 direction = target - origin;
            float distance = direction.magnitude;

            if (distance > searchRadius)
                continue;

            RaycastHit[] hits = Physics.RaycastAll(origin, direction.normalized, distance);

            bool blocked = false;
            foreach (var hit in hits)
            {
                if (hit.collider.GetComponentInParent<Waypoint>() == wp)
                    continue;

                if (
                    wp.transform.parent != null
                    && hit.collider.transform.IsChildOf(wp.transform.parent)
                )
                    continue;
                blocked = true;
                Debug.Log($"Ray from {name} to {wp.name} blocked by {hit.collider.name}");
                break;
            }

            if (!blocked)
            {
                visibleWaypoints.Add(wp);
            }
        }

        Debug.Log($"Visible waypoints: {visibleWaypoints.Count}");
        return visibleWaypoints;
    }
}
