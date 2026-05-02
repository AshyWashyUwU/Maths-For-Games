using UnityEngine;

public class BowlingPinController : MonoBehaviour
{
    public float pinRadius = 0.15f;
    public float pinHeight = 1.2f;
    public float pinMass = 2f;

    private CustomMathsLibrary.Vector3 pinVelocity = CustomMathsLibrary.Vector3.zero;
    private float verticalVelocity;
    private bool isGrounded;

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
        pinVelocity = CustomMathsLibrary.Add(pinVelocity, CustomMathsLibrary.Scale(impulse, 1f / pinMass));

        CustomMathsLibrary.Vector3 pinCenter = transform.position;

        CustomMathsLibrary.Vector3 r = CustomMathsLibrary.Subtract(hitPoint, pinCenter);

        CustomMathsLibrary.Vector3 torque = CustomMathsLibrary.CrossProduct(r, impulse);

        float intertiaForce = pinMass * 0.1f;

        CustomMathsLibrary.Vector3 angularAccel = CustomMathsLibrary.Scale(torque, 1f / intertiaForce);

        angularVelocity = CustomMathsLibrary.Add(angularVelocity, angularAccel);
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

        pinVelocity = CustomMathsLibrary.Scale(pinVelocity, 0.98f);

        transform.position = pos;

        float angularSpeed = CustomMathsLibrary.Magnitude(angularVelocity);

        if (angularSpeed > 0.0001f)
        {
            CustomMathsLibrary.Vector3 axis = CustomMathsLibrary.Normalize(angularVelocity);

            float angle = angularSpeed * Time.deltaTime;

            CustomMathsLibrary.Quat deltaRot = new CustomMathsLibrary.Quat(axis, angle);

            rotation = deltaRot * rotation;
        }

        angularVelocity = CustomMathsLibrary.Scale(angularVelocity, 0.98f);

        transform.rotation = rotation.ToUnityQuaternion();
    }
}
