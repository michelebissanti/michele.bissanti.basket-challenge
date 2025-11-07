using System;
using System.Diagnostics.Tracing;
using UnityEngine;

public enum GameState
{
    MainMenu,
    Gameplay,
    Reward,
    Pause
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

    public static event Action<int> OnScoreChanged;
    public static event Action<float> OnTimerChanged;
    public static event Action<int> OnBackboardBonusActivated;
    public static event Action OnBackboardBonusExpired;

    [SerializeField] private GameState gameState;
    public GameState GameState => gameState;

    [SerializeField] private int score = 0;
    public int Score => score;

    [SerializeField] private float gameDuration = 60f;
    public float GameDuration => gameDuration;

    [SerializeField] private float timer = 60f;
    public float Timer => timer;

    [SerializeField] private GameObject playerSpawnPointParent;
    private Transform[] playerSpawnPoints;
    public static event Action<Transform> positionReset;

    [SerializeField] private float bonusBackboardDuration = 10f;
    public float BonusBackboardDuration => bonusBackboardDuration;
    [SerializeField] private BackboardBonus[] backboardBonuses;
    [SerializeField] private float minBackboardBonusSpawnInterval = 15f;
    [SerializeField] private float maxBackboardBonusSpawnInterval = 20f;

    private float backboardBonusTimer = 0f;
    private float nextBackboardBonusTime = 0f;
    private int lastBackboardBonusPoints = 0;

    void Start()
    {
        SetState(GameState.MainMenu);

        playerSpawnPoints = playerSpawnPointParent.GetComponentsInChildren<Transform>();
        playerSpawnPoints = playerSpawnPoints[1..];
    }

    public void SetState(GameState newState)
    {
        if (gameState == newState) return;

        gameState = newState;

        OnGameStateChanged?.Invoke(newState);
    }

    private void AddScore(int pointsToAdd)
    {
        score += pointsToAdd;

        OnScoreChanged?.Invoke(score);
    }

    private void ResetScore()
    {
        score = 0;

        OnScoreChanged?.Invoke(score);
    }

    public void SetPerfectScore()
    {
        AddScore(3);
    }

    public void SetStandardScore()
    {
        AddScore(2);
    }

    public void SetBackboardScore()
    {
        if (lastBackboardBonusPoints > 0)
        {
            AddScore(lastBackboardBonusPoints);
        }
    }

    private void SetTimer(float newTime)
    {
        timer = newTime;

        OnTimerChanged?.Invoke(timer);
    }

    // --- GAME LOGIC METHODS ---

    public void StartGame()
    {
        AddScore(0);
        StopBackboardBonus();
        SetTimer(gameDuration);
        nextBackboardBonusTime = gameDuration - UnityEngine.Random.Range(minBackboardBonusSpawnInterval, maxBackboardBonusSpawnInterval);
        StartCoroutine(TimerCountdown());
        SetState(GameState.Gameplay);
        BallOutOfPlay();
    }

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
                break;
            }
        }
    }

    private void StopBackboardBonus()
    {
        OnBackboardBonusExpired?.Invoke();
        lastBackboardBonusPoints = 0;
    }

    public void EndGame()
    {
        StopBackboardBonus();
        SetState(GameState.Reward);
    }

    public void BallOutOfPlay()
    {
        Transform spawnTransform = playerSpawnPoints[UnityEngine.Random.Range(0, playerSpawnPoints.Length)];
        positionReset?.Invoke(spawnTransform);
    }
}