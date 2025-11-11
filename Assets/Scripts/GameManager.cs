using System;
using UnityEngine;
using Unity.Collections;
using static AudioManager;

/// <summary>
/// Enumeration representing the different states of the game.
/// </summary>
public enum GameState
{
    MainMenu,
    Gameplay,
    Reward,
    Pause
}

/// <summary>
/// Configuration class for backboard bonus mechanics.
/// Defines bonus points and spawn probability.
/// </summary>
[System.Serializable]
public class BackboardBonus
{
    /// <summary>Bonus points awarded when this bonus is active.</summary>
    [Tooltip("Bonus points awarded")]
    public int bonusPoints;

    /// <summary>Probability (0-100%) that this bonus will spawn.</summary>
    [Tooltip("Spawn percentage (0-100)")]
    [Range(0f, 100f)]
    public float spawnPercentage;
}

/// <summary>
/// Core game manager that controls game state, scoring, timers, and special mechanics.
/// Manages the gameplay loop, backboard bonuses, fireball mode, and high score persistence.
/// </summary>
public class GameManager : Singleton<GameManager>
{
    #region Events

    /// <summary>Event triggered when the game state changes.</summary>
    public static event Action<GameState> OnGameStateChanged;

    /// <summary>Event triggered when the score changes.</summary>
    public static event Action<int> OnScoreChanged;

    /// <summary>Event triggered when the game timer updates.</summary>
    public static event Action<float> OnTimerChanged;

    /// <summary>Event triggered when a backboard bonus is activated.</summary>
    public static event Action<int> OnBackboardBonusActivated;

    /// <summary>Event triggered when a backboard bonus expires.</summary>
    public static event Action OnBackboardBonusExpired;

    /// <summary>Event triggered when fireball progress changes (current streak, max baskets needed).</summary>
    public static event Action<int, int> OnFireballProgressChanged; // Current streak, max baskets needed

    /// <summary>Event triggered when fireball mode is activated with its duration.</summary>
    public static event Action<float> OnFireballModeActivated; // Duration parameter

    /// <summary>Event triggered when fireball mode expires.</summary>
    public static event Action OnFireballModeExpired;

    /// <summary>Event triggered when a standard score is achieved.</summary>
    public static event Action OnScoreDone;

    /// <summary>Event triggered when a perfect score is achieved.</summary>
    public static event Action OnPerfectScoreDone;

    /// <summary>Event triggered when a backboard bonus score is achieved.</summary>
    public static event Action OnBackboardScoreDone;

    /// <summary>Event triggered when the high score changes.</summary>
    public static event Action<int> OnHighScoreChanged;

    /// <summary>Event triggered when player position should be reset.</summary>
    public static event Action<Transform> positionReset;

    #endregion

    #region Serialized Fields

    /// <summary>Current game state.</summary>
    [SerializeField] private GameState gameState;

    /// <summary>Current score in the active game session.</summary>
    [SerializeField] private int score = 0;

    /// <summary>Total duration of a game session in seconds.</summary>
    [SerializeField] private float gameDuration = 60f;

    /// <summary>Current remaining time in the game session.</summary>
    [SerializeField] private float timer = 60f;

    /// <summary>Parent GameObject containing all player spawn point transforms.</summary>
    [SerializeField] private GameObject playerSpawnPointParent;

    /// <summary>Duration of the backboard bonus in seconds.</summary>
    [SerializeField] private float bonusBackboardDuration = 10f;

    /// <summary>Array of possible backboard bonuses with their spawn probabilities.</summary>
    [SerializeField] private BackboardBonus[] backboardBonuses;

    /// <summary>Minimum time interval between backboard bonus spawns.</summary>
    [SerializeField] private float minBackboardBonusSpawnInterval = 15f;

    /// <summary>Maximum time interval between backboard bonus spawns.</summary>
    [SerializeField] private float maxBackboardBonusSpawnInterval = 20f;

    [Header("Fireball Mechanic")]
    /// <summary>Number of consecutive baskets required to activate fireball mode.</summary>
    [SerializeField] private int maxBasketsForFireball = 5;

    /// <summary>Duration of fireball mode in seconds.</summary>
    [SerializeField] private float fireballDuration = 15f;

