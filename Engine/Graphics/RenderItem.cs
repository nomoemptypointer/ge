using Engine.Graphics.Structs;
using System.Numerics;
using Veldrid;

namespace Engine.Graphics
{
    public interface RenderItem
    {
        IList<string> GetStagesParticipated();
        void Render(GraphicsDevice rc, string pipelineStage);
        RenderOrderKey GetRenderOrderKey(Vector3 viewPosition);
        bool Cull(ref BoundingFrustum visibleFrustum);
    }
}
