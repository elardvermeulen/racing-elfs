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
        var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
        LocalTransform transform = state.EntityManager.GetComponentData<LocalTransform>(carEntity);
        PhysicsVelocity velocity = state.EntityManager.GetComponentData<PhysicsVelocity>(carEntity);
        PhysicsMass mass = state.EntityManager.GetComponentData<PhysicsMass>(carEntity);

        // Allow pitch, lock only roll
        mass.InverseInertia = new float3(mass.InverseInertia.x, mass.InverseInertia.y, 0f);
        state.EntityManager.SetComponentData(carEntity, mass);

        // Raycast down to detect ground and slow normal
        bool onGround = false;
        float3 groundNormal = new float3(0f, 1f, 0f);
        var rayInput = new RaycastInput
        {
            Start = transform.Position + new float3(0f, 0.3f, 0f),
            End = transform.Position - new float3(0f, 1.5f, 0f),
            Filter = CollisionFilter.Default
        };

        if (collisionWorld.CastRay(rayInput, out Unity.Physics.RaycastHit hit))
        {
            onGround = true;
            groundNormal = hit.SurfaceNormal;
        }
        
        float3 carForward = math.rotate(transform.Rotation, new float3(0f, 0f, 1f));
        float3 carRight = math.rotate(transform.Rotation, new float3(1f, 0f, 0f));
        float3 carUp = math.rotate(transform.Rotation, new float3(0f, 1f, 0f));

        // Acceleration along slow surface (or flat XZ when airborne)
        float3 accelerationDirection;
        if (onGround)
        {
            // project forward onto ground plane
            accelerationDirection = carForward - math.dot(carForward, groundNormal) * groundNormal;
            accelerationDirection = math.lengthsq(accelerationDirection) > 1e-6f ? math.normalize(accelerationDirection) : carForward;
        }
        else
        {
            float3 flat = new float3(carForward.x, 0f, carForward.z);
            accelerationDirection = math.lengthsq(flat) > 1e-6f ? math.normalize(flat) : new float3(0f, 1f, 1f);
        }

        if (inputData.accelerateInput == 1)
            velocity.Linear += accelerationDirection * carData.acceleration * SystemAPI.Time.DeltaTime;
          
        // Steering: yaw directly from input, no feedback from velocity.Angular
        float yawRadPerSec = math.radians(inputData.turnInput * carData.turnSpeed);

        // Pitch: rotate the car up toward slope normal (or upright when airborne)
        float3 targetUp = onGround ? groundNormal : new float3(0f, 1f, 0f);  
        float targetPitchRate = math.dot(math.cross(carUp, targetUp), carRight) * carData.slopeAlignSpeed;
        float currentPitchRate = math.dot(velocity.Angular, carRight);
        float pitchRate = targetPitchRate - currentPitchRate * 0.5f;

        // Combine: yaw around world Y + pitch around car right axis
        velocity.Angular = new float3(0f, yawRadPerSec, 0f) + carRight * pitchRate;

        // Lateral damping using horizontal right so slopes don't affect it
        float3 rightXZ = new float3(carRight.x, 0f, carRight.z);
        if (math.lengthsq(rightXZ) > 1e-6f)
        {
            rightXZ = math.normalize(rightXZ);  
            float sidewaysSpeed = math.dot(velocity.Linear, rightXZ);
            velocity.Linear -= rightXZ * sidewaysSpeed * carData.lateralDampning;
        }

        // Extra gravity
        velocity.Linear.y -= 9.81f * (carData.gravityMultiplier - 1f) * SystemAPI.Time.DeltaTime;

        // Cap horizontal speed
        float2 horizontalVelocity = new float2(velocity.Linear.x, velocity.Linear.z);
        float horizontalSpeed = math.length(horizontalVelocity);
        if (horizontalSpeed > carData.maxSpeed)
        {
            float2 clamped = math.normalize(horizontalVelocity) * carData.maxSpeed;
            velocity.Linear.x = clamped.x;
            velocity.Linear.z = clamped.y;
        }

        // Only clamp upward velocity when airborne - not on a slope
        if (!onGround && velocity.Linear.y > carData.maxUpwardSpeed)
        {
            velocity.Linear.y = carData.maxUpwardSpeed;
        }

        state.EntityManager.SetComponentData(carEntity, velocity);
    }

}
