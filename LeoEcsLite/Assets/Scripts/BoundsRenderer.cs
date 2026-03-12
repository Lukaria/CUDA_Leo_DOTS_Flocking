using Murmuration;
using Unity.Mathematics;
using UnityEngine;

namespace Scripts
{
    public class BoundsRenderer : MonoBehaviour
    {
        [SerializeField] private SharedDataView dataView;
        public Color color = Color.green;

        private Mesh _wireCubeMesh;
        private Material _lineMaterial;
        private float3 Bounds => dataView.SharedData.BoundsSize;

        private void Awake()
        {
            _wireCubeMesh = CreateWireCubeMesh(Bounds/2);
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");

            _lineMaterial = new Material(shader);
        }

        private void Update()
        {
            DrawBounds();
        }

        public void DrawBounds()
        {
            if (_wireCubeMesh == null || _lineMaterial == null) return;

            _lineMaterial.color = color;

            Graphics.DrawMesh(_wireCubeMesh, Matrix4x4.identity, _lineMaterial, 0);
        }

        private void OnDestroy()
        {
            if (_lineMaterial != null) Destroy(_lineMaterial);
            if (_wireCubeMesh != null) Destroy(_wireCubeMesh);
        }

        public static Mesh CreateWireCubeMesh(float3 bounds)
        {
            var mesh = new Mesh
            {
                name = "WireCube"
            };

            Vector3[] vertices =
            {
                new(-bounds.x, -bounds.y, -bounds.z),
                new(bounds.x, -bounds.y, -bounds.z),
                new(bounds.x, -bounds.y, bounds.z),
                new(-bounds.x, -bounds.y, bounds.z),
                new(-bounds.x, bounds.y, -bounds.z),
                new(bounds.x, bounds.y, -bounds.z),
                new(bounds.x, bounds.y, bounds.z),
                new(-bounds.x, bounds.y, bounds.z)
            };

            int[] indices =
            {
                0, 1, 1, 2, 2, 3, 3, 0,
                4, 5, 5, 6, 6, 7, 7, 4,
                0, 4, 1, 5, 2, 6, 3, 7
            };

            mesh.vertices = vertices;

            mesh.SetIndices(indices, MeshTopology.Lines, 0);

            return mesh;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = color;
            Gizmos.DrawWireCube(Vector3.zero, Bounds);
        }
    }
}