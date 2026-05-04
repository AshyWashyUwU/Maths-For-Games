using UnityEngine;

public class BowlingPinController : MonoBehaviour
{
    [Header("Pin Physical Variables")]
    [Range(0.25f, 1)] [SerializeField] private float pinRadius = 0.5f;
    [Range(0.5f, 2)]  [SerializeField] private float pinHeight = 2f;
    [Range(1, 5)]     [SerializeField] private float pinMass = 2f;

    public CustomMathsLibrary.Vector3 pinVelocity { get; private set; } = CustomMathsLibrary.Vector3.zero;
    private CustomMathsLibrary.Vector3 angularVelocity = CustomMathsLibrary.Vector3.zero;

    private CustomMathsLibrary.Vector3 up = new CustomMathsLibrary.Vector3(0, 1, 0);
    private CustomMathsLibrary.Vector3 bottomPoint = CustomMathsLibrary.Vector3.zero;

    private CustomMathsLibrary.Quat rotation = new CustomMathsLibrary.Quat(1, 0, 0, 0);

    private float inertia;

    private bool isGrounded;
    private bool hasFallen;

    private CustomMathsLibrary.Quat finalRotation;

    private void Start()
    {
        inertia = (1f / 12f) * pinMass * (3 * pinRadius * pinRadius + pinHeight * pinHeight);
    }

    public float GetPinRadius()
    {
        return pinRadius;
    }

    public float GetPinMass()
    {
        return pinMass;
    }

    public CustomMathsLibrary.Vector3 GetBottom()
    {
        return new CustomMathsLibrary.Vector3(transform.position.x, transform.position.y - (pinHeight * 0.5f), transform.position.z);
    }

    public CustomMathsLibrary.Vector3 GetTop()
    {
        return new CustomMathsLibrary.Vector3(transform.position.x, transform.position.y + (pinHeight * 0.5f) - pinRadius, transform.position.z);
    }

    private void FixedUpdate()
    {
        float dt = Time.deltaTime;

        CustomMathsLibrary.Vector3 pos = transform.position;

        UpdatePosition(ref pos);

        ApplyGravityTorque(pos);

        ApplyDamping();

        if (!hasFallen) { CheckFallen(); ApplyAngularRotation(); }

        CorrectPosition(ref pos);

        ApplyConstraints(ref pos);

        CheckSleep();

        if (hasFallen) { ApplyRotation(); }

        ApplyTransform(pos);

        DrawDebugs();
    }

    public void ApplyImpulse(CustomMathsLibrary.Vector3 impulse, CustomMathsLibrary.Vector3 hitPoint)
    {
        float impulseBoost = 1.5f;
        impulse = CustomMathsLibrary.Scale(impulse, impulseBoost);

        pinVelocity = CustomMathsLibrary.Add(pinVelocity, CustomMathsLibrary.Scale(impulse, 1f / pinMass));

        CustomMathsLibrary.Vector3 r = CustomMathsLibrary.Subtract(hitPoint, transform.position);
        CustomMathsLibrary.Vector3 torque = CustomMathsLibrary.CrossProduct(r, impulse);

        CustomMathsLibrary.Vector3 angularAccel = CustomMathsLibrary.Scale(torque, 1f / inertia);

        angularVelocity = CustomMathsLibrary.Add(angularVelocity, angularAccel);
    }

    private CustomMathsLibrary.Vector3 GetUpDir()
    {
        return rotation.RotateVector(up);
    }

    private CustomMathsLibrary.Vector3 GetBottomPoint(CustomMathsLibrary.Vector3 pos)
    {
        return CustomMathsLibrary.Subtract(pos, CustomMathsLibrary.Scale(GetUpDir(), (pinHeight * 0.5f) - pinRadius));
    }

    private void UpdatePosition(ref CustomMathsLibrary.Vector3 pos)
    {
        pos = CustomMathsLibrary.Add(pos, CustomMathsLibrary.Scale(pinVelocity, Time.deltaTime));
    }

