using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages camera transitions and priorities between different game states.
/// Controls Cinemachine virtual cameras for menu, gameplay, reward, and ball-follow views.
/// </summary>
public class CameraManager : Singleton<CameraManager>
{
    #region Events

    /// <summary>Event triggered when the gameplay camera is ready and positioned.</summary>
    public static Action<Transform> GamePlayCameraReady;

    #endregion

    #region Serialized Fields

    /// <summary>Virtual camera used for the main menu view.</summary>
    [SerializeField] private Cinemachine.CinemachineVirtualCamera menuCamera;

    /// <summary>Virtual camera used during gameplay.</summary>
    [SerializeField] private Cinemachine.CinemachineVirtualCamera gameplayCamera;

    /// <summary>Virtual camera used for the reward/results screen.</summary>
    [SerializeField] private Cinemachine.CinemachineVirtualCamera rewardCamera;

    /// <summary>Virtual camera that follows the ball during flight.</summary>
    [SerializeField] private Cinemachine.CinemachineVirtualCamera ballCamera;

    #endregion

    #region Unity Lifecycle Methods

    /// <summary>
    /// Subscribes to game events when the component is enabled.
    /// </summary>
    private void OnEnable()
    {
        GameManager.OnGameStateChanged += HandleGameStateChanged;
        GameManager.positionReset += ChangeCameraGameplay;
        BallPhysics.ballLaunched += SwitchToBallCamera;
    }

    /// <summary>
    /// Unsubscribes from game events when the component is disabled.
    /// Prevents memory leaks and null reference exceptions.
    /// </summary>
    private void OnDisable()
    {
        GameManager.OnGameStateChanged -= HandleGameStateChanged;
        GameManager.positionReset -= ChangeCameraGameplay;
        BallPhysics.ballLaunched -= SwitchToBallCamera;
    }

    #endregion

    #region Public Camera Switching Methods

    /// <summary>
    /// Switches to the menu camera by setting its priority above all other cameras.
    /// </summary>
    public void SwitchToMenuCamera()
    {
        gameplayCamera.Priority = 0;
        rewardCamera.Priority = 0;
        ballCamera.Priority = 0;
        menuCamera.Priority = 10;
    }

    /// <summary>
    /// Switches to the gameplay camera by setting its priority above all other cameras.
    /// </summary>
    public void SwitchToGameplayCamera()
    {
        menuCamera.Priority = 0;
        rewardCamera.Priority = 0;
        ballCamera.Priority = 0;
        gameplayCamera.Priority = 10;
    }

    /// <summary>
    /// Switches to the reward camera by setting its priority above all other cameras.
    /// </summary>
    public void SwitchToRewardCamera()
    {
        menuCamera.Priority = 0;
        gameplayCamera.Priority = 0;
        ballCamera.Priority = 0;
        rewardCamera.Priority = 10;

    }

    /// <summary>
    /// Switches to the ball-following camera and inherits the gameplay camera's rotation.
    /// Called when the ball is launched.
    /// </summary>
    public void SwitchToBallCamera()
    {
        ballCamera.transform.rotation = gameplayCamera.transform.rotation;
        ballCamera.Priority = 10;
        gameplayCamera.Priority = 0;
        menuCamera.Priority = 0;
        rewardCamera.Priority = 0;
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Handles game state changes and switches to the appropriate camera.
    /// </summary>
    /// <param name="state">The new game state.</param>
    private void HandleGameStateChanged(GameState state)
    {
        switch (state)
        {
            case GameState.MainMenu:
                SwitchToMenuCamera();
                break;
            case GameState.Gameplay:
                SwitchToGameplayCamera();
                break;
            case GameState.Reward:
                SwitchToRewardCamera();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    /// <summary>
    /// Updates the gameplay camera position and rotation to a new transform.
    /// Only executes during gameplay state.
    /// </summary>
    /// <param name="newTransform">The transform to position the camera at.</param>
    private void ChangeCameraGameplay(Transform newTransform)
    {
        if (GameManager.Instance.GameState != GameState.Gameplay) return;

        gameplayCamera.Follow = null;
        gameplayCamera.transform.position = newTransform.position;
        gameplayCamera.transform.rotation = newTransform.rotation;
        SwitchToGameplayCamera();
        GamePlayCameraReady?.Invoke(gameplayCamera.transform);
    }

    #endregion

}
