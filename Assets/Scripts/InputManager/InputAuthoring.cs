using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputAuthoring : MonoBehaviour
{
    public InputActionAsset InputActions;

    public class Baker : Baker<InputAuthoring>
    {
        public override void Bake(InputAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            // We use AddComponentObject to store the managed InputActionAsset
            AddComponentObject(entity, new InputManagedComponent { Actions = authoring.InputActions });
            AddComponent(entity, new InputData());
        }
    }
}

public class InputManagedComponent : IComponentData
{
    public InputActionAsset Actions;
}