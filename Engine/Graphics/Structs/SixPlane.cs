using System.Numerics;

namespace Engine.Graphics.Structs
{
    internal struct SixPlane
    {
        public Plane Left;
        public Plane Right;
        public Plane Bottom;
        public Plane Top;
        public Plane Near;
        public Plane Far;
    }
}
