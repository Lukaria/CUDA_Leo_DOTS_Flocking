using Leopotam.EcsLite;
using Murmuration.ECS.Particle.Components;

namespace Murmuration.ECS.Particle.Systems
{
    public class SpatialHashSystem : IEcsInitSystem, IEcsRunSystem
    {
        private EcsFilter _filter;
        private SpatialHash _spatialHash;
        private EcsPool<PositionComponent> _positionPool;

        public void Init(IEcsSystems systems)
        {
            var world = systems.GetWorld();
            var shared = systems.GetShared<SharedData>();
            _spatialHash = shared.SpatialHash;
            _positionPool = world.GetPool<PositionComponent>();
            _filter = world.Filter<PositionComponent>().Inc<RotationComponent>().Inc<VelocityComponent>().End();
            _spatialHash.Initialize(shared.ParticlesCount);
        }

        public void Run(IEcsSystems systems)
        {
            _spatialHash.Clear();
            foreach (var entity in _filter)
            {
                _spatialHash.Insert(entity, _positionPool.Get(entity).Position);
            }
        }
    }
}