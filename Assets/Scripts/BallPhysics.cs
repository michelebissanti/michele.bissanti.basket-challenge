using System;
using UnityEngine;

public class BallPhysics : MonoBehaviour
{
    private Rigidbody ballRigidbody;
    public static Action ballLaunched;
    public static Action<float> OnShotPowerChanged;
    // Aggiungi PlayerType agli eventi
    public static event Action<float, PlayerType> OnPerfectShotCalculated;
    public static event Action<float, PlayerType> OnBackboardShotCalculated;
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

    [SerializeField] private PlayerType playerType = PlayerType.Human;
    public PlayerType PlayerType => playerType;

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

        // Only respond if this is the correct ball for the input
        if (playerType == PlayerType.AI)
        {
            return; // AI ball doesn't respond to player input
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

        Debug.Log($"[{playerType}] Player speed: {playerSpeed:F2} | Perfect basket speed: {perfectSpeed:F2} | Perfect backboard speed: {backboardPerfectSpeed:F2}");
        Debug.Log($"[{playerType}] Basket shot error: {basketErrorPercentage:F1}%");
        Debug.Log($"[{playerType}] Backboard shot error: {backboardErrorPercentage:F1}%");

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
                Debug.Log($"[{playerType}] ✓ Perfect basket shot! Error: {basketErrorPercentage:F1}%");
            }
            else
            {
                velocityToUse = perfectBackboardVelocity;
                Debug.Log($"[{playerType}] ✓ Perfect backboard shot! Error: {backboardErrorPercentage:F1}%");
            }
        }
        else if (isBasketPerfect)
        {
            velocityToUse = perfectBasketVelocity;
            Debug.Log($"[{playerType}] ✓ Perfect basket shot! Error: {basketErrorPercentage:F1}%");
        }
        else if (isBackboardPerfect)
        {
            velocityToUse = perfectBackboardVelocity;
            Debug.Log($"[{playerType}] ✓ Perfect backboard shot! Error: {backboardErrorPercentage:F1}%");
        }
        else
        {
            Debug.Log($"[{playerType}] Using player velocity (no perfect match)");
        }

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
        // Disable kinematic mode before applying velocity
        if (ballRigidbody.isKinematic)
        {
            ballRigidbody.isKinematic = false;
            Debug.Log($"[{playerType}] isKinematic disabled for launch");
        }

        ballRigidbody.velocity = velocity;
        ballLaunched?.Invoke();
        Debug.Log($"[{playerType}] Ball launched with velocity: {velocity}");
    }

    /// <summary>
    /// Calculates and caches perfect shot velocities when the ball position is reset
    /// </summary>
    private void CalculateNewVelocityOnReset(Transform spawnPoint, PlayerType resetPlayerType)
    {
        if (resetPlayerType != playerType)
        {
            return;
        }

        Debug.Log($"[{playerType}] CalculateNewVelocityOnReset called");

        canShoot = true;

        // NON modificare isKinematic qui - viene gestito in Ball.cs e LaunchBall

        // Assicurati che le velocità siano azzerate
        ballRigidbody.velocity = Vector3.zero;
        ballRigidbody.angularVelocity = Vector3.zero;

        Debug.Log($"[{playerType}] Ball ready to shoot at position: {ballRigidbody.position}, isKinematic: {ballRigidbody.isKinematic}");

        // Calculate perfect basket shot
        bool basketSolutionFound;
        perfectBasketVelocity = PhysicsUtils.CalculatePerfectShotVelocity(
            ballRigidbody.position,
            basketTransform.position,
            basketShotAngle,
            out basketSolutionFound
        );

        if (!basketSolutionFound)
        {
            Debug.LogWarning($"[{playerType}] No solution found for basket shot!");
            return;
        }

        perfectSpeed = perfectBasketVelocity.magnitude;
        float perfectSpeedPercentage = MapSpeedToPercentage(perfectSpeed);

        OnPerfectShotCalculated?.Invoke(perfectSpeedPercentage, playerType);
        Debug.Log($"[{playerType}] Perfect basket speed: {perfectSpeed:F2} ({perfectSpeedPercentage:F1}%)");

        // Calculate perfect backboard shot
        Vector3 backboardTargetPos = Vector3.Lerp(
            backboardTransform.position,
            backboardMaxTransform.position,
            0.5f
        );

        bool backboardSolutionFound;
        perfectBackboardVelocity = PhysicsUtils.CalculatePerfectShotVelocity(
            ballRigidbody.position,
            backboardTargetPos,
            backboardShotAngle,
            out backboardSolutionFound
        );

        if (!backboardSolutionFound)
        {
            Debug.LogWarning($"[{playerType}] No solution found for backboard shot!");
            return;
        }

        backboardPerfectSpeed = perfectBackboardVelocity.magnitude;
        float backboardSpeedPercentage = MapSpeedToPercentage(backboardPerfectSpeed);

        OnBackboardShotCalculated?.Invoke(backboardSpeedPercentage, playerType);
        Debug.Log($"[{playerType}] Perfect backboard speed: {backboardPerfectSpeed:F2} ({backboardSpeedPercentage:F1}%)");

        maxSpeed = Mathf.Max(perfectSpeed, backboardPerfectSpeed) * 1.5f;
    }

    /// <summary>
    /// Public method for AI to trigger a shot
    /// </summary>
    public void HandleAIShot(Vector2 dragVector)
    {
        if (GameManager.Instance.GameState != GameState.Gameplay || !canShoot)
        {
            Debug.LogWarning($"[{playerType}] Cannot shoot - GameState: {GameManager.Instance.GameState}, canShoot: {canShoot}");
            return;
        }

        // Only AI ball should respond to this
        if (playerType != PlayerType.AI)
        {
            return;
        }

        canShoot = false;

        Vector3 aiVelocity = ConvertSwipeToVelocity(dragVector);

        // Calculate shot power percentage (0-100)
        float shotPowerPercentage = CalculateShotPowerPercentage(aiVelocity);
        OnShotPowerChanged?.Invoke(shotPowerPercentage);

        // Calculate errors as percentage difference in magnitude
        float aiSpeed = aiVelocity.magnitude;
        float basketErrorPercentage = Mathf.Abs(aiSpeed - perfectSpeed) / perfectSpeed * 100f;
        float backboardErrorPercentage = Mathf.Abs(aiSpeed - backboardPerfectSpeed) / backboardPerfectSpeed * 100f;

        Debug.Log($"[AI] Player speed: {aiSpeed:F2} | Perfect basket speed: {perfectSpeed:F2} | Perfect backboard speed: {backboardPerfectSpeed:F2}");
        Debug.Log($"[AI] Basket shot error: {basketErrorPercentage:F1}%");
        Debug.Log($"[AI] Backboard shot error: {backboardErrorPercentage:F1}%");

        // Determine which trajectory to use based on percentage error
        Vector3 velocityToUse = aiVelocity;

        bool isBasketPerfect = basketErrorPercentage < perfectShotThreshold;
        bool isBackboardPerfect = backboardErrorPercentage < perfectShotThreshold;

        if (isBasketPerfect && isBackboardPerfect)
        {
            // Se entrambi sono perfetti, usa quello con errore minore
            if (basketErrorPercentage < backboardErrorPercentage)
            {
                velocityToUse = perfectBasketVelocity;
                Debug.Log($"[AI] ✓ Perfect basket shot! Error: {basketErrorPercentage:F1}%");
            }
            else
            {
                velocityToUse = perfectBackboardVelocity;
                Debug.Log($"[AI] ✓ Perfect backboard shot! Error: {backboardErrorPercentage:F1}%");
            }
        }
        else if (isBasketPerfect)
        {
            velocityToUse = perfectBasketVelocity;
            Debug.Log($"[AI] ✓ Perfect basket shot! Error: {basketErrorPercentage:F1}%");
        }
        else if (isBackboardPerfect)
        {
            velocityToUse = perfectBackboardVelocity;
            Debug.Log($"[AI] ✓ Perfect backboard shot! Error: {backboardErrorPercentage:F1}%");
        }
        else
        {
            Debug.Log($"[AI] Using player velocity (no perfect match)");
        }

        LaunchBall(velocityToUse);
    }

    /// <summary>
    /// Maps a speed value to 0-100 percentage based on max speed
    /// </summary>
    private float MapSpeedToPercentage(float speed)
    {
        if (maxSpeed <= 0)
        {
            maxSpeed = 10f; // Fallback value
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