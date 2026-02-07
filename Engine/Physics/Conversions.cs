using Engine.Graphics.Structs;

namespace Engine.Physics
{
    public static class Conversions
    {
        public static BEPUutilities.Ray ToBEPURay(this Ray ray)
        {
            return new BEPUutilities.Ray(ray.Origin, ray.Direction);
        }
    }
}
