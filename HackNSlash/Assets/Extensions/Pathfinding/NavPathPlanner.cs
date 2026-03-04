using System;
using System.Collections.Generic;
using Pathfinding;
using UnityEngine;

namespace Extensions.Pathfinding
{
    /** <summary>
     * Handles asynchronous A* path planning via the Aron Granberg pathfinding package.
     * Owns the <c>Seeker</c> interaction: requests new paths on a configurable cadence,
     * tracks waypoint progress, and exposes the single output the motor needs —
     * a normalised XZ direction toward the next waypoint.
     * </summary>
     * <remarks>
     * Responsibilities of this class:
     * <list type="bullet">
     *   <item>Request paths through <see cref="Seeker"/>.</item>
     *   <item>Advance the waypoint index as the agent moves.</item>
     *   <item>Report arrival and path-failure states.</item>
     * </list>
     * It intentionally does <b>not</b> move the agent — that is <see cref="NavMotor"/>'s job.
     * </remarks>
     */
    [RequireComponent(typeof(Seeker))]
    [DisallowMultipleComponent]
    public class NavPathPlanner : MonoBehaviour
    {
        #region Configuration

        [Tooltip("Tuning data. If null, sensible defaults are used.")]
        public NavConfig config;

        #endregion

        #region Public State

        /** <summary>True once the agent has arrived within <see cref="NavConfig.arrivalRadius"/> of the destination.</summary> */
        public bool ReachedDestination { get; private set; }

        /** <summary>True when the last path request returned an error.</summary> */
        public bool PathFailed { get; private set; }

        /** <summary>True while a path request is in flight (async).</summary> */
        public bool IsCalculatingPath => _seeker != null && !_seeker.IsDone();

        /** <summary>World-space position of the current target set via <see cref="SetDestination"/>.</summary> */
        public Vector3 Destination { get; private set; }

        /** <summary>Current A* path. Null until the first successful request.</summary> */
        public Path CurrentPath { get; private set; }

        #endregion

        #region Private State

        private Seeker _seeker;
        private int _waypointIndex;
        private float _repathTimer;

        // Stall detection: track if the agent is stuck on a path that exists but isn't working.
        private Vector3 _lastStallCheckPosition;
        private float _stallTimer;

        // Config fallbacks.
        private float RepathRate => config != null ? config.repathRate : 0.35f;
        private float WaypointRadius => config != null ? config.waypointAcceptanceRadius : 0.6f;
        private float ArrivalRadius => config != null ? config.arrivalRadius : 0.25f;
        private float StallTimeout => config != null ? config.stallTimeout : 1.5f;
        private float StallMoveThreshold => config != null ? config.stallMoveThreshold : 0.15f;

        #endregion

        #region Events

        /** <summary>Raised when a new valid path has been computed.</summary> */
        public event Action OnPathUpdated;

        /** <summary>Raised when the agent arrives at the destination.</summary> */
        public event Action OnArrived;

        #endregion

        #region MonoBehaviour

        private void Awake()
        {
            _seeker = GetComponent<Seeker>();
            _lastStallCheckPosition = transform.position;
        }

        private void OnDisable()
        {
            _seeker.CancelCurrentPathRequest();
            ReleasePath();
        }

        private void Update()
        {
            // Only repath if we have a destination to navigate to.
            if (Destination == Vector3.zero) return;

            _repathTimer += Time.deltaTime;
            if (_repathTimer >= RepathRate && _seeker.IsDone())
            {
                _repathTimer = 0f;
                RequestPath(Destination);
            }

            // Stall detection: if we have a path but are barely moving, force an immediate repath.
            // This handles cases where the A* path leads into a corner or the agent is wedged.
            if (CurrentPath != null && !ReachedDestination)
            {
                float moved = Vector3.Distance(transform.position, _lastStallCheckPosition);
                if (moved < StallMoveThreshold)
                {
                    _stallTimer += Time.deltaTime;
                    if (_stallTimer >= StallTimeout && _seeker.IsDone())
                    {
                        _stallTimer = 0f;
                        _lastStallCheckPosition = transform.position;
                        // Force a fresh path — also resets the waypoint index.
                        ReleasePath();
                        RequestPath(Destination);
                    }
                }
                else
                {
                    _stallTimer = 0f;
                    _lastStallCheckPosition = transform.position;
                }
            }
            else
            {
                _stallTimer = 0f;
                _lastStallCheckPosition = transform.position;
            }
        }

