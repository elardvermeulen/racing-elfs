using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class CarAuthoring : MonoBehaviour
{
    public float acceleration;
    public float turnSpeed;
    //public Transform carModel;
    public float3 startModelOffset;
    public float groundCheckRate;
    public float lastGroundCheckTime;
    public float curYRot;
    public bool accelerateInput;
    public float turnInput;
    public float lateralDampning = 0.85f;

    private class Baker : Baker<CarAuthoring>
    {
        public override void Bake(CarAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            UnityEngine.Debug.Log("Baking CarAuthoring for entity: " + entity);
            AddComponent(entity, new CarData
            {
                acceleration = authoring.acceleration,
                turnSpeed = authoring.turnSpeed,
                startModelOffset = authoring.startModelOffset,
                groundCheckRate = authoring.groundCheckRate,
                lastGroundCheckTime = authoring.lastGroundCheckTime,
                lateralDampning = authoring.lateralDampning,
            });
        }
    }
}
