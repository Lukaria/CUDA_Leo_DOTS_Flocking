using System.Runtime.InteropServices;
using UnityEngine;

namespace Murmuration.ECS.Particle
{
    [System.Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct ParticleData
    {
        public Matrix4x4 matrix;
    }

    public struct ParticleDataInfo
    {
        public const int ParticleDataSize = 4 * 4 * 4;
    }
}