using System.Numerics;

namespace Engine
{
    public static class VectorExtensions
    {
        public static Vector3 XYZ(this Vector4 v) => new(v.X, v.Y, v.Z);
        public static Vector2 XY(this Vector4 v) => new(v.X, v.Y);
        public static Vector2 XY(this Vector3 v) => new(v.X, v.Y);
    }
}
