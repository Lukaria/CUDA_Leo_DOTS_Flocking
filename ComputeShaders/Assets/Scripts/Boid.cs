using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace Scripts
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Boid {
        public float3 position;
        private float memory_alignment_spacer_1; 
        public quaternion rotation;
        public float3 velocity;
        private float memory_alignment_spacer_2; 
    }

    public struct BoidInfo
    {
        //public const int BoidSize = 3 * 4 + 1* 4 + 4 * 4 + 3 * 4 + 1 * 4;
        public const int BoidSize = 48;
    }
}