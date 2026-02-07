using Engine.Graphics;
using ImGuiNET;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Engine.Editor
{
    public class TextureDrawer : Drawer<ImageSharpTexture>
    {
        private static ConditionalWeakTable<ImageSharpTexture, DeviceTexture> _deviceTextures = new ConditionalWeakTable<ImageSharpTexture, DeviceTexture>();

        public override bool Draw(string label, ref ImageSharpTexture obj, RenderContext rc)
        {
            ImGui.Text(label);

            DeviceTexture dt;
            if (!_deviceTextures.TryGetValue(obj, out dt))
            {
                dt = obj.CreateDeviceTexture(rc.ResourceFactory);
                _deviceTextures.Add(obj, dt);
            }

            IntPtr id = ImGuiImageHelper.GetOrCreateImGuiBinding(rc, dt);

            float ratio = (float)obj.Width / obj.Height;

            Vector2 region = ImGui.GetContentRegionAvail();
            float minDimension = Math.Min(500, Math.Min(region.X, region.Y)) - 50;
            Vector2 imageDimensions = new(minDimension, minDimension / ratio);

            ImGui.Image(id, imageDimensions, Vector2.Zero, Vector2.One, Vector4.One, Vector4.One);

            return false;
        }
    }
}
