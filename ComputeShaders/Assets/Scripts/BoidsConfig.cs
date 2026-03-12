using System;
using CustomEditor;
using Unity.Mathematics;
using UnityEngine;

namespace Scripts
{
    [Serializable]
    public class BoidsConfig
    {
        [Header("Particles")]
        [Header("RuntimeReadonly")]
        [RuntimeReadonly] public int particlesCount = 10000;
        [RuntimeReadonly] public float sphereRadius;
        [RuntimeReadonly] public float3 boundsSize;
        public float particleScale = 1f;
        [Header("Cohesion")]
        public float cohesionRadius = 1f;
        public float cohesionWeight = 1.5f;
        [Header("Separation")]
        public float separationRadius = 3f;
        public float separationWeight = 2f;
        [Header("Alignment")]
        public float alignmentRadius = 1f;
        public float alignmentWeight = 1f;
        [Header("Velocity")]
        public float minSpeed = 1f;
        public float maxSpeed = 1f;
        [Header("Rotation")]
        public float rotationSpeed = 1f;
    
        [Header("Rendering")]
        public Mesh particleMesh;
        public Material particleMaterial;
    }
}