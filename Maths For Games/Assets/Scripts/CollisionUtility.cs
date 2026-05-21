using UnityEngine;

public class CollisionUtility : MonoBehaviour
{
    public static bool SphereCapsuleCollision(CustomMathsLibrary.Vector3 sphereCenter, float sphereRadius, CustomMathsLibrary.Vector3 capsuleA, CustomMathsLibrary.Vector3 capsuleB, float capsuleRadius, out CustomMathsLibrary.Vector3 collisionNormal, out float penetrationDepth, out CustomMathsLibrary.Vector3 hitPoint)
    {
        // Find the closest point on the capsule line segment to the sphere center
        // Capsules = line segment with a radius
        CustomMathsLibrary.Vector3 closestPoint = CustomMathsLibrary.ClosestPointOnSegment(capsuleA, capsuleB, sphereCenter);

        // Set the hit point
        hitPoint = closestPoint;

        // Caculate the vector between the sphere and capsule
        CustomMathsLibrary.Vector3 delta = CustomMathsLibrary.Subtract(sphereCenter, closestPoint);

        // Caculate distance and total radii
        float distance = CustomMathsLibrary.Magnitude(delta); // Actual distance between the sphere collider and capsule line
        float totalRadius = sphereRadius + capsuleRadius; // How far apart the centers could be without touching

        // Collision check
        if (distance < totalRadius)
        {
            collisionNormal = CustomMathsLibrary.Normalize(delta); // Direction to push objects apart
            penetrationDepth = totalRadius - distance; // How much the sphere is inside the capsule
            return true;
        }

        // No collision fallback
        collisionNormal = CustomMathsLibrary.Vector3.zero;
        penetrationDepth = 0;
        return false;
    }

    public static bool CapsuleCapsuleCollision(CustomMathsLibrary.Vector3 a1, CustomMathsLibrary.Vector3 a2, float radiusA, CustomMathsLibrary.Vector3 b1, CustomMathsLibrary.Vector3 b2, float radiusB, out CustomMathsLibrary.Vector3 collisionNormal, out float penetrationDepth, out CustomMathsLibrary.Vector3 hitPoint)
    {
        // p1 = closest point to capsule B
        // p2 = closest point to capsule A
        CustomMathsLibrary.Vector3 p1, p2;

        // Find the closest point between the two capsule segments
        CustomMathsLibrary.ClosestPointsBetweenSegments(a1, a2, b1, b2, out p1, out p2);

        // Caculate the vector between the closest points (delta points from capsule A to B)
        CustomMathsLibrary.Vector3 delta = CustomMathsLibrary.Subtract(p2, p1);

        float distance = CustomMathsLibrary.Magnitude(delta); // Distance between the closest points on the segments
        float totalRadius = radiusA + radiusB; // Combined radii of the two capsules

        // Collision check
        if (distance < totalRadius)
        {
            collisionNormal = CustomMathsLibrary.Normalize(delta); // Direction to push objects apart
            penetrationDepth = totalRadius - distance; // How much the capsules overlap
            hitPoint = CustomMathsLibrary.Scale(CustomMathsLibrary.Add(p1, p2), 0.5f); // Midpoint between the closest points where the collison is "centered"
            return true;
        }

        // No collision fallback
        collisionNormal = CustomMathsLibrary.Vector3.zero;
        penetrationDepth = 0;
        hitPoint = CustomMathsLibrary.Vector3.zero;
        return false;
    }
}
