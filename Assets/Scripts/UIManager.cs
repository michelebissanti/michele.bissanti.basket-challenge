using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class UIManager : Singleton<UIManager>
{
    private VisualElement _mainMenuContainer;
    private VisualElement _inGameContainer;
    private VisualElement _rewardContainer;

    private Label _scoreLabel;
    private Label _timerLabel;
    private Label _finalScoreLabel;
    private Button _playButton;
    private Button _playAgainButton;
    private Button _returnToMenuButton;

    private VisualElement _inputBarColor;
    private VisualElement _inputBarPointer;
    private VisualElement _inputBarPerfectHint;
    private VisualElement _inputBarBackboardHint;

    [SerializeField] private int maxInputBarPercentagePointer = 97;

    [SerializeField] private int maxInputBarPercentageHint = 90;




    public override void Awake()
    {
        base.Awake();
        var root = GetComponent<UIDocument>().rootVisualElement;

        _mainMenuContainer = root.Q<VisualElement>("main-menu-container");
        _inGameContainer = root.Q<VisualElement>("in-game-container");
        _rewardContainer = root.Q<VisualElement>("reward-container");

        _scoreLabel = root.Q<Label>("score-label");
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
    }

    private void OnEnable()
    {
        GameManager.OnGameStateChanged += HandleGameStateChanged;
        GameManager.OnScoreChanged += HandleScoreChanged;
        GameManager.OnTimerChanged += HandleTimerChanged;
        BallPhysics.OnShotPowerChanged += HandleShotPowerChanged;
        GameManager.positionReset += (Transform spawnPoint) => UpdateInputBar(0f);
        BallPhysics.OnPerfectShotCalculated += UpdatePerfectShotHint;
        BallPhysics.OnBackboardShotCalculated += UpdateBackboardShotHint;
    }

    private void OnDisable()
    {
        GameManager.OnGameStateChanged -= HandleGameStateChanged;
        GameManager.OnScoreChanged -= HandleScoreChanged;
        GameManager.OnTimerChanged -= HandleTimerChanged;
        BallPhysics.OnShotPowerChanged -= HandleShotPowerChanged;
        GameManager.positionReset -= (Transform spawnPoint) => UpdateInputBar(0f);
        BallPhysics.OnPerfectShotCalculated -= UpdatePerfectShotHint;
        BallPhysics.OnBackboardShotCalculated -= UpdateBackboardShotHint;
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
                ShowRewardScreen(GameManager.Instance.Score);
                break;
        }
    }

    private void HandleScoreChanged(int newScore)
    {
        UpdateScore(newScore);
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
            float remappedPercentage = Mathf.Lerp(0, maxInputBarPercentagePointer, normalizedValue);
            _inputBarPointer.style.bottom = Length.Percent(Mathf.Min(remappedPercentage, maxInputBarPercentagePointer));
        }

    }

    private void UpdatePerfectShotHint(float normalizedValue)
    {
        float remappedPercentage = Mathf.Lerp(0, maxInputBarPercentageHint, normalizedValue);
        _inputBarPerfectHint.style.bottom = Length.Percent(remappedPercentage);
        Debug.LogWarning($"Perfect shot hint updated to: {remappedPercentage}%");
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

    public void ShowRewardScreen(int finalScore)
    {
        HideAllScreens();
        _finalScoreLabel.text = $"Final Score: {finalScore}";
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
}