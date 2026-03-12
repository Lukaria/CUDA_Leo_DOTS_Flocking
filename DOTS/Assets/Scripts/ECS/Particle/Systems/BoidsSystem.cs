using Murmuration.ECS.Particle.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Murmuration.ECS.Particle.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public partial struct BoidsSystem : ISystem
    {
        private NativeArray<int> _bucket;
        private NativeArray<int> _next;
        private bool _isInitialized;
        private NativeArray<float3> _positionLookup;
        private NativeArray<float3> _velocityLookup;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameConfig>();
        }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gameConfig = SystemAPI.GetSingleton<GameConfig>();
            var cellReciprocal = 1.0f / gameConfig.CellSize;


            if (!_isInitialized)
            {
                _positionLookup = new NativeArray<float3>(gameConfig.ParticlesCount, Allocator.Persistent);
                _velocityLookup = new NativeArray<float3>(gameConfig.ParticlesCount, Allocator.Persistent);
                _bucket = new NativeArray<int>(SpatialHashHelper.BucketSize, Allocator.Persistent);
                _next = new NativeArray<int>(gameConfig.ParticlesCount, Allocator.Persistent);
                _isInitialized = true;
            }

            var clearBucketJob = new ClearBucketJob()
            {
                Bucket = _bucket,
                DefaultValue = SpatialHashHelper.DefaultBucketValue,
            };
            var clearHandle = clearBucketJob.Schedule(SpatialHashHelper.BucketSize, 64, state.Dependency);
            
            var buildSpatialHashJob = new BuildSpatialHashJob
            {
                Next = _next,
                Bucket =  _bucket,
                CellReciprocal = cellReciprocal,
                PositionLookup = _positionLookup,
                VelocityLookup = _velocityLookup
            };
            
            var handle1 = buildSpatialHashJob.Schedule(clearHandle);
            
            var updateVelocityJob = new UpdateVelocityJob()
            {
                Bucket = _bucket,
                Next = _next,
                PositionLookup = _positionLookup,
                VelocityLookup = _velocityLookup,
                CellReciprocal = cellReciprocal,
                CohesionRadiusSq = gameConfig.CohesionRadius * gameConfig.CohesionRadius,
                SeparationRadiusSq = gameConfig.SeparationRadius * gameConfig.SeparationRadius,
                AlignmentRadiusSq = gameConfig.AlignmentRadius * gameConfig.AlignmentRadius,
                CohesionWeight = gameConfig.CohesionWeight,
                SeparationWeight = gameConfig.SeparationWeight,
                AlignmentWeight = gameConfig.AlignmentWeight,
                MinSpeed = gameConfig.MinSpeed,
                MaxSpeed = gameConfig.MaxSpeed,
            };
            
            var handle2 = updateVelocityJob.ScheduleParallel(handle1);
            
            var updatePositionJob = new UpdatePositionJob()
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                XBoundSize = gameConfig.BoundsSize.x * 0.5f,
                YBoundSize = gameConfig.BoundsSize.y * 0.5f,
                ZBoundSize = gameConfig.BoundsSize.z * 0.5f,
            };
            var handle3 = updatePositionJob.ScheduleParallel(handle2);
            
            var updateRotationJob = new UpdateRotationJob()
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                RotationSpeed = gameConfig.RotationSpeed
            };
            
            var handle4 = updateRotationJob.ScheduleParallel(handle3);

            state.Dependency = handle4;
        }
        
        [BurstCompile]
        public struct ClearBucketJob : IJobParallelFor
        {
            [WriteOnly] public NativeArray<int> Bucket;
            public int DefaultValue;

            public void Execute(int index)
            {
                Bucket[index] = DefaultValue;
            }
        }

        [BurstCompile]
        public partial struct BuildSpatialHashJob : IJobEntity
        {
            [NativeDisableContainerSafetyRestriction] public NativeArray<int> Next;
            [NativeDisableContainerSafetyRestriction] public NativeArray<int> Bucket;
            [NativeDisableContainerSafetyRestriction] public NativeArray<float3> PositionLookup;
            [NativeDisableContainerSafetyRestriction] public NativeArray<float3> VelocityLookup;
            public float CellReciprocal;
            
            private void Execute([EntityIndexInQuery] int entity, in LocalTransform localTransform, in VelocityComponent velocityComponent)
            {
                var key = SpatialHashHelper.Hash(localTransform.Position * CellReciprocal);
                Next[entity] = Bucket[key];
                Bucket[key] = entity;
                PositionLookup[entity] = localTransform.Position;
                VelocityLookup[entity] = velocityComponent.Velocity;
            }
        }

        [BurstCompile]
        public partial struct UpdateVelocityJob : IJobEntity 
        {
            [ReadOnly, NativeDisableContainerSafetyRestriction] public NativeArray<int> Next;
            [ReadOnly, NativeDisableContainerSafetyRestriction] public NativeArray<int> Bucket;
            [ReadOnly] public NativeArray<float3> PositionLookup;
            [ReadOnly, NativeDisableContainerSafetyRestriction] public NativeArray<float3> VelocityLookup;
            public float CellReciprocal;
            public float CohesionRadiusSq;
            public float SeparationRadiusSq;
            public float AlignmentRadiusSq;
            public float CohesionWeight;
            public float SeparationWeight;
            public float AlignmentWeight;
            public float MinSpeed;
            public float MaxSpeed;

            private void Execute([EntityIndexInQuery] int entity, in LocalTransform positionComponent, ref VelocityComponent velocityComponent)
            {
                var position = positionComponent.Position;
                ref var velocity = ref velocityComponent.Velocity;

                var cohesion = float3.zero;
                var separation = float3.zero;
                var alignment = float3.zero;

                var cohesionCount = 0;
                var separationCount = 0;
                var alignmentCount = 0;

                var cellCoord = position * CellReciprocal;
                var cx = SpatialHashHelper.FastFloor(cellCoord.x);
                var cy = SpatialHashHelper.FastFloor(cellCoord.y);
                var cz = SpatialHashHelper.FastFloor(cellCoord.z);
                
                for (var dx = -1; dx <= 1; dx++)
                for (var dy = -1; dy <= 1; dy++)
                for (var dz = -1; dz <= 1; dz++)
                {
                    var key = SpatialHashHelper.Hash(cx + dx, cy + dy, cz + dz);
                    var neighbor = Bucket[key];
                    while (neighbor != SpatialHashHelper.DefaultBucketValue)
                    {
                        if (neighbor == entity)
                        {
                            neighbor = Next[neighbor];
                            continue;
                        }
                        var distanceSq = math.lengthsq(PositionLookup[neighbor] - position);
                        if (distanceSq <= CohesionRadiusSq)
                        {
                            cohesion += PositionLookup[neighbor];
                            ++cohesionCount;
                        }

                        if (distanceSq <= SeparationRadiusSq && distanceSq > 0.0001f)
                        {
                            var distance = position - PositionLookup[neighbor];
                            separation += distance / math.lengthsq(distance);
                            ++separationCount;
                        }

                        if (distanceSq <= AlignmentRadiusSq)
                        {
                            alignment += VelocityLookup[neighbor];
                            ++alignmentCount;
                        }
                        
                        neighbor = Next[neighbor];
                    }
                }
                
                if (cohesionCount > 0)
                {
                    cohesion /= cohesionCount;
                    cohesion = (cohesion - position) * CohesionWeight;
                }

                if (separationCount > 0)
                {
                    separation /= separationCount;
                    separation *= SeparationWeight;
                }

                if (alignmentCount > 0)
                {
                    alignment /= alignmentCount;
                    alignment *= AlignmentWeight;
                }

                velocity += cohesion + separation + alignment;
                
                var speed = math.lengthsq(velocity); 

                if (speed < MinSpeed * MinSpeed) 
                {
                    velocity = math.normalize(velocity) * MinSpeed;
                }
                else if (speed > MaxSpeed * MaxSpeed) 
                {
                    velocity = math.normalize(velocity) * MaxSpeed;
                }
            }
        }
        
        [BurstCompile]
        public partial struct UpdatePositionJob : IJobEntity 
        {
            public float DeltaTime;
            public float XBoundSize;
            public float YBoundSize;
            public float ZBoundSize;
            
            private void Execute(ref LocalTransform positionComponent, in VelocityComponent velocityComponent)
            {
                ref var position = ref positionComponent.Position;
                position += velocityComponent.Velocity * DeltaTime;
                
                BoundCoordinate(ref position.x, XBoundSize);
                BoundCoordinate(ref position.y, YBoundSize);
                BoundCoordinate(ref position.z, ZBoundSize);
            }
            
            private void BoundCoordinate(ref float coord, float f)
            {
                if (coord > f)
                {
                    coord = coord % f - f;
                }
                else if (coord < -f)
                {
                    coord = coord % f + f;
                }
            }
        }
        
        [BurstCompile]
        public partial struct UpdateRotationJob : IJobEntity 
        {
            public float DeltaTime;
            public float RotationSpeed;
            
            private void Execute(ref LocalTransform rotationComponent, in VelocityComponent velocityComponent)
            {
                ref var rotation =  ref rotationComponent.Rotation;
                var velocity =  velocityComponent.Velocity;
                
                if (math.lengthsq(velocity) > 0.001f)
                {
                    rotation = math.slerp(
                        rotation, 
                        quaternion.LookRotation(math.normalize(velocity), math.up()),
                        RotationSpeed * DeltaTime);
                }
            }
        }

        public void OnDestroy(ref SystemState state)
        {
            _positionLookup.Dispose();
            _velocityLookup.Dispose();
            _bucket.Dispose();
            _next.Dispose();
        }
    }
}