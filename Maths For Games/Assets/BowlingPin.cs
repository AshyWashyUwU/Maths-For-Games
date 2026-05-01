using UnityEngine;

public class BowlingPin : MonoBehaviour
{
    [SerializeField] private float pinRadius = 1f;
    [SerializeField] private float pinHeight = 2f;

    private bool isGrounded;

    private CustomMathsLibrary.Vector3 pinVelocity = CustomMathsLibrary.Vector3.zero;
    private float verticalVelocity;

    public bool ApplyCollisionWithBall(CustomMathsLibrary.Vector3 ballPos, float ballRadius, CustomMathsLibrary.Vector3 ballDir, float ballForce)
    {
        CustomMathsLibrary.Vector3 A = transform.position;
        CustomMathsLibrary.Vector3 B = CustomMathsLibrary.Add(A, new CustomMathsLibrary.Vector3(0, pinHeight, 0));

        CustomMathsLibrary.Vector3 closestPoint = CustomMathsLibrary.ClosestPointOnSegment(A, B, ballPos);
        CustomMathsLibrary.Vector3 difference = CustomMathsLibrary.Subtract(ballPos, closestPoint);

        Debug.DrawLine(A, B, Color.red);
        Debug.DrawLine(ballPos, closestPoint, Color.green);

        float distance = CustomMathsLibrary.Magnitude(difference);
        float combinedRadius = ballRadius + pinRadius;

        if (distance <= combinedRadius)
        {
            if (distance < 0.0001f)
            {
                difference = new CustomMathsLibrary.Vector3(0, 1, 0);
                distance = 0.0001f;
            }

            CustomMathsLibrary.Vector3 normal = CustomMathsLibrary.Normalize(difference);

            float penetration = combinedRadius - distance;

            transform.position = CustomMathsLibrary.Add(transform.position, CustomMathsLibrary.Scale(normal, penetration + 0.01f));

            float velIntoNormal = CustomMathsLibrary.Dot(pinVelocity, normal);

            if (velIntoNormal < 0)
            {
                pinVelocity = CustomMathsLibrary.Subtract(normal, CustomMathsLibrary.Scale(normal, velIntoNormal));
            }

            CustomMathsLibrary.Vector3 impulse = CustomMathsLibrary.Add(CustomMathsLibrary.Scale(normal, 1f), CustomMathsLibrary.Scale(ballDir, 0.5f));

            pinVelocity = CustomMathsLibrary.Add(pinVelocity, CustomMathsLibrary.Scale(impulse, ballForce * 1.5f));

            return true;
        }

        return false;
    }

    private void Update()
    {
        CustomMathsLibrary.Vector3 pos = transform.position;

        if (!isGrounded)
        {
            verticalVelocity += CustomPhysicsLibrary.CaculateObjectGravityForce(1f) * Time.deltaTime;
        }

        pos = CustomMathsLibrary.Add(pos, CustomMathsLibrary.Scale(pinVelocity, Time.deltaTime));
        pos.y += verticalVelocity * Time.deltaTime;

        float bottomPoint = pos.y - pinRadius;

        if (bottomPoint <= WorldData.worldGroundPos)
        {
            pos.y = WorldData.worldGroundPos + pinRadius;
            verticalVelocity = 0f;
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }

        pinVelocity = CustomMathsLibrary.Scale(pinVelocity, 0.995f);
        transform.position = pos;
    }
}
