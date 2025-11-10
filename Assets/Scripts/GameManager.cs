using System;
using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;

public enum GameState
{
    MainMenu,
    Gameplay,
    Reward,
    Pause
}

public enum GameMode
{
    SinglePlayer,
    VersusAI
}

public enum PlayerType
{
    Human,
    AI
}

[System.Serializable]
public class BackboardBonus
{
    [Tooltip("Bonus points awarded")]
    public int bonusPoints;

    [Tooltip("Spawn percentage (0-100)")]
    [Range(0f, 100f)]
    public float spawnPercentage;
}

public class GameManager : Singleton<GameManager>
{
    public static event Action<GameState> OnGameStateChanged;
    public static event Action<int, PlayerType> OnScoreChanged;
    public static event Action<float> OnTimerChanged;
    public static event Action<int> OnBackboardBonusActivated;
    public static event Action OnBackboardBonusExpired;
    public static event Action<int, int, PlayerType> OnFireballProgressChanged;
    public static event Action<float, PlayerType> OnFireballModeActivated;
    public static event Action<PlayerType> OnFireballModeExpired;

    [SerializeField] private GameState gameState;
    public GameState GameState => gameState;

    [Header("Game Mode")]
    [SerializeField] private GameMode gameMode = GameMode.SinglePlayer;
    public GameMode CurrentGameMode => gameMode;
    [SerializeField] private AIDifficulty aiDifficulty = AIDifficulty.Medium;

    [Header("Player Scores")]
    [SerializeField] private int humanScore = 0;
    public int HumanScore => humanScore;
    [SerializeField] private int aiScore = 0;
    public int AIScore => aiScore;

    [SerializeField] private float gameDuration = 60f;
    public float GameDuration => gameDuration;

    [SerializeField] private float timer = 60f;
    public float Timer => timer;

    [SerializeField] private GameObject playerSpawnPointParent;
    private Transform[] playerSpawnPoints;
    private Transform currentHumanSpawnPoint;
    private Transform currentAISpawnPoint;

    public static event Action<Transform, PlayerType> positionReset;

    [SerializeField] private float bonusBackboardDuration = 10f;
    public float BonusBackboardDuration => bonusBackboardDuration;
    [SerializeField] private BackboardBonus[] backboardBonuses;
    [SerializeField] private float minBackboardBonusSpawnInterval = 15f;
    [SerializeField] private float maxBackboardBonusSpawnInterval = 20f;

    [Header("Fireball Mechanic")]
    [SerializeField] private int maxBasketsForFireball = 5;
    public int MaxBasketsForFireball => maxBasketsForFireball;
    [SerializeField] private float fireballDuration = 15f;
    public float FireballDuration => fireballDuration;

    private int fireballMultiplier = 2;

    private PlayerState humanPlayerState;
    private PlayerState aiPlayerState;

    // Cambia lo stato del backboard bonus da per-player a globale
    private float sharedBackboardBonusTimer = 0f;
    private float sharedNextBackboardBonusTime = 0f;
    private int sharedBackboardBonusPoints = 0;

    void OnEnable()
    {
        Ball.BallOutOfPlay += OnBallOutOfPlay;
    }

    void OnDisable()
    {
        Ball.BallOutOfPlay -= OnBallOutOfPlay;
    }

    void Start()
    {
        SetState(GameState.MainMenu);

        playerSpawnPoints = playerSpawnPointParent.GetComponentsInChildren<Transform>();
        playerSpawnPoints = playerSpawnPoints[1..];

        humanPlayerState = new PlayerState();
        aiPlayerState = new PlayerState();
    }

    public void SetState(GameState newState)
    {
        if (gameState == newState) return;

        gameState = newState;

        OnGameStateChanged?.Invoke(newState);
    }

    /// <summary>
    /// Sets the game mode
    /// </summary>
    public void SetGameMode(GameMode mode)
    {
        gameMode = mode;
        Debug.Log($"Game mode set to: {mode}");
    }

    /// <summary>
    /// Sets the AI difficulty
    /// </summary>
    public void SetAIDifficulty(AIDifficulty difficulty)
    {
        aiDifficulty = difficulty;
    }

    private void AddScore(int pointsToAdd, PlayerType playerType)
    {
        PlayerState state = GetPlayerState(playerType);

        // Apply fireball points multiplier
        if (state.isFireballModeActive)
        {
            pointsToAdd *= fireballMultiplier;
        }

        if (playerType == PlayerType.Human)
        {
            humanScore += pointsToAdd;
        }
        else
        {
            aiScore += pointsToAdd;
        }

        OnScoreChanged?.Invoke(playerType == PlayerType.Human ? humanScore : aiScore, playerType);

        if (!state.scoredPoint)
        {
            state.scoredPoint = true;
            IncrementFireballProgress(playerType);
        }
    }

