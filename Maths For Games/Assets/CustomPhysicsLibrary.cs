using UnityEngine;

public class CustomPhysicsLibrary : MonoBehaviour
{
    public static float GRAVITY_FORCE = -9.81f;

    public static float GROUND_FRICTION = 0.955f;

    public static float AIR_DENSITY = 0.3f;

    public static float CaculateObjectGravityForce(float objectMass)
    {
        return objectMass * GRAVITY_FORCE;
    }

    public static float CaculateObjectDragForce(float objectSpeed, float objectArea)
    {
        return 0.5f * AIR_DENSITY * objectSpeed * objectSpeed * GROUND_FRICTION * objectArea;
    }
}
