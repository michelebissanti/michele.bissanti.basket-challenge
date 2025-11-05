using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : Singleton<CameraManager>
{
    [SerializeField] private Cinemachine.CinemachineVirtualCamera menuCamera;
    [SerializeField] private Cinemachine.CinemachineVirtualCamera gameplayCamera;
    [SerializeField] private Cinemachine.CinemachineVirtualCamera rewardCamera;
    [SerializeField] private Cinemachine.CinemachineVirtualCamera ballCamera;

    public static Action<Transform> GamePlayCameraReady;

    private void OnEnable()
    {
        GameManager.OnGameStateChanged += HandleGameStateChanged;
        GameManager.positionReset += ChangeCameraGameplay;
        BallPhysics.ballLaunched += SwitchToBallCamera;
    }

    private void OnDisable()
    {
        GameManager.OnGameStateChanged -= HandleGameStateChanged;
        GameManager.positionReset -= ChangeCameraGameplay;
        BallPhysics.ballLaunched -= SwitchToBallCamera;
    }

    public void SwitchToMenuCamera()
    {
        gameplayCamera.Priority = 0;
        rewardCamera.Priority = 0;
        ballCamera.Priority = 0;
        menuCamera.Priority = 10;
    }

    public void SwitchToGameplayCamera()
    {
        menuCamera.Priority = 0;
        rewardCamera.Priority = 0;
        ballCamera.Priority = 0;
        gameplayCamera.Priority = 10;
    }

    public void SwitchToRewardCamera()
    {
        menuCamera.Priority = 0;
        gameplayCamera.Priority = 0;
        ballCamera.Priority = 0;
        rewardCamera.Priority = 10;

    }

    public void SwitchToBallCamera()
    {
        ballCamera.transform.rotation = gameplayCamera.transform.rotation;
        ballCamera.Priority = 10;
        gameplayCamera.Priority = 0;
        menuCamera.Priority = 0;
        rewardCamera.Priority = 0;
    }

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

    private void ChangeCameraGameplay(Transform newTransform)
    {
        gameplayCamera.Follow = null;
        gameplayCamera.transform.position = newTransform.position;
        gameplayCamera.transform.rotation = newTransform.rotation;
        SwitchToGameplayCamera();
        GamePlayCameraReady?.Invoke(gameplayCamera.transform);
    }

}
