using UnityEngine;
using UnityEngine.InputSystem;

public class BowlingBallControllerVERIFIER : MonoBehaviour
{
    private Rigidbody rb;
    private SphereCollider sphereCollider;

    private bool thrownBall, isGrounded, isCharging;

    [Header("Ball Holding Variables")]
    [Range(1, 3)]    [SerializeField] private float ballHoldMoveSpeed = 2f;
    [Range(0, 3)]    [SerializeField] private float ballRotateSpeed = 1f;
    [Range(0, 90)]   [SerializeField] private float ballMaxRotation = 45f;

    private Vector3 startPos = Vector3.zero; 
    private float yawDegrees;

    private Vector2 moveInput, rotateInput; 

    [Header("Ball Charge Variables")]
    [Range(1f, 10f)]   [SerializeField] private float ballChargeSpeed = 3f;
    [Range(0.1f, 1f)]  [SerializeField] private float maxPullbackDistance = 0.25f;
    [Range(1f, 10f)]   [SerializeField] private float pullbackSmoothing = 5f;
    [Range(0f, 3f)]    [SerializeField] private float maxThrowForce = 2;

    public float throwCharge;
    public float appliedThrowCharge;
    private float currentPullback;

    [Header("Ball Moving Variables")]
    [Range(0f, 10f)]   [SerializeField] private float ballMass = 8f;
    [Range(0f, 1f)]    [SerializeField] private float ballRadius = 0.5f;
    [Range(1f, 5f)]    [SerializeField] private float ballRollSpeed = 2f;
    [Range(1f, 5f)]    [SerializeField] private float ballMinRollSpeed = 2f;

    private float verticalVelocity;
    private float hookDirection;
    private float elapsedRollingTime;

    private Quaternion currentRotation = Quaternion.identity;
    private Vector3 up = Vector3.up;

    private void Start()
    {
        ResetBall();
    }

    public void ResetBall()
    {
        rb = GetComponent<Rigidbody>();

        transform.position = startPos;

        thrownBall = false;
        isGrounded = false;
        rb.useGravity = false;

        verticalVelocity = 0f;
        elapsedRollingTime = 0f;
        yawDegrees = 0f;

        throwCharge = 0f;
        appliedThrowCharge = 0f;
        currentPullback = 0f;
        hookDirection = 0f;

        currentRotation = Quaternion.identity;
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
        else
        {
            rotateInput = Vector2.zero;
        }
    }

    public void OnThrow(InputAction.CallbackContext context)
    {
        if (context.started && !thrownBall)
        {
            isCharging = true;
        }

        if (context.canceled && isCharging && !thrownBall)
        {
            isCharging = false;
            thrownBall = true;
            rb.useGravity = true;
            isGrounded = false;

            verticalVelocity = (throwCharge * 0.5f) / ballMass;
            appliedThrowCharge = throwCharge / ballMass;

            throwCharge = 0f;
        }
    }

    private void FixedUpdate()
    {
        if (isCharging)
        {
            throwCharge += ballChargeSpeed * Time.deltaTime;
            throwCharge = Mathf.Clamp(throwCharge, 0f, maxThrowForce);
        }

        if (!thrownBall)
        {
            yawDegrees -= rotateInput.y * ballRotateSpeed;
            yawDegrees = Mathf.Clamp(yawDegrees, -ballMaxRotation, ballMaxRotation);
        }

        Vector3 pos = transform.position; 
        pos = pos + new Vector3(0, 0, -currentPullback);

        Vector3 moveDir = GetMoveDir(pos);
        pos = GetMovement(pos, moveDir);

        if (thrownBall) pos = ApplyPhysics(pos, moveDir);

        ApplyRotation(moveDir);
        ApplyConstraints(ref pos);

        if (isCharging) pos = ApplyChargeForce(pos);

        ApplyTransform(pos);

        if (pos.z >= WorldData.laneDepth) ResetBall();
    }

