using UnityEngine;

public class BowlingPinController : MonoBehaviour
{
    public float pinRadius = 0.15f;
    public float pinHeight = 1.2f;
    public float pinMass = 2f;

    private CustomMathsLibrary.Vector3 pinVelocity = CustomMathsLibrary.Vector3.zero;
    private float verticalVelocity;
    private bool isGrounded, hasFallen;

    private CustomMathsLibrary.Vector3 up = new CustomMathsLibrary.Vector3(0, 1, 0);

    private CustomMathsLibrary.Vector3 angularVelocity = CustomMathsLibrary.Vector3.zero;
    private CustomMathsLibrary.Quat rotation = new CustomMathsLibrary.Quat(1, 0, 0, 0);

    public CustomMathsLibrary.Vector3 GetBottom()
    {
        return new CustomMathsLibrary.Vector3(transform.position.x, transform.position.y - (pinHeight * 0.5f), transform.position.z);
    }

    public CustomMathsLibrary.Vector3 GetTop()
    {
        return new CustomMathsLibrary.Vector3(transform.position.x, transform.position.y + (pinHeight * 0.5f), transform.position.z);
    }

    public void ApplyImpulse(CustomMathsLibrary.Vector3 impulse, CustomMathsLibrary.Vector3 hitPoint)
    {
        float impulseMag = CustomMathsLibrary.Magnitude(impulse);

        float minImpulse = 1.5f;

        if (impulseMag < minImpulse)
        {
            CustomMathsLibrary.Vector3 dir = CustomMathsLibrary.Normalize(impulse);
            impulse = CustomMathsLibrary.Scale(dir, minImpulse);
        }

        pinVelocity = CustomMathsLibrary.Add(pinVelocity, CustomMathsLibrary.Scale(impulse, 1f / pinMass));

        float minVelocity = 1.2f;

        if (CustomMathsLibrary.Magnitude(pinVelocity) < minVelocity)
        {
            pinVelocity = CustomMathsLibrary.Scale(CustomMathsLibrary.Normalize(pinVelocity), minVelocity);
        }

        CustomMathsLibrary.Vector3 pinCenter = transform.position;

        CustomMathsLibrary.Vector3 r = CustomMathsLibrary.Subtract(hitPoint, pinCenter);

        CustomMathsLibrary.Vector3 torque = CustomMathsLibrary.CrossProduct(r, impulse);

        float intertiaForce = pinMass * 0.05f;
        float torqueBoost = 2f;

        CustomMathsLibrary.Vector3 angularAccel = CustomMathsLibrary.Scale(torque, torqueBoost / intertiaForce);

        angularVelocity = CustomMathsLibrary.Add(angularVelocity, angularAccel);

        float minSpin = 2f;

        if (CustomMathsLibrary.Magnitude(angularVelocity) < minSpin)
        {
            angularVelocity = CustomMathsLibrary.Scale(CustomMathsLibrary.Normalize(angularVelocity), minSpin);
        }
    }

    private void FixedUpdate()
    {
        CustomMathsLibrary.Vector3 pos = transform.position;

        pos = CustomMathsLibrary.Add(pos, CustomMathsLibrary.Scale(pinVelocity, Time.deltaTime));

        if (!isGrounded)
        {
            verticalVelocity += CustomPhysicsLibrary.CaculateObjectGravityForce(pinMass) * Time.deltaTime;
            pos.y += verticalVelocity * Time.deltaTime;

            float bottom = pos.y - (pinHeight * 0.5f);

            if (bottom <= WorldData.worldGroundPos)
            {
                pos.y = WorldData.worldGroundPos + (pinHeight * 0.5f);
                verticalVelocity = 0;
                isGrounded = true;
            }
        }
        else
        {
            verticalVelocity = 0f;
        }

        float angularDamping = isGrounded ? 0.96f : 0.98f;

        angularVelocity = CustomMathsLibrary.Scale(angularVelocity, angularDamping);

        if (hasFallen)
        {
            angularVelocity = CustomMathsLibrary.Scale(angularVelocity, 0.7f);

            if (CustomMathsLibrary.Magnitude(angularVelocity) < 0.05f)
            {
                angularVelocity = CustomMathsLibrary.Vector3.zero;
            }
        }

        transform.position = pos;

        CustomMathsLibrary.Vector3 upDir = rotation.RotateVector(up);
        CustomMathsLibrary.Vector3 tiltAxis = CustomMathsLibrary.CrossProduct(up, upDir);

        // 1 == upright, 0 == sideways, < 0 upside down
        float uprightDot = CustomMathsLibrary.Dot(upDir, up);

        if (!hasFallen && uprightDot < 0.2f)
        {
            hasFallen = true;
        }

        float tiltAmount = CustomMathsLibrary.Magnitude(tiltAxis);

        if (!hasFallen && tiltAmount > 0.01f)
        {
            float tipStrength = 4f;

            float angSpeed = CustomMathsLibrary.Magnitude(angularVelocity);
            if (angSpeed < 1f)
            {
                tipStrength *= 3f;
            }

            angularVelocity = CustomMathsLibrary.Add(angularVelocity, CustomMathsLibrary.Scale(tiltAxis, tipStrength * Time.deltaTime));
        }

        float angularSpeed = CustomMathsLibrary.Magnitude(angularVelocity);

        if (angularSpeed > 0.0001f)
        {
            CustomMathsLibrary.Vector3 axis = CustomMathsLibrary.Normalize(angularVelocity);

            float angle = angularSpeed * Time.deltaTime;

            CustomMathsLibrary.Quat deltaRot = new CustomMathsLibrary.Quat(axis, angle);

            rotation = deltaRot * rotation;
        }

        transform.rotation = rotation.ToUnityQuaternion();

        // Stops micro movement

        if (CustomMathsLibrary.Magnitude(pinVelocity) < 0.01f) pinVelocity = CustomMathsLibrary.Vector3.zero;

        if (CustomMathsLibrary.Magnitude(angularVelocity) < 0.01f) angularVelocity = CustomMathsLibrary.Vector3.zero;

        upDir = rotation.RotateVector(up);

        uprightDot = CustomMathsLibrary.Dot(upDir, up);

        if (hasFallen && CustomMathsLibrary.Magnitude(pinVelocity) < 0.05f && CustomMathsLibrary.Magnitude(angularVelocity) < 0.1f)
        {
            pinVelocity = CustomMathsLibrary.Vector3.zero;
            angularVelocity = CustomMathsLibrary.Vector3.zero;
        }
    }

    public CustomMathsLibrary.Vector3 GetVelocity()
    {
        return pinVelocity;
    }
}
