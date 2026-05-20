using UnityEngine;

public class NEWBowlingPinController : MonoBehaviour
{
    [Header("Pin Physical Variables")]
    [Range(0.25f, 1)] [SerializeField] private float pinRadius = 0.5f;
    [Range(0.5f, 2)]  [SerializeField] private float pinHeight = 2f;
    [Range(1, 5)]     [SerializeField] private float pinMass = 2f;

    public CustomMathsLibrary.Vector3 pinVelocity { get; private set; } = CustomMathsLibrary.Vector3.zero; // Linear velocity of the pin
    public CustomMathsLibrary.Vector3 angularVelocity = CustomMathsLibrary.Vector3.zero; // Spin / tilt rate of the pin

    private CustomMathsLibrary.Vector3 up = new CustomMathsLibrary.Vector3(0, 1, 0); // World up
    private CustomMathsLibrary.Vector3 bottomPoint = CustomMathsLibrary.Vector3.zero; // Used to create the bottom point of the pin

    private CustomMathsLibrary.Quat rotation = new CustomMathsLibrary.Quat(1, 0, 0, 0); // Current orientation of the pin (custom quat)

    private float inertia; // Rotational resistance (mass distribution approximation)

    public bool isGrounded; // True = pin is fully touching the ground
    private bool hasFallen; // True = Pin has tipped past the threshold
    private bool hasCorrection; // External collision checker to check if correction needs to be applied

    private CustomMathsLibrary.Quat finalRotation; // Stored final rotation of the pin
    private CustomMathsLibrary.Vector3 startPosition; // Stored start position of the pin
    private CustomMathsLibrary.Vector3 storedCorrectionDelta; // Stored correction data of the pin (used for collisions)

    private void Start()
    {
        startPosition = transform.position;
        pos = transform.position;

        ResetPin();
    }

    // Resets the pins's variables to being held so it can be thrown again
    public void ResetPin()
    {
        transform.position = startPosition;
        pos = transform.position;

        transform.rotation = new CustomMathsLibrary.Quat(1, 0, 0, 0).ToUnityQuaternion();

        isGrounded = false;
        hasFallen = false;
        hasCorrection = false;

        finalRotation = new CustomMathsLibrary.Quat(1, 0, 0, 0);
        storedCorrectionDelta = CustomMathsLibrary.Vector3.zero;

        rotation = new CustomMathsLibrary.Quat(1, 0, 0, 0);
        pinVelocity = CustomMathsLibrary.Vector3.zero;
        angularVelocity = CustomMathsLibrary.Vector3.zero;

        inertia = (1f / 12f) * pinMass * (3 * pinRadius * pinRadius + pinHeight * pinHeight); // Computes moment of intertia for the pin (which is a cylinder-like object)
    }

    // Returns the pin radius / mass (used for determining collisions)
    public float GetPinRadius() { return pinRadius; }
    public float GetPinMass() { return pinMass; }

    public CustomMathsLibrary.Vector3 GetTopPoint() { return CustomMathsLibrary.Add(pos, CustomMathsLibrary.Scale(GetUpDir(), (pinHeight * 0.5f) - pinRadius)); }

    // Returns the bottom based on orientation
    public CustomMathsLibrary.Vector3 GetBottomPoint() { return CustomMathsLibrary.Subtract(pos, CustomMathsLibrary.Scale(GetUpDir(), (pinHeight * 0.5f) - pinRadius)); }

    // Returns the pin's actual up direction in the world space
    private CustomMathsLibrary.Vector3 GetUpDir() { return rotation.RotateVector(up); }

    private CustomMathsLibrary.Vector3 pos;

