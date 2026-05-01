using UnityEngine;
using UnityEngine.InputSystem;

public class BowlingBallController : MonoBehaviour
{
    private bool thrownBall, isGrounded, isCharging; // Keeps track of the ball's "phase" (thrown, grounded, charging)

    // ------ Holding Variables ------ //

    [Header("Ball Holding Variables")]
    [Range(1, 3)]    [SerializeField] private float ballHoldMoveSpeed = 2f; // How fast the ball moves left/right when being held (uses A/D to turn)
    [Range(0, 3)]    [SerializeField] private float ballRotateSpeed = 1f; // How fast the ball turns on the yaw axis when being held (uses W/S to turn)
    [Range(0, 90)]   [SerializeField] private float ballMaxRotation = 45f; // The maxiumum distance the ball can be aimed on the yaw

    private CustomMathsLibrary.Vector3 startPos; // Stored start position
    private float yawDegrees; // The stored aiming angle (when turning with W/S)

    private Vector2 moveInput, rotateInput; // Input that decides the ball's movement and rotation (WASD)

    [Header("Ball Charge Variables")]
    [Range(1, 10)]   [SerializeField] private float ballChargeSpeed = 3f; // How quickly the ball's charge fills when about to throw
    [Range(0.1f, 1)] [SerializeField] private float maxPullbackDistance = 0.25f; // How far the ball can (visually) be pulled back
    [Range(1, 10)]   [SerializeField] private float pullbackSmoothing = 5f; // For lerp / visual effect
    [Range(0, 3)]    [SerializeField] private float maxThrowForce = 2; // The maximum force the ball can be thrown (affects speed later on)

    private float throwCharge; // The current charge of the ball
    private float appliedThrowCharge; // The throw charge force that is applied after release
    private float currentPullback; // The current distance of the ball being pulled back (visually)

    // ------ Moving Variables ------ //

    [Header("Ball Moving Variables")]
    [Range(0, 10f)]  [SerializeField] private float ballMass = 8f; // The "weight" of the ball which affects gravity force and drag
    [Range(0, 1)]    [SerializeField] private float ballRadius = 0.5f; // The "size" of the ball which affects drag / rotation and how far away the ball is from the ground (TO DO)
    [Range(1, 5)]    [SerializeField] private float ballRollSpeed = 1.5f; // The ball's speed (based on forward)
    [Range(1, 5)]    [SerializeField] private float ballMinRollSpeed = 1.5f; // Prevents the ball from stopping entirely / going backwards (used to prevent softlock)

    private float verticalVelocity; // The velocity of the ball (affected by gravity)
    private float hookDirection; // Stored left/right curve of the ball
    private float elapsedRollingTime; // The total time the ball has been rolling since being released

    private CustomMathsLibrary.Quat currentRotation = new CustomMathsLibrary.Quat(1, 0, 0, 0); // Stored quaternion for the ball spin

    // ------ Constraint Variables ------ //

    [Header("Ball Constraints")]
    [Range(5, 50)]   [SerializeField] private float resetDistance = 30f; // The maxiumum distance the ball can travel before being reset

    private CustomMathsLibrary.Vector3 up = new CustomMathsLibrary.Vector3(0, 1, 0); // World up

    // ------ Collision Variables ------ //

    [SerializeField] private BowlingPinController[] pins;


    private void Start()
    {
        ResetBall();
    }

    // Resets the ball's variables to being held so it can be thrown again
    private void ResetBall()
    {
        transform.position = CustomMathsLibrary.Vector3.zero;

        thrownBall = false;
        isGrounded = false;

        verticalVelocity = 0f;
        elapsedRollingTime = 0f;
        yawDegrees = 0f;

        throwCharge = 0f;
        appliedThrowCharge = 0f;
        currentPullback = 0f;
        hookDirection = 0f;

        currentRotation = new CustomMathsLibrary.Quat(1, 0, 0, 0);
    }

    // Stores left/right input (uses A/D keys)
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    // Stores the rotate input (uses W/S keys)
    public void OnRotate(InputAction.CallbackContext context)
    {
        // The ball can only be aimed before throwing
        if (!thrownBall)
        {
            rotateInput = context.ReadValue<Vector2>();
        }
        else
        {
            rotateInput = CustomMathsLibrary.Vector2.zero;
        }
    }

