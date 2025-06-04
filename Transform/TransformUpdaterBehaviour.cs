using Engine.Core.Behaviours;
using Engine.Core.Entities;

namespace Engine.Core.Transform;

public class TransformUpdaterBehaviour : IEntityBehaviour
{
    public void OnStart(Entity entity)
    {
        if (!entity.IsComponentExists<WorldTransformComponent>())
        {
            entity.AddComponent(new WorldTransformComponent());
        }
        UpdateTransformRecursive(entity); 
        entity.SubscribeComponentChange<TransformComponent>((newValue, oldValue) =>
        {
            UpdateTransformRecursive(entity);
        });
    }

    private void UpdateTransformRecursive(Entity entity)
    {
        var parent = entity.Parent;

        if (parent == null)
        {
            return;
        }

        if (!parent.TryGetComponent<WorldTransformComponent>(out var parentWorld))
        {
            throw new Exception();
        }

        if (!entity.TryGetComponent<TransformComponent>(out var local))
        {
            throw new Exception();
        }

        entity.ApplyComponent(new WorldTransformComponent
        {
            Position = parentWorld.Position + local.Position,
            Scale = parentWorld.Scale * local.Scale,
            Rotation = parentWorld.Rotation + local.Rotation,
        });

        foreach (var child in entity.Children)
            UpdateTransformRecursive(child);
    }
}