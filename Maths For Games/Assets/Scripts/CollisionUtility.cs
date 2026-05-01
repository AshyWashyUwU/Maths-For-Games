using UnityEngine;

public class CollisionUtility : MonoBehaviour
{
    public static bool SphereCapsuleCollision(CustomMathsLibrary.Vector3 sphereCenter, float sphereRadius, CustomMathsLibrary.Vector3 capsuleA, CustomMathsLibrary.Vector3 capsuleB, float capsuleRadius, out CustomMathsLibrary.Vector3 collisionNormal, out float penetrationDepth)
    {
        CustomMathsLibrary.Vector3 closestPoint = CustomMathsLibrary.ClosestPointOnSegment(capsuleA, capsuleB, sphereCenter);

        CustomMathsLibrary.Vector3 delta = CustomMathsLibrary.Subtract(sphereCenter, closestPoint);

        float dist = CustomMathsLibrary.Magnitude(delta);
        float totalRadius = sphereRadius + capsuleRadius;

        if (dist < totalRadius)
        {
            collisionNormal = CustomMathsLibrary.Normalize(delta);
            penetrationDepth = totalRadius - dist;
            return true;
        }

        collisionNormal = CustomMathsLibrary.Vector3.zero;
        penetrationDepth = 0;
        return false;
    }
}
