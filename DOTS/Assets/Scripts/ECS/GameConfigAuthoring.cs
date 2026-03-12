using CustomEditor;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

namespace Murmuration.ECS
{
    public class GameConfigAuthoring : MonoBehaviour
    {
        [Header("Particle")]
        [Header("RuntimeReadonly")]
        [RuntimeReadonly] public GameObject prefab;
        [RuntimeReadonly] public int particlesCount = 1000;
        [RuntimeReadonly] public float3 boundsSize = 10;
        [RuntimeReadonly]public float particleScale = 10;
        [Header("Cohesion")]
        public float cohesionRadius = 3;
        public float cohesionWeight = 2;
        [Header("Separation")] 
        public float separationRadius = 2;
        public float separationWeight = 7;
        [Header("Alignment")] 
        public float alignmentRadius = 1;
        public float alignmentWeight = 1;
        [Header("Velocity")]
        public float minSpeed = 1;
        public float maxSpeed = 3;
        [Header("Rotation")]
        public float rotationSpeed = 1;
        
        class Baker : Baker<GameConfigAuthoring>
        {
            public override void Bake(GameConfigAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                UpdateGameObjectShaderMaterial(authoring);
                
                AddComponent(entity, new GameConfig
                {
                    Prototype = GetEntity(authoring.prefab, TransformUsageFlags.Dynamic),
                    ParticlesCount =  authoring.particlesCount,
                    BoundsSize = authoring.boundsSize,
                    ParticleScale = authoring.particleScale,
                    
                    CohesionRadius = authoring.cohesionRadius,
                    CohesionWeight = authoring.cohesionWeight,
                    
                    SeparationRadius = authoring.separationRadius,
                    SeparationWeight = authoring.separationWeight,
                    
                    AlignmentRadius = authoring.alignmentRadius,
                    AlignmentWeight = authoring.alignmentWeight,
                    
                    MinSpeed = authoring.minSpeed,
                    MaxSpeed = authoring.maxSpeed,
                    
                    RotationSpeed = authoring.rotationSpeed
                });
            }        

            private void UpdateGameObjectShaderMaterial(GameConfigAuthoring authoring)
            {
                var material = authoring.prefab.GetComponent<Renderer>().sharedMaterial;
                var xBoundSize = authoring.boundsSize.x;
                material.SetFloat("_MinBounds", -xBoundSize * 0.5f);
                material.SetFloat("_MaxBounds", xBoundSize * 0.5f);
            }
        }

    }
}