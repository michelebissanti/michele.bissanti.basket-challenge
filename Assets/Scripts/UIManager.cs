using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UIElements;

/// <summary>
/// Manages all UI elements and screens using Unity's UI Toolkit.
/// Handles transitions between game states, score display, timer, input bar visualization, and fireball mode UI.
/// </summary>
public class UIManager : Singleton<UIManager>
{
    #region Serialized Fields

    /// <summary>Maximum percentage for the input bar pointer position (to prevent overflow).</summary>
    [SerializeField] private int maxInputBarPercentagePointer = 97;

    /// <summary>Maximum percentage for the input bar hint indicators position.</summary>
    [SerializeField] private int maxInputBarPercentageHint = 90;

    /// <summary>Animation played when score increases.</summary>
    [SerializeField] private PlayableAsset scoreDifferenceAnimation;

    #endregion

    #region Private Fields - UI Containers

    /// <summary>Main menu screen container.</summary>
    private VisualElement _mainMenuContainer;

    /// <summary>In-game UI container (score, timer, input bar).</summary>
    private VisualElement _inGameContainer;

    /// <summary>Reward screen container (shown at game end).</summary>
    private VisualElement _rewardContainer;

    #endregion

    #region Private Fields - UI Labels

    /// <summary>Current score label in gameplay.</summary>
    private Label _scoreLabel;

    /// <summary>High score label shown in reward screen.</summary>
    private Label _highScoreLabel;

    /// <summary>Timer label showing remaining time.</summary>
    private Label _timerLabel;

    /// <summary>Final score label in reward screen.</summary>
    private Label _finalScoreLabel;

    /// <summary>Label showing score difference after each basket.</summary>
    private Label _scoreDifferenceLabel;

    /// <summary>Label showing fireball mode status.</summary>
    private Label _fireballProgressLabel;

    #endregion

    #region Private Fields - UI Buttons

    /// <summary>Play button in main menu.</summary>
    private Button _playButton;

    /// <summary>Play again button in reward screen.</summary>
    private Button _playAgainButton;

    /// <summary>Return to menu button in reward screen.</summary>
    private Button _returnToMenuButton;

    #endregion

    #region Private Fields - Input Bar Elements

    /// <summary>Visual element showing input bar fill color.</summary>
    private VisualElement _inputBarColor;

    /// <summary>Pointer indicating current drag power on input bar.</summary>
    private VisualElement _inputBarPointer;

    /// <summary>Hint indicator showing perfect shot power level.</summary>
    private VisualElement _inputBarPerfectHint;

    /// <summary>Hint indicator showing backboard shot power level.</summary>
    private VisualElement _inputBarBackboardHint;

    #endregion

    #region Private Fields - Fireball UI

    /// <summary>Progress bar showing fireball progress or duration.</summary>
    private ProgressBar _fireballProgressBar;

    /// <summary>Container for fireball progress UI elements.</summary>
    private VisualElement _fireballProgressContainer;

    /// <summary>Coroutine reference for fireball timer countdown.</summary>
    private Coroutine fireballTimerCoroutine;

    #endregion

    #region Private Fields - State Tracking

    /// <summary>Stores the last recorded score to calculate score differences.</summary>
    private int lastScore = 0;

    /// <summary>PlayableDirector for controlling UI animations.</summary>
    private PlayableDirector _playableDirector;

    #endregion

    #region Unity Lifecycle Methods

