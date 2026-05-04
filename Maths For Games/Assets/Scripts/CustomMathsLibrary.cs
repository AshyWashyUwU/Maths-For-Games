using UnityEngine;

public static class CustomMathsLibrary
{
    // ------------------------------------ CUSTOM VECTOR2 CLASS ------------------------------------ // 

    public class Vector2
    {
        public float x;
        public float y;

        // Constructor
        public Vector2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        // Converts UnityEngine.Vector2 -> custom Vector2 (for debugging)
        public static implicit operator Vector2(UnityEngine.Vector2 v)
        {
            return new Vector2(v.x, v.y);
        }

        // Converts custom Vector2 -> custom UnityEngine.Vector2 (for debugging)
        public static implicit operator UnityEngine.Vector2(Vector2 v)
        {
            return new UnityEngine.Vector2(v.x, v.y);
        }

        // Returns standard Vector2 zero by typing CustomMathsLibrary.Vector2.zero
        public static Vector2 zero
        {
            get { return new Vector2(0f, 0f); }
        }

        // String override (for debugging)
        public override string ToString()
        {
            return "(" + x + ", " + y + ")";
        }
    }

    // ------------------------------------ CUSTOM VECTOR3 CLASS ------------------------------------ // 

    public class Vector3
    {
        public float x;
        public float y;
        public float z;

        // Constructor
        public Vector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        // Converts UnityEngine.Vector3 -> custom Vector3 (for debugging)
        public static implicit operator Vector3(UnityEngine.Vector3 v)
        {
            return new Vector3(v.x, v.y, v.z);
        }

        // Converts custom Vector3 -> custom UnityEngine.Vector3 (for debugging)
        public static implicit operator UnityEngine.Vector3(Vector3 v)
        {
            return new UnityEngine.Vector3(v.x, v.y, v.z);
        }

        // Returns standard Vector3 zero by typing CustomMathsLibrary.Vector3.zero
        public static Vector3 zero
        {
            get { return new Vector3(0f, 0f, 0f); }
        }

        // String override (for debugging)
        public override string ToString()
        {
            return "(" + x + ", " + y + ", " + z + ")";
        }
    }

    // ------------------------------------ CUSTOM VECTOR4 CLASS ------------------------------------ // 

    public class Vector4
    {
        public float x;
        public float y;
        public float z;
        public float w;

        // Constructor
        public Vector4(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        // Converts UnityEngine.Vector4 -> custom Vector4 (for debugging)
        public static implicit operator Vector4(UnityEngine.Vector4 v)
        {
            return new Vector4(v.x, v.y, v.z, v.w);
        }

        // Converts custom Vector4 -> custom UnityEngine.Vector4 (for debugging)
        public static implicit operator UnityEngine.Vector4(Vector4 v)
        {
            return new UnityEngine.Vector4(v.x, v.y, v.z, v.w);
        }

        // Returns standard Vector4 zero by typing CustomMathsLibrary.Vector4.zero
        public static Vector4 zero
        {
            get { return new Vector4(0f, 0f, 0f, 0f); }
        }

        // String override (for debugging)
        public override string ToString()
        {
            return "(" + x + ", " + y + ", " + z + ", " + w + ")";
        }
    }

    // ------------------------------------ CUSTOM MATRIX CLASS ------------------------------------ // 

    public class Matrix4
    {
        public Vector4 row0;
        public Vector4 row1;
        public Vector4 row2;
        public Vector4 row3;

        // Builds a transform matrix from R = right, U = up, F = forward, P = position
        public Matrix4(Vector3 R, Vector3 U, Vector3 F, Vector3 P)
        {
            row0 = new Vector4(R.x, U.x, F.x, P.x);

            row1 = new Vector4(R.y, U.y, F.y, P.y);

            row2 = new Vector4(R.z, U.z, F.z, P.z);

            row3 = new Vector4(0, 0, 0, 1);
        }

        // Multiplies the matrix by a vector4 and returns it
        public Vector4 Multiply(Vector4 v)
        {
            float x = Dot4(row0, v);

            float y = Dot4(row1, v);

            float z = Dot4(row2, v);

            float w = Dot4(row3, v);

            return new Vector4(x, y, z, w);
        }
    }

    // ------------------------------------ CUSTOM QUATERNION CLASS ------------------------------------ // 

    public class Quat
    {
        public float w;
        public float x;
        public float y;
        public float z;

        // Constructor
        public Quat(float w, float x, float y, float z)
        {
            this.w = w;
            this.x = x;
            this.y = y;
            this.z = z;
        }

        // Creates a pure quaternion (for rotation)
        public Quat(Vector3 v)
        {
            this.w = 0;
            this.x = v.x;
            this.y = v.y;
            this.z = v.z;
        }

