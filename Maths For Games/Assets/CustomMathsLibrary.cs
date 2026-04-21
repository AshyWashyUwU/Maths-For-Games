using UnityEngine;
public static class CustomMathsLibrary
{
    public class Vector2
    {
        public float x;
        public float y;

        public Vector2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public static implicit operator Vector2(UnityEngine.Vector2 v)
        {
            return new Vector2(v.x, v.y);
        }

        public static implicit operator UnityEngine.Vector2(Vector2 v)
        {
            return new UnityEngine.Vector3(v.x, v.y);
        }

        public static Vector2 zero
        {
            get { return new Vector2(0f, 0f); }
        }

        public override string ToString()
        {
            return "(" + x + ", " + y + ")";
        }
    }

    public class Vector3
    {
        public float x;
        public float y;
        public float z;

        public Vector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static implicit operator Vector3(UnityEngine.Vector3 v)
        {
            return new Vector3(v.x, v.y, v.z);
        }

        public static implicit operator UnityEngine.Vector3(Vector3 v)
        {
            return new UnityEngine.Vector3(v.x, v.y, v.z);
        }

        public static Vector3 zero
        {
            get { return new Vector3(0f, 0f, 0f); }
        }

        public override string ToString()
        {
            return "(" + x + ", " + y + ", " + z + ")";
        }
    }

    public class Vector4
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public Vector4(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public static implicit operator Vector4(UnityEngine.Vector4 v)
        {
            return new Vector4(v.x, v.y, v.z, v.w);
        }

        public static implicit operator UnityEngine.Vector4(Vector4 v)
        {
            return new UnityEngine.Vector4(v.x, v.y, v.z, v.w);
        }

        public static Vector4 zero
        {
            get { return new Vector4(0f, 0f, 0f, 0f); }
        }

        public override string ToString()
        {
            return "(" + x + ", " + y + ", " + z + ", " + w + ")";
        }
    }

    public class Matrix4
    {
        public Vector4 row0;
        public Vector4 row1;
        public Vector4 row2;
        public Vector4 row3;

        public Matrix4(Vector3 R, Vector3 U, Vector3 F, Vector3 P)
        {
            row0 = new Vector4(R.x, U.x, F.x, P.x);

            row1 = new Vector4(R.y, U.y, F.y, P.y);

            row2 = new Vector4(R.z, U.z, F.z, P.z);

            row3 = new Vector4(0, 0, 0, 1);
        }

        public Vector4 Multiply(Vector4 v)
        {
            float x = Dot4(row0, v);

            float y = Dot4(row1, v);

            float z = Dot4(row2, v);

            float w = Dot4(row3, v);

            return new Vector4(x, y, z, w);
        }
    }

    public class Quat
    {
        public float w;
        public float x;
        public float y;
        public float z;

        public Quat(float w, float x, float y, float z)
        {
            this.w = w;
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Quat(Vector3 v)
        {
            this.w = 0;
            this.x = v.x;
            this.y = v.y;
            this.z = v.z;
        }

        public Quat(Vector3 axis, float angleRad)
        {
            axis = Normalize(axis);

            float halfAngle = angleRad * 0.5f;

            w = Mathf.Cos(halfAngle);

            x = axis.x * Mathf.Sin(halfAngle);

            y = axis.y * Mathf.Sin(halfAngle);

            z = axis.z * Mathf.Sin(halfAngle);
        }

        public static Quat operator *(Quat a, Quat b)
        {
            float wScaled = a.w * b.w - (a.x * b.x + a.y * b.y + a.z * b.z);

            float xScaled = a.w * b.x + b.w * a.x + (a.y * b.z - a.z * b.y);

            float yScaled = a.w * b.y + b.w * a.y + (a.z * b.x - a.x * b.z);

            float zScaled = a.w * b.z + b.w * a.z + (a.x * b.y - a.y * b.x);

            return new Quat(wScaled, xScaled, yScaled, zScaled);
        }

        public Quat Inverse()
        {
            return new Quat(w, -x, -y, -z);
        }


        public Vector3 RotateVector(Vector3 v)
        {
            Quat p = new Quat(v);

            Quat P = this * p * this.Inverse();

            return new Vector3(P.x, P.y, P.z);
        }

        public Quaternion ToUnityQuaternion()
        {
            return new Quaternion(x, y, z, w);
        }
    }

    // ------ VECTOR2 MATH ------ // 

    public static Vector2 Add(Vector2 a, Vector2 b)
    {
        Debug.Log(new Vector2(a.x + b.x, a.y + b.y));
        return new Vector2(a.x + b.x, a.y + b.y);
    }

    public static Vector2 Subtract(Vector2 a, Vector2 b)
    {
        return new Vector2(a.x - b.x, a.y - b.y);
    }

    public static float Magnitude(Vector2 v)
    {
        return Mathf.Sqrt(v.x * v.x + v.y * v.y);
    }

    public static float Distance(Vector2 a, Vector2 b)
    {
        return Magnitude(Subtract(a, b));
    }

    public static Vector2 Scale(Vector2 v, float s)
    {
        return new Vector2(v.x * s, v.y * s);
    }

    public static Vector2 Divide(Vector2 v, float s)
    {
        return new Vector2(v.x / s, v.y / s);
    }

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

    public static float Dot(Vector2 a, Vector2 b)
    {
        return (a.x * b.x) + (a.y * b.y);
    }

    // ------ VECTOR3 MATH ------ // 

