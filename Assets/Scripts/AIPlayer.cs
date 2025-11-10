using System.Collections;
using UnityEngine;

public enum AIDifficulty
{
    Easy,
    Medium,
    Hard
}

[System.Serializable]
public class DifficultyConfig
{
    [Tooltip("Shot accuracy variation (0-1, lower is more accurate)")]
    [Range(0f, 0.5f)]
    public float accuracyVariation = 0.15f;

    [Tooltip("Trajectory variation (0-1, lower is more accurate)")]
    [Range(0f, 0.3f)]
    public float trajectoryVariation = 0.1f;

    [Tooltip("Chance to attempt backboard shot (0-100%)")]
    [Range(0f, 100f)]
    public float backboardShotChance = 30f;
}

/// <summary>
/// AI player that shoots simultaneously with human player
/// </summary>
public class AIPlayer : MonoBehaviour
{
    [Header("AI Configuration")]
    [SerializeField] private AIDifficulty difficulty = AIDifficulty.Medium;
    [SerializeField] private float minShootDelay = 1f;
    [SerializeField] private float maxShootDelay = 3f;

    [Header("Difficulty Settings")]
    [SerializeField]
    private DifficultyConfig easyConfig = new DifficultyConfig
    {
        accuracyVariation = 0.3f,
        trajectoryVariation = 0.2f,
        backboardShotChance = 20f
    };
    [SerializeField]
    private DifficultyConfig mediumConfig = new DifficultyConfig
    {
        accuracyVariation = 0.15f,
        trajectoryVariation = 0.1f,
        backboardShotChance = 40f
    };
    [SerializeField]
    private DifficultyConfig hardConfig = new DifficultyConfig
    {
        accuracyVariation = 0.05f,
        trajectoryVariation = 0.05f,
        backboardShotChance = 60f
    };

    [Header("AI Ball Reference")]
    [SerializeField] private BallPhysics aiBallPhysics;

    private bool canShoot = false;
    private float currentPerfectSpeed;
    private float currentBackboardSpeed;
    private Coroutine shootingCoroutine;

    private void OnEnable()
    {
        GameManager.positionReset += OnBallReset;
        BallPhysics.OnPerfectShotCalculated += OnPerfectShotCalculated;
        BallPhysics.OnBackboardShotCalculated += OnBackboardShotCalculated;
    }

    private void OnDisable()
    {
        GameManager.positionReset -= OnBallReset;
        BallPhysics.OnPerfectShotCalculated -= OnPerfectShotCalculated;
        BallPhysics.OnBackboardShotCalculated -= OnBackboardShotCalculated;
    }

    private void Start()
    {
        // Auto-find AI ball if not assigned
        if (aiBallPhysics == null)
        {
            BallPhysics[] allBalls = FindObjectsOfType<BallPhysics>();
            foreach (var ball in allBalls)
            {
                if (ball.PlayerType == PlayerType.AI)
                {
                    aiBallPhysics = ball;
                    Debug.Log("AI Player found AI ball automatically");
                    break;
                }
            }

            if (aiBallPhysics == null)
            {
                Debug.LogError("AI Player could not find AI ball!");
            }
        }
    }

    public void SetDifficulty(AIDifficulty newDifficulty)
    {
        difficulty = newDifficulty;
        Debug.Log($"AI difficulty set to: {difficulty}");
    }

    private void OnBallReset(Transform spawnPoint, PlayerType playerType)
    {
        if (playerType != PlayerType.AI) return;
        if (GameManager.Instance.CurrentGameMode != GameMode.VersusAI) return;

        Debug.Log($"[AI] Ball reset detected at position: {spawnPoint.position}");

        canShoot = true;

        if (shootingCoroutine != null)
        {
            StopCoroutine(shootingCoroutine);
        }

        shootingCoroutine = StartCoroutine(ShootAfterDelay());
    }

    private IEnumerator ShootAfterDelay()
    {
        float delay = Random.Range(minShootDelay, maxShootDelay);
        Debug.Log($"[AI] Waiting {delay:F2}s before shooting...");
        yield return new WaitForSeconds(delay);

        if (canShoot && GameManager.Instance.GameState == GameState.Gameplay)
        {
            PerformShot();
        }
        else
        {
            Debug.Log($"[AI] Cannot shoot - canShoot: {canShoot}, GameState: {GameManager.Instance.GameState}");
        }
    }

    private void PerformShot()
    {
        if (!canShoot || aiBallPhysics == null)
        {
            Debug.LogWarning($"[AI] Cannot perform shot - canShoot: {canShoot}, aiBallPhysics: {aiBallPhysics != null}");
            return;
        }

        if (currentPerfectSpeed <= 0 || currentBackboardSpeed <= 0)
        {
            Debug.LogError($"[AI] Invalid speeds - Perfect: {currentPerfectSpeed}, Backboard: {currentBackboardSpeed}");
            return;
        }

        canShoot = false;
        DifficultyConfig config = GetCurrentDifficultyConfig();

        // Decide shot type
        bool useBackboard = Random.Range(0f, 100f) < config.backboardShotChance;
        float targetSpeed = useBackboard ? currentBackboardSpeed : currentPerfectSpeed;

        Debug.Log($"[AI] Target shot type: {(useBackboard ? "Backboard" : "Basket")}, Target speed: {targetSpeed:F2}%");

        // Apply accuracy variation
        float speedVariation = Random.Range(-config.accuracyVariation, config.accuracyVariation);
        float finalSpeed = targetSpeed * (1f + speedVariation);

        Vector2 simulatedDrag = ConvertSpeedToDragVector(finalSpeed, config);

        Debug.Log($"[AI] Shooting with drag vector: {simulatedDrag}, final speed: {finalSpeed:F2}%");

        // Call AI shot method directly
        aiBallPhysics.HandleAIShot(simulatedDrag);
    }

    private Vector2 ConvertSpeedToDragVector(float speedPercentage, DifficultyConfig config)
    {
        // Convert percentage (0-100) to drag magnitude
        float dragMagnitude = speedPercentage * 10f; // Scale factor for drag

        // Add trajectory variations
        float verticalVariation = Random.Range(-config.trajectoryVariation, config.trajectoryVariation);
        float horizontalVariation = Random.Range(-config.trajectoryVariation * 0.5f, config.trajectoryVariation * 0.5f);

        return new Vector2(
            horizontalVariation * dragMagnitude,
            dragMagnitude * (1f + verticalVariation)
        );
    }

    private DifficultyConfig GetCurrentDifficultyConfig()
    {
        return difficulty switch
        {
            AIDifficulty.Easy => easyConfig,
            AIDifficulty.Medium => mediumConfig,
            AIDifficulty.Hard => hardConfig,
            _ => mediumConfig
        };
    }

    // Filtra per PlayerType
    private void OnPerfectShotCalculated(float speedPercentage, PlayerType playerType)
    {
        if (playerType != PlayerType.AI) return;

        currentPerfectSpeed = speedPercentage;
        Debug.Log($"[AI] Perfect shot speed calculated: {speedPercentage:F2}%");
    }

    // Filtra per PlayerType
    private void OnBackboardShotCalculated(float speedPercentage, PlayerType playerType)
    {
        if (playerType != PlayerType.AI) return;

        currentBackboardSpeed = speedPercentage;
        Debug.Log($"[AI] Backboard shot speed calculated: {speedPercentage:F2}%");
    }
}