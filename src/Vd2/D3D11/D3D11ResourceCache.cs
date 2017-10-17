﻿using SharpDX.Direct3D11;

namespace Vd2.D3D11
{
    internal class D3D11ResourceCache
    {
        private readonly Device _device;

        public D3D11ResourceCache(Device device)
        {
            _device = device;
        }

        internal BlendState GetBlendState(ref BlendStateDescription description)
        {
            BlendAttachmentDescription[] attachmentStates = description.AttachmentStates;
            SharpDX.Direct3D11.BlendStateDescription d3dBlendStateDesc = new SharpDX.Direct3D11.BlendStateDescription();

            for (int i = 0; i < attachmentStates.Length; i++)
            {
                BlendAttachmentDescription state = attachmentStates[i];
                d3dBlendStateDesc.RenderTarget[i].IsBlendEnabled = state.BlendEnabled;
                d3dBlendStateDesc.RenderTarget[i].RenderTargetWriteMask = ColorWriteMaskFlags.All;
                d3dBlendStateDesc.RenderTarget[i].SourceBlend = D3D11Formats.VdToD3D11BlendOption(state.SourceColorFactor);
                d3dBlendStateDesc.RenderTarget[i].DestinationBlend = D3D11Formats.VdToD3D11BlendOption(state.DestinationColorFactor);
                d3dBlendStateDesc.RenderTarget[i].BlendOperation = D3D11Formats.VdToD3D11BlendOperation(state.ColorFunction);
                d3dBlendStateDesc.RenderTarget[i].SourceAlphaBlend = D3D11Formats.VdToD3D11BlendOption(state.SourceAlphaFactor);
                d3dBlendStateDesc.RenderTarget[i].DestinationAlphaBlend = D3D11Formats.VdToD3D11BlendOption(state.DestinationAlphaFactor);
                d3dBlendStateDesc.RenderTarget[i].AlphaBlendOperation= D3D11Formats.VdToD3D11BlendOperation(state.AlphaFunction);
            }

            return new BlendState(_device, d3dBlendStateDesc);
        }

        internal DepthStencilState GetDepthStencilState(ref DepthStencilStateDescription description)
        {
            SharpDX.Direct3D11.DepthStencilStateDescription dssDesc = new SharpDX.Direct3D11.DepthStencilStateDescription
            {
                DepthComparison = D3D11Formats.VdToD3D11DepthComparison(description.ComparisonKind),
                IsDepthEnabled = description.DepthTestEnabled,
                DepthWriteMask = description.DepthWriteEnabled ? DepthWriteMask.All : DepthWriteMask.Zero
            };

            return new DepthStencilState(_device, dssDesc);
        }

        internal RasterizerState GetRasterizerState(ref RasterizerStateDescription description)
        {
            SharpDX.Direct3D11.RasterizerStateDescription rssDesc = new SharpDX.Direct3D11.RasterizerStateDescription
            {
                CullMode = D3D11Formats.VdToD3D11CullMode(description.CullMode),
                FillMode = D3D11Formats.VdToD3D11FillMode(description.FillMode),
                IsDepthClipEnabled = description.DepthClipEnabled,
                IsScissorEnabled = description.ScissorTestEnabled
            };

            return new RasterizerState(_device, rssDesc);
        }

        internal InputLayout GetInputLayout(VertexLayoutDescription[] vertexLayouts, byte[] vsBytecode)
        {
            int totalCount = 0;
            for (int i = 0; i < vertexLayouts.Length; i++)
            {
                totalCount += vertexLayouts[i].Elements.Length;
            }

            int element = 0; // Total element index across slots.
            InputElement[] elements = new InputElement[totalCount];
            SemanticIndices si = new SemanticIndices();
            for (int slot = 0; slot < vertexLayouts.Length; slot++)
            {
                VertexElementDescription[] elementDescs = vertexLayouts[slot].Elements;
                int currentOffset = 0;
                for (int i = 0; i < elementDescs.Length; i++)
                {
                    VertexElementDescription desc = elementDescs[i];
                    elements[element] = new InputElement(
                        GetSemanticString(desc.Semantic),
                        SemanticIndices.GetAndIncrement(ref si, desc.Semantic),
                        D3D11Formats.ToDxgiFormat(desc.Format),
                        currentOffset,
                        slot,
                        desc.InstanceStepRate == 0 ? InputClassification.PerVertexData : InputClassification.PerInstanceData,
                        (int)desc.InstanceStepRate);

                    currentOffset += FormatHelpers.GetSizeInBytes(desc.Format);
                    element += 1;
                }
            }

            return new InputLayout(_device, vsBytecode, elements);
        }

        private string GetSemanticString(VertexElementSemantic semantic)
        {
            switch (semantic)
            {
                case VertexElementSemantic.Position:
                    return "POSITION";
                case VertexElementSemantic.Normal:
                    return "NORMAL";
                case VertexElementSemantic.TextureCoordinate:
                    return "TEXCOORD";
                case VertexElementSemantic.Color:
                    return "COLOR";
                default:
                    throw Illegal.Value<VertexElementSemantic>();
            }
        }

        private struct SemanticIndices
        {
            private int _position;
            private int _texCoord;
            private int _normal;
            private int _color;

            public static int GetAndIncrement(ref SemanticIndices si, VertexElementSemantic type)
            {
                switch (type)
                {
                    case VertexElementSemantic.Position:
                        return si._position++;
                    case VertexElementSemantic.TextureCoordinate:
                        return si._texCoord++;
                    case VertexElementSemantic.Normal:
                        return si._normal++;
                    case VertexElementSemantic.Color:
                        return si._color++;
                    default:
                        throw Illegal.Value<VertexElementSemantic>();
                }
            }
        }
    }
}
