using System;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour
{
    [Header("Waypoints")]
    public HashSet<Waypoint> allWaypoints;

    [Header("Scan Settings")]
    public float vertCheckDist = 0.5f; // height of ray origin
    public float searchRadius = 100f; // max distance to consider
    public Waypoint currentWaypoint; // waypoint to ignore
    public float reachDistance = 1.0f;
    private HashSet<Waypoint> visibleWaypoints = new();

    void Start()
    {
        allWaypoints = new HashSet<Waypoint>(FindObjectsOfType<Waypoint>());
        //Debug.Log($"All waypoints: {allWaypoints.Count}");
    }

    void Update()
    {
        if (currentWaypoint == null || ReachedCurrentWaypoint())
        {
            var wayPoints = FindVisibleWaypoints();
            //Debug.Log($"Choosing from {wayPoints.Count} visible waypoints.");
            if (wayPoints.Count > 0)
            {
                int index = UnityEngine.Random.Range(0, wayPoints.Count);
                foreach (var wp in wayPoints)
                {
                    if (index == 0)
                    {
                        currentWaypoint = wp;
                        //.Log($"New waypoint: {currentWaypoint.name}");
                        break;
                    }
                    index--;
                }
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
        // Simply wraps the same logic as FindVisibleWaypoints
        return FindVisibleWaypoints();
    }

    bool ReachedCurrentWaypoint()
    {
        // distance check (fast + reliable)
        Vector3 a = transform.position;
        Vector3 b = currentWaypoint.transform.position;
        a.y = 0f; // optional: ignore height
        b.y = 0f;

        return Vector3.Distance(a, b) <= reachDistance;
    }

    private HashSet<Waypoint> FindVisibleWaypoints()
    {
        visibleWaypoints.Clear();
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

            // RaycastAll to see everything along the ray
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
                //Debug.Log($"Ray from {name} to {wp.name} blocked by {hit.collider.name}");
                //Debug.DrawLine(origin, hit.point, Color.red, 0.1f);
                break;
            }

            if (!blocked)
            {
                visibleWaypoints.Add(wp);
                //Debug.DrawLine(origin, target, Color.green, 0.1f); // visible
            }
        }

       //Debug.Log($"Visible waypoints: {visibleWaypoints.Count}");
        return visibleWaypoints;
    }
}
