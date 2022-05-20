using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;
using static Veldrid.Vulkan.VulkanUtil;

namespace Veldrid.Vulkan
{
    internal unsafe class VkResourceLayout : ResourceLayout
    {
        private readonly VkGraphicsDevice _gd;
        private readonly VkDescriptorSetLayout _dsl;
        private readonly VkDescriptorType[] _descriptorTypes;
        private bool _disposed;
        private string? _name;

        public VkDescriptorSetLayout DescriptorSetLayout => _dsl;
        public VkDescriptorType[] DescriptorTypes => _descriptorTypes;
        public DescriptorResourceCounts DescriptorResourceCounts { get; }
        public new int DynamicBufferCount { get; }

        public override bool IsDisposed => _disposed;

        public VkResourceLayout(VkGraphicsDevice gd, in ResourceLayoutDescription description)
            : base(description)
        {

            _gd = gd;
            ResourceLayoutElementDescription[] elements = description.Elements;
            _descriptorTypes = new VkDescriptorType[elements.Length];
            VkDescriptorSetLayoutBinding* bindings = stackalloc VkDescriptorSetLayoutBinding[elements.Length];
            VkDescriptorBindingFlags* bindless_flags = stackalloc VkDescriptorBindingFlags[elements.Length];
            uint uniformBufferCount = 0;
            uint sampledImageCount = 0;
            uint samplerCount = 0;
            uint storageBufferCount = 0;
            uint storageImageCount = 0;
            //Only the last element may have variable length
            for (uint i = 0; i < elements.Length; i++)
            {
                bindings[i].binding = i;
                bindings[i].descriptorCount = 1;
                VkDescriptorType descriptorType = VkFormats.VdToVkDescriptorType(elements[i].Kind, elements[i].Options);
                bindings[i].descriptorType = descriptorType;
                bindings[i].stageFlags = VkFormats.VdToVkShaderStages(elements[i].Stages);
                if ((elements[i].Options & ResourceLayoutElementOptions.DynamicBinding) != 0)
                {
                    DynamicBufferCount += 1;
                }
                _descriptorTypes[i] = descriptorType;
                if (description.LastElementParams && i == elements.Length - 1)
                {
                    bindings[i].descriptorCount = 1024;
                    bindless_flags[i] = VkDescriptorBindingFlags.VK_DESCRIPTOR_BINDING_PARTIALLY_BOUND_BIT | VkDescriptorBindingFlags.VK_DESCRIPTOR_BINDING_VARIABLE_DESCRIPTOR_COUNT_BIT | VkDescriptorBindingFlags.VK_DESCRIPTOR_BINDING_UPDATE_AFTER_BIND_BIT;
                }
                switch (descriptorType)
                {
                    case VkDescriptorType.VK_DESCRIPTOR_TYPE_SAMPLER:
                        samplerCount += bindings[i].descriptorCount;
                        break;
                    case VkDescriptorType.VK_DESCRIPTOR_TYPE_SAMPLED_IMAGE:
                        sampledImageCount += bindings[i].descriptorCount;
                        break;
                    case VkDescriptorType.VK_DESCRIPTOR_TYPE_STORAGE_IMAGE:
                        storageImageCount += bindings[i].descriptorCount;
                        break;
                    case VkDescriptorType.VK_DESCRIPTOR_TYPE_UNIFORM_BUFFER:
                        uniformBufferCount += bindings[i].descriptorCount;
                        break;
                    case VkDescriptorType.VK_DESCRIPTOR_TYPE_STORAGE_BUFFER:
                        storageBufferCount += bindings[i].descriptorCount;
                        break;
                }
            }

            DescriptorResourceCounts = new DescriptorResourceCounts(
                uniformBufferCount,
                sampledImageCount,
                samplerCount,
                storageBufferCount,
                storageImageCount);


            VkDescriptorSetLayoutCreateInfo dslCI = new()
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_DESCRIPTOR_SET_LAYOUT_CREATE_INFO,
                bindingCount = (uint)elements.Length,
                pBindings = bindings,
                flags = VkDescriptorSetLayoutCreateFlags.VK_DESCRIPTOR_SET_LAYOUT_CREATE_UPDATE_AFTER_BIND_POOL_BIT,
            };

            if (description.LastElementParams)
            {
                VkDescriptorSetLayoutBindingFlagsCreateInfo extendedInfo = new()
                {
                    sType = VkStructureType.VK_STRUCTURE_TYPE_DESCRIPTOR_SET_LAYOUT_BINDING_FLAGS_CREATE_INFO,
                    bindingCount = (uint)elements.Length,
                    pBindingFlags = bindless_flags,
                };
                dslCI.pNext = &extendedInfo;
            }

            VkDescriptorSetLayout dsl;
            VkResult result = vkCreateDescriptorSetLayout(_gd.Device, &dslCI, null, &dsl);
            CheckResult(result);
            _dsl = dsl;
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
            if (!_disposed)
            {
                _disposed = true;
                vkDestroyDescriptorSetLayout(_gd.Device, _dsl, null);
            }
        }
    }
}
