using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UIElements;

public class UIManager : Singleton<UIManager>
{
    private VisualElement _mainMenuContainer;
    private VisualElement _inGameContainer;
    private VisualElement _rewardContainer;

    private Label _scoreLabel;
    private Label _highScoreLabel;
    private Label _timerLabel;
    private Label _finalScoreLabel;
    private Button _playButton;
    private Button _playAgainButton;
    private Button _returnToMenuButton;

    private VisualElement _inputBarColor;
    private VisualElement _inputBarPointer;
    private VisualElement _inputBarPerfectHint;
    private VisualElement _inputBarBackboardHint;

    private Label _scoreDifferenceLabel;

    [SerializeField] private int maxInputBarPercentagePointer = 97;

    [SerializeField] private int maxInputBarPercentageHint = 90;

    private ProgressBar _fireballProgressBar;
    private VisualElement _fireballProgressContainer;
    private Label _fireballProgressLabel;
    private Coroutine fireballTimerCoroutine;

    private int lastScore = 0;

    private PlayableDirector _playableDirector;

    [SerializeField] private PlayableAsset scoreDifferenceAnimation;

    public override void Awake()
    {
        base.Awake();
        var root = GetComponent<UIDocument>().rootVisualElement;

        _mainMenuContainer = root.Q<VisualElement>("main-menu-container");
        _inGameContainer = root.Q<VisualElement>("in-game-container");
        _rewardContainer = root.Q<VisualElement>("reward-container");

        _scoreLabel = root.Q<Label>("score-label");
        _highScoreLabel = root.Q<Label>("high-score-label");
        _timerLabel = root.Q<Label>("timer-label");
        _finalScoreLabel = root.Q<Label>("final-score-label");

        _playButton = root.Q<Button>("play-button");
        _playAgainButton = root.Q<Button>("play-again-button");
        _returnToMenuButton = root.Q<Button>("return-to-menu-button");

        _playButton.clicked += OnPlayButtonClicked;
        _playAgainButton.clicked += OnPlayAgainButtonClicked;
        _returnToMenuButton.clicked += OnReturnToMenuButtonClicked;

        _inputBarColor = root.Q<VisualElement>("input-bar-color");
        _inputBarPointer = root.Q<VisualElement>("input-bar-pointer");

        _inputBarPerfectHint = root.Q<VisualElement>("input-bar-perfect-hint");
        _inputBarBackboardHint = root.Q<VisualElement>("input-bar-backboard-hint");

        _fireballProgressBar = root.Q<ProgressBar>("fireball-progress-bar");
        _fireballProgressContainer = root.Q<VisualElement>("fireball-progress-container");
        _fireballProgressLabel = root.Q<Label>("fireball-label");

        _scoreDifferenceLabel = root.Q<Label>("score-difference");


    }

    private void OnEnable()
    {
        GameManager.OnGameStateChanged += HandleGameStateChanged;
        GameManager.OnScoreChanged += HandleScoreChanged;
        GameManager.OnTimerChanged += HandleTimerChanged;
        BallPhysics.OnShotPowerChanged += HandleShotPowerChanged;
        BallPhysics.OnDragPowerUpdated += HandleDragPowerUpdate;
        GameManager.positionReset += (Transform spawnPoint) => UpdateInputBar(0f);
        BallPhysics.OnPerfectShotCalculated += UpdatePerfectShotHint;
        BallPhysics.OnBackboardShotCalculated += UpdateBackboardShotHint;

        // Fireball events
        GameManager.OnFireballProgressChanged += HandleFireballProgressChanged;
        GameManager.OnFireballModeActivated += HandleFireballModeActivated;
        GameManager.OnFireballModeExpired += HandleFireballModeExpired;
    }

    private void OnDisable()
    {
        GameManager.OnGameStateChanged -= HandleGameStateChanged;
        GameManager.OnScoreChanged -= HandleScoreChanged;
        GameManager.OnTimerChanged -= HandleTimerChanged;
        BallPhysics.OnShotPowerChanged -= HandleShotPowerChanged;
        BallPhysics.OnDragPowerUpdated -= HandleDragPowerUpdate;
        GameManager.positionReset -= (Transform spawnPoint) => UpdateInputBar(0f);
        BallPhysics.OnPerfectShotCalculated -= UpdatePerfectShotHint;
        BallPhysics.OnBackboardShotCalculated -= UpdateBackboardShotHint;

        // Fireball events
        GameManager.OnFireballProgressChanged -= HandleFireballProgressChanged;
        GameManager.OnFireballModeActivated -= HandleFireballModeActivated;
        GameManager.OnFireballModeExpired -= HandleFireballModeExpired;
    }

    void Start()
    {
        HideAllScreens();
        ShowMainMenu();

        _playableDirector = GetComponentInChildren<PlayableDirector>();
    }

    // --- FIREBALL UI HANDLERS ---

    private void HandleFireballProgressChanged(int currentBaskets, int maxBaskets)
    {
        _fireballProgressBar.value = currentBaskets;
        _fireballProgressBar.highValue = maxBaskets;

        _fireballProgressBar.title = $"{currentBaskets}/{maxBaskets}";
    }

    private void HandleFireballModeActivated(float duration)
    {
        ShowFireballActiveIndicator();
        fireballTimerCoroutine = StartCoroutine(FireballTimer(duration));
    }

