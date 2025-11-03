using System;
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
    }

    private void OnDisable()
    {
        playerActions.Player.Disable();

        playerActions.Player.PrimaryContact.started -= OnContactStarted;
        playerActions.Player.PrimaryContact.canceled -= OnContactCanceled;
    }

    private void OnContactStarted(InputAction.CallbackContext context)
    {
        isDragging = true;

        // Leggi la posizione corrente dall'azione "ContactPosition"
        startDragPosition = playerActions.Player.ContactPosition.ReadValue<Vector2>();

        // Notifica al gioco che il trascinamento è iniziato
        OnStartDrag?.Invoke(startDragPosition);
    }

    private void OnContactCanceled(InputAction.CallbackContext context)
    {
        isDragging = false;

        // Leggi la posizione finale
        Vector2 endPosition = playerActions.Player.ContactPosition.ReadValue<Vector2>();

        // Calcola il vettore di trascinamento finale
        Vector2 dragVector = endPosition - startDragPosition;

        // Notifica al gioco la fine del trascinamento e il vettore risultante
        OnEndDrag?.Invoke(dragVector);
    }

    private void Update()
    {
        // Se non stiamo trascinando, non fare nulla
        if (!isDragging) return;

        // Finché stiamo trascinando, leggi la posizione corrente
        Vector2 currentPosition = playerActions.Player.ContactPosition.ReadValue<Vector2>();

        // Notifica la posizione corrente (utile per UI, linee di mira, ecc.)
        OnDrag?.Invoke(currentPosition);
    }
}