    private void ApplyGravityTorque(CustomMathsLibrary.Vector3 pos)
    {
        bottomPoint = GetBottomPoint(pos);

        if (bottomPoint.y <= WorldData.worldGroundPos)
        {
            float penetration = WorldData.worldGroundPos - bottomPoint.y;
            pos.y += penetration;
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }

        if (isGrounded && !hasFallen)
        {
            CustomMathsLibrary.Vector3 gravityForce = new CustomMathsLibrary.Vector3(0, pinMass * -9.81f, 0);

            CustomMathsLibrary.Vector3 r = CustomMathsLibrary.Subtract(pos, bottomPoint);

            CustomMathsLibrary.Vector3 torque = CustomMathsLibrary.CrossProduct(r, gravityForce);

            CustomMathsLibrary.Vector3 angularAccel = CustomMathsLibrary.Scale(torque, 1f / inertia);

            angularVelocity = CustomMathsLibrary.Add(angularVelocity, CustomMathsLibrary.Scale(angularAccel, Time.deltaTime));
        }
    }

    private void ApplyDamping()
    {
        float linearDamping = isGrounded ? 0.9f : 0.99f;
        pinVelocity = CustomMathsLibrary.Scale(pinVelocity, linearDamping);

        float angularDamping = isGrounded ? 0.94f : 0.98f;
        angularVelocity = CustomMathsLibrary.Scale(angularVelocity, angularDamping);
    }

    private void CheckFallen()
    {
        if (hasFallen) return;

        float uprightDot = CustomMathsLibrary.Dot(GetUpDir(), up);

        if (!hasFallen && uprightDot < 0.25f)
        {
            hasFallen = true;

            CustomMathsLibrary.Vector3 fallAxis = CustomMathsLibrary.CrossProduct(up, GetUpDir());
            if (CustomMathsLibrary.Magnitude(fallAxis) < 0.001f) fallAxis = new CustomMathsLibrary.Vector3(1,0,0);

            finalRotation = new CustomMathsLibrary.Quat(fallAxis, Mathf.PI / 2);
        }
    }

    private void ApplyAngularRotation()
    {
        float angularSpeed = CustomMathsLibrary.Magnitude(angularVelocity);

        if (angularSpeed > 0.00001f)
        {
            CustomMathsLibrary.Vector3 axis = CustomMathsLibrary.Normalize(angularVelocity);
            float angle = angularSpeed * Time.deltaTime;

            CustomMathsLibrary.Quat deltaRot = new CustomMathsLibrary.Quat(axis, angle);
            rotation = deltaRot * rotation;
        }
    }

    private void CorrectPosition(ref CustomMathsLibrary.Vector3 pos)
    {
        CustomMathsLibrary.Vector3 newUp = rotation.RotateVector(up);

        pos = CustomMathsLibrary.Add(bottomPoint, CustomMathsLibrary.Scale(newUp, (pinHeight * 0.5f) - pinRadius));
    }

    private void ApplyConstraints(ref CustomMathsLibrary.Vector3 pos)
    {
        pos.x = CustomMathsLibrary.Clamp(pos.x, -WorldData.laneWidth + pinRadius, WorldData.laneWidth - pinRadius);

        pos.z = CustomMathsLibrary.Clamp(pos.z, 0 + pinRadius, WorldData.laneDepth - pinRadius);

        pos.y = Mathf.Max(pos.y, WorldData.worldGroundPos + pinRadius);
    }

    private void CheckSleep()
    {
        float sleepThreshold = 0.02f;

        if (CustomMathsLibrary.Magnitude(pinVelocity) < sleepThreshold && CustomMathsLibrary.Magnitude(angularVelocity) < sleepThreshold)
        {
            pinVelocity = CustomMathsLibrary.Vector3.zero;
            angularVelocity = CustomMathsLibrary.Vector3.zero;
        }
    }

    private void ApplyRotation()
    {
        angularVelocity = CustomMathsLibrary.Vector3.zero;

        rotation = finalRotation;
    }

    private void ApplyTransform(CustomMathsLibrary.Vector3 pos)
    {
        transform.position = pos;
        transform.rotation = rotation.ToUnityQuaternion();
    }

    private void DrawDebugs()
    {
        CustomMathsLibrary.Vector3 upDirDebug = rotation.RotateVector(up);

        Debug.DrawLine(transform.position, transform.position + (Vector3)upDirDebug, Color.green);

        Debug.DrawLine(transform.position, transform.position + (Vector3)pinVelocity, Color.blue);

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