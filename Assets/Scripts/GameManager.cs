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

public class GameManager : Singleton<GameManager>
{
    public static event Action<GameState> OnGameStateChanged;

    public static event Action<int> OnScoreChanged;
    public static event Action<float> OnTimerChanged;

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

    public void SetScore(int pointsToAdd)
    {
        score += pointsToAdd;

        OnScoreChanged?.Invoke(score);
    }

    public void SetPerfectScore()
    {
        SetScore(3);
    }

    public void SetStandardScore()
    {
        SetScore(2);
    }

    public void SetTimer(float newTime)
    {
        timer = newTime;

        OnTimerChanged?.Invoke(timer);
    }

    // --- GAME LOGIC METHODS ---

    public void StartGame()
    {
        SetScore(0);
        SetTimer(gameDuration);
        StartCoroutine(TimerCountdown());
        BallOutOfPlay();
        SetState(GameState.Gameplay);
    }

    public System.Collections.IEnumerator TimerCountdown()
    {
        while (timer > 0)
        {
            yield return new WaitForSeconds(1f);
            SetTimer(timer - 1f);
        }
        EndGame();
    }

    public void EndGame()
    {
        SetState(GameState.Reward);
    }

    public void BallOutOfPlay()
    {
        Transform spawnTransform = playerSpawnPoints[UnityEngine.Random.Range(0, playerSpawnPoints.Length)];
        positionReset?.Invoke(spawnTransform);
    }
}