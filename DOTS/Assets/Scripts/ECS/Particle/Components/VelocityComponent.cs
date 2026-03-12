using Unity.Entities;
using Unity.Mathematics;

namespace Murmuration.ECS.Particle.Components
{
    public struct VelocityComponent : IComponentData
    {
        public float3 Velocity;
    }
}