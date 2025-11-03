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
    /// <param name="error">Error margin (0 = perfect, 0.1 = 10% error, 0.5 = 50% error)</param>
    /// <param name="foundSolution">Returns true if a solution was found, false otherwise.</param>
    /// <returns>The initial velocity vector to hit the target.</returns>
    public static Vector3 CalculatePerfectShotVelocity(Vector3 start, Vector3 target, float angleInDegrees, float error, out bool foundSolution)
    {
        float g = Physics.gravity.y;

        // --- Apply error margin to target ---
        Vector3 adjustedTarget = target;
        if (error > 0)
        {
            // Calculate horizontal distance to scale the error
            Vector3 deltaForError = target - start;
            float distance = new Vector3(deltaForError.x, 0, deltaForError.z).magnitude;

            // Apply a random offset proportional to error and distance
            float errorRadius = error * distance;
            Vector3 randomOffset = new Vector3(
                Random.Range(-errorRadius, errorRadius),
                0, // Don't modify the target's height
                Random.Range(-errorRadius, errorRadius)
            );

            adjustedTarget += randomOffset;
        }

        // --- 1. Separate distance into Horizontal (xz) and Vertical (y) ---

        // Total displacement vector (use the adjusted target)
        Vector3 delta = adjustedTarget - start;

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

    public static void DrawDebugTrajectory(Vector3 start, Vector3 initialVelocity, int steps = 30, float timeStep = 0.1f)
    {
        Vector3 previousPoint = start;
        for (int i = 1; i <= steps; i++)
        {
            float t = i * timeStep;
            Vector3 point = start + initialVelocity * t + 0.5f * Physics.gravity * t * t;
            Debug.DrawLine(previousPoint, point, Color.red, 1.0f);
            previousPoint = point;
        }
    }
}