    private IEnumerator FireballTimer(float duration)
    {
        float timeRemaining = duration;

        while (timeRemaining > 0f)
        {
            UpdateFireballTimer(timeRemaining);
            yield return null;
            timeRemaining -= Time.deltaTime;
        }

        UpdateFireballTimer(0f);
    }

    private void HandleFireballModeExpired()
    {
        HideFireballActiveIndicator();
        if (fireballTimerCoroutine != null)
        {
            StopCoroutine(fireballTimerCoroutine);
            fireballTimerCoroutine = null;
        }


        // Reset progress
        HandleFireballProgressChanged(0, GameManager.Instance.MaxBasketsForFireball);
    }

    private void ShowFireballActiveIndicator()
    {
        _fireballProgressLabel.text = "FIREBALL ACTIVE!";
    }

    private void HideFireballActiveIndicator()
    {
        _fireballProgressLabel.text = "FIREBALL BONUS";
    }

    private void UpdateFireballTimer(float timeRemaining)
    {
        _fireballProgressBar.value = timeRemaining;
        _fireballProgressBar.highValue = GameManager.Instance.FireballDuration;

        _fireballProgressBar.title = $"{timeRemaining:F1}s";
    }

    // --- PRIVATE METHODS ---

    private void HandleGameStateChanged(GameState newState)
    {
        HideAllScreens();

        // Mostra solo quello giusto in base al nuovo stato
        switch (newState)
        {
            case GameState.MainMenu:
                ShowMainMenu();
                break;
            case GameState.Gameplay:
                ShowInGameUI();
                break;
            case GameState.Reward:
                ShowRewardScreen(GameManager.Instance.Score, GameManager.Instance.HighScore);
                break;
        }
    }

    private void HandleScoreChanged(int newScore)
    {
        UpdateScore(newScore);

        int scoreDifference = newScore - lastScore;
        lastScore = newScore;

        if (scoreDifference <= 0) return;

        _scoreDifferenceLabel.text = $"+{scoreDifference}";
        _playableDirector.Play(scoreDifferenceAnimation);
    }

    private void HandleTimerChanged(float newTime)
    {
        UpdateTimer(newTime);
    }

    private void UpdateInputBar(float normalizedValue)
    {
        _inputBarColor.style.scale = new Scale(new Vector3(1, normalizedValue, 1));

        if (normalizedValue <= 0f)
        {
            _inputBarPointer.style.display = DisplayStyle.None;
            return;
        }
        else
        {
            _inputBarPointer.style.display = DisplayStyle.Flex;
            UpdatePointer(normalizedValue);
        }

    }

    private void UpdatePointer(float normalizedValue)
    {
        float remappedPercentage = Mathf.Lerp(0, maxInputBarPercentagePointer, normalizedValue);
        _inputBarPointer.style.bottom = Length.Percent(remappedPercentage);
    }

    private void UpdatePerfectShotHint(float normalizedValue)
    {
        float remappedPercentage = Mathf.Lerp(0, maxInputBarPercentageHint, normalizedValue);
        _inputBarPerfectHint.style.bottom = Length.Percent(remappedPercentage);
    }

    private void UpdateBackboardShotHint(float normalizedValue)
    {
        float remappedPercentage = Mathf.Lerp(0, maxInputBarPercentageHint, normalizedValue);
        _inputBarBackboardHint.style.bottom = Length.Percent(remappedPercentage);
    }

    // --- PUBLIC METHODS ---

    public void ShowMainMenu()
    {
        HideAllScreens();
        _mainMenuContainer.style.display = DisplayStyle.Flex;
    }

    public void ShowInGameUI()
    {
        HideAllScreens();
        _inGameContainer.style.display = DisplayStyle.Flex;
    }

    public void ShowRewardScreen(int finalScore, int highScore)
    {
        HideAllScreens();
        _finalScoreLabel.text = $"Final Score: {finalScore}";
        _highScoreLabel.text = $"High Score: {highScore}";
        _rewardContainer.style.display = DisplayStyle.Flex;
    }

    public void UpdateScore(int score)
    {
        _scoreLabel.text = score.ToString();
    }

    public void UpdateTimer(float time)
    {
        _timerLabel.text = time.ToString();
    }

    // --- EVENT HANDLERS ---

    private void OnPlayButtonClicked()
    {
        GameManager.Instance.StartGame();
    }

    private void OnPlayAgainButtonClicked()
    {
        GameManager.Instance.StartGame();
    }

    private void OnReturnToMenuButtonClicked()
    {
        GameManager.Instance.SetState(GameState.MainMenu);
    }

    // --- HELPER METHODS ---

    private void HideAllScreens()
    {
        _mainMenuContainer.style.display = DisplayStyle.None;
        _inGameContainer.style.display = DisplayStyle.None;
        _rewardContainer.style.display = DisplayStyle.None;
    }

    private void HandleShotPowerChanged(float powerPercentage)
    {
        // Converti da 0-100 a 0-1 per la barra
        float normalizedValue = powerPercentage / 100f;
        UpdateInputBar(normalizedValue);
    }

    private void HandleDragPowerUpdate(float powerPercentage)
    {
        // Converti da 0-100 a 0-1 per la barra
        float normalizedValue = powerPercentage / 100f;
        UpdateInputBar(normalizedValue);
    }
}