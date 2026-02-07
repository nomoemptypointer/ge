using System.Numerics;
using Veldrid;

namespace Engine.Graphics.Structs
{
    public struct VertexPositionColor(Vector3 position, RgbaFloat color)
    {
        public const byte SizeInBytes = 28;
        public const byte ColorOffset = 12;
        public const byte ElementCount = 2;

        public readonly Vector3 Position = position;
        public readonly RgbaFloat Color = color;
    }
}
