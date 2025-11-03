using UnityEngine;

public static class PhysicsUtils
{
    /// <summary>
    /// Calculates the initial velocity (force) to launch a projectile
    /// from the start point to the target point, given a launch angle.
    /// </summary>
    /// <param name="start">Starting position</param>
    /// <param name="target">Target position</param>
    /// <param name="angleInDegrees">Launch angle in degrees (relative to the horizon)</param>
    /// <param name="foundSolution">Returns true if a solution was found, false otherwise.</param>
    /// <returns>The initial velocity vector to hit the target.</returns>
    public static Vector3 CalculatePerfectShotVelocity(Vector3 start, Vector3 target, float angleInDegrees, out bool foundSolution)
    {
        float g = Physics.gravity.y;

        // --- 1. Separate distance into Horizontal (xz) and Vertical (y) ---

        // Total displacement vector
        Vector3 delta = target - start;

        // Displacement on horizontal plane (xz)
        Vector3 deltaXZ = new Vector3(delta.x, 0, delta.z);

        // Horizontal distance (magnitude)
        float x = deltaXZ.magnitude;

        // Vertical displacement (height difference)
        float y = delta.y;

        // --- 2. Calculate velocity using parabolic motion formula ---

        // Convert angle to radians
        float angleInRadians = angleInDegrees * Mathf.Deg2Rad;

        // Calculate tangent of the angle
        float tanTheta = Mathf.Tan(angleInRadians);

        float numerator = 0.5f * g * x * x;
        float denominator = y - x * tanTheta;

        // Since g is negative, the numerator is also negative.
        // For velocity^2 to be positive, the denominator must also be negative.
        // If the denominator is >= 0, the target is unreachable with this angle
        // (e.g. it's "too high" or "behind" the parabola).

        if (denominator >= 0)
        {
            foundSolution = false;
            return Vector3.zero;
        }

        float vH_squared = numerator / denominator;

        // Now we have the horizontal velocity (not squared)
        float vH = Mathf.Sqrt(vH_squared);

        // The initial vertical velocity is vH * tan(theta)
        float vY = vH * tanTheta;

        // --- 3. Combine velocities into a 3D Vector ---

        // Horizontal direction (a unit vector pointing towards the target)
        Vector3 directionXZ = deltaXZ.normalized;

        // Horizontal velocity vector
        Vector3 velocityXZ = directionXZ * vH;

        // Vertical velocity vector
        Vector3 velocityY = Vector3.up * vY;

        foundSolution = true;

        // The total initial velocity is the sum of the components
        return velocityXZ + velocityY;
    }
}