    private void FixedUpdate()
    {
        // ------ MAIN PIPELINE ------ //

        // 1. Start with the current position (pos)
        // 2. Apply linear motion to the pin by velocity
        // 3. Apply gravity torque / ground interaction (ApplyGravityTorque), handles pin tipping
        // 4. Apply damping to the pin, esentially handles killing the "energy" of the pin (ApplyDamping)
        // 5. IF the pin has fallen, check if the pin has tipped over, (and applies fall axis) (CheckFallen) and then applies angular rotation (ApplyAngularRotation)
        // 6. Corrects the position of the pin for geometry consistency (prevents drift between rotation and position) (CorrectPosition)
        // 7. Clamp/constrain the position to the ground, lane width, etc. (ApplyConstraints)
        // 8. IF the pin has fallen, lock the rotation of the pin to the final orientation (ConstrainRotation)
        // 9. IF the pin has a corrected position that has been sent by the collider, apply the delta (ApplyCollisionCorrection)
        // 10. Push the final pos to the transform (ApplyTransform)

        // x = x + v * dt
        pos = CustomMathsLibrary.Add(pos, CustomMathsLibrary.Scale(pinVelocity, Time.deltaTime));

        ApplyGravityTorque(ref pos);

        ApplyDamping();

        if (!hasFallen) { CheckFallen(); ApplyAngularRotation(); }

        CorrectPosition(ref pos);

        ApplyConstraints(ref pos);

        if (hasFallen) { ConstrainRotation(); }

        if (hasCorrection) ApplyCollisionCorrection(ref pos);

        ApplyTransform(pos);

        FinalGroundSnap(ref pos);

        DrawDebugs();
    }

    // Is called when a collision happens between either the bowling pin and the ball OR the bowling pin and another bowling pin

    public void ApplyCollisionImpulse(CustomMathsLibrary.Vector3 impulse, CustomMathsLibrary.Vector3 hitPoint)
    {
        // Artificial boost (impulseBoost) to increase the collision strength
        float impulseBoost = 1.5f;
        float angularBoost = 1.25f;
        impulse = CustomMathsLibrary.Scale(impulse, impulseBoost);

        impulse.y *= 0.15f;

        pinVelocity.y = Mathf.Min(pinVelocity.y, 3f);

        // Applies linear impulse to the ball to push it away from the hit point (deltaTime = ib / m)
        // Bigger mass = move less
        pinVelocity = CustomMathsLibrary.Add(pinVelocity, CustomMathsLibrary.Scale(impulse, 1f / pinMass));

        // r = vector that point's from the pin's center of mass to the point where it was hit
        CustomMathsLibrary.Vector3 r = CustomMathsLibrary.Subtract(hitPoint, transform.position);

        // Convert force into torque (r x F), measures the off-centered point of the hit
        CustomMathsLibrary.Vector3 torque = CustomMathsLibrary.CrossProduct(r, impulse);

        // Converts torque into angular acceleration (angularAccel)
        CustomMathsLibrary.Vector3 angularAccel = CustomMathsLibrary.Scale(torque, 1f / inertia);

        if (angularAccel.x < 0)
        {
            angularAccel = new CustomMathsLibrary.Vector3(CustomMathsLibrary.Clamp(angularAccel.x, -7.5f, -20), angularAccel.y, angularAccel.z);
        }

        // Apply the velocity change
        angularVelocity = CustomMathsLibrary.Add(angularVelocity, CustomMathsLibrary.Scale(angularAccel, angularBoost));
    }

