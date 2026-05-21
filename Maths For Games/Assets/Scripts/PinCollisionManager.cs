using UnityEngine;
using System.Collections.Generic;

public class PinCollisionManager : MonoBehaviour
{
    private static PinCollisionManager Instance;
    public static PinCollisionManager instance => Instance;

    [SerializeField] private List<BowlingPinController> pins; // All of the pins in the scene

    private void Awake() { if (Instance == null) Instance = this; }

    private void FixedUpdate()
    {
        HandlePinCollisions();
    }

    // Handles pin collisions every frame
    private void HandlePinCollisions()
    {
        for (int i = 0; i < pins.Count; i++)
        {
            // Checks every unique pair of pins within two for loops (the +1 at the end prevents self collision)
            for (int j = i + 1; j < pins.Count; j++)
            {
                BowlingPinController pinA = pins[i];
                BowlingPinController pinB = pins[j];

                // Each bowling pin is treated as a capsule
                // Line segment = pin body
                // Radius = thickness of the pin
                // CapsuleCapsuleCollision esentially checks if the 3D capsules overlap, if they don't, the pair is skipped
                if (!CollisionUtility.CapsuleCapsuleCollision(pinA.GetBottomPoint(), pinA.GetTopPoint(), pinA.GetPinRadius(), pinB.GetBottomPoint(), pinB.GetTopPoint(), pinB.GetPinRadius(), out CustomMathsLibrary.Vector3 normal, out float penetration, out CustomMathsLibrary.Vector3 hitPoint)) continue;

                ApplyPinPenetration(pinA, pinB, normal, penetration);
                ApplyPinImpulse(pinA, pinB, normal, hitPoint);
            }
        }
    }

    // Fixes the overlap (position correction)
    private void ApplyPinPenetration(BowlingPinController pinA, BowlingPinController pinB, CustomMathsLibrary.Vector3 normal, float penetration)
    {
        penetration = Mathf.Max(penetration - 0.01f, 0f); // Overlap bias to prevent jittering, Mathf.Max esentially always prints the highest value

        // Split correction between both pins, aims to apart both pins equally (roughly, not perfect, POTENTIAL FIX)
        float moveA = penetration * 0.6f;
        float moveB = penetration * 0.6f;

        // Push pin A backwards along the collision normal, push pin B forwards along the collision normal so they seperate
        CustomMathsLibrary.Vector3 correctionA = CustomMathsLibrary.Scale(normal, -moveA);
        CustomMathsLibrary.Vector3 correctionB = CustomMathsLibrary.Scale(normal, moveB);

        // Store the correction of both pin A and B so that they can be applied later in FixedUpdate() after collision resolves
        pinA.StoreCorrectionDelta(correctionA);
        pinB.StoreCorrectionDelta(correctionB);
    }

    // Apply pin bounce / force transfer
    private void ApplyPinImpulse(BowlingPinController pinA, BowlingPinController pinB, CustomMathsLibrary.Vector3 normal, CustomMathsLibrary.Vector3 hitPoint)
    {
        // Get velocities of pins
        CustomMathsLibrary.Vector3 velA = pinA.GetPinVelocity();
        CustomMathsLibrary.Vector3 velB = pinB.GetPinVelocity();

        // Caculate the relative velocity (basically how fast pin A is moving to pin B)
        CustomMathsLibrary.Vector3 relativeVel = CustomMathsLibrary.Subtract(velA, velB);

        // Project onto the collision normal using Dot product
        // Figures out if the pins are moving toward eachother or seperating
        float separatingVel = CustomMathsLibrary.Dot(relativeVel, normal);

        if (separatingVel <= 0f) return; // If they are not seperating, don't apply impulse

        // Artifical restitution is applied (basically bounciness)
        float restitution = 0.7f;

        // Impulse mangnitude to flip the velocity direction (increased by restitution)
        float impulseScalar = -(1f + restitution) * separatingVel;
        // Scale by mass (heavier pins resist motion)
        impulseScalar /= (1f / pinA.GetPinMass()) + (1f / pinB.GetPinMass());

        // Construct impulse by combining the normal with the impulseScalar
        CustomMathsLibrary.Vector3 impulse = CustomMathsLibrary.Scale(normal, impulseScalar);

        // Apply the impulse to both pins, pushing one away whilst the other recieves an equal opposite force
        pinA.ApplyCollisionImpulse(impulse, hitPoint);
        pinB.ApplyCollisionImpulse(CustomMathsLibrary.Scale(impulse, -1f), hitPoint);
    }

    // Reset all pins within the list (used by UI)
    public void ResetPins()
    {
        foreach(BowlingPinController pin in pins)
        {
            pin.ResetPin();
        }
    }

    public List<BowlingPinController> GetPins() { return pins; } // Pin list getter
}