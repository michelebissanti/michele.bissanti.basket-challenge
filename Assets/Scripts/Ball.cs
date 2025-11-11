using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static AudioManager;

/// <summary>
/// Manages ball physics, collision detection, and scoring mechanics.
/// Handles ball state tracking for different shot types (perfect, standard, backboard bonus).
/// </summary>
public class Ball : MonoBehaviour
{
    #region Events

    /// <summary>Event triggered when the ball goes out of play (hits the ground twice).</summary>
    public static event Action BallOutOfPlay;

    /// <summary>Event triggered when the ball position is reset.</summary>
    public static event Action OnBallReset;

    #endregion

    #region Serialized Fields

    /// <summary>Offset position of the ball relative to the gameplay camera.</summary>
    [SerializeField] private Vector3 cameraOffset = new Vector3(0, -0.4f, 2f);

    [Header("Trail Settings")]
    [Header("Normal Trail Settings")]
    /// <summary>Color of the trail during normal gameplay.</summary>
    [SerializeField] private Color normalTrailColor = Color.white;

    /// <summary>Material used for the normal trail effect.</summary>
    [SerializeField] private Material normalTrailMaterial;

    /// <summary>Duration of the normal trail in seconds.</summary>
    [SerializeField] private float normalTrailTime;

    [Header("Fireball Trail Settings")]
    /// <summary>Starting color of the trail during fireball mode.</summary>
    [SerializeField] private Color fireballTrailStartColor = Color.red;

    /// <summary>Ending color of the trail during fireball mode.</summary>
    [SerializeField] private Color fireballTrailEndColor = new Color(1f, 0.5f, 0f);

    /// <summary>Material used for the fireball trail effect.</summary>
    [SerializeField] private Material fireballTrailMaterial;

    /// <summary>Duration of the fireball trail in seconds.</summary>
    [SerializeField] private float fireballTrailTime;

    #endregion

    #region Private Fields

    /// <summary>Indicates whether the ball has touched the ring.</summary>
    private bool ringTouched = false;

    /// <summary>Indicates whether the ball has touched the backboard.</summary>
    private bool backboardTouched = false;

    /// <summary>Indicates whether the ball has touched the ground.</summary>
    private bool groundTouched = false;

    /// <summary>Rigidbody component for physics simulation.</summary>
    private Rigidbody rb;

    /// <summary>Indicates whether the backboard bonus scoring mode is currently active.</summary>
    private bool backboardBonusActive = false;

    /// <summary>TrailRenderer component for visual trail effects.</summary>
    private TrailRenderer trailRenderer;

    /// <summary>Tracks whether the ball has passed through the upper basket trigger.</summary>
    private bool hasPassedUpperTrigger = false;

    #endregion

    #region Unity Lifecycle Methods