    private void ResetScore()
    {
        humanScore = 0;
        aiScore = 0;

        OnScoreChanged?.Invoke(humanScore, PlayerType.Human);
        if (gameMode == GameMode.VersusAI)
        {
            OnScoreChanged?.Invoke(aiScore, PlayerType.AI);
        }
    }

    public void SetPerfectScore(PlayerType playerType)
    {
        AddScore(3, playerType);
    }

    public void SetStandardScore(PlayerType playerType)
    {
        AddScore(2, playerType);
    }

    public void SetBackboardScore(PlayerType playerType)
    {
        if (sharedBackboardBonusPoints > 0)
        {
            AddScore(sharedBackboardBonusPoints, playerType);
        }
    }

    private void SetTimer(float newTime)
    {
        timer = newTime;
        OnTimerChanged?.Invoke(timer);
    }

    // --- FIREBALL MECHANIC METHODS ---

    private void IncrementFireballProgress(PlayerType playerType)
    {
        PlayerState state = GetPlayerState(playerType);
        if (state.isFireballModeActive) return;

        state.consecutiveBaskets++;
        OnFireballProgressChanged?.Invoke(state.consecutiveBaskets, maxBasketsForFireball, playerType);

        if (state.consecutiveBaskets >= maxBasketsForFireball)
        {
            ActivateFireballMode(playerType);
        }
    }

    private void ResetFireballProgress(PlayerType playerType)
    {
        PlayerState state = GetPlayerState(playerType);
        state.consecutiveBaskets = 0;
        OnFireballProgressChanged?.Invoke(state.consecutiveBaskets, maxBasketsForFireball, playerType);
    }

    private void ActivateFireballMode(PlayerType playerType)
    {
        PlayerState state = GetPlayerState(playerType);
        state.isFireballModeActive = true;
        state.fireballModeTimer = fireballDuration;
        state.consecutiveBaskets = 0;
        OnFireballModeActivated?.Invoke(fireballDuration, playerType);
    }

    private void DeactivateFireballMode(PlayerType playerType)
    {
        PlayerState state = GetPlayerState(playerType);
        state.isFireballModeActive = false;
        state.fireballModeTimer = 0f;
        OnFireballModeExpired?.Invoke(playerType);
    }

    // --- GAME LOGIC METHODS ---

    public void StartGame()
    {
        ResetScore();

        humanPlayerState.Reset();
        aiPlayerState.Reset();

        // Initialize spawn points ensuring they are different
        ChangeSpawnPoint(PlayerType.Human);
        if (gameMode == GameMode.VersusAI)
        {
            ChangeSpawnPoint(PlayerType.AI);
        }

        // Sostituisci l'inizializzazione per-player con quella condivisa
        StopBackboardBonus();

        ResetFireballProgress(PlayerType.Human);
        ResetFireballProgress(PlayerType.AI);

        DeactivateFireballMode(PlayerType.Human);
        DeactivateFireballMode(PlayerType.AI);

        SetTimer(gameDuration);

        sharedNextBackboardBonusTime = gameDuration - UnityEngine.Random.Range(minBackboardBonusSpawnInterval, maxBackboardBonusSpawnInterval);

        StartCoroutine(TimerCountdown());
        SetState(GameState.Gameplay);

        OnBallOutOfPlay(PlayerType.Human);
        if (gameMode == GameMode.VersusAI)
        {
            OnBallOutOfPlay(PlayerType.AI);
        }
    }

    private System.Collections.IEnumerator TimerCountdown()
    {
        while (timer > 0)
        {
            yield return new WaitForSeconds(1f);
            SetTimer(timer - 1f);

            // Aggiorna il timer condiviso del backboard bonus
            UpdateSharedBackboardBonus();

            UpdatePlayerTimers(humanPlayerState, PlayerType.Human);
            if (gameMode == GameMode.VersusAI)
            {
                UpdatePlayerTimers(aiPlayerState, PlayerType.AI);
            }
        }
        EndGame();
    }

    private void UpdatePlayerTimers(PlayerState state, PlayerType playerType)
    {
        // Fireball mode timer update
        if (state.isFireballModeActive && state.fireballModeTimer > 0)
        {
            state.fireballModeTimer -= 1f;
            if (state.fireballModeTimer <= 0)
            {
                DeactivateFireballMode(playerType);
            }
        }

        // Sposta la gestione del backboard bonus fuori da questo metodo
    }

    private void UpdateSharedBackboardBonus()
    {
        // Backboard bonus timer update (condiviso)
        if (sharedBackboardBonusTimer > 0)
        {
            sharedBackboardBonusTimer -= 1f;
            if (sharedBackboardBonusTimer <= 0)
            {
                StopBackboardBonus();
            }
        }

        // Check to start a new backboard bonus (condiviso)
        if (timer <= sharedNextBackboardBonusTime && sharedBackboardBonusPoints == 0)
        {
            StartBackboardBonus();
            float nextInterval = UnityEngine.Random.Range(minBackboardBonusSpawnInterval, maxBackboardBonusSpawnInterval);
            sharedNextBackboardBonusTime = Mathf.Max(0, timer - nextInterval);
        }
    }