    /// <summary>
    /// Initializes the UI Manager and queries all UI elements from the UI Document.
    /// Sets up button click listeners.
    /// </summary>
    public override void Awake()
    {
        base.Awake();
        var root = GetComponent<UIDocument>().rootVisualElement;

        // Query containers
        _mainMenuContainer = root.Q<VisualElement>("main-menu-container");
        _inGameContainer = root.Q<VisualElement>("in-game-container");
        _rewardContainer = root.Q<VisualElement>("reward-container");

        // Query labels
        _scoreLabel = root.Q<Label>("score-label");
        _highScoreLabel = root.Q<Label>("high-score-label");
        _timerLabel = root.Q<Label>("timer-label");
        _finalScoreLabel = root.Q<Label>("final-score-label");
        _scoreDifferenceLabel = root.Q<Label>("score-difference");
        _fireballProgressLabel = root.Q<Label>("fireball-label");

        // Query buttons
        _playButton = root.Q<Button>("play-button");
        _playAgainButton = root.Q<Button>("play-again-button");
        _returnToMenuButton = root.Q<Button>("return-to-menu-button");

        // Subscribe to button clicks
        _playButton.clicked += OnPlayButtonClicked;
        _playAgainButton.clicked += OnPlayAgainButtonClicked;
        _returnToMenuButton.clicked += OnReturnToMenuButtonClicked;

        // Query input bar elements
        _inputBarColor = root.Q<VisualElement>("input-bar-color");
        _inputBarPointer = root.Q<VisualElement>("input-bar-pointer");
        _inputBarPerfectHint = root.Q<VisualElement>("input-bar-perfect-hint");
        _inputBarBackboardHint = root.Q<VisualElement>("input-bar-backboard-hint");

        // Query fireball elements
        _fireballProgressBar = root.Q<ProgressBar>("fireball-progress-bar");
        _fireballProgressContainer = root.Q<VisualElement>("fireball-progress-container");
    }

    /// <summary>
    /// Subscribes to game events when the component is enabled.
    /// </summary>
    private void OnEnable()
    {
        // Game state and core gameplay events
        GameManager.OnGameStateChanged += HandleGameStateChanged;
        GameManager.OnScoreChanged += HandleScoreChanged;
        GameManager.OnTimerChanged += HandleTimerChanged;
        GameManager.positionReset += (Transform spawnPoint) => UpdateInputBar(0f);

        // Ball physics events
        BallPhysics.OnShotPowerChanged += HandleShotPowerChanged;
        BallPhysics.OnDragPowerUpdated += HandleDragPowerUpdate;
        BallPhysics.OnPerfectShotCalculated += UpdatePerfectShotHint;
        BallPhysics.OnBackboardShotCalculated += UpdateBackboardShotHint;

        // Fireball mode events
        GameManager.OnFireballProgressChanged += HandleFireballProgressChanged;
        GameManager.OnFireballModeActivated += HandleFireballModeActivated;
        GameManager.OnFireballModeExpired += HandleFireballModeExpired;
    }

    /// <summary>
    /// Unsubscribes from game events when the component is disabled.
    /// Prevents memory leaks and null reference exceptions.
    /// </summary>
    private void OnDisable()
    {
        // Game state and core gameplay events
        GameManager.OnGameStateChanged -= HandleGameStateChanged;
        GameManager.OnScoreChanged -= HandleScoreChanged;
        GameManager.OnTimerChanged -= HandleTimerChanged;
        GameManager.positionReset -= (Transform spawnPoint) => UpdateInputBar(0f);

        // Ball physics events
        BallPhysics.OnShotPowerChanged -= HandleShotPowerChanged;
        BallPhysics.OnDragPowerUpdated -= HandleDragPowerUpdate;
        BallPhysics.OnPerfectShotCalculated -= UpdatePerfectShotHint;
        BallPhysics.OnBackboardShotCalculated -= UpdateBackboardShotHint;

        // Fireball mode events
        GameManager.OnFireballProgressChanged -= HandleFireballProgressChanged;
        GameManager.OnFireballModeActivated -= HandleFireballModeActivated;
        GameManager.OnFireballModeExpired -= HandleFireballModeExpired;
    }

    /// <summary>
    /// Initializes the UI state on start, showing the main menu.
    /// Gets the PlayableDirector component for animations.
    /// </summary>
    void Start()
    {
        HideAllScreens();
        ShowMainMenu();

        _playableDirector = GetComponentInChildren<PlayableDirector>();
    }

    #endregion

    #region Event Handlers - Game State

