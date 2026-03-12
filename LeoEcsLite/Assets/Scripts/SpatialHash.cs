using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Murmuration
{
    public class SpatialHash
    {
        private int[] _bucket;
        private int[] _next;
        
        private const int BucketSize = 65536;//2^16
        public const int DefaultBucketValue = -1;
        

        public void Initialize(int entitiesCount)
        {
            _bucket = new int[BucketSize];
            _next = new int[entitiesCount + 1];
            Array.Fill(_bucket, DefaultBucketValue);
        }

        public static int Floor(float x)
        {
            return (int)math.floor(x);
        }

        public static int Hash(float x, float y, float z)
        {
            var hash = (Floor(x) * 73856093) ^
                       (Floor(y) * 19349663) ^
                       (Floor(z) * 83492791);
            return hash & (BucketSize - 1);
        }
        
        public static int Hash(float3 vector) => Hash(vector.x, vector.y, vector.z);

        public void Insert(int entityId, float3 position)
        {
            var key = Hash(position.x, position.y, position.z);

            _next[entityId] = _bucket[key];
            _bucket[key] = entityId;
        }
        
        public void Clear()
        {
            Array.Fill(_bucket, DefaultBucketValue);
        }
        
        public int FromBucket(int key) => _bucket[key];
        
        public int Next(int entityId) => _next[entityId];
    }
}