using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VFXManager : Singleton<VFXManager>
{
    [Header("Score VFX")]
    [SerializeField] private GameObject scorePrefab;
    [SerializeField] private GameObject perfectScorePrefab;
    [SerializeField] private GameObject backboardScorePrefab;
    [SerializeField] private GameObject scoreVFXPosition;

    [Header("Ball VFX")]
    [SerializeField] private GameObject fireballTrailPrefab;
    [SerializeField] private GameObject ball;
    private bool isFireballTrailActive = false;
    private GameObject instantiatedFireballTrail;



    void OnEnable()
    {
        GameManager.OnScoreDone += ShowScoreVFX;
        GameManager.OnPerfectScoreDone += ShowPerfectScoreVFX;
        GameManager.OnBackboardScoreDone += ShowBackboardScoreVFX;

        GameManager.OnFireballModeActivated += EnableFireballTrail;
        GameManager.OnFireballModeExpired += DisableFireballTrail;
    }

    void OnDisable()
    {
        GameManager.OnScoreDone -= ShowScoreVFX;
        GameManager.OnPerfectScoreDone -= ShowPerfectScoreVFX;
        GameManager.OnBackboardScoreDone -= ShowBackboardScoreVFX;

        GameManager.OnFireballModeActivated -= EnableFireballTrail;
        GameManager.OnFireballModeExpired -= DisableFireballTrail;
    }

    private void ShowScoreVFX()
    {
        Instantiate(scorePrefab, scoreVFXPosition.transform);
        Debug.Log("Score VFX Shown");
    }

    private void ShowPerfectScoreVFX()
    {
        Instantiate(perfectScorePrefab, scoreVFXPosition.transform);
        Debug.Log("Perfect Score VFX Shown");
    }

    private void ShowBackboardScoreVFX()
    {
        Instantiate(backboardScorePrefab, scoreVFXPosition.transform);
        Debug.Log("Backboard Score VFX Shown");
    }

    private void EnableFireballTrail(float duration)
    {
        if (ball != null && fireballTrailPrefab != null)
        {
            instantiatedFireballTrail = Instantiate(fireballTrailPrefab, Vector3.zero, Quaternion.identity);
            instantiatedFireballTrail.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
            instantiatedFireballTrail.name = "FireballTrail";
            Debug.Log("Fireball Trail Enabled");
            isFireballTrailActive = true;
        }
    }

    private void DisableFireballTrail()
    {
        isFireballTrailActive = false;
        if (instantiatedFireballTrail != null)
        {
            Destroy(instantiatedFireballTrail);
            Debug.Log("Fireball Trail Disabled");
        }
    }

    private void Update()
    {
        if (isFireballTrailActive && instantiatedFireballTrail != null && ball != null)
        {
            instantiatedFireballTrail.transform.position = ball.transform.position;
        }
    }

}
