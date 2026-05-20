using UnityEngine;
using System.Collections.Generic;

public class PinCollisionManager : MonoBehaviour
{
    private static PinCollisionManager Instance;
    public static PinCollisionManager instance => Instance;

    [SerializeField] private List<BowlingPinController> pins;

    private void Awake() { if (Instance == null) Instance = this; }

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
                BowlingPinController pinA = pins[i];
                BowlingPinController pinB = pins[j];

                if (!CollisionUtility.CapsuleCapsuleCollision(pinA.GetBottom(), pinA.GetTop(), pinA.GetPinRadius(), pinB.GetBottom(), pinB.GetTop(), pinB.GetPinRadius(), out CustomMathsLibrary.Vector3 normal, out float penetration, out CustomMathsLibrary.Vector3 hitPoint)) continue;

                ResolvePinPenetration(pinA, pinB, normal, penetration);
                ApplyPinImpulse(pinA, pinB, normal, hitPoint);
            }
        }
    }

    private void ResolvePinPenetration(BowlingPinController pinA, BowlingPinController pinB, CustomMathsLibrary.Vector3 normal, float penetration)
    {
        float totalMass = pinA.GetPinMass() + pinB.GetPinMass();

        penetration = Mathf.Max(penetration - 0.01f, 0f);

        float moveA = penetration * 0.6f;
        float moveB = penetration * 0.6f;

        CustomMathsLibrary.Vector3 correctionA = CustomMathsLibrary.Scale(normal, -moveA);
        CustomMathsLibrary.Vector3 correctionB = CustomMathsLibrary.Scale(normal, moveB);

        pinA.StoreCorrectionDelta(correctionA);
        pinB.StoreCorrectionDelta(correctionB);
    }

    private void ApplyPinImpulse(BowlingPinController pinA, BowlingPinController pinB, CustomMathsLibrary.Vector3 normal, CustomMathsLibrary.Vector3 hitPoint)
    {
        CustomMathsLibrary.Vector3 velA = pinA.pinVelocity;
        CustomMathsLibrary.Vector3 velB = pinB.pinVelocity;

        CustomMathsLibrary.Vector3 relativeVel = CustomMathsLibrary.Subtract(velA, velB);

        float separatingVel = CustomMathsLibrary.Dot(relativeVel, normal);

        if (separatingVel <= 0f) return;

        float restitution = 0.7f;

        float impulseScalar = -(1f + restitution) * separatingVel;
        impulseScalar /= (1f / pinA.GetPinMass()) + (1f / pinB.GetPinMass());

        CustomMathsLibrary.Vector3 impulse = CustomMathsLibrary.Scale(normal, impulseScalar);

        pinA.ApplyCollisionImpulse(impulse, hitPoint);
        pinB.ApplyCollisionImpulse(CustomMathsLibrary.Scale(impulse, -1f), hitPoint);
    }

    public void ResetPins()
    {
        foreach(BowlingPinController pin in pins)
        {
            pin.ResetPin();
        }
    }

    public List<BowlingPinController> GetPins() { return pins; }
}