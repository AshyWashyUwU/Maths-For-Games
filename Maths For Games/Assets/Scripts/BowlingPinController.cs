using UnityEngine;

public class BowlingPinController : MonoBehaviour
{
    private bool hasFallen, isGrounded, hasStoredCorrection; // Keeps track of the pin's "phases" (fallen, grounded, hasStoredCorrection)

    [Header("Pin Physical Variables")]
    [Range(0.25f, 1f)] [SerializeField] private float pinRadius = 0.5f; // The "size" of the pin which is used for collisions / rotation
    [Range(0.5f, 2f)]  [SerializeField] private float pinHeight = 2f; // The "height" of the pin which is used mainly for tipping and the points between two radii
    [Range(1f, 5f)]    [SerializeField] private float pinMass = 2f; // The "weight" of the pin which affects gravity force, drag, collisions and how quickly the pin falls over
    [Range(0f, 1f)]    [SerializeField] private float fallThreshold = 0.95f; // The threshold at which the pin rotates enough to tilt over (used by a dot product)

    private CustomMathsLibrary.Vector3 pinVelocity = CustomMathsLibrary.Vector3.zero; // Linear velocity of the pin
    private CustomMathsLibrary.Vector3 angularVelocity = CustomMathsLibrary.Vector3.zero; // Spin / tilt rate of the pin

    private CustomMathsLibrary.Vector3 pos; // Stored pos for the pin (mainly stored so that GetTopPoint and GetBottomPoint work correctly)

    private CustomMathsLibrary.Vector3 up = new CustomMathsLibrary.Vector3(0, 1, 0); // World up

    private CustomMathsLibrary.Quat currentRotation = new CustomMathsLibrary.Quat(1, 0, 0, 0); // Current orientation of the pin (custom quat)

    private float inertia; // Rotational resistance (fake physicsy mass distribution approximation)

    private CustomMathsLibrary.Quat finalRotation; // Stored final rotation of the pin
    private CustomMathsLibrary.Vector3 startPosition; // Stored start position of the pin
    private CustomMathsLibrary.Vector3 storedCorrectionDelta; // Stored correction data of the pin (used for collisions)

    // Returns the pin radius / mass / velocity (used for determining collisions)
    public float GetPinRadius() { return pinRadius; }
    public float GetPinMass() { return pinMass; }
    public CustomMathsLibrary.Vector3 GetPinVelocity() { return pinVelocity; }

    // Returns the top and bottom points (also based on pin orientation)
    public CustomMathsLibrary.Vector3 GetTopPoint() { return CustomMathsLibrary.Add(pos, CustomMathsLibrary.Scale(GetUpDir(), (pinHeight * 0.5f) - pinRadius)); }
    public CustomMathsLibrary.Vector3 GetBottomPoint() { return CustomMathsLibrary.Subtract(pos, CustomMathsLibrary.Scale(GetUpDir(), (pinHeight * 0.5f) - pinRadius)); }

    // Returns the pin's actual up direction in the world space
    private CustomMathsLibrary.Vector3 GetUpDir() { return currentRotation.RotateVector(up); }

    // Reset ball and pos at the start for safety
    private void Start()
    {
        startPosition = transform.position;

        ResetPin();
    }

    // Resets the pins's variables to being held so it can be hit again
    public void ResetPin()
    {
        transform.position = startPosition;
        pos = transform.position;

        currentRotation = new CustomMathsLibrary.Quat(1, 0, 0, 0);

        transform.rotation = currentRotation.ToUnityQuaternion();

        isGrounded = false;
        hasFallen = false;
        hasStoredCorrection = false;

        finalRotation = new CustomMathsLibrary.Quat(1, 0, 0, 0);
        storedCorrectionDelta = CustomMathsLibrary.Vector3.zero;

        pinVelocity = CustomMathsLibrary.Vector3.zero;
        angularVelocity = CustomMathsLibrary.Vector3.zero;

        inertia = (1f / 12f) * pinMass * (3 * pinRadius * pinRadius + pinHeight * pinHeight); // Computes moment of intertia for the pin (which is a cylinder-like object)
    }