    private Vector3 GetMoveDir(Vector3 pos)
    {
        if (!thrownBall)
        {
            return new Vector3(moveInput.x, 0f, 0f);
        }
        else
        {
            elapsedRollingTime += Time.deltaTime;
        }

        float yawRadians = yawDegrees * Mathf.Deg2Rad;
        Vector3 forward = new Vector3(Mathf.Sin(yawRadians), 0f, Mathf.Cos(yawRadians));

        float hookTime = Mathf.Clamp01(elapsedRollingTime * 0.5f);

        float hookSpeed = ballRollSpeed + (appliedThrowCharge * 0.15f);

        float hookStrength = hookSpeed * 0.25f;

        if (hookDirection == 0)
        {
            Vector3 laneRight = new Vector3(1, 0, 0);

            float side = Vector3.Dot(pos, laneRight);

            switch (side)
            {
                case > 0f:
                    hookDirection = 1f;
                    break;

                case < 0f:
                    hookDirection = -1f;
                    break;

                default:
                    hookDirection = Random.value < 0.5f ? -1 : 1;
                    break;
            }
        }

        hookStrength = hookStrength * hookTime;

        Vector3 right = Vector3.Cross(up, forward);

        Vector3 hookVector = right * hookStrength * hookDirection;

        return forward * hookSpeed + hookVector;
    }

    private Vector3 GetMovement(Vector3 pos, Vector3 moveDir)
    {
        Vector3 velocity = moveDir * (thrownBall ? ballRollSpeed : ballHoldMoveSpeed);

        return pos + (velocity * Time.deltaTime);
    }

    private Vector3 ApplyPhysics(Vector3 pos, Vector3 moveDir)
    {
        if (!isGrounded)
        {
            verticalVelocity += CustomPhysicsLibrary.CaculateObjectGravityForce(ballMass) * Time.deltaTime;

            pos.y += verticalVelocity * Time.deltaTime;

            float bottomPoint = pos.y - ballRadius;

            if (bottomPoint <= WorldData.worldGroundPos)
            {
                pos.y = WorldData.worldGroundPos + ballRadius;
                verticalVelocity = 0f;
                isGrounded = true;
            }

            appliedThrowCharge *= 1f / (1f - CustomPhysicsLibrary.AIR_DENSITY * 6f * Time.deltaTime);
        }
        else
        {
            float moveDirSpeed = moveDir.magnitude;
            float area = Mathf.PI * ballRadius * ballRadius;

            float dragForce = CustomPhysicsLibrary.CaculateObjectDragForce(moveDirSpeed, area);

            float finalDrag = dragForce / ballMass;
            float dragAccel = finalDrag * Time.deltaTime;

            appliedThrowCharge = Mathf.Clamp(appliedThrowCharge - dragAccel, 0f, appliedThrowCharge);

            ballRollSpeed *= CustomPhysicsLibrary.GROUND_FRICTION;

            if (ballRollSpeed < ballMinRollSpeed) ballRollSpeed = ballMinRollSpeed;
        }

        return pos;
    }

    private void ApplyRotation(Vector3 moveDir)
    {
        if (!thrownBall || ballRollSpeed <= 0.01f) return;

        Vector3 direction = moveDir.normalized;

        Vector3 axis = Vector3.Cross(up, direction);

        float resistance = isGrounded ? 1f : CustomPhysicsLibrary.AIR_DENSITY;
        float moveDirSpeed = moveDir.magnitude;

        float finalAngle = (moveDirSpeed / ballRadius) * Time.deltaTime * resistance;

        Quaternion rotationQuat = Quaternion.AngleAxis(finalAngle * Mathf.Rad2Deg, axis);

        currentRotation = rotationQuat * currentRotation;
    }

    private void ApplyConstraints(ref Vector3 pos)
    {
        pos.x = Mathf.Clamp(pos.x, -WorldData.laneWidth + ballRadius, WorldData.laneWidth - ballRadius);
    }

    private Vector3 ApplyChargeForce(Vector3 pos)
    {
        float chargePercent = throwCharge / maxThrowForce;
        float targetPullback = -chargePercent * maxPullbackDistance;

        currentPullback = Mathf.Lerp(currentPullback, targetPullback, pullbackSmoothing * Time.deltaTime);

        Vector3 offset = new Vector3(0, 0, currentPullback);

        return pos + offset;
    }

    private void ApplyTransform(Vector3 pos)
    {
        float yawRadians = yawDegrees * Mathf.Deg2Rad;
        Quaternion yawRot = Quaternion.AngleAxis(yawDegrees, Vector3.up);

        Quaternion finalRot = yawRot * currentRotation;

        transform.rotation = finalRot;

        transform.position = pos;
    }
}