        // Creates a rotation quaternion
        public Quat(Vector3 axis, float angleRad)
        {
            axis = Normalize(axis);

            float halfAngle = angleRad * 0.5f;

            w = Mathf.Cos(halfAngle);

            x = axis.x * Mathf.Sin(halfAngle);

            y = axis.y * Mathf.Sin(halfAngle);

            z = axis.z * Mathf.Sin(halfAngle);
        }

        // Handles quaternion multiplication by using hamilton's product
        public static Quat operator *(Quat a, Quat b)
        {
            float wScaled = a.w * b.w - (a.x * b.x + a.y * b.y + a.z * b.z);

            float xScaled = a.w * b.x + b.w * a.x + (a.y * b.z - a.z * b.y);

            float yScaled = a.w * b.y + b.w * a.y + (a.z * b.x - a.x * b.z);

            float zScaled = a.w * b.z + b.w * a.z + (a.x * b.y - a.y * b.x);

            return new Quat(wScaled, xScaled, yScaled, zScaled);
        }

        // Handles the inverse of a unit quaternion
        public Quat Inverse()
        {
            return new Quat(w, -x, -y, -z);
        }

        // Rotates a vector using a quaternion by converting it, applying rotation and extracting the rotated vector
        public Vector3 RotateVector(Vector3 v)
        {
            Quat p = new Quat(v);

            Quat P = this * p * this.Inverse();

            return new Vector3(P.x, P.y, P.z);
        }

        // Converts a custom quaternion to unity's quaternion
        public Quaternion ToUnityQuaternion()
        {
            return new Quaternion(x, y, z, w);
        }

        // Builds a quaternion from euler angles by using half-angle trigonometry before combining them
        public static Quat Euler(float xDeg, float yDeg, float zDeg)
        {
            float xRad = CustomMathsLibrary.DegreesToRadians(xDeg);
            float yRad = CustomMathsLibrary.DegreesToRadians(yDeg);
            float zRad = CustomMathsLibrary.DegreesToRadians(zDeg);

            float cx = Mathf.Cos(xRad * 0.5f);
            float sx = Mathf.Sin(xRad * 0.5f);

            float cy = Mathf.Cos(yRad * 0.5f);
            float sy = Mathf.Sin(yRad * 0.5f);

            float cz = Mathf.Cos(zRad * 0.5f);
            float sz = Mathf.Sin(zRad * 0.5f);

            Quat q = new Quat(0, 0, 0, 0);

            q.w = (cx * cy * cz) + (sx * sy * sz);
            q.x = (sx * cy * cz) - (cx * sy * sz);
            q.y = (cx * sy * cz) + (sx * cy * sz);
            q.z = (cx * cy * sz) - (sx * sy * cz);

            return q;
        }
    }

    // ------------------------------------ VECTOR2 MATH ------------------------------------ // 

    // Adds two Vector2s together
    public static Vector2 Add(Vector2 a, Vector2 b)
    {
        return new Vector2(a.x + b.x, a.y + b.y);
    }

    // Subtracts a Vector2 (b) by another Vector2 (a)
    public static Vector2 Subtract(Vector2 a, Vector2 b)
    {
        return new Vector2(a.x - b.x, a.y - b.y);
    }

    // Finds the length of a Vector2
    public static float Magnitude(Vector2 v)
    {
        return Mathf.Sqrt(v.x * v.x + v.y * v.y);
    }

    // Finds the distance between two Vector2's
    public static float Distance(Vector2 a, Vector2 b)
    {
        return Magnitude(Subtract(a, b));
    }

    // Multiplies a Vector2 by a scalar (s)
    public static Vector2 Scale(Vector2 v, float s)
    {
        return new Vector2(v.x * s, v.y * s);
    }

    // Divides a Vector2 by a scalar (s)
    public static Vector2 Divide(Vector2 v, float s)
    {
        if (s == 0)
        {
            s = 1;
        }

        return new Vector2(v.x / s, v.y / s);
    }

    // Returns a unit Vector2 and handles dividing by zero safely
    public static Vector2 Normalize(Vector2 v)
    {
        float len = Magnitude(v);

        if (len > 0)
        {
            return Divide(v, len);
        }
        else
        {
            return new Vector2(0f, 0f);
        }
    }

    // Takes two Vector2s and returns a single scalar number that determines if they align in the same direction
    public static float Dot(Vector2 a, Vector2 b)
    {
        return (a.x * b.x) + (a.y * b.y);
    }

    // Creates linear interpolation between two Vector2 points
    public static Vector2 LerpVector(Vector2 startPos, Vector2 endPos, float t)
    {
        return Add(startPos, Scale(Subtract(endPos, startPos), t));
    }

