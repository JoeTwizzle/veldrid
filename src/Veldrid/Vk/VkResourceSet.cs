using System.Collections.Generic;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace Veldrid.Vulkan
{
    internal sealed unsafe class VkResourceSet : ResourceSet, IResourceRefCountTarget
    {
        private readonly VkGraphicsDevice _gd;
        private readonly DescriptorResourceCounts _descriptorCounts;
        private readonly DescriptorAllocationToken _descriptorAllocationToken;
        private readonly List<ResourceRefCount> _refCounts = new();
        private string? _name;

        public VkDescriptorSet DescriptorSet => _descriptorAllocationToken.Set;

        private readonly List<VkTexture> _sampledTextures = new();
        public List<VkTexture> SampledTextures => _sampledTextures;
        private readonly List<VkTexture> _storageImages = new();
        public List<VkTexture> StorageTextures => _storageImages;

        public ResourceRefCount RefCount { get; }
        public List<ResourceRefCount> RefCounts => _refCounts;

        public override bool IsDisposed => RefCount.IsDisposed;

        public VkResourceSet(VkGraphicsDevice gd, in ResourceSetDescription description)
            : base(description)
        {
            _gd = gd;
            RefCount = new ResourceRefCount(this);
            VkResourceLayout vkLayout = Util.AssertSubtype<ResourceLayout, VkResourceLayout>(description.Layout);

            VkDescriptorSetLayout dsl = vkLayout.DescriptorSetLayout;
            _descriptorCounts = vkLayout.DescriptorResourceCounts;
            if (vkLayout.Description.LastElementParams)
            {
                _descriptorAllocationToken = _gd.DescriptorPoolManager.AllocateBindless(_descriptorCounts, dsl);
            }
            else
            {
                _descriptorAllocationToken = _gd.DescriptorPoolManager.Allocate(_descriptorCounts, dsl);
            }

            int descriptorWriteCount = vkLayout.Description.Elements.Length;
            BindableResource[] boundResources = description.BoundResources;
            uint resourceCounts = (uint)boundResources.Length;
            VkWriteDescriptorSet* descriptorWrites = stackalloc VkWriteDescriptorSet[descriptorWriteCount];
            VkDescriptorBufferInfo* bufferInfos = stackalloc VkDescriptorBufferInfo[(int)resourceCounts];
            VkDescriptorImageInfo* imageInfos = stackalloc VkDescriptorImageInfo[(int)resourceCounts];
            for (int i = 0; i < descriptorWriteCount; i++)
            {
                VkDescriptorType type;

                type = vkLayout.DescriptorTypes[i];
                descriptorWrites[i] = new VkWriteDescriptorSet()
                {
                    sType = VkStructureType.VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET,
                    descriptorCount = 1,
                    descriptorType = type,
                    dstBinding = (uint)System.Math.Min(i, vkLayout.Description.Elements.Length - 1),
                    dstSet = _descriptorAllocationToken.Set
                };
                if (type == VkDescriptorType.VK_DESCRIPTOR_TYPE_UNIFORM_BUFFER ||
                type == VkDescriptorType.VK_DESCRIPTOR_TYPE_UNIFORM_BUFFER_DYNAMIC ||
                type == VkDescriptorType.VK_DESCRIPTOR_TYPE_STORAGE_BUFFER ||
                type == VkDescriptorType.VK_DESCRIPTOR_TYPE_STORAGE_BUFFER_DYNAMIC)
                {
                    descriptorWrites[i].pBufferInfo = &bufferInfos[i];
                }
                else if (type == VkDescriptorType.VK_DESCRIPTOR_TYPE_SAMPLED_IMAGE)
                {
                    descriptorWrites[i].pImageInfo = &imageInfos[i];
                }
                else if (type == VkDescriptorType.VK_DESCRIPTOR_TYPE_STORAGE_IMAGE)
                {
                    descriptorWrites[i].pImageInfo = &imageInfos[i];
                }
                else if (type == VkDescriptorType.VK_DESCRIPTOR_TYPE_SAMPLER)
                {
                    descriptorWrites[i].pImageInfo = &imageInfos[i];
                }
            }
            for (int i = 0; i < resourceCounts; i++)
            {
                VkDescriptorType type;

                int boundedIndex = System.Math.Min(i, descriptorWriteCount - 1);
                type = vkLayout.DescriptorTypes[boundedIndex];

                if (type == VkDescriptorType.VK_DESCRIPTOR_TYPE_UNIFORM_BUFFER ||
                        type == VkDescriptorType.VK_DESCRIPTOR_TYPE_UNIFORM_BUFFER_DYNAMIC ||
                        type == VkDescriptorType.VK_DESCRIPTOR_TYPE_STORAGE_BUFFER ||
                        type == VkDescriptorType.VK_DESCRIPTOR_TYPE_STORAGE_BUFFER_DYNAMIC)
                {
                    DeviceBufferRange range = Util.GetBufferRange(boundResources[i], 0);
                    VkBuffer rangedVkBuffer = Util.AssertSubtype<DeviceBuffer, VkBuffer>(range.Buffer);
                    bufferInfos[i] = new VkDescriptorBufferInfo()
                    {
                        buffer = rangedVkBuffer.DeviceBuffer,
                        offset = range.Offset,
                        range = range.SizeInBytes
                    };
                    _refCounts.Add(rangedVkBuffer.RefCount);
                }
                else if (type == VkDescriptorType.VK_DESCRIPTOR_TYPE_SAMPLED_IMAGE)
                {
                    TextureView texView = Util.GetTextureView(_gd, boundResources[i]);
                    VkTextureView vkTexView = Util.AssertSubtype<TextureView, VkTextureView>(texView);
                    imageInfos[i] = new VkDescriptorImageInfo()
                    {
                        imageView = vkTexView.ImageView,
                        imageLayout = VkImageLayout.VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL
                    };
                    _sampledTextures.Add(Util.AssertSubtype<Texture, VkTexture>(texView.Target));
                    _refCounts.Add(vkTexView.RefCount);
                }
                else if (type == VkDescriptorType.VK_DESCRIPTOR_TYPE_STORAGE_IMAGE)
                {
                    TextureView texView = Util.GetTextureView(_gd, boundResources[i]);
                    VkTextureView vkTexView = Util.AssertSubtype<TextureView, VkTextureView>(texView);
                    imageInfos[i] = new VkDescriptorImageInfo()
                    {
                        imageView = vkTexView.ImageView,
                        imageLayout = VkImageLayout.VK_IMAGE_LAYOUT_GENERAL
                    };
                    _storageImages.Add(Util.AssertSubtype<Texture, VkTexture>(texView.Target));
                    _refCounts.Add(vkTexView.RefCount);
                }
                else if (type == VkDescriptorType.VK_DESCRIPTOR_TYPE_SAMPLER)
                {
                    VkSampler sampler = Util.AssertSubtype<BindableResource, VkSampler>(boundResources[i]);
                    imageInfos[i] = new VkDescriptorImageInfo() { sampler = sampler.DeviceSampler };
                    _refCounts.Add(sampler.RefCount);
                }
            }

            //Change the last descriptorWrites instance to reflect arrayness
            if (vkLayout.Description.LastElementParams)
            {
                descriptorWrites[descriptorWriteCount - 1].descriptorCount = (uint)(resourceCounts - descriptorWriteCount) + 1;
            }

            vkUpdateDescriptorSets(_gd.Device, (uint)descriptorWriteCount, descriptorWrites, 0, null);
        }

        public override string? Name
        {
            get => _name;
            set
            {
                _name = value;
                _gd.SetResourceName(this, value);
            }
        }

        public override void Dispose()
        {
            RefCount.DecrementDispose();
        }

        void IResourceRefCountTarget.RefZeroed()
        {
            _gd.DescriptorPoolManager.Free(_descriptorAllocationToken, _descriptorCounts);
        }
    }
}
