using UnityEngine;

/// <summary>
/// Utility class for physics calculations related to projectile motion.
/// Provides methods for calculating trajectories and velocities in 3D space.
/// </summary>
public static class PhysicsUtils
{
    #region Public Methods

    /// <summary>
    /// Calculates the initial velocity (force) to launch a projectile
    /// from the start point to the target point, given a launch angle.
    /// Uses parabolic motion formulas to determine the exact velocity needed.
    /// </summary>
    /// <param name="start">Starting position in world space.</param>
    /// <param name="target">Target position in world space.</param>
    /// <param name="angleInDegrees">Launch angle in degrees (relative to the horizontal plane).</param>
    /// <param name="foundSolution">Returns true if a valid solution was found, false if target is unreachable with given angle.</param>
    /// <returns>The initial velocity vector to hit the target, or Vector3.zero if no solution exists.</returns>
    public static Vector3 CalculatePerfectShotVelocity(Vector3 start, Vector3 target, float angleInDegrees, out bool foundSolution)
    {
        // Get gravity value (negative in Unity)
        float g = Physics.gravity.y;

        // --- Step 1: Separate distance into Horizontal (xz) and Vertical (y) components ---

        // Total displacement vector from start to target
        Vector3 delta = target - start;

        // Project displacement onto horizontal plane (xz), ignoring vertical component
        Vector3 deltaXZ = new Vector3(delta.x, 0, delta.z);

        // Calculate horizontal distance (magnitude on xz plane)
        float x = deltaXZ.magnitude;

        // Calculate vertical displacement (height difference)
        float y = delta.y;

        // --- Step 2: Calculate velocity using parabolic motion formula ---
        // Formula: v² = (g * x²) / (2 * (y - x * tan(θ)))

        // Convert angle from degrees to radians for trigonometric calculations
        float angleInRadians = angleInDegrees * Mathf.Deg2Rad;

        // Calculate tangent of the launch angle
        float tanTheta = Mathf.Tan(angleInRadians);

        // Calculate numerator: 0.5 * g * x²
        float numerator = 0.5f * g * x * x;

        // Calculate denominator: y - x * tan(θ)
        float denominator = y - x * tanTheta;

        // Validate solution exists:
        // Since g is negative, numerator is negative.
        // For velocity² to be positive, denominator must also be negative.
        // If denominator >= 0, the target is unreachable with this angle
        // (target is too high or behind the parabolic arc).
        if (denominator >= 0)
        {
            foundSolution = false;
            return Vector3.zero;
        }

        // Calculate horizontal velocity squared
        float vH_squared = numerator / denominator;

        // Extract horizontal velocity magnitude
        float vH = Mathf.Sqrt(vH_squared);

        // Calculate initial vertical velocity component using: vY = vH * tan(θ)
        float vY = vH * tanTheta;

        // --- Step 3: Combine velocity components into a 3D vector ---

        // Get horizontal direction as unit vector pointing towards target
        Vector3 directionXZ = deltaXZ.normalized;

        // Calculate horizontal velocity vector (in xz plane)
        Vector3 velocityXZ = directionXZ * vH;

        // Calculate vertical velocity vector (along y axis)
        Vector3 velocityY = Vector3.up * vY;

        // Indicate successful calculation
        foundSolution = true;

        // Return combined initial velocity (sum of horizontal and vertical components)
        return velocityXZ + velocityY;
    }

    #endregion
}