    #endregion

    #region Public Properties

    /// <summary>Gets the current game state.</summary>
    public GameState GameState => gameState;

    /// <summary>Gets the current score.</summary>
    public int Score => score;

    /// <summary>Gets the all-time high score.</summary>
    public int HighScore => highScore;

    /// <summary>Gets the game duration in seconds.</summary>
    public float GameDuration => gameDuration;

    /// <summary>Gets the current timer value.</summary>
    public float Timer => timer;

    /// <summary>Gets the backboard bonus duration.</summary>
    public float BonusBackboardDuration => bonusBackboardDuration;

    /// <summary>Gets the maximum baskets needed for fireball activation.</summary>
    public int MaxBasketsForFireball => maxBasketsForFireball;

    /// <summary>Gets the fireball mode duration.</summary>
    public float FireballDuration => fireballDuration;

    #endregion

    #region Private Fields

    /// <summary>Persistent high score across game sessions.</summary>
    private int highScore = 0;

    /// <summary>PlayerPrefs key for storing high score.</summary>
    private const string HIGH_SCORE_KEY = "HighScore";

    /// <summary>Array of all available player spawn point transforms.</summary>
    private Transform[] playerSpawnPoints;

    /// <summary>Currently active spawn point.</summary>
    private Transform currentSpawnPoint;

    /// <summary>Timer tracking remaining backboard bonus time.</summary>
    private float backboardBonusTimer = 0f;

    /// <summary>Game time when the next backboard bonus should spawn.</summary>
    private float nextBackboardBonusTime = 0f;

    /// <summary>Points value of the currently active backboard bonus.</summary>
    private int lastBackboardBonusPoints = 0;

    /// <summary>Flag indicating if a point was scored in the current throw.</summary>
    private bool scoredPoint = false;

    /// <summary>Count of consecutive successful baskets.</summary>
    private int consecutiveBaskets = 0;

    /// <summary>Flag indicating if fireball mode is currently active.</summary>
    private bool isFireballModeActive = false;

    /// <summary>Timer tracking remaining fireball mode time.</summary>
    private float fireballModeTimer = 0f;

    /// <summary>Points multiplier applied during fireball mode.</summary>
    private int fireballMultiplier = 2;

    #endregion

    #region Unity Lifecycle Methods

    /// <summary>
    /// Subscribes to game events when the component is enabled.
    /// </summary>
    void OnEnable()
    {
        Ball.BallOutOfPlay += OnBallOutOfPlay;
    }

    /// <summary>
    /// Unsubscribes from game events when the component is disabled.
    /// </summary>
    void OnDisable()
    {
        Ball.BallOutOfPlay -= OnBallOutOfPlay;
    }

    /// <summary>
    /// Initializes the game manager, loads high score, and sets up spawn points.
    /// </summary>
    void Start()
    {
        SetState(GameState.MainMenu);
        AudioManager.Instance.ChangeMusic(SoundType.MenuMusic);

        playerSpawnPoints = playerSpawnPointParent.GetComponentsInChildren<Transform>();
        playerSpawnPoints = playerSpawnPoints[1..];

        LoadHighScore();
    }

    #endregion

    #region Game State Management

    /// <summary>
    /// Changes the current game state and triggers the state changed event.
    /// </summary>
    /// <param name="newState">The new game state to transition to.</param>
    public void SetState(GameState newState)
    {
        if (gameState == newState) return;

        gameState = newState;

        OnGameStateChanged?.Invoke(newState);
    }

    #endregion

    #region Score Management

    /// <summary>
    /// Adds points to the current score with fireball multiplier if active.
    /// Updates high score if exceeded.
    /// </summary>
    /// <param name="pointsToAdd">Base points to add before multiplier.</param>
    private void AddScore(int pointsToAdd)
    {
        if (gameState != GameState.Gameplay) return;

        // Apply fireball points multiplier
        if (isFireballModeActive)
        {
            pointsToAdd *= fireballMultiplier;
        }

        score += pointsToAdd;

        OnScoreChanged?.Invoke(score);

        // Check and update high score
        if (score > highScore)
        {
            highScore = score;
            SaveHighScore();
            OnHighScoreChanged?.Invoke(highScore);
        }

        if (scoredPoint == false)
        {
            scoredPoint = true;
            IncrementFireballProgress();
        }
    }

