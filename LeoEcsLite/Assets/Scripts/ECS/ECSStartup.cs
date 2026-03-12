using Leopotam.EcsLite;
using Murmuration.ECS.Particle.Systems;

namespace Murmuration.ECS
{
    public class ECSStartup
    {
        private EcsWorld _world;
        private IEcsSystems _systems;

        public void Initialize(SharedData data)
        {
            _world = new EcsWorld();
            _systems = new EcsSystems(_world, data);

            AddSystems();
        
            _systems.Init();
        }
        
        
        public void Update()
        {
            _systems.Run();
        }
    
        private void AddSystems()
        {
            _systems
                .Add(new SpawnSystem())
                .Add(new SpatialHashSystem())
                .Add(new BoidsSystem())
                .Add(new RenderSystem())
                ;
        }
        
        public void OnDestroy()
        {
            if(_systems == null) return;
        
            _systems.Destroy();
            _systems = null;
            _world.Destroy();
            _world = null;
        }
    }
}