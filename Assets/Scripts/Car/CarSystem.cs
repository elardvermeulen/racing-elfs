using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Physics;

public partial struct CarSystem : ISystem
{
    private Entity carEntity;
    private Entity inputEntity;

    private CarData carData;
    private InputData inputData;


    public void OnUpdate(ref SystemState state)
    {
        carEntity = SystemAPI.GetSingletonEntity<CarData>();
        inputEntity = SystemAPI.GetSingletonEntity<InputData>();

        carData = state.EntityManager.GetComponentData<CarData>(carEntity);
        inputData = state.EntityManager.GetComponentData<InputData>(inputEntity);

        Movement(ref state);
    }

    void Movement(ref SystemState state)
    {
        // Read transform for orientation/position info
        LocalTransform transform = state.EntityManager.GetComponentData<LocalTransform>(carEntity);

        // Apply model offset to visual only if entity isn't physics-driven
        //bool hasPhysics = SystemAPI.HasComponent<PhysicsVelocity>(carEntity) && SystemAPI.HasComponent<PhysicsMass>(carEntity);

        //if (!hasPhysics)
        //{
        //    // No physics body: modify transform directly
        //    transform.Position += carData.startModelOffset;

        //    float yawDegrees = inputData.turnInput * carData.turnSpeed * SystemAPI.Time.DeltaTime;
        //    quaternion yawRot = quaternion.Euler(0f, math.radians(yawDegrees), 0f);
        //    transform.Rotation = math.mul(transform.Rotation, yawRot);

        //    if (inputData.accelerateInput == 1)
        //    {
        //        float3 forward = math.rotate(transform.Rotation, new float3(0f, 0f, 1f));
        //        float3 forwardXZ = math.normalize(new float3(forward.x, 0f, forward.z));
        //        if (math.lengthsq(forwardXZ) > 0f)
        //        {
        //            transform.Position += forwardXZ * carData.acceleration * SystemAPI.Time.DeltaTime;
        //        }
        //    }

        //    state.EntityManager.SetComponentData(carEntity, transform);
        //    return;
        //}

        // Entity has physics: do not write LocalTransform.Rotation (physics owns transform)
        // Apply yaw by setting angular velocity around Y axis only. This avoids roll/pitch.
        PhysicsVelocity velocity = state.EntityManager.GetComponentData<PhysicsVelocity>(carEntity);
        PhysicsMass mass = state.EntityManager.GetComponentData<PhysicsMass>(carEntity);

        // Compute forward from read transform but project to XZ plane so acceleration doesn't add vertical velocity
        float3 forward = math.rotate(transform.Rotation, new float3(0f, 0f, 1f));
        float3 forwardXZ = new float3(forward.x, 0f, forward.z);
        if (math.lengthsq(forwardXZ) > 1e-6f)
        {
            forwardXZ = math.normalize(forwardXZ);
        }
        else
        {
            forwardXZ = new float3(0f, 0f, 1f);
        }

        // Apply forward acceleration as change in linear velocity on XZ only
        if (inputData.accelerateInput == 1)
        {
            float3 dv = forwardXZ * carData.acceleration * SystemAPI.Time.DeltaTime * mass.InverseMass;
            velocity.Linear = new float3(velocity.Linear.x + dv.x, velocity.Linear.y, velocity.Linear.z + dv.z);
        }

        // Set yaw angular velocity (rad/s) and zero out roll/pitch angular components to prevent tumbling
        float yawDegPerSec = inputData.turnInput * carData.turnSpeed;
        float yawRadPerSec = math.radians(yawDegPerSec);
        velocity.Angular = new float3(0f, yawRadPerSec, 0f);

        // Optionally apply some angular damping to XZ (already zero) and to Y for stability
        // Here we mildly damp existing Y angular velocity when there's no input
        if (math.abs(inputData.turnInput) < 1e-4f)
        {
            velocity.Angular.y *= 0.9f; // simple damping
        }
        // Prevent further pitch/roll by clearing X/Z angular velocity:
        velocity.Angular.x = 0f;
        velocity.Angular.z = 0f;

        state.EntityManager.SetComponentData(carEntity, velocity);

        // Helper to extract yaw (radians) from quaternion
        static float GetYaw(quaternion q)
        {
            float x = q.value.x;
            float y = q.value.y;
            float z = q.value.z;
            float w = q.value.w;
            float t3 = +2f * (w * z + x * y);
            float t4 = +1f - 2f * (y * y + z * z);
            return math.atan2(t3, t4);
        }


    }

}
