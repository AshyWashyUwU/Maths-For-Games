using UnityEngine;
using UnityEngine.InputSystem;
public class BowlingBallController : MonoBehaviour
{
    [Header("Ball Variables")]
    [Range(1, 3)][SerializeField] private float ballHoldMoveSpeed = 2f;
    [Range(1, 5)][SerializeField] private float ballRollSpeed = 3f;
    [Range(0, 3)][SerializeField] private float ballRotateSpeed = 1f;
    [Range(1, 10)][SerializeField] private float ballChargeSpeed = 5f;
    [Range(0, 1)][SerializeField] private float ballRadius = 0.5f;

    [Header("Ball Constraints")]
    [SerializeField] private float laneWidth = 3f;
    [SerializeField] private float maxRotation = 25f;
    [SerializeField] private float resetDistance = 30f;
    [SerializeField] private float maxThrowForce = 3;
    [SerializeField] private float minRollSpeed = 1.5f;

    [Header("Gravity Stuff")]
    private float verticalVelocity = 0f;
    [SerializeField] private float gravityForce = -9.81f;
    [SerializeField] private float groundY = -2f;
    [SerializeField] private float airResistance = 0.3f;

    private float elapsedRollingTime;
    private float yawDegrees;
    private float initalRollSpeed;
    private float throwCharge;
    private float appliedThrowCharge;

    private bool thrownBall, isDraggingRight, isGrounded, isCharging;

    private Vector2 moveInput, rotateInput;
    private CustomMathsLibrary.Vector3 randomDragEndVector;
    private CustomMathsLibrary.Vector3 startPos;

    private CustomMathsLibrary.Quat currentRotation = new CustomMathsLibrary.Quat(1, 0, 0, 0);

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

            appliedThrowCharge = throwCharge;
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

        if (!thrownBall)
        {
            yawDegrees -= rotateInput.y * ballRotateSpeed;
            yawDegrees = CustomMathsLibrary.Clamp(yawDegrees, -maxRotation, maxRotation);
        }

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

            CustomMathsLibrary.Vector3 randomDragVector = CustomMathsLibrary.Scale(right, CustomMathsLibrary.LerpVector(CustomMathsLibrary.Vector3.zero, randomDragEndVector, elapsedRollingTime).x);

            moveDir = CustomMathsLibrary.Add(CustomMathsLibrary.Scale(forward, ballRollSpeed + appliedThrowCharge), randomDragVector);

            if (CustomMathsLibrary.Dot(moveDir, forward) < 0)
            {
                moveDir = CustomMathsLibrary.Scale(forward, 0.1f);
            }

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

        if (thrownBall && ballRollSpeed > 0.01f)
        {
            CustomMathsLibrary.Vector3 direction = CustomMathsLibrary.Normalize(ballVelocity);

            CustomMathsLibrary.Vector3 axis = CustomMathsLibrary.CrossProduct(up, direction);

            float resistance = isGrounded ? 1f : airResistance;

            float speed = CustomMathsLibrary.Magnitude(ballVelocity);
            float angle = (speed / ballRadius) * Time.deltaTime * resistance;

            CustomMathsLibrary.Quat deltaRotation = new CustomMathsLibrary.Quat(axis, angle);

            currentRotation = deltaRotation * currentRotation;
        }

        CustomMathsLibrary.Quat yawRot = CustomMathsLibrary.Quat.Euler(0f, yawDegrees, 0f);

        yawRot = yawRot * currentRotation;

        transform.rotation = yawRot.ToUnityQuaternion();

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

        currentRotation = new CustomMathsLibrary.Quat(1, 0, 0, 0);
    }
}