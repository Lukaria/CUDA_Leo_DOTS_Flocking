using Unity.Entities;
using Unity.Mathematics;

namespace Murmuration.ECS
{
    public struct GameConfig : IComponentData
    {
        public Entity Prototype;
        public int ParticlesCount;
        public float3 BoundsSize;
        public float ParticleScale;
        
        public float CohesionRadius;
        public float CohesionWeight;
        
        public float SeparationRadius;

        public float SeparationWeight;
        
        public float AlignmentRadius;

        public float AlignmentWeight;
        
        public float MinSpeed;
        public float MaxSpeed;
        
        public float RotationSpeed;
        
        public float CellSize => math.max(
            math.max(CohesionRadius, SeparationRadius),
            AlignmentRadius
        );
    }
}