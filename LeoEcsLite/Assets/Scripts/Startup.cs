using Murmuration;
using Murmuration.ECS;
using Murmuration.ECS.Particle;
using UnityEngine;

public class Startup : MonoBehaviour
{
    [SerializeField] private SharedDataView _dataView;
    private ECSStartup _ecsSystem;

    private bool _gameInited;


    private void Awake()
    {
        _ecsSystem = new ECSStartup();
        Run();
    }

    private void Run()
    {
        var shared = _dataView.SharedData;
        shared.ParticleData = new ParticleData[shared.ParticlesCount];
        shared.DeltaTime = Time.deltaTime;
        _ecsSystem.Initialize(shared);
        _gameInited = true;
    }

    private void Update()
    {
        if (!_gameInited) return;
        _ecsSystem.Update();
    }

    private void OnDestroy()
    {
        DestroyEcs();
    }

    private void OnApplicationQuit()
    {
        DestroyEcs();
    }

    void DestroyEcs()
    {
        if (!_gameInited) return;
        _ecsSystem.OnDestroy();
    }
}
