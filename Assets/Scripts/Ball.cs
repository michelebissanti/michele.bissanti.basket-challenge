using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ball : MonoBehaviour
{
    private bool ringTouched = false;
    private bool backboardTouched = false;
    private bool groundTouched = false;
    private Rigidbody rb;
    [SerializeField] private Vector3 cameraOffset = new Vector3(0, -0.4f, 2f);
    private bool backboardBonusActive = false;

    public static event Action BallOutOfPlay;

    private TrailRenderer trailRenderer;
    [Header("Trail Settings")]
    [Header("Normal Trail Settings")]
    [SerializeField] private Color normalTrailColor = Color.white;
    [SerializeField] private Material normalTrailMaterial;
    [SerializeField] private float normalTrailTime;
    [Header("Fireball Trail Settings")]
    [SerializeField] private Color fireballTrailStartColor = Color.red;
    [SerializeField] private Color fireballTrailEndColor = new Color(1f, 0.5f, 0f);

    [SerializeField] private Material fireballTrailMaterial;

    [SerializeField] private float fireballTrailTime;


    private void OnEnable()
    {
        CameraManager.GamePlayCameraReady += ReturnToStart;
        GameManager.OnBackboardBonusActivated += OnBackboardBonusActivated;
        GameManager.OnBackboardBonusExpired += OnBackboardBonusExpired;

        GameManager.OnFireballModeActivated += SetFireballTrail;
        GameManager.OnFireballModeExpired += SetNormalTrail;
    }

    private void OnDisable()
    {
        CameraManager.GamePlayCameraReady -= ReturnToStart;
        GameManager.OnBackboardBonusActivated -= OnBackboardBonusActivated;
        GameManager.OnBackboardBonusExpired -= OnBackboardBonusExpired;
        GameManager.OnFireballModeActivated -= SetFireballTrail;
        GameManager.OnFireballModeExpired -= SetNormalTrail;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        trailRenderer = GetComponent<TrailRenderer>();
    }

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

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Basket"))
        {
            if (ringTouched && !backboardTouched)
            {
                GameManager.Instance.SetStandardScore();
            }

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

            if (!ringTouched && !backboardTouched)
            {
                GameManager.Instance.SetPerfectScore();
            }

            ringTouched = false;
            backboardTouched = false;

        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ring"))
        {
            Debug.Log("Ball touched the ring!");
            ringTouched = true;
        }

        if (collision.gameObject.CompareTag("Backboard"))
        {
            Debug.Log("Ball touched the backboard!");
            backboardTouched = true;
        }

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

    private void ReturnToStart(Transform cameraTransform)
    {
        Debug.Log("Resetting ball position.");
        rb.isKinematic = true;

        // place the ball in front of the gameplay camera with an offset
        transform.position = cameraTransform.position + cameraTransform.forward * cameraOffset.z + cameraTransform.up * cameraOffset.y + cameraTransform.right * cameraOffset.x;
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