        #endregion

        #region Public API

        /** <summary>
         * Begin navigating toward <paramref name="destination"/>.
         * Issues an immediate path request; subsequent requests fire every
         * <see cref="NavConfig.repathRate"/> seconds.
         * </summary>
         */
        public void SetDestination(Vector3 destination)
        {
            Destination = destination;
            ReachedDestination = false;
            PathFailed = false;
            _repathTimer = RepathRate; // trigger immediate repath
        }

        /** <summary>
         * Cancel active navigation and release the current path.
         * </summary>
         */
        public void Stop()
        {
            _seeker.CancelCurrentPathRequest();
            ReleasePath();
            ReachedDestination = false;
        }

        /** <summary>
         * Returns the normalised XZ direction from the agent's current position
         * toward the next waypoint in the path.
         * Returns <c>Vector3.zero</c> if no path is available or the agent has arrived.
         * </summary>
         */
        public Vector3 GetSteeringDirection()
        {
            if (CurrentPath == null || ReachedDestination) return Vector3.zero;

            List<Vector3> waypoints = CurrentPath.vectorPath;
            if (waypoints == null || waypoints.Count == 0) return Vector3.zero;

            AdvanceWaypoints(waypoints);

            if (ReachedDestination) return Vector3.zero;

            Vector3 toWaypoint = waypoints[_waypointIndex] - transform.position;
            toWaypoint.y = 0f;
            return toWaypoint.sqrMagnitude > 0.0001f ? toWaypoint.normalized : Vector3.zero;
        }

        /** <summary>
         * Returns the straight-line XZ distance to <see cref="Destination"/>.
         * </summary>
         */
        public float DistanceToDestination()
        {
            Vector3 delta = Destination - transform.position;
            delta.y = 0f;
            return delta.magnitude;
        }

        #endregion

        #region Private Helpers

        private void RequestPath(Vector3 destination)
        {
            if (_seeker == null) return;
            _seeker.StartPath(transform.position, destination, OnPathComplete);
        }

        private void OnPathComplete(Path p)
        {
            if (p.error)
            {
                p.Release(this);
                PathFailed = true;
                return;
            }

            ReleasePath();
            p.Claim(this);
            CurrentPath = p;
            _waypointIndex = 0;
            PathFailed = false;
            OnPathUpdated?.Invoke();
        }

        private void ReleasePath()
        {
            if (CurrentPath != null)
            {
                CurrentPath.Release(this);
                CurrentPath = null;
            }
        }

        private void AdvanceWaypoints(List<Vector3> waypoints)
        {
            // Check final arrival first.
            Vector3 toEnd = waypoints[^1] - transform.position;
            toEnd.y = 0f;
            if (toEnd.sqrMagnitude <= ArrivalRadius * ArrivalRadius)
            {
                ReachedDestination = true;
                OnArrived?.Invoke();
                return;
            }

            // Advance through intermediate waypoints.
            while (_waypointIndex < waypoints.Count - 1)
            {
                Vector3 toWp = waypoints[_waypointIndex] - transform.position;
                toWp.y = 0f;
                if (toWp.sqrMagnitude <= WaypointRadius * WaypointRadius)
                    _waypointIndex++;
                else
                    break;
            }
        }

        #endregion

        #region Gizmos
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (CurrentPath == null) return;

            List<Vector3> pts = CurrentPath.vectorPath;
            if (pts == null || pts.Count < 2) return;

            Gizmos.color = PathFailed ? Color.red : Color.cyan;
            for (int i = 0; i < pts.Count - 1; i++)
                Gizmos.DrawLine(pts[i], pts[i + 1]);

            // Current waypoint sphere.
            if (_waypointIndex < pts.Count)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(pts[_waypointIndex], 0.2f);
            }

            // Destination marker.
            Gizmos.color = ReachedDestination ? Color.green : Color.white;
            Gizmos.DrawWireSphere(Destination, ArrivalRadius);
        }
#endif
        #endregion
    }
}

