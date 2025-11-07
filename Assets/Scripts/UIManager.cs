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
    }

    private void OnEnable()
    {
        GameManager.OnGameStateChanged += HandleGameStateChanged;
        GameManager.OnScoreChanged += HandleScoreChanged;
        GameManager.OnTimerChanged += HandleTimerChanged;
    }

    private void OnDisable()
    {
        GameManager.OnGameStateChanged -= HandleGameStateChanged;
        GameManager.OnScoreChanged -= HandleScoreChanged;
        GameManager.OnTimerChanged -= HandleTimerChanged;
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
        _scoreLabel.text = $"Score\n{score}";
    }

    public void UpdateTimer(float time)
    {
        _timerLabel.text = $"Time\n{time:F0}";
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
}