using Engine.Behaviors;
using Engine.Graphics.Structs;
using ImGuiNET;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Veldrid;
using Veldrid.Sdl2;

namespace Engine.Graphics
{
    public class ImGuiRenderer : RenderItem, IUpdateable, IDisposable
    {
        private readonly DynamicDataProvider<Matrix4x4> _projectionMatrixProvider;
        private RawTextureDataArray<int> _fontTexture;
        private FontTextureData _textureData;

        // Context objects
        private Material _material;
        private DeviceBuffer _vertexBuffer;
        private DeviceBuffer _indexBuffer;
        private BlendState _blendState;
        private DepthStencilState _depthDisabledState;
        private RasterizerState _rasterizerState;
        private ShaderTextureBinding _fontTextureBinding;

        private int _fontAtlasID = 1;
        private GraphicsDevice _rc;
        private readonly InputSystem _input;
        private bool _controlDown;
        private bool _shiftDown;
        private bool _altDown;

        public ImGuiRenderer(GraphicsDevice rc, Sdl2Window window, InputSystem input)
        {
            _rc = rc;
            _input = input;
            //ImGui.GetIO().FontAtlas.AddDefaultFont(); // TODO: Problematic (once updated will be fixed, 2 projects were interfering)
            _projectionMatrixProvider = new DynamicDataProvider<Matrix4x4>();

            InitializeContextObjects(rc);

            SetPerFrameImGuiData(rc, 1f / 60f);

            ImGui.NewFrame();

            input.RegisterCallback(OnInputUpdated);
        }

        private void InitializeContextObjects(GraphicsDevice rc)
        {
            ResourceFactory factory = rc.ResourceFactory;
            _vertexBuffer = factory.CreateVertexBuffer(500, false);
            _indexBuffer = factory.CreateIndexBuffer(100, false);
            _blendState = factory.CreateCustomBlendState(
                true,
                Blend.InverseSourceAlpha, Blend.Zero, BlendFunction.Add,
                Blend.SourceAlpha, Blend.InverseSourceAlpha, BlendFunction.Add);
            _depthDisabledState = factory.CreateDepthStencilState(false, DepthComparison.Always);
            _rasterizerState = factory.CreateRasterizerState(FaceCullingMode.None, TriangleFillMode.Solid, true, true);
            RecreateFontDeviceTexture(rc);
            _material = factory.CreateMaterial(
                rc,
                "imgui-vertex", "imgui-frag",
                new MaterialVertexInput(20, new MaterialVertexInputElement[]
                {
                    new MaterialVertexInputElement("in_position", VertexSemanticType.Position, VertexElementFormat.Float2),
                    new MaterialVertexInputElement("in_texcoord", VertexSemanticType.TextureCoordinate, VertexElementFormat.Float2),
                    new MaterialVertexInputElement("in_color", VertexSemanticType.Color, VertexElementFormat.Byte4)
                }),
                new MaterialInputs<MaterialGlobalInputElement>(new MaterialGlobalInputElement[]
                {
                    new MaterialGlobalInputElement("ProjectionMatrixBuffer", MaterialInputType.Matrix4x4, _projectionMatrixProvider)
                }),
                MaterialInputs<MaterialPerObjectInputElement>.Empty,
                new MaterialTextureInputs(new MaterialTextureInputElement[]
                {
                    new TextureDataInputElement("surfaceTexture", _fontTexture)
                }));

        }

        public unsafe void RecreateFontDeviceTexture(GraphicsDevice rc)
        {
            var io = ImGui.GetIO();

            // Get font texture data from ImGui
            FontTextureData texData = io.FontAtlas.GetTexDataAsRGBA32();
            int width = texData.Width;
            int height = texData.Height;
            int bytesPerPixel = texData.BytesPerPixel;

            // Copy pixel data to managed int array
            int[] pixelData = new int[width * height];
            Buffer.MemoryCopy(texData.Pixels, Unsafe.AsPointer(ref pixelData[0]), pixelData.Length * sizeof(int), pixelData.Length * sizeof(int));

            // Create RawTextureDataArray using the copied data
            _fontTexture = new RawTextureDataArray<int>(pixelData, width, height, bytesPerPixel, PixelFormat.R8_G8_B8_A8);

            // Set ImGui font texture ID
            int fontAtlasID = 1;
            io.FontAtlas.SetTexID(fontAtlasID);

            // Create GPU texture and binding
            var deviceTexture = rc.ResourceFactory.CreateTexture(_fontTexture.PixelData, width, height, bytesPerPixel, PixelFormat.R8_G8_B8_A8);
            _fontTextureBinding = rc.ResourceFactory.CreateShaderTextureBinding(deviceTexture);

            // Clear CPU-side font data
            io.FontAtlas.ClearTexData();
        }

