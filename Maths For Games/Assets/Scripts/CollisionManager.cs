using UnityEngine;
using System.Collections.Generic;

public class PinCollisionManager : MonoBehaviour
{
    [SerializeField] private List<BowlingPinController> pins;

    private void FixedUpdate()
    {
        HandlePinCollisions();
    }

    private void HandlePinCollisions()
    {
        for (int i = 0; i < pins.Count; i++)
        {
            for (int j = i + 1; j < pins.Count; j++)
            {
                var a = pins[i];
                var b = pins[j];

                if (!CollisionUtility.CapsuleCapsuleCollision(a.GetBottom(), a.GetTop(), a.GetPinRadius(), b.GetBottom(), b.GetTop(), b.GetPinRadius(), out var normal, out var penetration, out var hitPoint)) continue;

                ResolvePinPenetration(a, b, normal, penetration);
                ApplyPinImpulse(a, b, normal, hitPoint);
            }
        }
    }

    private void ResolvePinPenetration(BowlingPinController a, BowlingPinController b, CustomMathsLibrary.Vector3 normal, float penetration)
    {
        float totalMass = a.GetPinMass() + b.GetPinMass();

        penetration = Mathf.Max(penetration - 0.01f, 0f);

        float moveA = penetration * 0.6f;
        float moveB = penetration * 0.6f;

        CustomMathsLibrary.Vector3 correctionA = CustomMathsLibrary.Scale(normal, -moveA);
        CustomMathsLibrary.Vector3 correctionB = CustomMathsLibrary.Scale(normal, moveB);

        a.StoreCorrectionDelta(correctionA);
        b.StoreCorrectionDelta(correctionB);
    }

    private void ApplyPinImpulse(BowlingPinController a, BowlingPinController b, CustomMathsLibrary.Vector3 normal, CustomMathsLibrary.Vector3 hitPoint)
    {
        var velA = a.pinVelocity;
        var velB = b.pinVelocity;

        var relativeVel = CustomMathsLibrary.Subtract(velA, velB);

        float separatingVel = CustomMathsLibrary.Dot(relativeVel, normal);

        if (separatingVel > 0f) return;

        float restitution = 0.25f;

        float impulseScalar = -(1f + restitution) * separatingVel;
        impulseScalar /= (1f / a.GetPinMass()) + (1f / b.GetPinMass());

        var impulse = CustomMathsLibrary.Scale(normal, impulseScalar);

        a.ApplyCollisionImpulse(impulse, hitPoint);
        b.ApplyCollisionImpulse(CustomMathsLibrary.Scale(impulse, -1f), hitPoint);
    }
}