    // Uses a charge/release system to apply inital force the ball when SPACE is held/released
    public void OnThrow(InputAction.CallbackContext context)
    {
        if (context.started && !thrownBall)
        {
            isCharging = true;
        }

        // Adds some force to upward and forward motion when the SPACE key is released (based on ball mass)
        // Heavier the ball = less force is applied

        if (context.canceled && isCharging && !thrownBall)
        {
            isCharging = false;
            thrownBall = true;
            isGrounded = false;

            verticalVelocity = (throwCharge * 0.5f) / ballMass;
            appliedThrowCharge = throwCharge / ballMass;

            throwCharge = 0f;
        }
    }

    // Runs physics every frame
    private void FixedUpdate()
    {
        // Handles the charging of the ball by time.deltaTime, clamping it to the max throwforce
        if (isCharging)
        {
            throwCharge += ballChargeSpeed * Time.deltaTime;
            throwCharge = CustomMathsLibrary.Clamp(throwCharge, 0f, maxThrowForce);
        }

        // Handles the aiming of the ball through the rotate input, clamping it to the negative max rotation and positive max rotation
        if (!thrownBall)
        {
            yawDegrees -= rotateInput.y * ballRotateSpeed;
            yawDegrees = CustomMathsLibrary.Clamp(yawDegrees, -ballMaxRotation, ballMaxRotation);
        }

        // ------ MAIN PIPELINE ------ //

        // 1. Start with current position (pos)
        // 2. Add the pullback offset (ApplyChargeForce)
        // 3. Get the move direction (moveDir) (GetMoveDir)
        // 4. Apply the movement (moveDir) to the current position (pos) to create a new position which combines the direction
        // 5. Using the new position and moveDir, apply physics like gravity, drag, hook etc. (ApplyPhysics)
        // 6. Using the moveDir, apply the rotation to the ball (ApplyRotation)
        // 7. Clamp/constrain the position to the ground, lane width, etc. (ApplyConstraints)
        // 8. (If charging) apply charge animation
        // 9. Push the final pos to the transform
        // 10. If the final pos.z >= than the resetDistance, reset the ball entirely (ResetBall)

        CustomMathsLibrary.Vector3 pos = transform.position; 

        pos = CustomMathsLibrary.Add(pos, new CustomMathsLibrary.Vector3(0, 0, -currentPullback));

        CustomMathsLibrary.Vector3 moveDir = GetMoveDir(pos);
        pos = GetMovement(pos, moveDir);

        if (thrownBall) pos = ApplyPhysics(pos, moveDir);

        ApplyRotation(moveDir);
        ApplyConstraints(pos);

        if (isCharging) pos = ApplyChargeForce(pos);

        HandlePinCollisions(ref pos, moveDir);

        ApplyTransform(pos);

        if (pos.z >= resetDistance) ResetBall();
    }

    private void HandlePinCollisions(ref CustomMathsLibrary.Vector3 ballPos, CustomMathsLibrary.Vector3 moveDir)
    {
        foreach (var pin in pins)
        {
            CustomMathsLibrary.Vector3 normal;
            float penetration;

            bool hit = CollisionUtility.SphereCapsuleCollision(ballPos, ballRadius, pin.GetBottom(), pin.GetTop(), pin.pinRadius, out normal, out penetration);
            
            if (!hit) continue;

            ballPos = CustomMathsLibrary.Add(ballPos, CustomMathsLibrary.Scale(normal, penetration));

            float speed = CustomMathsLibrary.Magnitude(moveDir);

            CustomMathsLibrary.Vector3 impulse = CustomMathsLibrary.Scale(normal, speed * ballMass);

            pin.ApplyImpulse(impulse);

            appliedThrowCharge *= 0.7f;
            ballRollSpeed *=0.85f;
        }
    }

    // Returns a charge force based on the ball's current position (pos) as an input
    // More charge = ball moves backward more until hitting the limit
    private CustomMathsLibrary.Vector3 ApplyChargeForce(CustomMathsLibrary.Vector3 pos)
    {
        float chargePercent = throwCharge / maxThrowForce;
        float targetPullback = -chargePercent * maxPullbackDistance;

        // The position is lerped based on the maximum pullback (targetPullback) vs the current pullback (currentPullback) and the smoothing (pullbackSmoothing) variables
        currentPullback = CustomMathsLibrary.Lerp(currentPullback, targetPullback, pullbackSmoothing * Time.deltaTime);

        CustomMathsLibrary.Vector3 offset = new CustomMathsLibrary.Vector3(0, 0, currentPullback);

        // Returns the original pos vector + adding on the offset (offset combining the pullback on the z-axis)
        return CustomMathsLibrary.Add(pos, offset);
    }