    // ------------------------------------ VECTOR3 MATH ------------------------------------ // 

    // Adds two Vector3s together
    public static Vector3 Add(Vector3 a, Vector3 b)
    {
        return new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
    }

    // Subtracts a Vector3 (b) by another Vector3 (a)
    public static Vector3 Subtract(Vector3 a, Vector3 b)
    {
        return new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
    }

    // Finds the length of a Vector3
    public static float Magnitude(Vector3 v)
    {
        return Mathf.Sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
    }

    // Finds the distance between two Vector3's
    public static float Distance(Vector3 a, Vector3 b)
    {
        return Magnitude(Subtract(a, b));
    }

    // Multiplies a Vector3 by a scalar (s)
    public static Vector3 Scale(Vector3 v, float s)
    {
        return new Vector3(v.x * s, v.y * s, v.z * s);
    }

    // Divides a Vector3 by a scalar (s)
    public static Vector3 Divide(Vector3 v, float s)
    {
        if (s == 0)
        {
            s = 1;
        }

        return new Vector3(v.x / s, v.y / s, v.z / s);
    }

    // Returns a unit Vector2 and handles dividing by zero safely
    public static Vector3 Normalize(Vector3 v)
    {
        float len = Magnitude(v);


        if (len > 0)
        {
            return Divide(v, len);
        }
        else
        {
            return new Vector3(0f, 0f, 0f);
        }
    }

    // Takes two Vector3s and returns a single scalar number that determines if they align in the same direction
    public static float Dot(Vector3 a, Vector3 b)
    {
        return (a.x * b.x) + (a.y * b.y) + (a.z * b.z);
    }

    // Creates linear interpolation between two Vector3 points
    public static Vector3 LerpVector(Vector3 startPos, Vector3 endPos, float t)
    {
        return Add(startPos, Scale(Subtract(endPos, startPos), t));
    }

    public static Vector3 ClosestPointOnSegment(Vector3 a, Vector3 b, Vector3 p)
    {
        Vector3 ab = Subtract(b, a);
        Vector3 ap = Subtract(p, a);

        float ab2 = Dot(ab, ab);

        if (ab2 == 0) return a;

        float t = Dot(ap, ab) / ab2;
        t = Clamp(t, 0f, 1f);

        return Add(a, Scale(ab, t));
    }

    public static void ClosestPointsBetweenSegments(Vector3 p1, Vector3 q1, Vector3 p2, Vector3 q2, out Vector3 c1, out Vector3 c2)
    {
        Vector3 d1 = Subtract(q1, p1);
        Vector3 d2 = Subtract(q2, p2);
        Vector3 r = Subtract(p1, p2);

        float a = Dot(d1, d1);
        float e = Dot(d2, d2);
        float f = Dot(d2, r);

        float s, t;

        if (a <= 1e-6f && e <= 1e-6f)
        {
            s = t = 0.0f;
            c1 = p1;
            c2 = p2;
            return;
        }

        if (a <= 1e-6f)
        {
            s = 0.0f;
            t = f / e;
            t = Mathf.Clamp01(t);
        }
        else
        {
            float c = Dot(d1, r);
            if (e <= 1e-6f)
            {
                t = 0;
                s = Mathf.Clamp01(-c / a);
            }
            else
            {
                float b = Dot(d1, d2);
                float denom = a * e - b * b;

                if (denom != 0) s = Mathf.Clamp01((b * f - c * e) / denom);
                else s = 0;

                t = (b * s + f) / e;

                if (t < 0.0f) { t = 0.0f; s = Mathf.Clamp01(-c / a); }
                else if (t > 1.0f) { t = 1.0f; s = Mathf.Clamp01((b - c) / a); }
            }
        }

        c1 = Add(p1, Scale(d1, s));
        c2 = Add(p2, Scale(d2, t));
    }

    // ------------------------------------ VECTOR4 MATH ------------------------------------ // 

    // Adds two Vector4s together
    public static Vector4 Add(Vector4 a, Vector4 b)
    {
        return new Vector4(a.x + b.x, a.y + b.y, a.z + b.z, a.w + b.w);
    }

    // Subtracts a Vector4 (b) by another Vector4 (a)
    public static Vector4 Subtract(Vector4 a, Vector4 b)
    {
        return new Vector4(a.x - b.x, a.y - b.y, a.z - b.z, a.w - b.w);
    }

    // Finds the length of a Vector4
    public static float Magnitude(Vector4 v)
    {
        return Mathf.Sqrt(v.x * v.x + v.y * v.y + v.z * v.z + v.w * v.w);
    }

    // Finds the distance between two Vector4's
    public static float Distance(Vector4 a, Vector4 b)
    {
        return Magnitude(Subtract(a, b));
    }

