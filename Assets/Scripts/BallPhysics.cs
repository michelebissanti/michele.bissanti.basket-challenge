using UnityEngine;

public class BallPhysics : MonoBehaviour
{
    private Rigidbody ballRigidbody;
    [SerializeField] private Transform basketTransform;
    [SerializeField] private Transform backboardTransform;

    [SerializeField] private float shotAngle = 60f;

    [SerializeField] private float errorMargin = 0.05f;

    private void OnEnable()
    {
        InputManager.OnStartDrag += HandleStartDrag;
        InputManager.OnEndDrag += HandleEndDrag;
        InputManager.OnDrag += HandleDrag;
    }

    private void OnDisable()
    {
        InputManager.OnStartDrag -= HandleStartDrag;
        InputManager.OnEndDrag -= HandleEndDrag;
        InputManager.OnDrag -= HandleDrag;
    }

    private void Start()
    {
        ballRigidbody = GetComponent<Rigidbody>();
    }


    private void HandleStartDrag(Vector2 startPosition)
    {
        Debug.Log("Drag started at: " + startPosition);
    }

    private void HandleEndDrag(Vector2 dragVector)
    {
        // Convert drag vector to world position
        Vector3 targetPos = Camera.main.ScreenToWorldPoint(new Vector3(dragVector.x, dragVector.y, Camera.main.transform.position.y));

        ShootPerfectShot(targetPos);

        Debug.Log("Drag ended with vector: " + dragVector);
    }

    private void HandleDrag(Vector2 currentPosition)
    {
        // Optional: Implement visual feedback during drag
        Debug.Log("Dragging at: " + currentPosition);
    }


    private void ShootPerfectShot(Vector3 targetPos)
    {

        Vector3 startPos = ballRigidbody.position;

        bool solutionFound;
        Vector3 velocity = PhysicsUtils.CalculatePerfectShotVelocity(startPos, targetPos, shotAngle, errorMargin, out solutionFound);

        if (solutionFound)
        {
            ballRigidbody.isKinematic = false;
            ballRigidbody.velocity = Vector3.zero;
            ballRigidbody.AddForce(velocity, ForceMode.Impulse);

            PhysicsUtils.DrawDebugTrajectory(startPos, velocity);
        }
        else
        {
            Debug.LogWarning("Impossible shot! The target is unreachable at this angle.");
        }
    }
}