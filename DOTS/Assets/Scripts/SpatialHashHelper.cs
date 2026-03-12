using Unity.Mathematics;

namespace Murmuration
{
    public struct SpatialHashHelper
    {
        public const int BucketSize = 65536;//2^16
        public const int DefaultBucketValue = -1;

        public static int FastFloor(float f)
        {
            return (int)math.floor(f);
        }

        public static int Hash(float x, float y, float z)
        {
            var hash = (FastFloor(x) * 73856093) ^
                       (FastFloor(y) * 19349663) ^
                       (FastFloor(z) * 83492791);
            return hash & (BucketSize - 1);
        }
        
        public static int Hash(float3 vector) => Hash(vector.x, vector.y, vector.z);
    }
}