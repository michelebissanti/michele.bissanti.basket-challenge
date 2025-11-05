using System;
using UnityEngine;

public class BallPhysics : MonoBehaviour
{
    private Rigidbody ballRigidbody;
    public static Action ballLaunched;
    [SerializeField] private Transform basketTransform;
    [SerializeField] private Transform backboardTransform;
    [SerializeField] private Transform backboardMaxTransform;
    [SerializeField] private float shotAngle = 60f;
    [SerializeField] private float perfectShotThreshold = 0.5f;

    [SerializeField] private float forceMultiplier = 0.02f;

    private void Start()
    {
        ballRigidbody = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        InputManager.OnEndDrag += HandlePlayerShot;
    }

    private void OnDisable()
    {
        InputManager.OnEndDrag -= HandlePlayerShot;
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

        // Calculate perfect velocity towards the basket
        bool basketSolutionFound;
        Vector3 perfectBasketVelocity = PhysicsUtils.CalculatePerfectShotVelocity(
            ballRigidbody.position,
            basketTransform.position,
            shotAngle,
            out basketSolutionFound
        );

        // Calculate perfect velocity towards the backboard
        bool backboardSolutionFound;
        Vector3 perfectBackboardVelocity = PhysicsUtils.CalculatePerfectShotVelocity(
            ballRigidbody.position,
            backboardTransform.position,
            shotAngle,
            out backboardSolutionFound
        );

        // Calculate errors for both trajectories
        float basketError = float.MaxValue;
        float backboardError = float.MaxValue;

        if (basketSolutionFound)
        {
            basketError = Vector3.Distance(playerVelocity, perfectBasketVelocity);
            Debug.Log($"Basket shot error: {basketError}");
        }

        if (backboardSolutionFound)
        {
            backboardError = Vector3.Distance(playerVelocity, perfectBackboardVelocity);
            Debug.Log($"Backboard shot error: {backboardError}");
        }

        // Determine which trajectory to use based on minimum error
        Vector3 velocityToUse = playerVelocity; // By default use player's velocity
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

        // Calculate maximum velocity (backboard shot)
        bool backboardSolutionFound;
        Vector3 maxVelocity = PhysicsUtils.CalculatePerfectShotVelocity(
            ballRigidbody.position,
            backboardMaxTransform.position,
            shotAngle,
            out backboardSolutionFound
        );

        float maxSpeed = backboardSolutionFound ? maxVelocity.magnitude : 20f; // Fallback to 20 if no solution

        // Use swipe magnitude to determine shot strength (0 to 1 range based on screen)
        float swipeMagnitude = drag.magnitude;

        // Use drag.y to control vertical angle/power
        float verticalInfluence = drag.y * forceMultiplier;

        // Combine direction towards basket with swipe force
        Vector3 velocity = directionToBasket * swipeMagnitude * forceMultiplier;
        velocity.y += verticalInfluence;

        // Clamp velocity to maximum speed (backboard shot)
        if (velocity.magnitude > maxSpeed)
        {
            velocity = velocity.normalized * maxSpeed;
            Debug.Log("Clamped shot to maximum backboard velocity.");
        }

        return velocity;
    }

    /// <summary>
    /// Helper function to physically launch the ball
    /// </summary>
    private void LaunchBall(Vector3 velocity)
    {
        ballRigidbody.isKinematic = false; // Make sure it's not kinematic
        ballRigidbody.velocity = Vector3.zero;
        ballRigidbody.angularVelocity = Vector3.zero;
        ballRigidbody.AddForce(velocity, ForceMode.Impulse); // Impulse is better than VelocityChange
        ballLaunched?.Invoke();
    }

    private void LaunchPerfectTestShot(Vector3 targetPos)
    {
        Vector3 startPos = ballRigidbody.position;
        bool solutionFound;
        Vector3 velocity = PhysicsUtils.CalculatePerfectShotVelocity(startPos, targetPos, shotAngle, out solutionFound);

        if (solutionFound)
        {
            LaunchBall(velocity);
        }
        else
        {
            Debug.LogWarning("Impossible shot! The target is unreachable at this angle.");
        }
    }
}