    // Returns a move trajectory on the ball's current position (pos) as an input
    private CustomMathsLibrary.Vector3 GetMoveDir(CustomMathsLibrary.Vector3 pos)
    {
        // If the ball hasn't been thrown, it uses the move input (moveInput) to control the sideways movement of the ball on the x-axis 
        if (!thrownBall)
        {
            return new CustomMathsLibrary.Vector3(moveInput.x, 0f, 0f);
        }

        // Caculates the inital forward direction by how much the ball has turned in degrees (yawDegrees) to radians (yawRadians)
        float yawRadians = CustomMathsLibrary.DegreesToRadians(yawDegrees);
        CustomMathsLibrary.Vector3 forward = CustomMathsLibrary.ForwardFromYawPitch(yawRadians, 0);

        elapsedRollingTime += Time.deltaTime;

        // Creates a time-based bowling curve by clamping the hook time (hookTime)
        // Hook starts weak -> gets stronger overtime
        float hookTime = CustomMathsLibrary.Clamp(elapsedRollingTime * 0.5f, 0, 1);

        // Speed has an influence on the hook.
        // More speed -> stronger hook
        float hookSpeed = ballRollSpeed + (appliedThrowCharge * 0.15f);
        float hookStrength = hookSpeed * 0.25f;

        // Finds the closest side of the lane and changes the direction based on it
        float closestSide = pos.x / WorldData.laneWidth;

        // POTENTAL FIX: only applies when the hookdirection != 0, returns Mathf.Sign(closest side (closestSide)) so a 0 or a 1
        if (hookDirection == 0) hookDirection = Mathf.Sign(closestSide);

        // If ball is on the right -> hooks right and vice versa
        hookStrength = hookStrength * hookTime;

        // Building the hook vector by creating a sideways force by finding the right pos and combining it with hook strength (hookStrength) * hook direction * (hookDirection)
        CustomMathsLibrary.Vector3 right = CustomMathsLibrary.CrossProduct(up, forward);
        CustomMathsLibrary.Vector3 hookVector = CustomMathsLibrary.Scale(right, hookStrength * hookDirection);

        // Returns the final movement/curved trajectory vector by combining the forward, hook speed (hookSpeed) and hook vector (hookVector)
        return CustomMathsLibrary.Add(CustomMathsLibrary.Scale(forward, hookSpeed), hookVector);
    }

    // Returns the ball's velocity based on the ball's position (pos) and ball's move direction (moveDir)
    private CustomMathsLibrary.Vector3 GetMovement(CustomMathsLibrary.Vector3 pos, CustomMathsLibrary.Vector3 moveDir)
    {
        // Creates a new velocity vector by scaling the move direction (moveDir) with either:
        // - The ball roll speed (ballRollSpeed) if the ball has been thrown
        // - The ball hold move speed (ballHoldMoveSpeed) if the ball has not been thrown
        CustomMathsLibrary.Vector3 velocity = CustomMathsLibrary.Scale(moveDir, thrownBall ? ballRollSpeed : ballHoldMoveSpeed);

        // Returns the new position by adding the velocity onto the old position  
        return CustomMathsLibrary.Add(pos, CustomMathsLibrary.Scale(velocity, Time.deltaTime));
    }

