using UnityEngine;

public class BowlingPin : MonoBehaviour
{
    [Header("Pin Shape")]
    [SerializeField] private float pinRadius = 0.2f;
    [SerializeField] private float pinHeight = 1.5f;

    [Header("Physics")]
    [SerializeField] private float pinMass = 1f;
    [SerializeField] private float damping = 0.98f;
    [SerializeField] private float collisionCooldownTime = 0.02f;

    private CustomMathsLibrary.Vector3 pinVelocity = CustomMathsLibrary.Vector3.zero;
    private float verticalVelocity;
    private bool isGrounded;

    private float collisionCooldown;

    public bool ApplyCollisionWithBall(
        CustomMathsLibrary.Vector3 ballPos,
        float ballRadius,
        CustomMathsLibrary.Vector3 ballDir,
        float ballForce,
        out CustomMathsLibrary.Vector3 pushOut)
    {
        pushOut = CustomMathsLibrary.Vector3.zero;

        CustomMathsLibrary.Vector3 A =
            CustomMathsLibrary.Add(transform.position, new CustomMathsLibrary.Vector3(0, pinRadius, 0));

        CustomMathsLibrary.Vector3 B =
            CustomMathsLibrary.Add(transform.position, new CustomMathsLibrary.Vector3(0, pinHeight - pinRadius, 0));

        CustomMathsLibrary.Vector3 closest =
            CustomMathsLibrary.ClosestPointOnSegment(A, B, ballPos);

        CustomMathsLibrary.Vector3 diff =
            CustomMathsLibrary.Subtract(ballPos, closest);

        float dist = CustomMathsLibrary.Magnitude(diff);
        float combined = ballRadius + pinRadius;

        if (dist >= combined)
            return false;

        if (dist < 0.0001f)
        {
            diff = new CustomMathsLibrary.Vector3(0, 1, 0);
            dist = 0.0001f;
        }

        CustomMathsLibrary.Vector3 normal =
            CustomMathsLibrary.Normalize(diff);

        float penetration = (combined - dist) + 0.01f;

        pushOut = CustomMathsLibrary.Scale(normal, penetration);

        transform.position =
            CustomMathsLibrary.Add(transform.position, pushOut);

        float velIntoNormal = CustomMathsLibrary.Dot(pinVelocity, normal);

        if (velIntoNormal < 0f)
        {
            pinVelocity = CustomMathsLibrary.Subtract(
                pinVelocity,
                CustomMathsLibrary.Scale(normal, velIntoNormal)
            );
        }

        CustomMathsLibrary.Vector3 impulse =
            CustomMathsLibrary.Scale(normal, ballForce / pinMass);

        pinVelocity = CustomMathsLibrary.Add(pinVelocity, impulse);

        pinVelocity = CustomMathsLibrary.Add(
            pinVelocity,
            CustomMathsLibrary.Scale(ballDir, ballForce * 0.3f)
        );

        collisionCooldown = collisionCooldownTime;

        return true;
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        if (collisionCooldown > 0f)
            collisionCooldown -= dt;

        CustomMathsLibrary.Vector3 pos = transform.position;

        if (!isGrounded)
        {
            verticalVelocity +=
                CustomPhysicsLibrary.CaculateObjectGravityForce(pinMass) * dt;
        }

        pos = CustomMathsLibrary.Add(
            pos,
            CustomMathsLibrary.Scale(pinVelocity, dt)
        );

        pos.y += verticalVelocity * dt;

        float bottom = pos.y - pinRadius;

        if (bottom <= WorldData.worldGroundPos)
        {
            pos.y = WorldData.worldGroundPos + pinRadius;
            verticalVelocity = 0f;
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }

        pinVelocity = CustomMathsLibrary.Scale(pinVelocity, damping);

        transform.position = pos;
    }
}