    private void FixedUpdate()
    {
        // ------ MAIN PIPELINE ------ //

        // 1. Start with the current position (pos)
        // 2. Apply linear motion to the pin by velocity
        // 3. Applies gravity force and tipping torque; handles grounding and angular acceleration (ApplyGravityTorque)
        // 4. Apply damping to the pin, esentially handles killing the "energy" of the pin (ApplyDamping)
        // 5. If pin hasn't fallen, check if it should be considered fallen (CheckFallen) and apply angular rotation (ApplyAngularRotation)
        // 6. Clamp/constrain the position to the ground, lane width, etc. (ApplyConstraints)
        // 7. IF the pin has fallen, lock the rotation of the pin to the final orientation (ConstrainRotation)
        // 8. IF the pin has a corrected position that has been sent by the collider, apply the delta (ApplyCollisionCorrection)
        // 9. Push the final pos to the transform (ApplyTransform)

        // x = x + v * dt
        pos = transform.position;
        pos = CustomMathsLibrary.Add(pos, CustomMathsLibrary.Scale(pinVelocity, Time.deltaTime));

        ApplyGravityTorque(ref pos);

        ApplyDamping();

        if (!hasFallen) { CheckFallen(); ApplyAngularRotation(); }

        ApplyConstraints(ref pos);

        if (hasFallen) { ConstrainRotation(); }

        if (hasStoredCorrection) ApplyCollisionCorrection(ref pos);

        ApplyTransform(pos);

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
        // Find the bottom point of the world + the pin height scaled by the UpDir
        CustomMathsLibrary.Vector3 bottomWorld = CustomMathsLibrary.Subtract(pos, CustomMathsLibrary.Scale(GetUpDir(), (pinHeight * 0.5f)));

        // Find the ground based on if the pin is rotated or not
        float groundY = hasFallen ? WorldData.worldGroundPos + pinRadius : WorldData.worldGroundPos;
        isGrounded = bottomWorld.y <= groundY;

        // Ground the pin
        if (isGrounded)
        {
            float penetration = groundY - bottomWorld.y;
            pos.y += penetration;

            if (pinVelocity.y < 0f) pinVelocity.y = 0f;
        }
        else
        {
            pinVelocity.y += CustomPhysicsLibrary.CaculateObjectGravityForce(pinMass) * Time.deltaTime; // Make the pin fall by gravity force
        }

        if (!isGrounded || hasFallen) return; // Only tilts if the pin has not finished falling OR is grounded 

        // Caculate the tilt amount using a crossproduct on the up direction (based on the pin's rotation and worldUp)
        CustomMathsLibrary.Vector3 tiltAxis = CustomMathsLibrary.CrossProduct(GetUpDir(), up);
        float tiltAmount = CustomMathsLibrary.Magnitude(tiltAxis);

        if (tiltAmount < 0.001f) return;

        tiltAxis = CustomMathsLibrary.Normalize(tiltAxis);

        // Artificial gravity strength tipping
        float gravityTorqueStrength = pinMass * 35f * tiltAmount;

        CustomMathsLibrary.Vector3 gravityTorque = CustomMathsLibrary.Scale(tiltAxis, gravityTorqueStrength);

        // Angular acceleration
        CustomMathsLibrary.Vector3 angularAccel = CustomMathsLibrary.Scale(gravityTorque, 1f / inertia);

        // Additional fake dampening
        angularVelocity = CustomMathsLibrary.Scale(angularVelocity, 0.995f);

        // Apply velocity
        angularVelocity = CustomMathsLibrary.Add(angularVelocity, CustomMathsLibrary.Scale(angularAccel, Time.deltaTime));
    }

