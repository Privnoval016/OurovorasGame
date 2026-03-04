using UnityEngine;

namespace Extensions.Pathfinding
{
    /** <summary>
     * ScriptableObject holding all tuning data for the navigation system.
     * Create one per enemy type via <c>Assets > Create > Navigation > NavConfig</c>.
     * </summary>
     */
    [CreateAssetMenu(menuName = "Navigation/NavConfig", fileName = "NavConfig")]
    public class NavConfig : ScriptableObject
    {
        #region Path Planning

        [Header("Path Planning")]

        [Tooltip("How often (seconds) a new path is requested.")]
        public float repathRate = 0.35f;

        [Tooltip("XZ distance to a waypoint before advancing to the next one.")]
        public float waypointAcceptanceRadius = 0.6f;

        [Tooltip("XZ distance to the final destination to consider the path complete.")]
        public float arrivalRadius = 0.25f;

        [Tooltip("Minimum destination change (metres) that triggers an immediate repath. " +
                 "Prevents repathing every frame when the target barely moves.")]
        public float destinationChangeTolerance = 0.25f;

        [Header("Stall Recovery")]
        [Tooltip("Seconds with less than stallMoveThreshold movement before forcing a fresh repath.")]
        public float stallTimeout = 1.5f;

        [Tooltip("Minimum XZ movement per stallTimeout window to not be considered stalled.")]
        public float stallMoveThreshold = 0.15f;

        #endregion

        #region Movement

        [Header("Movement")]

        [Tooltip("Maximum movement speed in m/s.")]
        public float maxSpeed = 5f;

        [Tooltip("Force multiplier applied to reach target speed (acceleration).")]
        public float accelerationForce = 40f;

        [Tooltip("Force multiplier applied when decelerating.")]
        public float decelerationForce = 40f;

        [Tooltip("Rotation speed in degrees per second.")]
        public float rotationSpeed = 360f;

        #endregion

        #region Strafe

        [Header("Strafe")]

        [Tooltip("Speed multiplier while strafing relative to maxSpeed.")]
        [Range(0.1f, 1f)] public float strafeSpeedMultiplier = 0.6f;

        #endregion

        #region Circle

        [Header("Circle")]

        [Tooltip("Preferred orbit radius when circling a target.")]
        public float circleRadius = 4f;

        [Tooltip("How hard the agent corrects back to the orbit radius (spring strength).")]
        public float circleRadiusSpring = 3f;

        #endregion

        #region Retreat / Back Jump

        [Header("Retreat / Back-Jump")]

        [Tooltip("Horizontal impulse speed for a back-jump.")]
        public float backJumpHorizontalSpeed = 6f;

        [Tooltip("Vertical impulse speed for a back-jump.")]
        public float backJumpVerticalSpeed = 5f;

        [Tooltip("Seconds to lock horizontal input after a back-jump so the agent flies freely.")]
        public float backJumpLockDuration = 0.35f;

        #endregion

        #region Wander

        [Header("Wander")]

        [Tooltip("Minimum seconds between picking a new wander destination.")]
        public float wanderIntervalMin = 2f;

        [Tooltip("Maximum seconds between picking a new wander destination.")]
        public float wanderIntervalMax = 5f;

        [Tooltip("Radius around the agent's origin to search for a random wander point.")]
        public float wanderRadius = 8f;

        #endregion
    }
}