    private CustomMathsLibrary.Vector3 ApplyPhysics(CustomMathsLibrary.Vector3 pos, CustomMathsLibrary.Vector3 moveDir)
    {
        if (!isGrounded)
        {
            // Caculates the ball's vertical velocity (verticalVelocity) using the ball's mass (ballMass) and gravity from the CustomPhysicsLibrary (-9.81)
            verticalVelocity += CustomPhysicsLibrary.CaculateObjectGravityForce(ballMass) * Time.deltaTime;

            pos.y += verticalVelocity * Time.deltaTime;

            float bottomPoint = pos.y - ballRadius;

            if (bottomPoint <= WorldData.worldGroundPos)
            {
                pos.y = WorldData.worldGroundPos + ballRadius;
                verticalVelocity = 0f;
                isGrounded = true;
            }

            // Applies air resistance to the ball if it is still in the air using the air density from the CustomPhysicsLibrary
            appliedThrowCharge *= 1f / (1f - CustomPhysicsLibrary.AIR_DENSITY * 6f * Time.deltaTime);
        }
        else
        {
            // Caculates the ball's overall speed and surface area
            float speed = CustomMathsLibrary.Magnitude(moveDir);
            float area = Mathf.PI * ballRadius * ballRadius;

            // Caculates the drag force using the CustomPhysicsLibrary by combining speed and area
            float dragForce = CustomPhysicsLibrary.CaculateObjectDragForce(speed, area);

            // Caculates the ball's final drag force by combining the inital dragforce with the ball's mass
            float finalDrag = dragForce / ballMass;
            float dragAccel = finalDrag * Time.deltaTime;

            // Reduces the ball's speed by clamping the drag (dragAccel) with the applied throw charge (appliedThrowCharge)
            appliedThrowCharge = CustomMathsLibrary.Clamp(appliedThrowCharge - dragAccel, 0f, appliedThrowCharge);

            // Combines the ball's roll speed with the ground friction (GROUND_FRICTION) from the CustomPhysicsLibrary
            ballRollSpeed *= CustomPhysicsLibrary.GROUND_FRICTION;

            // Safeguards to a minimum speed to prevent the friction from stopping the ball completely
            if (ballRollSpeed < ballMinRollSpeed) ballRollSpeed = ballMinRollSpeed;
        }

        return pos;
    }

    // Constrains the ball from going off of the lane width's (laneWidth)
    private void ApplyConstraints(CustomMathsLibrary.Vector3 pos)
    {
        pos.x = CustomMathsLibrary.Clamp(pos.x, -WorldData.laneWidth + ballRadius, WorldData.laneWidth - ballRadius);
    }

    // Applies the ball's rotation by using a Quat instead of euler angles
    private void ApplyRotation(CustomMathsLibrary.Vector3 moveDir)
    {
        // Only rotate if the ball has been thrown and if the ball's roll speed (ballRollSpeed) is above 0.01f
        if (!thrownBall || ballRollSpeed <= 0.01f) return;

        // Find a (correct) normalized direction of the ball 
        CustomMathsLibrary.Vector3 direction = CustomMathsLibrary.Normalize(moveDir);

        // Find the correct rolling axis based on worldUp (up) and the current normalized direction of the ball
        CustomMathsLibrary.Vector3 axis = CustomMathsLibrary.CrossProduct(up, direction);

        // Apply air resistance (AIR_DENSITY) depending on if the ball is grounded (isGrounded) or not
        float resistance = isGrounded ? 1f : CustomPhysicsLibrary.AIR_DENSITY;
        float speed = CustomMathsLibrary.Magnitude(moveDir);

        // Compute the final angle of the ball by dividing the speed of the ball (speed) with the radius of the ball (radius) This makes it so that the larger the ball; the slower it goes
        // Then multiply this by the air resistance (resistance)
        float finalAngle = (speed / ballRadius) * Time.deltaTime * resistance;

        // Create a new quaternion that combines the axis and the final angle
        CustomMathsLibrary.Quat rotationQuat = new CustomMathsLibrary.Quat(axis, finalAngle);

        // Apply the finished quaternion to the current rotation
        currentRotation = rotationQuat * currentRotation;
    }

    // Applies the final transform of the ball with the position (pos) of the ball as an input
    private void ApplyTransform(CustomMathsLibrary.Vector3 pos)
    {
        // The yaw rotation (yawRot) handles the aiming direction
        CustomMathsLibrary.Quat yawRot = CustomMathsLibrary.Quat.Euler(0f, yawDegrees, 0f);

        // The yawRot is then combined with the current rotation of the ball (currentRotation) which acts as the rolling spin
        CustomMathsLibrary.Quat finalRot = yawRot * currentRotation;

        // Apply the final rotation to the ball
        transform.rotation = finalRot.ToUnityQuaternion();

        // Apply the final position to the ball
        transform.position = pos;
    }
}