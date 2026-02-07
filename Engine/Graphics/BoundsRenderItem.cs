using Engine.Graphics.Structs;

namespace Engine.Graphics
{
    public interface BoundsRenderItem : RenderItem
    {
        BoundingBox Bounds { get; }
        bool RayCast(Ray ray, out float distance);
        int RayCast(Ray ray, List<float> distances);
    }
}
