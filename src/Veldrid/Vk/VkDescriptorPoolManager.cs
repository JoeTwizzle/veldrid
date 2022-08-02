using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace Veldrid.Vulkan
{
    internal sealed class VkDescriptorPoolManager
    {
        private readonly VkGraphicsDevice _gd;
        private readonly List<PoolInfo> _pools = new();
        private readonly object _lock = new();

        public VkDescriptorPoolManager(VkGraphicsDevice gd)
        {
            _gd = gd;
            _pools.Add(CreateNewPool(default));
        }

        public unsafe DescriptorAllocationToken AllocateBindless(DescriptorResourceCounts counts, VkDescriptorSetLayout setLayout)
        {
            lock (_lock)
            {
                uint total = counts.Total;
                uint max_binding = 1024;
                VkDescriptorSetVariableDescriptorCountAllocateInfo countInfo = new()
                {
                    sType = VkStructureType.VK_STRUCTURE_TYPE_DESCRIPTOR_SET_VARIABLE_DESCRIPTOR_COUNT_ALLOCATE_INFO,
                    descriptorSetCount = 1,
                    pDescriptorCounts = &max_binding
                };
                VkDescriptorPool pool = GetPool(counts);
                VkDescriptorSetAllocateInfo dsAI = new()
                {
                    sType = VkStructureType.VK_STRUCTURE_TYPE_DESCRIPTOR_SET_ALLOCATE_INFO,
                    descriptorSetCount = 1,
                    pSetLayouts = &setLayout,
                    descriptorPool = pool,
                    pNext = &countInfo
                };

                VkDescriptorSet set;
                VkResult result = vkAllocateDescriptorSets(_gd.Device, &dsAI, &set);
                VulkanUtil.CheckResult(result);

                return new DescriptorAllocationToken(set, pool);
            }
        }

        public unsafe DescriptorAllocationToken Allocate(DescriptorResourceCounts counts, VkDescriptorSetLayout setLayout)
        {
            lock (_lock)
            {
                VkDescriptorPool pool = GetPool(counts);
                VkDescriptorSetAllocateInfo dsAI = new()
                {
                    sType = VkStructureType.VK_STRUCTURE_TYPE_DESCRIPTOR_SET_ALLOCATE_INFO,
                    descriptorSetCount = 1,
                    pSetLayouts = &setLayout,
                    descriptorPool = pool,
                };

                VkDescriptorSet set;
                VkResult result = vkAllocateDescriptorSets(_gd.Device, &dsAI, &set);
                VulkanUtil.CheckResult(result);

                return new DescriptorAllocationToken(set, pool);
            }
        }

        public void Free(DescriptorAllocationToken token, DescriptorResourceCounts counts)
        {
            lock (_lock)
            {
                foreach (PoolInfo poolInfo in _pools)
                {
                    if (poolInfo.Pool == token.Pool)
                    {
                        poolInfo.Free(_gd.Device, token, counts);
                    }
                }
            }
        }

        private VkDescriptorPool GetPool(DescriptorResourceCounts counts)
        {
            foreach (PoolInfo poolInfo in _pools)
            {
                if (poolInfo.Allocate(counts))
                {
                    return poolInfo.Pool;
                }
            }

            PoolInfo newPool = CreateNewPool(counts);
            _pools.Add(newPool);
            bool result = newPool.Allocate(counts);
            Debug.Assert(result);
            return newPool.Pool;
        }

        private unsafe PoolInfo CreateNewPool(DescriptorResourceCounts counts)
        {
            uint uniformBufferCount = System.Math.Max(32, BitOperations.RoundUpToPowerOf2(counts.UniformBufferCount));
            uint sampledImageCount = System.Math.Max(32, BitOperations.RoundUpToPowerOf2(counts.SampledImageCount));
            uint samplerCount = System.Math.Max(32, BitOperations.RoundUpToPowerOf2(counts.SamplerCount));
            uint storageBufferCount = System.Math.Max(32, BitOperations.RoundUpToPowerOf2(counts.StorageBufferCount));
            uint storageImageCount = System.Math.Max(32, BitOperations.RoundUpToPowerOf2(counts.StorageImageCount));
            uint poolSizeCount = 7;
            VkDescriptorPoolSize* sizes = stackalloc VkDescriptorPoolSize[(int)poolSizeCount];
            sizes[0].type = VkDescriptorType.VK_DESCRIPTOR_TYPE_UNIFORM_BUFFER;
            sizes[0].descriptorCount = uniformBufferCount;
            sizes[1].type = VkDescriptorType.VK_DESCRIPTOR_TYPE_SAMPLED_IMAGE;
            sizes[1].descriptorCount = sampledImageCount;
            sizes[2].type = VkDescriptorType.VK_DESCRIPTOR_TYPE_SAMPLER;
            sizes[2].descriptorCount = samplerCount;
            sizes[3].type = VkDescriptorType.VK_DESCRIPTOR_TYPE_STORAGE_BUFFER;
            sizes[3].descriptorCount = storageBufferCount;
            sizes[4].type = VkDescriptorType.VK_DESCRIPTOR_TYPE_STORAGE_IMAGE;
            sizes[4].descriptorCount = storageImageCount;
            sizes[5].type = VkDescriptorType.VK_DESCRIPTOR_TYPE_UNIFORM_BUFFER_DYNAMIC;
            sizes[5].descriptorCount = uniformBufferCount;
            sizes[6].type = VkDescriptorType.VK_DESCRIPTOR_TYPE_STORAGE_BUFFER_DYNAMIC;
            sizes[6].descriptorCount = storageBufferCount;
            //Not supported by veldrid
            //sizes[7].type = VkDescriptorType.VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER;
            //sizes[7].descriptorCount = 0;
            uint descriptorCount = 0;
            for (int i = 0; i < poolSizeCount; i++)
            {
                descriptorCount += sizes[i].descriptorCount;
            }
            uint totalSets = descriptorCount * poolSizeCount;

            VkDescriptorPoolCreateInfo poolCI = new()
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_DESCRIPTOR_POOL_CREATE_INFO,
                flags = VkDescriptorPoolCreateFlags.VK_DESCRIPTOR_POOL_CREATE_FREE_DESCRIPTOR_SET_BIT | VkDescriptorPoolCreateFlags.VK_DESCRIPTOR_POOL_CREATE_UPDATE_AFTER_BIND_BIT,
                maxSets = totalSets,
                pPoolSizes = sizes,
                poolSizeCount = poolSizeCount
            };

            VkDescriptorPool descriptorPool;
            VkResult result = vkCreateDescriptorPool(_gd.Device, &poolCI, null, &descriptorPool);
            VulkanUtil.CheckResult(result);

            return new PoolInfo(descriptorPool, totalSets, uniformBufferCount, sampledImageCount, samplerCount, storageBufferCount, storageImageCount);
        }

        internal unsafe void DestroyAll()
        {
            foreach (PoolInfo poolInfo in _pools)
            {
                vkDestroyDescriptorPool(_gd.Device, poolInfo.Pool, null);
            }
        }

        private sealed class PoolInfo
        {
            public readonly VkDescriptorPool Pool;

            public uint RemainingSets;

            public uint UniformBufferCount;
            public uint SampledImageCount;
            public uint SamplerCount;
            public uint StorageBufferCount;
            public uint StorageImageCount;

            public PoolInfo(VkDescriptorPool pool, uint totalSets, uint descriptorCount)
            {
                Pool = pool;
                RemainingSets = totalSets;
                UniformBufferCount = descriptorCount;
                SampledImageCount = descriptorCount;
                SamplerCount = descriptorCount;
                StorageBufferCount = descriptorCount;
                StorageImageCount = descriptorCount;
            }

            public PoolInfo(VkDescriptorPool pool, uint totalSets, uint uniformBufferCount, uint sampledImageCount, uint samplerCount, uint storageBufferCount, uint storageImageCount)
            {
                Pool = pool;
                RemainingSets = totalSets;
                UniformBufferCount = uniformBufferCount;
                SampledImageCount = sampledImageCount;
                SamplerCount = samplerCount;
                StorageBufferCount = storageBufferCount;
                StorageImageCount = storageImageCount;
            }

            internal bool Allocate(DescriptorResourceCounts counts)
            {
                if (RemainingSets > 0
                    && UniformBufferCount >= counts.UniformBufferCount
                    && SampledImageCount >= counts.SampledImageCount
                    && SamplerCount >= counts.SamplerCount
                    && StorageBufferCount >= counts.SamplerCount
                    && StorageImageCount >= counts.StorageImageCount)
                {
                    RemainingSets -= 1;
                    UniformBufferCount -= counts.UniformBufferCount;
                    SampledImageCount -= counts.SampledImageCount;
                    SamplerCount -= counts.SamplerCount;
                    StorageBufferCount -= counts.StorageBufferCount;
                    StorageImageCount -= counts.StorageImageCount;
                    return true;
                }
                else
                {
                    return false;
                }
            }

            internal unsafe void Free(VkDevice device, DescriptorAllocationToken token, DescriptorResourceCounts counts)
            {
                VkDescriptorSet set = token.Set;
                vkFreeDescriptorSets(device, Pool, 1, &set);

                RemainingSets += 1;

                UniformBufferCount += counts.UniformBufferCount;
                SampledImageCount += counts.SampledImageCount;
                SamplerCount += counts.SamplerCount;
                StorageBufferCount += counts.StorageBufferCount;
                StorageImageCount += counts.StorageImageCount;
            }
        }
    }

    internal struct DescriptorAllocationToken
    {
        public readonly VkDescriptorSet Set;
        public readonly VkDescriptorPool Pool;

        public DescriptorAllocationToken(VkDescriptorSet set, VkDescriptorPool pool)
        {
            Set = set;
            Pool = pool;
        }
    }
}
