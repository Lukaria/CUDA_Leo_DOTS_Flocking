using Leopotam.EcsLite;
using UnityEngine;
using UnityEngine.Rendering;

namespace Murmuration.ECS.Particle.Systems
{
    public class RenderSystem : IEcsInitSystem, IEcsRunSystem, IEcsDestroySystem
    {
        private ComputeBuffer _particleBuffer;
        private ParticleData[] _particleArray;
        private int _instanceCount;
        private Mesh _instanceMesh;
        private Bounds _bounds;
        private Material _instanceMaterial;
        private static readonly int BufferNameId = Shader.PropertyToID("_ParticleBuffer");
        private static readonly int MinBoundID = Shader.PropertyToID("_MinBounds");
        private static readonly int MaxBoundID = Shader.PropertyToID("_MaxBounds");
        private const int SubMeshIndex = 0;

        public void Init(IEcsSystems systems)
        {
            var sharedData = systems.GetShared<SharedData>();

            _instanceCount = sharedData.ParticlesCount;
            _particleBuffer = new ComputeBuffer(sharedData.ParticlesCount, ParticleDataInfo.ParticleDataSize,
                ComputeBufferType.Structured);
            _bounds = new Bounds(Vector3.zero, sharedData.BoundsSize);
            _instanceMesh = sharedData.ParticlesMesh;
            _instanceMaterial = sharedData.ParticleMaterial;
            _particleArray = sharedData.ParticleData;

            _instanceMaterial.SetBuffer(BufferNameId, _particleBuffer);
            
            _instanceMaterial.SetFloat(MinBoundID, -sharedData.BoundsSize.x * 0.5f);
            _instanceMaterial.SetFloat(MaxBoundID, sharedData.BoundsSize.x * 0.5f);
            
            var renderParams = new RenderParams(_instanceMaterial);
            renderParams.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderParams.receiveShadows = false;
            
        }

        public void Run(IEcsSystems systems)
        {
            _particleBuffer?.SetData(_particleArray, 0, 0, _instanceCount);
            Graphics.DrawMeshInstancedProcedural(
                _instanceMesh,
                SubMeshIndex,
                _instanceMaterial,
                _bounds,
                _instanceCount,
                null,
                ShadowCastingMode.Off,
                false
            );
        }

        public void Destroy(IEcsSystems systems)
        {
            _particleBuffer?.Release();
        }
    }
}