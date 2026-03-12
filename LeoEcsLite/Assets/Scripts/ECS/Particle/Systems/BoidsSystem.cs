using Leopotam.EcsLite;
using Leopotam.EcsLite.Threads;
using Murmuration.ECS.Particle.Components;
using Murmuration.ECS.Particle.Threads;
using UnityEngine;

namespace Murmuration.ECS.Particle.Systems
{
    public class BoidsSystem : EcsThreadSystem<BoidsSystemThread, PositionComponent, RotationComponent, VelocityComponent>
    {
        private int _chunkSize;
        private bool _chunkSizeInitialized;
        protected override int GetChunkSize(IEcsSystems systems) => _chunkSize;

        protected override EcsFilter GetFilter(EcsWorld world) =>
            world.Filter<PositionComponent>().Inc<RotationComponent>().Inc<VelocityComponent>().End();

        protected override EcsWorld GetWorld(IEcsSystems systems) => systems.GetWorld();

        protected override void SetData(IEcsSystems systems, ref BoidsSystemThread thread)
        {
            var shared = systems.GetShared<SharedData>();
            shared.DeltaTime = Time.deltaTime;
            thread.SharedData = shared;
            
            if (_chunkSizeInitialized) return;
            _chunkSize = Mathf.CeilToInt((float)shared.ParticlesCount / SystemInfo.processorCount);
            _chunkSizeInitialized = true;
        }
    }
}