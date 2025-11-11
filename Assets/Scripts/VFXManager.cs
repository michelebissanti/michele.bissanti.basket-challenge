using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages all visual effects (VFX) in the game.
/// Handles score VFX instantiation for different basket types and fireball trail effects.
/// </summary>
public class VFXManager : Singleton<VFXManager>
{
    #region Serialized Fields - Score VFX

    /// <summary>VFX prefab for standard score baskets.</summary>
    [Header("Score VFX")]
    [SerializeField] private GameObject scorePrefab;

    /// <summary>VFX prefab for perfect score baskets.</summary>
    [SerializeField] private GameObject perfectScorePrefab;

    /// <summary>VFX prefab for backboard score baskets.</summary>
    [SerializeField] private GameObject backboardScorePrefab;

    /// <summary>GameObject marking the position where score VFX should spawn.</summary>
    [SerializeField] private GameObject scoreVFXPosition;

    #endregion

    #region Serialized Fields - Ball VFX

    /// <summary>VFX prefab for fireball trail effect during fireball mode.</summary>
    [Header("Ball VFX")]
    [SerializeField] private GameObject fireballTrailPrefab;

    /// <summary>Reference to the ball GameObject to attach trail effects.</summary>
    [SerializeField] private GameObject ball;

    #endregion

    #region Private Fields

    /// <summary>Flag indicating if the fireball trail is currently active.</summary>
    private bool isFireballTrailActive = false;

    /// <summary>Reference to the instantiated fireball trail GameObject.</summary>
    private GameObject instantiatedFireballTrail;

    #endregion

    #region Unity Lifecycle Methods

    /// <summary>
    /// Subscribes to game events when the component is enabled.
    /// </summary>
    void OnEnable()
    {
        // Score VFX events
        GameManager.OnScoreDone += ShowScoreVFX;
        GameManager.OnPerfectScoreDone += ShowPerfectScoreVFX;
        GameManager.OnBackboardScoreDone += ShowBackboardScoreVFX;

        // Fireball mode events
        GameManager.OnFireballModeActivated += EnableFireballTrail;
        GameManager.OnFireballModeExpired += DisableFireballTrail;
    }

    /// <summary>
    /// Unsubscribes from game events when the component is disabled.
    /// Prevents memory leaks and null reference exceptions.
    /// </summary>
    void OnDisable()
    {
        // Score VFX events
        GameManager.OnScoreDone -= ShowScoreVFX;
        GameManager.OnPerfectScoreDone -= ShowPerfectScoreVFX;
        GameManager.OnBackboardScoreDone -= ShowBackboardScoreVFX;

        // Fireball mode events
        GameManager.OnFireballModeActivated -= EnableFireballTrail;
        GameManager.OnFireballModeExpired -= DisableFireballTrail;
    }

    /// <summary>
    /// Updates fireball trail position every frame to follow the ball.
    /// Only executes when fireball trail is active.
    /// </summary>
    private void Update()
    {
        if (isFireballTrailActive && instantiatedFireballTrail != null && ball != null)
        {
            instantiatedFireballTrail.transform.position = ball.transform.position;
        }
    }

    #endregion

    #region Event Handlers - Score VFX

    /// <summary>
    /// Shows the standard score VFX when a basket is made.
    /// Instantiates the score prefab at the designated VFX position.
    /// </summary>
    private void ShowScoreVFX()
    {
        Instantiate(scorePrefab, scoreVFXPosition.transform);
        Debug.Log("Score VFX Shown");
    }

    /// <summary>
    /// Shows the perfect score VFX when a perfect basket is made (direct shot without touching rim or backboard).
    /// Instantiates the perfect score prefab at the designated VFX position.
    /// </summary>
    private void ShowPerfectScoreVFX()
    {
        Instantiate(perfectScorePrefab, scoreVFXPosition.transform);
        Debug.Log("Perfect Score VFX Shown");
    }

    /// <summary>
    /// Shows the backboard score VFX when a backboard basket is made.
    /// Instantiates the backboard score prefab at the designated VFX position.
    /// </summary>
    private void ShowBackboardScoreVFX()
    {
        Instantiate(backboardScorePrefab, scoreVFXPosition.transform);
        Debug.Log("Backboard Score VFX Shown");
    }

    #endregion

    #region Event Handlers - Fireball VFX

    /// <summary>
    /// Enables the fireball trail effect when fireball mode is activated.
    /// Instantiates the trail prefab and positions it at the ball's location.
    /// </summary>
    /// <param name="duration">Duration of the fireball mode (unused in this method but provided by event).</param>
    private void EnableFireballTrail(float duration)
    {
        if (ball != null && fireballTrailPrefab != null)
        {
            // Instantiate trail at world origin
            instantiatedFireballTrail = Instantiate(fireballTrailPrefab, Vector3.zero, Quaternion.identity);

            // Scale down the trail to 50% of original size
            instantiatedFireballTrail.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            instantiatedFireballTrail.name = "FireballTrail";

            Debug.Log("Fireball Trail Enabled");
            isFireballTrailActive = true;
        }
    }

    /// <summary>
    /// Disables the fireball trail effect when fireball mode expires.
    /// Destroys the instantiated trail GameObject.
    /// </summary>
    private void DisableFireballTrail()
    {
        isFireballTrailActive = false;

        if (instantiatedFireballTrail != null)
        {
            Destroy(instantiatedFireballTrail);
            Debug.Log("Fireball Trail Disabled");
        }
    }

    #endregion
}
