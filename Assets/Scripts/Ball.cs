using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ball : MonoBehaviour
{
    [SerializeField] private PlayerType playerType = PlayerType.Human;
    public PlayerType PlayerType => playerType;

    private bool ringTouched = false;
    private bool backboardTouched = false;
    private bool groundTouched = false;
    private Rigidbody rb;
    [SerializeField] private Vector3 cameraOffset = new Vector3(0, -0.4f, 2f);
    private bool backboardBonusActive = false;

    public static event Action<PlayerType> BallOutOfPlay;

    private void OnEnable()
    {
        CameraManager.GamePlayCameraReady += ReturnToStart;
        GameManager.OnBackboardBonusActivated += OnBackboardBonusActivated;
        GameManager.OnBackboardBonusExpired += OnBackboardBonusExpired;
        GameManager.positionReset += OnPositionReset;
    }

    private void OnDisable()
    {
        CameraManager.GamePlayCameraReady -= ReturnToStart;
        GameManager.OnBackboardBonusActivated -= OnBackboardBonusActivated;
        GameManager.OnBackboardBonusExpired -= OnBackboardBonusExpired;
        GameManager.positionReset -= OnPositionReset;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Basket"))
        {
            if (ringTouched && !backboardTouched)
            {
                GameManager.Instance.SetStandardScore(playerType);
            }

            if (backboardTouched)
            {
                if (backboardBonusActive)
                {
                    GameManager.Instance.SetBackboardScore(playerType);
                }
                else
                {
                    GameManager.Instance.SetStandardScore(playerType);
                }
            }

            if (!ringTouched && !backboardTouched)
            {
                GameManager.Instance.SetPerfectScore(playerType);
            }

            ringTouched = false;
            backboardTouched = false;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ring"))
        {
            Debug.Log($"Ball ({playerType}) touched the ring!");
            ringTouched = true;
        }

        if (collision.gameObject.CompareTag("Backboard"))
        {
            Debug.Log($"Ball ({playerType}) touched the backboard!");
            backboardTouched = true;
        }

        if (collision.gameObject.CompareTag("Ground"))
        {
            BallOutOfPlay?.Invoke(playerType);
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

    /// <summary>
    /// Called when ball position needs to be reset (responds to GameManager.positionReset event)
    /// </summary>
    private void OnPositionReset(Transform spawnPoint, PlayerType resetPlayerType)
    {
        // Only respond if this reset is for this ball
        if (resetPlayerType != playerType)
        {
            return;
        }

        Debug.Log($"[{playerType}] OnPositionReset called at {spawnPoint.position}");

        // Set kinematic to prevent physics until launch
        rb.isKinematic = true;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        transform.position = spawnPoint.position;

        ringTouched = false;
        backboardTouched = false;

        // NON riabilitare physics qui - sarà fatto in LaunchBall
    }

    /// <summary>
    /// Legacy method for camera-based reset (kept for backward compatibility)
    /// </summary>
    private void ReturnToStart(Transform cameraTransform)
    {
        Debug.Log($"Resetting ball position for {playerType}.");
        rb.isKinematic = true;

        transform.position = cameraTransform.position + cameraTransform.forward * cameraOffset.z +
                            cameraTransform.up * cameraOffset.y + cameraTransform.right * cameraOffset.x;
        ringTouched = false;
        backboardTouched = false;
    }

    private void OnBackboardBonusActivated(int bonusPoints)
    {
        backboardBonusActive = true;
    }

    private void OnBackboardBonusExpired()
    {
        backboardBonusActive = false;
    }
}