    // Multiplies a Vector4 by a scalar (s)
    public static Vector4 Scale(Vector4 v, float s)
    {
        return new Vector4(v.x * s, v.y * s, v.z * s, v.w * s);
    }

    // Divides a Vector4 by a scalar (s)
    public static Vector4 Divide(Vector4 v, float s)
    {
        return new Vector4(v.x / s, v.y / s, v.z / s, v.w / s);
    }

    // Takes two Vector4s and returns a single scalar number that determines if they align in the same direction (used in matrix multiplication)
    public static float Dot4(Vector4 a, Vector4 b)
    {
        return a.x * b.x + a.y * b.y + a.z * b.z + a.w * b.w;
    }

    // Builds an orthornormal basis off of forward (F), assuming the world up as (0, 1, 0) and computes right
    public static void BuildBasisFromForward(Vector3 forward, out Vector3 R, out Vector3 U, out Vector3 F)
    {
        F = Normalize(forward);

        U = new Vector3(0, 1, 0);

        R = Normalize(CrossProduct(U, F));

        U = CrossProduct(F, R);
    }

    // Transforms a point using a matrix, returning xyz
    public static Vector3 TransformPoint(Matrix4 M, Vector3 p)
    {
        Vector4 v = new Vector4(p.x, p.y, p.z, 1f);

        Vector4 outV = M.Multiply(v);

        return new Vector3(outV.x, outV.y, outV.z);
    }

    // ------ MATRIX MATH ------ // 

    // Component-wise scaling
    public static Vector3 NormalFromScale(Vector3 v, Vector3 s)
    {
        return new Vector3(v.x * s.x, v.y * s.y, v.z * s.z);
    }

    // Converts local direction to world direction
    public static Vector3 DirectionFromBasis(Vector3 localDir, Vector3 R, Vector3 U, Vector3 F)
    {
        Vector3 worldDir = Add(Add(Scale(R, localDir.x), Scale(U, localDir.y)), Scale(F, localDir.z));
        return worldDir;
    }

    // Adds wirkd position offset
    public static Vector3 LocalPointToWorldPoint(Vector3 P, Vector3 localPoint, Vector3 R, Vector3 U, Vector3 F)
    {
        Vector3 worldPoint = Add(P, DirectionFromBasis(localPoint, R, U, F));
        return worldPoint;
    }

    // ------ ANGLE MATH ------ // 

    // Converts degrees to radians
    public static float DegreesToRadians(float degrees)
    {
        return degrees * (Mathf.PI / 180);
    }

    // Converts radians to degrees
    public static float RadiansToDegrees(float radians)
    {
        return radians * (180 / Mathf.PI);
    }

    // Builds a unit vector and turns it into an angle
    public static float AngleFromVector2(Vector2 v)
    {
        return Mathf.Atan2(v.y, v.x);
    }

    // Builds a unit vector from an angle
    public static Vector2 Vector2FromAngle(float radians)
    {
        return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
    }

    // Creates direction vector from angles
    public static Vector3 ForwardFromYawPitch(float yawRadians, float pitchRadians)
    {
        Vector3 f = new Vector3(0f, 0f, 0f);

        f.x = Mathf.Sin(yawRadians) * Mathf.Cos(pitchRadians);
        f.y = Mathf.Sin(pitchRadians);
        f.z = Mathf.Cos(yawRadians) * Mathf.Cos(pitchRadians);

        return f;
    }

    // Returns a perpendicular vector
    public static Vector3 CrossProduct(Vector3 a, Vector3 b)
    {
        Vector3 f = new Vector3(0f, 0f, 0f);

        f.x = (a.y * b.z) - (a.z * b.y);
        f.y = (a.z * b.x) - (a.x * b.z);
        f.z = (a.x * b.y) - (a.y * b.x);

        return f;
    }

    // ------ QUATERNATIONS ------

    // Uses rodrigues rotation formula to rotate around an axis
    public static Vector3 RotateAroundAxis(Vector3 v, Vector3 axis, float angleRad)
    {
        axis = Normalize(axis);

        float cosTheta = Mathf.Cos(angleRad);
        float sinTheta = Mathf.Sin(angleRad);

        return (Add(Add(Scale(v, cosTheta), Scale(Scale(axis, Dot(v, axis)), 1 - cosTheta)), Scale(CrossProduct(axis, v), sinTheta)));
    }

    // ------ MISC ------

    // Restricts a value between min/max
    public static float Clamp(float value, float min, float max)
    {
        if (value < min) return min;
        if (value > max) return max;
        else return value;
    }

    // Scalar interpolation
    public static float Lerp(float a, float b, float t)
    {
        t = Clamp(t, 0, 1);

        return a + (b - a) * t;
    }
}