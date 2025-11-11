using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Manages player input through the new Input System.
/// Handles drag gestures for ball throwing mechanics and visual trail feedback.
/// </summary>
public class InputManager : Singleton<InputManager>
{
    #region Events

    /// <summary>Event triggered when a drag gesture starts.</summary>
    public static event Action<Vector2> OnStartDrag;

    /// <summary>Event triggered when a drag gesture ends.</summary>
    public static event Action<Vector2> OnEndDrag;

    /// <summary>Event triggered continuously during dragging.</summary>
    public static event Action<Vector2> OnDrag;

    #endregion

    #region Serialized Fields

    /// <summary>Visual trail GameObject shown during drag gestures.</summary>
    [SerializeField] private GameObject trail;

    /// <summary>Minimum distance required for a valid drag gesture (in pixels).</summary>
    [SerializeField] private float minimumDistance = 50f;

    /// <summary>Maximum time allowed for a valid drag gesture (in seconds).</summary>
    [SerializeField] private float maximumTime = 0.7f;

    #endregion

    #region Public Properties

    /// <summary>Gets the screen position where the drag started.</summary>
    public Vector2 StartDragPosition => startDragPosition;

    #endregion

    #region Private Fields

    /// <summary>Player input actions from the Input System.</summary>
    private PlayerInputActions playerActions;

    /// <summary>Flag indicating if a drag gesture is currently active.</summary>
    private bool isDragging = false;

    /// <summary>Screen position where the current drag started.</summary>
    private Vector2 startDragPosition;

    /// <summary>Time when the current drag started.</summary>
    private float dragStartTime;

    /// <summary>Last valid position during the drag (before time limit).</summary>
    private Vector2 lastValidPosition;

    /// <summary>Coroutine reference for the visual trail update.</summary>
    private Coroutine trailCoroutine;

    /// <summary>Flag indicating if the game is in gameplay state.</summary>
    private bool inGameplay;

    #endregion

    #region Unity Lifecycle Methods

    /// <summary>
    /// Initializes the Input Manager and creates the player input actions.
    /// </summary>
    public override void Awake()
    {
        base.Awake();
        playerActions = new PlayerInputActions();
    }

    /// <summary>
    /// Enables player input and subscribes to input and game events.
    /// </summary>
    private void OnEnable()
    {
        playerActions.Player.Enable();
        playerActions.Player.PrimaryContact.started += OnContactStarted;
        playerActions.Player.PrimaryContact.canceled += OnContactCanceled;
        GameManager.OnGameStateChanged += HandleGameStateChanged;
    }

    /// <summary>
    /// Disables player input and unsubscribes from events.
    /// Prevents memory leaks and null reference exceptions.
    /// </summary>
    private void OnDisable()
    {
        playerActions.Player.Disable();
        playerActions.Player.PrimaryContact.started -= OnContactStarted;
        playerActions.Player.PrimaryContact.canceled -= OnContactCanceled;
        GameManager.OnGameStateChanged -= HandleGameStateChanged;
    }

    /// <summary>
    /// Updates drag state each frame, checking for time limits and position changes.
    /// </summary>
    private void Update()
    {
        if (!isDragging) return;

        Vector2 currentPosition = playerActions.Player.ContactPosition.ReadValue<Vector2>();
        float elapsedTime = Time.time - dragStartTime;

        if (elapsedTime <= maximumTime)
        {
            lastValidPosition = currentPosition;
            OnDrag?.Invoke(currentPosition);
        }
        else
        {
            // Time limit exceeded - cancel the drag
            OnContactCanceled(new InputAction.CallbackContext());
        }
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Handles game state changes to enable/disable gameplay-specific input features.
    /// </summary>
    /// <param name="newState">The new game state.</param>
    private void HandleGameStateChanged(GameState newState)
    {
        if (newState == GameState.Gameplay)
        {
            inGameplay = true;
        }
        else
        {
            inGameplay = false;
        }
    }

    /// <summary>
    /// Handles the start of a touch/click input, initializing the drag gesture.
    /// </summary>
    /// <param name="context">Input action callback context.</param>
    private void OnContactStarted(InputAction.CallbackContext context)
    {
        isDragging = true;
        dragStartTime = Time.time;
        startDragPosition = playerActions.Player.ContactPosition.ReadValue<Vector2>();
        lastValidPosition = startDragPosition;

        OnStartDrag?.Invoke(startDragPosition);

        if (inGameplay)
        {
            trail.SetActive(true);
            trail.transform.position = Camera.main.ScreenToWorldPoint(new Vector3(startDragPosition.x, startDragPosition.y, 10f));
            trailCoroutine = StartCoroutine(Trail());

        }

    }

    /// <summary>
    /// Handles the end of a touch/click input, completing or canceling the drag gesture.
    /// Only triggers OnEndDrag if minimum distance threshold is met.
    /// </summary>
    /// <param name="context">Input action callback context.</param>
    private void OnContactCanceled(InputAction.CallbackContext context)
    {
        if (!isDragging) return;

        isDragging = false;

        trail.SetActive(false);
        if (trailCoroutine != null)
        {
            StopCoroutine(trailCoroutine);
        }

        Vector2 dragVector = lastValidPosition - startDragPosition;
        float dragDistance = dragVector.magnitude;

        // Only trigger end drag event if minimum distance requirement is met
        if (dragDistance >= minimumDistance)
        {
            OnEndDrag?.Invoke(dragVector);
        }
    }

    #endregion

    #region Coroutines

    /// <summary>
    /// Coroutine that continuously updates the visual trail position to follow the drag input.
    /// </summary>
    private IEnumerator Trail()
    {
        while (true)
        {
            trail.transform.position = Camera.main.ScreenToWorldPoint(new Vector3(playerActions.Player.ContactPosition.ReadValue<Vector2>().x, playerActions.Player.ContactPosition.ReadValue<Vector2>().y, 10f));
            yield return null;
        }
    }

    #endregion
}
