using UnityEngine;

public class CollisionUtility : MonoBehaviour
{
    public static bool SphereCapsuleCollision(CustomMathsLibrary.Vector3 sphereCenter, float sphereRadius, CustomMathsLibrary.Vector3 capsuleA, CustomMathsLibrary.Vector3 capsuleB, float capsuleRadius, out CustomMathsLibrary.Vector3 collisionNormal, out float penetrationDepth, out CustomMathsLibrary.Vector3 hitPoint)
    {
        CustomMathsLibrary.Vector3 closestPoint = CustomMathsLibrary.ClosestPointOnSegment(capsuleA, capsuleB, sphereCenter);

        hitPoint = closestPoint;

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

    public static bool CapsuleCapsuleCollision(CustomMathsLibrary.Vector3 a1, CustomMathsLibrary.Vector3 a2, float radiusA, CustomMathsLibrary.Vector3 b1, CustomMathsLibrary.Vector3 b2, float radiusB, out CustomMathsLibrary.Vector3 collisionNormal, out float penetrationDepth, out CustomMathsLibrary.Vector3 hitPoint)
    {
        CustomMathsLibrary.Vector3 p1, p2;
        CustomMathsLibrary.ClosestPointsBetweenSegments(a1, a2, b1, b2, out p1, out p2);

        CustomMathsLibrary.Vector3 delta = CustomMathsLibrary.Subtract(p2, p1);
        float dist = CustomMathsLibrary.Magnitude(delta);
        float totalRadius = radiusA + radiusB;

        if (dist < totalRadius)
        {
            collisionNormal = CustomMathsLibrary.Normalize(delta);
            penetrationDepth = totalRadius - dist;
            hitPoint = CustomMathsLibrary.Scale(CustomMathsLibrary.Add(p1, p2), 0.5f);
            return true;
        }

        collisionNormal = CustomMathsLibrary.Vector3.zero;
        penetrationDepth = 0;
        hitPoint = CustomMathsLibrary.Vector3.zero;
        return false;
    }
}
