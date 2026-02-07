using Engine.Graphics.Structs;
using System.Numerics;

namespace Engine.Graphics
{
    public interface RenderItem
    {
        IList<string> GetStagesParticipated();
        void Render(RenderContext rc, string pipelineStage);
        RenderOrderKey GetRenderOrderKey(Vector3 viewPosition);
        bool Cull(ref BoundingFrustum visibleFrustum);
    }
}