    // Prevents the pin from sliding / snapping depending on if it's grounded or not; more fake physics
    private void ApplyDamping()
    {
        float linearDamping = isGrounded ? 0.8f : 0.99f;
        pinVelocity = CustomMathsLibrary.Scale(pinVelocity, linearDamping);

        float angularDamping = isGrounded ? 0.65f : 0.95f;
        angularVelocity = CustomMathsLibrary.Scale(angularVelocity, angularDamping);
    }

    private void CheckFallen()
    {
        // Checks if pin is upright or tilted
        // 1 = perfectly upright
        // 0 = 90 degrees sideways 
        // - 1 = 180 degrees sideways
        float uprightDot = CustomMathsLibrary.Dot(GetUpDir(), up);

        // Decided whether or not the pin has fallen so it can snap the rotation later
        if (!hasFallen && uprightDot < fallThreshold)
        {
            hasFallen = true;

            CustomMathsLibrary.Vector3 fallAxis = CustomMathsLibrary.CrossProduct(up, GetUpDir());
            if (CustomMathsLibrary.Magnitude(fallAxis) < 0.001f) fallAxis = new CustomMathsLibrary.Vector3(1,0,0);

            finalRotation = new CustomMathsLibrary.Quat(fallAxis, Mathf.PI / 2);
        }
    }

    // Integrates angular velocity to update pin rotation and correct position offset
    private void ApplyAngularRotation()
    {
        float angularSpeed = CustomMathsLibrary.Magnitude(angularVelocity);

        if (angularSpeed <= 0.000001f) return;

        CustomMathsLibrary.Vector3 axis = CustomMathsLibrary.Normalize(angularVelocity);
        float angle = angularSpeed * Time.deltaTime;
        CustomMathsLibrary.Quat deltaRot = new CustomMathsLibrary.Quat(axis, angle);

        // Finds the old bottom point before the rotation
        CustomMathsLibrary.Vector3 oldBottom = GetBottomPoint();

        // apply rotation
        currentRotation = deltaRot * currentRotation;

        // recompute bottom AFTER rotation
        CustomMathsLibrary.Vector3 newBottom = GetBottomPoint();

        // Finds the new pos by adding the current pos with the old bottom subtracted by the new bottom
        pos = CustomMathsLibrary.Add(pos, CustomMathsLibrary.Subtract(oldBottom, newBottom));
    }

    // Applies constraints to the pin to stop it from going outside of the lane
    private void ApplyConstraints(ref CustomMathsLibrary.Vector3 pos)
    {
        pos.x = CustomMathsLibrary.Clamp(pos.x, -WorldData.laneWidth + pinRadius, WorldData.laneWidth - pinRadius);

        pos.z = CustomMathsLibrary.Clamp(pos.z, 0 + pinRadius, WorldData.laneDepth - pinRadius);
    }

    // POTENTIAL FIX: Currently the rotation locks when fallen, could be changed so that the rotation on the x axis is always set to 90
    private void ConstrainRotation()
    {
        angularVelocity = CustomMathsLibrary.Vector3.zero;

        currentRotation = finalRotation;
    }

    // Stores correction data (collision penetration) to prevent overlaps
    public void StoreCorrectionDelta(CustomMathsLibrary.Vector3 delta)
    {
        hasStoredCorrection = true;
        storedCorrectionDelta = delta;
    }

    // Applies a correction that fixes physics overlaps (collision penetration)
    private void ApplyCollisionCorrection(ref CustomMathsLibrary.Vector3 pos)
    {
        hasStoredCorrection = false;
        pos = CustomMathsLibrary.Add(pos, storedCorrectionDelta);
    }

    // Apply the final transformations to the pin, including the stored rotation
    private void ApplyTransform(CustomMathsLibrary.Vector3 pos)
    {
        transform.position = pos;
        transform.rotation = currentRotation.ToUnityQuaternion();
    }

    // Draw debugs (for verification check)
    private void DrawDebugs()
    {
        CustomMathsLibrary.Vector3 upDirDebug = currentRotation.RotateVector(up);

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