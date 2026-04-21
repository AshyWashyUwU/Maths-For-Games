using UnityEngine;
using UnityEngine.InputSystem;
public class BowlingBallController : MonoBehaviour
{
    [Header("Ball Variables")]
    [SerializeField] private float ballHoldMoveSpeed = 2f;
    [SerializeField] private float ballRollSpeed = 3f;
    [SerializeField] private float ballRotateSpeed = 1f;
    [SerializeField] private float ballChargeSpeed = 5f;

    [Header("Ball Constraits")]
    [SerializeField] private float laneWidth = 3f;
    [SerializeField] private float maxRotation = 25f;
    [SerializeField] private float resetDistance = 30f;
    [SerializeField] private float maxThrowForce = 3;
    [SerializeField] private float minRollSpeed = 1.5f;

    [Header("Gravity Stuff")]
    private float verticalVelocity = 0f;
    [SerializeField] private float gravityForce = -9.81f;
    [SerializeField] private float groundY = -2f;

    private float elapsedRollingTime;
    private float yawDegrees;
    private float initalRollSpeed;
    private float throwCharge;

    private bool thrownBall, isDraggingRight, isGrounded, isCharging;

    private Vector2 moveInput, rotateInput;
    private CustomMathsLibrary.Vector3 randomDragEndVector;
    private CustomMathsLibrary.Vector3 startPos;

    private void Start()
    {
        startPos = transform.position;

        initalRollSpeed = ballRollSpeed;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnRotate(InputAction.CallbackContext context)
    {
        if (!thrownBall)
        {
            rotateInput = context.ReadValue<Vector2>();
        }
    }

    public void OnThrow(InputAction.CallbackContext context)
    {
        if (context.started && !thrownBall)
        {
            Debug.Log("Started charging");
            isCharging = true;
        }

        if (context.canceled && isCharging && !thrownBall)
        {
            Debug.Log("Released throw");

            isCharging = false;
            thrownBall = true;
            isGrounded = false;

            verticalVelocity = throwCharge;

            float appliedCharge = throwCharge;
            throwCharge = 0f;

            randomDragEndVector = new CustomMathsLibrary.Vector3(Random.Range(-0.2f, 0.2f), 0, Random.Range(-0.2f, 0.2f));
        }
    }

    private void FixedUpdate()
    {
        MoveBall();
    }

    private void MoveBall()
    {
        if (isCharging)
        {
            throwCharge += ballChargeSpeed * Time.deltaTime;
            throwCharge = CustomMathsLibrary.Clamp(throwCharge, 0f, maxThrowForce);
        }

        yawDegrees -= rotateInput.y * ballRotateSpeed;
        yawDegrees = CustomMathsLibrary.Clamp(yawDegrees, -maxRotation, maxRotation);

        float yawRadians = CustomMathsLibrary.DegreesToRadians(yawDegrees);

        CustomMathsLibrary.Vector3 forward = CustomMathsLibrary.ForwardFromYawPitch(yawRadians, 0);
        CustomMathsLibrary.Vector3 up = new CustomMathsLibrary.Vector3(0, 1, 0);
        CustomMathsLibrary.Vector3 right = CustomMathsLibrary.CrossProduct(up, forward);

        CustomMathsLibrary.Vector3 moveDir = new Vector3(0f, 0f, 0f);

        CustomMathsLibrary.Vector3 pos = transform.position;

        if (!thrownBall)
        {
            moveDir = new CustomMathsLibrary.Vector3(moveInput.x, 0f, 0f);
        }
        else
        {
            elapsedRollingTime += Time.deltaTime;

            CustomMathsLibrary.Vector3 randomDragVector = CustomMathsLibrary.LerpVector(CustomMathsLibrary.Vector3.zero, randomDragEndVector, elapsedRollingTime);

            moveDir = CustomMathsLibrary.Add(CustomMathsLibrary.Scale(forward, ballRollSpeed + throwCharge), randomDragVector);

            if (!isGrounded)
            {
                verticalVelocity += gravityForce * Time.deltaTime;
                pos.y += verticalVelocity * Time.deltaTime;

                if (pos.y <= groundY)
                {
                    pos.y = groundY;
                    verticalVelocity = 0f;
                    isGrounded = true;
                }
            }
            else
            {
                ballRollSpeed *= 0.995f;

                if (ballRollSpeed < minRollSpeed)
                {
                    ballRollSpeed = minRollSpeed;
                }
            }
        }

        CustomMathsLibrary.Vector3 ballVelocity = CustomMathsLibrary.Scale(moveDir, ballHoldMoveSpeed);
        pos = CustomMathsLibrary.Add(pos, CustomMathsLibrary.Scale(ballVelocity, Time.deltaTime));

        transform.rotation = Quaternion.Euler(0f, yawDegrees, 0f);

        pos.x = CustomMathsLibrary.Clamp(pos.x, -laneWidth, laneWidth);

        transform.position = pos;

        if (transform.position.z >= resetDistance)
        {
            ResetBall();
        }
    }

    private void ResetBall()
    {
        transform.position = startPos;

        thrownBall = false;
        isGrounded = false;

        verticalVelocity = 0f;
        ballRollSpeed = initalRollSpeed;
        elapsedRollingTime = 0f;
        yawDegrees = 0f;
    }
}
