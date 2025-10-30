using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : Singleton<CameraManager>
{
    [SerializeField] private Cinemachine.CinemachineVirtualCamera menuCamera;
    [SerializeField] private Cinemachine.CinemachineVirtualCamera gameplayCamera;
    [SerializeField] private Cinemachine.CinemachineVirtualCamera rewardCamera;

    private void OnEnable()
    {
        GameManager.OnGameStateChanged += HandleGameStateChanged;
    }

    private void OnDisable()
    {
        GameManager.OnGameStateChanged -= HandleGameStateChanged;
    }

    public void SwitchToMenuCamera()
    {
        menuCamera.Priority = 10;
        gameplayCamera.Priority = 0;
        rewardCamera.Priority = 0;
    }

    public void SwitchToGameplayCamera()
    {
        menuCamera.Priority = 0;
        gameplayCamera.Priority = 10;
        rewardCamera.Priority = 0;
    }

    public void SwitchToRewardCamera()
    {
        menuCamera.Priority = 0;
        gameplayCamera.Priority = 0;
        rewardCamera.Priority = 10;
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

}
