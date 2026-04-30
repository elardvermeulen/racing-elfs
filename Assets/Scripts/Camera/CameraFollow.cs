using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    private float3 _offset;
    private bool _offsetInitialized;
    private EntityQuery _carQuery;

    void Start()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        _carQuery = world.EntityManager.CreateEntityQuery(typeof(CarData), typeof(LocalToWorld));
    }

    void LateUpdate()
    {
        if (_carQuery.IsEmpty) return;

        var localToWorld = _carQuery.GetSingleton<LocalToWorld>();
        float3 carPosistion = localToWorld.Position;

        if (!_offsetInitialized)
        {
            // Store offset in car-local space so it rotates with the car
            float4x4 worldToLocal = math.inverse(localToWorld.Value);
            _offset = math.transform(worldToLocal, (float3)transform.position);

            _offsetInitialized = true;
        }
        transform.position = carPosistion + _offset;
    }
}
