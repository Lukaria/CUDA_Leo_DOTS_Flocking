using Scripts;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class BoidsSimulationController : MonoBehaviour {

    public BoidsConfig config;
    
    [Header("Compute")]
    public ComputeShader boidsSystemShader;
    
    private ComputeBuffer _particlesBuffer;
    private ComputeBuffer _bucketBuffer;
    private ComputeBuffer _nextBuffer;
    private int _clearBucketsKernel;
    private int _buildSpatialHashKernel;
    private int _simulateBoidsKernel;
    private Bounds _drawBounds;
    private float _cellSize;

    const string ClearBucketsKernelName = "ClearBuckets";
    const string BuildSpatialHashKernelName = "BuildSpatialHash";
    const string SimulateBoidsKernelName = "SimulateBoids";
    private readonly int _bucketSize = (int)math.pow(2, 25);

    #region ids
    //arrays
    static readonly int ParticlesID = Shader.PropertyToID("particles");
    static readonly int MinBoundsID = Shader.PropertyToID("_MinBounds");
    static readonly int MaxBoundsID = Shader.PropertyToID("_MaxBounds");
    static readonly int ScaleID = Shader.PropertyToID("_Scale");
    static readonly int BucketID = Shader.PropertyToID("bucket");
    static readonly int NextID = Shader.PropertyToID("next");
    //general
    static readonly int ParticlesCountID = Shader.PropertyToID("particles_count");
    static readonly int DeltaTimeID = Shader.PropertyToID("delta_time");
    static readonly int CellReciprocalID = Shader.PropertyToID("cell_reciprocal");
    //steering
    static readonly int CohesionRadiusSqID = Shader.PropertyToID("cohesion_radius_sq");
    static readonly int SeparationRadiusSqID = Shader.PropertyToID("separation_radius_sq");
    static readonly int AlignmentRadiusSqID = Shader.PropertyToID("alignment_radius_sq");
    static readonly int CohesionWeightId = Shader.PropertyToID("cohesion_weight");
    static readonly int SeparationWeightId = Shader.PropertyToID("separation_weight");
    static readonly int AlignmentWeightId = Shader.PropertyToID("alignment_weight");
    //limits
    static readonly int MinSpeedId = Shader.PropertyToID("min_speed");
    static readonly int MaxSpeedId = Shader.PropertyToID("max_speed");
    static readonly int RotationSpeedId = Shader.PropertyToID("rotation_speed");
    static readonly int XBoundSize = Shader.PropertyToID("x_bound_size");
    static readonly int YBoundSize = Shader.PropertyToID("y_bound_size");
    static readonly int ZBoundSize = Shader.PropertyToID("z_bound_size");
    static readonly int BucketSizeId = Shader.PropertyToID("bucket_size");

    #endregion
    
    void Start()
    {
        UpdateCellSize();
        UpdateBounds();
        InitializeParticles();
        InitializeCompute();
        InitializeRendering();
    }

    private void UpdateBounds()
    {
        var xBoundSize = config.boundsSize.x;
        config.particleMaterial.SetFloat(MinBoundsID, -xBoundSize * 0.5f);
        config.particleMaterial.SetFloat(MaxBoundsID, xBoundSize * 0.5f);
    }

    void Update()
    {
        UpdateRuntimeShaderData();
        
        DispatchKernels();
        Render();
    }

    private void UpdateRuntimeShaderData()
    {
        config.particleMaterial.SetFloat(ScaleID, config.particleScale);
        
        boidsSystemShader.SetFloat(DeltaTimeID, Time.deltaTime);
        boidsSystemShader.SetFloat(CellReciprocalID, 1f / _cellSize);
        
        boidsSystemShader.SetFloat(CohesionRadiusSqID, config.cohesionRadius * config.cohesionRadius);
        boidsSystemShader.SetFloat(SeparationRadiusSqID, config.separationRadius * config.separationRadius);
        boidsSystemShader.SetFloat(AlignmentRadiusSqID, config.alignmentRadius * config.alignmentRadius);
        boidsSystemShader.SetFloat(CohesionWeightId, config.cohesionWeight);
        boidsSystemShader.SetFloat(SeparationWeightId, config.separationWeight);
        boidsSystemShader.SetFloat(AlignmentWeightId, config.alignmentWeight);
        
        boidsSystemShader.SetFloat(MinSpeedId, config.minSpeed);
        boidsSystemShader.SetFloat(MaxSpeedId, config.maxSpeed);
        boidsSystemShader.SetFloat(RotationSpeedId, config.rotationSpeed);
        boidsSystemShader.SetFloat(XBoundSize, config.boundsSize.x * 0.5f);
        boidsSystemShader.SetFloat(YBoundSize, config.boundsSize.y * 0.5f);
        boidsSystemShader.SetFloat(ZBoundSize, config.boundsSize.z * 0.5f);
    }

    private void OnDestroy()
    {
        _particlesBuffer?.Dispose();
        _bucketBuffer?.Dispose();
        _nextBuffer?.Dispose();
    }

    private void Render() {
        Graphics.DrawMeshInstancedProcedural(
            config.particleMesh,
            0,
            config.particleMaterial,
            _drawBounds,
            config.particlesCount
        );
    }

    private void DispatchKernels() {
        var bucketGroups = Mathf.CeilToInt(_bucketSize / 1024f);
        var particleGroups = Mathf.CeilToInt(config.particlesCount / 256f);
        
        SetClearBucketsKernelBuffers();
        boidsSystemShader.Dispatch(_clearBucketsKernel, bucketGroups, 1, 1);
        
        SetBuildSpatialHashKernelBuffers();
        boidsSystemShader.Dispatch(_buildSpatialHashKernel, particleGroups, 1, 1);
        
        SetSimulationKernelBuffers();
        boidsSystemShader.SetInt(ParticlesCountID, config.particlesCount);
        boidsSystemShader.Dispatch(_simulateBoidsKernel, particleGroups, 1, 1);
    }

    private void InitializeRendering() {
        config.particleMaterial.SetBuffer(ParticlesID, _particlesBuffer);
        
        _drawBounds = new Bounds(Vector3.zero, config.boundsSize);
    }

    private void InitializeCompute() {
        _bucketBuffer = new ComputeBuffer(_bucketSize, sizeof(int));
        _nextBuffer = new ComputeBuffer(config.particlesCount, sizeof(int));
        
        boidsSystemShader.SetInt(ParticlesCountID, config.particlesCount);
        boidsSystemShader.SetInt(BucketSizeId, _bucketSize);
        
        _clearBucketsKernel = boidsSystemShader.FindKernel(ClearBucketsKernelName);
        _buildSpatialHashKernel = boidsSystemShader.FindKernel(BuildSpatialHashKernelName);
        _simulateBoidsKernel = boidsSystemShader.FindKernel(SimulateBoidsKernelName);
        SetBuffers();
    }

    private void SetBuffers()
    {
        SetClearBucketsKernelBuffers();
        SetBuildSpatialHashKernelBuffers();
        SetSimulationKernelBuffers();
    }

    private void SetClearBucketsKernelBuffers()
    {
        boidsSystemShader.SetBuffer(_clearBucketsKernel, BucketID, _bucketBuffer);
    }

    private void SetBuildSpatialHashKernelBuffers()
    {
        boidsSystemShader.SetBuffer(_buildSpatialHashKernel, ParticlesID, _particlesBuffer);
        boidsSystemShader.SetBuffer(_buildSpatialHashKernel, BucketID, _bucketBuffer);
        boidsSystemShader.SetBuffer(_buildSpatialHashKernel, NextID, _nextBuffer);
    }

    void SetSimulationKernelBuffers()
    {
        boidsSystemShader.SetBuffer(_simulateBoidsKernel, ParticlesID, _particlesBuffer);
        boidsSystemShader.SetBuffer(_simulateBoidsKernel, BucketID, _bucketBuffer);
        boidsSystemShader.SetBuffer(_simulateBoidsKernel, NextID, _nextBuffer);
    }

    void InitializeParticles() {
        _particlesBuffer = new ComputeBuffer(config.particlesCount, BoidInfo.BoidSize);
        
        var particles = new Boid[config.particlesCount];
        for (var i = 0; i < config.particlesCount; i++) {
            particles[i] = new Boid {
                position = Random.insideUnitSphere * config.sphereRadius,
                rotation = Random.rotationUniform,
                velocity = Random.insideUnitSphere * config.minSpeed,
            };
        }
        
        _particlesBuffer.SetData(particles);
    }

    private void OnValidate()
    {
        UpdateCellSize();
    }

    private void UpdateCellSize()
    {
        _cellSize = math.max(
            math.max(config.cohesionRadius, config.separationRadius),
            config.alignmentRadius
        );
        _cellSize = Mathf.Max(_cellSize, 0.1f);
    }
}