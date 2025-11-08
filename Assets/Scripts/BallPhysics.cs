using System;
using UnityEngine;

public class BallPhysics : MonoBehaviour
{
    private Rigidbody ballRigidbody;
    public static Action ballLaunched;
    public static Action<float> OnShotPowerChanged;
    public static event Action<float> OnPerfectShotCalculated;
    public static event Action<float> OnBackboardShotCalculated;

    [SerializeField] private Transform basketTransform;
    [SerializeField] private Transform backboardTransform;
    [SerializeField] private Transform backboardMaxTransform;
    [SerializeField] private float shotAngle = 60f;
    [SerializeField] private float perfectShotThreshold = 0.5f;
    [SerializeField] private float forceMultiplier = 0.02f;

    // Cached values
    private float maxSpeed;
    private float perfectSpeed;
    private float backboardPerfectSpeed;
    private Vector3 perfectBasketVelocity;
    private Vector3 perfectBackboardVelocity;

    private void Start()
    {
        ballRigidbody = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        InputManager.OnEndDrag += HandlePlayerShot;
        GameManager.positionReset += CalculateNewVelocityOnReset;
    }

    private void OnDisable()
    {
        InputManager.OnEndDrag -= HandlePlayerShot;
        GameManager.positionReset -= CalculateNewVelocityOnReset;
    }

    // --- Player Shot Handling ---

    /// <summary>
    /// Called when the player finishes the swipe.
    /// </summary>
    private void HandlePlayerShot(Vector2 dragVector)
    {
        if (GameManager.Instance.GameState != GameState.Gameplay)
        {
            return;
        }

        Vector3 playerVelocity = ConvertSwipeToVelocity(dragVector);

        // Calculate shot power percentage (0-100)
        float shotPowerPercentage = CalculateShotPowerPercentage(playerVelocity);
        OnShotPowerChanged?.Invoke(shotPowerPercentage);

        // Calculate errors for both cached trajectories
        float basketError = Vector3.Distance(playerVelocity, perfectBasketVelocity);
        float backboardError = Vector3.Distance(playerVelocity, perfectBackboardVelocity);

        Debug.Log($"Basket shot error: {basketError}");
        Debug.Log($"Backboard shot error: {backboardError}");

        // Determine which trajectory to use based on minimum error
        Vector3 velocityToUse = playerVelocity;
        float minError = Mathf.Min(basketError, backboardError);

        if (minError < perfectShotThreshold)
        {
            if (basketError < backboardError)
            {
                velocityToUse = perfectBasketVelocity;
                Debug.Log("Perfect shot! Using calculated basket velocity.");
            }
            else
            {
                velocityToUse = perfectBackboardVelocity;
                Debug.Log("Perfect shot! Using calculated backboard velocity.");
            }
        }
        else
        {
            Debug.Log("Imperfect shot. Using player's velocity.");
        }

        // Launch the ball with the chosen velocity
        LaunchBall(velocityToUse);
    }

    /// <summary>
    /// Converts 2D swipe into a 3D launch force.
    /// </summary>
    private Vector3 ConvertSwipeToVelocity(Vector2 drag)
    {
        // Calculate direction towards the basket
        Vector3 directionToBasket = (basketTransform.position - ballRigidbody.position).normalized;

        // Use swipe magnitude to determine shot strength
        float swipeMagnitude = drag.magnitude;

        // Use drag.y to control vertical angle/power
        float verticalInfluence = drag.y * forceMultiplier;

        // Combine direction towards basket with swipe force
        Vector3 velocity = directionToBasket * swipeMagnitude * forceMultiplier;
        velocity.y += verticalInfluence;

        // Clamp velocity to maximum speed (using cached maxSpeed)
        if (velocity.magnitude > maxSpeed)
        {
            velocity = velocity.normalized * maxSpeed;
            Debug.Log("Clamped shot to maximum backboard velocity.");
        }

        return velocity;
    }

    /// <summary>
    /// Maps the velocity magnitude to a 0-100 scale based on max shot power
    /// </summary>
    private float CalculateShotPowerPercentage(Vector3 velocity)
    {
        return MapSpeedToPercentage(velocity.magnitude);
    }

    /// <summary>
    /// Helper function to physically launch the ball
    /// </summary>
    private void LaunchBall(Vector3 velocity)
    {
        ballRigidbody.isKinematic = false;
        ballRigidbody.velocity = Vector3.zero;
        ballRigidbody.angularVelocity = Vector3.zero;
        ballRigidbody.AddForce(velocity, ForceMode.Impulse);
        ballLaunched?.Invoke();
    }

    private void LaunchPerfectTestShot(Vector3 targetPos)
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
    }

    /// <summary>
    /// Calculates and caches perfect shot velocities when the ball position is reset
    /// </summary>
    private void CalculateNewVelocityOnReset(Transform spawnPoint)
    {
        // Calculate and cache perfect velocity towards the basket
        bool basketSolutionFound;
        perfectBasketVelocity = PhysicsUtils.CalculatePerfectShotVelocity(
            spawnPoint.position,
            basketTransform.position,
            shotAngle,
            out basketSolutionFound
        );

        perfectSpeed = basketSolutionFound ? perfectBasketVelocity.magnitude : 0f;

        if (!basketSolutionFound)
        {
            Debug.LogWarning("No solution found for perfect basket shot from current position.");
        }

        // Calculate and cache perfect velocity towards the backboard
        bool backboardSolutionFound;
        perfectBackboardVelocity = PhysicsUtils.CalculatePerfectShotVelocity(
            spawnPoint.position,
            backboardTransform.position,
            shotAngle,
            out backboardSolutionFound
        );

        backboardPerfectSpeed = backboardSolutionFound ? perfectBackboardVelocity.magnitude : 0f;

        if (!backboardSolutionFound)
        {
            Debug.LogWarning("No solution found for perfect backboard shot from current position.");
        }

        // Calculate and cache maximum velocity (backboard max shot) for mapping
        bool maxBackboardSolutionFound;
        Vector3 maxVelocity = PhysicsUtils.CalculatePerfectShotVelocity(
            spawnPoint.position,
            backboardMaxTransform.position,
            shotAngle,
            out maxBackboardSolutionFound
        );

        maxSpeed = maxBackboardSolutionFound ? maxVelocity.magnitude : 20f;

        // Map perfect shot speeds to 0-100 scale and invoke events
        float perfectShotPercentage = MapSpeedToPercentage(perfectSpeed);
        float backboardShotPercentage = MapSpeedToPercentage(backboardPerfectSpeed);

        OnPerfectShotCalculated?.Invoke(perfectShotPercentage / 100f);
        OnBackboardShotCalculated?.Invoke(backboardShotPercentage / 100f);

        Debug.Log($"Perfect basket shot: {perfectShotPercentage:F1}% | Backboard shot: {backboardShotPercentage:F1}%");
    }

    /// <summary>
    /// Maps a speed value to 0-100 percentage based on max speed
    /// </summary>
    private float MapSpeedToPercentage(float speed)
    {
        if (maxSpeed <= 0)
        {
            return 0f;
        }

        float percentage = (speed / maxSpeed) * 100f;
        return Mathf.Clamp(percentage, 0f, 100f);
    }
}