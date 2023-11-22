using System.Numerics;

namespace RszTool.via
{
    public struct Range
    {
        public float r;
        public float s;

        public readonly override string ToString()
        {
            return $"Range({r}, {s})";
        }

        public static explicit operator Range(Vector2 vector)
        {
            return new Range { r = vector.X, s = vector.Y };
        }

        public static explicit operator Vector2(Range range)
        {
            return new Vector2(range.r, range.s);
        }
    }


    public struct RangeI
    {
        public int r;
        public int s;

        public readonly override string ToString()
        {
            return $"Range({r}, {s})";
        }
    }


    public struct mat4 {
        public float m00;
        public float m01;
        public float m02;
        public float m03;
        public float m10;
        public float m11;
        public float m12;
        public float m13;
        public float m20;
        public float m21;
        public float m22;
        public float m23;
        public float m30;
        public float m31;
        public float m32;
        public float m33;
    }


    public struct OBB
    {
        public mat4 coord;
        public Vector3 extent;
        public float _padding;
    }


    public struct Sphere
    {
        public Vector3 pos;
        public float r;
    }


    public struct AABB
    {
        public Vector3 minpos;
        public float _padding;
        public Vector3 maxpos;
        public float _padding2;
    }
}
