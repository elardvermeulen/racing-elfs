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
        
        // Extract only horizontal facing direction (yaw), ignore pitch/roll
        float3 horizontalForward = new float3(localToWorld.Forward.x, 0f, localToWorld.Forward.z);
        if (math.lengthsq(horizontalForward) > 1e-6f)
            horizontalForward = math.normalize(horizontalForward);
        else
            horizontalForward = new float3(0f, 0f, 1f);


        quaternion yawOnly = quaternion.LookRotation(horizontalForward, new float3(0f, 1f, 0f));

        if (!_offsetInitialized)
        {
            quaternion inverseYaw = math.inverse(yawOnly);
            _localOffset = math.rotate(inverseYaw, (float3)transform.position - carPosistion);
            _offsetInitialized = true;
        }
        transform.position = carPosistion + math.rotate(yawOnly, _localOffset);
        transform.LookAt((Vector3)carPosistion);
    }
}
