using System;
using CustomEditor;
using Murmuration.ECS.Particle;
using Unity.Mathematics;
using UnityEngine;

namespace Murmuration
{
    [Serializable]
    public class SharedData
    {
        [Header("Particles")] 
        [Header("RuntimeReadonly")] 
        [RuntimeReadonly] public int ParticlesCount = 1000;
        [RuntimeReadonly] public float3 BoundsSize = 10f;
        [Space]
        public Mesh ParticlesMesh;
        public Material ParticleMaterial;
        public float ParticleScale = 1f;
        [Header("Cohesion")]
        public float CohesionRadius = 1f;
        public float CohesionWeight = 1.5f;
        [Header("Separation")]
        public float SeparationRadius = 3f;
        public float SeparationWeight = 2f;
        [Header("Alignment")]
        public float AlignmentRadius = 1f;
        public float AlignmentWeight = 1f;
        [Header("Velocity")]
        public float MinSpeed = 1f;
        public float MaxSpeed = 1f;
        [Header("Rotation")]
        public float RotationSpeed = 1f;
        
        [NonSerialized]  public SpatialHash SpatialHash = new();
        [NonSerialized] public float DeltaTime;

        //todo optimize
        public float CellSize => math.max(
            math.max(CohesionRadius, SeparationRadius),
            AlignmentRadius
        );

        [NonSerialized] public ParticleData[] ParticleData;
    }
}