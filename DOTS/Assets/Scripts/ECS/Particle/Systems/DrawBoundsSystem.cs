using Scripts;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace Murmuration.ECS.Particle.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class DrawBoundsSystem : SystemBase
    {
        private Material _lineMaterial;
        private static readonly Color Color = Color.green;
        
        protected override void OnCreate()
        {            
            RequireForUpdate<GameConfig>();
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
            _lineMaterial = new Material(shader);
        }

        protected override void OnUpdate()
        {
            var gameConfig = SystemAPI.GetSingleton<GameConfig>();
            var mesh = BoundsRenderer.CreateWireCubeMesh(gameConfig.BoundsSize/2);
            if (mesh == null || _lineMaterial == null) return;

            _lineMaterial.color = Color;

            Graphics.DrawMesh(mesh, Matrix4x4.identity, _lineMaterial, 0);
        }
    }
}