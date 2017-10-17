﻿using System;
using System.Diagnostics;
using System.Numerics;
using VdSdl2;

namespace Vd2.NeoDemo
{
    public class ImGuiRenderable : Renderable, IUpdateable
    {
        private ImGuiRenderer _imguiRenderer;
        private int _width;
        private int _height;

        public ImGuiRenderable(int width, int height)
        {
            _width = width;
            _height = height;
        }

        public void WindowResized(int width, int height) => _imguiRenderer.WindowResized(width, height);

        public override void CreateDeviceObjects(GraphicsDevice gd, CommandList cl, SceneContext sc)
        {
            if (_imguiRenderer == null)
            {
                _imguiRenderer = new ImGuiRenderer(gd, cl, _width, _height);
            }
            else
            {
                _imguiRenderer.CreateDeviceResources(gd, cl);
            }
        }

        public override void DestroyDeviceObjects()
        {
            _imguiRenderer.Dispose();
        }

        public override RenderOrderKey GetRenderOrderKey(Vector3 cameraPosition)
        {
            return new RenderOrderKey(ulong.MaxValue);
        }

        public override void Render(GraphicsDevice gd, CommandList cl, SceneContext sc, RenderPasses renderPass)
        {
            Debug.Assert(renderPass == RenderPasses.Overlay);
            _imguiRenderer.Render(gd, cl);
        }

        public override RenderPasses RenderPasses => RenderPasses.Overlay;

        public void Update(float deltaSeconds)
        {
            _imguiRenderer.Update(deltaSeconds);
            _imguiRenderer.OnInputUpdated(InputTracker.FrameSnapshot);
        }
    }
}
