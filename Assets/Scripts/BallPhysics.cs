using UnityEngine;

public class BallPhysics : MonoBehaviour
{
    private Rigidbody ballRigidbody;
    [SerializeField] private Transform basketTransform;
    [SerializeField] private Transform backboardTransform;

    [SerializeField] private float shotAngle = 60f;

    private void Start()
    {
        ballRigidbody = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            ShootPerfectShot(basketTransform.position);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            ShootPerfectShot(backboardTransform.position);
        }
    }


    private void ShootPerfectShot(Vector3 targetPos)
    {

        Vector3 startPos = ballRigidbody.position;

        bool solutionFound;
        Vector3 velocity = PhysicsUtils.CalculatePerfectShotVelocity(startPos, targetPos, shotAngle, out solutionFound);

        if (solutionFound)
        {
            ballRigidbody.isKinematic = false;
            ballRigidbody.velocity = Vector3.zero;
            ballRigidbody.AddForce(velocity, ForceMode.Impulse);
        }
        else
        {
            Debug.LogWarning("Impossible shot! The target is unreachable at this angle.");
        }
    }
}