    public static Vector3 Add(Vector3 a, Vector3 b)
    {
        return new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
    }

    public static Vector3 Subtract(Vector3 a, Vector3 b)
    {
        return new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
    }

    public static float Magnitude(Vector3 v)
    {
        return Mathf.Sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
    }

    public static float Distance(Vector3 a, Vector3 b)
    {
        return Magnitude(Subtract(a, b));
    }

    public static Vector3 Scale(Vector3 v, float s)
    {
        return new Vector3(v.x * s, v.y * s, v.z * s);
    }

    public static Vector3 Divide(Vector3 v, float s)
    {
        return new Vector3(v.x / s, v.y / s, v.z / s);
    }

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

    public static Vector3 LerpVector(Vector3 startPos, Vector3 endPos, float t)
    {
        return Add(startPos, Scale(Subtract(endPos, startPos), t));
    }

    public static float Dot(Vector3 a, Vector3 b)
    {
        return (a.x * b.x) + (a.y * b.y) + (a.z * b.z);
    }

    // ------ VECTOR4 MATH ------ // 

    public static Vector4 Add(Vector4 a, Vector4 b)
    {
        return new Vector4(a.x + b.x, a.y + b.y, a.z + b.z, a.w + b.w);
    }

    public static Vector4 Subtract(Vector4 a, Vector4 b)
    {
        return new Vector4(a.x - b.x, a.y - b.y, a.z - b.z, a.w - b.w);
    }

    public static float Magnitude(Vector4 v)
    {
        return Mathf.Sqrt(v.x * v.x + v.y * v.y + v.z * v.z + v.w * v.w);
    }

    public static float Distance(Vector4 a, Vector4 b)
    {
        return Magnitude(Subtract(a, b));
    }

    public static Vector4 Scale(Vector4 v, float s)
    {
        return new Vector4(v.x * s, v.y * s, v.z * s, v.w * s);
    }

    public static Vector4 Divide(Vector4 v, float s)
    {
        return new Vector4(v.x / s, v.y / s, v.z / s, v.w / s);
    }

    public static float Dot4(Vector4 a, Vector4 b)
    {
        return a.x * b.x + a.y * b.y + a.z * b.z + a.w * b.w;
    }

    public static void BuildBasisFromForward(Vector3 forward, out Vector3 R, out Vector3 U, out Vector3 F)
    {
        F = Normalize(forward);

        U = new Vector3(0, 1, 0);

        R = Normalize(CrossProduct(U, F));

        U = CrossProduct(F, R);
    }

    public static Vector3 TransformPoint(Matrix4 M, Vector3 p)
    {
        Vector4 v = new Vector4(p.x, p.y, p.z, 1f);

        Vector4 outV = M.Multiply(v);

        return new Vector3(outV.x, outV.y, outV.z);
    }

    // ------ MATRIX MATH ------ // 

    public static Vector3 NormalFromScale(Vector3 v, Vector3 s)
    {
        return new Vector3(v.x * s.x, v.y * s.y, v.z * s.z);
    }

    public static Vector3 DirectionFromBasis(Vector3 localDir, Vector3 R, Vector3 U, Vector3 F)
    {
        Vector3 worldDir = Add(Add(Scale(R, localDir.x), Scale(U, localDir.y)), Scale(F, localDir.z));
        return worldDir;
    }

    public static Vector3 LocalPointToWorldPoint(Vector3 P, Vector3 localPoint, Vector3 R, Vector3 U, Vector3 F)
    {
        Vector3 worldPoint = Add(P, DirectionFromBasis(localPoint, R, U, F));
        return worldPoint;
    }

    // ------ ANGLE MATH ------ // 

    public static float DegreesToRadians(float degrees)
    {
        return degrees * (Mathf.PI / 180);
    }

    public static float RadiansToDegrees(float radians)
    {
        return radians * (180 / Mathf.PI);
    }

    public static float AngleFromVector2(Vector2 v)
    {
        return Mathf.Atan2(v.y, v.x);
    }

    public static Vector2 Vector2FromAngle(float radians)
    {
        return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
    }

    public static Vector3 ForwardFromYawPitch(float yawRadians, float pitchRadians)
    {
        Vector3 f = new Vector3(0f, 0f, 0f);

        f.x = Mathf.Sin(yawRadians) * Mathf.Cos(pitchRadians);
        f.y = Mathf.Sin(pitchRadians);
        f.z = Mathf.Cos(yawRadians) * Mathf.Cos(pitchRadians);

        return f;
    }

    public static Vector3 CrossProduct(Vector3 a, Vector3 b)
    {
        Vector3 f = new Vector3(0f, 0f, 0f);

        f.x = (a.y * b.z) - (a.z * b.y);
        f.y = (a.z * b.x) - (a.x * b.z);
        f.z = (a.x * b.y) - (a.y * b.x);

        return f;
    }

    // ------ QUATERNATIONS ------

    public static Vector3 RotateAroundAxis(Vector3 v, Vector3 axis, float angleRad)
    {
        axis = Normalize(axis);

        float cosTheta = Mathf.Cos(angleRad);
        float sinTheta = Mathf.Sin(angleRad);

        return (Add(Add(Scale(v, cosTheta), Scale(Scale(axis, Dot(v, axis)), 1 - cosTheta)), Scale(CrossProduct(axis, v), sinTheta)));
    }

    // ------ MISC ------

    public static float Clamp(float value, float min, float max)
    {
        if (value < min) return min;
        if (value > max) return max;
        else return value;
    }
}