    // Applies gravity torque (tipping) to the pin to simulate falling over
    private void ApplyGravityTorque(ref CustomMathsLibrary.Vector3 pos)
    {
        bottomPoint = GetBottomPoint();

        print(bottomPoint.y);

        float targetBottomPoint = hasFallen ? WorldData.worldGroundPos + pinRadius : WorldData.worldGroundPos + (pinRadius * 2);

        // Ground collision
        if (bottomPoint.y <= targetBottomPoint)
        {
            if (pinVelocity.y < 0) pinVelocity.y = 0f;

            isGrounded = true;
        }
        else
        {
            isGrounded = false;

            pinVelocity.y += CustomPhysicsLibrary.CaculateObjectGravityForce(pinMass) * Time.deltaTime;
        }

        if (!isGrounded || hasFallen) return;

        CustomMathsLibrary.Vector3 worldUp = new CustomMathsLibrary.Vector3(0, 1, 0);

        CustomMathsLibrary.Vector3 pinUp = GetUpDir();

        CustomMathsLibrary.Vector3 tiltAxis = CustomMathsLibrary.CrossProduct(pinUp, worldUp);

        float tiltAmount = CustomMathsLibrary.Magnitude(tiltAxis);

        if (tiltAmount < 0.001f) return;

        tiltAxis = CustomMathsLibrary.Normalize(tiltAxis);

        // Artificial gravity strength tipping
        float gravityTorqueStrength = pinMass * 12.5f * tiltAmount;

        CustomMathsLibrary.Vector3 gravityTorque = CustomMathsLibrary.Scale(tiltAxis, gravityTorqueStrength);

        // Angular acceleration
        CustomMathsLibrary.Vector3 angularAccel = CustomMathsLibrary.Scale(gravityTorque, 1f / inertia);

        // Apply velocity
        angularVelocity =CustomMathsLibrary.Add(angularVelocity, CustomMathsLibrary.Scale(angularAccel, Time.deltaTime));

        // Sleep threshold
        if (CustomMathsLibrary.Magnitude(angularVelocity) < 0.05f)
        {
            angularVelocity = CustomMathsLibrary.Vector3.zero;
        }
    }

    // Prevents the pin from sliding / snapping depending on if it's grounded or not; more fake physics
    private void ApplyDamping()
    {
        float linearDamping = isGrounded ? 0.95f : 0.99f;
        pinVelocity = CustomMathsLibrary.Scale(pinVelocity, linearDamping);

        float angularDamping = isGrounded ? 0.85f : 0.95f;
        angularVelocity = CustomMathsLibrary.Scale(angularVelocity, angularDamping);
    }

    private void CheckFallen()
    {
        // Figures out if the pin is perfectly upright or not
        // 1 = perfectly upright
        // 0 = 90 degrees sideways 
        // - 1 = 180 degrees sideways
        float uprightDot = CustomMathsLibrary.Dot(GetUpDir(), up);

        // Decided whether or not the pin has fallen so it can snap the rotation later
        if (!hasFallen && uprightDot < 0.95f)
        {
            hasFallen = true;

            CustomMathsLibrary.Vector3 fallAxis = CustomMathsLibrary.CrossProduct(up, GetUpDir());
            if (CustomMathsLibrary.Magnitude(fallAxis) < 0.001f) fallAxis = new CustomMathsLibrary.Vector3(1,0,0);

            finalRotation = new CustomMathsLibrary.Quat(fallAxis, Mathf.PI / 2);
        }
    }

    // Quaternion intergration - turns the velocity into actual rotation
    private void ApplyAngularRotation()
    {
        float angularSpeed = CustomMathsLibrary.Magnitude(angularVelocity);

        if (angularSpeed > 0f)
        {
            CustomMathsLibrary.Vector3 axis = CustomMathsLibrary.Normalize(angularVelocity);
            float angle = Mathf.Min(angularSpeed * Time.deltaTime, 0.1f);

            CustomMathsLibrary.Quat deltaRot = new CustomMathsLibrary.Quat(axis, angle);

            CustomMathsLibrary.Vector3 pivot = GetBottomPoint();

            rotation = deltaRot * rotation;

            CustomMathsLibrary.Vector3 newBottom = GetBottomPoint();

            CustomMathsLibrary.Vector3 correction = CustomMathsLibrary.Subtract(pivot, newBottom);

            pos = CustomMathsLibrary.Add(pos, correction);
        }
    }

    // Corrects the position so that the geometry of the pin stays consistent; preventing floating due to the rotation drift
    // Esentially tried to ground the pin to the bottom point of the lane
    private void CorrectPosition(ref CustomMathsLibrary.Vector3 pos)
    {
        CustomMathsLibrary.Vector3 currentBottom = GetBottomPoint();

        float penetration = WorldData.worldGroundPos - currentBottom.y;

        if (penetration > 0f)
        {
            pos.y += penetration;
        }
    }