    /// <summary>
    /// Resets the score to zero and triggers score changed event.
    /// </summary>
    private void ResetScore()
    {
        score = 0;

        OnScoreChanged?.Invoke(score);
    }

    /// <summary>
    /// Awards 3 points for a perfect shot (no ring or backboard contact).
    /// </summary>
    public void SetPerfectScore()
    {
        AddScore(3);
        OnPerfectScoreDone?.Invoke();
        AudioManager.Instance.Play(SoundType.PerfectScore);
    }

    /// <summary>
    /// Awards 2 points for a standard basket.
    /// </summary>
    public void SetStandardScore()
    {
        AddScore(2);
        OnScoreDone?.Invoke();
        AudioManager.Instance.Play(SoundType.Score);
    }

    /// <summary>
    /// Awards bonus points for a backboard shot when bonus is active.
    /// </summary>
    public void SetBackboardScore()
    {
        if (lastBackboardBonusPoints > 0)
        {
            AddScore(lastBackboardBonusPoints);
            OnBackboardScoreDone?.Invoke();
            AudioManager.Instance.Play(SoundType.BackboardScore);
        }
    }

    #endregion

    #region Timer Management

    /// <summary>
    /// Sets the game timer and triggers timer changed event.
    /// </summary>
    /// <param name="newTime">The new timer value in seconds.</param>
    private void SetTimer(float newTime)
    {
        timer = newTime;

        OnTimerChanged?.Invoke(timer);
    }

    #endregion

    #region Fireball Mechanic

    /// <summary>
    /// Increments the consecutive basket counter and activates fireball mode if threshold is reached.
    /// Only increments when fireball mode is not already active.
    /// </summary>
    private void IncrementFireballProgress()
    {
        if (isFireballModeActive) return;

        consecutiveBaskets++;
        OnFireballProgressChanged?.Invoke(consecutiveBaskets, maxBasketsForFireball);

        if (consecutiveBaskets >= maxBasketsForFireball)
        {
            ActivateFireballMode();
        }
    }

    /// <summary>
    /// Resets the consecutive basket counter to zero.
    /// </summary>
    private void ResetFireballProgress()
    {
        consecutiveBaskets = 0;
        OnFireballProgressChanged?.Invoke(consecutiveBaskets, maxBasketsForFireball);
    }

    /// <summary>
    /// Activates fireball mode with point multiplier and timer.
    /// </summary>
    private void ActivateFireballMode()
    {
        isFireballModeActive = true;
        fireballModeTimer = fireballDuration;
        consecutiveBaskets = 0;
        OnFireballModeActivated?.Invoke(fireballDuration);
        AudioManager.Instance.Play(SoundType.OnFireballModeActivated);
    }

    /// <summary>
    /// Deactivates fireball mode and resets the timer.
    /// </summary>
    private void DeactivateFireballMode()
    {
        isFireballModeActive = false;
        fireballModeTimer = 0f;
        OnFireballModeExpired?.Invoke();
    }

    #endregion

    #region Game Loop Management

    /// <summary>
    /// Starts a new game session by resetting all game state and starting the countdown timer.
    /// </summary>
    public void StartGame()
    {
        ResetScore();
        ChangeSpawnPoint();
        StopBackboardBonus();
        ResetFireballProgress();
        DeactivateFireballMode();
        SetTimer(gameDuration);
        nextBackboardBonusTime = gameDuration - UnityEngine.Random.Range(minBackboardBonusSpawnInterval, maxBackboardBonusSpawnInterval);
        StartCoroutine(TimerCountdown());
        SetState(GameState.Gameplay);
        OnBallOutOfPlay();

        AudioManager.Instance.ChangeMusic(SoundType.GameMusic);

    }