    /// <summary>
    /// Handles game state changes by showing the appropriate UI screen.
    /// </summary>
    /// <param name="newState">The new game state.</param>
    private void HandleGameStateChanged(GameState newState)
    {
        HideAllScreens();

        // Show the appropriate screen based on the new state
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

    /// <summary>
    /// Handles score changes by updating the score display and showing the score difference animation.
    /// </summary>
    /// <param name="newScore">The new score value.</param>
    private void HandleScoreChanged(int newScore)
    {
        UpdateScore(newScore);

        // Calculate and display score difference
        int scoreDifference = newScore - lastScore;
        lastScore = newScore;

        if (scoreDifference <= 0) return;

        // Show score difference with animation
        _scoreDifferenceLabel.text = $"+{scoreDifference}";
        _playableDirector.Play(scoreDifferenceAnimation);
    }

    /// <summary>
    /// Handles timer changes by updating the timer display.
    /// </summary>
    /// <param name="newTime">The new time value.</param>
    private void HandleTimerChanged(float newTime)
    {
        UpdateTimer(newTime);
    }

    #endregion

    #region Event Handlers - Input Bar

    /// <summary>
    /// Handles shot power changes from ball physics, updating the input bar visualization.
    /// </summary>
    /// <param name="powerPercentage">Power percentage from 0 to 100.</param>
    private void HandleShotPowerChanged(float powerPercentage)
    {
        // Convert from 0-100 to 0-1 for the bar
        float normalizedValue = powerPercentage / 100f;
        UpdateInputBar(normalizedValue);
    }

    /// <summary>
    /// Handles drag power updates during player input, updating the input bar in real-time.
    /// </summary>
    /// <param name="powerPercentage">Power percentage from 0 to 100.</param>
    private void HandleDragPowerUpdate(float powerPercentage)
    {
        // Convert from 0-100 to 0-1 for the bar
        float normalizedValue = powerPercentage / 100f;
        UpdateInputBar(normalizedValue);
    }

    #endregion

    #region Event Handlers - Fireball Mode

    /// <summary>
    /// Handles fireball progress changes by updating the progress bar display.
    /// </summary>
    /// <param name="currentBaskets">Current number of consecutive baskets.</param>
    /// <param name="maxBaskets">Maximum baskets needed to activate fireball mode.</param>
    private void HandleFireballProgressChanged(int currentBaskets, int maxBaskets)
    {
        _fireballProgressBar.value = currentBaskets;
        _fireballProgressBar.highValue = maxBaskets;

        _fireballProgressBar.title = $"{currentBaskets}/{maxBaskets}";
    }

    /// <summary>
    /// Handles fireball mode activation by showing the active indicator and starting the timer.
    /// </summary>
    /// <param name="duration">Duration of the fireball mode in seconds.</param>
    private void HandleFireballModeActivated(float duration)
    {
        ShowFireballActiveIndicator();
        fireballTimerCoroutine = StartCoroutine(FireballTimer(duration));
    }

    /// <summary>
    /// Handles fireball mode expiration by hiding the indicator and resetting progress.
    /// </summary>
    private void HandleFireballModeExpired()
    {
        HideFireballActiveIndicator();

        if (fireballTimerCoroutine != null)
        {
            StopCoroutine(fireballTimerCoroutine);
            fireballTimerCoroutine = null;
        }

        // Reset progress display
        HandleFireballProgressChanged(0, GameManager.Instance.MaxBasketsForFireball);
    }

    #endregion

    #region Event Handlers - Button Clicks

    /// <summary>
    /// Handles the play button click, starting a new game.
    /// </summary>
    private void OnPlayButtonClicked()
    {
        GameManager.Instance.StartGame();
    }

    /// <summary>
    /// Handles the play again button click, restarting the game.
    /// </summary>
    private void OnPlayAgainButtonClicked()
    {
        GameManager.Instance.StartGame();
    }

    /// <summary>
    /// Handles the return to menu button click, transitioning to the main menu.
    /// </summary>
    private void OnReturnToMenuButtonClicked()
    {
        GameManager.Instance.SetState(GameState.MainMenu);
    }

    #endregion

    #region Public Methods - Screen Management

    /// <summary>
    /// Shows the main menu screen, hiding all other screens.
    /// </summary>
    public void ShowMainMenu()
    {
        HideAllScreens();
        _mainMenuContainer.style.display = DisplayStyle.Flex;
    }

    /// <summary>
    /// Shows the in-game UI (score, timer, input bar), hiding all other screens.
    /// </summary>
    public void ShowInGameUI()
    {
        HideAllScreens();
        _inGameContainer.style.display = DisplayStyle.Flex;
    }

    /// <summary>
    /// Shows the reward screen with final score and high score, hiding all other screens.
    /// </summary>
    /// <param name="finalScore">The final score achieved in the game.</param>
    /// <param name="highScore">The current high score.</param>
    public void ShowRewardScreen(int finalScore, int highScore)
    {
        HideAllScreens();
        _finalScoreLabel.text = $"Final Score: {finalScore}";
        _highScoreLabel.text = $"High Score: {highScore}";
        _rewardContainer.style.display = DisplayStyle.Flex;
    }

    /// <summary>
    /// Updates the score display with the current score value.
    /// </summary>
    /// <param name="score">The score to display.</param>
    public void UpdateScore(int score)
    {
        _scoreLabel.text = score.ToString();
    }

    /// <summary>
    /// Updates the timer display with the current time value.
    /// </summary>
    /// <param name="time">The time to display.</param>
    public void UpdateTimer(float time)
    {
        _timerLabel.text = time.ToString();
    }

    #endregion

    #region Private Methods - Input Bar Visualization

    /// <summary>
    /// Updates the input bar fill and pointer based on the normalized power value.
    /// </summary>
    /// <param name="normalizedValue">Normalized power value from 0 to 1.</param>
    private void UpdateInputBar(float normalizedValue)
    {
        // Scale the input bar color fill
        _inputBarColor.style.scale = new Scale(new Vector3(1, normalizedValue, 1));

        // Show or hide the pointer based on power value
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

    /// <summary>
    /// Updates the position of the input bar pointer.
    /// </summary>
    /// <param name="normalizedValue">Normalized power value from 0 to 1.</param>
    private void UpdatePointer(float normalizedValue)
    {
        float remappedPercentage = Mathf.Lerp(0, maxInputBarPercentagePointer, normalizedValue);
        _inputBarPointer.style.bottom = Length.Percent(remappedPercentage);
    }

    /// <summary>
    /// Updates the position of the perfect shot hint indicator on the input bar.
    /// </summary>
    /// <param name="normalizedValue">Normalized power value from 0 to 1 for perfect shot.</param>
    private void UpdatePerfectShotHint(float normalizedValue)
    {
        float remappedPercentage = Mathf.Lerp(0, maxInputBarPercentageHint, normalizedValue);
        _inputBarPerfectHint.style.bottom = Length.Percent(remappedPercentage);
    }

    /// <summary>
    /// Updates the position of the backboard shot hint indicator on the input bar.
    /// </summary>
    /// <param name="normalizedValue">Normalized power value from 0 to 1 for backboard shot.</param>
    private void UpdateBackboardShotHint(float normalizedValue)
    {
        float remappedPercentage = Mathf.Lerp(0, maxInputBarPercentageHint, normalizedValue);
        _inputBarBackboardHint.style.bottom = Length.Percent(remappedPercentage);
    }

    #endregion

    #region Private Methods - Fireball UI

    /// <summary>
    /// Shows the fireball active indicator, changing the label text.
    /// </summary>
    private void ShowFireballActiveIndicator()
    {
        _fireballProgressLabel.text = "FIREBALL ACTIVE!";
    }

    /// <summary>
    /// Hides the fireball active indicator, resetting the label text to default.
    /// </summary>
    private void HideFireballActiveIndicator()
    {
        _fireballProgressLabel.text = "FIREBALL BONUS";
    }

    /// <summary>
    /// Updates the fireball timer display with the remaining time.
    /// </summary>
    /// <param name="timeRemaining">Time remaining in seconds.</param>
    private void UpdateFireballTimer(float timeRemaining)
    {
        _fireballProgressBar.value = timeRemaining;
        _fireballProgressBar.highValue = GameManager.Instance.FireballDuration;

        _fireballProgressBar.title = $"{timeRemaining:F1}s";
    }

    #endregion

    #region Coroutines

    /// <summary>
    /// Coroutine that counts down the fireball mode duration and updates the UI.
    /// </summary>
    /// <param name="duration">Total duration of fireball mode in seconds.</param>
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

    #endregion

    #region Helper Methods

    /// <summary>
    /// Hides all UI screens (main menu, in-game, reward).
    /// Used before showing a specific screen to ensure clean transitions.
    /// </summary>
    private void HideAllScreens()
    {
        _mainMenuContainer.style.display = DisplayStyle.None;
        _inGameContainer.style.display = DisplayStyle.None;
        _rewardContainer.style.display = DisplayStyle.None;
    }

    #endregion
}
