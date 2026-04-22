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
    [SerializeField] private float maxPullbackDistance = 0.25f;
    [SerializeField] private float pullbackSmoothing = 5f;
    [SerializeField] private float ballMass = 5f;

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
    private float currentPullback;

    private bool thrownBall, isDraggingRight, isGrounded, isCharging;

    private Vector2 moveInput, rotateInput;

    private CustomMathsLibrary.Vector3 randomDragEndVector;
    private CustomMathsLibrary.Vector3 startPos;

    private CustomMathsLibrary.Vector3 up = new CustomMathsLibrary.Vector3(0, 1, 0);

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

            verticalVelocity = (throwCharge * 0.5f) / ballMass;
            appliedThrowCharge = throwCharge / ballMass;

            throwCharge = 0f;

            randomDragEndVector = new CustomMathsLibrary.Vector3(Random.Range(-0.2f, 0.2f), 0, Random.Range(-0.2f, 0.2f));
        }
    }

    private CustomMathsLibrary.Vector3 ApplyChargeVisual(CustomMathsLibrary.Vector3 pos)
    {
        float chargePercent = throwCharge / maxThrowForce;
        float targetPullback = -chargePercent * maxPullbackDistance;

        currentPullback = CustomMathsLibrary.Lerp(currentPullback, targetPullback, pullbackSmoothing * Time.deltaTime);

        CustomMathsLibrary.Vector3 offset = new CustomMathsLibrary.Vector3(0, 0, currentPullback);

        return CustomMathsLibrary.Add(pos, offset);
    }

    private void FixedUpdate()
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

        CustomMathsLibrary.Vector3 pos = transform.position;

        pos = CustomMathsLibrary.Add(pos, new CustomMathsLibrary.Vector3(0, 0, -currentPullback));

        CustomMathsLibrary.Vector3 moveDir = GetMoveDir();
        pos = GetMovement(pos, moveDir);

        ApplyPhysics(pos, moveDir);
        ApplyRotation(moveDir);

        ApplyConstraints(pos);

        if (isCharging) pos = ApplyChargeVisual(pos);

        ApplyTransform(pos);

        if (pos.z >= resetDistance) ResetBall();
    }

    private CustomMathsLibrary.Vector3 GetMoveDir()
    {
        if (!thrownBall)
        {
            return new CustomMathsLibrary.Vector3(moveInput.x, 0f, 0f);
        }

        float yawRadians = CustomMathsLibrary.DegreesToRadians(yawDegrees);

        CustomMathsLibrary.Vector3 forward = CustomMathsLibrary.ForwardFromYawPitch(yawRadians, 0);
        CustomMathsLibrary.Vector3 right = CustomMathsLibrary.CrossProduct(up, forward);

        elapsedRollingTime += Time.deltaTime;

        CustomMathsLibrary.Vector3 randomDragVector = CustomMathsLibrary.Scale(right, CustomMathsLibrary.LerpVector(CustomMathsLibrary.Vector3.zero, randomDragEndVector, elapsedRollingTime).x);
        CustomMathsLibrary.Vector3 moveDir = CustomMathsLibrary.Add(CustomMathsLibrary.Scale(forward, ballRollSpeed + (appliedThrowCharge * 0.15f)), randomDragVector);

        return moveDir;
    }

    private CustomMathsLibrary.Vector3 GetMovement(CustomMathsLibrary.Vector3 pos, CustomMathsLibrary.Vector3 moveDir)
    {
        CustomMathsLibrary.Vector3 velocity = CustomMathsLibrary.Scale(moveDir, ballHoldMoveSpeed);

        return CustomMathsLibrary.Add(pos, CustomMathsLibrary.Scale(velocity, Time.deltaTime));
    }

    private void ApplyPhysics(CustomMathsLibrary.Vector3 pos, CustomMathsLibrary.Vector3 moveDir)
    {
        if (!thrownBall) return;

        if (!isGrounded)
        {
            verticalVelocity += (gravityForce - (ballMass * 2.5f)) * Time.deltaTime;

            pos.y += verticalVelocity * Time.deltaTime;

            if (pos.y <= groundY)
            {
                pos.y = groundY;
                verticalVelocity = 0f;
                isGrounded = true;
            }

            appliedThrowCharge *= 1f / (1f - airResistance * 6f * Time.deltaTime);
        }
        else
        {
            appliedThrowCharge *= 0.9f;

            ballRollSpeed *= 0.995f;

            if (ballRollSpeed < minRollSpeed) ballRollSpeed = minRollSpeed;
        }
    }

    private void ApplyConstraints(CustomMathsLibrary.Vector3 pos)
    {
        pos.x = CustomMathsLibrary.Clamp(pos.x, -laneWidth, laneWidth);
    }

    private void ApplyRotation(CustomMathsLibrary.Vector3 moveDir)
    {
        if (!thrownBall || ballRollSpeed <= 0.01f) return;

        CustomMathsLibrary.Vector3 direction = CustomMathsLibrary.Normalize(moveDir);

        CustomMathsLibrary.Vector3 axis = CustomMathsLibrary.CrossProduct(up, direction);

        float resistance = isGrounded ? 1f : airResistance;
        float speed = CustomMathsLibrary.Magnitude(moveDir);

        float angle = (speed / ballRadius) * Time.deltaTime * resistance;

        CustomMathsLibrary.Quat rotationQuat = new CustomMathsLibrary.Quat(axis, angle);

        currentRotation = rotationQuat * currentRotation;
    }

    private void ApplyTransform(CustomMathsLibrary.Vector3 pos)
    {
        CustomMathsLibrary.Quat yawRot = CustomMathsLibrary.Quat.Euler(0f, yawDegrees, 0f);

        CustomMathsLibrary.Quat finalRot = yawRot * currentRotation;

        transform.rotation = finalRot.ToUnityQuaternion();
        transform.position = pos;
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

        throwCharge = 0f;
        appliedThrowCharge = 0f;
        currentPullback = 0f;

        randomDragEndVector = CustomMathsLibrary.Vector3.zero;

        currentRotation = new CustomMathsLibrary.Quat(1, 0, 0, 0);
    }
}