        public IList<string> GetStagesParticipated()
        {
            return CommonStages.Overlay;
        }

        public unsafe void Render(GraphicsDevice rc, string pipelineStage)
        {
            ImGui.Render();
            RenderImDrawData(ImGui.GetDrawData(), rc);
        }

        public RenderOrderKey GetRenderOrderKey(System.Numerics.Vector3 position)
        {
            return new RenderOrderKey();
        }

        public void Update(float deltaSeconds)
        {
            SetPerFrameImGuiData(_rc, deltaSeconds);
        }

        public unsafe void SetPerFrameImGuiData(GraphicsDevice rc, float deltaSeconds)
        {
            IO io = ImGui.GetIO();
            io.DisplaySize = new System.Numerics.Vector2(
                rc.Window.Width / rc.Window.ScaleFactor.X,
                rc.Window.Height / rc.Window.ScaleFactor.Y);
            io.DisplayFramebufferScale = rc.Window.ScaleFactor;
            io.DeltaTime = deltaSeconds / 1000; // DeltaTime is in seconds.
        }

        private void OnInputUpdated(InputSystem input)
        {
            UpdateImGuiInput((OpenTKWindow)_rc.Window, input.CurrentSnapshot);
            ImGui.NewFrame();
        }

        private unsafe void UpdateImGuiInput(Sdl2Window window, InputSnapshot snapshot)
        {
            IO io = ImGui.GetIO();
            MouseState cursorState = Mouse.GetCursorState();
            MouseState mouseState = Mouse.GetState();

            if (window.NativeWindow.Bounds.Contains(cursorState.X, cursorState.Y) && !RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // TODO: This does not take into account viewport coordinates.
                if (window.Exists)
                {
                    Point windowPoint = window.NativeWindow.PointToClient(new Point(cursorState.X, cursorState.Y));
                    io.MousePosition = new System.Numerics.Vector2(
                        windowPoint.X / window.ScaleFactor.X,
                        windowPoint.Y / window.ScaleFactor.Y);
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                io.MousePosition = new System.Numerics.Vector2(
                        cursorState.X,
                        cursorState.Y);
            }
            else
            {
                io.MousePosition = new System.Numerics.Vector2(-1f, -1f);
            }

            io.MouseDown[0] = mouseState.LeftButton == ButtonState.Pressed;
            io.MouseDown[1] = mouseState.RightButton == ButtonState.Pressed;
            io.MouseDown[2] = mouseState.MiddleButton == ButtonState.Pressed;

            float delta = snapshot.WheelDelta;
            io.MouseWheel = delta;

            ImGui.GetIO().MouseWheel = delta;

            IReadOnlyList<char> keyCharPresses = snapshot.KeyCharPresses;
            for (int i = 0; i < keyCharPresses.Count; i++)
            {
                char c = keyCharPresses[i];
                ImGui.AddInputCharacter(c);
            }

            IReadOnlyList<KeyEvent> keyEvents = snapshot.KeyEvents;
            for (int i = 0; i < keyEvents.Count; i++)
            {
                KeyEvent keyEvent = keyEvents[i];
                io.KeysDown[(int)keyEvent.Key] = keyEvent.Down;
                if (keyEvent.Key == Key.ControlLeft)
                {
                    _controlDown = keyEvent.Down;
                }
                if (keyEvent.Key == Key.ShiftLeft)
                {
                    _shiftDown = keyEvent.Down;
                }
                if (keyEvent.Key == Key.AltLeft)
                {
                    _altDown = keyEvent.Down;
                }
            }

            io.CtrlPressed = _controlDown;
            io.AltPressed = _altDown;
            io.ShiftPressed = _shiftDown;
        }

        private unsafe void RenderImDrawData(DrawData* draw_data, GraphicsDevice rc)
        {
            VertexDescriptor descriptor = new VertexDescriptor((byte)sizeof(DrawVert), 3, 0, IntPtr.Zero);

            int vertexOffsetInVertices = 0;
            int indexOffsetInElements = 0;

            if (draw_data->CmdListsCount == 0)
            {
                return;
            }

            for (int i = 0; i < draw_data->CmdListsCount; i++)
            {
                NativeDrawList* cmd_list = draw_data->CmdLists[i];

                _vertexBuffer.SetVertexData(new IntPtr(cmd_list->VtxBuffer.Data), descriptor, cmd_list->VtxBuffer.Size, vertexOffsetInVertices);
                _indexBuffer.SetIndices(new IntPtr(cmd_list->IdxBuffer.Data), IndexFormat.UInt16, sizeof(ushort), cmd_list->IdxBuffer.Size, indexOffsetInElements);

                vertexOffsetInVertices += cmd_list->VtxBuffer.Size;
                indexOffsetInElements += cmd_list->IdxBuffer.Size;
            }

            // Setup orthographic projection matrix into our constant buffer
            {
                var io = ImGui.GetIO();

                Matrix4x4 mvp = Matrix4x4.CreateOrthographicOffCenter(
                    0f,
                    io.DisplaySize.X,
                    io.DisplaySize.Y,
                    0.0f,
                    -1.0f,
                    1.0f);

                _projectionMatrixProvider.Data = mvp;
            }

            BlendState previousBlendState = rc.BlendState;
            rc.SetBlendState(_blendState);
            rc.SetDepthStencilState(_depthDisabledState);
            RasterizerState previousRasterizerState = rc.RasterizerState;
            rc.SetRasterizerState(_rasterizerState);
            rc.SetVertexBuffer(_vertexBuffer);
            rc.SetIndexBuffer(_indexBuffer);
            rc.SetMaterial(_material);

            ImGui.ScaleClipRects(draw_data, ImGui.GetIO().DisplayFramebufferScale);

            // Render command lists
            int vtx_offset = 0;
            int idx_offset = 0;
            for (int n = 0; n < draw_data->CmdListsCount; n++)
            {
                NativeDrawList* cmd_list = draw_data->CmdLists[n];
                for (int cmd_i = 0; cmd_i < cmd_list->CmdBuffer.Size; cmd_i++)
                {
                    DrawCmd* pcmd = &(((DrawCmd*)cmd_list->CmdBuffer.Data)[cmd_i]);
                    if (pcmd->UserCallback != IntPtr.Zero)
                    {
                        throw new NotImplementedException();
                    }
                    else
                    {
                        if (pcmd->TextureId != IntPtr.Zero)
                        {
                            if (pcmd->TextureId == new IntPtr(_fontAtlasID))
                            {
                                _material.UseTexture(0, _fontTextureBinding);
                            }
                            else
                            {
                                ShaderTextureBinding binding = ImGuiImageHelper.GetShaderTextureBinding(pcmd->TextureId);
                                _material.UseTexture(0, binding);
                            }
                        }

                        // TODO: This doesn't take into account viewport coordinates.
                        rc.SetScissorRectangle(
                            (int)pcmd->ClipRect.X,
                            (int)pcmd->ClipRect.Y,
                            (int)pcmd->ClipRect.Z,
                            (int)pcmd->ClipRect.W);

                        rc.DrawIndexedPrimitives((int)pcmd->ElemCount, idx_offset, vtx_offset);
                    }

                    idx_offset += (int)pcmd->ElemCount;
                }
                vtx_offset += cmd_list->VtxBuffer.Size;
            }

            rc.ClearScissorRectangle();
            rc.SetBlendState(previousBlendState);
            rc.SetDepthStencilState(rc.DefaultDepthStencilState);
            rc.SetRasterizerState(previousRasterizerState);
        }

        public void Dispose()
        {
            _vertexBuffer.Dispose();
            _indexBuffer.Dispose();
            _material.Dispose();
            _depthDisabledState.Dispose();
            _blendState.Dispose();
            _fontTextureBinding.Dispose();
        }

        public bool Cull(ref BoundingFrustum visibleFrustum)
        {
            return false;
        }
    }
}
