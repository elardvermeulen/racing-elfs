using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial class InputSystem : SystemBase
{
    private InputAction accelerateAction;
    private InputAction turnAction;

    protected override void OnUpdate()
    {
        // 1. Ensure the singleton exists
        if (  !SystemAPI.ManagedAPI.TryGetSingleton<InputManagedComponent>(out var managedInput)) return;
        Debug.Log("Input System: Found Managed Input Singleton");
        // 2. Initialize actions if needed (first run)
        if (turnAction == null)
        {
            var map = managedInput.Actions.FindActionMap("Main");
            turnAction = map.FindAction("Turn");
            accelerateAction = map.FindAction("Accelerate");

            managedInput.Actions.Enable();
        }

        // 3. Read values from the managed Input System
        float turnInput = turnAction.ReadValue<float>();
        float accelerateInput = accelerateAction.ReadValue<float>();


        // 4. Update the unmanaged ECS Singleton for use in Burst jobs
        SystemAPI.SetSingleton(new InputData
        {
            turnInput = turnInput,
            accelerateInput = accelerateInput
        });
    }
}
