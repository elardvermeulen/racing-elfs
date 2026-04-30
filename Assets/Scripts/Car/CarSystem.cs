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

        // Entity has physics: do not write LocalTransform.Rotation (physics owns transform)
        // Apply yaw by setting angular velocity around Y axis only. This avoids roll/pitch.
        PhysicsVelocity velocity = state.EntityManager.GetComponentData<PhysicsVelocity>(carEntity);
        PhysicsMass mass = state.EntityManager.GetComponentData<PhysicsMass>(carEntity);

        // Lock pitch (X) and roll (Z) at the physics body level so the contact
        // solver cant apply rolling torques - this is order-independent unlike
        // zeroing angular velocity after the fact.
        mass.InverseInertia = new float3(0f, mass.InverseInertia.y, 0f);
        state.EntityManager.SetComponentData(carEntity, mass);

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
            float3 dv = forwardXZ * carData.acceleration * SystemAPI.Time.DeltaTime;
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
        // Kill sideways velocity to simulate tire grip
        float3 right = math.rotate(transform.Rotation, new float3(1f, 0f, 0f));
        float sidewaysSpeed = math.dot(velocity.Linear, right);
        velocity.Linear -= right * sidewaysSpeed * carData.lateralDampning;
        // Extra downward force to keep the car planted
        velocity.Linear.y -= 9.81f * (carData.gravityMultiplier -1f)  * SystemAPI.Time.DeltaTime;

        // Cap horizontal speed
        float2 horizontalVelocity = new float2(velocity.Linear.x, velocity.Linear.z);
        float horizontalSpeed = math.length(horizontalVelocity);
        if (horizontalSpeed > carData.maxSpeed)
        {
            float2 clamped = math.normalize(horizontalVelocity) * carData.maxSpeed;
            velocity.Linear.x = clamped.x;
            velocity.Linear.z = clamped.y;
        }

        // Clamp upward velocity to prevent bounce launching
        if (velocity.Linear.y > carData.maxUpwardSpeed)
        {
            velocity.Linear.y = carData.maxUpwardSpeed;
        }


        state.EntityManager.SetComponentData(carEntity, velocity);
    }

}