    private void StartBackboardBonus()
    {
        float randomValue = UnityEngine.Random.Range(0f, 100f);
        float cumulativePercentage = 0f;

        foreach (BackboardBonus bonus in backboardBonuses)
        {
            cumulativePercentage += bonus.spawnPercentage;
            if (randomValue <= cumulativePercentage)
            {
                sharedBackboardBonusPoints = bonus.bonusPoints;
                sharedBackboardBonusTimer = bonusBackboardDuration;
                OnBackboardBonusActivated?.Invoke(bonus.bonusPoints);
                break;
            }
        }
    }

    private void StopBackboardBonus()
    {
        OnBackboardBonusExpired?.Invoke();
        sharedBackboardBonusPoints = 0;
    }

    public void EndGame()
    {
        StopBackboardBonus();
        DeactivateFireballMode(PlayerType.Human);
        DeactivateFireballMode(PlayerType.AI);
        SetState(GameState.Reward);
    }

    public void OnBallOutOfPlay(PlayerType playerType)
    {
        PlayerState state = GetPlayerState(playerType);

        if (state.scoredPoint)
        {
            state.scoredPoint = false;
            ChangeSpawnPoint(playerType);
        }
        else
        {
            // Basket missed - reset fireball progress
            if (!state.isFireballModeActive)
            {
                ResetFireballProgress(playerType);
            }
        }

        Transform spawnPoint = playerType == PlayerType.Human ? currentHumanSpawnPoint : currentAISpawnPoint;
        positionReset?.Invoke(spawnPoint, playerType);
    }

    /// <summary>
    /// Changes spawn point ensuring players never occupy the same position
    /// </summary>
    private void ChangeSpawnPoint(PlayerType playerType)
    {
        if (playerSpawnPoints.Length < 2)
        {
            Debug.LogWarning("Not enough spawn points for multiple players!");
            return;
        }

        Transform newSpawnPoint;
        Transform currentSpawnPoint = playerType == PlayerType.Human ? currentHumanSpawnPoint : currentAISpawnPoint;
        Transform otherPlayerSpawnPoint = playerType == PlayerType.Human ? currentAISpawnPoint : currentHumanSpawnPoint;

        int attempts = 0;
        int maxAttempts = 100;

        do
        {
            newSpawnPoint = playerSpawnPoints[UnityEngine.Random.Range(0, playerSpawnPoints.Length)];
            attempts++;

            if (attempts > maxAttempts)
            {
                Debug.LogError("Could not find valid spawn point after max attempts!");
                break;
            }

        } while ((newSpawnPoint == currentSpawnPoint || newSpawnPoint == otherPlayerSpawnPoint) && playerSpawnPoints.Length > 1);

        if (playerType == PlayerType.Human)
        {
            currentHumanSpawnPoint = newSpawnPoint;
            Debug.Log($"Human spawn point changed to: {newSpawnPoint.name}");
        }
        else
        {
            currentAISpawnPoint = newSpawnPoint;
            Debug.Log($"AI spawn point changed to: {newSpawnPoint.name}");
        }
    }

    /// <summary>
    /// Gets the player state for the specified player type
    /// </summary>
    private PlayerState GetPlayerState(PlayerType playerType)
    {
        return playerType == PlayerType.Human ? humanPlayerState : aiPlayerState;
    }

    /// <summary>
    /// Gets the winner at game end
    /// </summary>
    public string GetWinner()
    {
        if (gameMode == GameMode.SinglePlayer)
            return "Player";

        if (humanScore > aiScore)
            return "Human Player Wins!";
        else if (aiScore > humanScore)
            return "AI Wins!";
        else
            return "Draw!";
    }

    /// <summary>
    /// Checks if backboard bonus is active for a specific player
    /// </summary>
    public bool IsBackboardBonusActive()
    {
        return sharedBackboardBonusPoints > 0;
    }

    /// <summary>
    /// Gets the current backboard bonus points for a specific player
    /// </summary>
    public int GetBackboardBonusPoints()
    {
        return sharedBackboardBonusPoints;
    }
}

/// <summary>
/// Encapsulates the state of a single player
/// </summary>
[System.Serializable]
public class PlayerState
{
    // Rimuovi le variabili backboard-related
    public bool scoredPoint = false;
    public int consecutiveBaskets = 0;
    public bool isFireballModeActive = false;
    public float fireballModeTimer = 0f;

    public void Reset()
    {
        scoredPoint = false;
        consecutiveBaskets = 0;
        isFireballModeActive = false;
        fireballModeTimer = 0f;
    }
}