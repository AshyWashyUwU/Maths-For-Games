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

    public CustomMathsLibrary.Vector3 GetBottom()
    {
        return new CustomMathsLibrary.Vector3(transform.position.x, transform.position.y - (pinHeight * 0.5f), transform.position.z);
    }

    public CustomMathsLibrary.Vector3 GetTop()
    {
        return new CustomMathsLibrary.Vector3(transform.position.x, transform.position.y + (pinHeight * 0.5f), transform.position.z);
    }

    public void ApplyImpulse(CustomMathsLibrary.Vector3 impulse)
    {
        pinVelocity = CustomMathsLibrary.Add(pinVelocity, CustomMathsLibrary.Scale(impulse, 1f / pinMass));
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
    }
}
