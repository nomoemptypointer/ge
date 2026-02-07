using System.Numerics;

namespace Engine.Graphics.Structs
{
    public struct VertexPosition(Vector3 position)
    {
        public const byte SizeInBytes = 12;
        public const byte ElementCount = 1;

        public readonly Vector3 Position = position;
    }
}
