using System;
using UnityEngine;
using Pathfinding;

public class PhysicsNavigator : MonoBehaviour
{
    #region Components

    private Seeker seeker;

    #endregion
    
    #region Navigation 
    
    [Header("Navigation Parameters")]
    
    private Path path;
    
    public float nextWaypointDistance = 3;
    private int currentWaypoint = 0;
    public float repathRate = 0.5f;
    private float lastRepath = float.NegativeInfinity;
    public bool reachedEndOfPath;
    
    #endregion
    
    #region MonoBehaviour Callbacks

    private void Awake()
    {
        seeker = GetComponent<Seeker>();
    }

    #endregion
    
    #region Navigation Methods
    
    private void OnPathComplete(Path p)
    {
        p.Claim(this);
        
        if (!p.error) 
        {
            if (path != null) path.Release(this);
            path = p;
            // Reset the waypoint counter so that we start to move towards the first point in the path
            currentWaypoint = 0;
        } 
        else 
        {
            p.Release(this);
        }
    }

    public Vector3 CalculateDirectionByPath(Path p)
    {
        seeker.StartPath(p, OnPathComplete);
        
        if (path == null)
        {
            // We have no path to follow yet, so don't do anything
            return Vector3.zero;
        }

        return DirectionFromCurrentPath();
    }

    public Vector3 CalculateDirectionToTarget(Vector3 targetPosition)
    {
        if (Time.time > lastRepath + repathRate && seeker.IsDone())
        {
            lastRepath = Time.time;

            // Start a new path to the targetPosition, call the the OnPathComplete function
            // when the path has been calculated (which may take a few frames depending on the complexity)
            seeker.StartPath(transform.position, targetPosition, OnPathComplete);
        }

        if (path == null) 
        {
            // We have no path to follow yet, so don't do anything
            return Vector3.zero;
        }

        return DirectionFromCurrentPath();
    }

    private Vector3 DirectionFromCurrentPath()
    {
        // Check in a loop if we are close enough to the current waypoint to switch to the next one.
        // We do this in a loop because many waypoints might be close to each other and we may reach
        // several of them in the same frame.
        reachedEndOfPath = false;
        // The distance to the next waypoint in the path
        float distanceToWaypoint;
        while (!reachedEndOfPath && currentWaypoint < path.vectorPath.Count)
        {
            distanceToWaypoint = Vector3.Distance(transform.position, path.vectorPath[currentWaypoint]);
            if (distanceToWaypoint < nextWaypointDistance) 
            {
                // Check if there is another waypoint or if we have reached the end of the path
                if (currentWaypoint + 1 < path.vectorPath.Count) 
                {
                    currentWaypoint++;
                } 
                else 
                {
                    // Set a status variable to indicate that the agent has reached the end of the path.
                    // You can use this to trigger some special code if your game requires that.
                    reachedEndOfPath = true;
                    break;
                }
            }
            else 
            {
                break;
            }
        }

        // Direction to the next waypoint
        // Normalize it so that it has a length of 1 world unit
        Vector3 dir = (path.vectorPath[currentWaypoint] - transform.position).normalized;

        return dir;
    }
    
    #endregion
}
