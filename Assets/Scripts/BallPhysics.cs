using System;
using UnityEngine;

public class BallPhysics : MonoBehaviour
{
    private Rigidbody ballRigidbody;
    public static Action ballLaunched;
    public static Action<float> OnShotPowerChanged;
    public static event Action<float> OnPerfectShotCalculated;
    public static event Action<float> OnBackboardShotCalculated;
    public static event Action<float> OnDragPowerUpdated;

    [SerializeField] private Transform basketTransform;
    [SerializeField] private Transform backboardTransform;
    [SerializeField] private Transform backboardMaxTransform;
    [SerializeField] private float basketShotAngle = 55f; // Angolo più basso per il tiro diretto
    [SerializeField] private float backboardShotAngle = 70f; // Angolo più alto per il backboard
    [SerializeField][Range(0f, 100f)] private float perfectShotThreshold = 10f; // Percentuale di errore tollerata
    [SerializeField] private float forceMultiplier = 0.01f;

    // Cached values
    private float maxSpeed;
    private float perfectSpeed;
    private float backboardPerfectSpeed;
    private Vector3 perfectBasketVelocity;
    private Vector3 perfectBackboardVelocity;

    private bool canShoot = false;

    private void Start()
    {
        ballRigidbody = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        InputManager.OnEndDrag += HandlePlayerShot;
        InputManager.OnDrag += HandleDragUpdate;
        GameManager.positionReset += CalculateNewVelocityOnReset;
    }

    private void OnDisable()
    {
        InputManager.OnEndDrag -= HandlePlayerShot;
        InputManager.OnDrag -= HandleDragUpdate;
        GameManager.positionReset -= CalculateNewVelocityOnReset;
    }

    // --- Player Shot Handling ---

    /// <summary>
    /// Called when the player finishes the swipe.
    /// </summary>
    private void HandlePlayerShot(Vector2 dragVector)
    {
        if (GameManager.Instance.GameState != GameState.Gameplay || !canShoot)
        {
            return;
        }

        canShoot = false;

        Vector3 playerVelocity = ConvertSwipeToVelocity(dragVector);

        // Calculate shot power percentage (0-100)
        float shotPowerPercentage = CalculateShotPowerPercentage(playerVelocity);
        OnShotPowerChanged?.Invoke(shotPowerPercentage);

        // Calculate errors as percentage difference in magnitude
        float playerSpeed = playerVelocity.magnitude;
        float basketErrorPercentage = Mathf.Abs(playerSpeed - perfectSpeed) / perfectSpeed * 100f;
        float backboardErrorPercentage = Mathf.Abs(playerSpeed - backboardPerfectSpeed) / backboardPerfectSpeed * 100f;

        Debug.Log($"Player speed: {playerSpeed:F2} | Perfect basket speed: {perfectSpeed:F2} | Perfect backboard speed: {backboardPerfectSpeed:F2}");
        Debug.Log($"Basket shot error: {basketErrorPercentage:F1}%");
        Debug.Log($"Backboard shot error: {backboardErrorPercentage:F1}%");

        // Determine which trajectory to use based on percentage error
        Vector3 velocityToUse = playerVelocity;

        bool isBasketPerfect = basketErrorPercentage < perfectShotThreshold;
        bool isBackboardPerfect = backboardErrorPercentage < perfectShotThreshold;

        if (isBasketPerfect && isBackboardPerfect)
        {
            // Se entrambi sono perfetti, usa quello con errore minore
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

        // Add small rotational movement
        Vector3 randomTorque = new Vector3(
            UnityEngine.Random.Range(-0.5f, 0.5f),
            UnityEngine.Random.Range(-0.5f, 0.5f),
            UnityEngine.Random.Range(-0.5f, 0.5f)
        ).normalized * 0.3f;
        ballRigidbody.AddTorque(randomTorque, ForceMode.Impulse);

        ballLaunched?.Invoke();
    }

    /* private void LaunchPerfectTestShot(Vector3 targetPos)
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
    /// Calculates and caches perfect shot velocities when the ball position is reset
    /// </summary>
    private void CalculateNewVelocityOnReset(Transform spawnPoint)
    {
        canShoot = true;

        // Calculate and cache perfect velocity towards the basket
        bool basketSolutionFound;
        perfectBasketVelocity = PhysicsUtils.CalculatePerfectShotVelocity(
            spawnPoint.position,
            basketTransform.position,
            basketShotAngle, // Usa angolo specifico per il canestro
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
            backboardShotAngle, // Usa angolo specifico per il backboard
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
            backboardShotAngle, // Usa l'angolo del backboard anche per il max
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

    private void HandleDragUpdate(Vector2 currentPosition)
    {
        if (GameManager.Instance.GameState != GameState.Gameplay || !canShoot)
        {
            return;
        }

        // Calculate drag vector from start position to current
        Vector2 dragVector = currentPosition - InputManager.Instance.StartDragPosition;

        // Convert to velocity to get power percentage
        Vector3 previewVelocity = ConvertSwipeToVelocity(dragVector);
        float dragPowerPercentage = CalculateShotPowerPercentage(previewVelocity);

        // Invoke event for UI update
        OnDragPowerUpdated?.Invoke(dragPowerPercentage);
    }
}