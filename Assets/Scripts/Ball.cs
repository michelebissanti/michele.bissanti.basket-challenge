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

    private void OnEnable()
    {
        CameraManager.GamePlayCameraReady += ReturnToStart;
    }

    private void OnDisable()
    {
        CameraManager.GamePlayCameraReady -= ReturnToStart;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Basket"))
        {
            if (ringTouched || backboardTouched)
            {
                GameManager.Instance.SetStandardScore();
            }
            else
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
            // al secondo tocco
            if (groundTouched)
            {
                groundTouched = false;
                GameManager.Instance.BallOutOfPlay();
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

}
