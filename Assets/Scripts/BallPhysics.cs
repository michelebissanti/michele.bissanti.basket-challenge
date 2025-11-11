using System;
using UnityEngine;

/// <summary>
/// Manages the physics and trajectory calculations for basketball shooting mechanics.
/// Handles player input conversion, perfect shot calculations, and ball launching with realistic physics.
/// </summary>
public class BallPhysics : MonoBehaviour
{
    #region Fields

    private Rigidbody ballRigidbody;

    // Events for notifying other systems about shot states
    public static Action ballLaunched;
    public static Action<float> OnShotPowerChanged;
    public static event Action<float> OnPerfectShotCalculated;
    public static event Action<float> OnBackboardShotCalculated;
    public static event Action<float> OnDragPowerUpdated;

    // Target transforms for trajectory calculations
    [SerializeField] private Transform basketTransform;
    [SerializeField] private Transform backboardTransform;
    [SerializeField] private Transform backboardMaxTransform;

    // Shot angle configurations
    [SerializeField] private float basketShotAngle = 55f; // Lower angle for direct basket shots
    [SerializeField] private float backboardShotAngle = 70f; // Higher angle for backboard shots

    // Shot accuracy settings
    [SerializeField][Range(0f, 100f)] private float perfectShotThreshold = 10f; // Percentage error tolerance for perfect shots
    [SerializeField] private float forceMultiplier = 0.01f; // Multiplier to convert swipe input to physics force

    // Debug visualization settings
    [Header("Debug Visualization")]
    [SerializeField] private bool drawTrajectory = true; // Enable/disable trajectory visualization
    [SerializeField] private int trajectoryResolution = 30; // Number of points to draw in trajectory
    [SerializeField] private float trajectoryTimeStep = 0.1f; // Time between trajectory points
    [SerializeField] private Color perfectTrajectoryColor = Color.green; // Color for perfect basket trajectory
    [SerializeField] private Color backboardTrajectoryColor = Color.yellow; // Color for backboard trajectory
    [SerializeField] private Color playerTrajectoryColor = Color.red; // Color for player's attempted shot
    [SerializeField] private float trajectoryDrawDuration = 3f; // How long to show trajectory lines

    // Cached trajectory values to avoid repeated calculations
    private float maxSpeed; // Maximum possible shot speed (based on backboard max distance)
    private float perfectSpeed; // Exact speed needed for perfect basket shot
    private float backboardPerfectSpeed; // Exact speed needed for perfect backboard shot
    private Vector3 perfectBasketVelocity; // Pre-calculated perfect velocity for basket
    private Vector3 perfectBackboardVelocity; // Pre-calculated perfect velocity for backboard

    private bool canShoot = false; // Flag to prevent shooting during invalid game states

    #endregion

    #region Debug Testing Methods

