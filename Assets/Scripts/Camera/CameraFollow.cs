using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    private float3 _localOffset;
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

        _carQuery.CompleteDependency();

        var localToWorld = _carQuery.GetSingleton<LocalToWorld>();
        float3 carPosistion = localToWorld.Position;
        quaternion carRotation = new quaternion(localToWorld.Value);

        if (!_offsetInitialized)
        {
            // Store the camera's starting offset in the car's local space (rotation only)
            quaternion inverseRotation = math.inverse(carRotation);
            _localOffset = math.rotate(inverseRotation, (float3)transform.position - carPosistion);
            _offsetInitialized = true;
        }
        transform.position = carPosistion + math.rotate(carRotation, _localOffset);
        transform.LookAt((Vector3)carPosistion);
    }
}
