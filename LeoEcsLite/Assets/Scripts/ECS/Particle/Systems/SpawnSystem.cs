using Leopotam.EcsLite;
using Murmuration.ECS.Particle.Components;
using UnityEngine;

namespace Murmuration.ECS.Particle.Systems
{
    public class SpawnSystem : IEcsInitSystem
    {
        public void Init(IEcsSystems systems)
        {
            var shared = systems.GetShared<SharedData>();
            var world = systems.GetWorld();
            var positionPool = world.GetPool<PositionComponent>();
            var velocityPool = world.GetPool<VelocityComponent>();
            var rotationPool = world.GetPool<RotationComponent>();
            var sphereRadius = shared.BoundsSize.x * 0.5f;

            for (var i = 0; i < shared.ParticlesCount; ++i)
            {
                var entity = world.NewEntity();
                ref var objPosition = ref positionPool.Add(entity);
                objPosition.Position = Random.insideUnitSphere * sphereRadius;
                ref var velocity = ref velocityPool.Add(entity);
                velocity.Velocity = Vector3.zero;
                ref var rotation = ref rotationPool.Add(entity);
                rotation.Rotation = Random.rotationUniform;
            }
        }
    }
}