    // Applies constraints to the pin to stop it from going outside of the lane
    private void ApplyConstraints(ref CustomMathsLibrary.Vector3 pos)
    {
        pos.x = CustomMathsLibrary.Clamp(pos.x, -WorldData.laneWidth + pinRadius, WorldData.laneWidth - pinRadius);

        pos.z = CustomMathsLibrary.Clamp(pos.z, 0 + pinRadius, WorldData.laneDepth - pinRadius);
    }

    // Bug fix to snap the pin to a rotation after tilting
    private void ConstrainRotation()
    {
        angularVelocity = CustomMathsLibrary.Vector3.zero;

        rotation = finalRotation;
    }

    // Stores correction data (collision penetration) to prevent overlaps
    public void StoreCorrectionDelta(CustomMathsLibrary.Vector3 delta)
    {
        hasCorrection = true;
        storedCorrectionDelta = delta;
    }

    // Applies a correction that fixes physics overlaps (collision penetration)
    private void ApplyCollisionCorrection(ref CustomMathsLibrary.Vector3 pos)
    {
        hasCorrection = false;
        pos = CustomMathsLibrary.Add(pos, storedCorrectionDelta);
    }

    // Apply the final transformations to the pin, including the stored rotation
    private void ApplyTransform(CustomMathsLibrary.Vector3 pos)
    {
        transform.position = pos;
        transform.rotation = rotation.ToUnityQuaternion();
    }

    private void FinalGroundSnap(ref CustomMathsLibrary.Vector3 pos)
    {
        CustomMathsLibrary.Vector3 finalBottom = GetBottomPoint();

        float targetGround = WorldData.worldGroundPos + pinRadius;

        float penetration = targetGround - finalBottom.y;

        if (Mathf.Abs(penetration) > 0.0001f)
        {
            pos.y += penetration;
        }

        finalBottom = GetBottomPoint();

        isGrounded = Mathf.Abs(finalBottom.y - targetGround) < 0.01f;
    }

    private void DrawDebugs()
    {
        CustomMathsLibrary.Vector3 upDirDebug = rotation.RotateVector(up);

        CustomMathsLibrary.Vector3 bottomDebug = CustomMathsLibrary.Subtract(transform.position, CustomMathsLibrary.Scale(upDirDebug, pinHeight * 0.5f));
        CustomMathsLibrary.Vector3 topDebug = CustomMathsLibrary.Add(transform.position, CustomMathsLibrary.Scale(upDirDebug, pinHeight * 0.5f));

        Debug.DrawLine(bottomDebug, topDebug, Color.red);

        CustomMathsLibrary.Vector3 right = CustomMathsLibrary.Scale(CustomMathsLibrary.RotateAroundAxis(upDirDebug, new CustomMathsLibrary.Vector3(0, 1, 0), 0), pinRadius);
        CustomMathsLibrary.Vector3 left = CustomMathsLibrary.Scale(CustomMathsLibrary.RotateAroundAxis(upDirDebug, new CustomMathsLibrary.Vector3(0, 1, 0), Mathf.PI), pinRadius);
        CustomMathsLibrary.Vector3 forward = CustomMathsLibrary.Scale(CustomMathsLibrary.RotateAroundAxis(upDirDebug, new CustomMathsLibrary.Vector3(0, 1, 0), Mathf.PI / 2), pinRadius);
        CustomMathsLibrary.Vector3 back = CustomMathsLibrary.Scale(CustomMathsLibrary.RotateAroundAxis(upDirDebug, new CustomMathsLibrary.Vector3(0, 1, 0), -Mathf.PI / 2), pinRadius);

        Debug.DrawLine(bottomDebug, CustomMathsLibrary.Add(bottomDebug, right), Color.cyan);
        Debug.DrawLine(bottomDebug, CustomMathsLibrary.Add(bottomDebug, left), Color.cyan);
        Debug.DrawLine(bottomDebug, CustomMathsLibrary.Add(bottomDebug, forward), Color.cyan);
        Debug.DrawLine(bottomDebug, CustomMathsLibrary.Add(bottomDebug, back), Color.cyan);
    }
}