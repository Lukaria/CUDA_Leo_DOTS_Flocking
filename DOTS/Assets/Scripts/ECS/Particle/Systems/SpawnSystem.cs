using Murmuration.ECS.Particle.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;

namespace Murmuration.ECS.Particle.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct SpawnSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameConfig>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Enabled = false;
            var config = SystemAPI.GetSingleton<GameConfig>();

            var workerThreadCount = Unity.Jobs.LowLevel.Unsafe.JobsUtility.MaxJobThreadCount;
            var randomArray = new NativeArray<Unity.Mathematics.Random>(
                workerThreadCount,
                Allocator.TempJob, 
                NativeArrayOptions.UninitializedMemory
            );
            
            var seed = (uint)System.DateTime.Now.Ticks;
            for (var i = 0; i < workerThreadCount; i++)
            {
                randomArray[i] = new Unity.Mathematics.Random(seed == 0 ? 1 : seed + (uint)i);
            }
            
            
            var entityManager = state.EntityManager;

            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            
            var spawnJob = new SpawnJob
            {
                Prototype = config.Prototype,
                Ecb = ecb.AsParallelWriter(),
                RandomArray = randomArray,
                SphereRadius = config.BoundsSize.x,
                Scale = config.ParticleScale
            };

            var spawnHandle = spawnJob.Schedule(config.ParticlesCount, 128);
            spawnHandle.Complete();
            randomArray.Dispose();
            ecb.Playback(entityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        private struct SpawnJob : IJobParallelFor
        {
            public Entity Prototype;
            public EntityCommandBuffer.ParallelWriter Ecb;
            public float SphereRadius;
            public float Scale;

            [NativeDisableContainerSafetyRestriction, NativeDisableParallelForRestriction]
            public NativeArray<Unity.Mathematics.Random> RandomArray;
            
            [NativeSetThreadIndex] 
            private int _threadIndex;

            public void Execute(int index)
            {
                var e = Ecb.Instantiate(index, Prototype);
                var rng = RandomArray[_threadIndex];

                var pos = rng.NextFloat3Direction() * SphereRadius;
                var rot = rng.NextQuaternionRotation();
                var transform = LocalTransform.FromPositionRotationScale(pos, rot, Scale);
                
                Ecb.SetComponent(index, e, transform);
                Ecb.AddComponent(index, e, new VelocityComponent(){Velocity = Vector3.one});
                
                RandomArray[_threadIndex] = rng;
            }
        }
    }
}