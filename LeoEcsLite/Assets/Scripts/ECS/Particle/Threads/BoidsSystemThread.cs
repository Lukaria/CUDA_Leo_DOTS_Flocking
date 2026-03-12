using Leopotam.EcsLite.Threads;
using Murmuration.ECS.Particle.Components;
using Unity.Mathematics;
using UnityEngine;

namespace Murmuration.ECS.Particle.Threads
{
    public struct BoidsSystemThread : IEcsThread<PositionComponent, RotationComponent, VelocityComponent>
    {
        #region entities
        
        private VelocityComponent[] _velocityPool;
        private RotationComponent[] _rotationPool;
        private PositionComponent[] _positionPool;
        private int[] _positionIndices;
        private int[] _rotationIndices;
        private int[] _velocityIndices;
        private int[] _entities;
        
        #endregion
        
        #region data
        
        public SharedData SharedData;
        private SpatialHash _spatialHash;
        private float _cohesionRadiusSq;
        private float _separationRadiusSq;
        private float _alignmentRadiusSq;
        private float _cellReciprocal;
        private float _xBoundSize;
        private float _yBoundSize;
        private float _zBoundSize;
        
        #endregion

        public void Init(int[] entities, PositionComponent[] pool1, int[] indices1, RotationComponent[] pool2, int[] indices2,
            VelocityComponent[] pool3, int[] indices3)
        {
            _entities = entities;
            _positionPool = pool1;
            _positionIndices = indices1;
            _rotationPool = pool2;
            _rotationIndices = indices2;
            _velocityPool = pool3;
            _velocityIndices = indices3;
            
            _spatialHash = SharedData.SpatialHash;
            
            _cohesionRadiusSq = SharedData.CohesionRadius * SharedData.CohesionRadius;
            _separationRadiusSq = SharedData.SeparationRadius * SharedData.SeparationRadius;
            _alignmentRadiusSq = SharedData.AlignmentRadius * SharedData.AlignmentRadius;
            
            
            _cellReciprocal = 1.0f / SharedData.CellSize;
            _xBoundSize = SharedData.BoundsSize.x * 0.5f;
            _yBoundSize = SharedData.BoundsSize.x * 0.5f;
            _zBoundSize = SharedData.BoundsSize.x * 0.5f;
        }

        public void Execute(int threadId, int fromIndex, int beforeIndex)
        {
            for (var i = fromIndex; i < beforeIndex; i++)
            {
                var entity = _entities[i];
                ref var position = ref _positionPool[_positionIndices[entity]].Position;
                ref var rotationComponent = ref _rotationPool[_rotationIndices[entity]];
                ref var velocityComponent = ref _velocityPool[_velocityIndices[entity]];
                
                var accel = GetAcceleration(entity, in position);

                ref var velocity =  ref velocityComponent.Velocity;
                
                UpdateVelocity(ref velocity, in accel);
                UpdatePosition(ref position, in velocity);
                UpdateRotation(ref rotationComponent, in velocity);

                SharedData.ParticleData[entity].matrix =
                    float4x4.TRS(position, rotationComponent.Rotation, Vector3.one * SharedData.ParticleScale);
            }
        }

        private float3 GetAcceleration(int entity, in float3 position)
        {
            var cohesion = float3.zero;
            var separation = float3.zero;
            var alignment = float3.zero;

            var cohesionCount = 0;
            var separationCount = 0;
            var alignmentCount = 0;
            
            
            var cellCoord = position * _cellReciprocal;
            var cx = SpatialHash.Floor(cellCoord.x);
            var cy = SpatialHash.Floor(cellCoord.y);
            var cz = SpatialHash.Floor(cellCoord.z);
                
            for (var dx = -1; dx <= 1; dx++)
            for (var dy = -1; dy <= 1; dy++)
            for (var dz = -1; dz <= 1; dz++)
            {
                var key = SpatialHash.Hash(cx + dx, cy + dy, cz + dz);
                var neighbor = _spatialHash.FromBucket(key);
                while (neighbor != SpatialHash.DefaultBucketValue)
                {
                    if (neighbor == entity)
                    {
                        neighbor = _spatialHash.Next(neighbor);
                        continue;
                    }
                        
                    if(entity == neighbor) continue;
                    var distanceSq = math.lengthsq(_positionPool[_positionIndices[neighbor]].Position -
                                                   position);
                    if (distanceSq <= _cohesionRadiusSq)
                    {
                        cohesion += _positionPool[_positionIndices[neighbor]].Position;
                        ++cohesionCount;
                    }
                    if (distanceSq <= _separationRadiusSq  && distanceSq > 0.0001f)
                    {
                        var distance = position - _positionPool[_positionIndices[neighbor]].Position;
                        separation += distance / math.lengthsq(distance);
                        ++separationCount;
                    }
                    if (distanceSq <= _alignmentRadiusSq)
                    {
                        alignment += _velocityPool[_velocityIndices[neighbor]].Velocity;
                        ++alignmentCount;
                    }
                        
                    neighbor = _spatialHash.Next(neighbor);
                }
            }
                
                
                
            if (cohesionCount > 0)
            {
                cohesion /= cohesionCount;
                cohesion = (cohesion - position) * SharedData.CohesionWeight;
            }
            if (separationCount > 0)
            {
                separation /= separationCount;
                separation *= SharedData.SeparationWeight;
            }
            if (alignmentCount > 0)
            {
                alignment /= alignmentCount;
                alignment *= SharedData.AlignmentWeight;
            }

            return cohesion + separation + alignment;
        }

        private void UpdateVelocity(ref float3 velocity, in float3 acceleration)
        {
            velocity += acceleration;
                
            var speed = math.lengthsq(velocity); 

            if (speed < SharedData.MinSpeed  * SharedData.MinSpeed) 
            {
                velocity = math.normalize(velocity) * SharedData.MinSpeed;
            }
            else if (speed > SharedData.MaxSpeed * SharedData.MaxSpeed) 
            {
                velocity = math.normalize(velocity) * SharedData.MaxSpeed;
            }
        }
        
        private void UpdatePosition(ref float3 position, in float3 velocity)
        {
            position += velocity * SharedData.DeltaTime;
                
            BoundCoordinate(ref position.x, _xBoundSize);
            BoundCoordinate(ref position.y, _yBoundSize);
            BoundCoordinate(ref position.z, _zBoundSize);
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
        
        private RotationComponent UpdateRotation(ref RotationComponent rotationComponent, in float3 velocity)
        {
            if (math.lengthsq(velocity) > 0.001f)
            {
                rotationComponent.Rotation = math.slerp(
                    rotationComponent.Rotation, 
                    quaternion.LookRotation(math.normalize(velocity), math.up()),
                    SharedData.RotationSpeed * SharedData.DeltaTime);
            }

            return rotationComponent;
        }
    }
}