using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : Singleton<InputManager>
{
    private PlayerInputActions playerActions;

    public static event Action<Vector2> OnStartDrag;
    public static event Action<Vector2> OnEndDrag;
    public static event Action<Vector2> OnDrag;

    private bool isDragging = false;
    private Vector2 startDragPosition;
    private float dragStartTime;
    private Vector2 lastValidPosition;

    public Vector2 StartDragPosition => startDragPosition;

    [SerializeField] private GameObject trail;
    [SerializeField] private float minimumDistance = 50f;
    [SerializeField] private float maximumTime = 0.7f;

    private Coroutine trailCoroutine;
    private bool inGameplay;

    public override void Awake()
    {
        base.Awake();
        playerActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        playerActions.Player.Enable();
        playerActions.Player.PrimaryContact.started += OnContactStarted;
        playerActions.Player.PrimaryContact.canceled += OnContactCanceled;
        GameManager.OnGameStateChanged += HandleGameStateChanged;
    }

    private void OnDisable()
    {
        playerActions.Player.Disable();
        playerActions.Player.PrimaryContact.started -= OnContactStarted;
        playerActions.Player.PrimaryContact.canceled -= OnContactCanceled;
        GameManager.OnGameStateChanged -= HandleGameStateChanged;
    }

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

    private IEnumerator Trail()
    {
        while (true)
        {
            trail.transform.position = Camera.main.ScreenToWorldPoint(new Vector3(playerActions.Player.ContactPosition.ReadValue<Vector2>().x, playerActions.Player.ContactPosition.ReadValue<Vector2>().y, 10f));
            yield return null;
        }
    }

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

        if (dragDistance >= minimumDistance)
        {
            OnEndDrag?.Invoke(dragVector);
        }
    }

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
            OnContactCanceled(new InputAction.CallbackContext());
        }
    }
}