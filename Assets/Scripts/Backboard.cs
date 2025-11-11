using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Manages the backboard visual feedback system.
/// Handles material color changes and bonus text display when the backboard bonus is activated or expires.
/// </summary>
public class Backboard : MonoBehaviour
{
    #region Serialized Fields

    /// <summary>First bonus text UI element (front-facing).</summary>
    [SerializeField] private TextMeshProUGUI bonusText;

    /// <summary>Second bonus text UI element (back-facing or duplicate display).</summary>
    [SerializeField] private TextMeshProUGUI bonusText2;

    /// <summary>Color applied to the backboard material when bonus is active.</summary>
    [SerializeField] private Color bonusColor;

    #endregion

    #region Private Fields

    /// <summary>Renderer component for accessing and modifying the backboard materials.</summary>
    private Renderer rendererMat;

    /// <summary>Array of materials used by the backboard renderer.</summary>
    private Material[] materials;

    #endregion

    #region Unity Lifecycle Methods

    /// <summary>
    /// Initializes the backboard components and hides bonus text on start.
    /// </summary>
    void Start()
    {
        rendererMat = GetComponent<Renderer>();
        materials = rendererMat.materials;
        HideText();
    }

    /// <summary>
    /// Subscribes to GameManager events when the component is enabled.
    /// </summary>
    private void OnEnable()
    {
        GameManager.OnBackboardBonusActivated += OnBackboardBonusActivated;
        GameManager.OnBackboardBonusExpired += OnBackboardBonusExpired;
    }

    /// <summary>
    /// Unsubscribes from GameManager events when the component is disabled.
    /// Prevents memory leaks and null reference exceptions.
    /// </summary>
    private void OnDisable()
    {
        GameManager.OnBackboardBonusActivated -= OnBackboardBonusActivated;
        GameManager.OnBackboardBonusExpired -= OnBackboardBonusExpired;
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Handles the backboard bonus activation event.
    /// Changes the backboard material color and displays the bonus points text.
    /// </summary>
    /// <param name="bonusPoints">The amount of bonus points awarded.</param>
    private void OnBackboardBonusActivated(int bonusPoints)
    {
        // Change the third material's color to the bonus color (index 2)
        materials[2].SetColor("_BaseColor", bonusColor);
        rendererMat.materials = materials;
        ShowText(bonusPoints);
    }

    /// <summary>
    /// Handles the backboard bonus expiration event.
    /// Resets the backboard material color to white and hides the bonus text.
    /// </summary>
    private void OnBackboardBonusExpired()
    {
        // Reset the third material's color to white
        materials[2].SetColor("_BaseColor", Color.white);
        rendererMat.materials = materials;
        HideText();
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Activates and displays both bonus text elements with the specified bonus points value.
    /// </summary>
    /// <param name="bonusPoints">The bonus points value to display.</param>
    private void ShowText(int bonusPoints)
    {
        bonusText.gameObject.SetActive(true);
        bonusText2.gameObject.SetActive(true);
        bonusText.text = $"+{bonusPoints}";
        bonusText2.text = $"+{bonusPoints}";
    }

    /// <summary>
    /// Deactivates and hides both bonus text elements.
    /// </summary>
    private void HideText()
    {
        bonusText.gameObject.SetActive(false);
        bonusText2.gameObject.SetActive(false);
    }

    #endregion
}
