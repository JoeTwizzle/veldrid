using System.Diagnostics.CodeAnalysis;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.VkFormat;

namespace Veldrid.Vulkan
{
    internal static partial class VkFormats
    {

        internal static VkFormat VdToVkPixelFormat(PixelFormat format, bool toDepthFormat = false)
        {
            return format switch
            {
                PixelFormat.R8_UNorm => VK_FORMAT_R8_UNORM,
                PixelFormat.R8_SNorm => VK_FORMAT_R8_SNORM,
                PixelFormat.R8_UInt => VK_FORMAT_R8_UINT,
                PixelFormat.R8_SInt => VK_FORMAT_R8_SINT,
                PixelFormat.R16_UNorm => toDepthFormat ? VK_FORMAT_D16_UNORM : VK_FORMAT_R16_UNORM,
                PixelFormat.R16_SNorm => VK_FORMAT_R16_SNORM,
                PixelFormat.R16_UInt => VK_FORMAT_R16_UINT,
                PixelFormat.R16_SInt => VK_FORMAT_R16_SINT,
                PixelFormat.R16_Float => VK_FORMAT_R16_SFLOAT,
                PixelFormat.R32_UInt => VK_FORMAT_R32_UINT,
                PixelFormat.R32_SInt => VK_FORMAT_R32_SINT,
                PixelFormat.R32_Float => toDepthFormat ? VK_FORMAT_D32_SFLOAT : VK_FORMAT_R32_SFLOAT,
                PixelFormat.R8_G8_UNorm => VK_FORMAT_R8G8_UNORM,
                PixelFormat.R8_G8_SNorm => VK_FORMAT_R8G8_SNORM,
                PixelFormat.R8_G8_UInt => VK_FORMAT_R8G8_UINT,
                PixelFormat.R8_G8_SInt => VK_FORMAT_R8G8_SINT,
                PixelFormat.R16_G16_UNorm => VK_FORMAT_R16G16_UNORM,
                PixelFormat.R16_G16_SNorm => VK_FORMAT_R16G16_SNORM,
                PixelFormat.R16_G16_UInt => VK_FORMAT_R16G16_UINT,
                PixelFormat.R16_G16_SInt => VK_FORMAT_R16G16_SINT,
                PixelFormat.R16_G16_Float => VK_FORMAT_R16G16B16A16_SFLOAT,
                PixelFormat.R32_G32_UInt => VK_FORMAT_R32G32_UINT,
                PixelFormat.R32_G32_SInt => VK_FORMAT_R32G32_SINT,
                PixelFormat.R32_G32_Float => VK_FORMAT_R32G32B32A32_SFLOAT,
                PixelFormat.R8_G8_B8_A8_UNorm => VK_FORMAT_R8G8B8A8_UNORM,
                PixelFormat.R8_G8_B8_A8_UNorm_SRgb => VK_FORMAT_R8G8B8A8_SRGB,
                PixelFormat.B8_G8_R8_A8_UNorm => VK_FORMAT_B8G8R8A8_UNORM,
                PixelFormat.B8_G8_R8_A8_UNorm_SRgb => VK_FORMAT_B8G8R8A8_SRGB,
                PixelFormat.R8_G8_B8_A8_SNorm => VK_FORMAT_R8G8B8A8_SNORM,
                PixelFormat.R8_G8_B8_A8_UInt => VK_FORMAT_R8G8B8A8_UINT,
                PixelFormat.R8_G8_B8_A8_SInt => VK_FORMAT_R8G8B8A8_SINT,
                PixelFormat.R16_G16_B16_A16_UNorm => VK_FORMAT_R16G16B16A16_UNORM,
                PixelFormat.R16_G16_B16_A16_SNorm => VK_FORMAT_R16G16B16A16_SNORM,
                PixelFormat.R16_G16_B16_A16_UInt => VK_FORMAT_R16G16B16A16_UINT,
                PixelFormat.R16_G16_B16_A16_SInt => VK_FORMAT_R16G16B16A16_SINT,
                PixelFormat.R16_G16_B16_A16_Float => VK_FORMAT_R16G16B16A16_SFLOAT,
                PixelFormat.R32_G32_B32_A32_UInt => VK_FORMAT_R32G32B32A32_UINT,
                PixelFormat.R32_G32_B32_A32_SInt => VK_FORMAT_R32G32B32A32_SINT,
                PixelFormat.R32_G32_B32_A32_Float => VK_FORMAT_R32G32B32A32_SFLOAT,
                PixelFormat.BC1_Rgb_UNorm => VK_FORMAT_BC1_RGB_UNORM_BLOCK,
                PixelFormat.BC1_Rgb_UNorm_SRgb => VK_FORMAT_BC1_RGB_SRGB_BLOCK,
                PixelFormat.BC1_Rgba_UNorm => VK_FORMAT_BC1_RGBA_UNORM_BLOCK,
                PixelFormat.BC1_Rgba_UNorm_SRgb => VK_FORMAT_BC1_RGBA_SRGB_BLOCK,
                PixelFormat.BC2_UNorm => VK_FORMAT_BC2_UNORM_BLOCK,
                PixelFormat.BC2_UNorm_SRgb => VK_FORMAT_BC2_SRGB_BLOCK,
                PixelFormat.BC3_UNorm => VK_FORMAT_BC3_UNORM_BLOCK,
                PixelFormat.BC3_UNorm_SRgb => VK_FORMAT_BC3_SRGB_BLOCK,
                PixelFormat.BC4_UNorm => VK_FORMAT_BC4_UNORM_BLOCK,
                PixelFormat.BC4_SNorm => VK_FORMAT_BC4_SNORM_BLOCK,
                PixelFormat.BC5_UNorm => VK_FORMAT_BC5_UNORM_BLOCK,
                PixelFormat.BC5_SNorm => VK_FORMAT_BC5_SNORM_BLOCK,
                PixelFormat.BC6_SFloat => VK_FORMAT_BC6H_SFLOAT_BLOCK,
                PixelFormat.BC6_UFloat => VK_FORMAT_BC6H_UFLOAT_BLOCK,
                PixelFormat.BC7_UNorm => VK_FORMAT_BC7_UNORM_BLOCK,
                PixelFormat.BC7_UNorm_SRgb => VK_FORMAT_BC7_SRGB_BLOCK,
                PixelFormat.ETC2_R8_G8_B8_UNorm => VK_FORMAT_ETC2_R8G8B8_UNORM_BLOCK,
                PixelFormat.ETC2_R8_G8_B8_A1_UNorm => VK_FORMAT_ETC2_R8G8B8A1_UNORM_BLOCK,
                PixelFormat.ETC2_R8_G8_B8_A8_UNorm => VK_FORMAT_ETC2_R8G8B8A8_UNORM_BLOCK,
                PixelFormat.D32_Float_S8_UInt => VK_FORMAT_D32_SFLOAT_S8_UINT,
                PixelFormat.D24_UNorm_S8_UInt => VK_FORMAT_D24_UNORM_S8_UINT,
                PixelFormat.R10_G10_B10_A2_UNorm => VK_FORMAT_A2B10G10R10_UNORM_PACK32,
                PixelFormat.R10_G10_B10_A2_UInt => VK_FORMAT_A2B10G10R10_UINT_PACK32,
                PixelFormat.R11_G11_B10_Float => VK_FORMAT_B10G11R11_UFLOAT_PACK32,
                _ => throw new VeldridException($"Invalid {nameof(PixelFormat)}: {format}"),
            };
        }
    }
}
