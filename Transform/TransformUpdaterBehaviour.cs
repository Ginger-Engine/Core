using System.Numerics;
using Engine.Core.Behaviours;
using Engine.Core.Entities;

namespace Engine.Core.Transform;

public class TransformUpdaterBehaviour : IEntityBehaviour
{
    public void OnAttach(Entity entity)
    {
        if (!entity.IsComponentExists<WorldTransformComponent>())
        {
            entity.AddComponent(new WorldTransformComponent());
        }
    }

    public void OnStart(Entity entity)
    {
        UpdateTransformRecursive(entity);
        entity.SubscribeComponentChange<TransformComponent>((newValue, oldValue) =>
        {
            UpdateTransformRecursive(entity);
        });
    }

    private void UpdateTransformRecursive(Entity entity)
    {
        var parent = entity.Parent;
        var parentPosition = new Vector2();
        var parentScale = new Vector2(1, 1);
        var parentRotation = 0f;
        if (parent != null)
        {
            if (!parent.TryGetComponent<WorldTransformComponent>(out var parentWorld))
            {
                throw new Exception("Parent entity has no WorldTransformComponent");
            }
            parentPosition = parentWorld.Position;
            parentScale = parentWorld.Scale;
            parentRotation = parentWorld.Rotation;
        }

        if (!entity.TryGetComponent<TransformComponent>(out var local))
        {
            throw new Exception("Entity has no TransformComponent");
        }

        entity.ApplyComponent(new WorldTransformComponent
        {
            Position = parentPosition + local.Position,
            Scale = parentScale * local.Scale,
            Rotation = parentRotation + local.Rotation,
        });

        foreach (var child in entity.Children)
            UpdateTransformRecursive(child);
    }
}