    /// <summary>
    /// Coroutine that handles the game timer countdown and manages timed events.
    /// Updates backboard bonus timer, fireball mode timer, and triggers bonus spawns.
    /// </summary>
    private System.Collections.IEnumerator TimerCountdown()
    {
        while (timer > 0)
        {
            yield return new WaitForSeconds(1f);
            SetTimer(timer - 1f);

            // Backboard bonus timer update
            if (backboardBonusTimer > 0)
            {
                backboardBonusTimer -= 1f;
                if (backboardBonusTimer <= 0)
                {
                    StopBackboardBonus();
                }
            }

            // Fireball mode timer update
            if (isFireballModeActive && fireballModeTimer > 0)
            {
                fireballModeTimer -= 1f;
                if (fireballModeTimer <= 0)
                {
                    DeactivateFireballMode();
                }
            }

            // Check to start a new backboard bonus
            if (timer <= nextBackboardBonusTime && lastBackboardBonusPoints == 0)
            {
                StartBackboardBonus();
                // Calculate the next spawn time
                float nextInterval = UnityEngine.Random.Range(minBackboardBonusSpawnInterval, maxBackboardBonusSpawnInterval);
                nextBackboardBonusTime = Mathf.Max(0, timer - nextInterval);
            }
        }
        EndGame();
    }

    /// <summary>
    /// Ends the current game session and transitions to the reward state.
    /// </summary>
    public void EndGame()
    {
        StopBackboardBonus();
        DeactivateFireballMode();
        SetState(GameState.Reward);
        AudioManager.Instance.ChangeMusic(SoundType.MenuMusic);
    }

    /// <summary>
    /// Handles ball out of play event. Changes spawn point if a basket was scored,
    /// or resets fireball progress if basket was missed.
    /// </summary>
    public void OnBallOutOfPlay()
    {
        if (scoredPoint)
        {
            scoredPoint = false;
            ChangeSpawnPoint();
        }
        else
        {
            // Basket missed - reset fireball progress
            if (!isFireballModeActive)
            {
                ResetFireballProgress();
            }
        }

        positionReset?.Invoke(currentSpawnPoint);
    }

    #endregion

    #region Backboard Bonus Management

    /// <summary>
    /// Starts a backboard bonus by randomly selecting one based on spawn probabilities.
    /// </summary>
    private void StartBackboardBonus()
    {
        float randomValue = UnityEngine.Random.Range(0f, 100f);
        float cumulativePercentage = 0f;

        foreach (BackboardBonus bonus in backboardBonuses)
        {
            cumulativePercentage += bonus.spawnPercentage;
            if (randomValue <= cumulativePercentage)
            {
                lastBackboardBonusPoints = bonus.bonusPoints;
                backboardBonusTimer = bonusBackboardDuration;
                OnBackboardBonusActivated?.Invoke(bonus.bonusPoints);
                AudioManager.Instance.Play(SoundType.OnBackboardBonusActivated);
                break;
            }
        }
    }

    /// <summary>
    /// Stops the current backboard bonus and resets bonus points.
    /// </summary>
    private void StopBackboardBonus()
    {
        OnBackboardBonusExpired?.Invoke();
        lastBackboardBonusPoints = 0;
    }

    #endregion

    #region Spawn Point Management

    /// <summary>
    /// Changes to a random spawn point different from the current one.
    /// </summary>
    private void ChangeSpawnPoint()
    {
        Transform newSpawnPoint;
        do
        {
            newSpawnPoint = playerSpawnPoints[UnityEngine.Random.Range(0, playerSpawnPoints.Length)];
        } while (newSpawnPoint == currentSpawnPoint && playerSpawnPoints.Length > 1);

        currentSpawnPoint = newSpawnPoint;

    }

    #endregion

    #region High Score Persistence

    /// <summary>
    /// Loads the high score from PlayerPrefs.
    /// </summary>
    private void LoadHighScore()
    {
        highScore = PlayerPrefs.GetInt(HIGH_SCORE_KEY, 0);
        OnHighScoreChanged?.Invoke(highScore);
    }

    /// <summary>
    /// Saves the current high score to PlayerPrefs.
    /// </summary>
    private void SaveHighScore()
    {
        PlayerPrefs.SetInt(HIGH_SCORE_KEY, highScore);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Resets the high score to zero and clears it from PlayerPrefs.
    /// </summary>
    public void ResetHighScore()
    {
        highScore = 0;
        PlayerPrefs.DeleteKey(HIGH_SCORE_KEY);
        PlayerPrefs.Save();
        OnHighScoreChanged?.Invoke(highScore);
    }

    #endregion

}
