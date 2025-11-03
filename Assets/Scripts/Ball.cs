using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ball : MonoBehaviour
{
    private bool scored = false;
    private bool ringTouched = false;
    private bool backboardTouched = false;
    private bool groundTouched = false;

    [SerializeField] private Transform startPosition;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Basket"))
        {
            Debug.Log("Ball entered the basket!");
            scored = true;
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
                ReturnToStart();
            }
            else
            {
                groundTouched = true;
            }
        }

    }

    // DEBUG: Resetta la palla alla posizione iniziale
    private void ReturnToStart()
    {
        Debug.Log("Resetting ball position.");
        transform.position = startPosition.position;
        GetComponent<Rigidbody>().velocity = Vector3.zero;
        GetComponent<Rigidbody>().isKinematic = true;
        scored = false;
        ringTouched = false;
        backboardTouched = false;
    }

}
