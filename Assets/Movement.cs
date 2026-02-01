using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Movement : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    HashSet<Waypoint> allWaypoints;

    void Start()
    {
        allWaypoints = new HashSet<Waypoint>(FindObjectsOfType<Waypoint>());
    }

    public float vertCheckDist = 2f;

    public int rayCount = 360;
    public float viewDistance = 100f;
    public LayerMask hitMask;

    public Waypoint currentWaypoint;

    void Update()
    {
        Debug.Log(Scan().Count);
    }

    public HashSet<Waypoint> Scan()
    {
        HashSet<Waypoint> visibleWaypoints = new HashSet<Waypoint>();
        Vector3 origin = transform.position + Vector3.up * vertCheckDist;

        foreach (var wp in allWaypoints)
        {
            if (wp == currentWaypoint)
                continue;

            Vector3 direction = wp.transform.position - origin;
            float distance = direction.magnitude;

            if (!Physics.Raycast(origin, direction.normalized, distance, hitMask))
            {
                visibleWaypoints.Add(wp);
            }
            else
            {
                Debug.DrawLine(origin, wp.transform.position, Color.red, 0.1f);
            }
        }

        return visibleWaypoints;
    }
}
