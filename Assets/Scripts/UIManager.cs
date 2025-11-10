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

    // Human Player UI Elements
    private Label _scoreLabel;
    private Label _scoreDifferenceLabel;
    private VisualElement _inputBarColor;
    private VisualElement _inputBarPointer;
    private VisualElement _inputBarPerfectHint;
    private VisualElement _inputBarBackboardHint;
    private ProgressBar _fireballProgressBar;
    private VisualElement _fireballProgressContainer;
    private Label _fireballProgressLabel;

    // AI Player UI Elements
    private VisualElement _aiFlyer;
    private Label _aiScoreLabel;
    private ProgressBar _aiFireballProgressBar;
    private Label _aiFireballProgressLabel;

    private Label _timerLabel;
    private Label _finalScoreLabel;
    private Label _winnerLabel;
    private Button _playButton;
    private Button _playAgainButton;
    private Button _returnToMenuButton;

    [SerializeField] private int maxInputBarPercentagePointer = 97;
    [SerializeField] private int maxInputBarPercentageHint = 90;

    private int lastHumanScore = 0;
    private int lastAIScore = 0;

    private Coroutine humanFireballTimerCoroutine;
    private Coroutine aiFireballTimerCoroutine;

    private PlayableDirector _playableDirector;
    [SerializeField] private PlayableAsset scoreDifferenceAnimation;

    public override void Awake()
    {
        base.Awake();
        var root = GetComponent<UIDocument>().rootVisualElement;

        _mainMenuContainer = root.Q<VisualElement>("main-menu-container");
        _inGameContainer = root.Q<VisualElement>("in-game-container");
        _rewardContainer = root.Q<VisualElement>("reward-container");

        // Human Player UI
        _scoreLabel = root.Q<Label>("score-label");
        _scoreDifferenceLabel = root.Q<Label>("score-difference");
        _inputBarColor = root.Q<VisualElement>("input-bar-color");
        _inputBarPointer = root.Q<VisualElement>("input-bar-pointer");
        _inputBarPerfectHint = root.Q<VisualElement>("input-bar-perfect-hint");
        _inputBarBackboardHint = root.Q<VisualElement>("input-bar-backboard-hint");
        _fireballProgressBar = root.Q<ProgressBar>("fireball-progress-bar");
        _fireballProgressContainer = root.Q<VisualElement>("fireball-progress-container");
        _fireballProgressLabel = root.Q<Label>("fireball-label");

        // AI Player UI
        _aiFlyer = root.Q<VisualElement>("ai-column");
        _aiScoreLabel = root.Q<Label>("ai-score-label");
        _aiFireballProgressBar = root.Q<ProgressBar>("ai-fireball-progress-bar");
        _aiFireballProgressLabel = root.Q<Label>("ai-fireball-label");

        // Common UI
        _timerLabel = root.Q<Label>("timer-label");
        _finalScoreLabel = root.Q<Label>("final-score-label");
        _winnerLabel = root.Q<Label>("winner-label");

        _playButton = root.Q<Button>("play-button");
        _playAgainButton = root.Q<Button>("play-again-button");
        _returnToMenuButton = root.Q<Button>("return-to-menu-button");

        _playButton.clicked += OnPlayButtonClicked;
        _playAgainButton.clicked += OnPlayAgainButtonClicked;
        _returnToMenuButton.clicked += OnReturnToMenuButtonClicked;
    }

    private void OnEnable()
    {
        GameManager.OnGameStateChanged += HandleGameStateChanged;
        GameManager.OnScoreChanged += HandleScoreChanged;
        GameManager.OnTimerChanged += HandleTimerChanged;
        BallPhysics.OnShotPowerChanged += HandleShotPowerChanged;
        BallPhysics.OnDragPowerUpdated += HandleDragPowerUpdate;
        GameManager.positionReset += HandlePositionReset;

        // Aggiorna con il nuovo signature
        BallPhysics.OnPerfectShotCalculated += UpdatePerfectShotHint;
        BallPhysics.OnBackboardShotCalculated += UpdateBackboardShotHint;

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
        GameManager.positionReset -= HandlePositionReset;

        BallPhysics.OnPerfectShotCalculated -= UpdatePerfectShotHint;
        BallPhysics.OnBackboardShotCalculated -= UpdateBackboardShotHint;

        GameManager.OnFireballProgressChanged -= HandleFireballProgressChanged;
        GameManager.OnFireballModeActivated -= HandleFireballModeActivated;
        GameManager.OnFireballModeExpired -= HandleFireballModeExpired;
    }

    void Start()
    {
        HideAllScreens();
        ShowMainMenu();

        _playableDirector = GetComponentInChildren<PlayableDirector>();

        // Initially hide AI flyer
        if (_aiFlyer != null)
        {
            _aiFlyer.style.display = DisplayStyle.None;
        }
    }

    // --- GAME STATE HANDLERS ---

    private void HandleGameStateChanged(GameState newState)
    {
        HideAllScreens();

        switch (newState)
        {
            case GameState.MainMenu:
                ShowMainMenu();
                break;
            case GameState.Gameplay:
                ShowInGameUI();
                break;
            case GameState.Reward:
                ShowRewardScreen();
                break;
        }
    }

    private void HandlePositionReset(Transform spawnPoint, PlayerType playerType)
    {
        if (playerType == PlayerType.Human)
        {
            UpdateInputBar(0f);
        }
    }

    // --- SCORE HANDLERS ---

    private void HandleScoreChanged(int newScore, PlayerType playerType)
    {
        if (playerType == PlayerType.Human)
        {
            UpdateScore(newScore, _scoreLabel);

            int scoreDifference = newScore - lastHumanScore;
            lastHumanScore = newScore;

            if (_scoreDifferenceLabel != null)
            {
                _scoreDifferenceLabel.text = $"+{scoreDifference}";
                _playableDirector?.Play(scoreDifferenceAnimation);
            }
        }
        else if (playerType == PlayerType.AI)
        {
            UpdateScore(newScore, _aiScoreLabel);
        }
    }

    private void HandleTimerChanged(float newTime)
    {
        UpdateTimer(newTime);
    }

    // --- FIREBALL UI HANDLERS ---

    private void HandleFireballProgressChanged(int currentBaskets, int maxBaskets, PlayerType playerType)
    {
        ProgressBar progressBar = playerType == PlayerType.Human ? _fireballProgressBar : _aiFireballProgressBar;

        if (progressBar != null)
        {
            progressBar.value = currentBaskets;
            progressBar.highValue = maxBaskets;
            progressBar.title = $"{currentBaskets}/{maxBaskets}";
        }
    }

    private void HandleFireballModeActivated(float duration, PlayerType playerType)
    {
        if (playerType == PlayerType.Human)
        {
            ShowFireballActiveIndicator(_fireballProgressLabel);
            if (humanFireballTimerCoroutine != null)
            {
                StopCoroutine(humanFireballTimerCoroutine);
            }
            humanFireballTimerCoroutine = StartCoroutine(FireballTimer(duration, PlayerType.Human));
        }
        else if (playerType == PlayerType.AI)
        {
            ShowFireballActiveIndicator(_aiFireballProgressLabel);
            if (aiFireballTimerCoroutine != null)
            {
                StopCoroutine(aiFireballTimerCoroutine);
            }
            aiFireballTimerCoroutine = StartCoroutine(FireballTimer(duration, PlayerType.AI));
        }
    }

    private IEnumerator FireballTimer(float duration, PlayerType playerType)
    {
        float timeRemaining = duration;

        while (timeRemaining > 0f)
        {
            UpdateFireballTimer(timeRemaining, playerType);
            yield return null;
            timeRemaining -= Time.deltaTime;
        }

        UpdateFireballTimer(0f, playerType);
    }

    private void HandleFireballModeExpired(PlayerType playerType)
    {
        if (playerType == PlayerType.Human)
        {
            HideFireballActiveIndicator(_fireballProgressLabel);
            if (humanFireballTimerCoroutine != null)
            {
                StopCoroutine(humanFireballTimerCoroutine);
                humanFireballTimerCoroutine = null;
            }
            HandleFireballProgressChanged(0, GameManager.Instance.MaxBasketsForFireball, PlayerType.Human);
        }
        else if (playerType == PlayerType.AI)
        {
            HideFireballActiveIndicator(_aiFireballProgressLabel);
            if (aiFireballTimerCoroutine != null)
            {
                StopCoroutine(aiFireballTimerCoroutine);
                aiFireballTimerCoroutine = null;
            }
            HandleFireballProgressChanged(0, GameManager.Instance.MaxBasketsForFireball, PlayerType.AI);
        }
    }

    private void ShowFireballActiveIndicator(Label label)
    {
        if (label != null)
        {
            label.text = "FIREBALL ACTIVE!";
        }
    }

    private void HideFireballActiveIndicator(Label label)
    {
        if (label != null)
        {
            label.text = "FIREBALL BONUS";
        }
    }

    private void UpdateFireballTimer(float timeRemaining, PlayerType playerType)
    {
        ProgressBar progressBar = playerType == PlayerType.Human ? _fireballProgressBar : _aiFireballProgressBar;

        if (progressBar != null)
        {
            progressBar.value = timeRemaining;
            progressBar.highValue = GameManager.Instance.FireballDuration;
            progressBar.title = $"{timeRemaining:F1}s";
        }
    }

    // --- INPUT BAR HANDLERS ---

    private void UpdateInputBar(float normalizedValue)
    {
        if (_inputBarColor != null)
        {
            _inputBarColor.style.scale = new Scale(new Vector3(1, normalizedValue, 1));
        }

        if (_inputBarPointer != null)
        {
            if (normalizedValue <= 0f)
            {
                _inputBarPointer.style.display = DisplayStyle.None;
            }
            else
            {
                _inputBarPointer.style.display = DisplayStyle.Flex;
                UpdatePointer(normalizedValue);
            }
        }
    }

    private void UpdatePointer(float normalizedValue)
    {
        float remappedPercentage = Mathf.Lerp(0, maxInputBarPercentagePointer, normalizedValue);
        _inputBarPointer.style.bottom = Length.Percent(remappedPercentage);
    }

    private void UpdatePerfectShotHint(float normalizedValue, PlayerType playerType)
    {
        // Mostra solo per il giocatore umano
        if (playerType != PlayerType.Human) return;

        if (_inputBarPerfectHint != null)
        {
            float remappedPercentage = Mathf.Lerp(0, maxInputBarPercentageHint, normalizedValue / 100f);
            _inputBarPerfectHint.style.bottom = Length.Percent(remappedPercentage);
        }
    }

    private void UpdateBackboardShotHint(float normalizedValue, PlayerType playerType)
    {
        // Mostra solo per il giocatore umano
        if (playerType != PlayerType.Human) return;

        if (_inputBarBackboardHint != null)
        {
            float remappedPercentage = Mathf.Lerp(0, maxInputBarPercentageHint, normalizedValue / 100f);
            _inputBarBackboardHint.style.bottom = Length.Percent(remappedPercentage);
        }
    }

    // --- PUBLIC METHODS ---

    public void ShowMainMenu()
    {
        HideAllScreens();
        _mainMenuContainer.style.display = DisplayStyle.Flex;

        // Hide AI flyer in main menu
        if (_aiFlyer != null)
        {
            _aiFlyer.style.display = DisplayStyle.None;
        }
    }

    public void ShowInGameUI()
    {
        HideAllScreens();
        _inGameContainer.style.display = DisplayStyle.Flex;

        // Show AI flyer only in VersusAI mode
        if (_aiFlyer != null)
        {
            _aiFlyer.style.display = GameManager.Instance.CurrentGameMode == GameMode.VersusAI
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        // Reset scores
        lastHumanScore = 0;
        lastAIScore = 0;
    }

    public void ShowRewardScreen()
    {
        HideAllScreens();
        _rewardContainer.style.display = DisplayStyle.Flex;

        if (GameManager.Instance.CurrentGameMode == GameMode.VersusAI)
        {
            if (_finalScoreLabel != null)
            {
                _finalScoreLabel.text = $"Human: {GameManager.Instance.HumanScore} | AI: {GameManager.Instance.AIScore}";
            }

            if (_winnerLabel != null)
            {
                _winnerLabel.text = GameManager.Instance.GetWinner();
                _winnerLabel.style.display = DisplayStyle.Flex;
            }
        }
        else
        {
            if (_finalScoreLabel != null)
            {
                _finalScoreLabel.text = $"Final Score: {GameManager.Instance.HumanScore}";
            }

            if (_winnerLabel != null)
            {
                _winnerLabel.style.display = DisplayStyle.None;
            }
        }
    }

    public void UpdateScore(int score, Label scoreLabel)
    {
        if (scoreLabel != null)
        {
            scoreLabel.text = score.ToString();
        }
    }

    public void UpdateTimer(float time)
    {
        if (_timerLabel != null)
        {
            _timerLabel.text = time.ToString();
        }
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
        // Convert from 0-100 to 0-1 for the bar
        float normalizedValue = powerPercentage / 100f;
        UpdateInputBar(normalizedValue);
    }

    private void HandleDragPowerUpdate(float powerPercentage)
    {
        // Convert from 0-100 to 0-1 for the bar
        float normalizedValue = powerPercentage / 100f;
        UpdateInputBar(normalizedValue);
    }
}