    /// <summary>
    /// Initializes ball components.
    /// </summary>
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        trailRenderer = GetComponent<TrailRenderer>();
    }

    /// <summary>
    /// Subscribes to game events when the component is enabled.
    /// </summary>
    private void OnEnable()
    {
        CameraManager.GamePlayCameraReady += ReturnToStart;
        GameManager.OnBackboardBonusActivated += OnBackboardBonusActivated;
        GameManager.OnBackboardBonusExpired += OnBackboardBonusExpired;

        GameManager.OnFireballModeActivated += SetFireballTrail;
        GameManager.OnFireballModeExpired += SetNormalTrail;
    }

    /// <summary>
    /// Unsubscribes from game events when the component is disabled.
    /// Prevents memory leaks and null reference exceptions.
    /// </summary>
    private void OnDisable()
    {
        CameraManager.GamePlayCameraReady -= ReturnToStart;
        GameManager.OnBackboardBonusActivated -= OnBackboardBonusActivated;
        GameManager.OnBackboardBonusExpired -= OnBackboardBonusExpired;
        GameManager.OnFireballModeActivated -= SetFireballTrail;
        GameManager.OnFireballModeExpired -= SetNormalTrail;
    }

    #endregion

    #region Collision and Trigger Handlers

    /// <summary>
    /// Handles trigger collisions with basket zones.
    /// Determines the score type based on ball state (ring touched, backboard touched, perfect shot).
    /// </summary>
    /// <param name="other">The collider that triggered the event.</param>
    void OnTriggerEnter(Collider other)
    {
        // Track when ball passes through the upper basket trigger
        if (other.CompareTag("Basket"))
        {
            hasPassedUpperTrigger = true;
            // Disable trail to avoid visual artifacts
            trailRenderer.enabled = false;
        }

        // Score detection logic - only triggers when ball passes through lower basket zone
        if (other.CompareTag("BasketLow") && hasPassedUpperTrigger && GameManager.Instance.GameState == GameState.Gameplay)
        {
            // Standard score: Ball touched ring but not backboard
            if (ringTouched && !backboardTouched)
            {
                GameManager.Instance.SetStandardScore();
            }

            // Backboard score: Ball touched backboard
            if (backboardTouched)
            {
                if (backboardBonusActive)
                {
                    GameManager.Instance.SetBackboardScore();
                }
                else
                {
                    GameManager.Instance.SetStandardScore();
                }
            }

            // Perfect score: Clean shot without touching ring or backboard
            if (!ringTouched && !backboardTouched)
            {
                GameManager.Instance.SetPerfectScore();
            }

            // Reset state flags for next shot
            ringTouched = false;
            backboardTouched = false;
            hasPassedUpperTrigger = false;
        }
    }

    /// <summary>
    /// Handles physical collisions with ring, backboard, and ground.
    /// Tracks ball state for scoring calculations.
    /// </summary>
    /// <param name="collision">The collision data.</param>
    void OnCollisionEnter(Collision collision)
    {
        // Track ring collision for scoring
        if (collision.gameObject.CompareTag("Ring"))
        {
            Debug.Log("Ball touched the ring!");
            ringTouched = true;
        }

        // Track backboard collision for scoring
        if (collision.gameObject.CompareTag("Backboard"))
        {
            Debug.Log("Ball touched the backboard!");
            backboardTouched = true;
        }

        // Handle ground collision - ball out of play on second touch
        if (collision.gameObject.CompareTag("Ground"))
        {
            BallOutOfPlay?.Invoke();
            // al secondo tocco
            if (groundTouched)
            {
                groundTouched = false;

            }
            else
            {
                groundTouched = true;
            }
        }

    }

    #endregion

    #region Trail Management

    /// <summary>
    /// Activates the fireball trail effect with custom colors and material.
    /// </summary>
    /// <param name="duration">Duration of the fireball mode (unused in this method).</param>
    private void SetFireballTrail(float duration)
    {
        if (trailRenderer != null)
        {
            trailRenderer.material = fireballTrailMaterial;
            trailRenderer.startColor = fireballTrailStartColor;
            trailRenderer.endColor = fireballTrailEndColor;
            trailRenderer.time = fireballTrailTime;
        }
    }

    /// <summary>
    /// Resets the trail to normal appearance.
    /// </summary>
    private void SetNormalTrail()
    {
        if (trailRenderer != null)
        {
            trailRenderer.material = normalTrailMaterial;
            trailRenderer.startColor = normalTrailColor;
            trailRenderer.endColor = normalTrailColor;
            trailRenderer.time = normalTrailTime;
        }
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Resets the ball to its starting position in front of the gameplay camera.
    /// Called when the gameplay camera is ready.
    /// </summary>
    /// <param name="cameraTransform">Transform of the gameplay camera.</param>
    private void ReturnToStart(Transform cameraTransform)
    {
        Debug.Log("Resetting ball position.");
        rb.isKinematic = true;


        // place the ball in front of the gameplay camera with an offset
        transform.position = cameraTransform.position + cameraTransform.forward * cameraOffset.z + cameraTransform.up * cameraOffset.y + cameraTransform.right * cameraOffset.x;
        ringTouched = false;
        backboardTouched = false;
        OnBallReset?.Invoke();

        trailRenderer.enabled = true;
    }

    /// <summary>
    /// Handles the backboard bonus activation event.
    /// </summary>
    /// <param name="bonusPoints">The bonus points value (unused in this method).</param>
    private void OnBackboardBonusActivated(int bonusPoints)
    {
        backboardBonusActive = true;
    }

    /// <summary>
    /// Handles the backboard bonus expiration event.
    /// </summary>
    private void OnBackboardBonusExpired()
    {
        backboardBonusActive = false;
    }

    #endregion

}