    /// <summary>
    /// Test method: Launches a perfect shot towards the basket.
    /// Right-click the component in Inspector and select "Test Perfect Basket Shot".
    /// </summary>
    [ContextMenu("Test Perfect Basket Shot")]
    private void TestPerfectBasketShot()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Perfect shot test can only be executed in Play Mode!");
            return;
        }

        if (perfectBasketVelocity == Vector3.zero)
        {
            Debug.LogWarning("Perfect basket velocity not calculated yet. Reset ball position first!");
            return;
        }

        Debug.Log($"🎯 Testing perfect basket shot with velocity: {perfectBasketVelocity}");
        LaunchBall(perfectBasketVelocity);
    }

    /// <summary>
    /// Test method: Launches a perfect shot towards the backboard.
    /// Right-click the component in Inspector and select "Test Perfect Backboard Shot".
    /// </summary>
    [ContextMenu("Test Perfect Backboard Shot")]
    private void TestPerfectBackboardShot()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Perfect shot test can only be executed in Play Mode!");
            return;
        }

        if (perfectBackboardVelocity == Vector3.zero)
        {
            Debug.LogWarning("Perfect backboard velocity not calculated yet. Reset ball position first!");
            return;
        }

        Debug.Log($"🎯 Testing perfect backboard shot with velocity: {perfectBackboardVelocity}");
        LaunchBall(perfectBackboardVelocity);
    }

    /// <summary>
    /// Displays detailed information about current trajectory calculations.
    /// </summary>
    [ContextMenu("Show Trajectory Info")]
    private void ShowTrajectoryInfo()
    {
        Debug.Log("=== TRAJECTORY INFORMATION ===");
        Debug.Log($"Ball Position: {(ballRigidbody != null ? ballRigidbody.position.ToString() : "N/A")}");
        Debug.Log($"Basket Position: {(basketTransform != null ? basketTransform.position.ToString() : "N/A")}");
        Debug.Log($"Backboard Position: {(backboardTransform != null ? backboardTransform.position.ToString() : "N/A")}");
        Debug.Log($"\nPerfect Basket Velocity: {perfectBasketVelocity} (Speed: {perfectSpeed:F2})");
        Debug.Log($"Perfect Backboard Velocity: {perfectBackboardVelocity} (Speed: {backboardPerfectSpeed:F2})");
        Debug.Log($"Max Speed: {maxSpeed:F2}");
        Debug.Log($"\nRigidbody Drag: {(ballRigidbody != null ? ballRigidbody.drag.ToString() : "N/A")}");
        Debug.Log($"Rigidbody Mass: {(ballRigidbody != null ? ballRigidbody.mass.ToString() : "N/A")}");
        Debug.Log($"Gravity: {Physics.gravity}");
        Debug.Log($"\nShot Angles - Basket: {basketShotAngle}°, Backboard: {backboardShotAngle}°");
        Debug.Log("================================");
    }

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        ballRigidbody = GetComponent<Rigidbody>();
    }

    /// <summary>
    /// Subscribes to input and game state events.
    /// </summary>
    private void OnEnable()
    {
        InputManager.OnEndDrag += HandlePlayerShot;
        InputManager.OnDrag += HandleDragUpdate;
        Ball.OnBallReset += CalculateNewVelocityOnReset;
    }

    /// <summary>
    /// Unsubscribes from events to prevent memory leaks.
    /// </summary>
    private void OnDisable()
    {
        InputManager.OnEndDrag -= HandlePlayerShot;
        InputManager.OnDrag -= HandleDragUpdate;
        Ball.OnBallReset -= CalculateNewVelocityOnReset;
    }

    #endregion

    #region Player Shot Handling

    /// <summary>
    /// Handles the player's shot when the swipe gesture is completed.
    /// Converts the swipe input into velocity, evaluates shot accuracy,
    /// and applies trajectory correction for near-perfect shots.
    /// </summary>
    /// <param name="dragVector">The 2D vector representing the player's swipe gesture.</param>
    private void HandlePlayerShot(Vector2 dragVector)
    {
        // Validate game state and shooting availability
        if (GameManager.Instance.GameState != GameState.Gameplay || !canShoot)
        {
            return;
        }

        canShoot = false;

        // Convert player's swipe into 3D velocity
        Vector3 playerVelocity = ConvertSwipeToVelocity(dragVector);

        // Calculate and broadcast shot power for UI feedback
        float shotPowerPercentage = CalculateShotPowerPercentage(playerVelocity);
        OnShotPowerChanged?.Invoke(shotPowerPercentage);

        // Evaluate shot accuracy by comparing player's velocity with perfect trajectories
        float playerSpeed = playerVelocity.magnitude;
        float basketErrorPercentage = Mathf.Abs(playerSpeed - perfectSpeed) / perfectSpeed * 100f;
        float backboardErrorPercentage = Mathf.Abs(playerSpeed - backboardPerfectSpeed) / backboardPerfectSpeed * 100f;

        Debug.Log($"Player speed: {playerSpeed:F2} | Perfect basket speed: {perfectSpeed:F2} | Perfect backboard speed: {backboardPerfectSpeed:F2}");
        Debug.Log($"Basket shot error: {basketErrorPercentage:F1}%");
        Debug.Log($"Backboard shot error: {backboardErrorPercentage:F1}%");

        // Determine which trajectory to use based on accuracy threshold
        Vector3 velocityToUse = playerVelocity;

        bool isBasketPerfect = basketErrorPercentage < perfectShotThreshold;
        bool isBackboardPerfect = backboardErrorPercentage < perfectShotThreshold;

        // Priority system: if both shots are within threshold, use the more accurate one
        if (isBasketPerfect && isBackboardPerfect)
        {
            // If both trajectories are perfect, use the one with lower error
            if (basketErrorPercentage < backboardErrorPercentage)
            {
                velocityToUse = perfectBasketVelocity;
                Debug.Log($"✓ Perfect basket shot! Error: {basketErrorPercentage:F1}%");
            }
            else
            {
                velocityToUse = perfectBackboardVelocity;
                Debug.Log($"✓ Perfect backboard shot! Error: {backboardErrorPercentage:F1}%");
            }
        }
        else if (isBasketPerfect)
        {
            velocityToUse = perfectBasketVelocity;
            Debug.Log($"✓ Perfect basket shot! Error: {basketErrorPercentage:F1}%");
        }
        else if (isBackboardPerfect)
        {
            velocityToUse = perfectBackboardVelocity;
            Debug.Log($"✓ Perfect backboard shot! Error: {backboardErrorPercentage:F1}%");
        }
        else
        {
            Debug.Log($"✗ Imperfect shot. Basket error: {basketErrorPercentage:F1}%, Backboard error: {backboardErrorPercentage:F1}%");
        }

        // Draw player's trajectory for debug visualization
        if (drawTrajectory)
        {
            DrawTrajectoryPath(ballRigidbody.position, playerVelocity, playerTrajectoryColor, trajectoryDrawDuration);
        }

        // Execute the shot with the determined velocity
        LaunchBall(velocityToUse);
    }

    /// <summary>
    /// Converts a 2D swipe gesture into a 3D velocity vector for the ball.
    /// Applies force multiplier, vertical influence, and maximum speed clamping.
    /// </summary>
    /// <param name="drag">The 2D drag vector from the player's swipe input.</param>
    /// <returns>A 3D velocity vector ready to be applied to the ball's Rigidbody.</returns>
    private Vector3 ConvertSwipeToVelocity(Vector2 drag)
    {
        // Calculate normalized direction from ball to basket
        Vector3 directionToBasket = (basketTransform.position - ballRigidbody.position).normalized;

        // Extract swipe magnitude to determine horizontal power
        float swipeMagnitude = drag.magnitude;

        // Apply vertical component from swipe to control arc height
        float verticalInfluence = drag.y * forceMultiplier;

        // Construct velocity vector combining horizontal direction and vertical power
        Vector3 velocity = directionToBasket * swipeMagnitude * forceMultiplier;
        velocity.y += verticalInfluence;

        // Enforce maximum speed limit to prevent unrealistic shots
        if (velocity.magnitude > maxSpeed)
        {
            velocity = velocity.normalized * maxSpeed;
            Debug.Log("Clamped shot to maximum backboard velocity.");
        }

        return velocity;
    }

    /// <summary>
    /// Calculates the shot power as a percentage (0-100) based on velocity magnitude.
    /// </summary>
    /// <param name="velocity">The velocity vector of the shot.</param>
    /// <returns>Shot power percentage scaled to maximum possible shot speed.</returns>
    private float CalculateShotPowerPercentage(Vector3 velocity)
    {
        return MapSpeedToPercentage(velocity.magnitude);
    }

    /// <summary>
    /// Physically launches the ball with the specified velocity.
    /// Applies impulse force, random spin, and triggers associated events and audio.
    /// IMPORTANT: Uses direct velocity assignment instead of AddForce for better precision.
    /// </summary>
    /// <param name="velocity">The velocity vector to apply to the ball.</param>
    private void LaunchBall(Vector3 velocity)
    {
        // Reset rigidbody state and enable physics simulation
        ballRigidbody.isKinematic = false;
        ballRigidbody.velocity = Vector3.zero;
        ballRigidbody.angularVelocity = Vector3.zero;

        // PRECISION FIX: Use direct velocity assignment instead of AddForce
        // This ensures the exact calculated velocity is applied without any physics frame delays
        ballRigidbody.velocity = velocity;

        // Apply subtle random rotation for realistic ball spin
        Vector3 randomTorque = new Vector3(
            UnityEngine.Random.Range(-0.5f, 0.5f),
            UnityEngine.Random.Range(-0.5f, 0.5f),
            UnityEngine.Random.Range(-0.5f, 0.5f)
        ).normalized * 0.3f;
        ballRigidbody.AddTorque(randomTorque, ForceMode.Impulse);

        // Notify other systems that ball has been launched
        ballLaunched?.Invoke();

        // Play throw sound effect
        AudioManager.Instance.Play(AudioManager.SoundType.Throw);
    }    /* private void LaunchPerfectTestShot(Vector3 targetPos)
    {
        bool solutionFound;
        Vector3 velocity = PhysicsUtils.CalculatePerfectShotVelocity(
            ballRigidbody.position,
            targetPos,
            shotAngle,
            out solutionFound
        );

        if (solutionFound)
        {
            LaunchBall(velocity);
        }
        else
        {
            Debug.LogWarning("Impossible shot! The target is unreachable at this angle.");
        }
    } */

    /// <summary>
    /// Calculates and caches perfect shot trajectories when the ball position is reset.
    /// Computes optimal velocities for direct basket shots, backboard shots, and maximum range shots.
    /// These pre-calculated values are used for shot accuracy evaluation during gameplay.
    /// </summary>
    /// <param name="spawnPoint">The transform representing the ball's spawn position.</param>
    private void CalculateNewVelocityOnReset()
    {
        canShoot = true;

        // === Calculate Perfect Direct Basket Shot ===
        bool basketSolutionFound;
        perfectBasketVelocity = PhysicsUtils.CalculatePerfectShotVelocity(
            this.transform.position,
            basketTransform.position,
            basketShotAngle, // Use lower angle optimized for direct shots
            out basketSolutionFound
        );

        perfectSpeed = basketSolutionFound ? perfectBasketVelocity.magnitude : 0f;

        if (!basketSolutionFound)
        {
            Debug.LogWarning("No solution found for perfect basket shot from current position.");
        }

        // === Calculate Perfect Backboard Shot ===
        bool backboardSolutionFound;
        perfectBackboardVelocity = PhysicsUtils.CalculatePerfectShotVelocity(
            this.transform.position,
            backboardTransform.position,
            backboardShotAngle, // Use higher angle optimized for backboard rebounds
            out backboardSolutionFound
        );

        backboardPerfectSpeed = backboardSolutionFound ? perfectBackboardVelocity.magnitude : 0f;

        if (!backboardSolutionFound)
        {
            Debug.LogWarning("No solution found for perfect backboard shot from current position.");
        }

        // === Calculate Maximum Shot Range (for power scaling) ===
        bool maxBackboardSolutionFound;
        Vector3 maxVelocity = PhysicsUtils.CalculatePerfectShotVelocity(
            this.transform.position,
            backboardMaxTransform.position,
            backboardShotAngle, // Use backboard angle for consistency
            out maxBackboardSolutionFound
        );

        // Use calculated max speed or fallback to default value
        maxSpeed = maxBackboardSolutionFound ? maxVelocity.magnitude : 20f;

        // === Broadcast Perfect Shot Values to UI Systems ===
        float perfectShotPercentage = MapSpeedToPercentage(perfectSpeed);
        float backboardShotPercentage = MapSpeedToPercentage(backboardPerfectSpeed);

        OnPerfectShotCalculated?.Invoke(perfectShotPercentage / 100f);
        OnBackboardShotCalculated?.Invoke(backboardShotPercentage / 100f);

        Debug.Log($"Perfect basket shot: {perfectShotPercentage:F1}% | Backboard shot: {backboardShotPercentage:F1}%");

        // Draw perfect trajectories for debug visualization
        if (drawTrajectory)
        {
            DrawTrajectoryPath(this.transform.position, perfectBasketVelocity, perfectTrajectoryColor, trajectoryDrawDuration * 2f);
            DrawTrajectoryPath(this.transform.position, perfectBackboardVelocity, backboardTrajectoryColor, trajectoryDrawDuration * 2f);
        }
    }

    /// <summary>
    /// Maps a given speed value to a 0-100 percentage scale based on the maximum possible shot speed.
    /// Used for normalizing shot power for UI display and analytics.
    /// </summary>
    /// <param name="speed">The speed magnitude to convert.</param>
    /// <returns>A clamped percentage value between 0 and 100.</returns>
    private float MapSpeedToPercentage(float speed)
    {
        if (maxSpeed <= 0)
        {
            return 0f;
        }

        float percentage = (speed / maxSpeed) * 100f;
        return Mathf.Clamp(percentage, 0f, 100f);
    }

    #endregion

    #region Trajectory Visualization

    /// <summary>
    /// Draws the predicted trajectory path using Debug lines for visualization.
    /// Simulates the physics movement frame by frame to show where the ball will go.
    /// </summary>
    /// <param name="startPos">Starting position of the trajectory.</param>
    /// <param name="initialVelocity">Initial velocity vector.</param>
    /// <param name="color">Color of the trajectory line.</param>
    /// <param name="duration">How long the line should be visible.</param>
    private void DrawTrajectoryPath(Vector3 startPos, Vector3 initialVelocity, Color color, float duration)
    {
        Vector3 previousPoint = startPos;
        Vector3 currentVelocity = initialVelocity;
        Vector3 currentPosition = startPos;

        // Get the drag value from the ball's rigidbody to account for air resistance
        float drag = ballRigidbody.drag;
        float mass = ballRigidbody.mass;

        for (int i = 0; i < trajectoryResolution; i++)
        {
            // Calculate time delta for this step
            float dt = trajectoryTimeStep;

            // Apply gravity
            currentVelocity += Physics.gravity * dt;

            // Apply drag (same formula Unity uses internally)
            // F_drag = -drag * velocity
            // a = F/m but for drag Unity applies it directly to velocity
            currentVelocity *= Mathf.Clamp01(1f - drag * dt);

            // Update position
            currentPosition += currentVelocity * dt;

            // Draw line segment
            Debug.DrawLine(previousPoint, currentPosition, color, duration);

            // Draw small sphere at each point for better visibility
            DrawDebugSphere(currentPosition, 0.05f, color, duration);

            previousPoint = currentPosition;

            // Optional: Stop drawing if trajectory goes below ground
            if (currentPosition.y < 0)
            {
                break;
            }
        }

        // Draw target markers
        DrawDebugSphere(basketTransform.position, 0.1f, Color.cyan, duration);
        DrawDebugSphere(backboardTransform.position, 0.1f, Color.magenta, duration);
    }

    /// <summary>
    /// Draws a debug sphere using lines (since Debug.DrawSphere doesn't exist).
    /// </summary>
    private void DrawDebugSphere(Vector3 center, float radius, Color color, float duration)
    {
        // Draw 3 circles (XY, XZ, YZ planes)
        DrawDebugCircle(center, radius, Vector3.forward, color, duration);
        DrawDebugCircle(center, radius, Vector3.right, color, duration);
        DrawDebugCircle(center, radius, Vector3.up, color, duration);
    }

    /// <summary>
    /// Draws a debug circle.
    /// </summary>
    private void DrawDebugCircle(Vector3 center, float radius, Vector3 normal, Color color, float duration)
    {
        int segments = 16;
        Vector3 forward = Vector3.Slerp(normal, -normal, 0.5f);
        Vector3 right = Vector3.Cross(normal, forward).normalized;

        Vector3 prevPoint = center + right * radius;

        for (int i = 1; i <= segments; i++)
        {
            float angle = (float)i / segments * 2f * Mathf.PI;
            Vector3 newPoint = center + (right * Mathf.Cos(angle) + Vector3.Cross(normal, right) * Mathf.Sin(angle)) * radius;
            Debug.DrawLine(prevPoint, newPoint, color, duration);
            prevPoint = newPoint;
        }
    }

    #endregion

    #region Real-time Input Feedback

    /// <summary>
    /// Provides real-time shot power feedback during the drag gesture.
    /// Updates UI elements to show predicted shot strength before release.
    /// </summary>
    /// <param name="currentPosition">The current position of the drag input.</param>
    private void HandleDragUpdate(Vector2 currentPosition)
    {
        // Validate game state before processing input
        if (GameManager.Instance.GameState != GameState.Gameplay || !canShoot)
        {
            return;
        }

        // Calculate current drag vector from starting position
        Vector2 dragVector = currentPosition - InputManager.Instance.StartDragPosition;

        // Convert drag to velocity and calculate power percentage
        Vector3 previewVelocity = ConvertSwipeToVelocity(dragVector);
        float dragPowerPercentage = CalculateShotPowerPercentage(previewVelocity);

        // Broadcast power update for real-time UI feedback
        OnDragPowerUpdated?.Invoke(dragPowerPercentage);
    }

    #endregion
}